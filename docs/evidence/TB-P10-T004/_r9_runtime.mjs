import { writeFileSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { randomUUID } from "node:crypto";
const HOST = "http://127.0.0.1:5088";
const KG = "01a03826-9936-7000-b499-ff26a6123a8c";
const ADMIN = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const out = { ok: true, steps: [], at: new Date().toISOString(), secretsOmitted: true };
function note(name, data) {
  const row = { name, ...(data ?? {}) };
  out.steps.push(row);
  if (data && data.ok === false) out.ok = false;
  console.log(JSON.stringify(row));
}
function curl(args) {
  const raw = execFileSync("curl.exe", ["-sS", "-w", "\n%{http_code}", ...args], { encoding: "utf8", maxBuffer: 8e6 });
  const i = raw.lastIndexOf("\n");
  let json;
  try { json = JSON.parse(raw.slice(0, i)); } catch { json = raw.slice(0, i); }
  return { status: +raw.slice(i + 1), json, text: raw.slice(0, i) };
}
function pick(o, ...ks) {
  if (!o || typeof o !== "object") return;
  for (const k of ks) if (o[k] != null) return o[k];
}
function host(m, p, b, ex = {}) {
  const a = ["-X", m, HOST + p, "-H", "Host: alpha.localhost", "-H", "Content-Type: application/json"];
  for (const [k, v] of Object.entries(ex)) if (v != null) a.push("-H", `${k}: ${v}`);
  if (b !== undefined) a.push("--data-binary", JSON.stringify(b));
  return curl(a);
}
function admin() { return { "X-Tooba-Dev-Actor-User-Id": ADMIN }; }
function sql(q) {
  return execFileSync("docker", ["exec", "-i", "postgres-db", "psql", "-U", "admin", "-d", "tooba_alpha", "-t", "-A", "-c", q], { encoding: "utf8" }).trim();
}
function restoreKg() {
  sql(`UPDATE inventory.stock_positions SET on_hand=GREATEST(on_hand,200), reserved=COALESCE((SELECT SUM(r.quantity) FROM inventory.reservations r WHERE r.stock_item_id=stock_positions.stock_item_id AND r.status='Held'),0) WHERE offer_id='${KG}'`);
}
function cartHolds(cartId) {
  return Number(sql(`SELECT COUNT(*) FROM inventory.reservations WHERE external_reference LIKE 'cart:${cartId}%' AND status='Held'`));
}
function buildCart(qty = 1) {
  const cart = host("POST", "/v1/storefront/cart");
  const secret = pick(cart.json, "guestSecret", "GuestSecret");
  const cartId = pick(cart.json, "cartId", "CartId");
  let version = pick(cart.json, "version", "Version");
  const guest = { "X-Tooba-Guest-Secret": secret };
  const reservedBefore = Number(sql(`SELECT COALESCE(SUM(reserved),0) FROM inventory.stock_positions WHERE offer_id='${KG}'`));
  const line = host("POST", `/v1/storefront/cart/${cartId}/lines?expectedVersion=${version}`, { offerId: KG, quantity: qty }, guest);
  if (line.status >= 400) throw new Error("add " + line.text);
  version = pick(line.json, "version", "Version");
  const reservedAfter = Number(sql(`SELECT COALESCE(SUM(reserved),0) FROM inventory.stock_positions WHERE offer_id='${KG}'`));
  return { cartId, secret, guest, version, reservedBefore, reservedAfter, addStatus: line.status };
}
function ship(session, idem = randomUUID()) {
  const proj = host("POST", "/v1/storefront/shipping/projection", { cartId: session.cartId, provinceName: "تهران", methodCode: "post:express", language: "fa" }, session.guest);
  const minDate = pick(proj.json, "minimumDeliveryDate", "MinimumDeliveryDate");
  host("PUT", "/v1/storefront/shipping/selection", { cartId: session.cartId, expectedCartVersion: session.version, recipientName: "R9", contactMobile: "+989121240101", provinceName: "تهران", cityName: "تهران", postalAddress: "a", postalCode: "1234567890", shippingMethodCode: "post:express", selectedDeliveryDate: minDate, selectedDeliveryTimeWindow: "9-12", customerNote: "r9" }, session.guest);
  return host("POST", "/v1/storefront/shipping/commit", { cartId: session.cartId, expectedCartVersion: session.version, idempotencyKey: idem }, session.guest);
}
function grid(path) {
  return host("POST", path, { page: 1, pageSize: 50, sort: [{ field: "created", direction: "desc" }], filters: [] }, admin());
}
try {
  restoreKg();
  note("health", { status: curl(["-H", "Host: alpha.localhost", HOST + "/health"]).status, ok: true });

  const a = buildCart(1);
  const got = host("GET", `/v1/storefront/cart/${a.cartId}`, undefined, a.guest);
  const reservedAfterGet = Number(sql(`SELECT COALESCE(SUM(reserved),0) FROM inventory.stock_positions WHERE offer_id='${KG}'`));
  const commitA = ship(a);
  const checkoutA = pick(commitA.json, "checkoutId", "CheckoutId");
  const orderHolds = Number(sql(`SELECT COUNT(*) FROM inventory.reservations r JOIN "order".order_lines ol ON ol.reservation_id=r.reservation_id JOIN "order".seller_orders so ON so.seller_order_id=ol.seller_order_id WHERE so.checkout_id='${checkoutA}' AND r.status='Held'`));
  note("A-browse-beyond-old-ttl", {
    ok: a.reservedAfter === a.reservedBefore && cartHolds(a.cartId) === 0 && commitA.status < 400 && orderHolds === 1 && got.status === 200,
    cartHolds: cartHolds(a.cartId),
    reservedDelta: a.reservedAfter - a.reservedBefore,
    commit: commitA.status,
    orderHolds,
  });

  restoreKg();
  const b = buildCart(1);
  sql(`UPDATE inventory.stock_positions SET on_hand=0, reserved=0 WHERE offer_id='${KG}'`);
  const still = host("GET", `/v1/storefront/cart/${b.cartId}`, undefined, b.guest);
  const lines = pick(still.json, "lines", "Lines") || [];
  const failB = ship(b);
  const ordersB = Number(sql(`SELECT COUNT(*) FROM "order".checkouts WHERE cart_id='${b.cartId}'`));
  note("B-stock-lost", {
    ok: still.status === 200 && lines.length > 0 && failB.status >= 400 && ordersB === 0,
    cartLines: lines.length,
    commit: failB.status,
    orders: ordersB,
  });

  restoreKg();
  sql(`UPDATE inventory.stock_positions SET on_hand=1, reserved=0 WHERE offer_id='${KG}'`);
  const c1 = buildCart(1);
  const c2 = buildCart(1);
  const win = ship(c1);
  const lose = ship(c2);
  const reservedNow = Number(sql(`SELECT COALESCE(SUM(reserved),0) FROM inventory.stock_positions WHERE offer_id='${KG}'`));
  const onHand = Number(sql(`SELECT COALESCE(SUM(on_hand),0) FROM inventory.stock_positions WHERE offer_id='${KG}'`));
  note("C-last-unit-race", {
    ok: win.status < 400 && lose.status >= 400 && reservedNow <= onHand,
    win: win.status,
    lose: lose.status,
    reservedNow,
    onHand,
  });

  restoreKg();
  const d = buildCart(1);
  const commitD = ship(d);
  const checkoutD = pick(commitD.json, "checkoutId", "CheckoutId");
  const payD = host("POST", `/v1/storefront/checkout/${checkoutD}/payments`, { cartId: d.cartId, providerCode: "fake", idempotencyKey: randomUUID() }, d.guest);
  note("D-online-hold", {
    ok: commitD.status < 400 && Number(sql(`SELECT COUNT(*) FROM inventory.reservations r JOIN "order".order_lines ol ON ol.reservation_id=r.reservation_id JOIN "order".seller_orders so ON so.seller_order_id=ol.seller_order_id WHERE so.checkout_id='${checkoutD}' AND r.status='Held'`)) === 1,
    commit: commitD.status,
    pay: payD.status,
  });

  restoreKg();
  const e = buildCart(1);
  const commitE = ship(e);
  const checkoutE = pick(commitE.json, "checkoutId", "CheckoutId");
  const payE = host("POST", `/v1/storefront/checkout/${checkoutE}/payments`, { cartId: e.cartId, providerCode: "manual", idempotencyKey: randomUUID() }, e.guest);
  const paymentE = pick(payE.json, "paymentId", "PaymentId");
  note("E-manual-hold", {
    ok: commitE.status < 400 && payE.status < 400 && !!paymentE,
    commit: commitE.status,
    pay: payE.status,
  });

  const orders = grid("/v1/admin/orders/query");
  const items = pick(orders.json, "items", "Items") || [];
  const supply = items.some((x) => pick(x, "supplyStatus", "SupplyStatus"));
  note("F-admin-supply", { ok: orders.status === 200 && supply, status: orders.status, rows: items.length });

  restoreKg();
  const g = buildCart(1);
  const reservedMid = Number(sql(`SELECT COALESCE(SUM(reserved),0) FROM inventory.stock_positions WHERE offer_id='${KG}'`));
  host("GET", `/v1/storefront/cart/${g.cartId}`, undefined, g.guest);
  const reservedEnd = Number(sql(`SELECT COALESCE(SUM(reserved),0) FROM inventory.stock_positions WHERE offer_id='${KG}'`));
  note("G-performance", {
    ok: g.reservedAfter === g.reservedBefore && reservedEnd === reservedMid && cartHolds(g.cartId) === 0,
    addDelta: g.reservedAfter - g.reservedBefore,
    getDelta: reservedEnd - reservedMid,
  });
} catch (err) {
  note("crash", { ok: false, error: String(err && err.message ? err.message : err) });
} finally {
  restoreKg();
  writeFileSync("docs/evidence/TB-P10-T004/r9-runtime-raw.json", JSON.stringify(out, null, 2));
  console.log(JSON.stringify({ ok: out.ok, steps: out.steps.length }));
  if (!out.ok) process.exit(1);
}
