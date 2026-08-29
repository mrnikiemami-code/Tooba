import assert from "node:assert/strict";
import test from "node:test";
import {
  getCategoryLevel,
  isAssignableProductCategory,
  listCategoryChildren,
  PRODUCT_ASSIGNABLE_CATEGORY_LEVEL,
  PRODUCT_CATEGORY_LEVEL_REQUIRED_MESSAGE_FA,
} from "./product-category-level.ts";

const nodes = [
  { id: "l1", parentId: null, sortOrder: 0 },
  { id: "l2", parentId: "l1", sortOrder: 0 },
  { id: "l3a", parentId: "l2", sortOrder: 0 },
  { id: "l3b", parentId: "l2", sortOrder: 1 },
  { id: "other", parentId: null, sortOrder: 1 },
];

test("category level is one plus ancestor count", () => {
  assert.equal(getCategoryLevel(nodes, "l1"), 1);
  assert.equal(getCategoryLevel(nodes, "l2"), 2);
  assert.equal(getCategoryLevel(nodes, "l3a"), 3);
  assert.equal(getCategoryLevel(nodes, "missing"), null);
  assert.equal(PRODUCT_ASSIGNABLE_CATEGORY_LEVEL, 3);
});

test("only level 3 is assignable", () => {
  assert.equal(isAssignableProductCategory(nodes, "l1"), false);
  assert.equal(isAssignableProductCategory(nodes, "l2"), false);
  assert.equal(isAssignableProductCategory(nodes, "l3a"), true);
  assert.equal(isAssignableProductCategory(nodes, null), false);
  assert.match(PRODUCT_CATEGORY_LEVEL_REQUIRED_MESSAGE_FA, /سطح سوم/);
});

test("listCategoryChildren returns sorted siblings", () => {
  const kids = listCategoryChildren(nodes, "l2");
  assert.deepEqual(
    kids.map((k) => k.id),
    ["l3a", "l3b"],
  );
});
