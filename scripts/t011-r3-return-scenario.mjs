/**
 * TB-P06-T011-R3 — legitimate dev scenario via Host HTTP APIs only.
 * Paid → Fulfillment → Delivered (no return POST; capture opens customer modal first).
 */
import fs from "node:fs";
import path from "node:path";

const host = (process.env.TOOBA_HOST || "http://127.0.0.1:5088").replace(/\/$/, "");
const offerId = process.env.TOOBA_OFFER_ID || "01a03826-b318-7000-b6b6-aa85026be261";
const customerActor = process.env.TOOBA_CUSTOMER_ACTOR || "aaaaaaaa-aaaa-4aaa-8aaa-000000000009";
const sellerPartyIdDefault = process.env.TOOBA_SELLER_PARTY_ID || "01a030d1-40cb-7000-8abe-6d31739956c5";
let sellerPartyId = sellerPartyIdDefault;
const outDir = path.resolve("docs/evidence/TB-P06-T011-R3");
const outJson = path.join(outDir, "dev-scenario.json");

function pick(obj, ...keys) {
  for (const key of keys) {
    if (obj && obj[key] != null) return obj[key];
  }
  return null;
}

async function json(url, init = {}) {
  const res = await fetch(url, init);
  const text = await res.text();
  let body = null;
  try {
    body = text ? JSON.parse(text) : null;
  } catch {
    body = text;
  }
  if (!res.ok) {
    throw new Error(`${res.status} ${url} ${typeof body === "string" ? body : JSON.stringify(body)}`);
  }
  return body;
}

async function resolveSellerActorForParty(partyId) {
  const contexts = await json(`${host}/v1/seller/dev-contexts`);
  const list = Array.isArray(contexts?.actors)
    ? contexts.actors
    : Array.isArray(contexts)
      ? contexts
      : contexts?.contexts ?? [];
  const hit = list.find((x) => pick(x, "sellerPartyId", "SellerPartyId") === partyId);
  const actor = pick(hit, "actorUserId", "ActorUserId");
  if (!actor) throw new Error(`seller actor not found for party ${partyId}`);
  return actor;
}

async function resolveSellerActor() {
  return resolveSellerActorForParty(sellerPartyId);
}

function sellerHeaders(actor, partyId = sellerPartyId) {
  return {
    "content-type": "application/json",
    "X-Tooba-Seller-Party-Id": partyId,
    "X-Tooba-Dev-Actor-User-Id": actor,
  };
}

function customerHeaders() {
  return {
    "content-type": "application/json",
    "X-Tooba-Dev-Actor-User-Id": customerActor,
  };
}

async function resolveOfferWithStock() {
  if (process.env.TOOBA_OFFER_ID) return process.env.TOOBA_OFFER_ID;
  try {
    const actor = await resolveSellerActorForParty(sellerPartyIdDefault);
    const offers = await json(`${host}/v1/seller/offers`, { headers: sellerHeaders(actor, sellerPartyIdDefault) });
    const rows = Array.isArray(offers) ? offers : [];
    const hit = rows.find((o) => Number(pick(o, "availableUnits", "AvailableUnits") ?? 0) > 0);
    if (hit) {
      const id = pick(hit, "offerId", "OfferId");
      console.log("using seller offer", id, pick(hit, "productTitle", "ProductTitle"));
      return id;
    }
  } catch (error) {
    console.warn("seller offer lookup failed", error.message);
  }
  for (let page = 1; page <= 12; page += 1) {
    const listing = await json(`${host}/v1/storefront/products?page=${page}&pageSize=50`);
    const products = listing?.products ?? [];
    const hit = products.find((p) => Number(p.availableUnits ?? 0) > 0 && p.primaryOfferId);
    if (hit?.primaryOfferId) {
      console.log("using storefront offer", hit.primaryOfferId, hit.title, "units", hit.availableUnits);
      return hit.primaryOfferId;
    }
    if (products.length === 0) break;
  }
  throw new Error("no in-stock offer found");
}

async function createPaidCheckout() {
  const offer = await resolveOfferWithStock();
  const created = await json(`${host}/v1/storefront/cart`, { method: "POST" });
  const cartId = pick(created, "cartId", "CartId");
  const guestSecret = pick(created, "guestSecret", "GuestSecret");
  let version = pick(created, "version", "Version");
  const headers = {
    "content-type": "application/json",
    "X-Tooba-Guest-Secret": guestSecret,
    "X-Tooba-Cart-Version": String(version),
  };

  const withLine = await json(`${host}/v1/storefront/cart/${cartId}/lines?expectedVersion=${version}`, {
    method: "POST",
    headers,
    body: JSON.stringify({ offerId: offer, quantity: 1 }),
  });
  version = pick(withLine, "version", "Version");
  headers["X-Tooba-Cart-Version"] = String(version);

  const checkout = await json(`${host}/v1/storefront/checkout`, {
    method: "POST",
    headers,
    body: JSON.stringify({
      cartId,
      expectedCartVersion: version,
      idempotencyKey: `r3-${Date.now()}`,
      shipping: {
        recipientName: "خریدار R3 مرجوعی",
        contactMobile: "09121234567",
        provinceName: "تهران",
        cityName: "تهران",
        postalAddress: "خیابان آزادی، پلاک ۱",
        postalCode: "1234567890",
      },
    }),
  });
  const checkoutId = pick(checkout, "checkoutId", "CheckoutId");

  const payInit = await json(`${host}/v1/storefront/checkout/${checkoutId}/payments`, {
    method: "POST",
    headers,
    body: JSON.stringify({ cartId, idempotencyKey: `pay-r3-${Date.now()}` }),
  });
  const paymentId = pick(payInit, "paymentId", "PaymentId");
  const attemptId = pick(payInit, "attemptId", "AttemptId");
  const providerRequestReference = pick(payInit, "providerRequestReference", "ProviderRequestReference");

  await json(`${host}/v1/storefront/payments/${paymentId}/sandbox/complete`, {
    method: "POST",
    headers,
    body: JSON.stringify({ cartId, attemptId, providerRequestReference, outcome: "success" }),
  });

  for (let i = 0; i < 30; i += 1) {
    await new Promise((r) => setTimeout(r, 500));
    const confirmation = await json(`${host}/v1/storefront/checkout/${checkoutId}?cartId=${encodeURIComponent(cartId)}`, { headers });
    const state = String(pick(confirmation, "paymentState", "PaymentState"));
    if (state.toLowerCase() === "paid") break;
  }

  return { cartId, guestSecret, checkoutId };
}

async function waitForFulfillment(sellerActor, checkoutId, partyId) {
  for (let i = 0; i < 40; i += 1) {
    await new Promise((r) => setTimeout(r, 500));
    const list = await json(`${host}/v1/seller/fulfillments`, {
      headers: sellerHeaders(sellerActor, partyId),
    });
    const rows = Array.isArray(list) ? list : [];
    const hit = rows.find((x) => pick(x, "checkoutId", "CheckoutId") === checkoutId);
    if (hit) return hit;
  }
  throw new Error(`fulfillment not created for checkout ${checkoutId}`);
}

async function deliverFulfillment(sellerActor, fulfillmentRow, partyId = sellerPartyId) {
  const fulfillmentId = pick(fulfillmentRow, "fulfillmentId", "FulfillmentId");
  const items = pick(fulfillmentRow, "items", "Items") || [];
  const firstLine = items[0];
  const orderLineId = pick(firstLine, "orderLineId", "OrderLineId");
  const qty = pick(firstLine, "quantityOrdered", "QuantityOrdered") || 1;
  const headers = sellerHeaders(sellerActor, partyId);

  await json(`${host}/v1/seller/fulfillments/${fulfillmentId}/processing`, { method: "POST", headers });
  await json(`${host}/v1/seller/fulfillments/${fulfillmentId}/packed`, { method: "POST", headers });
  const shipped = await json(`${host}/v1/seller/fulfillments/${fulfillmentId}/shipments`, {
    method: "POST",
    headers,
    body: JSON.stringify({
      carrierDisplayName: "Post R3 Demo",
      items: [{ orderLineId, quantity: qty }],
    }),
  });
  const shipments = pick(shipped, "shipments", "Shipments") || [];
  const shipmentId = pick(shipments[shipments.length - 1], "shipmentId", "ShipmentId");
  await json(`${host}/v1/seller/fulfillments/${fulfillmentId}/shipments/${shipmentId}/tracking`, {
    method: "POST",
    headers,
    body: JSON.stringify({ trackingReference: `TRK-R3-${Date.now()}` }),
  });
  await json(`${host}/v1/seller/fulfillments/${fulfillmentId}/shipments/${shipmentId}/dispatch`, {
    method: "POST",
    headers,
  });
  const delivered = await json(`${host}/v1/seller/fulfillments/${fulfillmentId}/shipments/${shipmentId}/deliver`, {
    method: "POST",
    headers,
  });
  return { fulfillmentId, shipmentId, orderLineId, quantity: qty, snapshot: delivered };
}

async function readCustomerOrder(checkoutId) {
  return json(`${host}/v1/customer/orders/${checkoutId}`, { headers: customerHeaders() });
}

export async function submitReturnRequest(scenario) {
  const body = {
    sellerOrderId: scenario.sellerOrderId,
    idempotencyKey: scenario.returnIdempotencyKey || `return-r3-${scenario.checkoutId}`,
    reason: "Damaged item — R3 live modal proof",
    items: [{ orderLineId: scenario.orderLineId, quantity: scenario.quantity }],
  };
  const created = await json(`${host}/v1/customer/returns`, {
    method: "POST",
    headers: customerHeaders(),
    body: JSON.stringify(body),
  });
  return {
    returnRequestId: pick(created, "returnRequestId", "ReturnRequestId"),
    status: pick(created, "status", "Status"),
    snapshot: created,
  };
}

async function main() {
  fs.mkdirSync(outDir, { recursive: true });
  const { checkoutId } = await createPaidCheckout();
  const order = await readCustomerOrder(checkoutId);
  const sellerOrders = pick(order, "sellerOrders", "SellerOrders") || [];
  const firstSeller = sellerOrders[0] || {};
  sellerPartyId = pick(firstSeller, "sellerPartyId", "SellerPartyId") || sellerPartyId;
  const sellerActor = await resolveSellerActorForParty(sellerPartyId);
  const fulfillmentRow = await waitForFulfillment(sellerActor, checkoutId, sellerPartyId);
  const sellerOrderId = pick(fulfillmentRow, "sellerOrderId", "SellerOrderId");
  const delivered = await deliverFulfillment(sellerActor, fulfillmentRow, sellerPartyId);
  const fulfillments = await json(`${host}/v1/customer/orders/${checkoutId}/fulfillments`, {
    headers: customerHeaders(),
  });

  const scenario = {
    createdAt: new Date().toISOString(),
    method: "HTTP APIs via Host (storefront checkout + seller fulfillment lifecycle)",
    directDbMutation: false,
    customerActor,
    sellerPartyId,
    sellerActor,
    checkoutId,
    sellerOrderId,
    fulfillmentId: delivered.fulfillmentId,
    shipmentId: delivered.shipmentId,
    orderLineId: delivered.orderLineId,
    quantity: delivered.quantity,
    fulfillmentStatus: pick(delivered.snapshot, "status", "Status"),
    returnIdempotencyKey: `return-r3-${checkoutId}`,
    returnRequestId: null,
    orderReference: pick(order, "reference", "Reference"),
    customerOrderUrl: `http://127.0.0.1:3000/customer-panel/orders/${checkoutId}`,
    sellerReturnsUrl: `http://127.0.0.1:3000/vendor-panel/returns?sellerPartyId=${sellerPartyId}`,
    sellerReturnDetailUrl: null,
    fulfillments,
  };

  fs.writeFileSync(outJson, JSON.stringify(scenario, null, 2));
  console.log("R3_SCENARIO_READY", JSON.stringify(scenario));
}

import { fileURLToPath } from "node:url";

const isMain = process.argv[1] === fileURLToPath(import.meta.url);
if (isMain) {
  main().catch((error) => {
    console.error("R3_SCENARIO_FAIL", error);
    process.exit(1);
  });
}
