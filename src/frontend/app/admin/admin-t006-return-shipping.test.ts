/**
 * TB-P09-T006: Offer return policy mapping + shipment modal dynamic fields (no browser prompts).
 */
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import test from "node:test";
import { fileURLToPath } from "node:url";

const here = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(here, "../../../..");
const vendorOfferPage = path.join(
  here,
  "../vendor-panel/products/[offerId]/page.tsx",
);

test("vendor offer detail exposes سیاست مرجوعی controls", () => {
  const page = fs.readFileSync(vendorOfferPage, "utf8");
  assert.match(page, /سیاست مرجوعی/);
  assert.match(page, /استفاده از سیاست پیش‌فرض فروشگاه/);
  assert.match(page, /مهلت اختصاصی/);
  assert.match(page, /غیرقابل مرجوعی/);
  assert.match(page, /returnPolicyChoice/);
  assert.ok(fs.existsSync(path.join(repoRoot, "docs/evidence/TB-P09-T006/discovery.md")));
});

test("create shipment modal is method-dynamic without browser prompts", () => {
  const modal = fs.readFileSync(path.join(here, "admin-create-shipment-modal.tsx"), "utf8");
  assert.match(modal, /shipment-fields-post/);
  assert.match(modal, /shipment-fields-tipax/);
  assert.match(modal, /shipment-fields-courier/);
  assert.match(modal, /shipment-fields-store-courier/);
  assert.match(modal, /shipment-fields-in-person/);
  assert.match(modal, /shippingMethodCode/);
  assert.match(modal, /providerMetadataJson/);
  assert.doesNotMatch(modal, /window\.prompt|window\.confirm|window\.alert/);
  assert.match(modal, /Dialog/);
});

test("orders UI preservation markers remain for width/scroll/kebab", () => {
  const panel = fs.readFileSync(path.join(here, "admin-order-items-shipping-panel.tsx"), "utf8");
  assert.match(panel, /اقلام و ارسال/);
  assert.match(panel, /AdminCreateShipmentModal/);
});
