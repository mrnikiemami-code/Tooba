// Group A — identity, create workspace, inventory provisioning, shared persistence.
// Real admin HTTP + independent PostgreSQL. Isolated P1C- owned Draft fixtures; exact finally cleanup.
const { randomUUID } = require("node:crypto");
const S = require("./p1c-support.cjs");
const { assert, uuid, deterministicId, runCase, NotRun, SKU_PREFIX } = S;

const ALL_CHANNELS = ["Direct", "Marketplace", "Agency", "Corporate", "Affiliate", "Api"];

async function prepare(ctx) {
  const seller = uuid(process.env.P1C_SELLER_ID || "01a030d1-40db-7000-b90c-a0705133f0eb");
  const variant = uuid(process.env.P1C_CREATE_VARIANT_ID || "01a0967d-a2eb-7000-9a12-779c15789bac");
  ctx.state.seller = seller;
  const dev = await ctx.api("/v1/admin/dev-context");
  assert.equal(dev.status, 200, "admin dev-context");
  const actor = uuid(dev.body.actorUserId);
  const admin = { [S.ADMIN_HEADER]: actor };
  const channels = await ctx.api("/v1/admin/offers/allowed-channels", "GET", undefined, admin);
  assert.equal(channels.status, 200, "allowed-channels");
  assert.ok(Array.isArray(channels.body) && channels.body.length > 0, "No authorized channel");
  const actors = await ctx.api("/v1/seller/dev-contexts");
  const contexts = Array.isArray(actors.body?.actors) ? actors.body.actors : [];
  assert.ok(contexts.some(x => x.sellerPartyId.toLowerCase() === seller), "Seller is not a current DEV synthetic identity");
  ctx.trace.fixture = { seller, variant, channel: channels.body[0], allowed: channels.body, actor };
  return { seller, variant, channel: channels.body[0], allowed: channels.body, actor, admin, contexts };
}

function offerBody(fx, requestId, sku, extra = {}) {
  return {
    sellerPartyId: fx.seller,
    catalogVariantId: fx.variant,
    channel: fx.channel,
    sellerSku: sku,
    status: "Draft",
    requestId,
    ...extra,
  };
}

async function beginOffer(ctx, fx, requestId, variant = fx.variant) {
  const offerId = deterministicId(fx.seller, variant, requestId);
  ctx.addOffer(offerId);
  const first = ctx.observe("before-" + requestId.slice(-8));
  assert.ok(S.EMPTY(first), "Deterministic candidate identity is not isolated");
  return { requestId, offerId, sku: SKU_PREFIX + randomUUID() };
}

async function create(ctx, fx, fixture, extra = {}, variant = fx.variant, channel = fx.channel) {
  const body = { ...offerBody(fx, fixture.requestId, fixture.sku, extra), catalogVariantId: variant, channel };
  const res = await ctx.api("/v1/admin/offers", "POST", body, fx.admin);
  return { res, body };
}

const get = (ctx, fx, offerId) => ctx.api("/v1/admin/offers/" + uuid(offerId), "GET", undefined, fx.admin);
const history = (ctx, fx, offerId, page = 1, pageSize = 20) =>
  ctx.api("/v1/admin/offers/" + uuid(offerId) + "/history?page=" + page + "&pageSize=" + pageSize, "GET", undefined, fx.admin);
const locations = (ctx, fx) => ctx.api("/v1/admin/offers/inventory-locations", "GET", undefined, fx.admin);
const provision = (ctx, fx, offerId, locationId) =>
  ctx.api("/v1/admin/offers/" + uuid(offerId) + "/inventory/positions", "POST", { locationId }, fx.admin);
const inventory = (ctx, fx, offerId, body) =>
  ctx.api("/v1/admin/offers/" + uuid(offerId) + "/inventory", "PUT", body, fx.admin);
const price = (ctx, fx, offerId, body) =>
  ctx.api("/v1/admin/offers/" + uuid(offerId) + "/price", "PUT", body, fx.admin);
const patch = (ctx, fx, offerId, body) =>
  ctx.api("/v1/admin/offers/" + uuid(offerId), "PATCH", body, fx.admin);

function okCreate(res, fixture, fx, variant = fx.variant) {
  assert.equal(res.status, 201, "create status " + JSON.stringify(res.body));
  assert.equal(res.body.offerId, fixture.offerId, "deterministic offer id");
  assert.equal(res.body.sellerPartyId, fx.seller, "response seller");
  assert.equal(res.body.catalogVariantId, variant, "response variant");
  assert.equal(res.body.channel, fx.channel, "response channel");
  assert.equal(res.body.status, "Draft", "initial lifecycle is Draft");
}

function deny(res, allowedCodes, label) {
  assert.ok(res.status >= 400 && res.status < 500, (label || "denial") + " expected typed 4xx, got " + res.status + " " + JSON.stringify(res.body));
  if (allowedCodes && allowedCodes.length) {
    assert.ok(allowedCodes.includes(res.body?.errorCode), (label || "denial") + " wrong error code " + res.body?.errorCode + " not in " + allowedCodes.join(","));
  }
}

module.exports = {
  "R10-01-P": () => runCase("R10-01-P", async ctx => {
    const fx = await prepare(ctx);
    const active = await ctx.api("/v1/admin/offers/active-listing?sellerPartyId=" + fx.seller + "&catalogVariantId=" + fx.variant + "&channel=" + encodeURIComponent(fx.channel), "GET", undefined, fx.admin);
    assert.equal(active.status, 200);
    assert.equal(active.body, null, "Candidate variant already has an active listing");
    const fixture = await beginOffer(ctx, fx, "P1C-" + randomUUID());
    const { res } = await create(ctx, fx, fixture);
    okCreate(res, fixture, fx);
    const detail = await get(ctx, fx, fixture.offerId);
    assert.equal(detail.status, 200);
    assert.equal(detail.body.channel, fx.channel);
    assert.equal(detail.body.status, "Draft");
    assert.equal(detail.body.sellerSku, fixture.sku);
    const state = ctx.observe("created");
    assert.equal(state.offers.length, 1);
    assert.equal(state.offers[0].offer_id, fixture.offerId);
    assert.equal(state.offers[0].channel, fx.channel);
    assert.equal(state.offers[0].status, "Draft");
    assert.equal(state.offers[0].seller_sku, fixture.sku);
    assert.equal(state.history.length, 1);
    return { status: "PASS", reason: "Exact configured channel '" + fx.channel + "' persisted as one Draft; independent GET and SQL agree; history has exactly the create entry." };
  }),

  "R10-01-B": () => runCase("R10-01-B", async ctx => {
    const fx = await prepare(ctx);
    assert.equal(fx.allowed.length, 1, "Expected exactly one configured store channel");
    const fixture = await beginOffer(ctx, fx, "P1C-" + randomUUID());
    const { res } = await create(ctx, fx, fixture);
    okCreate(res, fixture, fx);
    const unauthorized = ALL_CHANNELS.find(c => !fx.allowed.includes(c));
    assert.ok(unauthorized, "Need an enumerated unauthorized channel");
    const changed = await ctx.api("/v1/admin/offers", "POST", offerBody(fx, fixture.requestId, fixture.sku, { channel: unauthorized }), fx.admin);
    deny(changed, ["offer.channel.not_allowed"], "changed channel");
    const distinct = await ctx.api("/v1/admin/offers", "POST", offerBody(fx, "P1C-" + randomUUID(), SKU_PREFIX + randomUUID()), fx.admin);
    deny(distinct, ["offer.listing.duplicate_active"], "distinct intent same listing");
    const state = ctx.observe("channel-isolation");
    assert.equal(state.offers.length, 1, "Only the authorized offer may persist");
    throw new NotRun("Current DEV store exposes only the single configured channel [" + fx.allowed.join(",") + "]. A second independently allowed channel is required to create two distinct authorized offers and to observe an intent conflict after a legitimate channel change; that context is unavailable and must not be fabricated or granted. Observed partial facts: unauthorized changed-channel denied '" + changed.body?.errorCode + "' with zero extra rows; distinct intent on the same listing denied '" + distinct.body?.errorCode + "'.");
  }),

  "R10-02-P": () => runCase("R10-02-P", async ctx => {
    const fx = await prepare(ctx);
    const fixture = await beginOffer(ctx, fx, "P1C-" + randomUUID());
    const extra = { returnPolicyChoice: "Custom", customReturnWindowDays: 9, minimumOrderQuantity: 2, maximumOrderQuantity: 6 };
    const { res } = await create(ctx, fx, fixture, extra);
    okCreate(res, fixture, fx);
    const detail = await get(ctx, fx, fixture.offerId);
    assert.equal(detail.status, 200);
    assert.equal(detail.body.sellerPartyId, fx.seller);
    assert.equal(detail.body.catalogVariantId, fx.variant);
    assert.equal(detail.body.offerId, fixture.offerId);
    assert.equal(detail.body.channel, fx.channel);
    assert.equal(detail.body.status, "Draft");
    assert.equal(detail.body.sellerSku, fixture.sku);
    assert.equal(detail.body.returnPolicyChoice, "Custom");
    assert.equal(detail.body.customReturnWindowDays, 9);
    assert.equal(Number(detail.body.minimumOrderQuantity), 2);
    assert.equal(Number(detail.body.maximumOrderQuantity), 6);
    const state = ctx.observe("created");
    assert.equal(state.offers.length, 1);
    assert.equal(state.offers[0].status, "Draft");
    assert.equal(state.offers[0].return_policy_choice, "Custom");
    assert.equal(state.offers[0].custom_return_window_days, 9);
    assert.equal(Number(state.offers[0].minimum_order_quantity), 2);
    assert.equal(Number(state.offers[0].maximum_order_quantity), 6);
    assert.equal(state.history.length, 1);
    assert.equal(state.prices.length, 0, "Create must not auto-write price");
    assert.equal(state.positions.length, 0, "Create must not auto-provision stock");
    return { status: "PASS", reason: "One durable Draft persisted with every requested Offer-owned field; no price or stock side effect before explicit save." };
  }),

  "R10-02-N": () => runCase("R10-02-N", async ctx => {
    const fx = await prepare(ctx);
    const cases = [
      { label: "min>max", extra: { minimumOrderQuantity: 5, maximumOrderQuantity: 2 }, codes: ["validation.failed", "offer.validation.order_quantity_range", "offer.min_quantity.exceeds_max"] },
      { label: "wrong precision", extra: { minimumOrderQuantity: 1.5 }, codes: ["offer.min_quantity.invalid"] },
      { label: "forbidden custom window", extra: { returnPolicyChoice: "Custom", customReturnWindowDays: 999 }, codes: ["offer.return_policy.custom_window_out_of_range"] },
    ];
    const observed = [];
    for (const item of cases) {
      const fixture = await beginOffer(ctx, fx, "P1C-" + randomUUID());
      const { res } = await create(ctx, fx, fixture, item.extra);
      deny(res, item.codes, item.label);
      observed.push({ label: item.label, status: res.status, errorCode: res.body?.errorCode });
      assert.ok(S.EMPTY(ctx.observe(item.label)), "Invalid draft '" + item.label + "' wrote offer-owned rows");
    }
    assert.equal(observed.length, 3);
    return { status: "PASS", reason: "Three invalid drafts (range, precision, return window) rejected before any offer or history write; independent SQL remained empty.", observed };
  }),

  "R10-02-B": () => runCase("R10-02-B", async ctx => {
    const fx = await prepare(ctx);
    const fixture = await beginOffer(ctx, fx, "P1C-" + randomUUID());
    const body = offerBody(fx, fixture.requestId, fixture.sku);
    const [a, b] = await Promise.all([
      ctx.api("/v1/admin/offers", "POST", body, fx.admin),
      ctx.api("/v1/admin/offers", "POST", body, fx.admin),
    ]);
    okCreate(a, fixture, fx);
    okCreate(b, fixture, fx);
    const third = await ctx.api("/v1/admin/offers", "POST", body, fx.admin);
    okCreate(third, fixture, fx);
    const state = ctx.observe("concurrent");
    assert.equal(state.offers.length, 1, "Concurrent identical submits must persist one offer");
    assert.equal(state.history.length, 1, "Concurrent identical submits must record one create event");
    assert.ok(state.offers[0].creation_intent_fingerprint, "Durable creation intent fingerprint missing");
    return { status: "PASS", reason: "Concurrent double-submit and third replay all returned the same deterministic offer with exactly one durable offer and one create history row." };
  }),

  "R10-03-P": () => runCase("R10-03-P", async ctx => {
    const fx = await prepare(ctx);
    const fixture = await beginOffer(ctx, fx, "P1C-" + randomUUID());
    const created = await create(ctx, fx, fixture);
    okCreate(created.res, fixture, fx);
    const list = await locations(ctx, fx);
    assert.equal(list.status, 200);
    assert.ok(Array.isArray(list.body) && list.body.length >= 2, "Need two eligible active locations");
    const chosen = list.body.slice(0, 2);
    const p1 = await provision(ctx, fx, fixture.offerId, chosen[0].locationId);
    const p2 = await provision(ctx, fx, fixture.offerId, chosen[1].locationId);
    assert.equal(p1.status, 200, JSON.stringify(p1.body));
    assert.equal(p2.status, 200, JSON.stringify(p2.body));
    assert.equal(p1.body.offerId, fixture.offerId);
    assert.equal(p2.body.offerId, fixture.offerId);
    assert.notEqual(p1.body.stockItemId, p2.body.stockItemId, "Two real positions required");
    const detail = await get(ctx, fx, fixture.offerId);
    assert.equal(detail.body.inventoryLocations.length, 2);
    let row = detail.body.inventoryLocations.find(x => x.stockItemId === p1.body.stockItemId);
    const write1 = await inventory(ctx, fx, fixture.offerId, { onHand: 3, reason: "P1C position one", stockItemId: p1.body.stockItemId, expectedVersion: row.revision, operationId: randomUUID() });
    assert.equal(write1.status, 200, JSON.stringify(write1.body));
    row = (await get(ctx, fx, fixture.offerId)).body.inventoryLocations.find(x => x.stockItemId === p2.body.stockItemId);
    const write2 = await inventory(ctx, fx, fixture.offerId, { onHand: 4, reason: "P1C position two", stockItemId: p2.body.stockItemId, expectedVersion: row.revision, operationId: randomUUID() });
    assert.equal(write2.status, 200, JSON.stringify(write2.body));
    const after = await get(ctx, fx, fixture.offerId);
    const one = after.body.inventoryLocations.find(x => x.stockItemId === p1.body.stockItemId);
    const two = after.body.inventoryLocations.find(x => x.stockItemId === p2.body.stockItemId);
    assert.equal(Number(one.onHand), 3);
    assert.equal(Number(two.onHand), 4);
    assert.equal(Number(one.reserved), 0);
    assert.equal(Number(two.reserved), 0);
    assert.equal(Number(one.available), 3);
    assert.equal(Number(two.available), 4);
    const state = ctx.observe("two-positions");
    assert.equal(state.positions.length, 2);
    assert.equal(state.positions.filter(x => x.offer_id === fixture.offerId).length, 2);
    assert.ok(state.positions.every(x => Number(x.reserved) === 0));
    return { status: "PASS", reason: "Two distinct eligible locations provisioned real positions; both physical counts persisted with independent reserved/available reads unchanged at zero." };
  }),

  "R10-03-N": () => runCase("R10-03-N", async ctx => {
    const fx = await prepare(ctx);
    const fixture = await beginOffer(ctx, fx, "P1C-" + randomUUID());
    const created = await create(ctx, fx, fixture);
    okCreate(created.res, fixture, fx);
    const list = await locations(ctx, fx);
    assert.ok(Array.isArray(list.body) && list.body.length >= 1);
    const provisioned = await provision(ctx, fx, fixture.offerId, list.body[0].locationId);
    assert.equal(provisioned.status, 200);
    const baseline = ctx.observe("baseline");
    assert.equal(baseline.positions.length, 1);
    const badLocation = "00000000-0000-4000-8000-" + randomUUID().slice(-12);
    const badProvision = await provision(ctx, fx, fixture.offerId, badLocation);
    deny(badProvision, ["inventory.location.not_found"], "unknown location");
    assert.equal(ctx.observe("unknown-location").positions.length, 1, "Unknown location must not create a position");
    const detail = await get(ctx, fx, fixture.offerId);
    const position = detail.body.inventoryLocations[0];
    const unknownStock = await inventory(ctx, fx, fixture.offerId, { onHand: 9, reason: "P1C unknown stock", stockItemId: randomUUID(), expectedVersion: position.revision, operationId: randomUUID() });
    deny(unknownStock, ["inventory.position.id_unknown", "inventory.position.not_found"], "unknown stock item");
    assert.equal(ctx.observe("unknown-stock").positions[0].on_hand, baseline.positions[0].on_hand);
    const belowFloor = await inventory(ctx, fx, fixture.offerId, { onHand: Number(position.reserved) - 1, reason: "P1C below floor", stockItemId: position.stockItemId, expectedVersion: position.revision, operationId: randomUUID() });
    deny(belowFloor, ["inventory.adjustment.quantity_exceeds", "inventory.quantity.invalid", "validation.failed"], "below reserved floor");
    const finalState = ctx.observe("after-denials");
    assert.equal(finalState.positions.length, 1);
    assert.equal(finalState.positions[0].on_hand, baseline.positions[0].on_hand);
    assert.equal(finalState.inventoryFacts.length, baseline.inventoryFacts.length);
    return { status: "PASS", reason: "Unknown/inactive location and unknown position were denied without creating rows; an on-hand below the actual reserved floor was denied with physical/reserved facts unchanged." };
  }),

  "R10-03-B": () => runCase("R10-03-B", async ctx => {
    const fx = await prepare(ctx);
    const fixture = await beginOffer(ctx, fx, "P1C-" + randomUUID());
    const created = await create(ctx, fx, fixture);
    okCreate(created.res, fixture, fx);
    const list = await locations(ctx, fx);
    assert.ok(list.body.length >= 2);
    const p1 = await provision(ctx, fx, fixture.offerId, list.body[0].locationId);
    const p2 = await provision(ctx, fx, fixture.offerId, list.body[1].locationId);
    assert.equal(p1.status, 200);
    assert.equal(p2.status, 200);
    let detail = await get(ctx, fx, fixture.offerId);
    const one = detail.body.inventoryLocations.find(x => x.stockItemId === p1.body.stockItemId);
    const firstWrite = await inventory(ctx, fx, fixture.offerId, { onHand: 6, reason: "P1C first remainder", stockItemId: one.stockItemId, expectedVersion: one.revision, operationId: randomUUID() });
    assert.equal(firstWrite.status, 200, JSON.stringify(firstWrite.body));
    const afterFirst = ctx.observe("first-success");
    const firstPosition = afterFirst.positions.find(x => x.stock_item_id === p1.body.stockItemId);
    assert.equal(Number(firstPosition.on_hand), 6);
    detail = await get(ctx, fx, fixture.offerId);
    const two = detail.body.inventoryLocations.find(x => x.stockItemId === p2.body.stockItemId);
    const stale = await inventory(ctx, fx, fixture.offerId, { onHand: 7, reason: "P1C retry remainder", stockItemId: two.stockItemId, expectedVersion: "2000-01-01T00:00:00.0000000+00:00", operationId: randomUUID() });
    deny(stale, ["inventory.position.revision_conflict"], "second write fails");
    const afterFailure = ctx.observe("second-failed");
    assert.equal(Number(afterFailure.positions.find(x => x.stock_item_id === p2.body.stockItemId).on_hand), 0, "Failed second write must not apply");
    assert.equal(afterFailure.positions.length, 2, "No duplicate position on failure");
    assert.equal(Number(afterFailure.positions.find(x => x.stock_item_id === p1.body.stockItemId).on_hand), 6, "First success must be preserved");
    detail = await get(ctx, fx, fixture.offerId);
    const retryRow = detail.body.inventoryLocations.find(x => x.stockItemId === p2.body.stockItemId);
    const retry = await inventory(ctx, fx, fixture.offerId, { onHand: 7, reason: "P1C retry remainder", stockItemId: p2.body.stockItemId, expectedVersion: retryRow.revision, operationId: randomUUID() });
    assert.equal(retry.status, 200, JSON.stringify(retry.body));
    const afterRetry = ctx.observe("retry-succeeded");
    assert.equal(afterRetry.positions.length, 2);
    assert.equal(Number(afterRetry.positions.find(x => x.stock_item_id === p1.body.stockItemId).on_hand), 6);
    assert.equal(Number(afterRetry.positions.find(x => x.stock_item_id === p2.body.stockItemId).on_hand), 7);
    const retryFact = afterRetry.inventoryFacts.find(x => x.stock_item_id === p2.body.stockItemId && Number(x.new_on_hand) === 7);
    assert.ok(retryFact, "Retry physical fact missing");
    assert.equal(retryFact.reason, "P1C retry remainder", "Retry reason must be retained");
    return { status: "PASS", reason: "A stale second position write failed without applying or duplicating; the first position stayed at 6; retrying only the remainder applied 7 with the intended reason retained." };
  }),

  "R10-04-P": () => runCase("R10-04-P", async ctx => {
    const fx = await prepare(ctx);
    const fixture = await beginOffer(ctx, fx, "P1C-" + randomUUID());
    const extra = { returnPolicyChoice: "Custom", customReturnWindowDays: 9, minimumOrderQuantity: 2, maximumOrderQuantity: 6 };
    const created = await create(ctx, fx, fixture, extra);
    okCreate(created.res, fixture, fx);
    let detail = await get(ctx, fx, fixture.offerId);
    assert.equal(detail.body.sellerSku, fixture.sku);
    assert.equal(detail.body.returnPolicyChoice, "Custom");
    const changedSku = fixture.sku + "-EDIT";
    const edited = await patch(ctx, fx, fixture.offerId, { sellerSku: changedSku, status: "Draft", minimumOrderQuantity: 3, maximumOrderQuantity: 8, expectedVersion: detail.body.revisions.offer });
    assert.equal(edited.status, 200, JSON.stringify(edited.body));
    assert.equal(edited.body.sellerSku, changedSku);
    detail = await get(ctx, fx, fixture.offerId);
    assert.equal(detail.body.sellerSku, changedSku);
    assert.equal(Number(detail.body.minimumOrderQuantity), 3);
    assert.equal(Number(detail.body.maximumOrderQuantity), 8);
    assert.equal(detail.body.returnPolicyChoice, "Custom");
    assert.equal(detail.body.customReturnWindowDays, 9);
    assert.equal(detail.body.channel, fx.channel);
    const state = ctx.observe("edited");
    assert.equal(state.offers.length, 1);
    assert.equal(state.offers[0].seller_sku, changedSku);
    assert.equal(Number(state.offers[0].minimum_order_quantity), 3);
    assert.equal(Number(state.offers[0].maximum_order_quantity), 8);
    assert.equal(state.history.length, 2, "Create plus one commercial edit event");
    return { status: "PASS", reason: "Create then edit reread returned every requested field from the same committed offer; the durable edit replaced the submitted values with no duplicate object." };
  }),

  "R10-04-N": () => runCase("R10-04-N", async ctx => {
    const fx = await prepare(ctx);
    const missingSeller = "00000000-0000-4000-8000-" + randomUUID().slice(-12);
    const badRequest = "P1C-" + randomUUID();
    const badOwnerId = deterministicId(missingSeller, fx.variant, badRequest);
    ctx.addOffer(badOwnerId);
    const wrongSeller = await ctx.api("/v1/admin/offers", "POST", {
      sellerPartyId: missingSeller, catalogVariantId: fx.variant, channel: fx.channel,
      sellerSku: SKU_PREFIX + randomUUID(), status: "Draft", requestId: badRequest,
    }, fx.admin);
    deny(wrongSeller, ["offer.seller.missing"], "wrong seller");
    assert.equal(ctx.observe("wrong-seller").offers.length, 0, "Wrong seller must not create an offer");
    const missingVariant = "00000000-0000-4000-8000-" + randomUUID().slice(-12);
    const wrongVariantRequest = "P1C-" + randomUUID();
    const wrongVariantId = deterministicId(fx.seller, missingVariant, wrongVariantRequest);
    ctx.addOffer(wrongVariantId);
    const badVariant = await ctx.api("/v1/admin/offers", "POST", {
      sellerPartyId: fx.seller, catalogVariantId: missingVariant, channel: fx.channel,
      sellerSku: SKU_PREFIX + randomUUID(), status: "Draft", requestId: wrongVariantRequest,
    }, fx.admin);
    deny(badVariant, ["offer.catalog_variant.missing"], "wrong variant");
    assert.equal(ctx.observe("wrong-variant").offers.length, 0, "Wrong variant must not create an offer");
    const fixture = await beginOffer(ctx, fx, "P1C-" + randomUUID());
    const recovered = await create(ctx, fx, fixture, { returnPolicyChoice: "Custom", customReturnWindowDays: 5 });
    okCreate(recovered.res, fixture, fx);
    const detail = await get(ctx, fx, fixture.offerId);
    assert.equal(detail.body.sellerPartyId, fx.seller);
    assert.equal(detail.body.catalogVariantId, fx.variant);
    assert.equal(detail.body.returnPolicyChoice, "Custom");
    assert.equal(detail.body.customReturnWindowDays, 5);
    const state = ctx.observe("recovered");
    assert.equal(state.offers.length, 1);
    assert.equal(state.offers[0].offer_id, fixture.offerId);
    return { status: "PASS", reason: "Controlled wrong-seller and wrong-variant injections were denied with zero rows (no false success); the corrected intent then persisted and reread exactly." };
  }),
};
