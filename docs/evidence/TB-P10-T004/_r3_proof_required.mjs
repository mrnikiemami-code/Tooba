/**
 * TB-P10-T004-R3 Runtime D — ManualProofRequirement=Required
 * Expect Host restarted with Payment__Gateway__ManualProofRequirement=Required
 */
import { writeFileSync, readFileSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { randomUUID } from "node:crypto";

const HOST = "http://127.0.0.1:5088";
const KG_OFFER = "01a03826-9936-7000-b499-ff26a6123a8c";
const PASSWORD = "Customer-t004r3-gate-1!";
const PROOF_PNG = "docs/evidence/TB-P10-T004/_r3-proof.png";
const stamp = Date.now();
const EMAIL = `t004r3.d.${stamp}@example.test`;

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
const req = pick(methods.json, "manualProofRequirement", "ManualProofRequirement");
if (String(req) !== "Required") {
  console.error("expected Required, got", req);
  process.exit(2);
}

host("POST", "/v1/auth/register", { identifierKind: "Email", identifier: EMAIL, password: PASSWORD });
const login = host("POST", "/v1/auth/login", { identifierKind: "Email", identifier: EMAIL, password: PASSWORD });
const token = pick(login.json, "accessToken", "AccessToken");
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
const guest = { "X-Tooba-Guest-Secret": pick(cart, "guestSecret", "GuestSecret") };
const cartId = pick(cart, "cartId", "CartId");
let version = pick(cart, "version", "Version");
const line = host("POST", `/v1/storefront/cart/${cartId}/lines?expectedVersion=${version}`, { offerId: KG_OFFER, quantity: 1 }, guest);
version = pick(line.json, "version", "Version");
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
    selectedDeliveryDate: pick(proj.json, "minimumDeliveryDate", "MinimumDeliveryDate"),
    selectedDeliveryTimeWindow: "9-12",
    customerNote: "d",
  },
  a,
);
const commit = host("POST", "/v1/storefront/shipping/commit", { cartId, expectedCartVersion: version, idempotencyKey: randomUUID() }, a);
const checkoutId = pick(commit.json, "checkoutId", "CheckoutId");
const pay = host("POST", `/v1/storefront/checkout/${checkoutId}/payments`, { cartId, idempotencyKey: randomUUID(), providerCode: "manual" }, a);
const paymentId = pick(pay.json, "paymentId", "PaymentId");
const missingProof = host(
  "POST",
  `/v1/storefront/payments/${paymentId}/manual-evidence`,
  { cartId, transferReference: "TRK-R3-D", proofMediaAssetId: null },
  a,
);
const uploadArgs = [
  "-X",
  "POST",
  `${HOST}/v1/storefront/payments/${paymentId}/proof?cartId=${cartId}`,
  "-H",
  "Host: alpha.localhost",
  "-H",
  `Authorization: Bearer ${token}`,
  "-H",
  `X-Tooba-Guest-Secret: ${guest["X-Tooba-Guest-Secret"]}`,
  "-F",
  `file=@${PROOF_PNG};type=image/png`,
];
const upload = curl(uploadArgs);
const mediaAssetId = pick(upload.json, "mediaAssetId", "MediaAssetId");
const submit = host(
  "POST",
  `/v1/storefront/payments/${paymentId}/manual-evidence`,
  { cartId, transferReference: "TRK-R3-D", proofMediaAssetId: mediaAssetId },
  a,
);
const after = host("GET", `/v1/storefront/payments/${paymentId}?cartId=${cartId}`, undefined, a);
const out = {
  ok:
    missingProof.status >= 400 &&
    (pick(missingProof.json, "errorCode", "ErrorCode") === "payment.proof.required" || /مدرک|proof/i.test(missingProof.text)) &&
    upload.status === 200 &&
    !!mediaAssetId &&
    String(pick(submit.json, "status", "Status")).toLowerCase() === "pending" &&
    String(pick(after.json, "proofMediaAssetId", "ProofMediaAssetId")) === String(mediaAssetId) &&
    !/App_Data|\\\\|C:\\\\/i.test(after.text),
  requirement: req,
  missingProofStatus: missingProof.status,
  missingCode: pick(missingProof.json, "errorCode", "ErrorCode"),
  uploadStatus: upload.status,
  mediaAssetId,
  submitStatus: pick(submit.json, "status", "Status"),
  checkoutId,
  paymentId,
};
writeFileSync("docs/evidence/TB-P10-T004/r3-proof-required-raw.json", JSON.stringify(out, null, 2));
console.log(JSON.stringify(out));
if (!out.ok) process.exit(1);
