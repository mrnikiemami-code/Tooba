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

/// <summary>
/// TB-282-R20 D16 — the public reader must publish the owning publication-window end so a customer
/// page can revalidate a bounded expiry instead of playing stopped/expired content indefinitely.
/// </summary>
public sealed class Story282R20PublicWindowTests
{
    [Fact]
    public async Task Public_card_carries_the_owning_window_end_and_stops_at_expiry()
    {
        var options = new DbContextOptionsBuilder<StoryDbContext>()
            .UseInMemoryDatabase("r20-public-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new StoryDbContext(options);
        var media = Guid.NewGuid();
        var dir = new StoryDirectory(db, new StubParty(), new StubMedia(media), new PlatformProducts(Guid.NewGuid()));

        var now = DateTimeOffset.Parse("2026-11-21T08:00:00Z");
        var end = now.AddHours(2);
        var shop = Guid.NewGuid();
        var seller = Guid.NewGuid();

        var created = await dir.SellerCreateDraftAsync(
            StoryTenantIds.StoreAlpha,
            shop,
            seller,
            new CreateStoryCommand("استوری فروشگاه", null, null, media, null, null, null, null, OccurrenceKey: "r20-pub-0"),
            CancellationToken.None);
        var withItem = await dir.SellerAddItemAsync(
            StoryTenantIds.StoreAlpha,
            shop,
            created.StoryId,
            new AddStoryItemCommand("image", media, "/m/r20.jpg", "کپشن", 7000, "none", null, 0, "r20-pub-ai", created.VersionToken),
            CancellationToken.None);
        var submitted = await dir.SellerSubmitAsync(
            StoryTenantIds.StoreAlpha,
            shop,
            created.StoryId,
            seller,
            CancellationToken.None,
            withItem.VersionToken,
            "r20-pub-submit");
        var published = await dir.AdminApproveAndPublishAsync(
            StoryTenantIds.StoreAlpha,
            created.StoryId,
            Guid.NewGuid(),
            submitted.VersionToken,
            CancellationToken.None,
            "r20-pub-ap");
        Assert.True(published.EffectivelyPublic);

        await dir.AdminSetScheduleAsync(
            StoryTenantIds.StoreAlpha,
            created.StoryId,
            new SetStoryScheduleCommand(now, end, ExpectedVersion: published.VersionToken, OccurrenceKey: "r20-pub-sched"),
            CancellationToken.None);

        var live = await dir.GetPublicStoriesAsync(StoryTenantIds.StoreAlpha, null, null, now.AddMinutes(5), CancellationToken.None);
        var card = Assert.Single(live, row => row.StoryId == created.StoryId);
        Assert.NotNull(card.EndAt);
        Assert.Equal(end, card.EndAt!.Value.ToUniversalTime());

        // After the window end the same owning reader no longer publishes it at all.
        var expired = await dir.GetPublicStoriesAsync(StoryTenantIds.StoreAlpha, null, null, end.AddMinutes(1), CancellationToken.None);
        Assert.DoesNotContain(expired, row => row.StoryId == created.StoryId);

        // A stop makes it unplayable immediately, independent of the window.
        var current = await dir.AdminGetAsync(StoryTenantIds.StoreAlpha, created.StoryId, CancellationToken.None);
        var stopped = await dir.AdminSoftDisableAsync(
            StoryTenantIds.StoreAlpha,
            created.StoryId,
            current?.VersionToken,
            CancellationToken.None,
            "r20-pub-stop");
        Assert.False(stopped.EffectivelyPublic);
        var afterStop = await dir.GetPublicStoriesAsync(StoryTenantIds.StoreAlpha, null, null, now.AddMinutes(10), CancellationToken.None);
        Assert.DoesNotContain(afterStop, row => row.StoryId == created.StoryId);
    }

    [Fact]
    public async Task Public_reader_resolves_every_page_product_cta_in_one_batch_lookup()
    {
        var options = new DbContextOptionsBuilder<StoryDbContext>()
            .UseInMemoryDatabase("r20-batch-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new StoryDbContext(options);
        var media = Guid.NewGuid();
        var shop = Guid.NewGuid();
        var seller = Guid.NewGuid();
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var p3 = Guid.NewGuid();
        var products = new BatchProducts(p1, p2, p3);
        var dir = new StoryDirectory(db, new StubParty(), new StubMedia(media), products);

        var created = await dir.SellerCreateDraftAsync(
            StoryTenantIds.StoreAlpha,
            shop,
            seller,
            new CreateStoryCommand("استوری ویترین", null, null, media, null, null, null, null, OccurrenceKey: "r20-batch-0"),
            CancellationToken.None);
        var version = created.VersionToken;
        var order = 0;
        foreach (var product in new[] { p1, p2, p3 })
        {
            var added = await dir.SellerAddItemAsync(
                StoryTenantIds.StoreAlpha,
                shop,
                created.StoryId,
                new AddStoryItemCommand("image", media, "/m/r20b.jpg", "کپشن", 5000, "product", product.ToString(), order++, "r20-batch-ai-" + order, version),
                CancellationToken.None);
            version = added.VersionToken;
        }
        var submitted = await dir.SellerSubmitAsync(
            StoryTenantIds.StoreAlpha,
            shop,
            created.StoryId,
            seller,
            CancellationToken.None,
            version,
            "r20-batch-submit");
        var published = await dir.AdminApproveAndPublishAsync(
            StoryTenantIds.StoreAlpha,
            created.StoryId,
            Guid.NewGuid(),
            submitted.VersionToken,
            CancellationToken.None,
            "r20-batch-ap");
        Assert.True(published.EffectivelyPublic);

        var live = await dir.GetPublicStoriesAsync(
            StoryTenantIds.StoreAlpha, null, null, DateTimeOffset.UtcNow, CancellationToken.None);
        var card = Assert.Single(live, row => row.StoryId == created.StoryId);
        Assert.Equal(3, card.Items.Count);
        Assert.All(card.Items, item => Assert.StartsWith("/products/", item.CtaHref));
        Assert.Equal(1, products.BatchCalls);
        Assert.Equal(0, products.SingleCalls);
    }

    private sealed class BatchProducts(params Guid[] published) : IStoryProductTargetPort, IStoryProductPathBatchPort
    {
        private readonly HashSet<Guid> _published = published.ToHashSet();
        public int BatchCalls { get; private set; }
        public int SingleCalls { get; private set; }

        public Task EnsureOwnedBySellerAsync(Guid productId, Guid sellerPartyId, CancellationToken cancellationToken)
        {
            if (!_published.Contains(productId))
                throw new SemanticException(new SemanticError(StoryErrorCodes.CtaRejected));
            return Task.CompletedTask;
        }

        public Task EnsurePublishedExistsAsync(Guid productId, CancellationToken cancellationToken)
        {
            if (!_published.Contains(productId))
                throw new SemanticException(new SemanticError(StoryErrorCodes.CtaRejected));
            return Task.CompletedTask;
        }

        public Task<string?> TryResolvePublicProductPathAsync(Guid productId, CancellationToken cancellationToken)
        {
            SingleCalls++;
            return Task.FromResult<string?>(_published.Contains(productId) ? "/products/" + productId.ToString("N") : null);
        }

        public Task<IReadOnlyDictionary<Guid, string>> ResolvePublicProductPathsAsync(
            IReadOnlyCollection<Guid> productIds,
            CancellationToken cancellationToken)
        {
            BatchCalls++;
            return Task.FromResult<IReadOnlyDictionary<Guid, string>>(
                productIds.Where(_published.Contains)
                    .ToDictionary(id => id, id => "/products/" + id.ToString("N")));
        }
    }

    private sealed class StubParty : IPartyAdminSellerReadGateway
    {
        public Task<IReadOnlyList<PartyStatusProjection>> GetStatusProjectionsAsync(
            IReadOnlyList<Guid> partyIds, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<PartyStatusProjection>>([]);
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
            Stream stream, string fileName, string contentType, Guid tenantId, Guid actorUserId,
            string? occurrenceKey, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<StoryMediaAssetAuthorization>> ListSellerAssetsAsync(
            Guid tenantId, Guid sellerPartyId, int take, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<StoryMediaAssetAuthorization>>([]);
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
