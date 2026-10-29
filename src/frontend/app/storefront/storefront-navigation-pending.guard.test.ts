import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const productCard = fs.readFileSync(path.join(root, "app/storefront/storefront-product-card.tsx"), "utf8");
const localizedLink = fs.readFileSync(path.join(root, "lib/i18n/LocalizedLink.tsx"), "utf8");
const providers = fs.readFileSync(path.join(root, "app/providers.tsx"), "utf8");
const pdpPage = fs.readFileSync(path.join(root, "app/products/[slug]/page.tsx"), "utf8");
const miniCart = fs.readFileSync(path.join(root, "app/storefront/storefront-mini-cart.tsx"), "utf8");
const pendingCtx = fs.readFileSync(
  path.join(root, "lib/storefront-navigation/navigation-pending-context.tsx"),
  "utf8",
);
const pendingPure = fs.readFileSync(path.join(root, "lib/storefront-navigation/navigation-pending.ts"), "utf8");

test("product card announces pending before router.push on eye action", () => {
  assert.match(productCard, /useStorefrontNavigationPending/);
  assert.match(productCard, /navPending\.begin\(href/);
});

test("LocalizedLink begins pending on primary click and blocks only duplicate same-target", () => {
  assert.match(localizedLink, /navPending\.begin\(resolved, event\)/);
  assert.match(localizedLink, /result\.kind === \"duplicate\"/);
  assert.match(localizedLink, /event\.preventDefault\(\)/);
});

test("AppProviders mounts storefront navigation pending provider", () => {
  assert.match(providers, /StorefrontNavigationPendingProvider/);
});

test("PDP page no longer loads full home for shell categories", () => {
  assert.match(pdpPage, /loadStorefrontCategories/);
  assert.doesNotMatch(pdpPage, /loadStorefrontHome/);
});

test("mini-cart cart CTA uses LocalizedLink for soft navigation + pending", () => {
  assert.match(miniCart, /LocalizedLink/);
  assert.match(miniCart, /data-testid="mini-cart-checkout-cta"/);
  assert.doesNotMatch(miniCart, /لینک کامل سند/);
});

test("timeout feedback is visible (not sr-only only) and auto-dismisses", () => {
  assert.match(pendingCtx, /data-testid="storefront-nav-timeout"/);
  assert.match(pendingCtx, /NAVIGATION_RETRY_DISMISS_MS/);
  assert.match(pendingCtx, /pointer-events-none/);
  assert.match(pendingPure, /باز شدن صفحه طول کشید/);
  assert.match(pendingPure, /The page is taking too long to open/);
  assert.doesNotMatch(pendingCtx, /StorefrontNavigationPendingProvider[\s\S]*StorefrontNavigationPendingProvider/);
});
