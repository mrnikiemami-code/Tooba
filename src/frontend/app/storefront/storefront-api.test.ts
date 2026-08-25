import assert from "node:assert/strict";
import test from "node:test";
import { formatOfferAmount, mapStorefrontDetail, mapStorefrontHome, mapStorefrontListing } from "./storefront-api.ts";
import { buildProductStructuredData } from "./storefront-product-seo.ts";

test("home mapper keeps offer amount off a product price field", () => {
  const home = mapStorefrontHome({
    heroTitle: "خانه",
    heroSubtitle: "زنده",
    categories: [
      { categoryId: "c1", parentCategoryId: null, name: "پوشاک" },
      { categoryId: "c2", parentCategoryId: "c1", name: "پیراهن" },
    ],
    brands: [{ brandId: "b1", name: "آرمان" }],
    featuredProducts: [
      {
        productId: "p1",
        slug: "shirt",
        title: "پیراهن",
        categoryName: "پوشاک",
        primaryOfferId: "o1",
        sellerDisplayName: "آرمان",
        offerAmountExclusiveOfTax: 1850000,
        promotionalAmountExclusiveOfTax: 1650000,
        currency: "IRR",
        availableUnits: 4,
        inStock: true,
      },
    ],
    specialOffers: [
      {
        productId: "p1",
        slug: "shirt",
        title: "پیراهن",
        categoryName: "پوشاک",
        categoryId: "c2",
        primaryOfferId: "o1",
        sellerDisplayName: "آرمان",
        offerAmountExclusiveOfTax: 1850000,
        promotionalAmountExclusiveOfTax: 1650000,
        currency: "IRR",
        availableUnits: 4,
        inStock: true,
        promotionLabel: "جشنواره تابستان",
      },
    ],
    campaignProducts: [],
    newArrivals: [],
    productRail: [],
  });
  assert.equal(home?.featuredProducts[0]?.offerAmountExclusiveOfTax, 1850000);
  assert.equal(home?.brands[0]?.name, "آرمان");
  assert.equal(home?.categories[1]?.parentCategoryId, "c1");
  assert.equal(home?.specialOffers[0]?.promotionLabel, "جشنواره تابستان");
  assert.equal(home?.specialOffers[0]?.promotionalAmountExclusiveOfTax, 1650000);
  assert.equal(home?.campaignProducts.length, 0);
  assert.equal("price" in (home?.featuredProducts[0] ?? {}), false);
});

test("detail mapper reads amount from primary offer only", () => {
  const detail = mapStorefrontDetail({
    productId: "p1",
    slug: "shirt",
    title: "پیراهن مردانه لینن",
    categoryName: "پوشاک",
    brandName: "آرمان",
    mediaAssetIds: [],
    selectedVariantId: "v1",
    primaryOffer: {
      offerId: "o-cheap",
      catalogVariantId: "v1",
      sellerPartyId: "s2",
      sellerDisplayName: "دیجی‌استایل نمونه",
      sellerSku: "DGS-LN-01",
      amountExclusiveOfTax: 1790000,
      currency: "IRR",
      market: "IR",
      availableUnits: 4,
      taxCategoryLabel: "استاندارد",
    },
    otherSellers: [
      {
        offerId: "o-other",
        sellerDisplayName: "فروشگاه آرمان",
        amountExclusiveOfTax: 1850000,
        currency: "IRR",
        availableUnits: 16,
        inStock: true,
      },
    ],
    relatedProducts: [
      {
        productId: "internal-related-product",
        slug: "related-shirt",
        title: "پیراهن مرتبط",
        categoryName: "پوشاک",
        primaryOfferId: "internal-related-offer",
        sellerPartyId: "internal-related-seller",
        sellerDisplayName: "فروشگاه آرمان",
        offerAmountExclusiveOfTax: 1850000,
        currency: "IRR",
        availableUnits: 2,
        inStock: true,
      },
    ],
    seoTitle: "پیراهن",
    seoDescription: "کالای زنده",
    cartMutationEnabled: false,
  });
  assert.equal(detail?.primaryOffer.amountExclusiveOfTax, 1790000);
  assert.equal(detail?.otherSellers.length, 1);
  assert.equal(detail?.relatedProducts[0]?.slug, "related-shirt");
  assert.equal(detail?.cartMutationEnabled, false);
  assert.equal(formatOfferAmount(1790000, "IRR").includes("ریال"), true);
  assert.ok(detail);
  const structuredData = JSON.stringify(buildProductStructuredData(detail, "/products/shirt"));
  assert.equal(structuredData.includes("p1"), false);
  assert.equal(structuredData.includes("o-cheap"), false);
  assert.equal(structuredData.includes("s2"), false);
  assert.match(structuredData, /"url":"\/products\/shirt"/);
});

test("listing mapper keeps backend discovery facets and pagination", () => {
  const listing = mapStorefrontListing({
    categories: [{ categoryId: "c1", parentCategoryId: null, name: "پوشاک" }],
    sellers: [{ sellerPartyId: "s1", displayName: "آرمان" }],
    products: [{
      productId: "p1",
      slug: "shirt",
      title: "پیراهن",
      categoryName: "پوشاک",
      categoryId: "c1",
      primaryOfferId: "o1",
      sellerPartyId: "s1",
      sellerDisplayName: "آرمان",
      offerAmountExclusiveOfTax: 100,
      currency: "IRR",
      availableUnits: 2,
      inStock: true,
    }],
    query: "پیراهن",
    categoryId: "c1",
    sellerPartyId: "s1",
    inStock: true,
    sort: "price-asc",
    page: 2,
    pageSize: 24,
    totalCount: 30,
  });

  assert.equal(listing?.sellers[0]?.sellerPartyId, "s1");
  assert.equal(listing?.products[0]?.sellerPartyId, "s1");
  assert.equal(listing?.sort, "price-asc");
  assert.equal(listing?.page, 2);
  assert.equal(listing?.totalCount, 30);
});
