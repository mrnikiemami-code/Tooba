// TB-296-PRICING-FINAL-UAT-CLOSURE — connected verification (HTTP + PostgreSQL + Playwright).
// Real Host 5088 / tooba_alpha. Dedicated P1C offer fixture with exact cleanup. No mocks.
const fs = require("node:fs");
const path = require("node:path");
const { execFileSync } = require("node:child_process");
const { randomUUID } = require("node:crypto");

const HOST = "http://127.0.0.1:5088";
const FRONT = "http://127.0.0.1:3000";
const ADMIN_HEADER = "X-Tooba-Dev-Actor-User-Id";
const SELLER_HEADER = "X-Tooba-Seller-Party-Id";
const SELLER = "01a030d1-40cb-7000-8abe-6d31739956c5";
const ACTOR = "01a03628-3f68-7000-844d-99f1cadb54b0";
const PSQL = process.env.P1C_PSQL || path.resolve(__dirname, "../../TB-296-P1-C-INTEGRATION/p1c-psql.exe");
const PLAYWRIGHT = process.env.P1C_PLAYWRIGHT || [
  path.resolve(__dirname, "../../../../src/frontend/node_modules/playwright"),
  "C:\\Users\\User\\OpenCode-UI-Lab-20261122-064208\\node_modules\\playwright",
  "C:\\Users\\User\\.cache\\codex-runtimes\\codex-primary-runtime\\dependencies\\node\\node_modules\\playwright",
].find(candidate => fs.existsSync(candidate));

function check(condition, message) { if (!condition) throw new Error(message); }
function uuid(v) { check(/^[0-9a-f]{8}-(?:[0-9a-f]{4}-){3}[0-9a-f]{12}$/i.test(v), "not a uuid: " + v); return v.toLowerCase(); }
function psql(q) {
  return execFileSync(PSQL, ["-X", "-qAt", "-v", "ON_ERROR_STOP=1", "-d", "tooba_alpha", "-c", q], { encoding: "utf8", timeout: 60000, windowsHide: true }).trim();
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

async function main() {
  const root = path.join(__dirname, "evidence");
  fs.mkdirSync(root, { recursive: true });
  const evidence = path.join(root, "connected-verification.json");
  check(!fs.existsSync(evidence), "Do not overwrite evidence");
  const trace = { taskId: "TB-296-PRICING-FINAL-UAT-CLOSURE", requests: [], sql: [], status: "NOT_RUN" };
  const save = () => fs.writeFileSync(evidence, JSON.stringify(trace, null, 2));
  let offerId = null;
  let browser = null;
  const req = async (label, p) => { const r = await p; trace.requests.push({ label, status: r.status, body: r.body }); save(); return r; };

  try {
    const dev = await call("/v1/admin/dev-context");
    check(dev.status === 200, "admin dev-context " + dev.status);
    const admin = { [ADMIN_HEADER]: uuid(dev.body.actorUserId) };

    // 1) Authoritative currency reference
    const currencies = await req("currencies", call("/v1/admin/offers/currencies", "GET", undefined, admin));
    check(currencies.status === 200 && Array.isArray(currencies.body), "currencies " + currencies.status);
    const codes = currencies.body.map(x => x.code);
    check(codes.includes("IRR") && codes.includes("USD"), "IRR/USD missing from reference: " + codes.join(","));
    const irr = currencies.body.find(x => x.code === "IRR");
    const usd = currencies.body.find(x => x.code === "USD");
    check(irr && irr.decimalPlaces === 0, "IRR decimal places not zero");
    check(usd && usd.decimalPlaces === 2, "USD decimal places not two");
    trace.currencies = currencies.body;

    const sellerCurrencies = await req("seller-currencies", call("/v1/seller/offers/currencies", "GET", undefined,
      { [ADMIN_HEADER]: ACTOR, [SELLER_HEADER]: SELLER }));
    check(sellerCurrencies.status === 200 && sellerCurrencies.body.some(x => x.code === "USD"), "seller currency reference missing USD");

    // 2) Dedicated P1C offer fixture
    const variant = psql("select v.variant_id from catalog.variants v join catalog.products p on p.product_id=v.product_id where v.status='Published' and p.quantity_decimal_places=0 and not exists (select 1 from offer.offers o where o.seller_party_id=" + quote(SELLER) + " and o.catalog_variant_id=v.variant_id) order by v.variant_id limit 1").trim();
    check(/^[0-9a-f-]{36}$/i.test(variant), "no free variant");
    const requestId = "P1C-" + randomUUID();
    const created = await req("create-offer", call("/v1/admin/offers", "POST", {
      sellerPartyId: SELLER, catalogVariantId: variant, channel: "Marketplace",
      sellerSku: "P1C-PRICING-" + randomUUID(), status: "Draft", requestId,
    }, admin));
    check(created.status === 201, "create offer " + created.status + " " + JSON.stringify(created.body).slice(0, 300));
    offerId = uuid(created.body.offerId);
    trace.offerId = offerId;

    const detail0 = await req("detail-empty", call("/v1/admin/offers/" + offerId, "GET", undefined, admin));
    check(detail0.status === 200, "detail " + detail0.status);
    check((detail0.body.prices ?? []).length === 0, "new offer unexpectedly has prices");
    check(detail0.body.currency === "IRR", "store default currency is not IRR: " + detail0.body.currency);

    // 3) IRR integer price
    const irrWrite = await req("write-irr", call("/v1/admin/offers/" + offerId + "/price", "PUT", {
      amount: 100000, currency: "IRR", expectedVersion: "absent", operationId: randomUUID(),
    }, admin));
    check(irrWrite.status === 200, "IRR write " + irrWrite.status + " " + JSON.stringify(irrWrite.body).slice(0, 300));
    const irrRow = (irrWrite.body.prices ?? []).find(x => x.currency === "IRR");
    check(irrRow && irrRow.amount === 100000, "IRR row missing");
    check(irrWrite.body.currency === "IRR" && irrWrite.body.amount === 100000, "IRR primary not set");

    // 4) USD second price
    const usdWrite = await req("write-usd", call("/v1/admin/offers/" + offerId + "/price", "PUT", {
      amount: 12.34, currency: "USD", expectedVersion: "absent", operationId: randomUUID(),
    }, admin));
    check(usdWrite.status === 200, "USD write " + usdWrite.status + " " + JSON.stringify(usdWrite.body).slice(0, 300));
    const afterUsd = usdWrite.body;
    const irrAfter = (afterUsd.prices ?? []).find(x => x.currency === "IRR");
    const usdAfter = (afterUsd.prices ?? []).find(x => x.currency === "USD");
    check(irrAfter && irrAfter.amount === 100000, "IRR row changed after USD write");
    check(usdAfter && usdAfter.amount === 12.34, "USD row missing");
    check(afterUsd.currency === "IRR" && afterUsd.amount === 100000, "primary changed after USD write");
    check(afterUsd.prices.length === 2, "expected two price rows");
    trace.afterSecondCurrency = { primary: { amount: afterUsd.amount, currency: afterUsd.currency }, prices: afterUsd.prices };

    // 5) Per-currency revision independence
    const usdRevision = usdAfter.revision;
    const irrRevision = irrAfter.revision;
    check(usdRevision && irrRevision && usdRevision !== irrRevision, "price revisions are not independent");
    const usdUpdate = await req("update-usd", call("/v1/admin/offers/" + offerId + "/price", "PUT", {
      amount: 13.5, currency: "USD", expectedVersion: usdRevision, operationId: randomUUID(),
    }, admin));
    check(usdUpdate.status === 200, "USD update " + usdUpdate.status + " " + JSON.stringify(usdUpdate.body).slice(0, 300));
    const irrStill = (usdUpdate.body.prices ?? []).find(x => x.currency === "IRR");
    check(irrStill && irrStill.amount === 100000 && irrStill.revision === irrRevision, "IRR row/revision changed after USD update");
    check((usdUpdate.body.prices ?? []).find(x => x.currency === "USD").amount === 13.5, "USD update not persisted");

    const stale = await req("stale-usd", call("/v1/admin/offers/" + offerId + "/price", "PUT", {
      amount: 99, currency: "USD", expectedVersion: usdRevision, operationId: randomUUID(),
    }, admin));
    check(stale.status >= 400 && stale.status < 500, "stale USD revision not denied: " + stale.status);

    // 6) IRR fractional input is normalized to the integer scale (no fractional persisted)
    const irrFractional = await req("irr-fractional", call("/v1/admin/offers/" + offerId + "/price", "PUT", {
      amount: 100000.75, currency: "IRR", expectedVersion: irrStill.revision, operationId: randomUUID(),
    }, admin));
    check(irrFractional.status === 200, "IRR fractional write " + irrFractional.status);
    const irrFinal = (irrFractional.body.prices ?? []).find(x => x.currency === "IRR");
    check(irrFinal && Number.isInteger(irrFinal.amount), "IRR persisted a fractional amount: " + JSON.stringify(irrFinal));

    // 7) Inventory provisioning still works through the existing owner endpoint
    const locations = await req("locations", call("/v1/admin/offers/inventory-locations", "GET", undefined, admin));
    check(locations.status === 200 && Array.isArray(locations.body) && locations.body.length > 0, "no inventory locations");
    const provision = await req("provision", call("/v1/admin/offers/" + offerId + "/inventory/positions", "POST", { locationId: locations.body[0].locationId }, admin));
    check(provision.status === 200, "inventory provision " + provision.status + " " + JSON.stringify(provision.body).slice(0, 200));
    const detailProvisioned = await req("detail-provisioned", call("/v1/admin/offers/" + offerId, "GET", undefined, admin));
    check((detailProvisioned.body.inventoryLocations ?? []).length === 1, "provisioned position not visible");

    // 8) Independent SQL: two active base rows, one per currency, IRR integer
    trace.sqlPrices = psqlJson("SELECT coalesce(jsonb_agg(jsonb_build_object('currency',currency,'amount',amount,'status',status) ORDER BY currency),'[]'::jsonb) FROM pricing.prices WHERE offer_id='" + offerId + "' AND qualifier_kind='Base'");
    trace.sqlFacts = Number(psql("SELECT count(*) FROM pricing.offer_price_changes WHERE offer_id='" + offerId + "'"));
    trace.sqlOutbox = Number(psql("SELECT count(*) FROM pricing.outbox_messages WHERE strpos(payload::text,'" + offerId + "')>0"));
    trace.sqlPositions = Number(psql("SELECT count(*) FROM inventory.stock_positions WHERE offer_id='" + offerId + "'"));
    save();
    const baseRows = trace.sqlPrices;
    check(baseRows.length === 2, "SQL expected two base rows before the browser step: " + JSON.stringify(baseRows));
    check(baseRows.find(x => x.currency === "IRR" && Number.isInteger(Number(x.amount))), "SQL IRR amount is not integer");
    check(baseRows.find(x => x.currency === "USD" && Number(x.amount) === 13.5), "SQL USD amount wrong");

    check(trace.sqlPositions === 1, "SQL inventory position missing");

    // 9) Real browser: admin pricing tab, USD edit, reload, locale/RTL, no market column
    check(PLAYWRIGHT, "Playwright unavailable");
    const { chromium } = require(PLAYWRIGHT);
    browser = await chromium.launch();
    const context = await browser.newContext({ locale: "fa-IR", acceptDownloads: true });
    await context.addInitScript(actor => localStorage.setItem("tooba.adminActorUserId", actor), dev.body.actorUserId);
    const page = await context.newPage();
    await page.goto(FRONT + "/fa/admin/offers/" + offerId, { waitUntil: "load" });
    await page.waitForSelector("[data-testid=offer-workspace]", { timeout: 30000 });
    await page.waitForTimeout(1200);
    const faShell = await page.evaluate(() => {
      const root = document.querySelector("[data-testid=offer-workspace]");
      return { dir: root?.getAttribute("dir"), lang: root?.getAttribute("lang") };
    });
    check(faShell.dir === "rtl" && String(faShell.lang || "").startsWith("fa"), "fa shell not RTL/fa: " + JSON.stringify(faShell));
    await page.click("[data-testid=offer-tab-button-pricing]");
    await page.waitForSelector("[data-testid=offer-tab-pricing]", { timeout: 15000 });
    await page.waitForSelector("[data-testid=offer-price-currency-select]", { timeout: 15000 });
    const pricingFacts = await page.evaluate(() => {
      const tab = document.querySelector("[data-testid=offer-tab-pricing]");
      const headers = Array.from(tab?.querySelectorAll("thead th") ?? []).map(x => x.textContent?.trim() ?? "");
      const rows = Array.from(tab?.querySelectorAll("[data-testid=offer-price-row]") ?? []).map(x => x.getAttribute("data-currency"));
      const select = document.querySelector("[data-testid=offer-price-currency-select]");
      const options = Array.from(select?.querySelectorAll("option") ?? []).map(x => x.value);
      return { headers, rows, options };
    });
    trace.browserFa = pricingFacts;
    save();
    check(!pricingFacts.headers.some(h => /بازار|market/i.test(h)), "market column still present: " + pricingFacts.headers.join(","));
    check(pricingFacts.rows.includes("IRR") && pricingFacts.rows.includes("USD"), "price rows missing in browser: " + pricingFacts.rows.join(","));
    check(pricingFacts.options.includes("IRR") && pricingFacts.options.includes("USD"), "currency selector missing IRR/USD: " + pricingFacts.options.join(","));
    check(pricingFacts.options.includes("EUR"), "currency selector does not include the authoritative list");

    await page.selectOption("[data-testid=offer-price-currency-select]", "EUR");
    await page.fill("[data-testid=offer-price-input]", "20.5");
    const saveButton = page.locator("[data-offer-save]").first();
    await saveButton.click();
    await page.waitForTimeout(2500);
    const afterBrowserSave = await req("browser-detail", call("/v1/admin/offers/" + offerId, "GET", undefined, admin));
    const eurBrowser = (afterBrowserSave.body.prices ?? []).find(x => x.currency === "EUR");
    check(eurBrowser && eurBrowser.amount === 20.5, "browser EUR (new currency) save not persisted: " + JSON.stringify(eurBrowser));
    trace.browserSave = { eur: eurBrowser, prices: afterBrowserSave.body.prices };
    trace.sqlPricesAfterBrowser = psqlJson("SELECT coalesce(jsonb_agg(jsonb_build_object('currency',currency,'amount',amount,'status',status) ORDER BY currency),'[]'::jsonb) FROM pricing.prices WHERE offer_id='" + offerId + "' AND qualifier_kind='Base'");
    save();
    check(trace.sqlPricesAfterBrowser.length === 3, "SQL expected three base rows after the browser EUR save: " + JSON.stringify(trace.sqlPricesAfterBrowser));
    check(trace.sqlPricesAfterBrowser.find(x => x.currency === "EUR" && Number(x.amount) === 20.5), "SQL EUR amount wrong after browser save");

    await page.goto(FRONT + "/en/admin/offers/" + offerId, { waitUntil: "load" });
    await page.waitForSelector("[data-testid=offer-workspace]", { timeout: 30000 });
    await page.waitForTimeout(800);
    const enShell = await page.evaluate(() => {
      const root = document.querySelector("[data-testid=offer-workspace]");
      return { dir: root?.getAttribute("dir"), lang: root?.getAttribute("lang") };
    });
    trace.browserEn = enShell;
    check(enShell.dir === "ltr" && String(enShell.lang || "").startsWith("en"), "en shell not LTR/en: " + JSON.stringify(enShell));

    await browser.close(); browser = null;

    // 10) Exact cleanup of this task's fixture
    psql("BEGIN;" +
      "DELETE FROM inventory.stock_positions WHERE offer_id='" + offerId + "';" +
      "DELETE FROM pricing.offer_price_changes WHERE offer_id='" + offerId + "';" +
      "DELETE FROM pricing.prices WHERE offer_id='" + offerId + "';" +
      "DELETE FROM pricing.outbox_messages WHERE strpos(payload::text,'" + offerId + "')>0;" +
      "DELETE FROM offer.offer_history WHERE offer_id='" + offerId + "';" +
      "DELETE FROM offer.offers WHERE offer_id='" + offerId + "';" +
      "DELETE FROM offer.outbox_messages WHERE strpos(payload::text,'" + offerId + "')>0;" +
      "DELETE FROM inventory.outbox_messages WHERE strpos(payload::text,'" + offerId + "')>0;" +
      "COMMIT;");
    trace.cleanup = {
      offers: Number(psql("SELECT count(*) FROM offer.offers WHERE offer_id='" + offerId + "'")),
      prices: Number(psql("SELECT count(*) FROM pricing.prices WHERE offer_id='" + offerId + "'")),
      facts: Number(psql("SELECT count(*) FROM pricing.offer_price_changes WHERE offer_id='" + offerId + "'")),
      positions: Number(psql("SELECT count(*) FROM inventory.stock_positions WHERE offer_id='" + offerId + "'")),
    };
    check(trace.cleanup.offers === 0 && trace.cleanup.prices === 0 && trace.cleanup.facts === 0 && trace.cleanup.positions === 0, "fixture residue: " + JSON.stringify(trace.cleanup));

    trace.status = "PASS";
    trace.reason = "Real Host/tooba_alpha: authoritative currency reference (IRR=0, USD=2, EUR included) served to admin and seller; a dedicated offer recorded independent IRR and USD base prices with per-currency revisions, stale USD denied, IRR integer normalized, inventory position provisioned; SQL confirmed exactly three active base rows (IRR, USD, EUR); real Playwright admin pricing tab showed no market column, IRR/USD rows and selector, selected a currency with no authored row (EUR), saved 20.5, reloaded/persisted, fa RTL and en LTR; fixture cleaned exactly.";
  } catch (error) {
    trace.status = "FAIL";
    trace.reason = String(error && error.stack ? error.stack : error);
  } finally {
    if (browser) { try { await browser.close(); } catch { /* ignore */ } }
    if (offerId) {
      try {
        psql("BEGIN;" +
          "DELETE FROM inventory.stock_positions WHERE offer_id='" + offerId + "';" +
          "DELETE FROM pricing.offer_price_changes WHERE offer_id='" + offerId + "';" +
          "DELETE FROM pricing.prices WHERE offer_id='" + offerId + "';" +
          "DELETE FROM pricing.outbox_messages WHERE strpos(payload::text,'" + offerId + "')>0;" +
          "DELETE FROM offer.offer_history WHERE offer_id='" + offerId + "';" +
          "DELETE FROM offer.offers WHERE offer_id='" + offerId + "';" +
          "DELETE FROM offer.outbox_messages WHERE strpos(payload::text,'" + offerId + "')>0;" +
          "DELETE FROM inventory.outbox_messages WHERE strpos(payload::text,'" + offerId + "')>0;" +
          "COMMIT;");
        trace.finalCleanup = {
          offers: Number(psql("SELECT count(*) FROM offer.offers WHERE offer_id='" + offerId + "'")),
          prices: Number(psql("SELECT count(*) FROM pricing.prices WHERE offer_id='" + offerId + "'")),
          facts: Number(psql("SELECT count(*) FROM pricing.offer_price_changes WHERE offer_id='" + offerId + "'")),
          positions: Number(psql("SELECT count(*) FROM inventory.stock_positions WHERE offer_id='" + offerId + "'")),
        };
      } catch (error) {
        trace.finalCleanupError = String(error && error.stack ? error.stack : error);
      }
    }
    save();
  }
  console.log(JSON.stringify({ status: trace.status, reason: trace.reason, cleanup: trace.cleanup }));
  if (trace.status !== "PASS") process.exit(1);
}

main().catch(error => { console.error(error); process.exit(1); });
