using Tooba.Story.Domain.Aggregates;
using Tooba.Story.Domain.Enums;
using Tooba.Story.Domain.Rules;
using StoryAggregate = Tooba.Story.Domain.Aggregates.Story;

namespace Tooba.Story.Domain.Policies;

/// <summary>
/// ONE Story-owned effective publication and readiness authority shared by commands and public reads.
/// </summary>
public static class StoryPublicationPolicy
{
    /// <summary>Drafts may be incomplete; submit/publish require at least one playable slide.</summary>
    public static bool HasPlayableContent(StoryAggregate story)
    {
        ArgumentNullException.ThrowIfNull(story);
        if (story.Items.Count == 0)
            return false;

        foreach (var item in story.Items)
        {
            if (!IsPlayableItem(item))
                return false;
        }

        return true;
    }

    /// <summary>A single slide is playable when it has a Ready-bound asset id or a non-empty media URL.</summary>
    public static bool IsPlayableItem(StoryItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.MediaAssetId is Guid assetId && assetId != Guid.Empty)
            return true;
        return !string.IsNullOrWhiteSpace(item.MediaUrl);
    }

    /// <summary>
    /// Effective public eligibility without requiring a daemon to rewrite Scheduled → Active.
    /// Disabled/Rejected/Draft/Expired never become public merely because a timestamp arrives.
    /// </summary>
    public static bool IsEffectivelyPublic(StoryAggregate story, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(story);
        if (!story.IsPublicationEligible())
            return false;
        if (story.Status is StoryStatus.Disabled or StoryStatus.Expired or StoryStatus.Draft)
            return false;
        if (story.Status is not (StoryStatus.Active or StoryStatus.Scheduled))
            return false;
        if (story.StartAt is { } start && start > now)
            return false;
        if (story.EndAt is { } end && end <= now)
            return false;
        return HasPlayableContent(story);
    }

    /// <summary>Action codes projected for the shared admin/seller workspace.</summary>
    public static IReadOnlyList<string> ProjectActionCodes(
        StoryAggregate story,
        bool isAdmin,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(story);
        var codes = new List<string>();
        var ready = HasPlayableContent(story);

        if (isAdmin)
        {
            codes.Add("update");
            codes.Add("add_item");
            if (story.Origin == StoryOrigin.Seller && story.ReviewStatus == StoryReviewStatus.Submitted)
            {
                codes.Add("approve");
                codes.Add("reject");
                if (ready)
                {
                    codes.Add("approve_and_publish");
                    codes.Add("approve_and_schedule");
                }
            }

            if (story.IsPublicationEligible() && ready)
            {
                codes.Add("publish_now");
                codes.Add("schedule");
            }

            if (story.Status is StoryStatus.Active or StoryStatus.Scheduled)
                codes.Add("stop");
        }
        else
        {
            if (story.IsSellerContentEditable())
            {
                codes.Add("update");
                codes.Add("add_item");
                if (ready)
                    codes.Add("submit");
            }

            if (story.Origin == StoryOrigin.Seller
                && story.SellerPartyId.HasValue
                && story.ReviewStatus == StoryReviewStatus.Approved
                && story.Status is StoryStatus.Active or StoryStatus.Scheduled)
            {
                codes.Add("stop");
                codes.Add("return_to_draft");
            }
        }

        _ = now;
        _ = StoryRules.CtaNone;
        return codes;
    }
}
