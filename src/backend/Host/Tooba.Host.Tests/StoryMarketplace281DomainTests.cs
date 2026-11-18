using Tooba.BuildingBlocks;
using Tooba.Story.Contracts.Errors;
using Tooba.Story.Domain.Aggregates;
using Tooba.Story.Domain.Enums;
using Tooba.Story.Domain.Policies;
using Tooba.Story.Domain.Tenant;
using Tooba.Story.Infrastructure.Directories;
using Tooba.Story.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Tooba.Story.Application.Stories.Models;
using Xunit;
using StoryAggregate = Tooba.Story.Domain.Aggregates.Story;

namespace Tooba.Host.Tests;

/// <summary>TB-281 focused domain/directory facts (in-memory; no Docker seed).</summary>
public sealed class StoryMarketplace281DomainTests
{
    [Fact]
    public void Scheduled_story_becomes_public_when_start_is_due_without_status_rewrite()
    {
        var now = DateTimeOffset.Parse("2026-11-19T12:00:00Z");
        var story = StoryAggregate.CreateSellerDraft(
            StoryTenantIds.StoreAlpha,
            Guid.Parse("01a030d1-40cb-7000-8abe-6d31739956c5"),
            Guid.Parse("01a03628-3f68-7000-844d-99f1cadb54b0"),
            "ready",
            0,
            now.AddHours(-2));
        story.AddItem("image", 0, now.AddHours(-2), mediaUrl: "https://cdn.example/a.jpg");
        story.SubmitForReview(Guid.Parse("01a03628-3f68-7000-844d-99f1cadb54b0"), now.AddHours(-1));
        story.Approve(Guid.Parse("01a036c2-970e-7000-8eb7-94bf5cc2d8db"), now.AddMinutes(-30));
        story.SetSchedule(now.AddMinutes(30), now.AddHours(2), now.AddMinutes(-30));
        Assert.Equal(StoryStatus.Scheduled, story.Status);
        Assert.False(StoryPublicationPolicy.IsEffectivelyPublic(story, now));
        Assert.True(StoryPublicationPolicy.IsEffectivelyPublic(story, now.AddMinutes(31)));
        Assert.False(StoryPublicationPolicy.IsEffectivelyPublic(story, now.AddHours(3)));
    }

    [Fact]
    public void Submit_and_publish_refuse_empty_content()
    {
        var now = DateTimeOffset.UtcNow;
        var story = StoryAggregate.CreateSellerDraft(
            StoryTenantIds.StoreAlpha,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "empty",
            0,
            now);
        var ex = Assert.Throws<SemanticException>(() => story.SubmitForReview(Guid.NewGuid(), now));
        Assert.Equal(StoryErrorCodes.ContentNotReady, ex.Error.Code);
    }

    [Fact]
    public async Task Admin_create_for_shop_is_visible_only_to_that_shop()
    {
        await using var db = CreateDb();
        var directory = new StoryDirectory(db);
        var shopA = Guid.Parse("01a030d1-40cb-7000-8abe-6d31739956c5");
        var shopB = Guid.Parse("01a0ffff-0001-7000-8000-000000000001");
        var admin = Guid.Parse("01a036c2-970e-7000-8eb7-94bf5cc2d8db");
        var created = await directory.AdminCreateForShopAsync(
            StoryTenantIds.StoreAlpha,
            admin,
            shopA,
            new CreateStoryCommand("for A", null, null, null, null, null, null, null, shopA, "occ-1"),
            CancellationToken.None);
        Assert.Equal(shopA, created.SellerPartyId);
        Assert.Equal(admin, created.CreatedByActorUserId);
        Assert.Equal(StoryOrigin.Seller, created.Origin);
        var inA = await directory.SellerGetAsync(StoryTenantIds.StoreAlpha, shopA, created.StoryId, CancellationToken.None);
        var inB = await directory.SellerGetAsync(StoryTenantIds.StoreAlpha, shopB, created.StoryId, CancellationToken.None);
        Assert.NotNull(inA);
        Assert.Null(inB);
        var replay = await directory.AdminCreateForShopAsync(
            StoryTenantIds.StoreAlpha,
            admin,
            shopA,
            new CreateStoryCommand("for A", null, null, null, null, null, null, null, shopA, "occ-1"),
            CancellationToken.None);
        Assert.Equal(created.StoryId, replay.StoryId);
    }

    [Fact]
    public async Task Stale_version_blocks_approve_and_publish()
    {
        await using var db = CreateDb();
        var directory = new StoryDirectory(db);
        var shop = Guid.NewGuid();
        var seller = Guid.NewGuid();
        var admin = Guid.NewGuid();
        var draft = await directory.SellerCreateDraftAsync(
            StoryTenantIds.StoreAlpha,
            shop,
            seller,
            new CreateStoryCommand("v", null, null, null, "https://cdn.example/c.jpg", null, null, null),
            CancellationToken.None);
        await directory.SellerAddItemAsync(
            StoryTenantIds.StoreAlpha,
            shop,
            draft.StoryId,
            new AddStoryItemCommand(
                "image",
                Guid.Parse("01a0aaaa-0001-7000-8000-000000000001"),
                "https://cdn.example/i.jpg",
                null,
                null,
                null,
                null,
                null),
            CancellationToken.None);
        await directory.SellerSubmitAsync(StoryTenantIds.StoreAlpha, shop, draft.StoryId, seller, CancellationToken.None);
        var current = await directory.SellerGetAsync(StoryTenantIds.StoreAlpha, shop, draft.StoryId, CancellationToken.None);
        Assert.NotNull(current);
        var ex = await Assert.ThrowsAsync<SemanticException>(() =>
            directory.AdminApproveAndPublishAsync(
                StoryTenantIds.StoreAlpha,
                draft.StoryId,
                admin,
                current!.VersionToken - 1,
                CancellationToken.None));
        Assert.Equal(StoryErrorCodes.VersionStale, ex.Error.Code);
    }

    private static StoryDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<StoryDbContext>()
            .UseInMemoryDatabase("tb281-" + Guid.NewGuid().ToString("N"))
            .Options;
        return new StoryDbContext(options);
    }
}
