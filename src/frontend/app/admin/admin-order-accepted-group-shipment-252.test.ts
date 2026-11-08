import assert from "node:assert/strict";
import test from "node:test";
import { readFileSync } from "node:fs";
import { join } from "node:path";

const root = join(process.cwd(), "app", "admin");

test("252 create modal has no method catalogue reselection", () => {
  const src = readFileSync(join(root, "admin-create-shipment-modal.tsx"), "utf8");
  assert.match(src, /shippingGroup/);
  assert.match(src, /admin-create-shipment-accepted-summary/);
  assert.doesNotMatch(src, /loadShippingMethodTree/);
  assert.doesNotMatch(src, /admin-create-shipment-method-table/);
  assert.doesNotMatch(src, /FALLBACK_TREE/);
  assert.match(src, /shippingGroupId:\s*shippingGroup\.shippingGroupId/);
});

test("252 panel opens create from accepted group cards", () => {
  const src = readFileSync(join(root, "admin-order-items-shipping-panel.tsx"), "utf8");
  assert.match(src, /AdminOrderOperationalDeliverySection/);
  assert.match(src, /setShipmentModalTarget/);
  assert.doesNotMatch(src, /setShipmentModalSellerId/);
  const op = readFileSync(join(root, "admin-order-operational-delivery-section.tsx"), "utf8");
  assert.match(op, /admin-order-operational-create-/);
  assert.match(op, /shippingGroups/);
});

test("252 ops menu no longer opens carrier method override for create_shipment", () => {
  const src = readFileSync(join(root, "admin-order-operations-menu.tsx"), "utf8");
  assert.match(src, /کارت گروه ارسال پذیرفته‌شده/);
  assert.doesNotMatch(src, /setCarrierAction\(action\)/);
});

test("252 admin api maps shippingGroups and shipment shippingGroupId", () => {
  const src = readFileSync(join(root, "admin-api.ts"), "utf8");
  assert.match(src, /AdminAcceptedShippingGroup/);
  assert.match(src, /shippingGroups/);
  assert.match(src, /shippingGroupId/);
});
