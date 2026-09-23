using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Order.Tests.Architecture;

/// <summary>TB-TMAR-ORDER-POSTCLOSURE-STRUCTURE-001 — Endpoints capability foldering + namespace guards.</summary>
public sealed class OrderEndpointOrganizationGuardTests
{
    private static readonly string[] ForbiddenRootCapabilityEndpointFiles =
    [
        "AdminCustomersEndpoints.cs",
        "AdminOrderCompletenessEndpoints.cs",
        "AdminOrderDetailEndpoints.cs",
        "AdminOrderInventoryRecoverySupplyEndpoints.cs",
        "AdminOrderOperationsEndpoints.cs",
        "AdminOrdersGridEndpoints.cs",
        "CustomerOrderEndpoints.cs",
        "SellerOrderEndpoints.cs",
        "StorefrontOrderEndpoints.cs",
    ];

    private static readonly string[] AllowedRootCsFiles =
    [
        "OrderEndpointModule.cs",
    ];

    private static readonly (string RelativePath, string Namespace, string MapToken)[] RequiredAlignments =
    [
        ("Admin/Customers/AdminCustomersEndpoints.cs", "Tooba.Order.Endpoints.Admin.Customers", "AdminCustomersEndpoints.Map"),
        ("Admin/Completeness/AdminOrderCompletenessEndpoints.cs", "Tooba.Order.Endpoints.Admin.Completeness", "AdminOrderCompletenessEndpoints.Map"),
        ("Admin/Detail/AdminOrderDetailEndpoints.cs", "Tooba.Order.Endpoints.Admin.Detail", "AdminOrderDetailEndpoints.Map"),
        ("Admin/InventoryRecovery/AdminOrderInventoryRecoverySupplyEndpoints.cs", "Tooba.Order.Endpoints.Admin.InventoryRecovery", "AdminOrderInventoryRecoverySupplyEndpoints.Map"),
        ("Admin/Operations/AdminOrderOperationsEndpoints.cs", "Tooba.Order.Endpoints.Admin.Operations", "AdminOrderOperationsEndpoints.Map"),
        ("Admin/OrdersGrid/AdminOrdersGridEndpoints.cs", "Tooba.Order.Endpoints.Admin.OrdersGrid", "AdminOrdersGridEndpoints.Map"),
        ("Customer/CustomerOrderEndpoints.cs", "Tooba.Order.Endpoints.Customer", "CustomerOrderEndpoints.Map"),
        ("Seller/SellerOrderEndpoints.cs", "Tooba.Order.Endpoints.Seller", "SellerOrderEndpoints.Map"),
        ("Storefront/StorefrontOrderEndpoints.cs", "Tooba.Order.Endpoints.Storefront", "StorefrontOrderEndpoints.Map"),
    ];

    [Fact]
    public void Forbidden_capability_endpoint_files_are_absent_from_endpoints_root()
    {
        var root = EndpointsRoot();
        foreach (var name in ForbiddenRootCapabilityEndpointFiles)
        {
            Assert.False(File.Exists(Path.Combine(root, name)), $"root still has {name}");
        }
    }

    [Fact]
    public void Remaining_root_cs_files_are_explicitly_allowlisted()
    {
        var rootFiles = Directory.GetFiles(EndpointsRoot(), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(AllowedRootCsFiles, rootFiles);
    }

    [Fact]
    public void Capability_endpoint_files_path_and_namespace_align()
    {
        foreach (var (relative, expectedNs, _) in RequiredAlignments)
        {
            var path = Path.Combine(EndpointsRoot(), relative.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(path), $"missing {relative}");
            var text = File.ReadAllText(path);
            Assert.Contains($"namespace {expectedNs};", text, StringComparison.Ordinal);
            Assert.DoesNotContain("namespace Tooba.Order.Endpoints;", text, StringComparison.Ordinal);
            Assert.DoesNotContain("using Tooba.Order.Endpoints =", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void OrderEndpointModule_maps_each_capability_exactly_once()
    {
        var module = File.ReadAllText(Path.Combine(EndpointsRoot(), "OrderEndpointModule.cs"));
        Assert.Contains("namespace Tooba.Order.Endpoints;", module, StringComparison.Ordinal);

        foreach (var (_, _, mapToken) in RequiredAlignments)
        {
            var count = Regex.Matches(module, Regex.Escape(mapToken)).Count;
            Assert.True(count == 1, $"{mapToken} mapped {count} times (expected 1)");
        }
    }

    [Fact]
    public void Shared_Errors_and_Resources_folders_remain()
    {
        Assert.True(Directory.Exists(Path.Combine(EndpointsRoot(), "Errors")));
        Assert.True(Directory.Exists(Path.Combine(EndpointsRoot(), "Resources")));
        Assert.True(File.Exists(Path.Combine(EndpointsRoot(), "Errors", "OrderErrorCatalogContributor.cs")));
        Assert.True(File.Exists(Path.Combine(EndpointsRoot(), "Resources", "OrderErrorResources.cs")));
    }

    [Fact]
    public void No_namespace_alias_workaround_for_order_endpoints()
    {
        foreach (var file in Directory.GetFiles(EndpointsRoot(), "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            Assert.False(
                Regex.IsMatch(text, @"using\s+\w+\s*=\s*Tooba\.Order\.Endpoints(\.|;)"),
                $"namespace alias workaround in {Path.GetRelativePath(EndpointsRoot(), file)}");
        }
    }

    [Fact]
    public void Host_does_not_duplicate_order_route_ownership()
    {
        var host = Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host");
        var program = File.ReadAllText(Path.Combine(host, "Program.cs"));
        Assert.Contains("MapOrderEndpoints()", program, StringComparison.Ordinal);

        var admin = File.ReadAllText(Path.Combine(host, "Admin", "AdminPanelEndpoints.cs"));
        Assert.DoesNotContain("MapGet(\"/orders\"", admin, StringComparison.Ordinal);

        Assert.False(File.Exists(Path.Combine(host, "Admin", "AdminOrderOperationsEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(host, "Admin", "AdminOrdersGridEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(host, "Admin", "AdminCustomersEndpoints.cs")));
    }

    private static string EndpointsRoot() =>
        Path.Combine(RepoRoot(), "src", "backend", "Modules", "Order", "Tooba.Order.Endpoints");

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "src", "backend", "Tooba.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
