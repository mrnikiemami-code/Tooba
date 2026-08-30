import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const listPath = path.join(root, "app/admin/product-list.tsx");
const workspacePath = path.join(root, "app/admin/product-workspace-screen.tsx");
const pickerPath = path.join(root, "app/admin/product-category-picker.tsx");
const pagePath = path.join(root, "app/admin/products/[productId]/page.tsx");
const hostPath = path.join(root, "app/admin/host-client.ts");
const modelPath = path.join(root, "app/admin/workspace-model.ts");

test("product list keeps AppDataGrid and rejects raw AgGridReact", () => {
  const list = fs.readFileSync(listPath, "utf8");
  assert.match(list, /from ["'].*design-system["']/);
  assert.match(list, /AppDataGrid/);
  assert.equal(list.includes("AgGridReact"), false);
  assert.equal(list.includes('from "ag-grid-react"'), false);
});

test("create CTA navigates to dedicated draft create route", () => {
  const list = fs.readFileSync(listPath, "utf8");
  assert.match(list, /href="\/admin\/products\/new"/);
  assert.match(list, /admin-create-product/);
  assert.equal(list.includes("admin-create-product-panel"), false);
  assert.equal(list.includes("ایجاد و انتشار"), false);
  assert.equal(list.includes("createAdminProduct"), false);
});

test("row actions use view/edit scopes and safe archive label", () => {
  const list = fs.readFileSync(listPath, "utf8");
  assert.match(list, /scope=view/);
  assert.match(list, /scope=edit/);
  assert.match(list, /admin-product-view-/);
  assert.match(list, /admin-product-edit-/);
  assert.match(list, /admin-product-delete-/);
  assert.match(list, /بایگانی \/ حذف امن/);
  assert.equal(list.includes("PRD-"), false);
  assert.equal(list.includes("productCode"), false);
});

test("variant terminology uses تنوع not گونه in product list", () => {
  const list = fs.readFileSync(listPath, "utf8");
  assert.match(list, /headerName:\s*"تنوع"/);
  assert.equal(list.includes('headerName: "گونه"'), false);
});

test("product category picker is hierarchical level-3-only with human paths", () => {
  const picker = fs.readFileSync(pickerPath, "utf8");
  assert.match(picker, /data-testid="product-category-picker"/);
  assert.match(picker, /loadCategoryTree/);
  assert.match(picker, /buildCategoryPath/);
  assert.match(picker, /جستجوی نام یا مسیر/);
  assert.match(picker, /data-assignable="true"/);
  assert.match(picker, /data-assignable="false"/);
  assert.match(picker, /product-category-tree/);
  assert.match(picker, /product-category-search-results/);
  assert.match(picker, /isAssignableProductCategory/);
  assert.match(picker, / > /);
  assert.equal(picker.includes("option value={opt.id"), false);
  assert.equal(picker.includes("AgGridReact"), false);
});

test("dedicated create screen blocks invalid category via level error mapping", () => {
  const createPath = path.join(root, "app/admin/product-create-screen.tsx");
  const create = fs.readFileSync(createPath, "utf8");
  assert.match(create, /ProductCategoryPicker/);
  assert.match(create, /workspace\.product\.category\.level\.invalid/);
  assert.match(create, /PRODUCT_CATEGORY_LEVEL_REQUIRED_MESSAGE_FA/);
  assert.match(create, /ایجاد پیش‌نویس/);
  assert.match(create, /ProductRichTextEditor/);
  assert.equal(create.includes("AgGridReact"), false);
});

test("create and workspace Brand use searchable combobox with بدون برند", () => {
  const createPath = path.join(root, "app/admin/product-create-screen.tsx");
  const create = fs.readFileSync(createPath, "utf8");
  const screen = fs.readFileSync(workspacePath, "utf8");
  assert.match(create, /AdminSearchableCombobox/);
  assert.match(create, /admin-product-create-brand/);
  assert.match(create, /بدون برند/);
  assert.doesNotMatch(create, /<select[\s\S]*admin-product-create-brand/);
  assert.match(screen, /AdminSearchableCombobox/);
  assert.match(screen, /بدون برند/);
  assert.match(screen, /نمایش در دسته‌های دیگر/);
  assert.match(screen, /CatalogTagsCard/);
});

test("workspace warns on non-L3 category and preserves VIEW/EDIT", () => {
  const screen = fs.readFileSync(workspacePath, "utf8");
  assert.match(screen, /product-category-level-warning/);
  assert.match(screen, /PRODUCT_CATEGORY_LEVEL_REQUIRED_MESSAGE_FA/);
  assert.match(screen, /invalidSelectionHint/);
  assert.match(screen, /isPrimaryCategoryAssignable/);
  assert.match(screen, /useAdminFormMode/);
  assert.match(screen, /product-general-summary/);
  assert.match(screen, /product-general-edit/);
  assert.equal(screen.includes("AgGridReact"), false);
});

test("workspace tabs and form mode foundation", () => {
  const screen = fs.readFileSync(workspacePath, "utf8");
  assert.match(screen, /label:\s*"عمومی"/);
  assert.match(screen, /label:\s*"ترجمه‌ها"/);
  assert.match(screen, /label:\s*"ویژگی‌ها"/);
  assert.match(screen, /label:\s*"تنوع‌ها"/);
  assert.match(screen, /label:\s*"رسانه"/);
  assert.match(screen, /label:\s*"SEO"/);
  assert.match(screen, /label:\s*"انتشار"/);
  assert.match(screen, /label:\s*"تاریخچه"/);
  assert.match(screen, /useAdminFormMode/);
  assert.match(screen, /product-general-summary/);
  assert.match(screen, /product-general-edit/);
  assert.match(screen, /product-translations-panel/);
  assert.equal(screen.includes("NameFa"), false);
  assert.equal(screen.includes("NameEn"), false);
  assert.equal(screen.includes("AgGridReact"), false);
  assert.equal(screen.includes('label: "گونه‌ها"'), false);
  assert.equal(screen.includes('id: "commercial"'), false);
  assert.equal(screen.includes('id: "inventory"'), false);
});

test("attributes tab requires categoryId and wires schema-driven panel", () => {
  const screen = fs.readFileSync(workspacePath, "utf8");
  assert.match(screen, /product-attributes-category-required/);
  assert.match(screen, /primaryCategoryId/);
  assert.match(screen, /ProductAttributesPanel/);
  assert.match(screen, /ProductVariantsPanel/);
  assert.match(screen, /product-attributes-panel/);
  assert.match(screen, /previewProductCategoryChange/);
  assert.match(screen, /canEdit=\{canMutateCatalog\}/);
  assert.match(screen, /mode=\{formMode\.mode === "edit" \? "edit" : "view"\}/);
  assert.equal(screen.includes('from "./catalog-attribute-ui"'), false);
});

test("workspace page passes view and edit scope", () => {
  const page = fs.readFileSync(pagePath, "utf8");
  assert.match(page, /scope === "view"/);
  assert.match(page, /scope === "edit"/);
  assert.match(page, /initialEdit/);
});

test("host-client maps core/category helpers and new workspace fields", () => {
  const host = fs.readFileSync(hostPath, "utf8");
  assert.match(host, /updateAdminProductCore/);
  assert.match(host, /assignAdminProductCategory/);
  assert.match(host, /primaryCategoryId/);
  assert.match(host, /categoryPath/);
  assert.match(host, /shortDescription/);
  assert.match(host, /translations/);
  const model = fs.readFileSync(modelPath, "utf8");
  assert.match(model, /ProductTranslationView/);
  assert.match(model, /primaryCategoryId\?/);
});
