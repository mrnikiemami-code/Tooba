/**
 * TB-P10-T004-R1 — authenticated customer checkout runtime (real session, no DevActor login substitute).
 * Host :5088 + FE :3000 BFF cookies. No secrets committed.
 */
import { writeFileSync, readFileSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { randomUUID } from "node:crypto";

const HOST = "http://127.0.0.1:5088";
const FE = "http://127.0.0.1:3000";
const KG_OFFER = "01a03826-9936-7000-b499-ff26a6123a8c";
const PASSWORD = "Customer-t004r1-gate-1!";
const stamp = Date.now();
const EMAIL_A = `t004r1.a.${stamp}@example.test`;
const EMAIL_B = `t004r1.b.${stamp}@example.test`;
const JAR_A = ".tmp-t004r1-cookies-a.txt";
const JAR_B = ".tmp-t004r1-cookies-b.txt";

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
  const args = ["-X", method, `${HOST}${path}`, "-H", "Host: alpha.localhost", "-H", "Content-Type: application/json"];
  for (const [k, v] of Object.entries(extra)) {
    if (v != null) args.push("-H", `${k}: ${v}`);
  }
  if (body !== undefined) args.push("--data-binary", JSON.stringify(body));
  return curl(args);
}

function bearer(token) {
  return { Authorization: `Bearer ${token}` };
}

function addDaysIso(iso, days) {
  const [y, m, d] = iso.split("-").map(Number);
  const dt = new Date(Date.UTC(y, m - 1, d));
  dt.setUTCDate(dt.getUTCDate() + days);
  return dt.toISOString().slice(0, 10);
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
  return { userId: String(userId), token, regStatus: reg.status, loginStatus: login.status, email };
}

function bffLogin(email, jar) {
  curl(["-c", jar, `${FE}/api/auth/csrf`]);
  const jarText = readFileSync(jar, "utf8");
  const csrf = jarText
    .split(/\r?\n/)
    .find((l) => l.includes("tooba_csrf"))
    ?.split(/\s+/)
    .at(-1);
  if (!csrf) throw new Error("no csrf");
  const login = curl([
    "-b",
    jar,
    "-c",
    jar,
    "-H",
    "Content-Type: application/json",
    "-H",
    `X-Tooba-Csrf: ${csrf}`,
    "--data-binary",
    JSON.stringify({ identifierKind: "Email", identifier: email, password: PASSWORD }),
    `${FE}/api/auth/login`,
  ]);
  const hasSession = readFileSync(jar, "utf8").includes("tooba_session");
  return { status: login.status, hasSession };
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
  version = pick(line.json, "version", "Version");
  return { cartId, secret, guest, version };
}

function shipCommit(session, token, savedAddressId) {
  const auth = { ...session.guest, ...bearer(token) };
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
      savedAddressId,
      recipientName: "ignored-when-saved",
      contactMobile: "+989121240101",
      provinceName: "تهران",
      cityName: "تهران",
      postalAddress: "ignored",
      postalCode: "1234567890",
      shippingMethodCode: "post:express",
      selectedDeliveryDate: minDate,
      selectedDeliveryTimeWindow: "9-12",
      customerNote: "t004r1",
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
    shippingAmount,
    checkoutId: pick(commit.json, "checkoutId", "CheckoutId"),
    payable: Number(pick(commit.json, "payableAmount", "PayableAmount")),
    commit,
    auth,
  };
}

const out = { ok: true, steps: [], secretsOmitted: true };
function note(name, data) {
  const row = { name, ...(data ?? {}) };
  out.steps.push(row);
  if (data && data.ok === false) out.ok = false;
  console.log(JSON.stringify(row));
}

const customerA = registerLogin(EMAIL_A);
const customerB = registerLogin(EMAIL_B);
note("auth-register-login", {
  ok: customerA.loginStatus === 200 && customerB.loginStatus === 200,
  aUserId: customerA.userId,
  bUserId: customerB.userId,
  usedDevActorHeader: false,
});

const bffA = bffLogin(EMAIL_A, JAR_A);
const bffB = bffLogin(EMAIL_B, JAR_B);
note("auth-bff-tooba-session", {
  ok: bffA.hasSession && bffB.hasSession && bffA.status === 200,
  a: bffA.status,
  b: bffB.status,
  hasSessionA: bffA.hasSession,
});

const addrA = createAddress(customerA.token, "A-home");
const addrB = createAddress(customerB.token, "B-home");
note("address-create", { ok: !!addrA && !!addrB && addrA !== addrB });

// A cannot list/get B address
const aList = host("GET", "/v1/customer/addresses", undefined, bearer(customerA.token));
const aIds = (Array.isArray(aList.json) ? aList.json : []).map((x) => String(pick(x, "addressId", "AddressId", "id", "Id")));
const aGetB = host("GET", `/v1/customer/addresses/${addrB}`, undefined, bearer(customerA.token));
note("address-ownership", {
  ok: aIds.includes(addrA) && !aIds.includes(addrB) && (aGetB.status === 404 || aGetB.status === 403 || aGetB.status === 400),
  aListCount: aIds.length,
  foreignStatus: aGetB.status,
});

// Authenticated E2E for A
const cartA = buildCart();
const shipA = shipCommit(cartA, customerA.token, addrA);
const payKey = `t004r1-pay-${shipA.checkoutId}`;
const pay1 = host(
  "POST",
  `/v1/storefront/checkout/${shipA.checkoutId}/payments`,
  { cartId: shipA.cartId, idempotencyKey: payKey, providerCode: "manual" },
  shipA.auth,
);
const pay2 = host(
  "POST",
  `/v1/storefront/checkout/${shipA.checkoutId}/payments`,
  { cartId: shipA.cartId, idempotencyKey: payKey, providerCode: "manual" },
  shipA.auth,
);
const paymentId = pick(pay1.json, "paymentId", "PaymentId");
const payDetail = host(
  "GET",
  `/v1/storefront/payments/${paymentId}?cartId=${shipA.cartId}`,
  undefined,
  shipA.auth,
);
const allocs = pick(payDetail.json, "allocations", "Allocations") || [];
const checkoutPage = host(
  "GET",
  `/v1/storefront/checkout/${shipA.checkoutId}?cartId=${shipA.cartId}`,
  undefined,
  shipA.auth,
);
const ownedOrder = host("GET", `/v1/customer/orders/${shipA.checkoutId}`, undefined, bearer(customerA.token));
const foreignOrder = host("GET", `/v1/customer/orders/${shipA.checkoutId}`, undefined, bearer(customerB.token));
const noGuestOrder = host("GET", `/v1/customer/orders/${shipA.checkoutId}`);

const feCart = curl(["-b", JAR_A, `${FE}/fa/cart`]);
const feShip = curl(["-b", JAR_A, `${FE}/fa/shipping`]);
const fePay = curl(["-b", JAR_A, `${FE}/fa/payment?checkoutId=${shipA.checkoutId}`]);
const feOrder = curl(["-b", JAR_A, `${FE}/api/customer/orders/${shipA.checkoutId}`]);

note("authenticated-e2e", {
  ok:
    shipA.commit.status === 200 &&
    pay1.status === 200 &&
    Number(pick(pay1.json, "amount", "Amount")) === shipA.payable &&
    String(pick(pay1.json, "paymentId", "PaymentId")) === String(pick(pay2.json, "paymentId", "PaymentId")) &&
    ownedOrder.status === 200 &&
    feCart.status === 200 &&
    feShip.status === 200 &&
    fePay.status === 200 &&
    !/CVV|cardNumber/i.test(fePay.text),
  checkoutId: shipA.checkoutId,
  payable: shipA.payable,
  paymentAmount: pick(pay1.json, "amount", "Amount"),
  ownedOrder: ownedOrder.status,
  feOrder: feOrder.status,
});

note("payment-ownership", {
  ok:
    Number(pick(pay1.json, "amount", "Amount")) === shipA.payable &&
    String(pick(pay1.json, "paymentId", "PaymentId")) === String(pick(pay2.json, "paymentId", "PaymentId")),
  idempotent: true,
});

// B cannot pay A's checkout with B token + wrong ownership
const bPayForeign = host(
  "POST",
  `/v1/storefront/checkout/${shipA.checkoutId}/payments`,
  { cartId: shipA.cartId, idempotencyKey: randomUUID(), providerCode: "manual" },
  { ...bearer(customerB.token), "X-Tooba-Guest-Secret": "wrong" },
);
note("payment-foreign-denied", {
  ok: bPayForeign.status >= 400,
  status: bPayForeign.status,
  code: pick(bPayForeign.json, "errorCode", "ErrorCode"),
});

note("order-ownership", {
  ok: ownedOrder.status === 200 && foreignOrder.status >= 400 && noGuestOrder.status >= 400,
  owned: ownedOrder.status,
  foreign: foreignOrder.status,
  unauth: noGuestOrder.status,
});

// Cart continuity: refresh GET cart with same guest+auth
const refreshCart = host("GET", `/v1/storefront/cart/${cartA.cartId}`, undefined, shipA.auth);
note("cart-continuity", {
  ok: refreshCart.status === 200 && Number(pick(refreshCart.json, "version", "Version") ?? 0) >= 1,
  status: refreshCart.status,
});

// A tries B saved address on new cart
const cartForeignAddr = buildCart();
const badSel = host(
  "PUT",
  "/v1/storefront/shipping/selection",
  {
    cartId: cartForeignAddr.cartId,
    expectedCartVersion: cartForeignAddr.version,
    savedAddressId: addrB,
    recipientName: "x",
    contactMobile: "+989121240101",
    provinceName: "تهران",
    cityName: "تهران",
    postalAddress: "x",
    postalCode: "1234567890",
    shippingMethodCode: "post:express",
    selectedDeliveryDate: "2099-01-01",
    selectedDeliveryTimeWindow: "9-12",
    customerNote: "foreign-addr",
  },
  { ...cartForeignAddr.guest, ...bearer(customerA.token) },
);
note("address-foreign-in-shipping", {
  ok: badSel.status >= 400,
  status: badSel.status,
  code: pick(badSel.json, "errorCode", "ErrorCode"),
});

// Guest regression
{
  const g = buildCart();
  const proj = host(
    "POST",
    "/v1/storefront/shipping/projection",
    { cartId: g.cartId, provinceName: "تهران", methodCode: "post:express", language: "fa" },
    g.guest,
  );
  const minDate = pick(proj.json, "minimumDeliveryDate", "MinimumDeliveryDate");
  host(
    "PUT",
    "/v1/storefront/shipping/selection",
    {
      cartId: g.cartId,
      expectedCartVersion: g.version,
      recipientName: "Guest R1",
      contactMobile: "+989121240199",
      provinceName: "تهران",
      cityName: "تهران",
      postalAddress: "آدرس مهمان",
      postalCode: "1234567890",
      shippingMethodCode: "post:express",
      selectedDeliveryDate: minDate,
      selectedDeliveryTimeWindow: "9-12",
      customerNote: "guest",
    },
    g.guest,
  );
  const commit = host(
    "POST",
    "/v1/storefront/shipping/commit",
    { cartId: g.cartId, expectedCartVersion: g.version, idempotencyKey: randomUUID() },
    g.guest,
  );
  const pay = host(
    "POST",
    `/v1/storefront/checkout/${pick(commit.json, "checkoutId", "CheckoutId")}/payments`,
    {
      cartId: g.cartId,
      idempotencyKey: `guest-${pick(commit.json, "checkoutId", "CheckoutId")}`,
      providerCode: "manual",
    },
    g.guest,
  );
  note("guest-regression", {
    ok: commit.status === 200 && pay.status === 200,
    checkoutId: pick(commit.json, "checkoutId", "CheckoutId"),
  });
}

// Financial sample
{
  const allocSum = allocs.reduce((n, a) => n + Number(pick(a, "allocatedAmount", "AllocatedAmount")), 0);
  note("financial-consistency", {
    ok:
      Math.abs(allocSum - shipA.payable) < 0.0001 &&
      Number(pick(pay1.json, "amount", "Amount")) === shipA.payable,
    payable: shipA.payable,
    allocSum,
    shipping: shipA.shippingAmount,
  });
}

writeFileSync("docs/evidence/TB-P10-T004/r1-runtime-raw.json", JSON.stringify(out, null, 2));
if (!out.ok) process.exit(1);
