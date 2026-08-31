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

test("categorySummaryIncludes matches full taxonomy path leaf", () => {
  const panel = fs.readFileSync(path.join(root, "app/admin/category-products-panel.tsx"), "utf8");
  assert.match(panel, /endsWith\(` > \$\{needle\}`\)/);
  assert.match(panel, /split\(\/\\s\*>\\s\*\/\)/);
});

test("category products panel wires column header filters like product list", () => {
  const panel = fs.readFileSync(path.join(root, "app/admin/category-products-panel.tsx"), "utf8");
  assert.match(panel, /applyCategoryProductsFilterHeader/);
  assert.match(panel, /CATEGORY_PRODUCTS_EXTERNAL_FILTER_FIELDS/);
  assert.match(panel, /externalFilterFields=\{CATEGORY_PRODUCTS_EXTERNAL_FILTER_FIELDS\}/);
  assert.match(panel, /advancedFilterColumns=\{CATEGORY_PRODUCTS_ADVANCED_FILTERS\}/);
  assert.match(panel, /statusFilterOptions=\{\[\.\.\.CATEGORY_PRODUCT_STATUS_FILTER_OPTIONS\]\}/);
  assert.match(panel, /assignmentRole/);
  assert.match(panel, /applyAppGridFilterHeader/);
  assert.match(panel, /advancedFilter:\s*true/);
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
  assert.match(panel, /افزودن محصول برای نمایش در این دسته/);
  assert.match(panel, /category-assign-tab-all/);
  assert.match(panel, /category-assign-tab-selected/);
  assert.match(panel, /همه محصولات/);
  assert.match(panel, /انتخاب‌شده‌ها/);
  assert.match(panel, /category-products-assign-grid/);
  assert.match(panel, /addAdminProductAdditionalCategory/);
  assert.match(panel, /removeAdminProductAdditionalCategory/);
  assert.match(panel, /cannot_remove_primary/);
  assert.match(panel, /category-products-helper/);
  assert.match(panel, /دسته اصلی/);
  assert.equal(panel.includes("assignAdminProductCategory"), false);
  assert.equal(panel.includes("category-products-assign-list"), false);
});

test("category products picker refreshes membership count and row state after mutation", () => {
  const panel = fs.readFileSync(path.join(root, "app/admin/category-products-panel.tsx"), "utf8");
  assert.match(panel, /bumpMembershipState/);
  assert.match(panel, /setAssignReloadToken/);
  assert.match(panel, /setReloadToken/);
  assert.match(panel, /setSelectedCount/);
  assert.match(panel, /afterMembershipAdded/);
  assert.match(panel, /afterMembershipRemoved/);
  assert.match(panel, /toast\.success\("محصول به دسته اضافه شد\."\)/);
  assert.match(panel, /toast\.success\("محصول از این دسته حذف شد\."\)/);
  assert.match(panel, /from "react-toastify"/);
});

test("category products assign dialog has top bulk add selected action", () => {
  const panel = fs.readFileSync(path.join(root, "app/admin/category-products-panel.tsx"), "utf8");
  assert.match(panel, /افزودن موارد انتخاب‌شده برای نمایش/);
  assert.match(panel, /BULK_ADD_FOR_DISPLAY_CTA_FA/);
  assert.match(panel, /category-assign-bulk-add-selected/);
  assert.match(panel, /runBulkAddSelected/);
  assert.match(panel, /selectionCount === 0/);
  assert.match(panel, /در حال افزودن/);
  assert.match(panel, /category-assign-add-/);
  assert.match(panel, /افزودن برای نمایش/);
  assert.equal(panel.includes("bulk-add-additional"), false);
  assert.equal(panel.includes("افزودن گروهی"), false);
});

test("category products panel is membership-only without change-category", () => {
  const panel = fs.readFileSync(path.join(root, "app/admin/category-products-panel.tsx"), "utf8");
  assert.match(panel, /حذف از این دسته/);
  assert.match(panel, /PRIMARY_MEMBERSHIP_HELPER_FA/);
  assert.match(panel, /برای تغییر دسته اصلی، محصول را باز کنید/);
  assert.match(panel, /نمایش در این دسته/);
  assert.match(panel, /DISPLAY_MEMBERSHIP_BADGE_FA/);
  assert.match(panel, /باز کردن محصول/);
  assert.match(panel, /remove-membership/);
  // Primary rows must not render trash/remove — even a disabled fake control is forbidden.
  assert.match(panel, /visible:\s*\(row\)\s*=>\s*row\.primaryCategoryId\s*!==\s*categoryId/);
  assert.equal(panel.includes("toast.info(PRIMARY_MEMBERSHIP_HELPER_FA)"), false);
  assert.match(panel, /CATEGORY_PRODUCTS_LEVEL_BLOCKED_MESSAGE_FA/);
  assert.match(panel, /محصول فقط به دسته‌های سطح ۳ متصل می‌شود/);
  assert.equal(panel.includes("تغییر دسته‌بندی"), false);
  assert.equal(panel.includes("category-assign-change-primary"), false);
  assert.equal(panel.includes("category-products-change-dialog"), false);
  assert.equal(panel.includes("previewProductCategoryChange"), false);
  assert.equal(panel.includes("ProductCategoryPicker"), false);
  assert.equal(panel.includes("onChangeCategory"), false);
  assert.equal(panel.includes('"change-category"'), false);
  assert.equal(panel.includes(">اضافی<"), false);
  assert.equal(panel.includes('"اضافی"'), false);
  assert.equal(panel.includes("نمایش دیگر"), false);
  assert.equal(panel.includes("اختصاص محصولات"), false);
});

