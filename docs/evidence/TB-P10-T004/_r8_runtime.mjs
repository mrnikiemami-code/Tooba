import { writeFileSync, readFileSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { randomUUID } from "node:crypto";
const HOST = "http://127.0.0.1:5088";
const KG = "01a03826-9936-7000-b499-ff26a6123a8c";
const ADMIN = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";
const stamp = Date.now();
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
function grid(path) {
  return host("POST", path, { page: 1, pageSize: 50, sort: [{ field: "created", direction: "desc" }], filters: [] }, admin());
}
function buildCart(qty = 1) {
  const cart = host("POST", "/v1/storefront/cart");
  const secret = pick(cart.json, "guestSecret", "GuestSecret");
  const cartId = pick(cart.json, "cartId", "CartId");
  let version = pick(cart.json, "version", "Version");
  const guest = { "X-Tooba-Guest-Secret": secret };
  const line = host("POST", `/v1/storefront/cart/${cartId}/lines?expectedVersion=${version}`, { offerId: KG, quantity: qty }, guest);
  if (line.status >= 400) throw new Error("add " + line.text);
  version = pick(line.json, "version", "Version");
  return { cartId, secret, guest, version };
}
function ship(session) {
  const proj = host("POST", "/v1/storefront/shipping/projection", { cartId: session.cartId, provinceName: "تهران", methodCode: "post:express", language: "fa" }, session.guest);
  const minDate = pick(proj.json, "minimumDeliveryDate", "MinimumDeliveryDate");
  host("PUT", "/v1/storefront/shipping/selection", { cartId: session.cartId, expectedCartVersion: session.version, recipientName: "R8", contactMobile: "+989121240101", provinceName: "تهران", cityName: "تهران", postalAddress: "a", postalCode: "1234567890", shippingMethodCode: "post:express", selectedDeliveryDate: minDate, selectedDeliveryTimeWindow: "9-12", customerNote: "r8" }, session.guest);
  const commit = host("POST", "/v1/storefront/shipping/commit", { cartId: session.cartId, expectedCartVersion: session.version, idempotencyKey: randomUUID() }, session.guest);
  if (commit.status >= 400) throw new Error("commit " + commit.text);
  return { ...session, checkoutId: pick(commit.json, "checkoutId", "CheckoutId") };
}
function pay(s) {
  const r = host("POST", `/v1/storefront/checkout/${s.checkoutId}/payments`, { cartId: s.cartId, providerCode: "manual", idempotencyKey: randomUUID() }, s.guest);
  return pick(r.json, "paymentId", "PaymentId");
}
function resv(checkoutId) {
  const row = sql(`SELECT r.reservation_id||'|'||r.status||'|'||COALESCE(r.expires_at::text,'') FROM "order".order_lines ol JOIN "order".seller_orders so ON so.seller_order_id=ol.seller_order_id JOIN inventory.reservations r ON r.reservation_id=ol.reservation_id WHERE so.checkout_id='${checkoutId}' LIMIT 1`);
  if (!row) return null;
  const [id, status, expiresAt] = row.split("|");
  return { id, status, expiresAt: expiresAt || null };
}
function release(checkoutId) {
  const r = resv(checkoutId);
  if (!r?.id) return null;
  sql(`UPDATE inventory.reservations SET status='Released', updated_at=NOW() WHERE reservation_id='${r.id}'`);
  sql(`UPDATE inventory.stock_positions SET reserved=GREATEST(reserved-1,0) WHERE stock_item_id=(SELECT stock_item_id FROM inventory.reservations WHERE reservation_id='${r.id}')`);
  return r.id;
}
function findRow(page, checkoutId) {
  const items = pick(page, "items", "Items") || [];
  return items.find((x) => String(pick(x, "checkoutId", "CheckoutId")) === checkoutId);
}
try {
  note("health", { status: curl(["-H", "Host: alpha.localhost", HOST + "/health"]).status, ok: true });
  const reserved = ship(buildCart());
  pay(reserved);
  const reacq = ship(buildCart());
  const reacqPay = pay(reacq);
  host("POST", `/v1/storefront/payments/${reacqPay}/manual-evidence`, { cartId: reacq.cartId, transferReference: `TRK-R8-D-${stamp}`, proofMediaAssetId: null }, reacq.guest);
  release(reacq.checkoutId);

  const orders1 = grid("/v1/admin/orders/query");
  const reservedRow = findRow(orders1.json, reserved.checkoutId);
  const reacqRow = findRow(orders1.json, reacq.checkoutId);
  const pays = grid("/v1/admin/payments/query");
  const payRow = (pick(pays.json, "items", "Items") || []).find((x) => String(pick(x, "checkoutId", "CheckoutId")) === reacq.checkoutId);
  note("C-payments-grid", {
    ok: pays.status < 400 && payRow && String(pick(payRow, "supplyStatus", "SupplyStatus")).length > 0,
    status: pays.status,
    supply: pick(payRow, "supplyStatus", "SupplyStatus"),
  });

  const opsD = host("GET", `/v1/admin/orders/${reacq.checkoutId}/operations`, undefined, admin());
  const confirmD = host("POST", `/v1/admin/orders/${reacq.checkoutId}/operations`, { code: "confirm_deposit", idempotencyKey: randomUUID().replaceAll("-", "") }, admin());
  const paid = resv(reacq.checkoutId);
  const recoverShownD = JSON.stringify(opsD.json).includes("recover_inventory_reservation");
  note("D-confirm-reacquire", {
    ok: confirmD.status < 400 && !paid?.expiresAt && !recoverShownD,
    confirm: confirmD.status,
    paidExpires: paid?.expiresAt,
    recoverHidden: !recoverShownD,
    hint: pick(opsD.json, "supplyMessageFa", "SupplyMessageFa"),
  });

  const unavail = ship(buildCart());
  const unavailPay = pay(unavail);
  host("POST", `/v1/storefront/payments/${unavailPay}/manual-evidence`, { cartId: unavail.cartId, transferReference: `TRK-R8-E-${stamp}`, proofMediaAssetId: null }, unavail.guest);
  const ur = resv(unavail.checkoutId);
  release(unavail.checkoutId);
  if (ur?.id) sql(`UPDATE inventory.stock_positions SET on_hand=0, reserved=0 WHERE stock_item_id=(SELECT stock_item_id FROM inventory.reservations WHERE reservation_id='${ur.id}')`);

  const orders2 = grid("/v1/admin/orders/query");
  const unavailRow = findRow(orders2.json, unavail.checkoutId);
  note("A-orders-grid", {
    ok: orders1.status < 400 && orders2.status < 400
      && String(pick(reservedRow, "supplyStatus", "SupplyStatus")) === "Reserved"
      && String(pick(reacqRow, "supplyStatus", "SupplyStatus")) === "AvailableForReacquire"
      && ["Unavailable", "PartiallyUnavailable"].includes(String(pick(unavailRow, "supplyStatus", "SupplyStatus"))),
    reserved: pick(reservedRow, "supplyStatus", "SupplyStatus"),
    reacquire: pick(reacqRow, "supplyStatus", "SupplyStatus"),
    unavailable: pick(unavailRow, "supplyStatus", "SupplyStatus"),
  });

  const detail = host("GET", `/v1/admin/orders/${unavail.checkoutId}/supply-status`, undefined, admin());
  const lines = pick(detail.json, "lines", "Lines") || [];
  note("B-order-detail", {
    ok: detail.status < 400 && lines.some((l) => Number(pick(l, "shortage", "Shortage")) > 0 || pick(l, "lineStatus", "LineStatus") === "Unavailable"),
    status: pick(detail.json, "status", "Status"),
  });

  const confirmE = host("POST", `/v1/admin/orders/${unavail.checkoutId}/operations`, { code: "confirm_deposit", idempotencyKey: randomUUID().replaceAll("-", "") }, admin());
  const code = pick(confirmE.json, "errorCode", "ErrorCode");
  const payGet = host("GET", `/v1/storefront/payments/${unavailPay}?cartId=${unavail.cartId}`, undefined, unavail.guest);
  const pstat = String(pick(payGet.json, "status", "Status") || "").toLowerCase();
  note("E-confirm-unavailable", {
    ok: confirmE.status >= 400 && String(code).includes("inventory.supply.unavailable") && pstat !== "succeeded",
    status: confirmE.status,
    code,
    paymentStatus: pstat,
  });

  const opsF = host("GET", `/v1/admin/orders/${reserved.checkoutId}/operations`, undefined, admin());
  note("F-recovery-capability", {
    ok: opsF.status < 400 && pick(opsF.json, "canRecoverInventory", "CanRecoverInventory") === false,
    canRecover: pick(opsF.json, "canRecoverInventory", "CanRecoverInventory"),
    supply: pick(opsF.json, "supplyStatus", "SupplyStatus"),
  });

  const fe = readFileSync("src/frontend/app/admin/admin-api.ts", "utf8") + readFileSync("src/frontend/app/admin/admin-order-supply.ts", "utf8");
  note("G-labels-rtl", {
    ok: fe.includes("تأمین‌شده") && fe.includes("قابل تأمین") && fe.includes("غیرقابل تأمین"),
  });

  if (ur?.id) sql(`UPDATE inventory.stock_positions SET on_hand=200, reserved=0 WHERE stock_item_id=(SELECT stock_item_id FROM inventory.reservations WHERE reservation_id='${ur.id}')`);
} catch (e) {
  out.ok = false;
  note("fatal", { ok: false, error: String(e.message || e) });
}
writeFileSync("docs/evidence/TB-P10-T004/r8-runtime-raw.json", JSON.stringify(out, null, 2));
console.log(JSON.stringify({ ok: out.ok, steps: out.steps.length }));
process.exit(out.ok ? 0 : 1);
