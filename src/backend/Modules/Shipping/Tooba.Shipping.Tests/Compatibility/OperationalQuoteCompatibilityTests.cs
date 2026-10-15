using Xunit;

namespace Tooba.Shipping.Tests.Compatibility;

/// <summary>
/// TB-SHIPPING-FULL-OPERATIONS-076 — the operational quote seam is additive and read-only: no module
/// outside Shipping consumes it, the legacy rate/packaging seams keep their exact shape, and nothing
/// in the new slice introduces a customer pricing cutover or a money-movement type. These guards pin
/// that boundary so a later task cannot quietly turn the new seam into Checkout authority.
/// </summary>
public sealed class OperationalQuoteCompatibilityTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }

    [Fact]
    public void No_module_outside_shipping_references_the_operational_quote_types()
    {
        var backend = Path.Combine(RepoRoot(), "src", "backend");
        var markers = new[]
        {
            "IShippingOperationalQuoteLookup",
            "ShippingOperationalQuote",
            "ShippingOperationalQuoteEvaluator",
            "ShippingScheduleAvailabilityEvaluator",
            "operational-quote",
        };

        var offenders = Directory
            .EnumerateFiles(backend, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedOrTestTree(path))
            .Where(path => !IsShippingModule(path))
            .Where(path => markers.Any(marker =>
                File.ReadAllText(path).Contains(marker, StringComparison.Ordinal)))
            .Select(path => Path.GetRelativePath(RepoRoot(), path).Replace('\\', '/'))
            .ToArray();

        Assert.Empty(offenders);
    }

    [Fact]
    public void The_legacy_read_seams_keep_their_exact_shape()
    {
        var contractsAssembly = typeof(Contracts.Rates.IShippingRateQuoteLookup).Assembly;
        var interfaces = contractsAssembly
            .GetTypes()
            .Where(x => x.IsInterface)
            .Select(x => x.Name)
            .ToArray();

        // The existing seams are untouched: still present, still one method each, still read-only.
        Assert.Contains("IShippingRateQuoteLookup", interfaces);
        Assert.Contains("IShippingPackagingFeeLookup", interfaces);
        Assert.Contains("IShippingShipmentCostLookup", interfaces);
        Assert.Contains("IShippingOperationalQuoteLookup", interfaces);

        // No write seam is exposed to another module by this slice.
        Assert.DoesNotContain("IShippingOperationalQuoteWriter", interfaces);
        Assert.DoesNotContain("IShippingCapacityReservation", interfaces);

        var rate = typeof(Contracts.Rates.IShippingRateQuoteLookup);
        Assert.Single(rate.GetMethods());

        var operational = typeof(Contracts.Operations.IShippingOperationalQuoteLookup);
        Assert.Single(operational.GetMethods());
    }

    [Fact]
    public void The_operational_contract_never_carries_a_success_shaped_zero()
    {
        // Every money field on the composite result is nullable: "not quoted" cannot be expressed as 0.
        var moneyFields = new[]
        {
            nameof(Contracts.Operations.ShippingOperationalQuote.FreightAmount),
            nameof(Contracts.Operations.ShippingOperationalQuote.PackagingAmount),
            nameof(Contracts.Operations.ShippingOperationalQuote.TotalServiceExpense),
            nameof(Contracts.Operations.ShippingOperationalQuote.AmountPayableAtCheckout),
            nameof(Contracts.Operations.ShippingOperationalQuote.AmountCollectOnDelivery),
        };

        foreach (var field in moneyFields)
        {
            var property = typeof(Contracts.Operations.ShippingOperationalQuote).GetProperty(field);
            Assert.NotNull(property);
            Assert.Equal(typeof(decimal?), property!.PropertyType);
        }
    }

    [Fact]
    public void The_operational_slice_adds_no_migration_and_no_money_movement()
    {
        // A read-only composition needs no schema change: the migration chain stays exactly as 075 left it.
        Assert.False(
            File.Exists(Path.Combine(
                RepoRoot(),
                "src",
                "backend",
                "Modules",
                "Shipping",
                "Tooba.Shipping.Infrastructure",
                "Persistence",
                "Migrations",
                "20261015090000_OperationalQuote.cs")));

        var domain = Path.Combine(RepoRoot(), "src", "backend", "Modules", "Shipping");
        var offenders = Directory
            .EnumerateFiles(domain, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedOrTestTree(path))
            .Where(path =>
            {
                // Only executable lines count: the pre-existing XML docs mention IModuleSchemaMigrator
                // precisely to explain WHY none is registered, and that documentation must stay legal.
                var code = File.ReadAllLines(path)
                    .Select(line => line.TrimStart())
                    .Where(line => !line.StartsWith("///", StringComparison.Ordinal)
                                   && !line.StartsWith("//", StringComparison.Ordinal))
                    .ToArray();

                return code.Any(line =>
                    line.Contains("AddModuleSchemaMigrator", StringComparison.Ordinal)
                    || line.Contains("Database.MigrateAsync", StringComparison.Ordinal)
                    || line.Contains("DbTransaction", StringComparison.Ordinal)
                    || line.Contains("BeginTransaction", StringComparison.Ordinal));
            })
            .Select(path => Path.GetRelativePath(RepoRoot(), path).Replace('\\', '/'))
            .ToArray();

        Assert.Empty(offenders);
    }

    [Fact]
    public void Capacity_is_declared_unenforced_by_the_type_system()
    {
        // The only declared enforcement value is "not enforced", so no caller can read a numeric cap as a
        // real reservation until an atomic claim is designed and approved.
        var values = Enum.GetValues<Contracts.Operations.ShippingCapacityEnforcement>();

        Assert.Equal([Contracts.Operations.ShippingCapacityEnforcement.NotEnforced], values);
    }

    private static bool IsShippingModule(string path)
    {
        var relative = Path.GetRelativePath(RepoRoot(), path).Replace('\\', '/');
        return relative.StartsWith("src/backend/Modules/Shipping/", StringComparison.Ordinal);
    }

    private static bool IsGeneratedOrTestTree(string path)
    {
        var relative = Path.GetRelativePath(RepoRoot(), path);
        var segments = relative.Split(
            new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
            StringSplitOptions.RemoveEmptyEntries);
        return segments.Any(segment => segment is "obj" or "bin")
               || segments.Any(segment => segment.EndsWith("Tests", StringComparison.Ordinal));
    }
}
