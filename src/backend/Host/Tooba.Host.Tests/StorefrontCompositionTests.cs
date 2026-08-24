using System.Text.Json;
using Tooba.Host.Storefront;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// قفل ترکیب فروشگاه: قیمت روی Product نیست و Offer نمایشی با قاعدهٔ قطعی انتخاب می‌شود.
/// </summary>
public sealed class StorefrontCompositionTests
{
    [Fact]
    public void Product_card_json_has_no_product_price_or_stock_field()
    {
        var card = new StorefrontProductCard(
            Guid.Parse("11111111-1111-7111-8111-111111111111"),
            "linen-shirt",
            "پیراهن",
            "پوشاک",
            Guid.Parse("22222222-2222-7222-8222-222222222222"),
            Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"),
            Guid.Parse("33333333-3333-7333-8333-333333333333"),
            "فروشگاه آرمان",
            1850000m,
            "IRR",
            16,
            true,
            null);
        var json = JsonSerializer.Serialize(card, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        Assert.DoesNotContain("\"price\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"stock\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"offerAmountExclusiveOfTax\":1850000", json, StringComparison.Ordinal);
        Assert.Contains("\"primaryOfferId\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Primary_offer_prefers_in_stock_then_lowest_amount()
    {
        var expensiveInStock = new StorefrontOfferCandidate(
            Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1"),
            Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb1"),
            Guid.Parse("cccccccc-cccc-4ccc-8ccc-ccccccccccc1"),
            "گران",
            "SKU-A",
            2000000m,
            "IRR",
            "IR",
            3,
            "استاندارد");
        var cheaperOutOfStock = new StorefrontOfferCandidate(
            Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa2"),
            Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb1"),
            Guid.Parse("cccccccc-cccc-4ccc-8ccc-ccccccccccc2"),
            "ارزان ناموجود",
            "SKU-B",
            1000000m,
            "IRR",
            "IR",
            0,
            "استاندارد");
        var cheaperInStock = new StorefrontOfferCandidate(
            Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa3"),
            Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb1"),
            Guid.Parse("cccccccc-cccc-4ccc-8ccc-ccccccccccc3"),
            "ارزان موجود",
            "SKU-C",
            1500000m,
            "IRR",
            "IR",
            2,
            "استاندارد");
        var selected = StorefrontPrimaryOfferResolver.Resolve([expensiveInStock, cheaperOutOfStock, cheaperInStock]);
        Assert.Equal(cheaperInStock.OfferId, selected?.OfferId);
    }

    [Fact]
    public void Detail_contract_exposes_cart_mutation_flag()
    {
        var names = typeof(StorefrontProductDetailPage).GetProperties().Select(item => item.Name).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("PrimaryOffer", names);
        Assert.Contains("OtherSellers", names);
        Assert.Contains("CartMutationEnabled", names);
        Assert.DoesNotContain("Price", names);
        Assert.DoesNotContain("Stock", names);
    }

    [Fact]
    public void Cart_page_json_keeps_offer_identity_and_has_no_product_price()
    {
        var page = new StorefrontCartPage(
            Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1"),
            3,
            "IR",
            "IRR",
            "Marketplace",
            2,
            3580000m,
            [
                new StorefrontCartLineView(
                    Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb1"),
                    Guid.Parse("cccccccc-cccc-4ccc-8ccc-ccccccccccc1"),
                    Guid.Parse("dddddddd-dddd-4ddd-8ddd-ddddddddddd1"),
                    Guid.Parse("eeeeeeee-eeee-4eee-8eee-eeeeeeeeeee1"),
                    Guid.Parse("ffffffff-ffff-4fff-8fff-fffffffffff1"),
                    "linen-shirt",
                    "پیراهن",
                    "دیجی‌استایل نمونه",
                    Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"),
                    2,
                    1790000m,
                    3580000m,
                    "IRR",
                    true)
            ],
            "guest-secret-once");
        var json = JsonSerializer.Serialize(page, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        Assert.Contains("\"offerId\":\"cccccccc-cccc-4ccc-8ccc-ccccccccccc1\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"productPrice\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"price\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"subtotalExclusiveOfTax\":3580000", json, StringComparison.Ordinal);
    }
}
