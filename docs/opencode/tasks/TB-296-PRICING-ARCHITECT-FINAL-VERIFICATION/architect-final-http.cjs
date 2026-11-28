// TB-296-PRICING-ARCHITECT-FINAL-VERIFICATION — HTTP/PostgreSQL cases.
// Real repaired candidate Host 5088 + tooba_alpha. Dedicated P1C fixtures, exact cleanup.
const fs = require("node:fs");
const path = require("node:path");
const { execFileSync } = require("node:child_process");
const { randomUUID } = require("node:crypto");

const HOST = "http://127.0.0.1:5088";
const ADMIN_HEADER = "X-Tooba-Dev-Actor-User-Id";
const SELLER_HEADER = "X-Tooba-Seller-Party-Id";
const SELLER_A = "01a030d1-40cb-7000-8abe-6d31739956c5";
const ACTOR_A = "01a03628-3f68-7000-844d-99f1cadb54b0";
const SELLER_B = "01a030d1-40db-7000-b90c-a0705133f0eb";
const ACTOR_B = "01a03628-3ff0-7000-a04e-6420c2e76f72";
const PSQL = process.env.P1C_PSQL || path.resolve(__dirname, "../../TB-296-P1-C-INTEGRATION/p1c-psql.exe");

function check(c, m) { if (!c) throw new Error(m); }
function uuid(v) { check(/^[0-9a-f]{8}-(?:[0-9a-f]{4}-){3}[0-9a-f]{12}$/i.test(v), "not uuid " + v); return v.toLowerCase(); }
function psql(q) { return execFileSync(PSQL, ["-X", "-qAt", "-v", "ON_ERROR_STOP=1", "-d", "tooba_alpha", "-c", q], { encoding: "utf8", timeout: 60000, windowsHide: true }).trim(); }
function psqlJson(q) { return JSON.parse(psql(q)); }
const quote = x => "'" + String(x).replaceAll("'", "''") + "'";

async function call(route, method = "GET", payload, headers = {}) {
  const res = await fetch(HOST + route, { method, headers: { "Accept-Language": "fa-IR", ...(payload !== undefined ? { "Content-Type": "application/json" } : {}), ...headers }, body: payload !== undefined ? JSON.stringify(payload) : undefined, signal: AbortSignal.timeout(30000) });
  const raw = await res.text(); let body; try { body = raw ? JSON.parse(raw) : null; } catch { body = raw; }
  return { status: res.status, body };
}
const adminHeaders = actor => ({ [ADMIN_HEADER]: actor });
const sellerHeaders = (seller, actor) => ({ [SELLER_HEADER]: seller, [ADMIN_HEADER]: actor });

async function createDraft(sellerId) {
  const variant = psql(`select v.variant_id from catalog.variants v join catalog.products p on p.product_id=v.product_id where v.status='Published' and p.quantity_decimal_places=0 and not exists (select 1 from offer.offers o where o.seller_party_id=${quote(sellerId)} and o.catalog_variant_id=v.variant_id) order by v.variant_id limit 1`).trim();
  check(/^[0-9a-f-]{36}$/i.test(variant), "no free variant");
  const requestId = "P1C-" + randomUUID();
  const created = await call("/v1/admin/offers", "POST", { sellerPartyId: sellerId, catalogVariantId: variant, channel: "Marketplace", sellerSku: "P1C-AFPV-" + randomUUID(), status: "Draft", requestId }, adminHeaders(ADMIN_ACTOR));
  check(created.status === 201, "create " + created.status + " " + JSON.stringify(created.body).slice(0, 200));
  return { offerId: uuid(created.body.offerId), sellerId, variantId: variant };
}
async function createSellerDraft(seller, actor) {
  const variant = psql(`select v.variant_id from catalog.variants v join catalog.products p on p.product_id=v.product_id where v.status='Published' and p.quantity_decimal_places=0 and not exists (select 1 from offer.offers o where o.seller_party_id=${quote(seller)} and o.catalog_variant_id=v.variant_id) order by v.variant_id limit 1`).trim();
  const requestId = "P1C-" + randomUUID();
  const created = await call("/v1/seller/offers", "POST", { catalogVariantId: variant, sellerSku: "P1C-AFPV-S-" + randomUUID(), channel: "Marketplace", status: "Draft", requestId }, sellerHeaders(seller, actor));
  check(created.status === 201, "seller create " + created.status + " " + JSON.stringify(created.body).slice(0, 200));
  return { offerId: uuid(created.body.offerId), sellerId: seller, variantId: variant };
}
const detail = async (offerId, headers = adminHeaders(ADMIN_ACTOR)) => call("/v1/admin/offers/" + offerId, "GET", undefined, headers);
const price = (offerId, amount, currency, expectedVersion, operationId, headers = adminHeaders(ADMIN_ACTOR)) => call("/v1/admin/offers/" + offerId + "/price", "PUT", { amount, currency, expectedVersion, operationId }, headers);
function offerCounts(offerId) {
  return psqlJson(`select jsonb_build_object('prices',(select count(*) from pricing.prices where offer_id='${offerId}'),'facts',(select count(*) from pricing.offer_price_changes where offer_id='${offerId}'),'outbox',(select count(*) from pricing.outbox_messages where strpos(payload::text,'${offerId}')>0),'history',(select count(*) from offer.offer_history where offer_id='${offerId}'))`);
}
function cleanup(offerIds) {
  if (!offerIds.length) return {};
  const list = offerIds.map(quote).join(",");
  const outbox = offerIds.map(id => `strpos(payload::text,${quote(id)})>0`).join(" OR ");
  psql(`BEGIN;
    DELETE FROM inventory.stock_positions WHERE offer_id IN (${list});
    DELETE FROM inventory.offer_inventory_changes WHERE offer_id IN (${list});
    DELETE FROM inventory.offer_allocations WHERE offer_id IN (${list});
    DELETE FROM pricing.offer_price_changes WHERE offer_id IN (${list});
    DELETE FROM pricing.prices WHERE offer_id IN (${list});
    DELETE FROM pricing.outbox_messages WHERE ${outbox};
    DELETE FROM offer.offer_history WHERE offer_id IN (${list});
    DELETE FROM offer.offers WHERE offer_id IN (${list});
    DELETE FROM offer.outbox_messages WHERE ${outbox};
    DELETE FROM inventory.outbox_messages WHERE ${outbox};
    COMMIT;`);
  return { offers: Number(psql(`select count(*) from offer.offers where offer_id in (${list})`)), prices: Number(psql(`select count(*) from pricing.prices where offer_id in (${list})`)) };
}

let ADMIN_ACTOR = "01a036c2-970e-7000-8eb7-94bf5cc2d8db";

async function main() {
  const ev = process.env.P1C_EVIDENCE_DIR; check(ev, "set P1C_EVIDENCE_DIR");
  fs.mkdirSync(ev, { recursive: true });
  const out = { taskId: "TB-296-PRICING-ARCHITECT-FINAL-VERIFICATION", stage: "http", cases: {}, fixtures: [], status: "NOT_RUN" };
  const save = () => fs.writeFileSync(path.join(ev, "http-cases.json"), JSON.stringify(out, null, 2));
  const run = (id, fn) => { out.cases[id] = { status: "NOT_RUN" }; save(); return Promise.resolve().then(fn).then(r => { out.cases[id] = { status: "PASS", ...r }; save(); return r; }).catch(e => { out.cases[id] = { status: "FAIL", error: String(e && e.stack ? e.stack : e) }; save(); return null; }); };
  try {
    const dev = await call("/v1/admin/dev-context"); check(dev.status === 200, "admin ctx");
    ADMIN_ACTOR = uuid(dev.body.actorUserId);

    const fixture = await createDraft(SELLER_A); out.fixtures.push(fixture.offerId); save();
    const f = fixture.offerId;
    const baseline = offerCounts(f);

    // R11-REF-01
    await run("R11-REF-01", async () => {
      const admin = await call("/v1/admin/offers/currencies", "GET", undefined, adminHeaders(ADMIN_ACTOR));
      const seller = await call("/v1/seller/offers/currencies", "GET", undefined, sellerHeaders(SELLER_A, ACTOR_A));
      check(admin.status === 200 && seller.status === 200, "reference status");
      const cfg = JSON.parse(fs.readFileSync(path.resolve(__dirname, "../../../../src/backend/Host/Tooba.Host/appsettings.Development.json"), "utf8").replace(/^\uFEFF/, "")).Pricing.Currencies.filter(x => x.Active);
      const expected = cfg.sort((a, b) => a.SortOrder - b.SortOrder).map(x => [x.Code, x.DecimalPlaces]);
      const actual = admin.body.map(x => [x.code, x.decimalPlaces]);
      check(JSON.stringify(actual) === JSON.stringify(expected), "admin reference mismatch " + JSON.stringify({ actual, expected }));
      check(JSON.stringify(seller.body.map(x => [x.code, x.decimalPlaces])) === JSON.stringify(actual), "seller reference mismatch");
      return { admin: admin.body, seller: seller.body };
    });

    // R11-REF-02 (unknown + TMN; inactive in a dedicated stage)
    await run("R11-REF-02", async () => {
      const before = offerCounts(f);
      const unknown = await price(f, 10, "ZZZ", "absent", randomUUID());
      check(unknown.status === 400 && unknown.body.errorCode === "pricing.currency.invalid", "unknown " + unknown.status + " " + JSON.stringify(unknown.body));
      const tmn = await price(f, 10, "TMN", "absent", randomUUID());
      check(tmn.status === 400 && tmn.body.errorCode === "pricing.currency.display_unit", "TMN " + tmn.status + " " + JSON.stringify(tmn.body));
      const after = offerCounts(f);
      check(JSON.stringify(before) === JSON.stringify(after), "rejected writes mutated: " + JSON.stringify({ before, after }));
      return { unknown: unknown.body, tmn: tmn.body, before, after };
    });

    // create IRR active price for further cases
    const irrWrite = await price(f, 100000, "IRR", "absent", randomUUID());
    check(irrWrite.status === 200, "IRR seed " + irrWrite.status + " " + JSON.stringify(irrWrite.body).slice(0, 200));
    const irrRow = (irrWrite.body.prices ?? []).find(x => x.currency === "IRR");
    check(irrRow, "IRR row missing");

    // R11-PRICE-02
    await run("R11-PRICE-02", async () => {
      const usd1 = await price(f, 12, "USD", "absent", randomUUID());
      check(usd1.status === 200, "USD seed " + usd1.status);
      const usdRow = (usd1.body.prices ?? []).find(x => x.currency === "USD");
      const irrBefore = (usd1.body.prices ?? []).find(x => x.currency === "IRR");
      check(usdRow && irrBefore, "rows");
      const ops = [randomUUID(), randomUUID()];
      const [w1, w2] = await Promise.all([
        price(f, 13, "USD", usdRow.revision, ops[0]),
        price(f, 14, "USD", usdRow.revision, ops[1]),
      ]);
      const wins = [w1, w2].filter(x => x.status === 200);
      const losses = [w1, w2].filter(x => x.status >= 400);
      check(wins.length === 1 && losses.length === 1, "USD stale race " + JSON.stringify([w1.status, w2.status]));
      check(losses[0].body.errorCode === "concurrency.conflict", "stale code " + JSON.stringify(losses[0].body));
      const winAmount = w1.status === 200 ? 13 : 14;
      const after = await detail(f);
      const usdAfter = (after.body.prices ?? []).find(x => x.currency === "USD");
      const irrAfter = (after.body.prices ?? []).find(x => x.currency === "IRR");
      check(usdAfter.amount === winAmount, "USD winner amount");
      check(irrAfter.amount === 100000 && irrAfter.revision === irrBefore.revision, "IRR changed by USD");
      const irrEdit = await price(f, 100001, "IRR", irrAfter.revision, randomUUID());
      check(irrEdit.status === 200, "IRR independent edit " + irrEdit.status);
      const usdStill = (irrEdit.body.prices ?? []).find(x => x.currency === "USD");
      check(usdStill.amount === winAmount, "USD changed by IRR");
      const facts = Number(psql(`select count(*) from pricing.offer_price_changes where offer_id='${f}' and currency='USD'`));
      check(facts === 2, "USD facts expected 2 (seed+winner): " + facts);
      return { winAmount, irrRevisionStable: irrAfter.revision === irrBefore.revision, usdFacts: facts };
    });

    // R11-PRICE-05 (replay + changed intent)
    await run("R11-PRICE-05", async () => {
      const op = randomUUID();
      const cur = (await detail(f)).body.prices.find(x => x.currency === "USD");
      const first = await price(f, cur.amount, "USD", cur.revision, op);
      check(first.status === 200, "replay seed " + first.status);
      const rev = first.body.prices.find(x => x.currency === "USD").revision;
      const replay = await price(f, cur.amount, "USD", cur.revision, op);
      check(replay.status === 200, "identical replay " + replay.status);
      check(replay.body.prices.find(x => x.currency === "USD").revision === rev, "replay revision differs");
      const changedAmount = await price(f, cur.amount + 1, "USD", cur.revision, op);
      check(changedAmount.status >= 400 && changedAmount.body.errorCode === "concurrency.conflict", "changed amount replay not conflict: " + changedAmount.status + " " + JSON.stringify(changedAmount.body));
      const changedCurrency = await price(f, cur.amount, "EUR", cur.revision, op);
      check(changedCurrency.status >= 400, "changed currency replay not rejected: " + changedCurrency.status);
      const facts = Number(psql(`select count(*) from pricing.offer_price_changes where offer_id='${f}' and operation_id='${op}'`));
      check(facts === 1, "replay facts expected 1: " + facts);
      return { factForOperation: facts };
    });

    // R11-PREC-02 (canonical money 100000.75 -> 100001)
    await run("R11-PREC-02", async () => {
      const op = randomUUID();
      const cur = (await detail(f)).body.prices.find(x => x.currency === "IRR");
      const r = await price(f, 100000.75, "IRR", cur.revision, op);
      check(r.status === 200, "fractional write " + r.status);
      const row = r.body.prices.find(x => x.currency === "IRR");
      check(row.amount === 100001, "price not canonical: " + row.amount);
      const db = psqlJson(`select jsonb_build_object('price',(select amount from pricing.prices where offer_id='${f}' and currency='IRR' order by price_id desc limit 1),'factNew',(select new_amount from pricing.offer_price_changes where offer_id='${f}' and operation_id='${op}'))`);
      check(Number(db.price) === 100001 && Number(db.factNew) === 100001, "DB canonical mismatch " + JSON.stringify(db));
      const replay = await price(f, 100000.75, "IRR", cur.revision, op);
      check(replay.status === 200, "canonical replay " + replay.status);
      const facts = Number(psql(`select count(*) from pricing.offer_price_changes where offer_id='${f}' and operation_id='${op}'`));
      check(facts === 1, "canonical replay facts " + facts);
      return { price: 100001, factNew: Number(db.factNew), facts };
    });

    // R11-PRICE-03 (authoring Draft/Retired vs effective quote; existing overlap rule)
    await run("R11-PRICE-03", async () => {
      const d0 = await detail(f);
      const irr = d0.body.prices.find(x => x.currency === "IRR");
      // Insert a newer Retired authoring row for IRR; the effective quote must still come from the older Active row.
      const retiredId = "ffffffff-ffff-7fff-8fff-ffffffffffff";
      psql(`INSERT INTO pricing.prices (price_id,offer_id,market,channel,currency,amount,valid_from,valid_to,status,qualifier_kind,qualifier_key,created_at,updated_at)
        VALUES ('${retiredId}','${f}','IR','Marketplace','IRR',900,now()-interval '1 day',NULL,'Retired','Base',NULL,now(),now())`);
      const d1 = await detail(f);
      const authoring = d1.body.prices.find(x => x.currency === "IRR");
      check(authoring.priceId === retiredId && authoring.status === "Retired", "authoring not retired latest: " + JSON.stringify(authoring));
      check(d1.body.amount === 100001 && d1.body.currency === "IRR", "effective quote changed by retired authoring: " + JSON.stringify({ amount: d1.body.amount, currency: d1.body.currency }));
      const w = await price(f, 950, "IRR", authoring.revision, randomUUID());
      check(w.status >= 400 && w.body.errorCode === "pricing.overlap", "retired authoring write did not follow overlap rule: " + w.status + " " + JSON.stringify(w.body));
      const after = await detail(f);
      check(after.body.amount === 100001, "quote changed after denied overlap");
      return { authoringStatus: authoring.status, quote: d1.body.amount, deniedCode: w.body.errorCode };
    });

    // R11-UAT-02 (cross-owner denial, no mutation)
    await run("R11-UAT-02", async () => {
      const before = offerCounts(f);
      const otherSeller = await call("/v1/seller/offers/" + f + "/price", "PUT", { amount: 1, currency: "IRR", expectedVersion: "absent", operationId: randomUUID() }, sellerHeaders(SELLER_B, ACTOR_B));
      check(otherSeller.status === 403 || otherSeller.status === 404, "cross-owner price not denied: " + otherSeller.status);
      const mismatched = await call("/v1/seller/offers/" + f + "/price", "PUT", { amount: 1, currency: "IRR", expectedVersion: "absent", operationId: randomUUID() }, sellerHeaders(SELLER_B, ACTOR_A));
      check(mismatched.status === 403 || mismatched.status === 404, "mismatched seller/actor not denied: " + mismatched.status);
      const after = offerCounts(f);
      check(JSON.stringify(before) === JSON.stringify(after), "cross-owner attempt mutated");
      return { otherSeller: otherSeller.status, mismatched: mismatched.status, before, after };
    });

    out.status = Object.values(out.cases).every(x => x.status === "PASS") ? "PASS" : "FAIL"; save();
    console.log(JSON.stringify({ stage: "http", status: out.status, cases: Object.fromEntries(Object.entries(out.cases).map(([k, v]) => [k, v.status])), cleanup: cleanup(out.fixtures) }));
  } catch (error) {
    out.status = "FAIL"; out.error = String(error && error.stack ? error.stack : error); save();
    try { out.cleanup = cleanup(out.fixtures); } catch (e) { out.cleanupError = String(e); }
    console.log(JSON.stringify({ stage: "http", status: out.status, error: out.error, cleanup: out.cleanup }));
    process.exit(1);
  }
}
main().catch(e => { console.error(e); process.exit(1); });
