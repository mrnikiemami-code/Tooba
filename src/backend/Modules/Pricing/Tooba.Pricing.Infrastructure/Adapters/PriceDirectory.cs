using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Offer.Contracts;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Pricing.Application;
using Tooba.Pricing.Contracts;
using Tooba.Pricing.Domain;
using Tooba.Pricing.Infrastructure.Persistence;

namespace Tooba.Pricing.Infrastructure;

/// <summary>
/// نگهبان باز موردکاربرد. ماتریس ادمین قیمت اینجا نیست.
/// </summary>
public sealed class OpenPricingUseCaseGuard : IPricingUseCaseGuard
{
    /// <inheritdoc />
    public Task EnsureCanMutateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>
/// نوشتن و انتخاب قیمت با قرارداد Offer. DbContext کاتالوگ و Offer لمس نمی‌شود.
/// </summary>
public sealed class PriceDirectory : IPriceDirectory, IPriceLookupGateway, ISellerOfferPricingGateway
{
    private readonly PricingDbContext _db;
    private readonly IPricingUseCaseGuard _guard;
    private readonly IOfferLookupGateway _offers;

    /// <summary>
    /// دایرکتوری را به schema Pricing و درز Offer وصل می‌کند نه به join بین‌schema.
    /// </summary>
    public PriceDirectory(PricingDbContext db, IPricingUseCaseGuard guard, IOfferLookupGateway offers)
    {
        _db = db;
        _guard = guard;
        _offers = offers;
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
                        && x.Channel == query.Channel
                        && x.Currency == currency.Value
                        && x.QualifierKind == PriceQualifierKind.Base
                        && x.Status == PriceStatus.Active)
            .ToListAsync(cancellationToken);
        var effective = matches.Where(x => x.IsEffectiveAt(query.At)).ToList();
        if (effective.Count > 1)
        {
            throw new InvalidOperationException("چند قیمت پایهٔ فعال هم‌پوشان برای همین کلید انتخاب وجود دارد.");
        }

        return effective.Count == 0 ? null : ToQuote(effective[0]);
    }

    /// <inheritdoc />
    public async Task SetPriceAsync(SetSellerOfferPrice request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Amount < 0)
            throw new SemanticException(new SemanticError(PricingErrorCodes.AmountInvalid));
        var offer = await _offers.FindOfferAsync(request.OfferId, cancellationToken);
        if (offer is null || offer.SellerPartyId != request.SellerPartyId)
            throw new SemanticException(new SemanticError(OfferErrorCodes.NotFound));
        var market = string.IsNullOrWhiteSpace(request.Market) ? "IR" : request.Market.Trim();
        var currency = string.IsNullOrWhiteSpace(request.Currency) ? "IRR" : request.Currency.Trim();
        var existing = await _db.Prices.AsNoTracking()
            .Where(x => x.OfferId == request.OfferId && x.Market == market
                        && x.Channel == offer.Channel && x.Currency == currency)
            .OrderByDescending(x => x.PriceId)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is null || existing.Status == PriceStatus.Retired)
        {
            var created = await CreatePriceAsync(
                request.OfferId, market, offer.Channel, request.Amount, currency,
                DateTimeOffset.UtcNow.AddYears(-1), null, cancellationToken);
            await ActivateAsync(created.PriceId, cancellationToken);
            return;
        }

        await ChangeAmountAsync(existing.PriceId, request.Amount, currency, cancellationToken);
        if (existing.Status != PriceStatus.Active)
            await ActivateAsync(existing.PriceId, cancellationToken);
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
        var ids = offerIds.Distinct().ToArray();
        var matches = await _db.Prices.AsNoTracking()
            .Where(x => ids.Contains(x.OfferId)
                        && x.Market == marketCode.Value
                        && x.Channel == channel
                        && x.Currency == currencyCode.Value
                        && x.QualifierKind == PriceQualifierKind.Base
                        && x.Status == PriceStatus.Active)
            .ToListAsync(cancellationToken);

        var result = new Dictionary<Guid, PriceQuote>();
        foreach (var group in matches.GroupBy(x => x.OfferId))
        {
            var effective = group.Where(x => x.IsEffectiveAt(at)).ToList();
            if (effective.Count > 1)
            {
                throw new InvalidOperationException(
                    $"چند قیمت پایهٔ فعال هم‌پوشان برای Offer {group.Key} وجود دارد.");
            }

            if (effective.Count == 1)
            {
                result[group.Key] = ToQuote(effective[0]);
            }
        }

        return result;
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
        var key = campaignId.ToString("D");
        var ids = offerIds.Distinct().ToArray();
        var matches = await _db.Prices.AsNoTracking()
            .Where(x => ids.Contains(x.OfferId)
                        && x.Market == marketCode.Value
                        && x.Channel == channel
                        && x.Currency == currencyCode.Value
                        && x.QualifierKind == PriceQualifierKind.MerchandisingCampaign
                        && x.QualifierKey == key
                        && x.Status == PriceStatus.Active)
            .ToListAsync(cancellationToken);

        var result = new Dictionary<Guid, PriceQuote>();
        foreach (var group in matches.GroupBy(x => x.OfferId))
        {
            var effective = group.Where(x => x.IsEffectiveAt(at)).ToList();
            if (effective.Count > 1)
            {
                throw new InvalidOperationException(
                    $"چند قیمت کمپین فعال هم‌پوشان برای Offer {group.Key} و Campaign {campaignId} وجود دارد.");
            }

            if (effective.Count == 1)
            {
                result[group.Key] = ToQuote(effective[0]);
            }
        }

        return result;
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
        if (await _offers.FindOfferAsync(offerId, cancellationToken) is null)
        {
            throw new InvalidOperationException("Offer از قرارداد Lookup پیدا نشد؛ DbContext Offer خوانده نشد.");
        }

        var price = AuthoredPrice.Create(offerId, market, channel, amount, currency, validFrom, validTo, DateTimeOffset.UtcNow);
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
        if (await _offers.FindOfferAsync(offerId, cancellationToken) is null)
        {
            throw new InvalidOperationException("Offer از قرارداد Lookup پیدا نشد؛ DbContext Offer خوانده نشد.");
        }

        var price = AuthoredPrice.CreateMerchandisingCampaign(
            offerId,
            campaignId,
            market,
            channel,
            amount,
            currency,
            validFrom,
            validTo,
            DateTimeOffset.UtcNow);
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
        price.Activate(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ChangeAmountAsync(Guid priceId, decimal amount, string currency, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var price = await _db.Prices.SingleAsync(x => x.PriceId == priceId, cancellationToken);
        price.ChangeAmount(amount, currency, DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ExpireAsync(Guid priceId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var price = await _db.Prices.SingleAsync(x => x.PriceId == priceId, cancellationToken);
        price.Expire(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
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
            throw new InvalidOperationException("قیمت فعال هم‌پوشان برای Offer و بازار و کانال و ارز و محدودکننده مجاز نیست.");
        }
    }

    private static PriceQuote ToQuote(AuthoredPrice price) =>
        new(
            price.PriceId,
            price.OfferId,
            price.Market,
            price.Channel,
            price.Amount,
            price.Currency,
            TaxExclusive: true,
            IsAuthored: true);
}
