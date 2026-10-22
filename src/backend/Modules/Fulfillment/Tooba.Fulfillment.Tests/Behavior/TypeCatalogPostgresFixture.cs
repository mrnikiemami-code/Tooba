using System.Data.Common;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application.Shipping.Models;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Fulfillment.Infrastructure.Shipping;
using Xunit;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>Collection definition for the disposable PostgreSQL schema/storage gate (no parallelization).</summary>
[CollectionDefinition(nameof(TypeCatalogPostgresCollection), DisableParallelization = true)]
public sealed class TypeCatalogPostgresCollection : ICollectionFixture<TypeCatalogPostgresSchemaFixture>;

/// <summary>
/// Disposable, task-owned PostgreSQL target for TB-196-R13. Parses and validates the connection string before
/// any connection, proves live database/user provenance, then executes the chronology-correct phased migration
/// proof once for the whole collection: pre-type checkpoint, historical type Up, accepted pending-index Up,
/// strict current-chain-versus-current-model equality, and the real inverse Down sequence at teardown.
/// </summary>
/// <remarks>
/// Only the clock, the id generator and the prepared language facts are fixtures. Store, context and the
/// checked-in migrations are production code. No credential is ever written to evidence.
/// </remarks>
public sealed class TypeCatalogPostgresSchemaFixture : IAsyncLifetime
{
    public const string PrimaryDatabase = "fulfillment_tb196";
    public const string ModelDatabase = "fulfillment_tb196_model";

    /// <summary>Pre-type checkpoint: the last migration before the shipping-type catalog.</summary>
    public const string PreviousMigration = "20261016120000_AddFulfillmentShippingGroups";

    /// <summary>The accepted shipping-type catalog migration under proof.</summary>
    public const string TypeMigration = "20261022101415_ShippingTypeCatalog";

    /// <summary>The accepted additive pending-outbox-index migration (ACTUAL latest).</summary>
    public const string LatestMigration = "20261022182208_AddFulfillmentOutboxPendingIndex";

    public const string TypeTable = "fulfillment.shipping_types";
    public const string TranslationTable = "fulfillment.shipping_type_translations";
    public const string OutboxTable = "fulfillment.outbox_messages";
    public const string PendingIndexName = "ix_outbox_messages_pending";
    public const string HistoryTable = "__ef_migrations_history";

    /// <summary>
    /// Complete own-schema catalog manifest. Migration-history objects are excluded here by EXACT
    /// (schema, table) identity — never by token/prefix matching — for columns, constraints and the owning
    /// table of every index.
    /// </summary>
    private const string ManifestSql = """
        select format('column %s.%s.%s %s %s default:%s',
                      n.nspname, c.relname, a.attname,
                      format_type(a.atttypid, a.atttypmod),
                      case when a.attnotnull then 'not-null' else 'null' end,
                      coalesce(pg_get_expr(d.adbin, d.adrelid), '-'))
        from pg_attribute a
        join pg_class c on c.oid = a.attrelid
        join pg_namespace n on n.oid = c.relnamespace
        left join pg_attrdef d on d.adrelid = a.attrelid and d.adnum = a.attnum
        where c.relkind in ('r', 'p') and a.attnum > 0 and not a.attisdropped
          and n.nspname not in ('pg_catalog', 'information_schema', 'pg_toast')
          and not (n.nspname = 'fulfillment' and c.relname = '__ef_migrations_history')
        union all
        select format('constraint %s.%s %s %s',
                      n.nspname, c.relname, con.conname, pg_get_constraintdef(con.oid))
        from pg_constraint con
        join pg_class c on c.oid = con.conrelid
        join pg_namespace n on n.oid = c.relnamespace
        where n.nspname not in ('pg_catalog', 'information_schema', 'pg_toast')
          and not (n.nspname = 'fulfillment' and c.relname = '__ef_migrations_history')
        union all
        select format('index %s %s', n.nspname, pg_get_indexdef(i.indexrelid))
        from pg_index i
        join pg_class c on c.oid = i.indrelid
        join pg_namespace n on n.oid = c.relnamespace
        where n.nspname not in ('pg_catalog', 'information_schema', 'pg_toast')
          and not (n.nspname = 'fulfillment' and c.relname = '__ef_migrations_history')
        order by 1
        """;

    private readonly List<string> _evidence = [];
    private string? _evidenceDirectory;

    public bool CanRun { get; private set; }

    public NpgsqlConnectionStringBuilder Target { get; private set; } = null!;

    public NpgsqlConnectionStringBuilder ModelTarget { get; private set; } = null!;

    public MutableClock Clock { get; } = new(new DateTimeOffset(2026, 10, 22, 12, 0, 0, TimeSpan.Zero));

    /// <summary>ONE fixture-scoped generator, so every store in the collection gets globally unique ids.</summary>
    private readonly SequentialIdGenerator _ids = new();

    // ---- offline metadata gate -------------------------------------------------------------------

    public bool SnapshotEqualsCurrentModel { get; private set; }

    public IReadOnlyList<string> ModelDifferences { get; private set; } = [];

    /// <summary>ACTUAL latest migration TargetModel versus the canonical design-time model.</summary>
    public IReadOnlyList<string> LatestTargetModelDifferences { get; private set; } = [];

    public string ActualLatestMigration { get; private set; } = string.Empty;

    // ---- phased historical parity ----------------------------------------------------------------

    /// <summary>Complete manifest at the pre-type checkpoint (no type tables, no pending index).</summary>
    public IReadOnlyList<string> LegacyManifestBeforeUp { get; private set; } = [];

    /// <summary>Complete manifest after the historical type Up.</summary>
    public IReadOnlyList<string> ManifestAfterTypeUp { get; private set; } = [];

    /// <summary>Complete manifest after the accepted pending-index Up (current real chain).</summary>
    public IReadOnlyList<string> ManifestAfterLatestUp { get; private set; } = [];

    /// <summary>Canonical current-model projection manifest from the separate model database.</summary>
    public IReadOnlyList<string> ModelManifest { get; private set; } = [];

    public IReadOnlyList<string> AddedByTypeUp { get; private set; } = [];

    public IReadOnlyList<string> RemovedByTypeUp { get; private set; } = [];

    public IReadOnlyList<string> ExpectedTypeOwnedObjects { get; private set; } = [];

    public IReadOnlyList<string> AddedByLatestUp { get; private set; } = [];

    public IReadOnlyList<string> RemovedByLatestUp { get; private set; } = [];

    /// <summary>Whole-manifest difference primary-current-chain versus canonical current model (both ways).</summary>
    public IReadOnlyList<string> CurrentChainVersusModelDifferences { get; private set; } = [];

    public bool LatestUpApplied { get; private set; }

    public int LegacyObjectCount => LegacyManifestBeforeUp.Count;

    public string ForwardTypeSql { get; private set; } = string.Empty;

    public string ForwardIndexSql { get; private set; } = string.Empty;

    // ---- real inverse Down ----------------------------------------------------------------------

    public bool DownIndexExecuted { get; private set; }

    public bool DownTypeExecuted { get; private set; }

    public IReadOnlyList<string> ManifestAfterDownIndex { get; private set; } = [];

    public IReadOnlyList<string> ManifestAfterDownType { get; private set; } = [];

    public IReadOnlyList<string> DownIndexDifferences { get; private set; } = [];

    public IReadOnlyList<string> DownTypeDifferences { get; private set; } = [];

    public bool ContainerStillOwned { get; private set; } = true;

    public async Task InitializeAsync()
    {
        var raw = Environment.GetEnvironmentVariable("TB196R6_CONNECTION");
        if (string.IsNullOrWhiteSpace(raw))
        {
            CanRun = false;
            return;
        }

        CanRun = true;
        _evidenceDirectory = Environment.GetEnvironmentVariable("TB196R6_EVIDENCE_DIR");

        Target = ValidateTarget(raw);
        var modelBuilder = new NpgsqlConnectionStringBuilder(raw) { Database = ModelDatabase };
        ModelTarget = ValidateTarget(modelBuilder.ConnectionString, ModelDatabase);

        var primaryProvenance = await LiveProvenanceAsync(Target);
        var modelProvenance = await LiveProvenanceAsync(ModelTarget);
        Assert.Equal(PrimaryDatabase, primaryProvenance.Database);
        Assert.Equal(ModelDatabase, modelProvenance.Database);
        Assert.Equal("tb196", primaryProvenance.User);
        Assert.Equal("tb196", modelProvenance.User);
        // Host binding is proven by ValidateTarget (55436); the server-side port inside the disposable container is 5432.
        Assert.Equal(5432, primaryProvenance.Port);
        Record($"target host={Target.Host} port={Target.Port} role={Target.Username} db={Target.Database}/{ModelDatabase}");
        Record($"live provenance primary={primaryProvenance} model={modelProvenance}");

        await using var modelContext = NewContext(ModelTarget);
        var migrationsAssembly = modelContext.GetService<IMigrationsAssembly>();
        var snapshot = migrationsAssembly.ModelSnapshot
                       ?? throw new InvalidOperationException("No checked-in ModelSnapshot found.");

        var initializer = modelContext.GetService<IModelRuntimeInitializer>();
        var snapshotModel = initializer.Initialize(snapshot.Model, designTime: true, validationLogger: null);
        var currentModel = modelContext.GetService<IDesignTimeModel>().Model;
        var differ = modelContext.GetService<IMigrationsModelDiffer>();

        ModelDifferences = differ
            .GetDifferences(snapshotModel.GetRelationalModel(), currentModel.GetRelationalModel())
            .Select(difference => difference.GetType().Name)
            .ToList();
        SnapshotEqualsCurrentModel = ModelDifferences.Count == 0;
        Record($"snapshot-vs-current-model differences={ModelDifferences.Count}");

        ActualLatestMigration = migrationsAssembly.Migrations.Keys.OrderBy(x => x, StringComparer.Ordinal).Last();
        Assert.Equal(LatestMigration, ActualLatestMigration);
        var latestMigration = (Migration)Activator.CreateInstance(
            migrationsAssembly.Migrations[ActualLatestMigration])!;
        var latestTargetModel = initializer.Initialize(
            latestMigration.TargetModel,
            designTime: true,
            validationLogger: null);
        LatestTargetModelDifferences = differ
            .GetDifferences(latestTargetModel.GetRelationalModel(), currentModel.GetRelationalModel())
            .Select(difference => difference.GetType().Name)
            .ToList();
        Record($"latest {ActualLatestMigration} TargetModel-vs-current-model differences={LatestTargetModelDifferences.Count}");

        // FAIL-FAST: an unproved snapshot/latest-TargetModel equivalence stops the run before any dependent DB proof.
        VerifyMetadataEquivalence(SnapshotEqualsCurrentModel, ModelDifferences, LatestTargetModelDifferences);

        var prerequisite = EvaluateFullSchemaPrerequisite(SnapshotEqualsCurrentModel, ModelDifferences, []);
        if (prerequisite is not null)
        {
            Save("01-target-and-model.txt", _evidence);
            Record(prerequisite);
            Save("00-prerequisite-stop.txt", [prerequisite]);
            throw new InvalidOperationException(prerequisite);
        }

        // Model database: canonical EF create script from the model the differ just proved equivalent.
        var createScript = modelContext.Database.GenerateCreateScript();
        await ExecuteScriptAsync(ModelTarget, "create schema if not exists fulfillment;");
        await ExecuteScriptAsync(ModelTarget, createScript);
        ModelManifest = await ManifestAsync(ModelTarget);
        ExpectedTypeOwnedObjects = ModelManifest
            .Where(line => IsTypeOwned(line))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();
        Save("01-model-manifest.txt", ModelManifest);
        Record($"model manifest objects={ModelManifest.Count} type-owned={ExpectedTypeOwnedObjects.Count}");

        // ---- PHASE A: the real chain up to the pre-type checkpoint ---------------------------------
        await using (var primary = NewContext())
        {
            var migrator = primary.GetService<IMigrator>();
            await migrator.MigrateAsync(PreviousMigration);
            ForwardTypeSql = FinePrint(migrator.GenerateScript(PreviousMigration, TypeMigration));
            ForwardIndexSql = FinePrint(migrator.GenerateScript(TypeMigration, LatestMigration));
        }

        Assert.True(await MigrationAppliedAsync(PreviousMigration), "the pre-type checkpoint must be applied");
        Assert.False(await MigrationAppliedAsync(TypeMigration), "no type migration may exist before its phase");
        LegacyManifestBeforeUp = await ManifestAsync(Target);
        Assert.DoesNotContain(LegacyManifestBeforeUp, line => IsTypeOwned(line));
        Assert.DoesNotContain(LegacyManifestBeforeUp, line => line.Contains(PendingIndexName, StringComparison.Ordinal));
        Save("02-phase-a-pre-type-manifest.txt", LegacyManifestBeforeUp);
        Save("03-phase-a-to-type-forward.sql", [ForwardTypeSql]);
        Record($"phase A pre-type manifest objects={LegacyManifestBeforeUp.Count}");

        // ---- PHASE B: historical type Up is strictly additive and only type-owned -------------------
        var legacyBeforeCount = LegacyManifestBeforeUp.Count;
        await using (var primary = NewContext())
        {
            await primary.GetService<IMigrator>().MigrateAsync(TypeMigration);
        }

        Assert.True(await MigrationAppliedAsync(TypeMigration), "the type migration history row must exist");
        ManifestAfterTypeUp = await ManifestAsync(Target);
        AddedByTypeUp = ManifestAfterTypeUp.Except(LegacyManifestBeforeUp, StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal).ToList();
        RemovedByTypeUp = LegacyManifestBeforeUp.Except(ManifestAfterTypeUp, StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal).ToList();
        Save("04-phase-b-added-by-type.txt", AddedByTypeUp);
        Save("05-phase-b-removed-by-type.txt", RemovedByTypeUp);
        Record(
            $"phase B added={AddedByTypeUp.Count} removed={RemovedByTypeUp.Count}"
            + $" legacy-before={legacyBeforeCount} after={ManifestAfterTypeUp.Count}");
        Assert.Empty(RemovedByTypeUp);
        Assert.Equal(ExpectedTypeOwnedObjects, AddedByTypeUp);
        Assert.All(AddedByTypeUp, line => Assert.True(
            IsTypeOwned(line),
            $"the type Up may add only objects owned by the two type tables but added: {line}"));
        Assert.Equal(
            legacyBeforeCount,
            ManifestAfterTypeUp.Count(line => !IsTypeOwned(line)));

        // ---- PHASE C: accepted pending-index Up adds exactly one owned index ------------------------
        await using (var primary = NewContext())
        {
            await primary.GetService<IMigrator>().MigrateAsync(LatestMigration);
        }

        LatestUpApplied = await MigrationAppliedAsync(LatestMigration);
        Assert.True(LatestUpApplied, "the accepted pending-index migration history row must exist");
        ManifestAfterLatestUp = await ManifestAsync(Target);
        AddedByLatestUp = ManifestAfterLatestUp.Except(ManifestAfterTypeUp, StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal).ToList();
        RemovedByLatestUp = ManifestAfterTypeUp.Except(ManifestAfterLatestUp, StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal).ToList();
        Save("06-phase-c-added-by-index.txt", AddedByLatestUp);
        Save("07-phase-c-removed-by-index.txt", RemovedByLatestUp);
        Save("08-phase-c-forward.sql", [ForwardIndexSql]);
        Record($"phase C added={AddedByLatestUp.Count} removed={RemovedByLatestUp.Count}");
        Assert.Empty(RemovedByLatestUp);
        var addedIndex = Assert.Single(AddedByLatestUp);
        Assert.Contains(PendingIndexName, addedIndex, StringComparison.Ordinal);
        Assert.Contains($" ON {OutboxTable} ", addedIndex, StringComparison.Ordinal);

        // ---- PHASE D: STRICT current-chain versus canonical current model --------------------------
        CurrentChainVersusModelDifferences = Diff(ModelManifest, ManifestAfterLatestUp);
        Save("09-current-chain-vs-model-strict-diff.txt", CurrentChainVersusModelDifferences);
        Save("10-current-chain-manifest.txt", ManifestAfterLatestUp);
        Record(
            $"phase D current-chain={ManifestAfterLatestUp.Count} model={ModelManifest.Count}"
            + $" differences={CurrentChainVersusModelDifferences.Count}");
        Assert.Empty(CurrentChainVersusModelDifferences);
        Assert.Equal(ModelManifest, ManifestAfterLatestUp);

        Save("11-execution-log.txt", _evidence);
    }

    public async Task DisposeAsync()
    {
        if (!CanRun || LegacyManifestBeforeUp.Count == 0 || !LatestUpApplied)
        {
            // HONEST BOOKKEEPING: no latest Up means no Down. A no-op equality is never Down parity.
            DownIndexExecuted = false;
            DownTypeExecuted = false;
            Save("19-execution-log.txt",
                [.. _evidence, $"latest Up applied={LatestUpApplied}; Down NOT executed (cleanup/no-op)"]);
            return;
        }

        // ---- REAL INVERSE DOWN 1: index migration -> type checkpoint --------------------------------
            Assert.True(await MigrationAppliedAsync(LatestMigration), "Down 1 requires an applied index migration");
            await using (var primary = NewContext())
            {
                await primary.GetService<IMigrator>().MigrateAsync(TypeMigration);
            }

            DownIndexExecuted = true;
            Assert.False(await MigrationAppliedAsync(LatestMigration), "the index migration row must be gone after Down 1");
            ManifestAfterDownIndex = await ManifestAsync(Target);
            DownIndexDifferences = Diff(ManifestAfterTypeUp, ManifestAfterDownIndex);
            Save("15-after-down-index-manifest.txt", ManifestAfterDownIndex);
            Save("16-after-down-index-diff.txt", DownIndexDifferences);
            Record($"down 1 objects={ManifestAfterDownIndex.Count} differences={DownIndexDifferences.Count}");
            Assert.Empty(DownIndexDifferences);
            Assert.DoesNotContain(ManifestAfterDownIndex, line => line.Contains(PendingIndexName, StringComparison.Ordinal));

            // ---- REAL INVERSE DOWN 2: type migration -> pre-type checkpoint -----------------------------
            Assert.True(await MigrationAppliedAsync(TypeMigration), "Down 2 requires an applied type migration");
            await using (var primary = NewContext())
            {
                await primary.GetService<IMigrator>().MigrateAsync(PreviousMigration);
            }

            DownTypeExecuted = true;
            Assert.False(await MigrationAppliedAsync(TypeMigration), "the type migration row must be gone after Down 2");
            ManifestAfterDownType = await ManifestAsync(Target);
            DownTypeDifferences = Diff(LegacyManifestBeforeUp, ManifestAfterDownType);
            Save("17-after-down-type-manifest.txt", ManifestAfterDownType);
            Save("18-after-down-type-diff.txt", DownTypeDifferences);
            Record($"down 2 objects={ManifestAfterDownType.Count} differences={DownTypeDifferences.Count}");
            Assert.Empty(DownTypeDifferences);
            Assert.DoesNotContain(ManifestAfterDownType, line => IsTypeOwned(line));
            Save("19-execution-log.txt", _evidence);
    }

    public bool PortReleasedAfterDown { get; private set; }

    /// <summary>
    /// Full-schema acceptance prerequisites. Returns a concrete diagnostic when the run must stop, or
    /// <see langword="null"/> when the legacy comparison is green. Unknown is never converted into "equal".
    /// </summary>
    public static string? EvaluateFullSchemaPrerequisite(
        bool snapshotEqualsCurrentModel,
        IReadOnlyList<string> modelDifferences,
        IReadOnlyList<string> substantiveLegacyDifferences)
    {
        if (!snapshotEqualsCurrentModel)
        {
            return "STOP: snapshot-versus-current-model equivalence is UNPROVEN ("
                   + $"{modelDifferences.Count} model differences: "
                   + $"{string.Join(", ", modelDifferences.Distinct(StringComparer.Ordinal).Take(10))}). "
                   + "No model projection, latest Up or Down parity may be claimed from an unproved prerequisite.";
        }

        if (substantiveLegacyDifferences.Count > 0)
        {
            return "STOP: substantive historical-versus-model legacy discrepancies exist ("
                   + $"{substantiveLegacyDifferences.Count}): "
                   + string.Join(" | ", substantiveLegacyDifferences.Take(10))
                   + ". Latest Up must not run against a diverged legacy schema.";
        }

        return null;
    }

    /// <summary>Offline metadata-equivalence gate: snapshot AND actual latest TargetModel must both be 0/0.</summary>
    public static void VerifyMetadataEquivalence(
        bool snapshotEqualsCurrentModel,
        IReadOnlyList<string> snapshotDifferences,
        IReadOnlyList<string> latestTargetModelDifferences)
    {
        if (!snapshotEqualsCurrentModel || latestTargetModelDifferences.Count > 0)
        {
            throw new InvalidOperationException(
                "STOP: model metadata equivalence is UNPROVEN (snapshot differences="
                + $"{snapshotDifferences.Count}: {string.Join(", ", snapshotDifferences.Distinct(StringComparer.Ordinal).Take(8))};"
                + $" latest TargetModel differences={latestTargetModelDifferences.Count}: "
                + $"{string.Join(", ", latestTargetModelDifferences.Distinct(StringComparer.Ordinal).Take(8))}). "
                + "No database projection or migration phase may be claimed from an unproved prerequisite.");
        }
    }

    /// <summary>
    /// Exact owning-table identity of a manifest line. Only the (schema, table) pair is returned — never a
    /// token/prefix match — so a lookalike name cannot be suppressed by accident.
    /// </summary>
    public static string OwningTable(string line)
    {
        if (line.StartsWith("column ", StringComparison.Ordinal))
        {
            var qualified = line.Split(' ')[1];
            return JoinSchemaTable(qualified);
        }

        if (line.StartsWith("constraint ", StringComparison.Ordinal))
        {
            return line.Split(' ')[1];
        }

        if (line.StartsWith("index ", StringComparison.Ordinal))
        {
            var onIndex = line.IndexOf(" ON ", StringComparison.Ordinal);
            if (onIndex < 0)
            {
                return string.Empty;
            }

            var tail = line[(onIndex + 4)..];
            var table = tail.Split(' ')[0];
            return JoinSchemaTable(table);
        }

        return string.Empty;
    }

    private static string JoinSchemaTable(string qualified)
    {
        var parts = qualified.Split('.');
        return parts.Length < 2 ? qualified : $"{parts[0]}.{parts[1]}";
    }

    /// <summary>True only when the object is owned by one of the two shipping-type tables.</summary>
    public static bool IsTypeOwned(string line)
    {
        var owner = OwningTable(line);
        return owner is TypeTable or TranslationTable;
    }

    public async Task<bool> MigrationAppliedAsync(string migrationId)
    {
        await using var connection = new NpgsqlConnection(Target.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"select count(*) from {FulfillmentDbContext.Schema}.{HistoryTable} "
                              + "where migration_id = @id";
        command.Parameters.AddWithValue("id", migrationId);
        return Convert.ToInt64(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture) > 0;
    }

    // -------------------------------------------------------------------------------------------
    // Context / store factories on the REAL Fulfillment context
    // -------------------------------------------------------------------------------------------

    public FulfillmentDbContext NewContext(
        NpgsqlConnectionStringBuilder? target = null,
        DbCommandInterceptor? commandInterceptor = null,
        SaveChangesInterceptor? saveInterceptor = null,
        int? maxBatchSize = null)
    {
        var options = new DbContextOptionsBuilder<FulfillmentDbContext>();
        // Canonical production module configuration (snake_case conventions, module history table and
        // migrations assembly). Only the provider's batch size can be overridden, and only for the test-only
        // mid-save observation.
        Tooba.Persistence.ToobaNpgsql.ConfigureModuleContext(
            options,
            (target ?? Target).ConnectionString,
            FulfillmentDbContext.Schema,
            typeof(FulfillmentDbContext));
        if (maxBatchSize is { } size)
        {
            options.UseNpgsql(
                (target ?? Target).ConnectionString,
                builder =>
                {
                    builder.MigrationsHistoryTable(HistoryTable, FulfillmentDbContext.Schema);
                    builder.MigrationsAssembly(typeof(FulfillmentDbContext).Assembly.FullName);
                    builder.MaxBatchSize(size);
                });
        }

        if (commandInterceptor is not null)
        {
            options.AddInterceptors(commandInterceptor);
        }

        if (saveInterceptor is not null)
        {
            options.AddInterceptors(saveInterceptor);
        }

        return new FulfillmentDbContext(options.Options);
    }

    public ShippingTypeStore NewStore(FulfillmentDbContext db) =>
        new(db, Clock, _ids);

    public IShippingTypeReader NewReader(FulfillmentDbContext db) => NewStore(db);

    /// <summary>Raw read-back using the ACTUAL accepted snake_case column names.</summary>
    public async Task<RawRow> ReadRawAsync(Guid id, Guid? languageId = null)
    {
        await using var connection = new NpgsqlConnection(Target.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select t.revision::text, t.updated_at::text, t.created_at::text,
                   coalesce((select tr.name from fulfillment.shipping_type_translations tr
                             where tr.shipping_type_id = t.shipping_type_id and tr.language_id = @lang), ''),
                   coalesce((select tr.description from fulfillment.shipping_type_translations tr
                             where tr.shipping_type_id = t.shipping_type_id and tr.language_id = @lang), ''),
                   (select count(*)::text from fulfillment.shipping_type_translations tr
                    where tr.shipping_type_id = t.shipping_type_id)
            from fulfillment.shipping_types t where t.shipping_type_id = @id
            """;
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("lang", languageId ?? Guid.Parse("01900000-0000-7000-8000-00000000cc01"));
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync(), "row must exist");
        return new RawRow(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5));
    }

    public async Task<long> ReadRootRevisionAsync(Guid id)
    {
        await using var connection = new NpgsqlConnection(Target.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "select revision from fulfillment.shipping_types where shipping_type_id = @id";
        command.Parameters.AddWithValue("id", id);
        return Convert.ToInt64(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
    }

    public async Task<int> CountRawAsync(string code)
    {
        await using var connection = new NpgsqlConnection(Target.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "select count(*) from fulfillment.shipping_types where code = @code";
        command.Parameters.AddWithValue("code", code);
        return Convert.ToInt32(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
    }

    public async Task<RawSqlOutcome> TryRawSqlAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(Target.ConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
            await transaction.RollbackAsync();
            return new RawSqlOutcome(true, null, null, null);
        }
        catch (PostgresException exception)
        {
            await transaction.RollbackAsync();
            return new RawSqlOutcome(false, exception.SqlState, exception.ConstraintName, exception.ColumnName);
        }
    }

    // -------------------------------------------------------------------------------------------
    // Validation
    // -------------------------------------------------------------------------------------------

    /// <summary>Parses and validates a target string; missing/mismatched/foreign targets fail closed.</summary>
    public static NpgsqlConnectionStringBuilder ValidateTarget(
        string connectionString,
        string expectedDatabase = PrimaryDatabase)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (!string.Equals(builder.Host, "127.0.0.1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Target host must be exact loopback 127.0.0.1 but was '{builder.Host}'.");
        }

        if (builder.Host.Contains(',', StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Multi-host targets are not allowed.");
        }

        if (builder.Port != 55436)
        {
            throw new InvalidOperationException($"Target port must be 55436 but was {builder.Port}.");
        }

        if (!string.Equals(builder.Username, "tb196", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Target role must be tb196 but was '{builder.Username}'.");
        }

        if (!string.Equals(builder.Database, expectedDatabase, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Target database must be exactly '{expectedDatabase}' but was '{builder.Database}'.");
        }

        return builder;
    }

    private async Task<(string Database, string User, int Port)> LiveProvenanceAsync(
        NpgsqlConnectionStringBuilder target)
    {
        await using var connection = new NpgsqlConnection(target.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "select current_database(), current_user, inet_server_port()";
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return (reader.GetString(0), reader.GetString(1), reader.GetInt32(2));
    }

    private static async Task ExecuteScriptAsync(NpgsqlConnectionStringBuilder target, string script)
    {
        if (string.IsNullOrWhiteSpace(script))
        {
            return;
        }

        await using var connection = new NpgsqlConnection(target.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = script;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<List<string>> ManifestAsync(NpgsqlConnectionStringBuilder target)
    {
        var lines = new List<string>();
        await using var connection = new NpgsqlConnection(target.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = ManifestSql;
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lines.Add(Normalize(reader.GetString(0)));
        }

        lines.Sort(StringComparer.Ordinal);
        return lines;
    }

    /// <summary>Both-direction whole-manifest difference, sorted. Empty means byte-equal sets.</summary>
    public static List<string> Diff(IReadOnlyList<string> expected, IReadOnlyList<string> actual)
    {
        var differences = new List<string>();
        foreach (var line in expected.Except(actual, StringComparer.Ordinal))
        {
            differences.Add($"missing-in-actual: {line}");
        }

        foreach (var line in actual.Except(expected, StringComparer.Ordinal))
        {
            differences.Add($"unexpected-in-actual: {line}");
        }

        differences.Sort(StringComparer.Ordinal);
        return differences;
    }

    /// <summary>
    /// Classifies a manifest difference instead of ignoring it: structural differences (object presence,
    /// column type/nullability, constraint/index shape) are separated from column DEFAULT clauses and from
    /// the same definition carried under a different constraint/index name.
    /// </summary>
    public static (List<string> Substantive, List<string> Defaults, List<string> Names) Classify(
        IReadOnlyList<string> expected,
        IReadOnlyList<string> actual)
    {
        var substantive = new List<string>();
        var defaults = new List<string>();
        var names = new List<string>();

        var expectedColumns = ColumnMap(expected);
        var actualColumns = ColumnMap(actual);
        foreach (var key in expectedColumns.Keys.Union(actualColumns.Keys).OrderBy(x => x, StringComparer.Ordinal))
        {
            var inExpected = expectedColumns.TryGetValue(key, out var expectedColumn);
            var inActual = actualColumns.TryGetValue(key, out var actualColumn);
            if (!inExpected || !inActual)
            {
                substantive.Add($"{(inExpected ? "missing-in-actual" : "unexpected-in-actual")}: column {key}");
                continue;
            }

            if (!string.Equals(expectedColumn!.Shape, actualColumn!.Shape, StringComparison.Ordinal))
            {
                substantive.Add($"column-shape {key}: expected '{expectedColumn.Shape}' actual '{actualColumn.Shape}'");
            }

            if (!string.Equals(expectedColumn.Default, actualColumn.Default, StringComparison.Ordinal))
            {
                defaults.Add($"column-default {key}: expected '{expectedColumn.Default}' actual '{actualColumn.Default}'");
            }
        }

        var expectedConstraints = ConstraintMap(expected, out var expectedConstraintNames);
        var actualConstraints = ConstraintMap(actual, out var actualConstraintNames);
        foreach (var key in expectedConstraints.Keys.Union(actualConstraints.Keys).OrderBy(x => x, StringComparer.Ordinal))
        {
            if (!expectedConstraints.ContainsKey(key))
            {
                substantive.Add($"unexpected-in-actual: constraint {key}");
            }
            else if (!actualConstraints.ContainsKey(key))
            {
                substantive.Add($"missing-in-actual: constraint {key}");
            }
        }

        foreach (var key in expectedConstraintNames.Keys.Union(actualConstraintNames.Keys)
                     .OrderBy(x => x, StringComparer.Ordinal))
        {
            var inExpected = expectedConstraintNames.TryGetValue(key, out var expectedName);
            var inActual = actualConstraintNames.TryGetValue(key, out var actualName);
            if (inExpected && inActual && !string.Equals(expectedName, actualName, StringComparison.Ordinal))
            {
                names.Add($"constraint-name {key}: expected '{expectedName}' actual '{actualName}'");
            }
        }

        var expectedIndexes = IndexMap(expected, out var expectedIndexNames);
        var actualIndexes = IndexMap(actual, out var actualIndexNames);
        foreach (var key in expectedIndexes.Keys.Union(actualIndexes.Keys).OrderBy(x => x, StringComparer.Ordinal))
        {
            if (!expectedIndexes.ContainsKey(key))
            {
                substantive.Add($"unexpected-in-actual: index {key}");
            }
            else if (!actualIndexes.ContainsKey(key))
            {
                substantive.Add($"missing-in-actual: index {key}");
            }
        }

        foreach (var key in expectedIndexNames.Keys.Union(actualIndexNames.Keys).OrderBy(x => x, StringComparer.Ordinal))
        {
            var inExpected = expectedIndexNames.TryGetValue(key, out var expectedName);
            var inActual = actualIndexNames.TryGetValue(key, out var actualName);
            if (inExpected && inActual && !string.Equals(expectedName, actualName, StringComparison.Ordinal))
            {
                names.Add($"index-name {key}: expected '{expectedName}' actual '{actualName}'");
            }
        }

        substantive.Sort(StringComparer.Ordinal);
        defaults.Sort(StringComparer.Ordinal);
        names.Sort(StringComparer.Ordinal);
        return (substantive, defaults, names);
    }

    private static Dictionary<string, (string Shape, string Default)> ColumnMap(IReadOnlyList<string> manifest)
    {
        var map = new Dictionary<string, (string, string)>(StringComparer.Ordinal);
        foreach (var line in manifest.Where(x => x.StartsWith("column ", StringComparison.Ordinal)))
        {
            // column <schema>.<table>.<attribute> <type...> <nullability> default:<value>
            var defaultIndex = line.LastIndexOf(" default:", StringComparison.Ordinal);
            var body = line[..defaultIndex];
            var value = line[(defaultIndex + " default:".Length)..];
            var parts = body.Split(' ');
            var shape = body[(("column " + parts[1]).Length + 1)..];
            map[parts[1]] = (shape, value);
        }

        return map;
    }

    private static Dictionary<string, string> ConstraintMap(IReadOnlyList<string> manifest, out Dictionary<string, string> names)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        names = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in manifest.Where(x => x.StartsWith("constraint ", StringComparison.Ordinal)))
        {
            // constraint <schema>.<table> <name> <definition>
            var parts = line.Split(' ', 4);
            var key = $"{parts[1]} {parts[3]}";
            map[key] = parts[2];
            names[$"{parts[1]} {parts[3]}"] = parts[2];
        }

        return map;
    }

    private static Dictionary<string, string> IndexMap(IReadOnlyList<string> manifest, out Dictionary<string, string> names)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        names = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in manifest.Where(x => x.StartsWith("index ", StringComparison.Ordinal)))
        {
            // index <schema> CREATE [UNIQUE] INDEX <name> ON <table> USING ...
            var match = System.Text.RegularExpressions.Regex.Match(
                line,
                @"^index (\S+) CREATE (UNIQUE )?INDEX (\S+) (.*)$");
            if (!match.Success)
            {
                continue;
            }

            var key = $"{match.Groups[1].Value} {match.Groups[2].Value}{match.Groups[4].Value}";
            map[key] = match.Groups[3].Value;
            names[key] = match.Groups[3].Value;
        }

        return map;
    }

    private static string Normalize(string line) =>
        string.Join(' ', line.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries));

    private static string FinePrint(string script) =>
        script.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();

    private void Record(string line) => _evidence.Add(line);

    private void Save(string fileName, IEnumerable<string> lines)
    {
        if (string.IsNullOrWhiteSpace(_evidenceDirectory))
        {
            return;
        }

        Directory.CreateDirectory(_evidenceDirectory);
        File.WriteAllText(
            Path.Combine(_evidenceDirectory, fileName),
            string.Join(Environment.NewLine, lines) + Environment.NewLine);
    }
}

/// <summary>Mutable controlled clock (fixture only).</summary>
public sealed class MutableClock(DateTimeOffset start) : IClock
{
    public DateTimeOffset UtcNow { get; private set; } = start;

    public void Advance(TimeSpan delta) => UtcNow = UtcNow.Add(delta);
}

/// <summary>Deterministic sequential id generator (fixture only).</summary>
public sealed class SequentialIdGenerator : IIdGenerator
{
    private long _next;

    public Guid NewId() =>
        Guid.Parse($"01900000-0000-7000-8000-{(Interlocked.Increment(ref _next) + 0xcc00000000):x12}");
}

/// <summary>
/// Two-arrival barrier on the FIRST <c>SavingChanges</c> of independent contexts. At that exact boundary it
/// records the ACTUAL tracked <c>ShippingType</c> entry's <c>OriginalValues["Revision"]</c> for the arriving
/// context, so the race proof rests on the real concurrency token rather than a copied view revision.
/// </summary>
public sealed class TwoArrivalGate(int expectedArrivals = 2) : SaveChangesInterceptor
{
    private readonly TaskCompletionSource _bothArrived = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly object _sync = new();
    private readonly List<long> _originalRevisions = [];
    private int _arrivals;

    public int Arrivals => Volatile.Read(ref _arrivals);

    /// <summary>One entry per arriving context that actually tracked a <c>ShippingType</c> root.</summary>
    public IReadOnlyList<long> OriginalRevisions
    {
        get
        {
            lock (_sync)
            {
                return [.. _originalRevisions];
            }
        }
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is { } context)
        {
            CaptureOriginalRevision(context);
        }

        if (Interlocked.Increment(ref _arrivals) >= expectedArrivals)
        {
            _bothArrived.TrySetResult();
        }

        await _release.Task.WaitAsync(TimeSpan.FromSeconds(20), cancellationToken);
        return result;
    }

    private void CaptureOriginalRevision(DbContext context)
    {
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Metadata.ClrType.Name != "ShippingType")
            {
                continue;
            }

            if (entry.State is EntityState.Unchanged or EntityState.Detached)
            {
                continue;
            }

            var revision = entry.OriginalValues["Revision"];
            if (revision is null)
            {
                continue;
            }

            lock (_sync)
            {
                _originalRevisions.Add(Convert.ToInt64(revision, CultureInfo.InvariantCulture));
            }

            return;
        }
    }

    public Task WaitForBothArrivalsAsync(TimeSpan timeout) => _bothArrived.Task.WaitAsync(timeout);

    public void Release() => _release.TrySetResult();
}

/// <summary>
/// Fails the Nth REAL write statement (INSERT/UPDATE/DELETE only — reads never count and never satisfy the
/// "first write completed" bookkeeping). Optionally cancels the caller's real token at that exact boundary,
/// so the observed cancellation is genuine rather than an uncancelled synthetic exception.
/// </summary>
public sealed class StatementFailureInterceptor(
    int failAtWrite,
    Exception? exception = null,
    CancellationTokenSource? cancelAtFailure = null) : DbCommandInterceptor
{
    private readonly object _sync = new();
    private readonly List<string> _completedWrites = [];
    private readonly List<string> _attemptedWrites = [];
    private int _writes;

    public bool FirstWriteCompleted
    {
        get
        {
            lock (_sync)
            {
                return _completedWrites.Count == 1;
            }
        }
    }

    public IReadOnlyList<string> CompletedWrites
    {
        get
        {
            lock (_sync)
            {
                return [.. _completedWrites];
            }
        }
    }

    /// <summary>Count of REAL write statements attempted (reads are excluded).</summary>
    public int AttemptedWriteCount
    {
        get
        {
            lock (_sync)
            {
                return _writes;
            }
        }
    }

    /// <summary>The second real write that was attempted but never completed.</summary>
    public string? SecondWriteAttempt
    {
        get
        {
            lock (_sync)
            {
                return _attemptedWrites.Count >= 2 ? _attemptedWrites[1] : null;
            }
        }
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var failure = Gate(command, cancellationToken);
        return failure is null
            ? ValueTask.FromResult(result)
            : ValueTask.FromException<InterceptionResult<int>>(failure);
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        Completed(command);
        return ValueTask.FromResult(result);
    }

    public override int NonQueryExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result)
    {
        Completed(command);
        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        // Reads are explicitly NOT writes: they neither increment the counter nor complete a write.
        var failure = Gate(command, cancellationToken);
        return failure is null
            ? ValueTask.FromResult(result)
            : ValueTask.FromException<InterceptionResult<DbDataReader>>(failure);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        Completed(command);
        return ValueTask.FromResult(result);
    }

    private Exception? Gate(DbCommand command, CancellationToken cancellationToken)
    {
        if (!IsWrite(command))
        {
            return null;
        }

        int position;
        lock (_sync)
        {
            position = ++_writes;
            _attemptedWrites.Add(Describe(command));
        }

        if (position != failAtWrite)
        {
            return null;
        }

        cancelAtFailure?.Cancel();
        if (exception is not null)
        {
            return exception;
        }

        return cancelAtFailure is not null
            ? new OperationCanceledException(cancelAtFailure.Token)
            : new InvalidOperationException("injected mid-save failure");
    }

    private void Completed(DbCommand command)
    {
        if (!IsWrite(command))
        {
            return;
        }

        lock (_sync)
        {
            _completedWrites.Add(Describe(command));
        }
    }

    private static bool IsWrite(DbCommand command) =>
        command.CommandText.StartsWith("INSERT ", StringComparison.OrdinalIgnoreCase)
        || command.CommandText.StartsWith("UPDATE ", StringComparison.OrdinalIgnoreCase)
        || command.CommandText.StartsWith("DELETE ", StringComparison.OrdinalIgnoreCase);

    private static string Describe(DbCommand command)
    {
        var text = command.CommandText.ReplaceLineEndings(" ");
        return text.Length <= 160 ? text : text[..160];
    }
}

/// <summary>Throws a synthetic provider exception so the REAL store classification path is exercised.</summary>
public sealed class SyntheticProviderFailureInterceptor(string constraintName, string sqlState) : SaveChangesInterceptor
{
    private bool _thrown;

    /// <summary>The exact injected instance, so the caller can assert identity-preserving propagation.</summary>
    public DbUpdateException? Injected { get; private set; }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (_thrown)
        {
            return ValueTask.FromResult(result);
        }

        _thrown = true;
        Injected = new DbUpdateException(
            "synthetic provider failure",
            new PostgresException(
                "synthetic",
                "ERROR",
                "ERROR",
                sqlState,
                constraintName: constraintName));
        throw Injected;
    }
}

/// <summary>Observed canonical row state read with raw SQL.</summary>
public sealed record RawRow(
    string Revision,
    string UpdatedAt,
    string CreatedAt,
    string Name,
    string Description,
    string TranslationCount);

/// <summary>Outcome of a rolled-back raw SQL negative.</summary>
public sealed record RawSqlOutcome(
    bool Succeeded,
    string? SqlState,
    string? ConstraintName,
    string? ColumnName);
