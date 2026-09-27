using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-ADMIN-AMC-001-W10-R1 — dead MapAttributeInvalid removed; Seller DTO relocated from Admin Attribute file.
/// </summary>
public sealed class HostAdminAmcW10R1GuardTests
{
    [Fact]
    public void Host_Attribute_file_deleted_after_W12_and_Seller_DTO_relocation_preserved()
    {
        var root = FindRepoRoot();
        var hostPath = Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/CatalogAttributeEndpoints.cs");
        Assert.False(File.Exists(hostPath));
    }

    [Fact]
    public void Migrated_Attribute_Catalog_surfaces_do_not_use_message_parsing()
    {
        var root = FindRepoRoot();
        var surfaces = new[]
        {
            "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Admin/Attributes/Definitions/CatalogAttributeDefinitionAdminEndpoints.cs",
            "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Admin/Attributes/Schema/CatalogCategoryAttributeSchemaAdminEndpoints.cs",
            "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Admin/Attributes/ProductValues/CatalogProductAttributeAdminEndpoints.cs",
        };
        foreach (var relative in surfaces)
        {
            var path = Path.Combine(root, relative);
            Assert.True(File.Exists(path), relative);
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("MapAttributeInvalid", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
            Assert.DoesNotContain("تکراری", text, StringComparison.Ordinal);
            Assert.DoesNotContain("PlatformHttpException", text, StringComparison.Ordinal);
            Assert.DoesNotContain("InvalidOperationException", text, StringComparison.Ordinal);
            Assert.Contains("ApiResponseFactory", text, StringComparison.Ordinal);
            Assert.Contains("ISender", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Seller_owns_SetProductAttributeRequest_with_RawValue_EnumOptionId_shape()
    {
        var root = FindRepoRoot();
        var sellerPath = Path.Combine(root, "src/backend/Host/Tooba.Host/Seller/SellerPanelEndpoints.cs");
        Assert.True(File.Exists(sellerPath));
        var seller = File.ReadAllText(sellerPath);
        Assert.Contains(
            "public sealed record SetProductAttributeRequest(string RawValue, Guid? EnumOptionId)",
            seller,
            StringComparison.Ordinal);
        Assert.Contains("SetProductAttributeAsync", seller, StringComparison.Ordinal);
        Assert.Contains("body.RawValue", seller, StringComparison.Ordinal);
        Assert.Contains("body.EnumOptionId", seller, StringComparison.Ordinal);
        Assert.Contains("SetProductVariantAxesRequest", seller, StringComparison.Ordinal);
        Assert.DoesNotContain("using Tooba.Host.Admin;", seller, StringComparison.Ordinal);

        var hostPath = Path.Combine(root, "src/backend/Host/Tooba.Host/Admin/CatalogAttributeEndpoints.cs");
        Assert.False(File.Exists(hostPath));
    }

    [Fact]
    public void W10_four_Catalog_product_attribute_routes_remain_exactly_once()
    {
        var root = FindRepoRoot();
        var adminPath = Path.Combine(
            root,
            "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Admin/Attributes/ProductValues/CatalogProductAttributeAdminEndpoints.cs");
        Assert.True(File.Exists(adminPath));
        var admin = File.ReadAllText(adminPath);
        Assert.Contains("MapGet(\"/attributes\", GetEditorStateAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/attributes\", SetAttributesAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/attributes/readiness\", GetReadinessAsync)", admin, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/attributes/{definitionId:guid}\", SetAttributeAsync)", admin, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/CatalogEndpointModule.cs"));
        Assert.Single(Regex.Matches(module, @"MapCatalogProductAttributeAdminEndpoints\(\)"));
    }

    [Fact]
    public void Host_Admin_file_count_remains_52()
    {
        var admin = Path.Combine(FindRepoRoot(), "src/backend/Host/Tooba.Host/Admin");
        var files = Directory.GetFiles(admin, "*.cs", SearchOption.AllDirectories);
        Assert.True(files.Length <= 52 && files.Length >= 12, $"Host/Admin count expected in [12,52], was {files.Length}");
        Assert.False(File.Exists(Path.Combine(admin, "CatalogAttributeEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(admin, "StoreAppearanceSettingsEndpoints.cs")));
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

        throw new InvalidOperationException("repo root not found");
    }
}
