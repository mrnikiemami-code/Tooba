import { writeFileSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { randomUUID } from "node:crypto";

const HOST = "http://127.0.0.1:5088";
const ADMIN = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const PARS = "01a03826-9936-7000-b499-ff26a6123a8c";
const ARMAN = "01a030d1-40f1-7000-95f6-b8efc58e2619";
const PASSWORD = "Customer-r2-horse-1!";
const EMAIL = `r2.customer.${Date.now()}@example.test`;

function curl(args) {
  const raw = execFileSync("curl.exe", ["-sS", "-w", "\n%{http_code}", ...args], {
    encoding: "utf8",
    maxBuffer: 10 * 1024 * 1024,
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

function host(method, path, body, extra = {}) {
  const headers = { Host: "alpha.localhost", "Content-Type": "application/json", ...extra };
  const args = ["-X", method, `${HOST}${path}`];
  for (const [k, v] of Object.entries(headers)) {
    if (v != null) args.push("-H", `${k}: ${v}`);
  }
  if (body !== undefined) args.push("--data-binary", JSON.stringify(body));
  return curl(args);
}

function sleep(ms) {
  Atomics.wait(new Int32Array(new SharedArrayBuffer(4)), 0, 0, ms);
}

function pick(obj, ...keys) {
  if (!obj || typeof obj !== "object") return undefined;
  for (const key of keys) if (obj[key] != null) return obj[key];
  return undefined;
}

function actionCodes(ops) {
  return (ops.json?.actions ?? ops.json?.Actions ?? []).map((a) => a.code ?? a.Code);
}

function waitForCode(checkoutId, code, tries = 80) {
  let ops;
  for (let i = 0; i < tries; i++) {
    ops = host("GET", `/v1/admin/orders/${checkoutId}/operations`, undefined, {
      "X-Tooba-Dev-Actor-User-Id": ADMIN,
    });
    if (actionCodes(ops).includes(code)) return ops;
    sleep(250);
  }
  return ops;
}

function postOp(checkoutId, body) {
  return host(
    "POST",
    `/v1/admin/orders/${checkoutId}/operations`,
    { idempotencyKey: randomUUID().replaceAll("-", ""), ...body },
    { "X-Tooba-Dev-Actor-User-Id": ADMIN },
  );
}

function detail(checkoutId) {
  return host("GET", `/v1/admin/orders/${checkoutId}`, undefined, {
    "X-Tooba-Dev-Actor-User-Id": ADMIN,
  }).json;
}

function sellersOf(d) {
  return d?.sellerOrders ?? d?.SellerOrders ?? [];
}

function sellerId(s) {
  return pick(s, "sellerOrderId", "SellerOrderId", "id", "Id");
}

function packagesOf(d) {
  return d?.consolidatedPackages ?? d?.ConsolidatedPackages ?? [];
}

function sql(text) {
  return execFileSync(
    "docker",
    ["exec", "-i", "postgres-db", "psql", "-U", "admin", "-d", "tooba_alpha", "-v", "ON_ERROR_STOP=1"],
    { encoding: "utf8", input: text },
  );
}

sql(`
UPDATE inventory.stock_positions
SET on_hand = GREATEST(on_hand, 200), reserved = LEAST(reserved, 20), updated_at = now()
WHERE offer_id IN ('${PARS}', '${ARMAN}');
`);

const report = { email: EMAIL, password: PASSWORD, task: "TB-P09-T022-R2" };

const reg = host("POST", "/v1/auth/register", {
  identifierKind: "Email",
  identifier: EMAIL,
  password: PASSWORD,
});
report.registerStatus = reg.status;
const userId = pick(reg.json, "userId", "UserId");
if (!userId) throw new Error(`register ${reg.status} ${JSON.stringify(reg.json)}`);
report.userId = userId;

const login = host("POST", "/v1/auth/login", {
  identifierKind: "Email",
  identifier: EMAIL,
  password: PASSWORD,
});
report.loginStatus = login.status;
report.accessToken = pick(login.json, "accessToken", "AccessToken", "sessionId", "SessionId");
report.refreshToken = pick(login.json, "refreshToken", "RefreshToken");
if (!report.accessToken) throw new Error(`login ${login.status}`);

// Guest-only paid multi-seller (same as T022 runtime), then rebind ownership to registered user.
const cart = host("POST", "/v1/storefront/cart");
const secret = pick(cart.json, "guestSecret", "GuestSecret");
const cartId = pick(cart.json, "cartId", "CartId");
let version = pick(cart.json, "version", "Version");
const guest = { "X-Tooba-Guest-Secret": secret };
for (const offerId of [PARS, ARMAN]) {
  const add = host(
    "POST",
    `/v1/storefront/cart/${cartId}/lines?expectedVersion=${version}`,
    { offerId, quantity: 1 },
    guest,
  );
  if (add.status >= 400) throw new Error(`add ${add.status} ${JSON.stringify(add.json)}`);
  version = pick(add.json, "version", "Version") ?? version;
}
const checkout = host(
  "POST",
  "/v1/storefront/checkout",
  {
    cartId,
    expectedCartVersion: version,
    idempotencyKey: `r2-${Date.now()}`,
    shipping: {
      recipientName: "R2 Owned Customer",
      contactMobile: "09121234567",
      provinceName: "تهران",
      cityName: "تهران",
      postalAddress: "آدرس R2",
      postalCode: "1234567890",
    },
  },
  guest,
);
const checkoutId = pick(checkout.json, "checkoutId", "CheckoutId");
if (!checkoutId) throw new Error(`checkout ${checkout.status} ${JSON.stringify(checkout.json)}`);
report.checkoutId = checkoutId;
report.guestSecret = "[redacted-in-evidence-copy]";
report.guestSecretRaw = secret;
report.cartId = cartId;

const pay = host(
  "POST",
  `/v1/storefront/checkout/${checkoutId}/payments`,
  { cartId, idempotencyKey: randomUUID().replaceAll("-", ""), providerCode: "manual" },
  guest,
);
report.paymentStatus = pay.status;
if (pay.status >= 400) throw new Error(`pay ${pay.status} ${JSON.stringify(pay.json)}`);

waitForCode(checkoutId, "confirm_deposit");
const confirm = postOp(checkoutId, { code: "confirm_deposit" });
report.confirmStatus = confirm.status;
if (confirm.status >= 400) throw new Error(`confirm ${confirm.status} ${JSON.stringify(confirm.json)}`);

for (let i = 0; i < 60; i++) {
  const d = detail(checkoutId);
  if (sellersOf(d).length >= 2 && sellersOf(d).every((s) => pick(s, "fulfillmentId", "FulfillmentId"))) break;
  sleep(250);
}

sql(`UPDATE "order".checkouts SET placed_by_user_id = '${userId}' WHERE checkout_id = '${checkoutId}';`);
report.placedByForced = true;

const members = [];
for (const s of sellersOf(detail(checkoutId))) {
  const sellerOrderId = sellerId(s);
  const fulfillmentId = pick(s, "fulfillmentId", "FulfillmentId");
  const line = (s.lines ?? s.Lines ?? [])[0];
  const orderLineId = pick(line, "orderLineId", "OrderLineId", "id", "Id");
  const qty = Number(pick(line, "quantity", "Quantity") ?? 1);
  waitForCode(checkoutId, "mark_processing");
  let r = postOp(checkoutId, { code: "mark_processing", sellerOrderId, fulfillmentId });
  if (r.status >= 400) throw new Error(`mark_processing ${r.status} ${JSON.stringify(r.json)}`);
  waitForCode(checkoutId, "pack_selected");
  r = postOp(checkoutId, {
    code: "pack_selected",
    sellerOrderId,
    fulfillmentId,
    selections: [{ orderLineId, quantity: qty }],
  });
  if (r.status >= 400) throw new Error(`pack_selected ${r.status} ${JSON.stringify(r.json)}`);
  waitForCode(checkoutId, "create_shipment");
  r = postOp(checkoutId, {
    code: "create_shipment",
    sellerOrderId,
    fulfillmentId,
    shippingMethodCode: "post",
    carrierDisplayName: "پست",
    providerMetadataJson: JSON.stringify({
      recipientName: "R2",
      recipientPhone: "09121234567",
      postalCode: "1234567890",
      destinationAddress: "تهران",
      serviceType: "پیشتاز",
      packageCount: 1,
      weightKg: qty,
    }),
    selections: [{ orderLineId, quantity: qty }],
  });
  if (r.status >= 400) throw new Error(`create_shipment ${r.status} ${JSON.stringify(r.json)}`);
  const refreshed = detail(checkoutId);
  const seller = sellersOf(refreshed).find((x) => sellerId(x) === sellerOrderId);
  const shipment = (seller?.shipments ?? seller?.Shipments ?? []).find(
    (sh) => (sh.status ?? sh.Status) === "Created",
  );
  const shipmentId = pick(shipment, "shipmentId", "ShipmentId");
  if (!shipmentId) throw new Error("missing shipment");
  r = postOp(checkoutId, {
    code: "assign_tracking",
    sellerOrderId,
    fulfillmentId,
    shipmentId,
    trackingReference: `TRK-${String(shipmentId).slice(0, 8)}`,
  });
  if (r.status >= 400) throw new Error(`assign_tracking ${r.status}`);
  members.push({ sellerOrderId, fulfillmentId, shipmentId });
}
report.members = members;

const create = postOp(checkoutId, {
  code: "create_consolidated_package",
  shipmentIds: members.map((m) => m.shipmentId),
  shippingMethodCode: "post",
  trackingReference: "CENTRAL-T022-R2",
});
if (create.status >= 400) throw new Error(`create package ${create.status} ${JSON.stringify(create.json)}`);
const active = packagesOf(detail(checkoutId)).find((p) => (p.status ?? p.Status) === "Created");
report.packageNumber = pick(active, "packageNumber", "PackageNumber");
report.packageId = pick(active, "consolidatedPackageId", "ConsolidatedPackageId");

const ful = host("GET", `/v1/customer/orders/${checkoutId}/fulfillments`, undefined, {
  Authorization: `Bearer ${report.accessToken}`,
});
report.ownedFulfillments = {
  status: ful.status,
  preferred: pick(ful.json, "preferredCustomerTrackingReference", "PreferredCustomerTrackingReference"),
  packageNumber: pick(ful.json, "preferredCustomerTrackingPackageNumber", "PreferredCustomerTrackingPackageNumber"),
};

const foreign = host("GET", `/v1/customer/orders/${checkoutId}/fulfillments`, undefined, {
  "X-Tooba-Dev-Actor-User-Id": "cccccccc-cccc-4ccc-8ccc-0000000000cc",
});
report.foreignStatus = foreign.status;

const noSession = host("GET", `/v1/customer/orders/${checkoutId}/fulfillments`);
report.guestActorAloneStatus = noSession.status;

writeFileSync("docs/evidence/TB-P09-T022/r2-fixture.json", JSON.stringify(report, null, 2));
console.log(JSON.stringify({ ...report, guestSecretRaw: undefined, refreshToken: "[redacted]" }, null, 2));
if (ful.status !== 200 || report.ownedFulfillments.preferred !== "CENTRAL-T022-R2") process.exit(1);
if (foreign.status !== 404) process.exit(1);
