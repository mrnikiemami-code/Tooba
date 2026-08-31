using System.Text.Json;
using Tooba.Host.Admin;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// قفل ترکیب Workspace: قیمت و موجودی روی هویت Product نیستند.
/// </summary>
public sealed class ProductWorkspaceCompositionTests
{
    [Fact]
    public void List_item_and_workspace_contracts_do_not_carry_product_price_or_stock()
    {
        var listNames = typeof(AdminProductListItem).GetProperties().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        Assert.DoesNotContain("Price", listNames);
        Assert.DoesNotContain("Stock", listNames);
        Assert.Contains("OfferCount", listNames);
        Assert.Contains("BrandName", listNames);
        Assert.Contains("PrimaryCategoryName", listNames);
        Assert.Contains("AdditionalCategoryNames", listNames);
        Assert.Contains("AdditionalCategoryCount", listNames);

        var workspaceNames = typeof(ProductWorkspaceView).GetProperties().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        Assert.DoesNotContain("Price", workspaceNames);
        Assert.DoesNotContain("StockQuantity", workspaceNames);
        Assert.Contains("Offers", workspaceNames);
        Assert.Contains("Prices", workspaceNames);
        Assert.Contains("Stock", workspaceNames);
    }

    [Fact]
    public void Serialized_list_item_json_has_no_product_price_field()
    {
        var item = new AdminProductListItem(
            Guid.Parse("11111111-1111-7111-8111-111111111111"),
            "shirt",
            "Published",
            1,
            2,
            "پوشاک",
            "1790000–1850000 IRR",
            12,
            3,
            DateTimeOffset.UnixEpoch,
            Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"));
        var json = JsonSerializer.Serialize(item, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        Assert.DoesNotContain("\"price\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"offerCount\":2", json, StringComparison.Ordinal);
        Assert.Contains("\"offerAmountRange\"", json, StringComparison.Ordinal);
        Assert.Contains("\"sellableUnits\":12", json, StringComparison.Ordinal);
        Assert.Contains("\"primaryMediaAssetId\"", json, StringComparison.Ordinal);
        Assert.Contains("\"brandName\"", json, StringComparison.Ordinal);
        Assert.Contains("\"primaryCategoryName\"", json, StringComparison.Ordinal);
        Assert.Contains("\"additionalCategoryNames\"", json, StringComparison.Ordinal);
        Assert.Contains("\"additionalCategoryCount\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Serialized_list_item_uses_leaf_primary_and_additional_arrays_not_path_blob()
    {
        var item = new AdminProductListItem(
            Guid.Parse("11111111-1111-7111-8111-111111111111"),
            "phone",
            "Draft",
            1,
            0,
            "گوشی هوشمند، گوشی اقتصادی",
            "بدون مبلغ",
            0,
            0,
            DateTimeOffset.UnixEpoch,
            null,
            Guid.Parse("22222222-2222-7222-8222-222222222222"),
            "بدون برند",
            "گوشی هوشمند",
            ["گوشی اقتصادی", "موبایل دانشجویی", "پیشنهاد ویژه", "پرچمدار"],
            4);
        var json = JsonSerializer.Serialize(item, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("گوشی هوشمند", doc.RootElement.GetProperty("primaryCategoryName").GetString());
        Assert.DoesNotContain(" > ", json, StringComparison.Ordinal);
        Assert.Equal(4, doc.RootElement.GetProperty("additionalCategoryCount").GetInt32());
        var names = doc.RootElement.GetProperty("additionalCategoryNames").EnumerateArray().Select(x => x.GetString()).ToList();
        Assert.Equal(4, names.Count);
        Assert.Equal("گوشی اقتصادی", names[0]);
    }
}
