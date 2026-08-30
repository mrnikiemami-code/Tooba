import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");

test("primary category migration wizard is three-step with preview then confirm", () => {
  const wizard = fs.readFileSync(
    path.join(root, "app/admin/primary-category-migration-wizard.tsx"),
    "utf8",
  );
  assert.match(wizard, /تغییر دسته اصلی/);
  assert.match(wizard, /primary-category-migration-wizard/);
  assert.match(wizard, /primary-migration-step-1/);
  assert.match(wizard, /primary-migration-step-2/);
  assert.match(wizard, /primary-migration-step-3/);
  assert.match(wizard, /previewProductCategoryChange/);
  assert.match(wizard, /assignAdminProductCategory/);
  assert.match(wizard, /ویژگی‌های قابل حفظ/);
  assert.match(wizard, /ویژگی‌های جدید/);
  assert.match(wizard, /ویژگی‌های خارج‌شونده/);
  assert.match(wizard, /تنوع‌های تحت تأثیر/);
  assert.match(wizard, /نمایش در دسته‌های دیگر/);
  assert.match(wizard, /تأیید و اعمال مهاجرت/);
  assert.match(wizard, /این مرحله تغییری ذخیره نمی‌کند/);
});

test("product workspace uses migration wizard instead of inline primary picker save", () => {
  const screen = fs.readFileSync(path.join(root, "app/admin/product-workspace-screen.tsx"), "utf8");
  assert.match(screen, /PrimaryCategoryMigrationWizard/);
  assert.match(screen, /product-change-primary-category/);
  assert.match(screen, /تغییر دسته اصلی/);
  assert.equal(screen.includes("previewProductCategoryChange"), false);
  assert.equal(screen.includes("assignAdminProductCategory"), false);
  assert.equal(screen.includes('label="دسته اصلی"'), false);
});
