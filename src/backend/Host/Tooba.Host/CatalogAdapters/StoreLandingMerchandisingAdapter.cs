using Tooba.BuildingBlocks;
using Tooba.Catalog.Application.StoreLandingPages.Ports;
using Tooba.Promotion.Application.Merchandising;
using Tooba.Promotion.Domain.Merchandising;

namespace Tooba.Host.CatalogAdapters;

/// <summary>Host adapter: Promotion merchandising query → Catalog Landing port.</summary>
public sealed class StoreLandingMerchandisingAdapter : IStoreLandingMerchandisingPort
{
    private readonly IMerchandisingCampaignQuery _campaigns;
    private readonly ICurrentCommerceContext _commerce;

    /// <summary>Creates the adapter.</summary>
    public StoreLandingMerchandisingAdapter(
        IMerchandisingCampaignQuery campaigns,
        ICurrentCommerceContext commerce)
    {
        _campaigns = campaigns;
        _commerce = commerce;
    }

    /// <inheritdoc />
    public int MaxMemberTake => MerchandisingCampaignRuntimeLimits.MaxMemberTake;

    /// <inheritdoc />
    public string AmazingTypeCode => MerchandisingPromotionType.AmazingCode;

    /// <inheritdoc />
    public Guid? ResolveStoreId()
    {
        var tenantId = _commerce.Current?.Tenant?.TenantId.Value;
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

    /// <inheritdoc />
    public async Task<IReadOnlyList<StoreLandingMerchandisingMember>> ResolveCampaignMembersAsync(
        Guid campaignId,
        Guid storeId,
        string locale,
        DateTimeOffset now,
        int take,
        CancellationToken cancellationToken)
    {
        var members = await _campaigns.ResolveCampaignMembersAsync(
            campaignId,
            storeId,
            locale,
            now,
            take,
            null,
            cancellationToken);
        return members.Select(Map).ToList();
    }

    /// <inheritdoc />
    public async Task<StoreLandingMerchandisingCampaign?> ResolveActiveByTypeAsync(
        Guid storeId,
        string typeCode,
        string locale,
        DateTimeOffset now,
        int take,
        CancellationToken cancellationToken)
    {
        var active = await _campaigns.ResolveActiveByTypeAsync(
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
