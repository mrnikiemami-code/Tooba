import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const listingSource = fs.readFileSync(path.join(root, "app/storefront/storefront-listing.tsx"), "utf8");
const cardSource = fs.readFileSync(path.join(root, "app/storefront/storefront-product-card.tsx"), "utf8");

test("listing guard keeps Shopeiva PLP structure markers", () => {
  for (const marker of [
    'data-testid="storefront-listing"',
    'data-testid="listing-filter-sidebar"',
    'data-testid="listing-sort-toolbar"',
    'data-testid="listing-product-grid"',
    'data-testid="listing-pagination"',
    'data-testid="listing-mobile-filter-open"',
    'data-testid="listing-mobile-filter-drawer"',
    'data-testid="listing-result-count"',
    'data-testid="listing-empty"',
  ]) {
    assert.ok(listingSource.includes(marker), `missing ${marker}`);
  }
});

test("listing guard keeps truthful sort options only", () => {
  assert.ok(listingSource.includes('value="default"'));
  assert.ok(listingSource.includes('value="newest"'));
  assert.ok(listingSource.includes('value="price-asc"'));
  assert.ok(listingSource.includes('value="price-desc"'));
  assert.doesNotMatch(listingSource, /value="rating"/);
  assert.doesNotMatch(listingSource, /value="popular"/);
});

test("listing product grid keeps Shopeiva density classes", () => {
  assert.match(listingSource, /grid-cols-2 sm:grid-cols-2 lg:grid-cols-4/);
});

test("product card keeps offer-based pricing and real rating gate", () => {
  assert.ok(cardSource.includes("offerAmountExclusiveOfTax"));
  assert.ok(cardSource.includes("reviewCount > 0"));
  assert.doesNotMatch(cardSource, /Product\.Price/);
  assert.doesNotMatch(cardSource, /Product\.Stock/);
});
