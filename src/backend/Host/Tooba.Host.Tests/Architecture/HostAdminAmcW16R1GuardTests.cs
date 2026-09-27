using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-ADMIN-AMC-001-W16-R1 — ProductPublishReadinessReader category readiness uses
/// CatalogCategoryTreeRules.IsAssignableProductCategory; zero expected InvalidOperationException flow.
/// </summary>
public sealed class HostAdminAmcW16R1GuardTests
{
    [Fact]
    public void ProductPublishReadinessReader_has_zero_expected_InvalidOperationException_control_flow()
    {
        var reader = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/ProductPublishReadinessReader.cs"));
        Assert.DoesNotContain("catch (InvalidOperationException", reader, StringComparison.Ordinal);
        Assert.DoesNotContain("catch(InvalidOperationException", reader, StringComparison.Ordinal);
        Assert.DoesNotContain("catch (", reader, StringComparison.Ordinal);
        Assert.DoesNotContain("throw new InvalidOperationException", reader, StringComparison.Ordinal);
        Assert.DoesNotContain("EnsureAssignableProductCategory", reader, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", reader, StringComparison.Ordinal);
        Assert.DoesNotContain("Message.Contains", reader, StringComparison.Ordinal);
        Assert.Contains("IsAssignableProductCategory", reader, StringComparison.Ordinal);
        Assert.Contains("CatalogCategoryTreeRules", reader, StringComparison.Ordinal);
    }

    [Fact]
    public void Category_readiness_uses_Domain_non_throwing_API_without_hierarchy_duplication()
    {
        var root = FindRepoRoot();
        var rules = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Domain/CatalogCategoryTreeRules.cs"));
        Assert.Contains("public static bool IsAssignableProductCategory", rules, StringComparison.Ordinal);
        Assert.Contains("public static void EnsureAssignableProductCategory", rules, StringComparison.Ordinal);
        Assert.Contains("GetCategoryLevel(categoryId, parentById) == ProductAssignableLevel", rules, StringComparison.Ordinal);

        var reader = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/ProductPublishReadinessReader.cs"));
        Assert.Contains("CatalogCategoryTreeRules.IsAssignableProductCategory", reader, StringComparison.Ordinal);
        Assert.DoesNotContain("GetCategoryLevel", reader, StringComparison.Ordinal);
        Assert.DoesNotContain("ProductAssignableLevel", reader, StringComparison.Ordinal);
        Assert.DoesNotContain("1 + ancestors", reader, StringComparison.Ordinal);
        Assert.DoesNotContain("MaxCategoryDepth", reader, StringComparison.Ordinal);
        Assert.DoesNotContain("IsDescendant", reader, StringComparison.Ordinal);
    }

    [Fact]
    public void W16_publish_readiness_route_ownership_and_reuse_preserved()
    {
        var root = FindRepoRoot();
        var admin = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Admin/ProductPublishing/CatalogProductPublishReadinessAdminEndpoints.cs"));
        Assert.Contains("MapGet(\"/publish/readiness\", GetAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("ISender", admin, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", admin, StringComparison.Ordinal);
        Assert.Contains("ICatalogAdminAuthorizer", admin, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/CatalogEndpointModule.cs"));
        Assert.Single(Regex.Matches(module, @"MapCatalogProductPublishReadinessAdminEndpoints\(\)"));


        var reader = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/ProductPublishReadinessReader.cs"));
        Assert.Contains("IProductSeoDirectory", reader, StringComparison.Ordinal);
        Assert.Contains("IProductAttributeDirectory", reader, StringComparison.Ordinal);
        Assert.Contains("IProductVariantDirectory", reader, StringComparison.Ordinal);
        Assert.Contains("IProductMediaDirectory", reader, StringComparison.Ordinal);
        Assert.Contains("\"category\"", reader, StringComparison.Ordinal);
        Assert.Contains("\"identity\"", reader, StringComparison.Ordinal);
        Assert.Contains("\"attributes\"", reader, StringComparison.Ordinal);
        Assert.Contains("\"variants\"", reader, StringComparison.Ordinal);
        Assert.Contains("\"media\"", reader, StringComparison.Ordinal);
        Assert.Contains("\"seo\"", reader, StringComparison.Ordinal);
        Assert.Contains("WorkspaceProductMissing", reader, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_Admin_count_and_W17_through_W20_artifacts_present()
    {
        var root = FindRepoRoot();
        var admin = Path.Combine(root, "src/backend/Host/Tooba.Host/Admin");
        var files = Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories);
        Assert.True(files.Length >= 23, $"expected Admin residue after demo/landing/PW waves; was {files.Length}");
        Assert.Equal(18, files.Length);
        Assert.False(File.Exists(Path.Combine(admin, "ProductWorkspaceEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsEndpoints.cs")));
        Assert.True(Directory.Exists(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Application/ProductPublishing")));
        Assert.True(File.Exists(Path.Combine(
            root, "docs/ai/tasks/TB-TMAR-HOST-ADMIN-AMC-001-W17.task.md")));
        Assert.True(Directory.Exists(Path.Combine(
            root, "docs/evidence/TB-TMAR-HOST-ADMIN-AMC-001-W17")));
        Assert.True(Directory.Exists(Path.Combine(
            root, "docs/evidence/TB-TMAR-HOST-ADMIN-AMC-001-W20")));
    }

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
