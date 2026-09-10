/** Clean single-pass Scenario A with fixed track/dispatch waits (TB-P09-T022-R4). */
import { writeFileSync } from "node:fs";
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

function nowIso() {
  return new Date().toISOString();
}
function sleep(ms) {
  Atomics.wait(new Int32Array(new SharedArrayBuffer(4)), 0, 0, ms);
}
function pick(obj, ...keys) {
  if (!obj || typeof obj !== "object") return undefined;
  for (const key of keys) if (obj[key] != null) return obj[key];
  return undefined;
}
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
  try {
    json = JSON.parse(text);
  } catch {
    json = text;
  }
  return { status, json, text, at: nowIso() };
}
function actionCodes(ops) {
  return (ops.json?.actions ?? ops.json?.Actions ?? []).map((a) => a.code ?? a.Code);
}
function waitForCode(checkoutId, code, tries = 60) {
  let ops;
  for (let i = 0; i < tries; i++) {
    ops = req("GET", `/v1/admin/orders/${checkoutId}/operations`);
    if (actionCodes(ops).includes(code)) return ops;
    sleep(250);
  }
  return ops;
}
function postOp(checkoutId, body) {
  return req("POST", `/v1/admin/orders/${checkoutId}/operations`, {
    idempotencyKey: randomUUID().replaceAll("-", ""),
    ...body,
  });
}
function sql(q) {
  return execFileSync(
    "docker",
    ["exec", "-i", "postgres-db", "psql", "-U", "admin", "-d", "tooba_alpha", "-t", "-A", "-F", "|"],
    { input: q, encoding: "utf8" },
  ).trim();
}
function snap(checkoutId) {
  const row = sql(`
SELECT ol.reservation_id::text, r.status, COALESCE(r.expires_at::text,''), r.quantity::text, sp.reserved::text
FROM "order".seller_orders so
JOIN "order".order_lines ol ON ol.seller_order_id = so.seller_order_id
JOIN inventory.reservations r ON r.reservation_id = ol.reservation_id
JOIN inventory.stock_positions sp ON sp.stock_item_id = r.stock_item_id
WHERE so.checkout_id='${checkoutId}' LIMIT 1;`);
  const [reservationId, status, expiresAt, quantity, reserved] = row.split("|");
  return { at: nowIso(), reservationId, status, expiresAt: expiresAt || null, quantity, reserved };
}

sql(`UPDATE inventory.stock_positions SET on_hand=GREATEST(on_hand,200), reserved=LEAST(reserved,50), updated_at=now() WHERE offer_id='${KG_OFFER}';`);

const cart = req("POST", "/v1/storefront/cart");
const secret = pick(cart.json, "guestSecret", "GuestSecret");
const cartId = pick(cart.json, "cartId", "CartId");
const guest = { "X-Tooba-Guest-Secret": secret };
const add = req(
  "POST",
  `/v1/storefront/cart/${cartId}/lines?expectedVersion=${pick(cart.json, "version", "Version")}`,
  { offerId: KG_OFFER, quantity: 1 },
  guest,
);
const beforePayRow = sql(`
SELECT r.reservation_id::text, r.status, COALESCE(r.expires_at::text,''), r.quantity::text
FROM cart.cart_lines cl JOIN inventory.reservations r ON r.reservation_id=cl.reservation_id
WHERE cl.cart_id='${cartId}' LIMIT 1;`);
const [rid, rstatus, rexpires, rqty] = beforePayRow.split("|");
const beforePay = { at: nowIso(), reservationId: rid, status: rstatus, expiresAt: rexpires || null, quantity: rqty };
const ttlMs = new Date(rexpires.replace(" ", "T")).getTime() - Date.now();
const payDelayMs = Math.max(0, ttlMs - 12000);
if (payDelayMs > 0) sleep(payDelayMs);
const checkout = req(
  "POST",
  "/v1/storefront/checkout",
  {
    cartId,
    expectedCartVersion: pick(add.json, "version", "Version"),
    idempotencyKey: `t022r4-cleanA-${Date.now()}`,
    shipping: {
      recipientName: "T022R4-CleanA",
      contactMobile: "09121234567",
      provinceName: "تهران",
      cityName: "تهران",
      postalAddress: "آدرس clean A",
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
const confirm = postOp(checkoutId, { code: "confirm_deposit" });
for (let i = 0; i < 40; i++) {
  const d = req("GET", `/v1/admin/orders/${checkoutId}`);
  const sellers = d.json?.sellerOrders ?? d.json?.SellerOrders ?? [];
  if (sellers.some((s) => pick(s, "fulfillmentId", "FulfillmentId"))) break;
  sleep(250);
}
const afterPay = snap(checkoutId);
const target = new Date(rexpires.replace(" ", "T")).getTime() + 3000;
sleep(Math.max(0, target - Date.now()));
sleep(8000);
const afterExpiry = snap(checkoutId);
const detail = req("GET", `/v1/admin/orders/${checkoutId}`);
const seller = (detail.json?.sellerOrders ?? detail.json?.SellerOrders ?? [])[0];
const line = (seller?.lines ?? seller?.Lines ?? [])[0];
const ctx = {
  sellerOrderId: pick(seller, "sellerOrderId", "SellerOrderId"),
  fulfillmentId: pick(seller, "fulfillmentId", "FulfillmentId"),
  orderLineId: pick(line, "orderLineId", "OrderLineId"),
};
waitForCode(checkoutId, "mark_processing");
const process = postOp(checkoutId, { code: "mark_processing", ...ctx });
waitForCode(checkoutId, "pack_selected");
const pack = postOp(checkoutId, {
  code: "pack_selected",
  ...ctx,
  selections: [{ orderLineId: ctx.orderLineId, quantity: 1 }],
});
waitForCode(checkoutId, "create_shipment");
const ship = postOp(checkoutId, {
  code: "create_shipment",
  ...ctx,
  carrierDisplayName: "پست",
  shippingMethodCode: "post",
  shippingMethodLabel: "پست",
  providerMetadataJson: JSON.stringify({
    recipientName: "T022R4",
    recipientPhone: "09121234567",
    postalCode: "1234567890",
    destinationAddress: "تهران",
    serviceType: "پیشتاز",
    packageCount: 1,
    weightKg: 1,
  }),
  selections: [{ orderLineId: ctx.orderLineId, quantity: 1 }],
});
let shipmentId = null;
for (let i = 0; i < 40; i++) {
  const d = req("GET", `/v1/admin/orders/${checkoutId}`);
  const s = (d.json?.sellerOrders ?? d.json?.SellerOrders ?? [])[0];
  const shipments = s?.shipments ?? s?.Shipments ?? [];
  const created = shipments.find((x) => (x.status ?? x.Status) === "Created") ?? shipments.at(-1);
  shipmentId = pick(created, "shipmentId", "ShipmentId");
  if (shipmentId) break;
  sleep(250);
}
waitForCode(checkoutId, "assign_tracking");
const track = postOp(checkoutId, {
  code: "assign_tracking",
  ...ctx,
  shipmentId,
  trackingReference: `TRK-R4-CLEAN-${Date.now().toString(36)}`,
});
waitForCode(checkoutId, "dispatch_shipment");
const dispatch = postOp(checkoutId, { code: "dispatch_shipment", ...ctx, shipmentId });
const afterDispatch = snap(checkoutId);

const out = {
  ok:
    confirm.status < 400 &&
    afterPay.status === "Held" &&
    !afterPay.expiresAt &&
    afterExpiry.status === "Held" &&
    !afterExpiry.expiresAt &&
    afterPay.reservationId === beforePay.reservationId &&
    process.status < 400 &&
    pack.status < 400 &&
    ship.status < 400 &&
    track.status < 400 &&
    dispatch.status < 400 &&
    afterDispatch.status === "Consumed",
  checkoutId,
  reservationId: beforePay.reservationId,
  payDelayMs,
  beforePay,
  confirmAt: confirm.at,
  afterPay,
  afterExpiry,
  flow: {
    process: process.status,
    pack: pack.status,
    ship: ship.status,
    track: track.status,
    dispatch: dispatch.status,
    shipmentId,
  },
  afterDispatch,
};
writeFileSync("docs/evidence/TB-P09-T022/_r4_clean_A_raw.json", JSON.stringify(out, null, 2));
console.log(JSON.stringify(out, null, 2));
process.exit(out.ok ? 0 : 2);
