import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import {
  applyPreviewFill,
  isStorePreviewFillEnabled,
  resolveVariantPreviewCardinality,
} from "../../../lib/storefront-composition/preview-fill-policy.ts";
import {
  createFakeProduct,
  createFakeReview,
  PREVIEW_FAKE_ID_PREFIX,
} from "../../../lib/storefront-composition/preview-fake-data.ts";
import { previewFakeProductTitle, previewSampleBadgeLabel } from "../../../lib/storefront-composition/preview-fake-locale.ts";
import { getVariant } from "../../../lib/storefront-composition/registry.ts";
import {
  buildFashionTemplatePage,
  FASHION_DEMO_ORIGIN,
  FASHION_STORE_ORIGIN,
} from "../../../lib/storefront-composition/fashion-demo-preview.ts";

const dir = dirname(fileURLToPath(import.meta.url));
const root = join(dir, "../../../../../");
const sharedRenderer = readFileSync(join(root, "src/frontend/lib/storefront-composition/shared-composition-renderer.tsx"), "utf8");
const fillPolicy = readFileSync(join(root, "src/frontend/lib/storefront-composition/preview-fill-policy.ts"), "utf8");
const fakeData = readFileSync(join(root, "src/frontend/lib/storefront-composition/preview-fake-data.ts"), "utf8");
const bannerGrid = readFileSync(join(root, "src/frontend/app/storefront/storefront-home-blocks.tsx"), "utf8");
const locks = readFileSync(join(root, "docs/architecture/TOOBA-LOCKS.md"), "utf8");
const fashionDemo = readFileSync(join(root, "src/frontend/lib/storefront-composition/fashion-demo-preview.ts"), "utf8");
const landingSections = readFileSync(join(root, "src/frontend/app/storefront/storefront-landing-sections.tsx"), "utf8");

test("PreviewFillPolicy module exists and is centralized", () => {
  assert.match(fillPolicy, /export function applyPreviewFill/);
  assert.match(fillPolicy, /export function resolveVariantPreviewCardinality/);
  assert.match(sharedRenderer, /applyPreviewFill/);
  assert.match(sharedRenderer, /isStorePreviewFillEnabled/);
  assert.doesNotMatch(fillPolicy, /getIndustryTemplate|loadFashionTemplate/);
  assert.match(fillPolicy, /never Template Catalog fallback/);
});

test("zero Store data uses fake fill, not PreviewPlaceholderSurface as primary path", () => {
  assert.doesNotMatch(sharedRenderer, /PreviewPlaceholderSurface/);
  assert.doesNotMatch(bannerGrid, /PreviewPlaceholderSurface/);
  const zero = applyPreviewFill({
    enabled: true,
    variantKey: "reviews.card-carousel",
    realItems: [],
    createFake: (i) => createFakeReview(i, "fa"),
  });
  assert.equal(zero.realCount, 0);
  assert.ok(zero.fakeCount >= 2);
  assert.equal(zero.items.length, zero.targetCount);
  assert.ok(zero.items.every((item) => item.previewFake));
});

test("partial Store data preserves real items and fills only missing slots", () => {
  const real = [createFakeReview(99, "fa")];
  real[0]!.previewFake = false;
  real[0]!.publicId = "real-review-1";
  const mixed = applyPreviewFill({
    enabled: true,
    variantKey: "reviews.card-carousel",
    realItems: real,
    createFake: (i) => createFakeReview(i, "fa"),
  });
  assert.equal(mixed.realCount, 1);
  assert.ok(mixed.fakeCount >= 1);
  assert.equal(mixed.items[0]!.publicId, "real-review-1");
  assert.equal(mixed.items[0]!.previewFake, false);
  assert.ok(mixed.items.slice(1).every((item) => item.previewFake));
});

test("Store fill never queries Template Catalog / Sample Fashion origin", () => {
  assert.doesNotMatch(fakeData, /loadFashionTemplatePreview|FASHION_DEMO_ORIGIN|getIndustryTemplate/);
  assert.match(fashionDemo, /Strict isolation: never inject Template\/Sample banner/);
  const page = buildFashionTemplatePage(
    { products: [], categories: [], brands: [], articles: [], reviews: [], menus: {} },
    { origin: FASHION_STORE_ORIGIN },
  );
  assert.doesNotMatch(JSON.stringify(page), /fashion-template-catalog-persisted/);
  const sample = buildFashionTemplatePage(
    { products: [], categories: [], brands: [], articles: [], reviews: [], menus: {} },
    { origin: FASHION_DEMO_ORIGIN },
  );
  assert.match(sample.sections.find((s) => s.sectionType === "BannerShowcase")!.config, /fashion-template|imageUrl/);
});

test("fake preview items are non-persistent and use preview-fake id prefix", () => {
  const product = createFakeProduct(0, "fa");
  assert.ok(product.productId.startsWith(PREVIEW_FAKE_ID_PREFIX));
  assert.equal(product.previewFake, true);
  assert.doesNotMatch(fakeData, /INSERT INTO/);
  assert.doesNotMatch(fakeData, /fetch\(/);
  assert.doesNotMatch(fakeData, /\/v1\//);
});

test("published storefront path does not enable fill", () => {
  assert.equal(isStorePreviewFillEnabled(false, "store"), false);
  assert.equal(isStorePreviewFillEnabled(true, "sample"), false);
  assert.equal(isStorePreviewFillEnabled(true, "store"), true);
  assert.equal(isStorePreviewFillEnabled(undefined, undefined), false);
  assert.match(landingSections, /previewSource/);
  const published = applyPreviewFill({
    enabled: isStorePreviewFillEnabled(false, "store"),
    variantKey: "product.card-carousel",
    realItems: [],
    createFake: (i) => createFakeProduct(i, "fa"),
  });
  assert.equal(published.fakeCount, 0);
  assert.equal(published.items.length, 0);
});

test("variant contract controls preview cardinality", () => {
  const two = resolveVariantPreviewCardinality("banner.two-equal");
  assert.equal(two.previewTargetItems, 2);
  const four = resolveVariantPreviewCardinality("banner.four-grid");
  assert.equal(four.previewTargetItems, 4);
  const reviews = getVariant("reviews.card-carousel");
  assert.ok(reviews?.previewTargetItems != null);
  assert.ok((reviews?.previewMinItems ?? 0) >= 1);
});

test("Banner Store preview with no Store banner uses fake fill not Template banner", () => {
  const filled = applyPreviewFill({
    enabled: true,
    variantKey: "banner.two-equal",
    realItems: [] as Array<{ src: string }>,
    createFake: (i) => ({ src: `/images/fashion-template/${(i % 8) + 1}.jpg`, previewFake: true as const }),
  });
  assert.equal(filled.items.length, 2);
  assert.ok(filled.items.every((item) => item.src.includes("/images/fashion-template/")));
  assert.match(sharedRenderer, /createFakeBanner/);
  assert.match(sharedRenderer, /allowHomeFallback=\{!storePreview\}/);
});

test("localized fake preview resources follow selected locale", () => {
  assert.match(previewFakeProductTitle(0, "fa"), /کالا/);
  assert.match(previewFakeProductTitle(0, "en-US"), /Preview product/);
  assert.match(previewFakeProductTitle(0, "ar"), /منتج/);
  assert.equal(previewSampleBadgeLabel("fa"), "نمونه نمایشی");
  assert.match(previewSampleBadgeLabel("en"), /Sample/);
});

test("same Variant geometry contract used for Sample and Store-empty preview", () => {
  assert.match(sharedRenderer, /LandingReviews|LandingArticleList|CompositionBannerGrid|LandingProductRail/);
  assert.doesNotMatch(sharedRenderer, /PreviewPlaceholderSurface/);
  assert.match(sharedRenderer, /storePreviewFill=\{storePreview\}/);
});

test("R8 locks registered", () => {
  for (const lock of [
    "LOCK-SF-306",
    "LOCK-SF-307",
    "LOCK-SF-308",
    "LOCK-SF-309",
    "LOCK-SF-310",
    "LOCK-SF-311",
  ]) {
    assert.match(locks, new RegExp(lock));
  }
});
