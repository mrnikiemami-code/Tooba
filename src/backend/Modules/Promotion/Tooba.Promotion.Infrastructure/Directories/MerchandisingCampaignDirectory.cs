using Tooba.Promotion.Domain.Merchandising;
using Tooba.Promotion.Application.Merchandising;
﻿using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Promotion.Application.Ports;
using Tooba.Promotion.Application.Checkout;
using Tooba.Promotion.Domain.Aggregates;
using Tooba.Promotion.Domain.ValueObjects;
using Tooba.Promotion.Domain.Events;
using Tooba.Promotion.Infrastructure.Persistence;

namespace Tooba.Promotion.Infrastructure.Directories;

/// <summary>
/// مالک کمپین مرچندایزینگ در schema promotion. تعریف تخفیف تسویه را لمس نمی‌کند.
/// </summary>
public sealed class MerchandisingCampaignDirectory : IMerchandisingCampaignDirectory
{
    private readonly PromotionDbContext _db;
    private readonly IClock _clock;
    private readonly IIdGenerator _ids;

    /// <summary>
    /// دایرکتوری را به schema promotion وصل می‌کند.
    /// </summary>
    public MerchandisingCampaignDirectory(
        PromotionDbContext db,
        IClock clock,
        IIdGenerator ids)
    {
        _db = db;
        ArgumentNullException.ThrowIfNull(clock);
        _clock = clock;
        ArgumentNullException.ThrowIfNull(ids);
        _ids = ids;
    }

    /// <inheritdoc />
    public async Task<MerchandisingPromotionTypeReference> EnsureAmazingTypeSeededAsync(
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var existing = await _db.MerchandisingPromotionTypes
            .SingleOrDefaultAsync(x => x.Code == MerchandisingPromotionType.AmazingCode, cancellationToken);

        if (existing is null)
        {
            existing = MerchandisingPromotionType.CreateSystem(_ids.NewId(),
                MerchandisingPromotionType.AmazingCode,
                sortOrder: 100,
                now);
            _db.MerchandisingPromotionTypes.Add(existing);
            await _db.SaveChangesAsync(cancellationToken);
        }

        await UpsertTypeTranslationAsync(
            existing.Id,
            "fa-IR",
            "پیشنهاد شگفت‌انگیز",
            cancellationToken);
        await UpsertTypeTranslationAsync(
            existing.Id,
            "en-US",
            "Amazing Offers",
            cancellationToken);

        return ToTypeReference(existing);
    }

    /// <inheritdoc />
    public async Task<MerchandisingCampaignReference> CreateCampaignAsync(
        Guid promotionTypeId,
        Guid storeId,
        DateTimeOffset startAt,
        DateTimeOffset? endAt,
        int priority,
        CancellationToken cancellationToken)
    {
        var typeExists = await _db.MerchandisingPromotionTypes
            .AnyAsync(x => x.Id == promotionTypeId, cancellationToken);
        if (!typeExists)
        {
            throw new InvalidOperationException("promotion.type.not_found");
        }

        var campaign = MerchandisingCampaign.Create(
            _ids.NewId(),
            promotionTypeId,
            storeId,
            startAt,
            endAt,
            priority,
            _clock.UtcNow);
        _db.MerchandisingCampaigns.Add(campaign);
        await _db.SaveChangesAsync(cancellationToken);
        return ToCampaignReference(campaign);
    }

    /// <inheritdoc />
    public async Task<MerchandisingCampaignReference> UpdateCampaignWindowAsync(
        Guid campaignId,
        DateTimeOffset startAt,
        DateTimeOffset? endAt,
        int priority,
        CancellationToken cancellationToken)
    {
        var campaign = await RequireCampaignAsync(campaignId, cancellationToken);
        campaign.UpdateWindow(startAt, endAt, priority, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        return ToCampaignReference(campaign);
    }

    /// <inheritdoc />
    public async Task PublishCampaignAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        var campaign = await RequireCampaignAsync(campaignId, cancellationToken);
        campaign.Publish(_clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ArchiveCampaignAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        var campaign = await RequireCampaignAsync(campaignId, cancellationToken);
        campaign.Archive(_clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<MerchandisingCampaignTranslationReference> UpsertCampaignTranslationAsync(
        Guid campaignId,
        string locale,
        string title,
        string? subtitle,
        string? badgeText,
        CancellationToken cancellationToken)
    {
        _ = await RequireCampaignAsync(campaignId, cancellationToken);
        var normalized = locale.Trim();
        var row = await _db.MerchandisingCampaignTranslations
            .SingleOrDefaultAsync(x => x.CampaignId == campaignId && x.Locale == normalized, cancellationToken);
        if (row is null)
        {
            row = MerchandisingCampaignTranslation.Create(campaignId, normalized, title, subtitle, badgeText);
            _db.MerchandisingCampaignTranslations.Add(row);
        }
        else
        {
            row.Upsert(title, subtitle, badgeText);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new MerchandisingCampaignTranslationReference(
            row.CampaignId,
            row.Locale,
            row.Title,
            row.Subtitle,
            row.BadgeText);
    }

    /// <inheritdoc />
    public async Task<MerchandisingCampaignMemberReference> AddOfferAsync(
        Guid campaignId,
        Guid sellerOfferId,
        int sortOrder,
        Guid expectedStoreId,
        CancellationToken cancellationToken)
    {
        var campaign = await RequireCampaignAsync(campaignId, cancellationToken);
        if (campaign.StoreId != expectedStoreId)
        {
            throw new InvalidOperationException("merchandising.campaign.store_mismatch");
        }

        var exists = await _db.MerchandisingCampaignOffers.AnyAsync(
            x => x.CampaignId == campaignId && x.SellerOfferId == sellerOfferId,
            cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("domain.invariant");
        }

        var membership = MerchandisingCampaignOffer.Create(
            _ids.NewId(),
            campaignId,
            sellerOfferId,
            sortOrder,
            _clock.UtcNow);
        _db.MerchandisingCampaignOffers.Add(membership);
        await _db.SaveChangesAsync(cancellationToken);
        return ToMemberReference(membership);
    }

    /// <inheritdoc />
    public async Task RemoveOfferAsync(
        Guid campaignId,
        Guid sellerOfferId,
        CancellationToken cancellationToken)
    {
        var row = await _db.MerchandisingCampaignOffers.SingleOrDefaultAsync(
            x => x.CampaignId == campaignId && x.SellerOfferId == sellerOfferId,
            cancellationToken);
        if (row is null)
        {
            return;
        }

        _db.MerchandisingCampaignOffers.Remove(row);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ReorderOffersAsync(
        Guid campaignId,
        IReadOnlyList<Guid> orderedSellerOfferIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(orderedSellerOfferIds);
        _ = await RequireCampaignAsync(campaignId, cancellationToken);
        var members = await _db.MerchandisingCampaignOffers
            .Where(x => x.CampaignId == campaignId)
            .ToListAsync(cancellationToken);
        if (members.Count != orderedSellerOfferIds.Count
            || members.Select(x => x.SellerOfferId).ToHashSet().Count != orderedSellerOfferIds.Count
            || orderedSellerOfferIds.Any(id => members.All(m => m.SellerOfferId != id)))
        {
            throw new InvalidOperationException("domain.invariant");
        }

        for (var i = 0; i < orderedSellerOfferIds.Count; i++)
        {
            var member = members.Single(x => x.SellerOfferId == orderedSellerOfferIds[i]);
            member.SetSortOrder(i);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<MerchandisingCampaignReference?> ResolveActiveCampaignAsync(
        Guid storeId,
        string typeCode,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(typeCode))
        {
            return null;
        }

        var code = typeCode.Trim().ToUpperInvariant();
        var type = await _db.MerchandisingPromotionTypes.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Code == code && x.IsActive, cancellationToken);
        if (type is null)
        {
            return null;
        }

        var candidates = await _db.MerchandisingCampaigns.AsNoTracking()
            .Where(x =>
                x.StoreId == storeId
                && x.PromotionTypeId == type.Id
                && x.LifecycleStatus == MerchandisingCampaignLifecycleStatus.Published
                && x.StartAt <= now
                && (x.EndAt == null || x.EndAt > now))
            .OrderByDescending(x => x.Priority)
            .ThenByDescending(x => x.StartAt)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var winner = candidates.FirstOrDefault();
        return winner is null ? null : ToCampaignReference(winner);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MerchandisingCampaignMemberReference>> ResolveOrderedMembersAsync(
        Guid campaignId,
        CancellationToken cancellationToken)
    {
        var rows = await _db.MerchandisingCampaignOffers.AsNoTracking()
            .Where(x => x.CampaignId == campaignId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
        return rows.Select(ToMemberReference).ToList();
    }

    /// <inheritdoc />
    public async Task<MerchandisingCampaignReference> UpsertSeedCampaignAsync(
        Guid campaignId,
        Guid promotionTypeId,
        Guid storeId,
        DateTimeOffset startAt,
        DateTimeOffset? endAt,
        int priority,
        MerchandisingCampaignLifecycleStatus lifecycle,
        CancellationToken cancellationToken)
    {
        var typeExists = await _db.MerchandisingPromotionTypes
            .AnyAsync(x => x.Id == promotionTypeId, cancellationToken);
        if (!typeExists)
        {
            throw new InvalidOperationException("promotion.type.not_found");
        }

        var now = _clock.UtcNow;
        var existing = await _db.MerchandisingCampaigns
            .SingleOrDefaultAsync(x => x.Id == campaignId, cancellationToken);
        if (existing is null)
        {
            existing = MerchandisingCampaign.Create(
                campaignId,
                promotionTypeId,
                storeId,
                startAt,
                endAt,
                priority,
                now);
            existing.ForceLifecycleForSeed(lifecycle, now);
            _db.MerchandisingCampaigns.Add(existing);
        }
        else
        {
            if (existing.StoreId != storeId || existing.PromotionTypeId != promotionTypeId)
            {
                throw new InvalidOperationException("domain.invariant");
            }

            existing.UpdateWindow(startAt, endAt, priority, now);
            existing.ForceLifecycleForSeed(lifecycle, now);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return ToCampaignReference(existing);
    }

    /// <inheritdoc />
    public async Task SyncSeedMembersAsync(
        Guid campaignId,
        Guid expectedStoreId,
        IReadOnlyList<Guid> orderedSellerOfferIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(orderedSellerOfferIds);
        var campaign = await RequireCampaignAsync(campaignId, cancellationToken);
        if (campaign.StoreId != expectedStoreId)
        {
            throw new InvalidOperationException("merchandising.campaign.store_mismatch");
        }

        var members = await _db.MerchandisingCampaignOffers
            .Where(x => x.CampaignId == campaignId)
            .ToListAsync(cancellationToken);
        var desired = orderedSellerOfferIds.Distinct().ToArray();
        var desiredSet = desired.ToHashSet();

        foreach (var row in members.Where(x => !desiredSet.Contains(x.SellerOfferId)).ToList())
        {
            _db.MerchandisingCampaignOffers.Remove(row);
        }

        var byOffer = members
            .Where(x => desiredSet.Contains(x.SellerOfferId))
            .ToDictionary(x => x.SellerOfferId);
        var now = _clock.UtcNow;
        for (var i = 0; i < desired.Length; i++)
        {
            var offerId = desired[i];
            if (byOffer.TryGetValue(offerId, out var existing))
            {
                existing.SetSortOrder(i);
            }
            else
            {
                _db.MerchandisingCampaignOffers.Add(
                    MerchandisingCampaignOffer.Create(_ids.NewId(), campaignId, offerId, i, now));
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<MerchandisingCampaignListRow> Items, int Total)> ListCampaignsAsync(
        Guid storeId,
        string? search,
        MerchandisingCampaignLifecycleStatus? lifecycle,
        Guid? promotionTypeId,
        string? runtimeWindow,
        DateTimeOffset now,
        string titleLocale,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 100);
        var locale = string.IsNullOrWhiteSpace(titleLocale) ? "fa-IR" : titleLocale.Trim();
        var query = _db.MerchandisingCampaigns.AsNoTracking().Where(x => x.StoreId == storeId);
        if (lifecycle is { } life)
        {
            query = query.Where(x => x.LifecycleStatus == life);
        }

        if (promotionTypeId is { } typeId && typeId != Guid.Empty)
        {
            query = query.Where(x => x.PromotionTypeId == typeId);
        }

        var window = runtimeWindow?.Trim().ToLowerInvariant();
        if (window is "active")
        {
            query = query.Where(x =>
                x.LifecycleStatus == MerchandisingCampaignLifecycleStatus.Published
                && x.StartAt <= now
                && (x.EndAt == null || x.EndAt > now));
        }
        else if (window is "future" or "scheduled")
        {
            query = query.Where(x =>
                x.LifecycleStatus == MerchandisingCampaignLifecycleStatus.Published && x.StartAt > now);
        }
        else if (window is "expired")
        {
            query = query.Where(x =>
                x.LifecycleStatus == MerchandisingCampaignLifecycleStatus.Published
                && x.EndAt != null
                && x.EndAt <= now);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var matchingIds = await _db.MerchandisingCampaignTranslations.AsNoTracking()
                .Where(t => t.Title.Contains(term) || (t.Subtitle != null && t.Subtitle.Contains(term)))
                .Select(t => t.CampaignId)
                .Distinct()
                .ToListAsync(cancellationToken);
            query = query.Where(x => matchingIds.Contains(x.Id));
        }

        var total = await query.CountAsync(cancellationToken);
        var page = await query
            .OrderByDescending(x => x.UpdatedAt)
            .ThenBy(x => x.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
        if (page.Count == 0)
        {
            return (Array.Empty<MerchandisingCampaignListRow>(), total);
        }

        var ids = page.Select(x => x.Id).ToArray();
        var typeIds = page.Select(x => x.PromotionTypeId).Distinct().ToArray();
        var titles = await _db.MerchandisingCampaignTranslations.AsNoTracking()
            .Where(t => ids.Contains(t.CampaignId))
            .ToListAsync(cancellationToken);
        var typeNames = await _db.MerchandisingPromotionTypeTranslations.AsNoTracking()
            .Where(t => typeIds.Contains(t.TypeId))
            .ToListAsync(cancellationToken);
        var counts = await _db.MerchandisingCampaignOffers.AsNoTracking()
            .Where(o => ids.Contains(o.CampaignId))
            .GroupBy(o => o.CampaignId)
            .Select(g => new { CampaignId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var countMap = counts.ToDictionary(x => x.CampaignId, x => x.Count);

        var rows = page.Select(c =>
        {
            var title = titles.FirstOrDefault(t => t.CampaignId == c.Id && t.Locale == locale)?.Title
                ?? titles.FirstOrDefault(t => t.CampaignId == c.Id)?.Title;
            var typeName = typeNames.FirstOrDefault(t => t.TypeId == c.PromotionTypeId && t.Locale == locale)?.DisplayName
                ?? typeNames.FirstOrDefault(t => t.TypeId == c.PromotionTypeId)?.DisplayName
                ?? "—";
            countMap.TryGetValue(c.Id, out var memberCount);
            return new MerchandisingCampaignListRow(
                c.Id,
                c.PromotionTypeId,
                typeName,
                c.LifecycleStatus,
                c.StartAt,
                c.EndAt,
                c.Priority,
                memberCount,
                title,
                c.UpdatedAt,
                DeriveRuntimeLabel(c, now));
        }).ToList();
        return (rows, total);
    }

    /// <inheritdoc />
    public async Task<MerchandisingCampaignReference?> GetCampaignAsync(
        Guid campaignId,
        Guid storeId,
        CancellationToken cancellationToken)
    {
        var campaign = await _db.MerchandisingCampaigns.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == campaignId && x.StoreId == storeId, cancellationToken);
        return campaign is null ? null : ToCampaignReference(campaign);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MerchandisingCampaignTranslationReference>> ListCampaignTranslationsAsync(
        Guid campaignId,
        CancellationToken cancellationToken)
    {
        var rows = await _db.MerchandisingCampaignTranslations.AsNoTracking()
            .Where(x => x.CampaignId == campaignId)
            .OrderBy(x => x.Locale)
            .ToListAsync(cancellationToken);
        return rows.Select(row => new MerchandisingCampaignTranslationReference(
            row.CampaignId,
            row.Locale,
            row.Title,
            row.Subtitle,
            row.BadgeText)).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MerchandisingPromotionTypeOption>> ListPromotionTypeOptionsAsync(
        string locale,
        CancellationToken cancellationToken)
    {
        await EnsureAmazingTypeSeededAsync(cancellationToken);
        var normalized = string.IsNullOrWhiteSpace(locale) ? "fa-IR" : locale.Trim();
        var types = await _db.MerchandisingPromotionTypes.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
        var typeIds = types.Select(x => x.Id).ToArray();
        var translations = await _db.MerchandisingPromotionTypeTranslations.AsNoTracking()
            .Where(t => typeIds.Contains(t.TypeId))
            .ToListAsync(cancellationToken);
        return types.Select(t =>
        {
            var name = translations.FirstOrDefault(x => x.TypeId == t.Id && x.Locale == normalized)?.DisplayName
                ?? translations.FirstOrDefault(x => x.TypeId == t.Id)?.DisplayName
                ?? t.Code;
            return new MerchandisingPromotionTypeOption(t.Id, name, t.Code, t.IsSystem);
        }).ToList();
    }

    private static string DeriveRuntimeLabel(MerchandisingCampaign campaign, DateTimeOffset now)
    {
        if (campaign.LifecycleStatus == MerchandisingCampaignLifecycleStatus.Draft)
        {
            return "draft";
        }

        if (campaign.LifecycleStatus == MerchandisingCampaignLifecycleStatus.Archived)
        {
            return "archived";
        }

        if (campaign.StartAt > now)
        {
            return "scheduled";
        }

        if (campaign.EndAt is { } end && end <= now)
        {
            return "expired";
        }

        return "active";
    }

    private async Task UpsertTypeTranslationAsync(
        Guid typeId,
        string locale,
        string displayName,
        CancellationToken cancellationToken)
    {
        var row = await _db.MerchandisingPromotionTypeTranslations
            .SingleOrDefaultAsync(x => x.TypeId == typeId && x.Locale == locale, cancellationToken);
        if (row is null)
        {
            _db.MerchandisingPromotionTypeTranslations.Add(
                MerchandisingPromotionTypeTranslation.Create(typeId, locale, displayName));
        }
        else
        {
            row.SetDisplayName(displayName);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<MerchandisingCampaign> RequireCampaignAsync(
        Guid campaignId,
        CancellationToken cancellationToken)
    {
        var campaign = await _db.MerchandisingCampaigns.SingleOrDefaultAsync(
            x => x.Id == campaignId,
            cancellationToken);
        if (campaign is null)
        {
            throw new InvalidOperationException("domain.invariant");
        }

        return campaign;
    }

    private static MerchandisingPromotionTypeReference ToTypeReference(MerchandisingPromotionType type) =>
        new(type.Id, type.Code, type.IsSystem, type.IsActive, type.SortOrder);

    private static MerchandisingCampaignReference ToCampaignReference(MerchandisingCampaign campaign) =>
        new(
            campaign.Id,
            campaign.PromotionTypeId,
            campaign.StoreId,
            campaign.LifecycleStatus,
            campaign.StartAt,
            campaign.EndAt,
            campaign.Priority,
            campaign.CreatedAt,
            campaign.UpdatedAt);

    private static MerchandisingCampaignMemberReference ToMemberReference(MerchandisingCampaignOffer membership) =>
        new(
            membership.Id,
            membership.CampaignId,
            membership.SellerOfferId,
            membership.SortOrder,
            membership.CreatedAt);
}
