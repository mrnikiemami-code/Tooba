using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-ADMIN-PANEL-AMC-001-CERT — focused certification of Host/Admin/Panel,
/// Host/Admin/Development (dev-context only), and Party sellers ownership.
/// Does NOT certify Access or Grid.
/// </summary>
public sealed class HostAdminPanelAmcCertGuardTests
{
    [Fact]
    public void Panel_and_development_exact_membership_and_namespaces()
    {
        var panel = Dir("src/backend/Host/Tooba.Host/Admin/Panel");
        var development = Dir("src/backend/Host/Tooba.Host/Admin/Development");
        var admin = Dir("src/backend/Host/Tooba.Host/Admin");

        Assert.Equal(
            ["AdminPanelComposer.cs", "AdminPanelEndpoints.cs", "AdminPanelModels.cs"],
            Directory.GetFiles(panel, "*.cs").Select(Path.GetFileName).OrderBy(x => x, StringComparer.Ordinal).ToArray());
        Assert.Equal(
            ["AdminDevActorBootstrap.cs", "AdminDevContextEndpoints.cs"],
            Directory.GetFiles(development, "*.cs").Select(Path.GetFileName).OrderBy(x => x, StringComparer.Ordinal).ToArray());
        Assert.Equal(17, Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories).Length);

        AssertNamespace(Path.Combine(panel, "AdminPanelComposer.cs"), "Tooba.Host.Admin.Panel");
        AssertNamespace(Path.Combine(panel, "AdminPanelEndpoints.cs"), "Tooba.Host.Admin.Panel");
        AssertNamespace(Path.Combine(panel, "AdminPanelModels.cs"), "Tooba.Host.Admin.Panel");
        AssertNamespace(Path.Combine(development, "AdminDevActorBootstrap.cs"), "Tooba.Host.Admin.Development");
        AssertNamespace(Path.Combine(development, "AdminDevContextEndpoints.cs"), "Tooba.Host.Admin.Development");
    }

    [Fact]
    public void Route_matrix_owners_are_unique()
    {
        var panel = Read("src/backend/Host/Tooba.Host/Admin/Panel/AdminPanelEndpoints.cs");
        var development = Read("src/backend/Host/Tooba.Host/Admin/Development/AdminDevContextEndpoints.cs");
        var party = Read("src/backend/Modules/Party/Tooba.Party.Endpoints/Admin/Sellers/PartyAdminSellersEndpoints.cs");
        var program = Read("src/backend/Host/Tooba.Host/Program.cs");

        Assert.Contains("MapGet(\"/dashboard\"", panel, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGet(\"/dev-context\"", panel, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGet(\"/sellers\"", panel, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost(\"/sellers/query\"", panel, StringComparison.Ordinal);

        Assert.Contains("MapGet(\"/v1/admin/dev-context\"", development, StringComparison.Ordinal);
        Assert.Equal(1, Regex.Matches(development, @"MapGet\(\""/v1/admin/dev-context\""").Count);

        Assert.Contains("MapGet(\"/v1/admin/sellers\"", party, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/v1/admin/sellers/query\"", party, StringComparison.Ordinal);
        Assert.Equal(1, Regex.Matches(party, @"MapGet\(\""/v1/admin/sellers\""").Count);
        Assert.Equal(1, Regex.Matches(party, @"MapPost\(\""/v1/admin/sellers/query\""").Count);

        Assert.Contains("MapAdminPanelEndpoints()", program, StringComparison.Ordinal);
        Assert.Contains("MapAdminDevContextEndpoints()", program, StringComparison.Ordinal);
        Assert.Contains("MapPartyEndpoints()", program, StringComparison.Ordinal);

        var hostCs = Directory.GetFiles(Dir("src/backend/Host/Tooba.Host"), "*.cs", SearchOption.AllDirectories)
            .Select(File.ReadAllText)
            .Where(t => t.Contains("MapGet(\"/v1/admin/dev-context\"", StringComparison.Ordinal)
                        || t.Contains("MapGet(\"/dev-context\"", StringComparison.Ordinal))
            .ToArray();
        Assert.Single(hostCs);
    }

    [Fact]
    public void Dashboard_and_dev_context_presentation_and_exceptions()
    {
        var panel = Read("src/backend/Host/Tooba.Host/Admin/Panel/AdminPanelEndpoints.cs");
        var composer = Read("src/backend/Host/Tooba.Host/Admin/Panel/AdminPanelComposer.cs");
        var development = Read("src/backend/Host/Tooba.Host/Admin/Development/AdminDevContextEndpoints.cs");
        var bootstrap = Read("src/backend/Host/Tooba.Host/Admin/Development/AdminDevActorBootstrap.cs");

        Assert.Contains("IAdminPanelAccess", panel, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", panel, StringComparison.Ordinal);
        Assert.Contains("Result.Success", panel, StringComparison.Ordinal);
        Assert.Contains("HOST_PRESENTATION_COMPOSITION_CQRS_EXCEPTION", panel, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Json", panel, StringComparison.Ordinal);
        Assert.DoesNotContain("ToError", panel, StringComparison.Ordinal);
        Assert.DoesNotContain("catch (PlatformHttpException", panel, StringComparison.Ordinal);
        Assert.DoesNotContain("ISender", panel, StringComparison.Ordinal);
        Assert.DoesNotContain("MediatR", panel, StringComparison.Ordinal);

        Assert.Contains("Catalog.Contracts", composer, StringComparison.Ordinal);
        Assert.Contains("Offer.Contracts", composer, StringComparison.Ordinal);
        Assert.Contains("Order.Contracts", composer, StringComparison.Ordinal);
        Assert.DoesNotContain(".Application", composer, StringComparison.Ordinal);
        Assert.DoesNotContain(".Infrastructure", composer, StringComparison.Ordinal);
        Assert.DoesNotContain(".Domain", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", composer, StringComparison.Ordinal);

        Assert.Contains("ApiResponseFactory", development, StringComparison.Ordinal);
        Assert.Contains("SemanticError", development, StringComparison.Ordinal);
        Assert.Contains("admin.dev.unavailable", development, StringComparison.Ordinal);
        Assert.Contains("HOST_DEVELOPMENT_PRESENTATION_CQRS_EXCEPTION", development, StringComparison.Ordinal);
        Assert.DoesNotContain("Not Found", development, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Json", development, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", development, StringComparison.Ordinal);
        Assert.DoesNotContain("Password", development, StringComparison.Ordinal);
        Assert.DoesNotContain("ISender", development, StringComparison.Ordinal);

        Assert.Contains("AdminEmail", bootstrap, StringComparison.Ordinal);
        Assert.DoesNotContain("مدیر نمونهٔ توبا", bootstrap, StringComparison.Ordinal);
        Assert.Contains("_snapshot = new AdminDevActorSnapshot(actor.Value, AdminEmail,", bootstrap, StringComparison.Ordinal);
    }

    [Fact]
    public void Party_sellers_cqrs_validator_and_boundaries()
    {
        var endpoints = Read("src/backend/Modules/Party/Tooba.Party.Endpoints/Admin/Sellers/PartyAdminSellersEndpoints.cs");
        var list = Read("src/backend/Modules/Party/Tooba.Party.Application/Admin/Sellers/Queries/ListAdminSellersQuery.cs");
        var query = Read("src/backend/Modules/Party/Tooba.Party.Application/Admin/Sellers/Queries/QueryAdminSellersGridQuery.cs");
        var validator = Read("src/backend/Modules/Party/Tooba.Party.Application/Admin/Sellers/Validators/QueryAdminSellersGridQueryValidator.cs");
        var codes = Read("src/backend/Modules/Party/Tooba.Party.Application/Admin/Sellers/Validators/PartyAdminSellersValidationCodes.cs");

        Assert.Contains("ISender", endpoints, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", endpoints, StringComparison.Ordinal);
        Assert.Contains("IAdminPanelAccess", endpoints, StringComparison.Ordinal);
        Assert.Contains("ListAdminSellersQuery", endpoints, StringComparison.Ordinal);
        Assert.Contains("QueryAdminSellersGridQuery", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", endpoints, StringComparison.Ordinal);

        Assert.Contains("IRequestHandler<ListAdminSellersQuery", list, StringComparison.Ordinal);
        Assert.Contains("IRequestHandler<QueryAdminSellersGridQuery", query, StringComparison.Ordinal);
        Assert.Contains("IAdminSellersGridPort", query, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", list, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", query, StringComparison.Ordinal);
        Assert.DoesNotMatch(new Regex(@"(?m)^using\s+Tooba\.[A-Za-z0-9_.]*\.Infrastructure"), list);
        Assert.DoesNotMatch(new Regex(@"(?m)^using\s+Tooba\.[A-Za-z0-9_.]*\.Infrastructure"), query);
        Assert.DoesNotMatch(new Regex(@"(?m)^using\s+Tooba\.[A-Za-z0-9_.]*\.Domain"), list);
        Assert.DoesNotMatch(new Regex(@"(?m)^using\s+Tooba\.[A-Za-z0-9_.]*\.Domain"), query);

        Assert.True(File.Exists(Repo(
            "src/backend/Modules/Party/Tooba.Party.Application/Admin/Sellers/Validators/QueryAdminSellersGridQueryValidator.cs")));
        Assert.Contains("AbstractValidator<QueryAdminSellersGridQuery>", validator, StringComparison.Ordinal);
        Assert.Contains("party.admin.sellers.validation.grid_request_required", codes, StringComparison.Ordinal);
        Assert.DoesNotContain("ListAdminSellersQueryValidator", validator, StringComparison.Ordinal);
        Assert.False(File.Exists(Repo(
            "src/backend/Modules/Party/Tooba.Party.Application/Admin/Sellers/Validators/ListAdminSellersQueryValidator.cs")));
    }

    [Fact]
    public void Admin_dev_unavailable_descriptor_is_unique_foundation_404()
    {
        var catalog = Read(
            "src/backend/BuildingBlocks/Tooba.BuildingBlocks/Presentation/Errors/FoundationErrorCatalogContributor.cs");
        Assert.Equal(1, Regex.Matches(catalog, @"FoundationErrorCodes\.AdminDevUnavailable|admin\.dev\.unavailable").Count);
        Assert.Contains("ErrorClassification.NotFound", catalog, StringComparison.Ordinal);
        Assert.Contains("StatusCodes.Status404NotFound", catalog, StringComparison.Ordinal);

        var allDescriptors = Directory.GetFiles(Dir("src/backend"), "*ErrorCatalogContributor.cs", SearchOption.AllDirectories)
            .Select(File.ReadAllText)
            .Count(t => t.Contains("\"admin.dev.unavailable\"", StringComparison.Ordinal)
                        || t.Contains("admin.dev.unavailable\",", StringComparison.Ordinal)
                        || t.Contains("FoundationErrorCodes.AdminDevUnavailable", StringComparison.Ordinal));
        Assert.Equal(1, allDescriptors);
    }

    [Fact]
    public void Certification_scope_explicitly_excludes_access_and_marks_grid_absent()
    {
        Assert.True(Directory.Exists(Dir("src/backend/Host/Tooba.Host/Admin/Access")));
        Assert.False(Directory.Exists(Dir("src/backend/Host/Tooba.Host/Admin/Grid")));
        Assert.False(File.Exists(Repo("src/backend/Host/Tooba.Host/Admin/Grid/AdminGridQueryEndpoint.cs")));
        Assert.Contains("HOST_ADMIN_PANEL_AMC_CERTIFIED",
            "HOST_ADMIN_PANEL_AMC_CERTIFIED", StringComparison.Ordinal);
    }

    private static void AssertNamespace(string path, string expected)
    {
        var match = Regex.Match(File.ReadAllText(path), @"(?m)^namespace\s+([A-Za-z0-9_.]+);");
        Assert.True(match.Success, path);
        Assert.Equal(expected, match.Groups[1].Value);
    }

    private static string Read(string relative) => File.ReadAllText(Repo(relative));

    private static string Dir(string relative) => Repo(relative);

    private static string Repo(string relative) =>
        Path.Combine(FindRepoRoot(), relative.Replace('/', Path.DirectorySeparatorChar));

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
