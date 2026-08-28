import assert from "node:assert/strict";
import test from "node:test";
import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";

const dir = dirname(fileURLToPath(import.meta.url));

test("clearAllFilters resets every filter partition in one load call", () => {
  const source = readFileSync(join(dir, "AppDataGrid.tsx"), "utf8");
  assert.match(source, /function clearAllFilters/);
  assert.match(source, /filters:\s*\{\}/);
  assert.match(source, /search:\s*undefined/);
  assert.match(source, /advancedFilter:/);
  assert.match(source, /conditions:\s*\[\]/);
  assert.match(source, /setFilterModel\(null\)/);
  assert.match(source, /data-testid="app-grid-clear-all-filters"/);
});

test("toolbar clear-all is visible when filtering is active", () => {
  const source = readFileSync(join(dir, "AppDataGrid.tsx"), "utf8");
  assert.match(source, /hasActiveFiltering \?/);
  assert.match(source, /clearAllFilters/);
  assert.match(source, /messages\.clearAllFilters/);
});
