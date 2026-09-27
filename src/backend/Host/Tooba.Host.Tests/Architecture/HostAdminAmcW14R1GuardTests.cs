using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-ADMIN-AMC-001-W14-R1 — ProductSeoDirectory uses Domain Try* slug path; zero expected IOE catch.
/// </summary>
public sealed class HostAdminAmcW14R1GuardTests
{
    [Fact]
    public void ProductSeoDirectory_has_zero_expected_InvalidOperationException_control_flow()
    {
        var directory = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/ProductSeoDirectory.cs"));
        Assert.DoesNotContain("catch (InvalidOperationException", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("catch(InvalidOperationException", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("catch (", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("Message.Contains", directory, StringComparison.Ordinal);
        Assert.Contains("TrySlugifyFromName", directory, StringComparison.Ordinal);
        Assert.Contains("TryNormalizeSlug", directory, StringComparison.Ordinal);
        Assert.Contains("WorkspaceProductSlugInvalid", directory, StringComparison.Ordinal);
        Assert.Contains("CatalogCategorySlugNormalizer", directory, StringComparison.Ordinal);
    }

    [Fact]
    public void Domain_owns_single_slug_normalization_core_with_Try_and_throwing_APIs()
    {
        var root = FindRepoRoot();
        var normalizer = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Domain/CatalogCategorySlugNormalizer.cs"));
        Assert.Contains("TryNormalizeSlug", normalizer, StringComparison.Ordinal);
        Assert.Contains("TrySlugifyFromName", normalizer, StringComparison.Ordinal);
        Assert.Contains("TryBuildNormalizedSlug", normalizer, StringComparison.Ordinal);
        Assert.Contains("public static string NormalizeSlug", normalizer, StringComparison.Ordinal);
        Assert.Contains("public static string SlugifyFromName", normalizer, StringComparison.Ordinal);
        Assert.Contains("InvalidOperationException", normalizer, StringComparison.Ordinal);
        Assert.Contains("pendingHyphen", normalizer, StringComparison.Ordinal);

        var directory = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/ProductSeoDirectory.cs"));
        Assert.DoesNotContain("pendingHyphen", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("TryBuildNormalizedSlug", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("StringBuilder", directory, StringComparison.Ordinal);

        var catalogRoot = Path.Combine(root, "src/backend/Modules/Catalog");
        var pendingHyphenFiles = Directory.GetFiles(catalogRoot, "*.cs", SearchOption.AllDirectories)
            .Where(f => File.ReadAllText(f).Contains("pendingHyphen", StringComparison.Ordinal))
            .Select(f => Path.GetFileName(f))
            .ToArray();
        Assert.Equal(new[] { "CatalogCategorySlugNormalizer.cs" }, pendingHyphenFiles);
    }

    [Fact]
    public void W14_seo_route_ownership_and_workspace_codes_preserved()
    {
        var root = FindRepoRoot();
        var admin = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Admin/ProductSeo/CatalogProductSeoAdminEndpoints.cs"));
        Assert.Contains("MapGet(\"/seo\", GetAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/seo\", PutAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/seo/readiness\", GetReadinessAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", admin, StringComparison.Ordinal);
        Assert.Contains("ICatalogAdminAuthorizer", admin, StringComparison.Ordinal);
        Assert.Contains("CatalogWorkspaceScope", admin, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/CatalogEndpointModule.cs"));
        Assert.Single(Regex.Matches(module, @"MapCatalogProductSeoAdminEndpoints\(\)"));

        var hostEndpoints = File.ReadAllText(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Admin/ProductWorkspaceEndpoints.cs"));
        Assert.DoesNotContain("MapGet(\"/{productId:guid}/seo\"", hostEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPut(\"/{productId:guid}/seo\"", hostEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("/seo/readiness", hostEndpoints, StringComparison.Ordinal);

        var codes = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Contracts/Errors/CatalogErrorCodes.cs"));
        foreach (var code in new[]
                 {
                     "workspace.product.slug.invalid",
                     "workspace.product.slug.duplicate",
                     "workspace.catalog.stale",
                     "workspace.product.seo.rejected",
                 })
        {
            Assert.Contains(code, codes, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Host_Admin_remains_52_and_W15_not_started()
    {
        var root = FindRepoRoot();
        var admin = Path.Combine(root, "src/backend/Host/Tooba.Host/Admin");
        var files = Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories);
        Assert.Equal(52, files.Length);
        Assert.True(File.Exists(Path.Combine(admin, "ProductWorkspaceEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsEndpoints.cs")));
        Assert.True(Directory.Exists(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Application/ProductSeo")));
        Assert.True(Directory.Exists(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Application/ProductMedia")));
        Assert.False(Directory.Exists(Path.Combine(
            root, "docs/evidence/TB-TMAR-HOST-ADMIN-AMC-001-W15")));
        Assert.False(File.Exists(Path.Combine(
            root, "docs/ai/tasks/TB-TMAR-HOST-ADMIN-AMC-001-W15.task.md")));
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
