import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const shippingPage = fs.readFileSync(path.join(root, "app/storefront/storefront-shipping.tsx"), "utf8");
const reconcile = fs.readFileSync(
  path.join(root, "app/storefront/storefront-shipping-captured-selection.ts"),
  "utf8",
);
const groupCards = fs.readFileSync(
  path.join(root, "app/storefront/storefront-shipping-group-cards.tsx"),
  "utf8",
);

test("province change updates address state only — zero direct refreshProjection", () => {
  assert.match(
    shippingPage,
    /onProvinceChange=\{\(provinceName\) => \{\s*\/\/ TB-245-R3:[\s\S]*?setAddress\(\(a\) => \(\{ \.\.\.a, provinceName, cityName: "" \}\)\)\s*;\s*\}\}/,
  );
  const provinceHandler = shippingPage.match(
    /onProvinceChange=\{\(provinceName\) => \{[\s\S]*?\}\}/,
  )?.[0] ?? "";
  assert.ok(provinceHandler.length > 0);
  assert.doesNotMatch(provinceHandler, /refreshProjection/);
});

test("hard invalidation is coupon/cart only; destination keeps provisional choices", () => {
  assert.match(reconcile, /isHardInvalidatingRoutingChange/);
  assert.match(reconcile, /destination revalidation|بازاعتبار مقصد|انتخاب موقت/i);
  assert.match(reconcile, /preview\.groups\.length === 0/);
});

test("destination pending keeps cards visible and locks unconfirmed money/interaction", () => {
  assert.match(groupCards, /shipping-captured-destination-revalidate/);
  assert.match(groupCards, /choicesLocked/);
  assert.match(groupCards, /data-destination-unconfirmed/);
  assert.doesNotMatch(
    shippingPage,
    /groups:\s*\[\],\s*unavailableReason:\s*destinationPreviewMode === "failed"/,
  );
});
