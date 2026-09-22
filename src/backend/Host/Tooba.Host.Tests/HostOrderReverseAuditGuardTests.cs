using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-TMAR-ORDER-GOLDEN-001-R7 — durable Host Order reverse-audit inventory guard.
/// Discovers Host production OrderDbContext / Order App|Infra|Domain refs + Order-route extras
/// and compares against evidence inventory JSON.
/// </summary>
public sealed class HostOrderReverseAuditGuardTests
{
    private static readonly Regex OrderNamespaceOrDb = new(
        @"OrderDbContext|Tooba\.Order\.Application|Tooba\.Order\.Infrastructure|Tooba\.Order\.Domain",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void Host_Order_reference_inventory_matches_discovered_production_files()
    {
        var root = FindRepoRoot();
        var inventoryPath = Path.Combine(
            root, "docs", "evidence", "TB-TMAR-ORDER-GOLDEN-001-R7", "host-order-reference-inventory.json");
        Assert.True(File.Exists(inventoryPath), inventoryPath);

        using var doc = JsonDocument.Parse(File.ReadAllText(inventoryPath));
        var expected = doc.RootElement.GetProperty("files").EnumerateArray()
            .Select(x => x.GetString()!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        var extras = doc.RootElement.GetProperty("extraFiles").EnumerateArray()
            .Select(x => x.GetString()!)
            .ToArray();

        var discovered = DiscoverHostOrderReferences(root, extras)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            expected.SequenceEqual(discovered, StringComparer.Ordinal),
            "Host Order inventory drift.\nExpected-only: "
            + string.Join("; ", expected.Except(discovered, StringComparer.Ordinal))
            + "\nDiscovered-only: "
            + string.Join("; ", discovered.Except(expected, StringComparer.Ordinal)));
    }

    [Fact]
    public void Host_Order_owned_panel_routes_are_enumerated_in_inventory()
    {
        var root = FindRepoRoot();
        var host = Path.Combine(root, "src", "backend", "Host", "Tooba.Host");
        var inventoryPath = Path.Combine(
            root, "docs", "evidence", "TB-TMAR-ORDER-GOLDEN-001-R7", "host-order-reference-inventory.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(inventoryPath));
        var files = doc.RootElement.GetProperty("files").EnumerateArray()
            .Select(x => x.GetString()!)
            .ToHashSet(StringComparer.Ordinal);

        var admin = File.ReadAllText(Path.Combine(host, "Admin", "AdminPanelEndpoints.cs"));
        Assert.Contains("MapGet(\"/orders\"", admin, StringComparison.Ordinal);
        Assert.Contains(files, f => f.Equals("Admin/AdminPanelEndpoints.cs", StringComparison.Ordinal));

        var customer = File.ReadAllText(Path.Combine(host, "Customer", "CustomerPanelEndpoints.cs"));
        Assert.Contains("MapGet(\"/orders\"", customer, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/orders/{checkoutId:guid}\"", customer, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/orders/{checkoutId:guid}/retry-unpaid\"", customer, StringComparison.Ordinal);
        Assert.Contains(files, f => f.Equals("Customer/CustomerPanelEndpoints.cs", StringComparison.Ordinal));

        var seller = File.ReadAllText(Path.Combine(host, "Seller", "SellerPanelEndpoints.cs"));
        Assert.Contains("MapGet(\"/orders\"", seller, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/orders/{sellerOrderId:guid}\"", seller, StringComparison.Ordinal);
        Assert.Contains(files, f => f.Equals("Seller/SellerPanelEndpoints.cs", StringComparison.Ordinal));

        var program = File.ReadAllText(Path.Combine(host, "Program.cs"));
        Assert.Contains("MapOrderEndpoints()", program, StringComparison.Ordinal);
        Assert.Contains(files, f => f.Equals("Program.cs", StringComparison.Ordinal));
    }

    [Fact]
    public void Host_OrderDbContext_consumers_are_enumerated_in_inventory()
    {
        var root = FindRepoRoot();
        var host = Path.Combine(root, "src", "backend", "Host", "Tooba.Host");
        var inventoryPath = Path.Combine(
            root, "docs", "evidence", "TB-TMAR-ORDER-GOLDEN-001-R7", "host-order-reference-inventory.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(inventoryPath));
        var files = doc.RootElement.GetProperty("files").EnumerateArray()
            .Select(x => x.GetString()!)
            .ToHashSet(StringComparer.Ordinal);

        var dbConsumers = Directory.EnumerateFiles(host, "*.cs", SearchOption.AllDirectories)
            .Select(p => p.Replace('\\', '/'))
            .Where(p => !p.Contains("/obj/", StringComparison.Ordinal) && !p.Contains("/bin/", StringComparison.Ordinal))
            .Where(p => File.ReadAllText(p).Contains("OrderDbContext", StringComparison.Ordinal))
            .Select(p => Relativize(host, p))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(dbConsumers);
        foreach (var consumer in dbConsumers)
        {
            Assert.True(
                files.Contains(consumer),
                "OrderDbContext consumer missing from R7 inventory: " + consumer);
        }
    }

    [Fact]
    public void Reverse_audit_evidence_declares_not_ready_and_r8_next()
    {
        var root = FindRepoRoot();
        var audit = File.ReadAllText(Path.Combine(
            root, "docs", "evidence", "TB-TMAR-ORDER-GOLDEN-001-R7", "order-host-reverse-audit.md"));
        Assert.Contains("NOT_READY_FOR_CLOSURE", audit, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-ORDER-GOLDEN-001-R8", audit, StringComparison.Ordinal);
        Assert.Contains("ILLEGAL_ORDER_AUTHORITY", audit, StringComparison.Ordinal);
        Assert.Contains("ReservationCycleCoordinator", audit, StringComparison.Ordinal);
        Assert.Contains("UnpaidOrderExpiryHostedService", audit, StringComparison.Ordinal);
    }

    [Fact]
    public void R8_removed_Host_cycle_policy_files_are_absent_from_inventory_and_disk()
    {
        var root = FindRepoRoot();
        var host = Path.Combine(root, "src", "backend", "Host", "Tooba.Host");
        Assert.False(File.Exists(Path.Combine(host, "ReservationCycleCoordinator.cs")));
        Assert.False(File.Exists(Path.Combine(host, "ReservationCyclePolicyResolver.cs")));

        var inventoryPath = Path.Combine(
            root, "docs", "evidence", "TB-TMAR-ORDER-GOLDEN-001-R7", "host-order-reference-inventory.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(inventoryPath));
        var files = doc.RootElement.GetProperty("files").EnumerateArray()
            .Select(x => x.GetString()!)
            .ToHashSet(StringComparer.Ordinal);
        Assert.DoesNotContain("ReservationCycleCoordinator.cs", files);
        Assert.DoesNotContain("ReservationCyclePolicyResolver.cs", files);
        Assert.Contains("UnpaidOrderExpiryHostedService.cs", files);

        var r8 = doc.RootElement.GetProperty("r8InventoryUpdate");
        Assert.Equal("TB-TMAR-ORDER-GOLDEN-001-R8", r8.GetProperty("updatedBy").GetString());
    }

    private static IReadOnlyList<string> DiscoverHostOrderReferences(string root, IEnumerable<string> extras)
    {
        var host = Path.Combine(root, "src", "backend", "Host", "Tooba.Host");
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var path in Directory.EnumerateFiles(host, "*.cs", SearchOption.AllDirectories))
        {
            var normalized = path.Replace('\\', '/');
            if (normalized.Contains("/obj/", StringComparison.Ordinal)
                || normalized.Contains("/bin/", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(path);
            if (OrderNamespaceOrDb.IsMatch(text))
            {
                set.Add(Relativize(host, normalized));
            }
        }

        foreach (var extra in extras)
        {
            set.Add(extra.Replace('\\', '/'));
        }

        return set.ToArray();
    }

    private static string Relativize(string hostRoot, string fullPath)
    {
        var root = hostRoot.Replace('\\', '/').TrimEnd('/');
        var full = fullPath.Replace('\\', '/');
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("path outside Host: " + full);
        }

        return full[(root.Length + 1)..];
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "docs", "PROJECT-STATE.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
