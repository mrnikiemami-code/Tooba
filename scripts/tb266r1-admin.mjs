/**
 * TB-266-R1 — admin helper: login as the Development admin actor and run owning admin operations
 * through the public admin API (no SQL, no factory).
 * Usage: node scripts/tb266r1-admin.mjs <checkoutId> <code> [json payload]
 */
const host = process.env.TOOBA_HOST || "http://127.0.0.1:5088";
const checkoutId = process.argv[2];
const code = process.argv[3];
const payload = process.argv[4] ? JSON.parse(process.argv[4]) : {};

const res = await fetch(`${host}/v1/auth/login`, {
  method: "POST", headers: { "content-type": "application/json" },
  body: JSON.stringify({ identifierKind: "Email", identifier: "admin-actor@tooba.local", password: "admin-dev-horse-1" }),
});
const login = await res.json();
if (!res.ok) throw new Error(`login -> ${res.status} ${JSON.stringify(login)}`);
const token = login.accessToken || login.AccessToken;
const headers = { authorization: `Bearer ${token}`, "content-type": "application/json" };

const url = code === "DETAIL"
  ? `${host}/v1/admin/orders/${checkoutId}`
  : code === "OPS"
    ? `${host}/v1/admin/orders/${checkoutId}/operations`
    : `${host}/v1/admin/orders/${checkoutId}/operations`;
const init = code === "DETAIL" || code === "OPS"
  ? { headers }
  : { method: "POST", headers, body: JSON.stringify({ code, ...payload }) };
const out = await fetch(url, init);
const text = await out.text();
let body;
try { body = text ? JSON.parse(text) : null; } catch { body = text; }
console.log("STATUS", out.status);
console.log(typeof body === "string" ? body.slice(0, 1500) : JSON.stringify(body).slice(0, 4000));
