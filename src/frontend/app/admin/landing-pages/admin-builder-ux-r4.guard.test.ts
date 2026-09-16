import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import {
  FASHION_DEMO_CATEGORY_TREE_COUNT,
  FASHION_DEMO_ORIGIN,
  FASHION_DEMO_PRODUCT_COUNT,
  fashionDemoMediaUrl,
  fashionPreviewErrorMessage,
} from "../../../lib/storefront-composition/fashion-demo-preview.ts";

const dir = dirname(fileURLToPath(import.meta.url));
const root = join(dir, "../../../../../");
const workspace = readFileSync(join(dir, "admin-template-selection-workspace.tsx"), "utf8");
const fashionPage = readFileSync(join(root, "src/frontend/app/template-preview/fashion/page.tsx"), "utf8");
const fashionView = readFileSync(join(root, "src/frontend/app/template-preview/fashion/fashion-template-preview-view.tsx"), "utf8");
const fashionFullPage = readFileSync(join(root, "src/frontend/app/template-preview/fashion/full/page.tsx"), "utf8");
const fashionDemo = readFileSync(join(root, "src/frontend/lib/storefront-composition/fashion-demo-preview.ts"), "utf8");
const fashionMedia = readFileSync(join(root, "src/frontend/lib/storefront-composition/fashion-demo-media.ts"), "utf8");
const sharedRenderer = readFileSync(join(root, "src/frontend/lib/storefront-composition/shared-composition-renderer.tsx"), "utf8");
const landingSections = readFileSync(join(root, "src/frontend/app/storefront/storefront-landing-sections.tsx"), "utf8");
const locks = readFileSync(join(root, "docs/architecture/TOOBA-LOCKS.md"), "utf8");
const catalogDomain = readFileSync(
  join(root, "src/backend/Modules/Catalog/Tooba.Catalog.Domain/TemplateCatalog.cs"),
  "utf8",
);
const productEntity = readFileSync(
  join(root, "src/backend/Modules/Catalog/Tooba.Catalog.Domain/CatalogDomain.cs"),
  "utf8",
);

test("Fashion preview route uses shared renderer and Template Catalog loader", () => {
  assert.match(fashionView, /StorefrontLandingSections/);
  assert.match(fashionView, /loadFashionTemplatePreview/);
  assert.match(fashionView, /loadFashionStorePreview/);
  assert.match(fashionDemo, /loadFashionStorePreview/);
  assert.match(fashionDemo, /buildFashionTemplatePage/);
  assert.doesNotMatch(fashionView, /buildFashionDemoContext\(\)/);
  assert.doesNotMatch(fashionView, /StorefrontHomePage|loadStorefrontHomeSelection|ShopeivaHome/);
  assert.match(landingSections, /adaptLandingSectionToComposition/);
  assert.match(landingSections, /renderSharedLandingSection/);
  assert.match(sharedRenderer, /export function renderSharedLandingSection/);
});

test("Fashion preview route has no Admin chrome", () => {
  assert.doesNotMatch(fashionView, /AdminShell|admin-layout|data-testid=\"admin-/);
  assert.match(fashionView, /StorefrontShell/);
  assert.match(fashionView, /data-preview-safe/);
  assert.match(fashionPage, /FashionTemplatePreviewView/);
  assert.match(fashionFullPage, /FashionTemplatePreviewView/);
  assert.match(fashionFullPage, /fullPage/);
});

test("device toolbar changes iframe viewport contract, not CSS zoom", () => {
  assert.match(workspace, /FASHION_IFRAME_VIEWPORT/);
  assert.match(workspace, /fashion-preview-iframe/);
  assert.match(workspace, /width:\s*fashionViewport\.width/);
  assert.match(workspace, /height:\s*fashionViewport\.height/);
  assert.doesNotMatch(workspace, /transform:\s*scale|zoom:\s*|style=\{\{[^}]*zoom/);
});

test("Fashion Template Catalog count constants remain 8 trees / 15 products", () => {
  assert.equal(FASHION_DEMO_CATEGORY_TREE_COUNT, 8);
  assert.equal(FASHION_DEMO_PRODUCT_COUNT, 15);
  assert.match(fashionDemo, /loadFashionTemplatePreview/);
  assert.match(fashionDemo, /In-memory Fashion demo removed/);
});

test("Fashion sample origin is persisted Template Catalog", () => {
  assert.equal(FASHION_DEMO_ORIGIN, "fashion-template-catalog-persisted");
  assert.match(fashionDemo, /origin:\s*FASHION_DEMO_ORIGIN/);
  assert.match(fashionDemo, /demoOrigin:\s*options\.origin/);
  assert.match(fashionDemo, /FASHION_STORE_ORIGIN/);
  assert.match(fashionDemo, /operational-store-catalog/);
  assert.match(fashionDemo, /loadStorefrontListing/);
  assert.match(fashionDemo, /loadStorefrontCategories/);
  assert.match(fashionDemo, /loadStorefrontBrands/);
  assert.doesNotMatch(fashionDemo, /loadStorefrontHome/);
  assert.match(fashionDemo, /fashionPreviewErrorMessage/);
  assert.match(fashionDemo, /store\.data\.unavailable/);
  assert.doesNotMatch(fashionDemo, /Operational store home is unavailable/);
  assert.match(fashionView, /fashionPreviewErrorMessage/);
  assert.match(fashionView, /previewLocale|TemplatePreviewContext|previewStatusLabel/);
  assert.match(fashionMedia, /\/images\/fashion-template\//);
  assert.ok(fashionDemoMediaUrl("demo-fashion-media-1")?.startsWith("/images/fashion-template/"));
  assert.ok(
    fashionDemoMediaUrl("019022a5-0000-7000-8000-00000000a001")?.startsWith("/images/fashion-template/"),
  );
  assert.equal(fashionDemoMediaUrl("real-asset"), null);
  assert.doesNotMatch(fashionMedia, /images\.unsplash\.com/);
});

test("operational Product schema has no template ownership columns", () => {
  assert.match(productEntity, /public sealed class CatalogProduct/);
  assert.doesNotMatch(productEntity, /class CatalogProduct[\s\S]*TemplateId/);
  assert.doesNotMatch(productEntity, /IsDemo|DataScope|SeedBatchId/);
  assert.match(catalogDomain, /class TemplateProduct/);
  assert.match(catalogDomain, /TemplateId/);
});

test("no destructive cleanup action", () => {
  assert.match(workspace, /cleanup-sample-data-action/);
  assert.match(workspace, /prepare-store-action/);
  assert.match(workspace, /disabled/);
  assert.match(workspace, /load-sample-data-action/);
  assert.match(workspace, /load-store-data-action/);
  assert.match(workspace, /template-open-full-page/);
  assert.match(workspace, /template-preview\/fashion\/full/);
  assert.match(workspace, /بارگذاری از داده‌های نمونه/);
  assert.match(workspace, /بارگذاری از داده‌های فروشگاه/);
  assert.match(workspace, /نمایش در یک صفحه/);
});

test("Fashion preview errors are bilingual", () => {
  assert.match(fashionPreviewErrorMessage("store.data.unavailable", "fa"), /فروشگاه/);
  assert.match(fashionPreviewErrorMessage("store.data.unavailable", "en"), /Store catalog/);
  assert.match(fashionPreviewErrorMessage("sample.preview.failed", "fa"), /Template Catalog/);
  assert.match(fashionPreviewErrorMessage("sample.preview.failed", "en"), /Template Catalog/);
  assert.doesNotMatch(fashionPreviewErrorMessage("store.data.unavailable", "fa"), /Operational store home/);
});

test("R4+R5+R6 locks registered", () => {
  for (const lock of [
    "LOCK-SF-280",
    "LOCK-SF-285",
    "LOCK-SF-286",
    "LOCK-SF-287",
    "LOCK-SF-288",
    "LOCK-SF-289",
    "LOCK-SF-290",
    "LOCK-SF-291",
    "LOCK-SF-292",
    "LOCK-SF-293",
    "LOCK-SF-294",
    "LOCK-SF-295",
    "LOCK-SF-296",
  ]) {
    assert.match(locks, new RegExp(lock));
  }
});
