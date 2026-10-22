using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Xunit;
using Xunit.Abstractions;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R8 — offline metadata parity between the checked-in migration
/// metadata artifacts and the unchanged canonical design-time model.
/// </summary>
/// <remarks>
/// <para>
/// Two artifacts must describe the canonical current model exactly: the checked-in
/// <c>FulfillmentDbContextModelSnapshot</c> and the latest migration's generated <c>BuildTargetModel</c>. Both are
/// resolved and initialized through the real EF design-time services and compared with the real
/// <see cref="IMigrationsModelDiffer"/>; no database, container, runner or connection is involved.
/// </para>
/// <para>
/// This proves metadata alignment only. Snapshot/model equality is NOT equality with the historically migrated
/// schema; the recorded historical differences remain open.
/// </para>
/// </remarks>
public sealed class TypeCatalogModelMetadataParityTests(ITestOutputHelper output)
{
    private const string LatestMigration = "20261022101415_ShippingTypeCatalog";

    [Fact]
    public void Snapshot_metadata_matches_the_canonical_design_time_model()
    {
        using var context = CanonicalContext();
        var snapshot = context.GetService<IMigrationsAssembly>().ModelSnapshot;
        Assert.NotNull(snapshot);

        var initializer = context.GetService<IModelRuntimeInitializer>();
        var snapshotModel = initializer.Initialize(
            snapshot!.Model,
            designTime: true,
            validationLogger: null);
        var currentModel = context.GetService<IDesignTimeModel>().Model;

        var operations = Describe(context, snapshotModel, currentModel);
        Dump("snapshot-vs-model.txt", operations);
        Assert.True(
            operations.Count == 0,
            $"snapshot metadata must match the canonical model; {operations.Count} operation(s):\n"
            + string.Join(Environment.NewLine, operations.Take(40)));

        Assert.Equal(DescribeSemantics(currentModel), DescribeSemantics(snapshotModel));
    }

    [Fact]
    public void Latest_migration_target_model_matches_the_canonical_design_time_model()
    {
        using var context = CanonicalContext();
        var migrations = context.GetService<IMigrationsAssembly>().Migrations;
        Assert.True(migrations.ContainsKey(LatestMigration), "latest migration must exist in the assembly");

        var migrationType = migrations[LatestMigration];
        var migration = (Migration)Activator.CreateInstance(migrationType)!;
        var initializer = context.GetService<IModelRuntimeInitializer>();
        var targetModel = initializer.Initialize(
            migration.TargetModel,
            designTime: true,
            validationLogger: null);
        Assert.NotNull(targetModel);

        var currentModel = context.GetService<IDesignTimeModel>().Model;
        var operations = Describe(context, targetModel, currentModel);
        Dump("latest-target-vs-model.txt", operations);
        Assert.True(
            operations.Count == 0,
            $"latest TargetModel must match the canonical model; {operations.Count} operation(s):\n"
            + string.Join(Environment.NewLine, operations.Take(40)));

        Assert.Equal(DescribeSemantics(currentModel), DescribeSemantics(targetModel));
        Assert.Equal(DescribeSemantics(targetModel), DescribeSemantics(currentModel));
    }

    [Fact]
    public void Canonical_type_metadata_keeps_corrected_identifiers_and_concurrency_token()
    {
        using var context = CanonicalContext();
        var model = context.GetService<IDesignTimeModel>().Model;

        var type = model.FindEntityType("Tooba.Fulfillment.Domain.Aggregates.ShippingType")!;
        Assert.Equal("shipping_types", type.GetTableName());
        Assert.Equal(
            ["code", "created_at", "is_active", "revision", "shipping_type_id", "sort_order", "updated_at"],
            type.GetProperties().Select(x => x.GetColumnName()).OrderBy(x => x, StringComparer.Ordinal));
        Assert.True(type.FindProperty("Revision")!.IsConcurrencyToken);
        Assert.Equal("pk_shipping_types", type.GetKeys().Single().GetName());
        Assert.Contains(type.GetIndexes(), x => x.GetDatabaseName() == "ix_shipping_types_code" && x.IsUnique);
        Assert.Contains(type.GetIndexes(), x => x.GetDatabaseName() == "ix_shipping_types_active_sort");

        var translation = model.FindEntityType("Tooba.Fulfillment.Domain.Aggregates.ShippingTypeTranslation")!;
        Assert.Equal("shipping_type_translations", translation.GetTableName());
        Assert.Equal("pk_shipping_type_translations", translation.GetKeys().Single().GetName());
        Assert.Equal(
            "fk_shipping_type_translations_shipping_types_shipping_type_id",
            translation.GetForeignKeys().Single().GetConstraintName());
        Assert.Contains(
            translation.GetIndexes(),
            x => x.GetDatabaseName() == "ix_shipping_type_translations_type_language" && x.IsUnique);
        Assert.Contains(translation.GetCheckConstraints(), x => x.Name == "ck_shipping_type_translations_name_nonblank");
        Assert.Contains(type.GetCheckConstraints(), x => x.Name == "ck_shipping_types_code_canonical");

        foreach (var column in type.GetProperties().Select(x => x.GetColumnName())
                     .Concat(translation.GetProperties().Select(x => x.GetColumnName())))
        {
            Assert.Equal(column.ToLowerInvariant(), column);
        }
    }

    private static List<string> Describe(DbContext context, IModel left, IModel right) =>
        context.GetService<IMigrationsModelDiffer>()
            .GetDifferences(left.GetRelationalModel(), right.GetRelationalModel())
            .Select(Describe)
            .ToList();

    private static string Describe(MigrationOperation operation)
    {
        var builder = new StringBuilder(operation.GetType().Name);
        foreach (var property in operation.GetType().GetProperties())
        {
            if (property.PropertyType != typeof(string))
            {
                continue;
            }

            var value = property.GetValue(operation) as string;
            if (!string.IsNullOrEmpty(value))
            {
                builder.Append(' ').Append(property.Name).Append('=').Append(value);
            }
        }

        return builder.ToString();
    }

    /// <summary>Normalized relational semantics: schema/table/column/type/nullability/default + named objects.</summary>
    private static List<string> DescribeSemantics(IModel model)
    {
        var lines = new List<string>();
        foreach (var table in model.GetRelationalModel().Tables)
        {
            lines.Add($"table {table.Schema}.{table.Name}");
            foreach (var column in table.Columns)
            {
                lines.Add(
                    $"  column {table.Schema}.{table.Name}.{column.Name} {column.StoreType} "
                    + (column.IsNullable ? "null" : "not-null")
                    + $" default:{column.DefaultValueSql ?? column.DefaultValue?.ToString() ?? "-"}");
            }

            foreach (var key in table.PrimaryKey is null ? [] : new[] { table.PrimaryKey })
            {
                lines.Add($"  pk {table.Schema}.{table.Name} {key.Name} ({string.Join(",", key.Columns.Select(x => x.Name))})");
            }

            foreach (var unique in table.UniqueConstraints)
            {
                lines.Add(
                    $"  unique {table.Schema}.{table.Name} {unique.Name} ({string.Join(",", unique.Columns.Select(x => x.Name))})");
            }

            foreach (var foreignKey in table.ForeignKeyConstraints)
            {
                lines.Add(
                    $"  fk {table.Schema}.{table.Name} {foreignKey.Name} "
                    + $"({string.Join(",", foreignKey.Columns.Select(x => x.Name))}) -> "
                    + $"{foreignKey.PrincipalTable.Schema}.{foreignKey.PrincipalTable.Name} "
                    + $"({string.Join(",", foreignKey.PrincipalColumns.Select(x => x.Name))})");
            }

            foreach (var check in table.CheckConstraints)
            {
                lines.Add($"  check {table.Schema}.{table.Name} {check.Name} {check.Sql}");
            }

            foreach (var index in table.Indexes)
            {
                lines.Add(
                    $"  index {table.Schema}.{table.Name} {index.Name} {(index.IsUnique ? "unique " : string.Empty)}"
                    + $"({string.Join(",", index.Columns.Select(x => x.Name))})");
            }
        }

        lines.Sort(StringComparer.Ordinal);
        return lines;
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

    private void Dump(string fileName, IReadOnlyList<string> lines)
    {
        output.WriteLine($"{fileName}: {lines.Count} operation(s)");
        var directory = Environment.GetEnvironmentVariable("TB196R8_EVIDENCE_DIR");
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        Directory.CreateDirectory(directory);
        File.WriteAllText(
            Path.Combine(directory, fileName),
            string.Join(Environment.NewLine, lines) + Environment.NewLine);
    }
}
