import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)));
const panel = fs.readFileSync(path.join(root, "category-attributes-panel.tsx"), "utf8");

test("humanizeAttributeCode and attributeCodeFromLabel helpers exist", () => {
  assert.match(panel, /export function humanizeAttributeCode/);
  assert.match(panel, /export function attributeCodeFromLabel/);
  assert.match(panel, /replace\(\/\[-_\.\]\+\/g/);
});

test("local vs inherited partition helpers exist", () => {
  assert.match(panel, /export function isLocalSchemaEntry/);
  assert.match(panel, /export function partitionEffectiveSchema/);
  assert.match(panel, /inheritedFromCategoryId/);
});

test("ordinary-user Persian flag labels are defined", () => {
  assert.match(panel, /برای محصولات این دسته باید مقدار داشته باشد/);
  assert.match(panel, /مشتری می‌تواند در صفحه دسته بر اساس این ویژگی فیلتر کند/);
  assert.match(panel, /می‌تواند برای ساخت تنوع‌های محصول مثل رنگ یا سایز استفاده شود/);
  assert.match(panel, /در جدول مقایسه محصولات نمایش داده می‌شود/);
  assert.match(panel, /Products in this category must have a value/);
});

test("behavior chips are independent toggles not checkboxes", () => {
  assert.match(panel, /attr-behavior-chips/);
  assert.match(panel, /attr-behavior-explanations/);
  assert.match(panel, /role="switch"/);
  assert.match(panel, /ATTRIBUTE_FLAG_CHIP_LABELS/);
  assert.match(panel, /fa:\s*"الزامی"/);
  assert.match(panel, /fa:\s*"فیلتر"/);
  assert.match(panel, /fa:\s*"تنوع"/);
  assert.match(panel, /fa:\s*"مقایسه"/);
  assert.equal(/attr-flag-required[\s\S]{0,120}type="checkbox"/.test(panel), false);
  assert.match(panel, /mapAdminErrorMessage/);
  assert.equal(panel.includes("فیلتر, تنوع"), false);
});

test("inherited vs category-specific human sections", () => {
  assert.match(panel, /ویژگی‌های به‌ارث‌رسیده/);
  assert.match(panel, /ویژگی‌های مخصوص این دسته/);
  assert.match(panel, /category-attributes-inherited-help/);
  assert.match(panel, /از دسته‌های والد به ارث رسیده‌اند/);
});

test("inherited override UX copy and reset-to-parent", () => {
  assert.match(panel, /به ارث رسیده از \{/);
  assert.match(panel, /تنظیم اختصاصی برای این دسته/);
  assert.match(panel, /تنظیم اختصاصی/);
  assert.match(panel, /بازگشت به تنظیمات والد/);
  assert.match(panel, /attr-badge-local-override/);
  assert.match(panel, /attr-reset-override-/);
  assert.match(panel, /isLocalOverride/);
  assert.match(panel, /DUPLICATE_INHERITED_ATTRIBUTE_MESSAGE/);
  assert.match(
    panel,
    /این ویژگی از قبل از دسته والد به ارث رسیده است\. در صورت نیاز تنظیمات استفاده آن را برای این دسته تغییر دهید\./,
  );
  assert.match(panel, /attr-add-duplicate-inherited/);
});

test("create+bind strips invalid variant axis before API", () => {
  assert.match(panel, /const variantAllowed/);
  assert.match(panel, /createKind === "Enumeration" \|\| createKind === "Number"/);
  assert.match(panel, /isVariantAxisAllowed: variantAllowed/);
});
