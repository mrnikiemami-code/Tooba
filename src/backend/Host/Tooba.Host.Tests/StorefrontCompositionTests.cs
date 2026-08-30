using System.Text.Json;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;
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
            Guid.Parse("44444444-4444-7444-8444-444444444444"),
            "فروشگاه آرمان",
            1850000m,
            1650000m,
            "IRR",
            16,
            true,
            null);
        var json = JsonSerializer.Serialize(card, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        Assert.DoesNotContain("\"price\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"stock\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"offerAmountExclusiveOfTax\":1850000", json, StringComparison.Ordinal);
        Assert.Contains("\"promotionalAmountExclusiveOfTax\":1650000", json, StringComparison.Ordinal);
        Assert.Contains("\"primaryOfferId\"", json, StringComparison.Ordinal);
        Assert.Contains("\"sellerPartyId\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Category_landing_includes_all_published_descendants()
    {
        var root = Guid.Parse("11111111-1111-7111-8111-111111111111");
        var child = Guid.Parse("22222222-2222-7222-8222-222222222222");
        var grandchild = Guid.Parse("33333333-3333-7333-8333-333333333333");
        var unrelated = Guid.Parse("44444444-4444-7444-8444-444444444444");
        var categories = new[]
        {
            new StorefrontCategoryItem(root, null, "پوشاک"),
            new StorefrontCategoryItem(child, root, "مردانه"),
            new StorefrontCategoryItem(grandchild, child, "پیراهن"),
            new StorefrontCategoryItem(unrelated, null, "خانه"),
        };

        var included = StorefrontComposer.DescendantCategoryIds(categories, root);

        Assert.Contains(root, included);
        Assert.Contains(child, included);
        Assert.Contains(grandchild, included);
        Assert.DoesNotContain(unrelated, included);
    }

    [Fact]
    public void Category_plp_page_json_exposes_canonical_path_facets_and_no_product_price()
    {
        var categoryId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
        var card = new StorefrontProductCard(
            Guid.Parse("11111111-1111-7111-8111-111111111111"),
            "linen-shirt",
            "پیراهن",
            "پوشاک",
            categoryId,
            Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb"),
            Guid.Parse("33333333-3333-7333-8333-333333333333"),
            Guid.Parse("44444444-4444-7444-8444-444444444444"),
            "فروشگاه آرمان",
            1850000m,
            null,
            "IRR",
            4,
            true,
            null);
        var page = new StorefrontCategoryPlpPage(
            categoryId,
            "fa-IR",
            "poshak",
            "پوشاک",
            "توضیح کوتاه",
            null,
            "/fa/category/poshak",
            false,
            null,
            1,
            1,
            24,
            "default",
            [new StorefrontCategoryBreadcrumbItem(categoryId, "پوشاک", "poshak", "/fa/category/poshak")],
            [],
            [
                new StorefrontPlpFacet(
                    Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc"),
                    "color",
                    "رنگ",
                    "Enumeration",
                    "CheckboxList",
                    true,
                    false,
                    true,
                    null,
                    null,
                    [new StorefrontPlpFacetOption("blue", "آبی", 1)]),
            ],
            [new StorefrontAppliedFilterChip("color", "رنگ", "blue", "آبی")],
            [card],
            ["default", "newest", "price-asc", "price-desc"]);

        var json = JsonSerializer.Serialize(page, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        Assert.Contains("\"canonicalPath\":\"/fa/category/poshak\"", json, StringComparison.Ordinal);
        Assert.Contains("\"facets\"", json, StringComparison.Ordinal);
        Assert.Contains("\"appliedFilters\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"price\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"offerAmountExclusiveOfTax\":1850000", json, StringComparison.Ordinal);
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
        Assert.Contains("RelatedProducts", names);
        Assert.Contains("CartMutationEnabled", names);
        Assert.DoesNotContain("Price", names);
        Assert.DoesNotContain("Stock", names);
    }

    [Fact]
    public void Detail_contract_exposes_descriptions_variants_and_truthful_review_aggregate()
    {
        var names = typeof(StorefrontProductDetailPage).GetProperties()
            .Select(item => item.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("ShortDescription", names);
        Assert.Contains("FullDescription", names);
        Assert.Contains("Specifications", names);
        Assert.Contains("Variants", names);
        Assert.Contains("SelectedVariantId", names);
        Assert.Contains("PromotionalAmountExclusiveOfTax", names);
        Assert.Contains("AverageRating", names);
        Assert.Contains("ReviewCount", names);
        Assert.DoesNotContain("RatingAggregate", names);
        Assert.DoesNotContain("Reviews", names);

        var variantNames = typeof(StorefrontProductVariant).GetProperties()
            .Select(item => item.Name)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("Axes", variantNames);
        Assert.Contains("Purchasable", variantNames);
        Assert.Contains("PrimaryOffer", variantNames);
        Assert.DoesNotContain("Price", variantNames);
        Assert.DoesNotContain("Stock", variantNames);
    }

    [Fact]
    public void Alternate_seller_contract_remains_offer_owned_and_has_no_product_price_or_inventory_field()
    {
        var names = typeof(StorefrontAlternateOffer).GetProperties()
            .Select(item => item.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("OfferId", names);
        Assert.Contains("AmountExclusiveOfTax", names);
        Assert.Contains("AvailableUnits", names);
        Assert.DoesNotContain("ProductPrice", names);
        Assert.DoesNotContain("ProductStock", names);
        Assert.DoesNotContain("InventoryId", names);
    }

    [Fact]
    public void Listing_contract_exposes_only_backend_owned_discovery_state()
    {
        var names = typeof(StorefrontListingPage).GetProperties().Select(item => item.Name).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("Categories", names);
        Assert.Contains("Sellers", names);
        Assert.Contains("InStock", names);
        Assert.Contains("Sort", names);
        Assert.Contains("TotalCount", names);
        Assert.DoesNotContain("PriceFacet", names);
        Assert.DoesNotContain("BrandFacet", names);
    }

    [Fact]
    public void Unsupported_merchandising_never_contains_fabricated_products()
    {
        var page = StorefrontComposer.UnsupportedMerchandising(
            "trending",
            "محبوب‌های روز",
            "سیگنال معتبر روند وجود ندارد.");

        Assert.False(page.Supported);
        Assert.Empty(page.Products);
        Assert.NotNull(page.UnavailableReason);
    }

    [Fact]
    public void Public_seller_identity_is_stable_and_does_not_expose_party_id()
    {
        var partyId = Guid.Parse("44444444-4444-7444-8444-444444444444");

        var first = StorefrontComposer.CreatePublicSellerId(partyId);
        var second = StorefrontComposer.CreatePublicSellerId(partyId);

        Assert.Equal(first, second);
        Assert.DoesNotContain(partyId.ToString("N"), first, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(24, first.Length);
    }

    [Fact]
    public void Public_seller_contract_has_no_private_or_authorization_fields()
    {
        var names = typeof(StorefrontPublicSellerItem).GetProperties().Select(item => item.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("PartyId", names);
        Assert.DoesNotContain("LegalName", names);
        Assert.DoesNotContain("Email", names);
        Assert.DoesNotContain("Phone", names);
        Assert.DoesNotContain("Settlement", names);
        Assert.DoesNotContain("Authorization", names);
    }

    [Fact]
    public void Related_products_exclude_current_product_and_prefer_same_category()
    {
        var current = Guid.Parse("11111111-1111-7111-8111-111111111111");
        var category = Guid.Parse("22222222-2222-7222-8222-222222222222");
        var otherCategory = Guid.Parse("33333333-3333-7333-8333-333333333333");
        StorefrontProductCard Card(Guid id, string slug, Guid? categoryId) => new(
            id,
            slug,
            slug,
            "رده",
            categoryId,
            null,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "فروشنده",
            100m,
            null,
            "IRR",
            1,
            true,
            null);

        var related = StorefrontComposer.SelectRelatedProducts(
            [Card(current, "current", category), Card(Guid.NewGuid(), "other", otherCategory), Card(Guid.NewGuid(), "same", category)],
            current,
            category);

        Assert.Equal(["same", "other"], related.Select(item => item.Slug));
        Assert.DoesNotContain(related, item => item.ProductId == current);
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

    [Fact]
    public void Plp_visible_facet_codes_are_viewed_category_plus_global_brand_only()
    {
        var colorDef = Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc");
        var categoryFacets = new[]
        {
            new EffectiveCategoryFacet(
                colorDef,
                "color",
                "رنگ",
                CatalogAttributeValueKind.Enumeration,
                CatalogFacetDisplayType.CheckboxList,
                1,
                true,
                true,
                false,
                true,
                Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"),
                false),
            new EffectiveCategoryFacet(
                Guid.Parse("dddddddd-dddd-4ddd-8ddd-dddddddddddd"),
                "hidden_axis",
                "پنهان",
                CatalogAttributeValueKind.Enumeration,
                CatalogFacetDisplayType.CheckboxList,
                2,
                false,
                true,
                false,
                true,
                Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"),
                false),
        };

        var codes = StorefrontComposer.ResolveVisiblePlpFacetCodes(categoryFacets);
        Assert.Contains("color", codes);
        Assert.Contains(StorefrontComposer.GlobalBrandFacetCode, codes);
        Assert.DoesNotContain("hidden_axis", codes);
        Assert.DoesNotContain("storage", codes); // not unioned from another product Primary Category
        Assert.Equal(StorefrontComposer.GlobalBrandFacetCode, "brand");
    }

    [Fact]
    public void Plp_typed_filter_excludes_products_missing_selected_facet_value()
    {
        var defId = Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc");
        var withValue = Guid.Parse("11111111-1111-7111-8111-111111111111");
        var missing = Guid.Parse("22222222-2222-7222-8222-222222222222");
        StorefrontProductCard Card(Guid id) => new(
            id, id.ToString("N"), "کالا", "رده", null, null,
            Guid.NewGuid(), Guid.NewGuid(), "فروشنده", 100m, null, "IRR", 1, true, null);

        var facetDefs = new[]
        {
            new EffectiveCategoryFacet(
                defId, "color", "رنگ", CatalogAttributeValueKind.Enumeration,
                CatalogFacetDisplayType.CheckboxList, 1, true, true, false, true,
                Guid.NewGuid(), false),
        };
        var values = new[]
        {
            CatalogProductAttributeValue.Create(withValue, defId, "blue"),
        };
        var filters = new[]
        {
            new StorefrontPlpFilterInput("color", "enum", ["blue"], null, null),
        };

        var filtered = StorefrontComposer.ApplyTypedFilters(
            [Card(withValue), Card(missing)],
            values,
            filters,
            facetDefs);

        Assert.Single(filtered);
        Assert.Equal(withValue, filtered[0].ProductId);
    }

    [Fact]
    public void Plp_brand_facet_options_exclude_brandless_and_selected_brand_filters_brandless_out()
    {
        var brandA = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
        var brandB = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");
        StorefrontProductCard Card(Guid id, Guid? brandId) => new(
            id, id.ToString("N"), "کالا", "رده", null, null,
            Guid.NewGuid(), Guid.NewGuid(), "فروشنده", 100m, null, "IRR", 1, true, null,
            BrandId: brandId);

        var branded = Card(Guid.Parse("11111111-1111-7111-8111-111111111111"), brandA);
        var other = Card(Guid.Parse("22222222-2222-7222-8222-222222222222"), brandB);
        var brandless = Card(Guid.Parse("33333333-3333-7333-8333-333333333333"), null);

        var facet = StorefrontComposer.BuildGlobalBrandFacet(
            [branded, other, brandless],
            new Dictionary<Guid, string> { [brandA] = "آرمان", [brandB] = "نوین" });

        Assert.Equal(StorefrontComposer.GlobalBrandFacetCode, facet.Code);
        Assert.Equal(2, facet.Options.Count);
        Assert.DoesNotContain(facet.Options, o => o.Label.Contains("بدون", StringComparison.Ordinal));
        Assert.Contains(facet.Options, o => o.Value == brandA.ToString("D"));

        var noBrandFilter = StorefrontComposer.ApplyTypedFilters(
            [branded, other, brandless],
            [],
            [],
            []);
        Assert.Equal(3, noBrandFilter.Count);

        var filtered = StorefrontComposer.ApplyTypedFilters(
            [branded, other, brandless],
            [],
            [new StorefrontPlpFilterInput(StorefrontComposer.GlobalBrandFacetCode, "enum", [brandA.ToString("D")], null, null)],
            []);
        Assert.Single(filtered);
        Assert.Equal(branded.ProductId, filtered[0].ProductId);
    }
}
