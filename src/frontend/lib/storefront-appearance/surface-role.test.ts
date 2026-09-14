import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import { listStorefrontPalettes, resolveTintTokens } from "./palette-registry.ts";
import { landingSectionSurfaceRole, STOREFRONT_SURFACE_ROLES } from "./surface-role.ts";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");

test("landing SectionTypes map to four global roles not page-local tokens", () => {
  assert.equal(landingSectionSurfaceRole("Hero"), "accent");
  assert.equal(landingSectionSurfaceRole("PromoBanner"), "accent");
  assert.equal(landingSectionSurfaceRole("ProductCollection"), "alternate");
  assert.equal(landingSectionSurfaceRole("ArticleList"), "alternate");
  assert.equal(landingSectionSurfaceRole("CategoryGrid"), "section");
  assert.equal(landingSectionSurfaceRole("BrandStrip"), "section");
  assert.equal(landingSectionSurfaceRole("Reviews"), "section");
  assert.equal(landingSectionSurfaceRole("RichText"), "section");
  assert.equal(landingSectionSurfaceRole("NavigationMenu"), "section");
  assert.equal(landingSectionSurfaceRole("Unknown"), "section");
  assert.deepEqual([...STOREFRONT_SURFACE_ROLES], ["page", "section", "alternate", "accent"]);
});

test("PaletteTint four roles are distinct and not primary or status", () => {
  for (const palette of listStorefrontPalettes()) {
    const tint = resolveTintTokens(palette.key);
    const light = [tint.pageBackgroundRgb, tint.sectionBackgroundRgb, tint.sectionAlternateRgb, tint.sectionAccentRgb];
    const dark = [tint.pageBackgroundDarkRgb, tint.sectionBackgroundDarkRgb, tint.sectionAlternateDarkRgb, tint.sectionAccentDarkRgb];
    assert.equal(new Set(light).size, 4, `${palette.key} light`);
    assert.equal(new Set(dark).size, 4, `${palette.key} dark`);
    for (const rgb of [...light, ...dark]) {
      assert.notEqual(rgb, palette.tokens.primaryRgb);
      assert.notEqual(rgb, "185 28 28");
      assert.notEqual(rgb, "21 128 61");
    }
  }
});

test("shared pages map to global surface roles without local appearance fetch", () => {
  const landing = fs.readFileSync(path.join(root, "app/storefront/storefront-landing-sections.tsx"), "utf8");
  const home = fs.readFileSync(path.join(root, "app/storefront/storefront-home.tsx"), "utf8");
  const listing = fs.readFileSync(path.join(root, "app/storefront/storefront-listing.tsx"), "utf8");
  const pdp = fs.readFileSync(path.join(root, "app/storefront/storefront-pdp.tsx"), "utf8");
  assert.match(landing, /landingSectionSurfaceRole/);
  assert.match(landing, /data-storefront-surface-role/);
  assert.doesNotMatch(landing, /loadStorefrontAppearance/);
  assert.match(home, /data-storefront-surface-role/);
  assert.match(listing, /data-storefront-surface-role/);
  assert.match(pdp, /data-storefront-surface-role/);
  assert.doesNotMatch(pdp, /\/v1\/storefront\/appearance/);
});
