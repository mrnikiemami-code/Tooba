/**
 * TB-P10-T022-R13-R2 — Product Showcase Embla variant expansion guards.
 */
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import { getVariant, isVariantImplemented, VARIANTS } from "../../../lib/storefront-composition/registry.ts";
import { variantDesignNameFa } from "../../../lib/storefront-composition/variant-design-names.ts";
import { canonicalizeVariantKey } from "../../../lib/storefront-composition/resolve-variant.ts";

const dir = dirname(fileURLToPath(import.meta.url));
const root = join(dir, "../../../../../");
const emblaRails = readFileSync(join(dir, "../../storefront/storefront-product-showcase-embla-rails.tsx"), "utf8");
const sharedRenderer = readFileSync(join(dir, "../../../lib/storefront-composition/shared-composition-renderer.tsx"), "utf8");
const packageJson = JSON.parse(readFileSync(join(dir, "../../../package.json"), "utf8")) as {
  dependencies?: Record<string, string>;
};
const locks = readFileSync(join(root, "docs/architecture/TOOBA-LOCKS.md"), "utf8");

const NEW_KEYS = [
  "product.sunny",
  "product.money",
  "product.cinematic",
  "product.cinematic-plus",
  "product.explorer",
] as const;

const NEW_NAMES: Record<(typeof NEW_KEYS)[number], string> = {
  "product.sunny": "سانی",
  "product.money": "مانی",
  "product.cinematic": "سینمایی",
  "product.cinematic-plus": "سینمایی پلاس",
  "product.explorer": "کاشف",
};

const PRESERVED = ["product.card-carousel", "product.amazing", "product.zohreh", "product.mahoor"] as const;

test("exactly five new Product Showcase Embla variants registered with exact Persian names", () => {
  for (const key of NEW_KEYS) {
    assert.ok(getVariant(key), `missing variant ${key}`);
    assert.equal(isVariantImplemented(key), true);
    assert.equal(variantDesignNameFa(key), NEW_NAMES[key]);
    assert.equal(getVariant(key)!.nameFa, NEW_NAMES[key]);
  }
  assert.equal(NEW_KEYS.length, 5);
});

test("bare technical keys canonicalize to product.* registry keys", () => {
  assert.equal(canonicalizeVariantKey("sunny"), "product.sunny");
  assert.equal(canonicalizeVariantKey("money"), "product.money");
  assert.equal(canonicalizeVariantKey("cinematic"), "product.cinematic");
  assert.equal(canonicalizeVariantKey("cinematic-plus"), "product.cinematic-plus");
  assert.equal(canonicalizeVariantKey("explorer"), "product.explorer");
});

test("existing Product Showcase named designs remain registered", () => {
  for (const key of PRESERVED) {
    assert.ok(getVariant(key), `preserved variant missing: ${key}`);
    assert.equal(isVariantImplemented(key), true);
  }
  assert.match(variantDesignNameFa("product.amazing"), /شگفت/);
  assert.match(variantDesignNameFa("product.zohreh"), /زهره/);
  assert.match(variantDesignNameFa("product.mahoor"), /ماهور/);
  assert.match(variantDesignNameFa("product.card-carousel"), /آریا/);
});

test("Embla dependency present and scoped to new rail component", () => {
  assert.ok(packageJson.dependencies?.["embla-carousel-react"]);
  assert.match(emblaRails, /embla-carousel-react/);
  assert.match(emblaRails, /StorefrontProductCardView/);
  assert.match(emblaRails, /prefers-reduced-motion/);
  assert.doesNotMatch(emblaRails, /three\.js|WebGL|canvas|video/i);
  assert.match(sharedRenderer, /ProductShowcaseEmblaRail/);
  assert.doesNotMatch(sharedRenderer, /replace.*Swiper|migrate.*Swiper/i);
});

test("locks LOCK-SF-375…380 present", () => {
  for (const id of [375, 376, 377, 378, 379, 380]) {
    assert.match(locks, new RegExp(`LOCK-SF-${id}`));
  }
});

test("Product Showcase variant count grew by five only for new keys", () => {
  const productShowcase = VARIANTS.filter((v) => v.sectionTypeKey === "ProductShowcase");
  for (const key of NEW_KEYS) {
    assert.ok(productShowcase.some((v) => v.key === key));
  }
});
