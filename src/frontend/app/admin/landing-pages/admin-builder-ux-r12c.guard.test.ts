import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, it } from "node:test";
import {
  buildTemplateSectionPayloads,
  getIndustryTemplate,
  listIndustryTemplates,
} from "../../../lib/storefront-composition/industry-templates.ts";
import {
  BATCH_C_TEMPLATE_KEYS,
  industryDemoMediaUrl,
  industryTemplateImages,
  isIndustryCatalogTemplateKey,
} from "../../../lib/storefront-composition/industry-demo-media.ts";
import { industryTemplatePreviewPath } from "../../../lib/storefront-composition/template-preview-context.ts";

const dir = dirname(fileURLToPath(import.meta.url));
const root = join(dir, "../../../../../");

describe("TB-P10-T022-R12C Batch C industry templates", () => {
  it("keeps 10 distinct templates including shoes/plants/beauty", () => {
    assert.equal(listIndustryTemplates().length, 10);
    for (const key of BATCH_C_TEMPLATE_KEYS) {
      const template = getIndustryTemplate(key);
      assert.ok(template, key);
      const payloads = buildTemplateSectionPayloads(key);
      assert.equal(payloads.length, template!.sectionPresetList.length);
      assert.ok(isIndustryCatalogTemplateKey(key));
    }
  });

  it("uses distinct compositions across Batch C packs", () => {
    const sigs = BATCH_C_TEMPLATE_KEYS.map((key) =>
      buildTemplateSectionPayloads(key)
        .map((p) => `${p.hostType}:${p.config.variantKey}`)
        .join("|"),
    );
    assert.equal(new Set(sigs).size, 3);
  });

  it("isolates media namespaces from Fashion and prior batches", () => {
    for (const key of BATCH_C_TEMPLATE_KEYS) {
      const images = industryTemplateImages(key);
      assert.equal(images.length, 8);
      assert.ok(images.every((url) => url.includes(`/images/template-${key}/`)));
      assert.ok(images.every((url) => !url.includes("fashion-template")));
      assert.ok(images.every((url) => !url.includes("template-auto-parts")));
      assert.ok(images.every((url) => !url.includes("template-tile-ceramic")));
      assert.equal(industryDemoMediaUrl(key, images[0]!), images[0]);
    }
  });

  it("preview routes use shared industry view", () => {
    for (const key of BATCH_C_TEMPLATE_KEYS) {
      const page = readFileSync(join(root, `src/frontend/app/template-preview/${key}/page.tsx`), "utf8");
      const full = readFileSync(join(root, `src/frontend/app/template-preview/${key}/full/page.tsx`), "utf8");
      assert.match(page, /IndustryTemplatePreviewView/);
      assert.match(full, /IndustryTemplatePreviewView/);
      assert.match(full, /fullPage/);
      assert.match(industryTemplatePreviewPath(key, false, { sourceMode: "sample", locale: "fa-IR" }), new RegExp(`/template-preview/${key}\\?`));
    }
  });

  it("selector wires live iframe for Batch C keys", () => {
    const workspace = readFileSync(
      join(root, "src/frontend/app/admin/landing-pages/admin-template-selection-workspace.tsx"),
      "utf8",
    );
    assert.match(workspace, /isIndustryCatalogTemplateKey/);
    assert.match(workspace, /shoes/);
    assert.match(workspace, /plants/);
    assert.match(workspace, /beauty/);
  });

  it("beauty seed/demo copy avoids medical-therapeutic claims", () => {
    const seed = readFileSync(
      join(root, "src/backend/Host/Tooba.Host/Admin/IndustryBatchCTemplateCatalogSeed.cs"),
      "utf8",
    );
    const beautySlice = seed.slice(seed.indexOf("CreateBeautyPack"), seed.indexOf("IndustryBatchCTemplateCatalogSeedHost"));
    assert.doesNotMatch(beautySlice, /درمان|دارو|پزشکی|therapeutic|medical|cure|heal/i);
    assert.match(beautySlice, /ژل شستشوی صورت ملایم/);
  });
});
