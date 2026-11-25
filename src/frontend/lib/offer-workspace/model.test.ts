import assert from "node:assert/strict";
import test from "node:test";
import {
  allowedStatuses,
  applySavedInventoryDraft,
  changedInventoryRows,
  commercialDirty,
  deriveBuyability,
  detailVersionChanged,
  draftFromDetail,
  inventoryDirty,
  normalizeDigits,
  parseDecimal,
  priceDirty,
  validateConditions,
  validateDraft,
  validateInventory,
  validatePrice,
} from "./model.ts";
import type { OfferWorkspaceDetail } from "./types.ts";

function detail(over: Partial<OfferWorkspaceDetail> = {}): OfferWorkspaceDetail {
  return {
    offerId: "offer-1",
    sellerPartyId: "seller-1",
    sellerDisplayName: "فروشگاه",
    catalogVariantId: "variant-1",
    productId: "product-1",
    productTitle: "کالا",
    brandName: null,
    sellerSku: "SKU-1",
    status: "Active",
    channel: "Marketplace",
    amount: 1000,
    currency: "IRR",
    onHand: 10,
    reserved: 2,
    availableUnits: 8,
    catalogReadOnly: true,
    returnPolicyChoice: "Default",
    customReturnWindowDays: null,
    defaultReturnWindowDays: 7,
    sellerCanOverrideReturnPolicy: true,
    minReturnWindowDays: 1,
    maxReturnWindowDays: 30,
    allowNonReturnableOffers: true,
    minimumOrderQuantity: 1,
    maximumOrderQuantity: 5,
    productUnitCode: "pcs",
    productUnitName: "عدد",
    productUnitShortName: "عدد",
    inventoryLocations: [
      { stockItemId: "stock-a", locationId: "loc-a", locationCode: "WH-A", onHand: 10, reserved: 2, available: 8 },
    ],
    ...over,
  };
}

test("digits and decimals normalize Persian/Arabic input", () => {
  assert.equal(normalizeDigits("۱۲۳"), "123");
  assert.equal(normalizeDigits("٤٥٦"), "456");
  assert.equal(parseDecimal("۱۲٫۵"), 12.5);
  assert.equal(parseDecimal("1,250"), 1250);
  assert.equal(parseDecimal("abc"), null);
});

test("IRR price rejects decimals and negatives; FX keeps decimals", () => {
  assert.equal(validatePrice("1000", "IRR"), null);
  assert.equal(validatePrice("1000.5", "IRR"), "offer.price.decimals_not_allowed");
  assert.equal(validatePrice("-1", "IRR"), "offer.price.invalid");
  assert.equal(validatePrice("1000.5", "USD"), null);
  assert.equal(validatePrice("0", "IRR"), null);
  assert.equal(validatePrice("", "IRR"), null);
});

test("inventory requires reason and never goes below reserved", () => {
  const base = detail();
  const draft = { ...draftFromDetail(base), inventoryOnHand: { "stock-a": "1" } };
  assert.equal(validateInventory(base, draft), "offer.inventory.below_reserved");
  const negative = { ...draft, inventoryOnHand: { "stock-a": "-1" } };
  assert.equal(validateInventory(base, negative), "offer.inventory.invalid");
  const changed = { ...draft, inventoryOnHand: { "stock-a": "12" } };
  assert.equal(validateInventory(base, changed), "offer.inventory.reason_required");
  assert.equal(validateInventory(base, { ...changed, inventoryReason: "count" }), null);
});

test("conditions validate quantity order and custom return window bounds", () => {
  const base = detail();
  const draft = draftFromDetail(base);
  assert.equal(validateConditions(base, draft), null);
  assert.equal(
    validateConditions(base, { ...draft, minimumOrderQuantity: "9", maximumOrderQuantity: "3" }),
    "offer.qty.min_above_max",
  );
  assert.equal(
    validateConditions(base, {
      ...draft,
      returnPolicyChoice: "Custom",
      customReturnWindowDays: "40",
    }),
    "offer.return.window_out_of_range",
  );
  assert.equal(
    validateConditions(base, {
      ...draft,
      returnPolicyChoice: "Custom",
      customReturnWindowDays: "0",
    }),
    "offer.return.window_invalid",
  );
  assert.equal(
    validateConditions(base, {
      ...draft,
      returnPolicyChoice: "Custom",
      customReturnWindowDays: "10",
    }),
    null,
  );
});

test("dirty detection is field-scoped", () => {
  const base = detail();
  const draft = draftFromDetail(base);
  assert.equal(commercialDirty(base, draft), false);
  assert.equal(priceDirty(base, draft), false);
  assert.equal(inventoryDirty(base, draft), false);
  assert.equal(commercialDirty(base, { ...draft, sellerSku: "SKU-2" }), true);
  assert.equal(priceDirty(base, { ...draft, amount: "1200" }), true);
  assert.equal(inventoryDirty(base, { ...draft, inventoryOnHand: { "stock-a": "11" } }), true);
});

test("validateDraft groups errors by owning tab", () => {
  const base = detail();
  const draft = {
    ...draftFromDetail(base),
    amount: "1.5",
    inventoryOnHand: { "stock-a": "1" },
  };
  const codes = validateDraft(base, draft).map((error) => `${error.tab}:${error.code}`);
  assert.ok(codes.includes("pricing:offer.price.decimals_not_allowed"));
  assert.ok(codes.includes("inventory:offer.inventory.below_reserved"));
});

test("buyability is derived, not a manual toggle", () => {
  assert.equal(deriveBuyability(detail()).buyable, true);
  assert.equal(deriveBuyability(detail({ status: "Draft" })).reason, "offer.buyable.inactive");
  assert.equal(deriveBuyability(detail({ amount: null })).reason, "offer.buyable.no_price");
  assert.equal(deriveBuyability(detail({ availableUnits: 0 })).reason, "offer.buyable.no_stock");
});

test("archived lifecycle is terminal", () => {
  assert.deepEqual(allowedStatuses("Archived"), ["Archived"]);
  assert.ok(!allowedStatuses("Archived").includes("Active"));
});

test("inventory partial save preserves the unsaved location draft and reason", () => {
  const base = detail({
    onHand: 10,
    inventoryLocations: [
      { stockItemId: "stock-a", locationId: "loc-a", locationCode: "A", onHand: 10, reserved: 2, available: 8 },
      { stockItemId: "stock-b", locationId: "loc-b", locationCode: "B", onHand: 4, reserved: 0, available: 4 },
    ],
  });
  const draft = {
    ...draftFromDetail(base),
    inventoryOnHand: { "stock-a": "12", "stock-b": "9" },
    inventoryReason: "شمارش دوره‌ای",
  };
  assert.deepEqual(changedInventoryRows(base, draft).map((row) => row.stockItemId), ["stock-a", "stock-b"]);
  const firstSaved = detail({
    onHand: 13,
    inventoryLocations: [
      { stockItemId: "stock-a", locationId: "loc-a", locationCode: "A", onHand: 12, reserved: 2, available: 10 },
      { stockItemId: "stock-b", locationId: "loc-b", locationCode: "B", onHand: 4, reserved: 0, available: 4 },
    ],
  });
  const afterFirst = applySavedInventoryDraft(draft, firstSaved, "stock-a");
  assert.equal(afterFirst.inventoryOnHand["stock-a"], "12");
  assert.equal(afterFirst.inventoryOnHand["stock-b"], "9");
  assert.equal(afterFirst.inventoryReason, "شمارش دوره‌ای");
  assert.deepEqual(changedInventoryRows(firstSaved, afterFirst).map((row) => row.stockItemId), ["stock-b"]);
});

test("stale detail snapshots are detected before overwrite", () => {
  const current = detail();
  assert.equal(detailVersionChanged(current, detail()), false);
  assert.equal(detailVersionChanged(current, detail({ amount: 2000 })), true);
  assert.equal(detailVersionChanged(current, detail({ lastUpdatedAt: "2026-01-01T00:00:00Z" })), true);
});
