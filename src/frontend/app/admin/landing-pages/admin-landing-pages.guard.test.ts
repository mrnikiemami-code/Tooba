import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const dir = dirname(fileURLToPath(import.meta.url));
const list = readFileSync(join(dir, "admin-landing-pages-screen.tsx"), "utf8");
const composer = readFileSync(join(dir, "admin-landing-page-composer.tsx"), "utf8");
const forms = readFileSync(join(dir, "admin-landing-section-forms.tsx"), "utf8");
const catalog = readFileSync(join(dir, "landing-section-catalog.ts"), "utf8");
const compositionCatalog = readFileSync(join(dir, "admin-composition-catalog.ts"), "utf8");
const api = readFileSync(join(dir, "admin-landing-pages-api.ts"), "utf8");
const wizard = readFileSync(join(dir, "admin-section-wizard.tsx"), "utf8");

test("landing admin list is human-readable and hides page ids", () => {
  assert.match(list, /عنوان/);
  assert.match(list, /آدرس صفحه/);
  assert.match(list, /صفحه اصلی؟/);
  assert.match(list, /ایجاد صفحه/);
  assert.doesNotMatch(list, />شناسه صفحه<|>Page ID</);
  assert.doesNotMatch(list, /textarea.*config|JSON\.stringify\(config\)/);
});

test("composer uses wizard + resource selector (no fake sources)", () => {
  assert.match(composer, /افزودن بخش/);
  assert.match(composer, /AdminSectionWizard|LANDING_SECTION_CHOICES|adminSelectableSectionTypes/);
  assert.match(composer, /شروع از صفحه خالی|start-blank/);
  assert.match(composer, /شروع از قالب آماده|start-from-template|template-picker/);
  assert.match(wizard, /composition-variant-picker|edit-variant-picker/);
  assert.match(forms, /AdminResourceSelector|landing-product-multi-picker|landing-category/);
  assert.match(forms, /banner-slot-editor|story-display-only-hint/);
  assert.match(catalog, /انتخاب دستی/);
  assert.match(catalog, /StoryRail|BannerShowcase/);
  assert.doesNotMatch(forms, /BestSelling|Featured|Discounted/);
  assert.doesNotMatch(compositionCatalog, /BestSelling|Featured|Discounted|MostViewed|HotTrending/);
  assert.doesNotMatch(composer, /dangerouslySetInnerHTML|contentEditable|BestSelling/);
});

test("section chooser has Persian labels and no raw JSON editor", () => {
  assert.match(catalog, /بنر اصلی/);
  assert.match(catalog, /مجموعه کالا/);
  assert.match(compositionCatalog, /nameFa|اسلایدر|نمایش کالا/);
  assert.match(wizard, /data-variant-key/);
  assert.doesNotMatch(api, /localStorage/);
  assert.match(api, /\/v1\/admin\/pages/);
});

test("TB-P10-T022 ordinary-user Admin UX: no technical leakage + empty category/brand hints", () => {
  assert.doesNotMatch(composer, />SectionType<|>VariantKey<|>ResponsiveContract</);
  assert.doesNotMatch(composer, /breakpoint|JSON\.stringify|customCss/i);
  assert.match(forms, /empty-state-category-source/);
  assert.match(forms, /empty-state-brand-source/);
  assert.match(forms, /empty-state-product-source/);
  assert.match(catalog, /بدون قالب‌بندی خام/);
  assert.doesNotMatch(catalog, /بدون HTML/);
});
