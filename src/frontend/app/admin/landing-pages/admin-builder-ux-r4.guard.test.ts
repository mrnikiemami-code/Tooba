import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import {
  buildFashionDemoContext,
  buildFashionDemoPage,
  FASHION_DEMO_CATEGORY_TREE_COUNT,
  FASHION_DEMO_ORIGIN,
  FASHION_DEMO_PRODUCT_COUNT,
  fashionDemoMediaUrl,
} from "../../../lib/storefront-composition/fashion-demo-preview.ts";

const dir = dirname(fileURLToPath(import.meta.url));
const root = join(dir, "../../../../../");
const workspace = readFileSync(join(dir, "admin-template-selection-workspace.tsx"), "utf8");
const fashionPage = readFileSync(join(root, "src/frontend/app/template-preview/fashion/page.tsx"), "utf8");
const fashionDemo = readFileSync(join(root, "src/frontend/lib/storefront-composition/fashion-demo-preview.ts"), "utf8");
const sharedRenderer = readFileSync(join(root, "src/frontend/lib/storefront-composition/shared-composition-renderer.tsx"), "utf8");
const landingSections = readFileSync(join(root, "src/frontend/app/storefront/storefront-landing-sections.tsx"), "utf8");
const locks = readFileSync(join(root, "docs/architecture/TOOBA-LOCKS.md"), "utf8");

test("Fashion preview route uses shared renderer/composition engine", () => {
  assert.match(fashionPage, /StorefrontLandingSections/);
  assert.match(fashionPage, /buildFashionDemoPage|buildFashionDemoContext/);
  assert.match(landingSections, /adaptLandingSectionToComposition/);
  assert.match(landingSections, /renderSharedLandingSection/);
  assert.match(sharedRenderer, /export function renderSharedLandingSection/);
  assert.doesNotMatch(fashionPage, /hardcoded Fashion renderer|FashionOnlyRenderer/i);
});

test("Fashion preview route has no Admin chrome", () => {
  assert.doesNotMatch(fashionPage, /AdminShell|admin-layout|data-testid=\"admin-/);
  assert.match(fashionPage, /StorefrontShell/);
  assert.match(fashionPage, /data-preview-safe/);
});

test("device toolbar changes iframe viewport contract, not CSS zoom", () => {
  assert.match(workspace, /FASHION_IFRAME_VIEWPORT/);
  assert.match(workspace, /fashion-preview-iframe/);
  assert.match(workspace, /width:\s*fashionViewport\.width/);
  assert.match(workspace, /height:\s*fashionViewport\.height/);
  assert.doesNotMatch(workspace, /transform:\s*scale|zoom:\s*|style=\{\{[^}]*zoom/);
  assert.match(workspace, /920/);
  assert.match(workspace, /768/);
  assert.match(workspace, /390/);
});

test("Fashion demo data count guard: 8 category trees / 15 products", () => {
  const ctx = buildFashionDemoContext();
  const roots = ctx.categories.filter((c) => c.parentCategoryId === null);
  assert.equal(roots.length, FASHION_DEMO_CATEGORY_TREE_COUNT);
  assert.equal(FASHION_DEMO_CATEGORY_TREE_COUNT, 8);
  assert.equal(ctx.products.length, FASHION_DEMO_PRODUCT_COUNT);
  assert.equal(FASHION_DEMO_PRODUCT_COUNT, 15);
  for (const rootCat of roots) {
    const mids = ctx.categories.filter((c) => c.parentCategoryId === rootCat.categoryId);
    assert.ok(mids.length >= 1, "each tree has mid level");
    const leaves = ctx.categories.filter((c) => mids.some((m) => m.categoryId === c.parentCategoryId));
    assert.ok(leaves.length >= 1, "each tree has leaf level");
  }
});

test("Fashion demo sources are traceable", () => {
  assert.equal(FASHION_DEMO_ORIGIN, "fashion-template-preview-pilot");
  assert.match(fashionDemo, /demoOrigin:\s*FASHION_DEMO_ORIGIN|demo-fashion-/);
  const page = buildFashionDemoPage(buildFashionDemoContext());
  for (const section of page.sections) {
    assert.match(section.config, /fashion-template-preview-pilot/);
  }
  assert.ok(fashionDemoMediaUrl("demo-fashion-media-1")?.startsWith("https://"));
  assert.equal(fashionDemoMediaUrl("real-asset"), null);
});

test("no destructive cleanup action", () => {
  assert.match(workspace, /cleanup-sample-data-action/);
  assert.match(workspace, /prepare-store-action/);
  assert.match(workspace, /disabled/);
  assert.match(workspace, /پس از تأیید الگوی داده نمونه فعال می‌شود/);
  assert.doesNotMatch(workspace, /fetch\(.*cleanup|DELETE.*seed|destroySample/i);
});

test("no Admin/Storefront style leakage via iframe boundary", () => {
  assert.match(workspace, /sandbox=\"allow-scripts allow-same-origin\"/);
  assert.match(workspace, /fashion-preview-iframe/);
  assert.doesNotMatch(workspace, /import .*globals\.css.*fashion|storefront-theme.*admin/i);
});

test("R4 locks registered", () => {
  for (const lock of ["LOCK-SF-280", "LOCK-SF-281", "LOCK-SF-282", "LOCK-SF-283", "LOCK-SF-284", "LOCK-SF-285"]) {
    assert.match(locks, new RegExp(lock));
  }
});
