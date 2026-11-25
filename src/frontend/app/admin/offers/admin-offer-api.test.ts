import "../../../features/order-carrier-execution/install-jsdom-globals.ts";
import assert from "node:assert/strict";
import test from "node:test";
import { ADMIN_ACTOR_STORAGE_KEY } from "../../../lib/admin/admin-result.ts";
import {
  assignAdminOfferFulfillment,
  loadAdminOfferDetail,
  patchAdminOffer,
  writeAdminOfferInventory,
  writeAdminOfferPrice,
} from "../admin-offer-api.ts";

type Captured = { url: string; method: string; body: unknown };

const realFetch = globalThis.fetch;

function installFetch(payload: unknown, status = 200): { calls: Captured[]; restore: () => void } {
  const calls: Captured[] = [];
  globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
    calls.push({
      url: String(input),
      method: init?.method ?? "GET",
      body: init?.body ? JSON.parse(String(init.body)) : null,
    });
    return new Response(JSON.stringify(payload), {
      status,
      headers: { "content-type": "application/json" },
    });
  }) as typeof fetch;
  return { calls, restore: () => { globalThis.fetch = realFetch; } };
}

function detailPayload(over: Record<string, unknown> = {}) {
  return {
    offerId: "offer-1",
    sellerPartyId: "seller-1",
    sellerDisplayName: "فروشگاه",
    catalogVariantId: "variant-1",
    productId: "product-1",
    productTitle: "کالا",
    brandName: "برند",
    sellerSku: "SKU-1",
    status: "Active",
    channel: "Marketplace",
    amount: 1000,
    currency: "IRR",
    onHand: 10,
    reserved: 2,
    availableUnits: 8,
    catalogReadOnly: true,
    returnPolicyChoice: "Custom",
    customReturnWindowDays: 10,
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

function actor(): void {
  window.localStorage.setItem(ADMIN_ACTOR_STORAGE_KEY, "actor-1");
}

test("admin detail maps the real multi-position inventory and return contract", async () => {
  actor();
  const fetched = installFetch(detailPayload());
  const result = await loadAdminOfferDetail("offer-1");
  fetched.restore();
  assert.equal(result.ok, true);
  if (!result.ok) return;
  assert.equal(result.detail.inventoryLocations.length, 1);
  assert.equal(result.detail.inventoryLocations[0]?.stockItemId, "stock-a");
  assert.equal(result.detail.returnPolicyChoice, "Custom");
  assert.equal(result.detail.customReturnWindowDays, 10);
  assert.equal(result.detail.reserved, 2);
  assert.equal(fetched.calls[0]?.url, "/v1/admin/offers/offer-1");
  assert.equal(fetched.calls[0]?.method, "GET");
});

test("admin price write rejects a mismatched server read-back", async () => {
  actor();
  const fetched = installFetch(detailPayload({ amount: 1000 }));
  const result = await writeAdminOfferPrice("offer-1", { amount: 1500, currency: "IRR" });
  fetched.restore();
  assert.equal(result.ok, false);
  if (!result.ok) assert.equal(result.errorCode, "admin.price.response_mismatch");
});

test("admin price write accepts a verified read-back and sends IR market", async () => {
  actor();
  const fetched = installFetch(detailPayload({ amount: 1500 }));
  const result = await writeAdminOfferPrice("offer-1", { amount: 1500, currency: "IRR" });
  fetched.restore();
  assert.equal(result.ok, true);
  assert.equal(fetched.calls[0]?.method, "PUT");
  assert.deepEqual(fetched.calls[0]?.body, { amount: 1500, currency: "IRR", market: "IR" });
});

test("admin inventory write targets one stock item and verifies on-hand", async () => {
  actor();
  const fetched = installFetch(
    detailPayload({
      inventoryLocations: [
        { stockItemId: "stock-a", locationId: "loc-a", locationCode: "WH-A", onHand: 14, reserved: 2, available: 12 },
      ],
    }),
  );
  const result = await writeAdminOfferInventory("offer-1", {
    stockItemId: "stock-a",
    onHand: 14,
    reason: "count",
  });
  fetched.restore();
  assert.equal(result.ok, true);
  assert.deepEqual(fetched.calls[0]?.body, { onHand: 14, reason: "count", stockItemId: "stock-a" });
});

test("admin patch sends only the supplied commercial patch", async () => {
  actor();
  const fetched = installFetch(detailPayload({ sellerSku: "SKU-2", status: "Suspended" }));
  const result = await patchAdminOffer("offer-1", {
    sellerSku: "SKU-2",
    status: "Suspended",
    returnPolicyChoice: "Custom",
    customReturnWindowDays: 10,
    minimumOrderQuantity: 2,
    maximumOrderQuantity: 4,
  });
  fetched.restore();
  assert.equal(result.ok, true);
  assert.equal(fetched.calls[0]?.method, "PATCH");
  assert.deepEqual(fetched.calls[0]?.body, {
    sellerSku: "SKU-2",
    status: "Suspended",
    returnPolicyChoice: "Custom",
    customReturnWindowDays: 10,
    minimumOrderQuantity: 2,
    maximumOrderQuantity: 4,
  });
});

test("admin fulfillment assigns explicit responsibility and validates the returned offer", async () => {
  actor();
  const fetched = installFetch({ offerId: "offer-1" });
  const ok = await assignAdminOfferFulfillment("offer-1", {
    responsibility: "Platform",
    shippingTypeId: "type-1",
  });
  fetched.restore();
  assert.equal(ok.ok, true);
  assert.equal(fetched.calls[0]?.method, "PUT");
  assert.deepEqual(fetched.calls[0]?.body, { responsibility: "Platform", shippingTypeId: "type-1" });

  actor();
  const mismatch = installFetch({ offerId: "someone-else" });
  const failed = await assignAdminOfferFulfillment("offer-1", {
    responsibility: "Seller",
    shippingTypeId: null,
  });
  mismatch.restore();
  assert.equal(failed.ok, false);
});
