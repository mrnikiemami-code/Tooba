using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Tooba.Fulfillment.Application.Shipping.Models;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Fulfillment.Infrastructure.Shipping;
using Xunit;
using Xunit.Abstractions;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R7 — isolated latest-migration Up/Down proof.
/// </summary>
/// <remarks>
/// <para>
/// Bounded and independent of the blocked whole-schema gate: the ACTUAL historical migrated schema is the before
/// baseline, the corrected latest migration is applied once, and Down must restore that baseline exactly. This is
/// NOT whole-snapshot parity and must never be reported as such.
/// </para>
/// <para>
/// Requires <c>TB196R7_CONNECTION</c> pointing at the disposable task container; the target is parsed with the same
/// fail-closed validation used by the R6 fixture and live provenance is asserted before any DDL.
/// </para>
/// </remarks>
public sealed class TypeCatalogIsolatedMigrationTests(ITestOutputHelper output)
{
    private const string PreviousMigration = "20261016120000_AddFulfillmentShippingGroups";
    private const string LatestMigration = "20261022101415_ShippingTypeCatalog";
    private static readonly Guid Fa = Guid.Parse("01900000-0000-7000-8000-00000000cc01");

    [Fact]
    public async Task Corrected_latest_migration_adds_only_the_two_owned_tables_and_down_restores_the_baseline()
    {
        var raw = Environment.GetEnvironmentVariable("TB196R7_CONNECTION");
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException(
                "TB196R7_CONNECTION is not set: the isolated PostgreSQL Up/Down proof must run against its "
                + "task-owned disposable target and is deliberately not silently skipped.");
        }

        var target = TypeCatalogPostgresSchemaFixture.ValidateTarget(raw);

        await using (var connection = new NpgsqlConnection(target.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "select current_database(), current_user";
            await using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal(TypeCatalogPostgresSchemaFixture.PrimaryDatabase, reader.GetString(0));
            Assert.Equal("tb196", reader.GetString(1));
        }

        await using var db = NewContext();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(PreviousMigration);

        var legacyBefore = await LegacyManifestAsync();
        Assert.NotEmpty(legacyBefore);
        Assert.False(await LatestAppliedAsync());

        await migrator.MigrateAsync(LatestMigration);
        Assert.True(await LatestAppliedAsync(), "the corrected latest migration must record its history row");

        // Only the two owned tables are added; nothing legacy is removed, renamed or changed.
        var afterUp = await LegacyManifestAsync();
        Assert.Equal(legacyBefore, afterUp);

        var typeObjects = await TypeObjectsAsync();
        Assert.NotEmpty(typeObjects);
        Assert.All(typeObjects, line =>
            Assert.True(
                line.Contains("shipping_types", StringComparison.Ordinal)
                || line.Contains("shipping_type_translations", StringComparison.Ordinal),
                $"unexpected non-type object: {line}"));

        // No quoted PascalCase/CamelCase column survived the repair.
        await using (var connection = new NpgsqlConnection(target.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                select count(*)
                from pg_attribute a
                join pg_class c on c.oid = a.attrelid
                join pg_namespace n on n.oid = c.relnamespace
                where n.nspname = 'fulfillment'
                  and c.relname in ('shipping_types', 'shipping_type_translations')
                  and a.attnum > 0 and not a.attisdropped
                  and a.attname <> lower(a.attname)
                """;
            Assert.Equal(0L, Convert.ToInt64(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture));
        }

        // Canonical identifiers, PK/FK/index/CHECK identities actually exist in PostgreSQL.
        Assert.Contains(typeObjects, line => line.Contains("pk_shipping_types", StringComparison.Ordinal));
        Assert.Contains(typeObjects, line => line.Contains("pk_shipping_type_translations", StringComparison.Ordinal));
        Assert.Contains(typeObjects, line => line.Contains(
            "fk_shipping_type_translations_shipping_types_shipping_type_id", StringComparison.Ordinal));
        Assert.Contains(typeObjects, line => line.Contains("ix_shipping_types_code", StringComparison.Ordinal));
        Assert.Contains(typeObjects, line => line.Contains("ix_shipping_types_active_sort", StringComparison.Ordinal));
        Assert.Contains(typeObjects, line => line.Contains(
            "ix_shipping_type_translations_type_language", StringComparison.Ordinal));

        // Fresh-context own-store smoke: ORM column compatibility only (no storage/race acceptance here).
        var clock = new MutableClock(new DateTimeOffset(2026, 10, 22, 12, 0, 0, TimeSpan.Zero));
        Guid createdId;
        await using (var write = NewContext())
        {
            var store = new ShippingTypeStore(write, clock, new SequentialIdGenerator());
            createdId = await store.CreateAsync(
                "smoke_type",
                0,
                [new ShippingTypePreparedTranslation(Fa, "نوع آزمایشی", "بدون رقابت")],
                CancellationToken.None);
        }

        await using (var read = NewContext())
        {
            var store = new ShippingTypeStore(read, clock, new SequentialIdGenerator());
            var view = await store.GetAsync(createdId, Fa, CancellationToken.None);
            Assert.NotNull(view);
            Assert.Equal("smoke_type", view!.Code);
            Assert.Equal(0, view.SortOrder);
            Assert.False(view.IsActive);
            Assert.Equal(1, view.Revision);
            var translation = Assert.Single(view.Translations);
            Assert.Equal("نوع آزمایشی", translation.Name);
            Assert.Equal("بدون رقابت", translation.Description);
        }

        // Real Down restores the exact pre-type baseline.
        await migrator.MigrateAsync(PreviousMigration);
        Assert.False(await LatestAppliedAsync());
        Assert.Empty(await TypeObjectsAsync());
        Assert.Equal(legacyBefore, await LegacyManifestAsync());

        output.WriteLine($"legacy objects={legacyBefore.Count} type objects={typeObjects.Count}");
        var directory = Environment.GetEnvironmentVariable("TB196R7_EVIDENCE_DIR");
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "isolated-legacy-before.txt"), Lines(legacyBefore));
            File.WriteAllText(Path.Combine(directory, "isolated-type-objects-after-up.txt"), Lines(typeObjects));
            File.WriteAllText(Path.Combine(directory, "isolated-legacy-after-down.txt"), Lines(await LegacyManifestAsync()));
        }
    }

    private FulfillmentDbContext NewContext()
    {
        var raw = Environment.GetEnvironmentVariable("TB196R7_CONNECTION")!;
        var options = new DbContextOptionsBuilder<FulfillmentDbContext>();
        Tooba.Persistence.ToobaNpgsql.ConfigureModuleContext(
            options,
            raw,
            FulfillmentDbContext.Schema,
            typeof(FulfillmentDbContext));
        return new FulfillmentDbContext(options.Options);
    }

    private async Task<bool> LatestAppliedAsync()
    {
        await using var connection = new NpgsqlConnection(
            TypeCatalogPostgresSchemaFixture.ValidateTarget(Environment.GetEnvironmentVariable("TB196R7_CONNECTION")!)
                .ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "select count(*) from fulfillment.__ef_migrations_history where migration_id = @id";
        command.Parameters.AddWithValue("id", LatestMigration);
        return Convert.ToInt64(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture) > 0;
    }

    /// <summary>Objects that belong to the two owned type tables only.</summary>
    private static async Task<List<string>> TypeObjectsAsync()
    {
        var all = await ManifestAsync();
        return all
            .Where(line => line.Contains("shipping_types", StringComparison.Ordinal)
                           || line.Contains("shipping_type_translations", StringComparison.Ordinal))
            .ToList();
    }

    /// <summary>Legacy manifest: excludes only the two owned tables and migration-history objects.</summary>
    private static async Task<List<string>> LegacyManifestAsync()
    {
        var all = await ManifestAsync("shipping_types", "shipping_type_translations", "__ef_migrations_history");
        return all;
    }

    private static async Task<List<string>> ManifestAsync(params string[] excludedTokens)
    {
        var target = TypeCatalogPostgresSchemaFixture.ValidateTarget(
            Environment.GetEnvironmentVariable("TB196R7_CONNECTION")!);
        var lines = new List<string>();
        await using var connection = new NpgsqlConnection(target.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
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
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var line = string.Join(
                ' ',
                reader.GetString(0).Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries));
            if (excludedTokens.Any(token => line.Contains(token, StringComparison.Ordinal)))
            {
                continue;
            }

            lines.Add(line);
        }

        lines.Sort(StringComparer.Ordinal);
        return lines;
    }

    private static string Lines(IEnumerable<string> values) =>
        string.Join(Environment.NewLine, values) + Environment.NewLine;
}
