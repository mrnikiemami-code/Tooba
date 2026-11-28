// TB-296-PRICING-ARCHITECT-FINAL-VERIFICATION — browser cases on the repaired candidate.
// Real Next 3000 + Host 5088; dedicated fixture; no screenshots; exact cleanup.
const fs = require("node:fs");
const path = require("node:path");
const { execFileSync } = require("node:child_process");
const { randomUUID } = require("node:crypto");

const HOST = "http://127.0.0.1:5088";
const FRONT = "http://127.0.0.1:3000";
const ADMIN_HEADER = "X-Tooba-Dev-Actor-User-Id";
const SELLER = "01a030d1-40cb-7000-8abe-6d31739956c5";
const PSQL = process.env.P1C_PSQL || path.resolve(__dirname, "../../TB-296-P1-C-INTEGRATION/p1c-psql.exe");
const PLAYWRIGHT = process.env.P1C_PLAYWRIGHT || [
  path.resolve(__dirname, "../../../../src/frontend/node_modules/playwright"),
  "C:\\Users\\User\\OpenCode-UI-Lab-20261122-064208\\node_modules\\playwright",
].find(p => fs.existsSync(p));

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

async function main() {
  const ev = process.env.P1C_EVIDENCE_DIR; check(ev, "set P1C_EVIDENCE_DIR");
  fs.mkdirSync(ev, { recursive: true });
  const out = { taskId: "TB-296-PRICING-ARCHITECT-FINAL-VERIFICATION", stage: "browser", cases: {}, fixtures: [], status: "NOT_RUN" };
  const save = () => fs.writeFileSync(path.join(ev, "browser-cases.json"), JSON.stringify(out, null, 2));
  let browser = null;
  const run = (id, fn) => { out.cases[id] = { status: "NOT_RUN" }; save(); return Promise.resolve().then(fn).then(r => { out.cases[id] = { status: "PASS", ...r }; save(); return r; }).catch(e => { out.cases[id] = { status: "FAIL", error: String(e && e.stack ? e.stack : e) }; save(); return null; }); };
  let page = null;
  let priceRequests = [];
  const detail = async (id, actor) => (await call("/v1/admin/offers/" + id, "GET", undefined, adminHeaders(actor))).body;
  try {
    const dev = await call("/v1/admin/dev-context"); check(dev.status === 200, "dev context");
    const actor = uuid(dev.body.actorUserId);
    const variant = psql(`select v.variant_id from catalog.variants v join catalog.products p on p.product_id=v.product_id where v.status='Published' and p.quantity_decimal_places=0 and not exists (select 1 from offer.offers o where o.seller_party_id=${quote(SELLER)} and o.catalog_variant_id=v.variant_id) order by v.variant_id limit 1`).trim();
    const created = await call("/v1/admin/offers", "POST", { sellerPartyId: SELLER, catalogVariantId: variant, channel: "Marketplace", sellerSku: "P1C-AFPV-B-" + randomUUID(), status: "Draft", requestId: "P1C-" + randomUUID() }, adminHeaders(actor));
    check(created.status === 201, "create " + created.status + " " + JSON.stringify(created.body).slice(0, 200));
    const offerId = uuid(created.body.offerId); out.fixtures.push(offerId); save();
    const irr = await call("/v1/admin/offers/" + offerId + "/price", "PUT", { amount: 100000, currency: "IRR", expectedVersion: "absent", operationId: randomUUID() }, adminHeaders(actor));
    check(irr.status === 200, "IRR seed " + irr.status);
    const usd = await call("/v1/admin/offers/" + offerId + "/price", "PUT", { amount: 12.34, currency: "USD", expectedVersion: "absent", operationId: randomUUID() }, adminHeaders(actor));
    check(usd.status === 200, "USD seed " + usd.status);
    const seedDetail = await detail(offerId, actor);
    const assigned = await call("/v1/admin/offers/" + offerId + "/fulfillment", "PUT", { responsibility: "Platform", shippingTypeId: null, expectedVersion: seedDetail.revisions.offer }, adminHeaders(actor));
    check(assigned.status === 200, "platform assignment " + assigned.status + " " + JSON.stringify(assigned.body).slice(0, 200));
    out.variantLabel = (await detail(offerId, actor)).variantLabel ?? "";

    check(PLAYWRIGHT, "playwright missing");
    const { chromium } = require(PLAYWRIGHT);
    const norm = s => String(s).replace(/[۰-۹]/g, d => String("۰۱۲۳۴۵۶۷۸۹".indexOf(d))).replace(/[٠-٩]/g, d => String("٠١٢٣٤٥٦٧٨٩".indexOf(d))).replace(/٫/g, ".");
    browser = await chromium.launch();
    const context = await browser.newContext({ locale: "fa-IR", viewport: { width: 1440, height: 900 } });
    await context.addInitScript(a => { localStorage.setItem("tooba.adminActorUserId", a); }, actor);
    page = await context.newPage();
    page.on("request", r => { if (r.method() === "PUT" && r.url().includes("/price")) priceRequests.push(r.url()); });
    const open = async (locale = "fa", target = offerId, requireSave = true) => {
      await page.goto(FRONT + "/" + locale + "/admin/offers", { waitUntil: "load" });
      await page.evaluate(() => sessionStorage.clear());
      await page.goto(FRONT + "/" + locale + "/admin/offers/" + target, { waitUntil: "load" });
      await page.waitForSelector("[data-testid=offer-workspace]", { timeout: 30000 });
      await page.waitForSelector("[data-testid=admin-offer-save]", { timeout: 30000, state: requireSave ? "visible" : "attached" });
      await page.waitForTimeout(700);
    };
    const pricing = async () => {
      await page.waitForSelector("[data-testid=offer-tab-button-pricing]", { timeout: 20000 });
      await page.click("[data-testid=offer-tab-button-pricing]");
      await page.waitForSelector("[data-testid=offer-tab-pricing]", { timeout: 15000 });
      await page.waitForTimeout(400);
    };
    const saveBtn = () => page.locator("[data-testid=admin-offer-save]").first();
    const setCurrency = async c => { await page.selectOption("[data-testid=offer-price-currency-select]", c); await page.waitForTimeout(350); };
    const fill = async v => { await page.fill("[data-testid=offer-price-input]", v); await page.waitForTimeout(300); };
    const invalid = async () => page.locator("[data-testid=offer-price-input]").getAttribute("aria-invalid");

    // R11-REF-04
    await run("R11-REF-04", async () => {
      await context.route("**/v1/admin/offers/currencies", route => route.abort());
      await open("fa"); await pricing();
      const unavail = await page.locator("[data-testid=offer-tab-pricing]").innerText();
      check(unavail.includes("فهرست ارزهای مجاز در دسترس نیست"), "unavailable text missing: " + unavail.slice(0, 160));
      const disabled = await page.evaluate(() => ({ select: document.querySelector("[data-testid=offer-price-currency-select]")?.disabled, input: document.querySelector("[data-testid=offer-price-input]")?.disabled }));
      check(disabled.select === true && disabled.input === true, "inputs not disabled while unavailable: " + JSON.stringify(disabled));
      const before = priceRequests.length;
      await context.unroute("**/v1/admin/offers/currencies");
      await page.getByRole("button", { name: "تلاش دوباره" }).first().click();
      await page.waitForFunction(() => {
        const s = document.querySelector("[data-testid=offer-price-currency-select]");
        return s && s.disabled === false && Array.from(s.querySelectorAll("option")).some(o => o.value === "USD");
      }, { timeout: 15000 });
      check(priceRequests.length === before, "price request while unavailable");
      const options = await page.$$eval("[data-testid=offer-price-currency-select] option", els => els.map(e => e.value));
      check(options.includes("IRR") && options.includes("USD"), "retry did not recover options");
      return { disabled, options };
    });

    // R11-PREC-01
    await run("R11-PREC-01", async () => {
      await open("fa"); await pricing();
      await setCurrency("IRR"); await fill("10.5"); check((await invalid()) === "true", "IRR fractional not invalid"); const irrBefore = priceRequests.length; await saveBtn().click(); await page.waitForTimeout(600); check(priceRequests.length === irrBefore, "IRR fractional submitted"); await fill("100000");
      await setCurrency("JPY"); await fill("10.5"); check((await invalid()) === "true", "JPY fractional not invalid"); const jpyBefore = priceRequests.length; await saveBtn().click(); await page.waitForTimeout(600); check(priceRequests.length === jpyBefore, "JPY fractional submitted"); await fill("");
      await fill("10.00"); check((await invalid()) === "false", "JPY integer invalid"); await saveBtn().click(); await page.waitForTimeout(1800); check(priceRequests.length === jpyBefore + 1, "JPY valid not submitted");
      await setCurrency("USD"); await fill("12.345"); check((await invalid()) === "true", "USD excess scale not invalid"); const usdBefore = priceRequests.length; await saveBtn().click(); await page.waitForTimeout(600); check(priceRequests.length === usdBefore, "USD excess scale submitted"); await fill("12.34");
      await fill("12.35"); check((await invalid()) === "false", "USD exact invalid"); await saveBtn().click(); await page.waitForTimeout(1800); check(priceRequests.length === usdBefore + 1, "USD valid not submitted");
      const d = await detail(offerId, actor);
      const jpyRow = d.prices.find(x => x.currency === "JPY");
      const usdRow = d.prices.find(x => x.currency === "USD");
      check(jpyRow && jpyRow.amount === 10, "JPY canonical 10 not persisted: " + JSON.stringify(jpyRow));
      check(usdRow && usdRow.amount === 12.35, "USD exact not persisted: " + JSON.stringify(usdRow));
      return { jpy: jpyRow, usd: usdRow };
    });

    // R11-LOCALE-01
    await run("R11-LOCALE-01", async () => {
      await open("fa"); await pricing();
      await setCurrency("IRR");
      const fa = await page.evaluate(() => ({ dir: document.querySelector("[data-testid=offer-workspace]")?.getAttribute("dir"), lang: document.querySelector("[data-testid=offer-workspace]")?.getAttribute("lang"), amount: document.querySelector("[data-testid=offer-price-input]")?.value, heading: document.querySelector("[data-testid=offer-tab-pricing] h2")?.textContent ?? "" }));
      check(fa.dir === "rtl" && String(fa.lang || "").startsWith("fa"), "fa dir/lang");
      check(!String(fa.amount).includes("."), "fa IRR shows decimal");
      await Promise.all([page.waitForURL(/\/en\/admin\/offers\//, { timeout: 20000 }), page.locator("[data-testid=panel-language-select]").selectOption("en")]);
      await page.waitForSelector("[data-testid=offer-workspace]", { timeout: 30000 }); await pricing();
      const en = await page.evaluate(() => ({ dir: document.querySelector("[data-testid=offer-workspace]")?.getAttribute("dir"), lang: document.querySelector("[data-testid=offer-workspace]")?.getAttribute("lang"), text: document.querySelector("[data-testid=offer-tab-pricing]")?.textContent ?? "" }));
      check(en.dir === "ltr" && String(en.lang || "").startsWith("en"), "en dir/lang");
      check(/Price|Amount|Currency/.test(en.text), "en pricing labels missing");
      await Promise.all([page.waitForURL(/\/ru\/admin\/offers\//, { timeout: 20000 }), page.locator("[data-testid=panel-language-select]").selectOption("ru")]);
      await page.waitForSelector("[data-testid=offer-workspace]", { timeout: 30000 }); await pricing();
      await setCurrency("USD");
      const ru = await page.evaluate(() => ({ dir: document.querySelector("[data-testid=offer-workspace]")?.getAttribute("dir"), lang: document.querySelector("[data-testid=offer-workspace]")?.getAttribute("lang"), amount: document.querySelector("[data-testid=offer-price-input]")?.value, text: document.querySelector("[data-testid=offer-tab-pricing]")?.textContent ?? "" }));
      check(ru.dir === "ltr" && String(ru.lang || "").startsWith("ru"), "ru dir/lang");
      check(String(ru.amount).includes(",") && !String(ru.amount).includes("."), "ru comma decimal not preserved: " + ru.amount);
      return { fa, en, ru };
    });

    // R11-LOCALE-02
    await run("R11-LOCALE-02", async () => {
      await open("fa"); await pricing();
      await setCurrency("USD");
      const serverUsd = (await detail(offerId, actor)).prices.find(x => x.currency === "USD").amount;
      await fill("13");
      await page.selectOption("[data-testid=offer-price-currency-select]", "EUR"); await page.waitForTimeout(700);
      const warn = await page.locator("[data-testid=offer-message-error]").innerText().catch(() => "");
      check(warn.includes("ابتدا قیمت فعلی را ذخیره کنید"), "switch warning missing: " + warn.slice(0, 160));
      const amountAfterWarn = await page.inputValue("[data-testid=offer-price-input]");
      check(norm(amountAfterWarn).replace(/[^\d]/g, "") === "13", "draft amount lost: " + amountAfterWarn);
      await page.locator("[data-testid=offer-message-error]").getByRole("button", { name: "بستن" }).click(); await page.waitForTimeout(300);
      await fill(String(serverUsd));
      await page.selectOption("[data-testid=offer-price-currency-select]", "EUR"); await page.waitForTimeout(600);
      const eurEmpty = await page.inputValue("[data-testid=offer-price-input]");
      check(eurEmpty === "", "EUR new currency should start empty: " + eurEmpty);
      await fill("20.5"); await saveBtn().click(); await page.waitForTimeout(1800);
      await setCurrency("USD");
      const usdBack = await page.inputValue("[data-testid=offer-price-input]");
      check(norm(usdBack).replace(/[^\d.]/g, "") === String(serverUsd), "USD amount not restored: " + usdBack);
      const d = await detail(offerId, actor);
      check(d.prices.find(x => x.currency === "USD").amount === serverUsd, "USD was overwritten by the blocked switch");
      check(d.prices.find(x => x.currency === "EUR")?.amount === 20.5, "EUR save missing");
      return { serverUsd, eur: 20.5 };
    });

    // R11-UAT-01
    await run("R11-UAT-01", async () => {
      await open("fa");
      await page.click("[data-testid=offer-tab-button-inventory]");
      await page.waitForSelector("[data-testid=offer-tab-inventory]", { timeout: 15000 });
      const hasInput = await page.locator("[data-testid=offer-inventory-input]").count();
      if (hasInput === 0) {
        await page.waitForSelector("[data-testid=offer-inventory-location-select]", { timeout: 15000 });
        const sel = page.locator("[data-testid=offer-inventory-location-select]");
        const values = await sel.locator("option").evaluateAll(opts => opts.map(o => o.value).filter(Boolean));
        check(values.length > 0, "no eligible inventory location");
        await sel.selectOption(values[0]);
        await page.click("[data-testid=offer-inventory-add-position]");
        await page.waitForSelector("[data-testid=offer-inventory-input]", { timeout: 15000 });
      }
      await page.fill("[data-testid=offer-inventory-input]", "50");
      await page.fill("[data-testid=offer-inventory-reason]", "شمارش انبار");
      await saveBtn().click(); await page.waitForTimeout(1800);
      const db = psqlJson(`select jsonb_build_object('onHand',coalesce(sum(on_hand),0),'positions',count(*)) from inventory.stock_positions where offer_id='${offerId}'`);
      check(Number(db.onHand) === 50 && Number(db.positions) === 1, "inventory not persisted: " + JSON.stringify(db));
      await page.reload({ waitUntil: "load" }); await page.waitForSelector("[data-testid=offer-workspace]", { timeout: 30000 });
      await page.click("[data-testid=offer-tab-button-inventory]"); await page.waitForTimeout(600);
      const inputValue = await page.inputValue("[data-testid=offer-inventory-input]");
      check(norm(inputValue).replace(/[^\d]/g, "") === "50", "reload inventory value wrong: " + inputValue);
      return { db, inputValue };
    });

    // R11-UAT-03
    await run("R11-UAT-03", async () => {
      const GEOM_OFFER = "2806d20e-d151-e95e-442b-a2acbb681a15";
      const geomLabel = (await detail(GEOM_OFFER, actor)).variantLabel ?? "";
      const geom = [];
      for (const width of [390, 1440]) {
        await page.setViewportSize({ width, height: 844 });
        await open("fa", GEOM_OFFER, false); await page.waitForTimeout(500);
        const identity = await page.evaluate((label) => {
          const root = document.querySelector("[data-testid=offer-workspace]");
          const header = document.querySelector("[data-testid=offer-workspace-header]");
          const tab = document.querySelector("[data-testid=offer-tab-identity]");
          const count = (text) => label ? (text.split(label).length - 1) : 0;
          const candidates = label ? Array.from(tab?.querySelectorAll("div") ?? []).filter(el => (el.textContent ?? "").includes(label)) : [];
          const smallest = candidates.sort((a, b) => (a.textContent?.length ?? 0) - (b.textContent?.length ?? 0))[0];
          return { overflow: document.documentElement.scrollWidth - document.documentElement.clientWidth, dir: root?.getAttribute("dir"), headerCount: count(header?.textContent ?? ""), fieldCount: smallest ? count(smallest.textContent ?? "") : 0, fieldText: (smallest?.textContent ?? "").slice(0, 180) };
        }, geomLabel);
        check(identity.overflow <= 0, "overflow at " + width + ": " + identity.overflow);
        check(identity.dir === "rtl", "dir at " + width);
        check(identity.headerCount <= 1 && identity.fieldCount === 1, "duplicate variant label at " + width + " h=" + identity.headerCount + " f=" + identity.fieldCount + " t=" + identity.fieldText);
        await pricing();
        const headers = await page.$$eval("[data-testid=offer-tab-pricing] thead th", els => els.map(e => e.textContent?.trim() ?? ""));
        check(!headers.some(h => /بازار|market/i.test(h)), "market column present: " + headers.join(","));
        const box = await page.locator("[data-testid=offer-workspace]").boundingBox();
        check(box && box.width <= width + 1, "workspace wider than viewport");
        geom.push({ width, ...identity, headers, box });
      }
      return { geomOffer: GEOM_OFFER, label: geomLabel, geom };
    });

    out.status = Object.values(out.cases).every(x => x.status === "PASS") ? "PASS" : "FAIL"; save();
  } catch (error) {
    out.status = "FAIL"; out.error = String(error && error.stack ? error.stack : error); save();
  } finally {
    if (browser) { try { await browser.close(); } catch { /* ignore */ } }
    try {
      const list = out.fixtures.map(quote).join(",");
      if (list) {
        const outbox = out.fixtures.map(id => `strpos(payload::text,${quote(id)})>0`).join(" OR ");
        psql(`BEGIN; DELETE FROM inventory.stock_positions WHERE offer_id IN (${list}); DELETE FROM inventory.offer_inventory_changes WHERE offer_id IN (${list}); DELETE FROM inventory.offer_allocations WHERE offer_id IN (${list}); DELETE FROM pricing.offer_price_changes WHERE offer_id IN (${list}); DELETE FROM pricing.prices WHERE offer_id IN (${list}); DELETE FROM pricing.outbox_messages WHERE ${outbox}; DELETE FROM offer.offer_history WHERE offer_id IN (${list}); DELETE FROM offer.offers WHERE offer_id IN (${list}); DELETE FROM offer.outbox_messages WHERE ${outbox}; DELETE FROM inventory.outbox_messages WHERE ${outbox}; COMMIT;`);
        out.cleanup = { offers: Number(psql(`select count(*) from offer.offers where offer_id in (${list})`)), prices: Number(psql(`select count(*) from pricing.prices where offer_id in (${list})`)) };
      }
    } catch (e) { out.cleanupError = String(e); }
    save();
  }
  console.log(JSON.stringify({ stage: "browser", status: out.status, cases: Object.fromEntries(Object.entries(out.cases).map(([k, v]) => [k, v.status])), cleanup: out.cleanup }));
  if (out.status !== "PASS") process.exit(1);
}
main().catch(e => { console.error(e); process.exit(1); });
