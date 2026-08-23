using Microsoft.EntityFrameworkCore;
using Tooba.Offer.Application;
using Tooba.Offer.Domain;
using Tooba.Pricing.Application;
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
public sealed class PriceDirectory : IPriceDirectory, IPriceLookupGateway
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
                        && x.Status == PriceStatus.Active
                        && x.PriceId != excludePriceId)
            .ToListAsync(cancellationToken);
        if (siblings.Any(candidate.Overlaps))
        {
            throw new InvalidOperationException("قیمت پایهٔ فعال هم‌پوشان برای Offer و بازار و کانال و ارز مجاز نیست.");
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
