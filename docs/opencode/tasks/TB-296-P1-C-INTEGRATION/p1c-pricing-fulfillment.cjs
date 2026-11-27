// Group C — unit/conditions and price/shipping.
// Real admin HTTP + independent PostgreSQL. Isolated P1C- owned fixtures; exact finally cleanup.
const { randomUUID } = require("node:crypto");
const S = require("./p1c-support.cjs");
const { assert, uuid, deterministicId, runCase, NotRun, SKU_PREFIX } = S;

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
  const self = contexts.find(x => x.sellerPartyId.toLowerCase() === seller);
  assert.ok(self, "Seller is not a current DEV synthetic identity");
  ctx.trace.fixture = { seller, variant, channel: channels.body[0], actor };
  return { seller, variant, channel: channels.body[0], actor, admin, contexts, self };
}

async function createOffer(ctx, fx, variant = fx.variant, extra = {}) {
  const requestId = "P1C-" + randomUUID();
  const offerId = deterministicId(fx.seller, variant, requestId);
  ctx.addOffer(offerId);
  // Isolate only the new candidate identity; earlier fixtures created deliberately in the same case are persisted.
  const isolatedBefore = S.snapshot([offerId]);
  ctx.trace.sql.push({ label: "before-" + requestId.slice(-8), state: isolatedBefore });
  assert.ok(S.EMPTY(isolatedBefore), "Candidate identity is not isolated");
  const sku = SKU_PREFIX + randomUUID();
  const res = await ctx.api("/v1/admin/offers", "POST", {
    sellerPartyId: fx.seller, catalogVariantId: variant, channel: fx.channel,
    sellerSku: sku, status: "Draft", requestId, ...extra,
  }, fx.admin);
  assert.equal(res.status, 201, "create " + JSON.stringify(res.body));
  assert.equal(res.body.offerId, offerId);
  return { requestId, offerId, sku, variant };
}

const get = (ctx, fx, offerId) => ctx.api("/v1/admin/offers/" + uuid(offerId), "GET", undefined, fx.admin);
const provision = (ctx, fx, offerId, locationId) =>
  ctx.api("/v1/admin/offers/" + uuid(offerId) + "/inventory/positions", "POST", { locationId }, fx.admin);
const inventory = (ctx, fx, offerId, body) =>
  ctx.api("/v1/admin/offers/" + uuid(offerId) + "/inventory", "PUT", body, fx.admin);
const price = (ctx, fx, offerId, body) =>
  ctx.api("/v1/admin/offers/" + uuid(offerId) + "/price", "PUT", body, fx.admin);
const patch = (ctx, fx, offerId, body) =>
  ctx.api("/v1/admin/offers/" + uuid(offerId), "PATCH", body, fx.admin);
const fulfillment = (ctx, fx, offerId, body) =>
  ctx.api("/v1/admin/offers/" + uuid(offerId) + "/fulfillment", "PUT", body, fx.admin);

async function addStock(ctx, fx, offerId, onHand) {
  const list = await ctx.api("/v1/admin/offers/inventory-locations", "GET", undefined, fx.admin);
  assert.equal(list.status, 200);
  assert.ok(Array.isArray(list.body) && list.body.length > 0, "No eligible active location");
  const provisioned = await provision(ctx, fx, offerId, list.body[0].locationId);
  assert.equal(provisioned.status, 200, JSON.stringify(provisioned.body));
  const detail = await get(ctx, fx, offerId);
  const row = detail.body.inventoryLocations.find(x => x.stockItemId === provisioned.body.stockItemId);
  assert.ok(row && row.revision, "Provisioned stock row or revision missing");
  const write = await inventory(ctx, fx, offerId, { onHand, reason: "P1C group C stock", stockItemId: row.stockItemId, expectedVersion: row.revision, operationId: randomUUID() });
  assert.equal(write.status, 200, "inventory " + JSON.stringify(write.body));
  return { stockItemId: provisioned.body.stockItemId, locationId: provisioned.body.locationId };
}

async function fractionalVariant(ctx, fx) {
  const forced = process.env.P1C_FRACTIONAL_VARIANT_ID;
  const candidates = forced
    ? [uuid(forced)]
    : ctx.sql("SELECT v.variant_id FROM catalog.variants v JOIN catalog.products p ON p.product_id=v.product_id WHERE v.status='Published' AND p.status='Published' AND p.quantity_decimal_places>0 ORDER BY v.variant_id LIMIT 30").split(/\r?\n/).filter(Boolean);
  for (const candidate of candidates) {
    const active = await ctx.api("/v1/admin/offers/active-listing?sellerPartyId=" + fx.seller + "&catalogVariantId=" + candidate + "&channel=" + encodeURIComponent(fx.channel), "GET", undefined, fx.admin);
    if (active.status === 200 && active.body === null) return uuid(candidate);
  }
  throw new NotRun("No unused published fractional-precision Catalog variant is available for an isolated fixture; refusing to reuse an existing listing.");
}

function deny(res, codes, label) {
  assert.ok(res.status >= 400 && res.status < 500, (label || "denial") + " expected 4xx, got " + res.status + " " + JSON.stringify(res.body));
  if (codes && codes.length) assert.ok(codes.includes(res.body?.errorCode), (label || "denial") + " errorCode " + res.body?.errorCode + " not in " + codes.join(","));
}

module.exports = {
  "R10-07-P": () => runCase("R10-07-P", async ctx => {
    const fx = await prepare(ctx);
    const integer = await createOffer(ctx, fx, fx.variant, { minimumOrderQuantity: 2, maximumOrderQuantity: 6 });
    let intDetail = await get(ctx, fx, integer.offerId);
    assert.equal(Number(intDetail.body.unitQuantityDecimalPlaces), 0, "Integer fixture must expose zero decimal places");
    const intStock = await addStock(ctx, fx, integer.offerId, 3);
    const intEdit = await patch(ctx, fx, integer.offerId, { status: "Draft", minimumOrderQuantity: 1, maximumOrderQuantity: 5, expectedVersion: intDetail.body.revisions.offer });
    assert.equal(intEdit.status, 200, JSON.stringify(intEdit.body));
    intDetail = await get(ctx, fx, integer.offerId);
    assert.equal(Number(intDetail.body.minimumOrderQuantity), 1);
    assert.equal(Number(intDetail.body.maximumOrderQuantity), 5);
    assert.equal(Number(intDetail.body.inventoryLocations.find(x => x.stockItemId === intStock.stockItemId).onHand), 3);

    const fractionVariant = await fractionalVariant(ctx, fx);
    const fractional = await createOffer(ctx, fx, fractionVariant, { minimumOrderQuantity: 0.5, maximumOrderQuantity: 2.5 });
    let fracDetail = await get(ctx, fx, fractional.offerId);
    assert.equal(Number(fracDetail.body.unitQuantityDecimalPlaces), 2, "Fractional fixture must expose two decimal places");
    const fracStock = await addStock(ctx, fx, fractional.offerId, 1.5);
    const fracEdit = await patch(ctx, fx, fractional.offerId, { status: "Draft", minimumOrderQuantity: 0.25, maximumOrderQuantity: 1.75, expectedVersion: fracDetail.body.revisions.offer });
    assert.equal(fracEdit.status, 200, JSON.stringify(fracEdit.body));
    fracDetail = await get(ctx, fx, fractional.offerId);
    assert.equal(Number(fracDetail.body.minimumOrderQuantity), 0.25);
    assert.equal(Number(fracDetail.body.maximumOrderQuantity), 1.75);
    assert.equal(Number(fracDetail.body.inventoryLocations.find(x => x.stockItemId === fracStock.stockItemId).onHand), 1.5);
    const state = ctx.observe("unit-values");
    const intOffer = state.offers.find(x => x.offer_id === integer.offerId);
    const fracOffer = state.offers.find(x => x.offer_id === fractional.offerId);
    assert.equal(Number(intOffer.minimum_order_quantity), 1);
    assert.equal(Number(intOffer.maximum_order_quantity), 5);
    assert.equal(Number(fracOffer.minimum_order_quantity), 0.25);
    assert.equal(Number(fracOffer.maximum_order_quantity), 1.75);
    assert.equal(Number(state.positions.find(x => x.offer_id === integer.offerId).on_hand), 3);
    assert.equal(Number(state.positions.find(x => x.offer_id === fractional.offerId).on_hand), 1.5);
    return { status: "PASS", reason: "Integer and fractional Catalog variants accepted their exact aligned limits and stock: 1/5 and 3, 0.25/1.75 and 1.5, with congruent independent GET and SQL reads." };
  }),

  "R10-07-N": () => runCase("R10-07-N", async ctx => {
    const fx = await prepare(ctx);
    const offer = await createOffer(ctx, fx, fx.variant, { minimumOrderQuantity: 2, maximumOrderQuantity: 6 });
    const before = await get(ctx, fx, offer.offerId);
    const wrongPrecision = await patch(ctx, fx, offer.offerId, { status: "Draft", minimumOrderQuantity: 1.5, expectedVersion: before.body.revisions.offer });
    deny(wrongPrecision, ["offer.min_quantity.invalid"], "integer wrong precision");
    const range = await patch(ctx, fx, offer.offerId, { status: "Draft", minimumOrderQuantity: 5, maximumOrderQuantity: 2, expectedVersion: before.body.revisions.offer });
    deny(range, ["validation.failed", "offer.validation.order_quantity_range", "offer.min_quantity.exceeds_max"], "min>max");
    const window = await patch(ctx, fx, offer.offerId, { status: "Draft", returnPolicyChoice: "Custom", customReturnWindowDays: 9999, expectedVersion: before.body.revisions.offer });
    deny(window, ["offer.return_policy.custom_window_out_of_range", "validation.failed"], "forbidden return window");
    const fractionVariant = await fractionalVariant(ctx, fx);
    const fractional = await createOffer(ctx, fx, fractionVariant);
    const fracBefore = await get(ctx, fx, fractional.offerId);
    const fracPrecision = await patch(ctx, fx, fractional.offerId, { status: "Draft", minimumOrderQuantity: 1.234, expectedVersion: fracBefore.body.revisions.offer });
    deny(fracPrecision, ["offer.min_quantity.invalid"], "fractional wrong precision");
    const after = await get(ctx, fx, offer.offerId);
    assert.equal(Number(after.body.minimumOrderQuantity), 2);
    assert.equal(Number(after.body.maximumOrderQuantity), 6);
    assert.equal(after.body.customReturnWindowDays ?? null, null);
    const fracAfter = await get(ctx, fx, fractional.offerId);
    assert.equal(fracAfter.body.minimumOrderQuantity ?? null, null);
    const state = ctx.observe("denied");
    assert.equal(state.offers.find(x => x.offer_id === offer.offerId).minimum_order_quantity !== undefined, true);
    assert.equal(Number(state.offers.find(x => x.offer_id === offer.offerId).minimum_order_quantity), 2);
    return { status: "PASS", reason: "Wrong precision on integer and fractional variants, min>max, and an out-of-range custom return window were rejected with typed errors while persisted limits and policy stayed unchanged." };
  }),

  "R10-07-B": () => runCase("R10-07-B", async ctx => {
    const fx = await prepare(ctx);
    const offer = await createOffer(ctx, fx, fx.variant, { minimumOrderQuantity: 2, maximumOrderQuantity: 6, returnPolicyChoice: "Custom", customReturnWindowDays: 9 });
    let detail = await get(ctx, fx, offer.offerId);
    const minDays = Number(detail.body.minReturnWindowDays);
    const maxDays = Number(detail.body.maxReturnWindowDays);
    assert.ok(minDays >= 1 && maxDays >= minDays, "Return window bounds missing");
    const cleared = await patch(ctx, fx, offer.offerId, { status: "Draft", clearOrderQuantityLimits: true, expectedVersion: detail.body.revisions.offer });
    assert.equal(cleared.status, 200, JSON.stringify(cleared.body));
    detail = await get(ctx, fx, offer.offerId);
    assert.equal(detail.body.minimumOrderQuantity ?? null, null, "Cleared minimum must be null");
    assert.equal(detail.body.maximumOrderQuantity ?? null, null, "Cleared maximum must be null");
    assert.equal(detail.body.returnPolicyChoice, "Custom");
    assert.equal(detail.body.customReturnWindowDays, 9);
    const atMin = await patch(ctx, fx, offer.offerId, { status: "Draft", returnPolicyChoice: "Custom", customReturnWindowDays: minDays, expectedVersion: detail.body.revisions.offer });
    assert.equal(atMin.status, 200, JSON.stringify(atMin.body));
    detail = await get(ctx, fx, offer.offerId);
    assert.equal(detail.body.customReturnWindowDays, minDays);
    const atMax = await patch(ctx, fx, offer.offerId, { status: "Draft", returnPolicyChoice: "Custom", customReturnWindowDays: maxDays, expectedVersion: detail.body.revisions.offer });
    assert.equal(atMax.status, 200, JSON.stringify(atMax.body));
    detail = await get(ctx, fx, offer.offerId);
    assert.equal(detail.body.customReturnWindowDays, maxDays);
    const belowMin = await patch(ctx, fx, offer.offerId, { status: "Draft", returnPolicyChoice: "Custom", customReturnWindowDays: minDays - 1, expectedVersion: detail.body.revisions.offer });
    deny(belowMin, ["offer.return_policy.custom_window_out_of_range", "validation.failed"], "below minimum days");
    const final = await get(ctx, fx, offer.offerId);
    assert.equal(final.body.customReturnWindowDays, maxDays, "Rejected boundary must not change the retained policy");
    assert.equal(final.body.minimumOrderQuantity ?? null, null);
    assert.equal(final.body.maximumOrderQuantity ?? null, null);
    const state = ctx.observe("cleared-boundary");
    const row = state.offers.find(x => x.offer_id === offer.offerId);
    assert.equal(row.minimum_order_quantity, null);
    assert.equal(row.maximum_order_quantity, null);
    assert.equal(row.return_policy_choice, "Custom");
    assert.equal(row.custom_return_window_days, maxDays);
    return { status: "PASS", reason: "Clearing limits produced true nulls while retaining the Custom policy; return-window boundaries " + minDays + " and " + maxDays + " persisted exactly and the below-boundary value was rejected without changing state." };
  }),

  "R10-08-P": () => runCase("R10-08-P", async ctx => {
    const fx = await prepare(ctx);
    const offer = await createOffer(ctx, fx);
    const stock = await addStock(ctx, fx, offer.offerId, 5);
    const priced = await price(ctx, fx, offer.offerId, { amount: 123456, currency: null, market: null, expectedVersion: "absent", operationId: randomUUID() });
    assert.equal(priced.status, 200, JSON.stringify(priced.body));
    let detail = await get(ctx, fx, offer.offerId);
    assert.equal(Number(detail.body.amount), 123456);
    assert.equal(detail.body.currency, "IRR");
    assert.ok(detail.body.revisions.price && detail.body.revisions.price !== "absent");
    const shippingType = ctx.sql("SELECT shipping_type_id FROM fulfillment.shipping_types WHERE is_active ORDER BY sort_order LIMIT 1");
    assert.match(shippingType, /^[0-9a-f-]{36}$/i, "Active shipping type missing");
    const assigned = await fulfillment(ctx, fx, offer.offerId, { responsibility: "Seller", shippingTypeId: shippingType, expectedVersion: detail.body.revisions.offer });
    assert.equal(assigned.status, 200, JSON.stringify(assigned.body));
    detail = await get(ctx, fx, offer.offerId);
    assert.equal(detail.body.fulfillmentResponsibility, "Seller");
    assert.equal(detail.body.fulfillmentShippingTypeId, shippingType);
    assert.equal(Number(detail.body.amount), 123456);
    assert.equal(detail.body.currency, "IRR");
    const state = ctx.observe("price-shipping");
    assert.equal(state.prices.length, 1);
    assert.equal(Number(state.prices[0].amount), 123456);
    assert.equal(state.prices[0].currency, "IRR");
    assert.equal(state.priceFacts.length, 1);
    const row = state.offers.find(x => x.offer_id === offer.offerId);
    assert.equal(row.fulfillment_responsibility, "Seller");
    assert.equal(row.fulfillment_shipping_type_id, shippingType);
    assert.equal(Number(state.positions.find(x => x.stock_item_id === stock.stockItemId).on_hand), 5);
    return { status: "PASS", reason: "Base price 123456 IRR and authorized Seller fulfillment with a real active shipping type both persisted and reread exactly from the owning Pricing/Offer stores." };
  }),

  "R10-08-N": () => runCase("R10-08-N", async ctx => {
    const fx = await prepare(ctx);
    const offer = await createOffer(ctx, fx);
    await addStock(ctx, fx, offer.offerId, 5);
    const first = await price(ctx, fx, offer.offerId, { amount: 100000, currency: null, market: null, expectedVersion: "absent", operationId: randomUUID() });
    assert.equal(first.status, 200, JSON.stringify(first.body));
    const before = await get(ctx, fx, offer.offerId);
    const problems = [];
    const nullAmount = await price(ctx, fx, offer.offerId, { amount: null, currency: null, market: null, expectedVersion: before.body.revisions.price, operationId: randomUUID() });
    if (!(nullAmount.status >= 400 && nullAmount.status < 500)) problems.push("blank null price returned " + nullAmount.status + " " + (nullAmount.body?.errorCode || ""));
    const afterNull = await get(ctx, fx, offer.offerId);
    if (Number(afterNull.body.amount) !== 100000) problems.push("blank null price changed persisted amount to " + afterNull.body.amount);
    const omittedAmount = await price(ctx, fx, offer.offerId, { currency: null, market: null, expectedVersion: afterNull.body.revisions.price, operationId: randomUUID() });
    if (omittedAmount.status >= 200 && omittedAmount.status < 300) problems.push("blank omitted price persisted amount " + omittedAmount.body?.amount + " instead of a typed error");
    const ownerHeaders = { [S.ADMIN_HEADER]: uuid(fx.self.actorUserId), [S.SELLER_HEADER]: uuid(fx.self.sellerPartyId) };
    const sellerFulfillment = await ctx.api("/v1/admin/offers/" + offer.offerId + "/fulfillment", "PUT", { responsibility: "Seller", shippingTypeId: null }, ownerHeaders);
    if (![401, 403].includes(sellerFulfillment.status)) problems.push("seller admin fulfillment returned " + sellerFulfillment.status);
    const after = await get(ctx, fx, offer.offerId);
    assert.equal(after.body.fulfillmentResponsibility ?? null, null, "Unauthorized seller fulfillment must not write responsibility");
    const state = ctx.observe("blank-and-unauthorized");
    assert.equal(state.priceFacts.length, 1, "Only the initial explicit price fact may exist");
    assert.equal(state.prices.length, 1);
    assert.equal(Number(state.prices[0].amount), 100000);
    assert.ok(state.offers.every(x => x.fulfillment_responsibility === null));
    assert.equal(problems.length, 0, problems.join("; "));
    return { status: "PASS", reason: "Blank price variants were rejected as typed 4xx without write and an unauthorized seller fulfillment attempt was denied without writing responsibility." };
  }),

  "R10-08-B": () => runCase("R10-08-B", async ctx => {
    const fx = await prepare(ctx);
    const offer = await createOffer(ctx, fx);
    await addStock(ctx, fx, offer.offerId, 5);
    const zero = await price(ctx, fx, offer.offerId, { amount: 0, currency: null, market: null, expectedVersion: "absent", operationId: randomUUID() });
    assert.equal(zero.status, 200, "Policy-approved zero price must persist: " + JSON.stringify(zero.body));
    let detail = await get(ctx, fx, offer.offerId);
    assert.equal(Number(detail.body.amount), 0);
    assert.equal(detail.body.currency, "IRR");
    const negative = await price(ctx, fx, offer.offerId, { amount: -1, currency: null, market: null, expectedVersion: detail.body.revisions.price, operationId: randomUUID() });
    deny(negative, ["validation.failed", "pricing.amount.invalid"], "negative price");
    detail = await get(ctx, fx, offer.offerId);
    assert.equal(Number(detail.body.amount), 0, "Denied negative price must not overwrite zero");
    const shippingType = ctx.sql("SELECT shipping_type_id FROM fulfillment.shipping_types WHERE is_active ORDER BY sort_order LIMIT 1");
    const assign = await fulfillment(ctx, fx, offer.offerId, { responsibility: "Seller", shippingTypeId: shippingType, expectedVersion: detail.body.revisions.offer });
    assert.equal(assign.status, 200, JSON.stringify(assign.body));
    detail = await get(ctx, fx, offer.offerId);
    assert.equal(detail.body.fulfillmentResponsibility, "Seller");
    const orphanType = await fulfillment(ctx, fx, offer.offerId, { responsibility: null, shippingTypeId: shippingType, expectedVersion: detail.body.revisions.offer });
    deny(orphanType, ["offer.fulfillment.type_without_responsibility"], "shipping type without responsibility");
    detail = await get(ctx, fx, offer.offerId);
    assert.equal(detail.body.fulfillmentResponsibility, "Seller");
    const cleared = await fulfillment(ctx, fx, offer.offerId, { responsibility: null, shippingTypeId: null, expectedVersion: detail.body.revisions.offer });
    assert.equal(cleared.status, 200, JSON.stringify(cleared.body));
    detail = await get(ctx, fx, offer.offerId);
    assert.equal(detail.body.fulfillmentResponsibility ?? null, null);
    assert.equal(detail.body.fulfillmentShippingTypeId ?? null, null);
    const state = ctx.observe("zero-clear");
    assert.equal(state.prices.length, 1);
    assert.equal(Number(state.prices[0].amount), 0);
    assert.equal(state.priceFacts.length, 1, "Denied negative price must not add a fact");
    assert.equal(state.offers.find(x => x.offer_id === offer.offerId).fulfillment_responsibility, null);
    return { status: "PASS", reason: "Policy-approved zero price persisted, negative price was denied without overwrite, and shipping assignment cleared to true null while an orphan shipping type was rejected by owner policy." };
  }),
};
