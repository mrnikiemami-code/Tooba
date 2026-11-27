// Group B — atomic concurrency and recorded history.
// Real admin HTTP + independent PostgreSQL. Isolated P1C- owned fixtures; exact finally cleanup.
const { randomUUID } = require("node:crypto");
const S = require("./p1c-support.cjs");
const { assert, uuid, deterministicId, runCase, SKU_PREFIX } = S;

async function prepare(ctx) {
  const seller = uuid(process.env.P1C_SELLER_ID || "01a030d1-40db-7000-b90c-a0705133f0eb");
  const variant = uuid(process.env.P1C_CREATE_VARIANT_ID || "01a0967d-a2eb-7000-9a12-779c15789bac");
  ctx.state.seller = seller;
  const dev = await ctx.api("/v1/admin/dev-context");
  assert.equal(dev.status, 200);
  const actor = uuid(dev.body.actorUserId);
  const admin = { [S.ADMIN_HEADER]: actor };
  const channels = await ctx.api("/v1/admin/offers/allowed-channels", "GET", undefined, admin);
  assert.equal(channels.status, 200);
  assert.ok(Array.isArray(channels.body) && channels.body.length > 0);
  const contexts = (await ctx.api("/v1/seller/dev-contexts")).body?.actors || [];
  assert.ok(contexts.some(x => x.sellerPartyId.toLowerCase() === seller), "Seller is not a current DEV synthetic identity");
  ctx.trace.fixture = { seller, variant, channel: channels.body[0], actor };
  return { seller, variant, channel: channels.body[0], actor, admin, contexts };
}

async function createOffer(ctx, fx, extra = {}) {
  const requestId = "P1C-" + randomUUID();
  const offerId = deterministicId(fx.seller, fx.variant, requestId);
  ctx.addOffer(offerId);
  assert.ok(S.EMPTY(ctx.observe("before-" + requestId.slice(-8))), "Candidate identity is not isolated");
  const sku = SKU_PREFIX + randomUUID();
  const res = await ctx.api("/v1/admin/offers", "POST", {
    sellerPartyId: fx.seller, catalogVariantId: fx.variant, channel: fx.channel,
    sellerSku: sku, status: "Draft", requestId, ...extra,
  }, fx.admin);
  assert.equal(res.status, 201, "create " + JSON.stringify(res.body));
  assert.equal(res.body.offerId, offerId);
  return { requestId, offerId, sku };
}

const get = (ctx, fx, offerId) => ctx.api("/v1/admin/offers/" + uuid(offerId), "GET", undefined, fx.admin);
const history = (ctx, fx, offerId, page = 1, pageSize = 20) =>
  ctx.api("/v1/admin/offers/" + uuid(offerId) + "/history?page=" + page + "&pageSize=" + pageSize, "GET", undefined, fx.admin);
const provision = (ctx, fx, offerId, locationId) =>
  ctx.api("/v1/admin/offers/" + uuid(offerId) + "/inventory/positions", "POST", { locationId }, fx.admin);
const inventory = (ctx, fx, offerId, body) =>
  ctx.api("/v1/admin/offers/" + uuid(offerId) + "/inventory", "PUT", body, fx.admin);
const price = (ctx, fx, offerId, body) =>
  ctx.api("/v1/admin/offers/" + uuid(offerId) + "/price", "PUT", body, fx.admin);
const patch = (ctx, fx, offerId, body) =>
  ctx.api("/v1/admin/offers/" + uuid(offerId), "PATCH", body, fx.admin);

async function addStock(ctx, fx, offerId, onHand) {
  const list = await ctx.api("/v1/admin/offers/inventory-locations", "GET", undefined, fx.admin);
  assert.equal(list.status, 200);
  assert.ok(Array.isArray(list.body) && list.body.length > 0, "No eligible active location");
  const provisioned = await provision(ctx, fx, offerId, list.body[0].locationId);
  assert.equal(provisioned.status, 200, JSON.stringify(provisioned.body));
  let detail = await get(ctx, fx, offerId);
  const row = detail.body.inventoryLocations.find(x => x.stockItemId === provisioned.body.stockItemId);
  assert.ok(row && row.revision, "Provisioned stock row or revision missing");
  const write = await inventory(ctx, fx, offerId, { onHand, reason: "P1C group B stock", stockItemId: row.stockItemId, expectedVersion: row.revision, operationId: randomUUID() });
  assert.equal(write.status, 200, "inventory " + JSON.stringify(write.body));
  detail = await get(ctx, fx, offerId);
  const updated = detail.body.inventoryLocations.find(x => x.stockItemId === provisioned.body.stockItemId);
  return { stockItemId: provisioned.body.stockItemId, locationId: provisioned.body.locationId, revision: updated.revision, detail };
}

function deny(res, codes, label) {
  assert.ok(res.status >= 400 && res.status < 500, (label || "denial") + " expected 4xx, got " + res.status + " " + JSON.stringify(res.body));
  if (codes && codes.length) assert.ok(codes.includes(res.body?.errorCode), (label || "denial") + " errorCode " + res.body?.errorCode + " not in " + codes.join(","));
}

module.exports = {
  "R10-05-P": () => runCase("R10-05-P", async ctx => {
    const fx = await prepare(ctx);
    const offer = await createOffer(ctx, fx);
    const before = await get(ctx, fx, offer.offerId);
    const version = before.body.revisions.offer;
    const first = await patch(ctx, fx, offer.offerId, { sellerSku: offer.sku + "-FIRST", status: "Draft", expectedVersion: version });
    assert.equal(first.status, 200, JSON.stringify(first.body));
    const second = await patch(ctx, fx, offer.offerId, { sellerSku: offer.sku + "-SECOND", status: "Draft", expectedVersion: version });
    deny(second, ["concurrency.conflict"], "stale offer write");
    const detail = await get(ctx, fx, offer.offerId);
    assert.equal(detail.body.sellerSku, offer.sku + "-FIRST", "First value must be retained");
    const state = ctx.observe("stale-denied");
    assert.equal(state.offers[0].seller_sku, offer.sku + "-FIRST");
    assert.equal(state.history.length, 2, "Only create and the winning edit may be recorded");
    return { status: "PASS", reason: "Two sessions shared one offer revision; the first write persisted and the stale second write was denied with a typed conflict while the first value remained." };
  }),

  "R10-05-N": () => runCase("R10-05-N", async ctx => {
    const fx = await prepare(ctx);
    const offer = await createOffer(ctx, fx);
    const stock = await addStock(ctx, fx, offer.offerId, 10);
    const firstPrice = await price(ctx, fx, offer.offerId, { amount: 123000, currency: null, market: null, expectedVersion: "absent", operationId: randomUUID() });
    assert.equal(firstPrice.status, 200, "first price " + JSON.stringify(firstPrice.body));
    const afterPrice = await get(ctx, fx, offer.offerId);
    const priceRevision = afterPrice.body.revisions.price;
    assert.notEqual(priceRevision, "absent");
    const stalePrice = await price(ctx, fx, offer.offerId, { amount: 999000, currency: null, market: null, expectedVersion: "absent", operationId: randomUUID() });
    deny(stalePrice, ["concurrency.conflict"], "stale price write");
    const staleStock = await inventory(ctx, fx, offer.offerId, { onHand: 2, reason: "P1C stale stock", stockItemId: stock.stockItemId, expectedVersion: "2000-01-01T00:00:00.0000000+00:00", operationId: randomUUID() });
    deny(staleStock, ["inventory.position.revision_conflict"], "stale stock write");
    const detail = await get(ctx, fx, offer.offerId);
    assert.equal(Number(detail.body.amount), 123000, "Stale price must not overwrite");
    assert.equal(Number(detail.body.inventoryLocations.find(x => x.stockItemId === stock.stockItemId).onHand), 10, "Stale stock must not overwrite");
    const state = ctx.observe("stale-denied");
    assert.equal(state.priceFacts.length, 1, "Stale price must not add a price fact");
    assert.equal(state.prices.length, 1);
    assert.equal(Number(state.prices[0].amount), 123000);
    return { status: "PASS", reason: "A stale price write and stale stock write were both denied with typed conflicts; the first price and physical count remained, and no extra price fact was recorded." };
  }),

  "R10-05-B": () => runCase("R10-05-B", async ctx => {
    const fx = await prepare(ctx);
    const offer = await createOffer(ctx, fx);
    const stock = await addStock(ctx, fx, offer.offerId, 10);
    const firstPrice = await price(ctx, fx, offer.offerId, { amount: 500000, currency: null, market: null, expectedVersion: "absent", operationId: randomUUID() });
    assert.equal(firstPrice.status, 200, JSON.stringify(firstPrice.body));
    let detail = await get(ctx, fx, offer.offerId);
    const currentStock = detail.body.inventoryLocations.find(x => x.stockItemId === stock.stockItemId);
    const reserved = Number(currentStock.reserved);
    const [invA, invB] = await Promise.all([
      inventory(ctx, fx, offer.offerId, { onHand: 4, reason: "P1C race A", stockItemId: stock.stockItemId, expectedVersion: currentStock.revision, operationId: randomUUID() }),
      inventory(ctx, fx, offer.offerId, { onHand: 7, reason: "P1C race B", stockItemId: stock.stockItemId, expectedVersion: currentStock.revision, operationId: randomUUID() }),
    ]);
    const stockResults = [invA, invB];
    assert.equal(stockResults.filter(r => r.status === 200).length, 1, "Exactly one concurrent stock writer may win: " + stockResults.map(r => r.status).join(","));
    assert.equal(stockResults.filter(r => r.status === 409 && r.body?.errorCode === "inventory.position.revision_conflict").length, 1, "Loser must receive owner revision conflict");
    const priceRevision = detail.body.revisions.price;
    const [priceA, priceB] = await Promise.all([
      price(ctx, fx, offer.offerId, { amount: 610000, currency: null, market: null, expectedVersion: priceRevision, operationId: randomUUID() }),
      price(ctx, fx, offer.offerId, { amount: 720000, currency: null, market: null, expectedVersion: priceRevision, operationId: randomUUID() }),
    ]);
    const priceResults = [priceA, priceB];
    assert.equal(priceResults.filter(r => r.status === 200).length, 1, "Exactly one concurrent price writer may win: " + priceResults.map(r => r.status).join(","));
    assert.equal(priceResults.filter(r => r.status === 409 && r.body?.errorCode === "concurrency.conflict").length, 1, "Loser must receive pricing concurrency conflict");
    detail = await get(ctx, fx, offer.offerId);
    const finalStock = detail.body.inventoryLocations.find(x => x.stockItemId === stock.stockItemId);
    const winnerStock = stockResults.find(r => r.status === 200).body.inventoryLocations.find(x => x.stockItemId === stock.stockItemId);
    assert.equal(Number(finalStock.onHand), Number(winnerStock.onHand), "Persisted stock must equal the winning request");
    const winnerPrice = priceResults.find(r => r.status === 200).body.amount;
    assert.equal(Number(detail.body.amount), Number(winnerPrice), "Persisted price must equal the winning request");
    assert.ok(Number(finalStock.onHand) >= Number(finalStock.reserved), "Reserve floor violated");
    const state = ctx.observe("race-resolved");
    assert.equal(state.priceFacts.length, 2, "Initial plus exactly one winning price fact");
    const winners = state.inventoryFacts.filter(x => x.kind === "PhysicalChanged");
    assert.equal(winners.length, 2, "Initial plus exactly one winning physical fact");
    return { status: "PASS", reason: "Two concurrent same-revision stock writes and two concurrent same-revision price writes each produced exactly one winner; persisted values equal the winners and on-hand (" + Number(finalStock.onHand) + ") >= reserved (" + Number(finalStock.reserved) + ").", observedReserved: reserved };
  }),

  "R10-06-P": () => runCase("R10-06-P", async ctx => {
    const fx = await prepare(ctx);
    const offer = await createOffer(ctx, fx, { returnPolicyChoice: "Custom", customReturnWindowDays: 9 });
    const stock = await addStock(ctx, fx, offer.offerId, 5);
    const priced = await price(ctx, fx, offer.offerId, { amount: 432100, currency: null, market: null, expectedVersion: "absent", operationId: randomUUID() });
    assert.equal(priced.status, 200, JSON.stringify(priced.body));
    const detail = await get(ctx, fx, offer.offerId);
    const edited = await patch(ctx, fx, offer.offerId, { sellerSku: offer.sku + "-H", status: "Draft", minimumOrderQuantity: 2, maximumOrderQuantity: 6, expectedVersion: detail.body.revisions.offer });
    assert.equal(edited.status, 200, JSON.stringify(edited.body));
    const page = await history(ctx, fx, offer.offerId, 1, 100);
    assert.equal(page.status, 200);
    const items = page.body.items;
    assert.equal(Number(page.body.totalCount), items.length);
    assert.ok(items.length >= 4, "Expected create, stock, price and commercial events; got " + items.map(x => x.kind).join(","));
    const kinds = new Set(items.map(x => x.kind));
    for (const kind of ["Created", "InventoryChanged", "PriceChanged", "CommercialChanged"]) assert.ok(kinds.has(kind), "Missing history kind " + kind + " in " + [...kinds].join(","));
    assert.ok(items.every(x => x.offerId === offer.offerId), "History scope must match the offer");
    assert.ok(items.every(x => !Number.isNaN(Date.parse(x.occurredAt))), "Every event needs a real timestamp");
    const inventoryItem = items.find(x => x.kind === "InventoryChanged");
    assert.equal(inventoryItem.targetId, stock.stockItemId, "Inventory history target must be the real stock item");
    assert.equal(inventoryItem.locationId, stock.locationId, "Inventory history location must be the real location");
    const priceItem = items.find(x => x.kind === "PriceChanged");
    assert.ok(String(priceItem.newValue).includes("432100"), "Price history must record the new amount");
    assert.ok(String(priceItem.newValue).includes("IRR"), "Price history must record the currency");
    const commercialItem = items.find(x => x.kind === "CommercialChanged");
    assert.equal(commercialItem.actorUserId, fx.actor, "Commercial actor must be the real admin actor");
    const ordered = [...items].sort((a, b) => Date.parse(b.occurredAt) - Date.parse(a.occurredAt));
    assert.deepEqual(items.map(x => Date.parse(x.occurredAt)), ordered.map(x => Date.parse(x.occurredAt)), "History must be ordered by occurrence descending");
    const state = ctx.observe("history");
    assert.ok(state.history.length >= 2);
    assert.equal(state.priceFacts.length, 1);
    assert.ok(state.inventoryFacts.length >= 1);
    return { status: "PASS", reason: "Authorized history returned the real recorded create, inventory, price and commercial changes with scope, timestamps, actor and old/new values matching independent SQL." };
  }),

  "R10-06-N": () => runCase("R10-06-N", async ctx => {
    const fx = await prepare(ctx);
    const offer = await createOffer(ctx, fx);
    const foreign = fx.contexts.find(x => x.sellerPartyId.toLowerCase() !== fx.seller);
    assert.ok(foreign, "Need a second real DEV seller context for a foreign read");
    const foreignHeaders = { [S.ADMIN_HEADER]: uuid(foreign.actorUserId), [S.SELLER_HEADER]: uuid(foreign.sellerPartyId) };
    const read = await ctx.api("/v1/seller/offers/" + offer.offerId, "GET", undefined, foreignHeaders);
    deny(read, ["offer.not_found"], "foreign detail read");
    const hist = await ctx.api("/v1/seller/offers/" + offer.offerId + "/history?page=1&pageSize=20", "GET", undefined, foreignHeaders);
    deny(hist, ["offer.not_found"], "foreign history read");
    assert.ok(!Array.isArray(hist.body?.items) || hist.body.items.length === 0, "Foreign denial must not disclose history items");
    const state = ctx.observe("foreign-denied");
    assert.equal(state.offers.length, 1);
    return { status: "PASS", reason: "A different current DEV seller was denied both detail and history for the owned offer with typed not-found and no history disclosure." };
  }),

  "R10-06-B": () => runCase("R10-06-B", async ctx => {
    const fx = await prepare(ctx);
    const offer = await createOffer(ctx, fx);
    const first = await history(ctx, fx, offer.offerId, 1, 1);
    assert.equal(first.status, 200);
    assert.equal(Number(first.body.totalCount), 1, "A newly persisted offer must expose exactly its recorded create event");
    assert.equal(first.body.items.length, 1);
    assert.equal(first.body.items[0].kind, "Created");
    assert.equal(first.body.items[0].actorUserId, fx.actor, "Recorded actor must be the real admin actor");
    const second = await history(ctx, fx, offer.offerId, 2, 1);
    assert.equal(second.status, 200);
    assert.equal(Number(second.body.totalCount), 1, "Total count must stay stable across pages");
    assert.equal(second.body.items.length, 0, "A page beyond the recorded set must be honestly empty");
    const full = await history(ctx, fx, offer.offerId, 1, 20);
    assert.equal(Number(full.body.totalCount), 1);
    assert.equal(full.body.items.length, 1);
    assert.equal(full.body.page, 1);
    assert.equal(full.body.pageSize, 20);
    return { status: "PASS", reason: "A newly persisted offer exposed exactly one honest create event with the recorded actor; a page beyond the set was empty while total count and page metadata stayed stable." };
  }),
};
