import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import { appearanceCssVars, resolveBrandTokens, resolveTintTokens } from "../../lib/storefront-appearance/palette-registry.ts";
import { storefrontAppearanceStyle } from "./storefront-appearance-api.ts";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");

test("SSR root applies appearance CSS variables", () => {
  const layout = fs.readFileSync(path.join(root, "app/layout.tsx"), "utf8");
  assert.match(layout, /loadStorefrontAppearance/);
  assert.match(layout, /data-storefront-palette/);
  assert.match(layout, /data-storefront-scope/);
  assert.match(layout, /data-storefront-product-card-skin/);
  assert.match(layout, /data-storefront-background-style/);
  assert.match(layout, /StorefrontProductCardSkinProvider/);
  assert.match(layout, /storefrontAppearanceStyle/);
  assert.match(layout, /THEME_BOOTSTRAP_SCRIPT/);
  assert.doesNotMatch(layout, /dangerouslySetInnerHTML=\{[^}]*payload/);
});

test("appearance style is brand-token only", () => {
  const style = storefrontAppearanceStyle({
    storeScope: "tenant:store-a",
    paletteKey: "tooba-blue",
    paletteKeyWasKnown: true,
    themeMode: "Light",
    productCardSkin: "classic",
    backgroundStyle: "Neutral",
    tokens: resolveBrandTokens("tooba-blue"),
    tint: resolveTintTokens("tooba-blue"),
  });
  assert.equal(style["--color-primary"], "37 99 235");
  assert.equal(style["--color-primary-on-dark"], "59 115 237");
  assert.equal(style["--color-page-tint"], resolveTintTokens("tooba-blue").pageBackgroundRgb);
  assert.ok(Object.hasOwn(style, "--color-card-derived"));
  assert.equal(Object.hasOwn(style, "--color-danger"), false);
  const vars = appearanceCssVars(resolveBrandTokens("unknown"));
  assert.equal(vars["--color-primary"], "37 99 235");
});

test("appearance API does not query per product card", () => {
  const card = fs.readFileSync(path.join(root, "app/storefront/storefront-product-card.tsx"), "utf8");
  assert.doesNotMatch(card, /loadStorefrontAppearance/);
  assert.doesNotMatch(card, /\/v1\/storefront\/appearance/);
  assert.match(card, /bg-primary|STOREFRONT_ACCENT/);
});
