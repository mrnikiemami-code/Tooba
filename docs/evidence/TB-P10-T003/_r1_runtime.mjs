/**
 * TB-P10-T003-R1 — shipping allocation runtime (multi-seller + ordering independence).
 */
import { writeFileSync } from "node:fs";
import { randomUUID } from "node:crypto";

const BASE = "http://127.0.0.1:5088";
const KG_OFFER = "01a03826-9936-7000-b499-ff26a6123a8c";
const ARMAN_OFFER = "01a030d1-40f1-7000-95f6-b8efc58e2619";
const headers = { Host: "alpha.localhost", "Content-Type": "application/json" };

async function req(method, path, body, extra = {}) {
  const r = await fetch(`${BASE}${path}`, {
    method,
    headers: { ...headers, ...extra },
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });
  const text = await r.text();
  let json;
  try {
    json = JSON.parse(text);
  } catch {
    json = text;
  }
  return { status: r.status, json, text };
}
function pick(obj, ...keys) {
  if (!obj || typeof obj !== "object") return undefined;
  for (const k of keys) if (obj[k] != null) return obj[k];
  return undefined;
}

const out = { ok: true, steps: [] };
function note(name, data) {
  const row = { name, ...(data ?? {}) };
  out.steps.push(row);
  if (data && data.ok === false) out.ok = false;
  console.log(JSON.stringify(row));
}

async function commitCart(offers) {
  const cart = await req("POST", "/v1/storefront/cart");
  const secret = pick(cart.json, "guestSecret", "GuestSecret");
  const guest = { "X-Tooba-Guest-Secret": secret };
  const cartId = pick(cart.json, "cartId", "CartId");
  let version = pick(cart.json, "version", "Version");
  for (const offerId of offers) {
    const line = await req(
      "POST",
      `/v1/storefront/cart/${cartId}/lines?expectedVersion=${version}`,
      { offerId, quantity: offerId === KG_OFFER ? 1.25 : 1 },
      guest,
    );
    version = pick(line.json, "version", "Version");
  }
  const proj = await req(
    "POST",
    "/v1/storefront/shipping/projection",
    { cartId, provinceName: "تهران", methodCode: "post:express", language: "fa" },
    guest,
  );
  const minDate = pick(proj.json, "minimumDeliveryDate", "MinimumDeliveryDate");
  const shippingAmount = pick(proj.json, "selectedShippingAmount", "SelectedShippingAmount");
  await req(
    "PUT",
    "/v1/storefront/shipping/selection",
    {
      cartId,
      expectedCartVersion: version,
      recipientName: "R1 Allocation",
      contactMobile: "+989121230091",
      provinceName: "تهران",
      cityName: "تهران",
      postalAddress: "آدرس تخصیص ارسال",
      postalCode: "1234567890",
      shippingMethodCode: "post:express",
      selectedDeliveryDate: minDate,
      selectedDeliveryTimeWindow: "9-12",
      customerNote: "r1-alloc",
    },
    guest,
  );
  const commit = await req(
    "POST",
    "/v1/storefront/shipping/commit",
    { cartId, expectedCartVersion: version, idempotencyKey: randomUUID() },
    guest,
  );
  return {
    cartId,
    guest,
    checkoutId: pick(commit.json, "checkoutId", "CheckoutId"),
    payable: Number(pick(commit.json, "payableAmount", "PayableAmount")),
    shippingAmount: Number(shippingAmount ?? pick(commit.json, "shippingAmount", "ShippingAmount")),
    sellers: (pick(commit.json, "sellerOrders", "SellerOrders") || []).map((s) => ({
      sellerOrderId: pick(s, "sellerOrderId", "SellerOrderId"),
      sellerPartyId: pick(s, "sellerPartyId", "SellerPartyId"),
      name: pick(s, "sellerDisplayName", "SellerDisplayName"),
      payable: Number(pick(s, "payableAmount", "PayableAmount")),
    })),
    commitStatus: commit.status,
  };
}

async function initiateManual(ship, key) {
  return req(
    "POST",
    `/v1/storefront/checkout/${ship.checkoutId}/payments`,
    { cartId: ship.cartId, idempotencyKey: key, providerCode: "manual" },
    ship.guest,
  );
}

async function loadPayment(ship, paymentId) {
  return req(
    "GET",
    `/v1/storefront/payments/${paymentId}?cartId=${ship.cartId}`,
    undefined,
    ship.guest,
  );
}

function analyzeAlloc(paymentJson, ship) {
  const allocs = pick(paymentJson, "allocations", "Allocations") || [];
  const mapped = allocs.map((a) => ({
    targetKind: String(pick(a, "targetKind", "TargetKind") || "SellerOrder"),
    sellerOrderId: String(pick(a, "sellerOrderId", "SellerOrderId")),
    amount: Number(pick(a, "allocatedAmount", "AllocatedAmount")),
  }));
  const storeShip = mapped.filter((a) => a.targetKind === "StoreShipping");
  const sellers = mapped.filter((a) => a.targetKind === "SellerOrder");
  const sum = mapped.reduce((s, a) => s + a.amount, 0);
  const shippingOnSeller = sellers.some((a) => {
    const merchandise = ship.sellers.find((s) => String(s.sellerOrderId) === a.sellerOrderId)?.payable;
    return merchandise != null && a.amount > merchandise + 0.0001;
  });
  return { mapped, storeShip, sellers, sum, shippingOnSeller };
}

const forward = await commitCart([KG_OFFER, ARMAN_OFFER]);
note("A-multiseller-commit", {
  ok: forward.commitStatus === 200 && forward.sellers.length >= 2 && forward.shippingAmount > 0,
  checkoutId: forward.checkoutId,
  sellers: forward.sellers,
  shippingAmount: forward.shippingAmount,
  payable: forward.payable,
});

const pay1 = await initiateManual(forward, `r1-fwd-${forward.checkoutId}`);
const paymentId = pick(pay1.json, "paymentId", "PaymentId");
const amount1 = Number(pick(pay1.json, "amount", "Amount"));
const detail1 = await loadPayment(forward, paymentId);
const a1 = analyzeAlloc(detail1.json, forward);
note("A-allocation", {
  ok:
    pay1.status === 200 &&
    amount1 === forward.payable &&
    a1.sum === forward.payable &&
    a1.storeShip.length === 1 &&
    Math.abs(a1.storeShip[0].amount - forward.shippingAmount) < 0.0001 &&
    a1.sellers.length === forward.sellers.length &&
    !a1.shippingOnSeller,
  amount: amount1,
  allocs: a1.mapped,
  shippingOnSeller: a1.shippingOnSeller,
});

const replay = await initiateManual(forward, `r1-fwd-${forward.checkoutId}`);
note("D-idempotency", {
  ok: replay.status === 200 && String(pick(replay.json, "paymentId", "PaymentId")) === String(paymentId),
  paymentId2: pick(replay.json, "paymentId", "PaymentId"),
});

const reversed = await commitCart([ARMAN_OFFER, KG_OFFER]);
const pay2 = await initiateManual(reversed, `r1-rev-${reversed.checkoutId}`);
const detail2 = await loadPayment(reversed, pick(pay2.json, "paymentId", "PaymentId"));
const a2 = analyzeAlloc(detail2.json, reversed);
const bySellerName = (ship, analysis) =>
  ship.sellers.map((s) => ({
    name: s.name,
    merchandise: s.payable,
    allocated: analysis.sellers.find((a) => a.sellerOrderId === String(s.sellerOrderId))?.amount,
  }));
note("B-ordering-independence", {
  ok:
    a1.storeShip[0]?.amount === a2.storeShip[0]?.amount &&
    !a2.shippingOnSeller &&
    bySellerName(forward, a1).every((row) => {
      const match = bySellerName(reversed, a2).find((x) => x.name === row.name);
      return match && match.allocated === row.allocated && match.merchandise === row.merchandise;
    }),
  forward: bySellerName(forward, a1),
  reversed: bySellerName(reversed, a2),
  shippingForward: a1.storeShip[0]?.amount,
  shippingReversed: a2.storeShip[0]?.amount,
});

note("C-settlement-semantics", {
  ok: a1.sellers.every((s) => {
    const merch = forward.sellers.find((x) => String(x.sellerOrderId) === s.sellerOrderId)?.payable;
    return merch != null && Math.abs(merch - s.amount) < 0.0001;
  }),
  note: "seller allocation == seller merchandise; shipping excluded from seller buckets",
});

const single = await commitCart([KG_OFFER]);
const payS = await initiateManual(single, `r1-single-${single.checkoutId}`);
const detailS = await loadPayment(single, pick(payS.json, "paymentId", "PaymentId"));
const aS = analyzeAlloc(detailS.json, single);
note("E-single-store", {
  ok:
    payS.status === 200 &&
    Number(pick(payS.json, "amount", "Amount")) === single.payable &&
    aS.storeShip.length === (single.shippingAmount > 0 ? 1 : 0) &&
    !aS.shippingOnSeller,
  payable: single.payable,
  allocs: aS.mapped,
});

writeFileSync("docs/evidence/TB-P10-T003/r1-runtime-raw.json", JSON.stringify(out, null, 2));
if (!out.ok) process.exit(1);
