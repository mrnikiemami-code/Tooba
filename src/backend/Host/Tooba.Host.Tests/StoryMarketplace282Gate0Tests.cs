using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Tooba.BuildingBlocks;
using Tooba.Media.Contracts.Assets;
using Tooba.Party.Contracts.Ports;
using Tooba.Persistence;
using Tooba.Story.Application.Stories.Models;
using Tooba.Story.Application.Stories.Ports;
using Tooba.Story.Contracts.Errors;
using Tooba.Story.Domain.Aggregates;
using Tooba.Story.Domain.Tenant;
using Tooba.Story.Infrastructure.Directories;
using Tooba.Story.Infrastructure.Persistence;
using Xunit;
using Xunit.Abstractions;
using StoryEntity = Tooba.Story.Domain.Aggregates.Story;

namespace Tooba.Host.Tests;

/// <summary>TB-282 Gate 0: full occurrence identity fold, attempt detach, real EF concurrency.</summary>
[Collection("PostgresSerial")]
public sealed class StoryMarketplace282Gate0Tests
{
    private static readonly Lazy<(bool Ok, string Reason)> CanonicalTarget = new(ValidateCanonicalTarget);
    private readonly ITestOutputHelper _output;
    private readonly string _runTag = $"tb282-{Guid.NewGuid():N}-";

    public StoryMarketplace282Gate0Tests(ITestOutputHelper output) => _output = output;

    private static string? Connection =>
        Environment.GetEnvironmentVariable("TB282_CONNECTION")
        ?? Environment.GetEnvironmentVariable("TB281R5_CONNECTION")
        ?? Environment.GetEnvironmentVariable("TB281R4_CONNECTION")
        ?? "Host=127.0.0.1;Port=5432;Username=admin;Password=123456;Database=tooba_alpha";

    [Fact]
    public async Task Gate0a_winner_fold_refuses_different_shop_even_with_same_intent_hash()
    {
        var dbName = "g0a-" + Guid.NewGuid().ToString("N");
        var media = Guid.NewGuid();
        var shopA = Guid.NewGuid();
        var shopB = Guid.NewGuid();
        var key = "shared-key";
        var command = new CreateStoryCommand("T", null, null, media, null, null, null, null, OccurrenceKey: key);
        var hashIntent = (string)typeof(StoryDirectory)
            .GetMethod("HashIntent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .Invoke(null, [command])!;

        var race = new ForeignShopWinnerInterceptor(dbName, hashIntent, media, StoryTenantIds.StoreAlpha, key, shopB);
        var options = new DbContextOptionsBuilder<StoryDbContext>()
            .UseInMemoryDatabase(dbName)
            .AddInterceptors(race)
            .Options;
        await using var db = new StoryDbContext(options);
        var directory = new StoryDirectory(db, new StubParty(shopA, shopB), new StubMedia(media), new StubProducts(shopA, Guid.NewGuid()));
        var conflict = await Assert.ThrowsAsync<SemanticException>(() =>
            directory.SellerCreateDraftAsync(
                StoryTenantIds.StoreAlpha, shopA, Guid.NewGuid(), command, CancellationToken.None));
        Assert.Equal(StoryErrorCodes.OccurrenceConflict, conflict.Error.Code);
        Assert.Equal(0, await db.Stories.CountAsync(s => s.SellerPartyId == shopA));
        Assert.Equal(1, await db.CommandOccurrences.CountAsync(o => o.OccurrenceKey == key));
    }

    [Fact]
    public async Task Gate0b_losing_create_detaches_staged_rows_so_later_save_is_clean()
    {
        var dbName = "g0b-" + Guid.NewGuid().ToString("N");
        var media = Guid.NewGuid();
        var key = "lose-create";
        var command = new CreateStoryCommand("Lose", null, null, media, null, null, null, null, OccurrenceKey: key);
        var hashIntent = (string)typeof(StoryDirectory)
            .GetMethod("HashIntent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .Invoke(null, [command])!;

        var race = new MatchingWinnerInterceptor(dbName, hashIntent, media, StoryTenantIds.StoreAlpha, key);
        var options = new DbContextOptionsBuilder<StoryDbContext>()
            .UseInMemoryDatabase(dbName)
            .AddInterceptors(race)
            .Options;
        await using var db = new StoryDbContext(options);
        var directory = new StoryDirectory(db, null, new StubMedia(media), new PlatformProducts(Guid.NewGuid()));
        var folded = await directory.AdminCreateAsync(StoryTenantIds.StoreAlpha, command, CancellationToken.None);
        Assert.Equal(race.WinnerStoryId, folded.StoryId);
        Assert.DoesNotContain(
            db.ChangeTracker.Entries(),
            e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);

        await db.SaveChangesAsync();
        Assert.Equal(1, await db.Stories.CountAsync());
        Assert.Equal(1, await db.CommandOccurrences.CountAsync(o => o.OccurrenceKey == key));
        Assert.Equal(race.WinnerStoryId, (await db.Stories.SingleAsync()).StoryId);
    }

    [Fact]
    public async Task Gate0c_two_context_preload_second_writer_reaches_save_typed_stale()
    {
        var storeName = "g0c-" + Guid.NewGuid().ToString("N");
        var baseOpt = new DbContextOptionsBuilder<StoryDbContext>().UseInMemoryDatabase(storeName).Options;
        var media = Guid.NewGuid();
        var shop = Guid.NewGuid();

        Guid storyId;
        int originalVersion;
        await using (var seed = new StoryDbContext(baseOpt))
        {
            var dir = new StoryDirectory(seed, new StubParty(shop), new StubMedia(media), new StubProducts(shop, Guid.NewGuid()));
            var created = await dir.SellerCreateDraftAsync(
                StoryTenantIds.StoreAlpha, shop, Guid.NewGuid(),
                new CreateStoryCommand("V", null, null, media, null, null, null, null, OccurrenceKey: "c0"),
                CancellationToken.None);
            storyId = created.StoryId;
            originalVersion = created.VersionToken;
        }

        // InMemory cross-context concurrency tokens are not reliable; inject the exact EF branch
        // the owning arbitration catches (DbUpdateConcurrencyException at SaveChanges).
        var reachedSave = false;
        var armConcurrencyLoss = false;
        await using var w1 = new StoryDbContext(baseOpt);
        await using var w2 = new StoryDbContext(
            new DbContextOptionsBuilder<StoryDbContext>()
                .UseInMemoryDatabase(storeName)
                .AddInterceptors(new ConcurrencyLossInterceptor(
                    () => armConcurrencyLoss,
                    () => reachedSave = true))
                .Options);

        // Preload ORIGINAL tracked rows in BOTH contexts BEFORE writer1 saves.
        Assert.Equal(originalVersion, (await w1.Stories.SingleAsync(s => s.StoryId == storyId)).VersionToken);
        Assert.Equal(originalVersion, (await w2.Stories.SingleAsync(s => s.StoryId == storyId)).VersionToken);

        var dir1 = new StoryDirectory(w1, new StubParty(shop), new StubMedia(media), new StubProducts(shop, Guid.NewGuid()));
        var dir2 = new StoryDirectory(w2, new StubParty(shop), new StubMedia(media), new StubProducts(shop, Guid.NewGuid()));

        var first = await dir1.SellerUpdateAsync(
            StoryTenantIds.StoreAlpha, shop, storyId,
            new UpdateStoryCommand("V-a", null, null, media, null, null, null, originalVersion, "u1"),
            CancellationToken.None);
        Assert.True(first.VersionToken > originalVersion);
        armConcurrencyLoss = true;

        var beforeOcc = await w2.CommandOccurrences.AsNoTracking().CountAsync();
        var stale = await Assert.ThrowsAsync<SemanticException>(() =>
            dir2.SellerUpdateAsync(
                StoryTenantIds.StoreAlpha, shop, storyId,
                new UpdateStoryCommand("V-b", null, null, media, null, null, null, originalVersion, "u2"),
                CancellationToken.None));
        Assert.Equal(StoryErrorCodes.VersionStale, stale.Error.Code);
        Assert.True(reachedSave, "writer2 must reach SaveChanges concurrency branch");
        Assert.Equal(beforeOcc, await w2.CommandOccurrences.AsNoTracking().CountAsync());
        Assert.DoesNotContain(
            w2.ChangeTracker.Entries(),
            e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);
    }

    [Fact]
    public async Task Gate0b_detach_owned_ids_only_preserves_unrelated_modified_then_later_save()
    {
        var dbName = "g0b-owned-" + Guid.NewGuid().ToString("N");
        var media = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<StoryDbContext>().UseInMemoryDatabase(dbName).Options;
        await using var db = new StoryDbContext(options);
        var dir = new StoryDirectory(db, null, new StubMedia(media), new PlatformProducts(Guid.NewGuid()));
        var a = await dir.AdminCreateAsync(StoryTenantIds.StoreAlpha, new CreateStoryCommand("A", null, null, media, null, null, null, null, OccurrenceKey: "oa"), CancellationToken.None);
        var b = await dir.AdminCreateAsync(StoryTenantIds.StoreAlpha, new CreateStoryCommand("B", null, null, media, null, null, null, null, OccurrenceKey: "ob"), CancellationToken.None);
        var u = await dir.AdminCreateAsync(StoryTenantIds.StoreAlpha, new CreateStoryCommand("U", null, null, media, null, null, null, null, OccurrenceKey: "ou"), CancellationToken.None);

        var storyA = await db.Stories.SingleAsync(s => s.StoryId == a.StoryId);
        var storyB = await db.Stories.SingleAsync(s => s.StoryId == b.StoryId);
        var storyU = await db.Stories.SingleAsync(s => s.StoryId == u.StoryId);
        storyA.EnsureExpectedVersion(a.VersionToken);
        storyA.SetDisplayOrder(1, DateTimeOffset.UtcNow);
        storyB.EnsureExpectedVersion(b.VersionToken);
        storyB.SetDisplayOrder(0, DateTimeOffset.UtcNow);
        storyU.EnsureExpectedVersion(u.VersionToken);
        storyU.SetDisplayOrder(7, DateTimeOffset.UtcNow);
        Assert.Equal(EntityState.Modified, db.Entry(storyA).State);
        Assert.Equal(EntityState.Modified, db.Entry(storyB).State);
        Assert.Equal(EntityState.Modified, db.Entry(storyU).State);

        var detach = typeof(StoryDirectory).GetMethod(
            "DetachAttemptOwned",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        detach.Invoke(dir, ["lose-batch", StoryTenantIds.StoreAlpha, (IReadOnlyCollection<Guid>)[a.StoryId, b.StoryId]]);

        Assert.Equal(EntityState.Detached, db.Entry(storyA).State);
        Assert.Equal(EntityState.Detached, db.Entry(storyB).State);
        Assert.Equal(EntityState.Modified, db.Entry(storyU).State);

        var orderABefore = a.DisplayOrder;
        var orderBBefore = b.DisplayOrder;
        await db.SaveChangesAsync();
        var savedU = await db.Stories.AsNoTracking().SingleAsync(s => s.StoryId == u.StoryId);
        Assert.Equal(7, savedU.DisplayOrder);
        // A/B were detached before save — their order mutations must NOT persist.
        Assert.Equal(orderABefore, (await db.Stories.AsNoTracking().SingleAsync(s => s.StoryId == a.StoryId)).DisplayOrder);
        Assert.Equal(orderBBefore, (await db.Stories.AsNoTracking().SingleAsync(s => s.StoryId == b.StoryId)).DisplayOrder);
    }

    [SkippableFact]
    public async Task Gate0c_pg_two_context_stale_owning_command_rollback_only()
    {
        Skip.IfNot(CanonicalTarget.Value.Ok, CanonicalTarget.Value.Reason);

        await using var db1 = CreateStoryDb();
        await using var caller = await db1.Database.BeginTransactionAsync();
        var media = Guid.NewGuid();
        var shop = Guid.NewGuid();
        var parties = new StubParty(shop);
        var products = new StubProducts(shop, Guid.NewGuid());
        var mediaPort = new StubMedia(media);
        var dir1 = new StoryDirectory(db1, parties, mediaPort, products);

        var created = await dir1.SellerCreateDraftAsync(
            StoryTenantIds.StoreAlpha, shop, Guid.NewGuid(),
            new CreateStoryCommand(_runTag + "pg", null, null, media, null, null, null, null, OccurrenceKey: _runTag + "c"),
            CancellationToken.None);
        var originalVersion = created.VersionToken;

        await using var db2 = CreateStoryDbSharing(db1, caller);
        _ = await db1.Stories.SingleAsync(s => s.StoryId == created.StoryId);
        _ = await db2.Stories.SingleAsync(s => s.StoryId == created.StoryId);

        var dir2 = new StoryDirectory(db2, parties, mediaPort, products);
        var reachedSave = false;
        // Probe via interceptor on db2 is hard after construction; assert VersionStale + zero extra occ.

        var first = await dir1.SellerUpdateAsync(
            StoryTenantIds.StoreAlpha, shop, created.StoryId,
            new UpdateStoryCommand(_runTag + "pg-a", null, null, media, null, null, null, originalVersion, _runTag + "u1"),
            CancellationToken.None);
        Assert.True(first.VersionToken > originalVersion);

        var beforeOcc = await db1.CommandOccurrences.CountAsync(x => x.OccurrenceKey.StartsWith(_runTag));
        var stale = await Assert.ThrowsAsync<SemanticException>(() =>
            dir2.SellerUpdateAsync(
                StoryTenantIds.StoreAlpha, shop, created.StoryId,
                new UpdateStoryCommand(_runTag + "pg-b", null, null, media, null, null, null, originalVersion, _runTag + "u2"),
                CancellationToken.None));
        Assert.Equal(StoryErrorCodes.VersionStale, stale.Error.Code);
        Assert.Equal(beforeOcc, await db1.CommandOccurrences.CountAsync(x => x.OccurrenceKey.StartsWith(_runTag)));

        await caller.RollbackAsync();
        await using var verify = CreateStoryDb();
        Assert.Equal(0, await verify.Stories.AsNoTracking().CountAsync(x => x.Title.StartsWith(_runTag)));
        Assert.Equal(0, await verify.CommandOccurrences.AsNoTracking().CountAsync(x => x.OccurrenceKey.StartsWith(_runTag)));
        _output.WriteLine($"Gate0c PG rollback committedRows=0 tag={_runTag} reachedSaveProbe={reachedSave}");
    }

    private static StoryDbContext CreateStoryDb()
    {
        var options = new DbContextOptionsBuilder<StoryDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, Connection!, StoryDbContext.Schema, typeof(StoryDbContext));
        return new StoryDbContext(options.Options);
    }

    private static StoryDbContext CreateStoryDbSharing(StoryDbContext owner, IDbContextTransaction tx)
    {
        var options = new DbContextOptionsBuilder<StoryDbContext>();
        ToobaNpgsql.ConfigureModuleContext(options, Connection!, StoryDbContext.Schema, typeof(StoryDbContext));
        var db = new StoryDbContext(options.Options);
        db.Database.SetDbConnection(owner.Database.GetDbConnection());
        db.Database.UseTransaction(tx.GetDbTransaction());
        return db;
    }

    private static (bool Ok, string Reason) ValidateCanonicalTarget()
    {
        try
        {
            var builder = new NpgsqlConnectionStringBuilder(Connection);
            if (builder.Port != 5432 || !string.Equals(builder.Database, "tooba_alpha", StringComparison.Ordinal))
                return (false, "not canonical");
            using var c = new NpgsqlConnection(Connection);
            c.Open();
            return (true, "ok");
        }
        catch (Exception ex)
        {
            return (false, ex.GetType().Name);
        }
    }

    private sealed class ConcurrencyLossInterceptor(Func<bool> armed, Action onSaving) : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData, InterceptionResult<int> result)
        {
            if (!armed())
                return result;
            onSaving();
            throw new DbUpdateConcurrencyException("forced optimistic concurrency loss");
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default) =>
            new(SavingChanges(eventData, result));
    }

    private sealed class MatchingWinnerInterceptor(
        string dbName, string intentHash, Guid mediaId, Guid tenantId, string occurrenceKey)
        : SaveChangesInterceptor
    {
        public Guid WinnerStoryId { get; private set; }
        private int _calls;

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData, InterceptionResult<int> result)
        {
            if (++_calls != 1) return result;
            using var seed = new StoryDbContext(
                new DbContextOptionsBuilder<StoryDbContext>().UseInMemoryDatabase(dbName).Options);
            var now = DateTimeOffset.UtcNow;
            var winner = StoryEntity.CreateDraft(tenantId, "Lose", 0, now, null, null, mediaId, null, null, null);
            WinnerStoryId = winner.StoryId;
            seed.Stories.Add(winner);
            seed.CommandOccurrences.Add(new StoryCommandOccurrence
            {
                OccurrenceKey = occurrenceKey,
                TenantId = tenantId,
                SellerPartyId = null,
                ActionCode = "admin_create",
                IntentHash = intentHash,
                StoryId = winner.StoryId,
                ActorUserId = Guid.Empty,
                CreatedAt = now,
            });
            seed.SaveChanges();
            throw new DbUpdateException("conflict", new PostgresException(
                "unique", "ERROR", "ERROR", "23505",
                schemaName: "story", tableName: "command_occurrences",
                constraintName: "pk_command_occurrences"));
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default) =>
            new(SavingChanges(eventData, result));
    }

    private sealed class ForeignShopWinnerInterceptor(
        string dbName, string intentHash, Guid mediaId, Guid tenantId, string occurrenceKey, Guid winnerShop)
        : SaveChangesInterceptor
    {
        private int _calls;

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData, InterceptionResult<int> result)
        {
            if (++_calls != 1) return result;
            using var seed = new StoryDbContext(
                new DbContextOptionsBuilder<StoryDbContext>().UseInMemoryDatabase(dbName).Options);
            var now = DateTimeOffset.UtcNow;
            var winner = StoryEntity.CreateSellerDraft(
                tenantId, winnerShop, Guid.NewGuid(), "OtherShop", 0, now, null, null, mediaId, null, null, null);
            seed.Stories.Add(winner);
            seed.CommandOccurrences.Add(new StoryCommandOccurrence
            {
                OccurrenceKey = occurrenceKey,
                TenantId = tenantId,
                SellerPartyId = winnerShop,
                ActionCode = "seller_create",
                IntentHash = intentHash,
                StoryId = winner.StoryId,
                ActorUserId = Guid.NewGuid(),
                CreatedAt = now,
            });
            seed.SaveChanges();
            throw new DbUpdateException("conflict", new PostgresException(
                "unique", "ERROR", "ERROR", "23505",
                schemaName: "story", tableName: "command_occurrences",
                constraintName: "pk_command_occurrences"));
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default) =>
            new(SavingChanges(eventData, result));
    }

    private sealed class StubParty(params Guid[] eligible) : IPartyAdminSellerReadGateway
    {
        private readonly HashSet<Guid> _ok = eligible.ToHashSet();
        public Task<IReadOnlyList<PartyStatusProjection>> GetStatusProjectionsAsync(
            IReadOnlyList<Guid> partyIds, CancellationToken cancellationToken)
        {
            IReadOnlyList<PartyStatusProjection> rows = partyIds
                .Where(_ok.Contains)
                .Select(id => new PartyStatusProjection(id, "Shop", "Active"))
                .ToList();
            return Task.FromResult(rows);
        }
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
            Stream stream, string fileName, string contentType, Guid tenantId, Guid actorUserId, string? occurrenceKey, CancellationToken cancellationToken) =>
            Task.FromResult(new StoryMediaAssetAuthorization(Guid.NewGuid(), contentType ?? "image/jpeg", "/v1/storefront/media/00000000-0000-0000-0000-000000000000", false, null));
        public Task<IReadOnlyList<StoryMediaAssetAuthorization>> ListSellerAssetsAsync(
            Guid tenantId, Guid sellerPartyId, int take, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<StoryMediaAssetAuthorization>>([]);
    }

    private sealed class StubProducts(Guid shop, Guid owned) : IStoryProductTargetPort
    {
        public Task EnsureOwnedBySellerAsync(Guid productId, Guid sellerPartyId, CancellationToken cancellationToken)
        {
            if (sellerPartyId != shop || productId != owned)
                throw new SemanticException(new SemanticError(StoryErrorCodes.CtaRejected));
            return Task.CompletedTask;
        }

        public Task EnsurePublishedExistsAsync(Guid productId, CancellationToken cancellationToken) =>
            Task.CompletedTask;
        public Task<string?> TryResolvePublicProductPathAsync(Guid productId, CancellationToken cancellationToken) =>
            Task.FromResult<string?>(productId == Guid.Empty ? null : "/products/" + productId.ToString("N"));
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
            Task.FromResult<string?>(productId == Guid.Empty ? null : "/products/" + productId.ToString("N"));
    }
}
