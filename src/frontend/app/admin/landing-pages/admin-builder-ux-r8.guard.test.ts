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
  createFakeArticle,
  createFakeBanner,
  createFakeBrand,
  createFakeCategory,
  createFakeProduct,
  createFakeReview,
  createFakeStory,
  PREVIEW_FAKE_ID_PREFIX,
  PREVIEW_FAKE_SOURCE,
} from "../../../lib/storefront-composition/preview-fake-data.ts";
import { PREVIEW_FAKE_ASSET_ROOT } from "../../../lib/storefront-composition/preview-fake-media.ts";
import { previewFakeProductTitle, previewSampleBadgeLabel } from "../../../lib/storefront-composition/preview-fake-locale.ts";
import { getVariant } from "../../../lib/storefront-composition/registry.ts";
import {
  buildFashionTemplatePage,
  FASHION_DEMO_ORIGIN,
  FASHION_STORE_ORIGIN,
} from "../../../lib/storefront-composition/fashion-demo-preview.ts";
import { FASHION_IMAGES, TEMPLATE_MEDIA_GUIDS } from "../../../lib/storefront-composition/fashion-demo-media.ts";

const dir = dirname(fileURLToPath(import.meta.url));
const root = join(dir, "../../../../../");
const sharedRenderer = readFileSync(join(root, "src/frontend/lib/storefront-composition/shared-composition-renderer.tsx"), "utf8");
const fillPolicy = readFileSync(join(root, "src/frontend/lib/storefront-composition/preview-fill-policy.ts"), "utf8");
const fakeData = readFileSync(join(root, "src/frontend/lib/storefront-composition/preview-fake-data.ts"), "utf8");
const fakeMedia = readFileSync(join(root, "src/frontend/lib/storefront-composition/preview-fake-media.ts"), "utf8");
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
    createFake: (i) => createFakeBanner(i, "fa"),
  });
  assert.equal(filled.items.length, 2);
  assert.ok(filled.items.every((item) => item.src.includes("/images/preview-placeholder/")));
  assert.ok(filled.items.every((item) => !item.src.includes("/images/fashion-template/")));
  assert.match(sharedRenderer, /createFakeBanner/);
  assert.match(sharedRenderer, /allowHomeFallback=\{!storePreview\}/);
});

test("localized fake preview resources follow selected locale", () => {
  assert.match(previewFakeProductTitle(0, "fa"), /محصول نمونه/);
  assert.match(previewFakeProductTitle(0, "en-US"), /Sample product/);
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

test("Store Preview fake fill uses dedicated Preview-Fake media, not Template Fashion assets", () => {
  assert.doesNotMatch(fakeData, /from ["'].*fashion-demo-media/);
  assert.doesNotMatch(fakeData, /FASHION_IMAGES|TEMPLATE_MEDIA_GUIDS|\/images\/fashion-template\//);
  assert.doesNotMatch(fakeMedia, /from ["'].*fashion-demo-media|\/images\/fashion-template\/|TEMPLATE_MEDIA_GUIDS\b/);
  assert.doesNotMatch(fakeMedia, /\bFASHION_IMAGES\b/);
  assert.match(fakeMedia, /preview-placeholder/);

  const product = createFakeProduct(0, "fa");
  const category = createFakeCategory(0, "fa");
  const brand = createFakeBrand(0, "fa");
  const article = createFakeArticle(0, "fa");
  const banner = createFakeBanner(0, "fa");
  const story = createFakeStory(0, "fa");
  const review = createFakeReview(0, "fa");

  const mediaUrls = [
    product.mediaAssetId,
    category.imageUrl,
    category.imageMediaAssetId,
    brand.logoMediaAssetId,
    article.coverMediaAssetId,
    banner.src,
    story.coverMediaUrl,
    review.authorAvatarUrl,
  ];

  for (const url of mediaUrls) {
    assert.ok(url?.startsWith(`${PREVIEW_FAKE_ASSET_ROOT}/`), `expected preview-placeholder url, got ${url}`);
    assert.ok(!url?.includes("/images/fashion-template/"));
    assert.equal(FASHION_IMAGES.includes(url as (typeof FASHION_IMAGES)[number]), false);
    assert.equal(TEMPLATE_MEDIA_GUIDS.includes(url as (typeof TEMPLATE_MEDIA_GUIDS)[number]), false);
  }

  assert.equal(product.previewSource, PREVIEW_FAKE_SOURCE);
  assert.equal(category.previewSource, PREVIEW_FAKE_SOURCE);
  assert.equal(brand.previewSource, PREVIEW_FAKE_SOURCE);
  assert.equal(article.previewSource, PREVIEW_FAKE_SOURCE);
  assert.equal(banner.previewSource, PREVIEW_FAKE_SOURCE);
  assert.equal(story.previewSource, PREVIEW_FAKE_SOURCE);
  assert.equal(review.previewSource, PREVIEW_FAKE_SOURCE);

  const serialized = JSON.stringify({ product, category, brand, article, banner, story, review });
  assert.doesNotMatch(serialized, /019022a5-0000-7000-8000-00000000a00/);
  assert.doesNotMatch(serialized, /fashion-template/);
  assert.doesNotMatch(serialized, /demo-fashion-media-/);
});

test("Sample mode still uses Template Catalog Fashion media paths", () => {
  const sample = buildFashionTemplatePage(
    { products: [], categories: [], brands: [], articles: [], reviews: [], menus: {} },
    { origin: FASHION_DEMO_ORIGIN },
  );
  const blob = JSON.stringify(sample);
  assert.match(blob, /fashion-template|imageUrl/);
  assert.doesNotMatch(blob, /preview-placeholder/);
});

test("R8-R2 Preview-Fake isolation locks registered", () => {
  for (const lock of ["LOCK-SF-312", "LOCK-SF-313", "LOCK-SF-314", "LOCK-SF-315"]) {
    assert.match(locks, new RegExp(lock));
  }
});
