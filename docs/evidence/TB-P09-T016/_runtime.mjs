import { writeFileSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { randomUUID } from "node:crypto";

const BASE = "http://127.0.0.1:5088";
const KG_OFFER = "01a03826-9936-7000-b499-ff26a6123a8c";
const ARMAN_OFFER = "01a030d1-40f1-7000-95f6-b8efc58e2619";
const ACTOR = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const T015_DELIVERED = "01a084a4-4138-7000-a7d1-676e53356f73";

const headers = {
  Host: "alpha.localhost",
  "X-Tooba-Dev-Actor-User-Id": ACTOR,
  "Content-Type": "application/json",
};

function req(method, path, body, extra = {}) {
  const args = ["-sS", "-o", "-", "-w", "\n%{http_code}", "-X", method, `${BASE}${path}`];
  const all = { ...headers, ...extra };
  if (all["X-Tooba-Guest-Secret"]) {
    delete all["X-Tooba-Dev-Actor-User-Id"];
  }
  for (const [key, value] of Object.entries(all)) {
    args.push("-H", `${key}: ${value}`);
  }
  if (body !== undefined) {
    args.push("--data-binary", JSON.stringify(body));
  }
  const raw = execFileSync("curl.exe", args, { encoding: "utf8" });
  const idx = raw.lastIndexOf("\n");
  const text = idx >= 0 ? raw.slice(0, idx) : raw;
  const status = Number(idx >= 0 ? raw.slice(idx + 1) : 0);
  let json;
  try {
    json = JSON.parse(text);
  } catch {
    json = text;
  }
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
  for (const key of keys) {
    if (obj[key] != null) return obj[key];
  }
  const lower = Object.fromEntries(Object.entries(obj).map(([k, v]) => [k.toLowerCase(), v]));
  for (const key of keys) {
    if (lower[key.toLowerCase()] != null) return lower[key.toLowerCase()];
  }
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

function createPaidCheckout(offerId, qty, label) {
  const cart = req("POST", "/v1/storefront/cart");
  const secret = cart.json?.guestSecret ?? cart.json?.GuestSecret;
  const guest = { "X-Tooba-Guest-Secret": secret };
  const added = req(
    "POST",
    `/v1/storefront/cart/${cart.json.cartId}/lines?expectedVersion=${cart.json.version}`,
    { offerId, quantity: qty },
    guest,
  );
  const addQty = added.json?.lines?.[0]?.quantity ?? added.json?.Lines?.[0]?.Quantity;
  note(`${label}-cart`, {
    status: added.status,
    qty: addQty,
    error: added.status >= 400 ? added.json : undefined,
    ok: added.status < 400 && Number(addQty) === qty,
  });
  const checkout = req(
    "POST",
    "/v1/storefront/checkout",
    {
      cartId: cart.json.cartId,
      expectedCartVersion: added.json?.version ?? cart.json.version,
      idempotencyKey: `t016-${label}-${Date.now()}`,
      shipping: {
        recipientName: "T016",
        contactMobile: "+989121234567",
        provinceName: "تهران",
        cityName: "تهران",
        postalAddress: "آدرس تست T016",
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
  if (!checkoutId) return { checkoutId: null, sellerOrderId: null, fulfillmentId: null, orderLineId: null };
  const pay = req(
    "POST",
    `/v1/storefront/checkout/${checkoutId}/payments`,
    { cartId: cart.json.cartId, idempotencyKey: randomUUID().replaceAll("-", ""), providerCode: "manual" },
    guest,
  );
  note(`${label}-pay`, { status: pay.status, payStatus: pick(pay.json, "status", "Status") });
  waitForCode(checkoutId, "confirm_deposit");
  const confirm = req("POST", `/v1/admin/orders/${checkoutId}/operations`, {
    code: "confirm_deposit",
    idempotencyKey: randomUUID().replaceAll("-", ""),
  });
  note(`${label}-confirm`, { status: confirm.status, error: confirm.status >= 400 ? confirm.json : undefined, ok: confirm.status < 400 });
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

const health = req("GET", "/health");
note("health", { status: health.status, ok: health.status < 400 || health.status === 404 });

const deliveredOps = req("GET", `/v1/admin/orders/${T015_DELIVERED}/operations`);
const deliveredCodes = actionCodes(deliveredOps);
const deliveredCancel = req("POST", `/v1/admin/orders/${T015_DELIVERED}/operations`, {
  code: "cancel",
  idempotencyKey: randomUUID().replaceAll("-", ""),
});
function errText(res) {
  const j = res.json;
  if (!j || typeof j !== "object") return String(res.text ?? "");
  return j.detail ?? j.message ?? j.title ?? j.Detail ?? res.text ?? "";
}

function errCode(res) {
  return res.json?.errorCode ?? res.json?.code ?? res.json?.extensions?.errorCode;
}

const deliveredMsg = errText(deliveredCancel);
note("smoke-B-delivered-blocks", {
  status: deliveredOps.status,
  codes: deliveredCodes,
  cancelProjected: deliveredCodes.includes("cancel"),
  postStatus: deliveredCancel.status,
  postCode: errCode(deliveredCancel),
  postMessage: deliveredMsg,
  ok:
    !deliveredCodes.includes("cancel")
    && deliveredCancel.status >= 400
    && String(deliveredMsg).includes("پس از ارسال کالا، لغو کامل سفارش امکان‌پذیر نیست."),
});

const stockBefore = sql(`SELECT on_hand::text||'|'||reserved::text FROM inventory.stock_positions WHERE offer_id = '${KG_OFFER}' LIMIT 1;`);
note("kg-stock-before", { row: stockBefore });

const unpaidCart = req("POST", "/v1/storefront/cart");
const unpaidGuest = { "X-Tooba-Guest-Secret": unpaidCart.json?.guestSecret ?? unpaidCart.json?.GuestSecret };
const unpaidAdded = req(
  "POST",
  `/v1/storefront/cart/${unpaidCart.json.cartId}/lines?expectedVersion=${unpaidCart.json.version}`,
  { offerId: KG_OFFER, quantity: 1.25 },
  unpaidGuest,
);
note("smoke-D-unpaid-cart", {
  status: unpaidAdded.status,
  qty: unpaidAdded.json?.lines?.[0]?.quantity ?? unpaidAdded.json?.Lines?.[0]?.Quantity,
  error: unpaidAdded.status >= 400 ? unpaidAdded.json : undefined,
  ok: unpaidAdded.status < 400,
});
const unpaidCheckout = req(
  "POST",
  "/v1/storefront/checkout",
  {
    cartId: unpaidCart.json.cartId,
    expectedCartVersion: unpaidAdded.json?.version ?? unpaidCart.json.version,
    idempotencyKey: `t016-unpaid-${Date.now()}`,
    shipping: {
      recipientName: "T016 unpaid",
      contactMobile: "+989121234567",
      provinceName: "تهران",
      cityName: "تهران",
      postalAddress: "آدرس تست T016 unpaid",
      postalCode: "1234567890",
    },
  },
  unpaidGuest,
);
const unpaidId = pick(unpaidCheckout.json, "checkoutId", "CheckoutId");
note("smoke-D-unpaid-checkout", {
  status: unpaidCheckout.status,
  checkoutId: unpaidId,
  error: unpaidCheckout.status >= 400 ? unpaidCheckout.json : undefined,
  ok: unpaidCheckout.status < 400 && Boolean(unpaidId),
});
if (unpaidId) {
  const unpaidOps = waitForCode(unpaidId, "cancel");
  const unpaidConfirm = findAction(unpaidOps, "cancel");
  const unpaidCancel = req("POST", `/v1/admin/orders/${unpaidId}/operations`, {
    code: "cancel",
    idempotencyKey: randomUUID().replaceAll("-", ""),
  });
  const afterUnpaid = req("GET", `/v1/admin/orders/${unpaidId}/operations`);
  note("smoke-D-unpaid-cancel", {
    status: unpaidCancel.status,
    confirm: unpaidConfirm?.confirmMessageFa ?? unpaidConfirm?.ConfirmMessageFa,
    after: actionCodes(afterUnpaid),
    error: unpaidCancel.status >= 400 ? unpaidCancel.json : undefined,
    ok:
      unpaidCancel.status < 400
      && String(unpaidConfirm?.confirmMessageFa ?? unpaidConfirm?.ConfirmMessageFa ?? "").includes("موجودی آزاد")
      && !actionCodes(afterUnpaid).includes("cancel"),
  });
}

const paid = createPaidCheckout(KG_OFFER, 1.25, "smoke-A");
if (paid.checkoutId) {
  waitForCode(paid.checkoutId, "mark_processing");
  const start = req("POST", `/v1/admin/orders/${paid.checkoutId}/operations`, {
    code: "mark_processing",
    sellerOrderId: paid.sellerOrderId,
    fulfillmentId: paid.fulfillmentId,
  });
  note("smoke-A-process", { status: start.status, error: start.status >= 400 ? start.json : undefined, ok: start.status < 400 });
  waitForCode(paid.checkoutId, "pack_selected");
  const pack = req("POST", `/v1/admin/orders/${paid.checkoutId}/operations`, {
    code: "pack_selected",
    sellerOrderId: paid.sellerOrderId,
    fulfillmentId: paid.fulfillmentId,
    selections: [{ orderLineId: paid.orderLineId, quantity: 0.5 }],
  });
  note("smoke-E-pack-0.50", { status: pack.status, error: pack.status >= 400 ? pack.json : undefined, ok: pack.status < 400 });
  const afterPack = req("GET", `/v1/admin/orders/${paid.checkoutId}`);
  const packedLine = afterPack.json?.sellerOrders?.[0]?.lines?.[0] ?? afterPack.json?.SellerOrders?.[0]?.Lines?.[0];
  const packedQty = Number(pick(packedLine, "quantityPacked", "QuantityPacked"));
  const orderedQty = Number(pick(packedLine, "quantity", "Quantity"));
  note("smoke-E-decimal", {
    packed: packedQty,
    ordered: orderedQty,
    remain: Number((orderedQty - packedQty).toFixed(2)),
    ok: packedQty === 0.5 && orderedQty === 1.25,
  });
  const ship = req("POST", `/v1/admin/orders/${paid.checkoutId}/operations`, {
    code: "create_shipment",
    sellerOrderId: paid.sellerOrderId,
    fulfillmentId: pick(afterPack.json?.sellerOrders?.[0] ?? afterPack.json?.SellerOrders?.[0], "fulfillmentId", "FulfillmentId") ?? paid.fulfillmentId,
    shippingMethodCode: "post",
    carrierDisplayName: "پست",
    providerMetadataJson: JSON.stringify({
      recipientName: "T016",
      recipientPhone: "09121234567",
      postalCode: "1234567890",
      destinationAddress: "تهران، آدرس T016",
      serviceType: "پیشتاز",
      packageCount: 1,
      weightKg: 0.5,
    }),
    selections: [{ orderLineId: paid.orderLineId, quantity: 0.5 }],
  });
  note("smoke-A-shipment", { status: ship.status, error: ship.status >= 400 ? ship.json : undefined, ok: ship.status < 400 });
  const afterShip = req("GET", `/v1/admin/orders/${paid.checkoutId}`);
  const soShip = afterShip.json?.sellerOrders?.[0] ?? afterShip.json?.SellerOrders?.[0];
  const shipment = (soShip?.shipments ?? soShip?.Shipments ?? [])[0];
  const shipmentId = pick(shipment, "shipmentId", "ShipmentId");
  if (shipmentId) {
    const track = req("POST", `/v1/admin/orders/${paid.checkoutId}/operations`, {
      code: "assign_tracking",
      sellerOrderId: paid.sellerOrderId,
      fulfillmentId: pick(soShip, "fulfillmentId", "FulfillmentId"),
      shipmentId,
      trackingReference: `T016-TRACK-${Date.now()}`,
    });
    note("smoke-A-tracking", { status: track.status, error: track.status >= 400 ? track.json : undefined, ok: track.status < 400 });
  }
  const reservedBefore = sql(`SELECT reserved::text FROM inventory.stock_positions WHERE offer_id = '${KG_OFFER}' LIMIT 1;`);
  const beforeCancelOps = req("GET", `/v1/admin/orders/${paid.checkoutId}/operations`);
  const cancelAction = findAction(beforeCancelOps, "cancel");
  note("smoke-A-cancel-projected", {
    codes: actionCodes(beforeCancelOps),
    confirm: cancelAction?.confirmMessageFa ?? cancelAction?.ConfirmMessageFa,
    ok: actionCodes(beforeCancelOps).includes("cancel") && actionCodes(beforeCancelOps).filter((c) => c === "cancel").length === 1,
  });
  const cancel = req("POST", `/v1/admin/orders/${paid.checkoutId}/operations`, {
    code: "cancel",
    idempotencyKey: randomUUID().replaceAll("-", ""),
  });
  note("smoke-A-cancel", { status: cancel.status, error: cancel.status >= 400 ? cancel.json : undefined, ok: cancel.status < 400 });
  const afterCancelOps = req("GET", `/v1/admin/orders/${paid.checkoutId}/operations`);
  const reservedAfter = sql(`SELECT reserved::text FROM inventory.stock_positions WHERE offer_id = '${KG_OFFER}' LIMIT 1;`);
  const shipmentStatus = sql(
    `SELECT status FROM fulfillment.shipments WHERE shipment_id = '${shipmentId ?? "00000000-0000-0000-0000-000000000000"}';`,
  );
  note("smoke-A-after", {
    codes: actionCodes(afterCancelOps),
    reservedBefore,
    reservedAfter,
    shipmentStatus,
    restore: actionCodes(afterCancelOps).includes("restore_cancelled_order"),
    ok:
      !actionCodes(afterCancelOps).includes("cancel")
      && Number(reservedAfter) < Number(reservedBefore || reservedAfter)
      && (shipmentStatus === "5" || String(shipmentStatus).toLowerCase().includes("cancel")),
  });
  const packAfter = req("POST", `/v1/admin/orders/${paid.checkoutId}/operations`, {
    code: "pack_selected",
    sellerOrderId: paid.sellerOrderId,
    fulfillmentId: paid.fulfillmentId,
    selections: [{ orderLineId: paid.orderLineId, quantity: 0.75 }],
  });
  const dispatchAfter = shipmentId
    ? req("POST", `/v1/admin/orders/${paid.checkoutId}/operations`, {
        code: "dispatch_shipment",
        sellerOrderId: paid.sellerOrderId,
        fulfillmentId: paid.fulfillmentId,
        shipmentId,
      })
    : { status: 400, json: { code: "missing" } };
  const packMsg = errText(packAfter);
  note("smoke-C-fulfillment-stop", {
    packStatus: packAfter.status,
    packCode: packAfter.json?.code ?? packAfter.json?.errorCode,
    packMsg,
    dispatchStatus: dispatchAfter.status,
    ok: packAfter.status >= 400 && dispatchAfter.status >= 400,
  });
  const list = req("POST", "/v1/admin/orders/query", {
    page: 1,
    pageSize: 20,
    sort: [{ field: "created", direction: "desc" }],
    filters: [],
  });
  const rows = list.json?.items ?? list.json?.rows ?? list.json?.Items ?? list.json?.Rows ?? [];
  const listed = Array.isArray(rows) ? rows.find((r) => String(pick(r, "checkoutId", "CheckoutId")) === paid.checkoutId) : null;
  note("grid-refresh", {
    status: list.status,
    listed: Boolean(listed),
    statusLabel: listed ? pick(listed, "status", "Status") : null,
    ok: list.status < 400 && listed != null,
  });
}

writeFileSync("docs/evidence/TB-P09-T016/runtime-raw.json", JSON.stringify(out, null, 2));
console.log(JSON.stringify(out, null, 2));
if (!out.ok) process.exit(1);
