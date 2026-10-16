import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "../..");
const handoff = fs.readFileSync(path.join(root, "app/payment/storefront-payment-handoff.tsx"), "utf8");

test("multi-group payment summary renders grouped methods instead of a blank scalar label", () => {
  // The grouped branch is driven by the Order-owned projection, not by the blank scalar shorthand.
  assert.match(handoff, /page\.shippingGroups\.length > 1 \?/);
  assert.match(handoff, /data-testid="payment-shipping-groups"/);
  assert.match(handoff, /روش‌های ارسال/);
  // Each rendered group contributes exactly one row identity and shows its authoritative label/amount.
  assert.match(handoff, /data-testid=\{`payment-shipping-group-\$\{group\.shippingGroupId\}`\}/);
  assert.match(handoff, /group\.methodLabel/);
  assert.match(handoff, /formatOfferAmount\(group\.shippingAmount, group\.currency \|\| page\.currency\)/);
  // No invented aggregate method identity, rate or schedule is fabricated on the client.
  assert.doesNotMatch(handoff, /روش ارسال:.*shippingGroups/);
  assert.doesNotMatch(handoff, /shippingAmount\s*=\s*[\d_]|methodCode\s*=\s*"|\+\s*shippingGroups\.length\s*\*/);
});

test("legacy scalar shipping label stays intact for non-grouped checkouts", () => {
  // The legacy display must remain byte-identical for empty/single-group drafts.
  assert.match(
    handoff,
    /<p className="text-sm text-gray-600">\s*روش ارسال: <span className="font-bold text-gray-900">\{page\.shippingMethodLabel\}<\/span>\s*<\/p>/,
  );
  // Aggregate shipping charge and payable stay displayed exactly once.
  assert.equal((handoff.match(/هزینه ارسال:/g) ?? []).length, 1);
  assert.equal((handoff.match(/قابل پرداخت:/g) ?? []).length, 1);
  assert.equal((handoff.match(/data-testid="payment-payable-amount"/g) ?? []).length, 1);
});

test("grouped summary reuses existing storefront classes and adds no styling surface", () => {
  // Same class vocabulary as the accepted storefront shipping summary fee rows.
  assert.match(handoff, /className="space-y-1.5 border-t border-dashed border-gray-200 pt-2\.5"/);
  assert.match(handoff, /className="flex justify-between gap-2 text-\[11px\] md:text-xs"/);
  assert.match(handoff, /className="text-gray-500 truncate"/);
  assert.match(handoff, /className="font-bold text-gray-800 shrink-0"/);
  // No inline style, CSS module, token or global selector change from this task.
  assert.doesNotMatch(handoff, /style=\{\{/);
  assert.doesNotMatch(handoff, /from ".*\.css"/);
});

test("payment page keeps read-only behaviour on load", () => {
  // Opening/reloading must not submit, reserve or clear the cart: the load effect only reads.
  const effectStart = handoff.indexOf("useEffect(() => {");
  const effectEnd = handoff.indexOf("}, [params]);");
  assert.ok(effectStart > 0 && effectEnd > effectStart, "load effect must exist");
  const loadEffect = handoff.slice(effectStart, effectEnd);
  assert.doesNotMatch(loadEffect, /startFinalClickPayment\(/);
  assert.doesNotMatch(loadEffect, /startStorefrontPayment\(/);
  assert.doesNotMatch(loadEffect, /method:\s*POST|method:\s*"DELETE"/);
  assert.match(loadEffect, /loadCartPaymentDraft\(/);
  assert.match(handoff, /payment-cart-draft-notice/);
});
