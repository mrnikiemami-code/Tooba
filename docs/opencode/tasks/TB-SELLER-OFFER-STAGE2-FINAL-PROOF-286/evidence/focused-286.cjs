const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const { execFileSync } = require('node:child_process');

const TASK = path.resolve(__dirname, '..');
const OUT = path.join(__dirname, 'focused-286.json');
const DB = 'tooba_offer286_proof';
const HOST = 'http://127.0.0.1:5093';
const CODE_SHA = '507d7933a54fe067c240beb6aeb62b63f4b110c0';
const HOST_DLL = 'D:\\temp\\tb285-repair-host\\Tooba.Host.dll';
const A = '01a205c6-16b2-7000-8745-69972e21d16f';
const A_ACTOR = '01a205c6-18b5-7000-bea8-bb68ac2c8c31';
const B = '01a205c6-16b6-7000-8f41-5b89016d834f';
const B_ACTOR = '01a205c6-1960-7000-a3c9-7880718e4310';
const ADMIN = '01a205c6-1a04-7000-a453-a019dfddf874';

const startedAt = new Date();
const scriptSha256 = crypto.createHash('sha256').update(fs.readFileSync(__filename)).digest('hex');
const hostSha256 = crypto.createHash('sha256').update(fs.readFileSync(HOST_DLL)).digest('hex');
const sql = q => execFileSync('docker', ['exec', 'postgres-db', 'psql', '-U', 'admin', '-d', DB, '-At', '-v', 'ON_ERROR_STOP=1', '-c', q], { encoding: 'utf8' }).trim();
const scalar = q => Number(sql(q));
const variant = code => sql(`select v.variant_id from catalog.variants v where v.catalog_code_seam='${code}'`);
const counts = () => ({
  offers: scalar('select count(*) from offer.offers'),
  prices: scalar('select count(*) from pricing.prices'),
  stockPositions: scalar('select count(*) from inventory.stock_positions'),
  reservations: scalar('select count(*) from inventory.reservations'),
});
const headers = (actor, seller) => ({
  Accept: 'application/json',
  'Content-Type': 'application/json',
  'X-Tooba-Dev-Actor-User-Id': actor,
  ...(seller ? { 'X-Tooba-Seller-Party-Id': seller } : {}),
});
const post = async (url, actor, seller, body) => {
  const r = await fetch(url, { method: 'POST', headers: headers(actor, seller), body: JSON.stringify(body) });
  const text = await r.text();
  let json = null;
  try { json = JSON.parse(text); } catch {}
  return { status: r.status, body: json, text };
};
const patch = async (url, actor, body) => {
  const r = await fetch(url, { method: 'PATCH', headers: headers(actor), body: JSON.stringify(body) });
  const text = await r.text();
  let json = null;
  try { json = JSON.parse(text); } catch {}
  return { status: r.status, body: json, text };
};
const get = async (url, actor, seller) => {
  const r = await fetch(url, { headers: headers(actor, seller) });
  const text = await r.text();
  let json = null;
  try { json = JSON.parse(text); } catch {}
  return { status: r.status, body: json, text };
};
const row = (seller, sku) => sql(`select offer_id||'|'||seller_sku||'|'||status from offer.offers where seller_party_id='${seller}' and seller_sku='${sku}'`);
const offerRows = (seller, sku) => scalar(`select count(*) from offer.offers where seller_party_id='${seller}' and seller_sku='${sku}'`);
const conflict = (seller, id, beforeSku, afterSku, before, after, response) => ({
  sellerPartyId: seller,
  originalOfferId: id,
  originalRows: offerRows(seller, beforeSku),
  secondRowCount: offerRows(seller, afterSku),
  responseStatus: response.status,
  errorCode: response.body?.errorCode ?? response.body?.title ?? null,
  priceWrites: after.prices - before.prices,
  stockPositionWrites: after.stockPositions - before.stockPositions,
  reservationWrites: after.reservations - before.reservations,
  before,
  after,
});

(async () => {
  const seller = (await fetch(`${HOST}/v1/seller/dev-contexts`)).ok ? JSON.parse(await (await fetch(`${HOST}/v1/seller/dev-contexts`)).text()) : null;
  const admin = JSON.parse(await (await fetch(`${HOST}/v1/admin/dev-context`)).text());
  const vSeller = variant('PHONE-BLK-128');
  const vAdmin = variant('LIVE-SCARF-NVY');
  const vConcurrent = variant('PHONE-BLU-256');
  const vLate = variant('PHONE-BLU-256');

  // V2 seller: identical replay, then changed SKU with no owning price/stock writes.
  const sellerBodyA = { catalogVariantId: vSeller, sellerSku: 'TB286-SELLER-A', status: 'Draft', requestId: 'tb286-v2-seller' };
  const sellerFirst = await post(`${HOST}/v1/seller/offers`, A_ACTOR, A, sellerBodyA);
  const sellerSecond = await post(`${HOST}/v1/seller/offers`, A_ACTOR, A, sellerBodyA);
  const sellerBeforeConflict = counts();
  const sellerConflictResponse = await post(`${HOST}/v1/seller/offers`, A_ACTOR, A, { ...sellerBodyA, sellerSku: 'TB286-SELLER-B' });
  const sellerAfterConflict = counts();

  // V2 admin: identical replay, then changed SKU with no owning price/stock writes.
  const adminBodyA = { sellerPartyId: B, catalogVariantId: vAdmin, sellerSku: 'TB286-ADMIN-A', status: 'Draft', requestId: 'tb286-v2-admin' };
  const adminFirst = await post(`${HOST}/v1/admin/offers`, ADMIN, null, adminBodyA);
  const adminSecond = await post(`${HOST}/v1/admin/offers`, ADMIN, null, adminBodyA);
  const adminBeforeConflict = counts();
  const adminConflictResponse = await post(`${HOST}/v1/admin/offers`, ADMIN, null, { ...adminBodyA, sellerSku: 'TB286-ADMIN-B' });
  const adminAfterConflict = counts();

  // V3: simultaneous identical seller creates.
  const concurrentBody = { catalogVariantId: vConcurrent, sellerSku: 'TB286-CONCURRENT', status: 'Draft', requestId: 'tb286-v3-concurrent' };
  const concurrentBefore = counts();
  const [concurrentFirst, concurrentSecond] = await Promise.all([
    post(`${HOST}/v1/seller/offers`, A_ACTOR, A, concurrentBody),
    post(`${HOST}/v1/seller/offers`, A_ACTOR, A, concurrentBody),
  ]);
  const concurrentAfter = counts();

  // V4: late replay after an authorized Offer-owned SKU edit.
  const lateBodyA = { sellerPartyId: B, catalogVariantId: vLate, sellerSku: 'TB286-LATE-A', status: 'Draft', requestId: 'tb286-v4-late' };
  const lateCreate = await post(`${HOST}/v1/admin/offers`, ADMIN, null, lateBodyA);
  const lateId = lateCreate.body?.offerId ?? null;
  const lateInitial = lateId ? await get(`${HOST}/v1/admin/offers/${lateId}`, ADMIN) : null;
  const latePatch = lateId ? await patch(`${HOST}/v1/admin/offers/${lateId}`, ADMIN, { sellerSku: 'TB286-LATE-B' }) : null;
  const lateEdited = lateId ? await get(`${HOST}/v1/admin/offers/${lateId}`, ADMIN) : null;
  const lateOriginalReplay = lateId ? await post(`${HOST}/v1/admin/offers`, ADMIN, null, lateBodyA) : null;
  const lateChangedReplay = lateId ? await post(`${HOST}/v1/admin/offers`, ADMIN, null, { ...lateBodyA, sellerSku: 'TB286-LATE-B' }) : null;
  const lateFinal = lateId ? await get(`${HOST}/v1/admin/offers/${lateId}`, ADMIN) : null;

  const evidence = {
    task: 'TB-SELLER-OFFER-STAGE2-FINAL-PROOF-286',
    protocol: 'TOOBA-OPENCODE-DELIVERY-V1@1.3.0',
    producer: {
      script: 'docs/opencode/tasks/TB-SELLER-OFFER-STAGE2-FINAL-PROOF-286/evidence/focused-286.cjs',
      scriptSha256,
      writesObservedJsonDirectly: true,
    },
    startedAt: startedAt.toISOString(),
    endedAt: new Date().toISOString(),
    elapsedMs: Date.now() - startedAt.getTime(),
    environment: {
      codeSha: CODE_SHA,
      host: HOST,
      database: DB,
      hostDllSha256: hostSha256,
      sellerA: A,
      sellerB: B,
      admin: ADMIN,
      sellerContexts: seller,
      adminContext: admin,
    },
    V2: {
      seller: {
        first: sellerFirst.status,
        second: sellerSecond.status,
        sameOfferId: Boolean(sellerFirst.body?.offerId && sellerFirst.body.offerId === sellerSecond.body?.offerId),
        offerId: sellerFirst.body?.offerId ?? null,
        original: row(A, 'TB286-SELLER-A'),
        conflict: conflict(A, sellerFirst.body?.offerId, 'TB286-SELLER-A', 'TB286-SELLER-B', sellerBeforeConflict, sellerAfterConflict, sellerConflictResponse),
      },
      admin: {
        first: adminFirst.status,
        second: adminSecond.status,
        sameOfferId: Boolean(adminFirst.body?.offerId && adminFirst.body.offerId === adminSecond.body?.offerId),
        offerId: adminFirst.body?.offerId ?? null,
        original: row(B, 'TB286-ADMIN-A'),
        conflict: conflict(B, adminFirst.body?.offerId, 'TB286-ADMIN-A', 'TB286-ADMIN-B', adminBeforeConflict, adminAfterConflict, adminConflictResponse),
      },
    },
    V3: {
      first: concurrentFirst.status,
      second: concurrentSecond.status,
      firstId: concurrentFirst.body?.offerId ?? null,
      secondId: concurrentSecond.body?.offerId ?? null,
      sameOfferId: Boolean(concurrentFirst.body?.offerId && concurrentFirst.body.offerId === concurrentSecond.body?.offerId),
      offerRows: offerRows(A, 'TB286-CONCURRENT'),
      before: concurrentBefore,
      after: concurrentAfter,
      errors: [concurrentFirst.body?.errorCode ?? null, concurrentSecond.body?.errorCode ?? null],
    },
    V4: {
      createStatus: lateCreate.status,
      offerId: lateId,
      initial: lateInitial?.body ? { sellerSku: lateInitial.body.sellerSku, status: lateInitial.body.status } : null,
      patchStatus: latePatch?.status ?? null,
      edited: lateEdited?.body ? { sellerSku: lateEdited.body.sellerSku, status: lateEdited.body.status } : null,
      originalReplayStatus: lateOriginalReplay?.status ?? null,
      originalReplayError: lateOriginalReplay?.body?.errorCode ?? lateOriginalReplay?.body?.title ?? null,
      changedReplayStatus: lateChangedReplay?.status ?? null,
      changedReplayId: lateChangedReplay?.body?.offerId ?? null,
      final: lateFinal?.body ? { sellerSku: lateFinal.body.sellerSku, status: lateFinal.body.status } : null,
      mutableComparison: true,
      lateReplayContractIssue: lateOriginalReplay?.status === 409 && lateChangedReplay?.status === 201,
    },
  };
  fs.writeFileSync(OUT, JSON.stringify(evidence, null, 2) + '\n');
  console.log(JSON.stringify(evidence, null, 2));
})().catch(error => {
  console.error(error);
  process.exitCode = 1;
});
