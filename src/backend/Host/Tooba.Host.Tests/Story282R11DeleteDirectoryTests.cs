using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Media.Contracts.Assets;
using Tooba.Party.Contracts.Ports;
using Tooba.Story.Application.Stories.Models;
using Tooba.Story.Application.Stories.Ports;
using Tooba.Story.Contracts.Errors;
using Tooba.Story.Domain.Policies;
using Tooba.Story.Domain.Tenant;
using Tooba.Story.Infrastructure.Directories;
using Tooba.Story.Infrastructure.Persistence;
using Xunit;
using StoryEntity = Tooba.Story.Domain.Aggregates.Story;

namespace Tooba.Host.Tests;

/// <summary>TB-282-R11 A/B — real AdminDelete/SellerDelete EF + effective publication status.</summary>
public sealed class Story282R11DeleteDirectoryTests
{
    [Fact]
    public async Task Admin_delete_shop_story_exact_replay_returns_tombstone_not_conflict()
    {
        var (db, dir, media, shop) = await CreateSellerDraftDirAsync();
        var actor = Guid.NewGuid();
        var created = await dir.SellerCreateDraftAsync(
            StoryTenantIds.StoreAlpha,
            shop,
            actor,
            new CreateStoryCommand("فروشگاهی", null, null, media, null, null, null, null, OccurrenceKey: "r11-c0"),
            CancellationToken.None);
        var withItem = await dir.SellerAddItemAsync(
            StoryTenantIds.StoreAlpha,
            shop,
            created.StoryId,
            new AddStoryItemCommand("image", media, "/m/a.jpg", null, 7000, "none", null, 0, "r11-ai0", created.VersionToken),
            CancellationToken.None);

        var beforeItems = await db.StoryItems.CountAsync(i => i.StoryId == created.StoryId);
        Assert.Equal(1, beforeItems);

        var deleted = await dir.AdminDeleteAsync(
            StoryTenantIds.StoreAlpha, created.StoryId, withItem.VersionToken, "r11-adel", CancellationToken.None);
        Assert.Equal(created.StoryId, deleted.StoryId);
        Assert.Equal(0, await db.Stories.CountAsync(s => s.StoryId == created.StoryId));
        Assert.Equal(0, await db.StoryItems.CountAsync(i => i.StoryId == created.StoryId));
        Assert.Equal(1, await db.CommandOccurrences.CountAsync(o => o.OccurrenceKey == "r11-adel"));
        var occ = await db.CommandOccurrences.SingleAsync(o => o.OccurrenceKey == "r11-adel");
        Assert.Equal(shop, occ.SellerPartyId);
        Assert.Equal("admin_delete", occ.ActionCode);

        // Exact retry after target gone — must tombstone, not OccurrenceConflict (R10 bug).
        var replay = await dir.AdminDeleteAsync(
            StoryTenantIds.StoreAlpha, created.StoryId, withItem.VersionToken, "r11-adel", CancellationToken.None);
        Assert.Equal(created.StoryId, replay.StoryId);
        Assert.False(replay.EffectivelyPublic);
        Assert.Equal(1, await db.CommandOccurrences.CountAsync(o => o.OccurrenceKey == "r11-adel"));
    }

    [Fact]
    public async Task Admin_delete_platform_story_replay_and_seller_own_delete()
    {
        var options = new DbContextOptionsBuilder<StoryDbContext>()
            .UseInMemoryDatabase("r11-plat-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new StoryDbContext(options);
        var media = Guid.NewGuid();
        var dir = new StoryDirectory(db, null, new StubMedia(media), new PlatformProducts(Guid.NewGuid()));
        var created = await dir.AdminCreateAsync(
            StoryTenantIds.StoreAlpha,
            new CreateStoryCommand("ویترین", null, null, media, null, null, null, null, OccurrenceKey: "r11-ac0"),
            CancellationToken.None);

        var gone = await dir.AdminDeleteAsync(
            StoryTenantIds.StoreAlpha, created.StoryId, created.VersionToken, "r11-apdel", CancellationToken.None);
        Assert.Equal(created.StoryId, gone.StoryId);
        var again = await dir.AdminDeleteAsync(
            StoryTenantIds.StoreAlpha, created.StoryId, created.VersionToken, "r11-apdel", CancellationToken.None);
        Assert.Equal(created.StoryId, again.StoryId);
        Assert.Equal(0, await db.Stories.CountAsync());
    }

    [Fact]
    public async Task Seller_delete_own_removes_items_foreign_shop_and_stale_version_refused()
    {
        var (db, dir, media, shop) = await CreateSellerDraftDirAsync();
        var otherShop = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var created = await dir.SellerCreateDraftAsync(
            StoryTenantIds.StoreAlpha,
            shop,
            actor,
            new CreateStoryCommand("مالک", null, null, media, null, null, null, null, OccurrenceKey: "r11-sc0"),
            CancellationToken.None);
        var withItem = await dir.SellerAddItemAsync(
            StoryTenantIds.StoreAlpha,
            shop,
            created.StoryId,
            new AddStoryItemCommand("image", media, "/m/b.jpg", null, 5000, "none", null, 0, "r11-sai", created.VersionToken),
            CancellationToken.None);

        var foreign = await Assert.ThrowsAsync<SemanticException>(() =>
            dir.SellerDeleteAsync(
                StoryTenantIds.StoreAlpha, otherShop, created.StoryId, withItem.VersionToken, "r11-fdel", CancellationToken.None));
        Assert.Equal(StoryErrorCodes.Missing, foreign.Error.Code);
        Assert.Equal(1, await db.Stories.CountAsync(s => s.StoryId == created.StoryId));

        var stale = await Assert.ThrowsAsync<SemanticException>(() =>
            dir.SellerDeleteAsync(
                StoryTenantIds.StoreAlpha, shop, created.StoryId, withItem.VersionToken - 1, "r11-stale", CancellationToken.None));
        Assert.Equal(StoryErrorCodes.VersionStale, stale.Error.Code);

        var deleted = await dir.SellerDeleteAsync(
            StoryTenantIds.StoreAlpha, shop, created.StoryId, withItem.VersionToken, "r11-sdel", CancellationToken.None);
        Assert.Equal(created.StoryId, deleted.StoryId);
        Assert.Equal(0, await db.Stories.CountAsync(s => s.StoryId == created.StoryId));
        Assert.Equal(0, await db.StoryItems.CountAsync(i => i.StoryId == created.StoryId));

        var replay = await dir.SellerDeleteAsync(
            StoryTenantIds.StoreAlpha, shop, created.StoryId, withItem.VersionToken, "r11-sdel", CancellationToken.None);
        Assert.Equal(created.StoryId, replay.StoryId);

        // Foreign occurrence action mismatch → conflict.
        var conflict = await Assert.ThrowsAsync<SemanticException>(() =>
            dir.AdminDeleteAsync(
                StoryTenantIds.StoreAlpha, created.StoryId, withItem.VersionToken, "r11-sdel", CancellationToken.None));
        Assert.Equal(StoryErrorCodes.OccurrenceConflict, conflict.Error.Code);
    }

    [Fact]
    public void Effectively_public_false_for_past_end_active_and_scheduled()
    {
        var now = DateTimeOffset.Parse("2026-11-19T12:00:00Z");
        var media = Guid.NewGuid();
        var tenant = Guid.Parse("a0000000-0001-4000-8000-000000000001");

        // Admin origin is publication-eligible without review.
        var expired = StoryEntity.CreateDraft(tenant, "E", 1, now, coverMediaAssetId: media);
        expired.AddItem("image", 0, now, mediaAssetId: media, mediaUrl: "/m.jpg");
        expired.SetSchedule(now.AddDays(-10), now.AddDays(-1), now);
        Assert.Equal(Tooba.Story.Domain.Enums.StoryStatus.Expired, expired.Status);
        Assert.False(StoryPublicationPolicy.IsEffectivelyPublic(expired, now));

        // Stale Active row with past EndAt (public reader clock) must not be effectively public.
        var staleActive = StoryEntity.CreateDraft(Guid.Parse("a0000000-0001-4000-8000-000000000002"), "A", 2, now, coverMediaAssetId: media);
        staleActive.AddItem("image", 0, now, mediaAssetId: media, mediaUrl: "/m.jpg");
        staleActive.SetSchedule(now.AddDays(-2), now.AddDays(2), now);
        Assert.Equal(Tooba.Story.Domain.Enums.StoryStatus.Active, staleActive.Status);
        typeof(StoryEntity).GetProperty("EndAt")!.SetValue(staleActive, now.AddHours(-1));
        Assert.False(StoryPublicationPolicy.IsEffectivelyPublic(staleActive, now));

        var future = StoryEntity.CreateDraft(Guid.Parse("a0000000-0001-4000-8000-000000000003"), "S", 3, now, coverMediaAssetId: media);
        future.AddItem("image", 0, now, mediaAssetId: media, mediaUrl: "/m.jpg");
        future.SetSchedule(now.AddDays(1), now.AddDays(10), now);
        Assert.Equal(Tooba.Story.Domain.Enums.StoryStatus.Scheduled, future.Status);
        Assert.False(StoryPublicationPolicy.IsEffectivelyPublic(future, now));

        var current = StoryEntity.CreateDraft(Guid.Parse("a0000000-0001-4000-8000-000000000004"), "C", 4, now, coverMediaAssetId: media);
        current.AddItem("image", 0, now, mediaAssetId: media, mediaUrl: "/m.jpg");
        current.SetSchedule(now.AddHours(-1), now.AddDays(2), now);
        Assert.True(StoryPublicationPolicy.IsEffectivelyPublic(current, now));
    }

    [Fact]
    public void Draft_window_equal_end_rejected_and_no_activation()
    {
        var now = DateTimeOffset.Parse("2026-11-19T12:00:00Z");
        var story = StoryEntity.CreateDraft(Guid.Parse("a0000000-0001-4000-8000-000000000004"), "W", 1, now);
        var start = now.AddDays(1);
        var ex = Assert.Throws<SemanticException>(() => story.SetDraftPublicationWindow(start, start, now));
        Assert.Equal(StoryErrorCodes.MutationRejected, ex.Error.Code);
        story.SetDraftPublicationWindow(start, start.AddHours(1), now);
        Assert.Equal(Tooba.Story.Domain.Enums.StoryStatus.Draft, story.Status);
        Assert.False(StoryPublicationPolicy.IsEffectivelyPublic(story, now.AddDays(2)));
    }

    private static async Task<(StoryDbContext db, StoryDirectory dir, Guid media, Guid shop)> CreateSellerDraftDirAsync()
    {
        var options = new DbContextOptionsBuilder<StoryDbContext>()
            .UseInMemoryDatabase("r11-sel-" + Guid.NewGuid().ToString("N"))
            .Options;
        var db = new StoryDbContext(options);
        var media = Guid.NewGuid();
        var shop = Guid.NewGuid();
        var dir = new StoryDirectory(db, new StubParty(shop), new StubMedia(media), new StubProducts(shop, Guid.NewGuid()));
        await Task.CompletedTask;
        return (db, dir, media, shop);
    }

    private sealed class StubParty(params Guid[] eligible) : IPartyAdminSellerReadGateway
    {
        private readonly HashSet<Guid> _ok = eligible.ToHashSet();
        public Task<IReadOnlyList<PartyStatusProjection>> GetStatusProjectionsAsync(
            IReadOnlyList<Guid> partyIds, CancellationToken cancellationToken)
        {
            IReadOnlyList<PartyStatusProjection> rows = partyIds
                .Where(_ok.Contains)
                .Select(id => new PartyStatusProjection(id, "Shop", "Active"))
                .ToList();
            return Task.FromResult(rows);
        }
    }

    private sealed class StubMedia(params Guid[] ok) : IStoryMediaAssetPort
    {
        private readonly HashSet<Guid> _ok = ok.ToHashSet();
        public Task<StoryMediaAssetAuthorization> AuthorizeForStoryAsync(
            Guid mediaAssetId, Guid tenantId, Guid? sellerPartyId, bool allowUnscopedAdminAssets,
            CancellationToken cancellationToken)
        {
            if (!_ok.Contains(mediaAssetId))
                throw new ContractOperationException("media.owner.forbidden");
            return Task.FromResult(new StoryMediaAssetAuthorization(
                mediaAssetId, "image/jpeg", $"/v1/storefront/media/{mediaAssetId:D}", false, sellerPartyId));
        }

        public Task<StoryMediaAssetAuthorization> UploadForSellerAsync(
            Stream stream, string fileName, string contentType, Guid tenantId, Guid sellerPartyId,
            Guid actorUserId, string? occurrenceKey, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<StoryMediaAssetAuthorization> UploadForPlatformStoryAsync(
            Stream stream, string fileName, string contentType, Guid tenantId, Guid actorUserId, string? occurrenceKey, CancellationToken cancellationToken) =>
            Task.FromResult(new StoryMediaAssetAuthorization(Guid.NewGuid(), contentType ?? "image/jpeg", "/v1/storefront/media/00000000-0000-0000-0000-000000000000", false, null));

        public Task<IReadOnlyList<StoryMediaAssetAuthorization>> ListSellerAssetsAsync(
            Guid tenantId, Guid sellerPartyId, int take, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<StoryMediaAssetAuthorization>>([]);
    }

    private sealed class StubProducts(Guid shop, Guid owned) : IStoryProductTargetPort
    {
        public Task EnsureOwnedBySellerAsync(Guid productId, Guid sellerPartyId, CancellationToken cancellationToken)
        {
            if (sellerPartyId != shop || productId != owned)
                throw new SemanticException(new SemanticError(StoryErrorCodes.CtaRejected));
            return Task.CompletedTask;
        }

        public Task EnsurePublishedExistsAsync(Guid productId, CancellationToken cancellationToken) =>
            Task.CompletedTask;
        public Task<string?> TryResolvePublicProductPathAsync(Guid productId, CancellationToken cancellationToken) =>
            Task.FromResult<string?>(productId == Guid.Empty ? null : "/products/" + productId.ToString("N"));
    }

    private sealed class PlatformProducts(Guid published) : IStoryProductTargetPort
    {
        public Task EnsureOwnedBySellerAsync(Guid productId, Guid sellerPartyId, CancellationToken cancellationToken) =>
            throw new SemanticException(new SemanticError(StoryErrorCodes.CtaRejected));

        public Task EnsurePublishedExistsAsync(Guid productId, CancellationToken cancellationToken)
        {
            if (productId != published)
                throw new SemanticException(new SemanticError(StoryErrorCodes.CtaRejected));
            return Task.CompletedTask;
        }

        public Task<string?> TryResolvePublicProductPathAsync(Guid productId, CancellationToken cancellationToken) =>
            Task.FromResult<string?>("/products/" + productId.ToString("N"));
    }
}
