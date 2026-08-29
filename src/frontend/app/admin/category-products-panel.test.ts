import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");

test("behavior chips use short independent labels without comma-separated UI", () => {
  const panel = fs.readFileSync(path.join(root, "app/admin/category-attributes-panel.tsx"), "utf8");
  assert.match(panel, /required:\s*\{\s*fa:\s*"الزامی"/);
  assert.match(panel, /filterable:\s*\{\s*fa:\s*"فیلتر"/);
  assert.match(panel, /variant:\s*\{\s*fa:\s*"تنوع"/);
  assert.match(panel, /comparable:\s*\{\s*fa:\s*"مقایسه"/);
  assert.match(panel, /data-testid="attr-behavior-chips"/);
  assert.match(panel, /role="switch"/);
  assert.match(panel, /ATTRIBUTE_FLAG_CHIP_LABELS/);
  assert.equal(panel.includes("فیلتر, تنوع"), false);
  assert.equal(panel.includes("فیلتر، تنوع"), false);
  assert.equal(/attr-flag-required[\s\S]{0,120}type="checkbox"/.test(panel), false);
});

test("admin nav groups Categories and Products distinctly", () => {
  const shell = fs.readFileSync(path.join(root, "app/admin/admin-shell.tsx"), "utf8");
  const messages = fs.readFileSync(path.join(root, "app/admin/admin-chrome-messages.ts"), "utf8");
  assert.match(shell, /groupCatalogCategories/);
  assert.match(shell, /groupProducts/);
  assert.match(shell, /labelKey: "productList"/);
  assert.match(messages, /groupCatalogCategories:\s*"کاتالوگ \/ دسته‌بندی‌ها"/);
  assert.match(messages, /groupProducts:\s*"محصولات"/);
  assert.match(messages, /productList:\s*"فهرست محصولات"/);
  assert.equal(messages.includes("کاتالوگ / محصولات"), false);
  assert.equal(messages.includes("Catalog / Products"), false);
});

test("category products panel is real and blocks L1/L2 assignment", () => {
  const panel = fs.readFileSync(path.join(root, "app/admin/category-products-panel.tsx"), "utf8");
  const screen = fs.readFileSync(path.join(root, "app/admin/category-admin-screen.tsx"), "utf8");
  assert.match(
    panel,
    /CATEGORY_PRODUCTS_LEVEL_BLOCKED_MESSAGE_FA/,
  );
  assert.match(panel, /category-products-level-blocked/);
  assert.match(panel, /AppDataGrid/);
  assert.match(panel, /queryAdminProductGrid/);
  assert.match(panel, /isAssignableProductCategory/);
  assert.match(screen, /CategoryProductsPanel/);
  assert.equal(panel.includes("AgGridReact"), false);
});

test("category products assign dialog uses AppDataGrid tabs not checkbox list", () => {
  const panel = fs.readFileSync(path.join(root, "app/admin/category-products-panel.tsx"), "utf8");
  assert.match(panel, /اختصاص محصولات به این دسته/);
  assert.match(panel, /category-assign-tab-all/);
  assert.match(panel, /category-assign-tab-selected/);
  assert.match(panel, /همه محصولات/);
  assert.match(panel, /انتخاب‌شده‌ها/);
  assert.match(panel, /category-products-assign-grid/);
  assert.match(panel, /addAdminProductAdditionalCategory/);
  assert.match(panel, /removeAdminProductAdditionalCategory/);
  assert.match(panel, /cannot_remove_primary/);
  assert.match(panel, /category-products-helper/);
  assert.match(panel, /bulk-add-additional/);
  assert.match(panel, /bulk-remove-additional/);
  assert.match(panel, /rowSelection:\s*true/);
  assert.match(panel, /دسته اصلی/);
  assert.equal(panel.includes("category-products-assign-list"), false);
});
