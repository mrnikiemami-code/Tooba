import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const dir = dirname(fileURLToPath(import.meta.url));
const header = readFileSync(join(dir, "storefront-header.tsx"), "utf8");
const api = readFileSync(join(dir, "storefront-menu-api.ts"), "utf8");
const tree = readFileSync(join(dir, "storefront-menu-tree.tsx"), "utf8");
const landing = readFileSync(join(dir, "storefront-landing-sections.tsx"), "utf8");

test("header uses store menu only when selected and otherwise keeps fallback", () => {
  assert.match(header, /loadStorefrontHeaderMenu/);
  assert.match(header, /usesFallback/);
  assert.match(header, /loadStorefrontMegaMenu/);
  assert.doesNotMatch(header, /dangerouslySetInnerHTML/);
});

test("one menu tree renderer is shared", () => {
  assert.match(api, /\/v1\/storefront\/header-menu/);
  assert.match(tree, /StorefrontMenuLinks/);
  assert.match(landing, /case "NavigationMenu"/);
  assert.doesNotMatch(tree, /eval\(/);
});
