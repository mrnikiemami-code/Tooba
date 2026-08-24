using System.Text.Json;
using Tooba.Host.Seller;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// قفل قرارداد و ایزولهٔ پنل فروشنده: فیلتر در سرور است و Product.Price وجود ندارد.
/// </summary>
public sealed class SellerPanelCompositionTests
{
    [Fact]
    public void Offer_list_contract_has_no_product_price_or_stock_identity()
    {
        var names = typeof(SellerOfferListItem).GetProperties().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        Assert.DoesNotContain("Price", names);
        Assert.DoesNotContain("Stock", names);
        Assert.Contains("OfferId", names);
        Assert.Contains("Amount", names);
        Assert.Contains("AvailableUnits", names);
        Assert.Contains("SellerSku", names);
    }

    [Fact]
    public void Offer_detail_marks_catalog_read_only()
    {
        var names = typeof(SellerOfferDetailPage).GetProperties().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("CatalogReadOnly", names);
        Assert.Contains("SellerSku", names);
        Assert.Contains("Amount", names);
        Assert.DoesNotContain("ProductPrice", names);
    }

    [Fact]
    public void Order_list_and_detail_keep_seller_slice_identity()
    {
        var list = typeof(SellerOrderListItem).GetProperties().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("SellerOrderId", list);
        Assert.Contains("PayableAmount", list);
        Assert.DoesNotContain("BuyerUserId", list);

        var detail = typeof(SellerOrderDetailPage).GetProperties().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("SellerPartyId", detail);
        Assert.Contains("Lines", detail);
    }

    [Fact]
    public void Serialized_offer_row_keeps_offer_amount_not_product_price()
    {
        var item = new SellerOfferListItem(
            Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"),
            Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb"),
            Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc"),
            "پیراهن",
            "LIVE-A",
            "Active",
            1850000m,
            "IRR",
            12,
            null);
        var json = JsonSerializer.Serialize(item, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        Assert.Contains("\"offerId\"", json, StringComparison.Ordinal);
        Assert.Contains("\"amount\":1850000", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"price\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"stock\":", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Endpoints_require_seller_party_header_constant()
    {
        Assert.Equal("X-Tooba-Seller-Party-Id", SellerPanelEndpoints.SellerPartyHeader);
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "backend",
            "Host",
            "Tooba.Host",
            "Seller",
            "SellerPanelComposer.cs"));
        Assert.Contains("x.SellerPartyId == sellerPartyId", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".Join(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("FromSql", source, StringComparison.OrdinalIgnoreCase);
    }

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

        throw new InvalidOperationException("repo root not found");
    }
}
