using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-SELLER-AMC-001-R4 — durable guard proving the seller dashboard route
/// (<c>GET /v1/seller/dashboard</c>) and its Order-owned composition / Party display-name enrichment
/// were evacuated from <c>Host/Seller</c> into Order (Endpoints -> Application), that the zero-consumer
/// <c>SellerPanelComposer.cs</c> and <c>SellerPanelModels.cs</c> residue is deleted, and that the Host
/// seller surface is now exactly one Host-owned route (<c>/dev-contexts</c>) with two production files.
/// R5 then evacuated that final route and its bootstrap, so Host/Seller is ABSENT and the dashboard
/// invariants hold over the module-owned surfaces.
/// </summary>
public sealed class HostSellerAmcR4GuardTests
{
    private static readonly string[] RetainedHostSellerFiles = [];

    [Fact]
    public void Host_seller_dashboard_is_absent_and_folder_is_gone()
    {
        var hostSeller = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Seller");
        Assert.False(File.Exists(Path.Combine(hostSeller, "SellerPanelComposer.cs")));
        Assert.False(File.Exists(Path.Combine(hostSeller, "SellerPanelModels.cs")));
        Assert.False(File.Exists(Path.Combine(hostSeller, "SellerSettingsEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(hostSeller, "SellerPanelEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(hostSeller, "SellerDevActorBootstrap.cs")));

        // R4 retained two Host/Seller files; R5 evacuated them, so the folder is ABSENT.
        Assert.False(Directory.Exists(hostSeller), "Host/Seller must be absent after R5");
        Assert.Empty(RetainedHostSellerFiles);
    }

    [Fact]
    public void Host_owns_zero_seller_routes_and_the_dev_contexts_route_is_accesscontrol_owned()
    {
        var hostSeller = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Seller");
        Assert.False(Directory.Exists(hostSeller), "Host/Seller must be absent after R5");

        var devContexts = Read(
            "src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Seller/Development/SellerDevContextEndpoints.cs");
        Assert.Contains("MapGet(\"/dev-contexts\"", devContexts, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGet(\"/dashboard\"", devContexts, StringComparison.Ordinal);
        Assert.Equal(1, Regex.Matches(devContexts, @"Map(?:Get|Post|Put|Patch|Delete)\(").Count);
    }

    [Fact]
    public void Host_seller_has_zero_dashboard_layer_leakage_and_zero_order_or_party_application_leakage()
    {
        // R5 removed the whole Host/Seller folder, so dashboard-layer leakage is ZERO by construction.
        var hostSeller = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Seller");
        Assert.False(Directory.Exists(hostSeller), "Host/Seller must be absent after R5");
    }

    [Fact]
    public void Program_no_longer_registers_the_host_seller_composer_dashboard_or_panel_mapping()
    {
        var program = Read("src/backend/Host/Tooba.Host/Program.cs");
        Assert.DoesNotContain("Tooba.Host.Seller.SellerPanelComposer", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapSellerPanelEndpoints", program, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host.Seller", program, StringComparison.Ordinal);
        Assert.Contains("MapOrderEndpoints()", program, StringComparison.Ordinal);
        Assert.Contains("MapAccessControlModuleEndpoints()", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Order_endpoints_own_the_seller_dashboard_route_exactly_once()
    {
        var endpoints = Read("src/backend/Modules/Order/Tooba.Order.Endpoints/Seller/SellerDashboardEndpoints.cs");
        Assert.Contains("MapGet(\"/dashboard\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("ISender", endpoints, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", endpoints, StringComparison.Ordinal);
        Assert.Contains("IOrderSellerAuthorizer", endpoints, StringComparison.Ordinal);
        Assert.Contains("GetSellerOrderDashboardSummaryQuery", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Json", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("Party.Application", endpoints, StringComparison.Ordinal);

        // Exactly one route mapping in the Order-owned seller dashboard surface.
        Assert.Equal(1, Regex.Matches(endpoints, @"Map(?:Get|Post|Put|Patch|Delete)\(").Count);

        var module = Read("src/backend/Modules/Order/Tooba.Order.Endpoints/OrderEndpointModule.cs");
        Assert.Equal(1, Regex.Matches(module, @"SellerDashboardEndpoints\.Map\(").Count);
    }

    [Fact]
    public void Dashboard_cqrs_retains_one_authoritative_query_with_order_and_party_boundaries()
    {
        var query = Read("src/backend/Modules/Order/Tooba.Order.Application/Seller/Queries/GetSellerOrderDashboardSummaryQuery.cs");
        Assert.Contains("IRequest<Result<SellerDashboardView>>", query, StringComparison.Ordinal);
        Assert.Contains("IRequestHandler<GetSellerOrderDashboardSummaryQuery, Result<SellerDashboardView>>", query, StringComparison.Ordinal);
        Assert.Contains("GetDashboardViewAsync", query, StringComparison.Ordinal);

        var composer = Read("src/backend/Modules/Order/Tooba.Order.Application/Seller/SellerOrderComposer.cs");
        Assert.Contains("GetDashboardViewAsync", composer, StringComparison.Ordinal);
        Assert.Contains("IPartyLookup", composer, StringComparison.Ordinal);
        Assert.Contains("SellerOrderErrors.SellerMissing", composer, StringComparison.Ordinal);
        Assert.Contains("ActiveOffers: 0", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Party.Application", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("PartyDbContext", composer, StringComparison.Ordinal);

        var view = Read("src/backend/Modules/Order/Tooba.Order.Application/Seller/Models/SellerOrderModels.cs");
        Assert.Contains("record SellerDashboardView", view, StringComparison.Ordinal);
        Assert.Contains("int ActiveOffers", view, StringComparison.Ordinal);
    }

    [Fact]
    public void Behavior_parity_and_recovery_invariants_are_preserved()
    {
        // Exact path, verb and dashboard field set preserved (Order-owned view, Party enrichment).
        var endpoints = Read("src/backend/Modules/Order/Tooba.Order.Endpoints/Seller/SellerDashboardEndpoints.cs");
        Assert.Contains("/v1/seller", endpoints, StringComparison.Ordinal);
        Assert.Contains("\"/dashboard\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("new GetSellerOrderDashboardSummaryQuery(seller, actor)", endpoints, StringComparison.Ordinal);

        // R1A panel gate untouched.
        var gate = Read("src/backend/Host/Tooba.Host/Security/Seller/SellerPanelAccess.cs");
        Assert.Contains("SellerSecurityErrorCodes.ActorMissing", gate, StringComparison.Ordinal);
        Assert.Contains("SellerSecurityErrorCodes.AuthorizationDenied", gate, StringComparison.Ordinal);
        Assert.Contains("SellerSecurityErrorCodes.AuthorizationUnavailable", gate, StringComparison.Ordinal);

        // R2/R3 evacuation invariants remain intact.
        Assert.True(File.Exists(Path.Combine(
            FindRepoRoot(), "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Seller/CatalogSellerEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(
            FindRepoRoot(), "src/backend/Modules/Party/Tooba.Party.Endpoints/Seller/PartySellerSettingsEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(
            FindRepoRoot(), "src/backend/Host/Tooba.Host/Security/Seller/HostOrderSellerAuthorizer.cs")));

        // No route sink-folder regression: the whole Host/Seller folder is gone after R5.
        Assert.False(Directory.Exists(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Seller")));
    }

    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(FindRepoRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
