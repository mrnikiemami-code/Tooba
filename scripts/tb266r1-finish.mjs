/**
 * TB-266-R1 — finish the already-committed captured checkout through PUBLIC storefront payment
 * (sandbox confirmation) and then the owning admin operations (mark_processing, mark_packed).
 */
import fs from "node:fs";

const host = process.env.TOOBA_HOST || "http://127.0.0.1:5088";
const checkoutId = process.argv[2];
const cartId = process.argv[3];
const paymentId = process.argv[4];
const attemptId = process.argv[5];
const reference = process.argv[6];
const identifier = process.argv[7];
const password = process.argv[8] || "Tb266r1-demo-pass-1";
const adminHeaders = {
  "content-type": "application/json",
  "X-Tooba-Dev-Actor-User-Id": "01a03628-3f68-7000-844d-99f1cadb54b0",
};

async function call(url, init = {}, label = "") {
  const res = await fetch(url, init);
  const text = await res.text();
  let body;
  try { body = text ? JSON.parse(text) : null; } catch { body = text; }
  if (!res.ok) throw new Error(`${label || url} -> ${res.status} ${typeof body === "string" ? body : JSON.stringify(body)}`);
  return body;
}

const login = await call(`${host}/v1/auth/login`, {
  method: "POST", headers: { "content-type": "application/json" },
  body: JSON.stringify({ identifierKind: "Email", identifier, password }),
}, "login");
const token = login.accessToken || login.AccessToken;
const authHeaders = { authorization: `Bearer ${token}`, "content-type": "application/json" };

const paid = await call(`${host}/v1/storefront/payments/${paymentId}/sandbox/complete`, {
  method: "POST", headers: authHeaders,
  body: JSON.stringify({ cartId, attemptId, providerRequestReference: reference, outcome: "success" }),
}, "sandbox");
console.log("PAID", JSON.stringify({ status: paid.status, orderNumber: paid.orderNumber }).slice(0, 300));

const detail = await call(`${host}/v1/admin/orders/${checkoutId}`, { headers: adminHeaders }, "adminDetail");
const fulfillmentId = detail.operationalDeliveryGroups?.[0]?.members?.[0]?.shippingGroupId
  ? null
  : null;
const sellerOrderId = detail.sellerOrders?.[0]?.sellerOrderId ?? detail.SellerOrders?.[0]?.sellerOrderId;
console.log("DETAIL status", detail.status, "payment", detail.paymentState, "groups", (detail.operationalDeliveryGroups ?? []).length);

async function op(code, extra = {}) {
  const body = { code, ...extra };
  const res = await call(`${host}/v1/admin/orders/${checkoutId}/operations`, {
    method: "POST", headers: adminHeaders, body: JSON.stringify(body),
  }, code);
  console.log("OP", code, "->", JSON.stringify(res).slice(0, 400));
  return res;
}

const ops = await call(`${host}/v1/admin/orders/${checkoutId}/operations`, { headers: adminHeaders }, "opsList");
fs.writeFileSync("D:/temp/tb266r1/opsA.json", JSON.stringify(ops, null, 2));
console.log("OPS keys", Object.keys(ops).join(","));
const targets = ops.targets ?? ops.Targets ?? [];
console.log("TARGETS", JSON.stringify(targets).slice(0, 1500));
console.log("ACTIONS", JSON.stringify((ops.actions ?? ops.Actions ?? []).map((a) => a.code ?? a.Code)).slice(0, 800));
