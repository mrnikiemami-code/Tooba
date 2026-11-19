using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.Media.Contracts.Assets;
using Tooba.Persistence.Grid;
using Tooba.Story.Application.Stories.Models;
using Tooba.Story.Application.Stories.Ports;
using Tooba.Story.Contracts.Errors;
using Tooba.Story.Domain.Enums;
using Tooba.Story.Domain.Rules;
using StoryEntity = Tooba.Story.Domain.Aggregates.Story;

namespace Tooba.Story.Infrastructure.Directories;

/// <summary>Cover/CTA ownership, seller query and batch shop enrichment.</summary>
public sealed partial class StoryDirectory
{
    /// <summary>Seller server-side page/count/filter/search for the owning shop.</summary>
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

    private async Task EnsureProductCtaAsync(
        Guid? sellerPartyId,
        string? ctaType,
        string? ctaTarget,
        CancellationToken cancellationToken)
    {
        var type = string.IsNullOrWhiteSpace(ctaType) ? StoryRules.CtaNone : ctaType.Trim().ToLowerInvariant();
        if (!string.Equals(type, "product", StringComparison.Ordinal))
            return;

        if (sellerPartyId is not Guid shop || shop == Guid.Empty)
            throw new SemanticException(new SemanticError(StoryErrorCodes.CtaRejected));
        if (_products is null)
            throw new SemanticException(new SemanticError(StoryErrorCodes.CtaRejected));
        if (!Guid.TryParse(ctaTarget?.Trim(), out var productId) || productId == Guid.Empty)
            throw new SemanticException(new SemanticError(StoryErrorCodes.CtaRejected));

        await _products.EnsureOwnedBySellerAsync(productId, shop, cancellationToken);
    }

    private async Task<IReadOnlyList<AdminStorySnapshot>> EnrichManyAsync(
        IReadOnlyList<AdminStorySnapshot> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0 || _partySellers is null)
            return rows;

        var shops = rows
            .Where(row => row.SellerPartyId is Guid id && id != Guid.Empty)
            .Select(row => row.SellerPartyId!.Value)
            .Distinct()
            .ToList();
        if (shops.Count == 0)
            return rows;

        var projections = await _partySellers.GetStatusProjectionsAsync(shops, cancellationToken);
        var names = projections.ToDictionary(row => row.PartyId, row => row.DisplayName);
        return rows.Select(row =>
        {
            if (row.SellerPartyId is Guid shop && names.TryGetValue(shop, out var name))
                return row with { ShopDisplayName = name };
            return row;
        }).ToList();
    }

    private async Task<IReadOnlyList<PublicStoryCard>> EnrichPublicAsync(
        IReadOnlyList<PublicStoryCard> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0 || _partySellers is null)
            return rows;

        var shops = rows
            .Where(row => row.SellerPartyId is Guid id && id != Guid.Empty)
            .Select(row => row.SellerPartyId!.Value)
            .Distinct()
            .ToList();
        if (shops.Count == 0)
            return rows;

        var projections = await _partySellers.GetStatusProjectionsAsync(shops, cancellationToken);
        var names = projections.ToDictionary(row => row.PartyId, row => row.DisplayName);
        // Canonical public id is not inventable from Party Guid; expose display name only.
        return rows.Select(row =>
        {
            if (row.SellerPartyId is Guid shop && names.TryGetValue(shop, out var name))
                return row with { ShopDisplayName = name, ShopPublicId = null };
            return row;
        }).ToList();
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
        CancellationToken cancellationToken)
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
            || existing.SellerPartyId != sellerPartyId)
        {
            throw new SemanticException(new SemanticError(StoryErrorCodes.OccurrenceConflict));
        }

        var story = await LoadStoryAsync(tenantId, existing.StoryId, track: false, cancellationToken)
            ?? throw new SemanticException(new SemanticError(StoryErrorCodes.Missing));
        return await EnrichAsync(MapAdmin(story, isAdmin), cancellationToken);
    }

    private static string HashLifecycleIntent(Guid storyId, int? expectedVersion, string? extra = null)
    {
        static string F(string? value)
        {
            var text = value ?? string.Empty;
            return text.Length.ToString(CultureInfo.InvariantCulture) + ":" + text;
        }

        var payload = string.Join('\u001f',
            F(storyId.ToString("D")),
            F(expectedVersion?.ToString(CultureInfo.InvariantCulture)),
            F(extra));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }

    private static string HashItemIntent(AddStoryItemCommand command)
    {
        static string F(string? value)
        {
            var text = value ?? string.Empty;
            return text.Length.ToString(CultureInfo.InvariantCulture) + ":" + text;
        }

        var payload = string.Join('\u001f',
            F(command.MediaType),
            F(command.MediaAssetId?.ToString("D")),
            F(command.MediaUrl),
            F(command.Caption),
            F(command.DurationMs?.ToString(CultureInfo.InvariantCulture)),
            F(command.CtaType),
            F(command.CtaTarget),
            F(command.DisplayOrder?.ToString(CultureInfo.InvariantCulture)),
            F(command.ExpectedVersion?.ToString(CultureInfo.InvariantCulture)));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }
}
