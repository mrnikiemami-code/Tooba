/**
 * TB-P10-T004-R3 Runtime I (sandbox disabled) — Payment__Gateway__Mode=Disabled
 */
import { writeFileSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { randomUUID } from "node:crypto";

const HOST = "http://127.0.0.1:5088";
const KG_OFFER = "01a03826-9936-7000-b499-ff26a6123a8c";
const PASSWORD = "Customer-t004r3-gate-1!";

function curl(args) {
  const raw = execFileSync("curl.exe", ["-sS", "-w", "\n%{http_code}", ...args], {
    encoding: "utf8",
    maxBuffer: 8 * 1024 * 1024,
  });
  const idx = raw.lastIndexOf("\n");
  const text = idx >= 0 ? raw.slice(0, idx) : raw;
  let json;
  try {
    json = JSON.parse(text);
  } catch {
    json = text;
  }
  return { status: Number(idx >= 0 ? raw.slice(idx + 1) : 0), json, text };
}
function pick(o, ...ks) {
  if (!o || typeof o !== "object") return;
  for (const k of ks) if (o[k] != null) return o[k];
}
function host(method, path, body, extra = {}) {
  const args = ["-X", method, `${HOST}${path}`, "-H", "Host: alpha.localhost", "-H", "Content-Type: application/json"];
  for (const [k, v] of Object.entries(extra)) if (v != null) args.push("-H", `${k}: ${v}`);
  if (body !== undefined) args.push("--data-binary", JSON.stringify(body));
  return curl(args);
}

const methods = host("GET", "/v1/storefront/payment-methods");
const email = `t004r3.dis.${Date.now()}@example.test`;
host("POST", "/v1/auth/register", { identifierKind: "Email", identifier: email, password: PASSWORD });
const token = pick(host("POST", "/v1/auth/login", { identifierKind: "Email", identifier: email, password: PASSWORD }).json, "accessToken");
const auth = { Authorization: `Bearer ${token}` };
const addr = pick(
  host(
    "POST",
    "/v1/customer/addresses",
    {
      label: "d",
      recipientName: "r",
      contactMobile: "+989121240101",
      provinceName: "تهران",
      cityName: "تهران",
      postalAddress: "a",
      postalCode: "1234567890",
      isDefault: true,
    },
    auth,
  ).json,
  "addressId",
  "id",
);
const cart = host("POST", "/v1/storefront/cart").json;
const guest = { "X-Tooba-Guest-Secret": pick(cart, "guestSecret") };
const cartId = pick(cart, "cartId");
let version = pick(cart, "version");
const line = host("POST", `/v1/storefront/cart/${cartId}/lines?expectedVersion=${version}`, { offerId: KG_OFFER, quantity: 1 }, guest);
version = pick(line.json, "version");
const a = { ...guest, ...auth };
const proj = host("POST", "/v1/storefront/shipping/projection", { cartId, provinceName: "تهران", methodCode: "post:express", language: "fa" }, a);
host(
  "PUT",
  "/v1/storefront/shipping/selection",
  {
    cartId,
    expectedCartVersion: version,
    savedAddressId: addr,
    recipientName: "x",
    contactMobile: "+989121240101",
    provinceName: "تهران",
    cityName: "تهران",
    postalAddress: "x",
    postalCode: "1234567890",
    shippingMethodCode: "post:express",
    selectedDeliveryDate: pick(proj.json, "minimumDeliveryDate"),
    selectedDeliveryTimeWindow: "9-12",
    customerNote: "d",
  },
  a,
);
const commit = host("POST", "/v1/storefront/shipping/commit", { cartId, expectedCartVersion: version, idempotencyKey: randomUUID() }, a);
const checkoutId = pick(commit.json, "checkoutId");
const payGateway = host("POST", `/v1/storefront/checkout/${checkoutId}/payments`, { cartId, idempotencyKey: randomUUID(), providerCode: "gateway" }, a);
const payManual = host("POST", `/v1/storefront/checkout/${checkoutId}/payments`, { cartId, idempotencyKey: randomUUID(), providerCode: "manual" }, a);
let sandboxDenied = null;
if (payManual.status === 200) {
  const paymentId = pick(payManual.json, "paymentId");
  sandboxDenied = host("GET", `/v1/storefront/payments/${paymentId}/sandbox?cartId=${cartId}`, undefined, a);
}
const out = {
  ok:
    (payGateway.status >= 400 ||
      pick(payGateway.json, "errorCode", "ErrorCode") === "payment.method.unavailable" ||
      pick(payGateway.json, "errorCode", "ErrorCode") === "payment.rejected") &&
    (sandboxDenied == null ||
      sandboxDenied.status === 403 ||
      pick(sandboxDenied.json, "errorCode", "ErrorCode") === "payment.sandbox.unavailable"),
  methods: methods.json,
  payGatewayStatus: payGateway.status,
  payGatewayCode: pick(payGateway.json, "errorCode", "ErrorCode"),
  payManualStatus: payManual.status,
  sandboxStatus: sandboxDenied?.status,
  sandboxCode: sandboxDenied ? pick(sandboxDenied.json, "errorCode", "ErrorCode") : null,
};
writeFileSync("docs/evidence/TB-P10-T004/r3-sandbox-disabled-raw.json", JSON.stringify(out, null, 2));
console.log(JSON.stringify(out));
if (!out.ok) process.exit(1);
