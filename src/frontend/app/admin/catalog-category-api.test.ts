import assert from "node:assert/strict";
import test from "node:test";
import {
  mapCategoryTreeNode,
  mapCategoryWorkspace,
  slugifyCategoryName,
} from "./catalog-category-api.ts";

test("slugifyCategoryName mirrors kebab + unicode keep", () => {
  assert.equal(slugifyCategoryName("Hello World"), "hello-world");
  assert.equal(slugifyCategoryName("  کتاب  خانه  "), "کتاب-خانه");
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
