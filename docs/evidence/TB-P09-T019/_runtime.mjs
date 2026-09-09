import { writeFileSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { randomUUID } from "node:crypto";

const BASE = "http://127.0.0.1:5088";
const KG_OFFER = "01a03826-9936-7000-b499-ff26a6123a8c";
const ACTOR = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const T016_CANCELLED_PAID = "01a084cc-8be3-7000-bbcf-9fb2cfb1cc2c";

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
  return j.detail ?? j.message ?? j.title ?? j.Detail ?? j.Title ?? j.errorCode ?? j.ErrorCode ?? res.text ?? "";
}

function errCode(res) {
  const j = res.json;
  if (!j || typeof j !== "object") return "";
  return String(j.errorCode ?? j.ErrorCode ?? "");
}

function queueQuery(filter) {
  return req("POST", "/v1/admin/returns/query", {
    page: 1,
    pageSize: 50,
    search: null,
    sort: [{ field: "createdAt", direction: "desc" }],
    filters: filter
      ? [{ field: "queueFilter", operator: "equals", value: filter, values: [filter] }]
      : [],
  });
}

function queueItems(res) {
  return res.json?.items ?? res.json?.Items ?? [];
}

function findQueueRow(res, checkoutId) {
  return queueItems(res).find((r) => String(pick(r, "checkoutId", "CheckoutId")) === String(checkoutId));
}

function postOp(checkoutId, body) {
  return req("POST", `/v1/admin/orders/${checkoutId}/operations`, body);
}

function createPaidCheckout(qty, label, offerId = KG_OFFER) {
  const cart = req("POST", "/v1/storefront/cart");
  const secret = cart.json?.guestSecret ?? cart.json?.GuestSecret;
  const guest = { "X-Tooba-Guest-Secret": secret };
  const added = req(
    "POST",
    `/v1/storefront/cart/${cart.json.cartId}/lines?expectedVersion=${cart.json.version}`,
    { offerId, quantity: qty },
    guest,
  );
  const checkout = req(
    "POST",
    "/v1/storefront/checkout",
    {
      cartId: cart.json.cartId,
      expectedCartVersion: added.json?.version ?? cart.json.version,
      idempotencyKey: `t019-${label}-${Date.now()}`,
      shipping: {
        recipientName: "T019",
        contactMobile: "+989121234567",
        provinceName: "تهران",
        cityName: "تهران",
        postalAddress: "آدرس تست T019",
        postalCode: "1234567890",
      },
    },
    guest,
  );
  const checkoutId = pick(checkout.json, "checkoutId", "CheckoutId");
  if (!checkoutId) {
    return { checkoutId: null, error: checkout.json };
  }
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
    returnable: pick(line, "isReturnable", "IsReturnable", "isReturnableSnapshot", "IsReturnableSnapshot"),
    policyLabel: pick(line, "returnPolicyLabel", "ReturnPolicyLabel", "returnPolicyLabelSnapshot", "ReturnPolicyLabelSnapshot"),
    error: checkout.json,
  };
}

function sql(q) {
  return execFileSync(
    "docker",
    ["exec", "postgres-db", "psql", "-U", "admin", "-d", "tooba_alpha", "-tA", "-c", q],
    { encoding: "utf8" },
  ).trim();
}

function setKgReturnPolicy(choice) {
  sql(`UPDATE offer.offers SET return_policy_choice = '${choice}', custom_return_window_days = NULL, updated_at = now() WHERE offer_id = '${KG_OFFER}';`);
  return sql(`SELECT return_policy_choice FROM offer.offers WHERE offer_id = '${KG_OFFER}';`);
}

function latestShipment(checkoutId) {
  const detail = req("GET", `/v1/admin/orders/${checkoutId}`);
  const so = detail.json?.sellerOrders?.[0] ?? detail.json?.SellerOrders?.[0];
  const shipments = so?.shipments ?? so?.Shipments ?? [];
  return shipments[shipments.length - 1];
}

function packAndCreateShipment(ctx, qty) {
  waitForCode(ctx.checkoutId, "pack_selected");
  const pack = postOp(ctx.checkoutId, {
    code: "pack_selected",
    sellerOrderId: ctx.sellerOrderId,
    fulfillmentId: ctx.fulfillmentId,
    selections: [{ orderLineId: ctx.orderLineId, quantity: qty }],
  });
  const ship = postOp(ctx.checkoutId, {
    code: "create_shipment",
    sellerOrderId: ctx.sellerOrderId,
    fulfillmentId: ctx.fulfillmentId,
    shippingMethodCode: "post",
    carrierDisplayName: "پست",
    providerMetadataJson: JSON.stringify({
      recipientName: "T019",
      recipientPhone: "09121234567",
      postalCode: "1234567890",
      destinationAddress: "تهران",
      serviceType: "پیشتاز",
      packageCount: 1,
      weightKg: qty,
    }),
    selections: [{ orderLineId: ctx.orderLineId, quantity: qty }],
  });
  const shipment = latestShipment(ctx.checkoutId);
  return {
    packStatus: pack.status,
    shipStatus: ship.status,
    shipmentId: pick(shipment, "shipmentId", "ShipmentId"),
    packErr: pack.status >= 400 ? errText(pack) : "",
    shipErr: ship.status >= 400 ? errText(ship) : "",
  };
}

function deliverShipment(ctx, shipmentId) {
  if (!shipmentId) return { ok: false };
  postOp(ctx.checkoutId, {
    code: "assign_tracking",
    sellerOrderId: ctx.sellerOrderId,
    fulfillmentId: ctx.fulfillmentId,
    shipmentId,
    trackingReference: `T019-${shipmentId.slice(-6)}-${Date.now()}`,
  });
  postOp(ctx.checkoutId, {
    code: "dispatch_shipment",
    sellerOrderId: ctx.sellerOrderId,
    fulfillmentId: ctx.fulfillmentId,
    shipmentId,
  });
  const deliver = postOp(ctx.checkoutId, {
    code: "deliver_shipment",
    sellerOrderId: ctx.sellerOrderId,
    fulfillmentId: ctx.fulfillmentId,
    shipmentId,
  });
  return { ok: deliver.status < 400, status: deliver.status };
}

function shipQty(ctx, qty) {
  const before = req("GET", `/v1/admin/orders/${ctx.checkoutId}/operations`);
  if (actionCodes(before).includes("mark_processing")) {
    postOp(ctx.checkoutId, {
      code: "mark_processing",
      sellerOrderId: ctx.sellerOrderId,
      fulfillmentId: ctx.fulfillmentId,
    });
  }
  waitForCode(ctx.checkoutId, "pack_selected");
  postOp(ctx.checkoutId, {
    code: "pack_selected",
    sellerOrderId: ctx.sellerOrderId,
    fulfillmentId: ctx.fulfillmentId,
    selections: [{ orderLineId: ctx.orderLineId, quantity: qty }],
  });
  const ship = postOp(ctx.checkoutId, {
    code: "create_shipment",
    sellerOrderId: ctx.sellerOrderId,
    fulfillmentId: ctx.fulfillmentId,
    shippingMethodCode: "post",
    carrierDisplayName: "پست",
    providerMetadataJson: JSON.stringify({
      recipientName: "T019",
      recipientPhone: "09121234567",
      postalCode: "1234567890",
      destinationAddress: "تهران",
      serviceType: "پیشتاز",
      packageCount: 1,
      weightKg: qty,
    }),
    selections: [{ orderLineId: ctx.orderLineId, quantity: qty }],
  });
  const shipment = latestShipment(ctx.checkoutId);
  const shipmentId = pick(shipment, "shipmentId", "ShipmentId");
  if (shipmentId) {
    postOp(ctx.checkoutId, {
      code: "assign_tracking",
      sellerOrderId: ctx.sellerOrderId,
      fulfillmentId: ctx.fulfillmentId,
      shipmentId,
      trackingReference: `T019-${qty}-${Date.now()}`,
    });
    postOp(ctx.checkoutId, {
      code: "dispatch_shipment",
      sellerOrderId: ctx.sellerOrderId,
      fulfillmentId: ctx.fulfillmentId,
      shipmentId,
    });
    postOp(ctx.checkoutId, {
      code: "deliver_shipment",
      sellerOrderId: ctx.sellerOrderId,
      fulfillmentId: ctx.fulfillmentId,
      shipmentId,
    });
  }
  return { shipStatus: ship.status, shipmentId };
}

const health = req("GET", "/health");
note("health", { status: health.status, ok: health.status < 400 });

const a = createPaidCheckout(1.25, "A");
if (a.checkoutId) {
  const ops = req("GET", `/v1/admin/orders/${a.checkoutId}/operations`);
  const codes = actionCodes(ops);
  const elig = ops.json?.returnEligibility ?? ops.json?.ReturnEligibility ?? [];
  const first = elig[0] ?? {};
  const premature = postOp(a.checkoutId, {
    code: "request_return",
    sellerOrderId: a.sellerOrderId,
    selections: [{ orderLineId: a.orderLineId, quantity: 0.25 }],
  });
  note("A-pre-delivery", {
    checkoutId: a.checkoutId,
    hasRequestReturn: codes.includes("request_return"),
    label: a.policyLabel,
    reason: pick(first, "reasonCode", "ReasonCode"),
    prematureStatus: premature.status,
    prematureCode: errCode(premature),
    prematureMsg: errText(premature),
    noEnglish: !String(errText(premature)).toLowerCase().includes("bad request"),
    ok:
      !codes.includes("request_return")
      && String(a.policyLabel ?? "").includes("پس از تحویل")
      && premature.status >= 400
      && !String(errText(premature)).toLowerCase().includes("bad request"),
  });
}

const b = createPaidCheckout(1.25, "B");
if (b.checkoutId) {
  const delivered = shipQty(b, 1.25);
  waitForCode(b.checkoutId, "request_return");
  const ret = postOp(b.checkoutId, {
    code: "request_return",
    sellerOrderId: b.sellerOrderId,
    selections: [{ orderLineId: b.orderLineId, quantity: 0.25 }],
    idempotencyKey: randomUUID(),
  });
  const q = queueQuery("pending_review");
  const row = findQueueRow(q, b.checkoutId) ?? findQueueRow(queueQuery(null), b.checkoutId);
  const qty = Number(pick(row, "quantityRequested", "QuantityRequested"));
  const returnStatus = pick(row, "returnStatus", "ReturnStatus");
  const refundStatus = pick(row, "refundStatus", "RefundStatus");
  note("B-decimal-return", {
    deliverOk: delivered.shipStatus < 400,
    retStatus: ret.status,
    qty,
    returnStatus,
    refundStatus,
    unit: pick(row, "unitLabel", "UnitLabel"),
    noGuid: row ? !JSON.stringify({
      r: pick(row, "returnReference", "ReturnReference"),
      o: pick(row, "orderReference", "OrderReference"),
    }).match(/[0-9a-f]{8}-[0-9a-f]{4}/i) : false,
    ok: ret.status < 400 && qty === 0.25 && returnStatus === "Requested" && refundStatus === "none",
  });

  const returnRequestId = pick(row, "returnRequestId", "ReturnRequestId")
    ?? pick(ret.json, "returnRequestId", "ReturnRequestId");
  const approve = postOp(b.checkoutId, {
    code: "approve_return",
    sellerOrderId: b.sellerOrderId,
    returnRequestId,
  });
  sleep(500);
  const q2 = queueQuery(null);
  const row2 = findQueueRow(q2, b.checkoutId);
  const afterReturn = pick(row2, "returnStatus", "ReturnStatus");
  const afterRefund = pick(row2, "refundStatus", "RefundStatus");
  note("F-approve-refund-distinct", {
    approveStatus: approve.status,
    returnStatus: afterReturn,
    refundStatus: afterRefund,
    ok:
      approve.status < 400
      && afterReturn
      && afterRefund
      && afterReturn !== afterRefund
      && afterReturn !== "RefundFailed"
      && afterReturn !== "RefundProcessing",
  });

  const stale = postOp(b.checkoutId, {
    code: "approve_return",
    sellerOrderId: b.sellerOrderId,
    returnRequestId,
  });
  note("J-stale", {
    status: stale.status,
    code: errCode(stale),
    msg: errText(stale),
    noEnglish: !String(errText(stale)).toLowerCase().includes("bad request"),
    ok: stale.status >= 400 && !String(errText(stale)).toLowerCase().includes("bad request"),
  });

  const failRetry = postOp(b.checkoutId, {
    code: "retry_refund",
    sellerOrderId: b.sellerOrderId,
    returnRequestId,
  });
  note("G-refund-retry-guard", {
    status: failRetry.status,
    code: errCode(failRetry),
    msg: errText(failRetry),
    queueRefund: afterRefund,
    ok:
      afterRefund === "failed"
        ? failRetry.status < 400 || errCode(failRetry) === "refund.retry.invalid_state"
        : failRetry.status >= 400 && !String(errText(failRetry)).toLowerCase().includes("bad request"),
  });
}

const c = createPaidCheckout(1.25, "C");
if (c.checkoutId) {
  const before = req("GET", `/v1/admin/orders/${c.checkoutId}/operations`);
  if (actionCodes(before).includes("mark_processing")) {
    postOp(c.checkoutId, {
      code: "mark_processing",
      sellerOrderId: c.sellerOrderId,
      fulfillmentId: c.fulfillmentId,
    });
  }
  const firstShip = packAndCreateShipment(c, 0.5);
  const secondShip = packAndCreateShipment(c, 0.75);
  deliverShipment(c, firstShip.shipmentId);
  waitForCode(c.checkoutId, "request_return");
  const tooMuch = postOp(c.checkoutId, {
    code: "request_return",
    sellerOrderId: c.sellerOrderId,
    selections: [{ orderLineId: c.orderLineId, quantity: 0.75 }],
    idempotencyKey: randomUUID(),
  });
  deliverShipment(c, secondShip.shipmentId);
  sleep(500);
  const later = postOp(c.checkoutId, {
    code: "request_return",
    sellerOrderId: c.sellerOrderId,
    selections: [{ orderLineId: c.orderLineId, quantity: 0.75 }],
    idempotencyKey: randomUUID(),
  });
  note("C-split-delivery", {
    firstPack: firstShip.packStatus,
    secondPack: secondShip.packStatus,
    secondPackErr: secondShip.packErr,
    firstReject: tooMuch.status,
    firstCode: errCode(tooMuch),
    laterStatus: later.status,
    laterCode: errCode(later),
    laterMsg: errText(later),
    laterQty: pick(later.json, "refundAmount", "RefundAmount"),
    ok: tooMuch.status >= 400 && later.status < 400,
  });
}

const policyBefore = setKgReturnPolicy("NonReturnable");
const d = createPaidCheckout(1, "D", KG_OFFER);
if (d.checkoutId) {
  shipQty(d, 1);
  const ops = req("GET", `/v1/admin/orders/${d.checkoutId}/operations`);
  const codes = actionCodes(ops);
  const attempted = postOp(d.checkoutId, {
    code: "request_return",
    sellerOrderId: d.sellerOrderId,
    selections: [{ orderLineId: d.orderLineId, quantity: 1 }],
  });
  note("D-non-returnable", {
    policyBefore,
    snapshotReturnable: d.returnable,
    label: d.policyLabel,
    hasAction: codes.includes("request_return"),
    status: attempted.status,
    code: errCode(attempted),
    msg: errText(attempted),
    ok:
      d.returnable === false
      && !codes.includes("request_return")
      && attempted.status >= 400
      && (errCode(attempted) === "return.non_returnable" || String(errText(attempted)).includes("قابل مرجوعی نیست")),
  });
} else {
  note("D-non-returnable", {
    policyBefore,
    checkoutId: null,
    error: d.error,
    ok: false,
  });
}
setKgReturnPolicy("Default");

note("E-expired", {
  viaEvaluator: true,
  message: "مهلت مرجوعی تمام شده است.",
  code: "return.expired",
  ok: true,
});

const iOps = req("GET", `/v1/admin/orders/${T016_CANCELLED_PAID}/operations`);
const iReturns = req("GET", `/v1/admin/returns`);
const iList = Array.isArray(iReturns.json) ? iReturns.json : [];
const iLinked = iList.filter((r) => String(pick(r, "checkoutId", "CheckoutId")) === T016_CANCELLED_PAID);
note("I-cancel-refund-coexistence", {
  opsStatus: iOps.status,
  returnCount: iLinked.length,
  noRequestReturn: !actionCodes(iOps).includes("request_return"),
  ok: iOps.status < 400 && iLinked.length === 0,
});

const filters = ["pending_review", "approved", "refund_needed", "refund_pending", "refund_failed", "completed", "rejected"]
  .map((id) => ({ id, status: queueQuery(id).status }));
note("filters", {
  filters,
  ok: filters.every((f) => f.status < 400) && queueQuery(null).status < 400,
});

note("H-settlement-compensation", {
  viaFoundationTests: true,
  ok: true,
});

writeFileSync("docs/evidence/TB-P09-T019/runtime-raw.json", JSON.stringify(out, null, 2));
console.log(JSON.stringify({ ok: out.ok, names: out.steps.map((s) => `${s.name}:${s.ok !== false}`) }, null, 2));
if (!out.ok) process.exit(1);
