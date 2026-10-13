import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";
import { mapShippingProjection, toCustomerShippingMessage } from "./storefront-shipping-api.ts";
import { StorefrontCartApiError } from "./storefront-cart-api.ts";

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

test("shipping date chips use Host weekday/date once and do not duplicate Jalali", () => {
  const source = fs.readFileSync(path.join(root, "src/frontend/app/storefront/storefront-shipping.tsx"), "utf8");
  const chipFormat = fs.readFileSync(path.join(root, "src/frontend/app/storefront/storefront-shipping-format.ts"), "utf8");
  assert.match(source, /deliveryChipParts/);
  assert.match(chipFormat, /option\.label/);
  assert.match(chipFormat, /option\.subLabel/);
  assert.doesNotMatch(source, /function deliveryDateUi/);
  assert.match(source, /overflow-x-auto/);
});

test("shipping continue persists the draft, keeps the cart active and never commits", () => {
  const source = fs.readFileSync(path.join(root, "src/frontend/app/storefront/storefront-shipping.tsx"), "utf8");
  const persistFn = source.slice(source.indexOf("async function persistAndContinue"), source.indexOf("async function submitNewAddress"));
  assert.ok(persistFn.length > 0);
  // پیش‌نویس ارسال ذخیره می‌شود و ناوبری به پرداخت با آدرس سبد است.
  const saveIndex = persistFn.indexOf("saveShippingSelection");
  const pushIndex = persistFn.indexOf("router.push(localizePath(`/payment?cartId=");
  assert.ok(saveIndex > 0 && pushIndex > saveIndex, "draft must be saved before navigating to the cart-addressed payment");
  // کلیک «ادامه به پرداخت» نباید هیچ commit/رزرو/ساخت سفارش یا پاک‌کردن سبد ایجاد کند.
  assert.doesNotMatch(persistFn, /commitShippingToPayment/);
  assert.doesNotMatch(persistFn, /shipping\/commit/);
  assert.doesNotMatch(persistFn, /checkoutId/);
  assert.doesNotMatch(persistFn, /clearCartSession|notifyCartChanged|setCartCount\(0\)/);
  assert.doesNotMatch(source, /commitShippingToPayment/);
});

test("shipping UI source has no hardcoded Shopeiva template method list", () => {
  const source = fs.readFileSync(path.join(root, "src/frontend/app/storefront/storefront-shipping.tsx"), "utf8");
  assert.doesNotMatch(source, /shippingMethodsList/);
  assert.doesNotMatch(source, /پست پیشتاز/);
  assert.doesNotMatch(source, /ارسال اکسپرس/);
  assert.doesNotMatch(source, /دریافت حضوری/);
  assert.match(source, /loadShippingProjection/);
  assert.match(source, /saveShippingSelection/);
  assert.match(source, /\/payment\?cartId=/);
  assert.match(source, /shipping-first-name/);
  assert.match(source, /shipping-last-name/);
  assert.doesNotMatch(source, /نام و نام خانوادگی/);
});

test("cart CTA routes to /shipping", () => {
  const cart = fs.readFileSync(path.join(root, "src/frontend/app/storefront/storefront-cart.tsx"), "utf8");
  assert.match(cart, /checkoutHref/);
  assert.match(cart, /"\/shipping"/);
  assert.doesNotMatch(cart, /href="\/checkout"/);
});

test("customer shipping errors stay localized", () => {
  assert.equal(toCustomerShippingMessage(new Error("امکان ادامهٔ مرحلهٔ ارسال وجود ندارد.")), "امکان ادامهٔ مرحلهٔ ارسال وجود ندارد.");
});

test("stale cart on shipping never leaks the English server title", () => {
  const stale = new StorefrontCartApiError(
    409,
    "cart.version.conflict",
    "Cart was updated concurrently. Refresh and retry.",
  );
  const fa = toCustomerShippingMessage(stale);
  assert.equal(fa.includes("Refresh"), false);
  assert.match(fa, /سبد/);
  const en = toCustomerShippingMessage(stale, "en");
  assert.equal(en.includes("سبد"), false);
  assert.match(en, /cart/i);

  const mappedOrderCode = toCustomerShippingMessage(
    new StorefrontCartApiError(409, "checkout.version.conflict", "The cart was updated concurrently. Refresh the page."),
  );
  assert.equal(mappedOrderCode.includes("Refresh"), false);
  assert.match(mappedOrderCode, /سبد/);
});

test("shipping stepper walks back to the cart instead of trapping the customer", () => {
  const source = fs.readFileSync(path.join(root, "src/frontend/app/storefront/storefront-shipping.tsx"), "utf8");
  assert.match(source, /label: "سبد خرید", icon: ShoppingBag, href: "\/cart"/);
  assert.match(source, /data-testid=\{`shipping-step-\$\{s\.id\}`\}/);
  assert.match(source, /aria-label="بازگشت به سبد خرید"/);
});
