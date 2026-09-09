import { writeFileSync } from "node:fs";
import { execFileSync } from "node:child_process";

const BASE = "http://127.0.0.1:5088";
const PRODUCT = "01a05387-fbd0-7000-acd3-4382ce92c773";
const OFFER = "01a030d1-40f1-7000-95f6-b8efc58e2619";
const KG_OFFER = "01a03826-9936-7000-b499-ff26a6123a8c";
const KG_SELLER = "01a03826-97c5-7000-ad15-c9d141b1f32e";
const UNIT_KG = "01900000-0000-7000-8000-000000000002";
const ACTOR = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const SELLER_ACTOR = "01a03628-3f68-7000-844d-99f1cadb54b0";
const SELLER_PARTY = "01a030d1-40cb-7000-8abe-6d31739956c5";

const headers = {
  Host: "alpha.localhost",
  "X-Tooba-Dev-Actor-User-Id": ACTOR,
  "Content-Type": "application/json",
};

function req(method, path, body, extra = {}) {
  const args = ["-sS", "-o", "-", "-w", "\n%{http_code}", "-X", method, `${BASE}${path}`];
  const all = { ...headers, ...extra };
  for (const [key, value] of Object.entries(all)) {
    args.push("-H", `${key}: ${value}`);
  }
  if (body !== undefined) {
    args.push("--data-binary", JSON.stringify(body));
  }
  const raw = execFileSync("curl.exe", args, { encoding: "utf8" });
  const idx = raw.lastIndexOf("\n");
  const text = idx >= 0 ? raw.slice(0, idx) : raw;
  const status = Number(idx >= 0 ? raw.slice(idx + 1) : 0);
  let json;
  try {
    json = JSON.parse(text);
  } catch {
    json = text;
  }
  return { status, json };
}

function sql(q) {
  return execFileSync(
    "docker",
    ["exec", "postgres-db", "psql", "-U", "admin", "-d", "tooba_alpha", "-t", "-A", "-c", q],
    { encoding: "utf8" },
  ).trim();
}

const out = { ok: true, steps: [] };
function note(name, data) {
  const row = { name, ...(data ?? {}) };
  out.steps.push(row);
  if (data && data.ok === false) {
    out.ok = false;
  }
}

const langs = await req("GET", "/v1/admin/languages");
note("languages", { status: langs.status, count: Array.isArray(langs.json) ? langs.json.length : 0 });
let ru = Array.isArray(langs.json)
  ? langs.json.find((row) => String(row.code ?? "").toLowerCase().startsWith("ru"))
  : null;
if (!ru) {
  const created = await req("POST", "/v1/admin/languages", {
    code: "ru-RU",
    urlPrefix: "ru",
    displayName: "Russian",
    nativeName: "Русский",
    direction: "ltr",
    culture: "ru-RU",
    calendarDisplay: "Gregorian",
    active: true,
    isDefault: false,
    sortOrder: 20,
  });
  note("create-ru", { status: created.status, code: created.json?.code, languageId: created.json?.languageId });
  ru = created.json;
}
const ruId = ru?.languageId;
if (!ruId) {
  out.ok = false;
  note("ru-missing", { ok: false });
}

const kg = await req("GET", `/v1/admin/catalog/units/${UNIT_KG}`);
const translations = Array.isArray(kg.json?.translations) ? [...kg.json.translations] : [];
if (ruId && !translations.some((row) => row.languageId === ruId)) {
  translations.push({ languageId: ruId, name: "килограмм", shortName: "кг" });
}
const kgSave = await req("PUT", `/v1/admin/catalog/units/${UNIT_KG}`, {
  code: kg.json.code ?? "kg",
  dimension: kg.json.dimension ?? "Mass",
  isActive: true,
  sortOrder: kg.json.sortOrder ?? 20,
  translations,
});
note("uom-kg-ru", { status: kgSave.status, referenced: kg.json?.isReferenced });

const unitList = await req("GET", "/v1/admin/catalog/units/?language=ru-RU");
const kgRow = Array.isArray(unitList.json)
  ? unitList.json.find((row) => row.unitOfMeasureId === UNIT_KG)
  : null;
note("uom-list-ru", { status: unitList.status, localizedName: kgRow?.name, localizedShortName: kgRow?.shortName });
if (kgRow && kgRow.name !== "килограмм") {
  out.ok = false;
}

const product = await req("GET", `/v1/admin/products/${PRODUCT}`);
const qty = await req("PATCH", `/v1/admin/products/${PRODUCT}/quantity-policy`, {
  unitOfMeasureId: UNIT_KG,
  decimalPlaces: 2,
  step: null,
  expectedUpdatedAt: product.json.catalogUpdatedAt,
});
note("product-qty", {
  status: qty.status,
  unit: qty.json?.unitOfMeasureId ?? qty.json?.unitCode,
  places: qty.json?.quantityDecimalPlaces,
  step: qty.json?.quantityStep ?? null,
});
if (qty.status !== 200 || qty.json?.quantityDecimalPlaces !== 2 || qty.json?.quantityStep != null) {
  out.ok = false;
}

const sellerPartyId = SELLER_PARTY;
const sellerHeaders = {
  "X-Tooba-Dev-Actor-User-Id": SELLER_ACTOR,
  "X-Tooba-Seller-Party-Id": sellerPartyId,
};
const offerGet = await req("GET", `/v1/seller/offers/${OFFER}`, undefined, sellerHeaders);
note("offer-unit", {
  status: offerGet.status,
  unitCode: offerGet.json?.productUnitCode ?? offerGet.json?.ProductUnitCode ?? null,
  unitLabel: offerGet.json?.productUnitName ?? offerGet.json?.ProductUnitName ?? null,
});
const originalAmount = offerGet.json?.amount ?? offerGet.json?.Amount;
const offerPatch = await req(
  "PATCH",
  `/v1/seller/offers/${OFFER}`,
  { sellerSku: offerGet.json?.sellerSku ?? null, status: "Active", minimumOrderQuantity: 0.5, maximumOrderQuantity: 20 },
  sellerHeaders,
);
note("offer-minmax", {
  status: offerPatch.status,
  min: offerPatch.json?.minimumOrderQuantity,
  max: offerPatch.json?.maximumOrderQuantity,
});
if (Number(offerPatch.json?.minimumOrderQuantity) !== 0.5 || Number(offerPatch.json?.maximumOrderQuantity) !== 20) {
  out.ok = false;
}

const floor = await req("PUT", "/v1/admin/settings/quantity-rounding", { globalRoundingMode: "Floor" });
note("rounding-floor", { status: floor.status, mode: floor.json?.globalRoundingMode, fa: floor.json?.labelFa });

const priceSet = await req(
  "PUT",
  `/v1/seller/offers/${OFFER}/price`,
  { amount: 998, currency: "IRR" },
  sellerHeaders,
);
note("price-998", { status: priceSet.status, amount: priceSet.json?.amount });

const coupon = `T014F${Date.now().toString().slice(-6)}`;
const promo = await req(
  "POST",
  "/v1/seller/promotions",
  {
    name: "T014 floor 20",
    couponCode: coupon,
    discountKind: "PercentageOff",
    discountValue: 20,
    effectiveFrom: new Date().toISOString(),
  },
  sellerHeaders,
);
const promoId = promo.json?.promotionId ?? promo.json?.PromotionId;
note("promo-create", { status: promo.status, coupon, promoId });
if (promoId) {
  const act = await req("POST", `/v1/seller/promotions/${promoId}/activate`, undefined, sellerHeaders);
  note("promo-activate", { status: act.status });
}

const originalKgPrice = sql(`SELECT amount::text FROM pricing.prices WHERE offer_id = '${KG_OFFER}' ORDER BY price_id DESC LIMIT 1;`);
sql(`UPDATE pricing.prices SET amount = 998 WHERE offer_id = '${KG_OFFER}';`);
const kgCoupon = `T014KG${Date.now().toString().slice(-6)}`;
sql(`INSERT INTO promotion.promotions (
  promotion_id, name, status, priority, effective_from, stacking_policy,
  discount_kind, percentage_rate, fixed_amount, coupon_code, seller_party_id, created_at, updated_at
) VALUES (
  gen_random_uuid(), 'T014 kg floor 20', 'Active', 100, now(), 'Exclusive',
  'PercentageOff', 0.2, 0, '${kgCoupon}', '${KG_SELLER}', now(), now()
);`);
const cart = await req("POST", "/v1/storefront/cart");
const secret = cart.json?.guestSecret;
const added = await req(
  "POST",
  `/v1/storefront/cart/${cart.json.cartId}/lines?expectedVersion=${cart.json.version}`,
  { offerId: KG_OFFER, quantity: 1 },
  { "X-Tooba-Guest-Secret": secret },
);
note("cart-add", { status: added.status, qty: added.json?.lines?.[0]?.quantity, error: added.status >= 400 ? added.json : undefined });

const checkout = await req(
  "POST",
  "/v1/storefront/checkout",
  {
    cartId: cart.json.cartId,
    expectedCartVersion: added.json?.version ?? cart.json.version,
    idempotencyKey: `t014-${Date.now()}`,
    couponCode: kgCoupon,
    shipping: {
      recipientName: "T014",
      contactMobile: "+989121234567",
      provinceName: "تهران",
      cityName: "تهران",
      postalAddress: "آدرس تست T014",
      postalCode: "1234567890",
    },
  },
  { "X-Tooba-Guest-Secret": secret },
);
note("checkout", {
  status: checkout.status,
  checkoutId: checkout.json?.checkoutId,
  discount: checkout.json?.discountAmount ?? checkout.json?.sellerOrders?.[0]?.discountAmount,
  error: checkout.status >= 400 ? checkout.json : undefined,
});

const checkoutId = checkout.json?.checkoutId;
let headerBefore = "";
if (checkoutId) {
  headerBefore = sql(
    `SELECT rounding_mode_used||'|'||money_decimal_places_used||'|'||subtotal_snapshot||'|'||discount_snapshot||'|'||net_amount_before_tax||'|'||tax_snapshot||'|'||total_duty_amount||'|'||total_tax_and_duty_amount||'|'||grand_total_snapshot||'|'||total_item_count||'|'||total_quantity
     FROM "order".seller_orders WHERE checkout_id = '${checkoutId}';`,
  );
  note("header", { row: headerBefore });
  const parts = headerBefore.split("|");
  const discount = Number(parts[3]);
  const net = Number(parts[4]);
  const duty = Number(parts[6]);
  const taxAndDuty = Number(parts[5]) + duty;
  if (parts[0] !== "Floor" || discount !== 199 || net !== 799 || Number(parts[7]) !== taxAndDuty) {
    out.ok = false;
    note("floor-math", { ok: false, expected: "Floor|199|799", row: headerBefore });
  } else {
    note("floor-math", { ok: true, discount: 199, net: 799, duty, taxAndDuty: Number(parts[7]) });
  }
  const invoice = await req("GET", `/v1/admin/orders/${checkoutId}/invoice.html`);
  const html = typeof invoice.json === "string" ? invoice.json : "";
  note("invoice-html", {
    status: invoice.status,
    hasItemCount: html.includes("تعداد اقلام"),
    hasDuty: html.includes("عوارض"),
    hasTaxDuty: html.includes("جمع مالیات و عوارض"),
  });
}

const ceiling = await req("PUT", "/v1/admin/settings/quantity-rounding", { globalRoundingMode: "Ceiling" });
note("rounding-ceiling", { status: ceiling.status, mode: ceiling.json?.globalRoundingMode });
if (checkoutId) {
  const headerAfter = sql(
    `SELECT rounding_mode_used||'|'||money_decimal_places_used||'|'||subtotal_snapshot||'|'||discount_snapshot||'|'||net_amount_before_tax||'|'||tax_snapshot||'|'||total_duty_amount||'|'||total_tax_and_duty_amount||'|'||grand_total_snapshot
     FROM "order".seller_orders WHERE checkout_id = '${checkoutId}';`,
  );
  note("historical-after-ceiling", { row: headerAfter, unchanged: headerAfter === headerBefore.split("|").slice(0, 9).join("|") || headerAfter === headerBefore });
  if (!headerAfter.startsWith("Floor|")) {
    out.ok = false;
  }
}

if (originalAmount != null) {
  const restorePrice = await req(
    "PUT",
    `/v1/seller/offers/${OFFER}/price`,
    { amount: originalAmount, currency: "IRR" },
    sellerHeaders,
  );
  note("restore-price", { status: restorePrice.status });
}
if (promoId) {
  const deact = await req("POST", `/v1/seller/promotions/${promoId}/deactivate`, undefined, sellerHeaders);
  note("promo-deactivate", { status: deact.status });
}
if (originalKgPrice) {
  sql(`UPDATE pricing.prices SET amount = ${originalKgPrice} WHERE offer_id = '${KG_OFFER}';`);
}
sql(`UPDATE promotion.promotions SET status = 'Expired', updated_at = now() WHERE coupon_code = '${kgCoupon}';`);
await req("PUT", "/v1/admin/settings/quantity-rounding", { globalRoundingMode: "Nearest" });

writeFileSync("docs/evidence/TB-P09-T014/runtime-raw.json", JSON.stringify(out, null, 2));
console.log(JSON.stringify(out, null, 2));
if (!out.ok) {
  process.exit(1);
}
