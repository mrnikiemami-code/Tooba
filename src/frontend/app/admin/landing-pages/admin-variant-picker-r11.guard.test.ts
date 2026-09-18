/**
 * TB-P10-T022-R11 focused guards — Variant Picker V2.
 */
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import { VARIANTS } from "../../../lib/storefront-composition/registry.ts";
import {
  VARIANT_DESIGN_NAMES,
  getVariantDesignMeta,
  variantDesignNameFa,
} from "../../../lib/storefront-composition/variant-design-names.ts";
import { isPreviewFakeId, PREVIEW_FAKE_ID_PREFIX } from "../../../lib/storefront-composition/preview-fake-data.ts";
import { canonicalizeVariantKey } from "../../../lib/storefront-composition/resolve-variant.ts";

const dir = dirname(fileURLToPath(import.meta.url));
const root = join(dir, "../../../../../");
const wizard = readFileSync(join(dir, "admin-section-wizard.tsx"), "utf8");
const livePreview = readFileSync(join(dir, "../../../lib/storefront-composition/variant-live-preview.tsx"), "utf8");
const designNames = readFileSync(join(dir, "../../../lib/storefront-composition/variant-design-names.ts"), "utf8");
const registry = readFileSync(join(dir, "../../../lib/storefront-composition/registry.ts"), "utf8");
const locks = readFileSync(join(root, "docs/architecture/TOOBA-LOCKS.md"), "utf8");
const sharedRenderer = readFileSync(join(dir, "../../../lib/storefront-composition/shared-composition-renderer.tsx"), "utf8");

test("every registered Variant has Persian human design metadata", () => {
  const exactProductShowcaseNames = new Set(["سانی", "مانی", "سینمایی", "سینمایی پلاس", "کاشف"]);
  for (const v of VARIANTS) {
    const meta = getVariantDesignMeta(v.key);
    assert.ok(meta, `missing design meta for ${v.key}`);
    if (!exactProductShowcaseNames.has(meta!.designNameFa)) {
      assert.match(meta!.designNameFa, /طرح/);
    }
    assert.ok(meta!.descriptionFa.trim().length > 4);
    assert.equal(v.nameFa, meta!.designNameFa);
    assert.doesNotMatch(meta!.designNameFa, /SectionType|VariantKey|breakpoint|JSON|CSS|HTML/i);
  }
  assert.equal(Object.keys(VARIANT_DESIGN_NAMES).length, VARIANTS.length);
});

test("no Variant picker falls back to geometric preview canvas", () => {
  assert.doesNotMatch(wizard, /VariantPreviewCanvas/);
  assert.match(wizard, /VariantLivePreview/);
  assert.match(wizard, /data-variant-picker-v2/);
  assert.doesNotMatch(livePreview, /layoutAwareVariantPreview/);
  assert.doesNotMatch(livePreview, /scale-\[[0-9]/);
  assert.doesNotMatch(livePreview, /transform:\s*scale/);
});

test("picker uses production shared renderer mapping", () => {
  assert.match(livePreview, /renderSharedLandingSection/);
  assert.match(livePreview, /previewSource:\s*"store"/);
  assert.match(sharedRenderer, /export function renderSharedLandingSection/);
});

test("selected Variant key persists unchanged (display name is presentation only)", () => {
  assert.match(wizard, /(?:config:\s*\{\s*\.\.\.config,\s*variantKey\s*\}|saveConfig:\s*Record<string,\s*unknown>\s*=\s*\{\s*\.\.\.config,\s*variantKey\s*\})/);
  assert.match(wizard, /setVariantKey\(variant\.variantKey\)/);
  assert.match(designNames, /Persisted identity remains the stable variant key/);
  assert.equal(canonicalizeVariantKey("banner.mosaic-2x2"), "banner.four-grid");
  assert.equal(variantDesignNameFa("product.card-carousel"), "ویترین محصولات — طرح آریا");
});

test("Review uses same VariantLivePreview renderer", () => {
  assert.match(wizard, /data-review-live-preview/);
  assert.match(wizard, /VariantLivePreview/);
  assert.match(wizard, /review-preview-/);
  assert.match(wizard, /data-review-design-name/);
});

test("PreviewFakeData non-persistent and Store/Template independent", () => {
  assert.match(livePreview, /data-preview-source="PreviewFake"/);
  assert.match(livePreview, /EMPTY_CONTEXT/);
  assert.ok(isPreviewFakeId(`${PREVIEW_FAKE_ID_PREFIX}product-1`));
  assert.equal(isPreviewFakeId("real-product"), false);
  assert.doesNotMatch(livePreview, /template-catalog|TemplateCatalog|fetchPublic/i);
});

test("carousel / tab interaction preserved; unsafe actions suppressed", () => {
  assert.match(livePreview, /suppressBusinessNavigation|onClickCapture/);
  assert.match(livePreview, /swiper-button-prev|role === "tab"/);
  assert.match(livePreview, /data-preview-interaction="safe"/);
  assert.match(livePreview, /IntersectionObserver/);
});

test("RTL / accessibility selection not color-only", () => {
  assert.match(wizard, /aria-pressed/);
  assert.match(wizard, /focus-visible:ring-2/);
  assert.match(wizard, /dir="rtl"|VariantLivePreview/);
  assert.match(livePreview, /dir="rtl"/);
  assert.match(wizard, /ring-1 ring-\[#2563EB\]/);
});

test("R11 locks registered LOCK-SF-335…341", () => {
  for (const lock of [
    "LOCK-SF-335",
    "LOCK-SF-336",
    "LOCK-SF-337",
    "LOCK-SF-338",
    "LOCK-SF-339",
    "LOCK-SF-340",
    "LOCK-SF-341",
  ]) {
    assert.match(locks, new RegExp(lock));
  }
});

test("registry wires design names from code-owned map", () => {
  assert.match(registry, /getVariantDesignMeta/);
  assert.match(registry, /designNameFa/);
});
