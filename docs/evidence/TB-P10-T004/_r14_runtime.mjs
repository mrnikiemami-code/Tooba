import { writeFileSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { randomUUID } from "node:crypto";

const HOST = process.env.TOOBA_HOST_ORIGIN || "http://127.0.0.1:5099";
const KG = "01a03826-9936-7000-b499-ff26a6123a8c";
const ADMIN = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
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
function admin() { return { "X-Tooba-Dev-Actor-User-Id": ADMIN }; }
function sql(q) {
  return execFileSync("docker", ["exec", "-i", "postgres-db", "psql", "-U", "admin", "-d", "tooba_alpha", "-t", "-A", "-c", q], { encoding: "utf8" }).trim();
}
function sleep(ms) {
  execFileSync("powershell", ["-NoProfile", "-Command", `Start-Sleep -Milliseconds ${ms}`], { encoding: "utf8" });
}
function waitExpired(paymentId, ms = 45000) {
  const start = Date.now();
  while (Date.now() - start < ms) {
    const st = sql(`SELECT status FROM payment.payments WHERE payment_id='${paymentId}'`);
    if (String(st).toLowerCase() === "expired") return st;
    sleep(2000);
  }
  return sql(`SELECT status FROM payment.payments WHERE payment_id='${paymentId}'`);
}
function waitPaid(checkoutId, paymentId, ms = 45000) {
  const start = Date.now();
  while (Date.now() - start < ms) {
    const st = sql(`SELECT status FROM payment.payments WHERE payment_id='${paymentId}'`);
    if (String(st).toLowerCase() === "succeeded") return st;
    sleep(2000);
  }
  return sql(`SELECT status FROM payment.payments WHERE payment_id='${paymentId}'`);
}
function payCount(checkoutId) {
  return Number(sql(`SELECT COUNT(*) FROM payment.payments WHERE checkout_id='${checkoutId}'`));
}
function attemptCount(checkoutId) {
  return Number(sql(`SELECT COUNT(*) FROM payment.attempts a JOIN payment.payments p ON p.payment_id=a.payment_id WHERE p.checkout_id='${checkoutId}'`));
}
function evidenceCount(checkoutId) {
  return Number(sql(`SELECT COUNT(*) FROM payment.attempts a JOIN payment.payments p ON p.payment_id=a.payment_id WHERE p.checkout_id='${checkoutId}' AND a.customer_transfer_reference IS NOT NULL`));
}
function code(res) { return String(pick(res.json, "errorCode", "ErrorCode") || ""); }
function buildCart() {
  const cart = host("POST", "/v1/storefront/cart");
  const secret = pick(cart.json, "guestSecret", "GuestSecret");
  const cartId = pick(cart.json, "cartId", "CartId");
  let version = pick(cart.json, "version", "Version");
  const guest = { "X-Tooba-Guest-Secret": secret };
  const line = host("POST", `/v1/storefront/cart/${cartId}/lines?expectedVersion=${version}`, { offerId: KG, quantity: 1 }, guest);
  if (line.status >= 400) throw new Error("add " + line.text);
  return { cartId, secret, guest, version: pick(line.json, "version", "Version") };
}
function ship(session, noteText = "r14") {
  const proj = host("POST", "/v1/storefront/shipping/projection", {
    cartId: session.cartId, provinceName: "تهران", methodCode: "post:express", language: "fa",
  }, session.guest);
  const minDate = pick(proj.json, "minimumDeliveryDate", "MinimumDeliveryDate");
  host("PUT", "/v1/storefront/shipping/selection", {
    cartId: session.cartId, expectedCartVersion: session.version, recipientName: "R14",
    contactMobile: "+989121240112", provinceName: "تهران", cityName: "تهران",
    postalAddress: "a", postalCode: "1234567890", shippingMethodCode: "post:express",
    selectedDeliveryDate: minDate, selectedDeliveryTimeWindow: "9-12", customerNote: noteText,
  }, session.guest);
  return host("POST", "/v1/storefront/shipping/commit", {
    cartId: session.cartId, expectedCartVersion: session.version, idempotencyKey: randomUUID(),
  }, session.guest);
}

try {
  const session = buildCart();
  const committed = ship(session, "r14-abg");
  const checkoutId = pick(committed.json, "checkoutId", "CheckoutId");
  const firstKey = randomUUID();
  const pay = host("POST", `/v1/storefront/checkout/${checkoutId}/payments`, {
    cartId: session.cartId, providerCode: "gateway", idempotencyKey: firstKey,
  }, session.guest);
  const paymentId = pick(pay.json, "paymentId", "PaymentId");
  const attemptId = pick(pay.json, "attemptId", "AttemptId");
  const pref = pick(pay.json, "providerRequestReference", "ProviderRequestReference");
  note("setup-initiate", { ok: pay.status === 200 && !!paymentId, status: pay.status, paymentId, checkoutId });
  const paysBefore = payCount(checkoutId);
  const attemptsBefore = attemptCount(checkoutId);

  const done = host("POST", `/v1/storefront/payments/${paymentId}/sandbox/complete`, {
    cartId: session.cartId, attemptId, providerRequestReference: pref, outcome: "success",
  }, session.guest);
  note("setup-sandbox-success", { ok: done.status === 200, status: done.status });

  const checkout = host("GET", `/v1/storefront/checkout/${checkoutId}?cartId=${session.cartId}`, undefined, session.guest);
  note("ui-paid-capability", {
    ok: checkout.status === 200 && pick(checkout.json, "canInitiatePayment", "CanInitiatePayment") === false
      && pick(checkout.json, "paymentState", "PaymentState") === "Paid",
    status: checkout.status,
    canInitiate: pick(checkout.json, "canInitiatePayment", "CanInitiatePayment"),
    paymentState: pick(checkout.json, "paymentState", "PaymentState"),
  });

  const manual = host("POST", `/v1/storefront/checkout/${checkoutId}/payments`, {
    cartId: session.cartId, providerCode: "manual", idempotencyKey: randomUUID(),
  }, session.guest);
  note("A-manual-after-success", {
    ok: manual.status === 409 && code(manual) === "payment.already_succeeded"
      && payCount(checkoutId) === paysBefore && attemptCount(checkoutId) === attemptsBefore,
    status: manual.status,
    code: code(manual),
    detail: pick(manual.json, "detail", "Detail"),
    pays: payCount(checkoutId),
    attempts: attemptCount(checkoutId),
  });

  const again = host("POST", `/v1/storefront/checkout/${checkoutId}/payments`, {
    cartId: session.cartId, providerCode: "gateway", idempotencyKey: randomUUID(),
  }, session.guest);
  note("B-online-after-success", {
    ok: again.status === 409 && code(again) === "payment.already_succeeded"
      && payCount(checkoutId) === paysBefore,
    status: again.status,
    code: code(again),
  });

  const sameKey = host("POST", `/v1/storefront/checkout/${checkoutId}/payments`, {
    cartId: session.cartId, providerCode: "gateway", idempotencyKey: firstKey,
  }, session.guest);
  note("same-key-replay-after-success", {
    ok: sameKey.status === 200 && pick(sameKey.json, "paymentId", "PaymentId") === paymentId
      && payCount(checkoutId) === paysBefore && attemptCount(checkoutId) === attemptsBefore,
    status: sameKey.status,
    paymentId: pick(sameKey.json, "paymentId", "PaymentId"),
  });

  const evidence = host("POST", `/v1/storefront/payments/${paymentId}/manual-evidence`, {
    cartId: session.cartId, transferReference: "R14-SHOULD-FAIL",
  }, session.guest);
  note("evidence-after-sandbox-success", {
    ok: evidence.status >= 400 && evidence.status !== 200,
    status: evidence.status,
    code: code(evidence),
  });

  const replay = host("POST", `/v1/storefront/payments/${paymentId}/sandbox/complete`, {
    cartId: session.cartId, attemptId, providerRequestReference: pref, outcome: "success",
  }, session.guest);
  note("G-duplicate-success", {
    ok: replay.status === 200 && payCount(checkoutId) === paysBefore,
    status: replay.status,
    pays: payCount(checkoutId),
  });

  const cSess = buildCart();
  const cCommit = ship(cSess, "r14-c");
  const cCheckout = pick(cCommit.json, "checkoutId", "CheckoutId");
  const cPay = host("POST", `/v1/storefront/checkout/${cCheckout}/payments`, {
    cartId: cSess.cartId, providerCode: "manual", idempotencyKey: randomUUID(),
  }, cSess.guest);
  const cPid = pick(cPay.json, "paymentId", "PaymentId");
  const cEv1 = host("POST", `/v1/storefront/payments/${cPid}/manual-evidence`, {
    cartId: cSess.cartId, transferReference: `TRK-R14-C-${Date.now()}`,
  }, cSess.guest);
  const evBefore = evidenceCount(cCheckout);
  const cConfirm = host("POST", `/v1/admin/orders/${cCheckout}/operations`, {
    code: "confirm_deposit", idempotencyKey: randomUUID().replaceAll("-", ""),
  }, admin());
  const cPaid = waitPaid(cCheckout, cPid);
  const cPays = payCount(cCheckout);
  const cEv2 = host("POST", `/v1/storefront/payments/${cPid}/manual-evidence`, {
    cartId: cSess.cartId, transferReference: "R14-C-SHOULD-FAIL",
  }, cSess.guest);
  note("C-manual-confirm-then-evidence", {
    ok: cPay.status === 200 && cEv1.status < 400 && cConfirm.status < 400
      && String(cPaid).toLowerCase() === "succeeded"
      && cEv2.status === 409 && code(cEv2) === "payment.already_succeeded"
      && evidenceCount(cCheckout) === evBefore && payCount(cCheckout) === cPays,
    initiate: cPay.status, evidence1: cEv1.status, confirm: cConfirm.status,
    paid: cPaid, evidence2: cEv2.status, code: code(cEv2),
  });

  const failSession = buildCart();
  const failCommit = ship(failSession, "r14-d");
  const failCheckout = pick(failCommit.json, "checkoutId", "CheckoutId");
  const failPay = host("POST", `/v1/storefront/checkout/${failCheckout}/payments`, {
    cartId: failSession.cartId, providerCode: "gateway", idempotencyKey: randomUUID(),
  }, failSession.guest);
  const failId = pick(failPay.json, "paymentId", "PaymentId");
  const failAttempt = pick(failPay.json, "attemptId", "AttemptId");
  const failPref = pick(failPay.json, "providerRequestReference", "ProviderRequestReference");
  host("POST", `/v1/storefront/payments/${failId}/sandbox/complete`, {
    cartId: failSession.cartId, attemptId: failAttempt, providerRequestReference: failPref, outcome: "failure",
  }, failSession.guest);
  const retry = host("POST", `/v1/storefront/checkout/${failCheckout}/payments`, {
    cartId: failSession.cartId, providerCode: "gateway", idempotencyKey: randomUUID(),
  }, failSession.guest);
  note("D-failed-then-retry", {
    ok: retry.status === 200 && !!pick(retry.json, "paymentId", "PaymentId"),
    status: retry.status,
    paymentId: pick(retry.json, "paymentId", "PaymentId"),
  });

  const eSess = buildCart();
  const eCommit = ship(eSess, "r14-e");
  const eCheckout = pick(eCommit.json, "checkoutId", "CheckoutId");
  const ePay = host("POST", `/v1/storefront/checkout/${eCheckout}/payments`, {
    cartId: eSess.cartId, providerCode: "manual", idempotencyKey: randomUUID(),
  }, eSess.guest);
  const ePid = pick(ePay.json, "paymentId", "PaymentId");
  host("POST", `/v1/storefront/payments/${ePid}/manual-evidence`, {
    cartId: eSess.cartId, transferReference: `TRK-R14-E-${Date.now()}`,
  }, eSess.guest);
  const eReject = host("POST", `/v1/admin/orders/${eCheckout}/operations`, {
    code: "reject_deposit", idempotencyKey: randomUUID().replaceAll("-", ""),
  }, admin());
  const eRetry = host("POST", `/v1/storefront/payments/${ePid}/manual-retry`, { cartId: eSess.cartId }, eSess.guest);
  const eEv2 = host("POST", `/v1/storefront/payments/${ePid}/manual-evidence`, {
    cartId: eSess.cartId, transferReference: `TRK-R14-E2-${Date.now()}`,
  }, eSess.guest);
  note("E-rejected-manual-retry", {
    ok: eReject.status < 400 && eRetry.status < 400 && eEv2.status < 400,
    reject: eReject.status, retry: eRetry.status, evidence2: eEv2.status,
  });

  const fSess = buildCart();
  const fCommit = ship(fSess, "r14-f");
  const fCheckout = pick(fCommit.json, "checkoutId", "CheckoutId");
  const fPay = host("POST", `/v1/storefront/checkout/${fCheckout}/payments`, {
    cartId: fSess.cartId, providerCode: "gateway", idempotencyKey: randomUUID(),
  }, fSess.guest);
  const fPid = pick(fPay.json, "paymentId", "PaymentId");
  sql(`UPDATE payment.payments SET unpaid_timeout_at=NOW() - INTERVAL '2 minutes' WHERE payment_id='${fPid}'`);
  const fExpired = waitExpired(fPid);
  const fRetry = host("POST", `/v1/storefront/payments/${fPid}/unpaid-retry`, { cartId: fSess.cartId }, fSess.guest);
  note("F-expired-unpaid-retry", {
    ok: String(fExpired).toLowerCase() === "expired" && fRetry.status === 200
      && Number(sql(`SELECT COUNT(*) FROM "order".checkouts WHERE checkout_id='${fCheckout}'`)) === 1,
    expired: fExpired, retry: fRetry.status, checkoutId: fCheckout,
  });
} catch (err) {
  note("runtime-exception", { ok: false, message: String(err && err.message ? err.message : err) });
}

writeFileSync(new URL("./r14-runtime-raw.json", import.meta.url), JSON.stringify(out, null, 2));
process.exit(out.ok ? 0 : 1);
