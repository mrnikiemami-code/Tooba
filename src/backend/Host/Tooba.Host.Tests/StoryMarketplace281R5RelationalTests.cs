using System.Data.Common;
using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.Media.Application.Ports;
using Tooba.Media.Contracts.Errors;
using Tooba.Media.Infrastructure.Assets;
using Tooba.Media.Infrastructure.Persistence;
using Tooba.Party.Contracts.Ports;
using Tooba.Persistence;
using Tooba.Story.Application.Stories.Models;
using Tooba.Story.Contracts.Errors;
using Tooba.Story.Domain.Tenant;
using Tooba.Story.Infrastructure.Directories;
using Tooba.Story.Infrastructure.Grid;
using Tooba.Story.Infrastructure.Persistence;
using Xunit;
using Xunit.Abstractions;
using StoryEntity = Tooba.Story.Domain.Aggregates.Story;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-281-R5 C2: rollback-only PostgreSQL proofs — fresh-context reads, admin grid shop-name,
/// EF two-context concurrency, ambient/enlistment Media guards, independent committedRows=0.
/// </summary>
[Collection("PostgresSerial")]
public sealed class StoryMarketplace281R5RelationalTests
{
    private static readonly Lazy<(bool Ok, string Reason)> CanonicalTarget = new(ValidateCanonicalTarget);
    private readonly ITestOutputHelper _output;
    private readonly string _runTag = $"tb281r5-{Guid.NewGuid():N}-";

    public StoryMarketplace281R5RelationalTests(ITestOutputHelper output) => _output = output;

    private static string? Connection =>
        Environment.GetEnvironmentVariable("TB281R5_CONNECTION") is { Length: > 0 } raw
            ? raw.Trim()
            : Environment.GetEnvironmentVariable("TB281R4_CONNECTION") is { Length: > 0 } r4
                ? r4.Trim()
                : Environment.GetEnvironmentVariable("TB272_CONNECTION") is { Length: > 0 } legacy
                    ? legacy.Trim()
                    : "Host=127.0.0.1;Port=5432;Username=admin;Password=123456;Database=tooba_alpha";

    [SkippableFact]
    public async Task C2_fresh_context_public_expired_admin_grid_shop_filter_and_seller_isolation()
    {
        Skip.IfNot(CanonicalTarget.Value.Ok, CanonicalTarget.Value.Reason);

        await using var writeDb = CreateStoryDb();
        await using var caller = await writeDb.Database.BeginTransactionAsync();
        var now = DateTimeOffset.UtcNow;
        var shopA = Guid.NewGuid();
        var shopB = Guid.NewGuid();
        var tenant = StoryTenantIds.StoreAlpha;

        var publishable = StoryEntity.CreateSellerDraft(
            tenant, shopA, Guid.NewGuid(), _runTag + "pub", 0, now, null, null, null, null, null, null);
        publishable.AddItem("image", 0, now, Guid.NewGuid(), "https://example.test/a.jpg", null, null, null, null);
        publishable.SubmitForReview(Guid.NewGuid(), now);
        publishable.Approve(Guid.NewGuid(), now);
        publishable.Activate(now);

        var future = StoryEntity.CreateSellerDraft(
            tenant, shopA, Guid.NewGuid(), _runTag + "future", 1, now, null, null, null, null, null, null);
        future.AddItem("image", 0, now, Guid.NewGuid(), "https://example.test/b.jpg", null, null, null, null);
        future.SubmitForReview(Guid.NewGuid(), now);
        future.Approve(Guid.NewGuid(), now);
        future.SetSchedule(now.AddDays(1), now.AddDays(2), now);

        var expired = StoryEntity.CreateSellerDraft(
            tenant, shopA, Guid.NewGuid(), _runTag + "expired", 2, now, null, null, null, null, null, null);
        expired.AddItem("image", 0, now, Guid.NewGuid(), "https://example.test/e.jpg", null, null, null, null);
        expired.SubmitForReview(Guid.NewGuid(), now);
        expired.Approve(Guid.NewGuid(), now);
        expired.SetSchedule(now.AddDays(-3), now.AddDays(-1), now);

        var otherShop = StoryEntity.CreateSellerDraft(
            tenant, shopB, Guid.NewGuid(), _runTag + "other", 0, now, null, null, null, null, null, null);
        otherShop.AddItem("image", 0, now, Guid.NewGuid(), "https://example.test/c.jpg", null, null, null, null);
        otherShop.SubmitForReview(Guid.NewGuid(), now);
        otherShop.Approve(Guid.NewGuid(), now);
        otherShop.Activate(now);

        foreach (var story in new[] { publishable, future, expired, otherShop })
        {
            writeDb.Stories.Add(story);
            foreach (var item in story.Items)
                writeDb.StoryItems.Add(item);
        }
        await writeDb.SaveChangesAsync();

        await using var readDb = CreateStoryDbSharing(writeDb, caller);
        var parties = new ShopNameParty(shopA, "Alpha Shop", shopB, "Beta Shop");
        var directory = new StoryDirectory(readDb, parties, null, null);

        var publicCards = await directory.GetPublicStoriesAsync(tenant, null, null, now, CancellationToken.None);
        Assert.Contains(publicCards, c => c.StoryId == publishable.StoryId);
        Assert.DoesNotContain(publicCards, c => c.StoryId == future.StoryId);
        Assert.DoesNotContain(publicCards, c => c.StoryId == expired.StoryId);

        var grid = new AdminStoryGridQueryEngine(readDb, parties);
        var page = await grid.QueryAsync(
            tenant,
            reviewStatus: null,
            new GridQueryRequest(
                Page: 1,
                PageSize: 20,
                Search: null,
                Sort: [new GridSortRequest("displayOrder", "asc")],
                Filters:
                [
                    new GridFilterRequest("shopDisplayName", "contains", "Alpha", null, null)
                ]),
            CancellationToken.None);
        Assert.True(page.TotalCount >= 1);
        Assert.Contains(page.Items, row => row.StoryId == publishable.StoryId);
        Assert.DoesNotContain(page.Items, row => row.StoryId == otherShop.StoryId);
        Assert.All(page.Items.Where(r => r.Title.StartsWith(_runTag, StringComparison.Ordinal)),
            row => Assert.Equal(shopA, row.SellerPartyId));

        var sellerPage = await directory.SellerQueryAsync(
            tenant,
            shopA,
            new GridQueryRequest(1, 20, null, [new GridSortRequest("displayOrder", "asc")], []),
            CancellationToken.None);
        Assert.All(sellerPage.Items, row => Assert.Equal(shopA, row.SellerPartyId));
        Assert.DoesNotContain(sellerPage.Items, row => row.StoryId == otherShop.StoryId);

        await caller.RollbackAsync();
        Assert.Equal(0L, await CountTaggedStoriesIndependentAsync());
        _output.WriteLine($"C2-fresh-grid committedRows=0 tag={_runTag}");
    }

    [SkippableFact]
    public async Task C2_two_context_stale_version_owning_command_zero_extra_occurrence()
    {
        Skip.IfNot(CanonicalTarget.Value.Ok, CanonicalTarget.Value.Reason);

        await using var db1 = CreateStoryDb();
        await using var caller = await db1.Database.BeginTransactionAsync();
        var media = Guid.NewGuid();
        var shop = Guid.NewGuid();
        var parties = new ShopNameParty(shop, "Shop");
        var products = new AlwaysOkProducts();
        var mediaPort = new AlwaysOkMedia(media);
        var dir1 = new StoryDirectory(db1, parties, mediaPort, products);

        var created = await dir1.SellerCreateDraftAsync(
            StoryTenantIds.StoreAlpha, shop, Guid.NewGuid(),
            new CreateStoryCommand(_runTag + "stale", null, null, media, null, null, null, null, OccurrenceKey: _runTag + "c"),
            CancellationToken.None);
        var originalVersion = created.VersionToken;

        await using var db2 = CreateStoryDbSharing(db1, caller);
        var dir2 = new StoryDirectory(db2, parties, mediaPort, products);

        var first = await dir1.SellerUpdateAsync(
            StoryTenantIds.StoreAlpha, shop, created.StoryId,
            new UpdateStoryCommand(_runTag + "stale-v2", null, null, media, null, null, null, originalVersion, _runTag + "u1"),
            CancellationToken.None);
        Assert.True(first.VersionToken > originalVersion);

        var beforeOcc = await db1.CommandOccurrences.CountAsync(x => x.OccurrenceKey.StartsWith(_runTag));
        var stale = await Assert.ThrowsAsync<SemanticException>(() =>
            dir2.SellerUpdateAsync(
                StoryTenantIds.StoreAlpha, shop, created.StoryId,
                new UpdateStoryCommand(_runTag + "stale-v3", null, null, media, null, null, null, originalVersion, _runTag + "u2"),
                CancellationToken.None));
        Assert.Equal(StoryErrorCodes.VersionStale, stale.Error.Code);
        Assert.Equal(beforeOcc, await db1.CommandOccurrences.CountAsync(x => x.OccurrenceKey.StartsWith(_runTag)));

        await caller.RollbackAsync();
        Assert.Equal(0L, await CountTaggedStoriesIndependentAsync());
        Assert.Equal(0L, await CountTaggedOccurrencesIndependentAsync());
        _output.WriteLine($"C2-two-context-stale committedRows=0 tag={_runTag}");
    }

    [SkippableFact]
    public async Task C2_media_ef_current_ambient_scope_and_enlistment_refuse_before_store()
    {
        Skip.IfNot(CanonicalTarget.Value.Ok, CanonicalTarget.Value.Reason);

        // EF CurrentTransaction
        await using (var db = CreateMediaDb())
        {
            await using var caller = await db.Database.BeginTransactionAsync();
            var store = new RecordingStore();
            var media = new MediaDirectory(db, store, Config());
            Assert.NotNull(db.Database.CurrentTransaction);
            var refused = await Assert.ThrowsAsync<ContractOperationException>(() =>
                media.UploadForSellerAsync(
                    new MemoryStream("x"u8.ToArray()), "a.jpg", "image/jpeg",
                    StoryTenantIds.StoreAlpha, Guid.NewGuid(), Guid.NewGuid(), _runTag + "ef", CancellationToken.None));
            Assert.Equal(MediaErrorCodes.ValidationFailed, refused.Code);
            Assert.Empty(store.Saved);
            await caller.RollbackAsync();
        }

        // System.Transactions.Transaction.Current (ambient TransactionScope)
        await using (var db = CreateMediaDb())
        {
            var store = new RecordingStore();
            var media = new MediaDirectory(db, store, Config());
            using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                Assert.NotNull(Transaction.Current);
                Assert.Null(db.Database.CurrentTransaction);
                var refused = await Assert.ThrowsAsync<ContractOperationException>(() =>
                    media.UploadForSellerAsync(
                        new MemoryStream("y"u8.ToArray()), "b.jpg", "image/jpeg",
                        StoryTenantIds.StoreAlpha, Guid.NewGuid(), Guid.NewGuid(), _runTag + "scope", CancellationToken.None));
                Assert.Equal(MediaErrorCodes.ValidationFailed, refused.Code);
                Assert.Empty(store.Saved);
            }
        }

        // Explicit provider enlistment (no EF CurrentTransaction, no ambient Transaction.Current)
        await using (var db = CreateMediaDb())
        {
            var store = new RecordingStore();
            var media = new MediaDirectory(db, store, Config());
            using var enlisted = new CommittableTransaction();
            await db.Database.OpenConnectionAsync();
            var connection = Assert.IsAssignableFrom<RelationalConnection>(
                db.Database.GetService<IRelationalConnection>());
            connection.EnlistTransaction(enlisted);
            Assert.Null(db.Database.CurrentTransaction);
            Assert.Null(Transaction.Current);
            Assert.Same(enlisted, connection.EnlistedTransaction);

            var refused = await Assert.ThrowsAsync<ContractOperationException>(() =>
                media.UploadForSellerAsync(
                    new MemoryStream("z"u8.ToArray()), "c.jpg", "image/jpeg",
                    StoryTenantIds.StoreAlpha, Guid.NewGuid(), Guid.NewGuid(), _runTag + "enlist", CancellationToken.None));
            Assert.Equal(MediaErrorCodes.ValidationFailed, refused.Code);
            Assert.Empty(store.Saved);
            Assert.Same(enlisted, connection.EnlistedTransaction);
            enlisted.Rollback();
            await db.Database.CloseConnectionAsync();
        }

        Assert.Equal(0L, await CountTaggedMediaOccurrencesIndependentAsync());
        _output.WriteLine($"C2-media-tx-guards committedRows=0 tag={_runTag}");
    }

    private async Task<long> CountTaggedStoriesIndependentAsync()
    {
        await using var verify = CreateStoryDb();
        return await verify.Stories.AsNoTracking().LongCountAsync(x => x.Title.StartsWith(_runTag));
    }

    private async Task<long> CountTaggedOccurrencesIndependentAsync()
    {
        await using var verify = CreateStoryDb();
        return await verify.CommandOccurrences.AsNoTracking()
            .LongCountAsync(x => x.OccurrenceKey.StartsWith(_runTag));
    }

    private async Task<long> CountTaggedMediaOccurrencesIndependentAsync()
    {
        await using var verify = CreateMediaDb();
        return await verify.AssetOwnerOccurrences.AsNoTracking()
            .LongCountAsync(x => x.OccurrenceKey.StartsWith(_runTag));
    }

    private static StoryDbContext CreateStoryDb()
    {
        var options = new DbContextOptionsBuilder<StoryDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options, Connection!, StoryDbContext.Schema, typeof(StoryDbContext));
        return new StoryDbContext(options.Options);
    }

    private static StoryDbContext CreateStoryDbSharing(StoryDbContext owner, IDbContextTransaction tx)
    {
        var options = new DbContextOptionsBuilder<StoryDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options, Connection!, StoryDbContext.Schema, typeof(StoryDbContext));
        var db = new StoryDbContext(options.Options);
        db.Database.SetDbConnection(owner.Database.GetDbConnection());
        db.Database.UseTransaction(tx.GetDbTransaction());
        return db;
    }

    private static MediaDbContext CreateMediaDb()
    {
        var options = new DbContextOptionsBuilder<MediaDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options, Connection!, MediaDbContext.Schema, typeof(MediaDbContext));
        return new MediaDbContext(options.Options);
    }

    private static IConfiguration Config() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Tooba:Media:MaxUploadBytes"] = "1048576",
        }).Build();

    private static (bool Ok, string Reason) ValidateCanonicalTarget()
    {
        var raw = Connection;
        if (string.IsNullOrWhiteSpace(raw))
            return (false, "TB281R5_CONNECTION is not set; rollback-only proof did not run.");

        NpgsqlConnectionStringBuilder builder;
        try { builder = new NpgsqlConnectionStringBuilder(raw); }
        catch { return (false, "TB281R5_CONNECTION is not a valid Npgsql connection string."); }

        if (!IsLoopback(builder.Host) || builder.Port != 5432
            || !string.Equals(builder.Database, "tooba_alpha", StringComparison.Ordinal))
            return (false, "Connection does not address loopback:5432/tooba_alpha.");

        try
        {
            using var connection = new NpgsqlConnection(raw);
            connection.Open();
            using var command = new NpgsqlCommand(
                "SELECT current_database(), inet_server_port()", connection);
            using var reader = command.ExecuteReader();
            if (!reader.Read())
                return (false, "Server did not answer identity.");
            var database = reader.GetString(0);
            var port = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
            if (!string.Equals(database, "tooba_alpha", StringComparison.Ordinal) || port != 5432)
                return (false, $"Server identity database={database} port={port} is not canonical.");
            return (true, "ok");
        }
        catch (Exception ex)
        {
            return (false, $"Canonical target connection failed: {ex.GetType().Name}");
        }
    }

    private static bool IsLoopback(string? host) =>
        string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
        || string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
        || string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase);

    private sealed class ShopNameParty : IPartyAdminSellerReadGateway
    {
        private readonly Dictionary<Guid, string> _names;
        public ShopNameParty(Guid a, string nameA, Guid? b = null, string? nameB = null)
        {
            _names = new Dictionary<Guid, string> { [a] = nameA };
            if (b is Guid id && nameB is not null)
                _names[id] = nameB;
        }

        public Task<IReadOnlyList<PartyStatusProjection>> GetStatusProjectionsAsync(
            IReadOnlyList<Guid> partyIds, CancellationToken cancellationToken)
        {
            IReadOnlyList<PartyStatusProjection> rows = partyIds
                .Where(_names.ContainsKey)
                .Select(id => new PartyStatusProjection(id, _names[id], "Active"))
                .ToList();
            return Task.FromResult(rows);
        }
    }

    private sealed class AlwaysOkMedia(Guid mediaId) : Tooba.Media.Contracts.Assets.IStoryMediaAssetPort
    {
        public Task<Tooba.Media.Contracts.Assets.StoryMediaAssetAuthorization> AuthorizeForStoryAsync(
            Guid mediaAssetId, Guid tenantId, Guid? sellerPartyId, bool allowUnscopedAdminAssets,
            CancellationToken cancellationToken) =>
            Task.FromResult(new Tooba.Media.Contracts.Assets.StoryMediaAssetAuthorization(
                mediaId, "image/jpeg", $"/v1/storefront/media/{mediaId:D}", false, sellerPartyId));

        public Task<Tooba.Media.Contracts.Assets.StoryMediaAssetAuthorization> UploadForSellerAsync(
            Stream stream, string fileName, string contentType, Guid tenantId, Guid sellerPartyId,
            Guid actorUserId, string? occurrenceKey, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Tooba.Media.Contracts.Assets.StoryMediaAssetAuthorization>> ListSellerAssetsAsync(
            Guid tenantId, Guid sellerPartyId, int take, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Tooba.Media.Contracts.Assets.StoryMediaAssetAuthorization>>([]);
    }

    private sealed class AlwaysOkProducts : Tooba.Story.Application.Stories.Ports.IStoryProductTargetPort
    {
        public Task EnsureOwnedBySellerAsync(Guid productId, Guid sellerPartyId, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task EnsurePublishedExistsAsync(Guid productId, CancellationToken cancellationToken) =>
            Task.CompletedTask;
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
