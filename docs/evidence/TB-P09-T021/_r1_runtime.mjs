import { writeFileSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { randomUUID } from "node:crypto";

const BASE = "http://127.0.0.1:5088";
const ACTOR = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const GUEST_ACTOR = "aaaaaaaa-aaaa-4aaa-8aaa-000000000009";
const OWNED_ACTOR = "bbbbbbbb-bbbb-4bbb-8bbb-0000000000bb";
const PARS = "01a03826-9936-7000-b499-ff26a6123a8c";
const ARMAN = "01a030d1-40f1-7000-95f6-b8efc58e2619";

const headers = {
  Host: "alpha.localhost",
  "X-Tooba-Dev-Actor-User-Id": ACTOR,
  "Content-Type": "application/json",
};

function req(method, path, body, extra = {}, opts = {}) {
  const args = ["-sS", "-o", "-", "-w", "\n%{http_code}", "-X", method, `${BASE}${path}`];
  const all = { ...headers, ...extra };
  if (!opts.keepActor && all["X-Tooba-Guest-Secret"]) delete all["X-Tooba-Dev-Actor-User-Id"];
  for (const [k, v] of Object.entries(all)) {
    if (v == null) continue;
    args.push("-H", `${k}: ${v}`);
  }
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
  throw new Error(`waitForCode timeout ${code} on ${checkoutId}; have=${actionCodes(ops).join(",")}`);
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

function createMultiSellerPaid(label, placeActor = null) {
  const cart = req("POST", "/v1/storefront/cart");
  const secret = pick(cart.json, "guestSecret", "GuestSecret");
  const cartId = pick(cart.json, "cartId", "CartId");
  const guest = { "X-Tooba-Guest-Secret": secret };
  const keepActor = Boolean(placeActor);
  if (placeActor) guest["X-Tooba-Dev-Actor-User-Id"] = placeActor;
  let version = pick(cart.json, "version", "Version");
  for (const offerId of [PARS, ARMAN]) {
    const add = req(
      "POST",
      `/v1/storefront/cart/${cartId}/lines?expectedVersion=${version}`,
      { offerId, quantity: 1 },
      guest,
      { keepActor },
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
    { keepActor },
  );
  const checkoutId = pick(checkout.json, "checkoutId", "CheckoutId");
  req(
    "POST",
    `/v1/storefront/checkout/${checkoutId}/payments`,
    { cartId, idempotencyKey: randomUUID().replaceAll("-", ""), providerCode: "manual" },
    guest,
    { keepActor },
  );
  waitForCode(checkoutId, "confirm_deposit");
  const confirm = postOp(checkoutId, { code: "confirm_deposit" });
  for (let i = 0; i < 40; i++) {
    const d = detail(checkoutId);
    if (sellersOf(d).length >= 2 && sellersOf(d).every((s) => pick(s, "fulfillmentId", "FulfillmentId"))) break;
    sleep(250);
  }
  waitForCode(checkoutId, "mark_processing", 80);
  return { checkoutId, cartId, secret, confirmStatus: confirm.status };
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
        recipientName: "T021-R1",
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

function custGet(checkoutId, extra) {
  return req("GET", `/v1/customer/orders/${checkoutId}/fulfillments`, undefined, extra);
}

function assert(cond, msg) {
  if (!cond) throw new Error(msg);
}

const report = { task: "TB-P09-T021-R1", scenarios: {} };

const a = createMultiSellerPaid("T021-R1-A");
const members = readyShipments(a.checkoutId);
assert(members.length >= 2, "need 2 seller shipments");

let create = postOp(a.checkoutId, {
  code: "create_consolidated_package",
  shipmentIds: members.map((m) => m.shipmentId),
  shippingMethodCode: "post",
  trackingReference: "CENTRAL-R1-A1",
});
assert(create.status < 400, `create ${create.status} ${JSON.stringify(create.json)}`);
let d = detail(a.checkoutId);
let pkgs = packagesOf(d);
const pkg1Id = pick(pkgs[0], "consolidatedPackageId", "ConsolidatedPackageId");
const pkg1Num = pick(pkgs[0], "packageNumber", "PackageNumber");

const cancelPkg = postOp(a.checkoutId, { code: "cancel_consolidated_package", consolidatedPackageId: pkg1Id });
assert(cancelPkg.status < 400, `cancel pkg ${cancelPkg.status}`);
create = postOp(a.checkoutId, {
  code: "create_consolidated_package",
  shipmentIds: members.map((m) => m.shipmentId),
  shippingMethodCode: "tipax",
  trackingReference: "CENTRAL-R1-A2",
});
assert(create.status < 400, `recreate ${create.status} ${JSON.stringify(create.json)}`);
d = detail(a.checkoutId);
const active = packagesOf(d).find((p) => (p.status ?? p.Status) === "Created");
const cancelled = packagesOf(d).find((p) => pick(p, "consolidatedPackageId", "ConsolidatedPackageId") === pkg1Id);
const pkg2Id = pick(active, "consolidatedPackageId", "ConsolidatedPackageId");
const pkg2Num = pick(active, "packageNumber", "PackageNumber");
assert(pkg2Id && pkg2Id !== pkg1Id, "new package id");
assert((cancelled?.status ?? cancelled?.Status) === "Cancelled", "old cancelled");

const guestCust = custGet(a.checkoutId, { "X-Tooba-Guest-Secret": a.secret });
assert(guestCust.status === 200, `guest cust ${guestCust.status} ${guestCust.text}`);
assert(
  (guestCust.json.preferredCustomerTrackingReference || guestCust.json.PreferredCustomerTrackingReference) ===
    "CENTRAL-R1-A2",
  "guest preferred tracking",
);
assert(
  (guestCust.json.preferredCustomerTrackingPackageNumber || guestCust.json.PreferredCustomerTrackingPackageNumber) ===
    pkg2Num,
  "guest preferred package is rebuilt active",
);

const guestActorCust = custGet(a.checkoutId, { "X-Tooba-Dev-Actor-User-Id": GUEST_ACTOR });
assert(guestActorCust.status === 404, `guest actor alone must not unlock guest checkout ${guestActorCust.status}`);

const badActor = custGet(a.checkoutId, { "X-Tooba-Dev-Actor-User-Id": OWNED_ACTOR });
assert(badActor.status === 404, `bad actor ${badActor.status}`);

const badSecret = custGet(a.checkoutId, { "X-Tooba-Guest-Secret": "not-the-real-secret" });
assert(badSecret.status === 404, `bad secret ${badSecret.status}`);

waitForCode(a.checkoutId, "dispatch_consolidated_package");
const dispatch = postOp(a.checkoutId, { code: "dispatch_consolidated_package", consolidatedPackageId: pkg2Id });
assert(dispatch.status < 400, `dispatch ${dispatch.status}`);
const afterDispatch = custGet(a.checkoutId, { "X-Tooba-Guest-Secret": a.secret });
assert(
  (afterDispatch.json.preferredCustomerPackageStatus || afterDispatch.json.PreferredCustomerPackageStatus) ===
    "Dispatched",
  "dispatched status",
);

waitForCode(a.checkoutId, "deliver_consolidated_package");
const deliver = postOp(a.checkoutId, { code: "deliver_consolidated_package", consolidatedPackageId: pkg2Id });
assert(deliver.status < 400, `deliver ${deliver.status}`);
const afterDeliver = custGet(a.checkoutId, { "X-Tooba-Guest-Secret": a.secret });
assert(afterDeliver.status === 200, `after deliver ${afterDeliver.status}`);
assert(
  (afterDeliver.json.preferredCustomerTrackingReference || afterDeliver.json.PreferredCustomerTrackingReference) ===
    "CENTRAL-R1-A2",
  "delivered preferred tracking",
);
assert(
  (afterDeliver.json.preferredCustomerPackageStatus || afterDeliver.json.PreferredCustomerPackageStatus) ===
    "Delivered",
  "delivered status primary",
);
const fulfills = afterDeliver.json.fulfillments || afterDeliver.json.Fulfillments || [];
for (const f of fulfills) {
  const st = f.status ?? f.Status;
  assert(st === "Delivered" || st === 5, `member fulfillment Delivered got ${st}`);
}

report.scenarios.guest_lifecycle = {
  checkoutId: a.checkoutId,
  cartId: a.cartId,
  pkg1Id,
  pkg1Num,
  pkg2Id,
  pkg2Num,
  guestStatus: guestCust.status,
  guestActorStatus: guestActorCust.status,
  badActorStatus: badActor.status,
  badSecretStatus: badSecret.status,
  preferredAfterDeliver:
    afterDeliver.json.preferredCustomerTrackingReference || afterDeliver.json.PreferredCustomerTrackingReference,
  packageStatus:
    afterDeliver.json.preferredCustomerPackageStatus || afterDeliver.json.PreferredCustomerPackageStatus,
};

const b = createMultiSellerPaid("T021-R1-B");
const bMembers = readyShipments(b.checkoutId);
const bCreate = postOp(b.checkoutId, {
  code: "create_consolidated_package",
  shipmentIds: bMembers.map((m) => m.shipmentId),
  shippingMethodCode: "post",
  trackingReference: "CENTRAL-R1-B",
});
assert(bCreate.status < 400, `b create ${bCreate.status} ${JSON.stringify(bCreate.json)}`);
execFileSync(
  "docker",
  ["exec", "-i", "postgres-db", "psql", "-U", "admin", "-d", "tooba_alpha", "-v", "ON_ERROR_STOP=1"],
  {
    input: `UPDATE "order".checkouts SET placed_by_user_id = '${OWNED_ACTOR}' WHERE checkout_id = '${b.checkoutId}';\n`,
    encoding: "utf8",
  },
);
const ownedCust = custGet(b.checkoutId, { "X-Tooba-Dev-Actor-User-Id": OWNED_ACTOR });
assert(ownedCust.status === 200, `owned cust ${ownedCust.status} ${ownedCust.text}`);
assert(
  (ownedCust.json.preferredCustomerTrackingReference || ownedCust.json.PreferredCustomerTrackingReference) ===
    "CENTRAL-R1-B",
  "owned preferred",
);
report.scenarios.owned_actor = {
  checkoutId: b.checkoutId,
  status: ownedCust.status,
  preferred: ownedCust.json.preferredCustomerTrackingReference || ownedCust.json.PreferredCustomerTrackingReference,
};

const c = createMultiSellerPaid("T021-R1-C");
const cMembers = readyShipments(c.checkoutId);
const cDispatch = postOp(c.checkoutId, {
  code: "dispatch_shipment",
  sellerOrderId: cMembers[0].sellerOrderId,
  fulfillmentId: cMembers[0].fulfillmentId,
  shipmentId: cMembers[0].shipmentId,
});
assert(cDispatch.status < 400, `c dispatch ${cDispatch.status}`);
const cCust = custGet(c.checkoutId, { "X-Tooba-Guest-Secret": c.secret });
assert(cCust.status === 200, `c cust ${cCust.status}`);
assert(
  !(cCust.json.preferredCustomerTrackingReference || cCust.json.PreferredCustomerTrackingReference),
  "no package => no preferred central",
);
report.scenarios.no_package = {
  checkoutId: c.checkoutId,
  status: cCust.status,
  preferred: cCust.json.preferredCustomerTrackingReference || cCust.json.PreferredCustomerTrackingReference || null,
};

writeFileSync(new URL("./r1-runtime-raw.json", import.meta.url), JSON.stringify(report, null, 2));
console.log(JSON.stringify(report, null, 2));
console.log("R1 runtime PASS");
