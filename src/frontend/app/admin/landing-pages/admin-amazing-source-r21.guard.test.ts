import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import { productSourceLabelFa, PRODUCT_SOURCE_CHOICES } from "./landing-section-catalog.ts";

const dir = dirname(fileURLToPath(import.meta.url));
const forms = readFileSync(join(dir, "admin-landing-section-forms.tsx"), "utf8");
const wizard = readFileSync(join(dir, "admin-section-wizard.tsx"), "utf8");
const catalog = readFileSync(join(dir, "landing-section-catalog.ts"), "utf8");
const capability = readFileSync(
  join(dir, "../../../lib/storefront-composition/source-capability.ts"),
  "utf8",
);
const registry = readFileSync(join(dir, "../../../lib/storefront-composition/registry.ts"), "utf8");

test("existing Manual/Category/Brand/Newest source labels preserved", () => {
  assert.equal(productSourceLabelFa("Manual"), "انتخاب دستی");
  assert.equal(productSourceLabelFa("Category"), "از یک دسته");
  assert.equal(productSourceLabelFa("Brand"), "از یک برند");
  assert.equal(productSourceLabelFa("Newest"), "جدیدترین کالاها");
});

test("Amazing option visible and maps to PromotionCampaign", () => {
  assert.ok(PRODUCT_SOURCE_CHOICES.some((c) => c.value === "PromotionCampaign" && c.label === "پیشنهاد شگفت‌انگیز"));
  assert.match(forms, /PromotionCampaign/);
  assert.match(forms, /promotionTypeCode:\s*"AMAZING"/);
  assert.match(forms, /campaignId:\s*null/);
  assert.match(capability, /case "PromotionCampaign"/);
  assert.match(capability, /پیشنهاد شگفت‌انگیز/);
  assert.match(registry, /PromotionCampaign/);
});

test("no raw technical values in normal source UI labels", () => {
  assert.doesNotMatch(capability, /return "AMAZING"|return "PromotionCampaign"/);
  assert.match(forms, /strategyLabelFa\(item\)/);
  assert.doesNotMatch(forms, /<option[^>]*>AMAZING<|>PromotionCampaign</);
  assert.match(wizard, /نوع منبع/);
  assert.match(wizard, /review-source-label/);
  assert.match(wizard, /productSourceLabelFa/);
});

test("zero-redesign: additive source wiring only", () => {
  assert.match(forms, /\["Manual", "Category", "Brand", "Newest", "PromotionCampaign"\]/);
  assert.match(catalog, /پیشنهاد شگفت‌انگیز/);
  assert.doesNotMatch(forms, /ProductSourceType\.Amazing|AmazingSourceEngine/);
});
