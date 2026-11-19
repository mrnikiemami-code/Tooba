using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Tooba.BuildingBlocks;
using Tooba.Media.Application.Ports;
using Tooba.Media.Contracts.Errors;
using Tooba.Media.Infrastructure.Assets;
using Tooba.Media.Infrastructure.Persistence;
using Tooba.Story.Domain.Tenant;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-281-R3 Media recording-store proofs for seller Story upload.</summary>
public sealed class MediaStorySellerUpload281R3Tests
{
    [Fact]
    public async Task Upload_success_persists_asset_binding_occurrence_and_one_object()
    {
        var dbName = "media-r3-ok-" + Guid.NewGuid().ToString("N");
        Guid assetId;
        var store = new RecordingStore();
        await using (var db = CreateDb(dbName))
        {
            var media = new MediaDirectory(db, store, Config());
            var first = await media.UploadForSellerAsync(
                new MemoryStream("hello-image"u8.ToArray()), "a.jpg", "image/jpeg",
                StoryTenantIds.StoreAlpha, Guid.NewGuid(), Guid.NewGuid(), "occ-1", CancellationToken.None);
            assetId = first.MediaAssetId;
            Assert.Equal(1, store.Saved.Count);
            Assert.Equal(1, await db.Assets.CountAsync());
            Assert.Equal(1, await db.AssetOwnerBindings.CountAsync());
            Assert.Equal(1, await db.AssetOwnerOccurrences.CountAsync());
        }

        await using var reopen = CreateDb(dbName);
        Assert.Equal(1, await reopen.Assets.AsNoTracking().CountAsync());
        Assert.Equal(assetId, (await reopen.Assets.AsNoTracking().SingleAsync()).MediaAssetId);
    }

    [Fact]
    public async Task Same_occurrence_replays_without_new_object_changed_bytes_conflict()
    {
        var dbName = "media-r3-replay-" + Guid.NewGuid().ToString("N");
        await using var db = CreateDb(dbName);
        var store = new RecordingStore();
        var media = new MediaDirectory(db, store, Config());
        var tenant = StoryTenantIds.StoreAlpha;
        var shop = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var bytes = "same-bytes"u8.ToArray();

        var first = await media.UploadForSellerAsync(
            new MemoryStream(bytes), "a.jpg", "image/jpeg", tenant, shop, actor, "occ-r", CancellationToken.None);
        var second = await media.UploadForSellerAsync(
            new MemoryStream(bytes), "a.jpg", "image/jpeg", tenant, shop, actor, "occ-r", CancellationToken.None);
        Assert.Equal(first.MediaAssetId, second.MediaAssetId);
        Assert.Equal(1, store.Saved.Count);
        Assert.Equal(1, await db.Assets.CountAsync());

        var conflict = await Assert.ThrowsAsync<ContractOperationException>(() =>
            media.UploadForSellerAsync(
                new MemoryStream("other-bytes"u8.ToArray()), "a.jpg", "image/jpeg",
                tenant, shop, actor, "occ-r", CancellationToken.None));
        Assert.Equal(MediaErrorCodes.OwnerForbidden, conflict.Code);
        Assert.Equal(1, store.Saved.Count);
        Assert.Equal(1, await db.Assets.CountAsync());
    }

    [Fact]
    public async Task Store_failure_leaves_no_asset_or_occurrence()
    {
        await using var db = CreateDb("media-r3-storefail-" + Guid.NewGuid().ToString("N"));
        var store = new RecordingStore { FailOnSave = true };
        var media = new MediaDirectory(db, store, Config());
        var ex = await Assert.ThrowsAsync<ContractOperationException>(() =>
            media.UploadForSellerAsync(
                new MemoryStream("x"u8.ToArray()), "a.jpg", "image/jpeg",
                StoryTenantIds.StoreAlpha, Guid.NewGuid(), Guid.NewGuid(), "occ-f", CancellationToken.None));
        Assert.Equal(MediaErrorCodes.StorageUnavailable, ex.Code);
        Assert.Equal(0, await db.Assets.CountAsync());
        Assert.Equal(0, await db.AssetOwnerOccurrences.CountAsync());
        Assert.Empty(store.Saved);
    }

    [Fact]
    public async Task Same_occurrence_replay_returns_winner_without_new_object()
    {
        // R3 name previously claimed a SaveChanges failure; that proof lives in MediaStorySellerUpload281R4Tests.
        await using var db = CreateDb("media-r3-replay-winner-" + Guid.NewGuid().ToString("N"));
        var store = new RecordingStore();
        var media = new MediaDirectory(db, store, Config());
        var tenant = StoryTenantIds.StoreAlpha;
        var shop = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var key = "occ-dup";

        var first = await media.UploadForSellerAsync(
            new MemoryStream("payload"u8.ToArray()), "a.jpg", "image/jpeg",
            tenant, shop, actor, key, CancellationToken.None);
        Assert.Equal(1, store.Saved.Count);

        var replay = await media.UploadForSellerAsync(
            new MemoryStream("payload"u8.ToArray()), "a.jpg", "image/jpeg",
            tenant, shop, actor, key, CancellationToken.None);
        Assert.Equal(first.MediaAssetId, replay.MediaAssetId);
        Assert.Equal(1, store.Saved.Count);
        Assert.Equal(0, store.Deleted.Count);
    }

    [Fact]
    public async Task Foreign_shop_binding_refuses_authorization()
    {
        await using var db = CreateDb("media-r3-foreign-" + Guid.NewGuid().ToString("N"));
        var store = new RecordingStore();
        var media = new MediaDirectory(db, store, Config());
        var tenant = StoryTenantIds.StoreAlpha;
        var shopA = Guid.NewGuid();
        var shopB = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var uploaded = await media.UploadForSellerAsync(
            new MemoryStream("payload"u8.ToArray()), "a.jpg", "image/jpeg",
            tenant, shopA, actor, "occ-a", CancellationToken.None);

        var ex = await Assert.ThrowsAsync<ContractOperationException>(() =>
            media.AuthorizeForStoryAsync(
                uploaded.MediaAssetId, tenant, shopB, allowUnscopedAdminAssets: false, CancellationToken.None));
        Assert.Equal(MediaErrorCodes.OwnerForbidden, ex.Code);
    }

    private static MediaDbContext CreateDb(string name)
    {
        var options = new DbContextOptionsBuilder<MediaDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new MediaDbContext(options);
    }

    private static IConfiguration Config() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Tooba:Media:MaxUploadBytes"] = "1048576",
        }).Build();

    private sealed class RecordingStore : IMediaObjectStore
    {
        public List<string> Saved { get; } = [];
        public List<string> Deleted { get; } = [];
        public bool FailOnSave { get; set; }

        public Task SaveAsync(Stream stream, string key, string contentType, CancellationToken cancellationToken)
        {
            if (FailOnSave)
                throw new InvalidOperationException("store down");
            Saved.Add(key);
            return Task.CompletedTask;
        }

        public Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken) =>
            Task.FromResult<Stream?>(null);

        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken) =>
            Task.FromResult(Saved.Contains(key) && !Deleted.Contains(key));

        public Task DeleteAsync(string key, CancellationToken cancellationToken)
        {
            Deleted.Add(key);
            return Task.CompletedTask;
        }
    }
}
