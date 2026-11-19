using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
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

/// <summary>TB-281-R2 focused Directory proofs for remaining first-delivery closure.</summary>
public sealed class StoryMarketplace281R2DirectoryTests
{
    [Fact]
    public async Task Stop_then_read_offers_return_to_draft_and_return_replay_is_stable()
    {
        await using var db = CreateDb("tb281r2-stop-return-" + Guid.NewGuid().ToString("N"));
        var shop = Guid.NewGuid();
        var seller = Guid.NewGuid();
        var admin = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        var directory = new StoryDirectory(db, new StubParty(shop), new StubMedia(mediaId));

        var draft = await directory.SellerCreateDraftAsync(
            StoryTenantIds.StoreAlpha, shop, seller,
            new CreateStoryCommand("cycle", null, null, null, null, null, null, null, OccurrenceKey: "create-1"),
            CancellationToken.None);
        await directory.SellerAddItemAsync(
            StoryTenantIds.StoreAlpha, shop, draft.StoryId,
            new AddStoryItemCommand("image", mediaId, null, null, null, null, null, null, OccurrenceKey: "occ-add-22", ExpectedVersion: draft.VersionToken),
            CancellationToken.None);
        var afterAdd = await directory.SellerGetAsync(StoryTenantIds.StoreAlpha, shop, draft.StoryId, CancellationToken.None);
        await directory.SellerSubmitAsync(StoryTenantIds.StoreAlpha, shop, draft.StoryId, seller, CancellationToken.None, afterAdd!.VersionToken, "submit-" + draft.StoryId.ToString("N"));
        var submitted = await directory.SellerGetAsync(StoryTenantIds.StoreAlpha, shop, draft.StoryId, CancellationToken.None);
        await directory.AdminApproveAndPublishAsync(StoryTenantIds.StoreAlpha, draft.StoryId, admin, submitted!.VersionToken, CancellationToken.None, "occ-aap-23");

        var live = await directory.SellerGetAsync(StoryTenantIds.StoreAlpha, shop, draft.StoryId, CancellationToken.None);
        Assert.Contains("stop", live!.ActionCodes!);
        Assert.Contains("return_to_draft", live.ActionCodes!);

        var stopped = await directory.SellerStopAsync(
            StoryTenantIds.StoreAlpha, shop, draft.StoryId, live.VersionToken, CancellationToken.None, "stop-1");
        Assert.Equal(StoryStatus.Disabled, stopped.Status);
        Assert.Equal(StoryReviewStatus.Approved, stopped.ReviewStatus);
        Assert.Contains("return_to_draft", stopped.ActionCodes!);

        var replayStop = await directory.SellerStopAsync(
            StoryTenantIds.StoreAlpha, shop, draft.StoryId, live.VersionToken, CancellationToken.None, "stop-1");
        Assert.Equal(stopped.StoryId, replayStop.StoryId);
        Assert.Equal(StoryStatus.Disabled, replayStop.Status);

        var afterStop = await directory.SellerGetAsync(StoryTenantIds.StoreAlpha, shop, draft.StoryId, CancellationToken.None);
        Assert.Contains("return_to_draft", afterStop!.ActionCodes!);

        var returned = await directory.SellerReturnToDraftAsync(
            StoryTenantIds.StoreAlpha, shop, draft.StoryId, afterStop.VersionToken, CancellationToken.None, "return-1");
        Assert.Equal(StoryStatus.Draft, returned.Status);
        Assert.Equal(StoryReviewStatus.None, returned.ReviewStatus);
        Assert.Contains("update", returned.ActionCodes!);
    }

    [Fact]
    public async Task Seller_query_pages_and_counts_owner_shop_only()
    {
        await using var db = CreateDb("tb281r2-query-" + Guid.NewGuid().ToString("N"));
        var shopA = Guid.NewGuid();
        var shopB = Guid.NewGuid();
        var seller = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        var directory = new StoryDirectory(db, new StubParty(shopA), new StubMedia(mediaId));

        await directory.SellerCreateDraftAsync(
            StoryTenantIds.StoreAlpha, shopA, seller,
            new CreateStoryCommand("alpha-one", null, null, null, null, null, null, null, OccurrenceKey: "occ-create-17"),
            CancellationToken.None);
        await directory.SellerCreateDraftAsync(
            StoryTenantIds.StoreAlpha, shopA, seller,
            new CreateStoryCommand("alpha-two", null, null, null, null, null, null, null, OccurrenceKey: "occ-create-18"),
            CancellationToken.None);
        await directory.SellerCreateDraftAsync(
            StoryTenantIds.StoreAlpha, shopB, seller,
            new CreateStoryCommand("beta-one", null, null, null, null, null, null, null, OccurrenceKey: "occ-create-19"),
            CancellationToken.None);

        var page = await directory.SellerQueryAsync(
            StoryTenantIds.StoreAlpha,
            shopA,
            new GridQueryRequest(1, 10, "alpha", [], [], null),
            CancellationToken.None);
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(2, page.Items.Count);
        Assert.All(page.Items, row => Assert.Equal(shopA, row.SellerPartyId));
    }

    [Fact]
    public async Task Foreign_product_cta_refuses_before_create()
    {
        await using var db = CreateDb("tb281r2-cta-" + Guid.NewGuid().ToString("N"));
        var shop = Guid.NewGuid();
        var seller = Guid.NewGuid();
        var owned = Guid.NewGuid();
        var foreign = Guid.NewGuid();
        var directory = new StoryDirectory(
            db, new StubParty(shop), new StubMedia(Guid.NewGuid()), new StubProducts(shop, owned));

        var ex = await Assert.ThrowsAsync<SemanticException>(() =>
            directory.SellerCreateDraftAsync(
                StoryTenantIds.StoreAlpha, shop, seller,
                new CreateStoryCommand(
                    "cta", null, null, null, null, null, "product", foreign.ToString("D"), OccurrenceKey: "occ-create-20"),
                CancellationToken.None));
        Assert.Equal(StoryErrorCodes.CtaRejected, ex.Error.Code);
        Assert.Equal(0, await db.Stories.CountAsync());

        var ok = await directory.SellerCreateDraftAsync(
            StoryTenantIds.StoreAlpha, shop, seller,
            new CreateStoryCommand(
                "cta-ok", null, null, null, null, null, "product", owned.ToString("D"), OccurrenceKey: "occ-create-21"),
            CancellationToken.None);
        Assert.Equal(owned.ToString("D"), ok.CtaTarget);
    }

    [Fact]
    public async Task Seller_create_occurrence_replays_without_duplicate()
    {
        await using var db = CreateDb("tb281r2-occ-" + Guid.NewGuid().ToString("N"));
        var shop = Guid.NewGuid();
        var seller = Guid.NewGuid();
        var directory = new StoryDirectory(db, new StubParty(shop), new StubMedia(Guid.NewGuid()));
        var cmd = new CreateStoryCommand(
            "once", null, null, null, null, null, null, null, OccurrenceKey: "seller-create-occ");
        var first = await directory.SellerCreateDraftAsync(
            StoryTenantIds.StoreAlpha, shop, seller, cmd, CancellationToken.None);
        var second = await directory.SellerCreateDraftAsync(
            StoryTenantIds.StoreAlpha, shop, seller, cmd, CancellationToken.None);
        Assert.Equal(first.StoryId, second.StoryId);
        Assert.Equal(1, await db.Stories.CountAsync());
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
            CancellationToken cancellationToken) =>
            Task.FromResult(new StoryMediaAssetAuthorization(
                mediaAssetId == Guid.Empty ? readyId : mediaAssetId,
                "image/jpeg",
                $"/v1/storefront/media/{(mediaAssetId == Guid.Empty ? readyId : mediaAssetId):D}",
                false,
                sellerPartyId));

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
}
