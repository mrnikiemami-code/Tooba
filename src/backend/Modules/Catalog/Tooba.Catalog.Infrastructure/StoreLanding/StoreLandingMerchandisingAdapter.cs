using Tooba.BuildingBlocks;
using Tooba.Catalog.Application.StoreLandingPages.Ports;
using Tooba.Promotion.Contracts.Merchandising;

namespace Tooba.Catalog.Infrastructure.StoreLanding;

/// <summary>Catalog adapter: Promotion merchandising query → Catalog Landing port.</summary>
internal sealed class StoreLandingMerchandisingAdapter(
    IMerchandisingCampaignQuery campaigns,
    ICurrentCommerceContext commerce) : IStoreLandingMerchandisingPort
{
    public int MaxMemberTake => MerchandisingCampaignRuntimeLimits.MaxMemberTake;

    public string AmazingTypeCode => MerchandisingPromotionTypeCodes.Amazing;

    public Guid? ResolveStoreId()
    {
        var tenantId = commerce.Current?.Tenant?.TenantId.Value;
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return null;
        }

        if (string.Equals(tenantId, "store-alpha", StringComparison.OrdinalIgnoreCase))
        {
            return MerchandisingDevelopmentIds.StoreAlphaId;
        }

        return Guid.TryParse(tenantId, out var parsed) ? parsed : null;
    }

    public async Task<IReadOnlyList<StoreLandingMerchandisingMember>> ResolveCampaignMembersAsync(
        Guid campaignId,
        Guid storeId,
        string locale,
        DateTimeOffset now,
        int take,
        CancellationToken cancellationToken)
    {
        var members = await campaigns.ResolveCampaignMembersAsync(
            campaignId,
            storeId,
            locale,
            now,
            take,
            null,
            cancellationToken);
        return members.Select(Map).ToList();
    }

    public async Task<StoreLandingMerchandisingCampaign?> ResolveActiveByTypeAsync(
        Guid storeId,
        string typeCode,
        string locale,
        DateTimeOffset now,
        int take,
        CancellationToken cancellationToken)
    {
        var active = await campaigns.ResolveActiveByTypeAsync(
            storeId,
            typeCode,
            locale,
            now,
            take,
            null,
            cancellationToken);
        if (active is null)
        {
            return null;
        }

        return new StoreLandingMerchandisingCampaign(
            active.CampaignId,
            active.BadgeText,
            active.Members.Select(Map).ToList());
    }

    private static StoreLandingMerchandisingMember Map(MerchandisingCampaignMemberRuntimeModel m) => new(
        m.CatalogVariantId,
        m.IsMarketable,
        m.AvailableQuantity,
        m.PriceAmount,
        m.PriceCurrency,
        m.CompareAtAmount);
}
