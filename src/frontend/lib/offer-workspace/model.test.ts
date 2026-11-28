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
  priceRevisionFor,
  priceRowFor,
  validateConditions,
  validateDraft,
  validateInventory,
  validatePrice,
} from "./model.ts";
import type { OfferWorkspaceDetail } from "./types.ts";
import { draftVersion } from "./draft-recovery.ts";
import { reconcileDraft } from "./offer-actions.ts";
import { parseOfferCurrencies } from "./currency-contract.ts";

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
    offerRevision: "offer-rev",
    priceRevision: "price-rev",
    allocationRevision: "alloc-rev",
    offeredQuantity: null,
    allocationHeld: null,
    revisions: { offer: "offer-rev", price: "price-rev", offeredQuantity: "alloc-rev" },
    capabilities: { canEditPhysicalInventory: true, canEditOfferedQuantity: false, canAssignFulfillment: true, canEditShippingPolicy: true },
    fulfillmentResponsibility: null,
    fulfillmentShippingTypeId: null,
    lastUpdatedAt: null,
    variantCode: null,
    variantLabel: null,
    primaryMediaAssetId: null,
    unitQuantityDecimalPlaces: 0,
    unitQuantityStep: null,
    unitRoundingMode: null,
    inventoryLocations: [
      { stockItemId: "stock-a", locationId: "loc-a", revision: "stock-rev", locationCode: "WH-A", locationName: "انبار الف", onHand: 10, reserved: 2, available: 8 },
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
      { stockItemId: "stock-a", locationId: "loc-a", revision: "stock-rev", locationCode: "A", locationName: "انبار الف", onHand: 10, reserved: 2, available: 8 },
      { stockItemId: "stock-b", locationId: "loc-b", revision: "stock-rev", locationCode: "B", locationName: "انبار ب", onHand: 4, reserved: 0, available: 4 },
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
      { stockItemId: "stock-a", locationId: "loc-a", revision: "stock-rev", locationCode: "A", locationName: "انبار الف", onHand: 12, reserved: 2, available: 10 },
      { stockItemId: "stock-b", locationId: "loc-b", revision: "stock-rev", locationCode: "B", locationName: "انبار ب", onHand: 4, reserved: 0, available: 4 },
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

test("quantity step and precision reject unaligned values", () => {
  const base = detail({ unitQuantityDecimalPlaces: 2, unitQuantityStep: 0.5 });
  assert.equal(validateConditions(base, { ...draftFromDetail(base), minimumOrderQuantity: "1.3" }), "offer.qty.step_invalid");
  assert.equal(validateConditions(base, { ...draftFromDetail(base), minimumOrderQuantity: "1.5" }), null);
  assert.equal(validateInventory(base, { ...draftFromDetail(base), inventoryOnHand: { "stock-a": "2.3", "stock-b": "4" }, inventoryReason: "شمارش" }), "offer.inventory.step_invalid");
});

test("reference scale validates each selected currency before writing", () => {
  const currencies = [
    { code: "IRR", englishName: "Rial", nativeName: "ریال", decimalPlaces: 0 },
    { code: "USD", englishName: "Dollar", nativeName: "Dollar", decimalPlaces: 2 },
    { code: "JPY", englishName: "Yen", nativeName: "Yen", decimalPlaces: 0 },
  ];
  assert.equal(validatePrice("12.345", "USD", currencies), "offer.price.decimals_not_allowed");
  assert.equal(validatePrice("۱۲٫۳۴", "USD", currencies), null);
  assert.equal(validatePrice("10.1", "JPY", currencies), "offer.price.decimals_not_allowed");
  assert.equal(validatePrice("10.00", "JPY", currencies), null);
  assert.equal(validatePrice("10", "ZZZ", currencies), "offer.price.invalid");
  assert.equal(validatePrice("10", "", currencies), "offer.price.invalid");
});

test("currency-specific revisions and persisted drafts detect non-primary edits", () => {
  const usd = { priceId: "usd-id", currency: "USD", amount: 12.34, revision: "usd-rev", status: "Active" };
  const base = detail({ prices: [usd] });
  assert.equal(priceRowFor(base, "USD")?.amount, 12.34);
  assert.equal(priceRevisionFor(base, "USD"), "usd-rev");
  assert.equal(priceRevisionFor(base, "EUR"), "absent");
  assert.equal(priceRevisionFor(base, "USD"), "usd-rev");
  assert.equal(priceRevisionFor(detail({ prices: [{ ...usd, revision: "" }] }), "USD"), null);
  const updated = detail({ prices: [{ ...usd, revision: "usd-rev-2" }] });
  assert.notEqual(draftVersion(base), draftVersion(updated));
  assert.equal(detailVersionChanged(base, updated), true);
});

test("currency reference decoder rejects missing precision and duplicate rows", () => {
  const usd = { code: "USD", englishName: "Dollar", nativeName: "Dollar", decimalPlaces: 2 };
  assert.deepEqual(parseOfferCurrencies([usd]), [usd]);
  assert.throws(() => parseOfferCurrencies([{ ...usd, decimalPlaces: null }]));
  assert.throws(() => parseOfferCurrencies([usd, usd]));
  assert.throws(() => parseOfferCurrencies([{ ...usd, decimalPlaces: "2" }]));
});

test("unknown save recovery never overwrites a concurrent non-primary price", () => {
  const usd = { priceId: "usd-id", currency: "USD", amount: 12, revision: "usd-r1", status: "Active" };
  const before = detail({ prices: [usd] });
  const desired = { ...draftFromDetail(before), currency: "USD", amount: "13" };
  const concurrent = detail({ prices: [{ ...usd, amount: 14, revision: "usd-r2" }] });
  assert.equal(reconcileDraft(before, concurrent, desired), null);
  const committed = detail({ prices: [{ ...usd, amount: 13, revision: "usd-r2" }] });
  assert.equal(reconcileDraft(before, committed, desired)?.amount, "13");
  const unrelated = detail({ amount: 2000, priceRevision: "irr-r2", prices: [usd] });
  const merged = reconcileDraft(before, unrelated, desired);
  assert.equal(merged?.currency, "USD");
  assert.equal(merged?.amount, "13");
});

test("authoring revision and status remain distinct from the effective store quote", () => {
  const retired = { priceId: "retired-id", currency: "IRR", amount: 900, revision: "retired-r", status: "Retired" };
  const base = detail({ amount: null, prices: [retired] });
  const draft = draftFromDetail(base);
  assert.equal(draft.amount, "");
  assert.equal(priceDirty(base, { ...draft, amount: "900" }), true);
  assert.equal(priceDirty(base, draft), false);
  assert.equal(priceRevisionFor(base, "IRR"), "retired-r");
  assert.equal(deriveBuyability(base).buyable, false);
});
