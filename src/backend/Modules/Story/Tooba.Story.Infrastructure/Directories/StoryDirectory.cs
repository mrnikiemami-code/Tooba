using Tooba.Story.Contracts.Errors;
using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Media.Contracts.Assets;
using Tooba.Party.Contracts.Ports;
using Tooba.Story.Application.Stories.Models;
using Tooba.Story.Application.Stories.Ports;
using Tooba.Story.Domain.Aggregates;
using Tooba.Story.Domain.Enums;
using Tooba.Story.Domain.Policies;
using Tooba.Story.Domain.Rules;
using Tooba.Story.Infrastructure.Persistence;
using StoryEntity = Tooba.Story.Domain.Aggregates.Story;

namespace Tooba.Story.Infrastructure.Directories;

/// <summary>دایرکتوری Story با schema مستقل.</summary>
public sealed partial class StoryDirectory : IStoryDirectory
{
    private readonly StoryDbContext _db;
    private readonly IPartyAdminSellerReadGateway? _partySellers;
    private readonly IStoryMediaAssetPort? _media;
    private readonly IStoryProductTargetPort? _products;
    private readonly ICatalogPublicSellerAttributionLookup? _publicSellers;
    private readonly ICatalogAdminProductTitleIdLookup? _productTitles;

    /// <summary>DbContext مالک را تزریق می‌کند؛ Party/Media/Product/PublicSeller/Titles اختیاری برای تست‌های جدا هستند.</summary>
    public StoryDirectory(
        StoryDbContext db,
        IPartyAdminSellerReadGateway? partySellers = null,
        IStoryMediaAssetPort? media = null,
        IStoryProductTargetPort? products = null,
        ICatalogPublicSellerAttributionLookup? publicSellers = null,
        ICatalogAdminProductTitleIdLookup? productTitles = null)
    {
        _db = db;
        _partySellers = partySellers;
        _media = media;
        _products = products;
        _publicSellers = publicSellers;
        _productTitles = productTitles;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PublicStoryCard>> GetPublicStoriesAsync(
        Guid tenantId,
        string? locale,
        string? market,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        // Load candidate headers first, attach slides, THEN apply playable/window policy.
        // Filtering before AttachItems would drop every freshly materialized row (Items.Count == 0).
        var rows = await _db.Stories.AsNoTracking()
            .Where(story => story.TenantId == tenantId
                && (story.Status == StoryStatus.Active || story.Status == StoryStatus.Scheduled)
                && (story.Origin == StoryOrigin.Admin
                    || story.ReviewStatus == StoryReviewStatus.Approved))
            .OrderBy(story => story.DisplayOrder)
            .ThenBy(story => story.StoryId)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
            return [];

        var ids = rows.Select(story => story.StoryId).ToList();
        var items = await _db.StoryItems.AsNoTracking()
            .Where(item => ids.Contains(item.StoryId))
            .OrderBy(item => item.DisplayOrder)
            .ToListAsync(cancellationToken);
        var itemsByStory = items.GroupBy(item => item.StoryId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<StoryItem>)group.ToList());

        foreach (var story in rows)
            story.AttachItems(itemsByStory.GetValueOrDefault(story.StoryId, []));

        var cards = rows
            .Where(story => story.IsPubliclyVisible(now)
                && StoryRules.MatchesLocale(story.Locale, locale)
                && StoryRules.MatchesMarket(story.Market, market))
            .Select(MapPublic)
            .ToList();
        return await EnrichPublicAsync(cards, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminStorySnapshot>> AdminListAsync(
        Guid tenantId,
        StoryReviewStatus? reviewStatus,
        CancellationToken cancellationToken)
    {
        var stories = await LoadStoriesAsync(tenantId, track: false, cancellationToken);
        IEnumerable<StoryEntity> query = stories;
        if (reviewStatus.HasValue)
            query = query.Where(story => story.ReviewStatus == reviewStatus.Value);

        var mapped = query
            .OrderBy(story => story.DisplayOrder)
            .ThenBy(story => story.StoryId)
            .Select(story => MapAdmin(story, isAdmin: true))
            .ToList();
        return await EnrichManyAsync(mapped, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AdminStorySnapshot>> AdminListPendingReviewAsync(
        Guid tenantId,
        CancellationToken cancellationToken) =>
        AdminListAsync(tenantId, StoryReviewStatus.Submitted, cancellationToken);

    /// <inheritdoc />
    public async Task<AdminStorySnapshot?> AdminGetAsync(
        Guid tenantId,
        Guid storyId,
        CancellationToken cancellationToken)
    {
        var story = await LoadStoryAsync(tenantId, storyId, track: false, cancellationToken);
        return story is null ? null : await EnrichAsync(MapAdmin(story, isAdmin: true), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AdminStorySnapshot> AdminCreateAsync(
        Guid tenantId,
        CreateStoryCommand command,
        CancellationToken cancellationToken)
    {
        var key = RequireOccurrenceKey(command.OccurrenceKey);
        var intent = HashIntent(command);
        var replay = await TryReplayMappedAsync(
            tenantId, null, "admin_create", key, intent, isAdmin: true, cancellationToken);
        if (replay is not null)
            return replay;

        // Platform (non-shop) create: product CTA uses Catalog validity; cover may be unscoped Ready assets.
        await EnsureProductCtaAsync(null, command.CtaType, command.CtaTarget, cancellationToken);
        var cover = await BindCoverAsync(
            tenantId, null, command.CoverMediaAssetId, command.CoverMediaUrl,
            allowUnscopedAdminAssets: true, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var order = command.DisplayOrder
            ?? await NextDisplayOrderAsync(tenantId, cancellationToken);
        var story = StoryEntity.CreateDraft(
            tenantId,
            command.Title,
            order,
            now,
            command.Locale,
            command.Market,
            cover.AssetId,
            cover.DisplayUrl,
            command.CtaType,
            command.CtaTarget);
        _db.Stories.Add(story);
        await RecordOccurrenceAsync(
            tenantId, null, "admin_create", key, intent,
            story.StoryId, Guid.Empty, now, cancellationToken);
        var foldedCreate = await SaveChangesWithOccurrenceArbitrationAsync(tenantId, key, intent, "admin_create", null, expectedStoryId: null, ownedStoryIds: [story.StoryId], cancellationToken);
        if (foldedCreate is Guid foldedStoryId)
        {
            story = await LoadStoryAsync(tenantId, foldedStoryId, track: false, cancellationToken)
                ?? throw new SemanticException(new SemanticError(StoryErrorCodes.Missing));
        }
        return await EnrichAsync(MapAdmin(story, isAdmin: true), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AdminStorySnapshot> AdminUpdateAsync(
        Guid tenantId,
        Guid storyId,
        UpdateStoryCommand command,
        CancellationToken cancellationToken)
    {
        var key = RequireOccurrenceKey(command.OccurrenceKey);
        var intent = HashUpdateStoryIntent(storyId, command);
        var story = await RequireStoryAsync(tenantId, storyId, cancellationToken);
        var replay = await TryReplayMappedAsync(
            tenantId, story.SellerPartyId, "admin_update", key, intent, isAdmin: true, cancellationToken, storyId);
        if (replay is not null)
            return replay;
        story.EnsureExpectedVersion(command.ExpectedVersion);
        await EnsureProductCtaAsync(story.SellerPartyId, command.CtaType, command.CtaTarget, cancellationToken);
        var cover = await BindCoverAsync(
            tenantId,
            story.SellerPartyId,
            command.CoverMediaAssetId,
            command.CoverMediaUrl,
            allowUnscopedAdminAssets: story.SellerPartyId is null,
            cancellationToken);
        story.Update(
            command.Title,
            command.Locale,
            command.Market,
            cover.AssetId,
            cover.DisplayUrl,
            command.CtaType,
            command.CtaTarget,
            DateTimeOffset.UtcNow);
        await RecordOccurrenceAsync(
            tenantId, story.SellerPartyId, "admin_update", key, intent,
            story.StoryId, story.CreatedByActorUserId ?? Guid.Empty, DateTimeOffset.UtcNow, cancellationToken);
        story = await SaveStoryAsync(story, key, intent, cancellationToken);
        return await EnrichAsync(MapAdmin(story, isAdmin: true), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AdminStorySnapshot> AdminSetStatusAsync(
        Guid tenantId,
        Guid storyId,
        StoryStatus status,
        int? expectedVersion,
        CancellationToken cancellationToken,
        string? occurrenceKey = null)
    {
        var key = RequireOccurrenceKey(occurrenceKey);
        var intent = HashCanonical(storyId.ToString("D"), expectedVersion?.ToString(System.Globalization.CultureInfo.InvariantCulture), "status", status.ToString());
        var story = await RequireStoryAsync(tenantId, storyId, cancellationToken);
        var replay = await TryReplayMappedAsync(
            tenantId, story.SellerPartyId, "admin_set_status", key, intent, isAdmin: true, cancellationToken, storyId);
        if (replay is not null)
            return replay;
        story.EnsureExpectedVersion(expectedVersion);
        if (status == StoryStatus.Active)
            await EnsureStoredContentAuthorizedAsync(story, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        switch (status)
        {
            case StoryStatus.Active:
                story.Activate(now);
                break;
            case StoryStatus.Disabled:
                story.Disable(now);
                break;
            case StoryStatus.Expired:
                story.MarkExpired(now);
                break;
            case StoryStatus.Draft:
                throw new SemanticException(new SemanticError(StoryErrorCodes.MutationRejected));
            case StoryStatus.Scheduled:
                throw new SemanticException(new SemanticError(StoryErrorCodes.MutationRejected));
            default:
                throw new SemanticException(new SemanticError(StoryErrorCodes.MutationRejected));
        }

        await RecordOccurrenceAsync(
            tenantId, story.SellerPartyId, "admin_set_status", key, intent,
            story.StoryId, story.CreatedByActorUserId ?? Guid.Empty, now, cancellationToken);
        story = await SaveStoryAsync(story, key, intent, cancellationToken);
        return MapAdmin(story, isAdmin: true);
    }

    /// <inheritdoc />
    public async Task<AdminStorySnapshot> AdminSetScheduleAsync(
        Guid tenantId,
        Guid storyId,
        SetStoryScheduleCommand command,
        CancellationToken cancellationToken)
    {
        var story = await RequireStoryAsync(tenantId, storyId, cancellationToken);
        var key = RequireOccurrenceKey(command.OccurrenceKey);
        var intent = HashScheduleIntent(storyId, command);
        var replay = await TryReplayMappedAsync(
            tenantId, story.SellerPartyId, "admin_set_schedule", key, intent, isAdmin: true, cancellationToken, storyId);
        if (replay is not null)
            return replay;
        story.EnsureExpectedVersion(command.ExpectedVersion);
        await EnsureStoredContentAuthorizedAsync(story, cancellationToken);
        story.SetSchedule(command.StartAt, command.EndAt, DateTimeOffset.UtcNow);
        await RecordOccurrenceAsync(
            tenantId, story.SellerPartyId, "admin_set_schedule", key, intent,
            story.StoryId, story.CreatedByActorUserId ?? Guid.Empty, DateTimeOffset.UtcNow, cancellationToken);
        story = await SaveStoryAsync(story, key, intent, cancellationToken);
        return MapAdmin(story, isAdmin: true);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminStorySnapshot>> AdminReorderStoriesAsync(
        Guid tenantId,
        IReadOnlyList<Guid> storyIdsInOrder,
        IReadOnlyList<int> expectedVersions,
        string? occurrenceKey,
        CancellationToken cancellationToken)
    {
        var key = RequireOccurrenceKey(occurrenceKey);
        if (expectedVersions is null || expectedVersions.Count != storyIdsInOrder.Count)
            throw new SemanticException(new SemanticError(StoryErrorCodes.MutationRejected));
        var versionPayload = string.Join(',', storyIdsInOrder.Select((id, i) => $"{id:D}:{expectedVersions[i]}"));
        var intent = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes("reorder_stories" + ((char)0x1f) + versionPayload))).ToLowerInvariant();
        var replayOcc = await _db.CommandOccurrences.AsNoTracking()
            .FirstOrDefaultAsync(row => row.OccurrenceKey == key && row.TenantId == tenantId, cancellationToken);
        if (replayOcc is not null)
        {
            if (!string.Equals(replayOcc.ActionCode, "admin_reorder_stories", StringComparison.Ordinal)
                || !string.Equals(replayOcc.IntentHash, intent, StringComparison.Ordinal))
                throw new SemanticException(new SemanticError(StoryErrorCodes.OccurrenceConflict));
            var replayStories = await LoadStoriesAsync(tenantId, track: false, cancellationToken);
            return replayStories
                .OrderBy(story => story.DisplayOrder)
                .Select(story => MapAdmin(story, isAdmin: true))
                .ToList();
        }

        var stories = await LoadStoriesAsync(tenantId, track: true, cancellationToken);
        if (storyIdsInOrder.Count != stories.Count)
            throw new SemanticException(new SemanticError(StoryErrorCodes.MutationRejected));
        if (storyIdsInOrder.Distinct().Count() != storyIdsInOrder.Count)
            throw new SemanticException(new SemanticError(StoryErrorCodes.MutationRejected));

        var lookup = stories.ToDictionary(story => story.StoryId);
        var now = DateTimeOffset.UtcNow;
        for (var index = 0; index < storyIdsInOrder.Count; index++)
        {
            if (!lookup.TryGetValue(storyIdsInOrder[index], out var story))
                throw new SemanticException(new SemanticError(StoryErrorCodes.Missing));
            story.EnsureExpectedVersion(expectedVersions[index]);
            story.SetDisplayOrder(index, now);
        }

        await RecordOccurrenceAsync(
            tenantId, null, "admin_reorder_stories", key, intent,
            storyIdsInOrder[0], Guid.Empty, now, cancellationToken);
        await SaveChangesWithOccurrenceArbitrationAsync(tenantId, key, intent, "admin_reorder_stories", null, expectedStoryId: null, ownedStoryIds: storyIdsInOrder, cancellationToken);
        return stories
            .OrderBy(story => story.DisplayOrder)
            .Select(story => MapAdmin(story, isAdmin: true))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<AdminStorySnapshot> AdminAddItemAsync(
        Guid tenantId,
        Guid storyId,
        AddStoryItemCommand command,
        CancellationToken cancellationToken)
    {
        var key = RequireOccurrenceKey(command.OccurrenceKey);
        var itemIntent = HashItemIntent(storyId, command);
        var story = await RequireStoryAsync(tenantId, storyId, cancellationToken);
        var replay = await TryReplayMappedAsync(
            tenantId, story.SellerPartyId, "admin_add_item", key, itemIntent, isAdmin: true, cancellationToken, storyId);
        if (replay is not null)
            return replay;
        story.EnsureExpectedVersion(command.ExpectedVersion);
        await EnsureProductCtaAsync(story.SellerPartyId, command.CtaType, command.CtaTarget, cancellationToken);
        var bound = story.SellerPartyId is Guid shop && shop != Guid.Empty
            ? await BindSellerMediaAsync(
                tenantId, shop, command.MediaType, command.MediaAssetId, command.MediaUrl, cancellationToken)
            : await BindCoverAsync(
                tenantId, null, command.MediaAssetId, command.MediaUrl,
                allowUnscopedAdminAssets: true, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var order = command.DisplayOrder
            ?? (story.Items.Count == 0 ? 0 : story.Items.Max(item => item.DisplayOrder) + 1);
        story.AddItem(
            command.MediaType,
            order,
            now,
            bound.AssetId,
            bound.DisplayUrl,
            command.Caption,
            command.DurationMs,
            command.CtaType,
            command.CtaTarget);
        await RecordOccurrenceAsync(
            tenantId, story.SellerPartyId, "admin_add_item", key, itemIntent,
            story.StoryId, story.CreatedByActorUserId ?? Guid.Empty, now, cancellationToken);
        story = await SaveStoryAsync(story, key, itemIntent, cancellationToken);
        return await EnrichAsync(MapAdmin(story, isAdmin: true), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AdminStorySnapshot> AdminUpdateItemAsync(
        Guid tenantId,
        Guid storyId,
        Guid itemId,
        UpdateStoryItemCommand command,
        CancellationToken cancellationToken)
    {
        var key = RequireOccurrenceKey(command.OccurrenceKey);
        var intent = HashUpdateItemIntent(storyId, itemId, command);
        var story = await RequireStoryAsync(tenantId, storyId, cancellationToken);
        var replay = await TryReplayMappedAsync(
            tenantId, story.SellerPartyId, "admin_update_item", key, intent, isAdmin: true, cancellationToken, storyId);
        if (replay is not null)
            return replay;
        story.EnsureExpectedVersion(command.ExpectedVersion);
        await EnsureProductCtaAsync(story.SellerPartyId, command.CtaType, command.CtaTarget, cancellationToken);
        var bound = story.SellerPartyId is Guid shop && shop != Guid.Empty
            ? await BindSellerMediaAsync(
                tenantId, shop, command.MediaType, command.MediaAssetId, command.MediaUrl, cancellationToken)
            : await BindCoverAsync(
                tenantId, null, command.MediaAssetId, command.MediaUrl,
                allowUnscopedAdminAssets: true, cancellationToken);
        story.UpdateItem(
            itemId,
            command.MediaType,
            bound.AssetId,
            bound.DisplayUrl,
            command.Caption,
            command.DurationMs,
            command.CtaType,
            command.CtaTarget,
            DateTimeOffset.UtcNow);
        await RecordOccurrenceAsync(
            tenantId, story.SellerPartyId, "admin_update_item", key, intent,
            story.StoryId, story.CreatedByActorUserId ?? Guid.Empty, DateTimeOffset.UtcNow, cancellationToken);
        story = await SaveStoryAsync(story, key, intent, cancellationToken);
        return await EnrichAsync(MapAdmin(story, isAdmin: true), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AdminStorySnapshot> AdminRemoveItemAsync(
        Guid tenantId,
        Guid storyId,
        Guid itemId,
        int? expectedVersion,
        string? occurrenceKey,
        CancellationToken cancellationToken)
    {
        var key = RequireOccurrenceKey(occurrenceKey);
        var intent = HashRemoveItemIntent(storyId, itemId, expectedVersion);
        var story = await RequireStoryAsync(tenantId, storyId, cancellationToken);
        var replay = await TryReplayMappedAsync(
            tenantId, story.SellerPartyId, "admin_remove_item", key, intent, isAdmin: true, cancellationToken, storyId);
        if (replay is not null)
            return replay;
        story.EnsureExpectedVersion(expectedVersion);
        story.RemoveItem(itemId, DateTimeOffset.UtcNow);
        await RecordOccurrenceAsync(
            tenantId, story.SellerPartyId, "admin_remove_item", key, intent,
            story.StoryId, story.CreatedByActorUserId ?? Guid.Empty, DateTimeOffset.UtcNow, cancellationToken);
        story = await SaveStoryAsync(story, key, intent, cancellationToken);
        return MapAdmin(story, isAdmin: true);
    }

    /// <inheritdoc />
    public async Task<AdminStorySnapshot> AdminReorderItemsAsync(
        Guid tenantId,
        Guid storyId,
        IReadOnlyList<Guid> itemIdsInOrder,
        int? expectedVersion,
        string? occurrenceKey,
        CancellationToken cancellationToken)
    {
        var key = RequireOccurrenceKey(occurrenceKey);
        var ordered = string.Join(',', itemIdsInOrder.Select(id => id.ToString("D")));
        var intent = HashReorderItemsIntent(storyId, expectedVersion, ordered);
        var story = await RequireStoryAsync(tenantId, storyId, cancellationToken);
        var replay = await TryReplayMappedAsync(
            tenantId, story.SellerPartyId, "admin_reorder_items", key, intent, isAdmin: true, cancellationToken, storyId);
        if (replay is not null)
            return replay;
        story.EnsureExpectedVersion(expectedVersion);
        story.ReorderItems(itemIdsInOrder, DateTimeOffset.UtcNow);
        await RecordOccurrenceAsync(
            tenantId, story.SellerPartyId, "admin_reorder_items", key, intent,
            story.StoryId, story.CreatedByActorUserId ?? Guid.Empty, DateTimeOffset.UtcNow, cancellationToken);
        story = await SaveStoryAsync(story, key, intent, cancellationToken);
        return MapAdmin(story, isAdmin: true);
    }

    /// <inheritdoc />
    public async Task<AdminStorySnapshot> AdminSoftDisableAsync(
        Guid tenantId,
        Guid storyId,
        int? expectedVersion,
        CancellationToken cancellationToken,
        string? occurrenceKey = null)
    {
        var key = RequireOccurrenceKey(occurrenceKey);
        var intent = HashLifecycleIntent(storyId, expectedVersion, "disable");
        var story = await RequireStoryAsync(tenantId, storyId, cancellationToken);
        var replay = await TryReplayMappedAsync(
            tenantId, story.SellerPartyId, "admin_disable", key, intent, isAdmin: true, cancellationToken, storyId);
        if (replay is not null)
            return replay;
        story.EnsureExpectedVersion(expectedVersion);
        story.Disable(DateTimeOffset.UtcNow);
        await RecordOccurrenceAsync(
            tenantId, story.SellerPartyId, "admin_disable", key, intent,
            story.StoryId, story.CreatedByActorUserId ?? Guid.Empty, DateTimeOffset.UtcNow, cancellationToken);
        story = await SaveStoryAsync(story, key, intent, cancellationToken);
        return MapAdmin(story, isAdmin: true);
    }

    /// <inheritdoc />
    public async Task<AdminStorySnapshot> AdminApproveAsync(
        Guid tenantId,
        Guid storyId,
        Guid adminActorUserId,
        int? expectedVersion,
        CancellationToken cancellationToken,
        string? occurrenceKey = null)
    {
        var key = RequireOccurrenceKey(occurrenceKey);
        var intent = HashLifecycleIntent(storyId, expectedVersion, "approve");
        var story = await RequireStoryAsync(tenantId, storyId, cancellationToken);
        var replay = await TryReplayMappedAsync(
            tenantId, story.SellerPartyId, "admin_approve", key, intent, isAdmin: true, cancellationToken, storyId);
        if (replay is not null)
            return replay;
        story.EnsureExpectedVersion(expectedVersion);
        await EnsureStoredContentAuthorizedAsync(story, cancellationToken);
        story.Approve(adminActorUserId, DateTimeOffset.UtcNow);
        await RecordOccurrenceAsync(
            tenantId, story.SellerPartyId, "admin_approve", key, intent,
            story.StoryId, adminActorUserId, DateTimeOffset.UtcNow, cancellationToken);
        story = await SaveStoryAsync(story, key, intent, cancellationToken);
        return MapAdmin(story, isAdmin: true);
    }

    /// <inheritdoc />
    public async Task<AdminStorySnapshot> AdminRejectAsync(
        Guid tenantId,
        Guid storyId,
        Guid adminActorUserId,
        string reason,
        int? expectedVersion,
        CancellationToken cancellationToken,
        string? occurrenceKey = null)
    {
        var key = RequireOccurrenceKey(occurrenceKey);
        var intent = HashCanonical(storyId.ToString("D"), expectedVersion?.ToString(System.Globalization.CultureInfo.InvariantCulture), "reject", reason);
        var story = await RequireStoryAsync(tenantId, storyId, cancellationToken);
        var replay = await TryReplayMappedAsync(
            tenantId, story.SellerPartyId, "admin_reject", key, intent, isAdmin: true, cancellationToken, storyId);
        if (replay is not null)
            return replay;
        story.EnsureExpectedVersion(expectedVersion);
        story.Reject(adminActorUserId, reason, DateTimeOffset.UtcNow);
        await RecordOccurrenceAsync(
            tenantId, story.SellerPartyId, "admin_reject", key, intent,
            story.StoryId, adminActorUserId, DateTimeOffset.UtcNow, cancellationToken);
        story = await SaveStoryAsync(story, key, intent, cancellationToken);
        return MapAdmin(story, isAdmin: true);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminStorySnapshot>> SellerListAsync(
        Guid tenantId,
        Guid sellerPartyId,
        CancellationToken cancellationToken)
    {
        var stories = await LoadStoriesForSellerAsync(tenantId, sellerPartyId, track: false, cancellationToken);
        var mapped = stories
            .OrderBy(story => story.DisplayOrder)
            .ThenBy(story => story.StoryId)
            .Select(story => MapAdmin(story, isAdmin: false))
            .ToList();
        return await EnrichManyAsync(mapped, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AdminStorySnapshot?> SellerGetAsync(
        Guid tenantId,
        Guid sellerPartyId,
        Guid storyId,
        CancellationToken cancellationToken)
    {
        var story = await LoadSellerStoryAsync(tenantId, sellerPartyId, storyId, track: false, cancellationToken);
        return story is null ? null : await EnrichAsync(MapAdmin(story, isAdmin: false), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AdminStorySnapshot> SellerCreateDraftAsync(
        Guid tenantId,
        Guid sellerPartyId,
        Guid actorUserId,
        CreateStoryCommand command,
        CancellationToken cancellationToken)
    {
        var key = RequireOccurrenceKey(command.OccurrenceKey);
        var createIntent = HashIntent(command);
        {
            var replay = await TryReplayMappedAsync(
                tenantId,
                sellerPartyId,
                "seller_create",
                key,
                createIntent,
                isAdmin: false,
                cancellationToken);
            if (replay is not null)
                return replay;
        }

        await EnsureProductCtaAsync(sellerPartyId, command.CtaType, command.CtaTarget, cancellationToken);
        var cover = await BindCoverAsync(
            tenantId, sellerPartyId, command.CoverMediaAssetId, command.CoverMediaUrl,
            allowUnscopedAdminAssets: false, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var order = command.DisplayOrder
            ?? await NextDisplayOrderAsync(tenantId, cancellationToken);
        var story = StoryEntity.CreateSellerDraft(
            tenantId,
            sellerPartyId,
            actorUserId,
            command.Title,
            order,
            now,
            command.Locale,
            command.Market,
            cover.AssetId,
            cover.DisplayUrl,
            command.CtaType,
            command.CtaTarget);
        _db.Stories.Add(story);
        await RecordOccurrenceAsync(
            tenantId,
            sellerPartyId,
            "seller_create",
            key,
            createIntent,
            story.StoryId,
            actorUserId,
            now,
            cancellationToken);
        var foldedSellerCreate = await SaveChangesWithOccurrenceArbitrationAsync(tenantId, key, createIntent, "seller_create", sellerPartyId, expectedStoryId: null, ownedStoryIds: [story.StoryId], cancellationToken);
        if (foldedSellerCreate is Guid foldedSellerStoryId)
        {
            story = await LoadStoryAsync(tenantId, foldedSellerStoryId, track: false, cancellationToken)
                ?? throw new SemanticException(new SemanticError(StoryErrorCodes.Missing));
        }
        return await EnrichAsync(MapAdmin(story, isAdmin: false), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AdminStorySnapshot> SellerUpdateAsync(
        Guid tenantId,
        Guid sellerPartyId,
        Guid storyId,
        UpdateStoryCommand command,
        CancellationToken cancellationToken)
    {
        var key = RequireOccurrenceKey(command.OccurrenceKey);
        var intent = HashUpdateStoryIntent(storyId, command);
        var replay = await TryReplayMappedAsync(
            tenantId, sellerPartyId, "seller_update", key, intent, isAdmin: false, cancellationToken, storyId);
        if (replay is not null)
            return replay;
        var story = await RequireSellerEditableAsync(tenantId, sellerPartyId, storyId, cancellationToken);
        story.EnsureExpectedVersion(command.ExpectedVersion);
        await EnsureProductCtaAsync(sellerPartyId, command.CtaType, command.CtaTarget, cancellationToken);
        var cover = await BindCoverAsync(
            tenantId, sellerPartyId, command.CoverMediaAssetId, command.CoverMediaUrl,
            allowUnscopedAdminAssets: false, cancellationToken);
        story.Update(
            command.Title,
            command.Locale,
            command.Market,
            cover.AssetId,
            cover.DisplayUrl,
            command.CtaType,
            command.CtaTarget,
            DateTimeOffset.UtcNow);
        await RecordOccurrenceAsync(
            tenantId, sellerPartyId, "seller_update", key, intent,
            story.StoryId, story.CreatedByActorUserId ?? Guid.Empty, DateTimeOffset.UtcNow, cancellationToken);
        story = await SaveStoryAsync(story, key, intent, cancellationToken);
        return await EnrichAsync(MapAdmin(story, isAdmin: false), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AdminStorySnapshot> SellerSubmitAsync(
        Guid tenantId,
        Guid sellerPartyId,
        Guid storyId,
        Guid actorUserId,
        CancellationToken cancellationToken,
        int? expectedVersion = null,
        string? occurrenceKey = null)
    {
        var key = RequireOccurrenceKey(occurrenceKey);
        var intent = HashLifecycleIntent(storyId, expectedVersion, "submit");
        var replay = await TryReplayLifecycleAsync(
            tenantId, sellerPartyId, "seller_submit", key, intent, isAdmin: false, cancellationToken);
        if (replay is not null)
            return replay;

        var story = await RequireSellerStoryAsync(tenantId, sellerPartyId, storyId, cancellationToken);
        story.EnsureExpectedVersion(expectedVersion);
        await EnsureStoredContentAuthorizedAsync(story, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        story.SubmitForReview(actorUserId, now);
        await RecordOccurrenceAsync(
            tenantId, sellerPartyId, "seller_submit", key, intent,
            story.StoryId, actorUserId, now, cancellationToken);
        story = await SaveStoryAsync(story, key, intent, cancellationToken);
        return await EnrichAsync(MapAdmin(story, isAdmin: false), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AdminStorySnapshot> SellerAddItemAsync(
        Guid tenantId,
        Guid sellerPartyId,
        Guid storyId,
        AddStoryItemCommand command,
        CancellationToken cancellationToken)
    {
        var key = RequireOccurrenceKey(command.OccurrenceKey);
        var itemIntent = HashItemIntent(storyId, command);
        {
            var replay = await TryReplayMappedAsync(
                tenantId,
                sellerPartyId,
                "seller_add_item",
                key,
                itemIntent,
                isAdmin: false,
                cancellationToken,
                expectedStoryId: storyId);
            if (replay is not null)
                return replay;
        }

        var story = await RequireSellerEditableAsync(tenantId, sellerPartyId, storyId, cancellationToken);
        story.EnsureExpectedVersion(command.ExpectedVersion);
        await EnsureProductCtaAsync(sellerPartyId, command.CtaType, command.CtaTarget, cancellationToken);
        var bound = await BindSellerMediaAsync(
            tenantId, sellerPartyId, command.MediaType, command.MediaAssetId, command.MediaUrl, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var order = command.DisplayOrder
            ?? (story.Items.Count == 0 ? 0 : story.Items.Max(item => item.DisplayOrder) + 1);
        story.AddItem(
            command.MediaType,
            order,
            now,
            bound.AssetId,
            bound.DisplayUrl,
            command.Caption,
            command.DurationMs,
            command.CtaType,
            command.CtaTarget);
        await RecordOccurrenceAsync(
            tenantId,
            sellerPartyId,
            "seller_add_item",
            key,
            itemIntent,
            story.StoryId,
            story.CreatedByActorUserId ?? Guid.Empty,
            now,
            cancellationToken);
        story = await SaveStoryAsync(story, key, itemIntent, cancellationToken);
        return await EnrichAsync(MapAdmin(story, isAdmin: false), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AdminStorySnapshot> SellerUpdateItemAsync(
        Guid tenantId,
        Guid sellerPartyId,
        Guid storyId,
        Guid itemId,
        UpdateStoryItemCommand command,
        CancellationToken cancellationToken)
    {
        var key = RequireOccurrenceKey(command.OccurrenceKey);
        var intent = HashUpdateItemIntent(storyId, itemId, command);
        var replay = await TryReplayMappedAsync(
            tenantId, sellerPartyId, "seller_update_item", key, intent, isAdmin: false, cancellationToken, storyId);
        if (replay is not null)
            return replay;
        var story = await RequireSellerEditableAsync(tenantId, sellerPartyId, storyId, cancellationToken);
        story.EnsureExpectedVersion(command.ExpectedVersion);
        await EnsureProductCtaAsync(sellerPartyId, command.CtaType, command.CtaTarget, cancellationToken);
        var bound = await BindSellerMediaAsync(
            tenantId, sellerPartyId, command.MediaType, command.MediaAssetId, command.MediaUrl, cancellationToken);
        story.UpdateItem(
            itemId,
            command.MediaType,
            bound.AssetId,
            bound.DisplayUrl,
            command.Caption,
            command.DurationMs,
            command.CtaType,
            command.CtaTarget,
            DateTimeOffset.UtcNow);
        await RecordOccurrenceAsync(
            tenantId, sellerPartyId, "seller_update_item", key, intent,
            story.StoryId, story.CreatedByActorUserId ?? Guid.Empty, DateTimeOffset.UtcNow, cancellationToken);
        story = await SaveStoryAsync(story, key, intent, cancellationToken);
        return await EnrichAsync(MapAdmin(story, isAdmin: false), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AdminStorySnapshot> SellerRemoveItemAsync(
        Guid tenantId,
        Guid sellerPartyId,
        Guid storyId,
        Guid itemId,
        int? expectedVersion,
        string? occurrenceKey,
        CancellationToken cancellationToken)
    {
        var key = RequireOccurrenceKey(occurrenceKey);
        var intent = HashRemoveItemIntent(storyId, itemId, expectedVersion);
        var replay = await TryReplayMappedAsync(
            tenantId, sellerPartyId, "seller_remove_item", key, intent, isAdmin: false, cancellationToken, storyId);
        if (replay is not null)
            return replay;
        var story = await RequireSellerEditableAsync(tenantId, sellerPartyId, storyId, cancellationToken);
        story.EnsureExpectedVersion(expectedVersion);
        story.RemoveItem(itemId, DateTimeOffset.UtcNow);
        await RecordOccurrenceAsync(
            tenantId, sellerPartyId, "seller_remove_item", key, intent,
            story.StoryId, story.CreatedByActorUserId ?? Guid.Empty, DateTimeOffset.UtcNow, cancellationToken);
        story = await SaveStoryAsync(story, key, intent, cancellationToken);
        return MapAdmin(story, isAdmin: false);
    }

    /// <inheritdoc />
    public async Task<AdminStorySnapshot> SellerReorderItemsAsync(
        Guid tenantId,
        Guid sellerPartyId,
        Guid storyId,
        IReadOnlyList<Guid> itemIdsInOrder,
        int? expectedVersion,
        string? occurrenceKey,
        CancellationToken cancellationToken)
    {
        var key = RequireOccurrenceKey(occurrenceKey);
        var ordered = string.Join(',', itemIdsInOrder.Select(id => id.ToString("D")));
        var intent = HashReorderItemsIntent(storyId, expectedVersion, ordered);
        var replay = await TryReplayMappedAsync(
            tenantId, sellerPartyId, "seller_reorder_items", key, intent, isAdmin: false, cancellationToken, storyId);
        if (replay is not null)
            return replay;
        var story = await RequireSellerEditableAsync(tenantId, sellerPartyId, storyId, cancellationToken);
        story.EnsureExpectedVersion(expectedVersion);
        story.ReorderItems(itemIdsInOrder, DateTimeOffset.UtcNow);
        await RecordOccurrenceAsync(
            tenantId, sellerPartyId, "seller_reorder_items", key, intent,
            story.StoryId, story.CreatedByActorUserId ?? Guid.Empty, DateTimeOffset.UtcNow, cancellationToken);
        story = await SaveStoryAsync(story, key, intent, cancellationToken);
        return MapAdmin(story, isAdmin: false);
    }
}
