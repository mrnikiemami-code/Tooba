using System.Data.Common;
using System.Globalization;
using System.Text;
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
/// Disposable, task-owned PostgreSQL target for TB-196-R6. Parses and validates the connection string before
/// any connection, proves live database/user provenance, then executes the historical-versus-model gate once
/// for the whole collection and records the executed manifests.
/// </summary>
/// <remarks>
/// Only the clock, the id generator and the prepared language facts are fixtures. Store, context and the
/// checked-in migrations are production code. No credential is ever written to evidence.
/// </remarks>
public sealed class TypeCatalogPostgresSchemaFixture : IAsyncLifetime
{
    public const string PrimaryDatabase = "fulfillment_tb196";
    public const string ModelDatabase = "fulfillment_tb196_model";
    public const string PreviousMigration = "20261016120000_AddFulfillmentShippingGroups";
    public const string LatestMigration = "20261022101415_ShippingTypeCatalog";
    public const string TypeTable = "fulfillment.shipping_types";
    public const string TranslationTable = "fulfillment.shipping_type_translations";

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
          and n.nspname not in ('pg_catalog', 'information_schema')
        union all
        select format('constraint %s.%s %s %s',
                      n.nspname, c.relname, con.conname, pg_get_constraintdef(con.oid))
        from pg_constraint con
        join pg_class c on c.oid = con.conrelid
        join pg_namespace n on n.oid = c.relnamespace
        where n.nspname not in ('pg_catalog', 'information_schema')
        union all
        select format('index %s %s', n.nspname, pg_get_indexdef(i.indexrelid))
        from pg_index i
        join pg_class c on c.oid = i.indrelid
        join pg_namespace n on n.oid = c.relnamespace
        where n.nspname not in ('pg_catalog', 'information_schema')
        order by 1
        """;

    private readonly List<string> _evidence = [];
    private string? _evidenceDirectory;

    public bool CanRun { get; private set; }

    public NpgsqlConnectionStringBuilder Target { get; private set; } = null!;

    public NpgsqlConnectionStringBuilder ModelTarget { get; private set; } = null!;

    public MutableClock Clock { get; } = new(new DateTimeOffset(2026, 10, 22, 12, 0, 0, TimeSpan.Zero));

    public bool SnapshotEqualsCurrentModel { get; private set; }

    public IReadOnlyList<string> ModelDifferences { get; private set; } = [];

    public IReadOnlyList<string> LegacyDiscrepancies { get; private set; } = [];

    public IReadOnlyList<string> LegacyManifestBeforeUp { get; private set; } = [];

    public IReadOnlyList<string> LegacyManifestAfterUp { get; private set; } = [];

    public IReadOnlyList<string> LegacyManifestAfterDown { get; private set; } = [];

    public IReadOnlyList<string> LegacyAfterUpDifferences { get; private set; } = [];

    public IReadOnlyList<string> LegacyAfterDownDifferences { get; private set; } = [];

    public IReadOnlyList<string> AddedObjects { get; private set; } = [];

    public IReadOnlyList<string> ExpectedAddedObjects { get; private set; } = [];

    public bool DownExecuted { get; private set; }

    public bool ContainerStillOwned { get; private set; } = true;

    public bool PortReleasedAfterDown { get; private set; }

    public int LegacyObjectCount => LegacyManifestBeforeUp.Count;

    public string ForwardSql { get; private set; } = string.Empty;

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
        var snapshot = modelContext.GetService<IMigrationsAssembly>().ModelSnapshot
                       ?? throw new InvalidOperationException("No checked-in ModelSnapshot found.");
        
        var initializer = modelContext.GetService<IModelRuntimeInitializer>();
        var snapshotModel = initializer.Initialize(
            snapshot.Model,
            designTime: true,
            validationLogger: null);
        var currentModel = modelContext.GetService<IDesignTimeModel>().Model;
        var differ = modelContext.GetService<IMigrationsModelDiffer>();
        ModelDifferences = differ
            .GetDifferences(snapshotModel.GetRelationalModel(), currentModel.GetRelationalModel())
            .Select(difference => difference.GetType().Name)
            .ToList();
        SnapshotEqualsCurrentModel = ModelDifferences.Count == 0;
        Record($"snapshot-vs-current-model differences={ModelDifferences.Count}");

        // Model database: canonical EF create script from the model that the differ just proved equivalent to
        // the checked-in snapshot. Applying it makes both manifests PostgreSQL pg_catalog output, so provider
        // type aliases/formatting are normalized by the provider itself rather than by a hand-written map.
        var createScript = modelContext.Database.GenerateCreateScript();
        await ExecuteScriptAsync(ModelTarget, "create schema if not exists fulfillment;");
        await ExecuteScriptAsync(ModelTarget, createScript);
        var modelManifest = await ManifestAsync(ModelTarget);
        Record($"model manifest objects={modelManifest.Count}");

        var expectedLegacy = LegacyProjection(modelManifest);
        ExpectedAddedObjects = modelManifest.Except(expectedLegacy).OrderBy(x => x, StringComparer.Ordinal).ToList();

        await using (var primary = NewContext())
        {
            var migrator = primary.GetService<IMigrator>();
            await migrator.MigrateAsync(PreviousMigration);
            ForwardSql = FinePrint(migrator.GenerateScript(PreviousMigration, LatestMigration));
        }

        LegacyManifestBeforeUp = await ManifestAsync(Target);
        Save("02-historical-legacy-manifest.txt", LegacyManifestBeforeUp);
        Save("03-model-expected-legacy.txt", expectedLegacy);
        LegacyDiscrepancies = Diff(expectedLegacy, LegacyManifestBeforeUp);
        Save("04-historical-vs-model-diff.txt", LegacyDiscrepancies);
        Record($"historical legacy objects={LegacyManifestBeforeUp.Count} discrepancies={LegacyDiscrepancies.Count}");

        await using (var primary = NewContext())
        {
            Save("01-target-and-model.txt", _evidence);
            await primary.GetService<IMigrator>().MigrateAsync(LatestMigration);
        }

        var afterUp = await ManifestAsync(Target);
        LegacyManifestAfterUp = LegacyProjection(afterUp);
        LegacyAfterUpDifferences = Diff(expectedLegacy, LegacyManifestAfterUp);
        AddedObjects = afterUp.Except(LegacyManifestBeforeUp).OrderBy(x => x, StringComparer.Ordinal).ToList();
        Save("05-additive-up.txt", AddedObjects);
        Save("06-forward-sql.sql", [ForwardSql]);
        Record($"after up added={AddedObjects.Count} legacy-differences={LegacyAfterUpDifferences.Count}");

        Save("01-target-and-model.txt", _evidence);
        Save("02-historical-legacy-manifest.txt", LegacyManifestBeforeUp);
        Save("03-model-expected-legacy.txt", expectedLegacy);
        Save("04-historical-vs-model-diff.txt", LegacyDiscrepancies);
        Save("05-additive-up.txt", AddedObjects);
        Save("06-forward-sql.sql", [ForwardSql]);
    }

    public async Task DisposeAsync()
    {
        if (!CanRun || LegacyManifestBeforeUp.Count == 0)
        {
            Save("09-execution-log.txt", [.. _evidence, "initialization did not complete; Down not executed"]);
            return;
        }

        await using var primary = NewContext();
        await primary.GetService<IMigrator>().MigrateAsync(PreviousMigration);
        DownExecuted = true;
        LegacyManifestAfterDown = LegacyProjection(await ManifestAsync(Target));
        LegacyAfterDownDifferences = Diff(LegacyManifestBeforeUp, LegacyManifestAfterDown);
        Record($"after-down legacy objects={LegacyManifestAfterDown.Count} differences={LegacyAfterDownDifferences.Count}");
        Save("07-after-down-legacy.txt", LegacyManifestAfterDown);
        Save("08-after-down-diff.txt", LegacyAfterDownDifferences);
        Save("09-execution-log.txt", _evidence);

        // Down must not recreate or drop legacy objects; the module history is the only difference.
        Assert.Empty(LegacyAfterDownDifferences);
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
                    builder.MigrationsHistoryTable("__ef_migrations_history", FulfillmentDbContext.Schema);
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
        new(db, Clock, new SequentialIdGenerator());

    public IShippingTypeReader NewReader(FulfillmentDbContext db) => NewStore(db);

    public async Task<RawRow> ReadRawAsync(Guid id)
    {
        await using var connection = new NpgsqlConnection(Target.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select t."Revision"::text, t."UpdatedAt"::text, t."CreatedAt"::text,
                   coalesce((select tr."Name" from fulfillment.shipping_type_translations tr
                             where tr."ShippingTypeId" = t."ShippingTypeId" and tr."LanguageId" = @fa), ''),
                   coalesce((select tr."Description" from fulfillment.shipping_type_translations tr
                             where tr."ShippingTypeId" = t."ShippingTypeId" and tr."LanguageId" = @fa), ''),
                   (select count(*)::text from fulfillment.shipping_type_translations tr
                    where tr."ShippingTypeId" = t."ShippingTypeId")
            from fulfillment.shipping_types t where t."ShippingTypeId" = @id
            """;
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("fa", Guid.Parse("01900000-0000-7000-8000-00000000cc01"));
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
        command.CommandText = "select \"Revision\" from fulfillment.shipping_types where \"ShippingTypeId\" = @id";
        command.Parameters.AddWithValue("id", id);
        return (long)(await command.ExecuteScalarAsync() ?? 0L);
    }

    public async Task<int> CountRawAsync(string code)
    {
        await using var connection = new NpgsqlConnection(Target.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "select count(*) from fulfillment.shipping_types where \"Code\" = @code";
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
        using var connection = new NpgsqlConnection(target.ConnectionString);
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

    /// <summary>Expected legacy projection: drop ONLY the two new type tables and migration-history infra.</summary>
    private static List<string> LegacyProjection(IReadOnlyList<string> manifest) =>
        manifest
            .Where(line => !line.Contains("shipping_type", StringComparison.Ordinal))
            .Where(line => !line.Contains("migrations_history", StringComparison.OrdinalIgnoreCase)  || line.Contains("__EFMigrationsHistory", StringComparison.Ordinal))
            .ToList();

    private static List<string> Diff(IReadOnlyList<string> expected, IReadOnlyList<string> actual)
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

/// <summary>Two-arrival barrier on the FIRST <c>SavingChanges</c> of independent contexts.</summary>
public sealed class TwoArrivalGate(int expectedArrivals = 2) : SaveChangesInterceptor
{
    private readonly TaskCompletionSource _bothArrived = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _arrivals;

    public int Arrivals => Volatile.Read(ref _arrivals);

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (Interlocked.Increment(ref _arrivals) >= expectedArrivals)
        {
            _bothArrived.TrySetResult();
        }

        await _release.Task.WaitAsync(TimeSpan.FromSeconds(20), cancellationToken);
        return result;
    }

    public Task WaitForBothArrivalsAsync(TimeSpan timeout) => _bothArrived.Task.WaitAsync(timeout);

    public void Release() => _release.TrySetResult();
}

/// <summary>Fails the Nth real statement, after at least one statement has completed.</summary>
public sealed class StatementFailureInterceptor(int failAtCommand, Exception? exception) : DbCommandInterceptor
{
    private int _commands;

    public bool FirstCommandCompleted { get; private set; }

    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        await GateAsync(cancellationToken);
        return result;
    }

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        await GateAsync(cancellationToken);
        return result;
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        FirstCommandCompleted = true;
        return ValueTask.FromResult(result);
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        FirstCommandCompleted = true;
        return ValueTask.FromResult(result);
    }

    private Task GateAsync(CancellationToken cancellationToken)
    {
        var position = Interlocked.Increment(ref _commands);
        if (position == failAtCommand)
        {
            return Task.FromException(exception ?? new InvalidOperationException("injected mid-save failure"));
        }

        return Task.CompletedTask;
    }
}

/// <summary>Throws a synthetic provider exception so the REAL store classification path is exercised.</summary>
public sealed class SyntheticProviderFailureInterceptor(string constraintName, string sqlState) : SaveChangesInterceptor
{
    private bool _thrown;

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
        throw new DbUpdateException(
            "synthetic provider failure",
            new PostgresException(
                "synthetic",
                "ERROR",
                "ERROR",
                sqlState,
                constraintName: constraintName));
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
