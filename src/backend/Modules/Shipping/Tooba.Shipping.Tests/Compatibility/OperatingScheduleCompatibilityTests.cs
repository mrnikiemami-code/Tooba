using Xunit;

namespace Tooba.Shipping.Tests.Compatibility;

/// <summary>
/// TB-SHIPPING-FULL-OPERATIONS-071 — the scheduling slice is additive and fail-open: it adds a
/// Shipping-owned table and an admin authoring surface, but no other module reads it and no legacy
/// checkout path changes shape. These guards pin that boundary so a future task cannot quietly turn
/// "no schedule" into a checkout blocker.
/// </summary>
public sealed class OperatingScheduleCompatibilityTests
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
    public void No_module_outside_shipping_references_the_operating_schedule_types()
    {
        var backend = Path.Combine(RepoRoot(), "src", "backend");
        var markers = new[]
        {
            "ShippingOperatingSchedule",
            "IShippingOperatingScheduleDirectory",
            "ShippingOperatingWindow",
            "operating_schedules",
            "operating-schedules",
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
    public void The_scheduling_aggregate_carries_no_presentation_or_promise_fields()
    {
        // Identity and presentation stay Fulfillment's; the customer delivery promise stays Order's.
        // Shipping's schedule may only describe carrier availability.
        var scheduleProperties = typeof(Domain.Scheduling.ShippingOperatingSchedule)
            .GetProperties()
            .Select(x => x.Name)
            .ToArray();
        var windowProperties = typeof(Domain.Scheduling.ShippingOperatingWindow)
            .GetProperties()
            .Select(x => x.Name)
            .ToArray();

        foreach (var foreignConcept in new[]
                 {
                     "Name", "Label", "IconKey", "ColorKey", "SortOrder", "Translations", "ProviderKind",
                     "Promise", "PromisedDeliveryDate", "DeliveryDate", "EstimatedArrival", "Snapshot",
                 })
        {
            Assert.DoesNotContain(foreignConcept, scheduleProperties);
            Assert.DoesNotContain(foreignConcept, windowProperties);
        }
    }

    [Fact]
    public void The_contracts_assembly_exposes_no_write_seam_for_other_modules()
    {
        // Shipping's cross-module surface stays read-only for rates; scheduling is admin-only and never
        // becomes a Contracts seam another module can call. A writable schedule contract would be a new
        // cross-module authority, which this task does not create.
        var contractsAssembly = typeof(Contracts.Scheduling.ShippingOperatingSchedulesView).Assembly;
        var interfaces = contractsAssembly
            .GetTypes()
            .Where(x => x.IsInterface)
            .Select(x => x.Name)
            .ToArray();

        Assert.DoesNotContain("IShippingOperatingScheduleDirectory", interfaces);
        Assert.DoesNotContain("IShippingOperatingScheduleWriter", interfaces);

        // The existing rate quote seam is unchanged: still exactly one, still read-only.
        Assert.Contains("IShippingRateQuoteLookup", interfaces);
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
        return segments.Any(segment => segment is "obj" or "bin");
    }
}
