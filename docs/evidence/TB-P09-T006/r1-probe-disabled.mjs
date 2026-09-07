import http from "node:http";
import { randomUUID } from "node:crypto";
import { writeFileSync } from "node:fs";

const ADMIN = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const CUSTOMER = "aaaaaaaa-aaaa-4aaa-8aaa-000000000009";
const SELLER = "01a03628-3f68-7000-844d-99f1cadb54b0";
const SELLER_PARTY = "01a030d1-40cb-7000-8abe-6d31739956c5";
const OFFER = "01a04402-7a75-7000-b614-6f093cb072ac";
const ADDRESS = "aaaaaaaa-aaaa-4aaa-8aaa-0000000000a1";

function req(method, path, { headers = {}, body } = {}) {
  return new Promise((resolve, reject) => {
    const payload = body == null ? null : Buffer.from(JSON.stringify(body), "utf8");
    const r = http.request(
      {
        host: "127.0.0.1",
        port: 5088,
        path,
        method,
        headers: {
          Accept: "application/json",
          Host: "alpha.localhost",
          ...(payload ? { "Content-Type": "application/json", "Content-Length": payload.length } : {}),
          ...headers,
        },
      },
      (res) => {
        const chunks = [];
        res.on("data", (c) => chunks.push(c));
        res.on("end", () => {
          const raw = Buffer.concat(chunks).toString("utf8");
          let json = null;
          try {
            json = raw ? JSON.parse(raw) : null;
          } catch {
            json = raw;
          }
          resolve({ status: res.statusCode, json, raw });
        });
      },
    );
    r.on("error", reject);
    if (payload) r.write(payload);
    r.end();
  });
}
function pick(obj, ...keys) {
  for (const k of keys) if (obj && obj[k] != null) return obj[k];
}

const out = {};
const deny = await req("PATCH", `/v1/seller/offers/${OFFER}`, {
  headers: {
    "X-Tooba-Dev-Actor-User-Id": SELLER,
    "X-Tooba-Seller-Party-Id": SELLER_PARTY,
  },
  body: { returnPolicyChoice: "Custom", customReturnWindowDays: 10 },
});
out.overrideDeny = { status: deny.status, title: pick(deny.json, "title", "Title"), body: deny.json };

const methods = await req("GET", "/v1/admin/shipping-methods", {
  headers: { "X-Tooba-Dev-Actor-User-Id": ADMIN },
});
out.methods = methods.json;
out.tipaxListed = Array.isArray(methods.json) && methods.json.some((x) => x.code === "tipax");

await req("POST", `/v1/admin/wallets/${CUSTOMER}/adjustments`, {
  headers: { "X-Tooba-Dev-Actor-User-Id": ADMIN },
  body: { amount: 2_000_000, direction: "Credit", reason: "r1-disable-tipax", idempotencyKey: randomUUID() },
});
const cartCreate = await req("POST", "/v1/storefront/cart");
const cartId = pick(cartCreate.json, "cartId", "CartId");
const guest = pick(cartCreate.json, "guestSecret", "GuestSecret");
let version = pick(cartCreate.json, "version", "Version") ?? 1;
const ch = { "X-Tooba-Guest-Secret": guest };
const add = await req("POST", `/v1/storefront/cart/${cartId}/lines?expectedVersion=${version}`, {
  headers: { ...ch, "X-Tooba-Cart-Version": String(version) },
  body: { offerId: OFFER, quantity: 1 },
});
version = pick(add.json, "version", "Version") ?? version + 1;
const submit = await req("POST", "/v1/storefront/checkout", {
  headers: { ...ch, "X-Tooba-Dev-Actor-User-Id": CUSTOMER, "X-Tooba-Cart-Version": String(version) },
  body: {
    cartId,
    expectedCartVersion: version,
    idempotencyKey: randomUUID(),
    shipping: {
      recipientName: "R1 disable",
      contactMobile: "+989120000093",
      provinceName: "تهران",
      cityName: "تهران",
      postalAddress: "آدرس",
      postalCode: "1919911111",
      savedAddressId: ADDRESS,
    },
  },
});
const checkoutId = pick(submit.json, "checkoutId", "CheckoutId");
await req("POST", `/v1/storefront/checkout/${checkoutId}/payments`, {
  headers: { ...ch, "X-Tooba-Dev-Actor-User-Id": CUSTOMER },
  body: { cartId, idempotencyKey: randomUUID(), providerCode: "wallet", useWallet: true },
});
for (let i = 0; i < 30; i++) {
  await new Promise((r) => setTimeout(r, 300));
  const od = await req("GET", `/v1/admin/orders/${checkoutId}`, {
    headers: { "X-Tooba-Dev-Actor-User-Id": ADMIN },
  });
  const so = (pick(od.json, "sellerOrders", "SellerOrders") || [])[0];
  const fid = pick(so, "fulfillmentId", "FulfillmentId");
  if (!fid) continue;
  const soid = pick(so, "sellerOrderId", "SellerOrderId");
  const lid = pick((pick(so, "lines", "Lines") || [])[0], "orderLineId", "OrderLineId");
  await req("POST", `/v1/admin/orders/${checkoutId}/operations`, {
    headers: { "X-Tooba-Dev-Actor-User-Id": ADMIN },
    body: { code: "mark_processing", fulfillmentId: fid, sellerOrderId: soid },
  });
  await req("POST", `/v1/admin/orders/${checkoutId}/operations`, {
    headers: { "X-Tooba-Dev-Actor-User-Id": ADMIN },
    body: {
      code: "mark_packed",
      fulfillmentId: fid,
      sellerOrderId: soid,
      selections: [{ orderLineId: lid, quantity: 1 }],
    },
  });
  const tipax = await req("POST", `/v1/admin/orders/${checkoutId}/operations`, {
    headers: { "X-Tooba-Dev-Actor-User-Id": ADMIN },
    body: {
      code: "create_shipment",
      fulfillmentId: fid,
      sellerOrderId: soid,
      shippingMethodCode: "tipax",
      providerMetadataJson: JSON.stringify({
        recipientName: "R1",
        recipientPhone: "09120000093",
        fullAddress: "آدرس",
      }),
      selections: [{ orderLineId: lid, quantity: 1 }],
    },
  });
  out.tipaxReject = {
    checkoutId,
    status: tipax.status,
    title: pick(tipax.json, "title", "Title"),
  };
  break;
}

writeFileSync(
  new URL("./r1-runtime-enablement-override-deny.json", import.meta.url),
  JSON.stringify(out, null, 2),
);
console.log(JSON.stringify(out, null, 2));
