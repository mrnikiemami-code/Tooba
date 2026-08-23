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
        var item = new AdminProductListItem(Guid.Parse("11111111-1111-7111-8111-111111111111"), "shirt", "Published", 1, 2, DateTimeOffset.UnixEpoch);
        var json = JsonSerializer.Serialize(item, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        Assert.DoesNotContain("\"price\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"offerCount\":2", json, StringComparison.Ordinal);
    }
}
