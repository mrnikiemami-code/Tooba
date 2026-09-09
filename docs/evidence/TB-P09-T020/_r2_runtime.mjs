import { writeFileSync, readFileSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { randomUUID } from "node:crypto";

const BASE = "http://127.0.0.1:5088";
const KG_OFFER = "01a03826-9936-7000-b499-ff26a6123a8c";
const ACTOR = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";

const headers = {
  Host: "alpha.localhost",
  "X-Tooba-Dev-Actor-User-Id": ACTOR,
  "Content-Type": "application/json",
};

function req(method, path, body, extra = {}) {
  const args = ["-sS", "-o", "-", "-w", "\n%{http_code}", "-X", method, `${BASE}${path}`];
  const all = { ...headers, ...extra };
  if (all["X-Tooba-Guest-Secret"]) delete all["X-Tooba-Dev-Actor-User-Id"];
  for (const [k, v] of Object.entries(all)) args.push("-H", `${k}: ${v}`);
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

function pick(obj, ...keys) {
  if (!obj || typeof obj !== "object") return undefined;
  for (const key of keys) if (obj[key] != null) return obj[key];
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

function postOp(checkoutId, body) {
  return req("POST", `/v1/admin/orders/${checkoutId}/operations`, body);
}

function sql(q) {
  return execFileSync(
    "docker",
    ["exec", "-i", "postgres-db", "psql", "-U", "admin", "-d", "tooba_alpha", "-t", "-A", "-F", "|"],
    { input: q, encoding: "utf8" },
  ).trim();
}

function createPaid(qty) {
  const cart = req("POST", "/v1/storefront/cart");
  const secret = pick(cart.json, "guestSecret", "GuestSecret");
  const cartId = pick(cart.json, "cartId", "CartId");
  const guest = { "X-Tooba-Guest-Secret": secret };
  const add = req(
    "POST",
    `/v1/storefront/cart/${cartId}/lines?expectedVersion=${pick(cart.json, "version", "Version")}`,
    { offerId: KG_OFFER, quantity: qty },
    guest,
  );
  const checkout = req(
    "POST",
    "/v1/storefront/checkout",
    {
      cartId,
      expectedCartVersion: pick(add.json, "version", "Version"),
      idempotencyKey: `t020r2-${Date.now()}-${qty}`,
      shipping: {
        recipientName: "T020R2",
        contactMobile: "09121234567",
        provinceName: "تهران",
        cityName: "تهران",
        postalAddress: "آدرس T020-R2",
        postalCode: "1234567890",
      },
    },
    guest,
  );
  const checkoutId = pick(checkout.json, "checkoutId", "CheckoutId");
  req(
    "POST",
    `/v1/storefront/checkout/${checkoutId}/payments`,
    { cartId, idempotencyKey: randomUUID().replaceAll("-", ""), providerCode: "manual" },
    guest,
  );
  waitForCode(checkoutId, "confirm_deposit");
  postOp(checkoutId, { code: "confirm_deposit", idempotencyKey: randomUUID().replaceAll("-", "") });
  for (let i = 0; i < 40; i++) {
    const d = req("GET", `/v1/admin/orders/${checkoutId}`);
    const sellers = d.json?.sellerOrders ?? d.json?.SellerOrders ?? [];
    if (sellers.some((s) => pick(s, "fulfillmentId", "FulfillmentId"))) break;
    sleep(250);
  }
  return checkoutId;
}

function reservationPair(checkoutId) {
  const rows = sql(`
SELECT ol.reservation_id::text || '|' || COALESCE(r_ol.status,'?') || '|' ||
       fi.reservation_id::text || '|' || COALESCE(r_fi.status,'?')
FROM "order".seller_orders so
JOIN "order".order_lines ol ON ol.seller_order_id = so.seller_order_id
JOIN fulfillment.items fi ON fi.order_line_id = ol.line_id
LEFT JOIN inventory.reservations r_ol ON r_ol.reservation_id = ol.reservation_id
LEFT JOIN inventory.reservations r_fi ON r_fi.reservation_id = fi.reservation_id
WHERE so.checkout_id = '${checkoutId}'
LIMIT 1;`);
  const [olId, olStatus, fiId, fiStatus] = rows.split("|");
  return { olId, olStatus, fiId, fiStatus, aligned: olId === fiId };
}

function runCase(qty) {
  const checkoutId = createPaid(qty);
  const beforeCancel = reservationPair(checkoutId);
  waitForCode(checkoutId, "cancel");
  const cancel = postOp(checkoutId, { code: "cancel", idempotencyKey: randomUUID().replaceAll("-", "") });
  sleep(500);
  const afterCancel = reservationPair(checkoutId);
  waitForCode(checkoutId, "restore_cancelled_order");
  const restore = postOp(checkoutId, {
    code: "restore_cancelled_order",
    idempotencyKey: randomUUID().replaceAll("-", ""),
  });
  sleep(500);
  const afterRestore = reservationPair(checkoutId);
  const detail = req("GET", `/v1/admin/orders/${checkoutId}`);
  const seller = (detail.json?.sellerOrders ?? detail.json?.SellerOrders ?? [])[0];
  const line = (seller?.lines ?? seller?.Lines ?? [])[0];
  const ctx = {
    checkoutId,
    sellerOrderId: pick(seller, "sellerOrderId", "SellerOrderId"),
    fulfillmentId: pick(seller, "fulfillmentId", "FulfillmentId"),
    orderLineId: pick(line, "orderLineId", "OrderLineId"),
  };
  waitForCode(checkoutId, "mark_processing");
  const process = postOp(checkoutId, {
    code: "mark_processing",
    sellerOrderId: ctx.sellerOrderId,
    fulfillmentId: ctx.fulfillmentId,
  });
  waitForCode(checkoutId, "pack_selected");
  const pack = postOp(checkoutId, {
    code: "pack_selected",
    sellerOrderId: ctx.sellerOrderId,
    fulfillmentId: ctx.fulfillmentId,
    selections: [{ orderLineId: ctx.orderLineId, quantity: qty }],
  });
  const ship = postOp(checkoutId, {
    code: "create_shipment",
    sellerOrderId: ctx.sellerOrderId,
    fulfillmentId: ctx.fulfillmentId,
    carrierDisplayName: "پست",
    shippingMethodCode: "post",
    shippingMethodLabel: "پست",
    providerMetadataJson: JSON.stringify({
      recipientName: "T020R2",
      recipientPhone: "09121234567",
      postalCode: "1234567890",
      destinationAddress: "تهران",
      serviceType: "پیشتاز",
      packageCount: 1,
      weightKg: qty,
    }),
    selections: [{ orderLineId: ctx.orderLineId, quantity: qty }],
  });
  if (ship.status >= 400) {
    return {
      qty,
      checkoutId,
      cancel: cancel.status,
      restore: restore.status,
      process: process.status,
      pack: pack.status,
      ship: ship.status,
      shipErr: ship.text,
      track: 0,
      dispatch: 0,
      beforeCancel,
      afterCancel,
      afterRestore,
      afterDispatch: reservationPair(checkoutId),
      errLeak: false,
      ok: false,
    };
  }
  const afterShip = req("GET", `/v1/admin/orders/${checkoutId}`);
  const shipments =
    (afterShip.json?.sellerOrders ?? afterShip.json?.SellerOrders ?? [])[0]?.shipments ??
    (afterShip.json?.sellerOrders ?? afterShip.json?.SellerOrders ?? [])[0]?.Shipments ??
    [];
  const created = shipments.find((s) => (s.status ?? s.Status) === "Created") ?? shipments.at(-1);
  const shipmentId = pick(created, "shipmentId", "ShipmentId");
  const track = postOp(checkoutId, {
    code: "assign_tracking",
    sellerOrderId: ctx.sellerOrderId,
    fulfillmentId: ctx.fulfillmentId,
    shipmentId,
    trackingReference: `TRK-R2-${qty}`,
  });
  const dispatch = postOp(checkoutId, {
    code: "dispatch_shipment",
    sellerOrderId: ctx.sellerOrderId,
    fulfillmentId: ctx.fulfillmentId,
    shipmentId,
  });
  const afterDispatch = reservationPair(checkoutId);
  const errLeak =
    String(dispatch.text ?? "").includes("Held") ||
    String(dispatch.json?.detail ?? "").includes("Held") ||
    String(dispatch.json?.title ?? "").includes("Held");
  return {
    qty,
    checkoutId,
    cancel: cancel.status,
    restore: restore.status,
    process: process.status,
    pack: pack.status,
    ship: ship.status,
    track: track.status,
    dispatch: dispatch.status,
    beforeCancel,
    afterCancel,
    afterRestore,
    afterDispatch,
    errLeak,
    ok:
      cancel.status < 400 &&
      restore.status < 400 &&
      process.status < 400 &&
      pack.status < 400 &&
      ship.status < 400 &&
      track.status < 400 &&
      dispatch.status < 400 &&
      afterCancel.fiStatus === "Released" &&
      afterRestore.olStatus === "Held" &&
      afterRestore.fiStatus === "Held" &&
      afterRestore.aligned === true &&
      afterDispatch.fiStatus === "Consumed" &&
      afterDispatch.olStatus === "Consumed" &&
      !errLeak,
  };
}

const out = {
  ok: true,
  steps: [
    { name: "health", ...(req("GET", "/health")), ok: true },
  ],
};
out.steps[0].ok = out.steps[0].status === 200;
if (!out.steps[0].ok) out.ok = false;

for (const qty of [1, 1.25]) {
  try {
    const row = runCase(qty);
    out.steps.push({ name: `cancel-restore-dispatch-${qty}`, ...row });
    if (!row.ok) out.ok = false;
  } catch (e) {
    out.ok = false;
    out.steps.push({ name: `cancel-restore-dispatch-${qty}`, ok: false, error: String(e) });
  }
}

const a = readFileSync("src/frontend/app/admin/admin-error-map.ts", "utf8");
const b = readFileSync("src/backend/Host/Tooba.Host/Admin/AdminOrderOperationsComposer.cs", "utf8");
const d = readFileSync("src/backend/Modules/Inventory/Tooba.Inventory.Domain/InventoryDomain.cs", "utf8");
const mapped = {
  name: "error-ux-map",
  hasFa: a.includes("inventory.reservation.not_active") && a.includes("رزرو موجودی این سفارش دیگر فعال نیست"),
  hasHost: b.includes("inventory.reservation.not_active"),
  noHeldDomain: !d.includes("فقط رزرو Held"),
};
mapped.ok = mapped.hasFa && mapped.hasHost && mapped.noHeldDomain;
out.steps.push(mapped);
if (!mapped.ok) out.ok = false;

writeFileSync("docs/evidence/TB-P09-T020/r2-runtime-raw.json", JSON.stringify(out, null, 2));
console.log(JSON.stringify({ ok: out.ok, names: out.steps.map((s) => `${s.name}:${s.ok}`) }, null, 2));
