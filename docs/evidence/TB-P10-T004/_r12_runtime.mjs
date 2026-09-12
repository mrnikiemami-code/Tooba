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
function restoreOffers() {
  for (const offer of [KG, ARMAN]) {
    sql(`UPDATE inventory.stock_positions SET on_hand=GREATEST(on_hand,200), reserved=COALESCE((SELECT SUM(r.quantity) FROM inventory.reservations r WHERE r.stock_item_id=stock_positions.stock_item_id AND r.status='Held'),0) WHERE offer_id='${offer}'`);
  }
}
function sleep(ms) {
  execFileSync("powershell", ["-NoProfile", "-Command", `Start-Sleep -Milliseconds ${ms}`], { encoding: "utf8" });
}
function orderHolds(checkoutId) {
  return Number(sql(`SELECT COUNT(*) FROM inventory.reservations r JOIN "order".order_lines ol ON ol.reservation_id=r.reservation_id JOIN "order".seller_orders so ON so.seller_order_id=ol.seller_order_id WHERE so.checkout_id='${checkoutId}' AND r.status='Held'`));
}
function cartReserves(cartId) {
  return Number(sql(`SELECT COUNT(*) FROM cart.cart_lines WHERE cart_id='${cartId}' AND reservation_id IS NOT NULL`));
}
function resv(checkoutId) {
  const line = sql(`SELECT r.reservation_id||'|'||r.status||'|'||COALESCE(r.expires_at::text,'') FROM "order".order_lines ol JOIN "order".seller_orders so ON so.seller_order_id=ol.seller_order_id JOIN inventory.reservations r ON r.reservation_id=ol.reservation_id WHERE so.checkout_id='${checkoutId}' LIMIT 1`);
  if (!line) return null;
  const [id, status, expiresAt] = line.split("|");
  return { id, status, expiresAt: expiresAt || "" };
}
function forceRelease(checkoutId) {
  const r = resv(checkoutId);
  if (!r?.id) return null;
  sql(`UPDATE inventory.reservations SET status='Released', expires_at=NOW()-interval '1 minute', updated_at=NOW() WHERE reservation_id='${r.id}'`);
  sql(`UPDATE inventory.stock_positions SET reserved=GREATEST(reserved-1,0) WHERE stock_item_id=(SELECT stock_item_id FROM inventory.reservations WHERE reservation_id='${r.id}')`);
  return r.id;
}
function waitPaid(checkoutId, paymentId, ms = 45000) {
  const start = Date.now();
  while (Date.now() - start < ms) {
    const paid = Number(sql(`SELECT COUNT(*) FROM "order".seller_orders WHERE checkout_id='${checkoutId}' AND status='Paid'`));
    const n = Number(sql(`SELECT COUNT(*) FROM "order".seller_orders WHERE checkout_id='${checkoutId}'`));
    const st = sql(`SELECT status FROM payment.payments WHERE payment_id='${paymentId}'`);
    if (String(st).toLowerCase() === "succeeded" && paid === n && n >= 1) return { paid, n, st };
    sleep(2000);
  }
  return {
    paid: Number(sql(`SELECT COUNT(*) FROM "order".seller_orders WHERE checkout_id='${checkoutId}' AND status='Paid'`)),
    n: Number(sql(`SELECT COUNT(*) FROM "order".seller_orders WHERE checkout_id='${checkoutId}'`)),
    st: sql(`SELECT status FROM payment.payments WHERE payment_id='${paymentId}'`),
  };
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
function forceDue(paymentId) {
  sql(`UPDATE payment.payments SET unpaid_timeout_at=NOW() - INTERVAL '2 minutes' WHERE payment_id='${paymentId}'`);
}
function buildCart(offers, qty = 1) {
  const cart = host("POST", "/v1/storefront/cart");
  const secret = pick(cart.json, "guestSecret", "GuestSecret");
  const cartId = pick(cart.json, "cartId", "CartId");
  let version = pick(cart.json, "version", "Version");
  const guest = { "X-Tooba-Guest-Secret": secret };
  for (const offerId of offers) {
    const line = host("POST", `/v1/storefront/cart/${cartId}/lines?expectedVersion=${version}`, { offerId, quantity: qty }, guest);
    if (line.status >= 400) throw new Error("add " + offerId + " " + line.text);
    version = pick(line.json, "version", "Version");
  }
  return { cartId, secret, guest, version };
}
function ship(session, noteText = "r12") {
  const proj = host("POST", "/v1/storefront/shipping/projection", { cartId: session.cartId, provinceName: "تهران", methodCode: "post:express", language: "fa" }, session.guest);
  const minDate = pick(proj.json, "minimumDeliveryDate", "MinimumDeliveryDate");
  host("PUT", "/v1/storefront/shipping/selection", {
    cartId: session.cartId, expectedCartVersion: session.version, recipientName: "R12", contactMobile: "+989121240112",
    provinceName: "تهران", cityName: "تهران", postalAddress: "a", postalCode: "1234567890",
    shippingMethodCode: "post:express", selectedDeliveryDate: minDate, selectedDeliveryTimeWindow: "9-12", customerNote: noteText,
  }, session.guest);
  return host("POST", "/v1/storefront/shipping/commit", { cartId: session.cartId, expectedCartVersion: session.version, idempotencyKey: randomUUID() }, session.guest);
}
function pay(session, checkoutId, provider, key = randomUUID()) {
  return host("POST", `/v1/storefront/checkout/${checkoutId}/payments`, { cartId: session.cartId, providerCode: provider, idempotencyKey: key }, session.guest);
}
function sandbox(session, paymentId, attemptId, pref, outcome) {
  return host("POST", `/v1/storefront/payments/${paymentId}/sandbox/complete`, { cartId: session.cartId, attemptId, providerRequestReference: pref, outcome }, session.guest);
}
function latestAttempt(paymentId) {
  const line = sql(`SELECT attempt_id::text||'|'||COALESCE(provider_request_reference,'') FROM payment.attempts WHERE payment_id='${paymentId}' ORDER BY created_at DESC LIMIT 1`);
  const [attemptId, pref] = line.split("|");
  return { attemptId, pref };
}

try {
  restoreOffers();
  note("health", { status: curl(["-H", "Host: alpha.localhost", HOST + "/health"]).status, ok: true });

  const aSess = buildCart([KG]);
  const aCartHolds = cartReserves(aSess.cartId);
  const aCommit = ship(aSess, "r12-a");
  const aCheckout = pick(aCommit.json, "checkoutId", "CheckoutId");
  const aPay = pay(aSess, aCheckout, "fake");
  const aPid = pick(aPay.json, "paymentId", "PaymentId");
  const aDone = sandbox(aSess, aPid, pick(aPay.json, "attemptId", "AttemptId"), pick(aPay.json, "providerRequestReference", "ProviderRequestReference"), "success");
  const aPaid = waitPaid(aCheckout, aPid);
  note("A-online-success", {
    ok: aCartHolds === 0 && aCommit.status < 400 && aDone.status < 400 && String(aPaid.st).toLowerCase() === "succeeded" && aPaid.paid === aPaid.n && orderHolds(aCheckout) === 1,
    cartReserves: aCartHolds, commit: aCommit.status, paid: aPaid.st, holds: orderHolds(aCheckout),
  });

  restoreOffers();
  const bSess = buildCart([KG]);
  const bCommit = ship(bSess, "r12-b");
  const bCheckout = pick(bCommit.json, "checkoutId", "CheckoutId");
  const bKey = `r12-b-${bCheckout}`;
  const bPay1 = pay(bSess, bCheckout, "fake", bKey);
  const bPid = pick(bPay1.json, "paymentId", "PaymentId");
  const bFail = sandbox(bSess, bPid, pick(bPay1.json, "attemptId", "AttemptId"), pick(bPay1.json, "providerRequestReference", "ProviderRequestReference"), "failure");
  const bPay2 = pay(bSess, bCheckout, "fake", bKey);
  const bDone = sandbox(bSess, bPid, pick(bPay2.json, "attemptId", "AttemptId"), pick(bPay2.json, "providerRequestReference", "ProviderRequestReference"), "success");
  const bPaid = waitPaid(bCheckout, bPid);
  note("B-online-fail-retry", {
    ok: bFail.status < 400 && bPay2.status < 400 && pick(bPay2.json, "paymentId", "PaymentId") === bPid && pick(bPay2.json, "attemptId", "AttemptId") !== pick(bPay1.json, "attemptId", "AttemptId") && String(bPaid.st).toLowerCase() === "succeeded" && Number(sql(`SELECT COUNT(*) FROM "order".checkouts WHERE checkout_id='${bCheckout}'`)) === 1,
    samePayment: pick(bPay2.json, "paymentId", "PaymentId") === bPid, newAttempt: pick(bPay2.json, "attemptId", "AttemptId") !== pick(bPay1.json, "attemptId", "AttemptId"), paid: bPaid.st,
  });

  restoreOffers();
  const cSess = buildCart([KG]);
  const cCommit = ship(cSess, "r12-c");
  const cCheckout = pick(cCommit.json, "checkoutId", "CheckoutId");
  const cPay = pay(cSess, cCheckout, "fake");
  const cPid = pick(cPay.json, "paymentId", "PaymentId");
  const cOld = resv(cCheckout)?.id;
  forceDue(cPid);
  waitExpired(cPid);
  const cHoldsAfterExpire = orderHolds(cCheckout);
  const cRetry = host("POST", `/v1/storefront/payments/${cPid}/unpaid-retry`, { cartId: cSess.cartId }, cSess.guest);
  const cNew = resv(cCheckout);
  const cAtt = latestAttempt(cPid);
  const cDone = sandbox(cSess, cPid, cAtt.attemptId, cAtt.pref, "success");
  const cPaid = waitPaid(cCheckout, cPid);
  note("C-timeout-retry-pay", {
    ok: cHoldsAfterExpire === 0 && cRetry.status < 400 && cNew?.id && cNew.id !== cOld && String(cPaid.st).toLowerCase() === "succeeded",
    expireHolds: cHoldsAfterExpire, retry: cRetry.status, oldRes: cOld, newRes: cNew?.id, paid: cPaid.st,
  });

  restoreOffers();
  const dSess = buildCart([KG]);
  const dCommit = ship(dSess, "r12-d");
  const dCheckout = pick(dCommit.json, "checkoutId", "CheckoutId");
  const dPay = pay(dSess, dCheckout, "fake");
  const dPid = pick(dPay.json, "paymentId", "PaymentId");
  forceDue(dPid);
  waitExpired(dPid);
  sql(`UPDATE inventory.stock_positions SET on_hand=0, reserved=0 WHERE offer_id='${KG}'`);
  const dRetry = host("POST", `/v1/storefront/payments/${dPid}/unpaid-retry`, { cartId: dSess.cartId }, dSess.guest);
  const dText = JSON.stringify(dRetry.json || {}) + dRetry.text;
  note("D-timeout-unavailable", {
    ok: dRetry.status >= 400 && dText.includes("قابل تأمین نیست") && String(sql(`SELECT status FROM payment.payments WHERE payment_id='${dPid}'`)).toLowerCase() === "expired",
    retry: dRetry.status,
  });

  restoreOffers();
  const eSess = buildCart([KG]);
  const eCommit = ship(eSess, "r12-e");
  const eCheckout = pick(eCommit.json, "checkoutId", "CheckoutId");
  const ePay = pay(eSess, eCheckout, "fake");
  const ePid = pick(ePay.json, "paymentId", "PaymentId");
  forceDue(ePid);
  waitExpired(ePid);
  const eDone = sandbox(eSess, ePid, pick(ePay.json, "attemptId", "AttemptId"), pick(ePay.json, "providerRequestReference", "ProviderRequestReference"), "success");
  const ePaid = waitPaid(eCheckout, ePid);
  note("E-late-captured", {
    ok: eDone.status < 400 && String(ePaid.st).toLowerCase() === "succeeded",
    complete: eDone.status, paid: ePaid.st, holds: orderHolds(eCheckout),
  });

  restoreOffers();
  const fSess = buildCart([KG]);
  const fCommit = ship(fSess, "r12-f");
  const fCheckout = pick(fCommit.json, "checkoutId", "CheckoutId");
  const fPay = pay(fSess, fCheckout, "manual");
  const fPid = pick(fPay.json, "paymentId", "PaymentId");
  const fEv = host("POST", `/v1/storefront/payments/${fPid}/manual-evidence`, { cartId: fSess.cartId, transferReference: `TRK-R12-F-${Date.now()}`, proofMediaAssetId: null }, fSess.guest);
  const fConfirm = host("POST", `/v1/admin/orders/${fCheckout}/operations`, { code: "confirm_deposit", idempotencyKey: randomUUID().replaceAll("-", "") }, admin());
  const fPaid = waitPaid(fCheckout, fPid);
  const fRes = resv(fCheckout);
  note("F-manual-normal", {
    ok: fEv.status < 400 && fConfirm.status < 400 && String(fPaid.st).toLowerCase() === "succeeded" && fRes?.status === "Held" && !fRes.expiresAt,
    evidence: fEv.status, confirm: fConfirm.status, paid: fPaid.st,
  });

  restoreOffers();
  const gSess = buildCart([KG]);
  const gCommit = ship(gSess, "r12-g");
  const gCheckout = pick(gCommit.json, "checkoutId", "CheckoutId");
  const gPay = pay(gSess, gCheckout, "manual");
  const gPid = pick(gPay.json, "paymentId", "PaymentId");
  host("POST", `/v1/storefront/payments/${gPid}/manual-evidence`, { cartId: gSess.cartId, transferReference: `TRK-R12-G-${Date.now()}`, proofMediaAssetId: null }, gSess.guest);
  const gOld = resv(gCheckout)?.id;
  const gReject = host("POST", `/v1/admin/orders/${gCheckout}/operations`, { code: "reject_deposit", idempotencyKey: randomUUID().replaceAll("-", "") }, admin());
  const gAfterReject = orderHolds(gCheckout);
  const gRetry = host("POST", `/v1/storefront/payments/${gPid}/manual-retry`, { cartId: gSess.cartId }, gSess.guest);
  const gEv2 = host("POST", `/v1/storefront/payments/${gPid}/manual-evidence`, { cartId: gSess.cartId, transferReference: `TRK-R12-G2-${Date.now()}`, proofMediaAssetId: null }, gSess.guest);
  const gNew = resv(gCheckout);
  const gOldStatus = gOld ? sql(`SELECT status FROM inventory.reservations WHERE reservation_id='${gOld}'`) : "";
  note("G-manual-reject-retry", {
    ok: gReject.status < 400 && gAfterReject === 0 && gRetry.status < 400 && gEv2.status < 400 && gNew?.id && gNew.id !== gOld && gNew.status === "Held" && gOldStatus === "Released" && Number(sql(`SELECT COUNT(*) FROM "order".checkouts WHERE checkout_id='${gCheckout}'`)) === 1,
    reject: gReject.status, retry: gRetry.status, evidence2: gEv2.status, oldRes: gOld, newRes: gNew?.id, afterRejectHolds: gAfterReject, oldStatus: gOldStatus,
  });

  restoreOffers();
  const hSess = buildCart([KG]);
  const hCommit = ship(hSess, "r12-h");
  const hCheckout = pick(hCommit.json, "checkoutId", "CheckoutId");
  const hPay = pay(hSess, hCheckout, "manual");
  const hPid = pick(hPay.json, "paymentId", "PaymentId");
  host("POST", `/v1/storefront/payments/${hPid}/manual-evidence`, { cartId: hSess.cartId, transferReference: `TRK-R12-H-${Date.now()}`, proofMediaAssetId: null }, hSess.guest);
  const hOld = forceRelease(hCheckout);
  const hConfirm = host("POST", `/v1/admin/orders/${hCheckout}/operations`, { code: "confirm_deposit", idempotencyKey: randomUUID().replaceAll("-", "") }, admin());
  const hPaid = waitPaid(hCheckout, hPid);
  const hNew = resv(hCheckout);
  note("H-review-expiry-reacquire", {
    ok: hConfirm.status < 400 && String(hPaid.st).toLowerCase() === "succeeded" && hNew?.id && hNew.id !== hOld && sql(`SELECT status FROM inventory.reservations WHERE reservation_id='${hOld}'`) === "Released",
    confirm: hConfirm.status, oldRes: hOld, newRes: hNew?.id, paid: hPaid.st,
  });

  restoreOffers();
  const iSess = buildCart([KG]);
  const iCommit = ship(iSess, "r12-i");
  const iCheckout = pick(iCommit.json, "checkoutId", "CheckoutId");
  const iPay = pay(iSess, iCheckout, "manual");
  const iPid = pick(iPay.json, "paymentId", "PaymentId");
  host("POST", `/v1/storefront/payments/${iPid}/manual-evidence`, { cartId: iSess.cartId, transferReference: `TRK-R12-I-${Date.now()}`, proofMediaAssetId: null }, iSess.guest);
  const iOld = forceRelease(iCheckout);
  sql(`UPDATE inventory.stock_positions SET on_hand=0, reserved=0 WHERE offer_id='${KG}'`);
  const iConfirm = host("POST", `/v1/admin/orders/${iCheckout}/operations`, { code: "confirm_deposit", idempotencyKey: randomUUID().replaceAll("-", "") }, admin());
  const iText = JSON.stringify(iConfirm.json || {}) + iConfirm.text;
  note("I-review-expiry-unavailable", {
    ok: iConfirm.status >= 400 && (iText.includes("قابل تأمین") || iText.includes("موجودی") || iText.includes("unavailable") || iText.includes("تأمین")),
    confirm: iConfirm.status, oldRes: iOld,
  });

  restoreOffers();
  const jSess = buildCart([KG, ARMAN]);
  const jCommit = ship(jSess, "r12-j");
  const jCheckout = pick(jCommit.json, "checkoutId", "CheckoutId");
  const jPay = pay(jSess, jCheckout, "fake");
  const jPid = pick(jPay.json, "paymentId", "PaymentId");
  sandbox(jSess, jPid, pick(jPay.json, "attemptId", "AttemptId"), pick(jPay.json, "providerRequestReference", "ProviderRequestReference"), "success");
  const jPaid = waitPaid(jCheckout, jPid);
  const jSeller = Number(sql(`SELECT COALESCE(SUM(allocated_amount),0) FROM payment.allocations WHERE payment_id='${jPid}' AND target_kind='SellerOrder'`));
  const jShip = Number(sql(`SELECT COALESCE(SUM(allocated_amount),0) FROM payment.allocations WHERE payment_id='${jPid}' AND target_kind='StoreShipping'`));
  const jAmt = Number(sql(`SELECT amount FROM payment.payments WHERE payment_id='${jPid}'`));
  const jSellers = Number(sql(`SELECT COUNT(*) FROM "order".seller_orders WHERE checkout_id='${jCheckout}'`));
  note("J-multiseller-shipping", {
    ok: jSellers >= 2 && jAmt === jSeller + jShip && jShip > 0 && String(jPaid.st).toLowerCase() === "succeeded",
    sellers: jSellers, payable: jAmt, sellerAlloc: jSeller, shippingAlloc: jShip,
  });

  restoreOffers();
  const kSess = buildCart([KG], 1.25);
  const kCommit = ship(kSess, "r12-k");
  const kCheckout = pick(kCommit.json, "checkoutId", "CheckoutId");
  const kQty = Number(sql(`SELECT quantity FROM "order".order_lines ol JOIN "order".seller_orders so ON so.seller_order_id=ol.seller_order_id WHERE so.checkout_id='${kCheckout}' LIMIT 1`));
  const kResQty = Number(sql(`SELECT r.quantity FROM inventory.reservations r JOIN "order".order_lines ol ON ol.reservation_id=r.reservation_id JOIN "order".seller_orders so ON so.seller_order_id=ol.seller_order_id WHERE so.checkout_id='${kCheckout}' LIMIT 1`));
  const kCartQty = Number(sql(`SELECT quantity FROM cart.cart_lines WHERE cart_id='${kSess.cartId}' LIMIT 1`));
  note("K-decimal-1.25", { ok: kCommit.status < 400 && kQty === 1.25 && kResQty === 1.25 && kCartQty === 1.25, cart: kCartQty, order: kQty, reservation: kResQty });

  restoreOffers();
  const lSess = buildCart([KG]);
  const lBefore = cartReserves(lSess.cartId);
  const lInv = Number(sql(`SELECT COUNT(*) FROM inventory.reservations r JOIN inventory.stock_positions sp ON sp.stock_item_id=r.stock_item_id WHERE sp.offer_id='${KG}' AND r.status='Held' AND r.created_at > NOW() - INTERVAL '15 seconds'`));
  const lCommit = ship(lSess, "r12-l");
  const lCheckout = pick(lCommit.json, "checkoutId", "CheckoutId");
  note("L-cart-no-hard-reserve", { ok: lBefore === 0 && lCommit.status < 400 && orderHolds(lCheckout) === 1, cartReserves: lBefore, recentKgHoldsBeforeCommit: lInv, afterCommitHolds: orderHolds(lCheckout) });

  restoreOffers();
  sql(`UPDATE inventory.stock_positions SET on_hand=reserved+1 WHERE offer_id='${KG}'`);
  const m1 = buildCart([KG]);
  const m2 = buildCart([KG]);
  const mWin = ship(m1, "r12-m1");
  const mLose = ship(m2, "r12-m2");
  const mWinId = pick(mWin.json, "checkoutId", "CheckoutId");
  const reserved = Number(sql(`SELECT reserved FROM inventory.stock_positions WHERE offer_id='${KG}' LIMIT 1`));
  const onHand = Number(sql(`SELECT on_hand FROM inventory.stock_positions WHERE offer_id='${KG}' LIMIT 1`));
  note("M-last-unit-race", {
    ok: mWin.status < 400 && mLose.status >= 400 && !!mWinId && !pick(mLose.json, "checkoutId", "CheckoutId") && reserved <= onHand,
    win: mWin.status, lose: mLose.status, reserved, onHand,
  });

  restoreOffers();
  const nOrders = host("POST", "/v1/admin/orders/query", { page: 1, pageSize: 20, sort: [{ field: "created", direction: "desc" }], filters: [] }, admin());
  const nPays = host("POST", "/v1/admin/payments/query", { page: 1, pageSize: 20, sort: [{ field: "created", direction: "desc" }], filters: [] }, admin());
  const nItems = pick(nOrders.json, "items", "Items") || [];
  const nHasSupply = nItems.some((x) => pick(x, "supplyStatus", "SupplyStatus"));
  note("N-admin-supply", { ok: nOrders.status === 200 && nPays.status === 200 && nHasSupply, orders: nOrders.status, payments: nPays.status, sample: pick(nItems[0], "supplyStatus", "SupplyStatus") });

  restoreOffers();
  const oSess = buildCart([KG]);
  const oCommit = ship(oSess, "r12-o");
  const oCheckout = pick(oCommit.json, "checkoutId", "CheckoutId");
  const oPay = pay(oSess, oCheckout, "manual");
  const oPid = pick(oPay.json, "paymentId", "PaymentId");
  host("POST", `/v1/storefront/payments/${oPid}/manual-evidence`, { cartId: oSess.cartId, transferReference: `TRK-R12-O-${Date.now()}`, proofMediaAssetId: null }, oSess.guest);
  host("POST", `/v1/admin/orders/${oCheckout}/operations`, { code: "confirm_deposit", idempotencyKey: randomUUID().replaceAll("-", "") }, admin());
  waitPaid(oCheckout, oPid);
  const oOld = forceRelease(oCheckout);
  const oRec = host("POST", `/v1/admin/orders/${oCheckout}/operations`, { code: "recover_inventory_reservation", idempotencyKey: randomUUID().replaceAll("-", ""), reason: "r12-O" }, admin());
  const oNew = resv(oCheckout);
  note("O-historical-recover", {
    ok: oRec.status < 400 && oNew?.id && oNew.id !== oOld && sql(`SELECT status FROM inventory.reservations WHERE reservation_id='${oOld}'`) === "Released",
    recover: pick(oRec.json, "outcome", "Outcome"), oldRes: oOld, newRes: oNew?.id,
  });

  const pGet = host("GET", `/v1/storefront/payments/${aPid}?cartId=${aSess.cartId}`, undefined, aSess.guest);
  const pNaked = host("GET", `/v1/storefront/payments/${aPid}`, undefined, {});
  const pWrong = host("GET", `/v1/storefront/payments/${aPid}?cartId=${aSess.cartId}`, undefined, { "X-Tooba-Guest-Secret": "ffffffff-ffff-4fff-8fff-ffffffffffff" });
  const empty = host("POST", "/v1/storefront/cart");
  const emptyId = pick(empty.json, "cartId", "CartId");
  const emptySecret = pick(empty.json, "guestSecret", "GuestSecret");
  const pEmpty = host("GET", `/v1/storefront/payments/${aPid}?cartId=${emptyId}`, undefined, { "X-Tooba-Guest-Secret": emptySecret });
  const pMode = host("POST", `/v1/admin/orders/${aCheckout}/operations`, { code: "ensure_order_supply", mode: "EnsurePaidDurable", idempotencyKey: randomUUID().replaceAll("-", "") }, {});
  note("P-result-and-ownership", {
    ok: pGet.status === 200 && String(pick(pGet.json, "status", "Status")).toLowerCase() === "succeeded" && pNaked.status >= 400 && pWrong.status >= 400 && pEmpty.status >= 400 && pMode.status >= 400,
    owned: pGet.status, naked: pNaked.status, wrongGuest: pWrong.status, emptyCart: pEmpty.status, clientEnsure: pMode.status,
  });

  const sGet = host("GET", "/v1/admin/settings/hold-policy", undefined, admin());
  const sPut = host("PUT", "/v1/admin/settings/hold-policy", {
    cartPersistenceHours: 168, onlinePaymentHoldHours: 3, manualPaymentInitialHoldHours: 4, manualPaymentReviewHoldHours: 24,
    methods: [{ providerCode: "fake", onlinePaymentHoldHours: 5, manualPaymentInitialHoldHours: null, manualPaymentReviewHoldHours: null }],
  }, admin());
  host("PUT", "/v1/admin/settings/hold-policy", {
    cartPersistenceHours: null, onlinePaymentHoldHours: null, manualPaymentInitialHoldHours: null, manualPaymentReviewHoldHours: null,
    methods: [{ providerCode: "fake", onlinePaymentHoldHours: null, manualPaymentInitialHoldHours: null, manualPaymentReviewHoldHours: null }],
  }, admin());
  note("settings-roundtrip", { ok: sGet.status === 200 && sPut.status === 200, get: sGet.status, put: sPut.status });
} catch (err) {
  note("crash", { ok: false, error: String(err && err.message ? err.message : err) });
} finally {
  restoreOffers();
  writeFileSync("docs/evidence/TB-P10-T004/r12-runtime-raw.json", JSON.stringify(out, null, 2));
  console.log(JSON.stringify({ ok: out.ok, steps: out.steps.length }));
  if (!out.ok) process.exit(1);
}
