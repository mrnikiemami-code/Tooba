/** Complete dispatch for R4 B/C orders left Held after track race; also re-verify A Consumed. */
import { writeFileSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { randomUUID } from "node:crypto";

const BASE = "http://127.0.0.1:5088";
const ACTOR = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const headers = {
  Host: "alpha.localhost",
  "X-Tooba-Dev-Actor-User-Id": ACTOR,
  "Content-Type": "application/json",
};

function nowIso() {
  return new Date().toISOString();
}

function req(method, path, body) {
  const args = ["-sS", "-o", "-", "-w", "\n%{http_code}", "-X", method, `${BASE}${path}`];
  for (const [k, v] of Object.entries(headers)) args.push("-H", `${k}: ${v}`);
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

function resSnap(id) {
  const row = sql(
    `SELECT reservation_id::text, status, COALESCE(expires_at::text,''), quantity::text FROM inventory.reservations WHERE reservation_id='${id}';`,
  );
  const [reservationId, status, expiresAt, quantity] = row.split("|");
  return { at: nowIso(), reservationId, status, expiresAt: expiresAt || null, quantity };
}

function orderCtx(checkoutId) {
  const detail = req("GET", `/v1/admin/orders/${checkoutId}`);
  const seller = (detail.json?.sellerOrders ?? detail.json?.SellerOrders ?? [])[0];
  const line = (seller?.lines ?? seller?.Lines ?? [])[0];
  return {
    checkoutId,
    sellerOrderId: pick(seller, "sellerOrderId", "SellerOrderId"),
    fulfillmentId: pick(seller, "fulfillmentId", "FulfillmentId"),
    orderLineId: pick(line, "orderLineId", "OrderLineId"),
  };
}

function fulfillToDispatch(checkoutId, qty, trackPrefix) {
  const ctx = orderCtx(checkoutId);
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
  waitForCode(checkoutId, "create_shipment");
  const ship = postOp(checkoutId, {
    code: "create_shipment",
    sellerOrderId: ctx.sellerOrderId,
    fulfillmentId: ctx.fulfillmentId,
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
      weightKg: Number(qty),
    }),
    selections: [{ orderLineId: ctx.orderLineId, quantity: qty }],
  });
  let shipmentId = null;
  for (let i = 0; i < 40; i++) {
    const d = req("GET", `/v1/admin/orders/${checkoutId}`);
    const seller = (d.json?.sellerOrders ?? d.json?.SellerOrders ?? [])[0];
    const shipments = seller?.shipments ?? seller?.Shipments ?? [];
    const created = shipments.find((s) => (s.status ?? s.Status) === "Created") ?? shipments.at(-1);
    shipmentId = pick(created, "shipmentId", "ShipmentId");
    if (shipmentId) break;
    sleep(250);
  }
  waitForCode(checkoutId, "assign_tracking");
  const track = postOp(checkoutId, {
    code: "assign_tracking",
    sellerOrderId: ctx.sellerOrderId,
    fulfillmentId: ctx.fulfillmentId,
    shipmentId,
    trackingReference: `${trackPrefix}-${Date.now().toString(36)}`,
  });
  waitForCode(checkoutId, "dispatch_shipment");
  const dispatch = postOp(checkoutId, {
    code: "dispatch_shipment",
    sellerOrderId: ctx.sellerOrderId,
    fulfillmentId: ctx.fulfillmentId,
    shipmentId,
  });
  return { process, pack, ship, track, dispatch, shipmentId };
}

const out = {
  at: nowIso(),
  A: {
    reservation: resSnap("01a08a40-90e4-7000-bf6c-26915dd8f2eb"),
    note: "manual track+dispatch after initial race; Consumed expected",
  },
  B: null,
  C: null,
};

{
  const checkoutId = "01a08a42-1f46-7000-bad4-fd05caf5faf2";
  const before = resSnap("01a08a42-2447-7000-b966-5b07dcee6006");
  const oldBefore = resSnap("01a08a42-1ea9-7000-88bd-18b8b06222af");
  const flow = fulfillToDispatch(checkoutId, 1, "TRK-R4B");
  const after = resSnap("01a08a42-2447-7000-b966-5b07dcee6006");
  const oldAfter = resSnap("01a08a42-1ea9-7000-88bd-18b8b06222af");
  out.B = {
    checkoutId,
    before,
    oldBefore,
    flow: {
      process: flow.process.status,
      pack: flow.pack.status,
      ship: flow.ship.status,
      track: flow.track.status,
      dispatch: flow.dispatch.status,
      shipmentId: flow.shipmentId,
    },
    after,
    oldAfter,
    ok: flow.dispatch.status < 400 && after.status === "Consumed" && oldAfter.status === "Released",
  };
}

{
  const checkoutId = "01a08a43-add3-7000-ba58-55bb498eb938";
  const before = resSnap("01a08a45-3c6b-7000-be90-8cf155582e2e");
  const released = resSnap("01a08a43-ad3a-7000-b8b2-e9eb5c3c9905");
  const flow = fulfillToDispatch(checkoutId, 1.25, "TRK-R4C");
  const after = resSnap("01a08a45-3c6b-7000-be90-8cf155582e2e");
  out.C = {
    checkoutId,
    before,
    released,
    flow: {
      process: flow.process.status,
      pack: flow.pack.status,
      ship: flow.ship.status,
      track: flow.track.status,
      dispatch: flow.dispatch.status,
      shipmentId: flow.shipmentId,
    },
    after,
    ok: flow.dispatch.status < 400 && after.status === "Consumed" && after.quantity === "1.250000",
  };
}

out.ok = out.A.reservation.status === "Consumed" && out.B.ok && out.C.ok;
writeFileSync("docs/evidence/TB-P09-T022/_r4_complete_dispatch_raw.json", JSON.stringify(out, null, 2));
console.log(JSON.stringify(out, null, 2));
process.exit(out.ok ? 0 : 2);
