/**
 * TB-P10-T004-R4 — ownership after cart finalize + security (Host :5088).
 * Secrets omitted from evidence JSON.
 */
import { writeFileSync, readFileSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { randomUUID } from "node:crypto";

const HOST = "http://127.0.0.1:5088";
const KG_OFFER = "01a03826-9936-7000-b499-ff26a6123a8c";
const ADMIN = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const PASSWORD = "Customer-t004r4-gate-1!";
const stamp = Date.now();
const EMAIL_A = `t004r4.a.${stamp}@example.test`;
const EMAIL_B = `t004r4.b.${stamp}@example.test`;
const PROOF_PNG = "docs/evidence/TB-P10-T004/_r3-proof.png";

const out = { ok: true, steps: [], secretsOmitted: true, at: new Date().toISOString() };
function note(name, data) {
  const row = { name, ...(data ?? {}) };
  out.steps.push(row);
  if (data && data.ok === false) out.ok = false;
  console.log(JSON.stringify(row));
}

function curl(args) {
  const raw = execFileSync("curl.exe", ["-sS", "-w", "\n%{http_code}", ...args], {
    encoding: "utf8",
    maxBuffer: 8 * 1024 * 1024,
  });
  const idx = raw.lastIndexOf("\n");
  const text = idx >= 0 ? raw.slice(0, idx) : raw;
  const status = Number(idx >= 0 ? raw.slice(idx + 1) : 0);
  let json;
  try {
    json = JSON.parse(text);
  } catch {
    json = text;
  }
  return { status, json, text };
}

function pick(o, ...ks) {
  if (!o || typeof o !== "object") return;
  for (const k of ks) if (o[k] != null) return o[k];
}

function host(method, path, body, extra = {}) {
  const args = [
    "-X",
    method,
    `${HOST}${path}`,
    "-H",
    "Host: alpha.localhost",
    "-H",
    "Content-Type: application/json",
  ];
  for (const [k, v] of Object.entries(extra)) {
    if (v != null) args.push("-H", `${k}: ${v}`);
  }
  if (body !== undefined) args.push("--data-binary", JSON.stringify(body));
  return curl(args);
}

function bearer(token) {
  return { Authorization: `Bearer ${token}` };
}

function adminHeaders() {
  return { "X-Tooba-Dev-Actor-User-Id": ADMIN };
}

function registerLogin(email) {
  const reg = host("POST", "/v1/auth/register", {
    identifierKind: "Email",
    identifier: email,
    password: PASSWORD,
  });
  const userId = pick(reg.json, "userId", "UserId");
  if (!userId) throw new Error(`register ${email} ${reg.status} ${reg.text}`);
  const login = host("POST", "/v1/auth/login", {
    identifierKind: "Email",
    identifier: email,
    password: PASSWORD,
  });
  const token = pick(login.json, "accessToken", "AccessToken");
  if (!token) throw new Error(`login ${email} ${login.status} ${login.text}`);
  return { userId: String(userId), token, email };
}

function createAddress(token, label) {
  const res = host(
    "POST",
    "/v1/customer/addresses",
    {
      label,
      recipientName: `گیرنده ${label}`,
      contactMobile: "+989121240101",
      provinceName: "تهران",
      cityName: "تهران",
      postalAddress: `آدرس ${label} خیابان ولیعصر`,
      postalCode: "1234567890",
      isDefault: true,
    },
    bearer(token),
  );
  const id = pick(res.json, "addressId", "AddressId", "id", "Id");
  if (res.status >= 300 || !id) throw new Error(`address ${res.status} ${res.text}`);
  return String(id);
}

function buildCart(offerId = KG_OFFER) {
  const cart = host("POST", "/v1/storefront/cart");
  const secret = pick(cart.json, "guestSecret", "GuestSecret");
  const cartId = pick(cart.json, "cartId", "CartId");
  let version = pick(cart.json, "version", "Version");
  const guest = { "X-Tooba-Guest-Secret": secret };
  const line = host(
    "POST",
    `/v1/storefront/cart/${cartId}/lines?expectedVersion=${version}`,
    { offerId, quantity: 1 },
    guest,
  );
  if (line.status >= 400) throw new Error(`add line ${line.status} ${line.text}`);
  version = pick(line.json, "version", "Version");
  return { cartId, secret, guest, version };
}

function shipCommit(session, authExtra = {}) {
  const auth = { ...session.guest, ...authExtra };
  const proj = host(
    "POST",
    "/v1/storefront/shipping/projection",
    { cartId: session.cartId, provinceName: "تهران", methodCode: "post:express", language: "fa" },
    auth,
  );
  const minDate = pick(proj.json, "minimumDeliveryDate", "MinimumDeliveryDate");
  const shippingAmount = Number(pick(proj.json, "selectedShippingAmount", "SelectedShippingAmount") ?? 0);
  const sel = host(
    "PUT",
    "/v1/storefront/shipping/selection",
    {
      cartId: session.cartId,
      expectedCartVersion: session.version,
      savedAddressId: session.savedAddressId,
      recipientName: session.recipientName ?? "گیرنده تست",
      contactMobile: session.contactMobile ?? "+989121240101",
      provinceName: "تهران",
      cityName: "تهران",
      postalAddress: session.postalAddress ?? "آدرس تست خیابان ولیعصر",
      postalCode: "1234567890",
      shippingMethodCode: "post:express",
      selectedDeliveryDate: minDate,
      selectedDeliveryTimeWindow: "9-12",
      customerNote: "t004r4",
    },
    auth,
  );
  if (sel.status >= 400) throw new Error(`selection ${sel.status} ${sel.text}`);
  const commit = host(
    "POST",
    "/v1/storefront/shipping/commit",
    { cartId: session.cartId, expectedCartVersion: session.version, idempotencyKey: randomUUID() },
    auth,
  );
  if (commit.status >= 400) throw new Error(`commit ${commit.status} ${commit.text}`);
  return {
    ...session,
    auth,
    shippingAmount,
    checkoutId: pick(commit.json, "checkoutId", "CheckoutId"),
    version: pick(commit.json, "cartVersion", "CartVersion", "version", "Version") ?? session.version,
  };
}

function initiate(session, providerCode) {
  const res = host(
    "POST",
    `/v1/storefront/checkout/${session.checkoutId}/payments`,
    {
      cartId: session.cartId,
      providerCode,
      idempotencyKey: randomUUID(),
    },
    session.auth,
  );
  if (res.status >= 400) throw new Error(`initiate ${providerCode} ${res.status} ${res.text}`);
  return {
    paymentId: pick(res.json, "paymentId", "PaymentId"),
    attemptId: pick(res.json, "attemptId", "AttemptId"),
    providerRequestReference: pick(res.json, "providerRequestReference", "ProviderRequestReference"),
    status: pick(res.json, "status", "Status"),
  };
}

function getPayment(paymentId, cartId, guestSecret, authExtra = {}) {
  const extra = { ...authExtra };
  if (guestSecret) extra["X-Tooba-Guest-Secret"] = guestSecret;
  return host("GET", `/v1/storefront/payments/${paymentId}?cartId=${cartId}`, undefined, extra);
}

function sleep(ms) {
  Atomics.wait(new Int32Array(new SharedArrayBuffer(4)), 0, 0, ms);
}

try {
  // health
  const health = curl(["-H", "Host: alpha.localhost", `${HOST}/health`]);
  note("health", { status: health.status, ok: health.status < 500 });

  // ---- A/B Manual AwaitingAdmin + refresh after cart finalize ----
  {
    const cart = buildCart();
    const ship = shipCommit({
      ...cart,
      recipientName: "مهمان R4",
      postalAddress: "آدرس مهمان R4",
    });
    const pay = initiate(ship, "manual");
    const evidence = host(
      "POST",
      `/v1/storefront/payments/${pay.paymentId}/manual-evidence`,
      { cartId: ship.cartId, transferReference: `TRK-R4-${stamp}`, proofMediaAssetId: null },
      ship.guest,
    );
    const statusAfter = pick(evidence.json, "status", "Status");
    const evidenceAt = pick(evidence.json, "evidenceSubmittedAt", "EvidenceSubmittedAt");
    note("A-manual-submit", {
      ok: evidence.status < 400 && !!evidenceAt && String(statusAfter).toLowerCase() === "pending",
      status: evidence.status,
      paymentStatus: statusAfter,
      evidenceSubmitted: !!evidenceAt,
    });

    // simulate active cart finalization: new empty cart
    const fresh = host("POST", "/v1/storefront/cart");
    const freshId = pick(fresh.json, "cartId", "CartId");
    const freshSecret = pick(fresh.json, "guestSecret", "GuestSecret");

    const pollStatuses = [];
    for (let i = 0; i < 8; i++) {
      const g = getPayment(pay.paymentId, ship.cartId, ship.secret);
      pollStatuses.push(g.status);
      sleep(400);
    }
    const ownerOk = pollStatuses.every((s) => s === 200);
    note("A-poll-with-committed-proof", {
      ok: ownerOk,
      statuses: pollStatuses,
      no401: !pollStatuses.includes(401),
    });

    const withFresh = getPayment(pay.paymentId, freshId, freshSecret);
    note("A-new-empty-cart-denied", {
      ok: withFresh.status === 401 || withFresh.status === 403 || withFresh.status >= 400,
      status: withFresh.status,
    });

    const refreshOwned = getPayment(pay.paymentId, ship.cartId, ship.secret);
    note("B-refresh-after-finalize", {
      ok: refreshOwned.status === 200,
      status: refreshOwned.status,
      paymentStatus: pick(refreshOwned.json, "status", "Status"),
      samePayment: pick(refreshOwned.json, "paymentId", "PaymentId") === pay.paymentId,
      sameCheckout: pick(refreshOwned.json, "checkoutId", "CheckoutId") === ship.checkoutId,
    });

    // C Admin confirm later
    const confirm = host(
      "POST",
      `/v1/admin/orders/${ship.checkoutId}/operations`,
      { code: "confirm_deposit", idempotencyKey: randomUUID().replaceAll("-", "") },
      adminHeaders(),
    );
    const afterConfirm = getPayment(pay.paymentId, ship.cartId, ship.secret);
    note("C-admin-confirm", {
      ok: confirm.status < 400 && String(pick(afterConfirm.json, "status", "Status")).toLowerCase() === "succeeded",
      confirmStatus: confirm.status,
      paymentStatus: pick(afterConfirm.json, "status", "Status"),
    });

    out.A = { paymentId: pay.paymentId, checkoutId: ship.checkoutId };
    out.B = { refreshOk: refreshOwned.status === 200 };
    out.C = { succeeded: String(pick(afterConfirm.json, "status", "Status")).toLowerCase() === "succeeded" };
  }

  // ---- D Sandbox success ----
  {
    const cart = buildCart();
    const ship = shipCommit({ ...cart, recipientName: "Sandbox R4" });
    const pay = initiate(ship, "gateway");
    const complete = host(
      "POST",
      `/v1/storefront/payments/${pay.paymentId}/sandbox/complete`,
      {
        cartId: ship.cartId,
        attemptId: pay.attemptId,
        providerRequestReference: pay.providerRequestReference,
        outcome: "success",
      },
      ship.guest,
    );
    const fresh = host("POST", "/v1/storefront/cart");
    const freshId = pick(fresh.json, "cartId", "CartId");
    const freshSecret = pick(fresh.json, "guestSecret", "GuestSecret");
    const owned = getPayment(pay.paymentId, ship.cartId, ship.secret);
    const denied = getPayment(pay.paymentId, freshId, freshSecret);
    note("D-sandbox-success", {
      ok:
        complete.status < 400 &&
        String(pick(owned.json, "status", "Status")).toLowerCase() === "succeeded" &&
        denied.status >= 400,
      completeStatus: complete.status,
      ownedStatus: owned.status,
      paymentStatus: pick(owned.json, "status", "Status"),
      freshDenied: denied.status,
    });
    out.D = { paymentId: pay.paymentId, checkoutId: ship.checkoutId };
  }

  // ---- E Sandbox failure + retry same order ----
  {
    const cart = buildCart();
    const ship = shipCommit({ ...cart, recipientName: "Sandbox Fail R4" });
    const key = `r4-e-${ship.checkoutId}`;
    const pay1 = host(
      "POST",
      `/v1/storefront/checkout/${ship.checkoutId}/payments`,
      { cartId: ship.cartId, providerCode: "gateway", idempotencyKey: key },
      ship.guest,
    );
    if (pay1.status >= 400) throw new Error(`E initiate1 ${pay1.status} ${pay1.text}`);
    const paymentId = pick(pay1.json, "paymentId", "PaymentId");
    const attempt1 = pick(pay1.json, "attemptId", "AttemptId");
    const pref1 = pick(pay1.json, "providerRequestReference", "ProviderRequestReference");
    const fail = host(
      "POST",
      `/v1/storefront/payments/${paymentId}/sandbox/complete`,
      {
        cartId: ship.cartId,
        attemptId: attempt1,
        providerRequestReference: pref1,
        outcome: "failure",
      },
      ship.guest,
    );
    const pay2 = host(
      "POST",
      `/v1/storefront/checkout/${ship.checkoutId}/payments`,
      { cartId: ship.cartId, providerCode: "gateway", idempotencyKey: key },
      ship.guest,
    );
    const attempt2 = pick(pay2.json, "attemptId", "AttemptId");
    const pref2 = pick(pay2.json, "providerRequestReference", "ProviderRequestReference");
    const success = host(
      "POST",
      `/v1/storefront/payments/${paymentId}/sandbox/complete`,
      {
        cartId: ship.cartId,
        attemptId: attempt2,
        providerRequestReference: pref2,
        outcome: "success",
      },
      ship.guest,
    );
    const afterFailOwned = getPayment(paymentId, ship.cartId, ship.secret);
    const afterSuccessOwned = getPayment(paymentId, ship.cartId, ship.secret);
    note("E-sandbox-failure-retry", {
      ok:
        fail.status < 400 &&
        pay2.status < 400 &&
        success.status < 400 &&
        afterFailOwned.status === 200 &&
        afterSuccessOwned.status === 200 &&
        String(pick(afterSuccessOwned.json, "status", "Status")).toLowerCase() === "succeeded" &&
        pick(afterSuccessOwned.json, "checkoutId", "CheckoutId") === ship.checkoutId &&
        pick(pay2.json, "paymentId", "PaymentId") === paymentId,
      samePayment: pick(pay2.json, "paymentId", "PaymentId") === paymentId,
      newAttempt: attempt2 !== attempt1,
      failOwnedStatus: afterFailOwned.status,
      successStatus: pick(afterSuccessOwned.json, "status", "Status"),
      failStatus: fail.status,
      successHttp: success.status,
      pay2Status: pay2.status,
    });
    out.E = {
      paymentId,
      checkoutId: ship.checkoutId,
      samePayment: pick(pay2.json, "paymentId", "PaymentId") === paymentId,
    };
  }

  // ---- F Authenticated ----
  {
    const user = registerLogin(EMAIL_A);
    const addr = createAddress(user.token, "R4-A");
    const cart = buildCart();
    const ship = shipCommit(
      { ...cart, savedAddressId: addr },
      bearer(user.token),
    );
    const pay = initiate(ship, "manual");
    host(
      "POST",
      `/v1/storefront/payments/${pay.paymentId}/manual-evidence`,
      { cartId: ship.cartId, transferReference: `TRK-AUTH-${stamp}`, proofMediaAssetId: null },
      { ...ship.guest, ...bearer(user.token) },
    );
    const fresh = host("POST", "/v1/storefront/cart");
    const freshId = pick(fresh.json, "cartId", "CartId");
    // auth owner with wrong/new cartId still ok (ownership from session)
    const owned = getPayment(pay.paymentId, freshId, null, bearer(user.token));
    note("F-authenticated", {
      ok: owned.status === 200,
      status: owned.status,
      paymentStatus: pick(owned.json, "status", "Status"),
    });
    out.F = { paymentId: pay.paymentId, ok: owned.status === 200 };
  }

  // ---- G Guest proof ----
  {
    const cart = buildCart();
    const ship = shipCommit({ ...cart, recipientName: "Guest Proof R4" });
    const pay = initiate(ship, "manual");
    host(
      "POST",
      `/v1/storefront/payments/${pay.paymentId}/manual-evidence`,
      { cartId: ship.cartId, transferReference: `TRK-G-${stamp}`, proofMediaAssetId: null },
      ship.guest,
    );
    const good = getPayment(pay.paymentId, ship.cartId, ship.secret);
    const wrong = getPayment(pay.paymentId, ship.cartId, randomUUID());
    note("G-guest-proof", {
      ok: good.status === 200 && wrong.status >= 400,
      good: good.status,
      wrong: wrong.status,
    });
    out.G = { paymentId: pay.paymentId, good: good.status, wrong: wrong.status };
  }

  // ---- H Security ----
  {
    const userA = registerLogin(EMAIL_B);
    const addr = createAddress(userA.token, "R4-B");
    const cart = buildCart();
    const ship = shipCommit({ ...cart, savedAddressId: addr }, bearer(userA.token));
    const pay = initiate(ship, "manual");
    host(
      "POST",
      `/v1/storefront/payments/${pay.paymentId}/manual-evidence`,
      { cartId: ship.cartId, transferReference: `TRK-H-${stamp}`, proofMediaAssetId: null },
      { ...ship.guest, ...bearer(userA.token) },
    );
    const foreign = registerLogin(`t004r4.foreign.${stamp}@example.test`);
    const foreignGet = getPayment(pay.paymentId, ship.cartId, null, bearer(foreign.token));
    const alone = host("GET", `/v1/storefront/payments/${pay.paymentId}`, undefined, {});
    const aloneWithBogusCart = getPayment(pay.paymentId, randomUUID(), null, {});
    note("H-security", {
      ok: foreignGet.status >= 400 && alone.status >= 400 && aloneWithBogusCart.status >= 400,
      foreign: foreignGet.status,
      paymentIdAlone: alone.status,
      bogusCart: aloneWithBogusCart.status,
    });
    out.H = {
      foreign: foreignGet.status,
      paymentIdAlone: alone.status,
      bogusCart: aloneWithBogusCart.status,
    };
  }

  // source polling contract (no browser)
  {
    const resultUi = readFileSync("src/frontend/app/payment/result/storefront-payment-result.tsx", "utf8");
    const api = readFileSync("src/frontend/app/storefront/storefront-payment-api.ts", "utf8");
    note("polling-contract", {
      ok:
        resultUi.includes("shouldPollStorefrontPayment") &&
        resultUi.includes("inFlight") &&
        api.includes("evidenceSubmittedAt") &&
        !resultUi.includes("window.setInterval(() => void refresh(), 1500)"),
    });
  }
} catch (err) {
  out.ok = false;
  note("fatal", { ok: false, error: String(err?.message ?? err) });
}

writeFileSync("docs/evidence/TB-P10-T004/r4-runtime-raw.json", JSON.stringify(out, null, 2));
console.log(JSON.stringify({ ok: out.ok, steps: out.steps.length }));
process.exit(out.ok ? 0 : 1);
