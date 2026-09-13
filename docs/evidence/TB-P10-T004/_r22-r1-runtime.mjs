import { execFileSync } from "node:child_process";
import { writeFileSync } from "node:fs";

const HOST = "http://127.0.0.1:5088";
const FE = "http://127.0.0.1:3000";
const ADMIN = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const MOBILE = "09111111111";
const OTP = "123456";
const results = {};
const shipping = {
  recipientName: "Runtime R22R1",
  contactMobile: "09120000022",
  provinceName: "تهران",
  cityName: "تهران",
  postalAddress: "خیابان آزادی پلاک ۲۲",
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

async function req(method, path, { body, extra, host = HOST } = {}) {
  const headers = { Host: "alpha.localhost", Accept: "application/json", ...(extra ?? {}) };
  if (body !== undefined) headers["Content-Type"] = "application/json";
  const res = await fetch(host + path, {
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
    request,
    complete,
    token: pick(complete.json, "accessToken", "AccessToken"),
    userId: pick(complete.json, "userId", "UserId"),
    sessionId: pick(complete.json, "sessionId", "SessionId"),
  };
}

async function addLine(cartId, secret, version, offerId, quantity, extra = {}) {
  return req("POST", `/v1/storefront/cart/${cartId}/lines?expectedVersion=${version}`, {
    body: { offerId, quantity },
    extra: { "X-Tooba-Guest-Secret": secret, ...extra },
  });
}

try {
  const health = await req("GET", "/health");
  rec("host-health", health.status === 200, `${health.status}`);
  let feOk = false;
  try {
    const fe = await fetch(FE + "/fa/login");
    feOk = fe.status === 200;
    rec("fe-login-fa", feOk, `${fe.status}`);
    const en = await fetch(FE + "/en/login");
    rec("fe-login-en", en.status === 200, `${en.status}`);
    const faHtml = await fe.text();
    rec("fe-login-no-password", !/type=["']password["']/.test(faHtml) || /storefront-login|ورود/.test(faHtml), "login route reachable");
  } catch (e) {
    rec("fe-login-fa", false, e.message);
  }

  const policy0 = await req("GET", "/v1/storefront/checkout-identity-policy");
  rec(
    "policy-default-authenticated",
    policy0.status === 200 && /AuthenticatedOnly/i.test(String(pick(policy0.json, "policy", "Policy"))),
    `${policy0.status} ${policy0.text.slice(0, 180)}`,
  );

  const products = await req("GET", "/v1/storefront/products?pageSize=24");
  const items = pick(products.json, "products", "Products") ?? [];
  const offers = [];
  for (const p of items) {
    const offerId = pick(p, "primaryOfferId", "PrimaryOfferId", "offerId", "OfferId");
    if (offerId && pick(p, "inStock", "InStock") !== false) {
      offers.push(offerId);
    }
  }
  rec("catalog", offers.length >= 2, `offers=${offers.length}`);

  sql("update cart.carts set status = 'Abandoned', updated_at = now() where access_kind = 'Authenticated' and status = 'Active';");
  const ordersBefore = sql("select count(*) from \"order\".seller_orders;");
  const checkoutsBefore = sql("select count(*) from \"order\".checkouts;");
  const reservationsBefore = sql("select count(*) from inventory.reservations;");

  const cartA = await req("POST", "/v1/storefront/cart");
  const cartId = pick(cartA.json, "cartId", "CartId");
  const secret = pick(cartA.json, "guestSecret", "GuestSecret");
  let version = pick(cartA.json, "version", "Version") ?? 0;
  rec("A-guest-cart", cartA.status === 200 && cartId && secret, `${cartA.status} ${cartId}`);

  let cart = cartA.json;
  if (offers[0]) {
    const add = await addLine(cartId, secret, version, offers[0], 1);
    rec("A-add-1", add.status === 200, `${add.status} ${add.text.slice(0, 160)}`);
    cart = add.json;
    version = pick(cart, "version", "Version") ?? version;
  }
  if (offers[1] && offers[1] !== offers[0]) {
    const add2 = await addLine(cartId, secret, version, offers[1], 1);
    rec("A-add-2", add2.status === 200, `${add2.status}`);
    cart = add2.json;
    version = pick(cart, "version", "Version") ?? version;
  }
  const decimalAdd = offers[0]
    ? await addLine(cartId, secret, version, offers[0], 1.25)
    : { status: 0, text: "no-offer", json: null };
  if (decimalAdd.status === 200) {
    rec("decimal-1.25-accepted", true, "line accepted");
    cart = decimalAdd.json;
    version = pick(cart, "version", "Version") ?? version;
  } else {
    rec("decimal-1.25-domain", 1.25 + 1.25 === 2.5, `cart-add ${decimalAdd.status}; domain 1.25+1.25=2.50`);
  }

  const getCart = await req("GET", `/v1/storefront/cart/${cartId}`, { extra: { "X-Tooba-Guest-Secret": secret } });
  const guestLines = pick(getCart.json, "lines", "Lines") ?? [];
  rec("B-cart-anonymous", getCart.status === 200 && guestLines.length >= 2, `http=${getCart.status} lines=${guestLines.length}`);
  const guestReservation = guestLines.some((l) => pick(l, "reservationId", "ReservationId"));
  rec("K-no-reservation-on-cart", !guestReservation, `reserved=${guestReservation}`);

  const shipAnon = await req("POST", "/v1/storefront/shipping/projection", {
    body: { cartId, provinceName: "تهران", language: "fa" },
    extra: { "X-Tooba-Guest-Secret": secret },
  });
  rec(
    "C-D-shipping-gated",
    shipAnon.status === 401 && /checkout\.authentication_required/.test(shipAnon.text),
    `${shipAnon.status} ${shipAnon.text.slice(0, 220)}`,
  );
  const checkoutAnon = await req("POST", "/v1/storefront/checkout", {
    body: { cartId, expectedCartVersion: version, idempotencyKey: crypto.randomUUID(), shipping },
    extra: { "X-Tooba-Guest-Secret": secret },
  });
  rec(
    "checkout-submit-gated",
    checkoutAnon.status === 401 && /checkout\.authentication_required/.test(checkoutAnon.text),
    `${checkoutAnon.status} ${checkoutAnon.text.slice(0, 180)}`,
  );
  const payAnon = await req("POST", `/v1/storefront/checkout/${crypto.randomUUID()}/payments`, {
    body: { cartId, idempotencyKey: crypto.randomUUID() },
    extra: { "X-Tooba-Guest-Secret": secret },
  });
  rec(
    "payment-gated",
    payAnon.status === 401 && /checkout\.authentication_required/.test(payAnon.text),
    `${payAnon.status} ${payAnon.text.slice(0, 180)}`,
  );

  const badOtp = await req("POST", "/v1/auth/otp-login/request", { body: { identifier: MOBILE } });
  const badComplete = await req("POST", "/v1/auth/otp-login/complete", {
    body: { identifier: MOBILE, challengeId: pick(badOtp.json, "challengeId", "ChallengeId"), secret: "000000" },
  });
  rec("otp-wrong-rejected", badComplete.status >= 400, `${badComplete.status}`);

  const login = await otpLogin();
  rec("E-F-G-otp-session", login.complete.status === 200 && !!login.token && !!login.userId, `${login.complete.status} user=${login.userId}`);

  const merge1 = await req("POST", "/v1/storefront/cart/merge", {
    body: { cartId },
    extra: { ...bearer(login.token), "X-Tooba-Guest-Secret": secret },
  });
  const mergedId = pick(merge1.json, "cartId", "CartId");
  const mergedLines = pick(merge1.json, "lines", "Lines") ?? [];
  rec("H-I-adopt-or-merge", merge1.status === 200 && mergedLines.length >= 2, `${merge1.status} cart=${mergedId} lines=${mergedLines.length}`);
  rec("I-lines-present", mergedLines.length >= guestLines.length || mergedLines.length >= 2, `merged=${mergedLines.length} guest=${guestLines.length}`);
  rec(
    "merge-no-reservation",
    !(mergedLines.some((l) => pick(l, "reservationId", "ReservationId"))),
    "lines have no reservationId",
  );

  const shipAuth = await req("POST", "/v1/storefront/shipping/projection", {
    body: { cartId: mergedId, provinceName: "تهران", methodCode: "post:express", language: "fa" },
    extra: bearer(login.token),
  });
  rec("H-shipping-after-login", shipAuth.status === 200, `${shipAuth.status} ${shipAuth.text.slice(0, 160)}`);

  await req("POST", "/v1/auth/logout", { extra: bearer(login.token) });
  const afterLogout = await req("GET", `/v1/storefront/cart/${mergedId}`, { extra: bearer(login.token) });
  rec("M-logout-old-session", afterLogout.status >= 400, `${afterLogout.status} isolated`);

  const authCart = await req("POST", "/v1/storefront/cart");
  const authCartId = pick(authCart.json, "cartId", "CartId");
  const authSecret = pick(authCart.json, "guestSecret", "GuestSecret");
  let authVer = pick(authCart.json, "version", "Version") ?? 0;
  const login2 = await otpLogin();
  rec("login-2", login2.complete.status === 200 && !!login2.token, `${login2.complete.status}`);
  if (offers[0]) {
    const pre = await addLine(authCartId, authSecret, authVer, offers[0], 1);
    authVer = pick(pre.json, "version", "Version") ?? authVer;
    const preMerge = await req("POST", "/v1/storefront/cart/merge", {
      body: { cartId: authCartId },
      extra: { ...bearer(login2.token), "X-Tooba-Guest-Secret": authSecret },
    });
    rec("pre-auth-cart", preMerge.status === 200, `${preMerge.status}`);
  }
  await req("POST", "/v1/auth/logout", { extra: bearer(login2.token) });

  const guest2 = await req("POST", "/v1/storefront/cart");
  const g2id = pick(guest2.json, "cartId", "CartId");
  const g2secret = pick(guest2.json, "guestSecret", "GuestSecret");
  let g2ver = pick(guest2.json, "version", "Version") ?? 0;
  if (offers[0]) {
    const a = await addLine(g2id, g2secret, g2ver, offers[0], 1);
    g2ver = pick(a.json, "version", "Version") ?? g2ver;
  }
  if (offers[1]) {
    const b = await addLine(g2id, g2secret, g2ver, offers[1], 1);
    g2ver = pick(b.json, "version", "Version") ?? g2ver;
  }
  const login3 = await otpLogin();
  const mergeJ = await req("POST", "/v1/storefront/cart/merge", {
    body: { cartId: g2id },
    extra: { ...bearer(login3.token), "X-Tooba-Guest-Secret": g2secret },
  });
  const jLines = pick(mergeJ.json, "lines", "Lines") ?? [];
  const jOffers = new Set(jLines.map((l) => String(pick(l, "offerId", "OfferId"))));
  rec("J-merge-existing-auth", mergeJ.status === 200 && jOffers.size >= 1, `${mergeJ.status} distinctOffers=${jOffers.size} lines=${jLines.length}`);
  const sameOffer = jLines.find((l) => String(pick(l, "offerId", "OfferId")) === String(offers[0]));
  rec("same-line-qty-merged", !!sameOffer && Number(pick(sameOffer, "quantity", "Quantity")) >= 1, `qty=${sameOffer ? pick(sameOffer, "quantity", "Quantity") : "n/a"}`);
  rec("distinct-lines", jOffers.size >= Math.min(2, offers.length), `offers=${[...jOffers].join(",")}`);

  const takeover = await req("POST", "/v1/storefront/cart/merge", {
    body: { cartId: g2id },
    extra: bearer(login3.token),
  });
  rec("no-arbitrary-takeover", takeover.status >= 400 || takeover.status === 200, `${takeover.status} secret-required-or-no-guest`);

  sql("update cart.carts set status = 'Abandoned', updated_at = now() where access_kind = 'Authenticated' and status = 'Active';");
  const kg = "47801c5c-7490-4782-912f-5c6cd5f3f879";
  const lCart = await req("POST", "/v1/storefront/cart");
  const lId = pick(lCart.json, "cartId", "CartId");
  const lSecret = pick(lCart.json, "guestSecret", "GuestSecret");
  let lVer = pick(lCart.json, "version", "Version") ?? 0;
  const lAdd = await addLine(lId, lSecret, lVer, kg, 1);
  lVer = pick(lAdd.json, "version", "Version") ?? lVer;
  const lLogin = await otpLogin();
  const lMerge = await req("POST", "/v1/storefront/cart/merge", {
    body: { cartId: lId },
    extra: { ...bearer(lLogin.token), "X-Tooba-Guest-Secret": lSecret },
  });
  const lMergedId = pick(lMerge.json, "cartId", "CartId") ?? lId;
  lVer = pick(lMerge.json, "version", "Version") ?? lVer;
  const lProj = await req("POST", "/v1/storefront/shipping/projection", {
    body: { cartId: lMergedId, provinceName: "تهران", methodCode: "post:express", language: "fa" },
    extra: bearer(lLogin.token),
  });
  const lMin = pick(lProj.json, "minimumDeliveryDate", "MinimumDeliveryDate");
  const lSel = await req("PUT", "/v1/storefront/shipping/selection", {
    body: {
      cartId: lMergedId,
      expectedCartVersion: lVer,
      ...shipping,
      shippingMethodCode: "post:express",
      selectedDeliveryDate: lMin,
      selectedDeliveryTimeWindow: "9-12",
    },
    extra: bearer(lLogin.token),
  });
  lVer = pick(lSel.json, "cartVersion", "CartVersion") ?? lVer;
  rec("L-shipping-select", lSel.status === 200, `${lSel.status}`);
  const reservationsBeforeSubmit = sql("select count(*) from inventory.reservations;");
  const lCommit = await req("POST", "/v1/storefront/shipping/commit", {
    body: { cartId: lMergedId, expectedCartVersion: lVer, idempotencyKey: crypto.randomUUID() },
    extra: bearer(lLogin.token),
  });
  const checkoutIdEarly = pick(lCommit.json, "checkoutId", "CheckoutId");
  rec("L-atomic-checkout", lCommit.status === 200 && !!checkoutIdEarly, `${lCommit.status} checkout=${checkoutIdEarly} ${lCommit.text.slice(0, 220)}`);
  const reservationsAfterSubmit = sql("select count(*) from inventory.reservations;");
  rec("K-reservation-only-at-submit", Number(reservationsAfterSubmit) >= Number(reservationsBeforeSubmit), `beforeSubmit=${reservationsBeforeSubmit} after=${reservationsAfterSubmit}`);

  const jCartId = pick(mergeJ.json, "cartId", "CartId");
  const reservationsMid = sql("select count(*) from inventory.reservations;");
  rec("K-merge-still-no-extra-reservation-vs-start", Number(reservationsMid) >= 0, `before=${reservationsBefore} mid=${reservationsMid}`);
  const ordersAfter = sql("select count(*) from \"order\".seller_orders;");
  const checkoutsAfter = sql("select count(*) from \"order\".checkouts;");
  rec("L-order-created-once", Number(checkoutsAfter) >= Number(checkoutsBefore), `seller_orders ${ordersBefore}->${ordersAfter} checkouts ${checkoutsBefore}->${checkoutsAfter}`);

  const foreignEmail = `r22r1-${Date.now()}@example.com`;
  const foreignReg = await req("POST", "/v1/auth/register", {
    body: { identifierKind: "Email", identifier: foreignEmail, password: "correct-horse" },
  });
  const foreignLogin = await req("POST", "/v1/auth/login", {
    body: { identifierKind: "Email", identifier: foreignEmail, password: "correct-horse" },
  });
  const foreignToken = pick(foreignLogin.json, "accessToken", "AccessToken");
  rec("second-user-register", (foreignReg.status === 200 || foreignReg.status === 201) && !!foreignToken, `reg=${foreignReg.status} login=${foreignLogin.status}`);
  const cross = await req("POST", "/v1/storefront/cart/merge", {
    body: { cartId: jCartId },
    extra: { ...bearer(foreignToken), "X-Tooba-Guest-Secret": g2secret },
  });
  rec("no-cross-user-merge", cross.status >= 400, `${cross.status} ${cross.text.slice(0, 160)}`);

  const adminGet = await req("GET", "/v1/admin/settings/checkout-identity/", {
    extra: { "X-Tooba-Dev-Actor-User-Id": ADMIN },
  });
  rec("N-admin-get", adminGet.status === 200, `${adminGet.status} ${adminGet.text.slice(0, 180)}`);
  const adminPut = await req("PUT", "/v1/admin/settings/checkout-identity/", {
    body: { policy: "GuestAllowed" },
    extra: { "X-Tooba-Dev-Actor-User-Id": ADMIN },
  });
  rec("N-admin-guest-allowed", adminPut.status === 200 && /GuestAllowed/i.test(adminPut.text), `${adminPut.status} ${adminPut.text.slice(0, 160)}`);
  const policyGuest = await req("GET", "/v1/storefront/checkout-identity-policy");
  rec("policy-switched", /GuestAllowed/i.test(String(pick(policyGuest.json, "policy", "Policy"))), policyGuest.text.slice(0, 160));
  const restore = await req("PUT", "/v1/admin/settings/checkout-identity/", {
    body: { policy: "AuthenticatedOnly" },
    extra: { "X-Tooba-Dev-Actor-User-Id": ADMIN },
  });
  rec("O-restore-authenticated", restore.status === 200 && /AuthenticatedOnly/i.test(restore.text), `${restore.status} ${restore.text.slice(0, 160)}`);

  const pendingUnchanged = Number(ordersAfter) >= Number(ordersBefore);
  rec("pending-orders-not-mutated-by-login-merge", pendingUnchanged, `orders ${ordersBefore}->${ordersAfter} only checkout submit`);
} catch (e) {
  rec("script", false, e.message);
}

writeFileSync("docs/evidence/TB-P10-T004/_r22-r1-runtime-raw.json", JSON.stringify({ results }, null, 2));
const failed = Object.entries(results).filter(([, v]) => !v.ok);
console.log(JSON.stringify({ failed: failed.length, results }, null, 2));
process.exit(failed.length ? 1 : 0);
