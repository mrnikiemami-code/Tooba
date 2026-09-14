import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const dir = dirname(fileURLToPath(import.meta.url));
const route = readFileSync(join(dir, "../[slug]/page.tsx"), "utf8");
const home = readFileSync(join(dir, "../page.tsx"), "utf8");
const api = readFileSync(join(dir, "storefront-landing-api.ts"), "utf8");
const renderer = readFileSync(join(dir, "storefront-landing-sections.tsx"), "utf8");

test("landing route uses one canonical section renderer", () => {
  assert.match(route, /loadPublishedLandingPage/);
  assert.match(route, /StorefrontLandingSections/);
  assert.match(route, /isReservedLandingSlug/);
  assert.match(route, /notFound\(\)/);
  assert.doesNotMatch(route, /drag|composer|PageId/);
  assert.match(api, /\/v1\/storefront\/pages\//);
  assert.match(api, /sections/);
  assert.doesNotMatch(api, /localStorage/);
});

test("home uses selected published landing or canonical home", () => {
  assert.match(home, /loadStorefrontHomeSelection/);
  assert.match(home, /selectedPage/);
  assert.match(home, /StorefrontShopeivaHome/);
  assert.match(home, /storefront-custom-home/);
});

test("renderer maps approved types and fails unknown types safely", () => {
  assert.match(renderer, /case "Hero"/);
  assert.match(renderer, /case "ProductCollection"/);
  assert.match(renderer, /default:\s*\n\s*return null/);
  assert.doesNotMatch(renderer, /eval\(|dangerouslySetInnerHTML/);
});
