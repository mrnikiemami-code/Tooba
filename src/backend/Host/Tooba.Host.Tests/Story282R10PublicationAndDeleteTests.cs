using Tooba.Story.Contracts.Errors;
using Tooba.Story.Domain.Aggregates;
using Tooba.Story.Domain.Enums;
using Tooba.Story.Domain.Policies;
using Xunit;
using StoryEntity = Tooba.Story.Domain.Aggregates.Story;

namespace Tooba.Host.Tests;

/// <summary>TB-282-R10 — playable seller asset gate, draft window, delete gates.</summary>
public sealed class Story282R10PublicationAndDeleteTests
{
    [Fact]
    public void Seller_url_only_item_is_not_playable_and_omits_submit()
    {
        var now = DateTimeOffset.Parse("2026-11-19T12:00:00Z");
        var shop = Guid.Parse("01a030d1-40cb-7000-8abe-6d31739956c5");
        var actor = Guid.Parse("01a03628-3f68-7000-844d-99f1cadb54b0");
        var story = StoryEntity.CreateSellerDraft(
            Guid.Parse("a0000000-0001-4000-8000-000000000001"),
            shop,
            actor,
            "پیش‌نویس فروشنده",
            1,
            now);
        story.AddItem("image", 0, now, mediaAssetId: null, mediaUrl: "/images/stories/1.jpg");

        Assert.False(StoryPublicationPolicy.HasPlayableContent(story));
        var codes = StoryPublicationPolicy.ProjectActionCodes(story, isAdmin: false, now);
        Assert.DoesNotContain("submit", codes);
        Assert.Contains("delete", codes);
        Assert.Contains("update", codes);
    }

    [Fact]
    public void Seller_asset_item_is_playable_and_offers_submit()
    {
        var now = DateTimeOffset.Parse("2026-11-19T12:00:00Z");
        var shop = Guid.Parse("01a030d1-40cb-7000-8abe-6d31739956c5");
        var actor = Guid.Parse("01a03628-3f68-7000-844d-99f1cadb54b0");
        var asset = Guid.Parse("55555555-5555-5555-5555-555555555551");
        var story = StoryEntity.CreateSellerDraft(
            Guid.Parse("a0000000-0001-4000-8000-000000000001"),
            shop,
            actor,
            "پیش‌نویس فروشنده",
            1,
            now);
        story.AddItem("image", 0, now, mediaAssetId: asset, mediaUrl: "/media/x.jpg", durationMs: 7000);

        Assert.True(StoryPublicationPolicy.HasPlayableContent(story));
        var codes = StoryPublicationPolicy.ProjectActionCodes(story, isAdmin: false, now);
        Assert.Contains("submit", codes);
    }

    [Fact]
    public void Draft_publication_window_does_not_activate()
    {
        var now = DateTimeOffset.Parse("2026-11-19T12:00:00Z");
        var story = StoryEntity.CreateDraft(
            Guid.Parse("a0000000-0001-4000-8000-000000000001"),
            "ویترین",
            1,
            now);
        var start = now.AddDays(1);
        var end = now.AddDays(10);
        story.SetDraftPublicationWindow(start, end, now);

        Assert.Equal(StoryStatus.Draft, story.Status);
        Assert.Equal(start, story.StartAt);
        Assert.Equal(end, story.EndAt);
        Assert.False(StoryPublicationPolicy.IsEffectivelyPublic(story, now.AddDays(2)));
    }

    [Fact]
    public void Seller_cannot_delete_submitted_story()
    {
        var now = DateTimeOffset.Parse("2026-11-19T12:00:00Z");
        var shop = Guid.Parse("01a030d1-40cb-7000-8abe-6d31739956c5");
        var actor = Guid.Parse("01a03628-3f68-7000-844d-99f1cadb54b0");
        var asset = Guid.Parse("55555555-5555-5555-5555-555555555551");
        var story = StoryEntity.CreateSellerDraft(
            Guid.Parse("a0000000-0001-4000-8000-000000000001"),
            shop,
            actor,
            "ارسال‌شده",
            1,
            now);
        story.AddItem("image", 0, now, mediaAssetId: asset, mediaUrl: "/media/x.jpg");
        story.SubmitForReview(actor, now);

        var ex = Assert.Throws<Tooba.BuildingBlocks.SemanticException>(() => story.EnsureCanDelete(isAdmin: false));
        Assert.Equal(StoryErrorCodes.MutationRejected, ex.Error.Code);
        story.EnsureCanDelete(isAdmin: true);
    }
}
