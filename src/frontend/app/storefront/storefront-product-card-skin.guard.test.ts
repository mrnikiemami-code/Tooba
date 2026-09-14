import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const read = (rel: string) => fs.readFileSync(path.join(root, rel), "utf8");

const surfaces = [
  "app/storefront/storefront-home.tsx",
  "app/storefront/storefront-home-repair-sections.tsx",
  "app/storefront/storefront-listing.tsx",
  "app/storefront/storefront-category-plp.tsx",
  "app/storefront/storefront-pdp.tsx",
  "app/storefront/storefront-merchandising.tsx",
  "app/storefront/storefront-cart.tsx",
  "app/customer-panel/wishlist/page.tsx",
];

test("one canonical card consumes store skin on all listing surfaces", () => {
  const card = read("app/storefront/storefront-product-card.tsx");
  assert.equal((card.match(/export function StorefrontProductCardView/g) ?? []).length, 1);
  assert.match(card, /useProductCardSkin/);
  assert.match(card, /data-product-card-skin/);
  assert.doesNotMatch(card, /loadStorefrontAppearance/);
  assert.doesNotMatch(card, /\/v1\/storefront\/appearance/);
  for (const surface of surfaces) {
    const source = read(surface);
    assert.match(source, /StorefrontProductCardView/, surface);
    assert.doesNotMatch(source, /productCardSkin\s*=/, surface);
    assert.doesNotMatch(source, /\/v1\/storefront\/appearance/, surface);
  }
});

test("listing density and hover geometry stay canonical", () => {
  const listing = read("app/storefront/storefront-listing.tsx");
  const category = read("app/storefront/storefront-category-plp.tsx");
  const skins = read("lib/storefront-appearance/product-card-skin.ts");
  assert.match(listing, /grid-cols-2 sm:grid-cols-2 lg:grid-cols-4/);
  assert.match(category, /grid-cols-2 md:grid-cols-3 xl:grid-cols-4/);
  assert.match(skins, /hover:-translate-y-1/);
  assert.match(skins, /aspect-\[4\/5\]/);
});
