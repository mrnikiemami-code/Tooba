import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import { validateWizardStep, wizardStepsForHost } from "./admin-section-wizard-logic.ts";
import { sourceCapabilityForVariant, truthfulStrategiesForVariant } from "../../../lib/storefront-composition/source-capability.ts";

const dir = path.dirname(fileURLToPath(import.meta.url));
const composer = fs.readFileSync(path.join(dir, "admin-landing-page-composer.tsx"), "utf8");
const forms = fs.readFileSync(path.join(dir, "admin-landing-section-forms.tsx"), "utf8");
const wizard = fs.readFileSync(path.join(dir, "admin-section-wizard.tsx"), "utf8");
const selector = fs.readFileSync(path.join(dir, "admin-resource-selector.tsx"), "utf8");
const previews = fs.readFileSync(path.join(dir, "layout-aware-previews.tsx"), "utf8");

test("section wizard replaces modal+drawer fragmentation", () => {
  assert.match(composer, /AdminSectionWizard/);
  assert.match(wizard, /section-wizard/);
  assert.match(wizard, /section-wizard-steps/);
  assert.doesNotMatch(composer, /landing-section-drawer/);
  assert.doesNotMatch(composer, /add-section-chooser/);
});

test("page language is page-level only", () => {
  assert.match(composer, /page-language-create/);
  assert.match(composer, /page-language-edit-fixed|page-language-fixed/);
  assert.doesNotMatch(forms, /زبان بخش|section-language|locale.*section/i);
  assert.doesNotMatch(wizard, /زبان بخش/);
});

test("story builder is display-settings only", () => {
  assert.match(forms, /story-display-only-hint|data-story-display-settings/);
  assert.doesNotMatch(forms, /افزودن استوری/);
  assert.doesNotMatch(forms, /story-items-editor/);
});

test("resource selector uses orders-canonical AppDataGrid profile", () => {
  assert.match(selector, /orders-canonical/);
  assert.match(selector, /AppDataGrid/);
  assert.match(selector, /DEFAULT_APP_GRID_CAPABILITIES|ORDERS_LIKE_CAPABILITIES/);
  assert.match(selector, /resource-selector-tab-selected/);
  assert.match(selector, /AppGridRowActionsCell/);
});

test("wizard step validation covers source requirements", () => {
  assert.equal(validateWizardStep("type", { config: {} }), "یک نوع بخش انتخاب کنید.");
  assert.equal(
    validateWizardStep("source", {
      hostType: "ProductCollection",
      config: { source: "Manual", productIds: [] },
    }),
    "حداقل یک کالا انتخاب کنید.",
  );
  assert.deepEqual(wizardStepsForHost("StoryRail"), ["type", "variant", "settings", "preview"]);
  assert.ok(wizardStepsForHost("ProductCollection").includes("source"));
});

test("truthful dynamic sources exclude HeuristicHomeOnly kinds", () => {
  const strategies = truthfulStrategiesForVariant("product.grid");
  assert.ok(strategies.includes("Newest"));
  assert.ok(strategies.includes("Manual"));
  assert.ok(!strategies.includes("BestSelling" as never));
  assert.ok(!strategies.includes("MostViewed" as never));
  const article = sourceCapabilityForVariant("article.grid");
  assert.ok(article?.strategies.includes("LatestArticles"));
  assert.ok(article?.manualSupported);
});

test("template/variant previews: banner slots stay layout-aware; picker uses live components", () => {
  assert.match(previews, /layoutAwareVariantPreview/);
  assert.match(previews, /banner\.two-equal/);
  assert.match(previews, /rounded-full/);
  assert.match(previews, /data-layout-aware/);
  // R10 removed geometric composition miniature from editor; R11 variant picker is live.
  assert.doesNotMatch(composer, /VariantPreviewCanvas/);
  assert.match(wizard, /VariantLivePreview|data-variant-picker-v2/);
});

test("operations column metadata remains icon-only tooltip pattern", () => {
  assert.match(selector, /AppGridRowActionsCell/);
  assert.match(selector, /compact/);
  assert.match(selector, /label:/);
});
