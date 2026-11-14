/**
 * TB-266 — create the FOUR coherent trial orders through the PUBLIC owning paths only.
 *
 * No SQL insert of Paid, no direct factory call, no fake reader: every order goes through the storefront
 * cart -> checkout -> payment initiation -> sandbox completion (the repository's own Development payment
 * gateway) -> payment.succeeded -> Fulfillment creation. Physical work then goes through the owning seller
 * and admin operation commands. Rows are labelled TB266-.
 *
 * TB-266 correction: the unpaid scenario deliberately initiates NO payment attempt. The Development `fake`
 * gateway answers every Verify with success, and the Payment-owned reconciliation worker therefore settles any
 * pending fake payment on its next cycle, which would silently turn the "unpaid" fixture into a Paid order
 * (observed 2026-11-14 15:23). An order that never asks for money stays PendingPayment for real, so the gate is
 * proven against a genuinely unpaid order rather than a re-written status.
 */
import fs from "node:fs";
import path from "node:path";

const host = (process.env.TOOBA_HOST || "http://127.0.0.1:5088").replace(/\/$/, "");
const outDir = path.resolve("docs/architecture/evidence/TB-SHIPPING-PAYMENT-GATED-DISPATCH-READABLE-QUEUE-AND-CLEAN-TRIAL-DATA-266");
const outJson = process.env.TB266_OUTFILE ? path.resolve(process.env.TB266_OUTFILE) : path.join(outDir, "trial-fixtures.json");
const requested = new Set(process.argv.slice(2).filter((x) => !x.startsWith("-")));
const wants = (key) => requested.size === 0 || requested.has(key);

const adminActor = process.env.TOOBA_ADMIN_ACTOR || "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const sellerPartyId = process.env.TOOBA_SELLER_PARTY_ID || "01a030d1-40cb-7000-8abe-6d31739956c5";
const sellerActor = process.env.TOOBA_SELLER_ACTOR || "01a03628-3f68-7000-844d-99f1cadb54b0";

const results = { createdAt: new Date().toISOString(), host, scenarios: [] };

async function call(url, init = {}, label = "") {
  const res = await fetch(url, init);
  const text = await res.text();
  let body = null;
  try {
    body = text ? JSON.parse(text) : null;
  } catch {
    body = text;
  }
  if (!res.ok) {
    throw new Error(`${label || url} -> ${res.status} ${typeof body === "string" ? body : JSON.stringify(body)}`);
  }
  return body;
}

const pick = (obj, ...keys) => {
  for (const key of keys) if (obj && obj[key] != null) return obj[key];
  return null;
};

const trialCustomer = {
  identifier: process.env.TOOBA_TRIAL_CUSTOMER || "tb266.customer.1@example.test",
  password: process.env.TOOBA_TRIAL_PASSWORD || "Tb266-demo-pass-1",
};

const adminHeaders = { "content-type": "application/json", "X-Tooba-Dev-Actor-User-Id": adminActor };

/**
 * Checkout is AuthenticatedOnly in this store, so the trial orders are created by a real customer session
 * obtained through the public identity endpoints (no dev impersonation header is trusted for checkout).
 */
async function customerSession(suffix) {
  const identifier = `${trialCustomer.identifier.split("@")[0]}.${suffix}@example.test`;
  const payload = { identifierKind: "Email", identifier, password: trialCustomer.password };
  let login;
  try {
    login = await call(`${host}/v1/auth/login`, {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: JSON.stringify(payload),
    }, "customer login");
  } catch {
    await call(`${host}/v1/auth/register`, {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: JSON.stringify(payload),
    }, "customer register");
    login = await call(`${host}/v1/auth/login`, {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: JSON.stringify(payload),
    }, "customer login after register");
  }
  const token = pick(login, "accessToken", "AccessToken");
  if (!token) throw new Error("customer session token missing");
  return {
    userId: pick(login, "userId", "UserId"),
    authHeaders: { authorization: `Bearer ${token}`, "content-type": "application/json" },
  };
}
const sellerHeaders = {
  "content-type": "application/json",
  "X-Tooba-Seller-Party-Id": sellerPartyId,
  "X-Tooba-Dev-Actor-User-Id": sellerActor,
};

async function offerWithStock() {
  if (process.env.TOOBA_OFFER_ID) return process.env.TOOBA_OFFER_ID;
  const offers = await call(`${host}/v1/seller/offers`, { headers: sellerHeaders }, "seller offers");
  const rows = Array.isArray(offers) ? offers : [];
  const hit = rows.find((o) => Number(pick(o, "availableUnits", "AvailableUnits") ?? 0) >= 20);
  if (!hit) throw new Error("no in-stock seller offer");
  return { offerId: pick(hit, "offerId", "OfferId"), title: pick(hit, "productTitle", "ProductTitle") };
}

async function checkout(quantity, label, session) {
  const created = await call(`${host}/v1/storefront/cart`, { method: "POST", headers: session.authHeaders }, "cart create");
  const cartId = pick(created, "cartId", "CartId");
  const guestSecret = pick(created, "guestSecret", "GuestSecret");
  let version = pick(created, "version", "Version");
  const headers = {
    ...session.authHeaders,
    "X-Tooba-Guest-Secret": guestSecret,
    "X-Tooba-Cart-Version": String(version),
  };
  const offer = await offerWithStock();
  const withLine = await call(
    `${host}/v1/storefront/cart/${cartId}/lines?expectedVersion=${version}`,
    { method: "POST", headers, body: JSON.stringify({ offerId: offer.offerId, quantity }) },
    "cart line"
  );
  version = pick(withLine, "version", "Version");
  headers["X-Tooba-Cart-Version"] = String(version);

  // The owning shipping projection is what supplies a real, feasible customer date for this method.
  const methodCode = process.env.TOOBA_METHOD_CODE || "post:standard";
  const projection = await call(
    `${host}/v1/storefront/shipping/projection`,
    {
      method: "POST",
      headers,
      body: JSON.stringify({ cartId, provinceName: "تهران", cityName: "تهران", language: "fa", methodCode }),
    },
    `shipping projection ${label}`
  );
  const dates = pick(projection, "deliveryDates", "DeliveryDates") || [];
  if (dates.length === 0) throw new Error(`no feasible delivery date for ${label}`);
  const date = pick(dates[0], "date", "Date", "value", "Value");
  const windows = pick(projection, "deliveryTimeWindows", "DeliveryTimeWindows") || [];
  const windowValue = pick(windows[0] || {}, "value", "Value");
  const shipping = {
    recipientName: `خریدار TB266 ${label}`,
    contactMobile: "09121234567",
    provinceName: "تهران",
    cityName: "تهران",
    postalAddress: "خیابان آزادی، پلاک ۱",
    postalCode: "1234567890",
    firstName: "خریدار",
    lastName: "TB266",
    shippingMethodCode: methodCode,
    selectedDeliveryDate: date,
    selectedDeliveryTimeWindow: windowValue,
  };

  // Persist the reviewed selection through the owning draft command before the order is committed.
  const selection = await call(
    `${host}/v1/storefront/shipping/selection?expectedVersion=${version}`,
    { method: "PUT", headers, body: JSON.stringify({ cartId, expectedCartVersion: version, ...shipping }) },
    `shipping selection ${label}`
  );
  version = Number(pick(selection, "cartVersion", "CartVersion", "version", "Version") || version);
  headers["X-Tooba-Cart-Version"] = String(version);

  const body = await call(
    `${host}/v1/storefront/checkout`,
    {
      method: "POST",
      headers,
      body: JSON.stringify({
        cartId,
        expectedCartVersion: version,
        idempotencyKey: `TB266-${label}-${Date.now()}`,
        shipping,
      }),
    },
    `checkout ${label}`
  );
  return {
    cartId,
    guestSecret,
    headers,
    checkoutId: pick(body, "checkoutId", "CheckoutId"),
    offerId: offer.offerId,
    offerTitle: offer.title,
    quantity,
    methodCode,
    deliveryDate: date,
    deliveryWindow: windowValue,
    checkout: body,
  };
}

async function initiatePayment(ctx, keySuffix) {
  const init = await call(
    `${host}/v1/storefront/checkout/${ctx.checkoutId}/payments`,
    {
      method: "POST",
      headers: ctx.headers,
      body: JSON.stringify({ cartId: ctx.cartId, idempotencyKey: `TB266-PAY-${keySuffix}-${Date.now()}` }),
    },
    `payment initiate ${keySuffix}`
  );
  return {
    paymentId: pick(init, "paymentId", "PaymentId"),
    attemptId: pick(init, "attemptId", "AttemptId"),
    providerRequestReference: pick(init, "providerRequestReference", "ProviderRequestReference"),
    raw: init,
  };
}

async function completeSandbox(ctx, pay) {
  await call(
    `${host}/v1/storefront/payments/${pay.paymentId}/sandbox/complete`,
    {
      method: "POST",
      headers: ctx.headers,
      body: JSON.stringify({
        cartId: ctx.cartId,
        attemptId: pay.attemptId,
        providerRequestReference: pay.providerRequestReference,
        outcome: "success",
      }),
    },
    "sandbox complete"
  );
  for (let i = 0; i < 40; i += 1) {
    await new Promise((r) => setTimeout(r, 500));
    const confirmation = await call(
      `${host}/v1/storefront/checkout/${ctx.checkoutId}?cartId=${encodeURIComponent(ctx.cartId)}`,
      { headers: ctx.headers },
      "checkout confirmation"
    );
    if (String(pick(confirmation, "paymentState", "PaymentState")).toLowerCase() === "paid") {
      return confirmation;
    }
  }
  throw new Error(`checkout ${ctx.checkoutId} did not become paid`);
}

async function sellerFulfillment(ctx) {
  for (let i = 0; i < 40; i += 1) {
    await new Promise((r) => setTimeout(r, 500));
    const list = await call(`${host}/v1/seller/fulfillments`, { headers: sellerHeaders }, "seller fulfillments");
    const rows = Array.isArray(list) ? list : [];
    const hit = rows.find((x) => pick(x, "checkoutId", "CheckoutId") === ctx.checkoutId);
    if (hit) return hit;
  }
  throw new Error(`fulfillment not created for ${ctx.checkoutId}`);
}

async function adminOperation(checkoutId, body, label) {
  return call(
    `${host}/v1/admin/orders/${checkoutId}/operations`,
    { method: "POST", headers: adminHeaders, body: JSON.stringify(body) },
    `admin ${label}`
  );
}

async function adminOperations(checkoutId) {
  return call(`${host}/v1/admin/orders/${checkoutId}/operations`, { headers: adminHeaders }, "admin operations read");
}

async function prepareAndShip(ctx, fulfillment, label) {
  const fulfillmentId = pick(fulfillment, "fulfillmentId", "FulfillmentId");
  const items = pick(fulfillment, "items", "Items") || [];
  const first = items[0];
  const orderLineId = pick(first, "orderLineId", "OrderLineId");
  const qty = Number(pick(first, "quantityOrdered", "QuantityOrdered") || ctx.quantity);

  await adminOperation(ctx.checkoutId, { code: "mark_processing", fulfillmentId }, `${label} processing`);
  await adminOperation(ctx.checkoutId, { code: "mark_packed", fulfillmentId }, `${label} packed`);
  const shipment = await adminOperation(
    ctx.checkoutId,
    { code: "create_shipment", fulfillmentId, items: [{ orderLineId, quantity: qty }] },
    `${label} shipment`
  );
  const shipments = pick(shipment, "shipments", "Shipments") || [];
  const shipmentId = pick(shipments[shipments.length - 1] || {}, "shipmentId", "ShipmentId");
  await adminOperation(
    ctx.checkoutId,
    { code: "assign_tracking", fulfillmentId, shipmentId, trackingReference: `TB266-TRK-${label}-${Date.now()}` },
    `${label} tracking`
  );
  return { fulfillmentId, orderLineId, shipmentId, quantity: qty };
}

async function main() {
  fs.mkdirSync(outDir, { recursive: true });
  const sessions = {};
  for (const key of ["unpaid", "ready", "resend", "damaged"]) {
    sessions[key] = await customerSession(key);
  }
  results.customers = Object.fromEntries(Object.entries(sessions).map(([k, v]) => [k, v.userId]));

  // 1 — unpaid awaiting payment: NO physical work may be authorized and NO payment attempt is made, so the
  // Payment-owned reconciliation worker cannot settle it (fake gateway Verify always succeeds).
  if (wants("unpaid")) {
    const ctx = await checkout(1, "UNPAID", sessions.unpaid);
    const ops = await adminOperations(ctx.checkoutId);
    const actions = (pick(ops, "actions", "Actions") || []).map((a) => pick(a, "code", "Code"));
    const logistics = (pick(ops, "fulfillments", "Fulfillments") || []).map((f) => pick(f, "fulfillmentId", "FulfillmentId"));
    // Positive proof of the new gate: the physical preparation command is refused for an unpaid order.
    let refusal = null;
    if (logistics.length > 0) {
      try {
        await adminOperation(ctx.checkoutId, { code: "mark_processing", fulfillmentId: logistics[0] }, "unpaid processing");
        refusal = "UNEXPECTEDLY_ALLOWED";
      } catch (error) {
        refusal = String(error.message || error).slice(0, 400);
      }
    }
    results.scenarios.push({
      key: "unpaid-awaiting-payment",
      checkoutId: ctx.checkoutId,
      paymentId: null,
      paymentAttempted: false,
      shipmentId: null,
      label: "سفارش ۱ — در انتظار پرداخت (بدون مجوز آماده‌سازی/ارسال)",
      projectedActions: actions,
      physicalPreparationRefusal: refusal,
      fulfillmentIds: logistics,
      reviewUrl: `http://127.0.0.1:3000/fa/admin/orders/${ctx.checkoutId}/operations`,
    });
    console.log("UNPAID", ctx.checkoutId, "actions", actions.join(","));
  }

  // 2 — lawfully paid ready order: paid via the owning sandbox completion, fulfillment created, still unpacked.
  if (wants("ready")) {
    const ctx = await checkout(1, "READY", sessions.ready);
    const pay = await initiatePayment(ctx, "ready");
    const confirmation = await completeSandbox(ctx, pay);
    const fulfillment = await sellerFulfillment(ctx);
    results.scenarios.push({
      key: "paid-ready-for-preparation",
      checkoutId: ctx.checkoutId,
      paymentId: pay.paymentId,
      fulfillmentId: pick(fulfillment, "fulfillmentId", "FulfillmentId"),
      paymentState: pick(confirmation, "paymentState", "PaymentState"),
      label: "سفارش ۲ — پرداخت‌شده و آمادهٔ آماده‌سازی",
      reviewUrl: `http://127.0.0.1:3000/fa/admin/orders/${ctx.checkoutId}/operations`,
    });
    console.log("READY", ctx.checkoutId, "fulfillment", pick(fulfillment, "fulfillmentId", "FulfillmentId"));
  }

  // 3 — lawfully paid and actually being prepared (owning mark_processing accepted by the new payment gate).
  if (wants("processing")) {
    const ctx = await checkout(1, "PROCESSING", sessions.resend);
    const pay = await initiatePayment(ctx, "processing");
    await completeSandbox(ctx, pay);
    const fulfillment = await sellerFulfillment(ctx);
    const fulfillmentId = pick(fulfillment, "fulfillmentId", "FulfillmentId");
    await adminOperation(ctx.checkoutId, { code: "mark_processing", fulfillmentId }, "processing mark_processing");
    results.scenarios.push({
      key: "paid-processing",
      checkoutId: ctx.checkoutId,
      paymentId: pay.paymentId,
      fulfillmentId,
      label: "سفارش ۳ — پرداخت‌شده و در حال آماده‌سازی",
      reviewUrl: `http://127.0.0.1:3000/fa/admin/orders/${ctx.checkoutId}/operations`,
    });
    console.log("PROCESSING", ctx.checkoutId, "fulfillment", fulfillmentId);
  }

  // 4 — lawfully paid and packed (the last physical step available before a parcel needs accepted group evidence).
  if (wants("packed")) {
    const ctx = await checkout(1, "PACKED", sessions.damaged);
    const pay = await initiatePayment(ctx, "packed");
    await completeSandbox(ctx, pay);
    const fulfillment = await sellerFulfillment(ctx);
    const fulfillmentId = pick(fulfillment, "fulfillmentId", "FulfillmentId");
    await adminOperation(ctx.checkoutId, { code: "mark_processing", fulfillmentId }, "packed mark_processing");
    await adminOperation(ctx.checkoutId, { code: "mark_packed", fulfillmentId }, "packed mark_packed");
    results.scenarios.push({
      key: "paid-packed",
      checkoutId: ctx.checkoutId,
      paymentId: pay.paymentId,
      fulfillmentId,
      label: "سفارش ۴ — پرداخت‌شده و بسته‌بندی‌شده",
      reviewUrl: `http://127.0.0.1:3000/fa/admin/orders/${ctx.checkoutId}/operations`,
    });
    console.log("PACKED", ctx.checkoutId, "fulfillment", fulfillmentId);
  }
  fs.writeFileSync(outJson, JSON.stringify(results, null, 2));
  console.log("TB266_TRIAL_READY", outJson);
}

main().catch((error) => {
  console.error("TB266_TRIAL_FAIL", error.message);
  process.exit(1);
});


