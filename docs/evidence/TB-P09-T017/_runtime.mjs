import { writeFileSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { randomUUID } from "node:crypto";

const BASE = "http://127.0.0.1:5088";
const KG_OFFER = "01a03826-9936-7000-b499-ff26a6123a8c";
const ARMAN_OFFER = "01a030d1-40f1-7000-95f6-b8efc58e2619";
const ACTOR = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const T015_DELIVERED = "01a084a4-4138-7000-a7d1-676e53356f73";
const T016_CANCELLED_PAID = "01a084cc-8be3-7000-bbcf-9fb2cfb1cc2c";
const T014_INVOICE = "01a08450-f2b0-7000-816d-552bb5fd54cd";

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

function sql(q) {
  try {
    return execFileSync(
      "docker",
      ["exec", "postgres-db", "psql", "-U", "admin", "-d", "tooba_alpha", "-t", "-A", "-c", q],
      { encoding: "utf8" },
    ).trim();
  } catch (err) {
    return `SQL_ERROR:${err.stderr || err.message}`;
  }
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

function findAction(ops, code) {
  return (ops.json?.actions ?? ops.json?.Actions ?? []).find((a) => (a.code ?? a.Code) === code);
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
  return j.detail ?? j.message ?? j.title ?? j.Detail ?? res.text ?? "";
}

function createPaidCheckout(offerId, qty, label, confirm = true) {
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
      idempotencyKey: `t017-${label}-${Date.now()}`,
      shipping: {
        recipientName: "T017",
        contactMobile: "+989121234567",
        provinceName: "تهران",
        cityName: "تهران",
        postalAddress: "آدرس تست T017",
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
  if (confirm) {
    waitForCode(checkoutId, "confirm_deposit");
    req("POST", `/v1/admin/orders/${checkoutId}/operations`, {
      code: "confirm_deposit",
      idempotencyKey: randomUUID().replaceAll("-", ""),
    });
  }
  let detail = req("GET", `/v1/admin/orders/${checkoutId}`);
  let seller = detail.json?.sellerOrders?.[0] ?? detail.json?.SellerOrders?.[0];
  let fulfillmentId = pick(seller, "fulfillmentId", "FulfillmentId");
  for (let i = 0; i < 40 && !fulfillmentId && confirm; i++) {
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

function gridQuery(statusValue) {
  return req("POST", "/v1/admin/orders/query", {
    page: 1,
    pageSize: 20,
    sort: [{ field: "created", direction: "desc" }],
    filters: statusValue
      ? [{ field: "status", operator: "equals", value: statusValue }]
      : [],
  });
}

const health = req("GET", "/health");
note("health", { status: health.status, ok: health.status < 400 });

const waiting = createPaidCheckout(KG_OFFER, 1.25, "A", false);
if (waiting.checkoutId) {
  const ops = waitForCode(waiting.checkoutId, "confirm_deposit");
  const codes = actionCodes(ops);
  note("A-waiting-manual", {
    codes,
    hasConfirm: codes.includes("confirm_deposit"),
    hasReject: codes.includes("reject_deposit"),
    hasCancel: codes.includes("cancel"),
    noFulfill: !codes.includes("mark_processing") && !codes.includes("pack_selected"),
    ok: codes.includes("confirm_deposit") && codes.includes("reject_deposit") && codes.includes("cancel"),
  });
}

const ready = createPaidCheckout(KG_OFFER, 1.25, "B", true);
if (ready.checkoutId) {
  const ops = waitForCode(ready.checkoutId, "mark_processing");
  const detail = req("GET", `/v1/admin/orders/${ready.checkoutId}`);
  const seller = detail.json?.sellerOrders?.[0] ?? detail.json?.SellerOrders?.[0];
  note("B-ready-to-fulfill", {
    codes: actionCodes(ops),
    sellerStatus: pick(seller, "status", "Status", "operationalStatus", "OperationalStatus"),
    hasProcess: actionCodes(ops).includes("mark_processing"),
    hasCancel: actionCodes(ops).includes("cancel"),
    ok: actionCodes(ops).includes("mark_processing") && actionCodes(ops).includes("cancel"),
  });
}

const pack = createPaidCheckout(KG_OFFER, 1.25, "C", true);
if (pack.checkoutId) {
  waitForCode(pack.checkoutId, "mark_processing");
  req("POST", `/v1/admin/orders/${pack.checkoutId}/operations`, {
    code: "mark_processing",
    sellerOrderId: pack.sellerOrderId,
    fulfillmentId: pack.fulfillmentId,
  });
  waitForCode(pack.checkoutId, "pack_selected");
  const packed = req("POST", `/v1/admin/orders/${pack.checkoutId}/operations`, {
    code: "pack_selected",
    sellerOrderId: pack.sellerOrderId,
    fulfillmentId: pack.fulfillmentId,
    selections: [{ orderLineId: pack.orderLineId, quantity: 0.5 }],
  });
  const after = req("GET", `/v1/admin/orders/${pack.checkoutId}`);
  const line = after.json?.sellerOrders?.[0]?.lines?.[0] ?? after.json?.SellerOrders?.[0]?.Lines?.[0];
  const packedQty = Number(pick(line, "quantityPacked", "QuantityPacked"));
  const orderedQty = Number(pick(line, "quantity", "Quantity"));
  note("C-partial-pack", {
    status: packed.status,
    packed: packedQty,
    ordered: orderedQty,
    remain: Number((orderedQty - packedQty).toFixed(2)),
    ok: packed.status < 400 && packedQty === 0.5 && orderedQty === 1.25,
  });
  const ship = req("POST", `/v1/admin/orders/${pack.checkoutId}/operations`, {
    code: "create_shipment",
    sellerOrderId: pack.sellerOrderId,
    fulfillmentId: pack.fulfillmentId,
    shippingMethodCode: "post",
    carrierDisplayName: "پست",
    providerMetadataJson: JSON.stringify({
      recipientName: "T017",
      recipientPhone: "09121234567",
      postalCode: "1234567890",
      destinationAddress: "تهران",
      serviceType: "پیشتاز",
      packageCount: 1,
      weightKg: 0.5,
    }),
    selections: [{ orderLineId: pack.orderLineId, quantity: 0.5 }],
  });
  const afterShip = req("GET", `/v1/admin/orders/${pack.checkoutId}`);
  const so = afterShip.json?.sellerOrders?.[0] ?? afterShip.json?.SellerOrders?.[0];
  const shipment = (so?.shipments ?? so?.Shipments ?? [])[0];
  const shipmentId = pick(shipment, "shipmentId", "ShipmentId");
  if (shipmentId) {
    req("POST", `/v1/admin/orders/${pack.checkoutId}/operations`, {
      code: "assign_tracking",
      sellerOrderId: pack.sellerOrderId,
      fulfillmentId: pick(so, "fulfillmentId", "FulfillmentId"),
      shipmentId,
      trackingReference: `T017-TRACK-${Date.now()}`,
    });
  }
  const dOps = req("GET", `/v1/admin/orders/${pack.checkoutId}/operations`);
  note("D-created-tracking-cancel", {
    shipStatus: ship.status,
    codes: actionCodes(dOps),
    cancel: actionCodes(dOps).includes("cancel"),
    confirm: findAction(dOps, "cancel")?.confirmMessageFa ?? findAction(dOps, "cancel")?.ConfirmMessageFa,
    ok: ship.status < 400 && actionCodes(dOps).includes("cancel") && actionCodes(dOps).filter((c) => c === "cancel").length === 1,
  });
}

const deliveredOps = req("GET", `/v1/admin/orders/${T015_DELIVERED}/operations`);
const deliveredCancel = req("POST", `/v1/admin/orders/${T015_DELIVERED}/operations`, {
  code: "cancel",
  idempotencyKey: randomUUID().replaceAll("-", ""),
});
note("E-dispatched-blocks-cancel", {
  codes: actionCodes(deliveredOps),
  cancelProjected: actionCodes(deliveredOps).includes("cancel"),
  postStatus: deliveredCancel.status,
  postMessage: errText(deliveredCancel),
  ok:
    !actionCodes(deliveredOps).includes("cancel")
    && deliveredCancel.status >= 400
    && String(errText(deliveredCancel)).includes("پس از ارسال کالا، لغو کامل سفارش امکان‌پذیر نیست."),
});

const cancelledOps = req("GET", `/v1/admin/orders/${T016_CANCELLED_PAID}/operations`);
const cancelledDetail = req("GET", `/v1/admin/orders/${T016_CANCELLED_PAID}`);
const cancelledHist = req("GET", `/v1/admin/orders/${T016_CANCELLED_PAID}/operational-history`);
const histItems = cancelledHist.json?.items ?? cancelledHist.json?.Items ?? cancelledHist.json?.entries ?? [];
const histText = JSON.stringify(cancelledHist.json ?? {});
note("F-cancelled-paid", {
  codes: actionCodes(cancelledOps),
  restore: actionCodes(cancelledOps).includes("restore_cancelled_order"),
  noForward:
    !actionCodes(cancelledOps).includes("confirm_deposit")
    && !actionCodes(cancelledOps).includes("mark_processing")
    && !actionCodes(cancelledOps).includes("pack_selected"),
  status: pick(cancelledDetail.json, "status", "Status"),
  historyHasCancel: histText.includes("سفارش لغو شد") || histText.includes("order_cancelled"),
  historyHasInventory: histText.includes("رزرو موجودی آزاد شد") || histText.includes("inventory_released"),
  ok:
    actionCodes(cancelledOps).includes("restore_cancelled_order")
    && !actionCodes(cancelledOps).includes("cancel")
    && !actionCodes(cancelledOps).includes("mark_processing"),
});

const gOps = req("GET", `/v1/admin/orders/${T015_DELIVERED}/operations`);
const gDetail = req("GET", `/v1/admin/orders/${T015_DELIVERED}`);
const returnPolicy = JSON.stringify(gDetail.json ?? {});
note("G-delivered", {
  codes: actionCodes(gOps),
  noCancel: !actionCodes(gOps).includes("cancel"),
  hasReturn: actionCodes(gOps).includes("request_return") || returnPolicy.includes("return") || returnPolicy.includes("مرجوع"),
  ok: !actionCodes(gOps).includes("cancel"),
});

const multi = sql(
  `SELECT checkout_id::text||'|'||count(*)::text FROM "order".seller_orders GROUP BY checkout_id HAVING count(*) > 1 ORDER BY count(*) DESC LIMIT 1;`,
);
note("H-multi-seller-row", { row: multi, ok: true });
if (multi && multi.includes("|")) {
  const checkoutId = multi.split("|")[0];
  const ops = req("GET", `/v1/admin/orders/${checkoutId}/operations`);
  const codes = actionCodes(ops);
  const processCount = codes.filter((c) => c === "mark_processing").length;
  const packCount = codes.filter((c) => c === "pack_selected").length;
  const whole = codes.filter((c) =>
    !["mark_processing", "mark_packed", "pack_selected", "unprocess", "unpack", "create_shipment", "cancel_shipment", "assign_tracking", "correct_tracking", "dispatch_shipment", "deliver_shipment", "request_return", "approve_return", "reject_return", "retry_refund"].includes(c),
  );
  note("H-multi-seller-actions", {
    checkoutId,
    codes,
    whole,
    processCount: codes.filter((c) => c === "mark_processing").length,
    packCount: codes.filter((c) => c === "pack_selected").length,
    cancelCount: whole.filter((c) => c === "cancel").length,
    ok: whole.filter((c) => c === "cancel").length <= 1 && !whole.includes("mark_processing") && !whole.includes("pack_selected"),
  });
}

const invoice = req("GET", `/v1/admin/orders/${T014_INVOICE}/invoice.html`);
const html = typeof invoice.json === "string" ? invoice.json : invoice.text ?? "";
note("print-invoice", {
  status: invoice.status,
  hasItemCount: html.includes("تعداد اقلام"),
  hasQty: html.includes("جمع مقدار") || html.includes("مقدار"),
  hasTax: html.includes("مالیات"),
  hasDuty: html.includes("عوارض"),
  noNoise: !html.includes(".000000"),
  ok: invoice.status < 400 && html.includes("تعداد اقلام") && html.includes("مالیات") && html.includes("عوارض"),
});

const list = gridQuery(null);
const rows = list.json?.items ?? list.json?.rows ?? list.json?.Items ?? [];
const listed = Array.isArray(rows) ? rows.find((r) => String(pick(r, "checkoutId", "CheckoutId")) === T016_CANCELLED_PAID) : null;
note("grid-list", {
  status: list.status,
  listed: Boolean(listed),
  lineCount: listed ? pick(listed, "lineCount", "LineCount") : null,
  ok: list.status < 400,
});

const returnFilter = gridQuery("ReturnRequested");
note("grid-status-return-filter", {
  status: returnFilter.status,
  ok: returnFilter.status < 400,
});

writeFileSync("docs/evidence/TB-P09-T017/runtime-raw.json", JSON.stringify(out, null, 2));
console.log(JSON.stringify(out, null, 2));
if (!out.ok) process.exit(1);
