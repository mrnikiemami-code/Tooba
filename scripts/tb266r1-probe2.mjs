/**
 * TB-266-R1 probe #2 — captured preview WITH a resolved destination (read-only except its own cart).
 */
import fs from "node:fs";

const host = process.env.TOOBA_HOST || "http://127.0.0.1:5088";
const sellerHeaders = {
  "content-type": "application/json",
  "X-Tooba-Seller-Party-Id": "01a030d1-40cb-7000-8abe-6d31739956c5",
  "X-Tooba-Dev-Actor-User-Id": "01a03628-3f68-7000-844d-99f1cadb54b0",
};
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
  body: JSON.stringify({
    cartId, provinceName: "تهران", cityName: "تهران", language: "fa",
    destination,
  }),
}, "projection");

fs.writeFileSync("D:/temp/tb266r1/projection2.json", JSON.stringify({
  projection, cartId, guest, version, offerId: hit.offerId ?? hit.OfferId, token, identifier, password,
}, null, 2));
console.log("PROBE2_OK", cartId);
console.log(JSON.stringify(projection.capturedPreview ?? projection.CapturedPreview ?? null, null, 1).slice(0, 6000));
