import assert from "node:assert/strict";
import test from "node:test";
import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";
import { pinnedGridEdge } from "./grid-direction.ts";

const dir = dirname(fileURLToPath(import.meta.url));

test("theme sets explicit chrome background for header and pinned body cells", () => {
  const css = readFileSync(join(dir, "theme.css"), "utf8");
  assert.match(css, /--app-grid-chrome-bg:\s*rgb\(243,\s*242,\s*242\)/);
  assert.match(css, /--ag-header-background-color:\s*var\(--app-grid-chrome-bg\)/);
  assert.match(css, /--ag-borders-critical:\s*solid 1px/);
  assert.match(css, /--ag-critical-border-color/);
});

test("theme reinforces AG v36 pinned cell containers with opaque surfaces", () => {
  const css = readFileSync(join(dir, "theme.css"), "utf8");
  assert.match(css, /\.ag-grid-pinned-right-cells/);
  assert.match(css, /\.ag-grid-pinned-left-cells/);
  assert.match(css, /\.ag-cell-first-right-pinned/);
  assert.match(css, /\.ag-cell-last-left-pinned/);
  assert.match(css, /--ag-header-background-color/);
  assert.match(css, /--app-grid-chrome-bg/);
  assert.doesNotMatch(css, /\.ag-(?:grid|cell|header|row)[^{]*\{[^}]*transform:/);
  assert.doesNotMatch(css, /\.ag-(?:grid|cell|header|row)[^{]*\{[^}]*z-index:/);
});

test("pinnedGridEdge maps rtl end to left pin", () => {
  assert.equal(pinnedGridEdge("rtl"), "left");
  assert.equal(pinnedGridEdge("ltr"), "right");
});

test("theme applies visible row separators", () => {
  const css = readFileSync(join(dir, "theme.css"), "utf8");
  assert.match(css, /--ag-row-border-color:\s*rgb\(var\(--color-border\)\)/);
  assert.match(css, /\.ag-row:not\(\.ag-header-row\)\s*>\s*\.ag-grid-scrolling-cells/);
});

test("product list keeps actions column pinned to rtl grid end", () => {
  const source = readFileSync(join(dir, "..", "..", "app", "admin", "product-list.tsx"), "utf8");
  assert.match(source, /colId:\s*"actions"/);
  assert.match(source, /pinned:\s*actionsPin/);
  assert.match(source, /pinnedGridEdge\("rtl"\)/);
  assert.match(source, /lockPinned:\s*true/);
  assert.match(source, /lockPosition:\s*actionsPin/);
  assert.match(source, /ProductActionsCell/);
  assert.match(source, /admin-product-view-/);
  assert.match(source, /admin-product-edit-/);
  assert.match(source, /admin-product-delete-/);
  assert.match(source, /scope=view/);
});
