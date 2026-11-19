using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Tooba.BuildingBlocks;
using Tooba.Media.Application.Ports;
using Tooba.Media.Contracts.Errors;
using Tooba.Media.Domain.Aggregates;
using Tooba.Media.Infrastructure.Assets;
using Tooba.Media.Infrastructure.Persistence;
using Tooba.Persistence;
using Tooba.Story.Domain.Tenant;
using Xunit;
using Xunit.Abstractions;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-281-R5 C3: Media focused counterexamples — real caller-owned tracked entity across pre-save
/// failure, fresh-context after-save durability, and independent committedRows=0 on PostgreSQL.
/// </summary>
public sealed class MediaStorySellerUpload281R5Tests
{
    private readonly ITestOutputHelper _output;
    public MediaStorySellerUpload281R5Tests(ITestOutputHelper output) => _output = output;

    [Fact]
    public async Task Pre_save_failure_preserves_caller_owned_tracked_entity_state()
    {
        var dbName = "media-r5-presave-" + Guid.NewGuid().ToString("N");
        var store = new RecordingStore();
        var now = DateTimeOffset.UtcNow;
        var callerAsset = MediaAsset.CreateReady(
            "caller/pre-existing.bin",
            "caller.bin",
            "image/jpeg",
            12,
            "aabbccddeeff00112233445566778899aabbccddeeff00112233445566778899",
            width: null,
            height: null,
            Guid.NewGuid(),
            now);

        await using (var seed = new MediaDbContext(
            new DbContextOptionsBuilder<MediaDbContext>().UseInMemoryDatabase(dbName).Options))
        {
            seed.Assets.Add(callerAsset);
            await seed.SaveChangesAsync();
        }

        var options = new DbContextOptionsBuilder<MediaDbContext>()
            .UseInMemoryDatabase(dbName)
            .AddInterceptors(new FailOnSavingChangesInterceptor())
            .Options;
        await using var db = new MediaDbContext(options);
        // Attach the real caller-owned entity as Unchanged; upload must not mutate/detach it.
        db.Attach(callerAsset);
        Assert.Equal(EntityState.Unchanged, db.Entry(callerAsset).State);
        var beforeId = callerAsset.MediaAssetId;
        var beforeKey = callerAsset.StorageKey;
        var beforeName = callerAsset.OriginalFileName;

        var media = new MediaDirectory(db, store, Config());
        var ex = await Assert.ThrowsAsync<ContractOperationException>(() =>
            media.UploadForSellerAsync(
                new MemoryStream("payload"u8.ToArray()), "a.jpg", "image/jpeg",
                StoryTenantIds.StoreAlpha, Guid.NewGuid(), Guid.NewGuid(), "occ-presave", CancellationToken.None));
        Assert.Equal(MediaErrorCodes.UploadFailed, ex.Code);

        Assert.Equal(EntityState.Unchanged, db.Entry(callerAsset).State);
        Assert.Equal(beforeId, callerAsset.MediaAssetId);
        Assert.Equal(beforeKey, callerAsset.StorageKey);
        Assert.Equal(beforeName, callerAsset.OriginalFileName);
        Assert.Equal(1, store.Saved.Count);
        Assert.Equal(1, store.Deleted.Count);
        Assert.Equal(0, await db.AssetOwnerOccurrences.CountAsync());
    }

    [Fact]
    public async Task After_save_throw_fresh_context_sees_durable_asset_binding_occurrence_and_replay()
    {
        var dbName = "media-r5-aftersave-" + Guid.NewGuid().ToString("N");
        var store = new RecordingStore();
        var options = new DbContextOptionsBuilder<MediaDbContext>()
            .UseInMemoryDatabase(dbName)
            .AddInterceptors(new ThrowAfterSaveInterceptor())
            .Options;
        await using var db = new MediaDbContext(options);
        var media = new MediaDirectory(db, store, Config());
        var tenant = StoryTenantIds.StoreAlpha;
        var shop = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var key = "occ-after-save-r5";
        var bytes = "durable-payload-r5"u8.ToArray();

        var first = await media.UploadForSellerAsync(
            new MemoryStream(bytes), "a.jpg", "image/jpeg",
            tenant, shop, actor, key, CancellationToken.None);
        Assert.Empty(store.Deleted);

        await using var fresh = new MediaDbContext(
            new DbContextOptionsBuilder<MediaDbContext>().UseInMemoryDatabase(dbName).Options);
        Assert.Equal(1, await fresh.Assets.AsNoTracking().CountAsync(a => a.MediaAssetId == first.MediaAssetId));
        Assert.Equal(1, await fresh.AssetOwnerBindings.AsNoTracking()
            .CountAsync(b => b.MediaAssetId == first.MediaAssetId && b.SellerPartyId == shop));
        Assert.Equal(1, await fresh.AssetOwnerOccurrences.AsNoTracking()
            .CountAsync(o => o.OccurrenceKey == key && o.MediaAssetId == first.MediaAssetId));
        Assert.True(await store.ExistsAsync(
            (await fresh.Assets.AsNoTracking().SingleAsync(a => a.MediaAssetId == first.MediaAssetId)).StorageKey,
            CancellationToken.None));

        var replay = await media.UploadForSellerAsync(
            new MemoryStream(bytes), "a.jpg", "image/jpeg",
            tenant, shop, actor, key, CancellationToken.None);
        Assert.Equal(first.MediaAssetId, replay.MediaAssetId);
        Assert.Equal(1, store.Saved.Count);
    }

    [SkippableFact]
    public async Task Relational_rollback_only_independent_committed_rows_zero_for_test_owned_ids()
    {
        var raw = Environment.GetEnvironmentVariable("TB281R5_CONNECTION")
            ?? Environment.GetEnvironmentVariable("TB281R4_CONNECTION")
            ?? Environment.GetEnvironmentVariable("TB272_CONNECTION")
            ?? "Host=127.0.0.1;Port=5432;Username=admin;Password=123456;Database=tooba_alpha";
        Skip.If(!CanConnectCanonical(raw), "canonical tooba_alpha unavailable");

        var runTag = $"tb281r5-media-{Guid.NewGuid():N}-";
        var options = new DbContextOptionsBuilder<MediaDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, raw, MediaDbContext.Schema, typeof(MediaDbContext));
        await using var db = new MediaDbContext(options.Options);
        await using var caller = await db.Database.BeginTransactionAsync();

        var store = new RecordingStore();
        var media = new MediaDirectory(db, store, Config());
        // Ambient EF tx must refuse before store — prove zero durable rows for the refused key.
        var refused = await Assert.ThrowsAsync<ContractOperationException>(() =>
            media.UploadForSellerAsync(
                new MemoryStream("rel"u8.ToArray()), "r.jpg", "image/jpeg",
                StoryTenantIds.StoreAlpha, Guid.NewGuid(), Guid.NewGuid(), runTag + "tx", CancellationToken.None));
        Assert.Equal(MediaErrorCodes.ValidationFailed, refused.Code);
        Assert.Empty(store.Saved);

        await caller.RollbackAsync();

        var verifyOptions = new DbContextOptionsBuilder<MediaDbContext>();
        ToobaNpgsql.ConfigureModuleContext(verifyOptions, raw, MediaDbContext.Schema, typeof(MediaDbContext));
        await using var verify = new MediaDbContext(verifyOptions.Options);
        var occ = await verify.AssetOwnerOccurrences.AsNoTracking()
            .LongCountAsync(x => x.OccurrenceKey.StartsWith(runTag));
        Assert.Equal(0L, occ);
        _output.WriteLine($"C3 media independent committedRows=0 tag={runTag} occ={occ}");
    }

    private static bool CanConnectCanonical(string raw)
    {
        try
        {
            var builder = new Npgsql.NpgsqlConnectionStringBuilder(raw);
            if (!string.Equals(builder.Database, "tooba_alpha", StringComparison.Ordinal)
                || builder.Port != 5432)
                return false;
            using var c = new Npgsql.NpgsqlConnection(raw);
            c.Open();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static IConfiguration Config() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Tooba:Media:MaxUploadBytes"] = "1048576",
        }).Build();

    private sealed class FailOnSavingChangesInterceptor : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData, InterceptionResult<int> result) =>
            throw new InvalidOperationException("forced save failure");

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("forced save failure");
    }

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
