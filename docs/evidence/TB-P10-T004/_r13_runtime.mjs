import { writeFileSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { randomUUID } from "node:crypto";

const HOST = process.env.TOOBA_HOST_ORIGIN || "http://127.0.0.1:5088";
const KG = "01a03826-9936-7000-b499-ff26a6123a8c";
const out = { ok: true, steps: [], at: new Date().toISOString(), host: HOST };

function note(name, data) {
  const row = { name, ...(data ?? {}) };
  out.steps.push(row);
  if (data && data.ok === false) out.ok = false;
  console.log(JSON.stringify(row));
}
function curl(args) {
  const raw = execFileSync("curl.exe", ["-sS", "-w", "\n%{http_code}", ...args], { encoding: "utf8", maxBuffer: 8e6 });
  const i = raw.lastIndexOf("\n");
  let json;
  try { json = JSON.parse(raw.slice(0, i)); } catch { json = raw.slice(0, i); }
  return { status: +raw.slice(i + 1), json, text: raw.slice(0, i) };
}
function pick(o, ...ks) {
  if (!o || typeof o !== "object") return;
  for (const k of ks) if (o[k] != null) return o[k];
}
function host(m, p, b, ex = {}) {
  const a = ["-X", m, HOST + p, "-H", "Host: alpha.localhost", "-H", "Content-Type: application/json"];
  for (const [k, v] of Object.entries(ex)) if (v != null) a.push("-H", `${k}: ${v}`);
  if (b !== undefined) a.push("--data-binary", JSON.stringify(b));
  return curl(a);
}

function buildCart(offerId, qty = 1) {
  const cart = host("POST", "/v1/storefront/cart");
  const secret = pick(cart.json, "guestSecret", "GuestSecret");
  const cartId = pick(cart.json, "cartId", "CartId");
  let version = pick(cart.json, "version", "Version");
  const guest = { "X-Tooba-Guest-Secret": secret };
  const line = host("POST", `/v1/storefront/cart/${cartId}/lines?expectedVersion=${version}`, { offerId, quantity: qty }, guest);
  if (line.status >= 400) throw new Error("add " + offerId + " " + line.text);
  version = pick(line.json, "version", "Version");
  return { cartId, secret, guest, version };
}

function ship(session) {
  const proj = host("POST", "/v1/storefront/shipping/projection", {
    cartId: session.cartId, provinceName: "تهران", methodCode: "post:express", language: "fa",
  }, session.guest);
  const minDate = pick(proj.json, "minimumDeliveryDate", "MinimumDeliveryDate");
  host("PUT", "/v1/storefront/shipping/selection", {
    cartId: session.cartId, expectedCartVersion: session.version, recipientName: "R13",
    contactMobile: "+989121240112", provinceName: "تهران", cityName: "تهران",
    postalAddress: "a", postalCode: "1234567890", shippingMethodCode: "post:express",
    selectedDeliveryDate: minDate, selectedDeliveryTimeWindow: "9-12", customerNote: "r13",
  }, session.guest);
  return host("POST", "/v1/storefront/shipping/commit", {
    cartId: session.cartId, expectedCartVersion: session.version, idempotencyKey: randomUUID(),
  }, session.guest);
}

function customerDetail(res) {
  return String(pick(res.json, "detail", "Detail") || res.text || "");
}
function errorCode(res) {
  return String(pick(res.json, "errorCode", "ErrorCode") || "");
}

try {
  const session = buildCart(KG);
  note("A-cart-created", { ok: true, cartId: session.cartId });
  const committed = ship(session);
  const checkoutId = pick(committed.json, "checkoutId", "CheckoutId");
  note("A-commit", {
    ok: committed.status === 200 && !!checkoutId,
    status: committed.status,
    checkoutId,
    sourceCartId: session.cartId,
  });

  const converted = host("GET", `/v1/storefront/cart/${session.cartId}`, undefined, session.guest);
  const convertedStatus = String(pick(converted.json, "status", "Status") || "");
  note("A-source-converted", { ok: converted.status === 200 && /converted/i.test(convertedStatus), status: converted.status, cartStatus: convertedStatus });

  const mutateDead = host("POST", `/v1/storefront/cart/${session.cartId}/lines?expectedVersion=${session.version}`, { offerId: KG, quantity: 1 }, session.guest);
  note("E-converted-mutation-rejected", { ok: mutateDead.status >= 400, status: mutateDead.status, code: errorCode(mutateDead) });

  const owned = host("GET", `/v1/storefront/checkout/${checkoutId}?cartId=${session.cartId}`, undefined, session.guest);
  note("A-payment-page-owned", { ok: owned.status === 200, status: owned.status, code: errorCode(owned) });

  const naked = host("GET", `/v1/storefront/checkout/${checkoutId}?cartId=${session.cartId}`);
  const nakedDetail = customerDetail(naked);
  note("F-id-alone-denied", {
    ok: naked.status >= 400 && !nakedDetail.includes("ثبت سفارش انجام نشد") && /access\.denied|guest\.invalid|missing/i.test(errorCode(naked) + nakedDetail),
    status: naked.status,
    code: errorCode(naked),
    detail: nakedDetail,
  });

  const fresh = host("POST", "/v1/storefront/cart");
  const freshId = pick(fresh.json, "cartId", "CartId");
  const freshSecret = pick(fresh.json, "guestSecret", "GuestSecret");
  const freshGuest = { "X-Tooba-Guest-Secret": freshSecret };
  note("A-fresh-cart", { ok: !!freshId && freshId !== session.cartId, freshId });

  const steal = host("GET", `/v1/storefront/checkout/${checkoutId}?cartId=${freshId}`, undefined, freshGuest);
  const stealDetail = customerDetail(steal);
  note("F-new-secret-denied", {
    ok: steal.status >= 400 && !stealDetail.includes("ثبت سفارش انجام نشد") && errorCode(steal) !== "checkout.rejected",
    status: steal.status,
    code: errorCode(steal),
    detail: stealDetail,
  });

  const addFresh = host("POST", `/v1/storefront/cart/${freshId}/lines?expectedVersion=${pick(fresh.json, "version", "Version")}`, { offerId: KG, quantity: 1 }, freshGuest);
  note("A-addtocart-new-active", { ok: addFresh.status === 200, status: addFresh.status });

  const pay = host("POST", `/v1/storefront/checkout/${checkoutId}/payments`, {
    cartId: session.cartId, providerCode: "gateway", idempotencyKey: randomUUID(),
  }, session.guest);
  const paymentId = pick(pay.json, "paymentId", "PaymentId");
  note("A-initiate-owned", { ok: pay.status === 200 && !!paymentId, status: pay.status, paymentId });

  const paySteal = host("POST", `/v1/storefront/checkout/${checkoutId}/payments`, {
    cartId: freshId, providerCode: "gateway", idempotencyKey: randomUUID(),
  }, freshGuest);
  note("F-initiate-new-secret-denied", {
    ok: paySteal.status >= 400 && errorCode(paySteal) !== "checkout.rejected",
    status: paySteal.status,
    code: errorCode(paySteal),
    detail: customerDetail(paySteal),
  });

  if (paymentId) {
    const result = host("GET", `/v1/storefront/payments/${paymentId}?cartId=${session.cartId}`, undefined, session.guest);
    note("A-result-owned", { ok: result.status === 200, status: result.status });
    const resultSteal = host("GET", `/v1/storefront/payments/${paymentId}?cartId=${freshId}`, undefined, freshGuest);
    note("F-result-new-secret-denied", { ok: resultSteal.status >= 400, status: resultSteal.status, code: errorCode(resultSteal) });

    const sandboxCtx = host("GET", `/v1/storefront/payments/${paymentId}/sandbox?cartId=${session.cartId}`, undefined, session.guest);
    note("C-sandbox-context", { ok: sandboxCtx.status === 200 || sandboxCtx.status === 403, status: sandboxCtx.status });
    if (sandboxCtx.status === 200) {
      const attemptId = pick(pay.json, "attemptId", "AttemptId");
      const pref = pick(pay.json, "providerRequestReference", "ProviderRequestReference");
      const done = host("POST", `/v1/storefront/payments/${paymentId}/sandbox/complete`, {
        cartId: session.cartId, attemptId, providerRequestReference: pref, outcome: "success",
      }, session.guest);
      note("C-sandbox-success", { ok: done.status === 200, status: done.status });
    }

    const manual = host("POST", `/v1/storefront/checkout/${checkoutId}/payments`, {
      cartId: session.cartId, providerCode: "manual", idempotencyKey: randomUUID(),
    }, session.guest);
    note("B-manual-initiate", { ok: manual.status === 200 || manual.status === 409, status: manual.status, code: errorCode(manual) });
  }

  const auth = host("GET", `/v1/storefront/checkout/${checkoutId}?cartId=${session.cartId}`, undefined, {
    "X-Tooba-Dev-Actor-User-Id": "01a036c2-970e-7000-8eb7-94bf5cc2d8db",
  });
  note("D-auth-without-guest-secret", { ok: auth.status === 200 || auth.status >= 400, status: auth.status, code: errorCode(auth) });

  const replay = host("POST", "/v1/storefront/shipping/commit", {
    cartId: session.cartId, expectedCartVersion: session.version, idempotencyKey: randomUUID(),
  }, session.guest);
  note("H-no-duplicate-commit", { ok: replay.status >= 400, status: replay.status, code: errorCode(replay) });
} catch (err) {
  note("runtime-exception", { ok: false, message: String(err && err.message ? err.message : err) });
}

writeFileSync(new URL("./r13-runtime-raw.json", import.meta.url), JSON.stringify(out, null, 2));
process.exit(out.ok ? 0 : 1);
