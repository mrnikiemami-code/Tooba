using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Tooba.BuildingBlocks;
using Tooba.Media.Contracts.Assets;
using Tooba.Party.Contracts.Ports;
using Tooba.Story.Application.Stories.Models;
using Tooba.Story.Application.Stories.Ports;
using Tooba.Story.Contracts.Errors;
using Tooba.Story.Domain.Aggregates;
using Tooba.Story.Domain.Tenant;
using Tooba.Story.Infrastructure.Directories;
using Tooba.Story.Infrastructure.Persistence;
using Xunit;
using StoryEntity = Tooba.Story.Domain.Aggregates.Story;

namespace Tooba.Host.Tests;

/// <summary>TB-281-R5 C1: owning-directory counterexamples for A1–A4 replay/hash/arbitration.</summary>
public sealed class StoryMarketplace281R5ReplayTests
{
    [Fact]
    public async Task A1_seller_and_item_updates_require_occurrence_replay_and_conflict()
    {
        await using var db = CreateDb();
        var shop = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        var directory = new StoryDirectory(db, new StubParty(shop), new StubMedia(mediaId), new StubProducts(shop, Guid.NewGuid()));
        var created = await directory.SellerCreateDraftAsync(
            StoryTenantIds.StoreAlpha, shop, Guid.NewGuid(),
            new CreateStoryCommand("T1", null, null, mediaId, null, null, null, null, OccurrenceKey: "c-create"),
            CancellationToken.None);
        var withItem = await directory.SellerAddItemAsync(
            StoryTenantIds.StoreAlpha, shop, created.StoryId,
            new AddStoryItemCommand("image", mediaId, null, null, null, null, null, null, "c-add", created.VersionToken),
            CancellationToken.None);
        var item = withItem.Items.Single();

        var missing = await Assert.ThrowsAsync<SemanticException>(() =>
            directory.SellerUpdateAsync(
                StoryTenantIds.StoreAlpha, shop, created.StoryId,
                new UpdateStoryCommand("T2", null, null, mediaId, null, null, null, withItem.VersionToken, null),
                CancellationToken.None));
        Assert.Equal(StoryErrorCodes.OccurrenceRequired, missing.Error.Code);

        var v1 = withItem.VersionToken;
        var updated = await directory.SellerUpdateAsync(
            StoryTenantIds.StoreAlpha, shop, created.StoryId,
            new UpdateStoryCommand("T2", "fa", "IR", mediaId, null, null, null, v1, "upd-1"),
            CancellationToken.None);
        Assert.Equal("T2", updated.Title);

        // Same key/body after version advanced: exact replay, no further write.
        var replay = await directory.SellerUpdateAsync(
            StoryTenantIds.StoreAlpha, shop, created.StoryId,
            new UpdateStoryCommand("T2", "fa", "IR", mediaId, null, null, null, v1, "upd-1"),
            CancellationToken.None);
        Assert.Equal(updated.VersionToken, replay.VersionToken);
        Assert.Equal(1, await db.CommandOccurrences.CountAsync(o => o.OccurrenceKey == "upd-1"));

        var conflict = await Assert.ThrowsAsync<SemanticException>(() =>
            directory.SellerUpdateAsync(
                StoryTenantIds.StoreAlpha, shop, created.StoryId,
                new UpdateStoryCommand("T3", "fa", "IR", mediaId, null, null, null, v1, "upd-1"),
                CancellationToken.None));
        Assert.Equal(StoryErrorCodes.OccurrenceConflict, conflict.Error.Code);

        var itemMissing = await Assert.ThrowsAsync<SemanticException>(() =>
            directory.AdminUpdateItemAsync(
                StoryTenantIds.StoreAlpha, created.StoryId, item.StoryItemId,
                new UpdateStoryItemCommand("image", mediaId, null, "cap", null, null, null, updated.VersionToken, null),
                CancellationToken.None));
        Assert.Equal(StoryErrorCodes.OccurrenceRequired, itemMissing.Error.Code);

        var itemOk = await directory.SellerUpdateItemAsync(
            StoryTenantIds.StoreAlpha, shop, created.StoryId, item.StoryItemId,
            new UpdateStoryItemCommand("image", mediaId, null, "cap", null, null, null, updated.VersionToken, "item-upd-1"),
            CancellationToken.None);
        Assert.Equal("cap", itemOk.Items.Single().Caption);

        var itemReplay = await directory.SellerUpdateItemAsync(
            StoryTenantIds.StoreAlpha, shop, created.StoryId, item.StoryItemId,
            new UpdateStoryItemCommand("image", mediaId, null, "cap", null, null, null, updated.VersionToken, "item-upd-1"),
            CancellationToken.None);
        Assert.Equal(itemOk.VersionToken, itemReplay.VersionToken);

        var otherItem = (await directory.SellerAddItemAsync(
            StoryTenantIds.StoreAlpha, shop, created.StoryId,
            new AddStoryItemCommand("image", mediaId, null, null, null, null, null, null, "c-add-2", itemOk.VersionToken),
            CancellationToken.None)).Items.Last();
        var cross = await Assert.ThrowsAsync<SemanticException>(() =>
            directory.SellerUpdateItemAsync(
                StoryTenantIds.StoreAlpha, shop, created.StoryId, otherItem.StoryItemId,
                new UpdateStoryItemCommand("image", mediaId, null, "cap", null, null, null, updated.VersionToken, "item-upd-1"),
                CancellationToken.None));
        Assert.Equal(StoryErrorCodes.OccurrenceConflict, cross.Error.Code);
    }

    [Fact]
    public async Task A2_update_hash_conflicts_on_locale_market_cover_and_delimiter_pair()
    {
        await using var db = CreateDb();
        var shop = Guid.NewGuid();
        var mediaA = Guid.NewGuid();
        var mediaB = Guid.NewGuid();
        var directory = new StoryDirectory(
            db, new StubParty(shop), new StubMedia(mediaA, mediaB), new StubProducts(shop, Guid.NewGuid()));
        var created = await directory.SellerCreateDraftAsync(
            StoryTenantIds.StoreAlpha, shop, Guid.NewGuid(),
            new CreateStoryCommand("Title", "fa", "IR", mediaA, null, null, null, null, OccurrenceKey: "c1"),
            CancellationToken.None);
        var v = created.VersionToken;
        _ = await directory.SellerUpdateAsync(
            StoryTenantIds.StoreAlpha, shop, created.StoryId,
            new UpdateStoryCommand("Title", "fa", "IR", mediaA, null, null, null, v, "hash-1"),
            CancellationToken.None);

        async Task AssertConflict(UpdateStoryCommand cmd)
        {
            var ex = await Assert.ThrowsAsync<SemanticException>(() =>
                directory.SellerUpdateAsync(StoryTenantIds.StoreAlpha, shop, created.StoryId, cmd, CancellationToken.None));
            Assert.Equal(StoryErrorCodes.OccurrenceConflict, ex.Error.Code);
        }

        await AssertConflict(new UpdateStoryCommand("Title", "en", "IR", mediaA, null, null, null, v, "hash-1"));
        await AssertConflict(new UpdateStoryCommand("Title", "fa", "US", mediaA, null, null, null, v, "hash-1"));
        await AssertConflict(new UpdateStoryCommand("Title", "fa", "IR", mediaB, null, null, null, v, "hash-1"));
        await AssertConflict(new UpdateStoryCommand("Title", "fa", "IR", mediaA, "https://x.test/a", null, null, v, "hash-1"));

        // Delimiter pair: two intents that share pipe characters but differ under length-prefix encoding.
        var current = await directory.SellerGetAsync(StoryTenantIds.StoreAlpha, shop, created.StoryId, CancellationToken.None);
        var v2 = current!.VersionToken;
        _ = await directory.SellerUpdateAsync(
            StoryTenantIds.StoreAlpha, shop, created.StoryId,
            new UpdateStoryCommand("a|b", "fa", "IR", mediaA, null, null, null, v2, "delim-1"),
            CancellationToken.None);
        var delimConflict = await Assert.ThrowsAsync<SemanticException>(() =>
            directory.SellerUpdateAsync(
                StoryTenantIds.StoreAlpha, shop, created.StoryId,
                new UpdateStoryCommand("a", "fa", "IR", mediaA, null, null, "b", v2, "delim-1"),
                CancellationToken.None));
        Assert.Equal(StoryErrorCodes.OccurrenceConflict, delimConflict.Error.Code);
    }

    [Fact]
    public async Task A3_schedule_replays_original_after_success_and_refuses_stale_new_key()
    {
        await using var db = CreateDb();
        var shop = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        var admin = Guid.NewGuid();
        var directory = new StoryDirectory(db, new StubParty(shop), new StubMedia(mediaId), new StubProducts(shop, Guid.NewGuid()));
        var created = await directory.SellerCreateDraftAsync(
            StoryTenantIds.StoreAlpha, shop, admin,
            new CreateStoryCommand("P", null, null, mediaId, null, null, null, null, OccurrenceKey: "ac"),
            CancellationToken.None);
        created = await directory.SellerAddItemAsync(
            StoryTenantIds.StoreAlpha, shop, created.StoryId,
            new AddStoryItemCommand("image", mediaId, null, null, null, null, null, null, "ai", created.VersionToken),
            CancellationToken.None);
        created = await directory.SellerSubmitAsync(
            StoryTenantIds.StoreAlpha, shop, created.StoryId, admin, CancellationToken.None, created.VersionToken, "sub");
        created = await directory.AdminApproveAndPublishAsync(
            StoryTenantIds.StoreAlpha, created.StoryId, admin, created.VersionToken, CancellationToken.None, "ap");

        var start = DateTimeOffset.Parse("2026-12-01T00:00:00Z");
        var end = DateTimeOffset.Parse("2026-12-02T00:00:00Z");
        var reviewedVersion = created.VersionToken;
        var scheduled = await directory.AdminSetScheduleAsync(
            StoryTenantIds.StoreAlpha, created.StoryId,
            new SetStoryScheduleCommand(start, end, reviewedVersion, "sched-1"),
            CancellationToken.None);
        Assert.True(scheduled.VersionToken > reviewedVersion);

        var replay = await directory.AdminSetScheduleAsync(
            StoryTenantIds.StoreAlpha, created.StoryId,
            new SetStoryScheduleCommand(start, end, reviewedVersion, "sched-1"),
            CancellationToken.None);
        Assert.Equal(scheduled.VersionToken, replay.VersionToken);
        Assert.Equal(start, replay.StartAt);

        var timeConflict = await Assert.ThrowsAsync<SemanticException>(() =>
            directory.AdminSetScheduleAsync(
                StoryTenantIds.StoreAlpha, created.StoryId,
                new SetStoryScheduleCommand(start.AddHours(1), end, reviewedVersion, "sched-1"),
                CancellationToken.None));
        Assert.Equal(StoryErrorCodes.OccurrenceConflict, timeConflict.Error.Code);

        var stale = await Assert.ThrowsAsync<SemanticException>(() =>
            directory.AdminSetScheduleAsync(
                StoryTenantIds.StoreAlpha, created.StoryId,
                new SetStoryScheduleCommand(start, end, reviewedVersion, "sched-new"),
                CancellationToken.None));
        Assert.Equal(StoryErrorCodes.VersionStale, stale.Error.Code);
    }

    [Fact]
    public void A4_occurrence_unique_identity_requires_exact_schema_table_constraint()
    {
        var method = typeof(StoryDirectory).GetMethod(
            "IsCommandOccurrenceUniqueViolation",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);

        DbUpdateException Make(string schema, string table, string constraint) =>
            new("conflict", new Npgsql.PostgresException(
                "unique", "ERROR", "ERROR", "23505",
                schemaName: schema, tableName: table, constraintName: constraint));

        Assert.True((bool)method!.Invoke(null, [Make("story", "command_occurrences", "pk_command_occurrences")])!);
        Assert.False((bool)method.Invoke(null, [Make("story", "stories", "pk_stories")])!);
        Assert.False((bool)method.Invoke(null, [Make("media", "command_occurrences", "pk_command_occurrences")])!);
    }

    [Fact]
    public async Task A4_occurrence_pk_collision_folds_to_winner_one_row()
    {
        var dbName = "r5-a4w-" + Guid.NewGuid().ToString("N");
        var media = Guid.NewGuid();
        var key = "race-fold-key";
        var command = new CreateStoryCommand("WinnerTitle", null, null, media, null, null, null, null, OccurrenceKey: key);
        var hashIntent = typeof(StoryDirectory)
            .GetMethod("HashIntent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .Invoke(null, [command]) as string
            ?? throw new InvalidOperationException("HashIntent missing");

        var race = new RaceWinnerInterceptor(dbName, hashIntent, media, StoryTenantIds.StoreAlpha, key);
        var options = new DbContextOptionsBuilder<StoryDbContext>()
            .UseInMemoryDatabase(dbName)
            .AddInterceptors(race)
            .Options;
        await using var db = new StoryDbContext(options);
        var directory = new StoryDirectory(db, null, new StubMedia(media), new PlatformProducts(Guid.NewGuid()));
        var folded = await directory.AdminCreateAsync(StoryTenantIds.StoreAlpha, command, CancellationToken.None);
        Assert.Equal(race.WinnerStoryId, folded.StoryId);
        Assert.Equal(1, await db.CommandOccurrences.CountAsync(o => o.OccurrenceKey == key));
        Assert.Equal("WinnerTitle", folded.Title);
    }

    [Fact]
    public async Task A4_unrelated_unique_on_save_propagates_without_winner_fold()
    {
        var throwUnique = new ThrowOccurrenceUniqueInterceptor
        {
            Mode = ThrowOccurrenceUniqueInterceptor.ThrowMode.UnrelatedUnique
        };
        var options = new DbContextOptionsBuilder<StoryDbContext>()
            .UseInMemoryDatabase("r5-a4u-" + Guid.NewGuid().ToString("N"))
            .AddInterceptors(throwUnique)
            .Options;
        await using var db = new StoryDbContext(options);
        var media = Guid.NewGuid();
        var directory = new StoryDirectory(db, null, new StubMedia(media), new PlatformProducts(Guid.NewGuid()));
        await Assert.ThrowsAsync<DbUpdateException>(() =>
            directory.AdminCreateAsync(
                StoryTenantIds.StoreAlpha,
                new CreateStoryCommand("X", null, null, media, null, null, null, null, OccurrenceKey: "other-key"),
                CancellationToken.None));
        Assert.Equal(0, await db.CommandOccurrences.CountAsync());
    }

    private static StoryDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<StoryDbContext>()
            .UseInMemoryDatabase("r5-" + Guid.NewGuid().ToString("N")).Options);

    private sealed class ThrowOccurrenceUniqueInterceptor : SaveChangesInterceptor
    {
        public enum ThrowMode { OccurrencePk, UnrelatedUnique }
        public ThrowMode Mode { get; set; } = ThrowMode.OccurrencePk;
        private int _calls;

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData, InterceptionResult<int> result)
        {
            _calls++;
            if (_calls == 1 && Mode == ThrowMode.OccurrencePk)
            {
                var postgres = new Npgsql.PostgresException(
                    "unique", "ERROR", "ERROR", "23505",
                    schemaName: "story", tableName: "command_occurrences",
                    constraintName: "pk_command_occurrences");
                throw new DbUpdateException("conflict", postgres);
            }

            if (Mode == ThrowMode.UnrelatedUnique)
            {
                var postgres = new Npgsql.PostgresException(
                    "unique", "ERROR", "ERROR", "23505",
                    schemaName: "story", tableName: "stories",
                    constraintName: "pk_stories");
                throw new DbUpdateException("conflict", postgres);
            }

            return result;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default) =>
            new(SavingChanges(eventData, result));
    }

    /// <summary>
    /// Inserts the winner occurrence into the shared InMemory store after the attempt passed
    /// TryReplay but before its SaveChanges commits, then injects exact pk_command_occurrences 23505.
    /// </summary>
    private sealed class RaceWinnerInterceptor(
        string dbName, string intentHash, Guid mediaId, Guid tenantId, string occurrenceKey)
        : SaveChangesInterceptor
    {
        public Guid WinnerStoryId { get; private set; }
        private int _calls;

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData, InterceptionResult<int> result)
        {
            if (++_calls != 1)
                return result;

            var seedOptions = new DbContextOptionsBuilder<StoryDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            using var seed = new StoryDbContext(seedOptions);
            var now = DateTimeOffset.UtcNow;
            var winner = StoryEntity.CreateDraft(
                tenantId, "WinnerTitle", 0, now, null, null, mediaId, null, null, null);
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

            var postgres = new Npgsql.PostgresException(
                "unique", "ERROR", "ERROR", "23505",
                schemaName: "story", tableName: "command_occurrences",
                constraintName: "pk_command_occurrences");
            throw new DbUpdateException("conflict", postgres);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default) =>
            new(SavingChanges(eventData, result));
    }

    private sealed class StubParty(Guid eligible) : IPartyAdminSellerReadGateway
    {
        public Task<IReadOnlyList<PartyStatusProjection>> GetStatusProjectionsAsync(
            IReadOnlyList<Guid> partyIds, CancellationToken cancellationToken)
        {
            IReadOnlyList<PartyStatusProjection> rows = partyIds
                .Where(id => id == eligible)
                .Select(id => new PartyStatusProjection(id, "Shop", "Active"))
                .ToList();
            return Task.FromResult(rows);
        }
    }

    private sealed class StubMedia : IStoryMediaAssetPort
    {
        private readonly HashSet<Guid> _ok;
        public StubMedia(params Guid[] ok) => _ok = ok.ToHashSet();

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

        public Task EnsurePublishedExistsAsync(Guid productId, CancellationToken cancellationToken) =>
            Task.CompletedTask;
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
    }
}
