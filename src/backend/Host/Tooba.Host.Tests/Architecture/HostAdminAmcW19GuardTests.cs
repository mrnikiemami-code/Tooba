using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-ADMIN-AMC-001-W19 — migrate only aggregate GET to ProductWorkspace via Contracts composition.
/// </summary>
public sealed class HostAdminAmcW19GuardTests
{
    private static readonly Regex MapRouteRegex = new(
        @"\bMap(Get|Post|Put|Patch|Delete)\s*\(",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void Aggregate_GET_owned_once_by_ProductWorkspace_and_absent_from_Host()
    {
        var root = FindRepoRoot();
        var moduleEndpoints = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/ProductWorkspace/Tooba.ProductWorkspace.Endpoints/ProductWorkspaceEndpointModule.cs"));
        Assert.Equal(17, MapRouteRegex.Matches(moduleEndpoints).Count);
        Assert.Contains("MapGet(\"/{productId:guid}\"", moduleEndpoints, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/{productId:guid}/publish\"", moduleEndpoints, StringComparison.Ordinal);
        Assert.Contains("IProductWorkspaceAdminAuthorizer", moduleEndpoints, StringComparison.Ordinal);
        Assert.Contains("GetProductWorkspaceQuery", moduleEndpoints, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", moduleEndpoints, StringComparison.Ordinal);
        Assert.Contains("X-Tooba-Workspace-Scope", moduleEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", moduleEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", moduleEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", moduleEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogActorHttpBinding", moduleEndpoints, StringComparison.Ordinal);


        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("MapProductWorkspaceModuleEndpoints", program, StringComparison.Ordinal);
        Assert.Contains("AddProductWorkspaceEndpointPresentation", program, StringComparison.Ordinal);
        Assert.Contains("GetProductWorkspaceQuery", program, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductWorkspace_Contracts_only_composition_and_Catalog_gateway_boundary()
    {
        var root = FindRepoRoot();
        var moduleRoot = Path.Combine(root, "src/backend/Modules/ProductWorkspace");
        var applicationRefs = GetProjectRefs(
            Path.Combine(moduleRoot, "Tooba.ProductWorkspace.Application", "Tooba.ProductWorkspace.Application.csproj"));
        Assert.DoesNotContain(applicationRefs, r => ContainsSegment(r, "Catalog.Application"));
        Assert.DoesNotContain(applicationRefs, r => ContainsSegment(r, "Party.Application"));
        Assert.DoesNotContain(applicationRefs, r => ContainsSegment(r, ".Infrastructure"));
        Assert.Contains(applicationRefs, r => ContainsSegment(r, "Catalog.Contracts"));
        Assert.Contains(applicationRefs, r => ContainsSegment(r, "Party.Contracts"));

        var handler = File.ReadAllText(Path.Combine(
            moduleRoot,
            "Tooba.ProductWorkspace.Application/Composition/Queries/GetProductWorkspaceHandler.cs"));
        Assert.Contains("ICatalogAdminProductWorkspaceReadGateway", handler, StringComparison.Ordinal);
        Assert.Contains("IOfferQueryGateway", handler, StringComparison.Ordinal);
        Assert.Contains("IPriceQueryGateway", handler, StringComparison.Ordinal);
        Assert.Contains("IInventoryQueryGateway", handler, StringComparison.Ordinal);
        Assert.Contains("ITaxQueryGateway", handler, StringComparison.Ordinal);
        Assert.Contains("IPartyLookup", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("IPartyLookupGateway", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("CatalogDbContext", handler, StringComparison.Ordinal);
        Assert.Contains("workspace.product.missing", File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Contracts/Errors/CatalogErrorCodes.cs")), StringComparison.Ordinal);
        Assert.Contains("CatalogErrorCodes.WorkspaceProductMissing", handler, StringComparison.Ordinal);

        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Contracts/Ports/CatalogAdminProductWorkspaceReadContracts.cs")));
        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Directories/CatalogAdminProductWorkspaceReadGateway.cs")));

        var catalogContract = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Contracts/Ports/CatalogAdminProductWorkspaceReadContracts.cs"));
        Assert.DoesNotContain("DbContext", catalogContract, StringComparison.Ordinal);
        Assert.DoesNotContain("IQueryable", catalogContract, StringComparison.Ordinal);
        Assert.DoesNotContain("EntityFramework", catalogContract, StringComparison.Ordinal);

        var endpointRefs = GetProjectRefs(
            Path.Combine(moduleRoot, "Tooba.ProductWorkspace.Endpoints", "Tooba.ProductWorkspace.Endpoints.csproj"));
        Assert.DoesNotContain(endpointRefs, r => ContainsSegment(r, "ProductWorkspace.Infrastructure"));
        Assert.DoesNotContain(endpointRefs, r => ContainsSegment(r, "Tooba.Host"));

        foreach (var file in Directory.GetFiles(moduleRoot, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("CatalogDbContext", text, StringComparison.Ordinal);
            Assert.DoesNotContain("using Tooba.Party.Application", text, StringComparison.Ordinal);
            Assert.DoesNotContain("using Tooba.Host", text, StringComparison.Ordinal);
        }

        var admin = Path.Combine(root, "src/backend/Host/Tooba.Host/Admin");
        var adminCount = Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories).Length;
        Assert.True(adminCount <= 52 && adminCount >= 12, $"Host/Admin count expected in [12,52], was {adminCount}");
        Assert.False(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(admin, "ProductWorkspaceComposer.cs")));
    }

    [Fact]
    public void SoT_records_W19_checkpoint_and_preserves_through_W20()
    {
        var root = FindRepoRoot();
        var sot = File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-current-state.json"));
        Assert.Contains("\"hostAdminAmcW19\"", sot, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_HOST_ADMIN_W19_CHECKPOINT", sot, StringComparison.Ordinal);
        Assert.Contains("\"aggregateGetOwner\": \"ProductWorkspace\"", sot, StringComparison.Ordinal);
        Assert.Contains("\"hostAdminAmcW20\"", sot, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_HOST_ADMIN_W20_CHECKPOINT", sot, StringComparison.Ordinal);
        Assert.Contains("\"HostRemainingProductWorkspaceRoutes\": 17", sot, StringComparison.Ordinal);

        Assert.True(Directory.Exists(Path.Combine(root, "docs/evidence/TB-TMAR-HOST-ADMIN-AMC-001-W19")));
        Assert.True(File.Exists(Path.Combine(root, "docs/ai/tasks/TB-TMAR-HOST-ADMIN-AMC-001-W19.task.md")));
        Assert.True(Directory.Exists(Path.Combine(root, "docs/evidence/TB-TMAR-HOST-ADMIN-AMC-001-W20")));
    }

    private static bool ContainsSegment(string projectRef, string segment) =>
        projectRef.Contains(segment, StringComparison.OrdinalIgnoreCase);

    private static string[] GetProjectRefs(string csprojPath) =>
        XDocument.Load(csprojPath)
            .Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .ToArray();

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
