import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const pdp = fs.readFileSync(path.join(root, "app/storefront/storefront-pdp.tsx"), "utf8");
const card = fs.readFileSync(path.join(root, "app/storefront/storefront-product-card.tsx"), "utf8");

test("PDP major regions use semantic/derived surfaces", () => {
  assert.match(pdp, /data-testid="pdp-primary-card"/);
  assert.match(pdp, /data-storefront-surface-role="card"/);
  assert.match(pdp, /data-storefront-surface-role="media"/);
  assert.match(pdp, /data-testid="pdp-gallery"/);
  assert.match(pdp, /data-testid="pdp-tabs-card"/);
  assert.match(pdp, /data-testid="pdp-related"/);
  assert.match(pdp, /data-storefront-surface-role="alternate"/);
  assert.doesNotMatch(pdp, /\bbg-white\b/);
  assert.doesNotMatch(pdp, /loadStorefrontAppearance/);
  assert.match(card, /data-storefront-surface-role="card"/);
  assert.match(card, /data-storefront-surface-role="media"/);
});
