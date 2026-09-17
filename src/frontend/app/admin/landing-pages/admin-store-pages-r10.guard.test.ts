import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import { buildTemplateSectionPayloads, getIndustryTemplate } from "../../../lib/storefront-composition/industry-templates.ts";
import { incompleteSourceWarning } from "./landing-section-catalog.ts";

const dir = dirname(fileURLToPath(import.meta.url));
const root = join(dir, "../../../../../");
const composer = readFileSync(join(dir, "admin-landing-page-composer.tsx"), "utf8");
const api = readFileSync(join(dir, "admin-landing-pages-api.ts"), "utf8");
const catalog = readFileSync(join(dir, "landing-section-catalog.ts"), "utf8");
const hostComposer = readFileSync(join(root, "src/backend/Host/Tooba.Host/Admin/StoreLandingPageComposer.cs"), "utf8");
const hostEndpoints = readFileSync(join(root, "src/backend/Host/Tooba.Host/Admin/StoreLandingPageEndpoints.cs"), "utf8");
const locks = readFileSync(join(root, "docs/architecture/TOOBA-LOCKS.md"), "utf8");

test("Template Apply materializes full Fashion composition via replace API", () => {
  const fashion = getIndustryTemplate("fashion");
  assert.ok(fashion);
  const payloads = buildTemplateSectionPayloads("fashion");
  assert.equal(payloads.length, fashion!.sectionPresetList.length);
  assert.ok(payloads.length >= 3);
  assert.match(composer, /applyTemplateToDraft|replaceAdminLandingComposition|buildTemplateSectionPayloads/);
  assert.match(composer, /onConfirmTemplate|applyTemplateToDraft/);
  assert.match(api, /sections\/composition|replaceAdminLandingComposition/);
  assert.match(hostComposer, /ReplaceCompositionAsync/);
  assert.match(hostEndpoints, /sections\/composition/);
});

test("exact template order preserved in payloads", () => {
  const fashion = getIndustryTemplate("fashion")!;
  const payloads = buildTemplateSectionPayloads("fashion");
  for (let i = 0; i < fashion.sectionPresetList.length; i++) {
    assert.equal(
      typeof payloads[i]!.config.variantKey === "string" ? payloads[i]!.config.variantKey : null,
      fashion.sectionPresetList[i]!.variantKey,
    );
  }
});

test("repeat apply requires confirmation — no silent append", () => {
  assert.match(composer, /template-replace-confirm/);
  assert.match(composer, /جایگزین/);
  assert.match(composer, /replaceAdminLandingComposition/);
  assert.doesNotMatch(composer, /silent.?append|appendTemplate/i);
});

test("unified section workspace — geometric preview removed", () => {
  assert.match(composer, /unified-section-workspace|data-unified-section-workspace/);
  assert.match(composer, /landing-section-composer/);
  assert.doesNotMatch(composer, /page-workspace-preview/);
  assert.doesNotMatch(composer, /composition-visual-preview/);
  assert.doesNotMatch(composer, /VariantPreviewCanvas/);
  assert.doesNotMatch(composer, /workspace-mini-/);
});

test("arrow + drag/drop share canonical reorder persistence", () => {
  assert.match(composer, /persistOrder/);
  assert.match(composer, /reorderAdminLandingSections/);
  assert.match(composer, /section-drag-handle/);
  assert.match(composer, /section-move-up/);
  assert.match(composer, /section-move-down/);
  assert.match(composer, /onSectionDrop|onDragStart/);
  assert.match(composer, /data-canonical-order/);
});

test("insert below section from in-row add action", () => {
  assert.match(composer, /section-add-below-/);
  assert.match(composer, /openAddSectionWizard/);
  assert.match(composer, /insertAt/);
  assert.match(api, /insertAt/);
  assert.match(hostComposer, /InsertAt/);
  assert.doesNotMatch(composer, /insert-before-first/);
  assert.doesNotMatch(composer, /پیش‌نمایش هندسی جدا حذف شده است/);
});

test("delete and disable preserve catalog / exclude published", () => {
  assert.match(composer, /delete-section-confirm|section-delete/);
  assert.match(composer, /section-toggle-enabled|section-disabled-badge/);
  assert.match(hostComposer, /publicOnly[\s\S]*IsEnabled|Where\(x => x\.IsEnabled\)/);
  assert.match(composer, /دادهٔ کالا، دسته، برند/);
});

test("incomplete source warning helper", () => {
  assert.match(catalog, /incompleteSourceWarning/);
  assert.equal(
    incompleteSourceWarning("CategoryGrid", { categoryIds: [] }),
    "هنوز دسته‌ای انتخاب نشده است.",
  );
  assert.equal(
    incompleteSourceWarning("ProductCollection", { source: "Newest", take: 8 }),
    null,
  );
});

test("Home and Landing share same editor", () => {
  assert.match(composer, /data-page-type/);
  assert.match(composer, /pageType === "Home"|pageType: meta\.pageType/);
  assert.equal(
    readFileSync(join(dir, "new/page.tsx"), "utf8").includes("AdminLandingPageComposer")
      && readFileSync(join(dir, "[pageId]/page.tsx"), "utf8").includes("AdminLandingPageComposer"),
    true,
  );
});

test("R10 locks registered", () => {
  for (const lock of [
    "LOCK-SF-328",
    "LOCK-SF-329",
    "LOCK-SF-330",
    "LOCK-SF-331",
    "LOCK-SF-332",
    "LOCK-SF-333",
    "LOCK-SF-334",
  ]) {
    assert.match(locks, new RegExp(lock));
  }
});
