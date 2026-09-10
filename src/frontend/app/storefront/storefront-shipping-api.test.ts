import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import { mapShippingProjection, toCustomerShippingMessage } from "./storefront-shipping-api.ts";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../../../..");

test("shipping projection maps store methods and never invents template carriers", () => {
  const page = mapShippingProjection({
    cartId: "11111111-1111-4111-8111-111111111111",
    cartVersion: 3,
    currency: "IRR",
    itemCount: 2,
    subtotalExclusiveOfTax: 3700000,
    sellerCount: 2,
    maxSellerPreparationDays: 2,
    methods: [
      {
        methodCode: "post:express",
        label: "پست — پیشتاز",
        serviceCode: "post",
        iconKey: "post",
        priceAmount: 200000,
        leadDays: 2,
        isFree: false,
        estimationLabel: "حدود ۲ روز",
      },
    ],
    selectedMethodCode: "post:express",
    selectedShippingAmount: 200000,
    minimumDeliveryDate: "2026-09-14",
    deliveryDates: [{ value: "2026-09-14", label: "امروز", subLabel: "۹/۱۴", isEarliest: true }],
    deliveryTimeWindows: [{ value: "9-12", label: "۹ صبح تا ۱۲ ظهر" }],
    draft: null,
    revalidationMessage: null,
    provinces: [{ code: "tehran", label: "تهران", cities: ["تهران"] }],
  });
  assert.equal(page.methods.length, 1);
  assert.equal(page.methods[0]!.methodCode, "post:express");
  assert.equal(page.selectedShippingAmount, 200000);
  assert.ok(!page.methods.some((m) => m.methodCode === "pickup" || m.label === "ارسال اکسپرس"));
});

test("shipping UI source has no hardcoded Shopeiva template method list", () => {
  const source = fs.readFileSync(path.join(root, "src/frontend/app/storefront/storefront-shipping.tsx"), "utf8");
  assert.doesNotMatch(source, /shippingMethodsList/);
  assert.doesNotMatch(source, /پست پیشتاز/);
  assert.doesNotMatch(source, /ارسال اکسپرس/);
  assert.doesNotMatch(source, /دریافت حضوری/);
  assert.match(source, /loadShippingProjection/);
  assert.match(source, /commitShippingToPayment/);
  assert.match(source, /\/payment/);
});

test("cart CTA routes to /shipping", () => {
  const cart = fs.readFileSync(path.join(root, "src/frontend/app/storefront/storefront-cart.tsx"), "utf8");
  assert.match(cart, /href="\/shipping"/);
  assert.doesNotMatch(cart, /href="\/checkout"/);
});

test("customer shipping errors stay localized", () => {
  assert.equal(toCustomerShippingMessage(new Error("امکان ادامهٔ مرحلهٔ ارسال وجود ندارد.")), "امکان ادامهٔ مرحلهٔ ارسال وجود ندارد.");
});
