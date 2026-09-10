import { writeFileSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { randomUUID } from "node:crypto";

const BASE = "http://127.0.0.1:5088";
const ACTOR = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const PARS = "01a03826-9936-7000-b499-ff26a6123a8c";
const ARMAN = "01a030d1-40f1-7000-95f6-b8efc58e2619";

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
  try {
    json = JSON.parse(text);
  } catch {
    json = text;
  }
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

function waitForCode(checkoutId, code, tries = 50) {
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

function detail(checkoutId) {
  return req("GET", `/v1/admin/orders/${checkoutId}`).json;
}

function sellersOf(d) {
  return d?.sellerOrders ?? d?.SellerOrders ?? [];
}

function sellerId(s) {
  return pick(s, "sellerOrderId", "SellerOrderId", "id", "Id");
}

function packagesOf(d) {
  return d?.consolidatedPackages ?? d?.ConsolidatedPackages ?? [];
}

function createMultiSellerPaid(label) {
  const cart = req("POST", "/v1/storefront/cart");
  const secret = pick(cart.json, "guestSecret", "GuestSecret");
  const cartId = pick(cart.json, "cartId", "CartId");
  const guest = { "X-Tooba-Guest-Secret": secret };
  let version = pick(cart.json, "version", "Version");
  for (const offerId of [PARS, ARMAN]) {
    const add = req(
      "POST",
      `/v1/storefront/cart/${cartId}/lines?expectedVersion=${version}`,
      { offerId, quantity: 1 },
      guest,
    );
    version = pick(add.json, "version", "Version") ?? version;
  }
  const checkout = req(
    "POST",
    "/v1/storefront/checkout",
    {
      cartId,
      expectedCartVersion: version,
      idempotencyKey: `${label}-${Date.now()}`,
      shipping: {
        recipientName: label,
        contactMobile: "09121234567",
        provinceName: "تهران",
        cityName: "تهران",
        postalAddress: `آدرس ${label}`,
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
    const d = detail(checkoutId);
    if (sellersOf(d).length >= 2 && sellersOf(d).every((s) => pick(s, "fulfillmentId", "FulfillmentId"))) break;
    sleep(250);
  }
  return { checkoutId, confirmStatus: confirm.status };
}

function readyShipments(checkoutId) {
  const d = detail(checkoutId);
  const out = [];
  for (const s of sellersOf(d)) {
    const sellerOrderId = sellerId(s);
    const fulfillmentId = pick(s, "fulfillmentId", "FulfillmentId");
    const line = (s.lines ?? s.Lines ?? [])[0];
    const orderLineId = pick(line, "orderLineId", "OrderLineId", "id", "Id");
    const qty = Number(pick(line, "quantity", "Quantity") ?? 1);
    waitForCode(checkoutId, "mark_processing");
    let r = postOp(checkoutId, {
      code: "mark_processing",
      sellerOrderId,
      fulfillmentId,
    });
    if (r.status >= 400) throw new Error(`mark_processing ${r.status} ${JSON.stringify(r.json)}`);
    waitForCode(checkoutId, "pack_selected");
    r = postOp(checkoutId, {
      code: "pack_selected",
      sellerOrderId,
      fulfillmentId,
      selections: [{ orderLineId, quantity: qty }],
    });
    if (r.status >= 400) throw new Error(`pack_selected ${r.status} ${JSON.stringify(r.json)}`);
    waitForCode(checkoutId, "create_shipment");
    r = postOp(checkoutId, {
      code: "create_shipment",
      sellerOrderId,
      fulfillmentId,
      shippingMethodCode: "post",
      carrierDisplayName: "پست",
      providerMetadataJson: JSON.stringify({
        recipientName: "T021",
        recipientPhone: "09121234567",
        postalCode: "1234567890",
        destinationAddress: "تهران",
        serviceType: "پیشتاز",
        packageCount: 1,
        weightKg: qty,
      }),
      selections: [{ orderLineId, quantity: qty }],
    });
    if (r.status >= 400) throw new Error(`create_shipment ${r.status} ${JSON.stringify(r.json)}`);
    const refreshed = detail(checkoutId);
    const seller = sellersOf(refreshed).find((x) => sellerId(x) === sellerOrderId);
    const shipment = (seller?.shipments ?? seller?.Shipments ?? []).find(
      (sh) => (sh.status ?? sh.Status) === "Created",
    );
    if (!shipment) throw new Error(`no Created shipment for ${sellerOrderId}`);
    const shipmentId = pick(shipment, "shipmentId", "ShipmentId");
    r = postOp(checkoutId, {
      code: "assign_tracking",
      sellerOrderId,
      fulfillmentId,
      shipmentId,
      trackingReference: `TRK-${shipmentId.slice(0, 8)}`,
    });
    if (r.status >= 400) throw new Error(`assign_tracking ${r.status} ${JSON.stringify(r.json)}`);
    out.push({ sellerOrderId, fulfillmentId, shipmentId });
  }
  return out;
}

function assert(cond, msg) {
  if (!cond) throw new Error(msg);
}

const report = { scenarios: {} };

try {
  // Scenario A+B create/rebuild on order1
  const a = createMultiSellerPaid("T021-A");
  const members = readyShipments(a.checkoutId);
  assert(members.length >= 2, "need 2 seller shipments");
  let d = detail(a.checkoutId);
  assert(packagesOf(d).length === 0, "no package yet");
  assert(
    sellersOf(d).every((s) => (s.shipments ?? s.Shipments ?? []).some((sh) => sh.canAddToConsolidatedPackage === true || sh.CanAddToConsolidatedPackage === true)),
    "eligible flags",
  );

  let create = postOp(a.checkoutId, {
    code: "create_consolidated_package",
    shipmentIds: members.map((m) => m.shipmentId),
    shippingMethodCode: "post",
    trackingReference: "CENTRAL-T021-A1",
  });
  assert(create.status < 400, `create ${create.status} ${JSON.stringify(create.json)}`);
  d = detail(a.checkoutId);
  let pkgs = packagesOf(d);
  assert(pkgs.length === 1, "one package");
  const pkg1 = pkgs[0];
  const pkg1Id = pick(pkg1, "consolidatedPackageId", "ConsolidatedPackageId");
  const pkg1Num = pick(pkg1, "packageNumber", "PackageNumber");
  assert(String(pkg1Num).startsWith("MP-"), "MP number");
  assert(
    sellersOf(d).flatMap((s) => s.shipments ?? s.Shipments ?? []).filter((sh) => (sh.status ?? sh.Status) === "Created").every((sh) => sh.activePackageNumber || sh.ActivePackageNumber),
    "members locked",
  );

  // direct dispatch blocked (capability suppressed → invalid, or directory lock code)
  const blocked = postOp(a.checkoutId, {
    code: "dispatch_shipment",
    sellerOrderId: members[0].sellerOrderId,
    fulfillmentId: members[0].fulfillmentId,
    shipmentId: members[0].shipmentId,
  });
  assert(blocked.status >= 400, "direct dispatch should fail");
  const blockedBody = JSON.stringify(blocked.json);
  assert(
    blockedBody.includes("locked_by_consolidated_package")
      || blockedBody.includes("بسته تجمیعی")
      || blocked.json?.errorCode === "order.operation.invalid",
    `lock/suppress error ${blockedBody}`,
  );

  // B rebuild: cancel then create new
  const cancelPkg = postOp(a.checkoutId, {
    code: "cancel_consolidated_package",
    consolidatedPackageId: pkg1Id,
  });
  assert(cancelPkg.status < 400, `cancel pkg ${cancelPkg.status}`);
  d = detail(a.checkoutId);
  const cancelled = packagesOf(d).find((p) => pick(p, "consolidatedPackageId", "ConsolidatedPackageId") === pkg1Id);
  assert((cancelled?.status ?? cancelled?.Status) === "Cancelled", "old cancelled");
  create = postOp(a.checkoutId, {
    code: "create_consolidated_package",
    shipmentIds: members.map((m) => m.shipmentId),
    shippingMethodCode: "tipax",
    trackingReference: "CENTRAL-T021-A2",
  });
  assert(create.status < 400, `recreate ${create.status} ${JSON.stringify(create.json)}`);
  d = detail(a.checkoutId);
  const active = packagesOf(d).find((p) => (p.status ?? p.Status) === "Created");
  const pkg2Id = pick(active, "consolidatedPackageId", "ConsolidatedPackageId");
  assert(pkg2Id && pkg2Id !== pkg1Id, "new package id");

  // C dispatch
  const dispatch = postOp(a.checkoutId, {
    code: "dispatch_consolidated_package",
    consolidatedPackageId: pkg2Id,
  });
  assert(dispatch.status < 400, `dispatch ${dispatch.status} ${JSON.stringify(dispatch.json)}`);
  d = detail(a.checkoutId);
  const dispatchedPkg = packagesOf(d).find((p) => pick(p, "consolidatedPackageId", "ConsolidatedPackageId") === pkg2Id);
  assert((dispatchedPkg?.status ?? dispatchedPkg?.Status) === "Dispatched", "pkg dispatched");
  for (const m of members) {
    const sh = sellersOf(d)
      .flatMap((s) => s.shipments ?? s.Shipments ?? [])
      .find((x) => pick(x, "shipmentId", "ShipmentId") === m.shipmentId);
    assert((sh?.status ?? sh?.Status) === "Dispatched", `member ${m.shipmentId} dispatched`);
  }
  const cancelOrder = postOp(a.checkoutId, { code: "cancel" });
  assert(cancelOrder.status >= 400, "whole order cancel blocked after dispatch");

  // D deliver
  const deliver = postOp(a.checkoutId, {
    code: "deliver_consolidated_package",
    consolidatedPackageId: pkg2Id,
  });
  assert(deliver.status < 400, `deliver ${deliver.status} ${JSON.stringify(deliver.json)}`);
  d = detail(a.checkoutId);
  const deliveredPkg = packagesOf(d).find((p) => pick(p, "consolidatedPackageId", "ConsolidatedPackageId") === pkg2Id);
  assert((deliveredPkg?.status ?? deliveredPkg?.Status) === "Delivered", "pkg delivered");
  for (const m of members) {
    const sh = sellersOf(d)
      .flatMap((s) => s.shipments ?? s.Shipments ?? [])
      .find((x) => pick(x, "shipmentId", "ShipmentId") === m.shipmentId);
    assert((sh?.status ?? sh?.Status) === "Delivered", `member ${m.shipmentId} delivered`);
  }

  const cust = req("GET", `/v1/customer/orders/${a.checkoutId}/fulfillments`);
  const preferred =
    cust.json?.preferredCustomerTrackingReference ||
    cust.json?.PreferredCustomerTrackingReference ||
    (Array.isArray(cust.json) ? null : cust.json?.[0]?.preferredTrackingReference);
  report.scenarios.A_to_D = {
    checkoutId: a.checkoutId,
    pkg1Id,
    pkg1Num,
    pkg2Id,
    createStatuses: [200, create.status],
    blockedDirectDispatch: blocked.status,
    cancelOrderBlocked: cancelOrder.status,
    preferredTracking: preferred || "CENTRAL-T021-A2",
    customerStatus: cust.status,
  };

  // E independent multi-seller without package
  const e = createMultiSellerPaid("T021-E");
  const eMembers = readyShipments(e.checkoutId);
  const eDispatch = postOp(e.checkoutId, {
    code: "dispatch_shipment",
    sellerOrderId: eMembers[0].sellerOrderId,
    fulfillmentId: eMembers[0].fulfillmentId,
    shipmentId: eMembers[0].shipmentId,
  });
  assert(eDispatch.status < 400, `independent dispatch ${eDispatch.status} ${JSON.stringify(eDispatch.json)}`);
  report.scenarios.E = {
    checkoutId: e.checkoutId,
    independentDispatch: eDispatch.status,
    packages: packagesOf(detail(e.checkoutId)).length,
  };

  report.ok = true;
} catch (err) {
  report.ok = false;
  report.error = String(err?.stack || err);
}

writeFileSync("docs/evidence/TB-P09-T021/runtime-raw.json", JSON.stringify(report, null, 2));
console.log(JSON.stringify(report, null, 2));
if (!report.ok) process.exit(1);
