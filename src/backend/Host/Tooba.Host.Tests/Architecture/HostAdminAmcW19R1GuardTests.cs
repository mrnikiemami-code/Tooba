using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-ADMIN-AMC-001-W19-R1 — CatalogAdminProductWorkspaceReadGateway uses Domain Try*
/// category assignability; zero expected InvalidOperationException control flow on aggregate GET path.
/// </summary>
public sealed class HostAdminAmcW19R1GuardTests
{
    private static readonly Regex MapRouteRegex = new(
        @"\bMap(Get|Post|Put|Patch|Delete)\s*\(",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void Aggregate_gateway_has_zero_expected_InvalidOperationException_assignability_flow()
    {
        var gateway = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/CatalogAdminProductWorkspaceReadGateway.cs"));

        Assert.DoesNotContain("catch (InvalidOperationException", gateway, StringComparison.Ordinal);
        Assert.DoesNotContain("catch(InvalidOperationException", gateway, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", gateway, StringComparison.Ordinal);
        Assert.DoesNotContain("Message.Contains", gateway, StringComparison.Ordinal);
        Assert.DoesNotContain("EnsureAssignableProductCategory", gateway, StringComparison.Ordinal);
        Assert.Contains("TryIsAssignableProductCategory", gateway, StringComparison.Ordinal);
        Assert.Contains("CatalogCategoryTreeRules", gateway, StringComparison.Ordinal);
        Assert.Contains("isPrimaryCategoryAssignable", gateway, StringComparison.Ordinal);
        Assert.Contains("ProductAssignableLevelRequiredMessageFa", gateway, StringComparison.Ordinal);
        Assert.DoesNotContain("1 + ancestors", gateway, StringComparison.Ordinal);
        Assert.DoesNotContain("IsDescendant", gateway, StringComparison.Ordinal);
        Assert.DoesNotContain("MaxCategoryDepth", gateway, StringComparison.Ordinal);
    }

    [Fact]
    public void Domain_exposes_Try_probes_sharing_core_with_throwing_APIs()
    {
        var rules = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src/backend/Modules/Catalog/Tooba.Catalog.Domain/CatalogCategoryTreeRules.cs"));

        Assert.Contains("public static bool TryGetCategoryLevel", rules, StringComparison.Ordinal);
        Assert.Contains("public static bool TryIsAssignableProductCategory", rules, StringComparison.Ordinal);
        Assert.Contains("TryResolveCategoryLevel", rules, StringComparison.Ordinal);
        Assert.Contains("public static int GetCategoryLevel", rules, StringComparison.Ordinal);
        Assert.Contains("public static bool IsAssignableProductCategory", rules, StringComparison.Ordinal);
        Assert.Contains(
            "GetCategoryLevel(categoryId, parentById) == ProductAssignableLevel",
            rules,
            StringComparison.Ordinal);
        Assert.Contains("رده در Catalog این Tenant وجود ندارد.", rules, StringComparison.Ordinal);
        Assert.Contains("حلقهٔ موجود در درخت رده تشخیص داده شد.", rules, StringComparison.Ordinal);
    }

    [Fact]
    public void W19_ownership_boundaries_and_counts_preserved()
    {
        var root = FindRepoRoot();
        var moduleEndpoints = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/ProductWorkspace/Tooba.ProductWorkspace.Endpoints/ProductWorkspaceEndpointModule.cs"));
        Assert.Equal(1, MapRouteRegex.Matches(moduleEndpoints).Count);
        Assert.Contains("MapGet(\"/{productId:guid}\"", moduleEndpoints, StringComparison.Ordinal);

        var hostEndpoints = File.ReadAllText(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Admin/ProductWorkspaceEndpoints.cs"));
        Assert.Equal(17, MapRouteRegex.Matches(hostEndpoints).Count);
        Assert.DoesNotContain("MapGet(\"/{productId:guid}\"", hostEndpoints, StringComparison.Ordinal);

        var admin = Path.Combine(root, "src/backend/Host/Tooba.Host/Admin");
        var adminCount = Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories).Length;
        Assert.True(adminCount <= 52 && adminCount >= 12, $"Host/Admin count expected in [12,52], was {adminCount}");

        var applicationRefs = GetProjectRefs(Path.Combine(
            root,
            "src/backend/Modules/ProductWorkspace/Tooba.ProductWorkspace.Application/Tooba.ProductWorkspace.Application.csproj"));
        Assert.Contains(applicationRefs, r => ContainsSegment(r, "Catalog.Contracts"));
        Assert.DoesNotContain(applicationRefs, r => ContainsSegment(r, "Catalog.Application"));
        Assert.DoesNotContain(applicationRefs, r => ContainsSegment(r, "Party.Application"));

        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/CatalogAdminProductWorkspaceReadGateway.cs")));
        Assert.True(File.Exists(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Contracts/CatalogAdminProductWorkspaceReadContracts.cs")));
    }

    [Fact]
    public void SoT_records_W19_R1_checkpoint_and_preserves_through_W20()
    {
        var root = FindRepoRoot();
        var sot = File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-current-state.json"));
        Assert.Contains("\"hostAdminAmcW19R1\"", sot, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_HOST_ADMIN_W19_R1_CHECKPOINT", sot, StringComparison.Ordinal);
        Assert.Contains("\"hostAdminAmcW19\"", sot, StringComparison.Ordinal);
        Assert.Contains("\"aggregateGetOwner\": \"ProductWorkspace\"", sot, StringComparison.Ordinal);
        Assert.Contains("\"hostAdminAmcW20\"", sot, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_HOST_ADMIN_W20_CHECKPOINT", sot, StringComparison.Ordinal);
        Assert.Contains("\"HostRemainingProductWorkspaceRoutes\": 17", sot, StringComparison.Ordinal);

        Assert.True(Directory.Exists(Path.Combine(root, "docs/evidence/TB-TMAR-HOST-ADMIN-AMC-001-W19-R1")));
        Assert.True(File.Exists(Path.Combine(root, "docs/ai/tasks/TB-TMAR-HOST-ADMIN-AMC-001-W19-R1.task.md")));
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
