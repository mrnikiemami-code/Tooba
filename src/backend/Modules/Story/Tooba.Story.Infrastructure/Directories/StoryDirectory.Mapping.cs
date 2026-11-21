using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Media.Contracts.Assets;
using Tooba.Story.Application.Stories.Models;
using Tooba.Story.Contracts.Errors;
using Tooba.Story.Domain.Aggregates;
using Tooba.Story.Domain.Enums;
using Tooba.Story.Domain.Policies;
using Tooba.Story.Domain.Rules;
using StoryEntity = Tooba.Story.Domain.Aggregates.Story;

namespace Tooba.Story.Infrastructure.Directories;

public sealed partial class StoryDirectory
{
    internal static AdminStorySnapshot MapAdmin(StoryEntity story, bool isAdmin)
    {
        var now = DateTimeOffset.UtcNow;
        var actions = StoryPublicationPolicy.ProjectActionCodes(story, isAdmin, now).ToList();
        return new(
            story.StoryId,
            story.TenantId,
            story.Origin,
            story.SellerPartyId,
            story.ReviewStatus,
            story.SubmittedByActorUserId,
            story.ReviewedByActorUserId,
            story.SubmittedAt,
            story.ReviewedAt,
            story.RejectionReason,
            story.Locale,
            story.Market,
            story.Title,
            story.CoverMediaAssetId,
            story.CoverMediaUrl,
            story.DisplayOrder,
            story.StartAt,
            story.EndAt,
            story.Status,
            story.CtaType,
            story.CtaTarget,
            story.VersionToken,
            story.CreatedAt,
            story.UpdatedAt,
            story.Items
                .OrderBy(item => item.DisplayOrder)
                .Select(MapAdminItem)
                .ToList(),
            story.CreatedByActorUserId,
            ShopDisplayName: null,
            ContentReady: StoryPublicationPolicy.HasPlayableContent(story),
            EffectivelyPublic: StoryPublicationPolicy.IsEffectivelyPublic(story, now),
            ActionCodes: actions);
    }

    private static PublicStoryCard MapPublic(StoryEntity story)
    {
        var items = story.Items
            .OrderBy(item => item.DisplayOrder)
            .Select(MapPublicItem)
            .ToList();
        return new PublicStoryCard(
            story.StoryId,
            story.Title,
            story.CoverMediaUrl,
            items.Any(item => string.Equals(item.MediaType, StoryRules.MediaVideo, StringComparison.Ordinal)),
            story.DisplayOrder,
            story.CtaType,
            story.CtaTarget,
            items,
            story.SellerPartyId,
            EndAt: story.EndAt);
    }

    private static PublicStoryItem MapPublicItem(StoryItem item) => new(
        item.StoryItemId,
        item.MediaType,
        item.MediaUrl,
        item.Caption,
        item.DurationMs,
        item.CtaType,
        item.CtaTarget);

    private static AdminStoryItemSnapshot MapAdminItem(StoryItem item) => new(
        item.StoryItemId,
        item.DisplayOrder,
        item.MediaType,
        item.MediaAssetId,
        item.MediaUrl,
        item.Caption,
        item.DurationMs,
        item.CtaType,
        item.CtaTarget,
        item.CreatedAt,
        item.UpdatedAt);

    private async Task<int> NextDisplayOrderAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var max = await _db.Stories.AsNoTracking()
            .Where(story => story.TenantId == tenantId)
            .Select(story => (int?)story.DisplayOrder)
            .MaxAsync(cancellationToken);
        return (max ?? -1) + 1;
    }

    private async Task<StoryEntity> RequireStoryAsync(
        Guid tenantId,
        Guid storyId,
        CancellationToken cancellationToken)
    {
        var story = await LoadStoryAsync(tenantId, storyId, track: true, cancellationToken);
        return story ?? throw new SemanticException(new SemanticError(StoryErrorCodes.Missing));
    }

    private async Task<StoryEntity> RequireSellerStoryAsync(
        Guid tenantId,
        Guid sellerPartyId,
        Guid storyId,
        CancellationToken cancellationToken)
    {
        var story = await LoadSellerStoryAsync(tenantId, sellerPartyId, storyId, track: true, cancellationToken);
        return story ?? throw new SemanticException(new SemanticError(StoryErrorCodes.Missing));
    }

    private async Task<StoryEntity> RequireSellerEditableAsync(
        Guid tenantId,
        Guid sellerPartyId,
        Guid storyId,
        CancellationToken cancellationToken)
    {
        var story = await RequireSellerStoryAsync(tenantId, sellerPartyId, storyId, cancellationToken);
        if (!story.IsSellerContentEditable())
            throw new SemanticException(new SemanticError(StoryErrorCodes.MutationRejected));
        return story;
    }

    private async Task<StoryEntity?> LoadStoryAsync(
        Guid tenantId,
        Guid storyId,
        bool track,
        CancellationToken cancellationToken)
    {
        var query = track ? _db.Stories : _db.Stories.AsNoTracking();
        var story = await query.FirstOrDefaultAsync(
            row => row.TenantId == tenantId && row.StoryId == storyId,
            cancellationToken);
        if (story is null)
            return null;

        await AttachItemsAsync(story, track, cancellationToken);
        return story;
    }

    private async Task<StoryEntity?> LoadSellerStoryAsync(
        Guid tenantId,
        Guid sellerPartyId,
        Guid storyId,
        bool track,
        CancellationToken cancellationToken)
    {
        var query = track ? _db.Stories : _db.Stories.AsNoTracking();
        var story = await query.FirstOrDefaultAsync(
            row => row.TenantId == tenantId
                && row.StoryId == storyId
                && row.Origin == StoryOrigin.Seller
                && row.SellerPartyId == sellerPartyId,
            cancellationToken);
        if (story is null)
            return null;

        await AttachItemsAsync(story, track, cancellationToken);
        return story;
    }

    private async Task AttachItemsAsync(StoryEntity story, bool track, CancellationToken cancellationToken)
    {
        var itemQuery = track ? _db.StoryItems : _db.StoryItems.AsNoTracking();
        var items = await itemQuery
            .Where(item => item.StoryId == story.StoryId)
            .OrderBy(item => item.DisplayOrder)
            .ToListAsync(cancellationToken);
        story.AttachItems(items);
    }

    private async Task<List<StoryEntity>> LoadStoriesAsync(
        Guid tenantId,
        bool track,
        CancellationToken cancellationToken)
    {
        var query = track ? _db.Stories : _db.Stories.AsNoTracking();
        var stories = await query
            .Where(story => story.TenantId == tenantId)
            .OrderBy(story => story.DisplayOrder)
            .ThenBy(story => story.StoryId)
            .ToListAsync(cancellationToken);
        await AttachItemsForStoriesAsync(stories, track, cancellationToken);
        return stories;
    }

    private async Task<List<StoryEntity>> LoadStoriesForSellerAsync(
        Guid tenantId,
        Guid sellerPartyId,
        bool track,
        CancellationToken cancellationToken)
    {
        var query = track ? _db.Stories : _db.Stories.AsNoTracking();
        var stories = await query
            .Where(story => story.TenantId == tenantId
                && story.Origin == StoryOrigin.Seller
                && story.SellerPartyId == sellerPartyId)
            .OrderBy(story => story.DisplayOrder)
            .ThenBy(story => story.StoryId)
            .ToListAsync(cancellationToken);
        await AttachItemsForStoriesAsync(stories, track, cancellationToken);
        return stories;
    }

    private async Task AttachItemsForStoriesAsync(
        List<StoryEntity> stories,
        bool track,
        CancellationToken cancellationToken)
    {
        if (stories.Count == 0)
            return;

        var ids = stories.Select(story => story.StoryId).ToList();
        var itemQuery = track ? _db.StoryItems : _db.StoryItems.AsNoTracking();
        var items = await itemQuery
            .Where(item => ids.Contains(item.StoryId))
            .OrderBy(item => item.DisplayOrder)
            .ToListAsync(cancellationToken);
        var grouped = items.GroupBy(item => item.StoryId)
            .ToDictionary(group => group.Key, group => group.ToList());
        foreach (var story in stories)
            story.AttachItems(grouped.GetValueOrDefault(story.StoryId, []));
    }

    private async Task<(Guid? AssetId, string? DisplayUrl)> BindSellerMediaAsync(
        Guid tenantId,
        Guid sellerPartyId,
        string? declaredMediaType,
        Guid? mediaAssetId,
        string? mediaUrl,
        CancellationToken cancellationToken)
    {
        if (_media is null)
            throw new SemanticException(new SemanticError(StoryErrorCodes.MediaUnavailable));

        if (mediaAssetId is Guid assetId && assetId != Guid.Empty)
        {
            try
            {
                var authorized = await _media.AuthorizeForStoryAsync(
                    assetId, tenantId, sellerPartyId, allowUnscopedAdminAssets: false, cancellationToken);
                if (!MediaTypeMatches(declaredMediaType, authorized.ContentType, authorized.IsVideo))
                    throw new SemanticException(new SemanticError(StoryErrorCodes.MediaForbidden));
                return (authorized.MediaAssetId, authorized.DisplayUrl);
            }
            catch (ContractOperationException)
            {
                throw new SemanticException(new SemanticError(StoryErrorCodes.MediaForbidden));
            }
        }

        // Seller submissions cannot bypass ownership with a raw external URL.
        if (!string.IsNullOrWhiteSpace(mediaUrl))
            throw new SemanticException(new SemanticError(StoryErrorCodes.MediaForbidden));

        return (null, null);
    }

    private static bool MediaTypeMatches(string? declaredMediaType, string contentType, bool isVideo)
    {
        if (string.IsNullOrWhiteSpace(declaredMediaType))
            return true;
        var declared = declaredMediaType.Trim().ToLowerInvariant();
        if (declared is "video" or "mp4")
            return isVideo || contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);
        if (declared is "image" or "photo" or "jpg" or "jpeg" or "png" or "webp")
            return !isVideo && contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        return true;
    }

}
