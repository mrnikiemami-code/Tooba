using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Media.Contracts.Assets;
using Tooba.Persistence.Grid;
using Tooba.Story.Application.Stories.Models;
using Tooba.Story.Application.Stories.Ports;
using Tooba.Story.Contracts.Errors;
using Tooba.Story.Domain.Enums;
using Tooba.Story.Domain.Policies;
using Tooba.Story.Domain.Rules;
using StoryEntity = Tooba.Story.Domain.Aggregates.Story;
using StoryItemEntity = Tooba.Story.Domain.Aggregates.StoryItem;

namespace Tooba.Story.Infrastructure.Directories;

/// <summary>مالکیت کاور/CTA، پرس‌وجوی فروشنده و غنی‌سازی دسته‌ای فروشگاه.</summary>
public sealed partial class StoryDirectory
{
    /// <summary>صفحه‌بندی/شمارش/فیلتر/جستجوی سمت سرور فروشنده برای فروشگاه مالک.</summary>
    public async Task<GridPageResponse<AdminStorySnapshot>> SellerQueryAsync(
        Guid tenantId,
        Guid sellerPartyId,
        GridQueryRequest request,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty || sellerPartyId == Guid.Empty)
            throw new SemanticException(new SemanticError(StoryErrorCodes.ShopInvalid));

        IQueryable<StoryEntity> q = _db.Stories.AsNoTracking()
            .Where(x => x.TenantId == tenantId
                && x.Origin == StoryOrigin.Seller
                && x.SellerPartyId == sellerPartyId);

        if (!string.IsNullOrWhiteSpace(request.Search))
            q = EfGridQuery.ApplySearchAny(q, request.Search, x => x.Title);

        foreach (var filter in request.Filters)
            q = ApplySellerFilter(q, filter);

        var sort = request.Sort.FirstOrDefault() ?? new GridSortRequest("displayOrder", "asc");
        return await EfGridQuery.PageAsync<StoryEntity, AdminStorySnapshot>(
            q,
            request,
            filtered => OrderSeller(filtered, sort),
            async (rows, ct) =>
            {
                if (rows.Count == 0)
                    return (IReadOnlyList<AdminStorySnapshot>)[];
                var ids = rows.Select(x => x.StoryId).ToList();
                var items = await _db.StoryItems.AsNoTracking()
                    .Where(x => ids.Contains(x.StoryId))
                    .OrderBy(x => x.DisplayOrder)
                    .ToListAsync(ct);
                var byStory = items.GroupBy(x => x.StoryId)
                    .ToDictionary(g => g.Key, g => (IReadOnlyList<Domain.Aggregates.StoryItem>)g.ToList());
                var snapshots = new List<AdminStorySnapshot>(rows.Count);
                foreach (var story in rows)
                {
                    story.AttachItems(byStory.GetValueOrDefault(story.StoryId, []));
                    snapshots.Add(await EnrichAsync(MapAdmin(story, isAdmin: false), ct));
                }

                return snapshots;
            },
            cancellationToken);
    }

    private static IQueryable<StoryEntity> ApplySellerFilter(
        IQueryable<StoryEntity> source,
        GridFilterRequest filter) =>
        filter.Field switch
        {
            "title" => EfGridQuery.ApplyTextFilter(source, x => x.Title, filter),
            "status" => EfGridQuery.ApplyEnumFilter(source, x => x.Status, filter),
            "reviewStatus" => EfGridQuery.ApplyEnumFilter(source, x => x.ReviewStatus, filter),
            "locale" => EfGridQuery.ApplyTextFilter(source, x => x.Locale, filter),
            "market" => EfGridQuery.ApplyTextFilter(source, x => x.Market, filter),
            "displayOrder" => EfGridQuery.ApplyIntFilter(source, x => x.DisplayOrder, filter),
            _ => source,
        };

    private static IQueryable<StoryEntity> OrderSeller(IQueryable<StoryEntity> source, GridSortRequest sort)
    {
        var asc = sort.Direction == "asc";
        return sort.Field switch
        {
            "title" => asc
                ? source.OrderBy(x => x.Title).ThenBy(x => x.DisplayOrder)
                : source.OrderByDescending(x => x.Title).ThenBy(x => x.DisplayOrder),
            "status" => asc
                ? source.OrderBy(x => x.Status).ThenBy(x => x.Title)
                : source.OrderByDescending(x => x.Status).ThenBy(x => x.Title),
            "reviewStatus" => asc
                ? source.OrderBy(x => x.ReviewStatus).ThenBy(x => x.Title)
                : source.OrderByDescending(x => x.ReviewStatus).ThenBy(x => x.Title),
            _ => asc
                ? source.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Title)
                : source.OrderByDescending(x => x.DisplayOrder).ThenBy(x => x.Title),
        };
    }

    private async Task<(Guid? AssetId, string? DisplayUrl)> BindCoverAsync(
        Guid tenantId,
        Guid? sellerPartyId,
        Guid? coverMediaAssetId,
        string? coverMediaUrl,
        bool allowUnscopedAdminAssets,
        CancellationToken cancellationToken)
    {
        if (coverMediaAssetId is null && string.IsNullOrWhiteSpace(coverMediaUrl))
            return (null, null);

        if (sellerPartyId is Guid shop && shop != Guid.Empty)
            return await BindSellerMediaAsync(
                tenantId, shop, null, coverMediaAssetId, coverMediaUrl, cancellationToken);

        if (_media is null)
            throw new SemanticException(new SemanticError(StoryErrorCodes.MediaUnavailable));

        if (coverMediaAssetId is Guid assetId && assetId != Guid.Empty)
        {
            try
            {
                var authorized = await _media.AuthorizeForStoryAsync(
                    assetId, tenantId, sellerPartyId: null, allowUnscopedAdminAssets, cancellationToken);
                return (authorized.MediaAssetId, authorized.DisplayUrl);
            }
            catch (ContractOperationException)
            {
                throw new SemanticException(new SemanticError(StoryErrorCodes.MediaForbidden));
            }
        }

        if (!string.IsNullOrWhiteSpace(coverMediaUrl) && !allowUnscopedAdminAssets)
            throw new SemanticException(new SemanticError(StoryErrorCodes.MediaForbidden));

        return (coverMediaAssetId, coverMediaUrl);
    }

    /// <summary>
    /// به‌روزرسانی نباید رسانهٔ موجود را بی‌صدا حذف کند؛ رسانهٔ بدون تغییر (دارایی مالک یا همان URL قدیمی)
    /// دقیقاً حفظ می‌شود تا ویرایش زیرنویس/CTA برای اسلایدهای قدیمی ممکن بماند. تغییر واقعی رسانه
    /// همچنان از مجوز مالک عبور می‌کند.
    /// </summary>
    /// <summary>
    /// به‌روزرسانی سطح استوری نباید URL کاور قدیمیِ بدون تغییر را حذف کند؛ فیلدهای خالی همان مقدار
    /// ذخیره‌شده را حفظ می‌کنند و مقدار واقعاً تغییرکرده از مجوز مالک رسانه عبور می‌کند.
    /// </summary>
    private async Task<(Guid? AssetId, string? DisplayUrl)> BindUpdatedCoverAsync(
        StoryEntity story,
        Guid? coverMediaAssetId,
        string? coverMediaUrl,
        CancellationToken cancellationToken)
    {
        var requestedAsset = coverMediaAssetId is Guid assetId && assetId != Guid.Empty ? assetId : (Guid?)null;
        var requestedUrl = string.IsNullOrWhiteSpace(coverMediaUrl) ? null : coverMediaUrl.Trim();
        var existingAsset = story.CoverMediaAssetId is Guid storedAsset && storedAsset != Guid.Empty
            ? storedAsset
            : (Guid?)null;
        var existingUrl = string.IsNullOrWhiteSpace(story.CoverMediaUrl) ? null : story.CoverMediaUrl.Trim();

        if (requestedAsset is null && requestedUrl is null)
            return (existingAsset, existingUrl);
        if (requestedAsset is not null && requestedAsset == existingAsset)
            return (existingAsset, existingUrl);
        if (requestedAsset is null
            && requestedUrl is not null
            && string.Equals(requestedUrl, existingUrl, StringComparison.Ordinal))
        {
            return (existingAsset, existingUrl);
        }

        return await BindCoverAsync(
            story.TenantId,
            story.SellerPartyId,
            coverMediaAssetId,
            coverMediaUrl,
            allowUnscopedAdminAssets: story.SellerPartyId is null,
            cancellationToken);
    }
    private async Task<(Guid? AssetId, string? DisplayUrl)> BindUpdatedItemMediaAsync(
        StoryEntity story,
        StoryItemEntity existing,
        UpdateStoryItemCommand command,
        CancellationToken cancellationToken)
    {
        var requestedAsset = command.MediaAssetId is Guid assetId && assetId != Guid.Empty ? assetId : (Guid?)null;
        var requestedUrl = string.IsNullOrWhiteSpace(command.MediaUrl) ? null : command.MediaUrl.Trim();
        var existingAsset = existing.MediaAssetId is Guid storedAsset && storedAsset != Guid.Empty
            ? storedAsset
            : (Guid?)null;
        var existingUrl = string.IsNullOrWhiteSpace(existing.MediaUrl) ? null : existing.MediaUrl.Trim();

        if (requestedAsset is null && requestedUrl is null)
            return (existingAsset, existingUrl);
        if (requestedAsset is not null && requestedAsset == existingAsset)
            return (existingAsset, existingUrl);
        if (requestedAsset is null
            && requestedUrl is not null
            && string.Equals(requestedUrl, existingUrl, StringComparison.Ordinal))
        {
            return (existingAsset, existingUrl);
        }

        return story.SellerPartyId is Guid shop && shop != Guid.Empty
            ? await BindSellerMediaAsync(
                story.TenantId, shop, command.MediaType, command.MediaAssetId, command.MediaUrl, cancellationToken)
            : await BindCoverAsync(
                story.TenantId, null, command.MediaAssetId, command.MediaUrl,
                allowUnscopedAdminAssets: true, cancellationToken);
    }
    private async Task EnsureProductCtaAsync(
        Guid? sellerPartyId,
        string? ctaType,
        string? ctaTarget,
        CancellationToken cancellationToken)
    {
        var type = string.IsNullOrWhiteSpace(ctaType) ? StoryRules.CtaNone : ctaType.Trim().ToLowerInvariant();
        if (!string.Equals(type, "product", StringComparison.Ordinal))
            return;

        if (_products is null)
            throw new SemanticException(new SemanticError(StoryErrorCodes.CtaRejected));
        if (!Guid.TryParse(ctaTarget?.Trim(), out var productId) || productId == Guid.Empty)
            throw new SemanticException(new SemanticError(StoryErrorCodes.CtaRejected));

        if (sellerPartyId is Guid shop && shop != Guid.Empty)
            await _products.EnsureOwnedBySellerAsync(productId, shop, cancellationToken);
        else
            await _products.EnsurePublishedExistsAsync(productId, cancellationToken);
    }

    private static string RequireOccurrenceKey(string? occurrenceKey)
    {
        if (string.IsNullOrWhiteSpace(occurrenceKey))
            throw new SemanticException(new SemanticError(StoryErrorCodes.OccurrenceRequired));
        return occurrenceKey.Trim();
    }

    private async Task<IReadOnlyList<AdminStorySnapshot>> EnrichManyAsync(
        IReadOnlyList<AdminStorySnapshot> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
            return rows;

        IReadOnlyDictionary<Guid, string> names = new Dictionary<Guid, string>();
        if (_partySellers is not null)
        {
            var shops = rows
                .Where(row => row.SellerPartyId is Guid id && id != Guid.Empty)
                .Select(row => row.SellerPartyId!.Value)
                .Distinct()
                .ToList();
            if (shops.Count > 0)
            {
                var projections = await _partySellers.GetStatusProjectionsAsync(shops, cancellationToken);
                names = projections.ToDictionary(row => row.PartyId, row => row.DisplayName);
            }
        }

        var withShops = rows.Select(row =>
        {
            if (row.SellerPartyId is Guid shop && names.TryGetValue(shop, out var name))
                return row with { ShopDisplayName = name };
            return row;
        }).ToList();

        if (_productTitles is null)
            return withShops;

        var productIds = new HashSet<Guid>();
        foreach (var row in withShops)
        {
            foreach (var item in row.Items)
                CollectProductCta(item.CtaType, item.CtaTarget, productIds);
        }

        if (productIds.Count == 0)
            return withShops;

        var titles = await _productTitles.GetProductTitlesByIdsAsync(productIds, cancellationToken);
        return withShops.Select(row =>
        {
            var items = row.Items.Select(item =>
            {
                if (!string.Equals(item.CtaType, "product", StringComparison.OrdinalIgnoreCase))
                    return item;
                if (!Guid.TryParse(item.CtaTarget?.Trim(), out var productId) || productId == Guid.Empty)
                    return item with { CtaProductTitle = null };
                titles.TryGetValue(productId, out var title);
                return item with { CtaProductTitle = string.IsNullOrWhiteSpace(title) ? null : title };
            }).ToList();
            return row with { Items = items };
        }).ToList();
    }

    private async Task<IReadOnlyList<PublicStoryCard>> EnrichPublicAsync(
        IReadOnlyList<PublicStoryCard> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
            return rows;

        var shops = rows
            .Where(row => row.SellerPartyId is Guid id && id != Guid.Empty)
            .Select(row => row.SellerPartyId!.Value)
            .Distinct()
            .ToList();

        IReadOnlyDictionary<Guid, string> names = new Dictionary<Guid, string>();
        IReadOnlyDictionary<Guid, CatalogPublicSellerAttribution> publicAttrs =
            new Dictionary<Guid, CatalogPublicSellerAttribution>();
        if (shops.Count > 0)
        {
            if (_partySellers is not null)
            {
                var projections = await _partySellers.GetStatusProjectionsAsync(shops, cancellationToken);
                names = projections.ToDictionary(row => row.PartyId, row => row.DisplayName);
            }

            if (_publicSellers is not null)
                publicAttrs = await _publicSellers.ResolveAsync(shops, cancellationToken);
        }

        var productIds = new HashSet<Guid>();
        foreach (var row in rows)
        {
            CollectProductCta(row.CtaType, row.CtaTarget, productIds);
            foreach (var item in row.Items)
                CollectProductCta(item.CtaType, item.CtaTarget, productIds);
        }

        var paths = new Dictionary<Guid, string>();
        if (_products is not null && productIds.Count > 0)
        {
            if (_products is IStoryProductPathBatchPort batch)
            {
                // One owning Catalog composition per page, not one serial lookup per CTA.
                var resolved = await batch.ResolvePublicProductPathsAsync(productIds, cancellationToken);
                foreach (var pair in resolved)
                {
                    if (!string.IsNullOrWhiteSpace(pair.Value))
                        paths[pair.Key] = pair.Value;
                }
            }
            else
            {
                foreach (var productId in productIds)
                {
                    var path = await _products.TryResolvePublicProductPathAsync(productId, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(path))
                        paths[productId] = path;
                }
            }
        }

        return rows.Select(row =>
        {
            var items = row.Items.Select(item => item with
            {
                CtaHref = ResolveCtaHref(item.CtaType, item.CtaTarget, item.CtaHref, paths),
            }).ToList();
            var enriched = row with
            {
                Items = items,
                CtaHref = ResolveCtaHref(row.CtaType, row.CtaTarget, row.CtaHref, paths),
            };
            if (row.SellerPartyId is not Guid shop)
                return enriched;
            publicAttrs.TryGetValue(shop, out var attr);
            names.TryGetValue(shop, out var partyName);
            return enriched with
            {
                ShopDisplayName = attr?.DisplayName ?? partyName,
                ShopPublicId = attr?.PublicId,
            };
        }).ToList();
    }

    private static void CollectProductCta(string? ctaType, string? ctaTarget, HashSet<Guid> sink)
    {
        if (!string.Equals(ctaType, "product", StringComparison.OrdinalIgnoreCase))
            return;
        if (Guid.TryParse(ctaTarget?.Trim(), out var id) && id != Guid.Empty)
            sink.Add(id);
    }

    private static string? ResolveCtaHref(
        string? ctaType,
        string? ctaTarget,
        string? existing,
        IReadOnlyDictionary<Guid, string> paths)
    {
        if (!string.IsNullOrWhiteSpace(existing))
            return existing;
        if (string.Equals(ctaType, "product", StringComparison.OrdinalIgnoreCase)
            && Guid.TryParse(ctaTarget?.Trim(), out var id)
            && paths.TryGetValue(id, out var path))
            return path;
        if (string.Equals(ctaType, "external", StringComparison.OrdinalIgnoreCase))
            return string.IsNullOrWhiteSpace(ctaTarget) ? null : ctaTarget.Trim();
        if (string.Equals(ctaType, "internal", StringComparison.OrdinalIgnoreCase)
            || string.Equals(ctaType, "category", StringComparison.OrdinalIgnoreCase)
            || string.Equals(ctaType, "article", StringComparison.OrdinalIgnoreCase))
            return StoryRules.IsSafeInternalPath(ctaTarget) ? ctaTarget!.Trim() : null;
        return null;
    }

    private async Task EnsureStoredContentAuthorizedAsync(
        StoryEntity story,
        CancellationToken cancellationToken)
    {
        if (!StoryPublicationPolicy.HasPlayableContent(story))
            throw new SemanticException(new SemanticError(StoryErrorCodes.ContentNotReady));

        _ = StoryRules.ValidateCta(story.CtaType, story.CtaTarget);
        await EnsureProductCtaAsync(story.SellerPartyId, story.CtaType, story.CtaTarget, cancellationToken);
        if (story.CoverMediaAssetId is Guid coverId && coverId != Guid.Empty)
        {
            _ = await BindCoverAsync(
                story.TenantId,
                story.SellerPartyId,
                coverId,
                story.CoverMediaUrl,
                allowUnscopedAdminAssets: story.SellerPartyId is null,
                cancellationToken);
        }

        foreach (var item in story.Items)
        {
            _ = StoryRules.ValidateCta(item.CtaType, item.CtaTarget);
            await EnsureProductCtaAsync(story.SellerPartyId, item.CtaType, item.CtaTarget, cancellationToken);
            if (item.MediaAssetId is Guid mediaId && mediaId != Guid.Empty)
            {
                if (story.SellerPartyId is Guid shop && shop != Guid.Empty)
                {
                    _ = await BindSellerMediaAsync(
                        story.TenantId, shop, item.MediaType, mediaId, item.MediaUrl, cancellationToken);
                }
                else
                {
                    _ = await BindCoverAsync(
                        story.TenantId, null, mediaId, item.MediaUrl,
                        allowUnscopedAdminAssets: true, cancellationToken);
                }
            }
            else if (!string.IsNullOrWhiteSpace(item.MediaUrl) && story.SellerPartyId is Guid)
            {
                throw new SemanticException(new SemanticError(StoryErrorCodes.MediaForbidden));
            }
        }
    }

    private async Task<AdminStorySnapshot?> TryReplayLifecycleAsync(
        Guid tenantId,
        Guid? sellerPartyId,
        string actionCode,
        string? occurrenceKey,
        string intentHash,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(occurrenceKey))
            return null;
        return await TryReplayMappedAsync(
            tenantId, sellerPartyId, actionCode, occurrenceKey!, intentHash, isAdmin, cancellationToken);
    }

    private async Task<AdminStorySnapshot?> TryReplayMappedAsync(
        Guid tenantId,
        Guid? sellerPartyId,
        string actionCode,
        string occurrenceKey,
        string intentHash,
        bool isAdmin,
        CancellationToken cancellationToken,
        Guid? expectedStoryId = null)
    {
        var key = occurrenceKey.Trim();
        var existing = await _db.CommandOccurrences.AsNoTracking()
            .FirstOrDefaultAsync(
                row => row.OccurrenceKey == key && row.TenantId == tenantId,
                cancellationToken);
        if (existing is null)
            return null;
        if (!string.Equals(existing.ActionCode, actionCode, StringComparison.Ordinal)
            || !string.Equals(existing.IntentHash, intentHash, StringComparison.Ordinal)
            || existing.SellerPartyId != sellerPartyId
            || (expectedStoryId is Guid requiredStory && existing.StoryId != requiredStory))
        {
            throw new SemanticException(new SemanticError(StoryErrorCodes.OccurrenceConflict));
        }

        var story = await LoadStoryAsync(tenantId, existing.StoryId, track: false, cancellationToken)
            ?? throw new SemanticException(new SemanticError(StoryErrorCodes.Missing));
        return await EnrichAsync(MapAdmin(story, isAdmin), cancellationToken);
    }

    /// <summary>
    /// بازپخش حذف: هویت از occurrence ledger (شامل SellerPartyId ذخیره‌شده).
    /// ادمین shop-owned را با sellerPartyId=null در آرگومان تطبیق نمی‌دهد — از ردیف occurrence می‌خواند.
    /// اگر هدف هنوز باشد، حذف را کامل می‌کند بدون occurrence دوم؛ tombstone فقط وقتی ردیف رفته باشد.
    /// </summary>
    private async Task<(bool Handled, AdminStorySnapshot? Snapshot)> TryReplayOrFinishDeleteAsync(
        Guid tenantId,
        Guid? callerSellerPartyId,
        string actionCode,
        string occurrenceKey,
        string intentHash,
        bool isAdmin,
        Guid expectedStoryId,
        int? expectedVersion,
        CancellationToken cancellationToken)
    {
        var key = occurrenceKey.Trim();
        var existing = await _db.CommandOccurrences
            .FirstOrDefaultAsync(
                row => row.OccurrenceKey == key && row.TenantId == tenantId,
                cancellationToken);
        if (existing is null)
            return (false, null);

        if (!string.Equals(existing.ActionCode, actionCode, StringComparison.Ordinal)
            || !string.Equals(existing.IntentHash, intentHash, StringComparison.Ordinal)
            || existing.StoryId != expectedStoryId)
        {
            throw new SemanticException(new SemanticError(StoryErrorCodes.OccurrenceConflict));
        }

        // Seller must match ledger shop; admin accepts ledger SellerPartyId (platform null or shop).
        if (!isAdmin && existing.SellerPartyId != callerSellerPartyId)
            throw new SemanticException(new SemanticError(StoryErrorCodes.OccurrenceConflict));

        var story = await LoadStoryAsync(tenantId, existing.StoryId, track: true, cancellationToken);
        if (story is null)
            return (true, MapDeletedSnapshot(tenantId, expectedStoryId, isAdmin));

        // Occurrence already minted but row still present — finish removal; never mint a second occurrence.
        story.EnsureCanDelete(isAdmin);
        story.EnsureExpectedVersion(expectedVersion);
        if (!isAdmin)
        {
            if (story.SellerPartyId != callerSellerPartyId)
                throw new SemanticException(new SemanticError(StoryErrorCodes.Missing));
        }

        _db.StoryItems.RemoveRange(story.Items);
        _db.Stories.Remove(story);
        await _db.SaveChangesAsync(cancellationToken);
        return (true, MapDeletedSnapshot(tenantId, expectedStoryId, isAdmin));
    }

    private static AdminStorySnapshot MapDeletedSnapshot(Guid tenantId, Guid storyId, bool isAdmin) =>
        new(
            storyId,
            tenantId,
            isAdmin ? StoryOrigin.Admin : StoryOrigin.Seller,
            SellerPartyId: null,
            StoryReviewStatus.None,
            SubmittedByActorUserId: null,
            ReviewedByActorUserId: null,
            SubmittedAt: null,
            ReviewedAt: null,
            RejectionReason: null,
            Locale: null,
            Market: null,
            Title: string.Empty,
            CoverMediaAssetId: null,
            CoverMediaUrl: null,
            DisplayOrder: 0,
            StartAt: null,
            EndAt: null,
            StoryStatus.Disabled,
            CtaType: StoryRules.CtaNone,
            CtaTarget: null,
            VersionToken: 0,
            CreatedAt: DateTimeOffset.MinValue,
            UpdatedAt: DateTimeOffset.MinValue,
            Items: Array.Empty<AdminStoryItemSnapshot>(),
            CreatedByActorUserId: null,
            ShopDisplayName: null,
            ContentReady: false,
            EffectivelyPublic: false,
            ActionCodes: Array.Empty<string>());

}
