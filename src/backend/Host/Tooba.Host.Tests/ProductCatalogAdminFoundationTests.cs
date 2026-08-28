using Tooba.Host.Admin;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// قفل قرارداد Product Catalog Admin — Create پیش‌نویس، بدون Product.Price.
/// </summary>
public sealed class ProductCatalogAdminFoundationTests
{
    [Fact]
    public void Create_request_carries_category_and_slug_without_price_fields()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(
            new AdminProductCreateRequest("آیفون ۱۶", "iphone-16", Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"), "fa-IR"),
            new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
        Assert.Contains("\"categoryId\"", json, StringComparison.Ordinal);
        Assert.Contains("\"slug\":\"iphone-16\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"price\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"stock\"", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Workspace_view_exposes_category_path_slug_and_translations_without_product_price()
    {
        var view = new ProductWorkspaceView(
            Guid.Parse("11111111-1111-7111-8111-111111111111"),
            "آیفون ۱۶",
            "Draft",
            "PhysicalGood",
            null,
            ["موبایل"],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            new ProductSeoView("iphone-16", null, ""),
            new ProductPublicationView("Draft", false, ["missing-image"]),
            [],
            [],
            new ProductWorkspacePermissions(true, true, false, false, true),
            DateTimeOffset.UtcNow,
            ["missing-image"],
            [],
            Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"),
            "کالای دیجیتال > موبایل",
            "iphone-16",
            "خلاصه",
            [new ProductTranslationView("fa-IR", "آیفون ۱۶", "iphone-16", "خلاصه", null, null, null)]);

        var json = System.Text.Json.JsonSerializer.Serialize(
            view,
            new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
        Assert.Contains("\"categoryPath\"", json, StringComparison.Ordinal);
        Assert.Contains("\"translations\"", json, StringComparison.Ordinal);
        Assert.Contains("\"slug\":\"iphone-16\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"price\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Draft", view.Status);
    }

    [Fact]
    public void Category_assign_request_requires_explicit_schema_confirmation_flag()
    {
        var body = new AdminProductCategoryAssignRequest(
            Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"),
            ConfirmSchemaImpact: false,
            DateTimeOffset.UtcNow);
        Assert.False(body.ConfirmSchemaImpact);
        Assert.NotEqual(Guid.Empty, body.CategoryId);
    }
}
