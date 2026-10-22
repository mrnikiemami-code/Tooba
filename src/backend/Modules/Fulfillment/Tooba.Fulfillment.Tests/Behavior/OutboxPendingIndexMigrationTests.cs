using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Xunit;
using Xunit.Abstractions;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R10 — the missing shared outbox pending index.
/// </summary>
/// <remarks>
/// <para>
/// Offline half: the new migration's executable operation universe is exactly one <c>CreateIndex</c> and its
/// <c>DropIndex</c>, and both the checked-in snapshot and the NEW latest <c>TargetModel</c> still describe the
/// unchanged canonical design-time model (0/0 differ operations).
/// </para>
/// <para>
/// Executed half: against the disposable task-owned PostgreSQL target only. The real migration chain is applied
/// THROUGH the ShippingTypeCatalog baseline, synthetic outbox rows are seeded, then Up/Down/reapply/fail-closed
/// duplicate-name behaviour is proved. This does not use, filter or suppress the blocked whole-schema gate.
/// </para>
/// </remarks>
public sealed class OutboxPendingIndexMigrationTests(ITestOutputHelper output)
{
    private const string NewMigration = "20261022182208_AddFulfillmentOutboxPendingIndex";
    private const string BaselineLatest = "20261022101415_ShippingTypeCatalog";
    private const string IndexName = "ix_outbox_messages_pending";
    private const string Schema = "fulfillment";
    private const string Table = "outbox_messages";
    private const string ExactFilter = "((processed_at IS NULL) AND (dead_lettered_at IS NULL))";

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
        union all
        select format('constraint %s.%s %s %s',
                      n.nspname, c.relname, con.conname, pg_get_constraintdef(con.oid))
        from pg_constraint con
        join pg_class c on c.oid = con.conrelid
        join pg_namespace n on n.oid = c.relnamespace
        where n.nspname not in ('pg_catalog', 'information_schema', 'pg_toast')
        union all
        select format('index %s %s', n.nspname, pg_get_indexdef(i.indexrelid))
        from pg_index i
        join pg_class c on c.oid = i.indrelid
        join pg_namespace n on n.oid = c.relnamespace
        where n.nspname not in ('pg_catalog', 'information_schema', 'pg_toast')
        order by 1
        """;

    private const string RowStateSql = """
        select string_agg(
                   concat_ws('|', id::text, attempt_count::text, coalesce(correlation_id, '<null>'),
                             coalesce(dead_lettered_at::text, '<null>'), deployment_id, edition, event_type,
                             coalesce(last_error, '<null>'), coalesce(locked_until::text, '<null>'),
                             coalesce(next_attempt_at::text, '<null>'), occurred_at::text, md5(payload),
                             coalesce(processed_at::text, '<null>'), coalesce(tenant_id, '<null>'),
                             version::text),
                   E'\n' order by id)
        from fulfillment.outbox_messages
        """;

    // ---------------------------------------------------------------------------------------------
    // Offline: executable operation universe and canonical metadata parity
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void New_migration_executes_exactly_one_create_index_and_one_drop_index()
    {
        var migration = new Tooba.Fulfillment.Infrastructure.Persistence.Migrations
            .AddFulfillmentOutboxPendingIndex();

        var up = Assert.Single(migration.UpOperations);
        var create = Assert.IsType<CreateIndexOperation>(up);
        Assert.Equal(IndexName, create.Name);
        Assert.Equal(Schema, create.Schema);
        Assert.Equal(Table, create.Table);
        Assert.False(create.IsUnique);
        Assert.Equal(["occurred_at"], create.Columns);
        Assert.Equal("processed_at IS NULL AND dead_lettered_at IS NULL", create.Filter);

        var down = Assert.Single(migration.DownOperations);
        var drop = Assert.IsType<DropIndexOperation>(down);
        Assert.Equal(IndexName, drop.Name);
        Assert.Equal(Schema, drop.Schema);
        Assert.Equal(Table, drop.Table);

        var upSql = WriteIndexSql(create);
        Assert.Contains($"CREATE INDEX {IndexName}", upSql, StringComparison.Ordinal);
        Assert.DoesNotContain("IF NOT EXISTS", upSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CONCURRENTLY", upSql, StringComparison.OrdinalIgnoreCase);
        output.WriteLine(upSql);
    }

    private static string WriteIndexSql(CreateIndexOperation operation)
    {
        using var context = CanonicalContext();
        var commands = context.GetService<IMigrationsSqlGenerator>()
            .Generate([operation], context.GetService<IDesignTimeModel>().Model);
        return string.Join(Environment.NewLine, commands.Select(x => x.CommandText));
    }

    [Fact]
    public void Snapshot_and_new_target_model_still_match_the_canonical_design_time_model()
    {
        using var context = CanonicalContext();
        var initializer = context.GetService<IModelRuntimeInitializer>();
        var differ = context.GetService<IMigrationsModelDiffer>();
        var current = context.GetService<IDesignTimeModel>().Model;
        var assembly = context.GetService<IMigrationsAssembly>();

        var snapshot = initializer.Initialize(assembly.ModelSnapshot!.Model, designTime: true, validationLogger: null);
        var snapshotOperations = differ.GetDifferences(snapshot.GetRelationalModel(), current.GetRelationalModel());
        Assert.Empty(snapshotOperations);

        Assert.True(assembly.Migrations.ContainsKey(NewMigration), "the new migration must be the assembly's latest metadata");
        var migration = (Migration)Activator.CreateInstance(assembly.Migrations[NewMigration])!;
        var target = initializer.Initialize(migration.TargetModel, designTime: true, validationLogger: null);
        var targetOperations = differ.GetDifferences(target.GetRelationalModel(), current.GetRelationalModel());
        Assert.Empty(targetOperations);

        var modelIndex = current.GetRelationalModel().Tables
            .Single(t => t.Schema == Schema && t.Name == Table)
            .Indexes.Single(i => i.Name == IndexName);
        Assert.False(modelIndex.IsUnique);
        Assert.Equal(["occurred_at"], modelIndex.Columns.Select(c => c.Name));
        Assert.Equal("processed_at IS NULL AND dead_lettered_at IS NULL", modelIndex.Filter);
    }

    // ---------------------------------------------------------------------------------------------
    // Executed proof on the disposable target
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public async Task Pending_index_up_down_reapply_and_duplicate_name_fail_closed()
    {
        var target = RequireTarget();
        var evidence = Environment.GetEnvironmentVariable("TB196R10_EVIDENCE_DIR");
        var log = new List<string>();
        void Record(string line)
        {
            log.Add(line);
            output.WriteLine(line);
        }

        await AssertLiveProvenanceAsync(target, Record);

        // --- baseline: the real existing chain through the ShippingTypeCatalog migration -------------
        await using (var context = Context(target))
        {
            await context.GetService<IMigrator>().MigrateAsync(BaselineLatest);
        }

        await SeedOutboxRowsAsync(target);
        Assert.False(await IndexExistsAsync(target), "the historical baseline must not contain the index");

        var beforeManifest = await ManifestAsync(target);
        var beforeRows = await RowStateAsync(target);
        var beforeCount = Convert.ToInt64(await ScalarAsync(target, "select count(*) from fulfillment.outbox_messages"), CultureInfo.InvariantCulture);
        Record($"baseline index present=False rows={beforeCount} manifest={beforeManifest.Count}");
        var beforeIndexNames = await IndexNamesAsync(target);
        Assert.DoesNotContain(IndexName, beforeIndexNames);

        // --- B: real Up ------------------------------------------------------------------------------
        await using (var context = Context(target))
        {
            await context.GetService<IMigrator>().MigrateAsync();
        }

        var shape = await ReadIndexAsync(target);
        Assert.NotNull(shape);
        Assert.False(shape!.Unique);
        Assert.True(shape.Valid);
        Assert.True(shape.Ready);
        Assert.Equal(["occurred_at"], shape.Columns);
        Assert.Equal(ExactFilter, shape.Predicate);
        Assert.Equal(1L, await HistoryCountAsync(target));
        Assert.Equal(beforeRows, await RowStateAsync(target));
        Assert.Equal(beforeCount, await ScalarAsync(target, "select count(*) from fulfillment.outbox_messages"));
        Record($"after-up shape={shape.Definition}");
        Record($"after-up history={await HistoryCountAsync(target)} rows={beforeCount}");

        var afterUpManifest = await ManifestAsync(target);
        var upDelta = Diff(beforeManifest, afterUpManifest);
        Assert.Single(upDelta);
        Assert.StartsWith($"unexpected-in-actual: index {Schema} CREATE INDEX {IndexName} ", upDelta[0], StringComparison.Ordinal);
        Record($"after-up manifest delta={upDelta.Count}");

        // --- C1: genuine Down restores the baseline exactly -------------------------------------------
        await using (var context = Context(target))
        {
            await context.GetService<IMigrator>().MigrateAsync(BaselineLatest);
        }

        Assert.Null(await ReadIndexAsync(target));
        Assert.Equal(0L, await HistoryCountAsync(target));
        Assert.Equal(beforeManifest, await ManifestAsync(target));
        Assert.Equal(beforeRows, await RowStateAsync(target));
        Record("after-down index=None history=0 manifest-equal=True rows-equal=True");

        // --- C2: reapply, then apply-to-same-target again (history gating, no churn) -------------------
        await using (var context = Context(target))
        {
            await context.GetService<IMigrator>().MigrateAsync();
        }

        var reapplyManifest = await ManifestAsync(target);
        var reapplyRows = await RowStateAsync(target);
        Assert.Equal(1L, await HistoryCountAsync(target));

        await using (var context = Context(target))
        {
            await context.GetService<IMigrator>().MigrateAsync();
        }

        Assert.Equal(reapplyManifest, await ManifestAsync(target));
        Assert.Equal(reapplyRows, await RowStateAsync(target));
        Assert.Equal(1L, await HistoryCountAsync(target));
        Assert.NotNull(await ReadIndexAsync(target));
        Record("reapply-then-migrate-again: one index, one history row, no schema/data change");

        // --- D: preexisting same-name objects are never adopted --------------------------------------
        await using (var context = Context(target))
        {
            await context.GetService<IMigrator>().MigrateAsync(BaselineLatest);
        }

        Assert.Null(await ReadIndexAsync(target));

        // D1: conflicting same-name index with the WRONG shape.
        await ExecuteAsync(target,
            $"CREATE INDEX {IndexName} ON fulfillment.outbox_messages (processed_at)");
        var wrongShape = await ReadIndexAsync(target);
        await AssertFailClosedAsync(target, wrongShape!.Definition, beforeRows, beforeCount, Record, "wrong-shape");
        await ExecuteAsync(target, $"DROP INDEX fulfillment.{IndexName}");
        Assert.Null(await ReadIndexAsync(target));

        // D2: EXACT-shape same-name index is still not silently adopted.
        await ExecuteAsync(target,
            $"CREATE INDEX {IndexName} ON fulfillment.outbox_messages (occurred_at) "
            + "WHERE processed_at IS NULL AND dead_lettered_at IS NULL");
        var exactShape = await ReadIndexAsync(target);
        Assert.Equal(ExactFilter, exactShape!.Predicate);
        await AssertFailClosedAsync(target, exactShape.Definition, beforeRows, beforeCount, Record, "exact-shape");

        // --- cleanup: remove only the task-owned conflicting object ----------------------------------
        await ExecuteAsync(target, $"DROP INDEX fulfillment.{IndexName}");
        Assert.Null(await ReadIndexAsync(target));
        Assert.Equal(0L, await HistoryCountAsync(target));
        Assert.Equal(beforeRows, await RowStateAsync(target));
        Record("cleanup: task-owned conflicting index removed, baseline restored, rows intact");

        if (!string.IsNullOrWhiteSpace(evidence))
        {
            Directory.CreateDirectory(evidence);
            File.WriteAllText(Path.Combine(evidence, "executed-proof.log"), string.Join(Environment.NewLine, log) + Environment.NewLine);
            File.WriteAllText(
                Path.Combine(evidence, "manifest-baseline.txt"),
                string.Join(Environment.NewLine, beforeManifest) + Environment.NewLine);
            File.WriteAllText(
                Path.Combine(evidence, "manifest-after-up-delta.txt"),
                string.Join(Environment.NewLine, upDelta) + Environment.NewLine);
            File.WriteAllText(
                Path.Combine(evidence, "manifest-after-down.txt"),
                string.Join(Environment.NewLine, await ManifestAsync(target)) + Environment.NewLine);
        }
    }

    // ---------------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------------

    private static async Task AssertFailClosedAsync(
        NpgsqlConnectionStringBuilder target,
        string preservedDefinition,
        string beforeRows,
        long beforeCount,
        Action<string> record,
        string label)
    {
        PostgresException? failure = null;
        try
        {
            await using var context = Context(target);
            await context.GetService<IMigrator>().MigrateAsync();
        }
        catch (Exception exception)
        {
            failure = FindPostgres(exception);
        }

        Assert.NotNull(failure);
        Assert.Equal("42P07", failure!.SqlState);
        Assert.Equal(0L, await HistoryCountAsync(target));
        Assert.Equal(beforeRows, await RowStateAsync(target));
        Assert.Equal(beforeCount, await ScalarAsync(target, "select count(*) from fulfillment.outbox_messages"));
        var preserved = await ReadIndexAsync(target);
        Assert.NotNull(preserved);
        Assert.Equal(preservedDefinition, preserved!.Definition);
        record($"{label}: {failure.SqlState} {failure.MessageText.Split('\n')[0]} history=0 object-preserved=True");
    }

    private static PostgresException? FindPostgres(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres)
            {
                return postgres;
            }
        }

        return null;
    }

    private static NpgsqlConnectionStringBuilder RequireTarget()
    {
        var raw = Environment.GetEnvironmentVariable("TB196R10_CONNECTION");
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException(
                "TB196R10_CONNECTION is required: this proof fails closed without its disposable target.");
        }

        return TypeCatalogPostgresSchemaFixture.ValidateTarget(raw);
    }

    private static async Task AssertLiveProvenanceAsync(
        NpgsqlConnectionStringBuilder target,
        Action<string> record)
    {
        await using var connection = new NpgsqlConnection(target.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "select current_database(), current_user, inet_server_port()";
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal("fulfillment_tb196", reader.GetString(0));
        Assert.Equal("tb196", reader.GetString(1));
        Assert.Equal(5432, reader.GetInt32(2));
        record($"target host={target.Host} port={target.Port} role={target.Username} db={target.Database}");
    }

    private static FulfillmentDbContext Context(NpgsqlConnectionStringBuilder target)
    {
        var options = new DbContextOptionsBuilder<FulfillmentDbContext>();
        Tooba.Persistence.ToobaNpgsql.ConfigureModuleContext(
            options,
            target.ConnectionString,
            FulfillmentDbContext.Schema,
            typeof(FulfillmentDbContext));
        return new FulfillmentDbContext(options.Options);
    }

    private static FulfillmentDbContext CanonicalContext()
    {
        var options = new DbContextOptionsBuilder<FulfillmentDbContext>();
        Tooba.Persistence.ToobaNpgsql.ConfigureModuleContext(
            options,
            "Host=127.0.0.1;Port=5432;Database=tooba_design;Username=tooba;Password=dev-placeholder",
            FulfillmentDbContext.Schema,
            typeof(FulfillmentDbContext));
        return new FulfillmentDbContext(options.Options);
    }

    private static async Task SeedOutboxRowsAsync(NpgsqlConnectionStringBuilder target)
    {
        await ExecuteAsync(target, "delete from fulfillment.outbox_messages where id in ('01900000-0000-7000-8000-0000000e1001','01900000-0000-7000-8000-0000000e1002','01900000-0000-7000-8000-0000000e1003')");
        await ExecuteAsync(target, """
            insert into fulfillment.outbox_messages
                (id, event_type, payload, correlation_id, tenant_id, deployment_id, edition,
                 occurred_at, processed_at, dead_lettered_at, attempt_count, version, last_error, locked_until, next_attempt_at)
            values
                ('01900000-0000-7000-8000-0000000e1001', 'fulfillment.pending', '{"n":1}', 'corr-pending', 'tenant-a',
                 'deploy-1', 'alpha', '2026-10-22T10:00:00Z', null, null, 0, 1, null, null, null),
                ('01900000-0000-7000-8000-0000000e1002', 'fulfillment.processed', '{"n":2}', null, null,
                 'deploy-1', 'alpha', '2026-10-22T10:01:00Z', '2026-10-22T10:02:00Z', null, 1, 1, null, null, null),
                ('01900000-0000-7000-8000-0000000e1003', 'fulfillment.dead', '{"n":3}', 'corr-dead', 'tenant-b',
                 'deploy-2', 'beta', '2026-10-22T10:03:00Z', null, '2026-10-22T10:04:00Z', 3, 1, 'boom', null, null);
            """);
    }

    private static async Task ExecuteAsync(NpgsqlConnectionStringBuilder target, string sql)
    {
        await using var connection = new NpgsqlConnection(target.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
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
            lines.Add(reader.GetString(0).ReplaceLineEndings(" "));
        }

        lines.Sort(StringComparer.Ordinal);
        return lines;
    }

    private static List<string> Diff(IReadOnlyList<string> expected, IReadOnlyList<string> actual)
    {
        var differences = new List<string>();
        differences.AddRange(expected.Except(actual, StringComparer.Ordinal).Select(x => $"missing-in-actual: {x}"));
        differences.AddRange(actual.Except(expected, StringComparer.Ordinal).Select(x => $"unexpected-in-actual: {x}"));
        differences.Sort(StringComparer.Ordinal);
        return differences;
    }

    private static async Task<string> RowStateAsync(NpgsqlConnectionStringBuilder target) =>
        (await ScalarAsync(target, RowStateSql)) as string ?? "<empty>";

    private static async Task<object?> ScalarAsync(NpgsqlConnectionStringBuilder target, string sql)
    {
        await using var connection = new NpgsqlConnection(target.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return await command.ExecuteScalarAsync();
    }

    private static async Task<long> HistoryCountAsync(NpgsqlConnectionStringBuilder target) =>
        Convert.ToInt64(
            await ScalarAsync(
                target,
                $"select count(*) from fulfillment.__ef_migrations_history where migration_id = '{NewMigration}'"),
            CultureInfo.InvariantCulture);

    private static async Task<List<string>> IndexNamesAsync(NpgsqlConnectionStringBuilder target)
    {
        var names = new List<string>();
        await using var connection = new NpgsqlConnection(target.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select c.relname from pg_class c
            join pg_namespace n on n.oid = c.relnamespace
            where c.relkind = 'i' and n.nspname = 'fulfillment'
            """;
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }

    private static async Task<bool> IndexExistsAsync(NpgsqlConnectionStringBuilder target) =>
        (await ReadIndexAsync(target)) is not null;

    private static async Task<IndexShape?> ReadIndexAsync(NpgsqlConnectionStringBuilder target)
    {
        await using var connection = new NpgsqlConnection(target.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select pg_get_indexdef(i.indexrelid),
                   i.indisunique, i.indisvalid, i.indisready,
                   coalesce(pg_get_expr(i.indpred, i.indrelid), '-'),
                   (select string_agg(a.attname, ',' order by k.ord)
                    from unnest(i.indkey) with ordinality as k(attnum, ord)
                    join pg_attribute a on a.attrelid = i.indrelid and a.attnum = k.attnum)
            from pg_index i
            join pg_class c on c.oid = i.indexrelid
            join pg_namespace n on n.oid = c.relnamespace
            where n.nspname = 'fulfillment' and c.relname = @name
            """;
        command.Parameters.AddWithValue("name", IndexName);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new IndexShape(
            reader.GetString(0).ReplaceLineEndings(" "),
            reader.GetBoolean(1),
            reader.GetBoolean(2),
            reader.GetBoolean(3),
            reader.GetString(4).ReplaceLineEndings(" "),
            reader.GetString(5).Split(','));
    }

    private sealed record IndexShape(
        string Definition,
        bool Unique,
        bool Valid,
        bool Ready,
        string Predicate,
        IReadOnlyList<string> Columns);
}
