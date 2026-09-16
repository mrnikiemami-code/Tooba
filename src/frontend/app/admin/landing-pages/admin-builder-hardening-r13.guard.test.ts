import assert from "node:assert/strict";
import { existsSync, readdirSync, readFileSync, statSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, it } from "node:test";
import {
  buildTemplateSectionPayloads,
  getIndustryTemplate,
  listIndustryTemplates,
} from "../../../lib/storefront-composition/industry-templates.ts";
import {
  industryDemoMediaUrl,
  industryTemplateImages,
  isIndustryCatalogTemplateKey,
} from "../../../lib/storefront-composition/industry-demo-media.ts";
import { industryTemplatePreviewPath } from "../../../lib/storefront-composition/template-preview-context.ts";
import { PREVIEW_FAKE_ASSET_ROOT } from "../../../lib/storefront-composition/preview-fake-media.ts";
import { VARIANTS } from "../../../lib/storefront-composition/registry.ts";

const dir = dirname(fileURLToPath(import.meta.url));
const root = join(dir, "../../../../../");
const evidence = join(root, "docs/evidence/TB-P10-T022-R13");

const ALL_KEYS = [
  "fashion",
  "auto-parts",
  "building-materials",
  "tools-hardware",
  "tile-ceramic",
  "interior-decor",
  "home-appliances",
  "shoes",
  "plants",
  "beauty",
] as const;

const MEDIA_PREFIX: Record<(typeof ALL_KEYS)[number], string> = {
  fashion: "/images/fashion-template/",
  "auto-parts": "/images/template-auto-parts/",
  "building-materials": "/images/template-building-materials/",
  "tools-hardware": "/images/template-tools-hardware/",
  "tile-ceramic": "/images/template-tile-ceramic/",
  "interior-decor": "/images/template-interior-decor/",
  "home-appliances": "/images/template-home-appliances/",
  shoes: "/images/template-shoes/",
  plants: "/images/template-plants/",
  beauty: "/images/template-beauty/",
};

const REQUIRED_SHOTS = [
  "store-pages-grid-final.png",
  "template-selector-all-10.png",
  "fashion-preview-final.png",
  "auto-parts-preview-final.png",
  "interior-decor-preview-final.png",
  "beauty-preview-final.png",
  "sample-store-mode-difference.png",
  "use-template-populated-editor.png",
  "editor-reorder-controls-final.png",
  "editor-insert-between-final.png",
  "editor-disabled-section-final.png",
  "variant-picker-real-components-final.png",
  "variant-carousel-before-final.png",
  "variant-carousel-after-final.png",
  "review-step-real-component-final.png",
  "home-seo-final.png",
  "landing-seo-final.png",
  "landing-route-final.png",
  "home-route-final.png",
  "mobile-preview-final.png",
];

describe("TB-P10-T022-R13 Final Builder Hardening", () => {
  it("selector lists all 10 industry packs with apply payloads", () => {
    const listed = listIndustryTemplates();
    assert.equal(listed.length, 10);
    for (const key of ALL_KEYS) {
      const template = getIndustryTemplate(key);
      assert.ok(template, key);
      assert.ok(template!.nameFa.trim().length > 0, key);
      assert.ok(template!.descriptionFa.trim().length > 0, key);
      const payloads = buildTemplateSectionPayloads(key);
      assert.ok(payloads.length >= 4, `${key} section count`);
      assert.equal(payloads.length, template!.sectionPresetList.length);
      if (key !== "fashion") assert.ok(isIndustryCatalogTemplateKey(key));
    }
  });

  it("isolates media namespaces across all packs and PreviewFake", () => {
    for (const key of ALL_KEYS) {
      const prefix = MEDIA_PREFIX[key];
      const images =
        key === "fashion"
          ? Array.from({ length: 8 }, (_, i) => `/images/fashion-template/${i + 1}.jpg`)
          : industryTemplateImages(key);
      assert.equal(images.length, 8, key);
      assert.ok(images.every((url) => url.startsWith(prefix)), key);
      assert.ok(images.every((url) => !url.includes(PREVIEW_FAKE_ASSET_ROOT)), key);
      for (const other of ALL_KEYS) {
        if (other === key) continue;
        const otherPrefix = MEDIA_PREFIX[other];
        assert.ok(images.every((url) => !url.startsWith(otherPrefix)), `${key} vs ${other}`);
      }
      if (key !== "fashion") {
        assert.equal(industryDemoMediaUrl(key, images[0]!), images[0]);
      }
    }
    assert.match(PREVIEW_FAKE_ASSET_ROOT, /preview-placeholder/);
  });

  it("preview routes resolve via shared engines for all 10 keys", () => {
    for (const key of ALL_KEYS) {
      if (key === "fashion") {
        assert.ok(existsSync(join(root, "src/frontend/app/template-preview/fashion/page.tsx")));
        continue;
      }
      const page = readFileSync(join(root, `src/frontend/app/template-preview/${key}/page.tsx`), "utf8");
      const full = readFileSync(join(root, `src/frontend/app/template-preview/${key}/full/page.tsx`), "utf8");
      assert.match(page, /IndustryTemplatePreviewView/);
      assert.match(full, /IndustryTemplatePreviewView/);
      assert.match(
        industryTemplatePreviewPath(key, false, { sourceMode: "sample", locale: "fa-IR" }),
        new RegExp(`/template-preview/${key}\\?`),
      );
    }
  });

  it("removes geometric VariantPreviewCanvas remnant", () => {
    const previews = readFileSync(join(root, "src/frontend/app/admin/landing-pages/layout-aware-previews.tsx"), "utf8");
    assert.doesNotMatch(previews, /export function VariantPreviewCanvas/);
    assert.doesNotMatch(previews, /variant-preview-canvas/);
    const composer = readFileSync(join(root, "src/frontend/app/admin/landing-pages/admin-landing-page-composer.tsx"), "utf8");
    assert.doesNotMatch(composer, /VariantPreviewCanvas/);
    const workspace = readFileSync(
      join(root, "src/frontend/app/admin/landing-pages/admin-template-selection-workspace.tsx"),
      "utf8",
    );
    assert.match(workspace, /fashion-preview-iframe/);
    assert.match(workspace, /isLiveCatalogPreview/);
  });

  it("keeps every implemented Variant human-named", () => {
    for (const v of VARIANTS.filter((row) => row.implemented)) {
      assert.match(v.nameFa, /طرح/);
      assert.ok((v.descriptionFa ?? "").trim().length > 0, v.key);
    }
  });

  it("locks 357–362 exist after 356", () => {
    const locks = readFileSync(join(root, "docs/architecture/TOOBA-LOCKS.md"), "utf8");
    for (const id of [357, 358, 359, 360, 361, 362]) {
      assert.match(locks, new RegExp(`### LOCK-SF-${id}`));
    }
    const idx356 = locks.indexOf("### LOCK-SF-356");
    const idx357 = locks.indexOf("### LOCK-SF-357");
    assert.ok(idx356 >= 0 && idx357 > idx356);
  });

  it("evidence pack lists required screenshots when present", () => {
    const shotsDir = join(evidence, "screenshots");
    if (!existsSync(shotsDir)) return;
    const files = readdirSync(shotsDir);
    for (const name of REQUIRED_SHOTS) {
      if (!files.includes(name)) continue;
      assert.ok(statSync(join(shotsDir, name)).size > 800, name);
    }
  });
});
