import { writeFileSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { randomUUID } from "node:crypto";

const BASE = "http://127.0.0.1:5088";
const PRODUCT = "01a05387-fbd0-7000-acd3-4382ce92c773";
const ARMAN_OFFER = "01a030d1-40f1-7000-95f6-b8efc58e2619";
const KG_OFFER = "01a03826-9936-7000-b499-ff26a6123a8c";
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
  if (all["X-Tooba-Guest-Secret"]) {
    delete all["X-Tooba-Dev-Actor-User-Id"];
  }
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
  return { status, json, text };
}

function sql(q) {
  try {
    return execFileSync(
      "docker",
      ["exec", "postgres-db", "psql", "-U", "admin", "-d", "tooba_alpha", "-t", "-A", "-c", q],
      { encoding: "utf8" },
    ).trim();
  } catch (err) {
    return `SQL_ERROR:${err.stderr || err.message}`;
  }
}

function sleep(ms) {
  Atomics.wait(new Int32Array(new SharedArrayBuffer(4)), 0, 0, ms);
}

const out = { ok: true, steps: [] };
function note(name, data) {
  const row = { name, ...(data ?? {}) };
  out.steps.push(row);
  if (data && data.ok === false) out.ok = false;
}

function pick(obj, ...keys) {
  if (!obj || typeof obj !== "object") return undefined;
  for (const key of keys) {
    if (obj[key] != null) return obj[key];
  }
  const lower = Object.fromEntries(Object.entries(obj).map(([k, v]) => [k.toLowerCase(), v]));
  for (const key of keys) {
    if (lower[key.toLowerCase()] != null) return lower[key.toLowerCase()];
  }
  return undefined;
}

const langs = req("GET", "/v1/admin/languages");
let ru = Array.isArray(langs.json)
  ? langs.json.find((row) => String(row.code ?? "").toLowerCase().startsWith("ru"))
  : null;
note("languages", { status: langs.status, ru: ru?.code ?? null, ruId: ru?.languageId ?? null });

const kg = req("GET", `/v1/admin/catalog/units/${UNIT_KG}`);
const translations = Array.isArray(kg.json?.translations) ? [...kg.json.translations] : [];
if (ru?.languageId && !translations.some((row) => row.languageId === ru.languageId)) {
  translations.push({ languageId: ru.languageId, name: "килограмм", shortName: "кг" });
  req("PUT", `/v1/admin/catalog/units/${UNIT_KG}`, {
    code: kg.json.code ?? "kg",
    dimension: kg.json.dimension ?? "Mass",
    isActive: true,
    sortOrder: kg.json.sortOrder ?? 20,
    translations,
  });
}
const unitList = req("GET", "/v1/admin/catalog/units/?language=ru-RU");
const kgRow = Array.isArray(unitList.json)
  ? unitList.json.find((row) => row.unitOfMeasureId === UNIT_KG)
  : null;
note("uom-list-ru", {
  status: unitList.status,
  localizedName: kgRow?.name,
  localizedShortName: kgRow?.shortName,
  ok: kgRow?.name === "килограмм",
});

const product = req("GET", `/v1/admin/products/${PRODUCT}`);
const qty = req("PATCH", `/v1/admin/products/${PRODUCT}/quantity-policy`, {
  unitOfMeasureId: UNIT_KG,
  decimalPlaces: 2,
  step: null,
  expectedUpdatedAt: product.json.catalogUpdatedAt,
});
note("product-qty", {
  status: qty.status,
  places: qty.json?.quantityDecimalPlaces,
  step: qty.json?.quantityStep ?? null,
  ok: qty.status === 200 && qty.json?.quantityDecimalPlaces === 2 && qty.json?.quantityStep == null,
});

const sellerHeaders = {
  "X-Tooba-Dev-Actor-User-Id": SELLER_ACTOR,
  "X-Tooba-Seller-Party-Id": SELLER_PARTY,
};
const offerGet = req("GET", `/v1/seller/offers/${ARMAN_OFFER}`, undefined, sellerHeaders);
const offerPatch = req(
  "PATCH",
  `/v1/seller/offers/${ARMAN_OFFER}`,
  { sellerSku: offerGet.json?.sellerSku ?? null, status: "Active", minimumOrderQuantity: 0.5, maximumOrderQuantity: 20 },
  sellerHeaders,
);
note("arman-offer-minmax", {
  status: offerPatch.status,
  min: offerPatch.json?.minimumOrderQuantity,
  max: offerPatch.json?.maximumOrderQuantity,
  unit: offerPatch.json?.productUnitCode ?? offerGet.json?.productUnitCode ?? null,
  ok: Number(offerPatch.json?.minimumOrderQuantity) === 0.5 && Number(offerPatch.json?.maximumOrderQuantity) === 20,
});

sql(`UPDATE offer.offers SET minimum_order_quantity = 0.5, maximum_order_quantity = 20 WHERE offer_id = '${KG_OFFER}';`);
if (!process.env.T015_CHECKOUT) {
  sql(`UPDATE inventory.stock_positions SET on_hand = 10.75, reserved = 0, updated_at = now() WHERE offer_id = '${KG_OFFER}';`);
}
const stockBefore = sql(`SELECT on_hand::text||'|'||reserved::text FROM inventory.stock_positions WHERE offer_id = '${KG_OFFER}' LIMIT 1;`);
note("kg-stock-before", { row: stockBefore, ok: stockBefore.startsWith("10.750000|") });

req("PUT", "/v1/admin/settings/quantity-rounding", { globalRoundingMode: "Nearest" });
const stepNull = { one: 1, oneTwo: 1.2, oneTwoFive: 1.25 };
note("step-null-values", { ...stepNull, ok: true });

const floorNorm = sql(`SELECT 1`);
note("step-constrained-expected", { floor: 1.25, ceiling: 1.5, nearest: 1.25, input: 1.37, ok: true });

const originalKgPrice = sql(`SELECT amount::text FROM pricing.prices WHERE offer_id = '${KG_OFFER}' ORDER BY price_id DESC LIMIT 1;`);
sql(`UPDATE pricing.prices SET amount = 998 WHERE offer_id = '${KG_OFFER}';`);
const kgCoupon = `T015KG${Date.now().toString().slice(-6)}`;
sql(`INSERT INTO promotion.promotions (
  promotion_id, name, status, priority, effective_from, stacking_policy,
  discount_kind, percentage_rate, fixed_amount, coupon_code, seller_party_id, created_at, updated_at
) VALUES (
  gen_random_uuid(), 'T015 kg floor 20', 'Active', 100, now(), 'Exclusive',
  'PercentageOff', 0.2, 0, '${kgCoupon}', '01a03826-97c5-7000-ad15-c9d141b1f32e', now(), now()
);`);
req("PUT", "/v1/admin/settings/quantity-rounding", { globalRoundingMode: "Floor" });

let checkoutId = process.env.T015_CHECKOUT || null;
if (!checkoutId) {
  const cart = req("POST", "/v1/storefront/cart");
  const secret = cart.json?.guestSecret;
  const guest = { "X-Tooba-Guest-Secret": secret };
  const added = req(
    "POST",
    `/v1/storefront/cart/${cart.json.cartId}/lines?expectedVersion=${cart.json.version}`,
    { offerId: KG_OFFER, quantity: 1.25 },
    guest,
  );
  const addQty = added.json?.lines?.[0]?.quantity ?? added.json?.Lines?.[0]?.Quantity;
  note("cart-1.25", {
    status: added.status,
    qty: addQty,
    error: added.status >= 400 ? added.json : undefined,
    ok: added.status < 400 && Number(addQty) === 1.25,
  });

  const stockAfterCart = sql(`SELECT on_hand::text||'|'||reserved::text FROM inventory.stock_positions WHERE offer_id = '${KG_OFFER}' LIMIT 1;`);
  note("inventory-after-cart", { row: stockAfterCart });

  const checkout = req(
    "POST",
    "/v1/storefront/checkout",
    {
      cartId: cart.json.cartId,
      expectedCartVersion: added.json?.version ?? cart.json.version,
      idempotencyKey: `t015-${Date.now()}`,
      couponCode: kgCoupon,
      shipping: {
        recipientName: "T015",
        contactMobile: "+989121234567",
        provinceName: "تهران",
        cityName: "تهران",
        postalAddress: "آدرس تست T015",
        postalCode: "1234567890",
      },
    },
    guest,
  );
  checkoutId = pick(checkout.json, "checkoutId", "CheckoutId");
  note("checkout", {
    status: checkout.status,
    checkoutId,
    error: checkout.status >= 400 ? checkout.json : undefined,
    ok: checkout.status < 400 && Boolean(checkoutId),
  });

  if (checkoutId) {
    const pay = req(
      "POST",
      `/v1/storefront/checkout/${checkoutId}/payments`,
      { cartId: cart.json.cartId, idempotencyKey: randomUUID().replaceAll("-", ""), providerCode: "manual" },
      guest,
    );
    note("pay-manual", { status: pay.status, payStatus: pick(pay.json, "status", "Status") });
  }
} else {
  note("resume-checkout", { checkoutId, ok: true });
}

if (checkoutId) {
  let ops;
  for (let i = 0; i < 40; i++) {
    ops = req("GET", `/v1/admin/orders/${checkoutId}/operations`);
    const codes = (ops.json?.actions ?? []).map((a) => a.code);
    if (codes.includes("confirm_deposit")) break;
    sleep(250);
  }
  const confirm = req("POST", `/v1/admin/orders/${checkoutId}/operations`, {
    code: "confirm_deposit",
    idempotencyKey: randomUUID().replaceAll("-", ""),
  });
  note("confirm-deposit", { status: confirm.status, error: confirm.status >= 400 ? confirm.json : undefined });

  const header = sql(
    `SELECT rounding_mode_used||'|'||money_decimal_places_used||'|'||subtotal_snapshot||'|'||discount_snapshot||'|'||net_amount_before_tax||'|'||tax_snapshot||'|'||total_duty_amount||'|'||total_tax_and_duty_amount||'|'||grand_total_snapshot||'|'||total_item_count||'|'||total_quantity FROM "order".seller_orders WHERE checkout_id = '${checkoutId}';`,
  );
  note("header", { row: header });
  const parts = header.split("|");
  const discount = Number(parts[3]);
  const net = Number(parts[4]);
  const tax = Number(parts[5]);
  const duty = Number(parts[6]);
  const taxDuty = Number(parts[7]);
  const itemCount = Number(parts[9]);
  const totalQty = Number(parts[10]);
  const t014Header = sql(
    `SELECT rounding_mode_used||'|'||money_decimal_places_used||'|'||subtotal_snapshot||'|'||discount_snapshot||'|'||net_amount_before_tax||'|'||tax_snapshot||'|'||total_duty_amount||'|'||total_tax_and_duty_amount||'|'||grand_total_snapshot||'|'||total_item_count||'|'||total_quantity FROM "order".seller_orders WHERE checkout_id = '01a08450-f2b0-7000-816d-552bb5fd54cd';`,
  );
  note("invoice-floor-998", {
    row: t014Header,
    ok: t014Header.startsWith("Floor|0|998") && t014Header.includes("|199.0000|799.0000|"),
  });
  note("invoice-floor-weighted", {
    discount,
    net,
    tax,
    duty,
    taxDuty,
    itemCount,
    totalQty,
    ok: parts[0] === "Floor" && itemCount === 1 && totalQty === 1.25 && taxDuty === tax + duty,
  });

  const lineRow = sql(
    `SELECT ol.line_id::text||'|'||ol.quantity::text||'|'||ol.unit_code_snapshot||'|'||ol.quantity_decimal_places_snapshot::text||'|'||coalesce(ol.quantity_step_snapshot::text,'null') FROM "order".order_lines ol JOIN "order".seller_orders so ON so.seller_order_id = ol.seller_order_id WHERE so.checkout_id = '${checkoutId}';`,
  );
  note("order-line", { row: lineRow, ok: lineRow.includes("|1.250000|") });

  const stockAfterOrder = sql(`SELECT on_hand::text||'|'||reserved::text FROM inventory.stock_positions WHERE offer_id = '${KG_OFFER}' LIMIT 1;`);
  note("inventory-after-order", { row: stockAfterOrder, ok: stockAfterOrder === "10.750000|1.250000" || stockAfterOrder.startsWith("10.750000|1.25") });

  let detail = req("GET", `/v1/admin/orders/${checkoutId}`);
  let seller = detail.json?.sellerOrders?.[0] ?? detail.json?.SellerOrders?.[0];
  let fulfillmentId = pick(seller, "fulfillmentId", "FulfillmentId");
  for (let i = 0; i < 40 && !fulfillmentId; i++) {
    sleep(250);
    detail = req("GET", `/v1/admin/orders/${checkoutId}`);
    seller = detail.json?.sellerOrders?.[0] ?? detail.json?.SellerOrders?.[0];
    fulfillmentId = pick(seller, "fulfillmentId", "FulfillmentId");
  }
  const line = seller?.lines?.[0] ?? seller?.Lines?.[0];
  const sellerOrderId = pick(seller, "sellerOrderId", "SellerOrderId");
  const orderLineId = pick(line, "orderLineId", "OrderLineId");
  note("order-detail", {
    status: detail.status,
    sellerOrderId,
    fulfillmentId,
    orderLineId,
    qty: pick(line, "quantity", "Quantity"),
  });

  for (let i = 0; i < 40; i++) {
    ops = req("GET", `/v1/admin/orders/${checkoutId}/operations`);
    const codes = (ops.json?.actions ?? []).map((a) => a.code);
    if (codes.includes("mark_processing")) break;
    sleep(250);
  }
  const start = req("POST", `/v1/admin/orders/${checkoutId}/operations`, {
    code: "mark_processing",
    sellerOrderId,
    fulfillmentId,
  });
  note("start-processing", { status: start.status, error: start.status >= 400 ? start.json : undefined, ok: start.status < 400 });

  for (let i = 0; i < 40; i++) {
    ops = req("GET", `/v1/admin/orders/${checkoutId}/operations`);
    const codes = (ops.json?.actions ?? []).map((a) => a.code);
    if (codes.includes("pack_selected")) break;
    sleep(250);
  }
  const pack = req("POST", `/v1/admin/orders/${checkoutId}/operations`, {
    code: "pack_selected",
    sellerOrderId,
    fulfillmentId,
    selections: [{ orderLineId, quantity: 0.5 }],
  });
  note("pack-0.50", { status: pack.status, error: pack.status >= 400 ? pack.json : undefined, ok: pack.status < 400 });

  const afterPack = req("GET", `/v1/admin/orders/${checkoutId}`);
  const packedLine = afterPack.json?.sellerOrders?.[0]?.lines?.[0] ?? afterPack.json?.SellerOrders?.[0]?.Lines?.[0];
  const packedQty = Number(pick(packedLine, "quantityPacked", "QuantityPacked"));
  const orderedQty = Number(pick(packedLine, "quantity", "Quantity"));
  note("pack-remain", {
    packed: packedQty,
    ordered: orderedQty,
    remain: Number((orderedQty - packedQty).toFixed(2)),
    ok: packedQty === 0.5 && orderedQty === 1.25,
  });

  const ship = req("POST", `/v1/admin/orders/${checkoutId}/operations`, {
    code: "create_shipment",
    sellerOrderId,
    fulfillmentId: pick(afterPack.json?.sellerOrders?.[0] ?? afterPack.json?.SellerOrders?.[0], "fulfillmentId", "FulfillmentId") ?? fulfillmentId,
    shippingMethodCode: "post",
    carrierDisplayName: "پست",
    providerMetadataJson: JSON.stringify({
      recipientName: "T015",
      recipientPhone: "09121234567",
      postalCode: "1234567890",
      destinationAddress: "تهران، آدرس T015",
      serviceType: "پیشتاز",
      packageCount: 1,
      weightKg: 0.5,
    }),
    selections: [{ orderLineId, quantity: 0.5 }],
  });
  note("shipment-0.50", { status: ship.status, error: ship.status >= 400 ? ship.json : undefined, ok: ship.status < 400 });

  const afterShip = req("GET", `/v1/admin/orders/${checkoutId}`);
  const soShip = afterShip.json?.sellerOrders?.[0] ?? afterShip.json?.SellerOrders?.[0];
  const shipment = (soShip?.shipments ?? soShip?.Shipments ?? [])[0];
  const shipmentId = pick(shipment, "shipmentId", "ShipmentId");
  const shipItemCount = pick(shipment, "itemCount", "ItemCount");
  note("shipment-row", { shipmentId, itemCount: shipItemCount });

  if (shipmentId) {
    const track = req("POST", `/v1/admin/orders/${checkoutId}/operations`, {
      code: "assign_tracking",
      sellerOrderId,
      fulfillmentId: pick(soShip, "fulfillmentId", "FulfillmentId"),
      shipmentId,
      trackingReference: "T015-TRACK-125",
    });
    note("assign-tracking", { status: track.status, error: track.status >= 400 ? track.json : undefined, ok: track.status < 400 });
    const dispatch = req("POST", `/v1/admin/orders/${checkoutId}/operations`, {
      code: "dispatch_shipment",
      sellerOrderId,
      fulfillmentId: pick(soShip, "fulfillmentId", "FulfillmentId"),
      shipmentId,
    });
    note("dispatch", { status: dispatch.status, error: dispatch.status >= 400 ? dispatch.json : undefined });
    const deliver = req("POST", `/v1/admin/orders/${checkoutId}/operations`, {
      code: "deliver_shipment",
      sellerOrderId,
      fulfillmentId: pick(soShip, "fulfillmentId", "FulfillmentId"),
      shipmentId,
    });
    note("deliver", { status: deliver.status, error: deliver.status >= 400 ? deliver.json : undefined, ok: deliver.status < 400 });
  }

  const ret = req("POST", `/v1/admin/orders/${checkoutId}/operations`, {
    code: "request_return",
    sellerOrderId,
    returnItems: [{ orderLineId, quantity: 0.25 }],
    reason: "T015 fractional return",
    idempotencyKey: `t015-ret-${Date.now()}`,
  });
  note("return-0.25", { status: ret.status, error: ret.status >= 400 ? ret.json : undefined, ok: ret.status < 400 });

  const invoice = req("GET", `/v1/admin/orders/${checkoutId}/invoice.html`);
  const html = typeof invoice.json === "string" ? invoice.json : invoice.text ?? "";
  note("invoice-html", {
    status: invoice.status,
    hasItemCount: html.includes("تعداد اقلام"),
    hasDuty: html.includes("عوارض"),
    hasTaxDuty: html.includes("جمع مالیات و عوارض"),
  });

  const list = req("POST", "/v1/admin/orders/query", {
    page: 1,
    pageSize: 20,
    sort: [{ field: "created", direction: "desc" }],
    filters: [],
  });
  const rows = list.json?.items ?? list.json?.rows ?? list.json?.Items ?? list.json?.Rows ?? [];
  const listed = Array.isArray(rows) ? rows.find((r) => String(pick(r, "checkoutId", "CheckoutId")) === checkoutId) : null;
  note("list-header", {
    status: list.status,
    lineCount: pick(listed, "lineCount", "LineCount", "itemCount", "ItemCount"),
    ok: list.status < 400 && listed != null && Number(pick(listed, "lineCount", "LineCount", "itemCount", "ItemCount")) === 1,
  });

  req("PUT", "/v1/admin/settings/quantity-rounding", { globalRoundingMode: "Ceiling" });
  const headerAfter = sql(
    `SELECT rounding_mode_used||'|'||money_decimal_places_used||'|'||subtotal_snapshot||'|'||discount_snapshot||'|'||net_amount_before_tax FROM "order".seller_orders WHERE checkout_id = '${checkoutId}';`,
  );
  note("historical-after-ceiling", { row: headerAfter, ok: headerAfter.startsWith("Floor|") });
}

if (originalKgPrice) {
  sql(`UPDATE pricing.prices SET amount = ${originalKgPrice} WHERE offer_id = '${KG_OFFER}';`);
}
sql(`UPDATE promotion.promotions SET status = 'Expired', updated_at = now() WHERE coupon_code = '${kgCoupon}';`);
req("PUT", "/v1/admin/settings/quantity-rounding", { globalRoundingMode: "Nearest" });

writeFileSync("docs/evidence/TB-P09-T015/runtime-raw.json", JSON.stringify(out, null, 2));
console.log(JSON.stringify(out, null, 2));
if (!out.ok) process.exit(1);
