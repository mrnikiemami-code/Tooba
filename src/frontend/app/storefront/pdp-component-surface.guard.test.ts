import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const pdp = fs.readFileSync(path.join(root, "app/storefront/storefront-pdp.tsx"), "utf8");
const card = fs.readFileSync(path.join(root, "app/storefront/storefront-product-card.tsx"), "utf8");

test("PDP major regions use section context and inherit-by-default", () => {
  assert.match(pdp, /data-testid="pdp-primary-card"/);
  assert.match(pdp, /data-storefront-surface-role="inherit"/);
  assert.match(pdp, /data-testid="pdp-product-info"/);
  assert.match(pdp, /data-testid="pdp-buy-column"/);
  assert.match(pdp, /data-storefront-surface-role="media"/);
  assert.match(pdp, /data-testid="pdp-gallery"/);
  assert.match(pdp, /data-testid="pdp-tabs-card"/);
  assert.match(pdp, /data-storefront-surface-role="alternate"/);
  assert.match(pdp, /data-testid="pdp-related"/);
  assert.doesNotMatch(pdp, /data-testid="pdp-primary-card"[^>]*data-storefront-surface-role="card"/);
  assert.doesNotMatch(pdp, /\bbg-white\b/);
  assert.doesNotMatch(pdp, /loadStorefrontAppearance/);
  assert.match(card, /data-storefront-surface-role="card"/);
  assert.match(card, /data-storefront-surface-role="media"/);
});
