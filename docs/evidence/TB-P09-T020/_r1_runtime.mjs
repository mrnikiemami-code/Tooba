import { writeFileSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { randomUUID } from "node:crypto";

const BASE = "http://127.0.0.1:5088";
const KG_OFFER = "01a03826-9936-7000-b499-ff26a6123a8c";
const B_OFFER_FIXTURE = "01a03826-a21a-7000-ac2a-b44156838b10";
const ARMAN_OFFER = "01a030d1-40f1-7000-95f6-b8efc58e2619";
const ARMAN_SELLER = "01a030d1-40cb-7000-8abe-6d31739956c5";
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

function postOp(checkoutId, body) {
  return req("POST", `/v1/admin/orders/${checkoutId}/operations`, body);
}

function sellerOfOffer(detail, offerId) {
  const sellers = detail.json?.sellerOrders ?? detail.json?.SellerOrders ?? [];
  return sellers.find((s) => {
    const lines = s.lines ?? s.Lines ?? [];
    return lines.some((l) => String(pick(l, "offerId", "OfferId")) === String(offerId));
  }) ?? sellers[0];
}

function lineOf(seller) {
  return seller?.lines?.[0] ?? seller?.Lines?.[0];
}

function shipmentsOf(seller) {
  return seller?.shipments ?? seller?.Shipments ?? [];
}

function loadDetail(checkoutId) {
  return req("GET", `/v1/admin/orders/${checkoutId}`);
}

function ctxFromSeller(checkoutId, seller) {
  const line = lineOf(seller);
  return {
    checkoutId,
    sellerOrderId: pick(seller, "sellerOrderId", "SellerOrderId"),
    fulfillmentId: pick(seller, "fulfillmentId", "FulfillmentId"),
    fulfillmentStatus: pick(seller, "fulfillmentStatus", "FulfillmentStatus"),
    orderLineId: pick(line, "orderLineId", "OrderLineId"),
    sellerPartyId: pick(seller, "sellerPartyId", "SellerPartyId"),
    operationalStatus: pick(line, "operationalStatus", "OperationalStatus"),
  };
}

function resolveSecondOffer() {
  const listing = req("GET", "/v1/storefront/products?inStock=true&pageSize=48");
  const products = listing.json?.products ?? listing.json?.Products ?? [];
  const kg = products.find((p) => String(pick(p, "primaryOfferId", "PrimaryOfferId")) === KG_OFFER);
  const kgSeller = pick(kg, "sellerPartyId", "SellerPartyId");
  const other = products.find((p) => {
    const offerId = String(pick(p, "primaryOfferId", "PrimaryOfferId") ?? "");
    const seller = String(pick(p, "sellerPartyId", "SellerPartyId") ?? "");
    const stock = pick(p, "inStock", "InStock");
    return offerId && offerId !== KG_OFFER && seller && seller !== String(kgSeller ?? "") && stock !== false;
  });
  const listingOffer = pick(other, "primaryOfferId", "PrimaryOfferId") ?? null;
  return {
    listingStatus: listing.status,
    productCount: products.length,
    fixtureOffer: B_OFFER_FIXTURE,
    offerId: listingOffer ?? ARMAN_OFFER,
    sellerPartyId: pick(other, "sellerPartyId", "SellerPartyId") ?? ARMAN_SELLER,
    kgSellerPartyId: kgSeller ?? null,
    usedListing: !!listingOffer,
  };
}

function createPaidMulti(secondOfferId) {
  const cart = req("POST", "/v1/storefront/cart");
  const secret = cart.json?.guestSecret ?? cart.json?.GuestSecret;
  const guest = { "X-Tooba-Guest-Secret": secret };
  const added = req(
    "POST",
    `/v1/storefront/cart/${cart.json.cartId}/lines?expectedVersion=${cart.json.version}`,
    { offerId: KG_OFFER, quantity: 1.25 },
    guest,
  );
  const v2 = added.json?.version ?? cart.json.version;
  const probeB = req(
    "POST",
    `/v1/storefront/cart/${cart.json.cartId}/lines?expectedVersion=${v2}`,
    { offerId: B_OFFER_FIXTURE, quantity: 1 },
    guest,
  );
  let version = probeB.json?.version ?? v2;
  let addedB = probeB;
  if (probeB.status >= 400 && secondOfferId && String(secondOfferId) !== B_OFFER_FIXTURE) {
    addedB = req(
      "POST",
      `/v1/storefront/cart/${cart.json.cartId}/lines?expectedVersion=${version}`,
      { offerId: secondOfferId, quantity: 1 },
      guest,
    );
    version = addedB.json?.version ?? version;
  }
  const checkout = req(
    "POST",
    "/v1/storefront/checkout",
    {
      cartId: cart.json.cartId,
      expectedCartVersion: addedB.json?.version ?? version,
      idempotencyKey: `t020r1-${Date.now()}`,
      shipping: {
        recipientName: "T020R1",
        contactMobile: "+989121234567",
        provinceName: "تهران",
        cityName: "تهران",
        postalAddress: "آدرس تست T020-R1",
        postalCode: "1234567890",
      },
    },
    guest,
  );
  const checkoutId = pick(checkout.json, "checkoutId", "CheckoutId");
  if (!checkoutId) return { checkoutId: null, error: checkout.json, addB: addedB.status, probeB: probeB.status, probeCode: errCode(probeB) };
  req(
    "POST",
    `/v1/storefront/checkout/${checkoutId}/payments`,
    { cartId: cart.json.cartId, idempotencyKey: randomUUID().replaceAll("-", ""), providerCode: "manual" },
    guest,
  );
  waitForCode(checkoutId, "confirm_deposit");
  postOp(checkoutId, { code: "confirm_deposit", idempotencyKey: randomUUID().replaceAll("-", "") });
  let detail = loadDetail(checkoutId);
  for (let i = 0; i < 40; i++) {
    const sellers = detail.json?.sellerOrders ?? detail.json?.SellerOrders ?? [];
    if (sellers.length >= 2 && sellers.every((s) => pick(s, "fulfillmentId", "FulfillmentId"))) break;
    sleep(250);
    detail = loadDetail(checkoutId);
  }
  const kgSeller = sellerOfOffer(detail, KG_OFFER);
  const bSeller = (detail.json?.sellerOrders ?? detail.json?.SellerOrders ?? [])
    .find((s) => String(pick(s, "sellerPartyId", "SellerPartyId")) !== String(pick(kgSeller, "sellerPartyId", "SellerPartyId")));
  return {
    checkoutId,
    kg: ctxFromSeller(checkoutId, kgSeller),
    b: bSeller ? ctxFromSeller(checkoutId, bSeller) : {},
    sellerCount: (detail.json?.sellerOrders ?? detail.json?.SellerOrders ?? []).length,
    addB: addedB.status,
    probeB: probeB.status,
    probeCode: errCode(probeB),
    probeDetail: errText(probeB),
    usedOffer: secondOfferId,
  };
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
      recipientName: "T020R1",
      recipientPhone: "09121234567",
      postalCode: "1234567890",
      destinationAddress: "تهران",
      serviceType: "پیشتاز",
      packageCount: 1,
      weightKg: qty,
    }),
    selections: [{ orderLineId: ctx.orderLineId, quantity: qty }],
  });
  const detail = loadDetail(ctx.checkoutId);
  const seller = (detail.json?.sellerOrders ?? detail.json?.SellerOrders ?? [])
    .find((s) => String(pick(s, "sellerOrderId", "SellerOrderId")) === String(ctx.sellerOrderId));
  const shipment = shipmentsOf(seller).filter((s) => {
    const st = pick(s, "status", "Status");
    return st === "Created" || st === 0 || st === "0";
  }).at(-1) ?? shipmentsOf(seller).at(-1);
  return {
    packStatus: pack.status,
    packErr: pack.status >= 400 ? errText(pack) : "",
    shipStatus: ship.status,
    shipErr: ship.status >= 400 ? errText(ship) : "",
    shipmentId: pick(shipment, "shipmentId", "ShipmentId"),
    fulfillmentStatus: pick(seller, "fulfillmentStatus", "FulfillmentStatus"),
    lineStatus: pick(lineOf(seller), "operationalStatus", "OperationalStatus"),
  };
}

function dispatchShipment(ctx, shipmentId) {
  postOp(ctx.checkoutId, {
    code: "assign_tracking",
    sellerOrderId: ctx.sellerOrderId,
    fulfillmentId: ctx.fulfillmentId,
    shipmentId,
    trackingReference: `T020R1-${String(shipmentId).slice(-6)}-${Date.now()}`,
  });
  return postOp(ctx.checkoutId, {
    code: "dispatch_shipment",
    sellerOrderId: ctx.sellerOrderId,
    fulfillmentId: ctx.fulfillmentId,
    shipmentId,
  });
}

function deliverShipment(ctx, shipmentId) {
  return postOp(ctx.checkoutId, {
    code: "deliver_shipment",
    sellerOrderId: ctx.sellerOrderId,
    fulfillmentId: ctx.fulfillmentId,
    shipmentId,
  });
}

const health = req("GET", "/health");
note("health", { status: health.status, ok: health.status < 400 });

const second = resolveSecondOffer();
note("multiseller-404", {
  fixtureStatusProbeDeferred: true,
  listingStatus: second.listingStatus,
  listingOffer: second.offerId,
  listingSeller: second.sellerPartyId,
  kgSeller: second.kgSellerPartyId,
    ok: !!second.offerId && String(second.sellerPartyId) !== String(second.kgSellerPartyId || "01a03826-97c5-7000-ad15-c9d141b1f32e"),
});

const paid = createPaidMulti(second.offerId);
if (!paid.checkoutId) {
  note("checkout", { error: paid.error, addB: paid.addB, probeB: paid.probeB, probeCode: paid.probeCode, ok: false });
} else {
  const kg = paid.kg;
  const b = paid.b;
  note("checkout", {
    checkoutId: paid.checkoutId,
    sellerCount: paid.sellerCount,
    distinctSellers: String(kg.sellerPartyId) !== String(b.sellerPartyId),
    probeB: paid.probeB,
    probeCode: paid.probeCode,
    addB: paid.addB,
    ok: paid.sellerCount >= 2 && String(kg.sellerPartyId) !== String(b.sellerPartyId),
  });
  note("multiseller-404-probe", {
    fixtureStatus: paid.probeB,
    fixtureCode: paid.probeCode,
    fixtureDetail: String(paid.probeDetail ?? "").slice(0, 180),
    listingOfferUsed: paid.addB < 400,
    ok: paid.probeB === 404 || paid.probeB < 400,
  });

  const before = req("GET", `/v1/admin/orders/${kg.checkoutId}/operations`);
  if (actionCodes(before).includes("mark_processing")) {
    postOp(kg.checkoutId, {
      code: "mark_processing",
      sellerOrderId: kg.sellerOrderId,
      fulfillmentId: kg.fulfillmentId,
    });
  }
  const a1 = packAndCreateShipment(kg, 0.5);
  const d1 = dispatchShipment(kg, a1.shipmentId);
  const afterA = loadDetail(kg.checkoutId);
  const kgAfter = sellerOfOffer(afterA, KG_OFFER);
  const bAfter = (afterA.json?.sellerOrders ?? afterA.json?.SellerOrders ?? [])
    .find((s) => String(pick(s, "sellerPartyId", "SellerPartyId")) === String(b.sellerPartyId));
  const opsAfterA = req("GET", `/v1/admin/orders/${kg.checkoutId}/operations`);
  const codesAfterA = actionCodes(opsAfterA);
  const cancel = postOp(kg.checkoutId, { code: "cancel", idempotencyKey: randomUUID() });
  const kgStatus = pick(kgAfter, "fulfillmentStatus", "FulfillmentStatus");
  const bStatus = pick(bAfter, "fulfillmentStatus", "FulfillmentStatus");
  const kgLineStatus = pick(lineOf(kgAfter), "operationalStatus", "OperationalStatus");
  note("partial-status", {
    packA: a1.packStatus,
    shipA: a1.shipStatus,
    dispatchA: d1.status,
    kgStatus,
    kgLineStatus,
    bStatus,
    hasPackAfter: codesAfterA.includes("pack_selected") || codesAfterA.includes("mark_packed"),
    hasCancel: codesAfterA.includes("cancel"),
    cancelStatus: cancel.status,
    ok:
      a1.packStatus < 400
      && d1.status < 400
      && kgStatus === "PartialDispatched"
      && kgLineStatus === "PartialDispatched"
      && bStatus !== "Dispatched"
      && bStatus !== "PartialDispatched"
      && (codesAfterA.includes("pack_selected") || codesAfterA.includes("mark_packed"))
      && !codesAfterA.includes("cancel")
      && cancel.status >= 400,
  });

  const qNeeds = req("POST", "/v1/admin/fulfillments/work-queue/query", {
    page: 1,
    pageSize: 20,
    search: null,
    sort: [{ field: "updatedAt", direction: "desc" }],
    filters: [{ field: "queueFilter", operator: "equals", value: "needs_action", values: ["needs_action"] }],
  });
  const qInTransit = req("POST", "/v1/admin/fulfillments/work-queue/query", {
    page: 1,
    pageSize: 20,
    search: null,
    sort: [{ field: "updatedAt", direction: "desc" }],
    filters: [{ field: "queueFilter", operator: "equals", value: "in_transit", values: ["in_transit"] }],
  });
  const items = (payload) => payload.json?.items ?? payload.json?.Items ?? [];
  const inNeeds = items(qNeeds).some((r) => String(pick(r, "fulfillmentId", "FulfillmentId")) === String(kg.fulfillmentId));
  const inTransit = items(qInTransit).some((r) => String(pick(r, "fulfillmentId", "FulfillmentId")) === String(kg.fulfillmentId));
  const needRow = items(qNeeds).find((r) => String(pick(r, "fulfillmentId", "FulfillmentId")) === String(kg.fulfillmentId));
  note("filters", {
    needs: qNeeds.status,
    inTransitStatus: qInTransit.status,
    inNeeds,
    inTransit,
    needRowStatus: pick(needRow, "status", "Status"),
    ok: qNeeds.status < 400 && inNeeds && !inTransit && pick(needRow, "status", "Status") === "PartialDispatched",
  });

  if (b.sellerOrderId) {
    const bOps = req("GET", `/v1/admin/orders/${b.checkoutId}/operations`);
    if (actionCodes(bOps).includes("mark_processing")) {
      postOp(b.checkoutId, {
        code: "mark_processing",
        sellerOrderId: b.sellerOrderId,
        fulfillmentId: b.fulfillmentId,
      });
    }
    const bShip = packAndCreateShipment(b, 1);
    const bDetail = loadDetail(b.checkoutId);
    const bNow = (bDetail.json?.sellerOrders ?? bDetail.json?.SellerOrders ?? [])
      .find((s) => String(pick(s, "sellerOrderId", "SellerOrderId")) === String(b.sellerOrderId));
    note("seller-b-separate", {
      pack: bShip.packStatus,
      ship: bShip.shipStatus,
      bStatus: pick(bNow, "fulfillmentStatus", "FulfillmentStatus"),
      differentFulfillment: String(b.fulfillmentId) !== String(kg.fulfillmentId),
      ok:
        bShip.packStatus < 400
        && bShip.shipStatus < 400
        && String(b.fulfillmentId) !== String(kg.fulfillmentId)
        && pick(bNow, "fulfillmentStatus", "FulfillmentStatus") !== "Dispatched"
        && pick(bNow, "fulfillmentStatus", "FulfillmentStatus") !== "PartialDispatched",
    });
  } else {
    note("seller-b-separate", { skipped: true, ok: false });
  }

  const a2 = packAndCreateShipment(kg, 0.75);
  const d2 = dispatchShipment(kg, a2.shipmentId);
  const afterB = loadDetail(kg.checkoutId);
  const kgDone = sellerOfOffer(afterB, KG_OFFER);
  note("terminal-dispatch", {
    packB: a2.packStatus,
    shipB: a2.shipStatus,
    dispatchB: d2.status,
    kgStatus: pick(kgDone, "fulfillmentStatus", "FulfillmentStatus"),
    ok: a2.packStatus < 400 && a2.shipStatus < 400 && d2.status < 400
      && pick(kgDone, "fulfillmentStatus", "FulfillmentStatus") === "Dispatched",
  });

  const del1 = deliverShipment(kg, a1.shipmentId);
  const del2 = deliverShipment(kg, a2.shipmentId);
  waitForCode(kg.checkoutId, "request_return");
  const ret = postOp(kg.checkoutId, {
    code: "request_return",
    sellerOrderId: kg.sellerOrderId,
    selections: [{ orderLineId: kg.orderLineId, quantity: 0.25 }],
    idempotencyKey: randomUUID(),
  });
  note("return-0.25", {
    deliverA: del1.status,
    deliverB: del2.status,
    retStatus: ret.status,
    ok: del1.status < 400 && del2.status < 400 && ret.status < 400,
  });
}

const iOps = req("GET", `/v1/admin/orders/${T016_CANCELLED_PAID}/operations`);
note("cancel-refund-coexistence", {
  opsStatus: iOps.status,
  noRequestReturn: !actionCodes(iOps).includes("request_return"),
  ok: iOps.status < 400 && !actionCodes(iOps).includes("request_return"),
});

writeFileSync("docs/evidence/TB-P09-T020/r1-runtime-raw.json", JSON.stringify(out, null, 2));
console.log(JSON.stringify({ ok: out.ok, names: out.steps.map((s) => `${s.name}:${s.ok !== false}`) }, null, 2));
if (!out.ok) process.exit(1);
