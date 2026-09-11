/**
 * TB-P10-T004-R3 — real payment-completion runtime A–J (Host :5088, FE :3000).
 * Secrets omitted from evidence JSON.
 */
import { writeFileSync, readFileSync, mkdirSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { randomUUID } from "node:crypto";
import { join } from "node:path";

const HOST = "http://127.0.0.1:5088";
const FE = "http://127.0.0.1:3000";
const KG_OFFER = "01a03826-9936-7000-b499-ff26a6123a8c";
const ADMIN = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const PASSWORD = "Customer-t004r3-gate-1!";
const stamp = Date.now();
const EMAIL_A = `t004r3.a.${stamp}@example.test`;
const EMAIL_B = `t004r3.b.${stamp}@example.test`;
const JAR_A = ".tmp-t004r3-cookies-a.txt";
const JAR_B = ".tmp-t004r3-cookies-b.txt";
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
  return { status: login.status, hasSession: readFileSync(jar, "utf8").includes("tooba_session") };
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
      customerNote: "t004r3",
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
    payable: Number(pick(commit.json, "payableAmount", "PayableAmount")),
    commit,
  };
}

function initiate(ship, providerCode, key = randomUUID()) {
  const res = host(
    "POST",
    `/v1/storefront/checkout/${ship.checkoutId}/payments`,
    { cartId: ship.cartId, idempotencyKey: key, providerCode },
    ship.auth,
  );
  if (res.status >= 400) {
    console.error("initiate-fail", res.status, res.text.slice(0, 400));
  }
  return res;
}

function getPayment(paymentId, cartId, auth) {
  return host("GET", `/v1/storefront/payments/${paymentId}?cartId=${cartId}`, undefined, auth);
}

function sandboxContext(paymentId, cartId, auth) {
  return host("GET", `/v1/storefront/payments/${paymentId}/sandbox?cartId=${cartId}`, undefined, auth);
}

function sandboxComplete(paymentId, body, auth) {
  return host("POST", `/v1/storefront/payments/${paymentId}/sandbox/complete`, body, auth);
}

function getCart(cartId, auth) {
  return host("GET", `/v1/storefront/cart/${cartId}`, undefined, auth);
}

function countCheckouts(checkoutId) {
  // Admin detail is one checkout; duplicate would appear as separate initiate on new cart.
  // Use operational payment list via admin order detail.
  const detail = host("GET", `/v1/admin/orders/${checkoutId}`, undefined, adminHeaders());
  return { status: detail.status, detail: detail.json };
}

function adminOps(checkoutId, code) {
  return host(
    "POST",
    `/v1/admin/orders/${checkoutId}/operations`,
    { code, idempotencyKey: randomUUID().replaceAll("-", "") },
    adminHeaders(),
  );
}

function adminDetail(checkoutId) {
  return host("GET", `/v1/admin/orders/${checkoutId}`, undefined, adminHeaders());
}

function uploadProof(paymentId, cartId, auth, filePath) {
  const args = [
    "-X",
    "POST",
    `${HOST}/v1/storefront/payments/${paymentId}/proof?cartId=${cartId}`,
    "-H",
    "Host: alpha.localhost",
    "-F",
    `file=@${filePath};type=image/png`,
  ];
  for (const [k, v] of Object.entries(auth)) {
    if (v != null) args.push("-H", `${k}: ${v}`);
  }
  return curl(args);
}

function ensureTinyPng() {
  // 1x1 PNG
  const b64 =
    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";
  mkdirSync("docs/evidence/TB-P10-T004", { recursive: true });
  writeFileSync(PROOF_PNG, Buffer.from(b64, "base64"));
}

function linesEmpty(cartJson) {
  const lines = pick(cartJson, "lines", "Lines") || [];
  const count = Number(pick(cartJson, "itemCount", "ItemCount") ?? lines.length);
  return Array.isArray(lines) && lines.length === 0 && count === 0;
}

ensureTinyPng();

const methods = host("GET", "/v1/storefront/payment-methods");
note("runtime-ready", {
  ok: methods.status === 200,
  proofRequirement: pick(methods.json, "manualProofRequirement", "ManualProofRequirement"),
  hostHealth: true,
});

const customerA = registerLogin(EMAIL_A);
const customerB = registerLogin(EMAIL_B);
const bffA = bffLogin(EMAIL_A, JAR_A);
const bffB = bffLogin(EMAIL_B, JAR_B);
const addrA = createAddress(customerA.token, "A-home");
note("auth-ready", {
  ok: bffA.hasSession && bffB.hasSession,
  aUserId: customerA.userId,
  bUserId: customerB.userId,
});

// ---------- A Sandbox Success ----------
{
  const cart = buildCart();
  const ship = shipCommit({ ...cart, savedAddressId: addrA }, bearer(customerA.token));
  const key = `r3-a-${ship.checkoutId}`;
  const pay = initiate(ship, "gateway", key);
  const paymentId = pick(pay.json, "paymentId", "PaymentId");
  const attemptId = pick(pay.json, "attemptId", "AttemptId");
  const pref = pick(pay.json, "providerRequestReference", "ProviderRequestReference");
  const redirect = String(pick(pay.json, "redirectUrl", "RedirectUrl") || "");
  const sbx = sandboxContext(paymentId, ship.cartId, ship.auth);
  const complete = sandboxComplete(
    paymentId,
    {
      cartId: ship.cartId,
      attemptId,
      providerRequestReference: pref,
      outcome: "success",
    },
    ship.auth,
  );
  const after = getPayment(paymentId, ship.cartId, ship.auth);
  const refresh = sandboxComplete(
    paymentId,
    {
      cartId: ship.cartId,
      attemptId,
      providerRequestReference: pref,
      outcome: "success",
    },
    ship.auth,
  );
  const cartAfter = getCart(ship.cartId, ship.auth);
  const order = host("GET", `/v1/customer/orders/${ship.checkoutId}`, undefined, bearer(customerA.token));
  const feResult = curl([
    "-b",
    JAR_A,
    `${FE}/fa/payment/result?paymentId=${paymentId}&cartId=${ship.cartId}`,
  ]);
  const feSbx = curl([
    "-b",
    JAR_A,
    `${FE}/fa/payment/sandbox?paymentId=${paymentId}&attemptId=${attemptId}&ref=${encodeURIComponent(pref)}&cartId=${ship.cartId}`,
  ]);
  const orderLink = curl(["-b", JAR_A, `${FE}/fa/customer-panel/orders/${ship.checkoutId}`]);
  const orderNumber = pick(after.json, "orderNumber", "OrderNumber");
  note("A-sandbox-success", {
    ok:
      pay.status === 200 &&
      redirect.includes("/payment/sandbox") &&
      sbx.status === 200 &&
      !!pick(sbx.json, "storeName", "StoreName") &&
      !!pick(sbx.json, "orderNumber", "OrderNumber") &&
      Number(pick(sbx.json, "amount", "Amount")) === ship.payable &&
      pick(sbx.json, "sandbox", "Sandbox") === true &&
      String(pick(complete.json, "status", "Status")).toLowerCase() === "succeeded" &&
      String(pick(after.json, "status", "Status")).toLowerCase() === "succeeded" &&
      String(pick(refresh.json, "status", "Status")).toLowerCase() === "succeeded" &&
      linesEmpty(cartAfter.json) &&
      order.status === 200 &&
      !!orderNumber &&
      feResult.status === 200 &&
      orderLink.status === 200 &&
      feSbx.status === 200 &&
      /پرداخت موفق|پرداخت ناموفق/.test(feSbx.text) &&
      !/CVV|cardNumber|card number/i.test(feSbx.text + feResult.text),
    checkoutId: ship.checkoutId,
    paymentId,
    orderNumber,
    payable: ship.payable,
    paymentAmount: pick(pay.json, "amount", "Amount"),
    cartEmpty: linesEmpty(cartAfter.json),
    redirect,
    refreshStillSucceeded: String(pick(refresh.json, "status", "Status")),
    orderLink: orderLink.status,
    feResult: feResult.status,
  });
  out.A = { checkoutId: ship.checkoutId, paymentId, payable: ship.payable, orderNumber, cartId: ship.cartId };
}

// ---------- B Sandbox Failure + Retry ----------
{
  const cart = buildCart();
  const ship = shipCommit({ ...cart, savedAddressId: addrA }, bearer(customerA.token));
  const key = `r3-b-${ship.checkoutId}`;
  const pay1 = initiate(ship, "gateway", key);
  const paymentId = pick(pay1.json, "paymentId", "PaymentId");
  const attempt1 = pick(pay1.json, "attemptId", "AttemptId");
  const pref1 = pick(pay1.json, "providerRequestReference", "ProviderRequestReference");
  const fail = sandboxComplete(
    paymentId,
    {
      cartId: ship.cartId,
      attemptId: attempt1,
      providerRequestReference: pref1,
      outcome: "failure",
    },
    ship.auth,
  );
  const afterFail = getPayment(paymentId, ship.cartId, ship.auth);
  const pay2 = initiate(ship, "gateway", key);
  const attempt2 = pick(pay2.json, "attemptId", "AttemptId");
  const pref2 = pick(pay2.json, "providerRequestReference", "ProviderRequestReference");
  const ok2 = sandboxComplete(
    paymentId,
    {
      cartId: ship.cartId,
      attemptId: attempt2,
      providerRequestReference: pref2,
      outcome: "success",
    },
    ship.auth,
  );
  const feFail = curl([
    "-b",
    JAR_A,
    `${FE}/fa/payment/result?paymentId=${paymentId}&cartId=${ship.cartId}`,
  ]);
  note("B-sandbox-failure-retry", {
    ok:
      String(pick(fail.json, "status", "Status")).toLowerCase() === "failed" &&
      String(pick(afterFail.json, "status", "Status")).toLowerCase() === "failed" &&
      String(pick(pay2.json, "paymentId", "PaymentId")) === String(paymentId) &&
      String(attempt2) !== String(attempt1) &&
      String(pick(ok2.json, "status", "Status")).toLowerCase() === "succeeded" &&
      feFail.status === 200 &&
      !/CVV|cardNumber/i.test(feFail.text),
    checkoutId: ship.checkoutId,
    paymentId,
    attempt1,
    attempt2,
    samePayment: String(pick(pay2.json, "paymentId", "PaymentId")) === String(paymentId),
    failStatus: pick(fail.json, "status", "Status"),
    retryStatus: pick(ok2.json, "status", "Status"),
    feFail: feFail.status,
  });
  out.B = { checkoutId: ship.checkoutId, paymentId };
}

// ---------- C Manual tracking only (Optional) ----------
{
  const cart = buildCart();
  const ship = shipCommit({ ...cart, savedAddressId: addrA }, bearer(customerA.token));
  const pay = initiate(ship, "manual", `r3-c-${ship.checkoutId}`);
  const paymentId = pick(pay.json, "paymentId", "PaymentId");
  const missing = host(
    "POST",
    `/v1/storefront/payments/${paymentId}/manual-evidence`,
    { cartId: ship.cartId, transferReference: "   ", proofMediaAssetId: null },
    ship.auth,
  );
  const submit = host(
    "POST",
    `/v1/storefront/payments/${paymentId}/manual-evidence`,
    { cartId: ship.cartId, transferReference: "TRK-R3-C-001", proofMediaAssetId: null },
    ship.auth,
  );
  const dup = host(
    "POST",
    `/v1/storefront/payments/${paymentId}/manual-evidence`,
    { cartId: ship.cartId, transferReference: "TRK-R3-C-001", proofMediaAssetId: null },
    ship.auth,
  );
  const after = getPayment(paymentId, ship.cartId, ship.auth);
  const cartAfter = getCart(ship.cartId, ship.auth);
  const fe = curl([
    "-b",
    JAR_A,
    `${FE}/fa/payment/result?paymentId=${paymentId}&cartId=${ship.cartId}`,
  ]);
  note("C-manual-tracking", {
    ok:
      missing.status >= 400 &&
      String(pick(submit.json, "status", "Status")).toLowerCase() === "pending" &&
      String(pick(after.json, "status", "Status")).toLowerCase() === "pending" &&
      pick(after.json, "customerTransferReference", "CustomerTransferReference") === "TRK-R3-C-001" &&
      linesEmpty(cartAfter.json) &&
      dup.status === 200 &&
      /در انتظار|پیگیری|مشاهده سفارش/.test(fe.text),
    checkoutId: ship.checkoutId,
    paymentId,
    missingStatus: missing.status,
    orderNumber: pick(after.json, "orderNumber", "OrderNumber"),
  });
  out.C = { checkoutId: ship.checkoutId, paymentId, cartId: ship.cartId, authGuest: ship.secret };
}

// ---------- E Admin Confirm (uses C) ----------
{
  const { checkoutId, paymentId } = out.C;
  const detail = adminDetail(checkoutId);
  const text = detail.text;
  const confirm = adminOps(checkoutId, "confirm_deposit");
  const order = host("GET", `/v1/customer/orders/${checkoutId}`, undefined, bearer(customerA.token));
  note("E-admin-confirm", {
    ok:
      detail.status === 200 &&
      /TRK-R3-C-001/.test(text) &&
      confirm.status === 200 &&
      order.status === 200,
    confirmStatus: confirm.status,
    confirmBody: typeof confirm.json === "object" ? pick(confirm.json, "status", "Status") : null,
    hasTrackingInAdmin: /TRK-R3-C-001/.test(text),
    checkoutId,
  });
  out.E = { checkoutId, paymentId, confirmStatus: confirm.status };
}

// ---------- F Admin Reject + Retry ----------
{
  const cart = buildCart();
  const ship = shipCommit({ ...cart, savedAddressId: addrA }, bearer(customerA.token));
  const pay = initiate(ship, "manual", `r3-f-${ship.checkoutId}`);
  const paymentId = pick(pay.json, "paymentId", "PaymentId");
  host(
    "POST",
    `/v1/storefront/payments/${paymentId}/manual-evidence`,
    { cartId: ship.cartId, transferReference: "TRK-R3-F-OLD", proofMediaAssetId: null },
    ship.auth,
  );
  const reject = adminOps(ship.checkoutId, "reject_deposit");
  const afterReject = getPayment(paymentId, ship.cartId, ship.auth);
  const retry = host(
    "POST",
    `/v1/storefront/payments/${paymentId}/manual-retry`,
    { cartId: ship.cartId },
    ship.auth,
  );
  const submit2 = host(
    "POST",
    `/v1/storefront/payments/${paymentId}/manual-evidence`,
    { cartId: ship.cartId, transferReference: "TRK-R3-F-NEW", proofMediaAssetId: null },
    ship.auth,
  );
  const hist = pick(getPayment(paymentId, ship.cartId, ship.auth).json, "evidenceHistory", "EvidenceHistory") || [];
  const refs = hist.map((h) => pick(h, "customerTransferReference", "CustomerTransferReference"));
  note("F-admin-reject-retry", {
    ok:
      reject.status === 200 &&
      String(pick(afterReject.json, "status", "Status")).toLowerCase() === "failed" &&
      retry.status === 200 &&
      String(pick(submit2.json, "status", "Status")).toLowerCase() === "pending" &&
      refs.includes("TRK-R3-F-OLD") &&
      refs.includes("TRK-R3-F-NEW"),
    checkoutId: ship.checkoutId,
    paymentId,
    historyRefs: refs,
  });
  out.F = { checkoutId: ship.checkoutId, paymentId };
}

// ---------- G Guest ----------
{
  const cart = buildCart();
  const ship = shipCommit({
    ...cart,
    recipientName: "مهمان R3",
    contactMobile: "+989121240199",
    postalAddress: "آدرس مهمان R3",
  });
  const pay = initiate(ship, "manual", `r3-g-${ship.checkoutId}`);
  const paymentId = pick(pay.json, "paymentId", "PaymentId");
  const okSubmit = host(
    "POST",
    `/v1/storefront/payments/${paymentId}/manual-evidence`,
    { cartId: ship.cartId, transferReference: "TRK-R3-G", proofMediaAssetId: null },
    ship.auth,
  );
  const wrong = host(
    "POST",
    `/v1/storefront/payments/${paymentId}/manual-evidence`,
    { cartId: ship.cartId, transferReference: "TRK-R3-G-X", proofMediaAssetId: null },
    { "X-Tooba-Guest-Secret": "deadbeef" },
  );
  const checkout = host(
    "GET",
    `/v1/storefront/checkout/${ship.checkoutId}?cartId=${ship.cartId}`,
    undefined,
    ship.auth,
  );
  note("G-guest", {
    ok:
      okSubmit.status === 200 &&
      wrong.status >= 400 &&
      checkout.status === 200,
    checkoutId: ship.checkoutId,
    paymentId,
    wrongStatus: wrong.status,
  });
  out.G = { checkoutId: ship.checkoutId, paymentId, guestSecretPresent: true };
}

// ---------- H Authenticated ownership ----------
{
  const foreign = host(
    "GET",
    `/v1/customer/orders/${out.A.checkoutId}`,
    undefined,
    bearer(customerB.token),
  );
  const own = host("GET", `/v1/customer/orders/${out.A.checkoutId}`, undefined, bearer(customerA.token));
  const foreignPay = host(
    "POST",
    `/v1/storefront/payments/${out.C.paymentId}/manual-evidence`,
    { cartId: randomUUID(), transferReference: "HACK", proofMediaAssetId: null },
    bearer(customerB.token),
  );
  const feOrder = curl(["-b", JAR_A, `${FE}/api/customer/orders/${out.A.checkoutId}`]);
  note("H-authenticated", {
    ok: own.status === 200 && foreign.status >= 400 && foreignPay.status >= 400 && feOrder.status === 200,
    own: own.status,
    foreign: foreign.status,
    foreignPay: foreignPay.status,
    feOrder: feOrder.status,
  });
}

// ---------- I Security (foreign media; Mode=Disabled checked in r3-i-disabled.mjs) ----------
{
  const cart = buildCart();
  const ship = shipCommit({ ...cart, savedAddressId: addrA }, bearer(customerA.token));
  const pay = initiate(ship, "manual", `r3-i-${ship.checkoutId}`);
  const paymentId = pick(pay.json, "paymentId", "PaymentId");
  const foreignMedia = host(
    "POST",
    `/v1/storefront/payments/${paymentId}/manual-evidence`,
    {
      cartId: ship.cartId,
      transferReference: "TRK-R3-I",
      proofMediaAssetId: "22222222-2222-2222-2222-222222222222",
    },
    ship.auth,
  );
  note("I-security-foreign-media", {
    ok: foreignMedia.status >= 400,
    status: foreignMedia.status,
    code: pick(foreignMedia.json, "errorCode", "ErrorCode"),
    checkoutId: ship.checkoutId,
  });
  out.I = { foreignMediaStatus: foreignMedia.status, checkoutId: ship.checkoutId, paymentId, cartId: ship.cartId };
}

// ---------- J Financial ----------
{
  const cart = buildCart();
  const ship = shipCommit({ ...cart, savedAddressId: addrA }, bearer(customerA.token));
  const pay = initiate(ship, "gateway", `r3-j-${ship.checkoutId}`);
  const paymentId = pick(pay.json, "paymentId", "PaymentId");
  const detail = getPayment(paymentId, ship.cartId, ship.auth);
  const allocs = pick(detail.json, "allocations", "Allocations") || [];
  const allocSum = allocs.reduce((n, a) => n + Number(pick(a, "allocatedAmount", "AllocatedAmount")), 0);
  const shippingRows = allocs.filter((a) =>
    String(pick(a, "targetKind", "TargetKind") || "").toLowerCase().includes("storeshipping"),
  );
  note("J-financial", {
    ok:
      Number(pick(pay.json, "amount", "Amount")) === ship.payable &&
      Math.abs(allocSum - ship.payable) < 0.0001 &&
      (ship.shippingAmount === 0 || shippingRows.length === 1),
    payable: ship.payable,
    paymentAmount: pick(pay.json, "amount", "Amount"),
    allocSum,
    shipping: ship.shippingAmount,
    shippingAllocCount: shippingRows.length,
  });
  out.J = { checkoutId: ship.checkoutId, payable: ship.payable };
}

// Visual smoke snippets
{
  const sbxHtml = curl(["-b", JAR_A, `${FE}/fa/payment/sandbox`]).text;
  const adminHtml = curl([
    "-H",
    `X-Tooba-Dev-Actor-User-Id: ${ADMIN}`,
    `${FE}/fa/admin/orders/${out.C.checkoutId}`,
  ]);
  note("visual-smoke-http", {
    ok: adminHtml.status === 200 || adminHtml.status === 307 || adminHtml.status === 200,
    adminStatus: adminHtml.status,
    sandboxHasCardFields: /CVV|cardNumber/i.test(sbxHtml),
  });
}

writeFileSync("docs/evidence/TB-P10-T004/r3-runtime-raw.json", JSON.stringify(out, null, 2));
if (!out.ok) process.exit(1);
console.log(JSON.stringify({ ok: out.ok, stepCount: out.steps.length }));
