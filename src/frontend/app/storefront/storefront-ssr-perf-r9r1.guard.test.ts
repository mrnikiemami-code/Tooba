import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const dir = dirname(fileURLToPath(import.meta.url));
const root = join(dir, "../../../../");
const resolver = readFileSync(join(dir, "storefront-store-page-resolver.ts"), "utf8");
const cache = readFileSync(join(dir, "storefront-store-page-cache.ts"), "utf8");
const landingApi = readFileSync(join(dir, "storefront-landing-api.ts"), "utf8");
const landingRoute = readFileSync(join(root, "src/frontend/app/landing/[slug]/page.tsx"), "utf8");
const homeRoute = readFileSync(join(root, "src/frontend/app/page.tsx"), "utf8");
const revalidateRoute = readFileSync(join(root, "src/frontend/app/api/storefront/revalidate/route.ts"), "utf8");
const adminApi = readFileSync(join(root, "src/frontend/app/admin/landing-pages/admin-landing-pages-api.ts"), "utf8");
const composer = readFileSync(
  join(root, "src/backend/Host/Tooba.Host/Admin/StoreLandingPageComposer.cs"),
  "utf8",
);
const locks = readFileSync(join(root, "docs/architecture/TOOBA-LOCKS.md"), "utf8");

test("request resolver uses React cache for page + home selection dedupe", () => {
  assert.match(resolver, /import \{ cache \} from "react"/);
  assert.match(resolver, /export const resolvePublishedStorePage = cache\(/);
  assert.match(resolver, /export const resolveStoreHomeSelection = cache\(/);
  assert.match(resolver, /export const resolveLandingRouteModel = cache\(/);
  assert.match(resolver, /export const resolveHomeRouteModel = cache\(/);
  assert.match(resolver, /storePageTag\(/);
  assert.match(resolver, /storeHomeSelectionTag\(/);
});

test("landing metadata and page share canonical resolver (no duplicate raw loadPublishedLandingPage)", () => {
  assert.match(landingRoute, /resolvePublishedStorePage/);
  assert.match(landingRoute, /resolveLandingRouteModel/);
  assert.match(landingRoute, /generateMetadata/);
  assert.doesNotMatch(landingRoute, /loadPublishedLandingPage\(/);
  assert.doesNotMatch(landingRoute, /loadLandingRenderContext\(/);
  assert.match(homeRoute, /resolveStoreHomeSelection/);
  assert.match(homeRoute, /resolveHomeRouteModel/);
});

test("public cache tags are Store+locale+page scoped; admin preview stays no-store path", () => {
  assert.match(cache, /storefront-page:\$\{storeScope\}:\$\{locale\}:\$\{pageKey\}/);
  assert.match(cache, /storefront-home-selection:\$\{storeScope\}/);
  assert.match(landingApi, /options\?\.revalidateSeconds == null/);
  assert.match(landingApi, /cache: "no-store"/);
  assert.match(revalidateRoute, /revalidateTag/);
  assert.match(adminApi, /\/api\/storefront\/revalidate/);
  assert.match(adminApi, /invalidateFromPage|invalidateStorePagesNamespace|invalidatePublicStorePageCache/);
});

test("render context prefers Host-embedded shell; avoids unconditional /home waterfall", () => {
  assert.match(landingApi, /embeddedProducts|page\.products/);
  assert.match(landingApi, /options\?\.home/);
  assert.match(landingApi, /productPoolCoversKeys|embeddedCovers/);
  assert.match(resolver, /home: null/);
  assert.match(composer, /ComposeProductCardsAsync/);
  assert.match(composer, /BuildLatestArticlesAsync|BuildFeaturedReviewsAsync/);
});

test("backend public resolve filters Status in query and embeds section-scoped product cards", () => {
  assert.match(composer, /x\.Status == StoreLandingPageStatus\.Published/);
  assert.match(composer, /ComposeProductCardsAsync/);
  assert.match(composer, /BuildLatestArticlesAsync|BuildFeaturedReviewsAsync/);
  assert.match(composer, /ListCategoriesAsync/);
});

test("locks 325–327 registered", () => {
  assert.match(locks, /LOCK-SF-325/);
  assert.match(locks, /LOCK-SF-326/);
  assert.match(locks, /LOCK-SF-327/);
  assert.match(locks, /deduplicated canonical request resolver/i);
  assert.match(locks, /Store\+locale\+page/i);
});
