import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");

test("behavior chips use short independent labels without comma-separated UI", () => {
  const panel = fs.readFileSync(path.join(root, "app/admin/category-attributes-panel.tsx"), "utf8");
  assert.match(panel, /required:\s*"الزامی"/);
  assert.match(panel, /filterable:\s*"فیلتر"/);
  assert.match(panel, /variant:\s*"تنوع"/);
  assert.match(panel, /comparable:\s*"مقایسه"/);
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
    /CATEGORY_PRODUCTS_LEVEL_BLOCKED_MESSAGE_FA\s*=\s*"محصول فقط به دسته‌بندی سطح سوم قابل اختصاص است\."/,
  );
  assert.match(panel, /queryAdminProductGrid/);
  assert.match(panel, /assignAdminProductCategory/);
  assert.match(panel, /confirmSchemaImpact/);
  assert.match(panel, /previewProductCategoryChange/);
  assert.match(panel, /AppDataGrid/);
  assert.match(panel, /category-products-level-blocked/);
  assert.match(panel, /isAssignableProductCategory/);
  assert.match(panel, /ProductCategoryPicker/);
  assert.match(screen, /id: "products", label: "محصولات", implemented: true/);
  assert.match(screen, /CategoryProductsPanel/);
  assert.equal(screen.includes('id: "seo"'), false);
  assert.equal(screen.includes('id: "settings"'), false);
  assert.equal(screen.includes('id: "history"'), false);
  assert.equal(screen.includes("ComingSoonPanel"), false);
  assert.equal(screen.includes("category-tab-coming-soon"), false);
});
