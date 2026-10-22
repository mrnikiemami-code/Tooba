using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R11 — the canonical Fulfillment model now describes twelve
/// objects that ALREADY exist in the historical database: two <c>shipping_methods</c> CHECK constraints and ten
/// historical FK/index names.
/// </summary>
/// <remarks>
/// <para>
/// These are model facts only: no migration was generated, no database object is renamed, and the historical
/// protection (a couple of raw-SQL CHECKs and short object names) is preserved rather than dropped. Naming and
/// check identity are asserted with full shape (ordered columns, uniqueness, partial filter, FK principal and
/// delete action) so a rename or a lost CHECK cannot slip through.
/// </para>
/// <para>
/// The negative cases prove the comparison itself is not filtering names or checks out: the same routine that is
/// green for the real model reports a difference as soon as one name changes or one CHECK disappears.
/// </para>
/// </remarks>
public sealed class HistoricalObjectNameAndCheckMappingTests
{
    private const string ShippingMethodCodeCheck =
        "length(method_code) BETWEEN 1 AND 32 AND method_code = lower(method_code) AND method_code !~ '[^a-z0-9_:]'";

    private const string ShippingMethodOptionCheck =
        "legacy_option_code IS NULL OR (length(btrim(legacy_option_code)) BETWEEN 1 AND 64"
        + " AND legacy_option_code = lower(legacy_option_code))";

    /// <summary>The ten physical names accepted in R9 with their exact shapes.</summary>
    private static readonly string[] ExpectedObjects =
    [
        "fk shipping_method_translations fk_shipping_method_translations_method"
        + " (shipping_method_id) -> shipping_methods(shipping_method_id) ondelete=Cascade",
        "index consolidated_package_members ix_consolidated_package_members_package_id (consolidated_package_id) unique=False filter=-",
        "index consolidated_package_members ix_consolidated_package_members_shipment_active (shipment_id) unique=True filter=released_at IS NULL",
        "index shipping_groups ix_shipping_groups_fulfillment_group (fulfillment_id,shipping_group_id) unique=True filter=-",
        "index shipping_method_translations ix_shipping_method_translations_method_lang (shipping_method_id,language_id) unique=True filter=-",
        "index shipping_methods ux_shipping_methods_legacy_binding (legacy_service_code,legacy_option_code) unique=True filter=legacy_option_code IS NOT NULL",
        "index shipping_service_option_translations ix_shipping_service_option_translations_option_lang (shipping_service_option_id,language_id) unique=True filter=-",
        "index shipping_service_options ix_shipping_service_options_service_code (shipping_service_id,code) unique=True filter=-",
        "index shipping_service_options ix_shipping_service_options_service_id (shipping_service_id) unique=False filter=-",
        "index shipping_service_translations ix_shipping_service_translations_service_lang (shipping_service_id,language_id) unique=True filter=-",
    ];

    [Fact]
    public void Model_pins_the_ten_historical_physical_names_with_unchanged_shapes()
    {
        var actual = DescribeObjects();
        Assert.Equal(ExpectedObjects.Order(StringComparer.Ordinal), actual);
    }

    [Fact]
    public void Model_describes_the_two_existing_shipping_method_checks_verbatim()
    {
        using var context = CanonicalContext();
        var checks = Checks(context);

        Assert.Equal(Normalize(ShippingMethodCodeCheck), Normalize(checks["ck_shipping_methods_code_canonical"]));
        Assert.Equal(Normalize(ShippingMethodOptionCheck), Normalize(checks["ck_shipping_methods_option_code_canonical"]));
        Assert.Equal(2, checks.Count);
    }

    [Fact]
    public void A_renamed_object_or_a_missing_check_is_reported_not_filtered()
    {
        var actual = DescribeObjects();
        var expected = ExpectedObjects.Order(StringComparer.Ordinal).ToList();
        Assert.Empty(Compare(expected, actual));

        // Rename one pinned index (the historical short name replaced by the EF convention name).
        var renamed = expected
            .Select(x => x.Replace(
                "ix_shipping_service_options_service_id",
                "ix_shipping_service_options_shipping_service_id",
                StringComparison.Ordinal))
            .ToList();
        var renameDifferences = Compare(renamed, actual);
        Assert.Contains(renameDifferences, x => x.Contains("ix_shipping_service_options_shipping_service_id", StringComparison.Ordinal));
        Assert.Contains(renameDifferences, x => x.Contains("ix_shipping_service_options_service_id", StringComparison.Ordinal));

        // Drop one object entirely (a deleted or never-created index).
        var missing = expected
            .Where(x => !x.Contains("ux_shipping_methods_legacy_binding", StringComparison.Ordinal))
            .ToList();
        Assert.Single(Compare(missing, actual));

        // A dropped CHECK is detected by the same check comparison.
        using var context = CanonicalContext();
        var checks = Checks(context);
        var withoutOne = checks
            .Where(x => x.Key != "ck_shipping_methods_option_code_canonical")
            .ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
        Assert.NotEmpty(CompareChecks(checks, withoutOne));
        Assert.Empty(CompareChecks(checks, checks));

        // A silently widened CHECK body is detected too (no semantic filtering).
        var relaxed = new Dictionary<string, string>(checks, StringComparer.Ordinal)
        {
            ["ck_shipping_methods_code_canonical"] = "length(method_code) BETWEEN 1 AND 32",
        };
        Assert.NotEmpty(CompareChecks(checks, relaxed));
    }

    // ---------------------------------------------------------------------------------------------

    private static List<string> DescribeObjects()
    {
        using var context = CanonicalContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var descriptions = new List<string>();
        foreach (var entity in model.GetEntityTypes())
        {
            var table = entity.GetTableName();
            if (table is null)
            {
                continue;
            }

            foreach (var index in entity.GetIndexes())
            {
                var name = index.GetDatabaseName() ?? index.GetDefaultDatabaseName();
                if (!name.StartsWith("ix_", StringComparison.Ordinal)
                    && !name.StartsWith("ux_", StringComparison.Ordinal))
                {
                    continue;
                }

                if (name is not ("ix_consolidated_package_members_package_id"
                    or "ix_consolidated_package_members_shipment_active"
                    or "ix_shipping_groups_fulfillment_group"
                    or "ix_shipping_method_translations_method_lang"
                    or "ux_shipping_methods_legacy_binding"
                    or "ix_shipping_service_option_translations_option_lang"
                    or "ix_shipping_service_options_service_code"
                    or "ix_shipping_service_options_service_id"
                    or "ix_shipping_service_translations_service_lang"))
                {
                    continue;
                }

                descriptions.Add(
                    $"index {table} {name} ({string.Join(",", index.Properties.Select(p => p.GetColumnName()))}) "
                    + $"unique={index.IsUnique} filter={index.GetFilter() ?? "-"}");
            }

            foreach (var foreignKey in entity.GetForeignKeys())
            {
                var name = foreignKey.GetConstraintName();
                if (name != "fk_shipping_method_translations_method")
                {
                    continue;
                }

                descriptions.Add(
                    $"fk {table} {name} "
                    + $"({string.Join(",", foreignKey.Properties.Select(p => p.GetColumnName()))}) -> "
                    + $"{foreignKey.PrincipalEntityType.GetTableName()}"
                    + $"({string.Join(",", foreignKey.PrincipalKey.Properties.Select(p => p.GetColumnName()))}) "
                    + $"ondelete={foreignKey.DeleteBehavior}");
            }
        }

        descriptions.Sort(StringComparer.Ordinal);
        return descriptions;
    }

    private static Dictionary<string, string> Checks(DbContext context)
    {
        var model = context.GetService<IDesignTimeModel>().Model;
        var entity = model.FindEntityType("Tooba.Fulfillment.Domain.Aggregates.ShippingMethod")!;
        return entity.GetCheckConstraints().ToDictionary(x => x.Name, x => x.Sql, StringComparer.Ordinal);
    }

    private static List<string> Compare(IReadOnlyList<string> expected, IReadOnlyList<string> actual)
    {
        var differences = new List<string>();
        differences.AddRange(expected.Except(actual, StringComparer.Ordinal).Select(x => $"missing: {x}"));
        differences.AddRange(actual.Except(expected, StringComparer.Ordinal).Select(x => $"unexpected: {x}"));
        return differences;
    }

    private static List<string> CompareChecks(
        IReadOnlyDictionary<string, string> expected,
        IReadOnlyDictionary<string, string> actual)
    {
        var differences = new List<string>();
        foreach (var name in expected.Keys.Union(actual.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            var hasExpected = expected.TryGetValue(name, out var expectedSql);
            var hasActual = actual.TryGetValue(name, out var actualSql);
            if (!hasExpected || !hasActual || Normalize(expectedSql!) != Normalize(actualSql!))
            {
                differences.Add($"check {name} differs");
            }
        }

        return differences;
    }

    private static string Normalize(string sql) =>
        string.Join(' ', sql.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

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
}
