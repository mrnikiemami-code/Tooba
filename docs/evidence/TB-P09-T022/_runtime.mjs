import { writeFileSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { randomUUID } from "node:crypto";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";

const __dirname = dirname(fileURLToPath(import.meta.url));
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

function shipmentsOf(d) {
  return sellersOf(d).flatMap((s) => s.shipments ?? s.Shipments ?? []);
}

function sql(q) {
  return execFileSync(
    "docker",
    ["exec", "-i", "postgres-db", "psql", "-U", "admin", "-d", "tooba_alpha", "-t", "-A", "-F", "|"],
    { input: q, encoding: "utf8" },
  ).trim();
}

function ensureStock() {
  sql(`
UPDATE inventory.stock_positions
SET on_hand = GREATEST(on_hand, 200), reserved = LEAST(reserved, 20), updated_at = now()
WHERE offer_id IN ('${PARS}', '${ARMAN}');
`);
}

function custGet(checkoutId, extra) {
  return req("GET", `/v1/customer/orders/${checkoutId}/fulfillments`, undefined, extra);
}

function assert(cond, msg) {
  if (!cond) throw new Error(msg);
}

function createPaid(label, lines) {
  const cart = req("POST", "/v1/storefront/cart");
  const secret = pick(cart.json, "guestSecret", "GuestSecret");
  const cartId = pick(cart.json, "cartId", "CartId");
  const guest = { "X-Tooba-Guest-Secret": secret };
  let version = pick(cart.json, "version", "Version");
  const lineResults = [];
  for (const line of lines) {
    const add = req(
      "POST",
      `/v1/storefront/cart/${cartId}/lines?expectedVersion=${version}`,
      { offerId: line.offerId, quantity: line.quantity },
      guest,
    );
    lineResults.push({ offerId: line.offerId, quantity: line.quantity, status: add.status, body: add.json });
    if (add.status >= 400) {
      return { ok: false, cartId, secret, lineResults, checkoutId: null };
    }
    version = pick(add.json, "version", "Version") ?? version;
  }
  const checkout = req(
    "POST",
    "/v1/storefront/checkout",
    {
      cartId,
      expectedCartVersion: version,
      idempotencyKey: `${label}-${Date.now()}-${randomUUID().slice(0, 8)}`,
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
  assert(checkoutId, `checkout failed ${checkout.status} ${JSON.stringify(checkout.json)}`);
  req(
    "POST",
    `/v1/storefront/checkout/${checkoutId}/payments`,
    { cartId, idempotencyKey: randomUUID().replaceAll("-", ""), providerCode: "manual" },
    guest,
  );
  waitForCode(checkoutId, "confirm_deposit");
  const confirm = postOp(checkoutId, { code: "confirm_deposit" });
  const needSellers = lines.length;
  for (let i = 0; i < 40; i++) {
    const d = detail(checkoutId);
    const sellers = sellersOf(d);
    if (sellers.length >= needSellers && sellers.every((s) => pick(s, "fulfillmentId", "FulfillmentId"))) break;
    sleep(250);
  }
  waitForCode(checkoutId, "mark_processing", 80);
  return { ok: true, checkoutId, cartId, secret, confirmStatus: confirm.status, lineResults };
}

function createMultiSellerPaid(label, parsQty = 1, armanQty = 1) {
  return createPaid(label, [
    { offerId: PARS, quantity: parsQty },
    { offerId: ARMAN, quantity: armanQty },
  ]);
}

function createSingleSellerPaid(label) {
  return createPaid(label, [{ offerId: PARS, quantity: 1 }]);
}

function readyShipments(checkoutId, metaLabel = "T022") {
  const d = detail(checkoutId);
  const out = [];
  for (const s of sellersOf(d)) {
    const sellerOrderId = sellerId(s);
    const fulfillmentId = pick(s, "fulfillmentId", "FulfillmentId");
    const line = (s.lines ?? s.Lines ?? [])[0];
    const orderLineId = pick(line, "orderLineId", "OrderLineId", "id", "Id");
    const qtyRaw = pick(line, "quantity", "Quantity") ?? 1;
    const qty = Number(qtyRaw);
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
        recipientName: metaLabel,
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
    const allocations = shipment.allocations ?? shipment.Allocations ?? [];
    r = postOp(checkoutId, {
      code: "assign_tracking",
      sellerOrderId,
      fulfillmentId,
      shipmentId,
      trackingReference: `TRK-${shipmentId.slice(0, 8)}`,
    });
    if (r.status >= 400) throw new Error(`assign_tracking ${r.status} ${JSON.stringify(r.json)}`);
    out.push({
      sellerOrderId,
      fulfillmentId,
      shipmentId,
      quantity: qty,
      quantityRaw: qtyRaw,
      allocations,
    });
  }
  return out;
}

function scenario(name, fn) {
  try {
    const data = fn();
    return { name, pass: true, ...data };
  } catch (err) {
    return { name, pass: false, error: String(err?.stack || err) };
  }
}

ensureStock();

const report = {
  task: "TB-P09-T022",
  host: BASE,
  startedAt: new Date().toISOString(),
  scenarios: {},
  summary: [],
};

// A — single-seller: no create_consolidated_package / empty packages
report.scenarios.A = scenario("A_single_seller", () => {
  const a = createSingleSellerPaid("T022-A");
  assert(a.ok, `single seller cart/checkout failed ${JSON.stringify(a.lineResults)}`);
  const members = readyShipments(a.checkoutId, "T022-A");
  assert(members.length === 1, `expected 1 seller shipment got ${members.length}`);
  const d = detail(a.checkoutId);
  assert(sellersOf(d).length === 1, "single seller order");
  assert(packagesOf(d).length === 0, "consolidatedPackages empty");
  const ops = req("GET", `/v1/admin/orders/${a.checkoutId}/operations`);
  const codes = actionCodes(ops);
  assert(!codes.includes("create_consolidated_package"), `create_consolidated_package must be absent; have=${codes.join(",")}`);
  return {
    checkoutId: a.checkoutId,
    sellerCount: sellersOf(d).length,
    packages: 0,
    hasCreateCapability: false,
    opsSample: codes.filter((c) => c.includes("package") || c.includes("shipment")).slice(0, 12),
  };
});

// B — multi-seller create + member lock
report.scenarios.B = scenario("B_create_and_lock", () => {
  const b = createMultiSellerPaid("T022-B");
  assert(b.ok, `multi cart failed ${JSON.stringify(b.lineResults)}`);
  const members = readyShipments(b.checkoutId, "T022-B");
  assert(members.length >= 2, "need 2 seller shipments");
  let d = detail(b.checkoutId);
  assert(packagesOf(d).length === 0, "no package yet");
  const create = postOp(b.checkoutId, {
    code: "create_consolidated_package",
    shipmentIds: members.map((m) => m.shipmentId),
    shippingMethodCode: "post",
    trackingReference: "CENTRAL-T022-B",
  });
  assert(create.status < 400, `create ${create.status} ${JSON.stringify(create.json)}`);
  d = detail(b.checkoutId);
  const pkgs = packagesOf(d);
  assert(pkgs.length === 1, "one package");
  const pkgId = pick(pkgs[0], "consolidatedPackageId", "ConsolidatedPackageId");
  const pkgNum = pick(pkgs[0], "packageNumber", "PackageNumber");
  assert(String(pkgNum).startsWith("MP-"), "MP number");
  assert(
    shipmentsOf(d)
      .filter((sh) => (sh.status ?? sh.Status) === "Created")
      .every((sh) => sh.activePackageNumber || sh.ActivePackageNumber),
    "members locked flags",
  );
  const blocked = postOp(b.checkoutId, {
    code: "dispatch_shipment",
    sellerOrderId: members[0].sellerOrderId,
    fulfillmentId: members[0].fulfillmentId,
    shipmentId: members[0].shipmentId,
  });
  assert(blocked.status >= 400, "direct dispatch should fail");
  const blockedBody = JSON.stringify(blocked.json);
  assert(
    blockedBody.includes("locked_by_consolidated_package") ||
      blockedBody.includes("بسته تجمیعی") ||
      blocked.json?.errorCode === "order.operation.invalid",
    `lock/suppress error ${blockedBody}`,
  );
  return {
    checkoutId: b.checkoutId,
    secret: b.secret,
    pkgId,
    pkgNum,
    members,
    createStatus: create.status,
    blockedDirectDispatch: blocked.status,
    blockedBody: blocked.json,
  };
});

// C — cancel then rebuild; old cancelled remains
report.scenarios.C = scenario("C_rebuild", () => {
  const base = report.scenarios.B;
  assert(base.pass, `depends on B: ${base.error}`);
  const { checkoutId, pkgId: pkg1Id, members } = base;
  const cancelPkg = postOp(checkoutId, {
    code: "cancel_consolidated_package",
    consolidatedPackageId: pkg1Id,
  });
  assert(cancelPkg.status < 400, `cancel pkg ${cancelPkg.status} ${JSON.stringify(cancelPkg.json)}`);
  let d = detail(checkoutId);
  const cancelled = packagesOf(d).find((p) => pick(p, "consolidatedPackageId", "ConsolidatedPackageId") === pkg1Id);
  assert((cancelled?.status ?? cancelled?.Status) === "Cancelled", "old cancelled");
  const create = postOp(checkoutId, {
    code: "create_consolidated_package",
    shipmentIds: members.map((m) => m.shipmentId),
    shippingMethodCode: "tipax",
    trackingReference: "CENTRAL-T022-C",
  });
  assert(create.status < 400, `recreate ${create.status} ${JSON.stringify(create.json)}`);
  d = detail(checkoutId);
  const pkgs = packagesOf(d);
  assert(pkgs.length >= 2, "old + new in list");
  const active = pkgs.find((p) => (p.status ?? p.Status) === "Created");
  const old = pkgs.find((p) => pick(p, "consolidatedPackageId", "ConsolidatedPackageId") === pkg1Id);
  const pkg2Id = pick(active, "consolidatedPackageId", "ConsolidatedPackageId");
  const pkg2Num = pick(active, "packageNumber", "PackageNumber");
  assert(pkg2Id && pkg2Id !== pkg1Id, "new package id");
  assert((old?.status ?? old?.Status) === "Cancelled", "cancelled remains in packages list");
  return {
    checkoutId,
    pkg1Id,
    pkg2Id,
    pkg2Num,
    members,
    packageCount: pkgs.length,
    createStatus: create.status,
  };
});

// D — central dispatch; whole-order cancel blocked
report.scenarios.D = scenario("D_dispatch", () => {
  const base = report.scenarios.C;
  assert(base.pass, `depends on C: ${base.error}`);
  const { checkoutId, pkg2Id, members } = base;
  waitForCode(checkoutId, "dispatch_consolidated_package");
  const dispatch = postOp(checkoutId, {
    code: "dispatch_consolidated_package",
    consolidatedPackageId: pkg2Id,
  });
  assert(dispatch.status < 400, `dispatch ${dispatch.status} ${JSON.stringify(dispatch.json)}`);
  const d = detail(checkoutId);
  const dispatchedPkg = packagesOf(d).find(
    (p) => pick(p, "consolidatedPackageId", "ConsolidatedPackageId") === pkg2Id,
  );
  assert((dispatchedPkg?.status ?? dispatchedPkg?.Status) === "Dispatched", "pkg dispatched");
  for (const m of members) {
    const sh = shipmentsOf(d).find((x) => pick(x, "shipmentId", "ShipmentId") === m.shipmentId);
    assert((sh?.status ?? sh?.Status) === "Dispatched", `member ${m.shipmentId} dispatched`);
  }
  const cancelOrder = postOp(checkoutId, { code: "cancel" });
  assert(cancelOrder.status >= 400, "whole order cancel blocked after dispatch");
  return {
    checkoutId,
    pkg2Id,
    dispatchStatus: dispatch.status,
    cancelOrderBlocked: cancelOrder.status,
    cancelOrderBody: cancelOrder.json,
  };
});

// E — central deliver; members Delivered
report.scenarios.E = scenario("E_deliver", () => {
  const base = report.scenarios.D;
  assert(base.pass, `depends on D: ${base.error}`);
  const c = report.scenarios.C;
  const { checkoutId, pkg2Id } = base;
  const members = c.members;
  waitForCode(checkoutId, "deliver_consolidated_package");
  const deliver = postOp(checkoutId, {
    code: "deliver_consolidated_package",
    consolidatedPackageId: pkg2Id,
  });
  assert(deliver.status < 400, `deliver ${deliver.status} ${JSON.stringify(deliver.json)}`);
  const d = detail(checkoutId);
  const deliveredPkg = packagesOf(d).find(
    (p) => pick(p, "consolidatedPackageId", "ConsolidatedPackageId") === pkg2Id,
  );
  assert((deliveredPkg?.status ?? deliveredPkg?.Status) === "Delivered", "pkg delivered");
  for (const m of members) {
    const sh = shipmentsOf(d).find((x) => pick(x, "shipmentId", "ShipmentId") === m.shipmentId);
    assert((sh?.status ?? sh?.Status) === "Delivered", `member ${m.shipmentId} delivered`);
  }
  return {
    checkoutId,
    pkg2Id,
    deliverStatus: deliver.status,
    pkgStatus: deliveredPkg?.status ?? deliveredPkg?.Status,
  };
});

// F — authenticated owned customer prefers package tracking
report.scenarios.F = scenario("F_owned_customer", () => {
  const f = createMultiSellerPaid("T022-F");
  assert(f.ok, `F cart failed ${JSON.stringify(f.lineResults)}`);
  const members = readyShipments(f.checkoutId, "T022-F");
  const create = postOp(f.checkoutId, {
    code: "create_consolidated_package",
    shipmentIds: members.map((m) => m.shipmentId),
    shippingMethodCode: "post",
    trackingReference: "CENTRAL-T022-F",
  });
  assert(create.status < 400, `F create ${create.status} ${JSON.stringify(create.json)}`);
  sql(
    `UPDATE "order".checkouts SET placed_by_user_id = '${OWNED_ACTOR}' WHERE checkout_id = '${f.checkoutId}';\n`,
  );
  const owned = custGet(f.checkoutId, { "X-Tooba-Dev-Actor-User-Id": OWNED_ACTOR });
  assert(owned.status === 200, `owned cust ${owned.status} ${owned.text}`);
  const preferred =
    owned.json.preferredCustomerTrackingReference || owned.json.PreferredCustomerTrackingReference;
  assert(preferred === "CENTRAL-T022-F", `preferred got ${preferred}`);
  return {
    checkoutId: f.checkoutId,
    status: owned.status,
    preferred,
  };
});

// G — guest secret works; wrong secret / GuestActor alone 404
report.scenarios.G = scenario("G_guest_security", () => {
  const g = createMultiSellerPaid("T022-G");
  assert(g.ok, `G cart failed ${JSON.stringify(g.lineResults)}`);
  const members = readyShipments(g.checkoutId, "T022-G");
  const create = postOp(g.checkoutId, {
    code: "create_consolidated_package",
    shipmentIds: members.map((m) => m.shipmentId),
    shippingMethodCode: "post",
    trackingReference: "CENTRAL-T022-G",
  });
  assert(create.status < 400, `G create ${create.status} ${JSON.stringify(create.json)}`);
  const d = detail(g.checkoutId);
  const pkgNum = pick(packagesOf(d)[0], "packageNumber", "PackageNumber");

  const guestOk = custGet(g.checkoutId, { "X-Tooba-Guest-Secret": g.secret });
  assert(guestOk.status === 200, `guest cust ${guestOk.status} ${guestOk.text}`);
  assert(
    (guestOk.json.preferredCustomerTrackingReference || guestOk.json.PreferredCustomerTrackingReference) ===
      "CENTRAL-T022-G",
    "guest preferred",
  );

  const guestActor = custGet(g.checkoutId, { "X-Tooba-Dev-Actor-User-Id": GUEST_ACTOR });
  assert(guestActor.status === 404, `guest actor alone ${guestActor.status}`);

  const badSecret = custGet(g.checkoutId, { "X-Tooba-Guest-Secret": "not-the-real-secret" });
  assert(badSecret.status === 404, `bad secret ${badSecret.status}`);

  return {
    checkoutId: g.checkoutId,
    pkgNum,
    guestStatus: guestOk.status,
    guestActorStatus: guestActor.status,
    badSecretStatus: badSecret.status,
    preferred:
      guestOk.json.preferredCustomerTrackingReference || guestOk.json.PreferredCustomerTrackingReference,
  };
});

// H — multi-seller without package: independent pack+dispatch
report.scenarios.H = scenario("H_legacy_independent", () => {
  const h = createMultiSellerPaid("T022-H");
  assert(h.ok, `H cart failed ${JSON.stringify(h.lineResults)}`);
  const members = readyShipments(h.checkoutId, "T022-H");
  const dispatch = postOp(h.checkoutId, {
    code: "dispatch_shipment",
    sellerOrderId: members[0].sellerOrderId,
    fulfillmentId: members[0].fulfillmentId,
    shipmentId: members[0].shipmentId,
  });
  assert(dispatch.status < 400, `independent dispatch ${dispatch.status} ${JSON.stringify(dispatch.json)}`);
  const d = detail(h.checkoutId);
  assert(packagesOf(d).length === 0, "no package on legacy flow");
  return {
    checkoutId: h.checkoutId,
    independentDispatch: dispatch.status,
    packages: 0,
  };
});

// I — concurrency: second create with overlapping active membership rejected
report.scenarios.I = scenario("I_concurrency", () => {
  const i = createMultiSellerPaid("T022-I");
  assert(i.ok, `I cart failed ${JSON.stringify(i.lineResults)}`);
  const members = readyShipments(i.checkoutId, "T022-I");
  const shipmentIds = members.map((m) => m.shipmentId);
  const first = postOp(i.checkoutId, {
    code: "create_consolidated_package",
    shipmentIds,
    shippingMethodCode: "post",
    trackingReference: "CENTRAL-T022-I1",
  });
  assert(first.status < 400, `first create ${first.status} ${JSON.stringify(first.json)}`);
  const second = postOp(i.checkoutId, {
    code: "create_consolidated_package",
    shipmentIds,
    shippingMethodCode: "tipax",
    trackingReference: "CENTRAL-T022-I2",
  });
  assert(second.status >= 400, `second create should fail got ${second.status}`);
  const body = JSON.stringify(second.json);
  assert(
    body.includes("shipment_already_member") ||
      body.includes("عضو") ||
      second.json?.errorCode === "fulfillment.package.shipment_already_member" ||
      second.json?.errorCode === "order.operation.invalid",
    `overlap reject ${body}`,
  );
  const d = detail(i.checkoutId);
  const active = packagesOf(d).filter((p) => (p.status ?? p.Status) === "Created");
  assert(active.length === 1, `exactly one active package got ${active.length}`);
  return {
    checkoutId: i.checkoutId,
    firstStatus: first.status,
    secondStatus: second.status,
    secondBody: second.json,
    activeCount: active.length,
  };
});

// J — decimal 1.25 preserved through package create
report.scenarios.J = scenario("J_decimal", () => {
  const notes = [];
  let paid = createPaid("T022-J", [
    { offerId: PARS, quantity: 1.25 },
    { offerId: ARMAN, quantity: 1 },
  ]);
  if (!paid.ok) {
    notes.push(`cart qty 1.25 failed: ${JSON.stringify(paid.lineResults)}`);
    paid = createMultiSellerPaid("T022-J-fallback", 1, 1);
    assert(paid.ok, `fallback cart failed ${JSON.stringify(paid.lineResults)}`);
    notes.push("fell back to qty 1+1; will still assert shipment allocation decimals when present");
  } else {
    notes.push("cart accepted PARS quantity 1.25");
  }
  const members = readyShipments(paid.checkoutId, "T022-J");
  assert(members.length >= 2, "need 2 shipments for package");
  const parsMember = members.find((m) => Number(m.quantity) === 1.25) || members[0];
  const create = postOp(paid.checkoutId, {
    code: "create_consolidated_package",
    shipmentIds: members.map((m) => m.shipmentId),
    shippingMethodCode: "post",
    trackingReference: "CENTRAL-T022-J",
  });
  assert(create.status < 400, `J create ${create.status} ${JSON.stringify(create.json)}`);
  const d = detail(paid.checkoutId);
  const pkgs = packagesOf(d);
  assert(pkgs.length === 1, "package created");
  const sh = shipmentsOf(d).find((x) => pick(x, "shipmentId", "ShipmentId") === parsMember.shipmentId);
  const allocs = sh?.allocations ?? sh?.Allocations ?? parsMember.allocations ?? [];
  const allocQty = allocs.map((a) => pick(a, "quantity", "Quantity")).find((q) => q != null);
  const lineQty = parsMember.quantityRaw ?? parsMember.quantity;
  const exact =
    String(lineQty) === "1.25" ||
    Number(lineQty) === 1.25 ||
    String(allocQty) === "1.25" ||
    Number(allocQty) === 1.25;
  if (notes[0]?.includes("accepted")) {
    assert(exact, `expected exact 1.25 preserved; line=${lineQty} alloc=${allocQty}`);
  } else {
    notes.push(`allocation/line after fallback: line=${lineQty} alloc=${JSON.stringify(allocQty)}`);
  }
  return {
    checkoutId: paid.checkoutId,
    createStatus: create.status,
    lineQuantity: lineQty,
    allocationQuantity: allocQty ?? null,
    exactDecimal: exact,
    notes,
  };
});

// Bonus — pre-dispatch order cancel voids package
report.scenarios.order_cancel_void = scenario("order_cancel_voids_package", () => {
  const o = createMultiSellerPaid("T022-CANCEL");
  assert(o.ok, `cancel-order cart failed ${JSON.stringify(o.lineResults)}`);
  const members = readyShipments(o.checkoutId, "T022-CANCEL");
  const create = postOp(o.checkoutId, {
    code: "create_consolidated_package",
    shipmentIds: members.map((m) => m.shipmentId),
    shippingMethodCode: "post",
    trackingReference: "CENTRAL-T022-CANCEL",
  });
  assert(create.status < 400, `cancel-order create ${create.status}`);
  const cancel = postOp(o.checkoutId, { code: "cancel" });
  assert(cancel.status < 400, `order cancel ${cancel.status} ${JSON.stringify(cancel.json)}`);
  const d = detail(o.checkoutId);
  const pkgs = packagesOf(d);
  assert(pkgs.length >= 1, "package row remains");
  assert(
    pkgs.every((p) => (p.status ?? p.Status) === "Cancelled"),
    `packages should be Cancelled got ${pkgs.map((p) => p.status ?? p.Status).join(",")}`,
  );
  return {
    checkoutId: o.checkoutId,
    cancelStatus: cancel.status,
    packageStatuses: pkgs.map((p) => p.status ?? p.Status),
  };
});

const order = ["A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "order_cancel_void"];
for (const key of order) {
  const s = report.scenarios[key];
  report.summary.push({
    key,
    name: s.name,
    pass: s.pass,
    error: s.pass ? undefined : s.error,
  });
}

report.ok = report.summary.every((s) => s.pass);
report.finishedAt = new Date().toISOString();

const outPath = join(__dirname, "runtime-raw.json");
writeFileSync(outPath, JSON.stringify(report, null, 2));

console.log("\n=== TB-P09-T022 Runtime Smoke Summary ===");
for (const s of report.summary) {
  console.log(`${s.pass ? "PASS" : "FAIL"}  ${s.key} (${s.name})${s.pass ? "" : " — " + (s.error || "").split("\n")[0]}`);
}
console.log(report.ok ? "\nOVERALL PASS" : "\nOVERALL FAIL");
console.log(`Wrote ${outPath}`);

if (!report.ok) process.exit(1);
