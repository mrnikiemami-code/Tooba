using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application.Shipping.Models;
using Tooba.Fulfillment.Contracts.Errors;
using Tooba.Fulfillment.Contracts.Shipping;
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
        await using (var seeding = schema.NewContext())
        {
            var store = schema.NewStore(seeding);
            id = await store.CreateAsync("race_revision", 0, [T(Fa, "اولیه", "اول")], CancellationToken.None);
            Assert.Equal(1, (await store.GetAsync(id, Fa, CancellationToken.None))!.Revision);
        }

        var gate = new TwoArrivalGate();
        await using var first = schema.NewContext(saveInterceptor: gate);
        await using var second = schema.NewContext(saveInterceptor: gate);
        var firstStore = schema.NewStore(first);
        var secondStore = schema.NewStore(second);

        var firstLoaded = await firstStore.GetAsync(id, Fa, CancellationToken.None);
        var secondLoaded = await secondStore.GetAsync(id, Fa, CancellationToken.None);
        Assert.Equal(1, firstLoaded!.Revision);
        Assert.Equal(1, secondLoaded!.Revision);

        var firstTask = RunAsync(() => firstStore.UpdateAsync(id, 1, null, 0, true,
            [T(Fa, "برنده", "متن برنده")], Fa, CancellationToken.None));
        var secondTask = RunAsync(() => secondStore.UpdateAsync(id, 1, null, 0, false,
            [T(Fa, "بازنده", "متن بازنده")], Fa, CancellationToken.None));

        await gate.WaitForBothArrivalsAsync(TimeSpan.FromSeconds(20));
        Assert.Equal(2, gate.Arrivals);

        // ACTUAL per-context original concurrency token, captured at each participant's SavingChanges boundary.
        var originalTokens = gate.OriginalRevisions;
        Assert.Equal(2, originalTokens.Count);
        Assert.All(originalTokens, token => Assert.Equal(1, token));
        output.WriteLine($"original revisions at first SavingChanges: {string.Join(",", originalTokens)}");

        gate.Release();

        var firstError = await firstTask;
        var secondError = await secondTask;
        Assert.Single(new[] { firstError, secondError }.Where(e => e is not null));
        var conflict = (firstError ?? secondError)!;
        Assert.Equal(FulfillmentErrorCodes.ShippingTypeRevisionConflict, conflict.FirstErrorCode);

        // Winner identified from the ACTUAL outcome, not from literal text.
        var winnerIsFirst = firstError is null;
        var winningName = winnerIsFirst ? "برنده" : "بازنده";
        var winningDescription = winnerIsFirst ? "متن برنده" : "متن بازنده";
        var winningActive = winnerIsFirst;

        await using var verify = schema.NewContext();
        var view = await schema.NewStore(verify).GetAsync(id, Fa, CancellationToken.None);
        Assert.Equal(2, view!.Revision);
        Assert.Equal(winningName, Translation(view, Fa).Name);
        Assert.Equal(winningDescription, Translation(view, Fa).Description);
        Assert.Equal(winningActive, view.IsActive);
        Assert.Equal(2, await schema.ReadRootRevisionAsync(id));

        // The ACTUAL loser tracker holds no pending edit: a later SaveChanges publishes nothing.
        var loser = firstError is not null ? first : second;
        Assert.Empty(loser.ChangeTracker.Entries().Where(entry => entry.State != EntityState.Unchanged));
        var beforeSave = await schema.ReadRawAsync(id);
        Assert.Equal(0, await loser.SaveChangesAsync());
        var afterSave = await schema.ReadRawAsync(id);
        Assert.Equal(beforeSave.Revision, afterSave.Revision);
        Assert.Equal(beforeSave.UpdatedAt, afterSave.UpdatedAt);
        Assert.Equal(beforeSave.Name, afterSave.Name);
        Assert.Equal("2", afterSave.Revision);
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

        var firstTask = RunAsync(() => firstStore.CreateAsync("same_code", 0,
            [T(Fa, "الف", null), T(En, "Alpha", null)], CancellationToken.None));
        var secondTask = RunAsync(() => secondStore.CreateAsync("same_code", 0,
            [T(Fa, "ب", null), T(En, "Beta", null)], CancellationToken.None));

        await gate.WaitForBothArrivalsAsync(TimeSpan.FromSeconds(20));
        Assert.Equal(2, gate.Arrivals);
        gate.Release();

        var firstError = await firstTask;
        var secondError = await secondTask;
        Assert.Single(new[] { firstError, secondError }.Where(e => e is not null));
        var duplicate = (firstError ?? secondError)!;
        // A raw provider exception masquerading as a canonical duplicate is rejected here.
        Assert.Equal(FulfillmentErrorCodes.ShippingTypeCodeDuplicate, duplicate.FirstErrorCode);

        await using var verify = schema.NewContext();
        var rows = await verify.ShippingTypes
            .AsNoTracking()
            .Include(x => x.Translations)
            .Where(x => x.Code == "same_code")
            .ToListAsync();
        var only = Assert.Single(rows);
        Assert.Equal(2, only.Translations.Count);
        var faName = only.Translations.Single(x => x.LanguageId == Fa).Name;
        Assert.Equal(faName == "الف" ? "Alpha" : "Beta",
            only.Translations.Single(x => x.LanguageId == En).Name);
        Assert.NotEqual(Guid.Empty, only.ShippingTypeId);
        output.WriteLine($"winning type id={only.ShippingTypeId} translations={only.Translations.Count}");

        var loser = firstError is not null ? first : second;
        Assert.Empty(loser.ChangeTracker.Entries().Where(entry => entry.State != EntityState.Unchanged));
        Assert.Equal(0, await loser.SaveChangesAsync());
        Assert.Equal(1, await verify.ShippingTypes.CountAsync(x => x.Code == "same_code"));
        Assert.Equal(1, await schema.CountRawAsync("same_code"));
    }

    [Fact]
    public async Task Mid_save_failure_and_caller_cancellation_roll_back_every_write_and_retry_succeeds()
    {
        Require();
        Guid id;
        await using (var seeding = schema.NewContext())
        {
            var store = schema.NewStore(seeding);
            id = await store.CreateAsync("atomicity", 0, [T(Fa, "قدیمی", "متن قدیمی")], CancellationToken.None);
        }

        var before = await schema.ReadRawAsync(id);
        Assert.Equal("1", before.Revision);

        // TEST-ONLY provider option: MaxBatchSize(1) makes the header and translation separate commands.
        var injected = new InvalidOperationException("injected mid-save failure");
        var failure = new StatementFailureInterceptor(failAtWrite: 2, exception: injected);
        await using (var db = schema.NewContext(commandInterceptor: failure, maxBatchSize: 1))
        {
            var store = schema.NewStore(db);
            var thrown = await Assert.ThrowsAnyAsync<Exception>(() => store.UpdateAsync(id, 1, null, 0, true,
                [T(Fa, "تازه", "متن تازه"), T(En, "New", null)], Fa, CancellationToken.None));
            AssertPropagated(injected, thrown);
            Assert.True(failure.FirstWriteCompleted,
                "at least one real write must complete before the injected failure");
            Assert.NotNull(failure.SecondWriteAttempt);
            Assert.Equal(1, failure.CompletedWrites.Count);
            output.WriteLine($"first completed write: {failure.CompletedWrites[0]}");
            output.WriteLine($"second attempted write (never completed): {failure.SecondWriteAttempt}");
        }

        await AssertTreeUnchangedAsync(id, before);

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
                () => store.UpdateAsync(id, 1, null, 0, true, [T(Fa, "لغو", null)], Fa, cancellation.Token));
            Assert.True(cancellation.IsCancellationRequested, "the caller token must be genuinely cancelled");
            Assert.Equal(cancellation.Token, thrown.CancellationToken);
            Assert.True(cancellationInterceptor.FirstWriteCompleted);
            output.WriteLine($"cancelled token at second write; first write completed={cancellationInterceptor.FirstWriteCompleted}");
        }

        await AssertTreeUnchangedAsync(id, before);

        await using (var retry = schema.NewContext())
        {
            var store = schema.NewStore(retry);
            var retried = await store.UpdateAsync(id, 1, null, 0, true,
                [T(Fa, "بازیابی", "متن بازیابی")], Fa, CancellationToken.None);
            Assert.True(retried.IsActive);
            Assert.Equal(2, retried.Revision);
            Assert.Equal("بازیابی", Translation(retried, Fa).Name);
            Assert.Equal("متن بازیابی", Translation(retried, Fa).Description);
        }

        await using var final = schema.NewContext();
        var finalView = await schema.NewStore(final).GetAsync(id, Fa, CancellationToken.None);
        Assert.Equal(2, finalView!.Revision);
        Assert.Equal(1, finalView.Translations.Count);
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

    private async Task AssertTreeUnchangedAsync(Guid id, RawRow before)
    {
        var after = await schema.ReadRawAsync(id);
        Assert.Equal(before.Revision, after.Revision);
        Assert.Equal(before.UpdatedAt, after.UpdatedAt);
        Assert.Equal(before.CreatedAt, after.CreatedAt);
        Assert.Equal(before.Name, after.Name);
        Assert.Equal(before.Description, after.Description);
        Assert.Equal(before.TranslationCount, after.TranslationCount);
    }

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

    private static async Task<ObservedFailure?> RunAsync(Func<Task> action)
    {
        try
        {
            await action();
            return null;
        }
        catch (SemanticException exception)
        {
            return new ObservedFailure(exception.Error.Code);
        }
        catch (Exception exception)
        {
            return new ObservedFailure(exception.GetType().FullName ?? "unknown");
        }
    }

    private static (string Name, string? Description) Translation(ShippingTypeView view, Guid languageId)
    {
        var row = view.Translations.Single(x => x.LanguageId == languageId);
        return (row.Name, row.Description);
    }

    private static ShippingTypePreparedTranslation T(Guid languageId, string name, string? description) =>
        new(languageId, name, description);

    private sealed record ObservedFailure(string FirstErrorCode);
}
