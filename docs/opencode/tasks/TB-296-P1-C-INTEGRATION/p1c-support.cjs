// Shared test-only infrastructure for TB-296 P1-C group A/B/C executable oracles.
// Isolated owned fixtures only. Never mutates product, seeds, guards, matrices or contracts.
const assert = require("node:assert/strict");
const { randomUUID, createHash } = require("node:crypto");
const { execFileSync } = require("node:child_process");
const fs = require("node:fs");
const path = require("node:path");

const HOST = "http://127.0.0.1:5088";
const ADMIN_HEADER = "X-Tooba-Dev-Actor-User-Id";
const SELLER_HEADER = "X-Tooba-Seller-Party-Id";
const SKU_PREFIX = "P1C-";
const EMPTY = x => Object.values(x).every(a => Array.isArray(a) && a.length === 0);

class NotRun extends Error {}

function uuid(value, name) {
  assert.match(value, /^[0-9a-f]{8}-(?:[0-9a-f]{4}-){3}[0-9a-f]{12}$/i, name + " must be a UUID");
  return value.toLowerCase();
}
const quote = x => "'" + String(x).replaceAll("'", "''") + "'";

function deterministicId(seller, variant, request) {
  const b = createHash("sha256")
    .update(uuid(seller).replaceAll("-", "") + "|" + uuid(variant).replaceAll("-", "") + "|" + request)
    .digest()
    .subarray(0, 16);
  const x = Buffer.from(b);
  x.subarray(0, 4).reverse();
  x.subarray(4, 6).reverse();
  x.subarray(6, 8).reverse();
  const h = x.toString("hex");
  return h.slice(0, 8) + "-" + h.slice(8, 12) + "-" + h.slice(12, 16) + "-" + h.slice(16, 20) + "-" + h.slice(20);
}

function psql(query) {
  return execFileSync(process.env.P1C_PSQL || "psql",
    ["-X", "-qAt", "-v", "ON_ERROR_STOP=1", "-d", "tooba_alpha", "-c", query],
    { encoding: "utf8", timeout: 30000, windowsHide: true }).trim();
}

function baselineBackup() {
  const file = process.env.P1C_BACKUP_FILE || path.join(__dirname, "p1c-baseline-backup-metadata.json");
  if (process.env.P1C_BACKUP_FILE && process.env.P1C_BACKUP_SHA256) {
    return { file: process.env.P1C_BACKUP_FILE, sha256: process.env.P1C_BACKUP_SHA256.toLowerCase() };
  }
  const meta = JSON.parse(fs.readFileSync(path.join(__dirname, "p1c-baseline-backup-metadata.json"), "utf8").replace(/^\uFEFF/, ""));
  return { file: file, sha256: String(meta.sha256).toLowerCase() };
}

let preflightCache = null;
function preflight() {
  if (preflightCache) return preflightCache;
  const backup = baselineBackup();
  const identity = process.env.P1C_DB_IDENTITY || "tooba_alpha|local|local";
  const absolute = fs.realpathSync(backup.file);
  const repo = fs.realpathSync(path.resolve(__dirname, "../../../.."));
  const relative = path.relative(repo, absolute);
  assert.ok(relative.startsWith(".." + path.sep) || path.isAbsolute(relative), "Backup must be outside repository");
  assert.equal(createHash("sha256").update(fs.readFileSync(absolute)).digest("hex"), backup.sha256, "Backup digest mismatch");
  execFileSync(process.env.P1C_PG_RESTORE || "pg_restore", ["--list", absolute], { stdio: "ignore", timeout: 30000, windowsHide: true });
  const actual = psql("SELECT current_database()||'|'||coalesce(inet_server_addr()::text,'local')||'|'||coalesce(inet_server_port()::text,'local')");
  assert.equal(actual, identity, "SQL target does not equal independently verified served Host database identity");
  assert.ok(actual.startsWith("tooba_alpha|"), "Not authorized DEV database");
  preflightCache = { database: actual, backupSha256: backup.sha256, backupFile: absolute };
  return preflightCache;
}

function snapshot(ids) {
  if (!ids.length) {
    return { offers: [], history: [], prices: [], priceFacts: [], positions: [], inventoryFacts: [], allocations: [], reservations: [], offerOutbox: [], priceOutbox: [], inventoryOutbox: [] };
  }
  const keys = ids.map(x => quote(uuid(x))).join(",");
  const outbox = ids.map(x => "strpos(payload::text," + quote('"' + uuid(x) + '"') + ")>0").join(" OR ");
  const rows = (schema, table, where) => "(SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY to_jsonb(t)::text),'[]'::jsonb) FROM " + schema + "." + table + " t WHERE " + where + ")";
  const messages = schema => "(SELECT coalesce(jsonb_agg(payload ORDER BY payload::text),'[]'::jsonb) FROM " + schema + ".outbox_messages WHERE " + outbox + ")";
  const predicate = "offer_id IN (" + keys + ")";
  const reservations = "(SELECT coalesce(jsonb_agg(to_jsonb(r) ORDER BY to_jsonb(r)::text),'[]'::jsonb) FROM inventory.reservations r WHERE r.stock_item_id IN (SELECT stock_item_id FROM inventory.stock_positions WHERE offer_id IN (" + keys + ")))";
  return JSON.parse(psql("SELECT jsonb_build_object(" +
    "'offers'," + rows("offer", "offers", predicate) + "," +
    "'history'," + rows("offer", "offer_history", predicate) + "," +
    "'prices'," + rows("pricing", "prices", predicate) + "," +
    "'priceFacts'," + rows("pricing", "offer_price_changes", predicate) + "," +
    "'positions'," + rows("inventory", "stock_positions", predicate) + "," +
    "'inventoryFacts'," + rows("inventory", "offer_inventory_changes", predicate) + "," +
    "'allocations'," + rows("inventory", "offer_allocations", predicate) + "," +
    "'reservations'," + reservations + "," +
    "'offerOutbox'," + messages("offer") + "," +
    "'priceOutbox'," + messages("pricing") + "," +
    "'inventoryOutbox'," + messages("inventory") + ")"));
}

function cleanupExact(ids, seller) {
  if (!ids.length) return { beforeCleanup: null, afterCleanup: null };
  const before = snapshot(ids);
  assert.ok(before.offers.every(r => ids.includes(r.offer_id) && r.seller_party_id === seller && String(r.seller_sku || "").startsWith(SKU_PREFIX)),
    "Unexpected fixture identity; refusing to delete any row");
  assert.ok(before.positions.every(r => ids.includes(r.offer_id)), "Unexpected position identity; refusing cleanup");
  const keys = ids.map(x => quote(uuid(x))).join(",");
  const outbox = ids.map(x => "strpos(payload::text," + quote('"' + uuid(x) + '"') + ")>0").join(" OR ");
  psql("BEGIN;" +
    " DELETE FROM inventory.reservations WHERE stock_item_id IN (SELECT stock_item_id FROM inventory.stock_positions WHERE offer_id IN (" + keys + "));" +
    " DELETE FROM inventory.offer_inventory_changes WHERE offer_id IN (" + keys + ");" +
    " DELETE FROM inventory.offer_allocations WHERE offer_id IN (" + keys + ");" +
    " DELETE FROM inventory.stock_positions WHERE offer_id IN (" + keys + ");" +
    " DELETE FROM pricing.offer_price_changes WHERE offer_id IN (" + keys + ");" +
    " DELETE FROM pricing.prices WHERE offer_id IN (" + keys + ");" +
    " DELETE FROM offer.offer_history WHERE offer_id IN (" + keys + ");" +
    " DELETE FROM offer.offers WHERE offer_id IN (" + keys + ");" +
    " DELETE FROM offer.outbox_messages WHERE " + outbox + ";" +
    " DELETE FROM pricing.outbox_messages WHERE " + outbox + ";" +
    " DELETE FROM inventory.outbox_messages WHERE " + outbox + ";" +
    " COMMIT;");
  const after = snapshot(ids);
  assert.ok(EMPTY(after), "Residual fixture data after cleanup: " + JSON.stringify(Object.fromEntries(Object.entries(after).map(([k, v]) => [k, v.length]))));
  return { beforeCleanup: before, afterCleanup: after };
}

async function http(route, method = "GET", payload, headers = {}) {
  const res = await fetch(HOST + route, {
    method,
    headers: { "Accept-Language": "fa-IR", ...(payload !== undefined ? { "Content-Type": "application/json" } : {}), ...headers },
    body: payload !== undefined ? JSON.stringify(payload) : undefined,
    signal: AbortSignal.timeout(20000),
  });
  const raw = await res.text();
  let body;
  try { body = raw ? JSON.parse(raw) : null; } catch { body = raw; }
  return { status: res.status, body };
}

async function runCase(id, body) {
  const root = process.env.P1C_EVIDENCE_DIR;
  assert.ok(root, "Set P1C_EVIDENCE_DIR to a new run-owned directory");
  fs.mkdirSync(root, { recursive: true });
  const evidence = path.join(root, id + ".json");
  assert.ok(!fs.existsSync(evidence), "Do not overwrite historical evidence");
  const trace = { id, requests: [], sql: [], status: "NOT_RUN" };
  const state = { offerIds: [], createdAny: false, seller: null };
  const save = () => fs.writeFileSync(evidence, JSON.stringify(trace, null, 2));
  const addOffer = value => { const v = uuid(value); if (!state.offerIds.includes(v)) state.offerIds.push(v); state.createdAny = true; return v; };
  async function api(route, method = "GET", payload, headers = {}) {
    const req = { route, method, payload };
    trace.requests.push(req);
    save();
    const res = await http(route, method, payload, headers);
    Object.assign(req, { status: res.status, body: res.body });
    save();
    return req;
  }
  const ctx = {
    id,
    state,
    trace,
    api,
    sql: psql,
    preflight,
    addOffer,
    snapshot: () => snapshot(state.offerIds),
    observe(label) { const value = snapshot(state.offerIds); trace.sql.push({ label, state: value }); save(); return value; },
    evidence,
  };
  try {
    trace.preflight = preflight();
    save();
    const result = await body(ctx);
    trace.status = result && result.status ? result.status : "PASS";
    trace.reason = result && result.reason ? result.reason : "All executable assertions observed on actual HTTP and independent PostgreSQL";
  } catch (error) {
    trace.status = error instanceof NotRun ? "NOT_RUN" : "FAIL";
    trace.reason = String(error && error.stack ? error.stack : error);
  } finally {
    if (state.offerIds.length) {
      try {
        const result = cleanupExact(state.offerIds, state.seller || uuid(process.env.P1C_SELLER_ID || ""));
        Object.assign(trace, result);
      } catch (error) {
        trace.status = "FAIL";
        trace.cleanupError = String(error && error.stack ? error.stack : error);
        trace.reason = (trace.reason || "") + "; cleanup not certified";
      }
    }
    save();
  }
  return { status: trace.status, reason: trace.reason, evidence };
}

module.exports = {
  assert, randomUUID, HOST, ADMIN_HEADER, SELLER_HEADER, SKU_PREFIX, EMPTY,
  NotRun, uuid, quote, deterministicId, psql, preflight, snapshot, runCase, http,
};
