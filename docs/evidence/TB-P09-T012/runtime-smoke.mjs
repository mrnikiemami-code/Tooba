import { writeFileSync } from "node:fs";

const HOST = "http://127.0.0.1:5088";
const ACTOR = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const SINGLE = "01a07ec5-7b94-7000-8e14-9b81ae2261d1";
const MULTI = "01a07ec5-81ad-7000-b77d-4f60962d6d50";
const OFFER_QTY2 = "01a03826-a3b0-7000-8e23-b12b7e3487c2";
const log = [];
function rec(line) {
  log.push(line);
  console.log(line);
}

async function req(method, path, body, extra = {}) {
  const headers = {
    Host: "alpha.localhost",
    Accept: "application/json",
    ...extra.headers,
  };
  if (!extra.guest && !headers["X-Tooba-Dev-Actor-User-Id"]) {
    headers["X-Tooba-Dev-Actor-User-Id"] = ACTOR;
  }
  let payload;
  if (body !== undefined) {
    headers["Content-Type"] = "application/json; charset=utf-8";
    payload = JSON.stringify(body);
  }
  const res = await fetch(HOST + path, { method, headers, body: payload });
  const text = await res.text();
  let json = null;
  try { json = text ? JSON.parse(text) : null; } catch { json = null; }
  return { status: res.status, json, text };
}

function codes(ops) {
  return (ops?.actions ?? []).map((a) => a.code);
}

async function waitOps(checkoutId, pred, tries = 20) {
  let last = null;
  for (let i = 0; i < tries; i++) {
    last = await req("GET", `/v1/admin/orders/${checkoutId}/operations`);
    if (last.status === 200 && pred(last.json)) return last.json;
    await new Promise((r) => setTimeout(r, 400));
  }
  return last?.json ?? null;
}

async function createCheckout(lines, recipient, confirm) {
  const cart = (await req("POST", "/v1/storefront/cart", {}, { guest: true })).json;
  const guest = { "X-Tooba-Guest-Secret": cart.guestSecret };
  let ver = cart.version;
  for (const line of lines) {
    const add = await req("POST", `/v1/storefront/cart/${cart.cartId}/lines?expectedVersion=${ver}`, {
      offerId: line.offerId,
      quantity: line.qty,
    }, { headers: guest, guest: true });
    ver = add.json.version;
  }
  const checkout = (await req("POST", "/v1/storefront/checkout", {
    cartId: cart.cartId,
    expectedCartVersion: ver,
    idempotencyKey: crypto.randomUUID().replaceAll("-", ""),
    shipping: {
      recipientName: recipient,
      contactMobile: "09121234567",
      provinceName: "تهران",
      cityName: "تهران",
      postalAddress: "آدرس T012",
      postalCode: "1234567890",
    },
  }, { headers: guest, guest: true })).json;
  const pay = await req("POST", `/v1/storefront/checkout/${checkout.checkoutId}/payments`, {
    cartId: cart.cartId,
    idempotencyKey: crypto.randomUUID().replaceAll("-", ""),
    providerCode: "manual",
  }, { headers: guest, guest: true });
  rec(`checkout ${checkout.checkoutId} pay=${pay.status} ${pay.json?.status || pay.text?.slice(0, 180)}`);
  if (!confirm) return checkout.checkoutId;
  const ops = await waitOps(checkout.checkoutId, (p) => codes(p).includes("confirm_deposit"), 40);
  const confirmAction = (ops?.actions ?? []).find((a) => a.code === "confirm_deposit");
  if (!confirmAction) {
    rec(`WAIT_CONFIRM fail ${checkout.checkoutId} actions=${codes(ops).join(",")} raw=${JSON.stringify(ops)?.slice(0, 500)}`);
    throw new Error("no confirm_deposit");
  }
  const conf = await req("POST", `/v1/admin/orders/${checkout.checkoutId}/operations`, {
    code: "confirm_deposit",
    idempotencyKey: crypto.randomUUID().replaceAll("-", ""),
  });
  rec(`confirm ${checkout.checkoutId} => ${conf.status}`);
  await waitOps(checkout.checkoutId, (p) => codes(p).includes("mark_processing"));
  return checkout.checkoutId;
}

const health = await req("GET", "/health");
rec(`health ${health.status} ${health.text}`);

// A — waiting payment (existing multi currently pending)
const opsA = (await req("GET", `/v1/admin/orders/${MULTI}/operations`)).json;
const sellerA = opsA.sellerCapabilities ?? [];
const linesA = opsA.lineCapabilities ?? [];
const aPass =
  sellerA.every((s) => s.paymentLocked === true && s.selectionAllowed === false && s.shipmentCreationPossible === false)
  && sellerA.every((s) => String(s.infoMessageFa || "").includes("پرداخت این بخش از سفارش هنوز تأیید نشده"))
  && linesA.every((l) => l.selectable === false && (l.rowActionCodes?.length ?? 0) === 0)
  && !codes(opsA).includes("mark_processing")
  && !codes(opsA).includes("pack_selected")
  && !codes(opsA).includes("create_shipment");
rec(`A payment-lock multi=${MULTI} sellers=${sellerA.length} locked=${aPass} actions=${codes(opsA).join(",")}`);

// B — ReadyToProcess: new qty2 order confirm, no start
const readyId = await createCheckout([{ offerId: OFFER_QTY2, qty: 2 }], "خریدار T012 Ready", true);
const opsB = (await req("GET", `/v1/admin/orders/${readyId}/operations`)).json;
const detailB = (await req("GET", `/v1/admin/orders/${readyId}`)).json;
const sellerB = detailB.sellerOrders[0];
const lineB = sellerB.lines[0];
const capB = (opsB.lineCapabilities ?? []).find((c) => c.orderLineId === lineB.orderLineId);
const bPass =
  codes(opsB).includes("mark_processing")
  && !codes(opsB).includes("pack_selected")
  && capB?.rowActionCodes?.includes("mark_processing")
  && !capB?.rowActionCodes?.includes("pack_selected")
  && (capB?.shipmentEligibleQuantity ?? 1) === 0;
const packBefore = await req("POST", `/v1/admin/orders/${readyId}/operations`, {
  code: "pack_selected",
  sellerOrderId: sellerB.sellerOrderId,
  fulfillmentId: sellerB.fulfillmentId,
  selections: [{ orderLineId: lineB.orderLineId, quantity: 1 }],
});
rec(`B ready ${readyId} cap=${JSON.stringify(capB?.rowActionCodes)} packBefore=${packBefore.status} ${packBefore.json?.errorCode || packBefore.json?.title || ""} pass=${bPass && packBefore.status === 400}`);

// C — StartProcessing then pack only that line
const start = await req("POST", `/v1/admin/orders/${readyId}/operations`, {
  code: "mark_processing",
  sellerOrderId: sellerB.sellerOrderId,
  fulfillmentId: sellerB.fulfillmentId,
});
const opsC = await waitOps(readyId, (p) => codes(p).includes("pack_selected"));
const capC = (opsC.lineCapabilities ?? []).find((c) => c.orderLineId === lineB.orderLineId);
const packOne = await req("POST", `/v1/admin/orders/${readyId}/operations`, {
  code: "pack_selected",
  sellerOrderId: sellerB.sellerOrderId,
  fulfillmentId: sellerB.fulfillmentId,
  selections: [{ orderLineId: lineB.orderLineId, quantity: 1 }],
});
const afterPack = (await req("GET", `/v1/admin/orders/${readyId}`)).json;
const lineAfter = afterPack.sellerOrders[0].lines[0];
const sellerAfter = afterPack.sellerOrders[0];
rec(`C start=${start.status} pack=${packOne.status} packed=${lineAfter.quantityPacked}/${lineAfter.quantity} sellerFulfill=${sellerAfter.fulfillmentStatus} row=${JSON.stringify(capC?.rowActionCodes)}`);

// D — qty 1 of 2 => seller not fully Packed
const dPass = lineAfter.quantityPacked === 1 && lineAfter.quantity === 2 && sellerAfter.fulfillmentStatus !== "Packed";
rec(`D partial-pack seller-not-Packed=${dPass} fulfill=${sellerAfter.fulfillmentStatus}`);

// E — mixed Processing+Packed on existing single
const opsE = (await req("GET", `/v1/admin/orders/${SINGLE}/operations`)).json;
const detailE = (await req("GET", `/v1/admin/orders/${SINGLE}`)).json;
const soE = detailE.sellerOrders[0];
const packedLine = soE.lines.find((l) => (l.quantityPacked ?? 0) > 0);
const processLine = soE.lines.find((l) => (l.quantityPacked ?? 0) === 0);
const capPacked = (opsE.lineCapabilities ?? []).find((c) => c.orderLineId === packedLine.orderLineId);
const capProc = (opsE.lineCapabilities ?? []).find((c) => c.orderLineId === processLine.orderLineId);
const shared = (capPacked.bulkActionCodes || []).filter((c) => (capProc.bulkActionCodes || []).includes(c));
const mixedPost = await req("POST", `/v1/admin/orders/${SINGLE}/operations`, {
  code: "pack_selected",
  sellerOrderId: soE.sellerOrderId,
  fulfillmentId: soE.fulfillmentId,
  selections: [
    { orderLineId: packedLine.orderLineId, quantity: 1 },
    { orderLineId: processLine.orderLineId, quantity: 1 },
  ],
});
rec(`E mixed shared=[${shared.join(",")}] packedRow=${capPacked.rowActionCodes} procRow=${capProc.rowActionCodes} bulkPost=${mixedPost.status} ${mixedPost.json?.errorCode || ""}`);

// F — shipment from exact packed eligible qty on the T012 ready order (packed 1/2)
const opsF0 = (await req("GET", `/v1/admin/orders/${readyId}/operations`)).json;
const detailF0 = (await req("GET", `/v1/admin/orders/${readyId}`)).json;
const soF = detailF0.sellerOrders[0];
const lineF = soF.lines[0];
const capF0 = (opsF0.lineCapabilities ?? []).find((c) => c.orderLineId === lineF.orderLineId);
const shipQty = capF0?.shipmentEligibleQuantity ?? 0;
const ship = await req("POST", `/v1/admin/orders/${readyId}/operations`, {
  code: "create_shipment",
  sellerOrderId: soF.sellerOrderId,
  fulfillmentId: soF.fulfillmentId,
  shippingMethodCode: "post",
  carrierDisplayName: "پست",
  providerMetadataJson: JSON.stringify({
    recipientName: detailF0.recipientName || "خریدار T012 Ready",
    recipientPhone: "09121234567",
    postalCode: "1234567890",
    destinationAddress: "تهران، آدرس T012",
    serviceType: "پیشتاز",
    packageCount: 1,
    weightKg: 1,
  }),
  selections: shipQty > 0 ? [{ orderLineId: lineF.orderLineId, quantity: shipQty }] : null,
});
const afterShip = (await req("GET", `/v1/admin/orders/${readyId}`)).json;
const shipments = afterShip.sellerOrders[0].shipments ?? [];
const opsF = (await req("GET", `/v1/admin/orders/${readyId}/operations`)).json;
const capF = (opsF.lineCapabilities ?? []).find((c) => c.orderLineId === lineF.orderLineId);
rec(`F ship=${ship.status} ${ship.json?.errorCode || ship.json?.title || "ok"} qty=${shipQty} shipments=${shipments.length} eligibleAfter=${capF?.shipmentEligibleQuantity} createPossible=${opsF.sellerCapabilities?.[0]?.shipmentCreationPossible}`);

// G — cancel a fresh pending order
const cancelId = await createCheckout([{ offerId: OFFER_QTY2, qty: 1 }], "خریدار T012 Cancel", false);
const cancel = await req("POST", `/v1/admin/orders/${cancelId}/operations`, { code: "cancel" });
const opsG = (await req("GET", `/v1/admin/orders/${cancelId}/operations`)).json;
const query = await req("POST", "/v1/admin/orders/query", { page: 1, pageSize: 50 });
const rowG = (query.json?.items ?? query.json?.rows ?? query.json ?? []).find?.((r) =>
  (r.checkoutId || r.id) === cancelId,
);
const blocked = [];
for (const code of ["confirm_deposit", "reject_deposit", "mark_processing"]) {
  const r = await req("POST", `/v1/admin/orders/${cancelId}/operations`, { code });
  blocked.push(`${code}:${r.status}:${r.json?.errorCode || r.json?.title || ""}`);
}
const gActions = codes(opsG);
const gPass =
  !gActions.includes("confirm_deposit")
  && !gActions.includes("reject_deposit")
  && !gActions.includes("mark_processing")
  && gActions.includes("restore_cancelled_order");
rec(`G cancel=${cancel.status} ${cancelId} actions=${gActions.join(",")} blocked=${blocked.join(" | ")} restore=${gPass} queryStatus=${rowG?.status || rowG?.paymentState || "n/a"}`);

const summary = {
  A: aPass,
  B: bPass && packBefore.status === 400,
  C: start.status === 200 && packOne.status === 200 && lineAfter.quantityPacked === 1,
  D: dPass,
  E: shared.length === 0 && mixedPost.status === 400,
  F: ship.status === 200 && shipments.length >= 1,
  G: cancel.status === 200 && gPass,
  readyId,
  cancelId,
  single: SINGLE,
  multi: MULTI,
};
rec(`SUMMARY ${JSON.stringify(summary)}`);
writeFileSync(new URL("./runtime-smoke.log", import.meta.url), log.join("\n") + "\n", "utf8");
if (!Object.values(summary).every((v) => v === true || typeof v === "string")) {
  const failed = Object.entries(summary).filter(([, v]) => v === false);
  if (failed.length) process.exitCode = 1;
}
