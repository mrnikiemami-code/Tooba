import { writeFileSync } from "node:fs";

const BASE = "http://127.0.0.1:5088";
const KG_OFFER = "01a03826-9936-7000-b499-ff26a6123a8c";
const ARMAN_OFFER = "01a030d1-40f1-7000-95f6-b8efc58e2619";

const headers = {
  Host: "alpha.localhost",
  "Content-Type": "application/json",
};

async function req(method, path, body, extra = {}) {
  const r = await fetch(`${BASE}${path}`, {
    method,
    headers: { ...headers, ...extra },
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });
  const text = await r.text();
  let json;
  try {
    json = JSON.parse(text);
  } catch {
    json = text;
  }
  return { status: r.status, json, text };
}

function pick(obj, ...keys) {
  if (!obj || typeof obj !== "object") return undefined;
  for (const key of keys) if (obj[key] != null) return obj[key];
  return undefined;
}

const listing = await req("GET", "/v1/storefront/products?inStock=true&pageSize=48");
const products = listing.json?.products ?? listing.json?.Products ?? [];
const sample = products.slice(0, 12).map((p) => ({
  title: pick(p, "title", "name", "Title"),
  offer: pick(p, "primaryOfferId", "PrimaryOfferId"),
  seller: pick(p, "sellerPartyId", "SellerPartyId"),
  sellerName: pick(p, "sellerName", "SellerName"),
  stock: pick(p, "inStock", "InStock"),
}));

const cart = await req("POST", "/v1/storefront/cart");
const secret = pick(cart.json, "guestSecret", "GuestSecret");
const guest = { "X-Tooba-Guest-Secret": secret };
const cartId = pick(cart.json, "cartId", "CartId");
const a1 = await req(
  "POST",
  `/v1/storefront/cart/${cartId}/lines?expectedVersion=${pick(cart.json, "version", "Version")}`,
  { offerId: KG_OFFER, quantity: 1.25 },
  guest,
);
const a2 = await req(
  "POST",
  `/v1/storefront/cart/${cartId}/lines?expectedVersion=${pick(a1.json, "version", "Version")}`,
  { offerId: ARMAN_OFFER, quantity: 1 },
  guest,
);

const lines = (a2.json?.lines ?? a2.json?.Lines ?? []).map((l) => ({
  seller: pick(l, "sellerPartyId", "SellerPartyId"),
  offer: pick(l, "offerId", "OfferId"),
  title: pick(l, "title", "Title", "productTitle"),
  qty: pick(l, "quantity", "Quantity"),
}));
const sellers = [...new Set(lines.map((l) => String(l.seller)))];

let proj = null;
if (a2.status < 400) {
  proj = await req(
    "POST",
    "/v1/storefront/shipping/projection",
    { cartId, provinceName: "تهران", methodCode: "post:express", language: "fa" },
    guest,
  );
}

const out = {
  productCount: products.length,
  sample,
  cartId,
  guestSecretPresent: !!secret,
  addKg: a1.status,
  addArman: a2.status,
  itemCount: pick(a2.json, "itemCount", "ItemCount"),
  lines,
  sellers,
  sellerCount: sellers.length,
  projectionStatus: proj?.status,
  projection: proj?.json
    ? {
        sellerCount: pick(proj.json, "sellerCount", "SellerCount"),
        maxSellerPreparationDays: pick(proj.json, "maxSellerPreparationDays", "MaxSellerPreparationDays"),
        selectedMethodCode: pick(proj.json, "selectedMethodCode", "SelectedMethodCode"),
        shippingAmount: pick(proj.json, "shippingAmount", "ShippingAmount", "selectedShippingAmount"),
        minimumDeliveryDate: pick(
          proj.json,
          "minimumDeliveryDate",
          "MinimumDeliveryDate",
          "earliestDeliveryDate",
        ),
        methodCodes: (pick(proj.json, "methods", "Methods") ?? []).map((m) =>
          pick(m, "methodCode", "MethodCode", "code"),
        ),
        deliveryDates: (pick(proj.json, "deliveryDates", "DeliveryDates", "dates") ?? [])
          .slice(0, 5)
          .map((d) => pick(d, "date", "Date", "value") ?? d),
        keys: Object.keys(proj.json),
      }
    : proj?.text,
};

writeFileSync(new URL("./_r1_probe-out.json", import.meta.url), JSON.stringify(out, null, 2));
console.log(JSON.stringify(out, null, 2));
