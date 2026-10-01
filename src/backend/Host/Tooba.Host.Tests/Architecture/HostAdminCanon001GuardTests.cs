using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-ADMIN-CANON-001 — Host AdminPanel composition must be Contracts-only across business modules.
/// </summary>
public sealed class HostAdminCanon001GuardTests
{
    private static readonly string[] ForeignBusinessModules =
    [
        "Tooba.Catalog",
        "Tooba.Party",
        "Tooba.Order",
        "Tooba.Offer",
        "Tooba.Pricing",
        "Tooba.Inventory",
        "Tooba.Payment",
        "Tooba.Promotion",
        "Tooba.Fulfillment",
        "Tooba.Settlement",
        "Tooba.Wallet",
        "Tooba.Support",
        "Tooba.Returns",
        "Tooba.Notification",
    ];

    [Fact]
    public void AdminPanelComposer_has_no_foreign_infrastructure_application_or_domain_dependencies()
    {
        var text = File.ReadAllText(ComposerPath());

        Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
        Assert.DoesNotContain("using Microsoft.EntityFrameworkCore", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ISender", text, StringComparison.Ordinal);

        foreach (var module in ForeignBusinessModules)
        {
            Assert.DoesNotContain("using " + module + ".Infrastructure", text, StringComparison.Ordinal);
            Assert.DoesNotContain("using " + module + ".Application", text, StringComparison.Ordinal);
            Assert.DoesNotContain("using " + module + ".Domain", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void AdminPanelComposer_reads_every_business_module_through_contracts_only()
    {
        var text = File.ReadAllText(ComposerPath());

        Assert.Contains("ICatalogAdminProductCountGateway", text, StringComparison.Ordinal);
        Assert.Contains("IAdminOrderDashboardMetricsPort", text, StringComparison.Ordinal);
        Assert.Contains("IOfferQueryGateway", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IAdminSellersGridPort", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ListSellersAsync", text, StringComparison.Ordinal);
        Assert.DoesNotContain("QuerySellersGridAsync", text, StringComparison.Ordinal);

        Assert.Contains("using Tooba.Catalog.Contracts;", text, StringComparison.Ordinal);
        Assert.DoesNotContain("using Tooba.Party.Contracts;", text, StringComparison.Ordinal);
        Assert.Contains("using Tooba.Order.Contracts.Admin;", text, StringComparison.Ordinal);
        Assert.Contains("using Tooba.Offer.Contracts.Ports;", text, StringComparison.Ordinal);

        var sellersQuery = File.ReadAllText(RepoFile(
            "src/backend/Modules/Party/Tooba.Party.Application/Admin/Sellers/Queries/ListAdminSellersQuery.cs"));
        Assert.Contains("IPartyAdminSellerReadGateway", sellersQuery, StringComparison.Ordinal);
        Assert.Contains("IAdminSellerOrderCountPort", sellersQuery, StringComparison.Ordinal);
        Assert.Contains("IOfferQueryGateway", sellersQuery, StringComparison.Ordinal);

        var sellersGridQuery = File.ReadAllText(RepoFile(
            "src/backend/Modules/Party/Tooba.Party.Application/Admin/Sellers/Queries/QueryAdminSellersGridQuery.cs"));
        Assert.Contains("IAdminSellersGridPort", sellersGridQuery, StringComparison.Ordinal);
        Assert.Contains("GridQueryValidationException", sellersGridQuery, StringComparison.Ordinal);
        Assert.Contains("ex.ErrorCode", sellersGridQuery, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", sellersGridQuery, StringComparison.Ordinal);
    }

    [Fact]
    public void SellersGridEngine_has_no_foreign_persistence_and_uses_contracts_boundary()
    {
        var text = File.ReadAllText(SellersGridPath());

        Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IQueryable", text, StringComparison.Ordinal);
        Assert.DoesNotContain("using Microsoft.EntityFrameworkCore", text, StringComparison.Ordinal);
        foreach (var module in ForeignBusinessModules.Where(m => m is not "Tooba.Party"))
        {
            Assert.DoesNotContain("using " + module + ".Infrastructure", text, StringComparison.Ordinal);
            Assert.DoesNotContain("using " + module + ".Application", text, StringComparison.Ordinal);
            Assert.DoesNotContain("using " + module + ".Domain", text, StringComparison.Ordinal);
        }

        Assert.Contains("IPartyAdminSellerReadGateway", text, StringComparison.Ordinal);
        Assert.Contains("IAdminSellerOrderCountPort", text, StringComparison.Ordinal);
        Assert.Contains("IOfferQueryGateway", text, StringComparison.Ordinal);
        Assert.Contains("Count(", text, StringComparison.Ordinal);
        Assert.Contains("Skip(", text, StringComparison.Ordinal);
        Assert.Contains("Take(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Contract_files_expose_no_persistence_types()
    {
        foreach (var path in new[]
                 {
                     RepoFile("src/backend/Modules/Catalog/Tooba.Catalog.Contracts/CatalogAdminProductCountContracts.cs"),
                     RepoFile("src/backend/Modules/Party/Tooba.Party.Contracts/IPartyAdminSellerReadGateway.cs"),
                     RepoFile("src/backend/Modules/Order/Tooba.Order.Contracts/Admin/AdminPanelReadContracts.cs"),
                 })
        {
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
            Assert.DoesNotContain("IQueryable", text, StringComparison.Ordinal);
            Assert.DoesNotContain("EntityFramework", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MediatR", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Adapters_own_persistence_inside_their_modules()
    {
        foreach (var path in new[]
                 {
                     RepoFile("src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/CatalogAdminProductCountGateway.cs"),
                     RepoFile("src/backend/Modules/Party/Tooba.Party.Infrastructure/Admin/PartyAdminSellerReadGateway.cs"),
                     RepoFile("src/backend/Modules/Order/Tooba.Order.Infrastructure/Admin/AdminPanelReadPortAdapters.cs"),
                 })
        {
            Assert.True(File.Exists(path), path);
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("Tooba.Host", text, StringComparison.Ordinal);
            Assert.DoesNotContain("PlatformHttpException", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Host_admin_endpoints_and_routes_are_preserved()
    {
        var endpoints = File.ReadAllText(RepoFile("src/backend/Host/Tooba.Host/Admin/Panel/AdminPanelEndpoints.cs"));
        Assert.Contains("MapGet(\"/dashboard\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("IAdminPanelAccess", endpoints, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", endpoints, StringComparison.Ordinal);
        Assert.Contains("Result.Success", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminPanelAccess.RequireAuthorizedAsync", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("catch (PlatformHttpException", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("ToError", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGet(\"/sellers\"", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost(\"/sellers/query\"", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminGridQueryEndpoint", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGet(\"/dev-context\"", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("admin.dev.unavailable", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("Not Found", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("using Tooba.Host.Admin.Development", endpoints, StringComparison.Ordinal);
        var devContext = File.ReadAllText(RepoFile(
            "src/backend/Host/Tooba.Host/Admin/Development/AdminDevContextEndpoints.cs"));
        Assert.Contains("MapGet(\"/v1/admin/dev-context\"", devContext, StringComparison.Ordinal);
        Assert.Contains("admin.dev.unavailable", devContext, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", devContext, StringComparison.Ordinal);
        Assert.Contains("HOST_DEVELOPMENT_PRESENTATION_CQRS_EXCEPTION", devContext, StringComparison.Ordinal);
        Assert.DoesNotContain("Not Found", devContext, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Json", devContext, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", devContext, StringComparison.Ordinal);
        var partySellers = File.ReadAllText(RepoFile(
            "src/backend/Modules/Party/Tooba.Party.Endpoints/Admin/Sellers/PartyAdminSellersEndpoints.cs"));
        Assert.Contains("MapGet(\"/v1/admin/sellers\"", partySellers, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/v1/admin/sellers/query\"", partySellers, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGet(\"/orders\"", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGet(\"/customers\"", endpoints, StringComparison.Ordinal);
        var program = File.ReadAllText(RepoFile("src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("MapAdminDevContextEndpoints()", program, StringComparison.Ordinal);
        Assert.Equal(1, System.Text.RegularExpressions.Regex.Matches(program + "\n" + endpoints + "\n" + devContext, @"MapGet\(\""/v1/admin/dev-context\""|MapGet\(\""/dev-context\""").Count);
    }

    [Fact]
    public void Host_admin_count_remains_platform_floor_18()
    {
        var admin = RepoFile("src/backend/Host/Tooba.Host/Admin");
        Assert.Equal(18, Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories).Length);
    }

    [Fact]
    public void Program_registers_new_contracts_boundaries_via_modules_not_host_persistence()
    {
        var program = File.ReadAllText(RepoFile("src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("AddToobaModules", program, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogAdminProductCountGateway", program, StringComparison.Ordinal);
        Assert.DoesNotContain("PartyAdminSellerReadGateway", program, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminPanelReadPortAdapters", program, StringComparison.Ordinal);

        var catalogModule = File.ReadAllText(
            RepoFile("src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/CatalogModule.cs"));
        Assert.Contains("ICatalogAdminProductCountGateway", catalogModule, StringComparison.Ordinal);

        var partyModule = File.ReadAllText(
            RepoFile("src/backend/Modules/Party/Tooba.Party.Infrastructure/PartyModule.cs"));
        Assert.Contains("IPartyAdminSellerReadGateway", partyModule, StringComparison.Ordinal);

        var orderModule = File.ReadAllText(
            RepoFile("src/backend/Modules/Order/Tooba.Order.Infrastructure/OrderModule.cs"));
        Assert.Contains("IAdminOrderDashboardMetricsPort", orderModule, StringComparison.Ordinal);
        Assert.Contains("IAdminSellerOrderCountPort", orderModule, StringComparison.Ordinal);
    }

    [Fact]
    public void W36_store_appearance_closure_remains_intact()
    {
        var admin = RepoFile("src/backend/Host/Tooba.Host/Admin");
        Assert.False(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsComposer.cs")));
        Assert.True(File.Exists(RepoFile(
            "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/StoreAppearance/StoreAppearanceProjector.cs")));
    }

    private static string ComposerPath() => RepoFile("src/backend/Host/Tooba.Host/Admin/Panel/AdminPanelComposer.cs");

    private static string SellersGridPath() => RepoFile(
        "src/backend/Modules/Party/Tooba.Party.Infrastructure/Grid/AdminSellersGridQueryEngine.cs");

    private static string RepoFile(string relative) => Path.Combine(FindRepoRoot(), relative.Replace('/', Path.DirectorySeparatorChar));

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Tooba.sln"))
                || File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
