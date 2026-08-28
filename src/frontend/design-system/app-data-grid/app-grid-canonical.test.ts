import assert from "node:assert/strict";
import test from "node:test";
import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";
import { DEFAULT_APP_GRID_CAPABILITIES, resolveAppGridCapabilities } from "./app-grid-capabilities.ts";

const dir = dirname(fileURLToPath(import.meta.url));

test("resolveAppGridCapabilities merges overrides with defaults", () => {
  const resolved = resolveAppGridCapabilities({ search: false, csvExport: false });
  assert.equal(resolved.search, false);
  assert.equal(resolved.csvExport, false);
  assert.equal(resolved.savedViews, true);
  assert.equal(resolved.rowSelection, true);
});

test("default capabilities match professional grid baseline", () => {
  assert.deepEqual(DEFAULT_APP_GRID_CAPABILITIES, {
    search: true,
    advancedFilter: true,
    savedViews: true,
    columnManager: true,
    csvExport: true,
    excelExport: true,
    rowSelection: true,
  });
});

test("canonical exports include row actions and filter header helpers", () => {
  const index = readFileSync(join(dir, "index.ts"), "utf8");
  assert.match(index, /AppGridRowActionsCell/);
  assert.match(index, /applyAppGridFilterHeader/);
  assert.match(index, /buildPinnedActionsColumnDef/);
  assert.match(index, /AppGridCapabilities/);
});

test("AppDataGrid exposes gridId and capabilities props", () => {
  const source = readFileSync(join(dir, "AppDataGrid.tsx"), "utf8");
  assert.match(source, /gridId\?:/);
  assert.match(source, /capabilities\?: AppGridCapabilities/);
  assert.match(source, /rowCountNoun\?:/);
  assert.match(source, /messageOverrides\?:/);
  assert.match(source, /data-grid-id=\{gridId\}/);
});

test("product list uses canonical row actions instead of inline action markup", () => {
  const source = readFileSync(join(dir, "..", "..", "app", "admin", "product-list.tsx"), "utf8");
  assert.match(source, /AppGridRowActionsCell/);
  assert.match(source, /buildPinnedActionsColumnDef/);
  assert.match(source, /AppGridMediaCell/);
  assert.match(source, /gridId=\{ADMIN_PRODUCT_GRID_VIEW_KEY\}/);
  assert.doesNotMatch(source, /function ProductActionsCell/);
});
