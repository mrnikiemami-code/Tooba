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
  BATCH_A_TEMPLATE_KEYS,
  industryDemoMediaUrl,
  industryTemplateImages,
} from "../../../lib/storefront-composition/industry-demo-media.ts";
import { industryTemplatePreviewPath } from "../../../lib/storefront-composition/template-preview-context.ts";

const dir = dirname(fileURLToPath(import.meta.url));
const root = join(dir, "../../../../../");

describe("TB-P10-T022-R12A Batch A industry templates", () => {
  it("renames building-materials and keeps 10 distinct templates", () => {
    assert.equal(listIndustryTemplates().length, 10);
    assert.ok(getIndustryTemplate("building-materials"));
    assert.equal(getIndustryTemplate("building-supplies"), undefined);
    for (const key of BATCH_A_TEMPLATE_KEYS) {
      const template = getIndustryTemplate(key);
      assert.ok(template, key);
      const payloads = buildTemplateSectionPayloads(key);
      assert.equal(payloads.length, template!.sectionPresetList.length);
    }
  });

  it("uses distinct compositions across Batch A packs", () => {
    const sigs = BATCH_A_TEMPLATE_KEYS.map((key) =>
      buildTemplateSectionPayloads(key)
        .map((p) => `${p.hostType}:${p.config.variantKey}`)
        .join("|"),
    );
    assert.equal(new Set(sigs).size, 3);
  });

  it("isolates media namespaces from Fashion", () => {
    for (const key of BATCH_A_TEMPLATE_KEYS) {
      const images = industryTemplateImages(key);
      assert.equal(images.length, 8);
      assert.ok(images.every((url) => url.includes(`/images/template-${key}/`) || url.includes(`/images/template-${key.replace("building-materials", "building-materials")}/`)));
      assert.ok(images.every((url) => !url.includes("fashion-template")));
      const resolved = industryDemoMediaUrl(key, images[0]!);
      assert.equal(resolved, images[0]);
    }
  });

  it("preview routes use shared industry view + Host catalog endpoint pattern", () => {
    for (const key of BATCH_A_TEMPLATE_KEYS) {
      const page = readFileSync(join(root, `src/frontend/app/template-preview/${key}/page.tsx`), "utf8");
      const full = readFileSync(join(root, `src/frontend/app/template-preview/${key}/full/page.tsx`), "utf8");
      assert.match(page, /IndustryTemplatePreviewView/);
      assert.match(full, /IndustryTemplatePreviewView/);
      assert.match(full, /fullPage/);
      const path = industryTemplatePreviewPath(key, false, { sourceMode: "sample", locale: "fa-IR" });
      assert.match(path, new RegExp(`/template-preview/${key}\\?`));
    }
    const shared = readFileSync(
      join(root, "src/frontend/app/template-preview/_shared/industry-template-preview-view.tsx"),
      "utf8",
    );
    assert.match(shared, /StorefrontLandingSections/);
    assert.match(shared, /loadIndustryTemplatePreview/);
    assert.match(shared, /loadIndustryStorePreview/);
  });

  it("selector wires live iframe for Batch A keys", () => {
    const workspace = readFileSync(
      join(root, "src/frontend/app/admin/landing-pages/admin-template-selection-workspace.tsx"),
      "utf8",
    );
    assert.match(workspace, /isBatchATemplateKey/);
    assert.match(workspace, /building-materials/);
    assert.doesNotMatch(workspace, /building-supplies/);
    assert.match(workspace, /industryTemplatePreviewPath|livePreviewSrc/);
  });
});
