// TB-296 P1-FINAL-ACCEPTANCE — G2 OWN-08 strengthened executable oracle.
// Real admin HTTP against the served DEV Host (5088) + independent PostgreSQL on tooba_alpha.
// Focus: SIMULTANEOUS price and physical-inventory concurrency with exact winners, independent
// owner revisions, durable owner facts and outbox assertions. Isolated P1C-owned fixture only.
const { randomUUID } = require("node:crypto");
const S = require("./p1c-support.cjs");
const { assert, uuid, deterministicId, runCase, SKU_PREFIX } = S;

const ADMIN_HEADER = "X-Tooba-Dev-Actor-User-Id";
const SELLER = uuid(process.env.P1C_SELLER_ID || "01a030d1-40cb-7000-8abe-6d31739956c5");

async function main() {
  const result = await runCase("OWN-08", async ctx => {
    const dev = await ctx.api("/v1/admin/dev-context");
    assert.equal(dev.status, 200);
    const admin = { [ADMIN_HEADER]: uuid(dev.body.actorUserId) };

    const variant = ctx.sql("SELECT v.variant_id FROM catalog.variants v JOIN catalog.products p ON p.product_id=v.product_id " +
      "WHERE v.status='Published' AND p.quantity_decimal_places=0 " +
      "AND NOT EXISTS (SELECT 1 FROM offer.offers o WHERE o.seller_party_id='" + SELLER + "' AND o.catalog_variant_id=v.variant_id) " +
      "ORDER BY v.variant_id LIMIT 1").trim();
    assert.match(variant, /^[0-9a-f-]{36}$/i, "no isolated variant");

    const requestId = "P1C-" + randomUUID();
    const offerId = deterministicId(SELLER, variant, requestId);
    ctx.addOffer(offerId);
    const created = await ctx.api("/v1/admin/offers", "POST", {
      sellerPartyId: SELLER, catalogVariantId: variant, channel: "Marketplace",
      sellerSku: SKU_PREFIX + randomUUID(), status: "Draft", requestId,
    }, admin);
    assert.equal(created.status, 201, "create failed: " + created.status + " " + JSON.stringify(created.body));
    assert.equal(uuid(created.body.offerId), offerId);

    const detail0 = await ctx.api("/v1/admin/offers/" + offerId, "GET", undefined, admin);
    const assign = await ctx.api("/v1/admin/offers/" + offerId + "/fulfillment", "PUT",
      { responsibility: "Platform", shippingTypeId: null, expectedVersion: detail0.body.revisions.offer }, admin);
    assert.equal(assign.status, 200, "assign failed: " + assign.status + " " + JSON.stringify(assign.body));

    const locations = await ctx.api("/v1/admin/offers/inventory-locations", "GET", undefined, admin);
    assert.equal(locations.status, 200);
    const provisioned = await ctx.api("/v1/admin/offers/" + offerId + "/inventory/positions", "POST", { locationId: locations.body[0].locationId }, admin);
    assert.equal(provisioned.status, 200, "provision failed");
    const stockItemId = provisioned.body.stockItemId;

    const setPhysical = async (onHand, reason, operationId) => {
      const d = await ctx.api("/v1/admin/offers/" + offerId, "GET", undefined, admin);
      const row = d.body.inventoryLocations.find(x => x.stockItemId === stockItemId);
      assert.ok(row && row.revision, "revision missing");
      return ctx.api("/v1/admin/offers/" + offerId + "/inventory", "PUT",
        { onHand, reason, stockItemId, expectedVersion: row.revision, operationId: operationId || randomUUID() }, admin);
    };
    const initial = await setPhysical(100, "OWN-08 initial physical");
    assert.equal(initial.status, 200, "initial physical failed: " + initial.status + " " + JSON.stringify(initial.body));

    // ---- Simultaneous PRICE concurrency on one real owner revision.
    const basePrice = await ctx.api("/v1/admin/offers/" + offerId + "/price", "PUT",
      { amount: 5000, currency: null, market: null, expectedVersion: "absent", operationId: randomUUID() }, admin);
    assert.equal(basePrice.status, 200, "base price failed: " + basePrice.status + " " + JSON.stringify(basePrice.body));
    const atBase = await ctx.api("/v1/admin/offers/" + offerId, "GET", undefined, admin);
    const priceRev = atBase.body.revisions.price;
    assert.notEqual(priceRev, "absent");
    const pricePayloads = [{ amount: 6000, op: randomUUID() }, { amount: 7000, op: randomUUID() }];
    const priceRace = await Promise.all(pricePayloads.map(p => ctx.api("/v1/admin/offers/" + offerId + "/price", "PUT",
      { amount: p.amount, currency: null, market: null, expectedVersion: priceRev, operationId: p.op }, admin)));
    const priceWins = priceRace.filter(r => r.status === 200);
    const priceLosses = priceRace.filter(r => r.status >= 400);
    assert.equal(priceWins.length, 1, "price race must have exactly one winner: " + JSON.stringify(priceRace.map(r => r.status)));
    assert.equal(priceLosses.length, 1, "price race must have exactly one loser");
    assert.equal(priceLosses[0].body.errorCode, "concurrency.conflict", "price loser code wrong: " + JSON.stringify(priceLosses[0].body));
    const winningAmount = pricePayloads[priceRace.indexOf(priceWins[0])].amount;
    const afterPrice = await ctx.api("/v1/admin/offers/" + offerId, "GET", undefined, admin);
    assert.equal(Number(afterPrice.body.amount), winningAmount, "price winner payload not persisted: " + afterPrice.body.amount + " vs " + winningAmount);
    assert.notEqual(afterPrice.body.revisions.price, priceRev, "price winner did not advance the price revision");
    ctx.trace.priceRace = { priceRev, winnerAmount: winningAmount, statuses: priceRace.map(r => ({ status: r.status, errorCode: r.body && r.body.errorCode })) };

    // ---- Simultaneous PHYSICAL INVENTORY concurrency on one real position revision.
    const beforeStock = await ctx.api("/v1/admin/offers/" + offerId, "GET", undefined, admin);
    const stockRev = beforeStock.body.inventoryLocations.find(x => x.stockItemId === stockItemId).revision;
    const stockPayloads = [{ onHand: 90, op: randomUUID(), reason: "OWN-08 race 90" }, { onHand: 80, op: randomUUID(), reason: "OWN-08 race 80" }];
    const stockRace = await Promise.all(stockPayloads.map(p => ctx.api("/v1/admin/offers/" + offerId + "/inventory", "PUT",
      { onHand: p.onHand, reason: p.reason, stockItemId, expectedVersion: stockRev, operationId: p.op }, admin)));
    const stockWins = stockRace.filter(r => r.status === 200);
    const stockLosses = stockRace.filter(r => r.status >= 400);
    assert.equal(stockWins.length, 1, "inventory race must have exactly one winner: " + JSON.stringify(stockRace.map(r => r.status)));
    assert.equal(stockLosses.length, 1, "inventory race must have exactly one loser");
    assert.equal(stockLosses[0].body.errorCode, "inventory.position.revision_conflict", "inventory loser code wrong: " + JSON.stringify(stockLosses[0].body));
    const winningStock = stockPayloads[stockRace.indexOf(stockWins[0])].onHand;
    const afterStock = await ctx.api("/v1/admin/offers/" + offerId, "GET", undefined, admin);
    const rowAfter = afterStock.body.inventoryLocations.find(x => x.stockItemId === stockItemId);
    assert.equal(Number(rowAfter.onHand), winningStock, "inventory winner payload not persisted: " + rowAfter.onHand + " vs " + winningStock);
    assert.ok(Number(rowAfter.available) >= 0, "negative availability after inventory race");
    ctx.trace.stockRace = { stockRev, winnerOnHand: winningStock, statuses: stockRace.map(r => ({ status: r.status, errorCode: r.body && r.body.errorCode })) };

    // ---- Independent owner revisions + durable facts + outbox from the real DB.
    const state = ctx.observe("after-both-races");
    assert.equal(state.offers.length, 1);
    assert.equal(state.prices.length, 1, "expected exactly one price row");
    assert.equal(state.priceFacts.length, 2, "price facts must be base + one winning race (loser wrote none): " + state.priceFacts.length);
    assert.equal(state.positions.length, 1);
    assert.equal(state.inventoryFacts.length, 2, "inventory facts must be initial + one winning race (loser wrote none): " + state.inventoryFacts.length);
    const winnerPriceFact = state.priceFacts.find(f => Number(f.new_amount) === winningAmount);
    assert.ok(winnerPriceFact, "no durable price fact for the winning payload");
    const winnerStockFact = state.inventoryFacts.find(f => Number(f.new_on_hand) === winningStock);
    assert.ok(winnerStockFact, "no durable inventory fact for the winning payload");
    assert.notEqual(afterStock.body.revisions.price, rowAfter.revision, "price and physical revisions are not independent");
    assert.ok(afterStock.body.revisions.price !== afterStock.body.revisions.offeredQuantity, "price and allocation revisions are not independent");
    ctx.trace.outbox = {
      price: state.priceOutbox.map(m => ({ eventType: m.event_type, processed: !!m.processed_at })),
      inventory: state.inventoryOutbox.map(m => ({ eventType: m.event_type, processed: !!m.processed_at })),
    };
    assert.ok(state.priceOutbox.length >= 1, "price change did not publish any outbox event");
    assert.ok(state.inventoryOutbox.length >= 1, "inventory change did not publish any outbox event");

    return {
      status: "PASS",
      reason: "Two simultaneous owner-revision races on the real Host: exactly one price winner (" + winningAmount + ") and one physical-stock winner (" + winningStock + "), single typed 409 concurrency.conflict / inventory.position.revision_conflict losers, persisted winner payloads, two durable price facts and two durable inventory facts (no loser facts), independent price/physical/allocation revisions, and published price+inventory outbox events.",
    };
  });
  console.log(JSON.stringify({ id: "OWN-08", status: result.status, reason: result.reason }));
  if (result.status !== "PASS") process.exit(1);
}

main().catch(error => { console.error(error); process.exit(1); });
