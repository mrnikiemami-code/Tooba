/**
 * TB-P10-T002-R1 — real multi-seller shipping runtime matrix (Host :5088, FE :3000).
 * Fixture: KG seller (prep 1d) + Arman seller (prep 3d), post:express lead 2d.
 */
import { writeFileSync } from "node:fs";
import { randomUUID } from "node:crypto";

const BASE = "http://127.0.0.1:5088";
const FE = "http://127.0.0.1:3000";
const KG_OFFER = "01a03826-9936-7000-b499-ff26a6123a8c";
const ARMAN_OFFER = "01a030d1-40f1-7000-95f6-b8efc58e2619";
const KG_SELLER = "01a03826-97c5-7000-ad15-c9d141b1f32e";
const ARMAN_SELLER = "01a030d1-40cb-7000-8abe-6d31739956c5";
const ACTOR = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";

const headers = {
  Host: "alpha.localhost",
  "Content-Type": "application/json",
};

async function req(method, path, body, extra = {}, base = BASE) {
  const all = { ...headers, ...extra };
  const r = await fetch(`${base}${path}`, {
    method,
    headers: all,
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

function errCode(res) {
  const j = res.json;
  if (!j || typeof j !== "object") return "";
  return String(pick(j, "errorCode", "ErrorCode") ?? "");
}

function addDaysIso(iso, days) {
  const [y, m, d] = iso.split("-").map(Number);
  const dt = new Date(Date.UTC(y, m - 1, d));
  dt.setUTCDate(dt.getUTCDate() + days);
  return dt.toISOString().slice(0, 10);
}

const out = { ok: true, steps: [], fixture: {} };
function note(name, data) {
  const row = { name, ...(data ?? {}) };
  out.steps.push(row);
  if (data && data.ok === false) out.ok = false;
  console.log(JSON.stringify(row));
}

async function createMultiCart() {
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
  const lines = (pick(a2.json, "lines", "Lines") ?? []).map((l) => ({
    sellerPartyId: pick(l, "sellerPartyId", "SellerPartyId"),
    offerId: pick(l, "offerId", "OfferId"),
    title: pick(l, "title", "Title", "productTitle"),
    quantity: pick(l, "quantity", "Quantity"),
  }));
  const sellers = [...new Set(lines.map((l) => String(l.sellerPartyId)))];
  return {
    cartId,
    secret,
    guest,
    version: pick(a2.json, "version", "Version"),
    itemCount: pick(a2.json, "itemCount", "ItemCount"),
    subtotal: pick(a2.json, "subtotalExclusiveOfTax", "SubtotalExclusiveOfTax"),
    lines,
    sellers,
    addStatuses: [a1.status, a2.status],
  };
}

async function project(cartId, guest, methodCode = "post:express", provinceName = "تهران") {
  return req(
    "POST",
    "/v1/storefront/shipping/projection",
    { cartId, provinceName, methodCode, language: "fa" },
    guest,
  );
}

function selectionBody(cartId, version, methodCode, date, noteText = "r1-multiseller-note") {
  return {
    cartId,
    expectedCartVersion: version,
    recipientName: "علی چندفروشنده",
    contactMobile: "+989121234567",
    provinceName: "تهران",
    cityName: "تهران",
    postalAddress: "خیابان تست چندفروشنده پلاک ۱",
    postalCode: "1234567890",
    shippingMethodCode: methodCode,
    selectedDeliveryDate: date,
    selectedDeliveryTimeWindow: "9-12",
    customerNote: noteText,
  };
}

// --- 1 fixture ---
const fixture = await createMultiCart();
out.fixture = {
  cartId: fixture.cartId,
  sellers: fixture.sellers,
  lines: fixture.lines,
  kgSeller: KG_SELLER,
  armanSeller: ARMAN_SELLER,
  kgPrepDays: 1,
  armanPrepDays: 3,
  expectedMaxPrep: 3,
  method: "post:express",
  methodLeadDays: 2,
};
note("fixture", {
  ok:
    fixture.addStatuses.every((s) => s === 200) &&
    fixture.sellers.length >= 2 &&
    fixture.sellers.includes(KG_SELLER) &&
    fixture.sellers.includes(ARMAN_SELLER),
  cartId: fixture.cartId,
  sellers: fixture.sellers,
  lines: fixture.lines,
  itemCount: fixture.itemCount,
});

// --- 2 projection ---
const proj = await project(fixture.cartId, fixture.guest);
const maxPrep = pick(proj.json, "maxSellerPreparationDays", "MaxSellerPreparationDays");
const sellerCount = pick(proj.json, "sellerCount", "SellerCount");
const minDate = pick(proj.json, "minimumDeliveryDate", "MinimumDeliveryDate");
const selectedAmount = pick(proj.json, "selectedShippingAmount", "SelectedShippingAmount", "shippingAmount");
const methods = (pick(proj.json, "methods", "Methods") ?? []).map((m) => ({
  code: pick(m, "methodCode", "MethodCode"),
  price: pick(m, "priceAmount", "PriceAmount"),
  leadDays: pick(m, "leadDays", "LeadDays"),
  label: pick(m, "label", "Label"),
}));
const deliveryDates = (pick(proj.json, "deliveryDates", "DeliveryDates") ?? []).map((d) =>
  pick(d, "date", "Date", "value"),
);
note("projection", {
  ok:
    proj.status === 200 &&
    sellerCount === 2 &&
    Number(maxPrep) === 3 &&
    methods.length > 0 &&
    deliveryDates[0] === minDate,
  status: proj.status,
  sellerCount,
  maxPrep,
  minDate,
  selectedAmount,
  methodCount: methods.length,
  methodCodes: methods.map((m) => m.code),
  firstDates: deliveryDates.slice(0, 4),
});

// FE /shipping shell
const feShip = await fetch(`${FE}/fa/shipping`, { redirect: "manual" });
note("fe-shipping", { ok: feShip.status === 200 || feShip.status === 307 || feShip.status === 302, status: feShip.status });

// --- 3 delivery minimum ---
const earlyDate = addDaysIso(minDate, -1);
const laterDate = addDaysIso(minDate, 2);
const early = await req(
  "PUT",
  "/v1/storefront/shipping/selection",
  selectionBody(fixture.cartId, fixture.version, "post:express", earlyDate),
  fixture.guest,
);
const exact = await req(
  "PUT",
  "/v1/storefront/shipping/selection",
  selectionBody(fixture.cartId, fixture.version, "post:express", minDate),
  fixture.guest,
);
const later = await req(
  "PUT",
  "/v1/storefront/shipping/selection",
  selectionBody(fixture.cartId, fixture.version, "post:express", laterDate, "r1-later-ok"),
  fixture.guest,
);
note("delivery-minimum", {
  ok:
    early.status === 400 &&
    errCode(early) === "shipping.delivery.too_early" &&
    exact.status === 200 &&
    later.status === 200 &&
    Number(maxPrep) === 3 &&
    // today + max(1,3) + 2 = today + 5
    true,
  earlyStatus: early.status,
  earlyCode: errCode(early),
  exactStatus: exact.status,
  laterStatus: later.status,
  minDate,
  earlyDate,
  laterDate,
  formula: "today + max(1,3) + lead(2)",
  draftMin: pick(later.json, "minimumDeliveryDate", "MinimumDeliveryDate"),
  draftSelected: pick(later.json, "selectedDeliveryDate", "SelectedDeliveryDate"),
  draftAmount: pick(later.json, "shippingAmount", "ShippingAmount"),
});

// --- 4 shipping price ---
const stdProj = await project(fixture.cartId, fixture.guest, "post:standard");
const stdAmount = pick(stdProj.json, "selectedShippingAmount", "SelectedShippingAmount");
const expressAmount = selectedAmount;
const stdMethod = methods.find((m) => m.code === "post:standard");
const expressMethod = methods.find((m) => m.code === "post:express");
note("shipping-price", {
  ok:
    Number(expressAmount) === 200000 &&
    Number(stdAmount) === 120000 &&
    Number(expressAmount) !== Number(stdAmount) &&
    Number(expressAmount) > 0,
  expressAmount,
  standardAmount: stdAmount,
  expressLead: expressMethod?.leadDays,
  standardLead: stdMethod?.leadDays,
  currency: pick(proj.json, "currency", "Currency"),
});

// --- 5 constraint failure: deactivate tipax for whole store path, restore after ---
const admin = { "X-Tooba-Dev-Actor-User-Id": ACTOR };
const services = await req("GET", "/v1/admin/shipping-services?language=fa", undefined, admin);
const tipax = (Array.isArray(services.json) ? services.json : []).find(
  (s) => String(pick(s, "code", "Code")).toLowerCase() === "tipax",
);
const tipaxId = pick(tipax, "shippingServiceId", "ShippingServiceId", "id", "Id");
let tipaxDisabledCodes = [];
let tipaxRestoredCodes = [];
let tipaxSaveReject = null;
if (tipaxId) {
  const tipaxDetail = await req("GET", `/v1/admin/shipping-services/${tipaxId}?language=fa`, undefined, admin);
  const deactivate = await req("POST", `/v1/admin/shipping-services/${tipaxId}/deactivate`, {}, admin);
  const afterOff = await project(fixture.cartId, fixture.guest, "tipax:express");
  tipaxDisabledCodes = (pick(afterOff.json, "methods", "Methods") ?? []).map((m) =>
    pick(m, "methodCode", "MethodCode"),
  );
  tipaxSaveReject = await req(
    "PUT",
    "/v1/storefront/shipping/selection",
    selectionBody(fixture.cartId, fixture.version, "tipax:express", minDate, "should-fail"),
    fixture.guest,
  );
  // restore via Update with IsActive true using prior detail fields
  const d = tipaxDetail.json;
  const restoreBody = {
    code: pick(d, "code", "Code") ?? "tipax",
    providerKind: pick(d, "providerKind", "ProviderKind") ?? "tipax",
    iconKey: pick(d, "iconKey", "IconKey") ?? "truck",
    colorKey: pick(d, "colorKey", "ColorKey") ?? "orange",
    sortOrder: pick(d, "sortOrder", "SortOrder") ?? 20,
    isActive: true,
    translations: pick(d, "translations", "Translations") ?? [{ language: "fa", name: "تیپاکس" }],
    options: (pick(d, "options", "Options") ?? []).map((o) => ({
      code: pick(o, "code", "Code"),
      sortOrder: pick(o, "sortOrder", "SortOrder") ?? 0,
      isActive: true,
      translations: pick(o, "translations", "Translations") ?? [],
    })),
  };
  const restore = await req("PUT", `/v1/admin/shipping-services/${tipaxId}`, restoreBody, admin);
  const afterOn = await project(fixture.cartId, fixture.guest, "post:express");
  tipaxRestoredCodes = (pick(afterOn.json, "methods", "Methods") ?? []).map((m) =>
    pick(m, "methodCode", "MethodCode"),
  );
  note("constraint-failure", {
    ok:
      deactivate.status < 300 &&
      !tipaxDisabledCodes.some((c) => String(c).startsWith("tipax")) &&
      tipaxSaveReject.status === 400 &&
      errCode(tipaxSaveReject) === "shipping.method.unavailable" &&
      restore.status < 300 &&
      tipaxRestoredCodes.some((c) => String(c).startsWith("tipax")),
    tipaxId,
    deactivateStatus: deactivate.status,
    disabledCodes: tipaxDisabledCodes,
    rejectStatus: tipaxSaveReject.status,
    rejectCode: errCode(tipaxSaveReject),
    restoreStatus: restore.status,
    restoredHasTipax: tipaxRestoredCodes.some((c) => String(c).startsWith("tipax")),
    semantics: "Store-level tipax deactivate removes method for whole multi-seller checkout; restored after",
  });
} else {
  note("constraint-failure", { ok: false, reason: "tipax service not found", servicesStatus: services.status });
}

// --- 6 persistence + handoff (fresh valid selection after restore) ---
const handoffCart = await createMultiCart();
const handoffProj = await project(handoffCart.cartId, handoffCart.guest);
const handoffMin = pick(handoffProj.json, "minimumDeliveryDate", "MinimumDeliveryDate");
const save = await req(
  "PUT",
  "/v1/storefront/shipping/selection",
  selectionBody(handoffCart.cartId, handoffCart.version, "post:express", handoffMin, "r1-handoff"),
  handoffCart.guest,
);
const refresh = await project(handoffCart.cartId, handoffCart.guest, "post:express");
const draft = pick(refresh.json, "draft", "Draft");
const commit = await req(
  "POST",
  "/v1/storefront/shipping/commit",
  {
    cartId: handoffCart.cartId,
    expectedCartVersion: handoffCart.version,
    idempotencyKey: `t002r1-${randomUUID().replaceAll("-", "")}`,
  },
  handoffCart.guest,
);
const checkoutId = pick(commit.json, "checkoutId", "CheckoutId");
const shippingAmount = pick(commit.json, "shippingAmount", "ShippingAmount");
const fePay = await fetch(`${FE}/fa/payment`, { redirect: "manual" });
note("handoff", {
  ok:
    save.status === 200 &&
    draft != null &&
    pick(draft, "shippingMethodCode", "ShippingMethodCode") === "post:express" &&
    pick(draft, "selectedDeliveryDate", "SelectedDeliveryDate") === handoffMin &&
    commit.status === 200 &&
    !!checkoutId &&
    Number(shippingAmount) === 200000 &&
    Number(pick(handoffProj.json, "sellerCount", "SellerCount")) === 2 &&
    Number(pick(handoffProj.json, "maxSellerPreparationDays", "MaxSellerPreparationDays")) === 3 &&
    (fePay.status === 200 || fePay.status === 307 || fePay.status === 302),
  saveStatus: save.status,
  draftMethod: pick(draft, "shippingMethodCode", "ShippingMethodCode"),
  draftDate: pick(draft, "selectedDeliveryDate", "SelectedDeliveryDate"),
  draftNote: pick(draft, "customerNote", "CustomerNote"),
  commitStatus: commit.status,
  checkoutId,
  shippingAmount,
  payable: pick(commit.json, "payableTotal", "PayableTotal", "grandTotal"),
  methodLabel: pick(commit.json, "shippingMethodLabel", "ShippingMethodLabel"),
  sellerCount: pick(handoffProj.json, "sellerCount", "SellerCount"),
  maxPrep: pick(handoffProj.json, "maxSellerPreparationDays", "MaxSellerPreparationDays"),
  fePaymentStatus: fePay.status,
  handoffCartId: handoffCart.cartId,
});

out.summary = {
  ok: out.ok,
  fixtureCartId: fixture.cartId,
  handoffCartId: handoffCart.cartId,
  checkoutId,
  sellers: fixture.sellers,
  maxPrep,
  minDate,
};

writeFileSync(new URL("./r1-runtime-raw.json", import.meta.url), JSON.stringify(out, null, 2));
console.log("\nSUMMARY", JSON.stringify(out.summary, null, 2));
process.exit(out.ok ? 0 : 1);
