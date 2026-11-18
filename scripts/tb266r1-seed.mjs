/**
 * TB-266-R1 trial seed — ONE coherent captured trial order through the PUBLIC storefront path
 * (cart -> projection -> captured selection -> commit -> sandbox payment -> mark processing -> mark packed).
 * No SQL, no factory bypass. Every row is task-labelled `TB266R1-*`.
 *
 * Usage: node scripts/tb266r1-seed.mjs <orderLabel>
 */
import fs from "node:fs";

const host = process.env.TOOBA_HOST || "http://127.0.0.1:5088";
const label = process.argv[2] || "A";
const destination = {
  countryId: "e3451206-e503-51a8-91ad-afa9ab70e330",
  administrativeAreaId: "3cefa472-d0ac-5b2e-817b-ae7df7c33382",
  cityId: "d63c8292-bc14-558a-825f-b39e25c07f61",
};
const offerId = "964ac1b4-b39e-4f29-9d41-e4402fdf21f1";
const adminHeaders = {
  "content-type": "application/json",
  "X-Tooba-Dev-Actor-User-Id": "01a03628-3f68-7000-844d-99f1cadb54b0",
};

async function call(url, init = {}, label2 = "") {
  const res = await fetch(url, init);
  const text = await res.text();
  let body;
  try { body = text ? JSON.parse(text) : null; } catch { body = text; }
  if (!res.ok) throw new Error(`${label2 || url} -> ${res.status} ${typeof body === "string" ? body : JSON.stringify(body)}`);
  return body;
}

const steps = [];
const mark = (name, data) => { steps.push({ name, at: new Date().toISOString(), data }); console.log("OK", name); };

// 1 account
const identifier = `tb266r1.${label.toLowerCase()}.${Date.now()}@example.test`;
const password = "Tb266r1-demo-pass-1";
await call(`${host}/v1/auth/register`, {
  method: "POST", headers: { "content-type": "application/json" },
  body: JSON.stringify({ identifierKind: "Email", identifier, password }),
}, "register").catch(() => null);
const login = await call(`${host}/v1/auth/login`, {
  method: "POST", headers: { "content-type": "application/json" },
  body: JSON.stringify({ identifierKind: "Email", identifier, password }),
}, "login");
const token = login.accessToken || login.AccessToken;
const authHeaders = { authorization: `Bearer ${token}`, "content-type": "application/json" };
mark("account", { identifier });

// 2 cart + line
const created = await call(`${host}/v1/storefront/cart`, { method: "POST", headers: authHeaders }, "cart");
const cartId = created.cartId || created.CartId;
const guest = created.guestSecret || created.GuestSecret;
let version = created.version ?? created.Version;
const headers = { ...authHeaders, "X-Tooba-Guest-Secret": guest, "X-Tooba-Cart-Version": String(version) };
const line = await call(`${host}/v1/storefront/cart/${cartId}/lines?expectedVersion=${version}`, {
  method: "POST", headers, body: JSON.stringify({ offerId, quantity: 1 }),
}, "line");
version = line.version ?? line.Version;
headers["X-Tooba-Cart-Version"] = String(version);
mark("cart", { cartId, version });

// 3 projection (captured)
const projection = await call(`${host}/v1/storefront/shipping/projection`, {
  method: "POST", headers,
  body: JSON.stringify({ cartId, provinceName: "تهران", cityName: "تهران", language: "fa", destination }),
}, "projection");
const preview = projection.capturedPreview ?? null;
const group = preview?.groups?.[0];
if (!group) throw new Error("no captured group");
const method = (group.methods ?? []).find((m) => (m.availableDates ?? []).length > 0);
if (!method) throw new Error(`no selectable captured method: ${JSON.stringify(group.methods)}`);
const day = method.availableDates[0];
const slot = day.slots[0];
mark("projection", {
  groupKey: group.groupKey,
  serviceCode: method.serviceCode,
  optionCode: method.optionCode,
  goods: group.finalGoodsAmountAfterDiscountAndTax,
  date: day.date,
  slot,
  selectionRevision: preview.selectionRevision,
});

// 4 captured selection
const selection = await call(`${host}/v1/storefront/shipping/selection`, {
  method: "PUT", headers,
  body: JSON.stringify({
    cartId,
    expectedCartVersion: version,
    recipientName: "TB266R1 گیرنده",
    contactMobile: "09120000000",
    provinceName: "تهران",
    cityName: "تهران",
    postalAddress: "خیابان ولیعصر، پلاک ۱، واحد ۲",
    postalCode: "1969764511",
    savedAddressId: null,
    shippingMethodCode: "",
    selectedDeliveryDate: "",
    selectedDeliveryTimeWindow: "",
    customerNote: `TB266R1-${label}`,
    firstName: "TB266R1",
    lastName: "گیرنده",
    shippingGroups: null,
    capturedGroups: [{
      groupKey: group.groupKey,
      serviceCode: method.serviceCode,
      optionCode: method.optionCode ?? null,
      selectedDeliveryDate: day.date,
      expectedSnapshotId: method.confirmationSnapshotId,
      expectedSourceRevision: method.confirmationSourceRevision,
      expectedSelectionVersion: method.confirmationSelectionVersion,
      expectedAreaGroupId: method.confirmationAreaGroupId,
      expectedRateRowId: method.confirmationRateRowId,
      expectedGoodsAmount: group.finalGoodsAmountAfterDiscountAndTax,
      selectedSlotStartTicks: slot.startTicks,
      selectedSlotEndTicks: slot.endTicks,
    }],
    expectedSelectionRevision: preview.selectionRevision ?? "0",
  }),
}, "selection");
const selectionRevision = selection.selectionRevision ?? selection.SelectionRevision;
version = selection.cartVersion ?? selection.CartVersion ?? version;
mark("selection", { selectionRevision, version });

// 5 commit (checkout)
const idem = `tb266r1-${label.toLowerCase()}-${Date.now()}`;
const checkout = await call(`${host}/v1/storefront/shipping/commit`, {
  method: "POST", headers,
  body: JSON.stringify({ cartId, expectedCartVersion: version, idempotencyKey: idem, expectedSelectionRevision: selectionRevision }),
}, "commit");
const checkoutId = checkout.checkoutId ?? checkout.CheckoutId;
version = checkout.cartVersion ?? checkout.CartVersion ?? version;
mark("commit", { checkoutId, payable: checkout.payableAmount, shippingMethod: checkout.shippingMethodLabel, version });

// 6 payment initiate + sandbox complete
const initiate = await call(`${host}/v1/storefront/checkout/${checkoutId}/payments`, {
  method: "POST", headers,
  body: JSON.stringify({ cartId, expectedCartVersion: version, idempotencyKey: `${idem}-pay`, expectedSelectionRevision: selectionRevision }),
}, "initiate");
const pick = (o, ...keys) => keys.map((k) => o?.[k]).find((v) => v != null);
const paymentId = pick(initiate, "paymentId", "PaymentId");
const attemptId = pick(initiate, "attemptId", "AttemptId");
const providerRequestReference = pick(initiate, "providerRequestReference", "ProviderRequestReference");
mark("payment", { paymentId, attemptId, providerRequestReference, status: pick(initiate, "paymentStatus", "PaymentStatus") });
if (!paymentId || !attemptId || !providerRequestReference) {
  throw new Error(`initiate must expose payment/attempt/reference: ${JSON.stringify(initiate).slice(0, 400)}`);
}

const sandbox = await call(`${host}/v1/storefront/payments/${paymentId}/sandbox/complete`, {
  method: "POST", headers: authHeaders,
  body: JSON.stringify({ cartId, attemptId, providerRequestReference, outcome: "success" }),
}, "sandbox");
mark("sandbox", { status: pick(sandbox, "status", "Status"), orderNumber: pick(sandbox, "orderNumber", "OrderNumber") });

// 7 owning admin operations: processing -> packed (whole seller order of this coherent checkout)
const adminLogin = await call(`${host}/v1/auth/login`, {
  method: "POST", headers: { "content-type": "application/json" },
  body: JSON.stringify({ identifierKind: "Email", identifier: "admin-actor@tooba.local", password: "admin-dev-horse-1" }),
}, "admin-login");
const adminToken = adminLogin.accessToken || adminLogin.AccessToken;
const adminHeaders2 = { authorization: `Bearer ${adminToken}`, "content-type": "application/json" };
const detail = await call(`${host}/v1/admin/orders/${checkoutId}`, { headers: adminHeaders2 }, "admin-detail");
const seller = (detail.sellerOrders ?? [])[0];
const fulfillmentId = pick(seller, "fulfillmentId", "FulfillmentId");
const sellerOrderId = pick(seller, "sellerOrderId", "SellerOrderId");
const selections = (seller?.lines ?? []).map((l) => ({ orderLineId: pick(l, "orderLineId", "OrderLineId"), quantity: pick(l, "quantity", "Quantity") }));
mark("admin-detail", { sellerOrderId, fulfillmentId, lineCount: selections.length, capturedGroups: (detail.shippingGroups ?? []).length });

for (const code of ["mark_processing", "mark_packed"]) {
  await call(`${host}/v1/admin/orders/${checkoutId}/operations`, {
    method: "POST", headers: adminHeaders2,
    body: JSON.stringify({ code, sellerOrderId, fulfillmentId, selections }),
  }, code);
  mark(code, { sellerOrderId, fulfillmentId });
}

const after = await call(`${host}/v1/admin/orders/${checkoutId}`, { headers: adminHeaders2 }, "admin-detail-after");
const afterSeller = (after.sellerOrders ?? [])[0];
mark("verified", {
  status: after.status,
  paymentState: after.paymentState,
  sellerStatus: pick(afterSeller, "status", "Status"),
  shippingGroups: (after.shippingGroups ?? []).length,
  operationalGroups: (after.operationalDeliveryGroups ?? []).length,
  createParcelEligible: (after.shippingGroups ?? [])[0]?.createParcelEligible ?? null,
});

fs.writeFileSync(`D:/temp/tb266r1/seed-${label}.json`, JSON.stringify({
  label, identifier, cartId, checkoutId, paymentId, offerId, groupKey: group.groupKey,
  serviceCode: method.serviceCode, optionCode: method.optionCode, date: day.date, slot, steps,
}, null, 2));
console.log("SEED_DONE", label, "checkout", checkoutId);
