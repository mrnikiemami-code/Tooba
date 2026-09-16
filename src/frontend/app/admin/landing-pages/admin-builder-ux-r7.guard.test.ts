import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import {
  buildFashionTemplatePage,
  FASHION_STORE_ORIGIN,
} from "../../../lib/storefront-composition/fashion-demo-preview.ts";
import {
  fashionPreviewPath,
  previewPlaceholderCopy,
  resolveObjectPosition,
} from "../../../lib/storefront-composition/template-preview-context.ts";

const dir = dirname(fileURLToPath(import.meta.url));
const root = join(dir, "../../../../../");
const fashionDemo = readFileSync(join(root, "src/frontend/lib/storefront-composition/fashion-demo-preview.ts"), "utf8");
const sharedRenderer = readFileSync(join(root, "src/frontend/lib/storefront-composition/shared-composition-renderer.tsx"), "utf8");
const bannerGrid = readFileSync(join(root, "src/frontend/app/storefront/storefront-home-blocks.tsx"), "utf8");
const workspace = readFileSync(join(dir, "admin-template-selection-workspace.tsx"), "utf8");
const fashionView = readFileSync(join(root, "src/frontend/app/template-preview/fashion/fashion-template-preview-view.tsx"), "utf8");
const mediaAsset = readFileSync(join(root, "src/backend/Modules/Media/Tooba.Media.Domain/MediaAsset.cs"), "utf8");
const locks = readFileSync(join(root, "docs/architecture/TOOBA-LOCKS.md"), "utf8");
const landingSections = readFileSync(join(root, "src/frontend/app/storefront/storefront-landing-sections.tsx"), "utf8");

test("Store mode never falls back to Fashion sample banner assets", () => {
  const page = buildFashionTemplatePage(
    { products: [], categories: [], brands: [], articles: [], reviews: [], menus: {} },
    { origin: FASHION_STORE_ORIGIN, locale: "fa" },
  );
  const banner = page.sections.find((s) => s.sectionType === "BannerShowcase");
  assert.ok(banner);
  const config = JSON.parse(banner!.config) as { items?: unknown[]; previewPlaceholder?: boolean; imageUrl?: string };
  assert.equal(Array.isArray(config.items) ? config.items.length : -1, 0);
  assert.equal(config.previewPlaceholder, true);
  assert.doesNotMatch(banner!.config, /fashion-template|FASHION_IMAGES|middleBanner/);
  assert.match(fashionDemo, /Strict isolation: never inject Template\/Sample banner/);
});

test("Store mode never substitutes Template catalog product/category/brand IDs", () => {
  const page = buildFashionTemplatePage(
    { products: [], categories: [], brands: [], articles: [], reviews: [], menus: {} },
    { origin: FASHION_STORE_ORIGIN },
  );
  const product = page.sections.find((s) => s.sectionType === "ProductCollection");
  const category = page.sections.find((s) => s.sectionType === "CategoryGrid");
  const brand = page.sections.find((s) => s.sectionType === "BrandStrip");
  assert.match(product!.config, /"productIds":\[\]/);
  assert.match(category!.config, /"categoryIds":\[\]/);
  assert.match(brand!.config, /"brandIds":\[\]/);
  assert.match(product!.config, /previewPlaceholder":true/);
});

test("missing Store content uses preview fake fill (not generic placeholder boxes)", () => {
  assert.doesNotMatch(sharedRenderer, /PreviewPlaceholderSurface/);
  assert.match(sharedRenderer, /applyPreviewFill|isStorePreviewFillEnabled/);
  assert.match(sharedRenderer, /allowHomeFallback=\{!storePreview\}/);
  assert.doesNotMatch(bannerGrid, /PreviewPlaceholderSurface/);
  assert.doesNotMatch(fashionView, /Template Catalog پایدار/);
  assert.match(fashionView, /previewStatusLabel|پیش‌نمایش با/);
});

test("preview fill is non-persistent and Store-preview gated", () => {
  assert.match(sharedRenderer, /isStorePreviewFillEnabled/);
  assert.match(landingSections, /previewSource/);
  assert.equal(previewPlaceholderCopy("banner", "fa").body.includes("بنر"), true);
  assert.doesNotMatch(fashionDemo, /INSERT INTO|previewPlaceholder.*db/i);
});

test("locale options come from language configuration API", () => {
  assert.match(workspace, /loadAdminLanguages/);
  assert.match(workspace, /template-preview-language/);
  assert.match(workspace, /fashionPreviewPath/);
  assert.doesNotMatch(workspace, /\["fa",\s*"en",\s*"ar"\]/);
  assert.match(fashionPreviewPath(false, { sourceMode: "store", locale: "en-US" }), /locale=en-US/);
  assert.match(fashionPreviewPath(true, { sourceMode: "sample", locale: "fa-IR" }), /\/full\?/);
});

test("iframe and full-page share canonical preview context", () => {
  assert.match(fashionView, /TemplatePreviewContext|context:\s*TemplatePreviewContext|previewContext/);
  assert.match(fashionView, /previewSource=\{source\}/);
  const iframePage = readFileSync(join(root, "src/frontend/app/template-preview/fashion/page.tsx"), "utf8");
  const fullPage = readFileSync(join(root, "src/frontend/app/template-preview/fashion/full/page.tsx"), "utf8");
  assert.match(iframePage, /FashionTemplatePreviewView/);
  assert.match(fullPage, /FashionTemplatePreviewView/);
  assert.match(fullPage, /deviceMode:\s*"fullPage"/);
});

test("focal-point crop contract", () => {
  assert.match(mediaAsset, /FocalPointX/);
  assert.match(mediaAsset, /FocalPointY/);
  assert.match(mediaAsset, /SetFocalPoint/);
  assert.equal(resolveObjectPosition(null, null), "50.00% 50.00%");
  assert.equal(resolveObjectPosition(0.62, 0.42), "62.00% 42.00%");
  assert.match(sharedRenderer, /resolveObjectPosition/);
  assert.match(bannerGrid, /objectPosition/);
});

test("R7 locks registered", () => {
  for (const lock of [
    "LOCK-SF-297",
    "LOCK-SF-298",
    "LOCK-SF-299",
    "LOCK-SF-300",
    "LOCK-SF-301",
    "LOCK-SF-302",
    "LOCK-SF-303",
    "LOCK-SF-304",
    "LOCK-SF-305",
  ]) {
    assert.match(locks, new RegExp(lock));
  }
});
