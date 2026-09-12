import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const dir = path.dirname(fileURLToPath(import.meta.url));
const screen = fs.readFileSync(path.join(dir, "catalog-units-screen.tsx"), "utf8");
const api = fs.readFileSync(path.join(dir, "catalog-units-api.ts"), "utf8");
const settings = fs.readFileSync(path.join(dir, "settings/page.tsx"), "utf8");
const offer = fs.readFileSync(path.join(dir, "../vendor-panel/products/[offerId]/page.tsx"), "utf8");

test("UoM admin uses AppDataGrid and LanguageId translations", () => {
  assert.match(screen, /AppDataGrid/);
  assert.match(screen, /واحدهای اندازه‌گیری/);
  assert.match(screen, /languageId/);
  assert.match(screen, /loadAdminLanguages/);
  assert.doesNotMatch(screen, /NameFa/);
  assert.doesNotMatch(screen, /NameEn/);
  assert.doesNotMatch(api, /nameFa|nameEn/);
  assert.match(api, /languageId/);
});

test("rounding helper mentions historical invoices", () => {
  assert.match(settings, /فاکتورهای تاریخی/);
  assert.match(settings, /Historical orders and invoices/);
  assert.match(settings, /admin-settings-rounding-helper-en/);
  assert.match(settings, /admin-settings-hold-form/);
  assert.match(settings, /موجودی را رزرو نمی‌کند/);
  assert.match(settings, /مهلت‌ها/);
});

test("offer form shows product unit and min max check", () => {
  assert.match(offer, /offer-product-unit/);
  assert.match(offer, /حداقل مقدار خرید نباید از حداکثر بیشتر باشد/);
  assert.doesNotMatch(offer, /unit-selector|product-edit-unit/);
});
