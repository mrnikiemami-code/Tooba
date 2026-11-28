// TB-296 P1-FINAL-ACCEPTANCE — G1 R10-01-B executable oracle.
// Two independently authorized seller contexts on the SAME approved DEV Host (5088) and SAME
// database (tooba_alpha): store-alpha (SalesChannel=Marketplace) and a test-only store-beta
// tenant (SalesChannel=Direct) bound to the same tenant-alpha connection. The store-beta tenant
// is a reversible DEV configuration change; the Host/config is restored after the run.
// Verification only: no product, guard, contract or immutable-matrix edit.

const { randomUUID } = require("node:crypto");
const fs = require("node:fs");
const path = require("node:path");
const S = require("./p1c-support.cjs");

const { assert, uuid, deterministicId, psql, preflight, snapshot, SKU_PREFIX, EMPTY } = S;

const ALPHA = process.env.FA_ALPHA || "http://127.0.0.1:5088";
const BETA = process.env.FA_BETA || "http://beta.localtest.me:5088";
const ADMIN_HEADER = "X-Tooba-Dev-Actor-User-Id";
const SELLER_HEADER = "X-Tooba-Seller-Party-Id";
const SELLER = uuid(process.env.FA_SELLER_ID || "01a030d1-40cb-7000-8abe-6d31739956c5");
const ACTOR = uuid(process.env.FA_SELLER_ACTOR || "01a03628-3f68-7000-844d-99f1cadb54b0");
const HEADERS = { [ADMIN_HEADER]: ACTOR, [SELLER_HEADER]: SELLER };

async function call(base, route, method = "GET", payload, headers = {}) {
  const res = await fetch(base + route, {
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

const quote = x => "'" + String(x).replaceAll("'", "''") + "'";

function cleanupExact(ids, seller) {
  if (!ids.length) return { beforeCleanup: null, afterCleanup: null };
  const before = snapshot(ids);
  assert.ok(before.offers.every(r => ids.includes(r.offer_id) && r.seller_party_id === seller && String(r.seller_sku || "").startsWith(SKU_PREFIX)),
    "Unexpected fixture identity; refusing to delete any row");
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

async function main() {
  const root = process.env.P1C_EVIDENCE_DIR;
  assert.ok(root, "Set P1C_EVIDENCE_DIR to a new run-owned directory");
  fs.mkdirSync(root, { recursive: true });
  const evidence = path.join(root, "R10-01-B.json");
  assert.ok(!fs.existsSync(evidence), "Do not overwrite historical evidence");
  const trace = { id: "R10-01-B", requests: [], sql: [], status: "NOT_RUN" };
  const save = () => fs.writeFileSync(evidence, JSON.stringify(trace, null, 2));
  const offers = [];
  async function api(base, route, method = "GET", payload, headers = {}) {
    const req = { base, route, method, payload };
    trace.requests.push(req); save();
    const res = await call(base, route, method, payload, headers);
    Object.assign(req, { status: res.status, body: res.body }); save();
    return req;
  }
  let seller = SELLER;
  try {
    trace.preflight = preflight(); save();
    trace.actor = { seller, actor: ACTOR };

    const alphaChannels = await api(ALPHA, "/v1/seller/offers/allowed-channels", "GET", undefined, HEADERS);
    const betaChannels = await api(BETA, "/v1/seller/offers/allowed-channels", "GET", undefined, HEADERS);
    assert.equal(alphaChannels.status, 200, "alpha allowed-channels " + alphaChannels.status + " " + JSON.stringify(alphaChannels.body));
    assert.equal(betaChannels.status, 200, "beta allowed-channels " + betaChannels.status + " " + JSON.stringify(betaChannels.body));
    assert.deepEqual(alphaChannels.body, ["Marketplace"], "alpha context is not the Marketplace store");
    assert.deepEqual(betaChannels.body, ["Direct"], "beta context is not the Direct store");
    trace.channelAuthority = { alpha: alphaChannels.body, beta: betaChannels.body };

    const variant = psql("SELECT v.variant_id FROM catalog.variants v JOIN catalog.products p ON p.product_id=v.product_id " +
      "WHERE v.status='Published' AND p.quantity_decimal_places=0 " +
      "AND NOT EXISTS (SELECT 1 FROM offer.offers o WHERE o.seller_party_id=" + quote(seller) + " AND o.catalog_variant_id=v.variant_id) " +
      "ORDER BY v.variant_id LIMIT 1").trim();
    assert.match(variant, /^[0-9a-f-]{36}$/i, "no isolated published variant available");
    trace.variant = variant;

    const r1 = "P1C-" + randomUUID();
    const o1 = deterministicId(seller, variant, r1);
    const r2 = "P1C-" + randomUUID();
    const o2 = deterministicId(seller, variant, r2);
    assert.notEqual(o1, o2);

    const createdAlpha = await api(ALPHA, "/v1/seller/offers", "POST", {
      catalogVariantId: variant, sellerSku: SKU_PREFIX + randomUUID(), status: "Draft", requestId: r1, channel: "Marketplace",
    }, HEADERS);
    assert.equal(createdAlpha.status, 201, "alpha create failed: " + createdAlpha.status + " " + JSON.stringify(createdAlpha.body));
    assert.equal(uuid(createdAlpha.body.offerId), o1, "alpha create identity mismatch");
    offers.push(o1);

    const createdBeta = await api(BETA, "/v1/seller/offers", "POST", {
      catalogVariantId: variant, sellerSku: SKU_PREFIX + randomUUID(), status: "Draft", requestId: r2, channel: "Direct",
    }, HEADERS);
    assert.equal(createdBeta.status, 201, "beta create failed: " + createdBeta.status + " " + JSON.stringify(createdBeta.body));
    assert.equal(uuid(createdBeta.body.offerId), o2, "beta create identity mismatch");
    offers.push(o2);

    const gotAlpha = await api(ALPHA, "/v1/seller/offers/" + o1, "GET", undefined, HEADERS);
    const gotBeta = await api(BETA, "/v1/seller/offers/" + o2, "GET", undefined, HEADERS);
    assert.equal(gotAlpha.status, 200, "alpha get " + gotAlpha.status);
    assert.equal(gotBeta.status, 200, "beta get " + gotBeta.status);
    assert.equal(gotAlpha.body.channel, "Marketplace", "alpha offer channel wrong");
    assert.equal(gotBeta.body.channel, "Direct", "beta offer channel wrong");
    assert.equal(uuid(gotAlpha.body.sellerPartyId), seller, "alpha offer seller wrong");
    assert.equal(uuid(gotBeta.body.sellerPartyId), seller, "beta offer seller wrong");

    const distinct = snapshot(offers);
    trace.sql.push({ label: "distinct-authorized-offers", state: distinct }); save();
    assert.equal(distinct.offers.length, 2, "expected exactly two distinct offers");
    const byId = Object.fromEntries(distinct.offers.map(x => [x.offer_id, x]));
    assert.equal(byId[o1].channel, "Marketplace");
    assert.equal(byId[o2].channel, "Direct");
    assert.equal(distinct.history.length, 2, "expected exactly two creation history rows");

    // Changed channel with the same creation intent: same requestId => same deterministic offer id,
    // but the durable intent fingerprint differs. Must be a typed conflict and write nothing.
    const conflict = await api(BETA, "/v1/seller/offers", "POST", {
      catalogVariantId: variant, sellerSku: SKU_PREFIX + randomUUID(), status: "Draft", requestId: r1, channel: "Direct",
    }, HEADERS);
    assert.ok(conflict.status >= 400 && conflict.status < 500, "intent conflict not a typed 4xx: " + conflict.status + " " + JSON.stringify(conflict.body));
    assert.equal(conflict.body && conflict.body.errorCode, "offer.create.intent_conflict", "wrong intent conflict code: " + JSON.stringify(conflict.body));

    const afterConflict = snapshot(offers);
    trace.sql.push({ label: "after-intent-conflict", state: afterConflict }); save();
    assert.equal(afterConflict.offers.length, 2, "intent conflict wrote an offer");
    assert.equal(afterConflict.history.length, 2, "intent conflict wrote history");
    assert.equal(afterConflict.offerOutbox.length, distinct.offerOutbox.length, "intent conflict wrote outbox");
    assert.equal(afterConflict.offers.find(x => x.offer_id === o1).channel, "Marketplace", "intent conflict mutated original offer");

    trace.status = "PASS";
    trace.reason = "Two independently authorized DEV channel contexts (store-alpha Marketplace, store-beta Direct) on the same tooba_alpha DB created two distinct Draft offers for the same seller+variant; a changed-channel request replaying the same creation intent was rejected with offer.create.intent_conflict and wrote nothing.";
  } catch (error) {
    trace.status = error instanceof S.NotRun ? "NOT_RUN" : "FAIL";
    trace.reason = String(error && error.stack ? error.stack : error);
  } finally {
    if (offers.length) {
      try { Object.assign(trace, cleanupExact(offers, seller)); }
      catch (error) { trace.status = "FAIL"; trace.cleanupError = String(error && error.stack ? error.stack : error); }
    }
    save();
  }
  console.log(JSON.stringify({ id: "R10-01-B", status: trace.status, reason: trace.reason }));
}

main().catch(error => { console.error(error); process.exit(1); });
