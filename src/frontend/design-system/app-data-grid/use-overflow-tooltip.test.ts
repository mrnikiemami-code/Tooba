import assert from "node:assert/strict";
import test from "node:test";
import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";
import { gridTooltipText } from "./use-overflow-tooltip.ts";

const dir = dirname(fileURLToPath(import.meta.url));

test("gridTooltipText returns undefined for empty values", () => {
  assert.equal(gridTooltipText(null), undefined);
  assert.equal(gridTooltipText(undefined), undefined);
  assert.equal(gridTooltipText("   "), undefined);
});

test("gridTooltipText prefers formatted value", () => {
  assert.equal(gridTooltipText("raw", "Formatted"), "Formatted");
  assert.equal(gridTooltipText(42, "۴۲"), "۴۲");
});

test("product list custom cells register overflow tooltip hook", () => {
  const source = readFileSync(join(dir, "..", "..", "app", "admin", "product-list.tsx"), "utf8");
  assert.match(source, /useOverflowTooltip/);
  assert.match(source, /data-overflow-measure/);
});
