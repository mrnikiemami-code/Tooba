import assert from "node:assert/strict";
import test from "node:test";
import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";

const dir = dirname(fileURLToPath(import.meta.url));

test("theme uses official AG Grid legacy variables for header and borders", () => {
  const css = readFileSync(join(dir, "theme.css"), "utf8");
  assert.match(css, /--ag-header-background-color/);
  assert.match(css, /--ag-data-background-color/);
  assert.match(css, /\.ag-grid-pinned-right-cells/);
  assert.match(css, /--ag-header-column-separator-display/);
  assert.match(css, /--ag-row-border-color/);
  assert.match(css, /--ag-cell-horizontal-border/);
  assert.match(css, /\.ag-header-row\.ag-header-row-column/);
  assert.match(css, /\.ag-theme-tooba \.ag-filter/);
  assert.doesNotMatch(css, /\.ag-cell-wrapper/);
});

test("AppDataGrid uses app-owned header filters and external filter fields", () => {
  const source = readFileSync(join(dir, "AppDataGrid.tsx"), "utf8");
  assert.match(source, /appColumnHeader:\s*AppColumnHeader/);
  assert.match(source, /externalFilterFields/);
  assert.match(source, /onExternalFilterApply/);
  assert.doesNotMatch(source, /jalaliDateColumnFilter/);
});

test("product list externalizes title and updatedAt header filters", () => {
  const source = readFileSync(join(dir, "..", "..", "app", "admin", "product-list.tsx"), "utf8");
  assert.match(source, /externalFilterFields=\{\["title",\s*"updatedAt"\]\}/);
  assert.match(source, /externalFilter:\s*"jalali-date"/);
  assert.match(source, /externalFilter:\s*"text"/);
  assert.doesNotMatch(source, /jalaliDateColumnFilter/);
});

test("Jalali header filter uses portal popover outside AG root", () => {
  const header = readFileSync(join(dir, "app-column-header.tsx"), "utf8");
  const popover = readFileSync(join(dir, "column-filter-popover.tsx"), "utf8");
  const panel = readFileSync(join(dir, "jalali-header-filter-panel.tsx"), "utf8");
  assert.match(header, /ColumnFilterPopover/);
  assert.match(popover, /Portal/);
  assert.match(popover, /data-app-filter-panel/);
  assert.match(panel, /data-testid="jalali-header-filter-panel"/);
});

test("text header filter preserves Enter-to-apply draft semantics", () => {
  const source = readFileSync(join(dir, "text-header-filter-panel.tsx"), "utf8");
  assert.match(source, /event\.key === "Enter"/);
  assert.match(source, /onApply/);
});

test("Advanced filter drawer rebuilt at 520px with portal", () => {
  const drawer = readFileSync(join(dir, "AdvancedFilterDrawer.tsx"), "utf8");
  const builder = readFileSync(join(dir, "AdvancedFilterBuilder.tsx"), "utf8");
  assert.match(drawer, /520px/);
  assert.match(drawer, /Portal/);
  assert.match(drawer, /sticky bottom-0/);
  assert.match(builder, /data-advanced-filter-card/);
  assert.match(builder, /data-advanced-filter-connector/);
});

test("globals define explicit overlay layering tokens", () => {
  const css = readFileSync(join(dir, "..", "..", "app", "globals.css"), "utf8");
  assert.match(css, /--z-popover:\s*60/);
  assert.match(css, /--z-drawer-backdrop:\s*80/);
  assert.match(css, /--z-drawer:\s*90/);
});
