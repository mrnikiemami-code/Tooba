import { writeFileSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { randomUUID } from "node:crypto";

const BASE = "http://127.0.0.1:5088";
const KG_OFFER = "01a03826-9936-7000-b499-ff26a6123a8c";
const ACTOR = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const T015_DELIVERED = "01a084a4-4138-7000-a7d1-676e53356f73";
const T016_CANCELLED_PAID = "01a084cc-8be3-7000-bbcf-9fb2cfb1cc2c";
const MULTI = "01a08175-32ba-7000-9551-daa24d42da61";

const headers = {
  Host: "alpha.localhost",
  "X-Tooba-Dev-Actor-User-Id": ACTOR,
  "Content-Type": "application/json",
};

function req(method, path, body, extra = {}) {
  const args = ["-sS", "-o", "-", "-w", "\n%{http_code}", "-X", method, `${BASE}${path}`];
  const all = { ...headers, ...extra };
  if (all["X-Tooba-Guest-Secret"]) delete all["X-Tooba-Dev-Actor-User-Id"];
  for (const [key, value] of Object.entries(all)) args.push("-H", `${key}: ${value}`);
  if (body !== undefined) args.push("--data-binary", JSON.stringify(body));
  const raw = execFileSync("curl.exe", args, { encoding: "utf8" });
  const idx = raw.lastIndexOf("\n");
  const text = idx >= 0 ? raw.slice(0, idx) : raw;
  const status = Number(idx >= 0 ? raw.slice(idx + 1) : 0);
  let json;
  try { json = JSON.parse(text); } catch { json = text; }
  return { status, json, text };
}

function sleep(ms) {
  Atomics.wait(new Int32Array(new SharedArrayBuffer(4)), 0, 0, ms);
}

const out = { ok: true, steps: [] };
function note(name, data) {
  const row = { name, ...(data ?? {}) };
  out.steps.push(row);
  if (data && data.ok === false) out.ok = false;
}

function pick(obj, ...keys) {
  if (!obj || typeof obj !== "object") return undefined;
  for (const key of keys) if (obj[key] != null) return obj[key];
  const lower = Object.fromEntries(Object.entries(obj).map(([k, v]) => [k.toLowerCase(), v]));
  for (const key of keys) if (lower[key.toLowerCase()] != null) return lower[key.toLowerCase()];
  return undefined;
}

function actionCodes(ops) {
  return (ops.json?.actions ?? ops.json?.Actions ?? []).map((a) => a.code ?? a.Code);
}

function waitForCode(checkoutId, code, tries = 40) {
  let ops;
  for (let i = 0; i < tries; i++) {
    ops = req("GET", `/v1/admin/orders/${checkoutId}/operations`);
    if (actionCodes(ops).includes(code)) return ops;
    sleep(250);
  }
  return ops;
}

function errText(res) {
  const j = res.json;
  if (!j || typeof j !== "object") return String(res.text ?? "");
  return j.detail ?? j.message ?? j.title ?? j.Detail ?? j.Title ?? res.text ?? "";
}

function queueQuery(filter) {
  return req("POST", "/v1/admin/fulfillments/work-queue/query", {
    page: 1,
    pageSize: 50,
    search: null,
    sort: [{ field: "updatedAt", direction: "desc" }],
    filters: filter
      ? [{ field: "queueFilter", operator: "equals", value: filter, values: [filter] }]
      : [],
  });
}

function queueItems(res) {
  return res.json?.items ?? res.json?.Items ?? [];
}

function findQueueRow(res, checkoutId) {
  return queueItems(res).find((r) => String(pick(r, "checkoutId", "CheckoutId")) === checkoutId);
}

function codesOf(row) {
  return row?.availableActionCodes ?? row?.AvailableActionCodes ?? [];
}

function postOp(checkoutId, body) {
  return req("POST", `/v1/admin/orders/${checkoutId}/operations`, body);
}

function createPaidCheckout(qty, label) {
  const cart = req("POST", "/v1/storefront/cart");
  const secret = cart.json?.guestSecret ?? cart.json?.GuestSecret;
  const guest = { "X-Tooba-Guest-Secret": secret };
  const added = req(
    "POST",
    `/v1/storefront/cart/${cart.json.cartId}/lines?expectedVersion=${cart.json.version}`,
    { offerId: KG_OFFER, quantity: qty },
    guest,
  );
  const checkout = req(
    "POST",
    "/v1/storefront/checkout",
    {
      cartId: cart.json.cartId,
      expectedCartVersion: added.json?.version ?? cart.json.version,
      idempotencyKey: `t018-${label}-${Date.now()}`,
      shipping: {
        recipientName: "T018",
        contactMobile: "+989121234567",
        provinceName: "تهران",
        cityName: "تهران",
        postalAddress: "آدرس تست T018",
        postalCode: "1234567890",
      },
    },
    guest,
  );
  const checkoutId = pick(checkout.json, "checkoutId", "CheckoutId");
  note(`${label}-checkout`, {
    status: checkout.status,
    checkoutId,
    error: checkout.status >= 400 ? checkout.json : undefined,
    ok: checkout.status < 400 && Boolean(checkoutId),
  });
  if (!checkoutId) return {};
  req(
    "POST",
    `/v1/storefront/checkout/${checkoutId}/payments`,
    { cartId: cart.json.cartId, idempotencyKey: randomUUID().replaceAll("-", ""), providerCode: "manual" },
    guest,
  );
  waitForCode(checkoutId, "confirm_deposit");
  req("POST", `/v1/admin/orders/${checkoutId}/operations`, {
    code: "confirm_deposit",
    idempotencyKey: randomUUID().replaceAll("-", ""),
  });
  let detail = req("GET", `/v1/admin/orders/${checkoutId}`);
  let seller = detail.json?.sellerOrders?.[0] ?? detail.json?.SellerOrders?.[0];
  let fulfillmentId = pick(seller, "fulfillmentId", "FulfillmentId");
  for (let i = 0; i < 40 && !fulfillmentId; i++) {
    sleep(250);
    detail = req("GET", `/v1/admin/orders/${checkoutId}`);
    seller = detail.json?.sellerOrders?.[0] ?? detail.json?.SellerOrders?.[0];
    fulfillmentId = pick(seller, "fulfillmentId", "FulfillmentId");
  }
  const line = seller?.lines?.[0] ?? seller?.Lines?.[0];
  return {
    checkoutId,
    sellerOrderId: pick(seller, "sellerOrderId", "SellerOrderId"),
    fulfillmentId,
    orderLineId: pick(line, "orderLineId", "OrderLineId"),
  };
}

function noGuidLeak(row) {
  const blob = JSON.stringify({
    orderReference: pick(row, "orderReference", "OrderReference"),
    sellerDisplayName: pick(row, "sellerDisplayName", "SellerDisplayName"),
    trackingSummary: pick(row, "trackingSummary", "TrackingSummary"),
    qty: pick(row, "quantityOrdered", "QuantityOrdered"),
  });
  return !String(blob).includes(".000000");
}

const health = req("GET", "/health");
note("health", { status: health.status, ok: health.status < 400 });

const a = createPaidCheckout(1.25, "A");
if (a.checkoutId) {
  waitForCode(a.checkoutId, "mark_processing");
  const q = queueQuery("ready_to_process");
  const row = findQueueRow(q, a.checkoutId);
  const codes = codesOf(row);
  note("A-ready-to-fulfill", {
    queryStatus: q.status,
    qty: pick(row, "quantityOrdered", "QuantityOrdered"),
    codes,
    hasProcess: codes.includes("mark_processing"),
    noPack: !codes.includes("mark_packed"),
    noDispatch: !codes.includes("dispatch_shipment"),
    noGuid: row ? noGuidLeak(row) : false,
    ok:
      q.status < 400
      && Boolean(row)
      && codes.includes("mark_processing")
      && !codes.includes("mark_packed")
      && !codes.includes("dispatch_shipment")
      && Number(pick(row, "quantityOrdered", "QuantityOrdered")) === 1.25,
  });
}

const b = createPaidCheckout(1.25, "B");
if (b.checkoutId) {
  waitForCode(b.checkoutId, "mark_processing");
  postOp(b.checkoutId, {
    code: "mark_processing",
    sellerOrderId: b.sellerOrderId,
    fulfillmentId: b.fulfillmentId,
  });
  waitForCode(b.checkoutId, "pack_selected");
  const packed = postOp(b.checkoutId, {
    code: "pack_selected",
    sellerOrderId: b.sellerOrderId,
    fulfillmentId: b.fulfillmentId,
    selections: [{ orderLineId: b.orderLineId, quantity: 0.5 }],
  });
  const q = queueQuery("ready_to_pack");
  const row = findQueueRow(q, b.checkoutId) ?? findQueueRow(queueQuery(null), b.checkoutId);
  const after = req("GET", `/v1/admin/orders/${b.checkoutId}`);
  const line = after.json?.sellerOrders?.[0]?.lines?.[0] ?? after.json?.SellerOrders?.[0]?.Lines?.[0];
  const packedQty = Number(pick(line, "quantityPacked", "QuantityPacked"));
  note("B-partial-pack", {
    packStatus: packed.status,
    packed: packedQty,
    remain: Number((1.25 - packedQty).toFixed(2)),
    queueCodes: codesOf(row),
    ok: packed.status < 400 && packedQty === 0.5,
  });

  const ship = postOp(b.checkoutId, {
    code: "create_shipment",
    sellerOrderId: b.sellerOrderId,
    fulfillmentId: b.fulfillmentId,
    shippingMethodCode: "post",
    carrierDisplayName: "پست",
    providerMetadataJson: JSON.stringify({
      recipientName: "T018",
      recipientPhone: "09121234567",
      postalCode: "1234567890",
      destinationAddress: "تهران",
      serviceType: "پیشتاز",
      packageCount: 1,
      weightKg: 0.5,
    }),
    selections: [{ orderLineId: b.orderLineId, quantity: 0.5 }],
  });
  const detail = req("GET", `/v1/admin/orders/${b.checkoutId}`);
  const so = detail.json?.sellerOrders?.[0] ?? detail.json?.SellerOrders?.[0];
  const shipment = (so?.shipments ?? so?.Shipments ?? [])[0];
  const shipmentId = pick(shipment, "shipmentId", "ShipmentId");
  const q2 = queueQuery(null);
  const row2 = findQueueRow(q2, b.checkoutId);
  note("C-create-shipment", {
    shipStatus: ship.status,
    shipmentId,
    queueShipmentId: pick(row2, "primaryShipmentId", "PrimaryShipmentId"),
    sameShipment: Boolean(shipmentId) && String(pick(row2, "primaryShipmentId", "PrimaryShipmentId")) === String(shipmentId),
    ok: ship.status < 400 && Boolean(shipmentId) && Boolean(row2),
  });

  if (shipmentId) {
    const track = postOp(b.checkoutId, {
      code: "assign_tracking",
      sellerOrderId: b.sellerOrderId,
      fulfillmentId: b.fulfillmentId,
      shipmentId,
      trackingReference: `T018-TRACK-${Date.now()}`,
    });
    const q3 = queueQuery("missing_tracking");
    const stillMissing = findQueueRow(q3, b.checkoutId);
    const qAll = queueQuery(null);
    const tracked = findQueueRow(qAll, b.checkoutId);
    const trackedCodes = codesOf(tracked);
    note("D-tracking", {
      trackStatus: track.status,
      missingGone: !stillMissing,
      codes: trackedCodes,
      hasCorrect: trackedCodes.includes("correct_tracking"),
      hasVoid: trackedCodes.includes("cancel_shipment"),
      hasDispatch: trackedCodes.includes("dispatch_shipment"),
      ok:
        track.status < 400
        && !stillMissing
        && trackedCodes.includes("correct_tracking")
        && trackedCodes.includes("cancel_shipment")
        && trackedCodes.includes("dispatch_shipment"),
    });

    const dispatch = postOp(b.checkoutId, {
      code: "dispatch_shipment",
      sellerOrderId: b.sellerOrderId,
      fulfillmentId: b.fulfillmentId,
      shipmentId,
    });
    const afterOps = req("GET", `/v1/admin/orders/${b.checkoutId}/operations`);
    const afterCodes = actionCodes(afterOps);
    const cancelAttempt = postOp(b.checkoutId, { code: "cancel", idempotencyKey: randomUUID().replaceAll("-", "") });
    const staleDispatch = postOp(b.checkoutId, {
      code: "dispatch_shipment",
      sellerOrderId: b.sellerOrderId,
      fulfillmentId: b.fulfillmentId,
      shipmentId,
    });
    const staleVoid = postOp(b.checkoutId, {
      code: "cancel_shipment",
      sellerOrderId: b.sellerOrderId,
      fulfillmentId: b.fulfillmentId,
      shipmentId,
    });
    note("E-dispatch-boundary", {
      dispatchStatus: dispatch.status,
      cancelProjected: afterCodes.includes("cancel"),
      cancelPost: cancelAttempt.status,
      cancelMsg: errText(cancelAttempt),
      staleDispatchStatus: staleDispatch.status,
      staleDispatchMsg: errText(staleDispatch),
      staleVoidStatus: staleVoid.status,
      staleVoidMsg: errText(staleVoid),
      noEnglishDispatch: !String(errText(staleDispatch)).includes("dispatch از") && !String(errText(staleVoid)).toLowerCase().includes("bad request"),
      ok:
        dispatch.status < 400
        && !afterCodes.includes("cancel")
        && cancelAttempt.status >= 400
        && String(errText(cancelAttempt)).includes("پس از ارسال کالا")
        && staleDispatch.status >= 400
        && staleVoid.status >= 400
        && !String(errText(staleDispatch)).includes("dispatch از")
        && !String(errText(staleVoid)).includes("JSON"),
    });
  }
}

const multiDetail = req("GET", `/v1/admin/orders/${MULTI}`);
const sellers = multiDetail.json?.sellerOrders ?? multiDetail.json?.SellerOrders ?? [];
const sellerIds = sellers.map((s) => pick(s, "sellerPartyId", "SellerPartyId")).filter(Boolean);
const qMulti = queueQuery(null);
const multiRows = queueItems(qMulti).filter((r) => String(pick(r, "checkoutId", "CheckoutId")) === MULTI);
const uniqueSellers = [...new Set(multiRows.map((r) => String(pick(r, "sellerPartyId", "SellerPartyId") ?? "")))];
let bulkCross = { status: 0, json: {} };
const first = multiRows[0];
const other = multiRows.find((r) => String(pick(r, "sellerPartyId", "SellerPartyId")) !== String(pick(first, "sellerPartyId", "SellerPartyId")));
if (first && other) {
  bulkCross = req("POST", "/v1/admin/fulfillments/work-queue/bulk", {
    actionCode: "mark_processing",
    items: [first, other].map((r) => ({
      checkoutId: pick(r, "checkoutId", "CheckoutId"),
      fulfillmentId: pick(r, "fulfillmentId", "FulfillmentId"),
      sellerOrderId: pick(r, "sellerOrderId", "SellerOrderId"),
      shipmentId: pick(r, "primaryShipmentId", "PrimaryShipmentId"),
    })),
  });
}
const bulkMsg = errText(bulkCross);
note("F-multi-seller", {
  orderSellers: sellerIds.length,
  queueRows: multiRows.length,
  uniqueSellers: uniqueSellers.length,
  bulkStatus: bulkCross.status,
  bulkMsg,
  ok:
    uniqueSellers.length >= 2
    && bulkCross.status >= 400
    && (bulkMsg.includes("فروشنده") || bulkMsg.includes("ناسازگار")),
});

const cancelledQ = queueQuery(null);
const cancelledRow = findQueueRow(cancelledQ, T016_CANCELLED_PAID);
const cancelledCodes = codesOf(cancelledRow ?? {});
const staleProcess = postOp(T016_CANCELLED_PAID, {
  code: "mark_processing",
  sellerOrderId: pick(cancelledRow, "sellerOrderId", "SellerOrderId"),
  fulfillmentId: pick(cancelledRow, "fulfillmentId", "FulfillmentId"),
});
note("G-cancelled", {
  rowPresent: Boolean(cancelledRow),
  codes: cancelledCodes,
  staleStatus: staleProcess.status,
  staleMsg: errText(staleProcess),
  ok:
    cancelledCodes.every((c) => !["mark_processing", "mark_packed", "create_shipment", "dispatch_shipment"].includes(c))
    && staleProcess.status >= 400
    && (String(errText(staleProcess)).includes("لغو") || String(errText(staleProcess)).includes("مجاز نیست")),
});

const deliveredQ = queueQuery("delivered");
note("filters", {
  ready: queueQuery("ready_to_process").status,
  pack: queueQuery("ready_to_pack").status,
  ship: queueQuery("ready_to_ship").status,
  missing: queueQuery("missing_tracking").status,
  transit: queueQuery("in_transit").status,
  delivered: deliveredQ.status,
  problem: queueQuery("problem").status,
  ok:
    queueQuery("ready_to_process").status < 400
    && deliveredQ.status < 400,
});

const deliveredRow = findQueueRow(queueQuery(null), T015_DELIVERED);
note("G2-delivered-queue", {
  codes: codesOf(deliveredRow ?? {}),
  noCancel: !(codesOf(deliveredRow ?? {}).includes("cancel")),
  ok: !codesOf(deliveredRow ?? {}).includes("mark_processing"),
});

writeFileSync("docs/evidence/TB-P09-T018/runtime-raw.json", JSON.stringify(out, null, 2));
console.log(JSON.stringify({ ok: out.ok, names: out.steps.map((s) => `${s.name}:${s.ok !== false}`) }, null, 2));
if (!out.ok) process.exit(1);
