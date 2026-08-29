import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import {
  buildPublishChecklist,
  formatProductLifecycleLabelFa,
  mapPublishReadiness,
  type ProductPublishReadiness,
} from "./product-publishing-panel-model.ts";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)));

function sampleReadiness(partial: Partial<ProductPublishReadiness> = {}): ProductPublishReadiness {
  return {
    isReady: false,
    categoryReady: true,
    translationReady: true,
    attributeReady: false,
    variantReady: true,
    mediaReady: false,
    seoReady: true,
    missingRequirements: [
      { code: "attributes", messageFa: "ویژگی‌های الزامی تکمیل نشده است.", workspaceTab: "attributes" },
      { code: "media", messageFa: "تصویر اصلی تعیین نشده است.", workspaceTab: "media" },
    ],
    messageFa: "برای انتشار، ۲ مورد دیگر باید تکمیل شود.",
    ...partial,
  };
}

test("lifecycle labels are Persian", () => {
  assert.equal(formatProductLifecycleLabelFa("Draft"), "پیش‌نویس");
  assert.equal(formatProductLifecycleLabelFa("Published"), "منتشرشده");
  assert.equal(formatProductLifecycleLabelFa("Archived"), "بایگانی‌شده");
});

test("checklist maps readiness and missing navigation tabs", () => {
  const checklist = buildPublishChecklist(sampleReadiness());
  assert.equal(checklist.length, 6);
  assert.equal(checklist.find((i) => i.code === "attributes")?.ready, false);
  assert.equal(checklist.find((i) => i.code === "attributes")?.workspaceTab, "attributes");
  assert.equal(checklist.find((i) => i.code === "media")?.ready, false);
  assert.equal(checklist.find((i) => i.code === "seo")?.ready, true);
});

test("mapPublishReadiness accepts PascalCase Host payload", () => {
  const mapped = mapPublishReadiness({
    IsReady: true,
    CategoryReady: true,
    TranslationReady: true,
    AttributeReady: true,
    VariantReady: true,
    MediaReady: true,
    SeoReady: true,
    MissingRequirements: [],
    MessageFa: "محصول برای انتشار آماده است.",
  });
  assert.ok(mapped);
  assert.equal(mapped!.isReady, true);
  assert.equal(mapped!.messageFa, "محصول برای انتشار آماده است.");
});

test("publishing panel and host-client wiring contracts", () => {
  const panel = fs.readFileSync(path.join(root, "product-publishing-panel.tsx"), "utf8");
  const screen = fs.readFileSync(path.join(root, "product-workspace-screen.tsx"), "utf8");
  const client = fs.readFileSync(path.join(root, "host-client.ts"), "utf8");

  assert.match(panel, /publish-readiness-checklist/);
  assert.match(panel, /publish-view-only-note/);
  assert.match(panel, /window\.confirm/);
  assert.match(panel, /min-h-11/);
  assert.match(panel, /بازگردانی به پیش‌نویس/);
  assert.match(screen, /ProductPublishingPanel/);
  assert.match(screen, /sectionId === "publication"/);
  assert.doesNotMatch(screen, /AgGridReact/);
  assert.match(client, /publish\/readiness/);
  assert.match(client, /getAdminProductPublishReadiness/);
  assert.match(client, /"restore"/);
  assert.doesNotMatch(panel, /Product\.Price|Product\.Stock/);
});
