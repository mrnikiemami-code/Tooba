using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Media.Contracts.Assets;
using Tooba.Party.Contracts.Ports;
using Tooba.Story.Application.Stories.Models;
using Tooba.Story.Application.Stories.Ports;
using Tooba.Story.Contracts.Errors;
using Tooba.Story.Domain.Tenant;
using Tooba.Story.Infrastructure.Directories;
using Tooba.Story.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-281-R4 A: version/occurrence transport, platform CTA, AdminSetSchedule content gate.</summary>
public sealed class StoryMarketplace281R4TransportTests
{
    [Fact]
    public async Task Platform_product_cta_uses_catalog_validity_not_shop_ownership()
    {
        await using var db = CreateDb("tb281r4-platform-cta-" + Guid.NewGuid().ToString("N"));
        var product = Guid.NewGuid();
        var directory = new StoryDirectory(db, products: new PlatformProducts(product));

        var created = await directory.AdminCreateAsync(
            StoryTenantIds.StoreAlpha,
            new CreateStoryCommand(
                "platform", null, null, null, null, null,
                "product", product.ToString("D"),
                OccurrenceKey: "plat-create-1"),
            CancellationToken.None);
        Assert.Null(created.SellerPartyId);
        Assert.Equal(product.ToString("D"), created.CtaTarget);

        var missing = await Assert.ThrowsAsync<SemanticException>(() =>
            directory.AdminCreateAsync(
                StoryTenantIds.StoreAlpha,
                new CreateStoryCommand(
                    "bad", null, null, null, null, null,
                    "product", Guid.NewGuid().ToString("D"),
                    OccurrenceKey: "plat-create-bad"),
                CancellationToken.None));
        Assert.Equal(StoryErrorCodes.CtaRejected, missing.Error.Code);
    }

    [Fact]
    public async Task Remove_and_reorder_require_version_and_occurrence_on_owning_path()
    {
        await using var db = CreateDb("tb281r4-remove-reorder-" + Guid.NewGuid().ToString("N"));
        var shop = Guid.NewGuid();
        var seller = Guid.NewGuid();
        var media = Guid.NewGuid();
        var directory = new StoryDirectory(db, new StubParty(shop), new StubMedia(media));

        var draft = await directory.SellerCreateDraftAsync(
            StoryTenantIds.StoreAlpha, shop, seller,
            new CreateStoryCommand("r", null, null, null, null, null, null, null, OccurrenceKey: "r4-c1"),
            CancellationToken.None);
        var withItem = await directory.SellerAddItemAsync(
            StoryTenantIds.StoreAlpha, shop, draft.StoryId,
            new AddStoryItemCommand(
                "image", media, null, null, null, null, null, null,
                OccurrenceKey: "r4-add-1", ExpectedVersion: draft.VersionToken),
            CancellationToken.None);
        var itemId = withItem.Items[0].StoryItemId;

        var missingOcc = await Assert.ThrowsAsync<SemanticException>(() =>
            directory.SellerRemoveItemAsync(
                StoryTenantIds.StoreAlpha, shop, draft.StoryId, itemId,
                withItem.VersionToken, null, CancellationToken.None));
        Assert.Equal(StoryErrorCodes.OccurrenceRequired, missingOcc.Error.Code);

        var stale = await Assert.ThrowsAsync<SemanticException>(() =>
            directory.SellerRemoveItemAsync(
                StoryTenantIds.StoreAlpha, shop, draft.StoryId, itemId,
                withItem.VersionToken - 1, "r4-rem-stale", CancellationToken.None));
        Assert.Equal(StoryErrorCodes.VersionStale, stale.Error.Code);

        var removed = await directory.SellerRemoveItemAsync(
            StoryTenantIds.StoreAlpha, shop, draft.StoryId, itemId,
            withItem.VersionToken, "r4-rem-ok", CancellationToken.None);
        Assert.Empty(removed.Items);

        var replay = await directory.SellerRemoveItemAsync(
            StoryTenantIds.StoreAlpha, shop, draft.StoryId, itemId,
            withItem.VersionToken, "r4-rem-ok", CancellationToken.None);
        Assert.Equal(removed.StoryId, replay.StoryId);
        Assert.Empty(replay.Items);
    }

    [Fact]
    public async Task Admin_set_schedule_requires_stored_content_authorization()
    {
        await using var db = CreateDb("tb281r4-schedule-gate-" + Guid.NewGuid().ToString("N"));
        var shop = Guid.NewGuid();
        var seller = Guid.NewGuid();
        var media = Guid.NewGuid();
        var owned = Guid.NewGuid();
        var directory = new StoryDirectory(
            db, new StubParty(shop), new StubMedia(media), new StubProducts(shop, owned));

        var draft = await directory.SellerCreateDraftAsync(
            StoryTenantIds.StoreAlpha, shop, seller,
            new CreateStoryCommand("s", null, null, null, null, null, null, null, OccurrenceKey: "r4-sc"),
            CancellationToken.None);
        // Empty story (no items) fails content readiness / media authorization on schedule.
        var refused = await Assert.ThrowsAsync<SemanticException>(() =>
            directory.AdminSetScheduleAsync(
                StoryTenantIds.StoreAlpha,
                draft.StoryId,
                new SetStoryScheduleCommand(
                    DateTimeOffset.UtcNow.AddHours(1),
                    DateTimeOffset.UtcNow.AddHours(2),
                    draft.VersionToken,
                    "r4-sched-1"),
                CancellationToken.None));
        Assert.Equal(StoryErrorCodes.ContentNotReady, refused.Error.Code);
    }

    [Fact]
    public async Task Admin_reorder_stories_requires_per_row_versions_and_occurrence()
    {
        await using var db = CreateDb("tb281r4-reorder-" + Guid.NewGuid().ToString("N"));
        var directory = new StoryDirectory(db);
        var a = await directory.AdminCreateAsync(
            StoryTenantIds.StoreAlpha,
            new CreateStoryCommand("a", null, null, null, null, null, null, null, OccurrenceKey: "r4-ra"),
            CancellationToken.None);
        var b = await directory.AdminCreateAsync(
            StoryTenantIds.StoreAlpha,
            new CreateStoryCommand("b", null, null, null, null, null, null, null, OccurrenceKey: "r4-rb"),
            CancellationToken.None);

        var missing = await Assert.ThrowsAsync<SemanticException>(() =>
            directory.AdminReorderStoriesAsync(
                StoryTenantIds.StoreAlpha,
                [b.StoryId, a.StoryId],
                [b.VersionToken, a.VersionToken],
                null,
                CancellationToken.None));
        Assert.Equal(StoryErrorCodes.OccurrenceRequired, missing.Error.Code);

        var reordered = await directory.AdminReorderStoriesAsync(
            StoryTenantIds.StoreAlpha,
            [b.StoryId, a.StoryId],
            [b.VersionToken, a.VersionToken],
            "r4-reorder-1",
            CancellationToken.None);
        Assert.Equal(b.StoryId, reordered[0].StoryId);
        Assert.Equal(a.StoryId, reordered[1].StoryId);
    }

    private static StoryDbContext CreateDb(string name) =>
        new(new DbContextOptionsBuilder<StoryDbContext>().UseInMemoryDatabase(name).Options);

    private sealed class StubParty(Guid eligible) : IPartyAdminSellerReadGateway
    {
        public Task<IReadOnlyList<PartyStatusProjection>> GetStatusProjectionsAsync(
            IReadOnlyList<Guid> partyIds, CancellationToken cancellationToken)
        {
            IReadOnlyList<PartyStatusProjection> rows = partyIds
                .Where(id => id == eligible)
                .Select(id => new PartyStatusProjection(id, "Shop " + id.ToString("N")[..8], "Active"))
                .ToList();
            return Task.FromResult(rows);
        }
    }

    private sealed class StubMedia(Guid ok) : IStoryMediaAssetPort
    {
        public Task<StoryMediaAssetAuthorization> AuthorizeForStoryAsync(
            Guid mediaAssetId, Guid tenantId, Guid? sellerPartyId, bool allowUnscopedAdminAssets,
            CancellationToken cancellationToken)
        {
            if (mediaAssetId != ok)
                throw new ContractOperationException("media.owner.forbidden");
            return Task.FromResult(new StoryMediaAssetAuthorization(
                mediaAssetId, "image/jpeg", $"/v1/storefront/media/{mediaAssetId:D}", false, sellerPartyId));
        }

        public Task<StoryMediaAssetAuthorization> UploadForSellerAsync(
            Stream stream, string fileName, string contentType, Guid tenantId, Guid sellerPartyId,
            Guid actorUserId, string? occurrenceKey, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<StoryMediaAssetAuthorization>> ListSellerAssetsAsync(
            Guid tenantId, Guid sellerPartyId, int take, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<StoryMediaAssetAuthorization>>([]);
    }

    private sealed class StubProducts(Guid shop, Guid ownedProduct) : IStoryProductTargetPort
    {
        public Task EnsureOwnedBySellerAsync(Guid productId, Guid sellerPartyId, CancellationToken cancellationToken)
        {
            if (sellerPartyId != shop || productId != ownedProduct)
                throw new SemanticException(new SemanticError(StoryErrorCodes.CtaRejected));
            return Task.CompletedTask;
        }

        public Task EnsurePublishedExistsAsync(Guid productId, CancellationToken cancellationToken)
        {
            if (productId != ownedProduct)
                throw new SemanticException(new SemanticError(StoryErrorCodes.CtaRejected));
            return Task.CompletedTask;
        }
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
    }
}
