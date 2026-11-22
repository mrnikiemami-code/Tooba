const { chromium } = require('playwright');
const { execFileSync } = require('node:child_process');
const fs = require('node:fs');
const path = require('node:path');

const DB = 'tooba_offer285_connected';
const HOST = 'http://127.0.0.1:5091';
const VIEW_HOST = 'http://127.0.0.1:5092';
const FE = 'http://127.0.0.1:3101';
const ADMIN = '01a2045c-9d31-7000-add1-1dfc712f11b2';
const VIEW_ADMIN = '66666666-6666-4666-8666-666666666666';
const SELLER_A = '01a2045c-9b4a-7000-ba31-538e7f3c3166';
const SELLER_A_ACTOR = '01a2045c-9c6e-7000-94e8-2fd465b302fd';
const SELLER_B = '01a2045c-9b4d-7000-8a84-562cac7c80e7';
const SELLER_B_ACTOR = '01a2045c-9cd5-7000-9853-9213b2936510';
const checks = [];
const results = { startedAt: new Date().toISOString(), environment: { db: DB, host: HOST, viewHost: VIEW_HOST, fe: FE }, checks, scenarios: {} };
function check(name, ok, observed) { checks.push({ name, ok: Boolean(ok), observed }); if (!ok) throw new Error(`${name}: ${JSON.stringify(observed)}`); }
function sql(q) { return execFileSync('docker', ['exec', 'postgres-db', 'psql', '-U', 'admin', '-d', DB, '-At', '-v', 'ON_ERROR_STOP=1', '-c', q], { encoding: 'utf8' }).trim(); }
function scalar(q) { return sql(q).split(/\r?\n/)[0] ?? ''; }
function headers(extra = {}) { return { Accept: 'application/json', 'Content-Type': 'application/json', ...extra }; }
function adminHeaders(actor = ADMIN) { return headers({ 'X-Tooba-Dev-Actor-User-Id': actor }); }
function sellerHeaders(actor = SELLER_A_ACTOR, seller = SELLER_A) { return headers({ 'X-Tooba-Seller-Party-Id': seller, 'X-Tooba-Dev-Actor-User-Id': actor }); }
async function jsonFetch(url, init) { const r = await fetch(url, init); const text = await r.text(); let body = null; try { body = JSON.parse(text); } catch {} return { status: r.status, body, text }; }
function mutationsFor(page) { const rows = []; page.on('request', r => { if (!/\/v1\/(seller|admin)\/offers/.test(r.url())) return; if (!['POST','PUT','PATCH','DELETE'].includes(r.method())) return; rows.push({ method: r.method(), url: r.url(), body: r.postData() }); }); return rows; }
function businessWrites(rows) { return rows.filter(r => !/\/offers\/query$/.test(r.url)); }
function variantId(code) { return scalar(`select v.variant_id from catalog.variants v where v.catalog_code_seam='${code}'`); }
function offerRow(sku) { const line = scalar(`select offer_id||'|'||seller_party_id||'|'||catalog_variant_id||'|'||status from offer.offers where seller_sku='${sku}'`); if (!line) return null; const [offerId, sellerPartyId, catalogVariantId, status] = line.split('|'); return { offerId, sellerPartyId, catalogVariantId, status }; }
function offerCount(q) { return Number(scalar(q)); }

(async () => {
  const browser = await chromium.launch({ headless: true });
  try {
    // REQ-01 admin creates Draft for seller A; seller A sees it, B does not; admin edit price allowed.
    {
      const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 }, locale: 'fa-IR' });
      await ctx.addInitScript(({ a }) => localStorage.setItem('tooba.adminActorUserId', a), { a: ADMIN });
      const page = await ctx.newPage();
      const writes = mutationsFor(page);
      await page.goto(`${FE}/fa/admin/offers/new`, { waitUntil: 'networkidle' });
      await page.getByTestId('admin-offer-seller').filter({ hasText: 'فروشگاه آرمان' }).click();
      await page.getByTestId('admin-offer-variant-search').fill('TB285-VAR-174');
      await page.getByTestId('admin-offer-variant-search-submit').click();
      await page.waitForFunction(() => document.querySelectorAll('[data-testid="admin-offer-variant"]').length === 1);
      await page.getByTestId('admin-offer-variant').first().click();
      await page.getByTestId('admin-offer-sku').fill('TB285-FINAL-ADMIN-174');
      await page.getByTestId('admin-offer-submit').click();
      await page.waitForURL(/\/fa\/admin\/offers\/[0-9a-f-]+/, { timeout: 30000 });
      const id = page.url().split('/').filter(Boolean).pop();
      const row = offerRow('TB285-FINAL-ADMIN-174');
      const createdRequests = writes.filter(r => r.method === 'POST' && r.url.endsWith('/v1/admin/offers'));
      check('admin-create-one-request', createdRequests.length === 1, createdRequests);
      check('admin-create-row-seller', row && row.offerId === id && row.sellerPartyId === SELLER_A && row.status === 'Draft', row);
      const sellerGet = await jsonFetch(`${HOST}/v1/seller/offers/${id}`, { headers: sellerHeaders() });
      check('seller-A-reads-admin-offer', sellerGet.status === 200 && sellerGet.body.sellerPartyId === SELLER_A, { status: sellerGet.status, body: sellerGet.body });
      const sellerBGet = await jsonFetch(`${HOST}/v1/seller/offers/${id}`, { headers: sellerHeaders(SELLER_B_ACTOR, SELLER_B) });
      check('seller-B-cannot-read-A', sellerBGet.status === 404, sellerBGet.status);
      const adminPrice = await jsonFetch(`${HOST}/v1/admin/offers/${id}/price`, { method: 'POST', headers: adminHeaders(), body: JSON.stringify({ amount: 1500000 }) });
      check('admin-edit-price-allowed', adminPrice.status === 200, { status: adminPrice.status, body: adminPrice.body });
      const afterPrice = await jsonFetch(`${HOST}/v1/seller/offers/${id}`, { headers: sellerHeaders() });
      check('admin-price-visible-to-seller', afterPrice.body.amount === 1500000, afterPrice.body.amount);
      const viewCreate = await jsonFetch(`${VIEW_HOST}/v1/admin/offers`, { method: 'POST', headers: adminHeaders(VIEW_ADMIN), body: JSON.stringify({ sellerPartyId: SELLER_A, catalogVariantId: variantId('TB285-VAR-173'), sellerSku: 'VIEW-DENY-185', status: 'Draft', requestId: 'tb285-view-deny' }) });
      const viewPrice = await jsonFetch(`${VIEW_HOST}/v1/admin/offers/${id}/price`, { method: 'POST', headers: adminHeaders(VIEW_ADMIN), body: JSON.stringify({ amount: 1 }) });
      const viewList = await jsonFetch(`${VIEW_HOST}/v1/admin/offers/query`, { method: 'POST', headers: adminHeaders(VIEW_ADMIN), body: JSON.stringify({ page: 1, pageSize: 1, sorts: [], filters: [] }) });
      const bPrice = await jsonFetch(`${HOST}/v1/seller/offers/${id}/price`, { method: 'PUT', headers: sellerHeaders(SELLER_B_ACTOR, SELLER_B), body: JSON.stringify({ amount: 1 }) });
      check('admin-view-only-denied', viewCreate.status === 403 && viewPrice.status === 403 && viewList.status === 200, { viewCreate: viewCreate.status, viewPrice: viewPrice.status, viewList: viewList.status });
      check('seller-B-denied-price', bPrice.status === 403 || bPrice.status === 404, bPrice.status);
      results.scenarios.REQ01 = { id, row, sellerGet: sellerGet.body, sellerBStatus: sellerBGet.status, adminPriceStatus: adminPrice.status, viewCreate: viewCreate.status, viewPrice: viewPrice.status, viewList: viewList.status, bPrice: bPrice.status, writes };
      await ctx.close();
    }

    // REQ-03 same request id after a lost response returns the original Offer; one SKU row.
    {
      const ctx = await browser.newContext({ viewport: { width: 1280, height: 800 }, locale: 'fa-IR' });
      await ctx.addInitScript(({ a, p }) => { localStorage.setItem('tooba.sellerActorUserId', a); localStorage.setItem('tooba.sellerPartyId', p); }, { a: SELLER_A_ACTOR, p: SELLER_A });
      const page = await ctx.newPage();
      const writes = mutationsFor(page);
      await page.goto(`${FE}/fa/vendor-panel/products/new`, { waitUntil: 'networkidle' });
      await page.getByTestId('seller-catalog-search').fill('TB285-VAR-172');
      await page.getByTestId('seller-catalog-search-submit').click();
      await page.waitForFunction(() => document.querySelectorAll('[data-testid="seller-catalog-result"]').length === 1);
      await page.getByTestId('seller-catalog-result').first().click();
      await page.getByTestId('seller-create-sku').fill('TB285-FINAL-LOST-172');
      await page.getByTestId('seller-create-price').fill('1300000');
      await page.getByTestId('seller-create-stock').fill('2');
      let dropped = false;
      await page.route('**/v1/seller/offers', async route => { if (!dropped && route.request().method() === 'POST') { dropped = true; await route.fetch(); return route.abort('failed'); } return route.continue(); });
      await page.getByTestId('seller-create-submit').click();
      await page.getByTestId('seller-create-error').waitFor({ timeout: 15000 });
      await page.unroute('**/v1/seller/offers');
      await page.getByTestId('seller-create-submit').click();
      await page.waitForURL(/\/fa\/vendor-panel\/products\/[0-9a-f-]+/, { timeout: 30000 });
      const id = page.url().split('/').filter(Boolean).pop();
      const posts = writes.filter(r => r.method === 'POST' && r.url.endsWith('/v1/seller/offers'));
      const rowCount = offerCount(`select count(*) from offer.offers where seller_sku='TB285-FINAL-LOST-172'`);
      const row = offerRow('TB285-FINAL-LOST-172');
      const get = await jsonFetch(`${HOST}/v1/seller/offers/${id}`, { headers: sellerHeaders() });
      check('lost-create-same-id', row && row.offerId === id && posts.length === 2 && new Set(posts.map(r => JSON.parse(r.body || '{}').requestId)).size === 1 && rowCount === 1, { row, rowCount, posts });
      check('lost-create-price-stock', get.body.amount === 1300000 && get.body.onHand === 2, { amount: get.body.amount, onHand: get.body.onHand });
      results.scenarios.REQ03 = { id, row, rowCount, requests: posts, get: get.body, writes };
      await ctx.close();
    }

    // REQ-06 cancel, wrong response, honest partial and retry.
    {
      const ctx = await browser.newContext({ viewport: { width: 1280, height: 800 }, locale: 'fa-IR' });
      await ctx.addInitScript(({ a, p }) => { localStorage.setItem('tooba.sellerActorUserId', a); localStorage.setItem('tooba.sellerPartyId', p); }, { a: SELLER_A_ACTOR, p: SELLER_A });
      const page = await ctx.newPage();
      const cancelWrites = mutationsFor(page);
      await page.goto(`${FE}/fa/vendor-panel/products/new`, { waitUntil: 'networkidle' });
      await page.getByTestId('seller-create-sku').fill('CANCEL-285');
      await page.getByTestId('seller-create-cancel').click();
      await page.waitForTimeout(400);
      check('cancel-zero-business-writes', businessWrites(cancelWrites).length === 0, businessWrites(cancelWrites));
      await page.goto(`${FE}/fa/vendor-panel/products/new`, { waitUntil: 'networkidle' });
      await page.getByTestId('seller-catalog-search').fill('TB285-VAR-170');
      await page.getByTestId('seller-catalog-search-submit').click();
      await page.waitForFunction(() => document.querySelectorAll('[data-testid="seller-catalog-result"]').length === 1);
      await page.getByTestId('seller-catalog-result').first().click();
      await page.getByTestId('seller-create-sku').fill('TB285-FINAL-PARTIAL-170');
      await page.getByTestId('seller-create-price').fill('1600000');
      await page.getByTestId('seller-create-stock').fill('1');
      await page.route('**/v1/seller/offers/*/price', async route => route.fulfill({ status: 500, contentType: 'application/json', body: '{"errorCode":"controlled.price.failure"}' }));
      await page.getByTestId('seller-create-submit').click();
      await page.getByTestId('seller-create-partial').waitFor({ timeout: 20000 });
      const partialText = await page.getByTestId('seller-create-partial').innerText();
      await page.unroute('**/v1/seller/offers/*/price');
      await page.getByTestId('seller-create-submit').click();
      await page.waitForURL(/\/fa\/vendor-panel\/products\/[0-9a-f-]+/, { timeout: 30000 });
      const partialId = page.url().split('/').filter(Boolean).pop();
      const partialGet = await jsonFetch(`${HOST}/v1/seller/offers/${partialId}`, { headers: sellerHeaders() });
      check('partial-explicit-and-recovered', partialText.includes('پیش‌نویس') && partialGet.status === 200 && partialGet.body.amount === 1600000 && partialGet.body.onHand === 1, { partialText, partialGet: partialGet.body });
      // Wrong seller response cannot claim success and cannot write.
      const beforeWrong = offerCount(`select count(*) from offer.offers`);
      const ctxWrong = await browser.newContext({ viewport: { width: 1280, height: 800 }, locale: 'fa-IR' });
      await ctxWrong.addInitScript(({ a, p }) => { localStorage.setItem('tooba.sellerActorUserId', a); localStorage.setItem('tooba.sellerPartyId', p); }, { a: SELLER_A_ACTOR, p: SELLER_A });
      const wrongPage = await ctxWrong.newPage();
      await wrongPage.goto(`${FE}/fa/vendor-panel/products/new`, { waitUntil: 'networkidle' });
      await wrongPage.getByTestId('seller-catalog-search').fill('TB285-VAR-168');
      await wrongPage.getByTestId('seller-catalog-search-submit').click();
      await wrongPage.waitForFunction(() => document.querySelectorAll('[data-testid="seller-catalog-result"]').length === 1);
      await wrongPage.getByTestId('seller-catalog-result').first().click();
      await wrongPage.getByTestId('seller-create-sku').fill('TB285-FINAL-WRONG-168');
      await wrongPage.route('**/v1/seller/offers', async route => { if (route.request().method() === 'POST') { const body = JSON.parse(route.request().postData() || '{}'); return route.fulfill({ status: 201, contentType: 'application/json', body: JSON.stringify({ offerId: '11111111-1111-4111-8111-111111111111', sellerPartyId: SELLER_B, catalogVariantId: body.catalogVariantId, status: 'Draft', productTitle: 'WRONG', amount: null, currency: 'IRR', onHand: 0, reserved: 0, availableUnits: 0 }) }); } return route.continue(); });
      await wrongPage.getByTestId('seller-create-submit').click();
      await wrongPage.getByTestId('seller-create-error').waitFor({ timeout: 15000 });
      const wrongNotice = await wrongPage.getByTestId('seller-create-notice').count();
      const afterWrong = offerCount(`select count(*) from offer.offers`);
      check('wrong-response-no-success-no-write', wrongNotice === 0 && afterWrong === beforeWrong, { wrongNotice, beforeWrong, afterWrong });
      results.scenarios.REQ06 = { partialText, partialId, partialGet: partialGet.body, wrongNotice, beforeWrong, afterWrong, cancelWrites: businessWrites(cancelWrites) };
      await ctxWrong.close();
      await ctx.close();
    }

    // REQ-04 old published variant beyond newest 100, server search and selection across paging.
    {
      const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 }, locale: 'fa-IR' });
      await ctx.addInitScript(({ a }) => localStorage.setItem('tooba.adminActorUserId', a), { a: ADMIN });
      const page = await ctx.newPage();
      const requests = [];
      page.on('request', r => { if (r.url().includes('/v1/admin/catalog/variants')) requests.push(r.url()); });
      await page.goto(`${FE}/fa/admin/offers/new`, { waitUntil: 'networkidle' });
      await page.getByTestId('admin-offer-variant-search').fill('TB285-OLD-VAR');
      await page.getByTestId('admin-offer-variant-search-submit').click();
      await page.waitForFunction(() => document.querySelectorAll('[data-testid="admin-offer-variant"]').length === 1);
      const oldText = await page.getByTestId('admin-offer-variant').first().innerText();
      await page.getByTestId('admin-offer-variant').first().click();
      const selectedBefore = await page.getByTestId('admin-offer-variant-page').innerText();
      await page.getByTestId('admin-offer-variant-search').fill('');
      await page.getByTestId('admin-offer-variant-search-submit').click();
      await page.waitForFunction(() => document.querySelectorAll('[data-testid="admin-offer-variant"]').length === 20);
      const selectedAfter = await page.getByTestId('admin-offer-variant-page').innerText();
      const oldProduct = scalar(`select extract(year from p.updated_at)::int from catalog.products p where p.slug_seam='tb285-old-product'`);
      check('old-variant-server-search', oldText.includes('TB285-OLD-VAR') && oldProduct === '2020', { oldText, oldProduct });
      check('picker-selection-retained', selectedBefore.split('·').at(-1)?.trim() === selectedAfter.split('·').at(-1)?.trim(), { selectedBefore, selectedAfter });
      results.scenarios.REQ04 = { oldText, oldProduct, selectedBefore, selectedAfter, requests };
      await ctx.close();
    }

    // REQ-05 server paging/search on seller and admin lists.
    {
      const out = {};
      for (const role of ['seller', 'admin']) {
        const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 }, locale: 'fa-IR' });
        await ctx.addInitScript(({ a, p, admin }) => { localStorage.setItem('tooba.sellerActorUserId', a); localStorage.setItem('tooba.sellerPartyId', p); localStorage.setItem('tooba.adminActorUserId', admin); }, { a: SELLER_A_ACTOR, p: SELLER_A, admin: ADMIN });
        const page = await ctx.newPage();
        const requests = [];
        page.on('request', r => { if (r.url().includes(role === 'seller' ? '/v1/seller/offers/query' : '/v1/admin/offers/query')) requests.push(JSON.parse(r.postData() || '{}')); });
        await page.goto(role === 'seller' ? `${FE}/fa/vendor-panel/products` : `${FE}/fa/admin/offers`, { waitUntil: 'networkidle' });
        await page.waitForTimeout(600);
        const firstRows = await page.locator('tbody tr').count();
        await page.getByRole('button', { name: 'بعدی' }).click();
        await page.waitForTimeout(500);
        const page2 = requests.at(-1);
        const search = page.locator('input[placeholder="جستجو"]');
        await search.fill('TB285-SKU-137');
        await search.press('Enter');
        await page.waitForTimeout(600);
        const rowsAfterSearch = await page.locator('tbody tr').count();
        const totalText = await page.locator('body').innerText();
        out[role] = { firstRows, page2, rowsAfterSearch, totalText: totalText.slice(0, 200), requests };
        check(`${role}-server-page2`, page2?.page === 2, page2);
        check(`${role}-server-search`, rowsAfterSearch === 1, rowsAfterSearch);
        await ctx.close();
      }
      results.scenarios.REQ05 = out;
    }

    // REQ-07 fa/en, desktop/mobile, 44px and no horizontal overflow.
    {
      const out = [];
      for (const width of [1440, 360]) {
        for (const locale of ['fa', 'en']) {
          const ctx = await browser.newContext({ viewport: { width, height: width === 360 ? 640 : 900 }, locale: locale === 'fa' ? 'fa-IR' : 'en-US' });
          await ctx.addInitScript(({ a, p, admin }) => { localStorage.setItem('tooba.sellerActorUserId', a); localStorage.setItem('tooba.sellerPartyId', p); localStorage.setItem('tooba.adminActorUserId', admin); }, { a: SELLER_A_ACTOR, p: SELLER_A, admin: ADMIN });
          const page = await ctx.newPage();
          await page.goto(`${FE}/${locale}/admin/offers/new`, { waitUntil: 'networkidle' });
          const adminGeom = await page.evaluate(() => ({ iw: innerWidth, sw: document.documentElement.scrollWidth, dir: document.documentElement.dir, lang: document.documentElement.lang }));
          const adminBox = await page.getByTestId('admin-offer-submit').boundingBox();
          await page.goto(`${FE}/${locale}/vendor-panel/products/new`, { waitUntil: 'networkidle' });
          const sellerGeom = await page.evaluate(() => ({ iw: innerWidth, sw: document.documentElement.scrollWidth, dir: document.documentElement.dir, lang: document.documentElement.lang }));
          const sellerBox = await page.getByTestId('seller-create-submit').boundingBox();
          out.push({ width, locale, adminGeom, adminBox, sellerGeom, sellerBox });
          check(`geometry-${width}-${locale}`, adminGeom.sw === adminGeom.iw && sellerGeom.sw === sellerGeom.iw && adminBox.height === 44 && sellerBox.height === 44 && (locale === 'fa' ? adminGeom.dir === 'rtl' : adminGeom.dir === 'ltr'), { adminGeom, sellerGeom, adminBox, sellerBox });
          await ctx.close();
        }
      }
      results.scenarios.REQ07 = out;
    }

    // SQL snapshot of the final isolated state.
    results.scenarios.REQ08 = {
      offerCount: offerCount('select count(*) from offer.offers'),
      sellerAOffers: offerCount(`select count(*) from offer.offers where seller_party_id='${SELLER_A}'`),
      sellerBOffers: offerCount(`select count(*) from offer.offers where seller_party_id='${SELLER_B}'`),
      oldProductYear: scalar(`select extract(year from updated_at)::int from catalog.products where slug_seam='tb285-old-product'`),
      oldOfferCount: offerCount(`select count(*) from offer.offers where seller_sku='TB285-FINAL-ADMIN-174'`),
    };
    results.finishedAt = new Date().toISOString();
    results.ok = checks.every(c => c.ok);
    const target = path.join(__dirname, 'connected-285.json');
    fs.writeFileSync(target, JSON.stringify(results, null, 2), 'utf8');
    console.log(JSON.stringify({ ok: results.ok, checks: checks.length, target, scenarios: Object.keys(results.scenarios) }, null, 2));
    process.exitCode = results.ok ? 0 : 1;
  } finally {
    await browser.close();
  }
})().catch(error => { console.error(error); process.exitCode = 1; });

