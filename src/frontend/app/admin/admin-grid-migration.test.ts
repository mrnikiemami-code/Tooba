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
const adapters = fs.readFileSync(path.join(root, "design-system/app-data-grid/legacy-grid-adapters.ts"), "utf8");
const designIndex = fs.readFileSync(path.join(root, "design-system/app-data-grid/index.ts"), "utf8");

test("admin-screens uses direct AppDataGrid not LegacyAppDataGrid", () => {
  assert.match(adminScreens, /AppDataGrid/);
  assert.match(adminScreens, /ServerGridPage/);
  assert.match(adminScreens, /queryAdminOrdersGrid/);
  assert.doesNotMatch(adminScreens, /LegacyAppDataGrid/);
  assert.doesNotMatch(adminScreens, /<DataGrid columns=/);
});

test("catalog attribute definitions and schema use direct AppDataGrid", () => {
  assert.match(catalogAttr, /admin-attribute-definitions-grid/);
  assert.match(catalogAttr, /admin-category-schema-grid/);
  assert.match(catalogAttr, /createClientGridQueryAdapter/);
  assert.doesNotMatch(catalogAttr, /LegacyAppDataGrid/);
});

test("gift cards admin list uses direct AppDataGrid", () => {
  assert.match(walletUi, /admin-gift-cards-grid/);
  assert.match(walletUi, /AppDataGrid/);
  assert.doesNotMatch(walletUi, /LegacyAppDataGrid/);
});

test("stories management uses direct AppDataGrid", () => {
  assert.match(storyScreen, /AppDataGrid/);
  assert.match(storyScreen, /queryAdminStoriesGrid/);
  assert.doesNotMatch(storyScreen, /LegacyAppDataGrid/);
});

test("legacy wrapper removed from design-system exports", () => {
  assert.doesNotMatch(designIndex, /LegacyAppDataGrid/);
  assert.match(designIndex, /createClientGridQueryAdapter/);
  assert.match(designIndex, /buildLegacyGridBridge/);
});

test("pure legacy grid adapters exist without render wrapper component", () => {
  assert.match(adapters, /createClientGridQueryAdapter/);
  assert.match(adapters, /useLegacyAdminGridDirectProps/);
  assert.doesNotMatch(adapters, /LegacyAppDataGrid/);
  assert.match(bridge, /buildLegacyGridBridge/);
});
