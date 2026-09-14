import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const card = fs.readFileSync(path.join(root, "app/storefront/storefront-product-card.tsx"), "utf8");
const globals = fs.readFileSync(path.join(root, "app/globals.css"), "utf8");
const skins = fs.readFileSync(path.join(root, "lib/storefront-appearance/product-card-skin.ts"), "utf8");

test("one StorefrontProductCardView uses semantic dark chrome", () => {
  assert.equal((card.match(/export function StorefrontProductCardView/g) ?? []).length, 1);
  assert.match(card, /useProductCardSkin/);
  assert.match(card, /resolveProductCardSkinChrome/);
  assert.match(card, /data-product-card-skin/);
  assert.match(card, /text-foreground/);
  assert.match(card, /text-primary/);
  assert.match(card, /data-storefront-media-well="true"/);
  assert.match(skins, /bg-surface/);
  assert.match(skins, /border-border/);
  assert.match(skins, /bg-surface-elevated\/90/);
  assert.match(skins, /bg-background/);
  assert.doesNotMatch(card, /bg-white\/90/);
  assert.doesNotMatch(card, /bg-white[^-]/);
  assert.doesNotMatch(card, /wine-burgundy|slate-navy|forest-green/);
  assert.doesNotMatch(card, /#[0-9A-Fa-f]{3,8}/);
});

test("canonical dark remaps cover leftover light utilities", () => {
  assert.match(globals, /html\.dark \.bg-white\\\/90/);
  assert.match(globals, /--color-brand-emphasis: var\(--color-primary-on-dark\)/);
  assert.doesNotMatch(globals, /wine-burgundy/);
});
