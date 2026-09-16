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
  BATCH_B_TEMPLATE_KEYS,
  industryDemoMediaUrl,
  industryTemplateImages,
  isIndustryCatalogTemplateKey,
} from "../../../lib/storefront-composition/industry-demo-media.ts";
import { industryTemplatePreviewPath } from "../../../lib/storefront-composition/template-preview-context.ts";

const dir = dirname(fileURLToPath(import.meta.url));
const root = join(dir, "../../../../../");

describe("TB-P10-T022-R12B Batch B industry templates", () => {
  it("keeps 10 distinct templates and home-appliances key", () => {
    assert.equal(listIndustryTemplates().length, 10);
    assert.ok(getIndustryTemplate("home-appliances"));
    assert.equal(getIndustryTemplate("home-appliance"), undefined);
    for (const key of BATCH_B_TEMPLATE_KEYS) {
      const template = getIndustryTemplate(key);
      assert.ok(template, key);
      const payloads = buildTemplateSectionPayloads(key);
      assert.equal(payloads.length, template!.sectionPresetList.length);
      assert.ok(isIndustryCatalogTemplateKey(key));
    }
  });

  it("uses distinct compositions across Batch B packs", () => {
    const sigs = BATCH_B_TEMPLATE_KEYS.map((key) =>
      buildTemplateSectionPayloads(key)
        .map((p) => `${p.hostType}:${p.config.variantKey}`)
        .join("|"),
    );
    assert.equal(new Set(sigs).size, 3);
  });

  it("isolates media namespaces from Fashion and Batch A", () => {
    for (const key of BATCH_B_TEMPLATE_KEYS) {
      const images = industryTemplateImages(key);
      assert.equal(images.length, 8);
      assert.ok(images.every((url) => url.includes(`/images/template-${key}/`)));
      assert.ok(images.every((url) => !url.includes("fashion-template")));
      assert.ok(images.every((url) => !url.includes("template-auto-parts")));
      const resolved = industryDemoMediaUrl(key, images[0]!);
      assert.equal(resolved, images[0]);
    }
  });

  it("preview routes use shared industry view", () => {
    for (const key of BATCH_B_TEMPLATE_KEYS) {
      const page = readFileSync(join(root, `src/frontend/app/template-preview/${key}/page.tsx`), "utf8");
      const full = readFileSync(join(root, `src/frontend/app/template-preview/${key}/full/page.tsx`), "utf8");
      assert.match(page, /IndustryTemplatePreviewView/);
      assert.match(full, /IndustryTemplatePreviewView/);
      assert.match(full, /fullPage/);
      const path = industryTemplatePreviewPath(key, false, { sourceMode: "sample", locale: "fa-IR" });
      assert.match(path, new RegExp(`/template-preview/${key}\\?`));
    }
  });

  it("selector wires live iframe for Batch B keys", () => {
    const workspace = readFileSync(
      join(root, "src/frontend/app/admin/landing-pages/admin-template-selection-workspace.tsx"),
      "utf8",
    );
    assert.match(workspace, /isIndustryCatalogTemplateKey/);
    assert.match(workspace, /tile-ceramic/);
    assert.match(workspace, /interior-decor/);
    assert.match(workspace, /home-appliances/);
    assert.doesNotMatch(workspace, /home-appliance"/);
  });
});
