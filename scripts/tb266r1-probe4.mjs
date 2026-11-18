/**
 * TB-266-R1 probe #4 — explicit Platform-responsibility offer.
 */
import fs from "node:fs";

const host = process.env.TOOBA_HOST || "http://127.0.0.1:5088";
const destination = {
  countryId: "e3451206-e503-51a8-91ad-afa9ab70e330",
  administrativeAreaId: "3cefa472-d0ac-5b2e-817b-ae7df7c33382",
  cityId: "d63c8292-bc14-558a-825f-b39e25c07f61",
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
const offerId = process.argv[2] || "964ac1b4-b39e-4f29-9d41-e4402fdf21f1";
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
const projection = await call(`${host}/v1/storefront/shipping/projection`, {
  method: "POST", headers,
  body: JSON.stringify({ cartId, provinceName: "تهران", cityName: "تهران", language: "fa", destination }),
}, "projection");
fs.writeFileSync("D:/temp/tb266r1/probe4.json", JSON.stringify({ projection, cartId, guest, version, offerId, identifier, password, token }, null, 2));
console.log("PROBE4", cartId, "offer", offerId);
console.log(JSON.stringify(projection.capturedPreview ?? null, null, 1).slice(0, 7000));
