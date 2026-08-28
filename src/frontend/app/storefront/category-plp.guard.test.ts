import assert from "node:assert/strict";
import test from "node:test";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const root = path.join(path.dirname(fileURLToPath(import.meta.url)), "../..");

test("category PLP route and public prefix are wired", () => {
  const page = fs.readFileSync(path.join(root, "app/category/[slug]/page.tsx"), "utf8");
  const routing = fs.readFileSync(path.join(root, "lib/i18n/routing.ts"), "utf8");
  const view = fs.readFileSync(path.join(root, "app/storefront/storefront-category-plp.tsx"), "utf8");
  const api = fs.readFileSync(path.join(root, "app/storefront/storefront-api.ts"), "utf8");
  const link = fs.readFileSync(path.join(root, "lib/i18n/LocalizedLink.tsx"), "utf8");

  assert.match(routing, /"\/category"/);
  assert.match(page, /loadStorefrontCategoryPlp/);
  assert.match(page, /robots: filtered/);
  assert.match(page, /plp\.isRedirect/);
  assert.match(view, /data-testid="category-plp-page"/);
  assert.match(view, /f_\$\{facet\.code\}|filterParamKey/);
  assert.match(api, /\/v1\/storefront\/category-plp\//);
  assert.match(link, /alreadyLocalePrefixed|\/\(fa\|en\)/);
});
