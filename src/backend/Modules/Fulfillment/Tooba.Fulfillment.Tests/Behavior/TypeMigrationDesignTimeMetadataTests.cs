using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Xunit;
using Xunit.Abstractions;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R7 — design-time factory versus canonical runtime configuration.
/// </summary>
/// <remarks>
/// The module's design-time factory must reuse the canonical production module configuration, so the relational
/// metadata used to scaffold/repair migrations for the two type entities is the metadata the runtime provider
/// actually builds. This is type-surface parity only; it is not proof that the complete legacy snapshot or model
/// is equivalent.
/// </remarks>
public sealed class TypeMigrationDesignTimeMetadataTests(ITestOutputHelper output)
{
    private static readonly string[] TypeEntities =
    [
        "Tooba.Fulfillment.Domain.Aggregates.ShippingType",
        "Tooba.Fulfillment.Domain.Aggregates.ShippingTypeTranslation",
    ];

    [Fact]
    public void Design_time_factory_matches_canonical_runtime_type_metadata()
    {
        var factoryModel = new FulfillmentDbContextFactory()
            .CreateDbContext([])
            .GetService<IDesignTimeModel>()
            .Model;
        var runtimeModel = CanonicalRuntimeContext().GetService<IDesignTimeModel>().Model;

        var factory = DescribeTypeSurface(factoryModel);
        var runtime = DescribeTypeSurface(runtimeModel);

        Assert.NotEmpty(runtime);
        Assert.Equal(runtime, factory);
        output.WriteLine(string.Join(Environment.NewLine, runtime));
    }

    [Fact]
    public void Canonical_type_surface_uses_snake_case_identifiers()
    {
        var surface = DescribeTypeSurface(CanonicalRuntimeContext().GetService<IDesignTimeModel>().Model);
        var runtime = string.Join(Environment.NewLine, surface);

        Assert.Contains("table: fulfillment.shipping_types", runtime);
        Assert.Contains("table: fulfillment.shipping_type_translations", runtime);
        Assert.Contains("column: shipping_type_id uuid not-null", runtime);
        Assert.Contains("column: code character varying(64) not-null", runtime);
        Assert.Contains("column: sort_order integer not-null", runtime);
        Assert.Contains("column: is_active boolean not-null", runtime);
        Assert.Contains("column: revision bigint not-null", runtime);
        Assert.Contains("column: created_at timestamp with time zone not-null", runtime);
        Assert.Contains("column: updated_at timestamp with time zone not-null", runtime);
        Assert.Contains("column: shipping_type_translation_id uuid not-null", runtime);
        Assert.Contains("column: language_id uuid not-null", runtime);
        Assert.Contains("column: name character varying(128) not-null", runtime);
        Assert.Contains("column: description character varying(512) null", runtime);
        Assert.Contains("index: ix_shipping_types_code unique", runtime);
        Assert.Contains("index: ix_shipping_types_active_sort", runtime);
        Assert.Contains("index: ix_shipping_type_translations_type_language unique", runtime);
        Assert.DoesNotContain("\"Code\"", runtime);
        Assert.DoesNotContain("\"SortOrder\"", runtime);
    }

    [Fact]
    public void Canonical_create_script_for_the_two_type_tables_is_recorded()
    {
        // Offline: GenerateCreateScript does not open a connection. The excerpt is the authoritative canonical
        // DDL for the two owned tables and is used to repair the unshipped latest migration.
        var script = CanonicalRuntimeContext().Database.GenerateCreateScript();
        var excerpt = new StringBuilder();
        foreach (var statement in script.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            if (statement.Contains("shipping_type", StringComparison.OrdinalIgnoreCase))
            {
                excerpt.Append(statement.Trim()).Append(';').AppendLine().AppendLine();
            }
        }

        var text = excerpt.ToString();
        Assert.Contains("shipping_types", text);
        Assert.Contains("shipping_type_translations", text);
        output.WriteLine(text);

        var directory = Environment.GetEnvironmentVariable("TB196R7_EVIDENCE_DIR");
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "canonical-type-ddl.sql"), text);
            File.WriteAllText(
                Path.Combine(directory, "canonical-type-surface.txt"),
                string.Join(Environment.NewLine, DescribeTypeSurface(
                    CanonicalRuntimeContext().GetService<IDesignTimeModel>().Model)));
        }
    }

    private static FulfillmentDbContext CanonicalRuntimeContext()
    {
        var options = new DbContextOptionsBuilder<FulfillmentDbContext>();
        Tooba.Persistence.ToobaNpgsql.ConfigureModuleContext(
            options,
            "Host=127.0.0.1;Port=5432;Database=tooba_design;Username=tooba;Password=dev-placeholder",
            FulfillmentDbContext.Schema,
            typeof(FulfillmentDbContext));
        return new FulfillmentDbContext(options.Options);
    }

    private static List<string> DescribeTypeSurface(IModel model)
    {
        var lines = new List<string>();
        var relational = model.GetRelationalModel();
        foreach (var entityName in TypeEntities)
        {
            var entity = model.FindEntityType(entityName)
                         ?? throw new InvalidOperationException($"{entityName} is not in the model");
            var table = relational.FindTable(entity.GetTableName()!, entity.GetSchema())
                        ?? throw new InvalidOperationException($"{entityName} has no relational table");
            lines.Add($"table: {entity.GetSchema()}.{entity.GetTableName()}");

            foreach (var property in entity.GetProperties())
            {
                var column = table.FindColumn(property.GetColumnName())!;
                lines.Add(
                    $"column: {column.Name} {column.StoreType} "
                    + (column.IsNullable ? "null" : "not-null"));
            }

            var key = entity.GetKeys().Single();
            lines.Add($"primary-key: {key.GetName()} ({string.Join(",", key.Properties.Select(x => x.GetColumnName()))})");

            foreach (var foreignKey in entity.GetForeignKeys())
            {
                lines.Add(
                    $"foreign-key: {foreignKey.GetConstraintName()} "
                    + $"({string.Join(",", foreignKey.Properties.Select(x => x.GetColumnName()))}) -> "
                    + $"{foreignKey.PrincipalEntityType.GetTableName()} "
                    + $"({string.Join(",", foreignKey.PrincipalKey.Properties.Select(x => x.GetColumnName()))})");
            }

            foreach (var index in entity.GetIndexes())
            {
                lines.Add(
                    $"index: {index.GetDatabaseName()} {(index.IsUnique ? "unique " : string.Empty)}"
                    + $"({string.Join(",", index.Properties.Select(x => x.GetColumnName()))})");
            }

            foreach (var check in entity.GetCheckConstraints())
            {
                lines.Add($"check: {check.Name} {check.Sql}");
            }
        }

        return lines;
    }
}
