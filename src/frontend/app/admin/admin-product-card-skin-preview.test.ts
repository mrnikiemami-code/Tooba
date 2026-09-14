import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const dir = dirname(fileURLToPath(import.meta.url));
const preview = readFileSync(join(dir, "admin-product-card-skin-preview.tsx"), "utf8");
const form = readFileSync(join(dir, "admin-appearance-settings.tsx"), "utf8");
const skins = readFileSync(join(dir, "../../lib/storefront-appearance/product-card-skin.ts"), "utf8");

test("admin skin preview consumes the canonical chrome registry", () => {
  assert.match(preview, /resolveProductCardSkinChrome/);
  assert.match(preview, /resolveProductCardSkin/);
  assert.match(preview, /هدفون بی‌سیم/);
  assert.match(preview, /افزودن به سبد/);
  assert.doesNotMatch(preview, /PRODUCT_CARD_SKINS\s*=/);
  assert.doesNotMatch(preview, /shadow-xl hover:shadow-black\/40/);
  assert.match(form, /AdminProductCardSkinPreview/);
  assert.match(skins, /descriptionFa/);
});
