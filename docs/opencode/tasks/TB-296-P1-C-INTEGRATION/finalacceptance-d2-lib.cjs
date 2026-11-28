// TB-296 P1-FINAL-CLOSURE — shared real-workflow helpers for D2 closure.
// Real authenticated checkout on the existing DEV Host (5088) and tooba_alpha DB. No bypass,
// no product change, no new Host/DB. Synthetic customer + exact fixture cleanup.
const fs = require("node:fs");
const path = require("node:path");
const crypto = require("node:crypto");
const { execFileSync } = require("node:child_process");

const HOST = "http://127.0.0.1:5088";
const ADMIN_HEADER = "X-Tooba-Dev-Actor-User-Id";

function check(condition, message) { if (!condition) throw new Error(message); }
function uuid(v) { check(/^[0-9a-f]{8}-(?:[0-9a-f]{4}-){3}[0-9a-f]{12}$/i.test(v), "not a uuid: " + v); return v.toLowerCase(); }
function psql(query) {
  return execFileSync(process.env.P1C_PSQL || "psql",
    ["-X", "-qAt", "-v", "ON_ERROR_STOP=1", "-d", "tooba_alpha", "-c", query],
    { encoding: "utf8", timeout: 60000, windowsHide: true }).trim();
}
function psqlJson(q) { return JSON.parse(psql(q)); }
const quote = x => "'" + String(x).replaceAll("'", "''") + "'";

async function call(route, method = "GET", payload, headers = {}) {
  const res = await fetch(HOST + route, {
    method,
    headers: { "Accept-Language": "fa-IR", ...(payload !== undefined ? { "Content-Type": "application/json" } : {}), ...headers },
    body: payload !== undefined ? JSON.stringify(payload) : undefined,
    signal: AbortSignal.timeout(30000),
  });
  const raw = await res.text();
  let body; try { body = raw ? JSON.parse(raw) : null; } catch { body = raw; }
  return { status: res.status, body };
}

async function registerCustomer(prefix) {
  const email = (prefix || "p1c-d2") + "-" + crypto.randomUUID().slice(0, 12) + "@tooba.local";
  const password = "P1c-dev-pass-12345";
  const reg = await call("/v1/auth/register", "POST", { IdentifierKind: "Email", Identifier: email, Password: password });
  check(reg.status === 201, "register failed " + reg.status + " " + JSON.stringify(reg.body));
  const userId = uuid(reg.body.userId);
  const login = await call("/v1/auth/login", "POST", { IdentifierKind: "Email", Identifier: email, Password: password });
  check(login.status === 200, "login failed " + login.status + " " + JSON.stringify(login.body));
  check(uuid(login.body.userId) === userId, "login user mismatch");
  const sessionId = uuid(login.body.sessionId);
  return { email, password, userId, sessionId, auth: { Authorization: "Bearer " + sessionId } };
}

async function adminContext() {
  const dev = await call("/v1/admin/dev-context", "GET", undefined, {});
  check(dev.status === 200, "admin dev-context " + dev.status);
  const actor = uuid(dev.body.actorUserId);
  return { actor, headers: { [ADMIN_HEADER]: actor } };
}

function buyableOffer(excludeLines = true) {
  const where = excludeLines ? 'and not exists(select 1 from "order".order_lines l where l.offer_id=o.offer_id)' : "";
  return psql("select o.offer_id from offer.offers o join pricing.prices p on p.offer_id=o.offer_id and p.status='Active' " +
    "join inventory.stock_positions s on s.offer_id=o.offer_id where o.status='Active' and o.channel='Marketplace' and s.on_hand>5 " + where +
    " order by o.offer_id limit 1").trim();
}

async function createCartWithLine(customer, offerId, quantity = 1) {
  const auth = customer.auth;
  const cart = await call("/v1/storefront/cart", "POST", null, auth);
  check(cart.status === 200, "cart create " + cart.status + " " + JSON.stringify(cart.body));
  const cartId = uuid(cart.body.cartId);
  const secret = cart.body.guestSecret;
  const add = await call("/v1/storefront/cart/" + cartId + "/lines", "POST", { offerId, quantity },
    { ...auth, "X-Tooba-Guest-Secret": secret, "X-Tooba-Cart-Version": String(cart.body.version) });
  check(add.status === 200, "cart add line " + add.status + " " + JSON.stringify(add.body));
  return { cartId, secret, cartVersion: add.body.version, offerId, quantity };
}

async function createCheckout(customer, offerId, quantity = 1) {
  const base = await createCartWithLine(customer, offerId, quantity);
  const shipping = {
    recipientName: "P1C Synthetic Customer", contactMobile: "09120000000",
    provinceName: "تهران", cityName: "تهران", postalAddress: "P1C synthetic checkout address", postalCode: "1234567890",
  };
  const submit = await call("/v1/storefront/checkout", "POST", {
    cartId: base.cartId, expectedCartVersion: base.cartVersion, idempotencyKey: "P1C-sub-" + crypto.randomUUID(), shipping, couponCode: null,
  }, { ...customer.auth, "X-Tooba-Guest-Secret": base.secret });
  return { ...base, submit };
}

function checkoutOf(result) {
  check(result.submit.status === 200, "checkout submit " + result.submit.status + " " + JSON.stringify(result.submit.body).slice(0, 400));
  return { ...result, checkoutId: uuid(result.submit.body.checkoutId), sellerOrders: result.submit.body.sellerOrders };
}

async function payCheckout(customer, co) {
  const guest = { ...customer.auth, "X-Tooba-Guest-Secret": co.secret };
  const init = await call("/v1/storefront/checkout/" + co.checkoutId + "/payments", "POST",
    { cartId: co.cartId, idempotencyKey: "P1C-pay-" + crypto.randomUUID(), useWallet: false, providerCode: "gateway" }, guest);
  check(init.status === 200, "payment init " + init.status + " " + JSON.stringify(init.body).slice(0, 400));
  const ref = init.body.providerRequestReference;
  const done = await call("/v1/storefront/payments/" + init.body.paymentId + "/sandbox/complete", "POST",
    { cartId: co.cartId, attemptId: init.body.attemptId, providerRequestReference: ref, outcome: "success" }, guest);
  check(done.status === 200, "sandbox complete " + done.status + " " + JSON.stringify(done.body).slice(0, 400));
  check(done.body.status === "Succeeded", "payment not succeeded: " + JSON.stringify(done.body).slice(0, 200));
  return { init: init.body, done: done.body, guest };
}

async function retryReservation(customer, co) {
  return call("/v1/storefront/checkout/" + co.checkoutId + "/retry-reservation", "POST",
    { cartId: co.cartId }, { ...customer.auth, "X-Tooba-Guest-Secret": co.secret });
}

async function adminAssign(admin, offerId, responsibility, expectedVersion) {
  return call("/v1/admin/offers/" + offerId + "/fulfillment", "PUT",
    { responsibility, shippingTypeId: null, expectedVersion }, admin.headers);
}
async function adminOfferDetail(admin, offerId) { return call("/v1/admin/offers/" + offerId, "GET", undefined, admin.headers); }
async function adminCancel(admin, checkoutId, sellerOrderId) {
  return call("/v1/admin/orders/" + checkoutId + "/operations", "POST",
    { code: "cancel", sellerOrderId, reason: "P1C D2 cancel", idempotencyKey: "P1C-cancel-" + crypto.randomUUID() }, admin.headers);
}
async function adminOrderDetail(admin, checkoutId) { return call("/v1/admin/orders/" + checkoutId, "GET", undefined, admin.headers); }

function orderState(checkoutId) {
  return psqlJson("SELECT jsonb_build_object(" +
    "'sellerOrders',(SELECT coalesce(jsonb_agg(jsonb_build_object('id',seller_order_id,'status',status,'cancelledFrom',cancelled_from_status)),'[]') FROM \"order\".seller_orders WHERE checkout_id='" + checkoutId + "')," +
    "'lines',(SELECT coalesce(jsonb_agg(jsonb_build_object('id',line_id,'offer',offer_id,'reservation',reservation_id,'qty',quantity)),'[]') FROM \"order\".order_lines WHERE seller_order_id IN (SELECT seller_order_id FROM \"order\".seller_orders WHERE checkout_id='" + checkoutId + "'))," +
    "'payments',(SELECT coalesce(jsonb_agg(jsonb_build_object('id',payment_id,'status',status,'provider',provider_code)),'[]') FROM payment.payments WHERE checkout_id='" + checkoutId + "')," +
    "'cycles',(SELECT coalesce(jsonb_agg(jsonb_build_object('n',cycle_number,'status',status,'expires',expires_at,'reservations',reservation_ids)),'[]') FROM \"order\".reservation_cycles WHERE checkout_id='" + checkoutId + "'))");
}

function reservationsOf(cartId) {
  return psqlJson("SELECT coalesce(jsonb_agg(jsonb_build_object('id',r.reservation_id,'status',r.status,'purpose',r.purpose,'qty',r.quantity,'stock',r.stock_item_id,'expires',r.expires_at) ORDER BY r.created_at),'[]') FROM inventory.reservations r WHERE r.external_reference LIKE " + quote("%" + cartId.replaceAll("-", "") + "%"));
}


function reservationRow(id) {
  const raw = psql("SELECT coalesce(jsonb_build_object('id',reservation_id,'status',status,'qty',quantity,'stock',stock_item_id,'expires',expires_at)::text,'') FROM inventory.reservations WHERE reservation_id='" + id + "'");
  return raw ? JSON.parse(raw) : null;
}
function stockState(offerId) {
  return psqlJson("SELECT jsonb_build_object('onHand',coalesce(sum(on_hand),0),'reserved',coalesce(sum(reserved),0),'positions',coalesce(jsonb_agg(jsonb_build_object('stock',stock_item_id,'onHand',on_hand,'reserved',reserved)),'[]')) FROM inventory.stock_positions WHERE offer_id='" + offerId + "'");
}

function offerState(offerId) {
  return psqlJson("SELECT jsonb_build_object('responsibility',fulfillment_responsibility,'updatedAt',updated_at,'history',(SELECT count(*) FROM offer.offer_history WHERE offer_id='" + offerId + "'),'lines',(SELECT count(*) FROM \"order\".order_lines WHERE offer_id='" + offerId + "')) FROM offer.offers WHERE offer_id='" + offerId + "'");
}


function pickCleanOffers(n) {
  const rows = psql("select distinct o.offer_id from offer.offers o join pricing.prices p on p.offer_id=o.offer_id and p.status='Active' " +
    "join inventory.stock_positions s on s.offer_id=o.offer_id join inventory.locations l on l.location_id=s.location_id " +
    "where o.status='Active' and o.channel='Marketplace' and s.on_hand>5 and s.reserved=0 and l.owner_kind='Store' and o.fulfillment_responsibility is null " +
    "and not exists(select 1 from \"order\".order_lines x where x.offer_id=o.offer_id) order by o.offer_id limit " + n).split(/\r?\n/).filter(Boolean);
  check(rows.length === n, "expected " + n + " clean offers, got " + rows.length);
  return rows;
}

async function createDraftOffer(admin, sellerId) {
  const variant = psql(`select v.variant_id from catalog.variants v join catalog.products p on p.product_id=v.product_id where v.status='Published' and p.quantity_decimal_places=0 and not exists (select 1 from offer.offers o where o.seller_party_id='${sellerId}' and o.catalog_variant_id=v.variant_id) order by v.variant_id limit 1`).trim();
  check(/^[0-9a-f-]{36}$/i.test(variant), "no free variant for draft offer");
  const requestId = "P1C-" + crypto.randomUUID();
  const res = await call("/v1/admin/offers", "POST", {
    sellerPartyId: sellerId, catalogVariantId: variant, channel: "Marketplace",
    sellerSku: "P1C-D2P-" + crypto.randomUUID(), status: "Draft", requestId,
  }, admin.headers);
  check(res.status === 201, "draft offer create failed " + res.status + " " + JSON.stringify(res.body).slice(0, 300));
  return { offerId: uuid(res.body.offerId), sellerId, variantId: variant, requestId };
}

function cleanupDraftOffers(offerIds) {
  if (!offerIds.length) return;
  const list = offerIds.map(quote).join(",");
  const S = [];
  S.push(`DELETE FROM inventory.reservations WHERE stock_item_id IN (SELECT stock_item_id FROM inventory.stock_positions WHERE offer_id IN (${list}))`);
  S.push(`DELETE FROM inventory.offer_inventory_changes WHERE offer_id IN (${list})`);
  S.push(`DELETE FROM inventory.offer_allocations WHERE offer_id IN (${list})`);
  S.push(`DELETE FROM inventory.stock_positions WHERE offer_id IN (${list})`);
  S.push(`DELETE FROM inventory.locations WHERE code LIKE 'P1C-F-%' AND location_id NOT IN (SELECT location_id FROM inventory.stock_positions)`);
  S.push(`DELETE FROM pricing.offer_price_changes WHERE offer_id IN (${list})`);
  S.push(`DELETE FROM pricing.prices WHERE offer_id IN (${list})`);
  S.push(`DELETE FROM offer.offer_history WHERE offer_id IN (${list})`);
  S.push(`DELETE FROM offer.offers WHERE offer_id IN (${list})`);
  psql("BEGIN;" + S.map(s => s + ";").join("") + "COMMIT;");
  return { offersLeft: Number(psql(`SELECT count(*) FROM offer.offers WHERE offer_id IN (${list})`)) };
}
// Exact cleanup of this task's owned fixtures (customer user / checkouts / carts / payments / reservations).
function cleanupOwned(owned) {
  const checkouts = owned.checkoutIds || [];
  const carts = owned.cartIds || [];
  const userId = owned.userId;
  const checkoutList = checkouts.length ? checkouts.map(quote).join(",") : "NULL";
  const cartList = carts.length ? carts.map(quote).join(",") : "NULL";
  const paymentIds = checkouts.length ? "(SELECT payment_id FROM payment.payments WHERE checkout_id IN (" + checkoutList + "))" : "NULL";
  const sellerOrderIds = checkouts.length ? "(SELECT seller_order_id FROM \"order\".seller_orders WHERE checkout_id IN (" + checkoutList + "))" : "NULL";
  const lineIds = checkouts.length ? "(SELECT line_id FROM \"order\".order_lines WHERE seller_order_id IN " + sellerOrderIds + ")" : "NULL";
  const reservationIds = checkouts.length ? "(SELECT reservation_id FROM \"order\".order_lines WHERE seller_order_id IN " + sellerOrderIds + " AND reservation_id IS NOT NULL)" : "NULL";
  const S = [];
  S.push(`DELETE FROM payment.proof_assets WHERE payment_id IN ${paymentIds}`);
  S.push(`DELETE FROM "order".payment_inbox WHERE payment_id IN ${paymentIds}`);
  S.push(`DELETE FROM "order".order_line_obligation_closures WHERE checkout_id IN (${checkoutList})`);
  S.push(`DELETE FROM "order".order_shipping_group_lines WHERE shipping_group_id IN (SELECT shipping_group_id FROM "order".order_shipping_groups WHERE checkout_id IN (${checkoutList}))`);
  S.push(`DELETE FROM "order".order_operational_group_occurrence_members WHERE occurrence_id IN (SELECT occurrence_id FROM "order".order_operational_group_occurrences WHERE checkout_id IN (${checkoutList}))`);
  S.push(`DELETE FROM "order".order_operational_group_occurrences WHERE checkout_id IN (${checkoutList})`);
  S.push(`DELETE FROM "order".order_shipping_groups WHERE checkout_id IN (${checkoutList})`);
  S.push(`DELETE FROM "order".shipment_dispatch_plans WHERE checkout_id IN (${checkoutList})`);
  S.push(`DELETE FROM "order".reservation_cycle_events WHERE checkout_id IN (${checkoutList})`);
  S.push(`DELETE FROM "order".reservation_cycles WHERE checkout_id IN (${checkoutList})`);
  S.push(`DELETE FROM "order".checkout_processes WHERE checkout_id IN (${checkoutList}) OR cart_id IN (${cartList})`);
  S.push(`DELETE FROM "order".checkout_reservation_commits WHERE checkout_id IN (${checkoutList})`);
  S.push(`DELETE FROM "order".checkout_admin_view_acks WHERE checkout_id IN (${checkoutList})`);
  S.push(`DELETE FROM "order".checkout_operational_notes WHERE checkout_id IN (${checkoutList})`);
  S.push(`DELETE FROM "order".pending_payment_card_hides WHERE checkout_id IN (${checkoutList})`);
  S.push(`DELETE FROM "order".order_lines WHERE seller_order_id IN ${sellerOrderIds}`);
  S.push(`DELETE FROM "order".seller_orders WHERE checkout_id IN (${checkoutList})`);
  S.push(`DELETE FROM "order".checkouts WHERE checkout_id IN (${checkoutList})`);
  S.push(`DELETE FROM payment.allocations WHERE payment_id IN ${paymentIds}`);
  S.push(`DELETE FROM payment.attempts WHERE payment_id IN ${paymentIds}`);
  S.push(`DELETE FROM payment.payments WHERE checkout_id IN (${checkoutList})`);
  S.push(`DELETE FROM "order".cart_shipping_group_lines WHERE shipping_group_id IN (SELECT shipping_group_id FROM "order".cart_shipping_groups WHERE cart_id IN (${cartList}))`);
  S.push(`DELETE FROM "order".cart_shipping_groups WHERE cart_id IN (${cartList})`);
  S.push(`DELETE FROM "order".cart_shipping_drafts WHERE cart_id IN (${cartList})`);
  S.push(`DELETE FROM cart.cart_lines WHERE cart_id IN (${cartList})`);
  S.push(`DELETE FROM cart.carts WHERE cart_id IN (${cartList})`);
  S.push(`DELETE FROM inventory.reservations WHERE reservation_id IN ${reservationIds}`);
  if (carts.length) S.push(`DELETE FROM inventory.reservations WHERE external_reference LIKE ANY (ARRAY[${carts.map(c => quote("%" + c.replaceAll("-", "") + "%")).join(",")}])`);
  if (userId) {
    S.push(`DELETE FROM identity.auth_sessions WHERE user_id='${userId}'`);
    S.push(`DELETE FROM identity.auth_challenges WHERE user_id='${userId}'`);
    S.push(`DELETE FROM identity.users WHERE user_id='${userId}'`);
  }
  psql("BEGIN;" + S.map(s => s + ";").join("") + "COMMIT;");
  return {
    cartsLeft: carts.length ? Number(psql(`SELECT count(*) FROM cart.carts WHERE cart_id IN (${cartList})`)) : 0,
    checkoutsLeft: checkouts.length ? Number(psql(`SELECT count(*) FROM "order".checkouts WHERE checkout_id IN (${checkoutList})`)) : 0,
    userLeft: userId ? Number(psql(`SELECT count(*) FROM identity.users WHERE user_id='${userId}'`)) : 0,
  };
}

module.exports = {
  HOST, ADMIN_HEADER, check, uuid, psql, psqlJson, quote, call,
  registerCustomer, adminContext, buyableOffer, pickCleanOffers, createDraftOffer, cleanupDraftOffers, createCartWithLine, createCheckout, checkoutOf, payCheckout,
  retryReservation, adminAssign, adminOfferDetail, adminCancel, adminOrderDetail,
  orderState, reservationsOf, reservationRow, stockState, offerState, cleanupOwned,
};
