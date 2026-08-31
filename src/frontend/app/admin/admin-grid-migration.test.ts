import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const adminScreens = fs.readFileSync(path.join(root, "app/admin/admin-screens.tsx"), "utf8");
const catalogAttr = fs.readFileSync(path.join(root, "app/admin/catalog-attribute-ui.tsx"), "utf8");
const walletUi = fs.readFileSync(path.join(root, "app/wallet/wallet-ui.tsx"), "utf8");
const storyScreen = fs.readFileSync(path.join(root, "app/stories/management/StoryManagementScreen.tsx"), "utf8");
const bridge = fs.readFileSync(path.join(root, "design-system/app-data-grid/legacy-grid-bridge.ts"), "utf8");

test("admin-screens GridPage uses LegacyAppDataGrid not legacy DataGrid", () => {
  assert.match(adminScreens, /LegacyAppDataGrid/);
  assert.doesNotMatch(adminScreens, /<DataGrid columns=/);
});

test("catalog attribute definitions and schema use AppDataGrid", () => {
  assert.match(catalogAttr, /admin-attribute-definitions-grid/);
  assert.match(catalogAttr, /admin-category-schema-grid/);
  assert.doesNotMatch(catalogAttr, /<table className="w-full min-w-\[640px\]/);
});

test("gift cards admin list uses AppDataGrid", () => {
  assert.match(walletUi, /admin-gift-cards-grid/);
  assert.doesNotMatch(walletUi, /admin-gift-cards-table/);
});

test("stories management uses LegacyAppDataGrid", () => {
  assert.match(storyScreen, /LegacyAppDataGrid/);
  assert.doesNotMatch(storyScreen, /<DataGrid columns=/);
});

test("central legacy bridge exists for GridColumnDef migration", () => {
  assert.match(bridge, /buildLegacyGridBridge/);
  assert.match(bridge, /applyAppGridFilterHeader/);
});
