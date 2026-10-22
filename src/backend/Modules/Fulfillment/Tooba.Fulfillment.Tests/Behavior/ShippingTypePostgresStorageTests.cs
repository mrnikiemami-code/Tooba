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
using Xunit.Sdk;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R6 — real PostgreSQL proof for the existing Fulfillment type
/// catalog: historical migrated schema versus the regenerated snapshot, additive latest Up, real storage
/// invariants, strict revision and unique-code races, mid-save rollback/cancellation and exact constraint
/// ownership.
/// </summary>
/// <remarks>
/// Only the clock, id generator and prepared language facts are fixtures; store, context, mappings and the
/// checked-in migrations are production code. This is storage/schema evidence, not production functional
/// acceptance. PostgreSQL schema is the disposable task target described by
/// <see cref="TypeCatalogPostgresSchemaFixture"/>.
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
    // GATE 1 — historical schema versus regenerated snapshot, additive Up
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Historical_legacy_schema_matches_the_regenerated_snapshot_projection()
    {
        Require();
        Assert.True(schema.SnapshotEqualsCurrentModel,
            $"snapshot must equal design-time model; differences: {string.Join("; ", schema.ModelDifferences)}");
        Assert.Empty(schema.ModelDifferences);
        Assert.Empty(schema.LegacySubstantiveDifferences);
        output.WriteLine(
            $"defaults={schema.LegacyDefaultDifferences.Count} names={schema.LegacyNameDifferences.Count}");
        Assert.True(schema.LegacyObjectCount > 0);
        output.WriteLine($"legacy objects={schema.LegacyObjectCount}");
    }

    [Fact]
    public void Latest_up_is_additive_and_touches_no_legacy_object()
    {
        Require();
        Assert.Empty(schema.LegacyAfterUpDifferences);
        Assert.NotEmpty(schema.AddedObjects);
        Assert.Equal(schema.ExpectedAddedObjects, schema.AddedObjects);
        Assert.All(schema.AddedObjects, line =>
            Assert.True(
                line.Contains("shipping_types", StringComparison.Ordinal)
                || line.Contains("shipping_type_translations", StringComparison.Ordinal),
                $"latest Up must add only owned type objects but added: {line}"));
        output.WriteLine($"added objects={schema.AddedObjects.Count}");
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
                + "(\"ShippingTypeId\",\"Code\",\"SortOrder\",\"IsActive\",\"Revision\",\"CreatedAt\",\"UpdatedAt\") "
                + $"values ('{newId}','{code}',{sort},false,{revision},now(),now())";

            string TranslationInsert(string typeId, Guid languageId, string name, string? description) =>
                "insert into fulfillment.shipping_type_translations "
                + "(\"ShippingTypeTranslationId\",\"ShippingTypeId\",\"LanguageId\",\"Name\",\"Description\") "
                + $"values ('{Guid.NewGuid()}','{typeId}','{languageId}','{name}',"
                + (description is null ? "null" : $"'{description}'") + ")";

            cases.Add(("duplicate-code",
                HeaderInsert("parent_type", 3, 1), "23505", "ix_shipping_types_code"));
            cases.Add(("duplicate-type-language",
                TranslationInsert(parentId.ToString(), Fa, "تکراری", null), "23505",
                "ix_shipping_type_translations_type_language"));
            cases.Add(("invalid-own-parent",
                TranslationInsert(Guid.NewGuid().ToString(), En, "یتیم", null), "23503",
                "FK_shipping_type_translations_shipping_types_ShippingTypeId"));
            cases.Add(("invalid-parent-with-foreign-schema-join-absent",
                "select count(*) from fulfillment.shipping_type_translations tr "
                + "join public.unknown_localization_table l on l.id = tr.\"LanguageId\"", "42P01", null));
            cases.Add(("required-null-code",
                $"insert into fulfillment.shipping_types (\"ShippingTypeId\",\"Code\",\"SortOrder\",\"IsActive\",\"Revision\",\"CreatedAt\",\"UpdatedAt\") values ('{Guid.NewGuid()}',null,3,false,1,now(),now())",
                "23502", "Code"));
            cases.Add(("required-null-name",
                "insert into fulfillment.shipping_type_translations (\"ShippingTypeTranslationId\",\"ShippingTypeId\",\"LanguageId\",\"Name\",\"Description\") "
                + $"values ('{Guid.NewGuid()}','{otherId}','{En}',null,null)", "23502", "Name"));
            cases.Add(("blank-name",
                TranslationInsert(otherId.ToString(), En, "   ", null), "23514",
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
                TranslationInsert(otherId.ToString(), En, new string('n', 129), null), "22001", null));
            cases.Add(("overlength-description",
                TranslationInsert(otherId.ToString(), En, "ok", new string('d', 513)), "22001", null));
        }

        foreach (var (name, sql, expectedState, expectedOwner) in cases)
        {
            var observed = await schema.TryRawSqlAsync(sql);
            Assert.False(observed.Succeeded, $"{name} must be rejected by PostgreSQL");
            Assert.Equal(expectedState, observed.SqlState);
            if (expectedOwner is not null)
            {
                Assert.True(
                    observed.ConstraintName == expectedOwner || observed.ColumnName == expectedOwner,
                    $"{name}: expected owned owner '{expectedOwner}' but observed constraint='{observed.ConstraintName}' column='{observed.ColumnName}'");
            }
        }

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

        output.WriteLine($"raw-sql negatives executed={cases.Count}");
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
        Assert.Equal(1, firstLoaded.Revision);
        Assert.Equal(1, secondLoaded.Revision);
        gate.Release();

        var firstError = await firstTask;
        var secondError = await secondTask;
        Assert.Single(new[] { firstError, secondError }.Where(e => e is not null));
        var conflict = (firstError ?? secondError)!;
        Assert.Equal(FulfillmentErrorCodes.ShippingTypeRevisionConflict, conflict.FirstErrorCode);

        await using var verify = schema.NewContext();
        var view = await schema.NewStore(verify).GetAsync(id, Fa, CancellationToken.None);
        Assert.Equal(2, view!.Revision);
        var name = Translation(view, Fa).Name;
        Assert.Equal(name == "برنده" ? "متن برنده" : "متن بازنده", Translation(view, Fa).Description);
        Assert.Equal(name == "برنده", view.IsActive);

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

        var loser = firstError is not null ? first : second;
        Assert.Empty(loser.ChangeTracker.Entries().Where(entry => entry.State != EntityState.Unchanged));
        Assert.Equal(0, await loser.SaveChangesAsync());
        Assert.Equal(1, await verify.ShippingTypes.CountAsync(x => x.Code == "same_code"));
    }

    [Fact]
    public async Task Mid_save_failure_and_cancellation_roll_back_every_statement_and_retry_succeeds()
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
        var failure = new StatementFailureInterceptor(failAtCommand: 2, exception: null);
        await using (var db = schema.NewContext(commandInterceptor: failure, maxBatchSize: 1))
        {
            var store = schema.NewStore(db);
            await Assert.ThrowsAnyAsync<Exception>(() => store.UpdateAsync(id, 1, null, 0, true,
                [T(Fa, "تازه", "متن تازه"), T(En, "New", null)], Fa, CancellationToken.None));
            Assert.True(failure.FirstCommandCompleted,
                "at least one real write statement must complete before the injected failure");
        }

        var afterFailure = await schema.ReadRawAsync(id);
        Assert.Equal(before.Revision, afterFailure.Revision);
        Assert.Equal(before.UpdatedAt, afterFailure.UpdatedAt);
        Assert.Equal(before.Name, afterFailure.Name);
        Assert.Equal(before.Description, afterFailure.Description);
        Assert.Equal(before.TranslationCount, afterFailure.TranslationCount);

        using var cancellation = new CancellationTokenSource();
        var cancellationException = new OperationCanceledException(cancellation.Token);
        var cancellationInterceptor = new StatementFailureInterceptor(2, cancellationException);
        await using (var db = schema.NewContext(commandInterceptor: cancellationInterceptor, maxBatchSize: 1))
        {
            var store = schema.NewStore(db);
            var thrown = await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => store.UpdateAsync(id, 1, null, 0, true, [T(Fa, "لغو", null)], Fa, cancellation.Token));
            Assert.Same(cancellationException, thrown);
            Assert.Equal(cancellation.Token, thrown.CancellationToken);
            Assert.True(cancellationInterceptor.FirstCommandCompleted);
        }

        var afterCancellation = await schema.ReadRawAsync(id);
        Assert.Equal(before.Revision, afterCancellation.Revision);
        Assert.Equal(before.UpdatedAt, afterCancellation.UpdatedAt);
        Assert.Equal(before.Name, afterCancellation.Name);
        Assert.Equal(before.TranslationCount, afterCancellation.TranslationCount);

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
    }

    // ---------------------------------------------------------------------------------------------
    // GATE 4 — exact constraint ownership, no message match
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public async Task Only_the_exact_owned_constraint_and_sqlstate_map_to_the_canonical_duplicate()
    {
        Require();
        await using (var owned = schema.NewContext(
                         saveInterceptor: new SyntheticProviderFailureInterceptor("ix_shipping_types_code", "23505")))
        {
            var store = schema.NewStore(owned);
            var mapped = await Assert.ThrowsAsync<SemanticException>(
                () => store.CreateAsync("synthetic_owned", 0, [T(Fa, "x", null)], CancellationToken.None));
            Assert.Equal(FulfillmentErrorCodes.ShippingTypeCodeDuplicate, mapped.Error.Code);
        }

        await using (var translationOwned = schema.NewContext(
                         saveInterceptor: new SyntheticProviderFailureInterceptor(
                             "ix_shipping_type_translations_type_language", "23505")))
        {
            var store = schema.NewStore(translationOwned);
            var mapped = await Assert.ThrowsAsync<SemanticException>(
                () => store.CreateAsync("synthetic_translation", 0, [T(Fa, "x", null)], CancellationToken.None));
            Assert.Equal(FulfillmentErrorCodes.ShippingTypeTranslationInvalid, mapped.Error.Code);
        }

        await using (var lookalike = schema.NewContext(
                         saveInterceptor: new SyntheticProviderFailureInterceptor(
                             "lookalike_ix_shipping_types_code_suffix", "23505")))
        {
            var store = schema.NewStore(lookalike);
            await Assert.ThrowsAsync<DbUpdateException>(
                () => store.CreateAsync("synthetic_lookalike", 0, [T(Fa, "x", null)], CancellationToken.None));
        }

        await using (var wrongState = schema.NewContext(
                         saveInterceptor: new SyntheticProviderFailureInterceptor("ix_shipping_types_code", "23503")))
        {
            var store = schema.NewStore(wrongState);
            await Assert.ThrowsAsync<DbUpdateException>(
                () => store.CreateAsync("synthetic_wrong_state", 0, [T(Fa, "x", null)], CancellationToken.None));
        }

        Assert.Equal(0, await schema.CountRawAsync("synthetic_owned"));
        Assert.Equal(0, await schema.CountRawAsync("synthetic_lookalike"));
    }

    // ---------------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------------

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
