using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.BuildingBlocks.Results;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Errors;
using Tooba.Offer.Contracts.Ports;
using Tooba.Pricing.Application.Ports;
using Tooba.Pricing.Contracts;
using Tooba.Pricing.Contracts.Ports;
using Tooba.Pricing.Contracts.Seller;
using Tooba.Pricing.Contracts.Dtos;
using Tooba.Pricing.Contracts.Errors;
using Tooba.Pricing.Domain.Aggregates;
using Tooba.Pricing.Domain.Enums;
using Tooba.Pricing.Domain.Events;
using Tooba.Pricing.Domain.ValueObjects;
using Tooba.Pricing.Infrastructure.Persistence;

namespace Tooba.Pricing.Infrastructure.Adapters;

/// <summary>Open use-case guard. Pricing admin matrix is not implemented here.</summary>
public sealed class OpenPricingUseCaseGuard : IPricingUseCaseGuard
{
    /// <inheritdoc />
    public Task EnsureCanMutateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>
/// Authored-price writer and reader. Offer is reached only through <see cref="IOfferLookupGateway"/>.
/// </summary>
public sealed class PriceDirectory : IPriceDirectory, IPriceLookupGateway, ISellerOfferPricingGateway, IPriceQueryGateway, IOfferPriceHistoryReader
{
    private readonly PricingDbContext _db;
    private readonly IPricingUseCaseGuard _guard;
    private readonly IOfferLookupGateway _offers;
    private readonly IClock _clock;
    private readonly IIdGenerator _ids;
    private readonly IModuleCallTracer _tracer;

    /// <summary>Binds the directory to the Pricing schema and the Offer lookup contract.</summary>
    public PriceDirectory(
        PricingDbContext db,
        IPricingUseCaseGuard guard,
        IOfferLookupGateway offers,
        IClock? clock = null,
        IIdGenerator? ids = null,
        IModuleCallTracer? tracer = null)
    {
        _db = db;
        _guard = guard;
        _offers = offers;
        _clock = clock ?? new SystemUtcClock();
        _ids = ids ?? new UuidV7IdGenerator();
        _tracer = tracer ?? new ModuleCallTracer();
    }

    /// <inheritdoc />
    public async Task<PriceQuote?> ResolvePriceAsync(PriceResolutionQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var market = MarketCode.Parse(query.Market);
        var currency = CurrencyCode.Parse(query.Currency);
        var matches = await _db.Prices.AsNoTracking()
            .Where(x => x.OfferId == query.OfferId
                        && x.Market == market.Value
                        && x.Channel == ToPriceChannel(query.Channel)
                        && x.Currency == currency.Value
                        && x.QualifierKind == PriceQualifierKind.Base
                        && x.Status == PriceStatus.Active)
            .ToListAsync(cancellationToken);
        var effective = matches.Where(x => x.IsEffectiveAt(query.At)).ToList();
        if (effective.Count > 1)
        {
            throw new SemanticException(new SemanticError(PricingErrorCodes.Overlap));
        }

        return effective.Count == 0 ? null : ToQuote(effective[0]);
    }

    /// <inheritdoc />
    public async Task<Result<SellerPriceWriteReceipt>> SetPriceAsync(
        SetSellerOfferPrice request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Amount < 0)
            return Result.Failure<SellerPriceWriteReceipt>(new SemanticError(PricingErrorCodes.AmountInvalid));

        var offer = await FindOfferAsync(request.OfferId, cancellationToken);
        if (offer is null || offer.SellerPartyId != request.SellerPartyId)
            return Result.Failure<SellerPriceWriteReceipt>(new SemanticError(OfferErrorCodes.NotFound));

        var market = string.IsNullOrWhiteSpace(request.Market) ? "IR" : request.Market.Trim();
        var currency = string.IsNullOrWhiteSpace(request.Currency) ? "IRR" : request.Currency.Trim();
        if (!MarketCode.TryParse(market, out var marketCode, out var marketError))
            return Result.Failure<SellerPriceWriteReceipt>(marketError!);
        if (!CurrencyCode.TryParse(currency, out var currencyCode, out var currencyError))
            return Result.Failure<SellerPriceWriteReceipt>(currencyError!);
        market = marketCode.Value;
        currency = currencyCode.Value;
        var channel = ToPriceChannel(offer.Channel);
        var operationId = request.OperationId == Guid.Empty ? _ids.NewId() : request.OperationId;

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var replay = await _db.PriceChanges.AsNoTracking()
            .SingleOrDefaultAsync(x => x.OperationId == operationId, cancellationToken);
        if (replay is not null)
        {
            if (replay.OfferId != request.OfferId || replay.NewAmount != request.Amount
                || !string.Equals(replay.Currency, currency, StringComparison.Ordinal))
            {
                return Result.Failure<SellerPriceWriteReceipt>(new SemanticError(FoundationErrorCodes.ConcurrencyConflict));
            }
            return Result.Success(ToReceipt(replay));
        }

        var latest = await _db.PriceChanges.AsNoTracking()
            .Where(x => x.OfferId == request.OfferId)
            .OrderByDescending(x => x.OccurredAt)
            .ThenByDescending(x => x.FactId)
            .FirstOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(request.ExpectedRevision)
            || (request.ExpectedRevision == "absent"
                ? latest is not null
                : latest is null || !string.Equals(latest.Revision, request.ExpectedRevision, StringComparison.Ordinal)))
        {
            return Result.Failure<SellerPriceWriteReceipt>(new SemanticError(FoundationErrorCodes.ConcurrencyConflict));
        }

        var existing = await _db.Prices
            .Where(x => x.OfferId == request.OfferId && x.Market == market
                        && x.Channel == channel && x.Currency == currency
                        && x.QualifierKind == PriceQualifierKind.Base)
            .OrderByDescending(x => x.PriceId)
            .FirstOrDefaultAsync(cancellationToken);
        var now = _clock.UtcNow;
        AuthoredPrice price;
        decimal? oldAmount;
        if (existing is null || existing.Status == PriceStatus.Retired)
        {
            if (await HasActiveBaseOverlapAsync(request.OfferId, market, channel, currency, Guid.Empty, cancellationToken))
                return Result.Failure<SellerPriceWriteReceipt>(new SemanticError(PricingErrorCodes.Overlap));
            price = AuthoredPrice.Create(_ids.NewId(), request.OfferId, market, channel,
                request.Amount, currency, now.AddYears(-1), null, now);
            price.Activate(now);
            _db.Prices.Add(price);
            oldAmount = null;
        }
        else
        {
            price = existing;
            oldAmount = price.Amount;
            await EnsureNoOverlapAsync(price, price.PriceId, cancellationToken);
            price.ChangeAmount(request.Amount, currency, now);
            if (price.Status != PriceStatus.Active)
                price.Activate(now);
        }

        var fact = new PriceChangeFact
        {
            FactId = _ids.NewId(),
            OfferId = request.OfferId,
            PriceId = price.PriceId,
            OperationId = operationId,
            ActorUserId = request.ActorUserId,
            OccurredAt = now,
            OldAmount = oldAmount,
            NewAmount = request.Amount,
            Currency = currency,
            Revision = _ids.NewId().ToString("N"),
        };
        _db.PriceChanges.Add(fact);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure<SellerPriceWriteReceipt>(new SemanticError(FoundationErrorCodes.ConcurrencyConflict));
        }

        return Result.Success(ToReceipt(fact));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SellerPriceChangeFact>> ListAsync(Guid offerId, CancellationToken cancellationToken) =>
        await _db.PriceChanges.AsNoTracking()
            .Where(x => x.OfferId == offerId)
            .OrderBy(x => x.OccurredAt).ThenBy(x => x.FactId)
            .Select(x => new SellerPriceChangeFact(x.FactId, x.OfferId, x.PriceId, x.OperationId,
                x.ActorUserId, x.OccurredAt, x.OldAmount, x.NewAmount, x.Currency, x.Revision))
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<SellerPriceRevision?> GetRevisionAsync(Guid offerId, CancellationToken cancellationToken)
    {
        var fact = await _db.PriceChanges.AsNoTracking()
            .Where(x => x.OfferId == offerId)
            .OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.FactId)
            .FirstOrDefaultAsync(cancellationToken);
        return fact is null
            ? null
            : new SellerPriceRevision(fact.Revision, fact.PriceId, fact.NewAmount, fact.Currency, fact.OccurredAt);
    }

    private static SellerPriceWriteReceipt ToReceipt(PriceChangeFact fact) =>
        new(fact.PriceId, fact.OfferId, fact.Revision, fact.OperationId, fact.ActorUserId,
            fact.OccurredAt, fact.OldAmount, fact.NewAmount, fact.Currency);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, PriceQuote>> ResolvePricesBatchAsync(
        IReadOnlyCollection<Guid> offerIds,
        string market,
        SalesChannel channel,
        string currency,
        DateTimeOffset at,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(offerIds);
        if (offerIds.Count == 0)
        {
            return new Dictionary<Guid, PriceQuote>();
        }

        var marketCode = MarketCode.Parse(market);
        var currencyCode = CurrencyCode.Parse(currency);
        var priceChannel = ToPriceChannel(channel);
        var ids = offerIds.Distinct().ToArray();
        var matches = await _db.Prices.AsNoTracking()
            .Where(x => ids.Contains(x.OfferId)
                        && x.Market == marketCode.Value
                        && x.Channel == priceChannel
                        && x.Currency == currencyCode.Value
                        && x.QualifierKind == PriceQualifierKind.Base
                        && x.Status == PriceStatus.Active)
            .ToListAsync(cancellationToken);

        return ToEffectiveQuotes(matches, at);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, PriceQuote>> ResolveCampaignPricesBatchAsync(
        IReadOnlyCollection<Guid> offerIds,
        Guid campaignId,
        string market,
        SalesChannel channel,
        string currency,
        DateTimeOffset at,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(offerIds);
        if (offerIds.Count == 0 || campaignId == Guid.Empty)
        {
            return new Dictionary<Guid, PriceQuote>();
        }

        var marketCode = MarketCode.Parse(market);
        var currencyCode = CurrencyCode.Parse(currency);
        var priceChannel = ToPriceChannel(channel);
        var key = campaignId.ToString("D");
        var ids = offerIds.Distinct().ToArray();
        var matches = await _db.Prices.AsNoTracking()
            .Where(x => ids.Contains(x.OfferId)
                        && x.Market == marketCode.Value
                        && x.Channel == priceChannel
                        && x.Currency == currencyCode.Value
                        && x.QualifierKind == PriceQualifierKind.MerchandisingCampaign
                        && x.QualifierKey == key
                        && x.Status == PriceStatus.Active)
            .ToListAsync(cancellationToken);

        return ToEffectiveQuotes(matches, at);
    }

    /// <inheritdoc />
    public async Task<PriceQuote> CreatePriceAsync(
        Guid offerId,
        string market,
        SalesChannel channel,
        decimal amount,
        string currency,
        DateTimeOffset validFrom,
        DateTimeOffset? validTo,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        await RequireOfferAsync(offerId, cancellationToken);
        var price = AuthoredPrice.Create(
            _ids.NewId(),
            offerId,
            market,
            ToPriceChannel(channel),
            amount,
            currency,
            validFrom,
            validTo,
            _clock.UtcNow);
        _db.Prices.Add(price);
        await _db.SaveChangesAsync(cancellationToken);
        return ToQuote(price);
    }

    /// <inheritdoc />
    public async Task<PriceQuote> CreateCampaignPriceAsync(
        Guid offerId,
        Guid campaignId,
        string market,
        SalesChannel channel,
        decimal amount,
        string currency,
        DateTimeOffset validFrom,
        DateTimeOffset? validTo,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        await RequireOfferAsync(offerId, cancellationToken);
        var price = AuthoredPrice.CreateMerchandisingCampaign(
            _ids.NewId(),
            offerId,
            campaignId,
            market,
            ToPriceChannel(channel),
            amount,
            currency,
            validFrom,
            validTo,
            _clock.UtcNow);
        _db.Prices.Add(price);
        await _db.SaveChangesAsync(cancellationToken);
        return ToQuote(price);
    }

    /// <inheritdoc />
    public async Task ActivateAsync(Guid priceId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var price = await _db.Prices.SingleAsync(x => x.PriceId == priceId, cancellationToken);
        await EnsureNoOverlapAsync(price, price.PriceId, cancellationToken);
        price.Activate(_clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ChangeAmountAsync(Guid priceId, decimal amount, string currency, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var price = await _db.Prices.SingleAsync(x => x.PriceId == priceId, cancellationToken);
        price.ChangeAmount(amount, currency, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ExpireAsync(Guid priceId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var price = await _db.Prices.SingleAsync(x => x.PriceId == priceId, cancellationToken);
        price.Expire(_clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OfferAmountRow>> ListOfferAmountsAsync(CancellationToken cancellationToken)
    {
        var rows = await _db.Prices.AsNoTracking()
            .Select(p => new OfferAmountRow(p.OfferId, p.Amount))
            .ToListAsync(cancellationToken);
        return rows;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuthoredPriceSnapshot>> ListByOfferIdsAsync(
        IReadOnlyCollection<Guid> offerIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(offerIds);
        if (offerIds.Count == 0)
        {
            return [];
        }

        var ids = offerIds.Distinct().ToArray();
        var rows = await _db.Prices.AsNoTracking()
            .Where(x => ids.Contains(x.OfferId))
            .ToListAsync(cancellationToken);
        return rows.Select(ToSnapshot).ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> ListActiveCampaignOfferIdsAsync(
        IReadOnlyCollection<Guid> offerIds,
        string campaignKey,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(offerIds);
        if (offerIds.Count == 0 || string.IsNullOrWhiteSpace(campaignKey))
        {
            return [];
        }

        var ids = offerIds.Distinct().ToArray();
        return await _db.Prices.AsNoTracking()
            .Where(x =>
                ids.Contains(x.OfferId)
                && x.QualifierKind == PriceQualifierKind.MerchandisingCampaign
                && x.QualifierKey == campaignKey
                && x.Status == PriceStatus.Active)
            .Select(x => x.OfferId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AuthoredPriceSnapshot?> FindLatestActiveBaseAsync(
        Guid offerId,
        string market,
        string currency,
        CancellationToken cancellationToken)
    {
        var row = await _db.Prices.AsNoTracking()
            .Where(x =>
                x.OfferId == offerId
                && x.QualifierKind == PriceQualifierKind.Base
                && x.Status == PriceStatus.Active
                && x.Market == market
                && x.Currency == currency)
            .OrderByDescending(x => x.ValidFrom)
            .FirstOrDefaultAsync(cancellationToken);
        return row is null ? null : ToSnapshot(row);
    }

    private async Task RequireOfferAsync(Guid offerId, CancellationToken cancellationToken)
    {
        if (await FindOfferAsync(offerId, cancellationToken) is null)
        {
            throw new SemanticException(new SemanticError(PricingErrorCodes.OfferMissing));
        }
    }

    private async Task<OfferReference?> FindOfferAsync(Guid offerId, CancellationToken cancellationToken)
    {
        using var trace = _tracer.Begin("Pricing", "Offer", "LookupOffer");
        try
        {
            var offer = await _offers.FindOfferAsync(offerId, cancellationToken).ConfigureAwait(false);
            trace.SetOk();
            return offer;
        }
        catch (Exception ex)
        {
            trace.SetError(ex);
            throw;
        }
    }

    private async Task<bool> HasActiveBaseOverlapAsync(
        Guid offerId,
        string market,
        PriceChannel channel,
        string currency,
        Guid excludePriceId,
        CancellationToken cancellationToken)
    {
        var siblings = await _db.Prices.AsNoTracking()
            .Where(x => x.OfferId == offerId
                        && x.Market == market
                        && x.Channel == channel
                        && x.Currency == currency
                        && x.QualifierKind == PriceQualifierKind.Base
                        && x.Status == PriceStatus.Active
                        && x.PriceId != excludePriceId)
            .ToListAsync(cancellationToken);
        var windowStart = _clock.UtcNow.AddYears(-1);
        var windowEnd = DateTimeOffset.MaxValue;
        return siblings.Any(x =>
        {
            var otherEnd = x.ValidTo ?? DateTimeOffset.MaxValue;
            return windowStart < otherEnd && x.ValidFrom < windowEnd;
        });
    }

    private async Task EnsureNoOverlapAsync(AuthoredPrice candidate, Guid excludePriceId, CancellationToken cancellationToken)
    {
        var siblings = await _db.Prices
            .Where(x => x.OfferId == candidate.OfferId
                        && x.Market == candidate.Market
                        && x.Channel == candidate.Channel
                        && x.Currency == candidate.Currency
                        && x.QualifierKind == candidate.QualifierKind
                        && x.QualifierKey == candidate.QualifierKey
                        && x.Status == PriceStatus.Active
                        && x.PriceId != excludePriceId)
            .ToListAsync(cancellationToken);
        if (siblings.Any(candidate.Overlaps))
        {
            throw new SemanticException(new SemanticError(PricingErrorCodes.Overlap));
        }
    }

    private static Dictionary<Guid, PriceQuote> ToEffectiveQuotes(List<AuthoredPrice> matches, DateTimeOffset at)
    {
        var result = new Dictionary<Guid, PriceQuote>();
        foreach (var group in matches.GroupBy(x => x.OfferId))
        {
            var effective = group.Where(x => x.IsEffectiveAt(at)).ToList();
            if (effective.Count > 1)
            {
                throw new SemanticException(new SemanticError(PricingErrorCodes.Overlap));
            }

            if (effective.Count == 1)
            {
                result[group.Key] = ToQuote(effective[0]);
            }
        }

        return result;
    }

    private static PriceQuote ToQuote(AuthoredPrice price) =>
        new(
            price.PriceId,
            price.OfferId,
            price.Market,
            ToSalesChannel(price.Channel),
            price.Amount,
            price.Currency,
            TaxExclusive: true,
            IsAuthored: true);

    private static AuthoredPriceSnapshot ToSnapshot(AuthoredPrice price) =>
        new(
            price.PriceId,
            price.OfferId,
            price.Market,
            ToSalesChannel(price.Channel),
            price.Amount,
            price.Currency,
            price.Status.ToString(),
            price.ValidFrom,
            price.ValidTo,
            price.QualifierKind.ToString(),
            price.QualifierKey);

    private static PriceChannel ToPriceChannel(SalesChannel channel) =>
        Enum.Parse<PriceChannel>(channel.ToString());

    private static SalesChannel ToSalesChannel(PriceChannel channel) =>
        Enum.Parse<SalesChannel>(channel.ToString());
}

