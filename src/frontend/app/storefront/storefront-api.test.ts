import assert from "node:assert/strict";
import test from "node:test";
import {
  formatOfferAmount,
  loadStorefrontDetail,
  mapStorefrontDetail,
  mapStorefrontHome,
  mapStorefrontListing,
  mapStorefrontMerchandising,
  mapStorefrontPublicSeller,
  mapStorefrontReviews,
  submitStorefrontReview,
  StorefrontReviewApiError,
} from "./storefront-api.ts";
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
        averageRating: 4.2,
        reviewCount: 8,
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
  assert.equal(home?.featuredProducts[0]?.averageRating, 4.2);
  assert.equal(home?.featuredProducts[0]?.reviewCount, 8);
  assert.equal(home?.specialOffers[0]?.averageRating, null);
  assert.equal(home?.specialOffers[0]?.reviewCount, 0);
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
    shortDescription: "خلاصهٔ واقعی",
    fullDescription: "شرح کامل واقعی",
    specifications: [{ label: "جنس", value: "لینن" }],
    variants: [{
      variantId: "v1",
      axes: [{ label: "رنگ", value: "آبی" }],
      primaryOffer: null,
      otherSellers: [],
    }],
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
    averageRating: 4.5,
    reviewCount: 12,
    cartMutationEnabled: false,
  });
  assert.equal(detail?.primaryOffer.amountExclusiveOfTax, 1790000);
  assert.equal(detail?.shortDescription, "خلاصهٔ واقعی");
  assert.equal(detail?.fullDescription, "شرح کامل واقعی");
  assert.deepEqual(detail?.specifications, [{ label: "جنس", value: "لینن" }]);
  assert.equal(detail?.variants[0]?.options[0]?.value, "آبی");
  assert.equal(detail?.averageRating, 4.5);
  assert.equal(detail?.reviewCount, 12);
  assert.equal(detail?.otherSellers.length, 1);
  assert.equal(detail?.relatedProducts[0]?.slug, "related-shirt");
  assert.equal(detail?.cartMutationEnabled, false);
  assert.equal(formatOfferAmount(1790000, "IRR").includes("ریال"), true);
  assert.ok(detail);
  const structuredData = JSON.stringify(buildProductStructuredData(detail, "/products/shirt"));
  assert.equal(structuredData.includes("p1"), false);
  assert.equal(structuredData.includes("o-cheap"), false);
  assert.equal(structuredData.includes("s2"), false);
  assert.equal(structuredData.includes("AggregateRating"), true);
  assert.equal(structuredData.includes('"reviewCount":12'), true);
  assert.match(structuredData, /"url":"\/products\/shirt"/);
});

test("maps only public review contract and backend aggregate", () => {
  const page = mapStorefrontReviews({
    AverageRating: 4.5,
    ReviewCount: 2,
    RatingDistribution: { 5: 1, 4: 1 },
    Reviews: [{
      ReviewId: "public-r1",
      AuthorDisplayName: "مینا",
      Rating: 5,
      Title: "خوب",
      Body: "کیفیت کالا بسیار خوب بود.",
      CreatedAt: "2026-08-25T00:00:00Z",
      VerifiedPurchase: true,
      ActorUserId: "internal-user",
      ModerationNotes: "private",
    }],
    Page: 1, PageSize: 10, TotalCount: 2,
  });
  assert.equal(page?.averageRating, 4.5);
  assert.equal(page?.ratingDistribution[0]?.count, 1);
  assert.equal(page?.reviews[0]?.verifiedPurchase, true);
  assert.equal("ActorUserId" in (page?.reviews[0] ?? {}), false);
  assert.equal("ModerationNotes" in (page?.reviews[0] ?? {}), false);
});

test("zero reviews do not emit aggregate rating", () => {
  const detail = mapStorefrontDetail({
    productId: "p", primaryOffer: { offerId: "o" }, reviewCount: 0, averageRating: null,
  });
  assert.ok(detail);
  assert.equal("aggregateRating" in buildProductStructuredData(detail, "/products/p"), false);
});

test("review submission sends exact payload and exposes auth/conflict statuses", async () => {
  const originalFetch = globalThis.fetch;
  for (const status of [401, 403, 409]) {
    let body = "";
    globalThis.fetch = (async (_input, init) => {
      body = String(init?.body);
      return new Response(null, { status });
    }) as typeof fetch;
    await assert.rejects(
      submitStorefrontReview({ productId: "p1", rating: 5, title: "عالی", body: "حداقل ده کاراکتر متن" }),
      (error) => error instanceof StorefrontReviewApiError && error.status === status,
    );
    assert.deepEqual(JSON.parse(body), { productId: "p1", rating: 5, title: "عالی", body: "حداقل ده کاراکتر متن" });
  }
  globalThis.fetch = originalFetch;
});

test("detail loader sends selected variant only to the authoritative endpoint", async () => {
  const originalFetch = globalThis.fetch;
  let requestedUrl = "";
  globalThis.fetch = (async (input) => {
    requestedUrl = String(input);
    return new Response(null, { status: 404 });
  }) as typeof fetch;
  try {
    await loadStorefrontDetail("پیراهن آبی", "variant/blue");
  } finally {
    globalThis.fetch = originalFetch;
  }
  assert.match(requestedUrl, /\/v1\/storefront\/products\/%D9%BE%DB%8C%D8%B1%D8%A7%D9%87%D9%86%20%D8%A2%D8%A8%DB%8C\?variantId=variant%2Fblue$/);
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

test("unsupported merchandising remains empty and explicit", () => {
  const page = mapStorefrontMerchandising({
    kind: "most-viewed",
    title: "پربازدیدترین‌ها",
    supported: false,
    unavailableReason: "سیگنال معتبر بازدید وجود ندارد.",
    products: [],
  });
  assert.equal(page?.supported, false);
  assert.equal(page?.products.length, 0);
  assert.match(page?.unavailableReason ?? "", /سیگنال معتبر/);
});

test("public seller mapper ignores private and internal party fields", () => {
  const seller = mapStorefrontPublicSeller({
    publicId: "public-seller",
    displayName: "فروشگاه آرمان",
    activeOfferCount: 2,
    productCount: 2,
    partyId: "internal-party",
    legalName: "private",
    settlementAccount: "secret",
  });
  assert.deepEqual(seller, {
    publicId: "public-seller",
    displayName: "فروشگاه آرمان",
    activeOfferCount: 2,
    productCount: 2,
  });
});
