import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import { buildStorefrontCategoryRoute } from "./catalog-category-api.ts";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const panelPath = path.join(root, "app/admin/category-mega-menu-panel.tsx");
const screenPath = path.join(root, "app/admin/category-admin-screen.tsx");
const headerPath = path.join(root, "app/storefront/storefront-header.tsx");

test("canonical category route has no visible ID suffix", () => {
  const route = buildStorefrontCategoryRoute("fa", "گوشی-موبایل");
  assert.equal(route, "/fa/category/گوشی-موبایل");
  assert.equal(/-[0-9a-f]{8}$/i.test(route), false);
});

test("Mega Menu tab: VIEW/EDIT, enable, placement, route preview, labels", () => {
  const panel = fs.readFileSync(panelPath, "utf8");
  const screen = fs.readFileSync(screenPath, "utf8");
  assert.match(screen, /CategoryMegaMenuPanel/);
  assert.match(screen, /activeTab === "mega-menu"/);
  assert.match(screen, /handleEnterMegaMenuEdit/);
  assert.match(screen, /id: "mega-menu", label: "مگامنو", implemented: true/);
  assert.match(panel, /category-mega-menu-panel/);
  assert.match(panel, /نمایش در مگامنو/);
  assert.equal(panel.includes("mega-menu-enter-edit"), false);
  assert.equal(panel.includes("onEnterEdit"), false);
  assert.match(panel, /mega-menu-placement/);
  assert.match(panel, /AdminSearchableCombobox/);
  assert.match(panel, /جستجو بر اساس نام یا مسیر/);
  assert.doesNotMatch(panel, /<select[\s\S]*mega-menu-placement/);
  assert.match(panel, /عنوان متفاوت در مگامنو/);
  assert.match(panel, /destinationPreview|DestinationPreview|مسیر ویترین/);
  assert.match(panel, /canEdit/);
  assert.equal(panel.match(/data-testid=.*categoryId/g)?.length ?? 0, 0);
});

test("storefront header consumes mega menu API with canonical destinations", () => {
  const header = fs.readFileSync(headerPath, "utf8");
  assert.match(header, /loadStorefrontMegaMenu/);
  assert.match(header, /parentMegaMenuItemId/);
  assert.match(header, /item\.destination/);
});

test("no raw AgGridReact in mega menu panel", () => {
  const panel = fs.readFileSync(panelPath, "utf8");
  assert.equal(panel.includes("AgGridReact"), false);
  assert.equal(panel.includes("AppDataGrid"), false);
});
