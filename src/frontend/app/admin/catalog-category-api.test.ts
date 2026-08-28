import assert from "node:assert/strict";
import test from "node:test";
import {
  buildStorefrontCategoryRoute,
  mapCategoryMutationError,
  mapCategoryTreeNode,
  mapCategoryWorkspace,
  slugifyCategoryName,
  slugLooksLikeIdSuffixed,
  CATEGORY_SLUG_DUPLICATE_MESSAGE,
} from "./catalog-category-api.ts";

test("slugifyCategoryName mirrors kebab + unicode keep", () => {
  assert.equal(slugifyCategoryName("Hello World"), "hello-world");
  assert.equal(slugifyCategoryName("  کتاب  خانه  "), "کتاب-خانه");
  assert.equal(slugifyCategoryName("گوشی موبایل"), "گوشی-موبایل");
});

test("storefront route never appends category id", () => {
  assert.equal(buildStorefrontCategoryRoute("fa", "گوشی-موبایل"), "/fa/category/گوشی-موبایل");
  assert.equal(slugLooksLikeIdSuffixed("گوشی-موبایل"), false);
  assert.equal(slugLooksLikeIdSuffixed("گوشی-موبایل-01a03826"), true);
});

test("duplicate slug error maps to Persian message", () => {
  assert.equal(
    mapCategoryMutationError({ message: "x (catalog.category.slug.duplicate)" }),
    CATEGORY_SLUG_DUPLICATE_MESSAGE,
  );
});

test("mapCategoryTreeNode reads mixed casing", () => {
  const mapped = mapCategoryTreeNode({
    id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    parentId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
    name: "موبایل",
    slug: "mobile",
    status: "Draft",
    sortOrder: 4,
    isVisible: false,
    hasChildren: true,
    productCount: null,
  });
  assert.equal(mapped?.parentId, "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
  assert.equal(mapped?.isVisible, false);
  assert.equal(mapped?.productCount, null);
});

test("mapCategoryWorkspace requires categoryId", () => {
  assert.equal(mapCategoryWorkspace({ status: "Draft" }), null);
});
