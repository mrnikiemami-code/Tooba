import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import test from "node:test";
function parseReservationInteger(raw: string): { ok: true; value: number | null } | { ok: false } {
  const trimmed = raw.trim();
  if (!trimmed) return { ok: true, value: null };
  if (!/^-?\d+$/.test(trimmed)) return { ok: false };
  const parsed = Number(trimmed);
  return Number.isFinite(parsed) ? { ok: true, value: parsed } : { ok: false };
}

const dir = dirname(fileURLToPath(import.meta.url));
const settings = readFileSync(join(dir, "settings/page.tsx"), "utf8");
const editor = readFileSync(join(dir, "reservation-policy-editor.tsx"), "utf8");
const category = readFileSync(join(dir, "category-admin-screen.tsx"), "utf8");
const categoryPanel = readFileSync(join(dir, "category-reservation-panel.tsx"), "utf8");
const offerPanel = readFileSync(join(dir, "offer-reservation-panel.tsx"), "utf8");
const product = readFileSync(join(dir, "product-workspace-screen.tsx"), "utf8");
const vendor = readFileSync(join(dir, "../vendor-panel/products/[offerId]/page.tsx"), "utf8");
const sellerApi = readFileSync(join(dir, "../vendor-panel/seller-api.ts"), "utf8");
const api = readFileSync(join(dir, "reservation-policy-api.ts"), "utf8");

test("store settings expose reservation fields with save/cancel", () => {
  assert.match(settings, /admin-settings-reservation-policy/);
  assert.match(settings, /admin-settings-save-holds/);
  assert.match(settings, /admin-settings-cancel-holds/);
  assert.match(settings, /ReservationPolicyEditor/);
  assert.match(settings, /admin-settings-limits-form/);
  assert.match(settings, /کنترل سفارش‌های پرداخت‌نشده و سوءاستفاده از رزرو/);
  assert.match(settings, /admin-settings-max-open-unpaid/);
  assert.doesNotMatch(settings, /MaxOpenUnpaidOrdersPerCustomer/);
});

test("category inherit/override surface exists", () => {
  assert.match(category, /id: "reservation"/);
  assert.match(category, /CategoryReservationPanel/);
  assert.match(categoryPanel, /category-reservation-panel/);
  assert.match(editor, /reservation-policy-inherit-\$\{key\}/);
  assert.match(editor, /inheritLabelFa/);
});

test("offer inherit/override/effective preview and flash-sale copy", () => {
  assert.match(offerPanel, /سیاست رزرو موجودی پیشنهادها/);
  assert.match(product, /OfferReservationPanel/);
  assert.match(editor, /overridden/);
  assert.match(editor, /inherited from/);
  assert.match(editor, /بازنویسی‌شده/);
  assert.match(editor, /ارث از \$\{field.sourceLabelFa\}/);
  assert.match(editor, /flashSaleHelpFa/);
  assert.match(editor, /stricterNoteFa/);
  assert.match(editor, /longHoldNoteFa/);
  assert.match(editor, /multiLineHelpFa/);
});

test("FA RTL and EN LTR without raw config keys", () => {
  assert.match(editor, /dir=\{dir\}/);
  assert.match(editor, /locale === "en" \? "ltr" : "rtl"/);
  assert.doesNotMatch(editor, /InitialReservationHoldMinutes/);
  assert.doesNotMatch(editor, /RetryReservationHoldMinutes/);
  assert.doesNotMatch(editor, /MaxReservationCycles/);
  assert.doesNotMatch(settings, /InitialReservationHoldMinutes/);
});

test("seller mutation is not exposed; effective is read-only", () => {
  assert.match(vendor, /canEdit=\{false\}/);
  assert.match(vendor, /seller-reservation-policy-readonly/);
  assert.match(sellerApi, /loadSellerReservationPolicy/);
  assert.doesNotMatch(sellerApi, /saveSellerReservationPolicy/);
  assert.doesNotMatch(vendor, /reservation-policy-save/);
});

test("mapper uses backend effective source and integer parse", () => {
  assert.match(api, /mapReservationPolicy/);
  assert.match(api, /inherited from \$\{field.sourceLabelEn\}/);
  assert.equal(parseReservationInteger("").ok, true);
  assert.equal(parseReservationInteger("3").ok, true);
  assert.equal(parseReservationInteger("3.5").ok, false);
  assert.doesNotMatch(api, /Offer > Category > Store/);
});
