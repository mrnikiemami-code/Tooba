import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const dir = dirname(fileURLToPath(import.meta.url));
const list = readFileSync(join(dir, "admin-landing-pages-screen.tsx"), "utf8");
const composer = readFileSync(join(dir, "admin-landing-page-composer.tsx"), "utf8");
const wizard = readFileSync(join(dir, "admin-section-wizard.tsx"), "utf8");
const forms = readFileSync(join(dir, "admin-landing-section-forms.tsx"), "utf8");
const previews = readFileSync(join(dir, "layout-aware-previews.tsx"), "utf8");
const workspace = readFileSync(join(dir, "admin-template-selection-workspace.tsx"), "utf8");
const locks = readFileSync(join(dir, "../../../../../docs/architecture/TOOBA-LOCKS.md"), "utf8");

test("Landing Pages uses canonical AppDataGrid profile", () => {
  assert.match(list, /AppDataGrid/);
  assert.match(list, /ORDERS_LIKE_CAPABILITIES|DEFAULT_APP_GRID_CAPABILITIES/);
  assert.match(list, /AppGridRowActionsCell/);
  assert.match(list, /createClientGridQueryAdapter/);
  assert.match(list, /orders-canonical/);
  assert.doesNotMatch(list, /<table[\s>]/);
});

test("Landing Operations pinned/icon-only", () => {
  assert.match(list, /AppGridRowActionsCell/);
  assert.match(list, /icon: Pencil/);
  assert.match(list, /icon: Eye/);
  assert.doesNotMatch(list, /rounded-lg border px-2 py-1.*ویرایش/);
});

test("Blank page can add first section", () => {
  assert.match(composer, /openAddSectionWizard/);
  assert.match(composer, /add-first-section-cta|افزودن اولین بخش/);
  assert.match(composer, /openWizardAfter/);
  assert.doesNotMatch(composer, /disabled=\{!page \|\| busy\}/);
});

test("Wizard shell stable across steps", () => {
  assert.match(wizard, /section-wizard-shell|data-wizard-shell="fixed"/);
  assert.match(wizard, /h-\[min\(90vh,720px\)\]/);
  assert.match(wizard, /shrink-0/);
  assert.match(wizard, /data-step-state/);
});

test("Review uses variant-aware preview metadata", () => {
  assert.match(wizard, /data-review-variant-aware/);
  assert.match(wizard, /review-plain-summary/);
  assert.match(wizard, /VariantPreviewCanvas/);
});

test("Template preview structural distinctness", () => {
  assert.match(previews, /data-template-preview-v2|template-preview-v2|layoutAwareTemplatePreview/);
  assert.match(previews, /industryTone/);
  assert.match(composer, /AdminTemplateSelectionWorkspace|use-selected-template/);
  assert.match(workspace, /data-template-preview-v2|template-device-toolbar|industryWireframeBlocks/);
  assert.match(workspace, /template-industry-photo|INDUSTRY_TEMPLATE_PHOTO/);
  assert.doesNotMatch(workspace, /template-composition-miniature/);
  assert.match(workspace, /use-selected-template/);
});

test("Banner editor geometry matches variant metadata", () => {
  assert.match(previews, /bannerSlotLayoutClass/);
  assert.match(previews, /banner\.two-equal/);
  assert.match(previews, /banner\.mosaic-2x2/);
  assert.match(forms, /banner-slot-editor/);
});

test("Brand Manual uses canonical Resource Selector", () => {
  assert.match(forms, /data-brand-source/);
  assert.match(forms, /family="brands"/);
  assert.match(forms, /AdminResourceSelector|ManualResourceField/);
  assert.match(forms, /brand-source-strategy/);
});

test("R2 locks registered", () => {
  for (const lock of [
    "LOCK-SF-266",
    "LOCK-SF-267",
    "LOCK-SF-268",
    "LOCK-SF-269",
    "LOCK-SF-270",
    "LOCK-SF-271",
    "LOCK-SF-272",
    "LOCK-SF-273",
  ]) {
    assert.match(locks, new RegExp(lock));
  }
});
