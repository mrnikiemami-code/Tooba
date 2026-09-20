using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Pricing.Contracts;
using Tooba.Promotion.Domain;
using Tooba.Promotion.Infrastructure.Persistence;

namespace Tooba.Promotion.Infrastructure;

/// <summary>
/// مرجع قیمت کمپین برای Cart/Checkout: Store + runtime-active + membership + AuthoredPrice کمپین.
/// </summary>
public sealed class CampaignCartPriceAuthority : ICampaignCartPriceAuthority
{
    private readonly PromotionDbContext _db;
    private readonly IPriceLookupGateway _prices;
    private readonly ICurrentCommerceContext _commerce;

    /// <summary>
    /// مرجع را به schema promotion و قیمت canonical وصل می‌کند.
    /// </summary>
    public CampaignCartPriceAuthority(
        PromotionDbContext db,
        IPriceLookupGateway prices,
        ICurrentCommerceContext commerce)
    {
        _db = db;
        _prices = prices;
        _commerce = commerce;
    }

    /// <inheritdoc />
    public async Task<PriceQuote?> TryResolveEligibleCampaignPriceAsync(
        Guid merchandisingCampaignId,
        Guid offerId,
        string market,
        SalesChannel channel,
        string currency,
        DateTimeOffset at,
        CancellationToken cancellationToken)
    {
        if (merchandisingCampaignId == Guid.Empty || offerId == Guid.Empty)
        {
            return null;
        }

        var storeId = ResolveStoreId();
        if (storeId is null)
        {
            return null;
        }

        var campaign = await _db.MerchandisingCampaigns.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == merchandisingCampaignId, cancellationToken);
        if (campaign is null
            || campaign.StoreId != storeId.Value
            || !campaign.IsRuntimeActive(at))
        {
            return null;
        }

        var isMember = await _db.MerchandisingCampaignOffers.AsNoTracking()
            .AnyAsync(
                x => x.CampaignId == merchandisingCampaignId && x.SellerOfferId == offerId,
                cancellationToken);
        if (!isMember)
        {
            return null;
        }

        var batch = await _prices.ResolveCampaignPricesBatchAsync(
            [offerId],
            merchandisingCampaignId,
            market,
            channel,
            currency,
            at,
            cancellationToken);
        return batch.TryGetValue(offerId, out var quote) ? quote : null;
    }

    private Guid? ResolveStoreId()
    {
        var tenantId = _commerce.Current?.Tenant?.TenantId.Value;
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return null;
        }

        // Dev SingleStore store-alpha → stable seed StoreId (same as StoreLandingPageComposer).
        if (string.Equals(tenantId, "store-alpha", StringComparison.OrdinalIgnoreCase))
        {
            return Guid.Parse("aaaaaaaa-aaaa-7aaa-8aaa-aaaaaaaaaaa1");
        }

        return Guid.TryParse(tenantId, out var parsed) && parsed != Guid.Empty ? parsed : null;
    }
}
