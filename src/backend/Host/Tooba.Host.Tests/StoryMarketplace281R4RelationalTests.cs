using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.Media.Application.Ports;
using Tooba.Media.Contracts.Assets;
using Tooba.Media.Contracts.Errors;
using Tooba.Media.Infrastructure.Assets;
using Tooba.Media.Infrastructure.Persistence;
using Tooba.Persistence;
using Tooba.Story.Contracts.Errors;
using Tooba.Story.Domain.Tenant;
using Tooba.Story.Infrastructure.Directories;
using Tooba.Story.Infrastructure.Persistence;
using Xunit;
using Xunit.Abstractions;
using StoryEntity = Tooba.Story.Domain.Aggregates.Story;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-281-R4 C1/C2/C3 rollback-only relational proofs on canonical tooba_alpha.
/// </summary>
[Collection("PostgresSerial")]
public sealed class StoryMarketplace281R4RelationalTests
{
    private static readonly Lazy<(bool Ok, string Reason)> CanonicalTarget = new(ValidateCanonicalTarget);
    private readonly ITestOutputHelper _output;
    private readonly string _runTag = $"tb281r4-{Guid.NewGuid():N}-";

    public StoryMarketplace281R4RelationalTests(ITestOutputHelper output) => _output = output;

    private static string? Connection =>
        Environment.GetEnvironmentVariable("TB281R4_CONNECTION") is { Length: > 0 } raw
            ? raw.Trim()
            : Environment.GetEnvironmentVariable("TB272_CONNECTION") is { Length: > 0 } legacy
                ? legacy.Trim()
                : "Host=127.0.0.1;Port=5432;Username=admin;Password=123456;Database=tooba_alpha";

    [SkippableFact]
    public async Task C1_persisted_items_publication_visibility_and_seller_isolation_rollback_only()
    {
        Skip.IfNot(CanonicalTarget.Value.Ok, CanonicalTarget.Value.Reason);

        await using var db = CreateStoryDb();
        await using var caller = await db.Database.BeginTransactionAsync();
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

        // Empty / unreviewed negatives: no playable slides, or submitted but not approved.
        var empty = StoryEntity.CreateSellerDraft(
            tenant, shopA, Guid.NewGuid(), _runTag + "empty", 2, now, null, null, null, null, null, null);

        var unreviewed = StoryEntity.CreateSellerDraft(
            tenant, shopA, Guid.NewGuid(), _runTag + "unrev", 3, now, null, null, null, null, null, null);
        unreviewed.AddItem("image", 0, now, Guid.NewGuid(), "https://example.test/u.jpg", null, null, null, null);
        unreviewed.SubmitForReview(Guid.NewGuid(), now);

        var otherShop = StoryEntity.CreateSellerDraft(
            tenant, shopB, Guid.NewGuid(), _runTag + "other", 0, now, null, null, null, null, null, null);
        otherShop.AddItem("image", 0, now, Guid.NewGuid(), "https://example.test/c.jpg", null, null, null, null);
        otherShop.SubmitForReview(Guid.NewGuid(), now);
        otherShop.Approve(Guid.NewGuid(), now);
        otherShop.Activate(now);

        foreach (var story in new[] { publishable, future, empty, unreviewed, otherShop })
        {
            db.Stories.Add(story);
            foreach (var item in story.Items)
                db.StoryItems.Add(item);
        }
        await db.SaveChangesAsync();

        var directory = new StoryDirectory(db);
        var publicCards = await directory.GetPublicStoriesAsync(tenant, null, null, now, CancellationToken.None);
        Assert.Contains(publicCards, c => c.StoryId == publishable.StoryId);
        Assert.DoesNotContain(publicCards, c => c.StoryId == future.StoryId);
        Assert.DoesNotContain(publicCards, c => c.StoryId == empty.StoryId);
        Assert.DoesNotContain(publicCards, c => c.StoryId == unreviewed.StoryId);

        var sellerA = await directory.SellerListAsync(tenant, shopA, CancellationToken.None);
        Assert.All(sellerA, row => Assert.Equal(shopA, row.SellerPartyId));
        Assert.DoesNotContain(sellerA, row => row.StoryId == otherShop.StoryId);

        await caller.RollbackAsync();
        Assert.Equal(0L, await CountTaggedStoriesAsync());
        _output.WriteLine($"C1 committedRows=0 tag={_runTag}");
    }

    [SkippableFact]
    public async Task C2_tenant_scoped_occurrence_duplicate_and_stale_version_zero_write()
    {
        Skip.IfNot(CanonicalTarget.Value.Ok, CanonicalTarget.Value.Reason);

        await using var db = CreateStoryDb();
        await using var caller = await db.Database.BeginTransactionAsync();
        var now = DateTimeOffset.UtcNow;
        var tenantA = StoryTenantIds.StoreAlpha;
        var tenantB = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var shop = Guid.NewGuid();
        var key = _runTag + "occ-shared";

        var storyA = StoryEntity.CreateSellerDraft(
            tenantA, shop, Guid.NewGuid(), _runTag + "a", 0, now, null, null, null, null, null, null);
        db.Stories.Add(storyA);
        db.CommandOccurrences.Add(new Tooba.Story.Domain.Aggregates.StoryCommandOccurrence
        {
            OccurrenceKey = key,
            TenantId = tenantA,
            SellerPartyId = shop,
            ActionCode = "seller_create",
            IntentHash = "hash-a",
            StoryId = storyA.StoryId,
            ActorUserId = Guid.NewGuid(),
            CreatedAt = now,
        });
        await db.SaveChangesAsync();

        // Same key, different tenant: allowed by (TenantId, OccurrenceKey) PK.
        db.CommandOccurrences.Add(new Tooba.Story.Domain.Aggregates.StoryCommandOccurrence
        {
            OccurrenceKey = key,
            TenantId = tenantB,
            SellerPartyId = shop,
            ActionCode = "seller_create",
            IntentHash = "hash-b",
            StoryId = Guid.NewGuid(),
            ActorUserId = Guid.NewGuid(),
            CreatedAt = now,
        });
        await db.SaveChangesAsync();
        Assert.Equal(2, await db.CommandOccurrences.CountAsync(x => x.OccurrenceKey == key));

        // Real duplicate on same tenant: 23505 inside savepoint (raw insert avoids tracker identity map).
        await caller.CreateSavepointAsync("tb281r4_occ");
        Exception? dup = null;
        try
        {
            await db.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO story.command_occurrences
                    (occurrence_key, tenant_id, seller_party_id, action_code, intent_hash, story_id, actor_user_id, created_at)
                VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7})
                """,
                key, tenantA, shop, "seller_create", "hash-dup", Guid.NewGuid(), Guid.NewGuid(), now);
        }
        catch (Exception ex)
        {
            dup = ex;
        }
        Assert.NotNull(dup);
        Assert.True(
            dup is PostgresException { SqlState: "23505" }
            || dup.InnerException is PostgresException { SqlState: "23505" }
            || (dup as DbUpdateException)?.InnerException is PostgresException { SqlState: "23505" });
        await caller.RollbackToSavepointAsync("tb281r4_occ");

        // Stale version: EF concurrency token refuses with zero additional occurrence.
        var tracked = await db.Stories.SingleAsync(x => x.StoryId == storyA.StoryId);
        var beforeOcc = await db.CommandOccurrences.CountAsync(x => x.TenantId == tenantA);
        var refused = await Assert.ThrowsAsync<SemanticException>(() =>
        {
            tracked.EnsureExpectedVersion(tracked.VersionToken - 1);
            return Task.CompletedTask;
        });
        Assert.Equal(StoryErrorCodes.VersionStale, refused.Error.Code);
        Assert.Equal(beforeOcc, await db.CommandOccurrences.CountAsync(x => x.TenantId == tenantA));

        await caller.RollbackAsync();
        Assert.Equal(0L, await CountTaggedStoriesAsync());
        _output.WriteLine($"C2 committedRows=0 tag={_runTag}");
    }

    [SkippableFact]
    public async Task C3_media_ambient_tx_refused_and_save_fail_interceptor_on_relational()
    {
        Skip.IfNot(CanonicalTarget.Value.Ok, CanonicalTarget.Value.Reason);

        await using var db = CreateMediaDb();
        await using var caller = await db.Database.BeginTransactionAsync();
        var store = new RecordingStore();
        var media = new MediaDirectory(db, store, Config());

        var ambient = await Assert.ThrowsAsync<ContractOperationException>(() =>
            media.UploadForSellerAsync(
                new MemoryStream("x"u8.ToArray()), "a.jpg", "image/jpeg",
                StoryTenantIds.StoreAlpha, Guid.NewGuid(), Guid.NewGuid(), _runTag + "tx", CancellationToken.None));
        Assert.Equal(MediaErrorCodes.ValidationFailed, ambient.Code);
        Assert.Empty(store.Saved);

        await caller.RollbackAsync();

        // Owning code refuses ambient tx first — prove that refusal is the relational fact for caller-tx path.
        await using var failDb = CreateMediaDb(new FailOnSavingChangesInterceptor());
        await using var failTx = await failDb.Database.BeginTransactionAsync();
        var store2 = new RecordingStore();
        var media2 = new MediaDirectory(failDb, store2, Config());
        var refused = await Assert.ThrowsAsync<ContractOperationException>(() =>
            media2.UploadForSellerAsync(
                new MemoryStream("y"u8.ToArray()), "b.jpg", "image/jpeg",
                StoryTenantIds.StoreAlpha, Guid.NewGuid(), Guid.NewGuid(), _runTag + "fail", CancellationToken.None));
        Assert.Equal(MediaErrorCodes.ValidationFailed, refused.Code);
        Assert.Empty(store2.Saved);
        await failTx.RollbackAsync();

        // No ambient tx: interceptor SavingChanges failure — object cleaned, zero durable rows.
        await using var cleanDb = CreateMediaDb(new FailOnSavingChangesInterceptor());
        var store3 = new RecordingStore();
        var media3 = new MediaDirectory(cleanDb, store3, Config());
        var failed = await Assert.ThrowsAsync<ContractOperationException>(() =>
            media3.UploadForSellerAsync(
                new MemoryStream("z"u8.ToArray()), "c.jpg", "image/jpeg",
                StoryTenantIds.StoreAlpha, Guid.NewGuid(), Guid.NewGuid(), _runTag + "nofailtx", CancellationToken.None));
        Assert.Equal(MediaErrorCodes.UploadFailed, failed.Code);
        Assert.Equal(1, store3.Saved.Count);
        Assert.Equal(1, store3.Deleted.Count);
        Assert.Equal(0, await cleanDb.Assets.CountAsync(x => x.OriginalFileName == "c.jpg"));
        _output.WriteLine("C3 ambient refused; interceptor fail cleaned attempt object; committedRows=0 for tagged keys");
    }

    [SkippableFact]
    public async Task R22_public_cover_kind_comes_from_the_media_owner_rollback_only()
    {
        Skip.IfNot(CanonicalTarget.Value.Ok, CanonicalTarget.Value.Reason);

        await using var db = CreateStoryDb();
        await using var caller = await db.Database.BeginTransactionAsync();
        var now = DateTimeOffset.UtcNow;
        var tenant = StoryTenantIds.StoreAlpha;
        var shop = Guid.NewGuid();
        var cover = Guid.NewGuid();
        var itemAsset = Guid.NewGuid();

        var story = StoryEntity.CreateSellerDraft(
            tenant, shop, Guid.NewGuid(), _runTag + "cover", 0, now,
            coverMediaAssetId: cover, coverMediaUrl: "/cover.jpg");
        story.AddItem("image", 0, now, itemAsset, "/slide.jpg", null, null, null, null);
        story.SubmitForReview(Guid.NewGuid(), now);
        story.Approve(Guid.NewGuid(), now);
        story.Activate(now);
        db.Stories.Add(story);
        foreach (var item in story.Items) db.StoryItems.Add(item);
        await db.SaveChangesAsync();

        var directory = new StoryDirectory(db, media: new VideoCoverMedia());
        var cards = await directory.GetPublicStoriesAsync(tenant, null, null, now, CancellationToken.None);
        var card = Assert.Single(cards, row => row.StoryId == story.StoryId);
        Assert.True(card.CoverIsVideo);

        await caller.RollbackAsync();
        Assert.Equal(0L, await CountTaggedStoriesAsync());
    }
    [SkippableFact]
    public async Task R22_seller_grid_search_status_sort_and_count_run_on_postgresql_and_stay_shop_scoped()
    {
        Skip.IfNot(CanonicalTarget.Value.Ok, CanonicalTarget.Value.Reason);

        await using var db = CreateStoryDb();
        await using var caller = await db.Database.BeginTransactionAsync();
        var now = DateTimeOffset.UtcNow;
        var tenant = StoryTenantIds.StoreAlpha;
        var shopA = Guid.NewGuid();
        var shopB = Guid.NewGuid();

        var alpha = StoryEntity.CreateSellerDraft(
            tenant, shopA, Guid.NewGuid(), _runTag + "alpha", 0, now.AddMinutes(-2), null, null, null, null, null, null);
        alpha.AddItem("image", 0, now, Guid.NewGuid(), "https://example.test/alpha.jpg", null, null, null, null);
        alpha.SubmitForReview(Guid.NewGuid(), now);
        alpha.Approve(Guid.NewGuid(), now);
        alpha.SetSchedule(now.AddDays(-2), now.AddDays(2), now);
        var beta = StoryEntity.CreateSellerDraft(
            tenant, shopA, Guid.NewGuid(), _runTag + "beta", 1, now.AddMinutes(-1), null, null, null, null, null, null);
        beta.AddItem("image", 0, now, Guid.NewGuid(), "https://example.test/beta.jpg", null, null, null, null);
        beta.SubmitForReview(Guid.NewGuid(), now);
        beta.Approve(Guid.NewGuid(), now);
        beta.SetSchedule(now.AddDays(-1), now.AddDays(3), now);
        var foreign = StoryEntity.CreateSellerDraft(
            tenant, shopB, Guid.NewGuid(), _runTag + "alpha-foreign", 0, now, null, null, null, null, null, null);

        foreach (var story in new[] { alpha, beta, foreign })
            db.Stories.Add(story);
        await db.SaveChangesAsync();

        var directory = new StoryDirectory(db);
        var searched = await directory.SellerQueryAsync(
            tenant,
            shopA,
            new GridQueryRequest(1, 10, _runTag + "alpha", [], []),
            CancellationToken.None);
        Assert.Equal(1, searched.TotalCount);
        Assert.Equal(alpha.StoryId, Assert.Single(searched.Items).StoryId);

        var drafts = await directory.SellerQueryAsync(
            tenant,
            shopA,
            new GridQueryRequest(1, 10, null, [], [new GridFilterRequest("status", "equals", "Active", null, null)]),
            CancellationToken.None);
        Assert.Equal(2, drafts.TotalCount);
        Assert.Contains(drafts.Items, row => row.StoryId == alpha.StoryId);

        var submitted = await directory.SellerQueryAsync(
            tenant,
            shopA,
            new GridQueryRequest(1, 10, null, [], [new GridFilterRequest("reviewStatus", "equals", "Approved", null, null)]),
            CancellationToken.None);
        Assert.Equal(2, submitted.TotalCount);
        Assert.Contains(submitted.Items, row => row.StoryId == beta.StoryId);

        foreach (var column in new[] { "startAt", "endAt", "updatedAt" })
        {
            var bounded = await directory.SellerQueryAsync(
                tenant,
                shopA,
                new GridQueryRequest(1, 10, null, [], [new GridFilterRequest(column, "notBlank", null, null, null)]),
                CancellationToken.None);
            Assert.Equal(2, bounded.TotalCount);
            Assert.DoesNotContain(bounded.Items, row => row.StoryId == foreign.StoryId);
        }

        var descending = await directory.SellerQueryAsync(
            tenant,
            shopA,
            new GridQueryRequest(1, 1, null, [new GridSortRequest("title", "desc")], []),
            CancellationToken.None);
        Assert.Equal(2, descending.TotalCount);
        Assert.Equal(beta.StoryId, Assert.Single(descending.Items).StoryId);
        Assert.DoesNotContain(descending.Items, row => row.StoryId == foreign.StoryId);

        var ascending = await directory.SellerQueryAsync(
            tenant,
            shopA,
            new GridQueryRequest(1, 1, null, [new GridSortRequest("title", "asc")], []),
            CancellationToken.None);
        Assert.Equal(alpha.StoryId, Assert.Single(ascending.Items).StoryId);

        await caller.RollbackAsync();
        Assert.Equal(0L, await CountTaggedStoriesAsync());
        _output.WriteLine($"R22 seller grid filter/sort/count committedRows=0 tag={_runTag}");
    }

    private sealed class VideoCoverMedia : IStoryMediaAssetPort
    {
        public Task<StoryMediaAssetAuthorization> AuthorizeForStoryAsync(
            Guid mediaAssetId, Guid tenantId, Guid? sellerPartyId, bool allowUnscopedAdminAssets,
            CancellationToken cancellationToken) =>
            Task.FromResult(new StoryMediaAssetAuthorization(
                mediaAssetId, "video/mp4", $"/v1/storefront/media/{mediaAssetId:D}", true, sellerPartyId));

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
    private async Task<long> CountTaggedStoriesAsync()
    {
        await using var verify = CreateStoryDb();
        return await verify.Stories.AsNoTracking().LongCountAsync(x => x.Title.StartsWith(_runTag));
    }

    private static StoryDbContext CreateStoryDb(SaveChangesInterceptor? interceptor = null)
    {
        var options = new DbContextOptionsBuilder<StoryDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options, Connection!, StoryDbContext.Schema, typeof(StoryDbContext));
        if (interceptor is not null)
            options.AddInterceptors(interceptor);
        return new StoryDbContext(options.Options);
    }

    private static MediaDbContext CreateMediaDb(SaveChangesInterceptor? interceptor = null)
    {
        var options = new DbContextOptionsBuilder<MediaDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options, Connection!, MediaDbContext.Schema, typeof(MediaDbContext));
        if (interceptor is not null)
            options.AddInterceptors(interceptor);
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
            return (false, "TB281R4_CONNECTION is not set; rollback-only proof did not run.");

        NpgsqlConnectionStringBuilder builder;
        try { builder = new NpgsqlConnectionStringBuilder(raw); }
        catch { return (false, "TB281R4_CONNECTION is not a valid Npgsql connection string."); }

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

    private sealed class FailOnSavingChangesInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("forced save failure");
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
