/**
 * TB-266-R1 probe — read the REAL captured storefront projection for the task-owned seller so the trial
 * orders can be created from the same lawful captured selection the UI sends (no SQL, no fake evidence).
 * Read-only except the throwaway probe cart it creates.
 */
import fs from "node:fs";

const host = process.env.TOOBA_HOST || "http://127.0.0.1:5088";
const sellerHeaders = {
  "content-type": "application/json",
  "X-Tooba-Seller-Party-Id": "01a030d1-40cb-7000-8abe-6d31739956c5",
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

const identifier = `tb266r1.probe.${Date.now()}@example.test`;
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

const offers = await call(`${host}/v1/seller/offers`, { headers: sellerHeaders }, "offers");
const rows = Array.isArray(offers) ? offers : [];
const hit = rows.find((o) => Number(o.availableUnits ?? o.AvailableUnits ?? 0) >= 10);
if (!hit) throw new Error("no seller offer with stock");

const created = await call(`${host}/v1/storefront/cart`, { method: "POST", headers: authHeaders }, "cart");
const cartId = created.cartId || created.CartId;
const guest = created.guestSecret || created.GuestSecret;
let version = created.version ?? created.Version;
const headers = { ...authHeaders, "X-Tooba-Guest-Secret": guest, "X-Tooba-Cart-Version": String(version) };
const line = await call(`${host}/v1/storefront/cart/${cartId}/lines?expectedVersion=${version}`, {
  method: "POST", headers, body: JSON.stringify({ offerId: hit.offerId ?? hit.OfferId, quantity: 1 }),
}, "line");
version = line.version ?? line.Version;
headers["X-Tooba-Cart-Version"] = String(version);

const projection = await call(`${host}/v1/storefront/shipping/projection`, {
  method: "POST", headers,
  body: JSON.stringify({ cartId, provinceName: "تهران", cityName: "تهران", language: "fa", methodCode: "tipax\u001Fexpress" }),
}, "projection");

fs.writeFileSync("D:/temp/tb266r1-projection.json", JSON.stringify({ projection, cartId, guest, version, offerId: hit.offerId ?? hit.OfferId }, null, 2));
console.log("PROBE_OK", cartId, "keys", Object.keys(projection).join(","));
console.log("groups", JSON.stringify((projection.capturedPreview || projection.CapturedPreview || {})?.groups || null)?.slice(0, 1200));
