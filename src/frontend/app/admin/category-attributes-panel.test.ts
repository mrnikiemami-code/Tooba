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
  assert.match(panel, /required:\s*"برای ثبت محصول الزامی است"/);
  assert.match(panel, /filterable:\s*"نمایش در فیلتر محصولات"/);
  assert.match(panel, /variant:\s*"برای ساخت تنوع محصول"/);
  assert.match(panel, /comparable:\s*"نمایش در مقایسه محصولات"/);
});

test("behavior chips are independent toggles not checkboxes", () => {
  assert.match(panel, /attr-behavior-chips/);
  assert.match(panel, /role="switch"/);
  assert.match(panel, /ATTRIBUTE_FLAG_CHIP_LABELS/);
  assert.match(panel, /required:\s*"الزامی"/);
  assert.match(panel, /filterable:\s*"فیلتر"/);
  assert.match(panel, /variant:\s*"تنوع"/);
  assert.match(panel, /comparable:\s*"مقایسه"/);
  assert.equal(/attr-flag-required[\s\S]{0,120}type="checkbox"/.test(panel), false);
  assert.match(panel, /mapAdminErrorMessage/);
  assert.equal(panel.includes("فیلتر, تنوع"), false);
});
