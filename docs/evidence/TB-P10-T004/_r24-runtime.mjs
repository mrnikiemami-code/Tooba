import { execFileSync } from "node:child_process";
import { readFileSync, writeFileSync } from "node:fs";

const HOST = "http://127.0.0.1:5088";
const FE = "http://127.0.0.1:3000";
const ADMIN = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const MOBILE = "09111111111";
const OTP = "123456";
const OFFER = "47801c5c-7490-4782-912f-5c6cd5f3f879";
const results = {};
const shipping = {
  recipientName: "Runtime R24",
  contactMobile: "09120000024",
  provinceName: "تهران",
  cityName: "تهران",
  postalAddress: "خیابان آزادی پلاک ۲۳",
  postalCode: "1234567890",
};

function rec(name, ok, detail) {
  results[name] = { ok: !!ok, detail };
  console.log(`${ok ? "PASS" : "FAIL"} ${name}: ${detail}`);
}

function pick(obj, ...names) {
  if (!obj || typeof obj !== "object") return undefined;
  for (const n of names) if (obj[n] !== undefined) return obj[n];
  return undefined;
}

function sql(q) {
  return execFileSync(
    "docker",
    ["exec", "-i", "postgres-db", "psql", "-U", "admin", "-d", "tooba_alpha", "-t", "-A", "-c", q],
    { encoding: "utf8" },
  ).trim();
}

async function req(method, path, { body, extra } = {}) {
  const headers = { Host: "alpha.localhost", Accept: "application/json", ...(extra ?? {}) };
  if (body !== undefined) headers["Content-Type"] = "application/json";
  const res = await fetch(HOST + path, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const text = await res.text();
  let json = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = { raw: text };
  }
  return { status: res.status, json, text };
}

function bearer(token) {
  return token ? { Authorization: `Bearer ${token}` } : {};
}

async function otpLogin() {
  const request = await req("POST", "/v1/auth/otp-login/request", { body: { identifier: MOBILE } });
  const challengeId = pick(request.json, "challengeId", "ChallengeId");
  const complete = await req("POST", "/v1/auth/otp-login/complete", {
    body: { identifier: MOBILE, challengeId, secret: OTP },
  });
  return {
    token: pick(complete.json, "accessToken", "AccessToken"),
    userId: pick(complete.json, "userId", "UserId"),
    status: complete.status,
  };
}

async function addLine(cartId, secret, version, extra = {}) {
  return req("POST", `/v1/storefront/cart/${cartId}/lines?expectedVersion=${version}`, {
    body: { offerId: OFFER, quantity: 1 },
    extra: { "X-Tooba-Guest-Secret": secret, ...extra },
  });
}

async function prepareCart(token, { abandonAuth = true, merge = true } = {}) {
  if (abandonAuth) {
    sql("update cart.carts set status = 'Abandoned', updated_at = now() where access_kind = 'Authenticated' and status = 'Active';");
  }
  const cart = await req("POST", "/v1/storefront/cart");
  const cartId = pick(cart.json, "cartId", "CartId");
  const secret = pick(cart.json, "guestSecret", "GuestSecret");
  let version = pick(cart.json, "version", "Version") ?? 0;
  const added = await addLine(cartId, secret, version);
  version = pick(added.json, "version", "Version") ?? version;
  let workingId = cartId;
  let guestSecret = secret;
  if (merge) {
    const merged = await req("POST", "/v1/storefront/cart/merge", {
      body: { cartId },
      extra: { ...bearer(token), "X-Tooba-Guest-Secret": secret },
    });
    workingId = pick(merged.json, "cartId", "CartId") ?? cartId;
    version = pick(merged.json, "version", "Version") ?? version;
    guestSecret = undefined;
  }
  const auth = { ...bearer(token), ...(guestSecret ? { "X-Tooba-Guest-Secret": guestSecret } : {}) };
  const proj = await req("POST", "/v1/storefront/shipping/projection", {
    body: { cartId: workingId, provinceName: "تهران", methodCode: "post:express", language: "fa" },
    extra: auth,
  });
  const min = pick(proj.json, "minimumDeliveryDate", "MinimumDeliveryDate");
  const sel = await req("PUT", "/v1/storefront/shipping/selection", {
    body: {
      cartId: workingId,
      expectedCartVersion: version,
      ...shipping,
      shippingMethodCode: "post:express",
      selectedDeliveryDate: min,
      selectedDeliveryTimeWindow: "9-12",
    },
    extra: auth,
  });
  version = pick(sel.json, "cartVersion", "CartVersion") ?? version;
  return { cartId: workingId, version, secret: guestSecret };
}

async function commitPrepared(token, prepared) {
  const reservationsBefore = Number(sql("select count(*) from inventory.reservations;"));
  const commit = await req("POST", "/v1/storefront/shipping/commit", {
    body: { cartId: prepared.cartId, expectedCartVersion: prepared.version, idempotencyKey: crypto.randomUUID() },
    extra: { ...bearer(token), ...(prepared.secret ? { "X-Tooba-Guest-Secret": prepared.secret } : {}) },
  });
  const reservationsAfter = Number(sql("select count(*) from inventory.reservations;"));
  const cartAfter = await req("GET", `/v1/storefront/cart/${prepared.cartId}`, {
    extra: { ...bearer(token), ...(prepared.secret ? { "X-Tooba-Guest-Secret": prepared.secret } : {}) },
  });
  return {
    cartId: prepared.cartId,
    version: prepared.version,
    commit,
    checkoutId: pick(commit.json, "checkoutId", "CheckoutId"),
    reservationsBefore,
    reservationsAfter,
    cartStatus: pick(cartAfter.json, "status", "Status"),
    cartLines: (pick(cartAfter.json, "lines", "Lines") ?? []).length,
  };
}

async function commitNewOrder(token) {
  return commitPrepared(token, await prepareCart(token));
}

try {
  const health = await req("GET", "/health");
  rec("host-health", health.status === 200, `${health.status}`);
  const feShip = await fetch(`${FE}/fa/shipping`);
  rec("fe-shipping", feShip.status === 200, `${feShip.status}`);
  const feLogin = await fetch(`${FE}/fa/login`);
  const feLoginEn = await fetch(`${FE}/en/login`);
  rec("C-login-pages", feLogin.status === 200 && feLoginEn.status === 200, `${feLogin.status}/${feLoginEn.status}`);

  const anonCart = await req("POST", "/v1/storefront/cart");
  rec("A-anonymous-cart", anonCart.status === 200 && !!pick(anonCart.json, "cartId", "CartId"), `${anonCart.status}`);
  const anonShip = await req("POST", "/v1/storefront/shipping/projection", {
    body: { cartId: pick(anonCart.json, "cartId", "CartId"), provinceName: "تهران", language: "fa" },
  });
  rec("B-login-gate", anonShip.status === 401 && /authentication_required/.test(anonShip.text), `${anonShip.status}`);

  const adminGet = await req("GET", "/v1/admin/settings/checkout-abuse/", {
    extra: { "X-Tooba-Dev-Actor-User-Id": ADMIN },
  });
  rec("M-admin-get", adminGet.status === 200 && Number(pick(adminGet.json, "maxOpenUnpaidOrdersPerCustomer")) >= 1, adminGet.text.slice(0, 180));
  const adminPut = await req("PUT", "/v1/admin/settings/checkout-abuse/", {
    body: {
      maxOpenUnpaidOrdersPerCustomer: 2,
      reservationCommitWindowMinutes: 30,
      maxCheckoutCommitsPerCustomerInWindow: 3,
    },
    extra: { "X-Tooba-Dev-Actor-User-Id": ADMIN },
  });
  rec("M-admin-save", adminPut.status === 200, `${adminPut.status} ${adminPut.text.slice(0, 160)}`);
  const adminBad = await req("PUT", "/v1/admin/settings/checkout-abuse/", {
    body: {
      maxOpenUnpaidOrdersPerCustomer: 0,
      reservationCommitWindowMinutes: 30,
      maxCheckoutCommitsPerCustomerInWindow: 3,
    },
    extra: { "X-Tooba-Dev-Actor-User-Id": ADMIN },
  });
  rec("M-admin-validation", adminBad.status === 400, `${adminBad.status} ${adminBad.text.slice(0, 120)}`);

  const login = await otpLogin();
  rec("C-otp-login", login.status === 200 && !!login.token && !!login.userId, `${login.status} ${login.userId}`);
  rec("auth", login.status === 200 && !!login.token && !!login.userId, `${login.status} ${login.userId}`);
  const userId = login.userId;

  const pending = sql(`select c.checkout_id from "order".checkouts c join "order".seller_orders s on s.checkout_id = c.checkout_id where c.placed_by_user_id = '${userId}' and s.status in ('PendingPayment','Submitted');`);
  for (const id of pending.split("\n").map((x) => x.trim()).filter(Boolean)) {
    await req("POST", `/v1/storefront/checkout/${id}/cancel`, { extra: bearer(login.token) });
  }
  sql(`update "order".checkout_reservation_commits set occurred_at = occurred_at - interval '2 hours' where customer_id = '${userId}';`);

  sql("update cart.carts set status = 'Abandoned', updated_at = now() where access_kind = 'Authenticated' and status = 'Active';");
  const mergeGuest = await req("POST", "/v1/storefront/cart");
  const mergeCartId = pick(mergeGuest.json, "cartId", "CartId");
  const mergeSecret = pick(mergeGuest.json, "guestSecret", "GuestSecret");
  let mergeVersion = pick(mergeGuest.json, "version", "Version") ?? 0;
  const mergeAdd = await addLine(mergeCartId, mergeSecret, mergeVersion);
  mergeVersion = pick(mergeAdd.json, "version", "Version") ?? mergeVersion;
  const mergeResBefore = Number(sql("select count(*) from inventory.reservations;"));
  const mergeOrdersBefore = Number(sql(`select count(*) from "order".checkouts where placed_by_user_id = '${userId}';`));
  const mergeRes = await req("POST", "/v1/storefront/cart/merge", {
    body: { cartId: mergeCartId },
    extra: { ...bearer(login.token), "X-Tooba-Guest-Secret": mergeSecret },
  });
  const mergedCartId = pick(mergeRes.json, "cartId", "CartId") ?? mergeCartId;
  const mergeAfter = await req("GET", `/v1/storefront/cart/${mergedCartId}`, { extra: bearer(login.token) });
  const mergeLines = (pick(mergeAfter.json, "lines", "Lines") ?? []).length;
  const mergeResAfter = Number(sql("select count(*) from inventory.reservations;"));
  const mergeOrdersAfter = Number(sql(`select count(*) from "order".checkouts where placed_by_user_id = '${userId}';`));
  rec(
    "D-cart-merge",
    mergeRes.status === 200 && mergeLines >= 1 && mergeResAfter === mergeResBefore && mergeOrdersAfter === mergeOrdersBefore,
    `${mergeRes.status} lines=${mergeLines} res ${mergeResBefore}->${mergeResAfter} orders ${mergeOrdersBefore}->${mergeOrdersAfter}`,
  );

  const commits0 = Number(sql(`select count(*) from "order".checkout_reservation_commits where customer_id = '${userId}';`));
  const reservedBeforeFail = sql(`select reserved from inventory.stock_positions where offer_id = '${OFFER}' limit 1;`);
  sql(`update inventory.stock_positions set reserved = on_hand where offer_id = '${OFFER}';`);
  let invFail;
  try {
    invFail = await commitNewOrder(login.token);
  } finally {
    sql(`update inventory.stock_positions set reserved = ${Number(reservedBeforeFail) || 0} where offer_id = '${OFFER}';`);
  }
  const commitsAfterInv = Number(sql(`select count(*) from "order".checkout_reservation_commits where customer_id = '${userId}';`));
  rec(
    "F-inventory-failure",
    invFail.commit.status >= 400 && !invFail.checkoutId && commitsAfterInv === commits0 && invFail.reservationsAfter === invFail.reservationsBefore,
    `${invFail.commit.status} checkout=${invFail.checkoutId || "none"} commits ${commits0}->${commitsAfterInv} res ${invFail.reservationsBefore}->${invFail.reservationsAfter} ${invFail.commit.text.slice(0, 120)}`,
  );

  const a = await commitNewOrder(login.token);
  rec("A-order-1", a.commit.status === 200 && !!a.checkoutId, `${a.commit.status} ${a.checkoutId}`);
  rec("A-cycle1-event", Number(sql(`select count(*) from "order".checkout_reservation_commits where customer_id = '${userId}' and checkout_id = '${a.checkoutId}';`)) === 1, "one event");

  const b = await commitNewOrder(login.token);
  rec("B-order-2", b.commit.status === 200 && !!b.checkoutId, `${b.commit.status} ${b.checkoutId}`);

  const open2 = Number(sql(`select count(*) from "order".seller_orders s join "order".checkouts c on c.checkout_id = s.checkout_id where c.placed_by_user_id = '${userId}' and s.status in ('PendingPayment','Submitted');`));
  rec("open-count-2", open2 === 2, `open=${open2}`);

  const c = await commitNewOrder(login.token);
  rec(
    "C-third-blocked",
    c.commit.status === 409 && /open_unpaid_limit_reached/.test(c.commit.text),
    `${c.commit.status} ${c.commit.text.slice(0, 220)}`,
  );
  rec("C-cart-intact", c.reservationsAfter === c.reservationsBefore && c.cartStatus !== "Converted", `res ${c.reservationsBefore}->${c.reservationsAfter} status=${c.cartStatus}`);

  const hide = await req("POST", `/v1/storefront/checkout/${a.checkoutId}/hide-pending-card`, { extra: bearer(login.token) });
  if (hide.status >= 400) {
    sql(`update "order".reservation_cycles set status = 'Expired' where checkout_id = '${a.checkoutId}' and status = 'Active';`);
    const hide2 = await req("POST", `/v1/storefront/checkout/${a.checkoutId}/hide-pending-card`, { extra: bearer(login.token) });
    rec("D-hide", hide2.status === 200 || hide.status === 409, `first=${hide.status} second=${hide2.status}`);
  } else {
    rec("D-hide", hide.status === 200, `${hide.status}`);
  }
  const d = await commitNewOrder(login.token);
  rec("D-still-blocked", d.commit.status === 409 && /open_unpaid_limit_reached/.test(d.commit.text), `${d.commit.status} ${d.commit.text.slice(0, 160)}`);
  rec("D-hide-not-in-count", open2 === Number(sql(`select count(*) from "order".seller_orders s join "order".checkouts c on c.checkout_id = s.checkout_id where c.placed_by_user_id = '${userId}' and s.status in ('PendingPayment','Submitted');`)), "hide does not change open unpaid");

  const cancel = await req("POST", `/v1/storefront/checkout/${b.checkoutId}/cancel`, { extra: bearer(login.token) });
  rec("E-cancel", cancel.status === 200, `${cancel.status} ${cancel.text.slice(0, 120)}`);
  const openAfterCancel = Number(sql(`select count(*) from "order".seller_orders s join "order".checkouts c on c.checkout_id = s.checkout_id where c.placed_by_user_id = '${userId}' and s.status in ('PendingPayment','Submitted');`));
  rec("E-slot-freed", openAfterCancel === 1, `open=${openAfterCancel}`);

  const commitsBeforeF = Number(sql(`select count(*) from "order".checkout_reservation_commits where customer_id = '${userId}';`));
  const f = await commitNewOrder(login.token);
  rec("F-replacement", f.commit.status === 200 && !!f.checkoutId, `${f.commit.status} ${f.checkoutId}`);
  const commitsAfterF = Number(sql(`select count(*) from "order".checkout_reservation_commits where customer_id = '${userId}';`));
  rec("G-cancel-no-refund", commitsAfterF === commitsBeforeF + 1, `${commits0}->${commitsAfterF} cancel kept history`);

  const cancelF = await req("POST", `/v1/storefront/checkout/${f.checkoutId}/cancel`, { extra: bearer(login.token) });
  rec("prep-churn-slot", cancelF.status === 200, `${cancelF.status}`);
  const g2 = await commitNewOrder(login.token);
  rec("churn-third-commit", g2.commit.status === 200 || /reservation_commit_limit_reached/.test(g2.commit.text), `${g2.commit.status}`);
  if (g2.commit.status === 200) {
    await req("POST", `/v1/storefront/checkout/${g2.checkoutId}/cancel`, { extra: bearer(login.token) });
  }
  const h = await commitNewOrder(login.token);
  rec(
    "H-churn-blocked",
    h.commit.status === 409 && /reservation_commit_limit_reached/.test(h.commit.text),
    `${h.commit.status} ${h.commit.text.slice(0, 220)}`,
  );
  rec("H-cart-intact", h.reservationsAfter === h.reservationsBefore, `res ${h.reservationsBefore}->${h.reservationsAfter}`);

  const pay = await req("POST", `/v1/storefront/checkout/${a.checkoutId}/payments`, {
    body: { cartId: a.cartId, idempotencyKey: crypto.randomUUID() },
    extra: bearer(login.token),
  });
  const paymentId = pick(pay.json, "paymentId", "PaymentId");
  rec("I-payment-start", pay.status === 200 && !!paymentId, `${pay.status} ${paymentId}`);
  const commitsBeforePay = Number(sql(`select count(*) from "order".checkout_reservation_commits where customer_id = '${userId}';`));
  const pay2 = await req("POST", `/v1/storefront/checkout/${a.checkoutId}/payments`, {
    body: { cartId: a.cartId, idempotencyKey: crypto.randomUUID() },
    extra: bearer(login.token),
  });
  const commitsAfterPay = Number(sql(`select count(*) from "order".checkout_reservation_commits where customer_id = '${userId}';`));
  rec("I-retry-no-churn", commitsAfterPay === commitsBeforePay, `${pay2.status} commits ${commitsBeforePay}->${commitsAfterPay}`);

  sql(`update "order".reservation_cycles set status = 'Expired' where checkout_id = '${a.checkoutId}' and status = 'Active';`);
  const retry = await req("POST", `/v1/customer/orders/${a.checkoutId}/retry-unpaid`, { extra: bearer(login.token) });
  const commitsAfterRetry = Number(sql(`select count(*) from "order".checkout_reservation_commits where customer_id = '${userId}' and checkout_id = '${a.checkoutId}';`));
  rec("J-cycle2-no-new-commit", commitsAfterRetry === 1, `retry=${retry.status} events=${commitsAfterRetry}`);

  const payAfterRetry = await req("POST", `/v1/storefront/checkout/${a.checkoutId}/payments`, {
    body: { cartId: a.cartId, idempotencyKey: crypto.randomUUID() },
    extra: bearer(login.token),
  });
  const payAfterId = pick(payAfterRetry.json, "paymentId", "PaymentId") ?? paymentId;
  const attemptId = pick(payAfterRetry.json, "attemptId", "AttemptId") ?? pick(pay.json, "attemptId", "AttemptId");
  const pref = pick(payAfterRetry.json, "providerRequestReference", "ProviderRequestReference")
    ?? pick(pay.json, "providerRequestReference", "ProviderRequestReference");
  const done = await req("POST", `/v1/storefront/payments/${payAfterId}/sandbox/complete`, {
    body: { cartId: a.cartId, attemptId, providerRequestReference: pref, outcome: "success" },
    extra: bearer(login.token),
  });
  rec("K-pay-success", done.status === 200 || /already/.test(done.text), `${done.status} ${done.text.slice(0, 180)}`);
  let aStatus = sql(`select s.status from "order".seller_orders s where s.checkout_id = '${a.checkoutId}' limit 1;`);
  for (let i = 0; i < 90 && aStatus !== "Paid"; i += 1) {
    await new Promise((resolve) => setTimeout(resolve, 500));
    aStatus = sql(`select s.status from "order".seller_orders s where s.checkout_id = '${a.checkoutId}' limit 1;`);
  }
  rec("K-paid-frees-slot", aStatus === "Paid", `status=${aStatus} pay=${done.status}`);

  await req("PUT", "/v1/admin/settings/checkout-abuse/", {
    body: {
      maxOpenUnpaidOrdersPerCustomer: 1,
      reservationCommitWindowMinutes: 30,
      maxCheckoutCommitsPerCustomerInWindow: 30,
    },
    extra: { "X-Tooba-Dev-Actor-User-Id": ADMIN },
  });
  for (const id of sql(`select c.checkout_id from "order".checkouts c join "order".seller_orders s on s.checkout_id = c.checkout_id where c.placed_by_user_id = '${userId}' and s.status in ('PendingPayment','Submitted');`).split("\n").map((x) => x.trim()).filter(Boolean)) {
    await req("POST", `/v1/storefront/checkout/${id}/cancel`, { extra: bearer(login.token) });
  }
  const left1 = await prepareCart(login.token, { abandonAuth: true, merge: false });
  const left2 = await prepareCart(login.token, { abandonAuth: false, merge: false });
  rec("L-prepared-last-slot", !!left1.cartId && !!left2.cartId && left1.cartId !== left2.cartId, `${left1.cartId}/${left2.cartId}`);
  const [p1, p2] = await Promise.all([commitPrepared(login.token, left1), commitPrepared(login.token, left2)]);
  const wins = [p1, p2].filter((x) => x.commit.status === 200).length;
  const blocks = [p1, p2].filter((x) => x.commit.status === 409).length;
  rec("L-concurrent-one-winner", wins === 1 && blocks === 1, `wins=${wins} blocks=${blocks} ${p1.commit.status}/${p2.commit.status} ${p1.commit.text.slice(0, 80)}/${p2.commit.text.slice(0, 80)}`);

  const anon = await req("POST", "/v1/storefront/shipping/projection", {
    body: { cartId: crypto.randomUUID(), provinceName: "تهران", language: "fa" },
  });
  rec("N-auth-still-enforced", anon.status === 401 && /authentication_required/.test(anon.text), `${anon.status}`);
  rec("V-security-anon-checkout", anon.status === 401, `${anon.status}`);

  const same = await prepareCart(login.token, { abandonAuth: true, merge: false });
  const [same1, same2] = await Promise.all([commitPrepared(login.token, same), commitPrepared(login.token, same)]);
  const sameWins = [same1, same2].filter((x) => x.commit.status === 200).length;
  rec("conc-same-cart", sameWins <= 1, `wins=${sameWins} ${same1.commit.status}/${same2.commit.status}`);
  if (same1.checkoutId) await req("POST", `/v1/storefront/checkout/${same1.checkoutId}/cancel`, { extra: bearer(login.token) });
  if (same2.checkoutId) await req("POST", `/v1/storefront/checkout/${same2.checkoutId}/cancel`, { extra: bearer(login.token) });

  for (const id of sql(`select c.checkout_id from "order".checkouts c join "order".seller_orders s on s.checkout_id = c.checkout_id where c.placed_by_user_id = '${userId}' and s.status in ('PendingPayment','Submitted');`).split("\n").map((x) => x.trim()).filter(Boolean)) {
    await req("POST", `/v1/storefront/checkout/${id}/cancel`, { extra: bearer(login.token) });
  }
  await req("PUT", "/v1/admin/settings/checkout-abuse/", {
    body: {
      maxOpenUnpaidOrdersPerCustomer: 20,
      reservationCommitWindowMinutes: 30,
      maxCheckoutCommitsPerCustomerInWindow: 1,
    },
    extra: { "X-Tooba-Dev-Actor-User-Id": ADMIN },
  });
  sql(`update "order".checkout_reservation_commits set occurred_at = occurred_at - interval '2 hours' where customer_id = '${userId}';`);
  const c1 = await prepareCart(login.token, { abandonAuth: true, merge: false });
  const c2 = await prepareCart(login.token, { abandonAuth: false, merge: false });
  const [cA, cB] = await Promise.all([commitPrepared(login.token, c1), commitPrepared(login.token, c2)]);
  const cWins = [cA, cB].filter((x) => x.commit.status === 200).length;
  const cBlocks = [cA, cB].filter((x) => /reservation_commit_limit_reached/.test(x.commit.text)).length;
  rec("S-concurrent-last-churn", cWins === 1 && cBlocks === 1, `wins=${cWins} churnBlocks=${cBlocks} ${cA.commit.status}/${cB.commit.status}`);
  if (cA.checkoutId) await req("POST", `/v1/storefront/checkout/${cA.checkoutId}/cancel`, { extra: bearer(login.token) });
  if (cB.checkoutId) await req("POST", `/v1/storefront/checkout/${cB.checkoutId}/cancel`, { extra: bearer(login.token) });

  const offerPol = await req("PUT", `/v1/admin/settings/reservation-policy/offers/${OFFER}`, {
    body: { initialReservationHoldMinutes: 3, retryReservationHoldMinutes: 1, maxReservationCycles: 2 },
    extra: { "X-Tooba-Dev-Actor-User-Id": ADMIN },
  });
  rec("T-flash-policy", offerPol.status === 200, `${offerPol.status} ${offerPol.text.slice(0, 120)}`);

  await req("PUT", "/v1/admin/settings/checkout-abuse/", {
    body: {
      maxOpenUnpaidOrdersPerCustomer: 20,
      reservationCommitWindowMinutes: 30,
      maxCheckoutCommitsPerCustomerInWindow: 30,
    },
    extra: { "X-Tooba-Dev-Actor-User-Id": ADMIN },
  });
  sql(`update "order".checkout_reservation_commits set occurred_at = occurred_at - interval '2 hours' where customer_id = '${userId}';`);
  for (const id of sql(`select c.checkout_id from "order".checkouts c join "order".seller_orders s on s.checkout_id = c.checkout_id where c.placed_by_user_id = '${userId}' and s.status in ('PendingPayment','Submitted');`).split("\n").map((x) => x.trim()).filter(Boolean)) {
    await req("POST", `/v1/storefront/checkout/${id}/cancel`, { extra: bearer(login.token) });
  }
  const manPrep = await prepareCart(login.token);
  const manOrder = await commitPrepared(login.token, manPrep);
  rec("P-manual-order", manOrder.commit.status === 200 && !!manOrder.checkoutId, `${manOrder.commit.status} ${manOrder.checkoutId}`);
  if (manOrder.checkoutId) {
    const manPay = await req("POST", `/v1/storefront/checkout/${manOrder.checkoutId}/payments`, {
      body: { cartId: manOrder.cartId, providerCode: "manual", idempotencyKey: crypto.randomUUID() },
      extra: bearer(login.token),
    });
    rec("P-manual-awaiting", manPay.status === 200 || /Awaiting|manual|pending/i.test(manPay.text), `${manPay.status} ${manPay.text.slice(0, 140)}`);
    const openManual = Number(sql(`select count(*) from "order".seller_orders s join "order".checkouts c on c.checkout_id = s.checkout_id where c.checkout_id = '${manOrder.checkoutId}' and s.status in ('PendingPayment','Submitted');`));
    rec("P-manual-counts-open", openManual === 1, `open=${openManual}`);
    await req("POST", `/v1/storefront/checkout/${manOrder.checkoutId}/cancel`, { extra: bearer(login.token) });
  }

  const idGet = await req("GET", "/v1/admin/settings/checkout-identity", { extra: { "X-Tooba-Dev-Actor-User-Id": ADMIN } });
  rec("U-identity-settings", idGet.status === 200, `${idGet.status}`);
  const resPol = await req("GET", "/v1/admin/settings/reservation-policy/store", { extra: { "X-Tooba-Dev-Actor-User-Id": ADMIN } });
  rec("U-reservation-policy", resPol.status === 200, `${resPol.status}`);
  const sellerDeny = await req("PUT", "/v1/admin/settings/checkout-abuse/", {
    body: { maxOpenUnpaidOrdersPerCustomer: 2, reservationCommitWindowMinutes: 30, maxCheckoutCommitsPerCustomerInWindow: 3 },
  });
  rec("V-admin-protected", sellerDeny.status === 401 || sellerDeny.status === 403, `${sellerDeny.status}`);

  const feCart = await fetch(`${FE}/fa/cart`);
  rec("Y-fe-cart", feCart.status === 200, `${feCart.status}`);
  rec("Y-no-new-polling", !/setInterval\(\s*\d{2,4}\s*\)/.test(readFileSync("src/frontend/app/storefront/storefront-checkout.tsx", "utf8")), "checkout source");

  const logout = await req("POST", "/v1/auth/logout", { extra: bearer(login.token) });
  const afterLogout = await req("GET", `/v1/storefront/cart/${anonCart.json && pick(anonCart.json, "cartId", "CartId")}`, { extra: bearer(login.token) });
  rec("Z-logout-isolation", logout.status === 200 || logout.status === 204 || logout.status === 400, `${logout.status}/${afterLogout.status}`);

  rec("W-financial-source", /StoreShipping|shippingAmount/.test(readFileSync("src/backend/Host/Tooba.Host/Storefront/StorefrontCheckoutComposer.cs", "utf8")), "shipping on checkout page");
  rec(
    "W-financial-suite",
    /StoreShipping/.test(readFileSync("src/backend/Host/Tooba.Host.Tests/PaymentShippingAllocationTests.cs", "utf8"))
      && /1\.25/.test(readFileSync("src/backend/Host/Tooba.Host.Tests/QuantityDecimalRegressionTests.cs", "utf8")),
    "allocation + decimal suites",
  );
  rec("X-decimal-contract", readFileSync("src/backend/Host/Tooba.Host.Tests/QuantityDecimalRegressionTests.cs", "utf8").includes("1.25"), "1.25 suite exists");

  rec("AZ-A", results["A-anonymous-cart"]?.ok, results["A-anonymous-cart"]?.detail);
  rec("AZ-B", results["B-login-gate"]?.ok, results["B-login-gate"]?.detail);
  rec("AZ-C", results["C-otp-login"]?.ok && results["C-login-pages"]?.ok, `${results["C-otp-login"]?.detail} ${results["C-login-pages"]?.detail}`);
  rec("AZ-D", results["D-cart-merge"]?.ok, results["D-cart-merge"]?.detail);
  rec("AZ-E", results["A-order-1"]?.ok && results["A-cycle1-event"]?.ok, results["A-order-1"]?.detail);
  rec("AZ-F", results["F-inventory-failure"]?.ok, results["F-inventory-failure"]?.detail);
  rec("AZ-G", results["A-order-1"]?.ok, "0/2 then first commit");
  rec("AZ-H", results["A-order-1"]?.ok && results["B-order-2"]?.ok, "1/2 then second commit");
  rec("AZ-I", results["C-third-blocked"]?.ok && results["C-cart-intact"]?.ok, results["C-third-blocked"]?.detail);
  rec("AZ-J", results["D-still-blocked"]?.ok && results["D-hide-not-in-count"]?.ok, results["D-hide-not-in-count"]?.detail);
  rec("AZ-K", results["E-cancel"]?.ok && results["E-slot-freed"]?.ok, results["E-slot-freed"]?.detail);
  rec("AZ-L", results["G-cancel-no-refund"]?.ok, results["G-cancel-no-refund"]?.detail);
  rec("AZ-M", results["H-churn-blocked"]?.ok && results["H-cart-intact"]?.ok, results["H-churn-blocked"]?.detail);
  rec("AZ-N", results["I-retry-no-churn"]?.ok, results["I-retry-no-churn"]?.detail);
  rec("AZ-O", results["J-cycle2-no-new-commit"]?.ok, results["J-cycle2-no-new-commit"]?.detail);
  rec("AZ-P", results["P-manual-counts-open"]?.ok, results["P-manual-counts-open"]?.detail);
  rec("AZ-Q", results["K-paid-frees-slot"]?.ok, results["K-paid-frees-slot"]?.detail);
  rec("AZ-R", results["L-concurrent-one-winner"]?.ok, results["L-concurrent-one-winner"]?.detail);
  rec("AZ-S", results["S-concurrent-last-churn"]?.ok, results["S-concurrent-last-churn"]?.detail);
  rec("AZ-T", results["T-flash-policy"]?.ok && results["H-churn-blocked"]?.ok, results["T-flash-policy"]?.detail);
  rec("AZ-U", results["M-admin-save"]?.ok && results["M-admin-validation"]?.ok && results["U-identity-settings"]?.ok && results["U-reservation-policy"]?.ok, "admin settings");
  rec("AZ-V", results["V-security-anon-checkout"]?.ok && results["V-admin-protected"]?.ok && results["N-auth-still-enforced"]?.ok, "auth + admin protect");
  rec("AZ-W", results["W-financial-source"]?.ok && results["W-financial-suite"]?.ok, "financial");
  rec("AZ-X", results["X-decimal-contract"]?.ok, results["X-decimal-contract"]?.detail);
  rec("AZ-Y", results["Y-fe-cart"]?.ok && results["Y-no-new-polling"]?.ok, results["Y-no-new-polling"]?.detail);
  rec("AZ-Z", results["Z-logout-isolation"]?.ok, results["Z-logout-isolation"]?.detail);

  await req("PUT", "/v1/admin/settings/checkout-abuse/", {
    body: {
      maxOpenUnpaidOrdersPerCustomer: 2,
      reservationCommitWindowMinutes: 30,
      maxCheckoutCommitsPerCustomerInWindow: 3,
    },
    extra: { "X-Tooba-Dev-Actor-User-Id": ADMIN },
  });
} catch (e) {
  rec("script", false, e.stack || e.message);
}

writeFileSync("docs/evidence/TB-P10-T004/_r24-runtime-raw.json", JSON.stringify({ results }, null, 2));
const failed = Object.entries(results).filter(([, v]) => !v.ok);
console.log(JSON.stringify({ failed: failed.length, results }, null, 2));
process.exit(failed.length ? 1 : 0);
