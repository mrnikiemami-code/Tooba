import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

import { pickCartRecommendations } from "./storefront-cart-recommendations.ts";
import {
  acceptStaleGeneration,
  cartRouteMountWithinBounds,
  eventRefreshWithinBounds,
  miniCartOpenWithinBounds,
  recommendationsWithinBounds,
  resetCartOwnerReadLedger,
  snapshotCartOwnerReadLedger,
  noteCartOwnerRead,
} from "./storefront-cart-read-ownership.ts";
import type { StorefrontHomePage, StorefrontProductCard } from "./storefront-model.ts";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const cartPage = fs.readFileSync(path.join(root, "app/cart/page.tsx"), "utf8");
const cartClient = fs.readFileSync(path.join(root, "app/storefront/storefront-cart.tsx"), "utf8");
const miniCart = fs.readFileSync(path.join(root, "app/storefront/storefront-mini-cart.tsx"), "utf8");
const header = fs.readFileSync(path.join(root, "app/storefront/storefront-header.tsx"), "utf8");
const destinationGuard = fs.readFileSync(
  path.join(root, "app/storefront/storefront-shipping-destination-choice.guard.test.ts"),
  "utf8",
);
const pendingGuard = fs.readFileSync(
  path.join(root, "app/storefront/storefront-navigation-pending.guard.test.ts"),
  "utf8",
);

test("cart page critical path uses categories only — no loadStorefrontHome await", () => {
  assert.match(cartPage, /loadStorefrontCategories/);
  assert.doesNotMatch(cartPage, /loadStorefrontHome/);
  assert.doesNotMatch(cartPage, /await loadStorefrontHome/);
  assert.match(cartPage, /searchCatalog=\{\[\]\}/);
});

test("recommendations load only after primary cart usable with cancel on unmount", () => {
  assert.match(cartClient, /if \(loading\) return/);
  assert.match(cartClient, /loadStorefrontHome/);
  assert.match(cartClient, /pickCartRecommendations/);
  assert.match(cartClient, /cancelled = true/);
  assert.match(cartClient, /noteCartOwnerRead\("cart-route-recommendations"\)/);
});

test("mini-cart CTA shows action-level busy feedback tied to navigation pending", () => {
  assert.match(miniCart, /data-action-pending/);
  assert.match(miniCart, /aria-busy/);
  assert.match(miniCart, /Loader2/);
  assert.match(miniCart, /useStorefrontNavigationPending/);
  assert.match(miniCart, /در حال باز کردن سبد/);
});

test("owners cancel listeners and ignore stale generations", () => {
  assert.match(cartClient, /removeEventListener\(CART_CHANGED_EVENT/);
  assert.match(miniCart, /removeEventListener\(CART_CHANGED_EVENT/);
  assert.match(header, /removeEventListener\(CART_CHANGED_EVENT/);
  assert.match(cartClient, /if \(cancelled\) return/);
  assert.match(miniCart, /mine !== generation/);
  assert.equal(acceptStaleGeneration(1, 2), false);
  assert.equal(acceptStaleGeneration(3, 3), true);
});

test("ownership ledger bounds for one mount / open / event", () => {
  resetCartOwnerReadLedger();
  noteCartOwnerRead("cart-route-mount-cart");
  noteCartOwnerRead("cart-route-mount-pending");
  noteCartOwnerRead("cart-route-mount-login");
  noteCartOwnerRead("cart-route-recommendations");
  noteCartOwnerRead("mini-cart-open-cart");
  noteCartOwnerRead("mini-cart-event-cart");
  noteCartOwnerRead("header-badge-mount");
  noteCartOwnerRead("header-badge-event");
  const snap = snapshotCartOwnerReadLedger();
  assert.equal(cartRouteMountWithinBounds(snap), true);
  assert.equal(recommendationsWithinBounds(snap), true);
  assert.equal(miniCartOpenWithinBounds(snap, 1), true);
  assert.equal(eventRefreshWithinBounds(snap["mini-cart-event-cart"], 1), true);
  assert.equal(eventRefreshWithinBounds(snap["header-badge-event"], 1), true);
});

test("pickCartRecommendations keeps live feed cards only", () => {
  const card = (id: string, offer: string | null) =>
    ({
      productId: id,
      primaryOfferId: offer,
      title: id,
      slug: id,
      inStock: true,
    }) as unknown as StorefrontProductCard;
  const home = {
    newArrivals: [card("a", "o1"), card("a", "o1")],
    featuredProducts: [card("b", "o2"), card("c", null)],
    specialOffers: [card("d", "o3")],
    categories: [],
    campaignProducts: [],
    productRail: [],
    brands: [],
    heroTitle: "",
    heroSubtitle: "",
    homeCategories: [],
    bestSellerColumns: [],
    mostViewedProducts: [],
    featuredReviews: [],
    latestArticles: [],
  } satisfies StorefrontHomePage;
  const picked = pickCartRecommendations(home);
  assert.deepEqual(
    picked.map((row) => row.productId),
    ["a", "b", "d"],
  );
  assert.deepEqual(pickCartRecommendations(null), []);
});

test("accepted 245-R2 timeout and 245-R3 destination guards remain present", () => {
  assert.match(pendingGuard, /storefront-nav-timeout/);
  assert.match(destinationGuard, /zero direct refreshProjection/);
  assert.match(destinationGuard, /isHardInvalidatingRoutingChange|hard invalidation/i);
});
