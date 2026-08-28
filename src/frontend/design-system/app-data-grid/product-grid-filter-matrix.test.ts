import assert from "node:assert/strict";
import test from "node:test";
import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";
import {
  ADMIN_PRODUCT_GRID_FILTER_MATRIX,
  ADMIN_PRODUCT_EXTERNAL_FILTER_FIELDS,
  applyProductGridFilterHeader,
} from "../../app/admin/product-grid-filter-matrix.ts";

const dir = dirname(fileURLToPath(import.meta.url));

test("filter matrix marks business columns filterable", () => {
  for (const field of [
    "title",
    "status",
    "variantCount",
    "offerCount",
    "categorySummary",
    "offerAmountRange",
    "sellableUnits",
    "updatedAt",
  ]) {
    assert.notEqual(ADMIN_PRODUCT_GRID_FILTER_MATRIX[field]?.kind, "none", field);
  }
});

test("filter matrix marks non-filterable columns", () => {
  assert.equal(ADMIN_PRODUCT_GRID_FILTER_MATRIX.actions.kind, "none");
  assert.equal(ADMIN_PRODUCT_GRID_FILTER_MATRIX.media.kind, "none");
});

test("offerAmountRange uses number filter kind", () => {
  assert.equal(ADMIN_PRODUCT_GRID_FILTER_MATRIX.offerAmountRange.kind, "number");
});

test("applyProductGridFilterHeader wires app column header for filterable fields", () => {
  const col = applyProductGridFilterHeader({ field: "offerAmountRange", headerName: "قیمت" });
  assert.equal(col.filter, false);
  assert.equal(col.headerComponent, "appColumnHeader");
  assert.equal(col.headerComponentParams?.externalFilter, "number");
});

test("product list uses unified external filter fields", () => {
  const source = readFileSync(join(dir, "..", "..", "app", "admin", "product-list.tsx"), "utf8");
  assert.match(source, /externalFilterFields=\{ADMIN_PRODUCT_EXTERNAL_FILTER_FIELDS\}/);
  assert.match(source, /applyProductGridFilterHeader\(/);
  assert.doesNotMatch(source, /filter:\s*"agNumberColumnFilter"/);
  assert.doesNotMatch(source, /filter:\s*"agTextColumnFilter"/);
});

test("external filter field list includes price column", () => {
  assert.ok(ADMIN_PRODUCT_EXTERNAL_FILTER_FIELDS.includes("offerAmountRange"));
});
