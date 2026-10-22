using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application.Shipping.Models;
using Tooba.Fulfillment.Contracts.Errors;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Fulfillment.Infrastructure.Shipping;
using Xunit;
using Xunit.Abstractions;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R13 — real PostgreSQL proof for the existing Fulfillment type
/// catalog on a CHRONOLOGY-CORRECT fixture: pre-type checkpoint, historical type Up, accepted pending-index Up,
/// strict current-chain-versus-current-model equality, real inverse Down, real storage invariants, strict
/// original-token and unique races, write-only mid-save rollback/caller cancellation and exact constraint
/// ownership.
/// </summary>
/// <remarks>
/// Only the clock, id generator and prepared language facts are fixtures; store, context, mappings and the
/// checked-in migrations are production code. This is bounded storage/schema evidence, not production
/// functional acceptance.
/// </remarks>
[Collection(nameof(TypeCatalogPostgresCollection))]
public sealed class ShippingTypePostgresStorageTests(
    TypeCatalogPostgresSchemaFixture schema,
    ITestOutputHelper output)
{
    private static readonly Guid Fa = Guid.Parse("01900000-0000-7000-8000-00000000cc01");
    private static readonly Guid En = Guid.Parse("01900000-0000-7000-8000-00000000cc02");
    private static readonly Guid Ar = Guid.Parse("01900000-0000-7000-8000-00000000cc03");

    // ---------------------------------------------------------------------------------------------
    // Target isolation (offline negatives, no connection)
    // ---------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("Host=10.0.0.5;Port=55436;Database=fulfillment_tb196;Username=tb196;Password=x")]
    [InlineData("Host=127.0.0.1,127.0.0.2;Port=55436;Database=fulfillment_tb196;Username=tb196;Password=x")]
    [InlineData("Host=localhost;Port=55436;Database=fulfillment_tb196;Username=tb196;Password=x")]
    [InlineData("Host=127.0.0.1;Port=5432;Database=fulfillment_tb196;Username=tb196;Password=x")]
    [InlineData("Host=127.0.0.1;Port=55436;Database=tooba_alpha;Username=tb196;Password=x")]
    [InlineData("Host=127.0.0.1;Port=55436;Database=fulfillment_tb196;Username=postgres;Password=x")]
    [InlineData("Host=127.0.0.1;Port=55436;Database=postgres;Username=tb196;Password=x")]
    public void Foreign_multi_host_shared_port_shared_database_and_wrong_role_targets_fail_closed(
        string connectionString)
    {
        Assert.ThrowsAny<Exception>(() => TypeCatalogPostgresSchemaFixture.ValidateTarget(connectionString));
    }

    [Fact]
    public void The_parsed_target_is_exactly_the_task_owned_loopback_endpoint()
    {
        Require();
        Assert.Equal("127.0.0.1", schema.Target.Host);
        Assert.Equal(55436, schema.Target.Port);
        Assert.Equal("tb196", schema.Target.Username);
        Assert.Equal(TypeCatalogPostgresSchemaFixture.PrimaryDatabase, schema.Target.Database);
        Assert.Equal(TypeCatalogPostgresSchemaFixture.ModelDatabase, schema.ModelTarget.Database);
    }

    // ---------------------------------------------------------------------------------------------
    // GATE 1 — offline metadata gate, phased historical additivity, strict current parity
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Snapshot_and_actual_latest_target_model_both_equal_the_design_time_model()
    {
        Require();
        Assert.True(schema.SnapshotEqualsCurrentModel,
            $"snapshot must equal design-time model; differences: {string.Join("; ", schema.ModelDifferences)}");
        Assert.Empty(schema.ModelDifferences);
        Assert.Empty(schema.LatestTargetModelDifferences);
        Assert.Equal(TypeCatalogPostgresSchemaFixture.LatestMigration, schema.ActualLatestMigration);
        output.WriteLine(
            $"snapshot=0 latest-target-model=0 actual-latest={schema.ActualLatestMigration}");
    }

    [Fact]
    public void Metadata_equivalence_gate_stops_on_any_snapshot_or_latest_target_model_difference()
    {
        TypeCatalogPostgresSchemaFixture.VerifyMetadataEquivalence(true, [], []);

        var snapshotStop = Assert.Throws<InvalidOperationException>(
            () => TypeCatalogPostgresSchemaFixture.VerifyMetadataEquivalence(false, ["CreateTableOperation"], []));
        Assert.Contains("UNPROVEN", snapshotStop.Message, StringComparison.Ordinal);

        var latestStop = Assert.Throws<InvalidOperationException>(
            () => TypeCatalogPostgresSchemaFixture.VerifyMetadataEquivalence(true, [], ["AlterColumnOperation"]));
        Assert.Contains("latest TargetModel differences=1", latestStop.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Historical_type_up_is_additive_and_adds_exactly_the_two_owned_tables()
    {
        Require();
        Assert.Empty(schema.RemovedByTypeUp);
        var added = schema.AddedByTypeUp;
        Assert.NotEmpty(added);
        Assert.Equal(schema.ExpectedTypeOwnedObjects, added);
        Assert.All(added, line => Assert.True(
            TypeCatalogPostgresSchemaFixture.IsTypeOwned(line),
            $"the type Up may add only objects owned by the two type tables but added: {line}"));

        var owners = added.Select(TypeCatalogPostgresSchemaFixture.OwningTable).Distinct(StringComparer.Ordinal).ToList();
        Assert.Equal(
            new[] { TypeCatalogPostgresSchemaFixture.TypeTable, TypeCatalogPostgresSchemaFixture.TranslationTable }
                .OrderBy(x => x, StringComparer.Ordinal),
            owners.OrderBy(x => x, StringComparer.Ordinal));

        // every pre-existing (legacy) object is still present, byte-identical
        Assert.All(schema.LegacyManifestBeforeUp, line => Assert.Contains(line, schema.ManifestAfterTypeUp));
        Assert.Equal(
            schema.LegacyManifestBeforeUp.Count,
            schema.ManifestAfterTypeUp.Count(line => !TypeCatalogPostgresSchemaFixture.IsTypeOwned(line)));

        output.WriteLine($"type-owned added={added.Count} owners={string.Join(",", owners)}");
    }

    [Fact]
    public void Accepted_pending_index_up_adds_only_that_one_index_on_the_outbox_table()
    {
        Require();
        Assert.True(schema.LatestUpApplied);
        Assert.Empty(schema.RemovedByLatestUp);
        var added = Assert.Single(schema.AddedByLatestUp);
        Assert.Contains(TypeCatalogPostgresSchemaFixture.PendingIndexName, added, StringComparison.Ordinal);
        Assert.Equal(
            TypeCatalogPostgresSchemaFixture.OutboxTable,
            TypeCatalogPostgresSchemaFixture.OwningTable(added));
        Assert.Equal(
            schema.ManifestAfterTypeUp.Count,
            schema.ManifestAfterLatestUp.Count - 1);
        output.WriteLine($"index added: {added}");
    }

    [Fact]
    public void Current_real_chain_matches_the_canonical_model_exactly_with_no_classified_exception()
    {
        Require();
        Assert.Empty(schema.CurrentChainVersusModelDifferences);
        Assert.Equal(schema.ModelManifest, schema.ManifestAfterLatestUp);
        // the two type tables and the pending index must be present on both sides at CURRENT chain
        Assert.Contains(schema.ManifestAfterLatestUp,
            line => TypeCatalogPostgresSchemaFixture.OwningTable(line) == TypeCatalogPostgresSchemaFixture.TypeTable);
        Assert.Contains(schema.ManifestAfterLatestUp,
            line => line.Contains(TypeCatalogPostgresSchemaFixture.PendingIndexName, StringComparison.Ordinal));
        output.WriteLine(
            $"current-chain={schema.ManifestAfterLatestUp.Count} model={schema.ModelManifest.Count} differences=0");
    }

    // ---------------------------------------------------------------------------------------------
    // GATE 2 — own storage / fresh context
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public async Task Create_read_activate_deactivate_and_revision_semantics_survive_fresh_contexts()
    {
        Require();
        await using (var seeding = schema.NewContext())
        {
            var store = schema.NewStore(seeding);
            var id = await store.CreateAsync("ordinary_type", 0, [T(Fa, "عادی", "توضیح")], CancellationToken.None);

            await using var readContext = schema.NewContext();
            var created = await schema.NewStore(readContext).GetAsync(id, Fa, CancellationToken.None);
            Assert.NotNull(created);
            Assert.False(created!.IsActive);
            Assert.Equal(1, created.Revision);
            Assert.Equal(0, created.SortOrder);
            Assert.Equal("ordinary_type", created.Code);
            Assert.Equal("عادی", Translation(created, Fa).Name);
            Assert.Equal("توضیح", Translation(created, Fa).Description);

            await using var readerContext = schema.NewContext();
            var snapshot = Assert.Single(
                await schema.NewReader(readerContext).GetByIdsAsync([id], CancellationToken.None));
            Assert.Equal("ordinary_type", snapshot.Code);
            Assert.False(snapshot.IsActive);

            var activated = await store.UpdateAsync(id, 1, null, 0, true, [T(Fa, "عادی", "توضیح")], Fa,
                CancellationToken.None);
            Assert.True(activated.IsActive);
            Assert.Equal(2, activated.Revision);

            var deactivated = await store.UpdateAsync(id, 2, null, 0, false, [T(Fa, "عادی", "توضیح")], Fa,
                CancellationToken.None);
            Assert.False(deactivated.IsActive);
            Assert.Equal(3, deactivated.Revision);

            var reactivated = await store.UpdateAsync(id, 3, null, 0, true, [T(Fa, "عادی", "توضیح")], Fa,
                CancellationToken.None);
            Assert.True(reactivated.IsActive);
            Assert.Equal(4, reactivated.Revision);

            var multilingual = await store.UpdateAsync(id, 4, null, 0, true,
                [T(Fa, "عادی", "توضیح"), T(En, "Ordinary", "English")], Fa, CancellationToken.None);
            Assert.Equal("عادی", Translation(multilingual, Fa).Name);

            var edited = await store.UpdateAsync(id, 5, null, 0, true,
                [T(En, "Ordinary v2", "English v2")], Fa, CancellationToken.None);
            Assert.Equal("Ordinary v2", Translation(edited, En).Name);
            Assert.Equal("عادی", Translation(edited, Fa).Name);
            Assert.Equal("توضیح", Translation(edited, Fa).Description);

            var before = await schema.ReadRawAsync(id);
            schema.Clock.Advance(TimeSpan.FromMinutes(5));
            var repeated = await store.UpdateAsync(id, 6, null, 0, true,
                [T(En, "Ordinary v2", "English v2")], Fa, CancellationToken.None);
            var after = await schema.ReadRawAsync(id);

            Assert.Equal(6, repeated.Revision);
            Assert.Equal("6", after.Revision);
            Assert.Equal(before.UpdatedAt, after.UpdatedAt);
            Assert.Equal(before.CreatedAt, after.CreatedAt);
            Assert.NotEqual(schema.Clock.UtcNow.ToString("O"), after.UpdatedAt);

            var stale = await Assert.ThrowsAsync<SemanticException>(() => store.UpdateAsync(id, 5, null, 0, true,
                [T(En, "Ordinary v2", "English v2")], Fa, CancellationToken.None));
            Assert.Equal(FulfillmentErrorCodes.ShippingTypeRevisionConflict, stale.Error.Code);
            Assert.Equal(after.UpdatedAt, (await schema.ReadRawAsync(id)).UpdatedAt);
            Assert.Equal(6, await schema.ReadRootRevisionAsync(id));
        }
    }

    [Fact]
    public async Task Raw_sql_negatives_report_actual_sqlstate_and_owned_owner()
    {
        Require();
        var cases = new List<(string Name, string Sql, string ExpectedState, string? ExpectedOwner)>();

        await using (var seeding = schema.NewContext())
        {
            var store = schema.NewStore(seeding);
            var parentId = await store.CreateAsync("parent_type", 0, [T(Fa, "والد", null)], CancellationToken.None);
            var otherId = await store.CreateAsync("other_type", 1, [T(Fa, "دیگر", null)], CancellationToken.None);
            var newId = Guid.NewGuid();

            string HeaderInsert(string code, int sort, int revision) =>
                "insert into fulfillment.shipping_types "
                + "(shipping_type_id,code,sort_order,is_active,revision,created_at,updated_at) "
                + $"values ('{newId}','{code}',{sort},false,{revision},now(),now())";

            string TranslationInsert(Guid typeId, Guid languageId, string name, string? description) =>
                "insert into fulfillment.shipping_type_translations "
                + "(shipping_type_translation_id,shipping_type_id,language_id,name,description) "
                + $"values ('{Guid.NewGuid()}','{typeId}','{languageId}','{name}',"
                + (description is null ? "null" : $"'{description}'") + ")";

            cases.Add(("duplicate-code",
                HeaderInsert("parent_type", 3, 1), "23505", "ix_shipping_types_code"));
            cases.Add(("duplicate-type-language",
                TranslationInsert(parentId, Fa, "تکراری", null), "23505",
                "ix_shipping_type_translations_type_language"));
            cases.Add(("invalid-own-parent",
                TranslationInsert(Guid.NewGuid(), En, "یتیم", null), "23503",
                "fk_shipping_type_translations_shipping_types_shipping_type_id"));
            // Absence of a foreign-schema join target only shows the table is not there; it is NOT a
            // Contracts-boundary proof and is labelled as such.
            cases.Add(("absent-foreign-schema-table-is-not-a-contracts-proof",
                "select count(*) from fulfillment.shipping_type_translations tr "
                + "join public.unknown_localization_table l on l.id = tr.language_id", "42P01", null));
            cases.Add(("required-null-code",
                "insert into fulfillment.shipping_types (shipping_type_id,code,sort_order,is_active,revision,created_at,updated_at) "
                + $"values ('{Guid.NewGuid()}',null,3,false,1,now(),now())",
                "23502", "code"));
            cases.Add(("required-null-name",
                "insert into fulfillment.shipping_type_translations (shipping_type_translation_id,shipping_type_id,language_id,name,description) "
                + $"values ('{Guid.NewGuid()}','{otherId}','{En}',null,null)", "23502", "name"));
            cases.Add(("blank-name",
                TranslationInsert(otherId, En, "   ", null), "23514",
                "ck_shipping_type_translations_name_nonblank"));
            cases.Add(("malformed-code", HeaderInsert("Bad Code", 3, 1), "23514",
                "ck_shipping_types_code_canonical"));
            cases.Add(("uppercase-code", HeaderInsert("UPPER", 3, 1), "23514",
                "ck_shipping_types_code_canonical"));
            cases.Add(("non-ascii-code", HeaderInsert("نوع", 3, 1), "23514",
                "ck_shipping_types_code_canonical"));
            cases.Add(("whitespace-code", HeaderInsert(" padded ", 3, 1), "23514",
                "ck_shipping_types_code_canonical"));
            cases.Add(("sort-negative", HeaderInsert("neg_sort", -1, 1), "23514",
                "ck_shipping_types_sort_order_range"));
            cases.Add(("sort-too-large", HeaderInsert("big_sort", 1_000_001, 1), "23514",
                "ck_shipping_types_sort_order_range"));
            cases.Add(("revision-non-positive", HeaderInsert("zero_rev", 3, 0), "23514",
                "ck_shipping_types_revision_positive"));
            cases.Add(("overlength-name",
                TranslationInsert(otherId, En, new string('n', 129), null), "22001", null));
            cases.Add(("overlength-description",
                TranslationInsert(otherId, En, "ok", new string('d', 513)), "22001", null));
        }

        foreach (var (name, sql, expectedState, expectedOwner) in cases)
        {
            var observed = await schema.TryRawSqlAsync(sql);
            Assert.False(observed.Succeeded, $"{name} must be rejected by PostgreSQL");
            Assert.Equal(expectedState, observed.SqlState);
            if (expectedOwner is not null)
            {
                Assert.True(
                    string.Equals(observed.ConstraintName, expectedOwner, StringComparison.Ordinal)
                    || string.Equals(observed.ColumnName, expectedOwner, StringComparison.Ordinal),
                    $"{name}: expected owned owner '{expectedOwner}' but observed constraint='{observed.ConstraintName}' column='{observed.ColumnName}'");
            }
        }

        // every rejected statement was rolled back independently: no case leaked a row
        Assert.Equal(1, await schema.CountRawAsync("parent_type"));
        Assert.Equal(1, await schema.CountRawAsync("other_type"));
        Assert.Equal(0, await schema.CountRawAsync("neg_sort"));
        Assert.Equal(0, await schema.CountRawAsync("zero_rev"));

        var atLimitName = new string('ی', 128);
        var atLimitDescription = new string('ی', 512);
        Guid minId;
        Guid maxId;
        await using (var seeding = schema.NewContext())
        {
            var store = schema.NewStore(seeding);
            minId = await store.CreateAsync("limit_min", 0,
                [T(Fa, atLimitName, atLimitDescription)], CancellationToken.None);
            maxId = await store.CreateAsync("limit_max", 1_000_000,
                [T(Fa, "حداکثر", null)], CancellationToken.None);
        }

        await using var fresh = schema.NewContext();
        var freshStore = schema.NewStore(fresh);
        var min = await freshStore.GetAsync(minId, Fa, CancellationToken.None);
        Assert.Equal(atLimitName, Translation(min!, Fa).Name);
        Assert.Equal(atLimitDescription, Translation(min, Fa).Description);
        Assert.Equal(0, min.SortOrder);
        Assert.Equal(1_000_000, (await freshStore.GetAsync(maxId, Fa, CancellationToken.None))!.SortOrder);

        output.WriteLine($"raw-sql negatives executed={cases.Count} (each rolled back)");
    }

    // ---------------------------------------------------------------------------------------------
    // GATE 3 — real atomicity / races
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public async Task Strict_original_revision_race_has_one_winner_one_conflict_and_a_clean_loser()
    {
        Require();
        Guid id;
        TreeSnapshot seeded;
        await using (var seeding = schema.NewContext())
        {
            var store = schema.NewStore(seeding);
            // Every language a contender proposes already exists, so the race exercises pure UPDATE patch
            // semantics: a loser can only be decided by the STALE REVISION predicate, never masked by a
            // concurrent unique-violation on a newly inserted translation row.
            id = await store.CreateAsync("race_revision", 0,
            [
                T(Fa, "اولیه", "اول"),
                T(En, "Initial", "Initial english"),
                T(Ar, "أولي", null),
            ], CancellationToken.None);
            Assert.Equal(1, (await store.GetAsync(id, Fa, CancellationToken.None))!.Revision);
            seeded = await CompleteSnapshotAsync(id);
            Assert.Equal(3, seeded.Translations.Count);
        }

        // The clock is advanced so the winner's UpdatedAt must actually move: the immutability assertions are
        // not vacuous.
        schema.Clock.Advance(TimeSpan.FromMinutes(3));

        var gate = new TwoArrivalGate();
        await using var first = schema.NewContext(saveInterceptor: gate);
        await using var second = schema.NewContext(saveInterceptor: gate);
        var firstStore = schema.NewStore(first);
        var secondStore = schema.NewStore(second);

        var firstLoaded = await firstStore.GetAsync(id, Fa, CancellationToken.None);
        var secondLoaded = await secondStore.GetAsync(id, Fa, CancellationToken.None);
        Assert.Equal(1, firstLoaded!.Revision);
        Assert.Equal(1, secondLoaded!.Revision);

        // Two DISTINCT multi-language proposals: different sort, activation and every text, so a hybrid root or
        // a mixed/extra translation set is detectable.
        var firstProposal = new
        {
            SortOrder = 7,
            IsActive = true,
            Translations = new[]
            {
                T(Fa, "برنده", "متن برنده"),
                T(En, "Winner", "Winner text"),
                T(Ar, "فائز", "نص الفائز"),
            },
        };
        var secondProposal = new
        {
            SortOrder = 9,
            IsActive = false,
            Translations = new[]
            {
                T(Fa, "بازنده", "متن بازنده"),
                T(En, "Loser", "Loser text"),
                T(Ar, "خاسر", "نص الخاسر"),
            },
        };

        var firstTask = RunTypedAsync("first", () => firstStore.UpdateAsync(id, 1, null,
            firstProposal.SortOrder, firstProposal.IsActive, firstProposal.Translations, Fa, CancellationToken.None));
        var secondTask = RunTypedAsync("second", () => secondStore.UpdateAsync(id, 1, null,
            secondProposal.SortOrder, secondProposal.IsActive, secondProposal.Translations, Fa, CancellationToken.None));

        await gate.WaitForBothArrivalsAsync(TimeSpan.FromSeconds(20));
        Assert.Equal(2, gate.Arrivals);

        // ACTUAL per-context original concurrency token, captured at each participant's SavingChanges boundary.
        var originalTokens = gate.OriginalRevisions;
        Assert.Equal(2, originalTokens.Count);
        Assert.All(originalTokens, token => Assert.Equal(1, token));
        output.WriteLine($"original revisions at first SavingChanges: {string.Join(",", originalTokens)}");

        gate.Release();

        var firstOutcome = await firstTask;
        var secondOutcome = await secondTask;
        Assert.Single(new[] { firstOutcome, secondOutcome }.Where(x => x.ErrorCode is not null));
        var conflictOutcome = (firstOutcome.ErrorCode is not null ? firstOutcome : secondOutcome);
        Assert.Equal(FulfillmentErrorCodes.ShippingTypeRevisionConflict, conflictOutcome.ErrorCode);

        // Winner mapped from the ACTUAL result, never from literal text.
        var winnerOutcome = firstOutcome.ErrorCode is null ? firstOutcome : secondOutcome;
        Assert.NotNull(winnerOutcome.Revision);
        Assert.Equal("race_revision", winnerOutcome.Code);
        var winningProposal = winnerOutcome.Participant == "first" ? firstProposal : secondProposal;
        Assert.Equal(winningProposal.SortOrder, winnerOutcome.SortOrder);
        Assert.Equal(winningProposal.IsActive, winnerOutcome.IsActive);
        output.WriteLine($"winner={winnerOutcome.Participant} revision={winnerOutcome.Revision}");

        await using var verify = schema.NewContext();
        var view = await schema.NewStore(verify).GetAsync(id, Fa, CancellationToken.None);
        Assert.Equal(2, view!.Revision);
        Assert.Equal(id, view.ShippingTypeId);
        Assert.Equal("race_revision", view.Code);
        Assert.Equal(winningProposal.SortOrder, view.SortOrder);
        Assert.Equal(winningProposal.IsActive, view.IsActive);

        // COMPLETE persisted tree versus the winning participant's complete proposal.
        var winnerTree = await CompleteSnapshotAsync(id);
        Assert.Equal(2, winnerTree.Revision);
        Assert.Equal(id, winnerTree.Id);
        Assert.Equal("race_revision", winnerTree.Code);
        Assert.Equal(winningProposal.SortOrder, winnerTree.SortOrder);
        Assert.Equal(winningProposal.IsActive, winnerTree.IsActive);
        Assert.Equal(seeded.CreatedAt, winnerTree.CreatedAt);
        Assert.NotEqual(seeded.UpdatedAt, winnerTree.UpdatedAt);
        Assert.True(
            DateTimeOffset.Parse(winnerTree.UpdatedAt, CultureInfo.InvariantCulture)
            > DateTimeOffset.Parse(winnerTree.CreatedAt, CultureInfo.InvariantCulture),
            "the advanced clock must be reflected in the winner's UpdatedAt");

        Assert.Equal(
            winningProposal.Translations
                .Select(x => (x.LanguageId, x.Name, x.Description))
                .OrderBy(x => x.LanguageId),
            winnerTree.Translations
                .Select(x => (x.LanguageId, x.Name, x.Description))
                .OrderBy(x => x.LanguageId));
        Assert.Equal(winningProposal.Translations.Length, winnerTree.Translations.Count);

        // The ACTUAL loser tracker holds no pending edit: a later SaveChanges publishes nothing.
        var loser = firstOutcome.ErrorCode is not null ? first : second;
        Assert.Empty(loser.ChangeTracker.Entries().Where(entry => entry.State != EntityState.Unchanged));
        var loserView = await schema.NewStore(loser).GetAsync(id, Fa, CancellationToken.None);
        Assert.Equal(winningProposal.SortOrder, loserView!.SortOrder);
        Assert.Equal(winningProposal.IsActive, loserView.IsActive);
        Assert.Equal(0, await loser.SaveChangesAsync());
        Assert.Equal(winnerTree, await CompleteSnapshotAsync(id));
        output.WriteLine($"loser pending-entries=0 publisher=0 complete-tree-unchanged=True");
        SaveTrace("r1-revision-race.txt",
        [
            "fact=Strict_original_revision_race_has_one_winner_one_conflict_and_a_clean_loser",
            $"original-revisions-at-first-SavingChanges={string.Join(",", originalTokens)}",
            $"typed-outcomes={firstOutcome.Participant}:{(firstOutcome.ErrorCode ?? "success")},{secondOutcome.Participant}:{(secondOutcome.ErrorCode ?? "success")}",
            $"winner-participant={winnerOutcome.Participant} winner-revision={winnerOutcome.Revision}",
            $"conflict-participant={conflictOutcome.Participant} conflict-error={conflictOutcome.ErrorCode}",
            $"seeded-tree={seeded}",
            $"winner-tree={winnerTree}",
            $"created-at-unchanged={seeded.CreatedAt == winnerTree.CreatedAt} updated-at-advanced={(seeded.UpdatedAt != winnerTree.UpdatedAt)}",
            $"translation-count={winnerTree.Translations.Count} languages={string.Join(",", winnerTree.Translations.Select(x => x.LanguageId))}",
            "loser-pending-entries=0 loser-later-SaveChanges=0 complete-tree-unchanged=True",
        ]);
    }

    [Fact]
    public async Task Concurrent_unique_code_create_race_persists_exactly_one_complete_type()
    {
        Require();
        var gate = new TwoArrivalGate();
        await using var first = schema.NewContext(saveInterceptor: gate);
        await using var second = schema.NewContext(saveInterceptor: gate);
        var firstStore = schema.NewStore(first);
        var secondStore = schema.NewStore(second);

        // Typed outcomes keep the participant identity AND the ACTUAL Guid returned by CreateAsync.
        var firstProposal = new[] { T(Fa, "الف", "متن الف"), T(En, "Alpha", "Alpha text") };
        var secondProposal = new[] { T(Fa, "ب", "متن ب"), T(En, "Beta", "Beta text") };

        var firstTask = RunCreateAsync("first", () => firstStore.CreateAsync("same_code", 3, firstProposal, CancellationToken.None));
        var secondTask = RunCreateAsync("second", () => secondStore.CreateAsync("same_code", 3, secondProposal, CancellationToken.None));

        await gate.WaitForBothArrivalsAsync(TimeSpan.FromSeconds(20));
        Assert.Equal(2, gate.Arrivals);
        gate.Release();

        var firstOutcome = await firstTask;
        var secondOutcome = await secondTask;

        // Exactly one typed success with a non-empty returned id and one exact canonical duplicate.
        Assert.Single(new[] { firstOutcome, secondOutcome }.Where(x => x.CreatedId is not null));
        Assert.Single(new[] { firstOutcome, secondOutcome }.Where(x => x.ErrorCode is not null));
        var winnerOutcome = firstOutcome.CreatedId is not null ? firstOutcome : secondOutcome;
        var duplicateOutcome = (firstOutcome.ErrorCode is not null ? firstOutcome : secondOutcome);
        Assert.Equal(FulfillmentErrorCodes.ShippingTypeCodeDuplicate, duplicateOutcome.ErrorCode);
        Assert.NotEqual(Guid.Empty, winnerOutcome.CreatedId);
        var winnerId = winnerOutcome.CreatedId!.Value;
        var winnerProposal = winnerOutcome.Participant == "first" ? firstProposal : secondProposal;
        output.WriteLine($"winner={winnerOutcome.Participant} created-id={winnerId}");

        await using (var verify = schema.NewContext())
        {
            var persistedId = await verify.ShippingTypes
                .AsNoTracking()
                .Where(x => x.Code == "same_code")
                .Select(x => x.ShippingTypeId)
                .SingleAsync();
            Assert.Equal(winnerId, persistedId);

            var translated = await verify.ShippingTypes
                .AsNoTracking()
                .Include(x => x.Translations)
                .SingleAsync(x => x.ShippingTypeId == winnerId);
            Assert.Equal(2, translated.Translations.Count);
            Assert.Equal("same_code", translated.Code);
            Assert.Equal(3, translated.SortOrder);
            Assert.False(translated.IsActive);
            Assert.Equal(1, translated.Revision);
            Assert.Equal(
                winnerProposal.Select(x => (x.LanguageId, x.Name, x.Description)).OrderBy(x => x.LanguageId),
                translated.Translations.Select(x => (x.LanguageId, x.Name, x.Description)).OrderBy(x => x.LanguageId));
        }

        // COMPLETE persisted tree, read fresh, matches that successful participant's complete proposal.
        var winnerTree = await CompleteSnapshotAsync("same_code");
        Assert.Equal(winnerId, winnerTree.Id);
        Assert.Equal(1, winnerTree.Revision);
        Assert.Equal(winnerProposal.Length, winnerTree.Translations.Count);
        Assert.Equal(
            winnerProposal.Select(x => (x.LanguageId, x.Name, x.Description)).OrderBy(x => x.LanguageId),
            winnerTree.Translations.Select(x => (x.LanguageId, x.Name, x.Description)).OrderBy(x => x.LanguageId));
        Assert.All(winnerTree.Translations, row => Assert.Equal(winnerId, row.ShippingTypeId));

        var loser = duplicateOutcome.Participant == "first" ? first : second;
        Assert.Empty(loser.ChangeTracker.Entries().Where(entry => entry.State != EntityState.Unchanged));
        Assert.Equal(0, await loser.SaveChangesAsync());
        Assert.Equal(winnerTree, await CompleteSnapshotAsync("same_code"));
        Assert.Equal(1, await schema.CountRawAsync("same_code"));
        output.WriteLine($"loser pending-entries=0 publisher=0 complete-tree-unchanged=True translations={winnerTree.Translations.Count}");
        SaveTrace("r1-unique-race.txt",
        [
            "fact=Concurrent_unique_code_create_race_persists_exactly_one_complete_type",
            "same-code=same_code",
            $"participants=first,second arrivals={gate.Arrivals}",
            $"actual-returned-id-winner={winnerId}",
            $"persisted-root-id={winnerTree.Id} id-equal={(winnerId == winnerTree.Id)}",
            $"typed-outcomes={firstOutcome.Participant}:{(firstOutcome.ErrorCode ?? "success")},{secondOutcome.Participant}:{(secondOutcome.ErrorCode ?? "success")}",
            $"duplicate-error={duplicateOutcome.ErrorCode}",
            $"complete-root={winnerTree}",
            $"translation-count={winnerTree.Translations.Count}",
            "loser-pending-entries=0 loser-later-SaveChanges=0 complete-tree-unchanged=True",
        ]);
    }

    [Fact]
    public async Task Mid_save_failure_and_caller_cancellation_roll_back_every_write_and_retry_succeeds()
    {
        Require();
        Guid id;
        TreeSnapshot before;
        await using (var seeding = schema.NewContext())
        {
            var store = schema.NewStore(seeding);
            // TWO existing translations with distinct descriptions: a same-count/different-content change must fail.
            id = await store.CreateAsync("atomicity", 2,
                [T(Fa, "قدیمی", "متن قدیمی"), T(En, "Old", "Old english text")], CancellationToken.None);
            before = await CompleteSnapshotAsync(id);
        }

        Assert.Equal(1, before.Revision);
        Assert.Equal(2, before.Translations.Count);

        // TEST-ONLY provider option: MaxBatchSize(1) makes the header and translation separate commands.
        var injected = new InvalidOperationException("injected mid-save failure");
        var failure = new StatementFailureInterceptor(failAtWrite: 2, exception: injected);
        await using (var db = schema.NewContext(commandInterceptor: failure, maxBatchSize: 1))
        {
            var store = schema.NewStore(db);
            var thrown = await Assert.ThrowsAnyAsync<Exception>(() => store.UpdateAsync(id, 1, null, 4, true,
                [T(Fa, "تازه", "متن تازه"), T(En, "New", "New english text")], Fa, CancellationToken.None));
            AssertPropagated(injected, thrown);
            AssertFirstWriteCompletedSecondNot(failure);
            output.WriteLine($"failure: completed={failure.CompletedWrites[0]} | second-attempt={failure.SecondWriteAttempt}");
        }

        // COMPLETE persisted tree must be identical after the injected second-write failure.
        Assert.Equal(before, await CompleteSnapshotAsync(id));

        // REAL caller cancellation: the caller's own CTS is actually cancelled at the second-write boundary.
        using var cancellation = new CancellationTokenSource();
        Assert.False(cancellation.IsCancellationRequested);
        var cancellationInterceptor = new StatementFailureInterceptor(
            failAtWrite: 2,
            cancelAtFailure: cancellation);
        await using (var db = schema.NewContext(commandInterceptor: cancellationInterceptor, maxBatchSize: 1))
        {
            var store = schema.NewStore(db);
            var thrown = await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => store.UpdateAsync(id, 1, null, 5, false, [T(Fa, "لغو", "متن لغو")], Fa, cancellation.Token));
            Assert.True(cancellation.IsCancellationRequested, "the caller token must be genuinely cancelled");
            Assert.Equal(cancellation.Token, thrown.CancellationToken);
            AssertFirstWriteCompletedSecondNot(cancellationInterceptor);
            output.WriteLine($"cancel: completed={cancellationInterceptor.CompletedWrites[0]} | second-attempt={cancellationInterceptor.SecondWriteAttempt}");
        }

        // COMPLETE persisted tree must be identical after the real cancellation, BEFORE the retry.
        Assert.Equal(before, await CompleteSnapshotAsync(id));

        await using (var retry = schema.NewContext())
        {
            var store = schema.NewStore(retry);
            var retried = await store.UpdateAsync(id, 1, null, 6, true,
                [T(Fa, "بازیابی", "متن بازیابی")], Fa, CancellationToken.None);
            Assert.True(retried.IsActive);
            Assert.Equal(2, retried.Revision);
            Assert.Equal(6, retried.SortOrder);
            Assert.Equal("بازیابی", Translation(retried, Fa).Name);
            Assert.Equal("متن بازیابی", Translation(retried, Fa).Description);
            // untouched-language patch semantics: the English row keeps its previous text
            Assert.Equal("Old english text", Translation(retried, En).Description);
        }

        var final = await CompleteSnapshotAsync(id);
        Assert.Equal(2, final.Revision);
        Assert.Equal(6, final.SortOrder);
        Assert.True(final.IsActive);
        Assert.Equal(before.CreatedAt, final.CreatedAt);
        Assert.Equal(2, final.Translations.Count);
        Assert.Equal("متن بازیابی", final.Translations.Single(x => x.LanguageId == Fa).Description);
        Assert.Equal("Old english text", final.Translations.Single(x => x.LanguageId == En).Description);
        output.WriteLine($"retry: revision={final.Revision} sort={final.SortOrder} active={final.IsActive} translations={final.Translations.Count}");
        SaveTrace("r1-atomicity.txt",
        [
            "fact=Mid_save_failure_and_caller_cancellation_roll_back_every_write_and_retry_succeeds",
            $"seeded-tree={before}",
            $"injected-failure-first-write={failure.CompletedWrites[0]}",
            $"injected-failure-second-attempt={failure.SecondWriteAttempt}",
            $"injected-failure-attempted-writes={failure.AttemptedWriteCount} completed-writes={failure.CompletedWrites.Count}",
            "injected-failure-complete-tree-unchanged=True",
            $"cancellation-first-write={cancellationInterceptor.CompletedWrites[0]}",
            $"cancellation-second-attempt={cancellationInterceptor.SecondWriteAttempt}",
            $"cancellation-attempted-writes={cancellationInterceptor.AttemptedWriteCount} completed-writes={cancellationInterceptor.CompletedWrites.Count}",
            $"caller-token-cancelled={cancellation.IsCancellationRequested} propagated-token-equal=True",
            "cancellation-complete-tree-unchanged=True",
            $"retry-tree={final}",
            "retry-untouched-language-english-description-preserved=True",
        ]);
    }

    private static void AssertFirstWriteCompletedSecondNot(StatementFailureInterceptor interceptor)
    {
        Assert.True(interceptor.FirstWriteCompleted, "the FIRST real write must complete");
        Assert.Equal(1, interceptor.CompletedWrites.Count);
        Assert.Equal(2, interceptor.AttemptedWriteCount);
        Assert.NotNull(interceptor.SecondWriteAttempt);
        Assert.True(
            interceptor.SecondWriteAttempt!.StartsWith("UPDATE ", StringComparison.OrdinalIgnoreCase)
            || interceptor.SecondWriteAttempt.StartsWith("INSERT ", StringComparison.OrdinalIgnoreCase)
            || interceptor.SecondWriteAttempt.StartsWith("DELETE ", StringComparison.OrdinalIgnoreCase),
            "the second attempted statement must be a real write");
    }

    /// <summary>Persists a narrowly sanitized case trace (task-owned synthetic ids/text only, no credentials).</summary>
    private static void SaveTrace(string fileName, IEnumerable<string> lines)
    {
        var directory = Environment.GetEnvironmentVariable("TB196R6_EVIDENCE_DIR");
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException(
                "TB196R6_EVIDENCE_DIR is required: the three-fact proof must persist its case trace.");
        }

        Directory.CreateDirectory(directory);
        File.WriteAllText(
            Path.Combine(directory, fileName),
            string.Join(Environment.NewLine, lines) + Environment.NewLine);
    }

    // ---------------------------------------------------------------------------------------------
    // GATE 4 — exact constraint ownership, no message match
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public async Task Only_the_exact_owned_constraint_and_sqlstate_map_to_the_canonical_duplicate()
    {
        Require();
        var ownedFailure = new SyntheticProviderFailureInterceptor("ix_shipping_types_code", "23505");
        await using (var owned = schema.NewContext(saveInterceptor: ownedFailure))
        {
            var store = schema.NewStore(owned);
            var mapped = await Assert.ThrowsAsync<SemanticException>(
                () => store.CreateAsync("synthetic_owned", 0, [T(Fa, "x", null)], CancellationToken.None));
            Assert.Equal(FulfillmentErrorCodes.ShippingTypeCodeDuplicate, mapped.Error.Code);
        }

        var translationFailure = new SyntheticProviderFailureInterceptor(
            "ix_shipping_type_translations_type_language", "23505");
        await using (var translationOwned = schema.NewContext(saveInterceptor: translationFailure))
        {
            var store = schema.NewStore(translationOwned);
            var mapped = await Assert.ThrowsAsync<SemanticException>(
                () => store.CreateAsync("synthetic_translation", 0, [T(Fa, "x", null)], CancellationToken.None));
            Assert.Equal(FulfillmentErrorCodes.ShippingTypeTranslationInvalid, mapped.Error.Code);
        }

        // A lookalike name containing the owned token must NOT be treated as the owned constraint.
        var lookalikeFailure = new SyntheticProviderFailureInterceptor(
            "lookalike_ix_shipping_types_code_suffix", "23505");
        await using (var lookalike = schema.NewContext(saveInterceptor: lookalikeFailure))
        {
            var store = schema.NewStore(lookalike);
            var thrown = await Assert.ThrowsAsync<DbUpdateException>(
                () => store.CreateAsync("synthetic_lookalike", 0, [T(Fa, "x", null)], CancellationToken.None));
            Assert.Same(lookalikeFailure.Injected, thrown);
        }

        // The exact owned name with the WRONG sqlstate must also propagate unchanged.
        var wrongStateFailure = new SyntheticProviderFailureInterceptor("ix_shipping_types_code", "23503");
        await using (var wrongState = schema.NewContext(saveInterceptor: wrongStateFailure))
        {
            var store = schema.NewStore(wrongState);
            var thrown = await Assert.ThrowsAsync<DbUpdateException>(
                () => store.CreateAsync("synthetic_wrong_state", 0, [T(Fa, "x", null)], CancellationToken.None));
            Assert.Same(wrongStateFailure.Injected, thrown);
        }

        Assert.Equal(0, await schema.CountRawAsync("synthetic_owned"));
        Assert.Equal(0, await schema.CountRawAsync("synthetic_translation"));
        Assert.Equal(0, await schema.CountRawAsync("synthetic_lookalike"));
        Assert.Equal(0, await schema.CountRawAsync("synthetic_wrong_state"));
    }

    // ---------------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// COMPLETE persisted tree read through a NEW context: every root column and EVERY translation row in a
    /// stable order. Reference equality is never used; records compare structurally, so a same-count but
    /// different-content change cannot pass.
    /// </summary>
    private async Task<TreeSnapshot> CompleteSnapshotAsync(Guid id)
    {
        await using var context = schema.NewContext();
        var entity = await context.ShippingTypes
            .AsNoTracking()
            .Include(x => x.Translations)
            .SingleAsync(x => x.ShippingTypeId == id);
        return Snapshot(entity);
    }

    private async Task<TreeSnapshot> CompleteSnapshotAsync(string code)
    {
        await using var context = schema.NewContext();
        var entity = await context.ShippingTypes
            .AsNoTracking()
            .Include(x => x.Translations)
            .SingleAsync(x => x.Code == code);
        return Snapshot(entity);
    }

    private static TreeSnapshot Snapshot(ShippingType entity) =>
        new(
            entity.ShippingTypeId,
            entity.Code,
            entity.SortOrder,
            entity.IsActive,
            entity.Revision,
            entity.CreatedAt.ToString("O", CultureInfo.InvariantCulture),
            entity.UpdatedAt.ToString("O", CultureInfo.InvariantCulture),
            entity.Translations
                .OrderBy(x => x.LanguageId)
                .Select(x => new TranslationSnapshot(
                    x.ShippingTypeTranslationId,
                    x.ShippingTypeId,
                    x.LanguageId,
                    x.Name,
                    x.Description))
                .ToArray());

    private static void AssertPropagated(Exception expected, Exception observed)
    {
        for (Exception? current = observed; current is not null; current = current.InnerException)
        {
            if (ReferenceEquals(current, expected))
            {
                return;
            }
        }

        Assert.Fail($"the injected {expected.GetType().Name} must propagate (observed {observed.GetType().Name}: {observed.Message})");
    }

    private void Require()
    {
        if (!schema.CanRun)
        {
            throw new InvalidOperationException(
                "TB196R6_CONNECTION is not set: the disposable PostgreSQL gate must be executed against its "
                + "task-owned target and is deliberately not silently skipped.");
        }
    }

    /// <summary>
    /// Typed outcome for the revision race: keeps the participant identity, the canonical business error AND the
    /// actual view returned by the winning <c>UpdateAsync</c> call.
    /// </summary>
    private static async Task<UpdateOutcome> RunTypedAsync(string participant, Func<Task<ShippingTypeView>> action)
    {
        try
        {
            var view = await action();
            return new UpdateOutcome(participant, view.Code, view.SortOrder, view.IsActive, view.Revision, null);
        }
        catch (SemanticException exception)
        {
            return new UpdateOutcome(participant, null, 0, false, null, exception.Error.Code);
        }
        catch (Exception exception)
        {
            return new UpdateOutcome(participant, null, 0, false, null, exception.GetType().FullName ?? "unknown");
        }
    }

    /// <summary>
    /// Typed outcome for the unique-create race: keeps the participant identity and the ACTUAL Guid returned by
    /// <c>CreateAsync</c> on success. A raw provider exception can never masquerade as a canonical code.
    /// </summary>
    private static async Task<CreateOutcome> RunCreateAsync(string participant, Func<Task<Guid>> action)
    {
        try
        {
            return new CreateOutcome(participant, await action(), null);
        }
        catch (SemanticException exception)
        {
            return new CreateOutcome(participant, null, exception.Error.Code);
        }
        catch (Exception exception)
        {
            return new CreateOutcome(participant, null, $"{exception.GetType().FullName}: {exception.Message}");
        }
    }

    private static (string Name, string? Description) Translation(ShippingTypeView view, Guid languageId)
    {
        var row = view.Translations.Single(x => x.LanguageId == languageId);
        return (row.Name, row.Description);
    }

    private static ShippingTypePreparedTranslation T(Guid languageId, string name, string? description) =>
        new(languageId, name, description);

    private sealed record UpdateOutcome(
        string Participant,
        string? Code,
        int SortOrder,
        bool IsActive,
        long? Revision,
        string? ErrorCode);

    private sealed record CreateOutcome(string Participant, Guid? CreatedId, string? ErrorCode);

    private sealed record TranslationSnapshot(
        Guid TranslationId,
        Guid ShippingTypeId,
        Guid LanguageId,
        string Name,
        string? Description);

    /// <summary>
    /// COMPLETE persisted tree snapshot with STRUCTURAL equality: every root column plus every translation row
    /// compared by value (never by reference identity of the collection), so a same-count but different-content
    /// change cannot pass.
    /// </summary>
    private sealed class TreeSnapshot : IEquatable<TreeSnapshot>
    {
        public TreeSnapshot(
            Guid id,
            string code,
            int sortOrder,
            bool isActive,
            long revision,
            string createdAt,
            string updatedAt,
            IReadOnlyList<TranslationSnapshot> translations)
        {
            Id = id;
            Code = code;
            SortOrder = sortOrder;
            IsActive = isActive;
            Revision = revision;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
            Translations = translations;
        }

        public Guid Id { get; }

        public string Code { get; }

        public int SortOrder { get; }

        public bool IsActive { get; }

        public long Revision { get; }

        public string CreatedAt { get; }

        public string UpdatedAt { get; }

        public IReadOnlyList<TranslationSnapshot> Translations { get; }

        public bool Equals(TreeSnapshot? other) =>
            other is not null
            && Id == other.Id
            && string.Equals(Code, other.Code, StringComparison.Ordinal)
            && SortOrder == other.SortOrder
            && IsActive == other.IsActive
            && Revision == other.Revision
            && string.Equals(CreatedAt, other.CreatedAt, StringComparison.Ordinal)
            && string.Equals(UpdatedAt, other.UpdatedAt, StringComparison.Ordinal)
            && Translations.SequenceEqual(other.Translations);

        public override bool Equals(object? obj) => Equals(obj as TreeSnapshot);

        public override int GetHashCode() =>
            HashCode.Combine(Id, Code, SortOrder, IsActive, Revision, CreatedAt, UpdatedAt, Translations.Count);

        public override string ToString() =>
            $"TreeSnapshot {{ Id = {Id}, Code = {Code}, SortOrder = {SortOrder}, IsActive = {IsActive},"
            + $" Revision = {Revision}, CreatedAt = {CreatedAt}, UpdatedAt = {UpdatedAt},"
            + $" Translations = [{string.Join(" | ", Translations.Select(x => $"{x.LanguageId}:{x.Name}:{x.Description ?? "<null>"}"))}] }}";
    }
}
