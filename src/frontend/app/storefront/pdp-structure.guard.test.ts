import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const pdpSource = fs.readFileSync(path.join(root, "app/storefront/storefront-pdp.tsx"), "utf8");
const qaSource = fs.readFileSync(path.join(root, "app/storefront/storefront-pdp-qa.tsx"), "utf8");
const reviewsSource = fs.readFileSync(path.join(root, "app/storefront/storefront-pdp-reviews.tsx"), "utf8");
const bulkSource = fs.readFileSync(path.join(root, "app/storefront/storefront-pdp-bulk.tsx"), "utf8");

const TAB_IDS = ["intro", "full", "specs", "reviews", "qa", "bulk"] as const;

test("pdp guard keeps sticky structurally distinct tabs", () => {
  assert.ok(pdpSource.includes('data-testid="storefront-pdp"'));
  assert.ok(pdpSource.includes('data-testid="pdp-sticky-tabs"'));
  assert.ok(pdpSource.includes('data-testid="pdp-tabs-card"'));
  assert.match(pdpSource, /sticky top-0/);
  assert.ok(pdpSource.includes("pdp-tab-${item.id}") || pdpSource.includes("`pdp-tab-${item.id}`"));
  for (const id of TAB_IDS) {
    assert.ok(pdpSource.includes(`id: "${id}"`), `missing tab id ${id}`);
  }
});

test("pdp guard keeps distinct tab bodies (no generic flatten)", () => {
  assert.ok(pdpSource.includes('data-testid="pdp-intro"'));
  assert.ok(pdpSource.includes('data-testid="pdp-full"'));
  assert.ok(pdpSource.includes('data-testid="pdp-specs"'));
  assert.ok(reviewsSource.includes('data-testid="pdp-reviews"'));
  assert.ok(qaSource.includes('data-testid="pdp-qa"'));
  assert.ok(bulkSource.includes('data-testid="pdp-bulk"'));
  assert.doesNotMatch(pdpSource, /TabContent/);
  assert.doesNotMatch(pdpSource, /generic-tab-panel/);
});

test("pdp guard keeps other sellers and related product seams", () => {
  assert.ok(pdpSource.includes('data-testid="pdp-other-sellers"'));
  assert.ok(pdpSource.includes('data-testid="pdp-related"'));
  assert.ok(pdpSource.includes("otherSellers"));
  assert.ok(pdpSource.includes("relatedProducts"));
});

test("pdp guard forbids Product.Price / Product.Stock authority markers", () => {
  assert.doesNotMatch(pdpSource, /Product\.Price/);
  assert.doesNotMatch(pdpSource, /Product\.Stock/);
  assert.ok(pdpSource.includes("formatOfferAmount") || pdpSource.includes("offerAmount") || pdpSource.includes("amountExclusiveOfTax"));
});
