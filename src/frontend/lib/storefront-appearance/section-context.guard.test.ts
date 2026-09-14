import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const read = (rel: string) => fs.readFileSync(path.join(root, rel), "utf8");

test("inherit-by-default: structural PDP/Cart wrappers are not giant cards", () => {
  const pdp = read("app/storefront/storefront-pdp.tsx");
  const cart = read("app/storefront/storefront-cart.tsx");
  const primitive = read("app/storefront/storefront-surface.tsx");
  assert.match(pdp, /data-storefront-surface-role="inherit"/);
  assert.doesNotMatch(pdp, /data-testid="pdp-primary-card"[^>]*data-storefront-surface-role="card"/);
  assert.match(pdp, /data-testid="pdp-tabs-card"[^>]*data-storefront-surface-role="alternate"/);
  assert.match(cart, /data-testid="cart-items"[^>]*data-storefront-surface-role="inherit"/);
  assert.match(cart, /data-testid="cart-hero"[^>]*data-storefront-surface-role="accent"/);
  assert.match(cart, /data-testid="cart-benefits"[^>]*data-storefront-surface-role="alternate"/);
  assert.match(primitive, /surface = "inherit"/);
  assert.doesNotMatch(primitive, /backgroundColor/);
});

test("card-area/giant-cardification static gate", () => {
  const pdp = read("app/storefront/storefront-pdp.tsx");
  const dash = read("app/customer-panel/page.tsx");
  assert.doesNotMatch(pdp, /bg-surface rounded-2xl border border-gray-200 shadow-sm" data-storefront-surface-role="card" data-testid="pdp-primary-card"/);
  assert.doesNotMatch(dash, /to-white/);
  assert.match(dash, /data-storefront-surface-role="inherit"/);
  assert.match(dash, /data-storefront-surface-role="card"/);
});
