using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Tooba.BuildingBlocks;
using Tooba.Media.Application.Ports;
using Tooba.Media.Contracts.Errors;
using Tooba.Media.Infrastructure.Assets;
using Tooba.Media.Infrastructure.Persistence;
using Tooba.Story.Domain.Tenant;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-282-R1 F4: platform Story upload — keyed replay, changed-file conflict, after-save durability,
/// ambient-tx refuse, distinct attempt storage keys.
/// </summary>
public sealed class MediaStoryPlatformUpload282R1Tests
{
    [Fact]
    public async Task Exact_replay_returns_same_platform_asset_without_second_store_save()
    {
        var dbName = "media-plat-replay-" + Guid.NewGuid().ToString("N");
        var store = new RecordingStore();
        await using var db = new MediaDbContext(
            new DbContextOptionsBuilder<MediaDbContext>().UseInMemoryDatabase(dbName).Options);
        var media = new MediaDirectory(db, store, Config());
        var tenant = StoryTenantIds.StoreAlpha;
        var actor = Guid.NewGuid();
        var key = "plat-occ-replay";
        var bytes = "platform-payload-282r1"u8.ToArray();

        var first = await media.UploadForPlatformStoryAsync(
            new MemoryStream(bytes), "p.jpg", "image/jpeg", tenant, actor, key, CancellationToken.None);
        var replay = await media.UploadForPlatformStoryAsync(
            new MemoryStream(bytes), "p.jpg", "image/jpeg", tenant, actor, key, CancellationToken.None);

        Assert.Equal(first.MediaAssetId, replay.MediaAssetId);
        Assert.Equal(1, store.Saved.Count);
        Assert.Equal(0, await db.AssetOwnerBindings.AsNoTracking()
            .CountAsync(b => b.MediaAssetId == first.MediaAssetId));
        Assert.Equal(Guid.Empty, await db.AssetOwnerOccurrences.AsNoTracking()
            .Where(o => o.MediaAssetId == first.MediaAssetId)
            .Select(o => o.SellerPartyId)
            .SingleAsync());
    }

    [Fact]
    public async Task Changed_bytes_same_key_conflicts_and_keeps_winner()
    {
        var dbName = "media-plat-conflict-" + Guid.NewGuid().ToString("N");
        var store = new RecordingStore();
        await using var db = new MediaDbContext(
            new DbContextOptionsBuilder<MediaDbContext>().UseInMemoryDatabase(dbName).Options);
        var media = new MediaDirectory(db, store, Config());
        var tenant = StoryTenantIds.StoreAlpha;
        var actor = Guid.NewGuid();
        var key = "plat-occ-conflict";

        var first = await media.UploadForPlatformStoryAsync(
            new MemoryStream("aaa"u8.ToArray()), "a.jpg", "image/jpeg", tenant, actor, key, CancellationToken.None);
        var ex = await Assert.ThrowsAsync<ContractOperationException>(() =>
            media.UploadForPlatformStoryAsync(
                new MemoryStream("bbb"u8.ToArray()), "a.jpg", "image/jpeg", tenant, actor, key, CancellationToken.None));
        Assert.Equal(MediaErrorCodes.OwnerForbidden, ex.Code);
        Assert.Equal(1, await db.Assets.AsNoTracking().CountAsync(a => a.MediaAssetId == first.MediaAssetId));
    }

    [Fact]
    public async Task After_save_throw_preserves_winner_on_fresh_context_replay()
    {
        var dbName = "media-plat-aftersave-" + Guid.NewGuid().ToString("N");
        var store = new RecordingStore();
        var options = new DbContextOptionsBuilder<MediaDbContext>()
            .UseInMemoryDatabase(dbName)
            .AddInterceptors(new ThrowAfterSaveInterceptor())
            .Options;
        await using var db = new MediaDbContext(options);
        var media = new MediaDirectory(db, store, Config());
        var tenant = StoryTenantIds.StoreAlpha;
        var actor = Guid.NewGuid();
        var key = "plat-occ-after";
        var bytes = "plat-durable"u8.ToArray();

        var first = await media.UploadForPlatformStoryAsync(
            new MemoryStream(bytes), "a.jpg", "image/jpeg", tenant, actor, key, CancellationToken.None);

        await using var fresh = new MediaDbContext(
            new DbContextOptionsBuilder<MediaDbContext>().UseInMemoryDatabase(dbName).Options);
        Assert.Equal(1, await fresh.Assets.AsNoTracking().CountAsync(a => a.MediaAssetId == first.MediaAssetId));

        var replay = await media.UploadForPlatformStoryAsync(
            new MemoryStream(bytes), "a.jpg", "image/jpeg", tenant, actor, key, CancellationToken.None);
        Assert.Equal(first.MediaAssetId, replay.MediaAssetId);
        Assert.Equal(1, store.Saved.Count);
    }

    [Fact]
    public async Task Ambient_system_transaction_refuses_before_object_store()
    {
        var dbName = "media-plat-tx-" + Guid.NewGuid().ToString("N");
        var store = new RecordingStore();
        await using var db = new MediaDbContext(
            new DbContextOptionsBuilder<MediaDbContext>().UseInMemoryDatabase(dbName).Options);
        var media = new MediaDirectory(db, store, Config());
        using var scope = new System.Transactions.TransactionScope(
            System.Transactions.TransactionScopeOption.RequiresNew,
            System.Transactions.TransactionScopeAsyncFlowOption.Enabled);
        var ex = await Assert.ThrowsAsync<ContractOperationException>(() =>
            media.UploadForPlatformStoryAsync(
                new MemoryStream("x"u8.ToArray()), "x.jpg", "image/jpeg",
                StoryTenantIds.StoreAlpha, Guid.NewGuid(), "plat-tx", CancellationToken.None));
        Assert.Equal(MediaErrorCodes.ValidationFailed, ex.Code);
        Assert.Empty(store.Saved);
        scope.Dispose();
    }

    [Fact]
    public async Task Concurrent_attempt_storage_keys_are_distinct_while_asset_id_is_deterministic()
    {
        var dbName = "media-plat-race-" + Guid.NewGuid().ToString("N");
        var store = new RecordingStore();
        await using var db = new MediaDbContext(
            new DbContextOptionsBuilder<MediaDbContext>().UseInMemoryDatabase(dbName).Options);
        var media = new MediaDirectory(db, store, Config());
        var tenant = StoryTenantIds.StoreAlpha;
        var actor = Guid.NewGuid();
        var key = "plat-occ-race";
        var bytes = "race-payload"u8.ToArray();

        var first = await media.UploadForPlatformStoryAsync(
            new MemoryStream(bytes), "r.jpg", "image/jpeg", tenant, actor, key, CancellationToken.None);
        // Second attempt with same intent after durable winner → no new save.
        var second = await media.UploadForPlatformStoryAsync(
            new MemoryStream(bytes), "r.jpg", "image/jpeg", tenant, actor, key, CancellationToken.None);
        Assert.Equal(first.MediaAssetId, second.MediaAssetId);
        Assert.Equal(1, store.Saved.Count);
        Assert.Contains("story-platform/", store.Saved[0], StringComparison.Ordinal);
    }

    private static IConfiguration Config() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Tooba:Media:MaxUploadBytes"] = "1048576",
        }).Build();

    private sealed class ThrowAfterSaveInterceptor : SaveChangesInterceptor
    {
        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result) =>
            throw new InvalidOperationException("lost ack after commit");

        public override ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("lost ack after commit");
    }

    private sealed class RecordingStore : IMediaObjectStore
    {
        public List<string> Saved { get; } = [];
        public List<string> Deleted { get; } = [];

        public Task SaveAsync(Stream stream, string key, string contentType, CancellationToken cancellationToken)
        {
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
