using Microsoft.EntityFrameworkCore;
using Tooba.Inventory.Application;
using Tooba.Offer.Application;
using Tooba.Offer.Domain;
using Tooba.Pricing.Application;
using Tooba.Promotion.Application;
using Tooba.Promotion.Domain;
using Tooba.Promotion.Infrastructure.Persistence;

namespace Tooba.Promotion.Infrastructure;

/// <summary>
/// خوانش زمان‌اجرای کمپین با batch Offer/Price/Inventory — بدون N+1.
/// </summary>
public sealed class MerchandisingCampaignQuery : IMerchandisingCampaignQuery
{
    private readonly PromotionDbContext _db;
    private readonly IOfferLookupGateway _offers;
    private readonly IPriceLookupGateway _prices;
    private readonly IInventoryAvailabilityGateway _inventory;

    /// <summary>
    /// کوئری را به schema promotion و درزهای Offer/Price/Inventory وصل می‌کند.
    /// </summary>
    public MerchandisingCampaignQuery(
        PromotionDbContext db,
        IOfferLookupGateway offers,
        IPriceLookupGateway prices,
        IInventoryAvailabilityGateway inventory)
    {
        _db = db;
        _offers = offers;
        _prices = prices;
        _inventory = inventory;
    }

    /// <inheritdoc />
    public async Task<MerchandisingCampaignRuntimeModel?> ResolveActiveByTypeAsync(
        Guid storeId,
        string typeCode,
        string locale,
        DateTimeOffset now,
        int take,
        MerchandisingPriceScope? priceScope,
        CancellationToken cancellationToken)
    {
        var type = await FindActiveTypeAsync(typeCode, cancellationToken);
        if (type is null)
        {
            return null;
        }

        var winner = await _db.MerchandisingCampaigns.AsNoTracking()
            .Where(x =>
                x.StoreId == storeId
                && x.PromotionTypeId == type.Id
                && x.LifecycleStatus == MerchandisingCampaignLifecycleStatus.Published
                && x.StartAt <= now
                && (x.EndAt == null || x.EndAt > now))
            .OrderByDescending(x => x.Priority)
            .ThenByDescending(x => x.StartAt)
            .ThenBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (winner is null)
        {
            return null;
        }

        return await ProjectCampaignAsync(
            winner,
            type.Code,
            locale,
            now,
            take,
            priceScope ?? MerchandisingPriceScope.Default,
            filterUnavailable: true,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<MerchandisingCampaignRuntimeModel?> ResolveFutureByTypeAsync(
        Guid storeId,
        string typeCode,
        string locale,
        DateTimeOffset now,
        int take,
        MerchandisingPriceScope? priceScope,
        CancellationToken cancellationToken)
    {
        var type = await FindActiveTypeAsync(typeCode, cancellationToken);
        if (type is null)
        {
            return null;
        }

        var future = await _db.MerchandisingCampaigns.AsNoTracking()
            .Where(x =>
                x.StoreId == storeId
                && x.PromotionTypeId == type.Id
                && x.LifecycleStatus == MerchandisingCampaignLifecycleStatus.Published
                && x.StartAt > now)
            .OrderBy(x => x.StartAt)
            .ThenByDescending(x => x.Priority)
            .ThenBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (future is null)
        {
            return null;
        }

        return await ProjectCampaignAsync(
            future,
            type.Code,
            locale,
            now,
            take,
            priceScope ?? MerchandisingPriceScope.Default,
            filterUnavailable: true,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MerchandisingCampaignMemberRuntimeModel>> ResolveCampaignMembersAsync(
        Guid campaignId,
        Guid storeId,
        string locale,
        DateTimeOffset now,
        int take,
        MerchandisingPriceScope? priceScope,
        CancellationToken cancellationToken)
    {
        _ = locale;
        var campaign = await _db.MerchandisingCampaigns.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == campaignId, cancellationToken);
        if (campaign is null || campaign.StoreId != storeId)
        {
            return Array.Empty<MerchandisingCampaignMemberRuntimeModel>();
        }

        return await ProjectMembersAsync(
            campaign.Id,
            now,
            take,
            priceScope ?? MerchandisingPriceScope.Default,
            filterUnavailable: true,
            cancellationToken);
    }

    private async Task<MerchandisingPromotionType?> FindActiveTypeAsync(
        string typeCode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(typeCode))
        {
            return null;
        }

        var code = typeCode.Trim().ToUpperInvariant();
        return await _db.MerchandisingPromotionTypes.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Code == code && x.IsActive, cancellationToken);
    }

    private async Task<MerchandisingCampaignRuntimeModel> ProjectCampaignAsync(
        MerchandisingCampaign campaign,
        string typeCode,
        string locale,
        DateTimeOffset now,
        int take,
        MerchandisingPriceScope priceScope,
        bool filterUnavailable,
        CancellationToken cancellationToken)
    {
        var text = await ResolveTranslationAsync(campaign.Id, locale, cancellationToken);
        var members = await ProjectMembersAsync(
            campaign.Id,
            now,
            take,
            priceScope,
            filterUnavailable,
            cancellationToken);
        var isTeasing = campaign.LifecycleStatus == MerchandisingCampaignLifecycleStatus.Published
                        && campaign.StartAt > now;
        TimeSpan? remaining = null;
        if (campaign.EndAt is { } end && end > now)
        {
            remaining = end - now;
        }

        return new MerchandisingCampaignRuntimeModel(
            campaign.Id,
            campaign.StoreId,
            typeCode,
            text.Title,
            text.Subtitle,
            text.BadgeText,
            campaign.StartAt,
            campaign.EndAt,
            campaign.Priority,
            isTeasing,
            remaining,
            members);
    }

    private async Task<IReadOnlyList<MerchandisingCampaignMemberRuntimeModel>> ProjectMembersAsync(
        Guid campaignId,
        DateTimeOffset now,
        int take,
        MerchandisingPriceScope priceScope,
        bool filterUnavailable,
        CancellationToken cancellationToken)
    {
        var capped = Math.Clamp(
            take <= 0 ? MerchandisingCampaignRuntimeLimits.MaxMemberTake : take,
            1,
            MerchandisingCampaignRuntimeLimits.MaxMemberTake);

        // بارگذاری بیش از take تا پس از فیلتر موجودی هنوز به حد برسیم؛ سقف امن 3×take حداکثر 48.
        var fetch = Math.Min(MerchandisingCampaignRuntimeLimits.MaxMemberTake, Math.Max(capped * 3, capped));
        var memberships = await _db.MerchandisingCampaignOffers.AsNoTracking()
            .Where(x => x.CampaignId == campaignId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .Take(fetch)
            .ToListAsync(cancellationToken);
        if (memberships.Count == 0)
        {
            return Array.Empty<MerchandisingCampaignMemberRuntimeModel>();
        }

        var offerIds = memberships.Select(x => x.SellerOfferId).Distinct().ToArray();
        var offers = await _offers.FindOffersBatchAsync(offerIds, cancellationToken);
        var stock = await _inventory.GetAvailabilityBatchAsync(offerIds, cancellationToken);
        var prices = await _prices.ResolvePricesBatchAsync(
            offerIds,
            priceScope.Market,
            priceScope.Channel,
            priceScope.Currency,
            now,
            cancellationToken);

        var projected = new List<MerchandisingCampaignMemberRuntimeModel>(memberships.Count);
        foreach (var membership in memberships)
        {
            if (!offers.TryGetValue(membership.SellerOfferId, out var offer)
                || offer.Status != OfferStatus.Active)
            {
                continue;
            }

            stock.TryGetValue(membership.SellerOfferId, out var availability);
            var available = availability?.Available ?? 0m;
            if (filterUnavailable && available <= 0)
            {
                continue;
            }

            prices.TryGetValue(membership.SellerOfferId, out var quote);
            projected.Add(
                new MerchandisingCampaignMemberRuntimeModel(
                    membership.SellerOfferId,
                    offer.CatalogVariantId,
                    membership.SortOrder,
                    IsMarketable: true,
                    quote?.Amount,
                    quote?.Currency,
                    available,
                    offer.MinimumOrderQuantity,
                    offer.MaximumOrderQuantity));

            if (projected.Count >= capped)
            {
                break;
            }
        }

        return projected;
    }

    private async Task<(string Title, string? Subtitle, string? BadgeText)> ResolveTranslationAsync(
        Guid campaignId,
        string locale,
        CancellationToken cancellationToken)
    {
        var rows = await _db.MerchandisingCampaignTranslations.AsNoTracking()
            .Where(x => x.CampaignId == campaignId)
            .ToListAsync(cancellationToken);
        if (rows.Count == 0)
        {
            return (string.Empty, null, null);
        }

        var requested = (locale ?? string.Empty).Trim();
        MerchandisingCampaignTranslation? Match(Func<MerchandisingCampaignTranslation, bool> predicate) =>
            rows.FirstOrDefault(predicate);

        var hit =
            Match(x => string.Equals(x.Locale, requested, StringComparison.OrdinalIgnoreCase))
            ?? Match(x => x.Locale.StartsWith("fa", StringComparison.OrdinalIgnoreCase))
            ?? Match(x => x.Locale.StartsWith("en", StringComparison.OrdinalIgnoreCase))
            ?? rows[0];

        return (hit.Title, hit.Subtitle, hit.BadgeText);
    }
}
