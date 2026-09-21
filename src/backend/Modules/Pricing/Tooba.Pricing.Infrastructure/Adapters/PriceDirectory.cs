using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.BuildingBlocks.Results;
using Tooba.Offer.Contracts;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Pricing.Application;
using Tooba.Pricing.Contracts;
using Tooba.Pricing.Domain;
using PricingErrorCodes = Tooba.Pricing.Contracts.PricingErrorCodes;
using Tooba.Pricing.Infrastructure.Persistence;

namespace Tooba.Pricing.Infrastructure;

/// <summary>Open use-case guard. Pricing admin matrix is not implemented here.</summary>
public sealed class OpenPricingUseCaseGuard : IPricingUseCaseGuard
{
    /// <inheritdoc />
    public Task EnsureCanMutateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>
/// Authored-price writer and reader. Offer is reached only through <see cref="IOfferLookupGateway"/>.
/// </summary>
public sealed class PriceDirectory : IPriceDirectory, IPriceLookupGateway, ISellerOfferPricingGateway, IPriceQueryGateway
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
    public async Task<Result> SetPriceAsync(SetSellerOfferPrice request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Amount < 0)
        {
            return Result.Failure(new SemanticError(PricingErrorCodes.AmountInvalid));
        }

        var offer = await FindOfferAsync(request.OfferId, cancellationToken);
        if (offer is null || offer.SellerPartyId != request.SellerPartyId)
        {
            return Result.Failure(new SemanticError(OfferErrorCodes.NotFound));
        }

        var market = string.IsNullOrWhiteSpace(request.Market) ? "IR" : request.Market.Trim();
        var currency = string.IsNullOrWhiteSpace(request.Currency) ? "IRR" : request.Currency.Trim();
        if (!MarketCode.TryParse(market, out var marketCode, out var marketError))
        {
            return Result.Failure(marketError!);
        }

        if (!CurrencyCode.TryParse(currency, out var currencyCode, out var currencyError))
        {
            return Result.Failure(currencyError!);
        }

        market = marketCode.Value;
        currency = currencyCode.Value;
        var channel = ToPriceChannel(offer.Channel);
        var existing = await _db.Prices.AsNoTracking()
            .Where(x => x.OfferId == request.OfferId && x.Market == market
                        && x.Channel == channel && x.Currency == currency
                        && x.QualifierKind == PriceQualifierKind.Base)
            .OrderByDescending(x => x.PriceId)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is null || existing.Status == PriceStatus.Retired)
        {
            if (await HasActiveBaseOverlapAsync(request.OfferId, market, channel, currency, Guid.Empty, cancellationToken))
            {
                return Result.Failure(new SemanticError(PricingErrorCodes.Overlap));
            }

            var created = await CreatePriceAsync(
                request.OfferId, market, offer.Channel, request.Amount, currency,
                _clock.UtcNow.AddYears(-1), null, cancellationToken);
            await ActivateAsync(created.PriceId, cancellationToken);
            return Result.Success();
        }

        await ChangeAmountAsync(existing.PriceId, request.Amount, currency, cancellationToken);
        if (existing.Status != PriceStatus.Active)
        {
            if (await HasActiveBaseOverlapAsync(request.OfferId, market, channel, currency, existing.PriceId, cancellationToken))
            {
                return Result.Failure(new SemanticError(PricingErrorCodes.Overlap));
            }

            await ActivateAsync(existing.PriceId, cancellationToken);
        }

        return Result.Success();
    }

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
