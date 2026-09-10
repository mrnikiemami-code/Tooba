/**
 * TB-P10-T003 — payment runtime matrix (Host :5088, FE :3000).
 */
import { writeFileSync } from "node:fs";
import { randomUUID } from "node:crypto";

const BASE = "http://127.0.0.1:5088";
const FE = "http://127.0.0.1:3000";
const OFFER = "01a03826-9936-7000-b499-ff26a6123a8c";

const headers = {
  Host: "alpha.localhost",
  "Content-Type": "application/json",
};

async function req(method, path, body, extra = {}, base = BASE) {
  const all = { ...headers, ...extra };
  const r = await fetch(`${base}${path}`, {
    method,
    headers: all,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });
  const text = await r.text();
  let json;
  try {
    json = JSON.parse(text);
  } catch {
    json = text;
  }
  return { status: r.status, json, text };
}

function pick(obj, ...keys) {
  if (!obj || typeof obj !== "object") return undefined;
  for (const key of keys) if (obj[key] != null) return obj[key];
  return undefined;
}

function errCode(res) {
  const j = res.json;
  if (!j || typeof j !== "object") return "";
  return String(pick(j, "errorCode", "ErrorCode") ?? "");
}

const out = { ok: true, steps: [] };
function note(name, data) {
  const row = { name, ...(data ?? {}) };
  out.steps.push(row);
  if (data && data.ok === false) out.ok = false;
  console.log(JSON.stringify(row));
}

async function shippingCommit() {
  const cart = await req("POST", "/v1/storefront/cart");
  const secret = pick(cart.json, "guestSecret", "GuestSecret");
  const guest = { "X-Tooba-Guest-Secret": secret };
  const cartId = pick(cart.json, "cartId", "CartId");
  const line = await req(
    "POST",
    `/v1/storefront/cart/${cartId}/lines?expectedVersion=${pick(cart.json, "version", "Version")}`,
    { offerId: OFFER, quantity: 1 },
    guest,
  );
  const version = pick(line.json, "version", "Version");
  const projection = await req(
    "POST",
    "/v1/storefront/shipping/projection",
    { cartId, provinceName: "تهران", methodCode: "post:express", language: "fa" },
    guest,
  );
  const methods = pick(projection.json, "methods", "Methods") || [];
  const method =
    methods.find((m) => pick(m, "methodCode", "MethodCode") === "post:express") || methods[0];
  const methodCode = pick(method, "methodCode", "MethodCode");
  const minDate = pick(projection.json, "minimumDeliveryDate", "MinimumDeliveryDate");
  const save = await req(
    "PUT",
    "/v1/storefront/shipping/selection",
    {
      cartId,
      expectedCartVersion: version,
      recipientName: "خریدار تی۰۰۳",
      contactMobile: "+989121230003",
      provinceName: "تهران",
      cityName: "تهران",
      postalAddress: "خیابان تست پرداخت ۳",
      postalCode: "1234567890",
      shippingMethodCode: methodCode,
      selectedDeliveryDate: minDate,
      selectedDeliveryTimeWindow: "9-12",
      customerNote: "t003-runtime",
    },
    guest,
  );
  const commit = await req(
    "POST",
    "/v1/storefront/shipping/commit",
    { cartId, expectedCartVersion: version, idempotencyKey: randomUUID() },
    guest,
  );
  return {
    cartId,
    secret,
    guest,
    checkoutId: pick(commit.json, "checkoutId", "CheckoutId"),
    payable: pick(commit.json, "payableAmount", "PayableAmount"),
    methodCode,
    saveStatus: save.status,
    commitStatus: commit.status,
    commitBody: commit.json,
    projectionStatus: projection.status,
    lineStatus: line.status,
  };
}

async function main() {
  const methods = await req("GET", "/v1/storefront/payment-methods");
  const codes = (pick(methods.json, "methods", "Methods") || []).map((m) =>
    String(pick(m, "code", "Code") || "").toLowerCase(),
  );
  note("A-method-projection", {
    ok: methods.status === 200 && (codes.includes("gateway") || codes.includes("manual")),
    status: methods.status,
    codes,
  });

  const ship = await shippingCommit();
  note("shipping-commit", {
    ok: ship.commitStatus === 200 && !!ship.checkoutId,
    checkoutId: ship.checkoutId,
    payable: ship.payable,
    saveStatus: ship.saveStatus,
    commitStatus: ship.commitStatus,
    lineStatus: ship.lineStatus,
    projectionStatus: ship.projectionStatus,
  });
  if (!ship.checkoutId) {
    writeFileSync("docs/evidence/TB-P10-T003/runtime-smoke-raw.json", JSON.stringify(out, null, 2));
    process.exit(1);
  }

  const fePayment = await req(
    "GET",
    `/fa/payment?checkoutId=${encodeURIComponent(ship.checkoutId)}`,
    undefined,
    {},
    FE,
  );
  const html = typeof fePayment.text === "string" ? fePayment.text : "";
  note("A-fe-payment", {
    ok: fePayment.status === 200 && !/CVV|cardNumber|expiryMonth/i.test(html),
    status: fePayment.status,
    hasCvv: /CVV/i.test(html),
  });

  const checkout = await req(
    "GET",
    `/v1/storefront/checkout/${ship.checkoutId}?cartId=${ship.cartId}`,
    undefined,
    ship.guest,
  );
  const payable = pick(checkout.json, "payableAmount", "PayableAmount");
  note("pricing-integrity", {
    ok: checkout.status === 200 && Number(payable) > 0,
    payable,
  });

  const manual1 = await req(
    "POST",
    `/v1/storefront/checkout/${ship.checkoutId}/payments`,
    { cartId: ship.cartId, idempotencyKey: `t003-manual-${ship.checkoutId}`, providerCode: "manual" },
    ship.guest,
  );
  const paymentId = pick(manual1.json, "paymentId", "PaymentId");
  const amount = pick(manual1.json, "amount", "Amount");
  note("B-manual", {
    ok:
      manual1.status === 200 &&
      String(pick(manual1.json, "providerCode", "ProviderCode")).toLowerCase() === "manual" &&
      Number(amount) === Number(payable),
    status: manual1.status,
    paymentId,
    amount,
    payable,
    provider: pick(manual1.json, "providerCode", "ProviderCode"),
  });

  const manual2 = await req(
    "POST",
    `/v1/storefront/checkout/${ship.checkoutId}/payments`,
    { cartId: ship.cartId, idempotencyKey: `t003-manual-${ship.checkoutId}`, providerCode: "manual" },
    ship.guest,
  );
  note("D-idempotency", {
    ok:
      manual2.status === 200 &&
      String(pick(manual2.json, "paymentId", "PaymentId")) === String(paymentId),
    status: manual2.status,
    paymentId2: pick(manual2.json, "paymentId", "PaymentId"),
  });

  const shipG = await shippingCommit();
  const gateway = await req(
    "POST",
    `/v1/storefront/checkout/${shipG.checkoutId}/payments`,
    { cartId: shipG.cartId, idempotencyKey: `t003-gw-${shipG.checkoutId}`, providerCode: "gateway" },
    shipG.guest,
  );
  const redirect = String(pick(gateway.json, "redirectUrl", "RedirectUrl") || "");
  note("C-gateway-sandbox", {
    ok: !codes.includes("gateway")
      ? true
      : gateway.status === 200 && redirect.length > 0,
    status: gateway.status,
    redirect,
    applicable: codes.includes("gateway"),
    na: !codes.includes("gateway"),
  });

  const foreign = await req(
    "GET",
    `/v1/storefront/checkout/${ship.checkoutId}?cartId=${ship.cartId}`,
    undefined,
    { "X-Tooba-Guest-Secret": "wrong-secret" },
  );
  note("E-wrong-guest", {
    ok: foreign.status === 401 || foreign.status === 404 || foreign.status === 400,
    status: foreign.status,
    code: errCode(foreign),
  });

  const methods2 = await req("GET", "/v1/storefront/payment-methods");
  note("G-refresh-methods", {
    ok: methods2.status === 200 && (pick(methods2.json, "methods", "Methods") || []).length > 0,
    status: methods2.status,
  });

  const shipF = await shippingCommit();
  const checkoutF = await req(
    "GET",
    `/v1/storefront/checkout/${shipF.checkoutId}?cartId=${shipF.cartId}`,
    undefined,
    shipF.guest,
  );
  const payableF = pick(checkoutF.json, "payableAmount", "PayableAmount");
  const spoof = await req(
    "POST",
    `/v1/storefront/checkout/${shipF.checkoutId}/payments`,
    {
      cartId: shipF.cartId,
      idempotencyKey: `t003-spoof-${shipF.checkoutId}`,
      providerCode: "manual",
      amount: 1,
    },
    shipF.guest,
  );
  note("F-amount-not-trusted", {
    ok:
      spoof.status === 200 &&
      Number(pick(spoof.json, "amount", "Amount")) === Number(payableF),
    status: spoof.status,
    amount: pick(spoof.json, "amount", "Amount"),
    payableF,
  });

  writeFileSync("docs/evidence/TB-P10-T003/runtime-smoke-raw.json", JSON.stringify(out, null, 2));
  if (!out.ok) process.exit(1);
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
