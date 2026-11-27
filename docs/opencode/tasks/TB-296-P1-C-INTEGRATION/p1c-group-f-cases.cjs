// TB-296 P1-C group F — executable authorization/ownership, deferred D2 and F-BENTRY oracles.
// Real Host 5088 + PostgreSQL tooba_alpha; isolated P1C-owned fixtures; exact finally cleanup.
// Allocation/reservation/recovery are executed through the real Inventory owner contracts in an
// external test-only harness because the authorized DEV Host is a SingleStore tenant and exposes
// no marketplace allocation surface. No product, seed, guard, contract, test or matrix is modified.
const fs = require("node:fs");
const os = require("node:os");
const path = require("node:path");
const { randomUUID } = require("node:crypto");
const { execFileSync } = require("node:child_process");
const S = require("./p1c-support.cjs");
const { assert, uuid, quote, deterministicId, psql, preflight, NotRun, HOST, ADMIN_HEADER, SELLER_HEADER, SKU_PREFIX } = S;

const FRONT = "http://127.0.0.1:3000";
function loadDevConnection() {
  if (process.env.P1C_DB_CONNECTION) return process.env.P1C_DB_CONNECTION;
  const settingsPath = path.resolve(__dirname, "../../../../src/backend/Host/Tooba.Host/appsettings.Development.json");
  const settings = JSON.parse(fs.readFileSync(settingsPath, "utf8"));
  const connection = settings?.Tooba?.PostgreSQL?.ConnectionReferences?.["tenant-alpha"];
  if (!connection) throw new Error("Set P1C_DB_CONNECTION or provide the authorized DEV tenant-alpha connection reference");
  return connection;
}
const CONN = loadDevConnection();
const HARNESS = process.env.P1C_F_HARNESS_DLL ||
  path.join(os.tmpdir(), "opencode", "p1c-f-harness", "bin", "Debug", "net8.0", "p1c-f-harness.dll");
const PLAYWRIGHT = process.env.P1C_PLAYWRIGHT || [
  path.resolve(__dirname, "../../../../src/frontend/node_modules/playwright"),
  "C:\\Users\\User\\OpenCode-UI-Lab-20261122-064208\\node_modules\\playwright",
  "C:\\Users\\User\\.cache\\codex-runtimes\\codex-primary-runtime\\dependencies\\node\\node_modules\\playwright",
].find(candidate => fs.existsSync(candidate));

if (!process.env.P1C_PSQL) process.env.P1C_PSQL = path.join(__dirname, "p1c-psql.exe");
if (!process.env.P1C_PG_RESTORE) process.env.P1C_PG_RESTORE = path.join(__dirname, "p1c-pg-restore.exe");
if (!process.env.P1C_BACKUP_FILE) process.env.P1C_BACKUP_FILE = "C:\\Users\\User\\AppData\\Local\\Temp\\opencode\\p1c-baseline-20261127062248.dump";
if (!process.env.P1C_BACKUP_SHA256) process.env.P1C_BACKUP_SHA256 = "D5E2DF7B6F7AF59005EE966531D74A09187E08C9A1153EF2E752014BA477ADE5";
if (!process.env.P1C_DB_IDENTITY) process.env.P1C_DB_IDENTITY = "tooba_alpha|local|local";

const sql = q => psql(q);
const scalar = q => psql(q);
const sqlJson = q => JSON.parse(psql(q));
const nowIso = () => new Date().toISOString();

function check(condition, message) {
  if (!condition) throw new Error(message);
}
function findStep(harness, keyOrLabel, value) {
  const steps = harness.steps || [];
  const found = steps.find(s => s && (s[keyOrLabel] === value));
  check(found, "Harness step not found: " + keyOrLabel + "=" + value);
  return found;
}
function fixtureOf(harness, key) {
  const list = (harness.fixture || []).filter(x => (x.OfferKey || x.offerKey) === key);
  check(list.length > 0, "Harness fixture missing offer " + key);
  return list;
}

async function callRun(id, body) {
  const root = process.env.P1C_EVIDENCE_DIR;
  assert.ok(root, "Set P1C_EVIDENCE_DIR to a new run-owned directory");
  fs.mkdirSync(root, { recursive: true });
  const evidence = path.join(root, id + ".json");
  assert.ok(!fs.existsSync(evidence), "Do not overwrite historical evidence");
  const trace = { id, generatedAt: nowIso(), requests: [], sql: [], harness: [], status: "NOT_RUN" };
  const state = { offers: [], codes: [], orderFixtures: [], access: null, harnessCfgs: [] };
  const save = () => fs.writeFileSync(evidence, JSON.stringify(trace, null, 2));
  const api = async (route, method = "GET", payload, headers = {}) => {
    const req = { route, method, payload };
    trace.requests.push(req); save();
    const res = await S.http(route, method, payload, headers);
    Object.assign(req, res); save();
    return req;
  };
  const getJson = async (route, headers) => {
    const r = await api(route, "GET", undefined, headers);
    check(r.status === 200, methodRoute(route) + " expected 200 got " + r.status + " " + JSON.stringify(r.body));
    return r.body;
  };
  const addOffer = (offerId, sellerId) => {
    const value = uuid(offerId);
    if (!state.offers.some(x => x.offerId === value)) state.offers.push({ offerId: value, sellerId: uuid(sellerId) });
    return value;
  };
  const addCode = code => { if (!state.codes.includes(code)) state.codes.push(code); };
  const observe = label => {
    const value = S.snapshot(state.offers.map(x => x.offerId));
    trace.sql.push({ label, state: value }); save(); return value;
  };
  const ctx = {
    id, trace, state, api, getJson, addOffer, addCode, observe, save,
    evidence, adminHeaders: null, sellers: null, adminActor: null,
  };
  try {
    trace.preflight = preflight(); save();
    await body(ctx);
    trace.status = "PASS";
    trace.reason = "All executable assertions observed on actual HTTP/owner-contract/PostgreSQL execution";
  } catch (error) {
    trace.status = error instanceof NotRun ? "NOT_RUN" : "FAIL";
    trace.reason = String(error && error.stack ? error.stack : error);
  } finally {
    try { await cleanup(ctx); }
    catch (error) {
      trace.status = "FAIL";
      trace.cleanupError = String(error && error.stack ? error.stack : error);
      trace.reason = (trace.reason || "") + "; cleanup not certified";
    }
    try {
      const ids = state.offers.map(x => x.offerId);
      if (ids.length) {
        const after = S.snapshot(ids);
        trace.afterCleanup = after;
        check(S.EMPTY(after), "Residual fixture rows after cleanup: " + JSON.stringify(Object.fromEntries(Object.entries(after).map(([k, v]) => [k, v.length]))));
      }
    } catch (error) {
      trace.status = "FAIL";
      trace.cleanupError = String(error && error.stack ? error.stack : error);
    }
    save();
  }
  return { status: trace.status, reason: trace.reason, evidence };
}
const methodRoute = route => route;

async function resolveActors(ctx) {
  if (ctx.sellers) return ctx.sellers;
  const dev = await ctx.api("/v1/admin/dev-context");
  check(dev.status === 200, "admin dev-context " + dev.status);
  ctx.adminActor = uuid(dev.body.actorUserId);
  ctx.adminHeaders = { [ADMIN_HEADER]: ctx.adminActor };
  const contexts = await ctx.api("/v1/seller/dev-contexts");
  check(contexts.status === 200 && Array.isArray(contexts.body.actors), "seller dev-contexts");
  const actors = contexts.body.actors;
  const a = actors.find(x => x.sellerPartyId.toLowerCase() === "01a030d1-40cb-7000-8abe-6d31739956c5") || actors[0];
  const b = actors.find(x => x.sellerPartyId.toLowerCase() === "01a030d1-40db-7000-b90c-a0705133f0eb") || actors[1];
  check(a && b && a.sellerPartyId !== b.sellerPartyId, "two independent synthetic seller contexts required");
  ctx.sellers = {
    A: { sellerPartyId: uuid(a.sellerPartyId), actorUserId: uuid(a.actorUserId), label: a.sellerLabel },
    B: { sellerPartyId: uuid(b.sellerPartyId), actorUserId: uuid(b.actorUserId), label: b.sellerLabel },
  };
  ctx.sellers.A.headers = { [ADMIN_HEADER]: ctx.sellers.A.actorUserId, [SELLER_HEADER]: ctx.sellers.A.sellerPartyId };
  ctx.sellers.B.headers = { [ADMIN_HEADER]: ctx.sellers.B.actorUserId, [SELLER_HEADER]: ctx.sellers.B.sellerPartyId };
  return ctx.sellers;
}

async function chooseVariant(ctx, sellerId, used = []) {
  const forced = process.env.P1C_CREATE_VARIANT_ID ? uuid(process.env.P1C_CREATE_VARIANT_ID) : null;
  const candidates = forced ? [forced] : scalar(
    "SELECT v.variant_id FROM catalog.variants v JOIN catalog.products p ON p.product_id=v.product_id " +
    "WHERE v.status='Published' AND p.quantity_decimal_places=0 ORDER BY v.variant_id LIMIT 400"
  ).split(/\r?\n/).filter(Boolean).map(uuid);
  for (const candidate of candidates) {
    if (used.includes(candidate)) continue;
    const active = await ctx.api("/v1/admin/offers/active-listing?sellerPartyId=" + sellerId + "&catalogVariantId=" + candidate + "&channel=Marketplace", "GET", undefined, ctx.adminHeaders);
    if (active.status === 200 && active.body === null) return candidate;
  }
  throw new NotRun("No unused published integer-precision Catalog variant is available for an isolated fixture.");
}

async function createOffer(ctx, sellerId, variantId, extra = {}) {
  const requestId = "P1C-" + randomUUID();
  const offerId = deterministicId(sellerId, variantId, requestId);
  const sku = SKU_PREFIX + randomUUID();
  const res = await ctx.api("/v1/admin/offers", "POST", {
    sellerPartyId: sellerId, catalogVariantId: variantId, channel: "Marketplace",
    sellerSku: sku, status: "Draft", requestId, ...extra,
  }, ctx.adminHeaders);
  check(res.status === 201, "create offer " + res.status + " " + JSON.stringify(res.body));
  check(res.body.offerId === offerId, "create offer identity mismatch");
  ctx.addOffer(offerId, sellerId);
  return { offerId, sku, variantId, sellerId };
}

const detailOf = (ctx, offerId) => ctx.getJson("/v1/admin/offers/" + uuid(offerId), ctx.adminHeaders);
const sellerDetailOf = (ctx, offerId, seller) => ctx.api("/v1/seller/offers/" + uuid(offerId), "GET", undefined, seller.headers);

async function assign(ctx, offerId, responsibility, expectedVersion) {
  const before = await detailOf(ctx, offerId);
  const version = expectedVersion || before.revisions.offer;
  const res = await ctx.api("/v1/admin/offers/" + uuid(offerId) + "/fulfillment", "PUT",
    { responsibility, shippingTypeId: null, expectedVersion: version }, ctx.adminHeaders);
  return { res, before, after: res.status === 200 ? await detailOf(ctx, offerId) : null };
}

async function provision(ctx, offerId, locationId) {
  const res = await ctx.api("/v1/admin/offers/" + uuid(offerId) + "/inventory/positions", "POST", { locationId }, ctx.adminHeaders);
  check(res.status === 200, "provision " + res.status + " " + JSON.stringify(res.body));
  return res.body;
}

async function setStock(ctx, offerId, stockItemId, onHand, reason = "P1C F fixture stock") {
  const detail = await detailOf(ctx, offerId);
  const row = (detail.inventoryLocations || []).find(x => x.stockItemId === stockItemId);
  check(row && row.revision, "stock row/revision missing");
  const res = await ctx.api("/v1/admin/offers/" + uuid(offerId) + "/inventory", "PUT",
    { onHand, reason, stockItemId, expectedVersion: row.revision, operationId: randomUUID() }, ctx.adminHeaders);
  return { res, detail, row };
}

async function runHarness(ctx, command, config) {
  check(fs.existsSync(HARNESS), "Inventory owner-contract harness is missing: " + HARNESS);
  const cfgPath = path.join(process.env.P1C_EVIDENCE_DIR, ctx.id + "-" + command + "-" + randomUUID() + ".cfg.json");
  fs.writeFileSync(cfgPath, JSON.stringify(config, null, 2));
  ctx.state.harnessCfgs.push(cfgPath);
  const output = execFileSync("dotnet", [HARNESS, CONN, command, cfgPath], {
    encoding: "utf8", timeout: 180000, windowsHide: true, maxBuffer: 32 * 1024 * 1024,
  });
  let parsed;
  try { parsed = JSON.parse(output); } catch { throw new Error("Harness output is not JSON: " + output.slice(0, 400)); }
  ctx.trace.harness.push({ command, config, output: parsed });
  if (parsed.ok !== true) throw new Error("Harness " + command + " failed: " + JSON.stringify(parsed.error || parsed));
  for (const item of parsed.fixture || []) ctx.addCode(item.Code || item.code);
  return parsed;
}

function harnessOffers(ctx, definitions) {
  // definitions: [{key, seller, variant, positionCount}]
  return {
    offers: definitions.map(d => ({
      key: d.key, offerId: d.offer.offerId, sellerId: d.seller.sellerPartyId, variantId: d.offer.variantId,
      responsibility: null,
      positions: Array.from({ length: d.positionCount || 1 }, () => ({ ownerKind: "Marketplace", stock: 100, eligible: true })),
    })),
  };
}

async function prepareSellerB(ctx) {
  const sellers = await resolveActors(ctx);
  const sellerId = sellers.B.sellerPartyId;
  const base = "/v1/admin/sellers/" + sellerId + "/access-control";
  const snapshot = {
    ceiling: sqlJson("SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY to_jsonb(t)::text),'[]'::jsonb) FROM access_control.platform_seller_ceilings t WHERE seller_party_id=" + quote(sellerId)),
    roles: sqlJson("SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY to_jsonb(t)::text),'[]'::jsonb) FROM access_control.roles t WHERE owner_scope_id=" + quote(sellerId)),
    assignments: sqlJson("SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY to_jsonb(t)::text),'[]'::jsonb) FROM access_control.user_role_assignments t WHERE owner_scope_id=" + quote(sellerId)),
  };
  ctx.state.access = { sellerId, actorUserId: sellers.B.actorUserId, snapshot, roleId: null, assignmentId: null, granted: false };
  ctx.trace.accessSnapshot = {
    sellerId,
    ceiling: snapshot.ceiling,
    roles: snapshot.roles,
    assignments: snapshot.assignments,
    counts: { ceiling: snapshot.ceiling.length, roles: snapshot.roles.length, assignments: snapshot.assignments.length },
  };
  ctx.save();
  const ceiling = await ctx.getJson(base + "/ceiling", ctx.adminHeaders);
  const entries = ceiling.map(e => ({
    permissionId: e.permissionId,
    enabled: ["product.create", "product.view", "product.edit"].includes(e.permissionId) ? true : e.enabled,
    scopeKind: e.scopeKind, scopeResourceId: e.scopeResourceId || null,
  }));
  const put = await ctx.api(base + "/ceiling", "PUT", { entries }, ctx.adminHeaders);
  check([200,204].includes(put.status), "sellerB ceiling grant " + put.status + " " + JSON.stringify(put.body));
  const code = "p1c-f-" + ctx.id.toLowerCase() + "-" + Date.now().toString(36);
  const role = await ctx.api(base + "/roles", "POST", { name: "P1C F " + ctx.id, code, description: "P1C group F temporary fixture" }, ctx.adminHeaders);
  check(role.status === 200 || role.status === 201, "sellerB role " + role.status + " " + JSON.stringify(role.body));
  const roleId = uuid(role.body.id);
  ctx.state.access.roleId = roleId;
  const perms = await ctx.api(base + "/roles/" + roleId + "/permissions", "PUT", [
    { permissionId: "product.create", scopeKind: "GlobalWithinOwner", scopeResourceId: null, enabled: true },
    { permissionId: "product.view", scopeKind: "GlobalWithinOwner", scopeResourceId: null, enabled: true },
    { permissionId: "product.edit", scopeKind: "GlobalWithinOwner", scopeResourceId: null, enabled: true },
  ], ctx.adminHeaders);
  check([200,204].includes(perms.status), "sellerB role permissions " + perms.status + " " + JSON.stringify(perms.body));
  const assignment = await ctx.api(base + "/assignments", "POST", { userId: sellers.B.actorUserId, roleId }, ctx.adminHeaders);
  check(assignment.status === 200 || assignment.status === 201, "sellerB assignment " + assignment.status + " " + JSON.stringify(assignment.body));
  ctx.state.access.assignmentId = uuid(assignment.body.id);
  ctx.state.access.granted = true;
  const capabilities = await ctx.api("/v1/seller/access-control/me/capabilities", "GET", undefined, sellers.B.headers);
  check(capabilities.status === 200, "sellerB capabilities " + capabilities.status);
  check((capabilities.body.permissions || []).some(x => x.permissionId === "product.edit"), "sellerB product.edit not effective");
  ctx.trace.accessGrant = capabilities.body; ctx.save();
}

function restoreSellerB(ctx) {
  const access = ctx.state.access;
  if (!access) return;
  if (access.assignmentId) sql("DELETE FROM access_control.user_role_assignments WHERE id=" + quote(access.assignmentId) + " AND user_id=" + quote(access.actorUserId));
  if (access.roleId) {
    sql("DELETE FROM access_control.role_permissions WHERE role_id=" + quote(access.roleId));
    sql("DELETE FROM access_control.roles WHERE id=" + quote(access.roleId) + " AND owner_scope_id=" + quote(access.sellerId));
  }
  sql("DELETE FROM access_control.platform_seller_ceilings WHERE seller_party_id=" + quote(access.sellerId));
  for (const row of access.snapshot.ceiling) {
    sql("INSERT INTO access_control.platform_seller_ceilings (id, seller_party_id, permission_id, enabled, updated_at, scope_kind, scope_resource_id) VALUES (" +
      [row.id, row.seller_party_id, row.permission_id, row.enabled, row.updated_at, row.scope_kind, row.scope_resource_id].map((v, i) => v === null ? "NULL" : (i === 3 ? String(v) : quote(v))).join(",") + ")");
  }
  const after = sqlJson("SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY to_jsonb(t)::text),'[]'::jsonb) FROM access_control.platform_seller_ceilings t WHERE seller_party_id=" + quote(access.sellerId));
  const afterRoles = sqlJson("SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY to_jsonb(t)::text),'[]'::jsonb) FROM access_control.roles t WHERE owner_scope_id=" + quote(access.sellerId));
  const afterAssignments = sqlJson("SELECT coalesce(jsonb_agg(to_jsonb(t) ORDER BY to_jsonb(t)::text),'[]'::jsonb) FROM access_control.user_role_assignments t WHERE owner_scope_id=" + quote(access.sellerId));
  ctx.trace.accessRestore = {
    after,
    afterRoles,
    afterAssignments,
    matchesCeilingSnapshot: JSON.stringify(after) === JSON.stringify(access.snapshot.ceiling),
    createdRoleRemoved: access.roleId ? !afterRoles.some(x => x.id === access.roleId) : true,
    createdAssignmentRemoved: access.assignmentId ? !afterAssignments.some(x => x.id === access.assignmentId) : true,
    beforeCounts: { ceiling: access.snapshot.ceiling.length, roles: access.snapshot.roles.length, assignments: access.snapshot.assignments.length },
    afterCounts: { ceiling: after.length, roles: afterRoles.length, assignments: afterAssignments.length },
    preservedLegacyRoles: access.snapshot.roles.length === afterRoles.length,
    preservedLegacyAssignments: access.snapshot.assignments.length === afterAssignments.length,
  };
  if (!ctx.trace.accessRestore.matchesCeilingSnapshot || !ctx.trace.accessRestore.createdRoleRemoved || !ctx.trace.accessRestore.createdAssignmentRemoved) {
    throw new Error("sellerB access fixture was not restored exactly: " + JSON.stringify(ctx.trace.accessRestore));
  }
  ctx.save();
}

function insertOrderFixture(ctx, { offerId, sellerId, variantId, status, cancelledFrom }) {
  const checkoutId = randomUUID();
  const sellerOrderId = randomUUID();
  const lineId = randomUUID();
  const buyer = randomUUID();
  sql("INSERT INTO \"order\".checkouts (checkout_id, idempotency_key, cart_id, mode, buyer_party_id, placed_by_user_id, market, currency, channel, created_at, submitted_at) VALUES (" +
    [quote(checkoutId), quote("P1C-F-" + checkoutId), quote(randomUUID()), quote("OnlinePurchase"), quote(buyer), quote(ctx.adminActor), quote("IR"), quote("IRR"), quote("Marketplace")].join(",") +
    ",now(),now())");
  sql("INSERT INTO \"order\".seller_orders (seller_order_id, checkout_id, order_number, seller_party_id, status, subtotal_snapshot, tax_snapshot, discount_snapshot, grand_total_snapshot, currency, cancelled_from_status, total_item_count, total_quantity) VALUES (" +
    [quote(sellerOrderId), quote(checkoutId), quote("P1C-F-" + sellerOrderId.slice(0, 8)), quote(sellerId), quote(status), "1000", "0", "0", "1000", quote("IRR"), cancelledFrom ? quote(cancelledFrom) : "NULL", "1", "1"].join(",") + ")");
  sql("INSERT INTO \"order\".order_lines (line_id, seller_order_id, offer_id, catalog_variant_id, seller_party_id, quantity, unit_price_snapshot, line_total_snapshot, currency, tax_exclusive, price_id) VALUES (" +
    [quote(lineId), quote(sellerOrderId), quote(offerId), quote(variantId), quote(sellerId), "1", "1000", "1000", quote("IRR"), "false", quote(randomUUID())].join(",") + ")");
  ctx.state.orderFixtures.push({ checkoutId, sellerOrderId, lineIds: [lineId] });
  return { checkoutId, sellerOrderId, lineId };
}

async function cleanup(ctx) {
  const prefix = "P1C-";
  const offers = ctx.state.offers.length > 0;
  const ids = ctx.state.offers.map(x => x.offerId);
  if (ids.length) {
    const keys = ids.map(quote).join(",");
    const rows = sqlJson("SELECT coalesce(jsonb_agg(jsonb_build_object('id',offer_id,'seller',seller_party_id,'sku',seller_sku)),'[]'::jsonb) FROM offer.offers WHERE offer_id IN (" + keys + ")");
    for (const row of rows) {
      const known = ctx.state.offers.find(x => x.offerId === row.id);
      check(known && row.seller === known.sellerId && String(row.sku || "").startsWith(prefix), "Refusing cleanup: fixture identity changed for " + row.id);
    }
    const reservationKeys = scalar("SELECT coalesce(string_agg(quote_literal(r.reservation_id::text),','),'') FROM inventory.reservations r JOIN inventory.stock_positions p ON p.stock_item_id=r.stock_item_id WHERE p.offer_id IN (" + keys + ")");
    if (reservationKeys) {
      sql("DELETE FROM inventory.reservation_consumptions WHERE reservation_id IN (" + reservationKeys + ")");
      sql("DELETE FROM inventory.held_obligation_releases WHERE reservation_id IN (" + reservationKeys + ")");
      sql("DELETE FROM inventory.return_restock_inbox WHERE reservation_id IN (" + reservationKeys + ")");
    }
    const outbox = ids.map(x => "strpos(payload::text," + quote('"' + x + '"') + ")>0").join(" OR ");
    sql("BEGIN;" +
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
  }
  if (ctx.state.codes.length) {
    sql("DELETE FROM inventory.locations WHERE code IN (" + ctx.state.codes.map(quote).join(",") + ")");
  }
  for (const f of ctx.state.orderFixtures) {
    sql("BEGIN; DELETE FROM \"order\".order_lines WHERE line_id=" + quote(f.lineIds[0]) + "; DELETE FROM \"order\".seller_orders WHERE seller_order_id=" + quote(f.sellerOrderId) + "; DELETE FROM \"order\".checkouts WHERE checkout_id=" + quote(f.checkoutId) + "; COMMIT;");
  }
  restoreSellerB(ctx);
  if (ids.length) {
    const residual = sqlJson("SELECT coalesce(jsonb_agg(to_jsonb(t)),'[]'::jsonb) FROM inventory.locations t WHERE code IN (" + (ctx.state.codes.length ? ctx.state.codes.map(quote).join(",") : "NULL") + ")");
    check(residual.length === 0, "Residual harness locations");
  }
}

function harnessBase(ctx, edition) {
  return {
    edition, runId: ctx.id.toLowerCase().replace(/[^a-z0-9-]/g, "-") + "-" + Date.now().toString(36),
    adminActor: ctx.adminActor, httpBase: HOST,
  };
}

// ---------------------------------------------------------------- OWN-01
exports["OWN-01"] = () => callRun("OWN-01", async ctx => {
  const sellers = await resolveActors(ctx);
  await prepareSellerB(ctx);
  const used = [];
  const variants = [await chooseVariant(ctx, sellers.A.sellerPartyId, used)];
  used.push(variants[0]);
  const vB = await chooseVariant(ctx, sellers.B.sellerPartyId, used); used.push(vB);
  const a = await createOffer(ctx, sellers.A.sellerPartyId, variants[0]);
  const b = await createOffer(ctx, sellers.B.sellerPartyId, vB);
  await assign(ctx, a.offerId, "Seller");
  await assign(ctx, b.offerId, "Seller");

  // HTTP seller-scope proof while B has the real product.edit capability.
  const listA = await ctx.getJson("/v1/seller/offers", sellers.A.headers);
  check((listA || []).some(x => x.offerId === a.offerId), "seller A cannot list own offer");
  const crossRead = await ctx.api("/v1/seller/offers/" + b.offerId, "GET", undefined, sellers.A.headers);
  check([403, 404].includes(crossRead.status) && crossRead.body?.errorCode === "offer.not_found", "seller A cross read not a typed denial: " + crossRead.status);
  const crossEdit = await ctx.api("/v1/seller/offers/" + b.offerId, "PATCH", { status: "Draft", sellerSku: SKU_PREFIX + randomUUID(), expectedVersion: (await sellerDetailOf(ctx, b.offerId, sellers.B)).body.revisions.offer }, sellers.A.headers);
  check([403, 404].includes(crossEdit.status), "seller A cross edit not denied: " + crossEdit.status);
  const beforeForeignWrite = await detailOf(ctx, b.offerId);

  // Retired seller-owned location for A.
  const retiredLocationId = randomUUID();
  const retiredCode = "P1C-F-" + ctx.id + "-RET-" + Date.now().toString(36);
  sql("INSERT INTO inventory.locations (location_id, code, name, status, created_at, updated_at, seller_party_id, owner_kind) VALUES (" +
    [quote(retiredLocationId), quote(retiredCode.toUpperCase()), quote("P1C F retired"), quote("Retired"), "now()", "now()", quote(sellers.A.sellerPartyId), quote("Seller")].join(",") + ")");
  ctx.addCode(retiredCode.toUpperCase());

  const cfg = {
    ...harnessBase(ctx, "Marketplace"),
    retiredLocationId,
    offers: [
      { key: "A", offerId: a.offerId, sellerId: sellers.A.sellerPartyId, variantId: a.variantId, positions: [{ ownerKind: "Seller", sellerOwner: sellers.A.sellerPartyId, stock: 100, eligible: true }] },
      { key: "B", offerId: b.offerId, sellerId: sellers.B.sellerPartyId, variantId: b.variantId, positions: [{ ownerKind: "Seller", sellerOwner: sellers.B.sellerPartyId, stock: 100, eligible: true }] },
    ],
  };
  const harness = await runHarness(ctx, "ownership", cfg);
  const eligible = findStep(harness, "phase", "eligible").eligible;
  const aLocation = fixtureOf(harness, "A")[0];
  const bLocation = fixtureOf(harness, "B")[0];
  check(eligible.length === 1 && eligible[0].LocationId === aLocation.LocationId, "seller A eligible locations are not scoped to own IDs: " + JSON.stringify(eligible));
  check(findStep(harness, "phase", "own-provision").same === true, "own provision is not idempotent");
  check(findStep(harness, "label", "foreign-provision").ok === false, "foreign provision was not denied");
  check(findStep(harness, "label", "retired-provision").ok === false, "retired provision was not denied");
  check(findStep(harness, "label", "unknown-provision").ok === false, "unknown provision was not denied");
  check(findStep(harness, "phase", "own-write").ok === true, "own seller stock write did not persist");
  const afterOwn = findStep(harness, "label", "after own write");
  check(afterOwn.positions.length === 1 && Number(afterOwn.positions[0].OnHand) === 90, "own write did not persist exactly 90");
  check(findStep(harness, "phase", "foreign-write-a-by-b").ok === false && findStep(harness, "phase", "foreign-write-a-by-b").code === "offer.not_found", "seller B write to A was not a typed owner denial");
  check(findStep(harness, "phase", "foreign-write-b-by-a").ok === false && findStep(harness, "phase", "foreign-write-b-by-a").code === "offer.not_found", "seller A write to B was not a typed owner denial");
  const afterForeign = findStep(harness, "label", "after foreign write");
  check(Number(afterForeign.positions[0].OnHand) === 90, "foreign denial mutated A stock");
  const afterCross = findStep(harness, "label", "after cross write B");
  check(Number(afterCross.positions[0].OnHand) === 100, "cross denial mutated B stock");
  const bAfter = await detailOf(ctx, b.offerId);
  check(bAfter.sellerSku === beforeForeignWrite.sellerSku && bAfter.revisions.offer === beforeForeignWrite.revisions.offer, "cross edit mutated B offer");
  return "PASS";
});

// ---------------------------------------------------------------- OWN-02
exports["OWN-02"] = () => callRun("OWN-02", async ctx => {
  const sellers = await resolveActors(ctx);
  const used = [];
  const va = await chooseVariant(ctx, sellers.A.sellerPartyId, used); used.push(va);
  const vb = await chooseVariant(ctx, sellers.B.sellerPartyId, used);
  const a = await createOffer(ctx, sellers.A.sellerPartyId, va);
  const b = await createOffer(ctx, sellers.B.sellerPartyId, vb);
  await assign(ctx, a.offerId, "Platform");
  await assign(ctx, b.offerId, "Platform");
  const locations = await ctx.getJson("/v1/admin/offers/inventory-locations", ctx.adminHeaders);
  check(locations.length > 0, "no eligible Store location");
  const pa = await provision(ctx, a.offerId, locations[0].locationId);
  const pb = await provision(ctx, b.offerId, locations[1] ? locations[1].locationId : locations[0].locationId);
  const sa = await setStock(ctx, a.offerId, pa.stockItemId, 100, "P1C F platform receipt A");
  check(sa.res.status === 200, "admin receipt A " + sa.res.status);
  const sb = await setStock(ctx, b.offerId, pb.stockItemId, 50, "P1C F platform receipt B");
  check(sb.res.status === 200, "admin receipt B " + sb.res.status);

  const sellerA = await ctx.getJson("/v1/seller/offers/" + a.offerId, sellers.A.headers);
  const sellerRows = sellerA.inventoryLocations || [];
  check(sellerRows.length === 1 && sellerRows[0].stockItemId === pa.stockItemId && Number(sellerRows[0].onHand) === 100, "seller A does not see exactly own stock");
  const cross = await ctx.api("/v1/seller/offers/" + b.offerId, "GET", undefined, sellers.A.headers);
  check([403, 404].includes(cross.status), "seller A read B not denied: " + cross.status);

  const denied = await ctx.api("/v1/admin/offers/" + a.offerId + "/inventory", "PUT",
    { onHand: 90, reason: "P1C F seller physical attempt", stockItemId: pa.stockItemId, expectedVersion: (await detailOf(ctx, a.offerId)).inventoryLocations[0].revision, operationId: randomUUID() },
    sellers.A.headers);
  check(denied.status >= 400 && denied.status < 500, "seller physical write on platform offer was not denied: " + denied.status);
  const afterDenied = await detailOf(ctx, a.offerId);
  check(Number(afterDenied.inventoryLocations[0].onHand) === 100, "denied seller write mutated physical stock");
  const receipt = await setStock(ctx, a.offerId, pa.stockItemId, 105, "P1C F admin receipt +5");
  check(receipt.res.status === 200, "admin receipt +5 " + receipt.res.status);
  const after = await detailOf(ctx, a.offerId);
  check(Number(after.inventoryLocations[0].onHand) === 105, "admin receipt did not persist exactly");
  const state = ctx.observe("own/foreign/admin receipt");
  const posA = state.positions.find(x => x.stock_item_id === pa.stockItemId);
  const posB = state.positions.find(x => x.stock_item_id === pb.stockItemId);
  check(Number(posA.on_hand) === 105 && Number(posB.on_hand) === 50, "independent SQL quantities do not match observed receipts");
  return "PASS";
});

// ---------------------------------------------------------------- OWN-03
exports["OWN-03"] = () => callRun("OWN-03", async ctx => {
  const sellers = await resolveActors(ctx);
  const used = [];
  const v = await chooseVariant(ctx, sellers.A.sellerPartyId, used); used.push(v);
  const offer = await createOffer(ctx, sellers.A.sellerPartyId, v);
  const before = await detailOf(ctx, offer.offerId);
  const selfAssign = await ctx.api("/v1/admin/offers/" + offer.offerId + "/fulfillment", "PUT",
    { responsibility: "Platform", shippingTypeId: null, expectedVersion: before.revisions.offer }, sellers.A.headers);
  check([401, 403].includes(selfAssign.status), "seller self-assign platform not denied: " + selfAssign.status);
  const adminAssign = await assign(ctx, offer.offerId, "Platform");
  check(adminAssign.res.status === 200 && adminAssign.after.fulfillmentResponsibility === "Platform", "authorized admin platform assignment failed");
  const ownPolicy = await ctx.api("/v1/seller/offers/" + offer.offerId, "PATCH",
    { status: "Draft", returnPolicyChoice: "Custom", customReturnWindowDays: 5, expectedVersion: adminAssign.after.revisions.offer }, sellers.A.headers);
  check(ownPolicy.status === 200, "seller own policy edit failed: " + ownPolicy.status + " " + JSON.stringify(ownPolicy.body));
  const ownRead = await ctx.getJson("/v1/seller/offers/" + offer.offerId, sellers.A.headers);
  check(ownRead.returnPolicyChoice === "Custom" && ownRead.customReturnWindowDays === 5, "seller own policy did not persist");

  // SingleStore trusted context: Store location is the physical authority and marketplace
  // allocation/controls do not apply.
  const vs = await chooseVariant(ctx, sellers.A.sellerPartyId, used);
  const single = await createOffer(ctx, sellers.A.sellerPartyId, vs);
  await assign(ctx, single.offerId, "Platform");
  const cfg = {
    ...harnessBase(ctx, "SingleStore"),
    offers: [{ key: "A", offerId: single.offerId, sellerId: sellers.A.sellerPartyId, variantId: single.variantId, positions: [{ ownerKind: "Store", stock: 100, eligible: true }] }],
  };
  const harness = await runHarness(ctx, "single-edition", cfg);
  const availability = findStep(harness, "phase", "single-availability").availability;
  check(availability && availability.Locations.length === 1 && availability.Locations[0].LocationCode.startsWith("P1C-F-"), "SingleStore did not resolve its Store location");
  const denied = findStep(harness, "label", "single-allocation-denied").value;
  check(denied.ok === false && denied.code === "inventory.location.not_eligible", "SingleStore marketplace allocation was not denied");
  const state = ctx.observe("single-edition");
  check(state.allocations.length === 0, "SingleStore allocation attempt wrote an allocation row");
  return "PASS";
});

// ---------------------------------------------------------------- OWN-04
exports["OWN-04"] = () => callRun("OWN-04", async ctx => {
  const sellers = await resolveActors(ctx);
  const v = await chooseVariant(ctx, sellers.A.sellerPartyId);
  const offer = await createOffer(ctx, sellers.A.sellerPartyId, v);
  await assign(ctx, offer.offerId, "Platform");
  const harness = await runHarness(ctx, "limits", {
    ...harnessBase(ctx, "Marketplace"),
    offers: [{ key: "A", offerId: offer.offerId, sellerId: sellers.A.sellerPartyId, variantId: offer.variantId, positions: [{ ownerKind: "Marketplace", stock: 100, eligible: true }] }],
  });
  const initial = findStep(harness, "label", "after allocate20");
  check(initial.allocations.length === 1 && Number(initial.allocations[0].Quantity) === 20, "allocation 20 not persisted");
  check(Number(fixtureOf(harness, "A")[0].Stock) === 100 && Number(initial.positions[0].OnHand) === 100, "physical 100 not persisted");
  const hold21 = findStep(harness, "label", "hold21").value;
  check(hold21.ok === false && hold21.code === "inventory.supply.unavailable", "hold 21 was not denied");
  const afterHold = findStep(harness, "label", "after denied hold21");
  check(Number(afterHold.positions[0].OnHand) === 100 && Number(afterHold.positions[0].Reserved) === 0 && Number(afterHold.allocations[0].Held) === 0 && Number(afterHold.allocations[0].Quantity) === 20, "denied hold mutated physical/held/allocation");
  const alloc101 = findStep(harness, "label", "allocate101").value;
  check(alloc101.ok === false && alloc101.code === "inventory.allocation.exceeds_physical", "allocation > physical was not denied");
  const after101 = findStep(harness, "label", "after denied allocation101");
  check(Number(after101.allocations[0].Quantity) === 20 && after101.facts.length === 1, "denied allocation > physical mutated state/facts");
  const zero = findStep(harness, "label", "allocate0").value;
  check(zero.ok === true && Number(zero.receipt.Quantity) === 0, "zero allocation did not persist");
  const hold1 = findStep(harness, "label", "hold1-after-zero").value;
  check(hold1.ok === false && hold1.code === "inventory.supply.unavailable", "zero allocation did not stop new sale");
  const final = findStep(harness, "label", "after denied hold1");
  check(final.reservations.length === 0 && Number(final.allocations[0].Quantity) === 0 && Number(final.positions[0].OnHand) === 100, "final zero/no-sale state incorrect");
  return "PASS";
});

// ---------------------------------------------------------------- OWN-05
exports["OWN-05"] = () => callRun("OWN-05", async ctx => {
  const sellers = await resolveActors(ctx);
  const v = await chooseVariant(ctx, sellers.A.sellerPartyId);
  const offer = await createOffer(ctx, sellers.A.sellerPartyId, v);
  await assign(ctx, offer.offerId, "Platform");
  const harness = await runHarness(ctx, "lifecycle", {
    ...harnessBase(ctx, "Marketplace"),
    offers: [{ key: "A", offerId: offer.offerId, sellerId: sellers.A.sellerPartyId, variantId: offer.variantId, positions: [{ ownerKind: "Marketplace", stock: 100, eligible: true }] }],
  });
  const allocated = findStep(harness, "label", "after allocate20");
  check(Number(allocated.allocations[0].Quantity) === 20 && Number(allocated.positions[0].OnHand) === 100, "allocate20/physical100 wrong");
  const held = findStep(harness, "label", "after hold5");
  check(Number(held.allocations[0].Held) === 5 && Number(held.allocations[0].Quantity) - Number(held.allocations[0].Held) === 15, "hold 5 availability is not 15");
  check(held.reservations.filter(x => x.Status === "Held").length === 1, "hold 5 did not create exactly one Held reservation");
  const released = findStep(harness, "label", "after release");
  check(Number(released.allocations[0].Held) === 0 && Number(released.allocations[0].Quantity) - Number(released.allocations[0].Held) === 20, "release did not restore availability 20");
  check(Number(released.positions[0].Reserved) === 0, "release did not restore reserved 0");
  const consumed = findStep(harness, "label", "after consume");
  check(Number(consumed.positions[0].OnHand) === 95 && Number(consumed.allocations[0].Quantity) === 15 && Number(consumed.allocations[0].Held) === 0, "consume did not produce physical95/allocation15/held0");
  check(Number(consumed.allocations[0].Quantity) - Number(consumed.allocations[0].Held) === 15, "post-consume available is not 15");
  check(consumed.reservations.some(x => x.Status === "Consumed") && consumed.consumptions === 1, "consume journal missing");
  const replay = findStep(harness, "label", "after consume replay");
  check(Number(replay.positions[0].OnHand) === 95 && replay.consumptions === 1 && Number(replay.allocations[0].Quantity) === 15, "consume replay mutated state or double-consumed");
  const stale = findStep(harness, "label", "stale-allocation").value;
  check(stale.ok === false && stale.code === "inventory.allocation.revision_conflict", "stale allocation revision was not denied");
  const afterStale = findStep(harness, "label", "after stale allocation");
  check(Number(afterStale.allocations[0].Quantity) === 15 && afterStale.facts.length === 1, "stale allocation mutated state/facts");
  return "PASS";
});

// ---------------------------------------------------------------- OWN-06
exports["OWN-06"] = () => callRun("OWN-06", async ctx => {
  const sellers = await resolveActors(ctx);
  const v = await chooseVariant(ctx, sellers.A.sellerPartyId);
  const offer = await createOffer(ctx, sellers.A.sellerPartyId, v);
  await assign(ctx, offer.offerId, "Platform");
  const harness = await runHarness(ctx, "race-holds", {
    ...harnessBase(ctx, "Marketplace"),
    offers: [{ key: "A", offerId: offer.offerId, sellerId: sellers.A.sellerPartyId, variantId: offer.variantId, positions: [{ ownerKind: "Marketplace", stock: 100, eligible: true }, { ownerKind: "Marketplace", stock: 100, eligible: true }] }],
  });
  const steps = harness.steps || [];
  const concurrent = steps.find(s => Array.isArray(s));
  check(concurrent && concurrent.length === 2, "missing concurrent hold results");
  const winners = concurrent.filter(x => x.ok === true);
  check(winners.length === 1, "expected exactly one winning hold, got " + winners.length);
  const loser = concurrent.find(x => x.ok === false);
  check(loser && loser.code === "inventory.supply.unavailable", "losing hold was not a typed unavailable denial");
  const after = findStep(harness, "label", "after concurrent holds");
  check(Number(after.allocations[0].Held) === 15 && Number(after.allocations[0].Quantity) - Number(after.allocations[0].Held) === 5, "final held/available are not 15/5");
  check(after.reservations.filter(x => x.Status === "Held").length === 1, "loser left a reservation row");
  check(Number(after.positions[0].Reserved) + Number(after.positions[1].Reserved) === 15, "loser mutated position reserved");
  const reduce = findStep(harness, "label", "reduce-below-held").value;
  check(reduce.ok === false && reduce.code === "inventory.allocation.below_held", "reduce below held was not denied");
  const final = findStep(harness, "label", "after reduce");
  check(Number(final.allocations[0].Quantity) === 20 && Number(final.allocations[0].Held) === 15, "denied reduction mutated allocation");
  return "PASS";
});

// ---------------------------------------------------------------- OWN-07
exports["OWN-07"] = () => callRun("OWN-07", async ctx => {
  const sellers = await resolveActors(ctx);
  const v = await chooseVariant(ctx, sellers.A.sellerPartyId);
  const offer = await createOffer(ctx, sellers.A.sellerPartyId, v);
  await assign(ctx, offer.offerId, "Platform");
  const harness = await runHarness(ctx, "recovery", {
    ...harnessBase(ctx, "Marketplace"),
    offers: [{ key: "A", offerId: offer.offerId, sellerId: sellers.A.sellerPartyId, variantId: offer.variantId, positions: [{ ownerKind: "Marketplace", stock: 100, eligible: true }] }],
  });
  const paid = findStep(harness, "label", "after paid consume");
  check(Number(paid.positions[0].OnHand) === 95 && Number(paid.allocations[0].Quantity) === 15 && paid.consumptions === 1, "paid consume state incorrect");
  const restock = findStep(harness, "label", "after return restock");
  check(Number(restock.positions[0].OnHand) === 96 && Number(restock.allocations[0].Quantity) === 15, "return restock did not replenish exactly 1 without increasing allocation");
  const restockReplay = findStep(harness, "label", "after return restock replay");
  check(Number(restockReplay.positions[0].OnHand) === 96, "return restock replay double-increased stock");
  const replacementStep = stepsFind(harness, "phase", "replacement");
  check(replacementStep && replacementStep.Status === "Held", "damaged replacement hold missing");
  const afterReplacement = findStep(harness, "label", "after replacement");
  check(Number(afterReplacement.allocations[0].Quantity) === 15, "replacement silently increased new-sale allocation");
  check(Number(afterReplacement.positions[0].OnHand) === 96 && Number(afterReplacement.positions[0].Reserved) === 1, "replacement physical/reserved incorrect");
  check(afterReplacement.reservations.filter(x => x.Status === "Held" && x.Purpose === "DamagedReplacement").length === 1, "replacement protected commitment missing");
  const replReplay = stepsFind(harness, "phase", "replacement-replay");
  check(replReplay && replReplay.same === true, "replacement replay created a second hold");
  const expiredHold = stepsFind(harness, "phase", "expired-hold") || null;
  const expired = stepsFind(harness, "phase", "expired-release");
  const retry = stepsFind(harness, "phase", "retry");
  const afterRetry = findStep(harness, "label", "after retry");
  const expiredRow = afterRetry.reservations.find(x => x.Purpose === "NewSale" && x.Status === "Released");
  check(expired && expiredRow, "expired unpaid hold was not released");
  check(retry && ([1, "Reacquired", 0, "AlreadyReserved"].includes(retry.Outcome)), "unpaid retry did not reacquire: " + JSON.stringify(retry));
  const replayPaid = findStep(harness, "label", "after paid consume replay");
  check(replayPaid.consumptions === 1 && Number(replayPaid.positions[0].OnHand) === 96, "paid consume replay caused a second commercial debit");
  return "PASS";
});

function stepsFind(harness, key, value) {
  return (harness.steps || []).find(s => s && s[key] === value);
}

// ---------------------------------------------------------------- OWN-08
exports["OWN-08"] = () => callRun("OWN-08", async ctx => {
  const sellers = await resolveActors(ctx);
  const v = await chooseVariant(ctx, sellers.A.sellerPartyId);
  const offer = await createOffer(ctx, sellers.A.sellerPartyId, v);
  await assign(ctx, offer.offerId, "Platform");
  const before = await detailOf(ctx, offer.offerId);
  const missing = await ctx.api("/v1/admin/offers/" + offer.offerId + "/price", "PUT",
    { amount: 5000, currency: null, market: null, expectedVersion: "", operationId: randomUUID() }, ctx.adminHeaders);
  check(missing.status >= 400 && missing.status < 500, "missing price revision was not denied");
  const stale = await ctx.api("/v1/admin/offers/" + offer.offerId + "/price", "PUT",
    { amount: 5000, currency: null, market: null, expectedVersion: "stale", operationId: randomUUID() }, ctx.adminHeaders);
  check(stale.status >= 400 && stale.status < 500, "stale price revision was not denied");
  const noPrice = await detailOf(ctx, offer.offerId);
  check(noPrice.priceRevision === "absent", "denied price writes left a price fact");

  const priced = await ctx.api("/v1/admin/offers/" + offer.offerId + "/price", "PUT",
    { amount: 5000, currency: null, market: null, expectedVersion: noPrice.revisions.price, operationId: randomUUID() }, ctx.adminHeaders);
  check(priced.status === 200 && priced.body.amount === 5000, "authorized price write failed");
  const harness = await runHarness(ctx, "race-allocation", {
    ...harnessBase(ctx, "Marketplace"),
    offers: [{ key: "A", offerId: offer.offerId, sellerId: sellers.A.sellerPartyId, variantId: offer.variantId, positions: [{ ownerKind: "Marketplace", stock: 100, eligible: true }] }],
  });
  const firstRace = (harness.steps || []).find(s => Array.isArray(s));
  check(firstRace && firstRace.filter(x => x.ok === true).length === 1, "first allocation race did not produce exactly one winner");
  const afterFirst = findStep(harness, "label", "after first allocation race");
  check(afterFirst.facts.length === 1 && Number(afterFirst.allocations[0].Quantity) === 20, "first allocation race wrote loser facts or wrong quantity");
  const mutation = (harness.steps || []).filter(s => Array.isArray(s))[1];
  check(mutation && mutation.filter(x => x.ok === true).length === 1, "mutation race did not produce exactly one winner");
  const winner = mutation.find(x => x.ok === true);
  const winningQuantity = Number(winner.receipt.Quantity);
  const afterMutation = findStep(harness, "label", "after mutation race");
  check(afterMutation.facts.length === 2 && Number(afterMutation.allocations[0].Quantity) === winningQuantity, "winner payload/state mismatch or loser fact present");
  const missingAlloc = findStep(harness, "label", "missing-revision").value;
  check(missingAlloc.ok === false && missingAlloc.code === "inventory.allocation.revision_conflict", "missing allocation revision was not denied");
  const final = findStep(harness, "label", "after missing revision");
  check(Number(final.allocations[0].Quantity) === winningQuantity && final.facts.length === 2, "missing revision mutated allocation/facts");
  const pricedAfter = await detailOf(ctx, offer.offerId);
  check(pricedAfter.priceRevision !== "absent" && pricedAfter.revisions.price !== pricedAfter.revisions.offeredQuantity, "price and allocation revisions are not independent");
  return "PASS";
});

// ---------------------------------------------------------------- OWN-09
exports["OWN-09"] = () => callRun("OWN-09", async ctx => {
  const sellers = await resolveActors(ctx);
  const v = await chooseVariant(ctx, sellers.A.sellerPartyId);
  const offer = await createOffer(ctx, sellers.A.sellerPartyId, v);
  await assign(ctx, offer.offerId, "Platform");
  const harness = await runHarness(ctx, "fault", {
    ...harnessBase(ctx, "Marketplace"),
    offers: [{ key: "A", offerId: offer.offerId, sellerId: sellers.A.sellerPartyId, variantId: offer.variantId, positions: [{ ownerKind: "Marketplace", stock: 100, eligible: true }] }],
  });
  const preFault = findStep(harness, "label", "precommit-fault");
  check(preFault.ok === false && preFault.error?.includes("injected-before-owner-commit") && stepsFind(harness, "phase", "precommit-injection")?.hit === true, "real owner transaction commit fault was not reached");
  const afterPre = findStep(harness, "label", "after precommit");
  check(afterPre.allocations.length === 0 && afterPre.facts.length === 0, "precommit fault changed owner state/events");
  const postReal = stepsFind(harness, "phase", "postcommit-real");
  check(postReal && postReal.ok === true, "postcommit owner write did not commit");
  const postFault = findStep(harness, "label", "postcommit-projection-fault");
  check(postFault.ok === false && postFault.error?.includes("injected-owner-history-read") && stepsFind(harness, "phase", "projection-injection")?.hit === true, "real owner history query fault was not reached");
  const afterPost = findStep(harness, "label", "after postcommit fault");
  check(afterPost.allocations.length === 1 && afterPost.facts.length === 1, "durable owner facts were lost after projection fault");
  const replay = findStep(harness, "label", "replay").value;
  check(replay.IsSuccess === true, "replay did not report the committed owner result");
  const afterReplay = findStep(harness, "label", "after replay");
  check(afterReplay.allocations.length === 1 && afterReplay.facts.length === 1 && afterReplay.consumptions === 0, "replay duplicated owner facts or reported a false zero-write failure");
  check(JSON.stringify(afterPost.facts) === JSON.stringify(afterReplay.facts), "replay changed committed owner fact");
  const fact = afterReplay.facts[0];
  check(fact.OperationId === postReal.operationId && fact.ActorUserId === postReal.actor && fact.OldAllocation === null && Number(fact.NewAllocation) === 20, "owner fact actor/intent/delta mismatch");
  const history = await ctx.getJson("/v1/admin/offers/" + offer.offerId + "/history?page=1&pageSize=50", ctx.adminHeaders);
  ctx.trace.recoveredHistory = history;
  const rows = history.items.filter(row => row.kind === "AllocationChanged");
  check(rows.length === 1, "actual Offer history must project exactly one logical allocation event");
  const row = rows[0];
  check(row.eventId === fact.FactId && row.actorUserId === fact.ActorUserId && row.targetId === offer.offerId &&
    row.offerId === offer.offerId && row.occurredAt === fact.OccurredAt &&
    (row.oldValue ?? null) === null && Number(row.newValue) === 20 && (row.locationId ?? null) === null,
    "actual history event identity/actor/time/delta did not match committed owner fact");
  return "PASS";
});

// ---------------------------------------------------------------- OWN-10
exports["OWN-10"] = () => callRun("OWN-10", async ctx => {
  const sellers = await resolveActors(ctx);
  const languages = await ctx.getJson("/v1/languages");
  const rows = Array.isArray(languages) ? languages : [];
  const active = rows.filter(x => x.isActive === true || x.is_active === true);
  check(active.some(x => x.direction === "Rtl" || x.direction === "rtl"), "no RTL active language registry row");
  check(active.some(x => x.direction === "Ltr" || x.direction === "ltr"), "no LTR active language registry row");
  const dbLanguages = sqlJson("SELECT coalesce(jsonb_agg(jsonb_build_object('code',code,'prefix',url_prefix,'direction',direction,'calendar',calendar_display,'active',is_active,'default',is_default) ORDER BY sort_order),'[]'::jsonb) FROM localization.languages");
  ctx.trace.languages = { endpoint: rows, db: dbLanguages };
  const third = dbLanguages.find(x => x.active && x.code === "ru-RU") || dbLanguages.find(x => x.active && x.code !== "fa-IR" && x.code !== "en-US");
  check(third, "no configured distinct third locale");
  check(new Set(dbLanguages.map(x => x.prefix)).size === dbLanguages.length, "locale prefixes are not distinct");

  const v = await chooseVariant(ctx, sellers.A.sellerPartyId);
  const offer = await createOffer(ctx, sellers.A.sellerPartyId, v);
  await assign(ctx, offer.offerId, "Platform");
  const locations = await ctx.getJson("/v1/admin/offers/inventory-locations", ctx.adminHeaders);
  const p = await provision(ctx, offer.offerId, locations[0].locationId);
  await setStock(ctx, offer.offerId, p.stockItemId, 12, "P1C F geometry stock");

  check(PLAYWRIGHT, "No Playwright installation is available for the real browser oracle");
  const { chromium } = require(PLAYWRIGHT);
  const browser = await chromium.launch();
  try {
    const context = await browser.newContext({ locale: "fa-IR" });
    await context.addInitScript(actor => localStorage.setItem("tooba.adminActorUserId", actor), ctx.adminActor);
    const page = await context.newPage();
    const geometry = [];
    for (const width of [360, 768, 1440]) {
      await page.setViewportSize({ width, height: 900 });
      await page.goto(FRONT + "/fa/admin/offers/" + offer.offerId, { waitUntil: "load" });
      await page.waitForSelector("[data-testid=offer-workspace]", { timeout: 30000 });
      await page.waitForTimeout(500);
      const facts = await page.evaluate(() => {
        const root = document.querySelector("[data-testid=offer-workspace]");
        const tabs = Array.from(document.querySelectorAll("[data-testid^=offer-tab-button-]")).map(x => x.getAttribute("data-testid"));
        return {
          overflow: document.documentElement.scrollWidth - document.documentElement.clientWidth,
          dir: root?.getAttribute("dir"), lang: root?.getAttribute("lang"),
          tabs, title: document.querySelector("[data-testid=offer-workspace-title]")?.textContent?.trim() || "",
        };
      });
      geometry.push({ width, ...facts });
      check(facts.overflow <= 0, "admin workspace overflows at " + width + ": " + facts.overflow);
      check(facts.dir === "rtl" && String(facts.lang || "").startsWith("fa"), "admin fa shell direction/lang wrong at " + width + ": " + JSON.stringify(facts));
      check(facts.tabs.length === 7, "admin workspace does not expose the seven tabs at " + width + ": " + facts.tabs.join(","));
    }
    await page.setViewportSize({ width: 1440, height: 900 });
    await page.goto(FRONT + "/" + third.prefix + "/admin/offers/" + offer.offerId, { waitUntil: "load" });
    await page.waitForSelector("[data-testid=offer-workspace]", { timeout: 30000 });
    await page.waitForTimeout(500);
    const ru = await page.evaluate(() => {
      const root = document.querySelector("[data-testid=offer-workspace]");
      return { overflow: document.documentElement.scrollWidth - document.documentElement.clientWidth, dir: root?.getAttribute("dir"), lang: root?.getAttribute("lang") };
    });
    ctx.trace.thirdLocale = { third, ru };
    check(ru.overflow <= 0, "third-locale workspace overflows: " + ru.overflow);
    check(ru.dir === String(third.direction).toLowerCase() && String(ru.lang || "").startsWith(third.prefix), "third-locale shell direction/lang mismatch: " + JSON.stringify(ru));
    ctx.trace.geometry = geometry;

    // The authorized DEV Host is SingleStore: the marketplace-only "platform physical read-only /
    // allocation editable" clause has no observable counterpart here. Record the exact dependency
    // instead of inferring PASS or fabricating a UI state.
    const adminDetail = await detailOf(ctx, offer.offerId);
    ctx.trace.capabilities = adminDetail.capabilities;
    throw new NotRun("OWN-10 geometry, seven-tab, DB registry and third-locale shell observed; the Marketplace-only clause 'platform physical read-only, allocation editable' and the in-app locale switch with draft preservation cannot be observed on the authorized SingleStore DEV Host (admin capability has canEditPhysicalInventory=" + adminDetail.capabilities.canEditPhysicalInventory + ", canEditOfferedQuantity=" + adminDetail.capabilities.canEditOfferedQuantity + "). No cross-edition switch or invented control was used.");
  } finally { await browser.close(); }
});

// ---------------------------------------------------------------- D2-ORDER
exports["D2-ORDER"] = () => callRun("D2-ORDER", async ctx => {
  const sellers = await resolveActors(ctx);
  const used = [];
  const v1 = await chooseVariant(ctx, sellers.A.sellerPartyId, used); used.push(v1);
  const v2 = await chooseVariant(ctx, sellers.A.sellerPartyId, used); used.push(v2);
  const v3 = await chooseVariant(ctx, sellers.A.sellerPartyId, used);
  const paid = await createOffer(ctx, sellers.A.sellerPartyId, v1);
  const cancelled = await createOffer(ctx, sellers.A.sellerPartyId, v2);
  const free = await createOffer(ctx, sellers.A.sellerPartyId, v3);
  insertOrderFixture(ctx, { offerId: paid.offerId, sellerId: sellers.A.sellerPartyId, variantId: paid.variantId, status: "Paid" });
  insertOrderFixture(ctx, { offerId: cancelled.offerId, sellerId: sellers.A.sellerPartyId, variantId: cancelled.variantId, status: "Cancelled", cancelledFrom: "Paid" });

  for (const [label, offer] of [["paid", paid], ["restorable-cancelled", cancelled]]) {
    const before = await detailOf(ctx, offer.offerId);
    const historyBefore = await ctx.getJson("/v1/admin/offers/" + offer.offerId + "/history?page=1&pageSize=50", ctx.adminHeaders);
    const denied = await ctx.api("/v1/admin/offers/" + offer.offerId + "/fulfillment", "PUT",
      { responsibility: "Seller", shippingTypeId: null, expectedVersion: before.revisions.offer }, ctx.adminHeaders);
    check(denied.status === 409 && denied.body?.errorCode === "offer.fulfillment.change_protected", label + " order commitment was not protected: " + denied.status + " " + JSON.stringify(denied.body));
    const after = await detailOf(ctx, offer.offerId);
    check(after.fulfillmentResponsibility === before.fulfillmentResponsibility && after.revisions.offer === before.revisions.offer, label + " denial mutated offer");
    const historyAfter = await ctx.getJson("/v1/admin/offers/" + offer.offerId + "/history?page=1&pageSize=50", ctx.adminHeaders);
    check(Number(historyAfter.totalCount) === Number(historyBefore.totalCount), label + " denial wrote history");
  }

  // Safe no-commitment assignment/reassignment/clear.
  const transitions = ["Seller", "Platform", null, "Seller"];
  let detail = await detailOf(ctx, free.offerId);
  for (const responsibility of transitions) {
    const r = await ctx.api("/v1/admin/offers/" + free.offerId + "/fulfillment", "PUT",
      { responsibility, shippingTypeId: null, expectedVersion: detail.revisions.offer }, ctx.adminHeaders);
    check(r.status === 200, "free offer assignment " + responsibility + " failed: " + r.status + " " + JSON.stringify(r.body));
    detail = await detailOf(ctx, free.offerId);
    check((detail.fulfillmentResponsibility ?? null) === responsibility, "free offer responsibility not persisted: " + responsibility);
  }
  const history = await ctx.getJson("/v1/admin/offers/" + free.offerId + "/history?page=1&pageSize=50", ctx.adminHeaders);
  ctx.trace.freeHistory = history;
  check(Number(history.totalCount) >= transitions.length, "assignment history missing exact events");
  const state = ctx.observe("order commitments");
  check(state.offers.find(x => x.offer_id === paid.offerId).fulfillment_responsibility === null, "paid offer responsibility changed");
  check(state.offers.find(x => x.offer_id === cancelled.offerId).fulfillment_responsibility === null, "cancelled offer responsibility changed");
  return "PASS";
});

// ---------------------------------------------------------------- D2-HOLD
exports["D2-HOLD"] = () => callRun("D2-HOLD", async ctx => {
  const sellers = await resolveActors(ctx);
  const used = [];
  const va = await chooseVariant(ctx, sellers.A.sellerPartyId, used); used.push(va);
  const vb = await chooseVariant(ctx, sellers.A.sellerPartyId, used);
  const a = await createOffer(ctx, sellers.A.sellerPartyId, va);
  const b = await createOffer(ctx, sellers.A.sellerPartyId, vb);
  await assign(ctx, a.offerId, "Platform");
  await assign(ctx, b.offerId, "Platform");
  const harness = await runHarness(ctx, "race-hold-assignment", {
    ...harnessBase(ctx, "Marketplace"),
    offers: [
      { key: "A", offerId: a.offerId, sellerId: sellers.A.sellerPartyId, variantId: a.variantId, positions: [{ ownerKind: "Marketplace", stock: 100, eligible: true }] },
      { key: "B", offerId: b.offerId, sellerId: sellers.A.sellerPartyId, variantId: b.variantId, positions: [{ ownerKind: "Marketplace", stock: 100, eligible: true }] },
    ],
  });
  const concurrent = stepsFind(harness, "phase", "concurrent");
  check(concurrent, "missing concurrent hold/assignment result");
  const holdOk = concurrent.hold && concurrent.hold.ok === true;
  const assignOk = concurrent.assignment && concurrent.assignment.status === 200;
  check(!(holdOk && assignOk), "hold and responsibility change both committed (unsafe commitment)");
  check(holdOk || assignOk, "neither concurrent hold nor assignment committed");
  if (holdOk) check(concurrent.assignment.status === 409 && String(concurrent.assignment.body).includes("change_protected"), "hold winner did not deny assignment");
  if (assignOk) check(concurrent.hold.ok === false, "assignment winner did not fail the Marketplace hold");
  const afterConcurrent = findStep(harness, "label", "after concurrent hold/assignment");
  const heldRows = afterConcurrent.reservations.filter(x => x.Status === "Held");
  if (heldRows.length) check(afterConcurrent.offers.find(x => x.offer_id === a.offerId).fulfillment_responsibility === "Platform", "stale owner: held reservation survived a responsibility change");
  const reusedAcceptance = stepsFind(harness, "phase", "reused-hold-acceptance");
  check(reusedAcceptance && ([0, "AlreadyReserved"].includes(reusedAcceptance.Outcome)), "checkout reused-hold acceptance did not reuse the exact hold");
  check(reusedAcceptance && (Number(reusedAcceptance.Status) === 0 || reusedAcceptance.Status === "Reserved"), "reused hold acceptance did not resolve as Reserved");
  const reusedAssign = stepsFind(harness, "phase", "reused-hold-assignment");
  check(reusedAssign.status === 409 && String(reusedAssign.body).includes("change_protected"), "assignment over a reused hold was not denied");
  const afterReused = findStep(harness, "label", "after reused hold assignment");
  check(afterReused.reservations.filter(x => x.Status === "Held").length === 1, "reused hold was released or duplicated");
  check(afterReused.offers.find(x => x.offer_id === b.offerId).fulfillment_responsibility === "Platform", "reused-hold assignment changed owner");
  return "PASS";
});

// ---------------------------------------------------------------- D2-ALLOCATION
exports["D2-ALLOCATION"] = () => callRun("D2-ALLOCATION", async ctx => {
  const sellers = await resolveActors(ctx);
  const used = [];
  const v1 = await chooseVariant(ctx, sellers.A.sellerPartyId, used); used.push(v1);
  const v2 = await chooseVariant(ctx, sellers.A.sellerPartyId, used); used.push(v2);
  const v3 = await chooseVariant(ctx, sellers.A.sellerPartyId, used);
  const raceOffer = await createOffer(ctx, sellers.A.sellerPartyId, v1);
  const firstOffer = await createOffer(ctx, sellers.A.sellerPartyId, v2);
  const staleOffer = await createOffer(ctx, sellers.A.sellerPartyId, v3);
  for (const o of [raceOffer, firstOffer, staleOffer]) await assign(ctx, o.offerId, "Platform");

  const race = await runHarness(ctx, "race-allocation-assignment", {
    ...harnessBase(ctx, "Marketplace"),
    offers: [{ key: "A", offerId: raceOffer.offerId, sellerId: sellers.A.sellerPartyId, variantId: raceOffer.variantId, positions: [{ ownerKind: "Marketplace", stock: 100, eligible: true }] }],
  });
  const concurrent = stepsFind(race, "phase", "concurrent");
  const allocOk = concurrent.allocation && concurrent.allocation.ok === true;
  const assignOk = concurrent.assignment && concurrent.assignment.status === 200;
  check(!(allocOk && assignOk), "allocation and assignment both committed");
  check(allocOk || assignOk, "neither allocation nor assignment committed");
  const after = findStep(race, "label", "after concurrent allocation/assignment");
  if (allocOk) check(after.facts.filter(x => x.Kind === "AllocationChanged").length === 1 && after.offers.find(x => x.offer_id === raceOffer.offerId).fulfillment_responsibility === "Platform", "allocation winner state/fact mismatch");
  if (assignOk) check(after.allocations.length === 0 && after.facts.filter(x => x.Kind === "AllocationChanged").length === 0, "assignment winner left loser allocation facts");

  const first = await runHarness(ctx, "race-allocation", {
    ...harnessBase(ctx, "Marketplace"),
    offers: [{ key: "A", offerId: firstOffer.offerId, sellerId: sellers.A.sellerPartyId, variantId: firstOffer.variantId, positions: [{ ownerKind: "Marketplace", stock: 100, eligible: true }] }],
  });
  const firstRace = (first.steps || []).filter(s => Array.isArray(s));
  check(firstRace[0] && firstRace[0].filter(x => x.ok === true).length === 1, "first-allocation concurrency did not have exactly one winner");
  const afterFirst = findStep(first, "label", "after first allocation race");
  check(afterFirst.facts.length === 1 && Number(afterFirst.allocations[0].Quantity) === 20, "first-allocation loser facts present");
  const mutation = firstRace[1];
  check(mutation && mutation.filter(x => x.ok === true).length === 1, "allocation mutation race did not have exactly one winner");
  const winnerQuantity = Number(mutation.find(x => x.ok === true).receipt.Quantity);
  const afterMutation = findStep(first, "label", "after mutation race");
  check(Number(afterMutation.allocations[0].Quantity) === winnerQuantity && afterMutation.facts.length === 2, "allocation winner payload/fact mismatch");
  const missing = findStep(first, "label", "missing-revision").value;
  check(missing.ok === false && missing.code === "inventory.allocation.revision_conflict", "missing allocation revision was not denied");

  const stale = await runHarness(ctx, "stale-owner-physical", {
    ...harnessBase(ctx, "Marketplace"),
    offers: [{ key: "A", offerId: staleOffer.offerId, sellerId: sellers.A.sellerPartyId, variantId: staleOffer.variantId, positions: [{ ownerKind: "Marketplace", stock: 100, eligible: true }] }],
  });
  const assignment = stepsFind(stale, "phase", "assignment");
  check(assignment && assignment.status === 200, "owner change for stale write did not persist");
  const staleWrite = stepsFind(stale, "phase", "stale-owner-physical");
  check(staleWrite && staleWrite.ok === false && staleWrite.code === "inventory.location.not_eligible", "stale-owner physical write was not denied");
  const afterStale = findStep(stale, "label", "after stale physical");
  check(Number(afterStale.positions[0].OnHand) === 100 && afterStale.facts.length === 0, "stale-owner denial mutated physical/facts");
  return "PASS";
});

// ---------------------------------------------------------------- D2-PARTIAL
exports["D2-PARTIAL"] = () => callRun("D2-PARTIAL", async ctx => {
  const sellers = await resolveActors(ctx);
  const used = [];
  const v1 = await chooseVariant(ctx, sellers.A.sellerPartyId, used); used.push(v1);
  const v2 = await chooseVariant(ctx, sellers.A.sellerPartyId, used);
  const raceOffer = await createOffer(ctx, sellers.A.sellerPartyId, v1);
  const partialOffer = await createOffer(ctx, sellers.A.sellerPartyId, v2);
  await assign(ctx, raceOffer.offerId, "Platform");
  await assign(ctx, partialOffer.offerId, "Platform");
  const race = await runHarness(ctx, "race-release-consume", {
    ...harnessBase(ctx, "Marketplace"),
    offers: [{ key: "A", offerId: raceOffer.offerId, sellerId: sellers.A.sellerPartyId, variantId: raceOffer.variantId, positions: [{ ownerKind: "Marketplace", stock: 100, eligible: true }] }],
  });
  const concurrent = (race.steps || []).find(s => Array.isArray(s));
  check(concurrent && concurrent.length === 2, "missing concurrent release/consume");
  check(concurrent.filter(x => x.ok === true).length === 1, "release and consume both committed");
  const afterRace = findStep(race, "label", "after concurrent release/consume");
  check(Number(afterRace.positions[0].Reserved) >= 0 && Number(afterRace.allocations[0].Held) >= 0, "concurrent release/consume produced negative availability");
  const consumed = concurrent.find(x => x.op === "consume" && x.ok === true);
  if (consumed) check(Number(afterRace.positions[0].OnHand) === 95 && Number(afterRace.positions[0].Reserved) === 0 && Number(afterRace.allocations[0].Held) === 0, "consume winner state incorrect");
  else check(Number(afterRace.positions[0].OnHand) === 100 && Number(afterRace.positions[0].Reserved) === 0 && Number(afterRace.allocations[0].Held) === 0, "release winner state incorrect");

  const partial = await runHarness(ctx, "partial-release", {
    ...harnessBase(ctx, "Marketplace"),
    offers: [{ key: "A", offerId: partialOffer.offerId, sellerId: sellers.A.sellerPartyId, variantId: partialOffer.variantId, positions: [{ ownerKind: "Marketplace", stock: 100, eligible: true }] }],
  });
  const r2 = stepsFind(partial, "phase", "release2");
  check(r2 && Number(r2.ReleasedQuantity) === 2 && r2.Outcome === "PartiallyHeld", "partial release 2 did not report PartiallyHeld");
  const after2 = findStep(partial, "label", "after release2");
  check(Number(after2.positions[0].Reserved) === 3 && Number(after2.allocations[0].Held) === 3 && Number(after2.allocations[0].Quantity) - Number(after2.allocations[0].Held) === 17, "partial release remaining quantity incorrect: reserved=" + after2.positions[0].Reserved + " held=" + after2.allocations[0].Held);
  const r3 = stepsFind(partial, "phase", "release3");
  check(r3 && Number(r3.ReleasedQuantity) === 3 && r3.Outcome === "Released", "final partial release did not report Released");
  const after3 = findStep(partial, "label", "after release3");
  check(Number(after3.positions[0].Reserved) === 0 && Number(after3.allocations[0].Held) === 0 && Number(after3.allocations[0].Quantity) - Number(after3.allocations[0].Held) === 20, "final release left wrong remaining");
  const replay = stepsFind(partial, "phase", "replay");
  check(replay && replay.Status === "Replayed" && Number(replay.ReleasedQuantity) === 3, "partial release replay did not return the recorded answer");
  const afterReplay = findStep(partial, "label", "after replay");
  check(Number(afterReplay.positions[0].Reserved) === 0 && Number(afterReplay.allocations[0].Held) === 0, "partial release replay double-released");
  return "PASS";
});

// ---------------------------------------------------------------- D2-RECOVERY
exports["D2-RECOVERY"] = () => callRun("D2-RECOVERY", async ctx => {
  const sellers = await resolveActors(ctx);
  const v = await chooseVariant(ctx, sellers.A.sellerPartyId);
  const offer = await createOffer(ctx, sellers.A.sellerPartyId, v);
  await assign(ctx, offer.offerId, "Platform");
  const harness = await runHarness(ctx, "recovery", {
    ...harnessBase(ctx, "Marketplace"),
    offers: [{ key: "A", offerId: offer.offerId, sellerId: sellers.A.sellerPartyId, variantId: offer.variantId, positions: [{ ownerKind: "Marketplace", stock: 100, eligible: true }] }],
  });
  const paid = findStep(harness, "label", "after paid consume");
  check(Number(paid.positions[0].OnHand) === 95 && paid.consumptions === 1 && Number(paid.allocations[0].Quantity) === 15, "paid order recovery base state incorrect");
  const restock = findStep(harness, "label", "after return restock");
  check(Number(restock.positions[0].OnHand) === 96 && Number(restock.allocations[0].Quantity) === 15, "return/replenishment changed allocation");
  const replacement = stepsFind(harness, "phase", "replacement");
  check(replacement && replacement.Status === "Held", "replacement reservation missing");
  const afterReplacement = findStep(harness, "label", "after replacement");
  check(Number(afterReplacement.allocations[0].Quantity) === 15, "replacement silently increased new-sale allocation");
  check(Number(afterReplacement.positions[0].OnHand) === 96 && Number(afterReplacement.positions[0].Reserved) === 1, "replacement commitment incorrect");
  const replay = stepsFind(harness, "phase", "replacement-replay");
  check(replay && replay.same === true, "replacement replay created a second commitment");
  const expired = stepsFind(harness, "phase", "expired-release");
  const retry = stepsFind(harness, "phase", "retry");
  const afterRetry = findStep(harness, "label", "after retry");
  check(expired && afterRetry.reservations.some(x => x.Purpose === "NewSale" && x.Status === "Released"), "unpaid retry hold expiry did not release");
  check(retry && ([1, "Reacquired", 0, "AlreadyReserved"].includes(retry.Outcome)), "unpaid retry capability did not reacquire");
  const paidReplay = findStep(harness, "label", "after paid consume replay");
  check(paidReplay.consumptions === 1 && Number(paidReplay.positions[0].OnHand) === 96, "paid obligation replay caused a second commercial debit");
  return "PASS";
});

// ---------------------------------------------------------------- D2-HTTP
exports["D2-HTTP"] = () => callRun("D2-HTTP", async ctx => {
  const sellers = await resolveActors(ctx);
  const v = await chooseVariant(ctx, sellers.A.sellerPartyId);
  const offer = await createOffer(ctx, sellers.A.sellerPartyId, v);
  await assign(ctx, offer.offerId, "Platform");
  const locations = await ctx.getJson("/v1/admin/offers/inventory-locations", ctx.adminHeaders);
  const p = await provision(ctx, offer.offerId, locations[0].locationId);
  const detail0 = await detailOf(ctx, offer.offerId);
  const row0 = detail0.inventoryLocations.find(x => x.stockItemId === p.stockItemId);
  check(row0 && row0.revision, "provisioned stock revision missing");
  const replayBody = { onHand: 100, reason: "P1C F d2 http 100", stockItemId: p.stockItemId, expectedVersion: row0.revision, operationId: randomUUID() };
  const write1 = await ctx.api("/v1/admin/offers/" + offer.offerId + "/inventory", "PUT", replayBody, ctx.adminHeaders);
  check(write1.status === 200 && write1.body.onHand === 100, "explicit physical write failed");
  const write1Replay = await ctx.api("/v1/admin/offers/" + offer.offerId + "/inventory", "PUT", replayBody, ctx.adminHeaders);
  check(write1Replay.status === 200 && write1Replay.body.onHand === 100, "operation replay after no mutation is not truthful");
  const detail2 = await detailOf(ctx, offer.offerId);
  const row2 = detail2.inventoryLocations.find(x => x.stockItemId === p.stockItemId);
  const write2 = await ctx.api("/v1/admin/offers/" + offer.offerId + "/inventory", "PUT",
    { onHand: 110, reason: "P1C F d2 http 110", stockItemId: p.stockItemId, expectedVersion: row2.revision, operationId: randomUUID() }, ctx.adminHeaders);
  check(write2.status === 200 && write2.body.onHand === 110, "subsequent physical mutation failed");
  const lateReplay = await ctx.api("/v1/admin/offers/" + offer.offerId + "/inventory", "PUT", replayBody, ctx.adminHeaders);
  check(lateReplay.status === 200, "replay after subsequent mutation was not reconciled");
  const afterLate = await detailOf(ctx, offer.offerId);
  check(Number(afterLate.inventoryLocations.find(x => x.stockItemId === p.stockItemId).onHand) === 110, "late replay reverted committed physical state");
  const lateReplayWrong = await ctx.api("/v1/admin/offers/" + offer.offerId + "/inventory", "PUT",
    { ...replayBody, onHand: 99 }, ctx.adminHeaders);
  check(lateReplayWrong.status >= 400 && lateReplayWrong.status < 500, "replay of changed intent was not denied");
  const history = await ctx.getJson("/v1/admin/offers/" + offer.offerId + "/history?page=1&pageSize=50", ctx.adminHeaders);
  ctx.trace.history = history;
  const facts = sqlJson("SELECT coalesce(jsonb_agg(jsonb_build_object('kind',kind,'stock',stock_item_id,'location',location_id,'actor',actor_user_id,'operation',operation_id,'old',old_on_hand,'new',new_on_hand) ORDER BY occurred_at),'[]'::jsonb) FROM inventory.offer_inventory_changes WHERE offer_id=" + quote(offer.offerId));
  ctx.trace.inventoryFacts = facts;
  check(facts.length === 2, "replay wrote duplicate owner facts");
  check(facts.every(x => x.stock === p.stockItemId && x.location === locations[0].locationId), "owner fact target/location not exact");
  const sellerDetail = await sellerDetailOf(ctx, offer.offerId, sellers.A);
  check(sellerDetail.status === 200 && sellerDetail.body.capabilities.canEditPhysicalInventory === false, "seller platform physical capability was not read-only");
  const currentRow = afterLate.inventoryLocations.find(x => x.stockItemId === p.stockItemId);
  const sellerWrite = await ctx.api("/v1/seller/offers/" + offer.offerId + "/inventory", "PUT",
    { onHand: 90, reason: "P1C F seller d2 http", stockItemId: p.stockItemId, expectedVersion: currentRow.revision, operationId: randomUUID() }, sellers.A.headers);
  check(sellerWrite.status >= 400 && sellerWrite.status < 500, "seller platform inventory write was not denied");
  const finalDetail = await detailOf(ctx, offer.offerId);
  check(Number(finalDetail.inventoryLocations.find(x => x.stockItemId === p.stockItemId).onHand) === 110, "seller denial mutated owner state");
  return "PASS";
});

// ---------------------------------------------------------------- F-BENTRY-01
exports["F-BENTRY-01"] = () => callRun("F-BENTRY-01", async ctx => {
  const sellers = await resolveActors(ctx);
  const used = [];
  const va = await chooseVariant(ctx, sellers.A.sellerPartyId, used); used.push(va);
  const vu = await chooseVariant(ctx, sellers.A.sellerPartyId, used);
  const a = await createOffer(ctx, sellers.A.sellerPartyId, va);
  const u = await createOffer(ctx, sellers.A.sellerPartyId, vu);
  await assign(ctx, a.offerId, "Seller");
  // U deliberately has no responsibility assignment.
  const cfg = {
    ...harnessBase(ctx, "Marketplace"),
    offers: [
      { key: "A", offerId: a.offerId, sellerId: sellers.A.sellerPartyId, variantId: a.variantId, positions: [
        { ownerKind: "Seller", sellerOwner: sellers.A.sellerPartyId, stock: 100, eligible: true },
        { ownerKind: "Store", stock: 40, eligible: false },
        { ownerKind: "Seller", sellerOwner: sellers.B.sellerPartyId, stock: 30, eligible: false },
      ] },
      { key: "U", offerId: u.offerId, sellerId: sellers.A.sellerPartyId, variantId: u.variantId, positions: [{ ownerKind: "Store", stock: 25, eligible: false }] },
    ],
  };
  const harness = await runHarness(ctx, "availability", cfg);
  const availability = harness.availability;
  check(availability && Array.isArray(availability.Locations), "availability missing");
  check(availability.Locations.length === 1, "unauthorized location rows were disclosed: " + availability.Locations.length);
  const own = availability.Locations[0];
  check(own.LocationCode.startsWith("P1C-F-") && own.LocationCode.includes("1"), "eligible own location not selected");
  check(Number(availability.OnHand) === 100 && Number(availability.Available) === 100, "availability totals include ineligible physical stock");
  check(Number(own.OnHand) === 100 && Number(own.Reserved) === 0, "eligible row physical/held incorrect");
  const codes = availability.Locations.map(x => x.LocationCode);
  check(!codes.some(c => c.endsWith("-2") || c.endsWith("-3")), "ineligible location label leaked");
  const unassigned = harness.unassignedAvailability;
  check(unassigned && unassigned.Locations.length === 0 && Number(unassigned.OnHand) === 0, "unassigned Marketplace rows were disclosed");
  return "PASS";
});

// ---------------------------------------------------------------- F-BENTRY-02
exports["F-BENTRY-02"] = () => callRun("F-BENTRY-02", async ctx => {
  const sellers = await resolveActors(ctx);
  const used = [];
  const va = await chooseVariant(ctx, sellers.A.sellerPartyId, used); used.push(va);
  const vb = await chooseVariant(ctx, sellers.A.sellerPartyId, used);
  const a = await createOffer(ctx, sellers.A.sellerPartyId, va);
  const b = await createOffer(ctx, sellers.A.sellerPartyId, vb);
  await assign(ctx, a.offerId, "Platform");
  await assign(ctx, b.offerId, "Platform");
  const harness = await runHarness(ctx, "checkout-reuse", {
    ...harnessBase(ctx, "Marketplace"),
    offers: [
      { key: "A", offerId: a.offerId, sellerId: sellers.A.sellerPartyId, variantId: a.variantId, positions: [{ ownerKind: "Marketplace", stock: 100, eligible: true }] },
      { key: "B", offerId: b.offerId, sellerId: sellers.A.sellerPartyId, variantId: b.variantId, positions: [{ ownerKind: "Marketplace", stock: 100, eligible: true }] },
    ],
  });
  const wrong = findStep(harness, "label", "wrong-offer-reuse");
  check(wrong.ok === false && wrong.code === "inventory.reservation.conflict", "wrong-offer reused hold was not rejected");
  const duplicate = findStep(harness, "label", "duplicate-reuse");
  check(duplicate.ok === false && duplicate.code === "inventory.reservation.conflict", "duplicate reused hold was not rejected");
  const before = findStep(harness, "label", "before wrong offer");
  const afterWrong = findStep(harness, "label", "after wrong offer");
  const afterDuplicate = findStep(harness, "label", "after duplicate");
  check(JSON.stringify(before.reservations) === JSON.stringify(afterWrong.reservations), "wrong-offer rejection released or added reservations");
  check(JSON.stringify(before.reservations) === JSON.stringify(afterDuplicate.reservations), "duplicate rejection released or added reservations");
  check(JSON.stringify(before.positions) === JSON.stringify(afterWrong.positions) && JSON.stringify(before.positions) === JSON.stringify(afterDuplicate.positions), "rejection mutated stock positions");
  const valid = stepsFind(harness, "phase", "valid-reuse");
  check(valid && valid.same === true, "valid single reuse did not return the exact hold");
  const afterValid = findStep(harness, "label", "after valid reuse");
  check(JSON.stringify(before.reservations) === JSON.stringify(afterValid.reservations), "valid reuse created a new reservation");
  return "PASS";
});








