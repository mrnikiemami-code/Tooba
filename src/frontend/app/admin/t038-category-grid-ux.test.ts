import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");

test("additional chips helper keeps +N remainder from payload only", () => {
  const chips = fs.readFileSync(path.join(root, "app/admin/additional-category-chips-cell.tsx"), "utf8");
  assert.match(chips, /ADDITIONAL_CATEGORY_INLINE_LIMIT\s*=\s*3/);
  assert.match(chips, /remainingAdditionalCategoryNames/);
  assert.match(chips, /Popover/);
  assert.match(chips, /additional-category-more/);
  assert.equal(chips.includes("fetch("), false);
});

test("product list uses separate primary and additional category columns", () => {
  const panel = fs.readFileSync(path.join(root, "app/admin/product-list.tsx"), "utf8");
  assert.match(panel, /headerName:\s*"دسته اصلی"/);
  assert.match(panel, /headerName:\s*"نمایش در دسته‌های دیگر"/);
  assert.match(panel, /AdditionalCategoryChipsCell/);
  assert.match(panel, /primaryCategoryName/);
  assert.match(panel, /additionalCategoryNames/);
  assert.equal(panel.includes('headerName: "دسته"'), false);
});

test("category products panel shows leaf primary and comma list for additional categories", () => {
  const panel = fs.readFileSync(path.join(root, "app/admin/category-products-panel.tsx"), "utf8");
  assert.match(panel, /headerName:\s*"دسته اصلی"/);
  assert.match(panel, /headerName:\s*"نمایش در دسته‌های دیگر"/);
  assert.match(panel, /AdditionalCategoryListCell/);
  assert.match(panel, /rowAssignedToCategory/);
});

test("category tree selection follows route and scrolls selected node", () => {
  const screen = fs.readFileSync(path.join(root, "app/admin/category-admin-screen.tsx"), "utf8");
  const tree = fs.readFileSync(path.join(root, "design-system/app-category-tree/AppCategoryTree.tsx"), "utf8");
  assert.match(screen, /flatNodes\.some\(\(n\) => n\.id === categoryId\)/);
  assert.match(screen, /collectAncestorIds/);
  assert.match(tree, /scrollIntoView/);
  assert.match(tree, /ant-tree-treenode-selected/);
});
