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
        @"OrderDbContext|Tooba\.Order\.Application|Tooba\.Order\.Infrastructure|Tooba\.Order\.Domain|Tooba\.Order\.Contracts|Tooba\.Order\.Endpoints",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex OrderTypeDeclaration = new(
        @"(?m)^\s*(?:public|internal|private|protected)?\s*(?:sealed\s+|abstract\s+|static\s+|partial\s+)*(?:class|record|struct|interface|enum)\s+(\w*Order\w*)\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex OrderRouteRegistration = new(
        @"Map(?:Get|Post|Put|Delete)\(""/[^""]*orders",
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
        Assert.DoesNotContain("MapGet(\"/orders\"", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGet(\"/customers\"", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost(\"/customers/query\"", admin, StringComparison.Ordinal);
        Assert.Contains(files, f => f.Equals("Admin/AdminPanelEndpoints.cs", StringComparison.Ordinal));

        var orderAdminOrders = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Modules", "Order", "Tooba.Order.Endpoints", "AdminOrdersGridEndpoints.cs"));
        Assert.Contains("MapGet(\"/v1/admin/orders\"", orderAdminOrders, StringComparison.Ordinal);
        Assert.Contains("ListAdminOrdersQuery", orderAdminOrders, StringComparison.Ordinal);

        var orderAdminCustomers = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Modules", "Order", "Tooba.Order.Endpoints", "AdminCustomersEndpoints.cs"));
        Assert.Contains("MapGet(\"/v1/admin/customers\"", orderAdminCustomers, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/v1/admin/customers/query\"", orderAdminCustomers, StringComparison.Ordinal);

        var customer = File.ReadAllText(Path.Combine(host, "Customer", "CustomerPanelEndpoints.cs"));
        Assert.DoesNotContain("MapGet(\"/orders\"", customer, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost(\"/orders/{checkoutId:guid}/retry-unpaid\"", customer, StringComparison.Ordinal);
        Assert.Contains("GetCustomerOrderDashboardSummaryQuery", customer, StringComparison.Ordinal);
        Assert.Contains(files, f => f.Equals("Customer/CustomerPanelEndpoints.cs", StringComparison.Ordinal));
        Assert.Contains(files, f => f.Equals("Customer/HostOrderCustomerAuthorizer.cs", StringComparison.Ordinal));

        var orderCustomerEndpoints = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Modules", "Order", "Tooba.Order.Endpoints", "CustomerOrderEndpoints.cs"));
        Assert.Contains("MapGet(\"/orders\"", orderCustomerEndpoints, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/orders/{checkoutId:guid}/retry-unpaid\"", orderCustomerEndpoints, StringComparison.Ordinal);

        var seller = File.ReadAllText(Path.Combine(host, "Seller", "SellerPanelEndpoints.cs"));
        Assert.DoesNotContain("MapGet(\"/orders\"", seller, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGet(\"/orders/{sellerOrderId:guid}\"", seller, StringComparison.Ordinal);
        Assert.Contains("GetSellerOrderDashboardSummaryQuery", seller, StringComparison.Ordinal);
        Assert.Contains(files, f => f.Equals("Seller/SellerPanelEndpoints.cs", StringComparison.Ordinal));
        Assert.Contains(files, f => f.Equals("Seller/HostOrderSellerAuthorizer.cs", StringComparison.Ordinal));
        Assert.Contains(files, f => f.Equals("Seller/HostSellerOrderViewAccessReader.cs", StringComparison.Ordinal));

        var orderSellerEndpoints = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Modules", "Order", "Tooba.Order.Endpoints", "SellerOrderEndpoints.cs"));
        Assert.Contains("MapGet(\"/orders\"", orderSellerEndpoints, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/orders/{sellerOrderId:guid}\"", orderSellerEndpoints, StringComparison.Ordinal);

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

        Assert.DoesNotContain("Customer/CustomerPanelComposer.cs", dbConsumers);
        Assert.DoesNotContain("Seller/SellerPanelComposer.cs", dbConsumers);
        Assert.DoesNotContain("Admin/AdminPanelComposer.cs", dbConsumers);
        Assert.DoesNotContain("Grid/AdminCustomersGridQueryEngine.cs", dbConsumers);
        Assert.DoesNotContain("Grid/AdminSellersGridQueryEngine.cs", dbConsumers);
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

    [Fact]
    public void R9_customer_order_move_is_recorded_in_inventory_without_erasing_r7_r8()
    {
        var root = FindRepoRoot();
        var inventoryPath = Path.Combine(
            root, "docs", "evidence", "TB-TMAR-ORDER-GOLDEN-001-R7", "host-order-reference-inventory.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(inventoryPath));
        var r9 = doc.RootElement.GetProperty("r9InventoryUpdate");
        Assert.Equal("TB-TMAR-ORDER-GOLDEN-001-R9", r9.GetProperty("updatedBy").GetString());
        Assert.Contains(
            "CustomerPanel Order list/detail/retry",
            r9.GetProperty("note").GetString()!,
            StringComparison.Ordinal);
        Assert.True(doc.RootElement.TryGetProperty("r8InventoryUpdate", out _));

        var hostComposer = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Host", "Tooba.Host", "Customer", "CustomerPanelComposer.cs"));
        Assert.DoesNotContain("OrderDbContext", hostComposer, StringComparison.Ordinal);
    }

    [Fact]
    public void R10_seller_order_move_is_recorded_in_inventory_without_erasing_r7_r8_r9()
    {
        var root = FindRepoRoot();
        var inventoryPath = Path.Combine(
            root, "docs", "evidence", "TB-TMAR-ORDER-GOLDEN-001-R7", "host-order-reference-inventory.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(inventoryPath));
        var r10 = doc.RootElement.GetProperty("r10InventoryUpdate");
        Assert.Equal("TB-TMAR-ORDER-GOLDEN-001-R10", r10.GetProperty("updatedBy").GetString());
        Assert.Contains(
            "SellerPanel Order list/detail/dashboard",
            r10.GetProperty("note").GetString()!,
            StringComparison.Ordinal);
        Assert.True(doc.RootElement.TryGetProperty("r9InventoryUpdate", out _));
        Assert.True(doc.RootElement.TryGetProperty("r8InventoryUpdate", out _));

        var hostComposer = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Host", "Tooba.Host", "Seller", "SellerPanelComposer.cs"));
        Assert.DoesNotContain("OrderDbContext", hostComposer, StringComparison.Ordinal);
    }

    [Fact]
    public void R11_admin_order_residual_move_is_recorded_in_inventory_without_erasing_r7_r10()
    {
        var root = FindRepoRoot();
        var inventoryPath = Path.Combine(
            root, "docs", "evidence", "TB-TMAR-ORDER-GOLDEN-001-R7", "host-order-reference-inventory.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(inventoryPath));
        var r11 = doc.RootElement.GetProperty("r11InventoryUpdate");
        Assert.Equal("TB-TMAR-ORDER-GOLDEN-001-R11", r11.GetProperty("updatedBy").GetString());
        Assert.Contains(
            "AdminPanel Order residuals",
            r11.GetProperty("note").GetString()!,
            StringComparison.Ordinal);
        Assert.True(doc.RootElement.TryGetProperty("r10InventoryUpdate", out _));
        Assert.True(doc.RootElement.TryGetProperty("r9InventoryUpdate", out _));
        Assert.True(doc.RootElement.TryGetProperty("r8InventoryUpdate", out _));

        var host = Path.Combine(root, "src", "backend", "Host", "Tooba.Host");
        Assert.DoesNotContain(
            "OrderDbContext",
            File.ReadAllText(Path.Combine(host, "Admin", "AdminPanelComposer.cs")),
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "OrderDbContext",
            File.ReadAllText(Path.Combine(host, "Grid", "AdminSellersGridQueryEngine.cs")),
            StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(host, "Grid", "AdminCustomersGridQueryEngine.cs")));
        Assert.False(File.Exists(Path.Combine(host, "Admin", "AdminReservationCycleMapper.cs")));
        var files = doc.RootElement.GetProperty("files").EnumerateArray()
            .Select(x => x.GetString()!)
            .ToHashSet(StringComparer.Ordinal);
        Assert.DoesNotContain("Grid/AdminCustomersGridQueryEngine.cs", files);
        Assert.DoesNotContain("Admin/AdminReservationCycleMapper.cs", files);
    }

    [Fact]
    public void R11R1_symbolic_sweep_deleted_dead_completeness_models_and_records_inventory()
    {
        var root = FindRepoRoot();
        var host = Path.Combine(root, "src", "backend", "Host", "Tooba.Host");
        Assert.False(File.Exists(Path.Combine(host, "Admin", "AdminOrderCompletenessModels.cs")));

        var inventoryPath = Path.Combine(
            root, "docs", "evidence", "TB-TMAR-ORDER-GOLDEN-001-R7", "host-order-reference-inventory.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(inventoryPath));
        var r11r1 = doc.RootElement.GetProperty("r11r1InventoryUpdate");
        Assert.Equal("TB-TMAR-ORDER-GOLDEN-001-R11-R1", r11r1.GetProperty("updatedBy").GetString());
        Assert.Contains(
            "AdminOrderCompletenessModels",
            r11r1.GetProperty("note").GetString()!,
            StringComparison.Ordinal);
        Assert.True(doc.RootElement.TryGetProperty("r11InventoryUpdate", out _));

        var files = doc.RootElement.GetProperty("files").EnumerateArray()
            .Select(x => x.GetString()!)
            .ToHashSet(StringComparer.Ordinal);
        Assert.DoesNotContain("Admin/AdminOrderCompletenessModels.cs", files);
        Assert.Contains(files, f => f.Equals("UnpaidOrderExpiryHostOptions.cs", StringComparison.Ordinal));
        Assert.Contains(files, f => f.Equals("Admin/ProductWorkspaceModels.cs", StringComparison.Ordinal));

        var audit = File.ReadAllText(Path.Combine(
            root, "docs", "evidence", "TB-TMAR-ORDER-GOLDEN-001-R11-R1", "final-host-symbolic-audit.md"));
        Assert.Contains("ILLEGAL_ORDER_AUTHORITY", audit, StringComparison.Ordinal);
        Assert.Contains("DEAD_ORDER_RESIDUE", audit, StringComparison.Ordinal);
        Assert.Contains("| **0** |", audit, StringComparison.Ordinal);
        Assert.Contains("AdminOrderCompletenessModels.cs", audit, StringComparison.Ordinal);
        Assert.Contains("AdminProductMediaOrderRequest", audit, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_has_no_dead_AdminOrderCompleteness_DTO_types()
    {
        var host = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host");
        var leaks = Directory.EnumerateFiles(host, "*.cs", SearchOption.AllDirectories)
            .Select(p => p.Replace('\\', '/'))
            .Where(p => !p.Contains("/obj/", StringComparison.Ordinal) && !p.Contains("/bin/", StringComparison.Ordinal))
            .Where(p =>
            {
                var text = File.ReadAllText(p);
                return text.Contains("record AdminOrderNoteRequest", StringComparison.Ordinal)
                    || text.Contains("record AdminOrderNoteView", StringComparison.Ordinal)
                    || text.Contains("record AdminOperationalHistoryEntry", StringComparison.Ordinal)
                    || text.Contains("record AdminOperationalHistoryPage", StringComparison.Ordinal);
            })
            .Select(p => Relativize(host, p))
            .ToList();
        Assert.True(leaks.Count == 0, "dead Host Order completeness DTOs: " + string.Join("; ", leaks));
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

            var relative = Relativize(host, normalized);
            var fileName = Path.GetFileName(normalized);
            var text = File.ReadAllText(path);
            if (OrderNamespaceOrDb.IsMatch(text)
                || OrderTypeDeclaration.IsMatch(text)
                || OrderRouteRegistration.IsMatch(text)
                || fileName.Contains("Order", StringComparison.Ordinal))
            {
                set.Add(relative);
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
