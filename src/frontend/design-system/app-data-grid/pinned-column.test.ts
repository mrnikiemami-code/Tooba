import assert from "node:assert/strict";
import test from "node:test";
import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";

const dir = dirname(fileURLToPath(import.meta.url));

test("theme sets explicit opaque data background for pinned body cells", () => {
  const css = readFileSync(join(dir, "theme.css"), "utf8");
  assert.match(css, /--ag-data-background-color:\s*hsl\(var\(--surface-elevated\)\)/);
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
  assert.match(css, /--ag-data-background-color/);
  assert.doesNotMatch(css, /\.ag-(?:grid|cell|header|row)[^{]*\{[^}]*transform:/);
  assert.doesNotMatch(css, /\.ag-(?:grid|cell|header|row)[^{]*\{[^}]*z-index:/);
});

test("product list keeps actions column pinned right only", () => {
  const source = readFileSync(join(dir, "..", "..", "app", "admin", "product-list.tsx"), "utf8");
  assert.match(source, /colId:\s*"actions"/);
  assert.match(source, /pinned:\s*directionPin\(\)/);
  assert.match(source, /function directionPin\(\): "left" \| "right" \{\s*return "right";/);
});
