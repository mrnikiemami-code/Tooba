import assert from "node:assert/strict";
import test from "node:test";
import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";

const dir = dirname(fileURLToPath(import.meta.url));

test("theme uses official AG Grid legacy variables for header and borders", () => {
  const css = readFileSync(join(dir, "theme.css"), "utf8");
  assert.match(css, /--ag-header-background-color/);
  assert.match(css, /--app-grid-chrome-bg/);
  assert.match(css, /--ag-subheader-background-color/);
  assert.match(css, /--app-grid-filter-control-bg/);
  assert.match(css, /\.ag-grid-pinned-right-cells/);
  assert.match(css, /--ag-header-column-separator-display/);
  assert.match(css, /--ag-row-border-color/);
  assert.match(css, /--ag-cell-horizontal-border:\s*none/);
  assert.match(css, /--ag-header-column-separator-display:\s*none/);
  assert.match(css, /\[data-app-grid-shell\][\s\S]*\.ag-cell[\s\S]*align-items:\s*center/);
  assert.match(css, /\[data-app-grid-shell\][\s\S]*\.ag-cell-wrapper[\s\S]*justify-content:\s*flex-start/);
  assert.match(css, /\[data-app-grid-shell\]\[dir="rtl"\][\s\S]*\.ag-cell[\s\S]*text-align:\s*right/);
  assert.match(css, /\.ag-header-cell-filter-button[\s\S]*display:\s*none/);
  assert.match(css, /\.ag-theme-tooba \.ag-filter/);
});

test("AppDataGrid uses app-owned header filters and external filter fields", () => {
  const source = readFileSync(join(dir, "AppDataGrid.tsx"), "utf8");
  assert.match(source, /appColumnHeader:\s*AppColumnHeader/);
  assert.match(source, /externalFilterFields/);
  assert.match(source, /onExternalFilterApply/);
  assert.match(source, /tooltipShowMode="whenTruncated"/);
  assert.match(source, /tooltipValueGetter/);
  assert.doesNotMatch(source, /jalaliDateColumnFilter/);
});

test("product list externalizes all matrix filter fields", () => {
  const source = readFileSync(join(dir, "..", "..", "app", "admin", "product-list.tsx"), "utf8");
  assert.match(source, /externalFilterFields=\{ADMIN_PRODUCT_EXTERNAL_FILTER_FIELDS\}/);
  assert.match(source, /applyProductGridFilterHeader\(/);
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
