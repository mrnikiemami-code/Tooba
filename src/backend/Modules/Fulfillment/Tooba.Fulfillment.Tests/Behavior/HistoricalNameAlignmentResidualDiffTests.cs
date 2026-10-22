using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Xunit;
using Xunit.Abstractions;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R11 — residual difference between the real historical migrated
/// schema and the current canonical model must now be ONLY the five known column defaults.
/// </summary>
/// <remarks>
/// <para>
/// The primary database is migrated with the real, unchanged chain (through the accepted R10 pending-index
/// migration) — it is never model-created. The current model is projected into a separate task-owned model
/// database with the provider's own create script. Both sides are then described from <c>pg_catalog</c>, so the
/// comparison is PostgreSQL-normalized on both sides.
/// </para>
/// <para>
/// This is a focused residual-diff proof for the R11 mapping alignment. It does NOT run the blocked whole-schema
/// collection, does not relax that gate, and does not claim that the five defaults are accepted.
/// </para>
/// </remarks>
public sealed class HistoricalNameAlignmentResidualDiffTests(ITestOutputHelper output)
{
    private const string PrimaryDatabase = "fulfillment_tb196";
    private const string ModelDatabase = "fulfillment_tb196_model";
    private const string LatestMigration = "20261022182208_AddFulfillmentOutboxPendingIndex";
    private const string HistoryTable = "__ef_migrations_history";

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

    private static readonly string[] ExpectedDefaultPairs =
    [
        "column fulfillment.items.quantity_packed numeric(18,6) not-null default:0",
        "column fulfillment.items.quantity_processing numeric(18,6) not-null default:0",
        "column fulfillment.shipments.provider_metadata_version integer not-null default:0",
        "column fulfillment.shipments.shipping_method_code character varying(64) not-null default:''::character varying",
        "column fulfillment.shipments.shipping_method_label character varying(128) not-null default:''::character varying",
    ];

    [Fact]
    public async Task Historical_schema_differs_from_the_current_model_only_by_the_five_known_defaults()
    {
        var target = RequireTarget();
        var modelTarget = RequireModelTarget();
        var evidence = Environment.GetEnvironmentVariable("TB196R11_EVIDENCE_DIR");
        var log = new List<string>();
        void Record(string line)
        {
            log.Add(line);
            output.WriteLine(line);
        }

        await AssertLiveProvenanceAsync(target, PrimaryDatabase, Record);
        await AssertLiveProvenanceAsync(modelTarget, ModelDatabase, Record);

        // --- primary: the REAL unchanged migration chain, never a model-created database ---------------
        await using (var context = Context(target))
        {
            await context.GetService<IMigrator>().MigrateAsync();
        }

        var beforeManifest = await ManifestAsync(target);
        Assert.Contains(beforeManifest, x => x.Contains("ix_outbox_messages_pending", StringComparison.Ordinal));
        Record($"primary manifest objects={beforeManifest.Count}");

        // --- model database: project the current canonical model only ----------------------------------
        string createScript;
        await using (var context = Context(modelTarget))
        {
            createScript = context.Database.GenerateCreateScript();
        }

        await ExecuteAsync(modelTarget, "create schema if not exists fulfillment;");
        await ExecuteAsync(modelTarget, createScript);
        var modelManifest = await ManifestAsync(modelTarget);
        Record($"model manifest objects={modelManifest.Count}");

        // --- residual classification ------------------------------------------------------------------
        var missingInModel = beforeManifest.Except(modelManifest, StringComparer.Ordinal).ToList();
        var unexpectedInModel = modelManifest.Except(beforeManifest, StringComparer.Ordinal).ToList();
        var defaults = new List<string>();
        var residual = new List<string>();

        foreach (var line in missingInModel)
        {
            var expectedColumn = ExpectedDefaultPairs
                .SingleOrDefault(d => line.StartsWith(d[..d.IndexOf(" default:", StringComparison.Ordinal)], StringComparison.Ordinal));
            var counterpart = expectedColumn is null ? null : unexpectedInModel.FirstOrDefault(u =>
                u.StartsWith(line[..line.IndexOf(" default:", StringComparison.Ordinal)], StringComparison.Ordinal));
            if (expectedColumn is null || counterpart is null)
            {
                residual.Add($"missing-in-model: {line}");
                continue;
            }

            defaults.Add($"missing-in-model: {line}");
            defaults.Add($"unexpected-in-model: {counterpart}");
            unexpectedInModel.Remove(counterpart);
        }

        residual.AddRange(unexpectedInModel.Select(x => $"unexpected-in-model: {x}"));
        residual.Sort(StringComparer.Ordinal);
        defaults.Sort(StringComparer.Ordinal);

        Assert.Empty(residual);
        Assert.Equal(ExpectedDefaultPairs.Length * 2, defaults.Count);
        Assert.Equal(
            ExpectedDefaultPairs.Order(StringComparer.Ordinal),
            defaults.Where(x => x.StartsWith("missing-in-model:", StringComparison.Ordinal))
                .Select(x => x["missing-in-model: ".Length..])
                .Order(StringComparer.Ordinal));
        Record($"residual={residual.Count} default-lines={defaults.Count}");

        // every check and every historical object name must now agree exactly
        foreach (var name in new[]
                 {
                     "ck_shipping_methods_code_canonical",
                     "ck_shipping_methods_option_code_canonical",
                     "fk_shipping_method_translations_method",
                     "ix_consolidated_package_members_package_id",
                     "ix_consolidated_package_members_shipment_active",
                     "ix_shipping_groups_fulfillment_group",
                     "ix_shipping_method_translations_method_lang",
                     "ux_shipping_methods_legacy_binding",
                     "ix_shipping_service_option_translations_option_lang",
                     "ix_shipping_service_options_service_code",
                     "ix_shipping_service_options_service_id",
                     "ix_shipping_service_translations_service_lang",
                 })
        {
            Assert.Contains(beforeManifest, x => x.Contains(name, StringComparison.Ordinal));
            Assert.Contains(modelManifest, x => x.Contains(name, StringComparison.Ordinal));
        }

        // --- primary must be untouched by the model projection -----------------------------------------
        Assert.Equal(beforeManifest, await ManifestAsync(target));
        Record("primary re-read equal=True (no DDL beyond its own migration chain)");

        if (!string.IsNullOrWhiteSpace(evidence))
        {
            Directory.CreateDirectory(evidence);
            File.WriteAllText(Path.Combine(evidence, "manifest-primary.txt"), string.Join(Environment.NewLine, beforeManifest) + Environment.NewLine);
            File.WriteAllText(Path.Combine(evidence, "manifest-model.txt"), string.Join(Environment.NewLine, modelManifest) + Environment.NewLine);
            File.WriteAllText(Path.Combine(evidence, "residual-default-lines.txt"), string.Join(Environment.NewLine, defaults) + Environment.NewLine);
            File.WriteAllText(Path.Combine(evidence, "model-only-create.sql"), createScript);
            File.WriteAllText(Path.Combine(evidence, "residual-diff.log"), string.Join(Environment.NewLine, log) + Environment.NewLine);
        }
    }

    // ---------------------------------------------------------------------------------------------

    private static NpgsqlConnectionStringBuilder RequireTarget() =>
        TypeCatalogPostgresSchemaFixture.ValidateTarget(
            Environment.GetEnvironmentVariable("TB196R11_CONNECTION") ?? throw new InvalidOperationException(
                "TB196R11_CONNECTION is required: the residual-diff proof fails closed without its disposable target."),
            PrimaryDatabase);

    private static NpgsqlConnectionStringBuilder RequireModelTarget()
    {
        var raw = Environment.GetEnvironmentVariable("TB196R11_CONNECTION") ?? throw new InvalidOperationException(
            "TB196R11_CONNECTION is required: the residual-diff proof fails closed without its disposable target.");
        var builder = new NpgsqlConnectionStringBuilder(raw) { Database = ModelDatabase };
        return TypeCatalogPostgresSchemaFixture.ValidateTarget(builder.ConnectionString, ModelDatabase);
    }

    private static async Task AssertLiveProvenanceAsync(
        NpgsqlConnectionStringBuilder target,
        string expectedDatabase,
        Action<string> record)
    {
        await using var connection = new NpgsqlConnection(target.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "select current_database(), current_user, inet_server_port()";
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(expectedDatabase, reader.GetString(0));
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

    private static async Task ExecuteAsync(NpgsqlConnectionStringBuilder target, string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return;
        }

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
            var line = reader.GetString(0).ReplaceLineEndings(" ");
            // Exclusion by exact table identity only (never by broad tokens).
            if (line.Contains($" fulfillment.{HistoryTable}", StringComparison.Ordinal)
                && !line.StartsWith($"column fulfillment.{HistoryTable}.", StringComparison.Ordinal))
            {
                continue;
            }

            if (line.StartsWith($"column fulfillment.{HistoryTable}.", StringComparison.Ordinal))
            {
                continue;
            }

            lines.Add(line);
        }

        lines.Sort(StringComparer.Ordinal);
        return lines;
    }
}
