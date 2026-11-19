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

/// <summary>TB-281-R4 B: real SaveChanges failure injection and exact unique identity proofs.</summary>
public sealed class MediaStorySellerUpload281R4Tests
{
    [Fact]
    public async Task Db_fail_before_commit_cleans_attempt_object_and_leaves_caller_entities()
    {
        var dbName = "media-r4-dbfail-" + Guid.NewGuid().ToString("N");
        var store = new RecordingStore();
        var options = new DbContextOptionsBuilder<MediaDbContext>()
            .UseInMemoryDatabase(dbName)
            .AddInterceptors(new FailOnSavingChangesInterceptor())
            .Options;
        await using var db = new MediaDbContext(options);
        var media = new MediaDirectory(db, store, Config());
        var tenant = StoryTenantIds.StoreAlpha;
        var shop = Guid.NewGuid();
        var actor = Guid.NewGuid();

        var ex = await Assert.ThrowsAsync<ContractOperationException>(() =>
            media.UploadForSellerAsync(
                new MemoryStream("payload"u8.ToArray()), "a.jpg", "image/jpeg",
                tenant, shop, actor, "occ-dbfail", CancellationToken.None));
        Assert.Equal(MediaErrorCodes.UploadFailed, ex.Code);
        Assert.Equal(0, await db.Assets.CountAsync());
        Assert.Equal(0, await db.AssetOwnerOccurrences.CountAsync());
        Assert.Equal(1, store.Saved.Count);
        Assert.Equal(1, store.Deleted.Count);
        Assert.Equal(store.Saved[0], store.Deleted[0]);
    }

    [Fact]
    public async Task After_save_throw_keeps_durable_rows_and_replay_returns_same_asset()
    {
        var dbName = "media-r4-aftersave-" + Guid.NewGuid().ToString("N");
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
        var key = "occ-after-save";
        var bytes = "durable-payload"u8.ToArray();

        // First call: SaveChanges commits, then SavedChanges throws; owning code must not delete the object.
        var first = await media.UploadForSellerAsync(
            new MemoryStream(bytes), "a.jpg", "image/jpeg",
            tenant, shop, actor, key, CancellationToken.None);
        Assert.Equal(1, await db.Assets.CountAsync());
        Assert.Equal(1, await db.AssetOwnerOccurrences.CountAsync());
        Assert.Equal(1, store.Saved.Count);
        Assert.Empty(store.Deleted);

        var replay = await media.UploadForSellerAsync(
            new MemoryStream(bytes), "a.jpg", "image/jpeg",
            tenant, shop, actor, key, CancellationToken.None);
        Assert.Equal(first.MediaAssetId, replay.MediaAssetId);
        Assert.Equal(1, store.Saved.Count);
        Assert.Empty(store.Deleted);
    }

    // Ambient-transaction refusal is proven on real PostgreSQL in StoryMarketplace281R4RelationalTests.C3
    // because the InMemory provider does not support transactions.

    [Fact]
    public void Occurrence_unique_identity_requires_exact_schema_table_constraint()
    {
        // Mirror the production gate: SqlState 23505 + schema media + table asset_owner_occurrences
        // + constraint pk_asset_owner_occurrences. Substring OR matching is intentionally not enough.
        var method = typeof(MediaDirectory).GetMethod(
            "IsOccurrenceUniqueViolation",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);

        var exact = CreateDbUpdateException(
            sqlState: "23505",
            schema: "media",
            table: "asset_owner_occurrences",
            constraint: "pk_asset_owner_occurrences");
        Assert.True((bool)method!.Invoke(null, [exact])!);

        var wrongTable = CreateDbUpdateException(
            sqlState: "23505",
            schema: "media",
            table: "assets",
            constraint: "pk_assets");
        Assert.False((bool)method.Invoke(null, [wrongTable])!);

        var substringTrap = CreateDbUpdateException(
            sqlState: "23505",
            schema: "media",
            table: "other_asset_owner_occurrences_x",
            constraint: "ux_something");
        Assert.False((bool)method.Invoke(null, [substringTrap])!);
    }

    private static DbUpdateException CreateDbUpdateException(
        string sqlState, string schema, string table, string constraint)
    {
        var postgres = new Npgsql.PostgresException(
            "unique", "ERROR", "ERROR", sqlState,
            schemaName: schema, tableName: table, constraintName: constraint);
        return new DbUpdateException("conflict", postgres);
    }

    private static MediaDbContext CreateDb(string name) =>
        new(new DbContextOptionsBuilder<MediaDbContext>().UseInMemoryDatabase(name).Options);

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
