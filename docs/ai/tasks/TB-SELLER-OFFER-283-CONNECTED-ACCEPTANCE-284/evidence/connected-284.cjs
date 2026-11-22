/*
 * TB-284 connected acceptance. Real Chromium -> served Next 3100 -> real Host 5090 -> disposable PostgreSQL.
 * This file is evidence, not a production test target. It writes only to tooba_offer283_connected.
 */
const { chromium } = require("playwright");
const { execFileSync } = require("node:child_process");
const fs = require("node:fs");
const path = require("node:path");

const DB = "tooba_offer283_connected";
const HOST = "http://127.0.0.1:5090";
const FE = "http://127.0.0.1:3100";
const OFFER = "01a2041e-bb6b-7000-9163-d736d71634d5";
const SELLER = "01a2041e-bb65-7000-9b34-1d049b3a788a";
const ACTOR = "01a2041e-bca5-7000-87e7-f621a913c987";
const POS_A = "01a2041e-bbe0-7000-9897-006e14fe11c0";
const POS_B = "01a2041e-bbd3-7000-9fe5-0e7d2157003e";
const FOREIGN = "01a2041e-bbef-7000-b845-d6f8aa14acdd";
const URL = `${FE}/fa/vendor-panel/products/${OFFER}`;
const HEADERS = {
  Accept: "application/json",
  "Content-Type": "application/json",
  "X-Tooba-Seller-Party-Id": SELLER,
  "X-Tooba-Dev-Actor-User-Id": ACTOR,
};
const out = { task: "TB-SELLER-OFFER-283-CONNECTED-ACCEPTANCE-284", startedAt: new Date().toISOString(), environment: { db: DB, host: HOST, fe: FE, offer: OFFER, seller: SELLER, actor: ACTOR, positions: [POS_A, POS_B] }, scenarios: [], checks: [] };

function sql(statement) {
  return execFileSync("docker", ["exec", "postgres-db", "psql", "-U", "admin", "-d", DB, "-At", "-v", "ON_ERROR_STOP=1", "-c", statement], { encoding: "utf8" }).trim();
}
function state() {
  return JSON.parse(sql(`select json_build_object(
    'sku', o.seller_sku,
    'status', o.status,
    'price', p.amount::text,
    'min', o.minimum_order_quantity::text,
    'max', o.maximum_order_quantity::text,
    'positions', (select json_agg(json_build_object('id', sp.stock_item_id::text, 'onHand', sp.on_hand::text, 'reserved', sp.reserved::text) order by sp.stock_item_id) from inventory.stock_positions sp where sp.offer_id = o.offer_id),
    'reservations', (select json_agg(json_build_object('id', r.reservation_id::text, 'qty', r.quantity::text, 'status', r.status) order by r.reservation_id) from inventory.reservations r where r.stock_item_id in (select stock_item_id from inventory.stock_positions where offer_id = o.offer_id)),
    'outbox', (select count(*) from inventory.outbox_messages)
  )::text from offer.offers o join pricing.prices p on p.offer_id = o.offer_id where o.offer_id = '${OFFER}'`));
}
function reset() {
  sql(`update offer.offers set seller_sku='ARM-LN-01', status='Active', minimum_order_quantity=null, maximum_order_quantity=null where offer_id='${OFFER}';
       update pricing.prices set amount=2500000, updated_at=now() where offer_id='${OFFER}';
       update inventory.stock_positions set on_hand=3, reserved=1, updated_at=now() where stock_item_id='${POS_A}';
       update inventory.stock_positions set on_hand=7, reserved=2, updated_at=now() where stock_item_id='${POS_B}';`);
}
async function detail() {
  const response = await fetch(`${HOST}/v1/seller/offers/${OFFER}`, { headers: HEADERS });
  const body = await response.json();
  return { status: response.status, body };
}
function check(id, condition, detailText) {
  out.checks.push({ id, ok: Boolean(condition), detail: detailText });
  if (!condition) throw new Error(`${id}: ${detailText}`);
}
function scenario(id, fn) {
  return { id, fn };
}
function mutationsFor(page) {
  const rows = [];
  page.on("request", request => {
    if (!/\/v1\/seller\/offers\//.test(request.url())) return;
    if (!["PATCH", "POST", "PUT", "DELETE"].includes(request.method())) return;
    rows.push({ method: request.method(), url: request.url(), body: request.postData() });
  });
  return rows;
}
function parseMoney(value) {
  return Number(String(value).replace(/[^\d]/g, ""));
}
function positionOf(s, id) {
  return s.positions.find(row => row.id === id);
}

(async () => {
  const browser = await chromium.launch({ headless: true });
  try {
    const scenarios = [
      scenario("REQ-01", async () => {
        reset();
        const before = state();
        const context = await browser.newContext({ viewport: { width: 1280, height: 800 }, locale: "fa-IR" });
        const page = await context.newPage();
        const writes = mutationsFor(page);
        await page.goto(URL, { waitUntil: "networkidle" });
        await page.getByTestId("offer-inventory-position").first().waitFor();
        check("REQ-01-two-positions", await page.getByTestId("offer-inventory-position").count() === 2, "exactly two positions");
        await page.getByLabel("SKU فروشنده").fill("ARM-LN-01-C01");
        await Promise.all([
          page.waitForResponse(response => response.url().includes(`/v1/seller/offers/${OFFER}`) && response.request().method() === "PATCH"),
          page.getByTestId("offer-save").click(),
        ]);
        await page.getByTestId("offer-save-notice").waitFor({ timeout: 15000 });
        const after = state();
        const independent = await detail();
        const priceWrites = writes.filter(row => row.url.includes("/price"));
        const inventoryWrites = writes.filter(row => row.url.includes("/inventory"));
        check("REQ-01-one-patch", writes.filter(row => row.method === "PATCH" && row.url.endsWith(`/offers/${OFFER}`)).length === 1, JSON.stringify(writes));
        check("REQ-01-zero-owner-writes", priceWrites.length === 0 && inventoryWrites.length === 0, JSON.stringify({ priceWrites, inventoryWrites }));
        check("REQ-01-sku", after.sku === "ARM-LN-01-C01", after.sku);
        check("REQ-01-price", after.price === before.price && independent.body.amount === 2500000, JSON.stringify({ before: before.price, after: after.price, get: independent.body.amount }));
        check("REQ-01-positions", JSON.stringify(after.positions) === JSON.stringify(before.positions), JSON.stringify({ before: before.positions, after: after.positions }));
        check("REQ-01-reservations", JSON.stringify(after.reservations) === JSON.stringify(before.reservations), JSON.stringify({ before: before.reservations, after: after.reservations }));
        check("REQ-01-outbox", after.outbox === before.outbox, JSON.stringify({ before: before.outbox, after: after.outbox }));
        check("REQ-01-independent-get", independent.status === 200 && independent.body.sellerSku === "ARM-LN-01-C01" && independent.body.offerId === OFFER && independent.body.sellerPartyId === SELLER, JSON.stringify(independent));
        await context.close();
        return { before, after, independent, writes };
      }),
      scenario("REQ-03", async () => {
        reset();
        const before = state();
        const contextB = await browser.newContext({ viewport: { width: 1280, height: 800 }, locale: "fa-IR" });
        const pageB = await contextB.newPage();
        const writesB = mutationsFor(pageB);
        await pageB.goto(URL, { waitUntil: "networkidle" });
        await pageB.getByTestId("offer-inventory-position").first().waitFor();
        check("REQ-03-B-old-price", parseMoney(await pageB.getByLabel("قیمت بدون مالیات (ریال)").inputValue()) === 2500000, await pageB.getByLabel("قیمت بدون مالیات (ریال)").inputValue());

        const contextA = await browser.newContext({ viewport: { width: 1280, height: 800 }, locale: "fa-IR" });
        const pageA = await contextA.newPage();
        const writesA = mutationsFor(pageA);
        await pageA.goto(URL, { waitUntil: "networkidle" });
        await pageA.getByTestId("offer-inventory-position").first().waitFor();
        await pageA.getByLabel("قیمت بدون مالیات (ریال)").fill("2750000");
        await Promise.all([
          pageA.waitForResponse(response => response.url().endsWith(`/v1/seller/offers/${OFFER}/price`)),
          pageA.getByTestId("offer-save").click(),
        ]);
        await pageA.getByTestId("offer-save-notice").waitFor({ timeout: 15000 });
        const afterPrice = state();
        const afterPriceGet = await detail();
        check("REQ-03-A-one-price-write", writesA.filter(row => row.url.includes("/price")).length === 1, JSON.stringify(writesA));
        check("REQ-03-A-no-commercial-write", writesA.filter(row => row.method === "PATCH").length === 0, JSON.stringify(writesA));
        check("REQ-03-price-persisted", afterPrice.price === "2750000.0000" && afterPriceGet.body.amount === 2750000, JSON.stringify({ sql: afterPrice.price, get: afterPriceGet.body.amount }));
        check("REQ-03-B-still-stale", parseMoney(await pageB.getByLabel("قیمت بدون مالیات (ریال)").inputValue()) === 2500000, await pageB.getByLabel("قیمت بدون مالیات (ریال)").inputValue());

        await pageB.getByLabel("SKU فروشنده").fill("ARM-LN-01-C03");
        await Promise.all([
          pageB.waitForResponse(response => response.url().endsWith(`/v1/seller/offers/${OFFER}`) && response.request().method() === "PATCH"),
          pageB.getByTestId("offer-save").click(),
        ]);
        await pageB.getByTestId("offer-save-notice").waitFor({ timeout: 15000 });
        const after = state();
        const independent = await detail();
        check("REQ-03-B-one-commercial-write", writesB.filter(row => row.method === "PATCH").length === 1, JSON.stringify(writesB));
        check("REQ-03-B-zero-price-write", writesB.filter(row => row.url.includes("/price")).length === 0, JSON.stringify(writesB));
        check("REQ-03-both-persist", after.price === "2750000.0000" && after.sku === "ARM-LN-01-C03" && independent.body.amount === 2750000 && independent.body.sellerSku === "ARM-LN-01-C03", JSON.stringify({ sql: after, get: independent.body }));
        check("REQ-03-stock-unchanged", JSON.stringify(after.positions) === JSON.stringify(before.positions) && JSON.stringify(after.reservations) === JSON.stringify(before.reservations), JSON.stringify({ before, after }));
        await contextA.close();
        await contextB.close();
        return { before, afterPrice, after, independent, writesA, writesB };
      }),
      scenario("REQ-02", async () => {
        reset();
        sql(`update offer.offers set status='Draft', minimum_order_quantity=2, maximum_order_quantity=9 where offer_id='${OFFER}';`);
        const before = state();
        const context = await browser.newContext({ viewport: { width: 1280, height: 800 }, locale: "fa-IR" });
        const page = await context.newPage();
        const writes = mutationsFor(page);
        await page.goto(URL, { waitUntil: "networkidle" });
        await page.getByTestId("offer-inventory-position").first().waitFor();
        await page.getByLabel("SKU فروشنده").fill("ARM-LN-01-C02");
        await Promise.all([page.waitForResponse(response => response.request().method() === "PATCH"), page.getByTestId("offer-save").click()]);
        await page.getByTestId("offer-save-notice").waitFor({ timeout: 15000 });
        const afterDraft = state();
        check("REQ-02-draft-stays", afterDraft.status === "Draft" && afterDraft.sku === "ARM-LN-01-C02", JSON.stringify(afterDraft));
        await page.getByTestId("offer-min-qty").fill("");
        await page.getByTestId("offer-max-qty").fill("");
        await Promise.all([page.waitForResponse(response => response.request().method() === "PATCH"), page.getByTestId("offer-save").click()]);
        await page.getByTestId("offer-save-notice").waitFor({ timeout: 15000 });
        const after = state();
        check("REQ-02-limits-cleared", after.min === null && after.max === null, JSON.stringify(after));
        check("REQ-02-still-draft", after.status === "Draft" && after.sku === "ARM-LN-01-C02", JSON.stringify(after));
        await context.close();
        return { before, afterDraft, after, writes };
      }),
      scenario("REQ-04", async () => {
        reset();
        const before = state();
        const context = await browser.newContext({ viewport: { width: 1280, height: 800 }, locale: "fa-IR" });
        const page = await context.newPage();
        const writes = mutationsFor(page);
        await page.goto(URL, { waitUntil: "networkidle" });
        await page.getByTestId("offer-inventory-position").first().waitFor();
        await page.locator(`[data-stock-item-id="${POS_B}"] input`).fill("8");
        await Promise.all([page.waitForResponse(response => response.url().endsWith(`/v1/seller/offers/${OFFER}/inventory`)), page.getByTestId("offer-save").click()]);
        await page.getByTestId("offer-save-notice").waitFor({ timeout: 15000 });
        const after = state();
        const independent = await detail();
        check("REQ-04-targeted-position", positionOf(after, POS_B).onHand === "8.000000" && positionOf(after, POS_A).onHand === "3.000000", JSON.stringify(after.positions));
        check("REQ-04-total", Number(positionOf(after, POS_A).onHand) + Number(positionOf(after, POS_B).onHand) === 11, JSON.stringify(after.positions));
        check("REQ-04-other-state", after.sku === before.sku && after.price === before.price && JSON.stringify(after.reservations) === JSON.stringify(before.reservations), JSON.stringify({ before, after }));
        check("REQ-04-one-inventory-write", writes.filter(row => row.url.includes("/inventory")).length === 1 && writes.filter(row => row.method === "PATCH").length === 0, JSON.stringify(writes));
        const negativeBefore = state();
        const negative = [];
        for (const [name, body] of [
          ["ambiguous", { onHand: 5, reason: "tb284-negative" }],
          ["foreign", { onHand: 5, reason: "tb284-negative", stockItemId: FOREIGN }],
          ["below-reserved", { onHand: 0, reason: "tb284-negative", stockItemId: POS_A }],
        ]) {
          const response = await fetch(`${HOST}/v1/seller/offers/${OFFER}/inventory`, { method: "POST", headers: HEADERS, body: JSON.stringify(body) });
          negative.push({ name, status: response.status, body: await response.text() });
        }
        const negativeAfter = state();
        check("REQ-04-negatives-refused", negative.every(row => row.status >= 400), JSON.stringify(negative));
        check("REQ-04-negatives-zero-change", JSON.stringify(negativeAfter.positions) === JSON.stringify(negativeBefore.positions) && JSON.stringify(negativeAfter.reservations) === JSON.stringify(negativeBefore.reservations) && negativeAfter.outbox === negativeBefore.outbox, JSON.stringify({ negativeBefore, negativeAfter }));
        await context.close();
        return { before, after, independent, writes, negative, negativeBefore, negativeAfter };
      }),
      scenario("REQ-06", async () => {
        reset();
        const context = await browser.newContext({ viewport: { width: 360, height: 640 }, locale: "fa-IR" });
        const page = await context.newPage();
        const writes = mutationsFor(page);
        const responses = [];
        const pageErrors = [];
        page.on("response", response => { if (response.url().includes("/v1/")) responses.push([response.status(), response.url()]); });
        page.on("pageerror", error => pageErrors.push(String(error)));
        await page.goto(URL, { waitUntil: "networkidle" });
        const loaded = await page.getByTestId("offer-inventory-position").first().waitFor({ timeout: 10000 }).then(() => true).catch(() => false);
        if (!loaded) {
          const body = await page.locator("body").innerText().catch(() => "unreadable");
          throw new Error(JSON.stringify({ stage: "REQ-06-load", url: page.url(), responses, pageErrors, body: body.slice(0, 1200) }));
        }
        await page.getByLabel("SKU فروشنده").fill("ARM-LN-01-C06");
        const save = page.getByTestId("offer-save");
        const cancel = page.getByTestId("offer-cancel");
        await save.scrollIntoViewIfNeeded();
        const geometry = await page.evaluate(() => ({ innerWidth: window.innerWidth, scrollWidth: document.documentElement.scrollWidth, overflowX: document.documentElement.scrollWidth - window.innerWidth }));
        const boxes = { save: await save.boundingBox(), cancel: await cancel.boundingBox(), position: await page.getByTestId("offer-inventory-position").first().boundingBox() };
        check("REQ-06-no-horizontal-overflow", geometry.overflowX === 0, JSON.stringify(geometry));
        check("REQ-06-controls-visible", Boolean(boxes.save && boxes.cancel && boxes.position && boxes.save.width > 0 && boxes.cancel.width > 0 && boxes.position.width > 0), JSON.stringify(boxes));
        await cancel.click();
        await page.waitForTimeout(300);
        check("REQ-06-cancel-zero-writes", writes.length === 0, JSON.stringify(writes));
        await page.close();
        const reopened = await context.newPage();
        await reopened.goto(URL, { waitUntil: "networkidle" });
        await reopened.getByTestId("offer-inventory-position").first().waitFor();
        const reopenedSku = await reopened.getByLabel("SKU فروشنده").inputValue();
        check("REQ-06-old-value", reopenedSku === "ARM-LN-01", reopenedSku);
        await context.close();
        return { geometry, boxes, writes, skuAfterReopen: reopenedSku };
      }),
      scenario("REQ-07", async () => {
        reset();
        const context = await browser.newContext({ viewport: { width: 1280, height: 800 }, locale: "fa-IR" });
        const page = await context.newPage();
        const writes = mutationsFor(page);
        await page.goto(URL, { waitUntil: "networkidle" });
        await page.getByTestId("offer-inventory-position").first().waitFor();
        await page.getByTestId("offer-save").click();
        await page.waitForTimeout(400);
        check("REQ-07-no-change-zero-writes", writes.length === 0, JSON.stringify(writes));
        await page.route(`**/v1/seller/offers/${OFFER}`, async route => {
          if (route.request().method() === "PATCH") return route.fulfill({ status: 409, contentType: "application/json", body: JSON.stringify({ errorCode: "tb284.forced" }) });
          return route.continue();
        });
        await page.getByLabel("SKU فروشنده").fill("ARM-LN-01-C07");
        await page.getByLabel("قیمت بدون مالیات (ریال)").fill("2800000");
        await Promise.all([page.waitForResponse(response => response.request().method() === "PATCH"), page.getByTestId("offer-save").click()]);
        await page.getByTestId("offer-save-error").waitFor({ timeout: 15000 });
        const priceDraft = await page.getByLabel("قیمت بدون مالیات (ریال)").inputValue();
        check("REQ-07-second-draft-retained", parseMoney(priceDraft) === 2800000, priceDraft);
        await page.unroute(`**/v1/seller/offers/${OFFER}`);
        await Promise.all([page.waitForResponse(response => response.request().method() === "PATCH"), page.getByTestId("offer-save").click()]);
        await page.getByTestId("offer-save-notice").waitFor({ timeout: 15000 });
        const after = state();
        const independent = await detail();
        check("REQ-07-both-persist", after.sku === "ARM-LN-01-C07" && after.price === "2800000.0000" && independent.body.sellerSku === "ARM-LN-01-C07" && independent.body.amount === 2800000, JSON.stringify({ after, get: independent.body }));
        await context.close();
        return { writes, priceDraft, after, independent };
      }),
      scenario("REQ-05", async () => {
        reset();
        const before = state();
        const context = await browser.newContext({ viewport: { width: 1280, height: 800 }, locale: "fa-IR" });
        const page = await context.newPage();
        const writes = mutationsFor(page);
        await page.goto(URL, { waitUntil: "networkidle" });
        await page.getByTestId("offer-inventory-position").first().waitFor();
        await page.route(`**/v1/seller/offers/${OFFER}`, async route => {
          if (route.request().method() === "PATCH") {
            return route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify({ offerId: "01a2041e-0000-7000-8000-000000000000", sellerPartyId: SELLER, sellerSku: "WRONG", amount: 2500000, currency: "IRR", status: "Active", onHand: 10, reserved: 3, availableUnits: 7, inventoryLocations: [] }) });
          }
          return route.continue();
        });
        await page.getByLabel("SKU فروشنده").fill("ARM-LN-01-C05-WRONG");
        await Promise.all([page.waitForResponse(response => response.request().method() === "PATCH"), page.getByTestId("offer-save").click()]);
        await page.getByTestId("offer-save-error").waitFor({ timeout: 15000 });
        const wrongNotice = await page.getByTestId("offer-save-notice").count();
        check("REQ-05-wrong-no-success", wrongNotice === 0, JSON.stringify({ wrongNotice }));
        await page.unroute(`**/v1/seller/offers/${OFFER}`);
        const afterWrong = state();
        check("REQ-05-wrong-zero-change", JSON.stringify(afterWrong) === JSON.stringify(before), JSON.stringify({ before, afterWrong }));

        await page.route(`**/v1/seller/offers/${OFFER}`, async route => {
          if (route.request().method() === "PATCH") {
            await route.fetch();
            return route.abort("failed");
          }
          return route.continue();
        });
        await page.getByLabel("SKU فروشنده").fill("ARM-LN-01-C05-LOST");
        await Promise.all([page.waitForResponse(response => response.request().method() === "PATCH").catch(() => null), page.getByTestId("offer-save").click()]);
        await page.waitForTimeout(1500);
        await page.unroute(`**/v1/seller/offers/${OFFER}`);
        const afterLost = state();
        const independent = await detail();
        check("REQ-05-lost-write-persisted", afterLost.sku === "ARM-LN-01-C05-LOST" && independent.body.sellerSku === "ARM-LN-01-C05-LOST", JSON.stringify({ afterLost, get: independent.body }));
        check("REQ-05-no-automatic-second-write", writes.filter(row => row.method === "PATCH").length === 2, JSON.stringify(writes));
        await context.close();
        reset();
        return { before, afterWrong, afterLost, independent, writes };
      }),
    ];

    for (const row of scenarios) {
      const startedAt = new Date().toISOString();
      try {
        const result = await row.fn();
        out.scenarios.push({ id: row.id, status: "PASS", startedAt, finishedAt: new Date().toISOString(), result });
      } catch (error) {
        out.scenarios.push({ id: row.id, status: "FAIL", startedAt, finishedAt: new Date().toISOString(), error: String(error && error.stack || error) });
      }
    }
    out.finishedAt = new Date().toISOString();
    out.ok = out.scenarios.every(row => row.status === "PASS") && out.checks.every(row => row.ok);
    const target = path.join(__dirname, "connected-284.json");
    fs.writeFileSync(target, JSON.stringify(out, null, 2), "utf8");
    console.log(JSON.stringify({ ok: out.ok, scenarios: out.scenarios.map(row => ({ id: row.id, status: row.status, error: row.error })), checks: out.checks.length, target }, null, 2));
    process.exitCode = out.ok ? 0 : 1;
  } finally {
    await browser.close();
  }
})().catch(error => { console.error(error); process.exitCode = 1; });
