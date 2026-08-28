import assert from "node:assert/strict";
import test from "node:test";
import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";

const dir = dirname(fileURLToPath(import.meta.url));

test("Jalali column filter panel has professional minimum width", () => {
  const source = readFileSync(join(dir, "jalali-date-column-filter.tsx"), "utf8");
  assert.match(source, /min-w-\[min\(22\.5rem/);
  assert.match(source, /panelMinWidth=\{340\}/);
  assert.match(source, /data-app-filter-panel/);
});

test("Jalali date picker renders calendar through Portal", () => {
  const source = readFileSync(join(dir, "jalali-date-picker.tsx"), "utf8");
  assert.match(source, /Portal/);
  assert.match(source, /fixed z-\[var\(--z-popover\)\]/);
  assert.match(source, /data-jalali-picker-panel/);
});

test("Advanced filter drawer matches approved layout structure", () => {
  const drawer = readFileSync(join(dir, "AdvancedFilterDrawer.tsx"), "utf8");
  const builder = readFileSync(join(dir, "AdvancedFilterBuilder.tsx"), "utf8");
  assert.match(drawer, /z-\[var\(--z-drawer\)\]/);
  assert.match(drawer, /data-advanced-filter-panel/);
  assert.match(builder, /data-advanced-filter-card/);
  assert.match(builder, /data-advanced-filter-connector/);
  assert.match(builder, /md:grid-cols-3/);
});

test("globals define explicit overlay layering tokens", () => {
  const css = readFileSync(join(dir, "..", "..", "app", "globals.css"), "utf8");
  assert.match(css, /--z-popover:\s*60/);
  assert.match(css, /--z-drawer-backdrop:\s*80/);
  assert.match(css, /--z-drawer:\s*90/);
});
