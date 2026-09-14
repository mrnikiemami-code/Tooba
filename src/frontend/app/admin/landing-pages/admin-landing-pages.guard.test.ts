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
const api = readFileSync(join(dir, "admin-landing-pages-api.ts"), "utf8");

test("landing admin list is human-readable and hides page ids", () => {
  assert.match(list, /عنوان/);
  assert.match(list, /آدرس صفحه/);
  assert.match(list, /صفحه اصلی؟/);
  assert.match(list, /ایجاد صفحه/);
  assert.doesNotMatch(list, />شناسه صفحه<|>Page ID</);
  assert.doesNotMatch(list, /textarea.*config|JSON\.stringify\(config\)/);
});

test("composer uses typed forms and searchable pickers", () => {
  assert.match(composer, /افزودن بخش/);
  assert.match(composer, /LANDING_SECTION_CHOICES/);
  assert.match(forms, /AdminSearchableCombobox|landing-product-multi-picker|landing-category/);
  assert.match(catalog, /انتخاب دستی/);
  assert.doesNotMatch(forms, /BestSelling|Featured|Discounted/);
  assert.doesNotMatch(composer, /dangerouslySetInnerHTML|contentEditable/);
});

test("section chooser has Persian labels and no raw JSON editor", () => {
  assert.match(catalog, /بنر اصلی/);
  assert.match(catalog, /مجموعه کالا/);
  assert.doesNotMatch(api, /localStorage/);
  assert.match(api, /\/v1\/admin\/pages/);
});
