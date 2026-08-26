import assert from "node:assert/strict";
import test from "node:test";
import { mapStorefrontHome } from "./storefront-api.ts";

test("home mapper prefers homeCategories rail and does not require dumping all categories", () => {
  const home = mapStorefrontHome({
    heroTitle: "خانه",
    heroSubtitle: "زنده",
    categories: [
      { categoryId: "c1", parentCategoryId: null, name: "موبایل" },
      { categoryId: "c2", parentCategoryId: null, name: "لپ‌تاپ" },
      { categoryId: "c3", parentCategoryId: "c1", name: "گوشی" },
    ],
    homeCategories: [
      { categoryId: "c1", parentCategoryId: null, name: "موبایل" },
      { categoryId: "c2", parentCategoryId: null, name: "لپ‌تاپ" },
    ],
    bestSellerColumns: [
      {
        categoryId: "c1",
        categoryName: "موبایل",
        products: [
          {
            productId: "p1",
            slug: "phone",
            title: "گوشی",
            categoryName: "موبایل",
            primaryOfferId: "o1",
            sellerDisplayName: "آرمان",
            offerAmountExclusiveOfTax: 1000,
            currency: "IRR",
            availableUnits: 2,
            inStock: true,
            reviewCount: 3,
          },
        ],
      },
    ],
    mostViewedProducts: [],
    featuredProducts: [],
    specialOffers: [],
    campaignProducts: [],
    newArrivals: [],
    productRail: [],
    brands: [{ brandId: "b1", slug: "arman", name: "آرمان", productCount: 1, logoMediaAssetId: "d0d0d0d0-0001-4000-8000-000000000001" }],
    featuredReviews: [{
      publicId: "r1",
      authorDisplayName: "مریم",
      rating: 5,
      title: "عالی",
      body: "کیفیت خوب بود.",
      verifiedPurchase: true,
      createdAt: "2026-08-20T10:00:00Z",
      productTitle: "گوشی",
      productSlug: "phone",
    }],
    latestArticles: [{
      articleId: "a1",
      slug: "guide",
      title: "راهنما",
      excerpt: "متن کوتاه",
      coverMediaAssetId: "d0d0d0d0-0001-4000-8000-000000000001",
      publishDate: "2026-08-20T10:00:00Z",
      authorDisplayName: "تحریریه",
      tags: ["راهنما"],
      isFeatured: true,
    }],
  });

  assert.ok(home);
  assert.equal(home.categories.length, 3);
  assert.equal(home.homeCategories.length, 2);
  assert.equal(home.bestSellerColumns.length, 1);
  assert.equal(home.bestSellerColumns[0]?.products[0]?.offerAmountExclusiveOfTax, 1000);
  assert.equal(home.featuredReviews.length, 1);
  assert.equal(home.latestArticles.length, 1);
  assert.equal(home.brands[0]?.logoMediaAssetId?.includes("d0d0"), true);
});
