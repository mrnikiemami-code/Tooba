import assert from "node:assert/strict";
import test from "node:test";
import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";

const dir = dirname(fileURLToPath(import.meta.url));

test("selection column resolves to localized label", () => {
  const labels = readFileSync(join(dir, "column-labels.ts"), "utf8");
  assert.match(labels, /ag-Grid-SelectionColumn/);
  assert.match(labels, /انتخاب/);
  assert.match(labels, /Selection/);
});

test("column manager uses drag handle and no arrow reorder controls", () => {
  const drawer = readFileSync(join(dir, "ColumnManagerDrawer.tsx"), "utf8");
  const grid = readFileSync(join(dir, "AppDataGrid.tsx"), "utf8");
  assert.match(drawer, /data-column-drag-handle/);
  assert.doesNotMatch(drawer, /↑/);
  assert.doesNotMatch(drawer, /↓/);
  assert.doesNotMatch(grid, /moveColumnUp/);
  assert.match(grid, /selectionColumnDef/);
  assert.match(grid, /refreshColumnManagerState/);
  assert.match(grid, /onColumnVisible/);
});

test("page size options include 1000", () => {
  const source = readFileSync(join(dir, "AppDataGrid.tsx"), "utf8");
  assert.match(source, /PAGE_SIZES = \[10, 25, 50, 100, 1000\]/);
});
