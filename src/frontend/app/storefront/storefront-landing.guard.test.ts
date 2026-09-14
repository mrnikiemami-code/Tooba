import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const dir = dirname(fileURLToPath(import.meta.url));
const route = readFileSync(join(dir, "../[slug]/page.tsx"), "utf8");
const api = readFileSync(join(dir, "storefront-landing-api.ts"), "utf8");

test("landing route is a published-page shell without a section builder", () => {
  assert.match(route, /loadPublishedLandingPage/);
  assert.match(route, /isReservedLandingSlug/);
  assert.match(route, /notFound\(\)/);
  assert.doesNotMatch(route, /PageSection|drag|composer/);
  assert.match(api, /\/v1\/storefront\/pages\//);
  assert.doesNotMatch(api, /localStorage/);
});
