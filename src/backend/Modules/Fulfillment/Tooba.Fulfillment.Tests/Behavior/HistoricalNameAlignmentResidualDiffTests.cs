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

    private static readonly string[] ExpectedDefaultPairs =
    [
        "column fulfillment.items.quantity_packed numeric(18,6) not-null default:0",
        "column fulfillment.items.quantity_processing numeric(18,6) not-null default:0",
        "column fulfillment.shipments.provider_metadata_version integer not-null default:0",
        "column fulfillment.shipments.shipping_method_code character varying(64) not-null default:''::character varying",
        "column fulfillment.shipments.shipping_method_label character varying(128) not-null default:''::character varying",
    ];

    [Fact]
    public async Task Historical_schema_is_strictly_identical_to_the_current_model_including_the_five_defaults()
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

        // --- strict whole-manifest equality (no classifier, no allow-list) -----------------------------
        var missingInModel = beforeManifest.Except(modelManifest, StringComparer.Ordinal).ToList();
        var unexpectedInModel = modelManifest.Except(beforeManifest, StringComparer.Ordinal).ToList();
        Assert.Empty(missingInModel);
        Assert.Empty(unexpectedInModel);
        Assert.Equal(beforeManifest, modelManifest);
        Assert.Equal(beforeManifest.Order(StringComparer.Ordinal), modelManifest.Order(StringComparer.Ordinal));

        // the five historical default expressions now exist identically on both sides
        foreach (var expected in ExpectedDefaultPairs)
        {
            Assert.Contains(beforeManifest, x => x == expected);
            Assert.Contains(modelManifest, x => x == expected);
        }

        Record($"residual={missingInModel.Count + unexpectedInModel.Count} strict-equal=True defaults-both-sides={ExpectedDefaultPairs.Length}");

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
            File.WriteAllText(Path.Combine(evidence, "strict-diff.txt"), "both directions empty; whole manifests equal" + Environment.NewLine);
            File.WriteAllText(Path.Combine(evidence, "model-only-create.sql"), createScript);
            File.WriteAllText(Path.Combine(evidence, "residual-diff.log"), string.Join(Environment.NewLine, log) + Environment.NewLine);
        }
    }

    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Strict_manifest_comparison_detects_a_changed_default()
    {
        string[] before =
        [
            "column fulfillment.items.quantity_packed numeric(18,6) not-null default:0",
            "column fulfillment.shipments.shipping_method_code character varying(64) not-null default:''::character varying",
        ];
        string[] changedDefault =
        [
            "column fulfillment.items.quantity_packed numeric(18,6) not-null default:0.0",
            "column fulfillment.shipments.shipping_method_code character varying(64) not-null default:'x'::character varying",
        ];

        var differences = TypeCatalogPostgresSchemaFixture.Diff(before, changedDefault);
        Assert.Equal(4, differences.Count);
        Assert.Contains(differences, x => x.Contains("default:0", StringComparison.Ordinal));
        Assert.Contains(differences, x => x.Contains("unexpected-in-actual", StringComparison.Ordinal));
    }

    [Fact]
    public void Exact_history_and_type_identity_never_suppresses_a_same_prefix_non_history_object()
    {
        const string lookalike = "fulfillment.shipping_type_catalog_archive";
        string[] lines =
        [
            $"column {lookalike}.id uuid not-null default:-",
            $"constraint {lookalike} pk_shipping_type_catalog_archive PRIMARY KEY (id)",
            $"index fulfillment CREATE INDEX ix_shipping_type_catalog_archive_id ON {lookalike} USING btree (id)",
            "column fulfillment.shipping_types.code character varying(64) not-null default:-",
            "column fulfillment.shipping_type_translations.name character varying(128) not-null default:-",
            "column fulfillment.__ef_migrations_history.migration_id character varying(150) not-null default:-",
        ];

        // exact identity: only the two OWNED tables are type-owned; a longer same-prefix table is NOT.
        Assert.False(TypeCatalogPostgresSchemaFixture.IsTypeOwned(lines[0]));
        Assert.False(TypeCatalogPostgresSchemaFixture.IsTypeOwned(lines[1]));
        Assert.False(TypeCatalogPostgresSchemaFixture.IsTypeOwned(lines[2]));
        Assert.Equal(lookalike, TypeCatalogPostgresSchemaFixture.OwningTable(lines[0]));
        Assert.True(TypeCatalogPostgresSchemaFixture.IsTypeOwned(lines[3]));
        Assert.True(TypeCatalogPostgresSchemaFixture.IsTypeOwned(lines[4]));

        // the history table is excluded ONLY by its exact identity, so a lookalike column must survive
        var historyOwned = lines.Where(x =>
            TypeCatalogPostgresSchemaFixture.OwningTable(x)
            == $"fulfillment.{TypeCatalogPostgresSchemaFixture.HistoryTable}").ToList();
        var retained = lines.Except(historyOwned, StringComparer.Ordinal).ToList();
        Assert.Single(historyOwned);
        Assert.Contains(lines[0], retained);
        Assert.Contains(lines[1], retained);
        Assert.Contains(lines[2], retained);

        // if the lookalike object disappeared from one side, the strict comparison reports it
        var differences = TypeCatalogPostgresSchemaFixture.Diff(retained, retained[3..]);
        Assert.Equal(3, differences.Count);
        Assert.All(differences, x => Assert.StartsWith("missing-in-actual:", x, StringComparison.Ordinal));
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
            // History objects are already excluded by exact (schema, table) identity in the catalog query.
            lines.Add(reader.GetString(0).ReplaceLineEndings(" "));
        }

        lines.Sort(StringComparer.Ordinal);
        return lines;
    }
}
