/**
 * TB-SHIPPING-ONE-LEVEL-TABS-135 — آزمون متمرکز انتخاب ارسال «هر خط سبد».
 *
 * سنجش invariants قرارداد گروه‌بندی (بدون React و بدون سرور):
 * - یک فروشنده با دو خط و دو روش ⇒ **دو** گروه مستقل با شناسهٔ واقعی خطوط
 * - یک فروشنده با دو خط و روش یکسان ⇒ **یک** گروه ادغام‌شده (ناوردایی Order M1)
 * - دو فروشنده با روش یکسان ⇒ هرگز در یک گروه ادغام نمی‌شوند
 * - خط بدون انتخاب/با روش ناواجد ⇒ بدون گروه و ادامه‌ی سفارش بسته
 * - بازیابی قطعی از پیش‌نویس، و fail-closed برای روش نامعتبر/خط ناپیدا
 */
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

import type { StorefrontCartLine } from "./storefront-cart-api.ts";
import {
  buildShippingItemGroups,
  isItemGroupingComplete,
  restoreLineMethodSelections,
} from "./storefront-shipping-item-groups.ts";
import type { StorefrontShippingMethod } from "./storefront-shipping-api.ts";

const here = path.dirname(fileURLToPath(import.meta.url));
const shippingSource = fs.readFileSync(path.join(here, "storefront-shipping.tsx"), "utf8");
const modelSource = fs.readFileSync(path.join(here, "storefront-shipping-item-groups.ts"), "utf8");

function line(lineId: string, sellerPartyId: string, title = "کالا"): StorefrontCartLine {
  return {
    lineId,
    offerId: `offer-${lineId}`,
    catalogVariantId: `variant-${lineId}`,
    sellerPartyId,
    productId: null,
    productSlug: null,
    title,
    sellerDisplayName: `فروشنده ${sellerPartyId}`,
    mediaAssetId: null,
    quantity: 1,
    unitAmountExclusiveOfTax: null,
    lineAmountExclusiveOfTax: null,
    currency: "IRR",
    quotedTaxExclusive: true,
    unitCode: null,
    unitDisplayName: null,
    quantityDecimalPlaces: 0,
    quantityStep: null,
    availability: "available",
  };
}

function method(methodCode: string, priceAmount = 100_000): StorefrontShippingMethod {
  return {
    methodCode,
    label: `روش ${methodCode}`,
    serviceCode: "standard",
    iconKey: "truck",
    priceAmount,
    leadDays: 2,
    isFree: false,
    estimationLabel: "۲ روز",
  };
}

const twoMethods = [method("post:pishgamspecial"), method("tipax:standard")];

test("یک فروشنده، دو خط، دو روش ⇒ دو گروه مستقل با خطوط واقعی", () => {
  const lines = [line("l1", "seller-a"), line("l2", "seller-a", "کالای دوم")];
  const grouping = buildShippingItemGroups({
    lines,
    methodCodeByLine: { l1: "post:pishgamspecial", l2: "tipax:standard" },
    methods: twoMethods,
  });

  assert.equal(grouping.groups.length, 2);
  assert.deepEqual(grouping.unassignedLineIds, []);
  assert.deepEqual(
    grouping.groups.map((group) => [group.sellerPartyId, group.methodCode, group.lines.map((l) => l.lineId)]),
    [
      ["seller-a", "post:pishgamspecial", ["l1"]],
      ["seller-a", "tipax:standard", ["l2"]],
    ],
  );
  assert.equal(isItemGroupingComplete(grouping, lines), true);
  // هیچ مبلغی از کلاینت ساخته نمی‌شود: مبلغ همان روش authoritative است.
  assert.equal(grouping.groups[1].method.priceAmount, 100_000);
});

test("یک فروشنده، دو خط، روش یکسان ⇒ یک گروه ادغام‌شده (ناوردایی M1)", () => {
  const lines = [line("l1", "seller-a"), line("l2", "seller-a", "کالای دوم")];
  const grouping = buildShippingItemGroups({
    lines,
    methodCodeByLine: { l1: "tipax:standard", l2: "tipax:standard" },
    methods: twoMethods,
  });

  assert.equal(grouping.groups.length, 1);
  assert.deepEqual(grouping.groups[0].lines.map((l) => l.lineId), ["l1", "l2"]);
  assert.equal(grouping.groups[0].methodCode, "tipax:standard");
  // دو گروه (فروشنده، روش) تکراری هرگز ساخته نمی‌شود چون سرور آن را رد می‌کند.
  assert.equal(isItemGroupingComplete(grouping, lines), true);
});

test("دو فروشنده با روش یکسان ⇒ گروه‌ها جدا می‌مانند (بدون ادغام فروشنده)", () => {
  const lines = [line("l1", "seller-a"), line("l2", "seller-b")];
  const grouping = buildShippingItemGroups({
    lines,
    methodCodeByLine: { l1: "tipax:standard", l2: "tipax:standard" },
    methods: twoMethods,
  });

  assert.equal(grouping.groups.length, 2);
  assert.deepEqual(
    grouping.groups.map((group) => [group.sellerPartyId, group.lines.map((l) => l.lineId)]),
    [["seller-a", ["l1"]], ["seller-b", ["l2"]]],
  );
});

test("خط بدون انتخاب یا با روش ناواجد ⇒ بدون گروه و ادامهٔ سفارش بسته است", () => {
  const lines = [line("l1", "seller-a"), line("l2", "seller-a", "کالای دوم"), line("l3", "seller-b")];
  const grouping = buildShippingItemGroups({
    lines,
    // l2 روش نامعلوم دارد (خارج فهرست authoritative) ⇒ هیچ گروهی نمی‌سازد.
    methodCodeByLine: { l1: "tipax:standard", l2: "made:up" },
    methods: twoMethods,
  });

  assert.equal(grouping.groups.length, 1);
  assert.deepEqual(
    grouping.groups[0].lines.map((l) => l.lineId),
    ["l1"],
  );
  assert.deepEqual(grouping.unassignedLineIds, ["l2", "l3"]);
  assert.equal(isItemGroupingComplete(grouping, lines), false);
  // سبد خالی هرگز «کامل» تلقی نمی‌شود.
  assert.equal(isItemGroupingComplete({ groups: [], unassignedLineIds: [] }, []), false);
});

test("بازیابی قطعی انتخاب هر خط از گروه‌های ذخیره‌شدهٔ پیش‌نویس", () => {
  const lines = [line("l1", "seller-a"), line("l2", "seller-a", "کالای دوم")];
  const restored = restoreLineMethodSelections({
    lines,
    persistedGroups: [
      { sellerPartyId: "seller-a", methodCode: "post:pishgamspecial", cartLineIds: ["l1"] },
      { sellerPartyId: "seller-a", methodCode: "tipax:standard", cartLineIds: ["l2"] },
    ],
    methods: twoMethods,
  });

  assert.deepEqual(restored, { l1: "post:pishgamspecial", l2: "tipax:standard" });
});

test("بازیابی fail-closed: روش ناواجد، خط ناپیدا و خط فروشندهٔ دیگر بازیابی نمی‌شوند", () => {
  const lines = [line("l1", "seller-a"), line("l2", "seller-a", "کالای دوم")];
  const restored = restoreLineMethodSelections({
    lines,
    persistedGroups: [
      // روش امروز واجد شرایط نیست ⇒ نه این گروه و نه روش جانشین حدس زده نمی‌شود.
      { sellerPartyId: "seller-a", methodCode: "carrier:removed", cartLineIds: ["l1", "l2"] },
      // خطی که دیگر در سبد نیست نادیده می‌شود.
      { sellerPartyId: "seller-b", methodCode: "tipax:standard", cartLineIds: ["l9"] },
      // گروه ذخیره‌شدهٔ فروشندهٔ دیگر نمی‌تواند خط این فروشنده را تصاحب کند.
      { sellerPartyId: "seller-b", methodCode: "post:pishgamspecial", cartLineIds: ["l2"] },
    ],
    methods: twoMethods,
  });

  assert.deepEqual(restored, {});
});

test("بازیابی بدون شناسهٔ خط: فقط فروشندهٔ تک‌خطی قطعی بازیابی می‌شود", () => {
  const lines = [line("l1", "seller-a"), line("l2", "seller-b"), line("l3", "seller-b", "کالای دوم")];
  const restored = restoreLineMethodSelections({
    lines,
    persistedGroups: [
      { sellerPartyId: "seller-a", methodCode: "tipax:standard", cartLineIds: [] },
      { sellerPartyId: "seller-b", methodCode: "tipax:standard", cartLineIds: [] },
    ],
    methods: twoMethods,
  });

  assert.deepEqual(restored, { l1: "tipax:standard" });
});

test("بازیابی هیچ خطی را دو بار تخصیص نمی‌دهد (اولین گروه برنده است)", () => {
  const lines = [line("l1", "seller-a")];
  const restored = restoreLineMethodSelections({
    lines,
    persistedGroups: [
      { sellerPartyId: "seller-a", methodCode: "tipax:standard", cartLineIds: ["l1"] },
      { sellerPartyId: "seller-a", methodCode: "post:pishgamspecial", cartLineIds: ["l1"] },
    ],
    methods: twoMethods,
  });

  assert.deepEqual(restored, { l1: "tipax:standard" });
});

test("سیم‌کشی سطح ارسال: انتخاب هر خط، گروه‌بندی (فروشنده،روش) و بازیابی fail-closed", () => {
  assert.match(shippingSource, /buildShippingItemGroups\(\{/);
  assert.match(shippingSource, /isItemGroupingComplete\(itemGrouping, cartLines\)/);
  assert.match(shippingSource, /restoreLineMethodSelections\(\{/);
  assert.match(shippingSource, /setLineMethodByLine\(\(prev\) => \(\{ \.\.\.prev, \[line\.lineId\]: m\.methodCode \}\)\)/);
  assert.match(shippingSource, /cartLineIds: group\.lines\.map\(\(line\) => line\.lineId\)/);
  assert.match(shippingSource, /const isGroupedCart = cartLines\.length > 1;/);
  // هیچ تاریخ/مبلغ/فروشندهٔ ساختگی و هیچ commit در این مرحله نیست.
  assert.doesNotMatch(shippingSource, /new Date\(/);
  assert.doesNotMatch(shippingSource, /shipping\/commit/);
  assert.doesNotMatch(modelSource, /fetch\(/);
});
