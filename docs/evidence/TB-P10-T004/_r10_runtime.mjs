import { writeFileSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { randomUUID } from "node:crypto";

const HOST = "http://127.0.0.1:5088";
const KG = "01a03826-9936-7000-b499-ff26a6123a8c";
const ADMIN = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const out = { ok: true, steps: [], at: new Date().toISOString(), secretsOmitted: true };

function note(name, data) {
  const row = { name, ...(data ?? {}) };
  out.steps.push(row);
  if (data && data.ok === false) out.ok = false;
  console.log(JSON.stringify(row));
}

function curl(args) {
  const raw = execFileSync("curl.exe", ["-sS", "-w", "\n%{http_code}", ...args], {
    encoding: "utf8",
    maxBuffer: 8e6,
  });
  const i = raw.lastIndexOf("\n");
  let json;
  try {
    json = JSON.parse(raw.slice(0, i));
  } catch {
    json = raw.slice(0, i);
  }
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

function admin() {
  return { "X-Tooba-Dev-Actor-User-Id": ADMIN };
}

function sql(q) {
  return execFileSync(
    "docker",
    ["exec", "-i", "postgres-db", "psql", "-U", "admin", "-d", "tooba_alpha", "-t", "-A", "-c", q],
    { encoding: "utf8" },
  ).trim();
}

function restoreKg() {
  sql(`UPDATE inventory.stock_positions SET on_hand=GREATEST(on_hand,200), reserved=COALESCE((SELECT SUM(r.quantity) FROM inventory.reservations r WHERE r.stock_item_id=stock_positions.stock_item_id AND r.status='Held'),0) WHERE offer_id='${KG}'`);
}

function orderHolds(checkoutId) {
  return Number(
    sql(`SELECT COUNT(*) FROM inventory.reservations r JOIN "order".order_lines ol ON ol.reservation_id=r.reservation_id JOIN "order".seller_orders so ON so.seller_order_id=ol.seller_order_id WHERE so.checkout_id='${checkoutId}' AND r.status='Held'`),
  );
}

function releasedHolds(checkoutId) {
  return Number(
    sql(`SELECT COUNT(*) FROM inventory.reservations r WHERE r.status='Released' AND r.stock_item_id IN (SELECT stock_item_id FROM inventory.stock_positions WHERE offer_id='${KG}') AND r.updated_at > NOW() - INTERVAL '30 minutes'`),
  );
}

function reservationIds(checkoutId) {
  return sql(`SELECT COALESCE(string_agg(ol.reservation_id::text, ','), '') FROM "order".order_lines ol JOIN "order".seller_orders so ON so.seller_order_id=ol.seller_order_id WHERE so.checkout_id='${checkoutId}'`);
}

function payRow(paymentId) {
  const line = sql(`SELECT status||'|'||COALESCE(unpaid_timeout_at::text,'') FROM payment.payments WHERE payment_id='${paymentId}'`);
  const [status, timeout] = line.split("|");
  const attempts = Number(sql(`SELECT COUNT(*) FROM payment.attempts WHERE payment_id='${paymentId}'`));
  return { status, timeout: timeout || "", attempts };
}

function checkoutCount(checkoutId) {
  return Number(sql(`SELECT COUNT(*) FROM "order".checkouts WHERE checkout_id='${checkoutId}'`));
}

function sleep(ms) {
  execFileSync("powershell", ["-NoProfile", "-Command", `Start-Sleep -Milliseconds ${ms}`], { encoding: "utf8" });
}

function waitExpired(paymentId, ms = 45000) {
  const start = Date.now();
  while (Date.now() - start < ms) {
    const row = payRow(paymentId);
    if (String(row.status).toLowerCase() === "expired") return row;
    sleep(2000);
  }
  return payRow(paymentId);
}

function waitOrderHolds(checkoutId, min = 1, ms = 45000) {
  const start = Date.now();
  let n = orderHolds(checkoutId);
  while (Date.now() - start < ms && n < min) {
    sleep(2000);
    n = orderHolds(checkoutId);
  }
  return n;
}

function buildCart(qty = 1) {
  const cart = host("POST", "/v1/storefront/cart");
  const secret = pick(cart.json, "guestSecret", "GuestSecret");
  const cartId = pick(cart.json, "cartId", "CartId");
  let version = pick(cart.json, "version", "Version");
  const guest = { "X-Tooba-Guest-Secret": secret };
  const line = host(
    "POST",
    `/v1/storefront/cart/${cartId}/lines?expectedVersion=${version}`,
    { offerId: KG, quantity: qty },
    guest,
  );
  if (line.status >= 400) throw new Error("add " + line.text);
  version = pick(line.json, "version", "Version");
  return { cartId, secret, guest, version };
}

function ship(session, idem = randomUUID()) {
  const proj = host(
    "POST",
    "/v1/storefront/shipping/projection",
    { cartId: session.cartId, provinceName: "تهران", methodCode: "post:express", language: "fa" },
    session.guest,
  );
  const minDate = pick(proj.json, "minimumDeliveryDate", "MinimumDeliveryDate");
  host(
    "PUT",
    "/v1/storefront/shipping/selection",
    {
      cartId: session.cartId,
      expectedCartVersion: session.version,
      recipientName: "R10",
      contactMobile: "+989121240101",
      provinceName: "تهران",
      cityName: "تهران",
      postalAddress: "a",
      postalCode: "1234567890",
      shippingMethodCode: "post:express",
      selectedDeliveryDate: minDate,
      selectedDeliveryTimeWindow: "9-12",
      customerNote: "r10",
    },
    session.guest,
  );
  return host(
    "POST",
    "/v1/storefront/shipping/commit",
    { cartId: session.cartId, expectedCartVersion: session.version, idempotencyKey: idem },
    session.guest,
  );
}

function pay(session, checkoutId, provider) {
  return host(
    "POST",
    `/v1/storefront/checkout/${checkoutId}/payments`,
    { cartId: session.cartId, providerCode: provider, idempotencyKey: randomUUID() },
    session.guest,
  );
}

function forceDue(paymentId) {
  sql(`UPDATE payment.payments SET unpaid_timeout_at=NOW() - INTERVAL '2 minutes' WHERE payment_id='${paymentId}' AND status IN ('Created','Pending','Failed')`);
}

function sandboxSuccess(session, init) {
  const paymentId = pick(init.json, "paymentId", "PaymentId");
  const attemptId = pick(init.json, "attemptId", "AttemptId");
  const pref = pick(init.json, "providerRequestReference", "ProviderRequestReference");
  return host(
    "POST",
    `/v1/storefront/payments/${paymentId}/sandbox/complete`,
    {
      cartId: session.cartId,
      attemptId,
      providerRequestReference: pref,
      outcome: "success",
    },
    session.guest,
  );
}

function unpaidRetry(session, paymentId) {
  return host(
    "POST",
    `/v1/storefront/payments/${paymentId}/unpaid-retry`,
    { cartId: session.cartId },
    session.guest,
  );
}

function drainKg() {
  sql(`UPDATE inventory.stock_positions SET on_hand=0, reserved=0 WHERE offer_id='${KG}'`);
}

try {
  restoreKg();
  note("health", { status: curl(["-H", "Host: alpha.localhost", HOST + "/health"]).status, ok: true });

  const aSess = buildCart();
  const aCommit = ship(aSess);
  const aCheckout = pick(aCommit.json, "checkoutId", "CheckoutId");
  const aPay = pay(aSess, aCheckout, "fake");
  const aPid = pick(aPay.json, "paymentId", "PaymentId");
  if (!aPid) throw new Error("A initiate " + aPay.text);
  const aBefore = payRow(aPid);
  const aHoldsBefore = orderHolds(aCheckout);
  forceDue(aPid);
  const aAfter = waitExpired(aPid);
  const aHoldsAfter = orderHolds(aCheckout);
  const aReleased = releasedHolds(aCheckout);
  const aOldRes = reservationIds(aCheckout);
  const aOrders = checkoutCount(aCheckout);
  const aAttempts = Number(sql(`SELECT COUNT(*) FROM payment.attempts WHERE payment_id='${aPid}'`));
  note("A-online-unpaid-timeout", {
    ok:
      aCommit.status < 400 &&
      aPay.status < 400 &&
      aHoldsBefore === 1 &&
      String(aAfter.status).toLowerCase() === "expired" &&
      aHoldsAfter === 0 &&
      aReleased >= 1 &&
      aOrders === 1 &&
      aAttempts >= 1 &&
      !!aBefore.timeout,
    commit: aCommit.status,
    pay: aPay.status,
    beforeStatus: aBefore.status,
    afterStatus: aAfter.status,
    holdsBefore: aHoldsBefore,
    holdsAfter: aHoldsAfter,
    released: aReleased,
    orders: aOrders,
    attempts: aAttempts,
  });

  restoreKg();
  const bRetry = unpaidRetry(aSess, aPid);
  const bPage = host("GET", `/v1/storefront/payments/${aPid}?cartId=${aSess.cartId}`, undefined, aSess.guest);
  const bRow = payRow(aPid);
  const bHolds = orderHolds(aCheckout);
  const bNewRes = reservationIds(aCheckout);
  const bOldStillReleased = Number(sql(`SELECT COUNT(*) FROM inventory.reservations WHERE reservation_id IN ('${String(aOldRes).split(",").filter(Boolean).join("','")}') AND status='Released'`));
  const bOrders = checkoutCount(aCheckout);
  note("B-expired-retry-stock-available", {
    ok:
      bRetry.status < 400 &&
      String(bRow.status).toLowerCase() !== "expired" &&
      bHolds === 1 &&
      bOldStillReleased >= 1 &&
      bNewRes !== aOldRes &&
      bOrders === 1 &&
      bRow.attempts >= 2,
    retry: bRetry.status,
    status: bRow.status,
    holds: bHolds,
    oldReleased: bOldStillReleased,
    oldRes: aOldRes,
    newRes: bNewRes,
    orders: bOrders,
    attempts: bRow.attempts,
    pageStatus: pick(bPage.json, "status", "Status"),
  });

  restoreKg();
  const cSess = buildCart();
  const cCommit = ship(cSess);
  const cCheckout = pick(cCommit.json, "checkoutId", "CheckoutId");
  const cPay = pay(cSess, cCheckout, "fake");
  const cPid = pick(cPay.json, "paymentId", "PaymentId");
  forceDue(cPid);
  waitExpired(cPid);
  drainKg();
  const cRetry = unpaidRetry(cSess, cPid);
  const cRow = payRow(cPid);
  const cHolds = orderHolds(cCheckout);
  const cTitle = JSON.stringify(cRetry.json || {}) + cRetry.text;
  note("C-expired-retry-stock-unavailable", {
    ok:
      cRetry.status >= 400 &&
      cTitle.includes("قابل تأمین نیست") &&
      String(cRow.status).toLowerCase() === "expired" &&
      cHolds === 0,
    retry: cRetry.status,
    title: cTitle,
    status: cRow.status,
    holds: cHolds,
  });

  restoreKg();
  const dSess = buildCart();
  const dCommit = ship(dSess);
  const dCheckout = pick(dCommit.json, "checkoutId", "CheckoutId");
  const dPay = pay(dSess, dCheckout, "manual");
  const dPid = pick(dPay.json, "paymentId", "PaymentId");
  const dHoldsBefore = orderHolds(dCheckout);
  forceDue(dPid);
  const dAfter = waitExpired(dPid);
  note("D-manual-initial-timeout", {
    ok: dPay.status < 400 && dHoldsBefore === 1 && String(dAfter.status).toLowerCase() === "expired" && orderHolds(dCheckout) === 0,
    pay: dPay.status,
    afterStatus: dAfter.status,
    holdsBefore: dHoldsBefore,
    holdsAfter: orderHolds(dCheckout),
  });

  restoreKg();
  const eSess = buildCart();
  const eCommit = ship(eSess);
  const eCheckout = pick(eCommit.json, "checkoutId", "CheckoutId");
  const ePay = pay(eSess, eCheckout, "manual");
  const ePid = pick(ePay.json, "paymentId", "PaymentId");
  const eEv = host(
    "POST",
    `/v1/storefront/payments/${ePid}/manual-evidence`,
    { cartId: eSess.cartId, transferReference: `TRK-R10-E-${Date.now()}`, proofMediaAssetId: null },
    eSess.guest,
  );
  sql(`UPDATE payment.payments SET unpaid_timeout_at=NOW() - INTERVAL '2 minutes' WHERE payment_id='${ePid}'`);
  sleep(18000);
  const eAfter = payRow(ePid);
  note("E-manual-evidence-protected", {
    ok: eEv.status < 400 && String(eAfter.status).toLowerCase() !== "expired" && orderHolds(eCheckout) === 1,
    evidence: eEv.status,
    status: eAfter.status,
    holds: orderHolds(eCheckout),
  });

  restoreKg();
  const fSess = buildCart();
  const fCommit = ship(fSess);
  const fCheckout = pick(fCommit.json, "checkoutId", "CheckoutId");
  const fPay = pay(fSess, fCheckout, "fake");
  const fPid = pick(fPay.json, "paymentId", "PaymentId");
  const fOk = sandboxSuccess(fSess, fPay);
  sql(`UPDATE payment.payments SET unpaid_timeout_at=NOW() - INTERVAL '2 minutes' WHERE payment_id='${fPid}'`);
  sleep(18000);
  const fAfter = payRow(fPid);
  note("F-succeeded-untouched", {
    ok: fOk.status < 400 && String(fAfter.status).toLowerCase() === "succeeded" && orderHolds(fCheckout) === 1,
    complete: fOk.status,
    status: fAfter.status,
    holds: orderHolds(fCheckout),
  });

  restoreKg();
  const gSess = buildCart();
  const gCommit = ship(gSess);
  const gCheckout = pick(gCommit.json, "checkoutId", "CheckoutId");
  const gPay = pay(gSess, gCheckout, "fake");
  const gPid = pick(gPay.json, "paymentId", "PaymentId");
  forceDue(gPid);
  waitExpired(gPid);
  const gLate = sandboxSuccess(gSess, gPay);
  const gAfter = payRow(gPid);
  const gHolds = waitOrderHolds(gCheckout, 1);
  note("G-timeout-then-late-success", {
    ok: gLate.status < 400 && String(gAfter.status).toLowerCase() === "succeeded" && gHolds === 1,
    complete: gLate.status,
    status: gAfter.status,
    holds: gHolds,
    orders: checkoutCount(gCheckout),
  });

  restoreKg();
  const g2Sess = buildCart();
  const g2Commit = ship(g2Sess);
  const g2Checkout = pick(g2Commit.json, "checkoutId", "CheckoutId");
  const g2Pay = pay(g2Sess, g2Checkout, "fake");
  const g2Pid = pick(g2Pay.json, "paymentId", "PaymentId");
  forceDue(g2Pid);
  waitExpired(g2Pid);
  drainKg();
  const g2Late = sandboxSuccess(g2Sess, g2Pay);
  const g2After = payRow(g2Pid);
  const g2Supply = host("GET", `/v1/admin/orders/${g2Checkout}/supply-status`, undefined, admin());
  const g2Kind = String(pick(g2Supply.json, "status", "Status", "kind", "Kind") || JSON.stringify(g2Supply.json));
  note("G2-late-success-supply-unavailable", {
    ok:
      g2Late.status < 400 &&
      String(g2After.status).toLowerCase() === "succeeded" &&
      /Unavailable|PartiallyUnavailable/i.test(g2Kind),
    complete: g2Late.status,
    status: g2After.status,
    supply: g2Kind,
  });

  restoreKg();
  const hGet = host("GET", "/v1/admin/settings/hold-policy", undefined, admin());
  const hPut = host(
    "PUT",
    "/v1/admin/settings/hold-policy",
    {
      cartPersistenceHours: 168,
      onlinePaymentHoldHours: 3,
      manualPaymentInitialHoldHours: 4,
      manualPaymentReviewHoldHours: 24,
      methods: [
        {
          providerCode: "fake",
          labelFa: "درگاه آنلاین",
          labelEn: "Online gateway",
          onlinePaymentHoldHours: 5,
          manualPaymentInitialHoldHours: null,
          manualPaymentReviewHoldHours: null,
        },
        {
          providerCode: "manual",
          labelFa: "کارت به کارت",
          labelEn: "Card-to-card",
          onlinePaymentHoldHours: null,
          manualPaymentInitialHoldHours: 6,
          manualPaymentReviewHoldHours: null,
        },
      ],
    },
    admin(),
  );
  const hSess = buildCart();
  const hCommit = ship(hSess);
  const hCheckout = pick(hCommit.json, "checkoutId", "CheckoutId");
  const hPay = pay(hSess, hCheckout, "fake");
  const hPid = pick(hPay.json, "paymentId", "PaymentId");
  const hours = Number(
    sql(`SELECT ROUND(EXTRACT(EPOCH FROM (unpaid_timeout_at - NOW()))/3600.0) FROM payment.payments WHERE payment_id='${hPid}'`),
  );
  const hView = host("GET", "/v1/admin/settings/hold-policy", undefined, admin());
  const onlineEff = pick(pick(hView.json, "onlinePaymentHold", "OnlinePaymentHold"), "effectiveHours", "EffectiveHours");
  const fakeMethod = (pick(hView.json, "methods", "Methods") || []).find(
    (x) => String(pick(x, "providerCode", "ProviderCode")).toLowerCase() === "fake",
  );
  host(
    "PUT",
    "/v1/admin/settings/hold-policy",
    {
      cartPersistenceHours: null,
      onlinePaymentHoldHours: null,
      manualPaymentInitialHoldHours: null,
      manualPaymentReviewHoldHours: null,
      methods: [
        {
          providerCode: "fake",
          onlinePaymentHoldHours: null,
          manualPaymentInitialHoldHours: null,
          manualPaymentReviewHoldHours: null,
        },
        {
          providerCode: "manual",
          onlinePaymentHoldHours: null,
          manualPaymentInitialHoldHours: null,
          manualPaymentReviewHoldHours: null,
        },
      ],
    },
    admin(),
  );
  note("H-admin-settings", {
    ok: hGet.status === 200 && hPut.status === 200 && hours >= 4 && hours <= 6 && Number(onlineEff) === 3 && Number(pick(fakeMethod, "onlinePaymentHoldHours", "OnlinePaymentHoldHours")) === 5,
    get: hGet.status,
    put: hPut.status,
    timeoutHours: hours,
    storeOnlineEffective: onlineEff,
    fakeOverride: pick(fakeMethod, "onlinePaymentHoldHours", "OnlinePaymentHoldHours"),
  });

  restoreKg();
  const iOrders = host(
    "POST",
    "/v1/admin/orders/query",
    { page: 1, pageSize: 50, sort: [{ field: "created", direction: "desc" }], filters: [] },
    admin(),
  );
  const iPays = host(
    "POST",
    "/v1/admin/payments/query",
    { page: 1, pageSize: 50, sort: [{ field: "created", direction: "desc" }], filters: [] },
    admin(),
  );
  const iOrderItems = pick(iOrders.json, "items", "Items") || [];
  const iPayItems = pick(iPays.json, "items", "Items") || [];
  const iExpiredPay = iPayItems.find((x) => /expired/i.test(String(pick(x, "status", "Status", "paymentStatus", "PaymentStatus") || "")));
  const iExpiredOrder = iOrderItems.find((x) => String(pick(x, "checkoutId", "CheckoutId")) === String(dCheckout))
    || iOrderItems.find((x) => String(pick(x, "checkoutId", "CheckoutId")) === String(cCheckout));
  const iSupply = pick(iExpiredOrder, "supplyStatus", "SupplyStatus");
  const iPayState = pick(iExpiredOrder, "paymentState", "PaymentState", "paymentStatus", "PaymentStatus");
  note("I-admin-orders-payments", {
    ok:
      iOrders.status === 200 &&
      iPays.status === 200 &&
      !!iExpiredPay &&
      !!iExpiredOrder &&
      String(iSupply || "") !== String(iPayState || "") &&
      /reacquire|available|unavailable|reserved/i.test(String(iSupply || "")),
    orders: iOrders.status,
    payments: iPays.status,
    expiredPayment: pick(iExpiredPay, "status", "Status"),
    orderPayment: iPayState,
    supply: iSupply,
  });

  const jGet = host("GET", `/v1/customer/orders/${dCheckout}`, undefined, {});
  const jState = String(pick(jGet.json, "paymentState", "PaymentState") || "");
  const jCan = pick(jGet.json, "canRetryUnpaid", "CanRetryUnpaid");
  const jRetry = host("POST", `/v1/customer/orders/${dCheckout}/retry-unpaid`, {}, {});
  const jAfter = host("GET", `/v1/customer/orders/${dCheckout}`, undefined, {});
  const jAfterState = String(pick(jAfter.json, "paymentState", "PaymentState") || "");
  note("J-customer-expired-retry", {
    ok:
      jGet.status === 200 &&
      /PaymentExpired|Expired/i.test(jState) &&
      (jCan === true || jRetry.status < 400 || jRetry.status === 409) &&
      (jRetry.status < 400 ? !/PaymentExpired/i.test(jAfterState) : jRetry.status === 409),
    get: jGet.status,
    paymentState: jState,
    canRetry: jCan,
    retry: jRetry.status,
    afterState: jAfterState,
  });
} catch (err) {
  note("crash", { ok: false, error: String(err && err.message ? err.message : err) });
} finally {
  restoreKg();
  writeFileSync("docs/evidence/TB-P10-T004/r10-runtime-raw.json", JSON.stringify(out, null, 2));
  console.log(JSON.stringify({ ok: out.ok, steps: out.steps.length }));
  if (!out.ok) process.exit(1);
}
