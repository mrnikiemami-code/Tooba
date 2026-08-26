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
    brands: [{ brandId: "b1", slug: "arman", name: "آرمان", productCount: 1 }],
  });

  assert.ok(home);
  assert.equal(home.categories.length, 3);
  assert.equal(home.homeCategories.length, 2);
  assert.equal(home.bestSellerColumns.length, 1);
  assert.equal(home.bestSellerColumns[0]?.products[0]?.offerAmountExclusiveOfTax, 1000);
  assert.equal("price" in (home.bestSellerColumns[0]?.products[0] as object), false);
});
