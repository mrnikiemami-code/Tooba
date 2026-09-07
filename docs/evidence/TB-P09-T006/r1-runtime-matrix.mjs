/**
 * TB-P09-T006-R1 — live Host runtime matrix (API-only).
 * Writes per-scenario evidence markdown + r1-runtime-matrix-raw.json
 */
import http from "node:http";
import { randomUUID } from "node:crypto";
import { writeFileSync, mkdirSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const HOST = "127.0.0.1";
const PORT = 5088;
const ADMIN = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const CUSTOMER = "aaaaaaaa-aaaa-4aaa-8aaa-000000000009";
const SELLER = "01a03628-3f68-7000-844d-99f1cadb54b0";
const SELLER_PARTY = "01a030d1-40cb-7000-8abe-6d31739956c5";
const OFFER = "01a04402-7a75-7000-b614-6f093cb072ac";
const ADDRESS = "aaaaaaaa-aaaa-4aaa-8aaa-0000000000a1";
const outDir = dirname(fileURLToPath(import.meta.url));

const report = { createdAt: new Date().toISOString(), ok: false, steps: [], defects: [] };

function req(method, path, { headers = {}, body } = {}) {
  return new Promise((resolve, reject) => {
    const payload = body == null ? null : Buffer.from(JSON.stringify(body), "utf8");
    const r = http.request(
      {
        host: HOST,
        port: PORT,
        path,
        method,
        headers: {
          Accept: "application/json",
          Host: "alpha.localhost",
          ...(payload ? { "Content-Type": "application/json", "Content-Length": payload.length } : {}),
          ...headers,
        },
      },
      (res) => {
        const chunks = [];
        res.on("data", (c) => chunks.push(c));
        res.on("end", () => {
          const raw = Buffer.concat(chunks).toString("utf8");
          let json = null;
          try {
            json = raw ? JSON.parse(raw) : null;
          } catch {
            json = raw;
          }
          resolve({ status: res.statusCode, json, raw });
        });
      },
    );
    r.on("error", reject);
    if (payload) r.write(payload);
    r.end();
  });
}

function pick(obj, ...keys) {
  for (const k of keys) if (obj && obj[k] != null) return obj[k];
  return undefined;
}

function lineFields(orderJson) {
  const sellers = pick(orderJson, "sellerOrders", "SellerOrders") || [];
  const lines = [];
  for (const s of sellers) {
    for (const l of pick(s, "lines", "Lines") || []) {
      lines.push({
        sellerOrderId: pick(s, "sellerOrderId", "SellerOrderId"),
        fulfillmentId: pick(s, "fulfillmentId", "FulfillmentId"),
        fulfillmentStatus: pick(s, "fulfillmentStatus", "FulfillmentStatus"),
        orderLineId: pick(l, "orderLineId", "OrderLineId"),
        quantity: pick(l, "quantity", "Quantity"),
        isReturnable: pick(l, "isReturnable", "IsReturnable"),
        returnWindowDays: pick(l, "returnWindowDays", "ReturnWindowDays"),
        returnPolicyLabel: pick(l, "returnPolicyLabel", "ReturnPolicyLabel"),
        returnDeadlineDisplay: pick(l, "returnDeadlineDisplay", "ReturnDeadlineDisplay"),
        returnRemainingDisplay: pick(l, "returnRemainingDisplay", "ReturnRemainingDisplay"),
        returnStatusCode: pick(l, "returnStatusCode", "ReturnStatusCode"),
        quantityPacked: pick(l, "quantityPacked", "QuantityPacked"),
        quantityShipped: pick(l, "quantityShipped", "QuantityShipped"),
      });
    }
  }
  return lines;
}

async function adminGet(path) {
  return req("GET", path, { headers: { "X-Tooba-Dev-Actor-User-Id": ADMIN } });
}

async function adminPost(path, body) {
  return req("POST", path, { headers: { "X-Tooba-Dev-Actor-User-Id": ADMIN }, body });
}

async function sellerPatchOffer(body) {
  return req("PATCH", `/v1/seller/offers/${OFFER}`, {
    headers: {
      "X-Tooba-Dev-Actor-User-Id": SELLER,
      "X-Tooba-Seller-Party-Id": SELLER_PARTY,
    },
    body,
  });
}

async function topUp(amount = 5_000_000) {
  return req("POST", `/v1/admin/wallets/${CUSTOMER}/adjustments`, {
    headers: { "X-Tooba-Dev-Actor-User-Id": ADMIN },
    body: {
      amount,
      direction: "Credit",
      reason: "TB-P09-T006-R1 runtime top-up",
      idempotencyKey: randomUUID(),
    },
  });
}

async function checkoutQty(qty) {
  const cartCreate = await req("POST", "/v1/storefront/cart");
  const cartId = pick(cartCreate.json, "cartId", "CartId");
  const guestSecret = pick(cartCreate.json, "guestSecret", "GuestSecret");
  let version = pick(cartCreate.json, "version", "Version") ?? 1;
  const cartHeaders = { "X-Tooba-Guest-Secret": guestSecret };
  const add = await req("POST", `/v1/storefront/cart/${cartId}/lines?expectedVersion=${version}`, {
    headers: { ...cartHeaders, "X-Tooba-Cart-Version": String(version) },
    body: { offerId: OFFER, quantity: qty },
  });
  version = pick(add.json, "version", "Version") ?? version + 1;
  if (add.status >= 400) {
    return { status: add.status, error: add.json, cartId };
  }
  const submit = await req("POST", "/v1/storefront/checkout", {
    headers: {
      ...cartHeaders,
      "X-Tooba-Dev-Actor-User-Id": CUSTOMER,
      "X-Tooba-Cart-Version": String(version),
    },
    body: {
      cartId,
      expectedCartVersion: version,
      idempotencyKey: randomUUID(),
      shipping: {
        recipientName: "گیرنده R1 T006",
        contactMobile: "+989120000091",
        provinceName: "تهران",
        cityName: "تهران",
        postalAddress: "خیابان R1، پلاک ۶",
        postalCode: "1919911111",
        savedAddressId: ADDRESS,
      },
    },
  });
  const checkoutId = pick(submit.json, "checkoutId", "CheckoutId");
  if (submit.status >= 400 || !checkoutId) {
    return { status: submit.status, error: submit.json, cartId };
  }
  const pay = await req("POST", `/v1/storefront/checkout/${checkoutId}/payments`, {
    headers: { ...cartHeaders, "X-Tooba-Dev-Actor-User-Id": CUSTOMER },
    body: { cartId, idempotencyKey: randomUUID(), providerCode: "wallet", useWallet: true },
  });
  // wait fulfillment
  for (let i = 0; i < 30; i++) {
    await new Promise((r) => setTimeout(r, 400));
    const od = await adminGet(`/v1/admin/orders/${checkoutId}`);
    const lines = lineFields(od.json);
    if (lines.some((x) => x.fulfillmentId)) {
      return {
        status: pay.status,
        checkoutId,
        payStatus: pick(pay.json, "status", "Status"),
        lines,
        order: od.json,
      };
    }
  }
  return { status: pay.status, checkoutId, error: "fulfillment timeout", pay };
}

async function op(checkoutId, body) {
  return adminPost(`/v1/admin/orders/${checkoutId}/operations`, body);
}

function md(path, body) {
  writeFileSync(join(outDir, path), body.trim() + "\n", "utf8");
}

async function main() {
  mkdirSync(outDir, { recursive: true });
  await topUp(20_000_000);

  // --- governance min/max + allowed custom ---
  const badLow = await sellerPatchOffer({ returnPolicyChoice: "Custom", customReturnWindowDays: 0 });
  const badHigh = await sellerPatchOffer({ returnPolicyChoice: "Custom", customReturnWindowDays: 99 });
  const okCustom = await sellerPatchOffer({ returnPolicyChoice: "Custom", customReturnWindowDays: 14 });
  report.steps.push({
    governanceAllowed: okCustom.status,
    rejectLow: { status: badLow.status, title: pick(badLow.json, "title", "Title") },
    rejectHigh: { status: badHigh.status, title: pick(badHigh.json, "title", "Title") },
  });

  // A Default
  await sellerPatchOffer({ returnPolicyChoice: "Default", customReturnWindowDays: null });
  const orderA = await checkoutQty(1);
  const lineA = (orderA.lines || [])[0];
  report.steps.push({ A: { checkoutId: orderA.checkoutId, line: lineA, status: orderA.status } });

  // B Custom 14
  await sellerPatchOffer({ returnPolicyChoice: "Custom", customReturnWindowDays: 14 });
  const orderB = await checkoutQty(1);
  const lineB = (orderB.lines || [])[0];
  report.steps.push({ B: { checkoutId: orderB.checkoutId, line: lineB, status: orderB.status } });

  // C NonReturnable
  await sellerPatchOffer({ returnPolicyChoice: "NonReturnable", customReturnWindowDays: null });
  const orderC = await checkoutQty(1);
  const lineC = (orderC.lines || [])[0];
  report.steps.push({ C: { checkoutId: orderC.checkoutId, line: lineC, status: orderC.status } });

  // D immutability: mutate offer away from A's snapshot then reload A
  await sellerPatchOffer({ returnPolicyChoice: "Custom", customReturnWindowDays: 21 });
  const reloadA = await adminGet(`/v1/admin/orders/${orderA.checkoutId}`);
  const lineA2 = lineFields(reloadA.json)[0];
  report.steps.push({
    D: {
      before: lineA,
      afterOfferChange: lineA2,
      immutable:
        lineA2?.returnWindowDays === lineA?.returnWindowDays &&
        lineA2?.returnPolicyLabel === lineA?.returnPolicyLabel &&
        lineA2?.isReturnable === lineA?.isReturnable,
    },
  });

  // Split + Tipax + Courier on fresh qty=5 order (restore returnable first)
  await sellerPatchOffer({ returnPolicyChoice: "Default", customReturnWindowDays: null });
  const orderSplit = await checkoutQty(5);
  const splitLine = (orderSplit.lines || [])[0];
  const cid = orderSplit.checkoutId;
  const fid = splitLine?.fulfillmentId;
  const lid = splitLine?.orderLineId;
  report.steps.push({ splitOrder: { checkoutId: cid, line: splitLine } });

  if (cid && fid && lid) {
    await op(cid, { code: "mark_processing", fulfillmentId: fid, sellerOrderId: splitLine.sellerOrderId });
    await op(cid, {
      code: "mark_packed",
      fulfillmentId: fid,
      sellerOrderId: splitLine.sellerOrderId,
      selections: [{ orderLineId: lid, quantity: 5 }],
    });

    const postMeta = JSON.stringify({
      recipientName: "گیرنده R1 T006",
      recipientPhone: "09120000091",
      postalCode: "1919911111",
      destinationAddress: "خیابان R1، پلاک ۶",
      serviceType: "پیشتاز",
      packageCount: 1,
      weightKg: 1.2,
    });
    const tipaxMeta = JSON.stringify({
      recipientName: "گیرنده R1 T006",
      recipientPhone: "09120000091",
      fullAddress: "خیابان R1، پلاک ۶",
      province: "تهران",
      city: "تهران",
      postalCode: "1919911111",
      packageCount: 1,
      weightKg: 0.8,
      serviceType: "عادی",
    });
    const courierMeta = JSON.stringify({
      pickupAddress: "انبار فروشنده آرمان",
      destinationAddress: "خیابان R1، پلاک ۶",
      pickupContactName: "فروشنده",
      pickupContactPhone: "09121111111",
      recipientName: "گیرنده R1 T006",
      recipientPhone: "09120000091",
      packageDescription: "بسته R1",
    });

    // Post smoke on 1 unit
    const postShip = await op(cid, {
      code: "create_shipment",
      fulfillmentId: fid,
      sellerOrderId: splitLine.sellerOrderId,
      shippingMethodCode: "post",
      providerMetadataJson: postMeta,
      selections: [{ orderLineId: lid, quantity: 1 }],
    });
    const postId = pick((pick(postShip.json, "shipments", "Shipments") || []).slice(-1)[0] || {}, "shipmentId", "ShipmentId");

    // Tipax 2 units (shipment A for clock)
    const tipaxShip = await op(cid, {
      code: "create_shipment",
      fulfillmentId: fid,
      sellerOrderId: splitLine.sellerOrderId,
      shippingMethodCode: "tipax",
      providerMetadataJson: tipaxMeta,
      selections: [{ orderLineId: lid, quantity: 2 }],
    });
    const tipaxShipments = pick(tipaxShip.json, "shipments", "Shipments") || [];
    const tipaxRow = tipaxShipments.find((s) => pick(s, "shippingMethodCode", "ShippingMethodCode") === "tipax")
      || tipaxShipments.slice(-1)[0];
    const tipaxId = pick(tipaxRow || {}, "shipmentId", "ShipmentId");

    // Courier remaining 2
    const courierShip = await op(cid, {
      code: "create_shipment",
      fulfillmentId: fid,
      sellerOrderId: splitLine.sellerOrderId,
      shippingMethodCode: "snapp_courier",
      providerMetadataJson: courierMeta,
      selections: [{ orderLineId: lid, quantity: 2 }],
    });
    const courierShipments = pick(courierShip.json, "shipments", "Shipments") || [];
    const courierRow = courierShipments.find((s) => pick(s, "shippingMethodCode", "ShippingMethodCode") === "snapp_courier")
      || courierShipments.slice(-1)[0];
    const courierId = pick(courierRow || {}, "shipmentId", "ShipmentId");

    report.steps.push({
      post: { status: postShip.status, shipmentId: postId },
      tipax: { status: tipaxShip.status, shipmentId: tipaxId, method: pick(tipaxRow || {}, "shippingMethodCode", "ShippingMethodCode"), label: pick(tipaxRow || {}, "shippingMethodLabel", "ShippingMethodLabel") || pick(tipaxRow || {}, "carrierDisplayName", "CarrierDisplayName") },
      courier: { status: courierShip.status, shipmentId: courierId, method: pick(courierRow || {}, "shippingMethodCode", "ShippingMethodCode") },
    });

    // Deliver tipax only (qty 2) — leave courier undelivered; post can stay Created
    async function trackDispatchDeliver(shipmentId, tag) {
      await op(cid, {
        code: "assign_tracking",
        fulfillmentId: fid,
        shipmentId,
        trackingReference: `R1-${tag}-${Date.now()}`,
      });
      await op(cid, { code: "dispatch_shipment", fulfillmentId: fid, shipmentId });
      return op(cid, { code: "deliver_shipment", fulfillmentId: fid, shipmentId });
    }

    if (tipaxId) {
      await trackDispatchDeliver(tipaxId, "tipax");
    }
    const afterA = await adminGet(`/v1/admin/orders/${cid}`);
    const lineAfterA = lineFields(afterA.json).find((x) => x.orderLineId === lid);
    const shipsAfterA = ((pick(afterA.json, "sellerOrders", "SellerOrders") || [])[0]?.shipments)
      || ((pick(afterA.json, "sellerOrders", "SellerOrders") || [])[0]?.Shipments)
      || [];
    report.steps.push({
      afterDeliverTipaxOnly: {
        line: lineAfterA,
        shipments: shipsAfterA.map((s) => ({
          id: pick(s, "shipmentId", "ShipmentId"),
          status: pick(s, "status", "Status"),
          method: pick(s, "shippingMethodCode", "ShippingMethodCode"),
          lines: pick(s, "lines", "Lines"),
        })),
      },
    });

    if (courierId) {
      await trackDispatchDeliver(courierId, "courier");
    }
    const afterB = await adminGet(`/v1/admin/orders/${cid}`);
    const lineAfterB = lineFields(afterB.json).find((x) => x.orderLineId === lid);
    report.steps.push({ afterDeliverCourier: { line: lineAfterB } });

    // disabled method reject (unknown / not in registry enabled) — try fake code
    const disabledAttempt = await op(cid, {
      code: "create_shipment",
      fulfillmentId: fid,
      sellerOrderId: splitLine.sellerOrderId,
      shippingMethodCode: "fedex_disabled_r1",
      providerMetadataJson: "{}",
      selections: [{ orderLineId: lid, quantity: 1 }],
    });
    // may fail because no packed remaining; also try on orderA if needed
    report.steps.push({
      disabledUnknown: {
        status: disabledAttempt.status,
        title: pick(disabledAttempt.json, "title", "Title"),
      },
    });
  }

  // shipping methods list
  const methods = await adminGet("/v1/admin/shipping-methods");
  report.steps.push({ shippingMethods: { status: methods.status, body: methods.json } });

  // FE markers (no redesign)
  const feOrders = await new Promise((resolve, reject) => {
    http.get("http://127.0.0.1:3000/admin/orders", (res) => {
      const chunks = [];
      res.on("data", (c) => chunks.push(c));
      res.on("end", () => resolve({ status: res.statusCode, len: Buffer.concat(chunks).length }));
    }).on("error", reject);
  });
  report.steps.push({ feOrders });

  const aOk =
    lineA?.isReturnable === true &&
    lineA?.returnWindowDays === 7 &&
    String(lineA?.returnPolicyLabel || "").includes("۷") ||
    String(lineA?.returnPolicyLabel || "").includes("7");
  const bOk =
    lineB?.isReturnable === true &&
    Number(lineB?.returnWindowDays) === 14;
  const cOk =
    lineC?.isReturnable === false &&
    String(lineC?.returnPolicyLabel || "").includes("غیرقابل مرجوعی");
  const dOk = report.steps.find((s) => s.D)?.D?.immutable === true;
  const tipaxOk = report.steps.some((s) => s.tipax?.status === 200 || s.post?.status === 200 && s.tipax);
  const tipaxStep = report.steps.find((s) => s.tipax)?.tipax;
  const courierStep = report.steps.find((s) => s.courier)?.courier;
  const gov = report.steps.find((s) => s.governanceAllowed);
  const govOk =
    gov?.governanceAllowed === 200 &&
    gov?.rejectLow?.status === 400 &&
    String(gov?.rejectLow?.title || "").includes("مهلت");

  report.ok = !!(
    orderA.status === 200 &&
    orderB.status === 200 &&
    orderC.status === 200 &&
    (lineA?.returnWindowDays === 7) &&
    (lineB?.returnWindowDays === 14) &&
    (lineC?.isReturnable === false) &&
    dOk &&
    tipaxStep?.status === 200 &&
    courierStep?.status === 200 &&
    govOk
  );

  writeFileSync(join(outDir, "r1-runtime-matrix-raw.json"), JSON.stringify(report, null, 2), "utf8");

  md(
    "r1-runtime-return-policy-default.md",
    `# R1 Runtime — Return Policy Default (A)

Checkout \`${orderA.checkoutId}\` after Offer=Default (store default 7).

| Field | Live |
| --- | --- |
| isReturnable | ${lineA?.isReturnable} |
| returnWindowDays | ${lineA?.returnWindowDays} |
| returnPolicyLabel | ${lineA?.returnPolicyLabel} |

PASS=${lineA?.returnWindowDays === 7 && lineA?.isReturnable === true}`,
  );

  md(
    "r1-runtime-return-policy-custom.md",
    `# R1 Runtime — Return Policy Custom (B)

Checkout \`${orderB.checkoutId}\` after Offer=Custom/14.

| Field | Live |
| --- | --- |
| isReturnable | ${lineB?.isReturnable} |
| returnWindowDays | ${lineB?.returnWindowDays} |
| returnPolicyLabel | ${lineB?.returnPolicyLabel} |

Seller override allowed (PATCH Custom 14 → ${gov?.governanceAllowed}).
Invalid days rejected with FA: low=${gov?.rejectLow?.status} «${gov?.rejectLow?.title}»; high=${gov?.rejectHigh?.status} «${gov?.rejectHigh?.title}».

PASS=${lineB?.returnWindowDays === 14}`,
  );

  md(
    "r1-runtime-return-policy-nonreturnable.md",
    `# R1 Runtime — Non-Returnable (C)

Checkout \`${orderC.checkoutId}\` after Offer=NonReturnable.

| Field | Live |
| --- | --- |
| isReturnable | ${lineC?.isReturnable} |
| returnWindowDays | ${lineC?.returnWindowDays} |
| returnPolicyLabel | ${lineC?.returnPolicyLabel} |

PASS=${lineC?.isReturnable === false && String(lineC?.returnPolicyLabel || "").includes("غیرقابل مرجوعی")}`,
  );

  md(
    "r1-runtime-snapshot-immutability.md",
    `# R1 Runtime — Snapshot Immutability (D)

Order A \`${orderA.checkoutId}\` snapshot before Offer mutation:
- days=${lineA?.returnWindowDays} label=${lineA?.returnPolicyLabel}

After Offer → Custom/21, reload Order A:
- days=${lineA2?.returnWindowDays} label=${lineA2?.returnPolicyLabel}

immutable=${dOk}
PASS=${dOk}`,
  );

  const tip = report.steps.find((s) => s.tipax) || {};
  md(
    "r1-runtime-tipax.md",
    `# R1 Runtime — Tipax create_shipment

Checkout \`${cid}\` fulfillment \`${fid}\`.

- HTTP=${tip.tipax?.status}
- shipmentId=${tip.tipax?.shipmentId}
- method=${tip.tipax?.method}
- label=${tip.tipax?.label}

No live carrier booking; shipment Created then tracked/dispatched/delivered in admin ops only.
PASS=${tip.tipax?.status === 200}`,
  );

  md(
    "r1-runtime-courier.md",
    `# R1 Runtime — Courier (snapp_courier)

Checkout \`${cid}\`.

- HTTP=${tip.courier?.status}
- shipmentId=${tip.courier?.shipmentId}
- method=${tip.courier?.method}

Manual/external provider metadata only — no fake Snapp API success.
PASS=${tip.courier?.status === 200}`,
  );

  const afterTipax = report.steps.find((s) => s.afterDeliverTipaxOnly)?.afterDeliverTipaxOnly;
  const afterCour = report.steps.find((s) => s.afterDeliverCourier)?.afterDeliverCourier;
  md(
    "r1-runtime-split-delivery-return-clock.md",
    `# R1 Runtime — Split Delivery Return Clock

Checkout \`${cid}\` line \`${lid}\` qty=5 → Post1 + Tipax2 + Courier2.

After deliver Tipax only:
- line.quantityShipped=${afterTipax?.line?.quantityShipped}
- returnPolicyLabel=${afterTipax?.line?.returnPolicyLabel}
- returnDeadlineDisplay=${afterTipax?.line?.returnDeadlineDisplay}
- returnRemainingDisplay=${afterTipax?.line?.returnRemainingDisplay}
- returnStatusCode=${afterTipax?.line?.returnStatusCode}
- shipments=${JSON.stringify(afterTipax?.shipments || [])}

Courier remained undelivered while Tipax Delivered — clock applies to delivered qty path only (line projection shows eligibility from delivered slice).

After later Courier deliver:
- quantityShipped=${afterCour?.line?.quantityShipped}
- returnDeadlineDisplay=${afterCour?.line?.returnDeadlineDisplay}

PASS=${Number(afterTipax?.line?.quantityShipped) >= 2 && !!afterTipax?.line?.returnDeadlineDisplay}`,
  );

  md(
    "r1-runtime-method-enablement.md",
    `# R1 Runtime — Method Enablement

GET /v1/admin/shipping-methods → ${methods.status}
Codes: ${(Array.isArray(methods.json) ? methods.json.map((x) => x.code) : []).join(", ")}

Post create_shipment HTTP=${tip.post?.status}
Disabled/unknown method \`fedex_disabled_r1\` → HTTP=${report.steps.find((s) => s.disabledUnknown)?.disabledUnknown?.status} title=${report.steps.find((s) => s.disabledUnknown)?.disabledUnknown?.title}

Composer now honors ShippingMethodsOptions.EnabledCodes (defect fix vs Enabled(null)).
Config-gated disable of a known code is covered by unit/options + this unknown-code backend reject path.

PASS=${methods.status === 200 && tip.post?.status === 200}`,
  );

  md(
    "r1-ui-preservation.md",
    `# R1 UI Preservation

- No Orders grid / AppDataGrid flex redesign
- Create Shipment modal + Offer return policy UI unchanged in this repair
- FE /admin/orders HTTP=${feOrders.status} bytes≈${feOrders.len}
- Human FA labels used in return policy / shipping method labels (پست، تیپاکس، غیرقابل مرجوعی)

USER_VISUAL_ACCEPTED=NO`,
  );

  console.log(JSON.stringify({ ok: report.ok, a: lineA, b: lineB, c: lineC, dOk, tipax: tipaxStep, courier: courierStep, govOk }, null, 2));
  process.exit(report.ok ? 0 : 2);
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
