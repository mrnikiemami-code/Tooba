import { writeFileSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { randomUUID } from "node:crypto";

const HOST = "http://127.0.0.1:5088";
const KG = "01a03826-9936-7000-b499-ff26a6123a8c";
const ARMAN = "01a030d1-40f1-7000-95f6-b8efc58e2619";
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

function restoreOffers() {
  for (const offer of [KG, ARMAN]) {
    sql(
      `UPDATE inventory.stock_positions SET on_hand=GREATEST(on_hand,200), reserved=COALESCE((SELECT SUM(r.quantity) FROM inventory.reservations r WHERE r.stock_item_id=stock_positions.stock_item_id AND r.status='Held'),0) WHERE offer_id='${offer}'`,
    );
  }
}

function sleep(ms) {
  execFileSync("powershell", ["-NoProfile", "-Command", `Start-Sleep -Milliseconds ${ms}`], { encoding: "utf8" });
}

function money(checkoutId, paymentId) {
  const payable = Number(sql(`SELECT amount FROM payment.payments WHERE payment_id='${paymentId}'`));
  const shippingAlloc = Number(
    sql(
      `SELECT COALESCE(SUM(allocated_amount),0) FROM payment.allocations WHERE payment_id='${paymentId}' AND target_kind='StoreShipping'`,
    ),
  );
  const sellerAlloc = Number(
    sql(
      `SELECT COALESCE(SUM(allocated_amount),0) FROM payment.allocations WHERE payment_id='${paymentId}' AND target_kind='SellerOrder'`,
    ),
  );
  const checkoutShip = Number(sql(`SELECT shipping_amount FROM "order".checkouts WHERE checkout_id='${checkoutId}'`));
  const sellerTotals = Number(
    sql(`SELECT COALESCE(SUM(grand_total_snapshot),0) FROM "order".seller_orders WHERE checkout_id='${checkoutId}'`),
  );
  const orderPaid = Number(
    sql(`SELECT COUNT(*) FROM "order".seller_orders WHERE checkout_id='${checkoutId}' AND status='Paid'`),
  );
  const orderCount = Number(sql(`SELECT COUNT(*) FROM "order".seller_orders WHERE checkout_id='${checkoutId}'`));
  const inbox = Number(sql(`SELECT COUNT(*) FROM "order".payment_inbox WHERE payment_id='${paymentId}'`));
  const payStatus = sql(`SELECT status FROM payment.payments WHERE payment_id='${paymentId}'`);
  const shippingOnSeller = Number(
    sql(
      `SELECT COUNT(*) FROM payment.allocations a JOIN "order".seller_orders so ON so.seller_order_id=a.seller_order_id WHERE a.payment_id='${paymentId}' AND a.target_kind='SellerOrder' AND a.allocated_amount > so.grand_total_snapshot`,
    ),
  );
  return {
    payable,
    shippingAlloc,
    sellerAlloc,
    checkoutShip,
    sellerTotals,
    orderPaid,
    orderCount,
    inbox,
    payStatus,
    shippingOnSeller,
    totalAlloc: sellerAlloc + shippingAlloc,
  };
}

function moneyOk(m, expectShipping) {
  const shipMatch = expectShipping === 0 ? m.shippingAlloc === 0 && m.checkoutShip === 0 : m.shippingAlloc === m.checkoutShip && m.shippingAlloc > 0;
  return (
    m.payable === m.sellerAlloc + m.shippingAlloc &&
    m.sellerAlloc === m.sellerTotals &&
    shipMatch &&
    String(m.payStatus).toLowerCase() === "succeeded" &&
    m.orderPaid === m.orderCount &&
    m.orderCount >= 1 &&
    m.inbox >= 1 &&
    m.shippingOnSeller === 0
  );
}

function waitPaid(checkoutId, paymentId, ms = 45000) {
  const start = Date.now();
  let m = money(checkoutId, paymentId);
  while (Date.now() - start < ms && !(String(m.payStatus).toLowerCase() === "succeeded" && m.orderPaid === m.orderCount && m.inbox >= 1)) {
    sleep(2000);
    m = money(checkoutId, paymentId);
  }
  return m;
}

function buildCart(offers) {
  const cart = host("POST", "/v1/storefront/cart");
  const secret = pick(cart.json, "guestSecret", "GuestSecret");
  const cartId = pick(cart.json, "cartId", "CartId");
  let version = pick(cart.json, "version", "Version");
  const guest = { "X-Tooba-Guest-Secret": secret };
  for (const offerId of offers) {
    const line = host(
      "POST",
      `/v1/storefront/cart/${cartId}/lines?expectedVersion=${version}`,
      { offerId, quantity: 1 },
      guest,
    );
    if (line.status >= 400) throw new Error("add " + offerId + " " + line.text);
    version = pick(line.json, "version", "Version");
  }
  return { cartId, secret, guest, version };
}

function ship(session, noteText = "r11") {
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
      recipientName: "R11",
      contactMobile: "+989121240111",
      provinceName: "تهران",
      cityName: "تهران",
      postalAddress: "a",
      postalCode: "1234567890",
      shippingMethodCode: "post:express",
      selectedDeliveryDate: minDate,
      selectedDeliveryTimeWindow: "9-12",
      customerNote: noteText,
    },
    session.guest,
  );
  return host(
    "POST",
    "/v1/storefront/shipping/commit",
    { cartId: session.cartId, expectedCartVersion: session.version, idempotencyKey: randomUUID() },
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

function latestAttempt(paymentId) {
  const line = sql(
    `SELECT attempt_id::text||'|'||COALESCE(provider_request_reference,'') FROM payment.attempts WHERE payment_id='${paymentId}' ORDER BY created_at DESC LIMIT 1`,
  );
  const [attemptId, pref] = line.split("|");
  return { attemptId, pref };
}

function sandboxSuccess(session, init, paymentIdOverride) {
  const paymentId = paymentIdOverride || pick(init.json, "paymentId", "PaymentId");
  let attemptId = pick(init?.json, "attemptId", "AttemptId");
  let pref = pick(init?.json, "providerRequestReference", "ProviderRequestReference");
  if (!attemptId || paymentIdOverride) {
    const latest = latestAttempt(paymentId);
    attemptId = latest.attemptId;
    pref = latest.pref;
  }
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

function forceDue(paymentId) {
  sql(
    `UPDATE payment.payments SET unpaid_timeout_at=NOW() - INTERVAL '2 minutes' WHERE payment_id='${paymentId}' AND status IN ('Created','Pending','Failed','Expired')`,
  );
}

function waitExpired(paymentId, ms = 45000) {
  const start = Date.now();
  while (Date.now() - start < ms) {
    const status = sql(`SELECT status FROM payment.payments WHERE payment_id='${paymentId}'`);
    if (String(status).toLowerCase() === "expired") return status;
    sleep(2000);
  }
  return sql(`SELECT status FROM payment.payments WHERE payment_id='${paymentId}'`);
}

function unpaidRetry(session, paymentId) {
  return host(
    "POST",
    `/v1/storefront/payments/${paymentId}/unpaid-retry`,
    { cartId: session.cartId },
    session.guest,
  );
}

try {
  restoreOffers();
  note("health", { status: curl(["-H", "Host: alpha.localhost", HOST + "/health"]).status, ok: true });

  const aSess = buildCart([KG]);
  const aCommit = ship(aSess, "r11-a");
  const aCheckout = pick(aCommit.json, "checkoutId", "CheckoutId");
  const aPay = pay(aSess, aCheckout, "fake");
  const aPid = pick(aPay.json, "paymentId", "PaymentId");
  const aDone = sandboxSuccess(aSess, aPay);
  const aMoney = waitPaid(aCheckout, aPid);
  note("A-one-seller-shipping", {
    ok: aCommit.status < 400 && aPay.status < 400 && aDone.status < 400 && moneyOk(aMoney, 1),
    payable: aMoney.payable,
    sellerAlloc: aMoney.sellerAlloc,
    shippingAlloc: aMoney.shippingAlloc,
    totalAlloc: aMoney.totalAlloc,
    payStatus: aMoney.payStatus,
    orderPaid: `${aMoney.orderPaid}/${aMoney.orderCount}`,
    settlementVisible: aMoney.sellerAlloc,
    inbox: aMoney.inbox,
  });

  restoreOffers();
  const bSess = buildCart([KG, ARMAN]);
  const bCommit = ship(bSess, "r11-b");
  const bCheckout = pick(bCommit.json, "checkoutId", "CheckoutId");
  const bSellers = Number(sql(`SELECT COUNT(*) FROM "order".seller_orders WHERE checkout_id='${bCheckout}'`));
  const bPay = pay(bSess, bCheckout, "fake");
  const bPid = pick(bPay.json, "paymentId", "PaymentId");
  const bDone = sandboxSuccess(bSess, bPay);
  const bMoney = waitPaid(bCheckout, bPid);
  note("B-multiseller-shipping", {
    ok: bCommit.status < 400 && bSellers >= 2 && bPay.status < 400 && bDone.status < 400 && moneyOk(bMoney, 1),
    sellers: bSellers,
    payable: bMoney.payable,
    sellerAlloc: bMoney.sellerAlloc,
    shippingAlloc: bMoney.shippingAlloc,
    totalAlloc: bMoney.totalAlloc,
    payStatus: bMoney.payStatus,
    orderPaid: `${bMoney.orderPaid}/${bMoney.orderCount}`,
    settlementVisible: bMoney.sellerAlloc,
    shippingOnSeller: bMoney.shippingOnSeller,
  });

  restoreOffers();
  const zSess = buildCart([KG]);
  const zCommit = ship(zSess, "r11-zero");
  const zCheckout = pick(zCommit.json, "checkoutId", "CheckoutId");
  sql(`UPDATE "order".checkouts SET shipping_amount=0 WHERE checkout_id='${zCheckout}'`);
  const zPay = pay(zSess, zCheckout, "fake");
  const zPid = pick(zPay.json, "paymentId", "PaymentId");
  const zDone = sandboxSuccess(zSess, zPay);
  const zMoney = waitPaid(zCheckout, zPid);
  note("shipping-zero", {
    ok: zPay.status < 400 && zDone.status < 400 && moneyOk(zMoney, 0),
    payable: zMoney.payable,
    sellerAlloc: zMoney.sellerAlloc,
    shippingAlloc: zMoney.shippingAlloc,
    payStatus: zMoney.payStatus,
    orderPaid: `${zMoney.orderPaid}/${zMoney.orderCount}`,
  });

  restoreOffers();
  const cSess = buildCart([KG]);
  const cCommit = ship(cSess, "r11-c");
  const cCheckout = pick(cCommit.json, "checkoutId", "CheckoutId");
  const cPay = pay(cSess, cCheckout, "manual");
  const cPid = pick(cPay.json, "paymentId", "PaymentId");
  const cEv = host(
    "POST",
    `/v1/storefront/payments/${cPid}/manual-evidence`,
    { cartId: cSess.cartId, transferReference: `TRK-R11-C-${Date.now()}`, proofMediaAssetId: null },
    cSess.guest,
  );
  const cConfirm = host(
    "POST",
    `/v1/admin/orders/${cCheckout}/operations`,
    { code: "confirm_deposit", idempotencyKey: randomUUID().replaceAll("-", "") },
    admin(),
  );
  const cMoney = waitPaid(cCheckout, cPid);
  note("C-manual-confirm", {
    ok: cPay.status < 400 && cEv.status < 400 && cConfirm.status < 400 && moneyOk(cMoney, 1),
    evidence: cEv.status,
    confirm: cConfirm.status,
    payable: cMoney.payable,
    sellerAlloc: cMoney.sellerAlloc,
    shippingAlloc: cMoney.shippingAlloc,
    payStatus: cMoney.payStatus,
    orderPaid: `${cMoney.orderPaid}/${cMoney.orderCount}`,
    settlementVisible: cMoney.sellerAlloc,
  });

  restoreOffers();
  const dSess = buildCart([KG]);
  const dCommit = ship(dSess, "r11-d");
  const dCheckout = pick(dCommit.json, "checkoutId", "CheckoutId");
  const dPay = pay(dSess, dCheckout, "fake");
  const dPid = pick(dPay.json, "paymentId", "PaymentId");
  const dDone = sandboxSuccess(dSess, dPay);
  const dMoney = waitPaid(dCheckout, dPid);
  note("D-sandbox-success", {
    ok: dDone.status < 400 && moneyOk(dMoney, 1),
    payable: dMoney.payable,
    sellerAlloc: dMoney.sellerAlloc,
    shippingAlloc: dMoney.shippingAlloc,
    payStatus: dMoney.payStatus,
    orderPaid: `${dMoney.orderPaid}/${dMoney.orderCount}`,
  });

  restoreOffers();
  const eSess = buildCart([KG]);
  const eCommit = ship(eSess, "r11-e");
  const eCheckout = pick(eCommit.json, "checkoutId", "CheckoutId");
  const ePay = pay(eSess, eCheckout, "fake");
  const ePid = pick(ePay.json, "paymentId", "PaymentId");
  forceDue(ePid);
  waitExpired(ePid);
  const eRetry = unpaidRetry(eSess, ePid);
  const eDone = sandboxSuccess(eSess, ePay, ePid);
  const eMoney = waitPaid(eCheckout, ePid);
  note("E-expired-retry-then-success", {
    ok: eRetry.status < 400 && eDone.status < 400 && moneyOk(eMoney, 1),
    retry: eRetry.status,
    complete: eDone.status,
    payable: eMoney.payable,
    sellerAlloc: eMoney.sellerAlloc,
    shippingAlloc: eMoney.shippingAlloc,
    payStatus: eMoney.payStatus,
    orderPaid: `${eMoney.orderPaid}/${eMoney.orderCount}`,
  });

  restoreOffers();
  const fSess = buildCart([KG]);
  const fCommit = ship(fSess, "r11-f");
  const fCheckout = pick(fCommit.json, "checkoutId", "CheckoutId");
  const fPay = pay(fSess, fCheckout, "fake");
  const fPid = pick(fPay.json, "paymentId", "PaymentId");
  forceDue(fPid);
  waitExpired(fPid);
  const fDone = sandboxSuccess(fSess, fPay);
  const fMoney = waitPaid(fCheckout, fPid);
  note("F-late-captured", {
    ok: fDone.status < 400 && moneyOk(fMoney, 1),
    complete: fDone.status,
    payable: fMoney.payable,
    sellerAlloc: fMoney.sellerAlloc,
    shippingAlloc: fMoney.shippingAlloc,
    payStatus: fMoney.payStatus,
    orderPaid: `${fMoney.orderPaid}/${fMoney.orderCount}`,
    inbox: fMoney.inbox,
  });

  restoreOffers();
  const gSess = buildCart([KG]);
  const gCommit = ship(gSess, "r11-g");
  const gCheckout = pick(gCommit.json, "checkoutId", "CheckoutId");
  const gPay = pay(gSess, gCheckout, "fake");
  const gPid = pick(gPay.json, "paymentId", "PaymentId");
  const gDone1 = sandboxSuccess(gSess, gPay);
  const gFirst = waitPaid(gCheckout, gPid);
  const gDone2 = sandboxSuccess(gSess, gPay);
  sleep(4000);
  const gSecond = money(gCheckout, gPid);
  note("G-duplicate-succeeded", {
    ok:
      gDone1.status < 400 &&
      moneyOk(gFirst, 1) &&
      gSecond.inbox === gFirst.inbox &&
      gSecond.orderPaid === gFirst.orderPaid &&
      String(gSecond.payStatus).toLowerCase() === "succeeded",
    firstInbox: gFirst.inbox,
    secondInbox: gSecond.inbox,
    complete1: gDone1.status,
    complete2: gDone2.status,
    payable: gSecond.payable,
    sellerAlloc: gSecond.sellerAlloc,
    shippingAlloc: gSecond.shippingAlloc,
    payStatus: gSecond.payStatus,
  });
} catch (err) {
  note("crash", { ok: false, error: String(err && err.message ? err.message : err) });
} finally {
  restoreOffers();
  writeFileSync("docs/evidence/TB-P10-T004/r11-runtime-raw.json", JSON.stringify(out, null, 2));
  console.log(JSON.stringify({ ok: out.ok, steps: out.steps.length }));
  if (!out.ok) process.exit(1);
}
