using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Media.Contracts.Assets;
using Tooba.Party.Contracts.Ports;
using Tooba.Story.Application.Stories.Models;
using Tooba.Story.Application.Stories.Ports;
using Tooba.Story.Contracts.Errors;
using Tooba.Story.Domain.Enums;
using Tooba.Story.Domain.Tenant;
using Tooba.Story.Infrastructure.Directories;
using Tooba.Story.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-281-R3 focused Directory proofs for remaining owning-path closure.</summary>
public sealed class StoryMarketplace281R3DirectoryTests
{
    [Fact]
    public async Task Admin_for_shop_update_and_item_use_same_media_and_cta_gates_as_seller()
    {
        await using var db = CreateDb("tb281r3-admin-gates-" + Guid.NewGuid().ToString("N"));
        var shop = Guid.NewGuid();
        var admin = Guid.NewGuid();
        var owned = Guid.NewGuid();
        var foreign = Guid.NewGuid();
        var mediaOk = Guid.NewGuid();
        var mediaBad = Guid.NewGuid();
        var directory = new StoryDirectory(
            db, new StubParty(shop), new StubMedia(mediaOk), new StubProducts(shop, owned));

        var draft = await directory.AdminCreateForShopAsync(
            StoryTenantIds.StoreAlpha, admin, shop,
            new CreateStoryCommand("shop", null, null, null, null, null, null, null, OccurrenceKey: "occ-create-1"),
            CancellationToken.None);

        var ctaEx = await Assert.ThrowsAsync<SemanticException>(() =>
            directory.AdminUpdateAsync(
                StoryTenantIds.StoreAlpha, draft.StoryId,
                new UpdateStoryCommand(
                    "shop", null, null, null, null, "product", foreign.ToString("D"), draft.VersionToken, OccurrenceKey: "occ-upd-12"),
                CancellationToken.None));
        Assert.Equal(StoryErrorCodes.CtaRejected, ctaEx.Error.Code);

        var mediaEx = await Assert.ThrowsAsync<SemanticException>(() =>
            directory.AdminAddItemAsync(
                StoryTenantIds.StoreAlpha, draft.StoryId,
                new AddStoryItemCommand(
                    "image", mediaBad, null, null, null, null, null, null, OccurrenceKey: "occ-add-7", ExpectedVersion: draft.VersionToken),
                CancellationToken.None));
        Assert.Equal(StoryErrorCodes.MediaForbidden, mediaEx.Error.Code);

        var withItem = await directory.AdminAddItemAsync(
            StoryTenantIds.StoreAlpha, draft.StoryId,
            new AddStoryItemCommand(
                "image", mediaOk, null, null, null, "product", owned.ToString("D"), null,
                OccurrenceKey: "occ-add-8", ExpectedVersion: draft.VersionToken),
            CancellationToken.None);
        Assert.Equal(owned.ToString("D"), withItem.Items[0].CtaTarget);
    }

    [Fact]
    public async Task Add_item_occurrence_same_key_on_other_story_conflicts()
    {
        await using var db = CreateDb("tb281r3-cross-target-" + Guid.NewGuid().ToString("N"));
        var shop = Guid.NewGuid();
        var seller = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        var directory = new StoryDirectory(db, new StubParty(shop), new StubMedia(mediaId));

        var a = await directory.SellerCreateDraftAsync(
            StoryTenantIds.StoreAlpha, shop, seller,
            new CreateStoryCommand("a", null, null, null, null, null, null, null, OccurrenceKey: "occ-create-2"),
            CancellationToken.None);
        var b = await directory.SellerCreateDraftAsync(
            StoryTenantIds.StoreAlpha, shop, seller,
            new CreateStoryCommand("b", null, null, null, null, null, null, null, OccurrenceKey: "occ-create-3"),
            CancellationToken.None);

        var cmd = new AddStoryItemCommand(
            "image", mediaId, null, null, null, null, null, null,
            OccurrenceKey: "shared-add-key", ExpectedVersion: a.VersionToken);
        var first = await directory.SellerAddItemAsync(
            StoryTenantIds.StoreAlpha, shop, a.StoryId, cmd, CancellationToken.None);
        Assert.Equal(a.StoryId, first.StoryId);

        var replaySame = await directory.SellerAddItemAsync(
            StoryTenantIds.StoreAlpha, shop, a.StoryId,
            cmd with { ExpectedVersion = a.VersionToken },
            CancellationToken.None);
        Assert.Equal(a.StoryId, replaySame.StoryId);

        var conflict = await Assert.ThrowsAsync<SemanticException>(() =>
            directory.SellerAddItemAsync(
                StoryTenantIds.StoreAlpha, shop, b.StoryId,
                cmd with { ExpectedVersion = b.VersionToken },
                CancellationToken.None));
        Assert.Equal(StoryErrorCodes.OccurrenceConflict, conflict.Error.Code);
        Assert.Empty((await directory.SellerGetAsync(StoryTenantIds.StoreAlpha, shop, b.StoryId, CancellationToken.None))!.Items);
    }

    [Fact]
    public async Task Submit_requires_version_and_replays_exact_occurrence()
    {
        await using var db = CreateDb("tb281r3-submit-" + Guid.NewGuid().ToString("N"));
        var shop = Guid.NewGuid();
        var seller = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        var directory = new StoryDirectory(db, new StubParty(shop), new StubMedia(mediaId));
        var draft = await directory.SellerCreateDraftAsync(
            StoryTenantIds.StoreAlpha, shop, seller,
            new CreateStoryCommand("s", null, null, null, null, null, null, null, OccurrenceKey: "occ-create-4"),
            CancellationToken.None);
        var withItem = await directory.SellerAddItemAsync(
            StoryTenantIds.StoreAlpha, shop, draft.StoryId,
            new AddStoryItemCommand("image", mediaId, null, null, null, null, null, null, OccurrenceKey: "occ-add-9", ExpectedVersion: draft.VersionToken),
            CancellationToken.None);

        var missing = await Assert.ThrowsAsync<SemanticException>(() =>
            directory.SellerSubmitAsync(StoryTenantIds.StoreAlpha, shop, draft.StoryId, seller, CancellationToken.None, expectedVersion: null, occurrenceKey: "occ-sub-16"));
        Assert.Equal(StoryErrorCodes.VersionRequired, missing.Error.Code);

        var submitted = await directory.SellerSubmitAsync(
            StoryTenantIds.StoreAlpha, shop, draft.StoryId, seller, CancellationToken.None,
            withItem.VersionToken, "submit-occ-1");
        Assert.Equal(StoryReviewStatus.Submitted, submitted.ReviewStatus);

        var replay = await directory.SellerSubmitAsync(
            StoryTenantIds.StoreAlpha, shop, draft.StoryId, seller, CancellationToken.None,
            withItem.VersionToken, "submit-occ-1");
        Assert.Equal(submitted.StoryId, replay.StoryId);
        Assert.Equal(StoryReviewStatus.Submitted, replay.ReviewStatus);
    }

    [Fact]
    public async Task Public_enrich_uses_catalog_public_seller_id_when_available()
    {
        await using var db = CreateDb("tb281r3-public-id-" + Guid.NewGuid().ToString("N"));
        var shop = Guid.NewGuid();
        var seller = Guid.NewGuid();
        var admin = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        var publicId = "abc123publicid";
        var directory = new StoryDirectory(
            db, new StubParty(shop), new StubMedia(mediaId), null, new StubPublicSellers(shop, publicId, "Shop Name"));

        var draft = await directory.SellerCreateDraftAsync(
            StoryTenantIds.StoreAlpha, shop, seller,
            new CreateStoryCommand("pub", null, null, null, null, null, null, null, OccurrenceKey: "occ-create-5"),
            CancellationToken.None);
        var withItem = await directory.SellerAddItemAsync(
            StoryTenantIds.StoreAlpha, shop, draft.StoryId,
            new AddStoryItemCommand("image", mediaId, null, null, null, null, null, null, OccurrenceKey: "occ-add-10", ExpectedVersion: draft.VersionToken),
            CancellationToken.None);
        await directory.SellerSubmitAsync(
            StoryTenantIds.StoreAlpha, shop, draft.StoryId, seller, CancellationToken.None,
            withItem.VersionToken, "sub-pub");
        var submitted = await directory.SellerGetAsync(StoryTenantIds.StoreAlpha, shop, draft.StoryId, CancellationToken.None);
        await directory.AdminApproveAndPublishAsync(StoryTenantIds.StoreAlpha, draft.StoryId, admin, submitted!.VersionToken, CancellationToken.None, "occ-aap-14");

        var cards = await directory.GetPublicStoriesAsync(
            StoryTenantIds.StoreAlpha, null, null, DateTimeOffset.UtcNow, CancellationToken.None);
        Assert.Single(cards);
        Assert.Equal(publicId, cards[0].ShopPublicId);
        Assert.Equal("Shop Name", cards[0].ShopDisplayName);
    }

    [Fact]
    public async Task Stop_return_edit_resubmit_cycle_with_versions()
    {
        await using var db = CreateDb("tb281r3-cycle-" + Guid.NewGuid().ToString("N"));
        var shop = Guid.NewGuid();
        var seller = Guid.NewGuid();
        var admin = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        var directory = new StoryDirectory(db, new StubParty(shop), new StubMedia(mediaId));

        var draft = await directory.SellerCreateDraftAsync(
            StoryTenantIds.StoreAlpha, shop, seller,
            new CreateStoryCommand("cycle", null, null, null, null, null, null, null, OccurrenceKey: "occ-create-6"),
            CancellationToken.None);
        var withItem = await directory.SellerAddItemAsync(
            StoryTenantIds.StoreAlpha, shop, draft.StoryId,
            new AddStoryItemCommand("image", mediaId, null, null, null, null, null, null, OccurrenceKey: "occ-add-11", ExpectedVersion: draft.VersionToken),
            CancellationToken.None);
        await directory.SellerSubmitAsync(
            StoryTenantIds.StoreAlpha, shop, draft.StoryId, seller, CancellationToken.None,
            withItem.VersionToken, "sub1");
        var submitted = await directory.SellerGetAsync(StoryTenantIds.StoreAlpha, shop, draft.StoryId, CancellationToken.None);
        await directory.AdminApproveAndPublishAsync(StoryTenantIds.StoreAlpha, draft.StoryId, admin, submitted!.VersionToken, CancellationToken.None, "occ-aap-15");

        var live = await directory.SellerGetAsync(StoryTenantIds.StoreAlpha, shop, draft.StoryId, CancellationToken.None);
        var stopped = await directory.SellerStopAsync(
            StoryTenantIds.StoreAlpha, shop, draft.StoryId, live!.VersionToken, CancellationToken.None, "stop1");
        var returned = await directory.SellerReturnToDraftAsync(
            StoryTenantIds.StoreAlpha, shop, draft.StoryId, stopped.VersionToken, CancellationToken.None, "ret1");
        Assert.Equal(StoryStatus.Draft, returned.Status);

        var edited = await directory.SellerUpdateAsync(
            StoryTenantIds.StoreAlpha, shop, draft.StoryId,
            new UpdateStoryCommand("cycle-2", null, null, null, null, null, null, returned.VersionToken, OccurrenceKey: "occ-upd-13"),
            CancellationToken.None);
        var resubmitted = await directory.SellerSubmitAsync(
            StoryTenantIds.StoreAlpha, shop, draft.StoryId, seller, CancellationToken.None,
            edited.VersionToken, "sub2");
        Assert.Equal(StoryReviewStatus.Submitted, resubmitted.ReviewStatus);
        Assert.Equal("cycle-2", resubmitted.Title);
    }

    private static StoryDbContext CreateDb(string name)
    {
        var options = new DbContextOptionsBuilder<StoryDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new StoryDbContext(options);
    }

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

    private sealed class StubMedia(Guid readyId) : IStoryMediaAssetPort
    {
        public Task<StoryMediaAssetAuthorization> AuthorizeForStoryAsync(
            Guid mediaAssetId, Guid tenantId, Guid? sellerPartyId, bool allowUnscopedAdminAssets,
            CancellationToken cancellationToken)
        {
            if (mediaAssetId != readyId && mediaAssetId != Guid.Empty)
                throw new ContractOperationException("media.owner.forbidden");
            var id = mediaAssetId == Guid.Empty ? readyId : mediaAssetId;
            return Task.FromResult(new StoryMediaAssetAuthorization(
                id, "image/jpeg", $"/v1/storefront/media/{id:D}", false, sellerPartyId));
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

        public Task<string?> TryResolvePublicProductPathAsync(Guid productId, CancellationToken cancellationToken) =>
            Task.FromResult<string?>(productId == Guid.Empty ? null : "/products/" + productId.ToString("N"));
    }

    private sealed class StubPublicSellers(Guid shop, string publicId, string name) : ICatalogPublicSellerAttributionLookup
    {
        public Task<IReadOnlyDictionary<Guid, CatalogPublicSellerAttribution>> ResolveAsync(
            IReadOnlyCollection<Guid> sellerPartyIds, CancellationToken cancellationToken)
        {
            IReadOnlyDictionary<Guid, CatalogPublicSellerAttribution> map = sellerPartyIds.Contains(shop)
                ? new Dictionary<Guid, CatalogPublicSellerAttribution>
                {
                    [shop] = new CatalogPublicSellerAttribution(shop, publicId, name),
                }
                : new Dictionary<Guid, CatalogPublicSellerAttribution>();
            return Task.FromResult(map);
        }
    }
}
