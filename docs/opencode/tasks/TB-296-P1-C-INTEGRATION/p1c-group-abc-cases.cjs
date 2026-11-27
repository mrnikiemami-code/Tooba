// TB-296 P1-C — executable oracles for R10-01..R10-04 (group A) composed with the real
// group B (p1c-edit-history.cjs) and group C (p1c-pricing-fulfillment.cjs) handlers.
//
// Test-only. Real admin HTTP against the served DEV Host (5088) plus independent PostgreSQL
// reads against the served DEV database. Isolated run-owned P1C- fixtures only, exact cleanup
// in finally via p1c-support.runCase. The UI-presentation clauses (tab-switch preservation,
// committed-draft clearing, uncertain-response recovery) are executed by running the REAL shared
// workspace mounted regression (src/frontend/app/admin/offers/offer-workspace.mount.test.ts)
// against the shipped component and asserting the exact test identities. No product, guard,
// contract or immutable-matrix edits.

const { randomUUID } = require("node:crypto");
const { execFileSync } = require("node:child_process");
const fs = require("node:fs");
const path = require("node:path");

const S = require("./p1c-support.cjs");
const historyCases = require("./p1c-edit-history.cjs");
const pricingCases = require("./p1c-pricing-fulfillment.cjs");

const { assert, uuid, deterministicId, runCase, NotRun, SKU_PREFIX, EMPTY } = S;

const FRONTEND_DIR = path.resolve(__dirname, "../../../..", "src/frontend");
const MOUNT_TEST = "app/admin/offers/offer-workspace.mount.test.ts";

let mountCache = null;
// Executes the real shared-workspace mounted regression once per process and asserts that the
// exact required test identities passed. Raw output is persisted immediately as run evidence.
function mountRegression(requiredNames) {
  const root = process.env.P1C_EVIDENCE_DIR;
  assert.ok(root, "P1C_EVIDENCE_DIR must be a run-owned directory");
  fs.mkdirSync(root, { recursive: true });
  if (!mountCache) {
    const logPath = path.join(root, "p1c-mount-regression.log");
    let raw = "";
    let code = 0;
    try {
      raw = execFileSync(
        process.env.ComSpec || "cmd.exe",
        ["/c", "npx --no-install tsx --tsconfig tsconfig.mount-tests.json --test " + MOUNT_TEST],
        { cwd: FRONTEND_DIR, encoding: "utf8", timeout: 180000, windowsHide: true, maxBuffer: 64 * 1024 * 1024 },
      );
    } catch (error) {
      raw = String(error && error.stdout ? error.stdout : "") + "\n" + String(error && error.stderr ? error.stderr : "");
      code = error && typeof error.status === "number" ? error.status : 1;
    }
    const out = raw.replace(/\x1b\[[0-9;]*m/g, "");
    fs.writeFileSync(logPath, out, { flag: "wx" });
    const pass = /(?:^|\s)pass\s+(\d+)/m.exec(out);
    const fail = /(?:^|\s)fail\s+(\d+)/m.exec(out);
    mountCache = { code, out, logPath, pass: pass ? Number(pass[1]) : null, fail: fail ? Number(fail[1]) : null };
  }
  const result = mountCache;
  assert.equal(result.code, 0, "mounted regression exited non-zero");
  assert.ok(result.pass != null && result.pass > 0, "mounted regression reported no passing tests");
  assert.equal(result.fail, 0, "mounted regression reported failing tests");
  const lines = result.out.split(/\r?\n/);
  for (const name of requiredNames) {
    const hit = lines.find(line => line.includes("\u2714") && line.includes(name));
    assert.ok(hit, "required mounted regression did not pass: " + name);
  }
  return { logPath: result.logPath, total: result.pass, required: requiredNames };
}

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
  assert.ok(Array.isArray(channels.body) && channels.body.length > 0, "No authorized sales channel");
  const contextsRes = await ctx.api("/v1/seller/dev-contexts");
  const contexts = (contextsRes.body && contextsRes.body.actors) || [];
  assert.ok(contexts.some(x => x.sellerPartyId.toLowerCase() === seller), "Seller is not a current DEV synthetic identity");
  const fx = { seller, variant, channels: channels.body, channel: channels.body[0], actor, admin, contexts };
  ctx.trace.fixture = fx;
  return fx;
}

async function createOffer(ctx, fx, extra = {}, options = {}) {
  const requestId = "P1C-" + randomUUID();
  const variantId = options.variant || fx.variant;
  const channelId = options.channel || fx.channel;
  const offerId = deterministicId(fx.seller, variantId, requestId);
  ctx.addOffer(offerId);
  assert.ok(EMPTY(ctx.observe("before-" + requestId.slice(-8))), "Candidate identity is not isolated (including archived rows)");
  const sku = SKU_PREFIX + randomUUID();
  const res = await ctx.api("/v1/admin/offers", "POST", {
    sellerPartyId: fx.seller,
    catalogVariantId: variantId,
    channel: channelId,
    sellerSku: sku,
    status: "Draft",
    requestId,
    ...extra,
  }, fx.admin);
  assert.equal(res.status, 201, "create failed: " + JSON.stringify(res.body));
  assert.equal(res.body.offerId, offerId);
  return { requestId, offerId, sku, variant: variantId, channel: channelId, response: res };
}

const get = (ctx, fx, offerId) => ctx.api("/v1/admin/offers/" + uuid(offerId), "GET", undefined, fx.admin);
const patch = (ctx, fx, offerId, body) => ctx.api("/v1/admin/offers/" + uuid(offerId), "PATCH", body, fx.admin);
const provision = (ctx, fx, offerId, locationId) =>
  ctx.api("/v1/admin/offers/" + uuid(offerId) + "/inventory/positions", "POST", { locationId }, fx.admin);
const inventory = (ctx, fx, offerId, body) =>
  ctx.api("/v1/admin/offers/" + uuid(offerId) + "/inventory", "PUT", body, fx.admin);

async function eligibleLocations(ctx, fx) {
  const list = await ctx.api("/v1/admin/offers/inventory-locations", "GET", undefined, fx.admin);
  assert.equal(list.status, 200);
  assert.ok(Array.isArray(list.body), "Inventory locations must be a list");
  return list.body;
}

const groupA = {
  "R10-01-P": () => runCase("R10-01-P", async ctx => {
    const fx = await prepare(ctx);
    const selected = fx.channels[0];
    const active = await ctx.api(
      "/v1/admin/offers/active-listing?sellerPartyId=" + fx.seller + "&catalogVariantId=" + fx.variant + "&channel=" + encodeURIComponent(selected),
      "GET", undefined, fx.admin);
    assert.equal(active.status, 200);
    assert.equal(active.body, null, "An active listing already exists for the isolated identity");
    const created = await createOffer(ctx, fx, {}, { channel: selected });
    assert.equal(created.response.body.channel, selected);
    assert.equal(created.response.body.status, "Draft");
    const detail = await get(ctx, fx, created.offerId);
    assert.equal(detail.status, 200);
    assert.equal(detail.body.sellerPartyId, fx.seller);
    assert.equal(detail.body.catalogVariantId, fx.variant);
    assert.equal(detail.body.channel, selected);
    assert.equal(detail.body.status, "Draft");
    const state = ctx.observe("created");
    assert.equal(state.offers.length, 1, "Exactly one Draft must exist");
    assert.equal(state.offers[0].offer_id, created.offerId);
    assert.equal(state.offers[0].channel, selected);
    assert.equal(state.offers[0].status, "Draft");
    assert.equal(state.history.length, 1);
    return {
      status: "PASS",
      reason: "Eligible DEV identity created exactly one Draft with the selected allowed channel '" + selected + "', confirmed by the owner GET and independent SQL.",
    };
  }),

  "R10-01-B": () => runCase("R10-01-B", async ctx => {
    const fx = await prepare(ctx);
    const enumerated = ["Direct", "Marketplace", "Agency", "Corporate", "Affiliate", "Api"];
    const allowed = fx.channels;
    const unauthorized = enumerated.filter(channel => !allowed.includes(channel));
    ctx.trace.channelAuthority = { allowed, unauthorized };
    // Record the real denial that precedes intent evaluation for a changed channel.
    const requestId = "P1C-" + randomUUID();
    const offerId = deterministicId(fx.seller, fx.variant, requestId);
    ctx.addOffer(offerId);
    assert.ok(EMPTY(ctx.observe("before")));
    if (unauthorized.length) {
      const denied = await ctx.api("/v1/admin/offers", "POST", {
        sellerPartyId: fx.seller, catalogVariantId: fx.variant, channel: unauthorized[0],
        sellerSku: SKU_PREFIX + randomUUID(), status: "Draft", requestId,
      }, fx.admin);
      assert.ok(denied.status >= 400 && denied.status < 500, "Unauthorized channel must be denied, got " + denied.status);
      assert.equal(denied.body.errorCode, "offer.channel.not_allowed");
      assert.ok(EMPTY(ctx.observe("denied")), "Denied channel must not write");
    }
    if (allowed.length < 2) {
      throw new NotRun("R10-01-B requires the same variant in two independently allowed channels. The served SingleStore DEV tenant exposes only [" +
        allowed.join(",") + "]; the other enumerated channels [" + unauthorized.join(",") +
        "] would require a second authorized StoreCommerce context / edition switch, which is not authorized here. A changed channel cannot reach intent-conflict evaluation because it is denied earlier with offer.channel.not_allowed (observed).");
    }
    throw new NotRun("R10-01-B two-channel intent-conflict sub-case is not reachable with the current authorized contexts.");
  }),

  "R10-02-P": () => runCase("R10-02-P", async ctx => {
    const fx = await prepare(ctx);
    const fields = { returnPolicyChoice: "Custom", customReturnWindowDays: 9, minimumOrderQuantity: 2, maximumOrderQuantity: 6 };
    const created = await createOffer(ctx, fx, fields);
    const detail = await get(ctx, fx, created.offerId);
    assert.equal(detail.status, 200);
    assert.equal(detail.body.sellerSku, created.sku);
    assert.equal(detail.body.returnPolicyChoice, "Custom");
    assert.equal(Number(detail.body.customReturnWindowDays), 9);
    assert.equal(Number(detail.body.minimumOrderQuantity), 2);
    assert.equal(Number(detail.body.maximumOrderQuantity), 6);
    assert.equal(detail.body.status, "Draft");
    // A single populated save: an identical replay must be a deterministic no-op, not a second Draft.
    const replay = await ctx.api("/v1/admin/offers", "POST", {
      sellerPartyId: fx.seller, catalogVariantId: fx.variant, channel: fx.channel,
      sellerSku: created.sku, status: "Draft", requestId: created.requestId, ...fields,
    }, fx.admin);
    assert.equal(replay.status, 201, "identical replay must succeed: " + JSON.stringify(replay.body));
    assert.equal(replay.body.offerId, created.offerId);
    const detail2 = await get(ctx, fx, created.offerId);
    assert.deepEqual(detail2.body, detail.body, "Replay must not change the durable Draft");
    const state = ctx.observe("saved-once");
    assert.equal(state.offers.length, 1, "One populated save must yield exactly one Draft");
    assert.equal(state.history.length, 1);
    assert.equal(state.offers[0].return_policy_choice, "Custom");
    assert.equal(Number(state.offers[0].custom_return_window_days), 9);
    assert.equal(Number(state.offers[0].minimum_order_quantity), 2);
    assert.equal(Number(state.offers[0].maximum_order_quantity), 6);
    const ui = mountRegression(["tab switches preserve typed draft values (UX-03)"]);
    return {
      status: "PASS",
      reason: "One populated save persisted exactly one durable Draft (SKU, Custom return policy 9, limits 2/6) confirmed by owner GET and SQL; the real shared-workspace mounted regression preserved typed values across tab switches (log " + path.basename(ui.logPath) + ").",
    };
  }),

  "R10-02-N": () => runCase("R10-02-N", async ctx => {
    const fx = await prepare(ctx);
    const attempts = [];
    async function attempt(label, extra) {
      const requestId = "P1C-" + randomUUID();
      const offerId = deterministicId(fx.seller, fx.variant, requestId);
      ctx.addOffer(offerId);
      const res = await ctx.api("/v1/admin/offers", "POST", {
        sellerPartyId: fx.seller, catalogVariantId: fx.variant, channel: fx.channel,
        sellerSku: SKU_PREFIX + randomUUID(), status: "Draft", requestId, ...extra,
      }, fx.admin);
      attempts.push({ label, status: res.status, errorCode: res.body && res.body.errorCode });
      return res;
    }
    const range = await attempt("min>max", { minimumOrderQuantity: 6, maximumOrderQuantity: 2 });
    const policy = await attempt("custom window out of range", { returnPolicyChoice: "Custom", customReturnWindowDays: 9999 });
    for (const pair of [[range, "min>max"], [policy, "custom window"]]) {
      const res = pair[0];
      assert.ok(res.status >= 400 && res.status < 500, pair[1] + " must be a typed 4xx, got " + res.status + " " + JSON.stringify(res.body));
      assert.ok(res.body && res.body.errorCode, pair[1] + " must carry a typed errorCode");
    }
    const state = ctx.observe("after-invalid");
    assert.ok(EMPTY(state), "Validation failures must not write: " + JSON.stringify(Object.fromEntries(Object.entries(state).map(([k, v]) => [k, v.length]))));
    ctx.trace.attempts = attempts;
    return {
      status: "PASS",
      reason: "Invalid drafts (min>max and out-of-range custom return window) were rejected as typed 4xx before any Offer-owned write; independent SQL shows zero offers, history and outbox rows.",
    };
  }),

  "R10-02-B": () => runCase("R10-02-B", async ctx => {
    const fx = await prepare(ctx);
    const requestId = "P1C-" + randomUUID();
    const offerId = deterministicId(fx.seller, fx.variant, requestId);
    ctx.addOffer(offerId);
    assert.ok(EMPTY(ctx.observe("before")));
    const sku = SKU_PREFIX + randomUUID();
    const payload = { sellerPartyId: fx.seller, catalogVariantId: fx.variant, channel: fx.channel, sellerSku: sku, status: "Draft", requestId };
    const send = () => ctx.api("/v1/admin/offers", "POST", payload, fx.admin);
    const responses = await Promise.all([send(), send(), send()]);
    for (const res of responses) {
      const replay = res.status === 201 && res.body && res.body.offerId === offerId;
      const duplicate = res.status === 409 && res.body && res.body.errorCode === "offer.listing.duplicate_active";
      assert.ok(replay || duplicate, "concurrent identical create must be a deterministic replay or duplicate_active, got " + res.status + " " + JSON.stringify(res.body));
    }
    assert.ok(responses.some(res => res.status === 201 && res.body.offerId === offerId), "At least one concurrent request must create/replay the deterministic offer");
    // A later serial replay of the same intent must deterministically resolve the same Offer.
    const finalReplay = await ctx.api("/v1/admin/offers", "POST", payload, fx.admin);
    assert.equal(finalReplay.status, 201, "serial replay must succeed: " + JSON.stringify(finalReplay.body));
    assert.equal(finalReplay.body.offerId, offerId);
    const state = ctx.observe("after-concurrent");
    assert.equal(state.offers.length, 1, "Double submit must create exactly one Offer");
    assert.equal(state.offers[0].offer_id, offerId);
    assert.equal(state.offers[0].seller_sku, sku);
    assert.equal(state.history.length, 1, "Exactly one Created history entry");
    assert.equal(state.offerOutbox.length, 1, "Exactly one creation outbox message");
    const ui = mountRegression(["rapid double submit creates exactly one draft"]);
    return {
      status: "PASS",
      reason: "Three concurrent identical create intents resolved to one durable Offer with one Created history entry and one outbox message (deterministic replay); the shared-workspace mounted regression also created exactly one draft on rapid double submit (log " + path.basename(ui.logPath) + ").",
    };
  }),

  "R10-03-P": () => runCase("R10-03-P", async ctx => {
    const fx = await prepare(ctx);
    const offer = await createOffer(ctx, fx);
    const locations = await eligibleLocations(ctx, fx);
    assert.ok(locations.length >= 2, "Need two eligible active locations, got " + locations.length);
    const loc0 = locations[0];
    const loc1 = locations[1];
    const p0 = await provision(ctx, fx, offer.offerId, loc0.locationId);
    const p1 = await provision(ctx, fx, offer.offerId, loc1.locationId);
    assert.equal(p0.status, 200, JSON.stringify(p0.body));
    assert.equal(p1.status, 200, JSON.stringify(p1.body));
    let detail = await get(ctx, fx, offer.offerId);
    const row0 = detail.body.inventoryLocations.find(x => x.stockItemId === p0.body.stockItemId);
    const row1 = detail.body.inventoryLocations.find(x => x.stockItemId === p1.body.stockItemId);
    assert.ok(row0 && row1 && row0.revision && row1.revision, "Provisioned rows/revisions missing");
    const w0 = await inventory(ctx, fx, offer.offerId, { onHand: 7, reason: "P1C loc0", stockItemId: row0.stockItemId, expectedVersion: row0.revision, operationId: randomUUID() });
    const w1 = await inventory(ctx, fx, offer.offerId, { onHand: 4, reason: "P1C loc1", stockItemId: row1.stockItemId, expectedVersion: row1.revision, operationId: randomUUID() });
    assert.equal(w0.status, 200, JSON.stringify(w0.body));
    assert.equal(w1.status, 200, JSON.stringify(w1.body));
    detail = await get(ctx, fx, offer.offerId);
    const a0 = detail.body.inventoryLocations.find(x => x.stockItemId === p0.body.stockItemId);
    const a1 = detail.body.inventoryLocations.find(x => x.stockItemId === p1.body.stockItemId);
    assert.equal(Number(a0.onHand), 7);
    assert.equal(Number(a0.reserved), 0);
    assert.equal(Number(a0.available), 7);
    assert.equal(Number(a1.onHand), 4);
    assert.equal(Number(a1.reserved), 0);
    assert.equal(Number(a1.available), 4);
    const state = ctx.observe("two-positions");
    assert.equal(state.positions.length, 2, "Exactly two stock positions");
    assert.ok(state.positions.every(r => r.offer_id === offer.offerId));
    assert.deepEqual(state.positions.map(r => r.location_id).sort(), [loc0.locationId, loc1.locationId].sort());
    assert.equal(Number(state.positions.find(r => r.stock_item_id === p0.body.stockItemId).on_hand), 7);
    assert.equal(Number(state.positions.find(r => r.stock_item_id === p1.body.stockItemId).on_hand), 4);
    assert.ok(state.positions.every(r => Number(r.reserved) === 0));
    return {
      status: "PASS",
      reason: "Two eligible locations each received a real stock position with on-hand/available 7 and 4 and reserved 0, matching the owner GET and independent SQL.",
    };
  }),

  "R10-03-N": () => runCase("R10-03-N", async ctx => {
    const fx = await prepare(ctx);
    const offer = await createOffer(ctx, fx);
    const before = await get(ctx, fx, offer.offerId);
    assert.equal(before.status, 200);
    const foreignLocation = randomUUID();
    const denied = await provision(ctx, fx, offer.offerId, foreignLocation);
    assert.ok(denied.status >= 400 && denied.status < 500, "Foreign/ineligible location must be denied, got " + denied.status + " " + JSON.stringify(denied.body));
    assert.ok(denied.body && denied.body.errorCode, "Denial must carry a typed errorCode");
    const after = await get(ctx, fx, offer.offerId);
    assert.deepEqual(after.body.inventoryLocations, before.body.inventoryLocations, "Denied provisioning must not change positions");
    const state = ctx.observe("denied");
    assert.equal(state.positions.length, 0, "No stock position may be created for an ineligible location");
    return {
      status: "PASS",
      reason: "A direct API provisioning attempt at a foreign/ineligible location (" + foreignLocation + ") was denied with typed " + denied.body.errorCode + " and no stock position was created for the offer.",
    };
  }),

  "R10-03-B": () => runCase("R10-03-B", async ctx => {
    const fx = await prepare(ctx);
    const offer = await createOffer(ctx, fx);
    const locations = await eligibleLocations(ctx, fx);
    assert.ok(locations.length >= 2, "Need two eligible active locations");
    const loc0 = locations[0];
    const loc1 = locations[1];
    const p0 = await provision(ctx, fx, offer.offerId, loc0.locationId);
    const p1 = await provision(ctx, fx, offer.offerId, loc1.locationId);
    assert.equal(p0.status, 200, JSON.stringify(p0.body));
    assert.equal(p1.status, 200, JSON.stringify(p1.body));
    let detail = await get(ctx, fx, offer.offerId);
    const row0 = detail.body.inventoryLocations.find(x => x.stockItemId === p0.body.stockItemId);
    const row1 = detail.body.inventoryLocations.find(x => x.stockItemId === p1.body.stockItemId);
    const first = await inventory(ctx, fx, offer.offerId, { onHand: 6, reason: "P1C first-position", stockItemId: row0.stockItemId, expectedVersion: row0.revision, operationId: randomUUID() });
    assert.equal(first.status, 200, JSON.stringify(first.body));
    // Deliberately stale revision on the second (known-failing remainder).
    const failed = await inventory(ctx, fx, offer.offerId, { onHand: 3, reason: "P1C failed-second", stockItemId: row1.stockItemId, expectedVersion: "2000-01-01T00:00:00.0000000+00:00", operationId: randomUUID() });
    assert.ok(failed.status >= 400 && failed.status < 500, "Stale second write must fail, got " + failed.status);
    assert.equal(failed.body.errorCode, "inventory.position.revision_conflict");
    detail = await get(ctx, fx, offer.offerId);
    let cur0 = detail.body.inventoryLocations.find(x => x.stockItemId === p0.body.stockItemId);
    let cur1 = detail.body.inventoryLocations.find(x => x.stockItemId === p1.body.stockItemId);
    assert.equal(Number(cur0.onHand), 6, "First successful write must be preserved");
    assert.equal(Number(cur1.onHand), 0, "Failed second write must not change its own position");
    assert.equal(detail.body.inventoryLocations.length, 2, "No duplicate position may be created");
    // Retry only the known-failed remainder with its current revision.
    const retry = await inventory(ctx, fx, offer.offerId, { onHand: 3, reason: "P1C retry-second", stockItemId: cur1.stockItemId, expectedVersion: cur1.revision, operationId: randomUUID() });
    assert.equal(retry.status, 200, JSON.stringify(retry.body));
    detail = await get(ctx, fx, offer.offerId);
    cur0 = detail.body.inventoryLocations.find(x => x.stockItemId === p0.body.stockItemId);
    cur1 = detail.body.inventoryLocations.find(x => x.stockItemId === p1.body.stockItemId);
    assert.equal(Number(cur0.onHand), 6);
    assert.equal(Number(cur1.onHand), 3);
    assert.equal(detail.body.inventoryLocations.length, 2);
    const state = ctx.observe("retry-complete");
    assert.equal(state.positions.length, 2, "Still exactly two positions after retry");
    const facts = state.inventoryFacts.filter(x => x.kind === "PhysicalChanged");
    assert.equal(facts.length, 2, "Exactly the first success and the retry fact; the failed attempt records nothing");
    assert.ok(facts.some(f => f.reason === "P1C first-position"));
    assert.ok(facts.some(f => f.reason === "P1C retry-second"));
    assert.ok(facts.every(f => f.reason !== "P1C failed-second"), "Failed attempt reason must not be recorded");
    return {
      status: "PASS",
      reason: "The first position write persisted; the second failed with a typed revision conflict leaving its own state unchanged and no duplicate position; retrying only the failed remainder persisted it, keeping both successes and their recorded reasons.",
    };
  }),

  "R10-04-P": () => runCase("R10-04-P", async ctx => {
    const fx = await prepare(ctx);
    const fields = { returnPolicyChoice: "Custom", customReturnWindowDays: 9, minimumOrderQuantity: 2, maximumOrderQuantity: 6 };
    const created = await createOffer(ctx, fx, fields);
    let detail = await get(ctx, fx, created.offerId);
    assert.equal(detail.status, 200);
    const assertFields = (d, sku, min, max) => {
      assert.equal(d.sellerPartyId, fx.seller);
      assert.equal(d.catalogVariantId, fx.variant);
      assert.equal(d.channel, fx.channel);
      assert.equal(d.status, "Draft");
      assert.equal(d.sellerSku, sku);
      assert.equal(d.returnPolicyChoice, "Custom");
      assert.equal(Number(d.customReturnWindowDays), 9);
      assert.equal(Number(d.minimumOrderQuantity), min);
      assert.equal(Number(d.maximumOrderQuantity), max);
    };
    assertFields(detail.body, created.sku, 2, 6);
    const editedSku = created.sku + "-EDIT";
    const edit = await patch(ctx, fx, created.offerId, {
      sellerSku: editedSku, status: "Draft", returnPolicyChoice: "Custom", customReturnWindowDays: 9,
      minimumOrderQuantity: 3, maximumOrderQuantity: 7, expectedVersion: detail.body.revisions.offer,
    });
    assert.equal(edit.status, 200, JSON.stringify(edit.body));
    detail = await get(ctx, fx, created.offerId);
    assertFields(detail.body, editedSku, 3, 7);
    const state = ctx.observe("edited");
    const row = state.offers.find(x => x.offer_id === created.offerId);
    assert.equal(row.status, "Draft");
    assert.equal(row.seller_sku, editedSku);
    assert.equal(row.return_policy_choice, "Custom");
    assert.equal(Number(row.custom_return_window_days), 9);
    assert.equal(Number(row.minimum_order_quantity), 3);
    assert.equal(Number(row.maximum_order_quantity), 7);
    assert.ok(state.history.length >= 2, "Create and commercial edit must be recorded");
    const ui = mountRegression(["committed create clears scoped recovery before navigation and never restores the old Draft"]);
    return {
      status: "PASS",
      reason: "Create then commercial edit reread all requested fields exactly from the owner GET and SQL (" + editedSku + ", Custom/9, 3/7); the real shared-workspace mounted regression confirmed a committed create clears scoped recovery before navigation and create-page re-entry never restores the old draft (log " + path.basename(ui.logPath) + ").",
    };
  }),

  "R10-04-N": () => runCase("R10-04-N", async ctx => {
    const fx = await prepare(ctx);
    const requestId = "P1C-" + randomUUID();
    const offerId = deterministicId(fx.seller, fx.variant, requestId);
    ctx.addOffer(offerId);
    assert.ok(EMPTY(ctx.observe("before")));
    const sku = SKU_PREFIX + randomUUID();
    const payload = { sellerPartyId: fx.seller, catalogVariantId: fx.variant, channel: fx.channel, sellerSku: sku, status: "Draft", requestId };
    // Simulated lost response: the first create commits but the client cannot trust the reply.
    const first = await ctx.api("/v1/admin/offers", "POST", payload, fx.admin);
    assert.equal(first.status, 201);
    assert.equal(first.body.offerId, offerId);
    ctx.trace.lostResponse = { requestId, committedOfferId: first.body.offerId };
    // Recover by re-sending the exact same intent; this must resolve the committed offer, not a new one.
    const recovered = await ctx.api("/v1/admin/offers", "POST", payload, fx.admin);
    assert.equal(recovered.status, 201, "lost-response recovery must return the committed offer, got " + recovered.status);
    assert.equal(recovered.body.offerId, offerId, "Recovery must resolve the same offer, not create a second");
    // Inject a changed value under the same request id: it must be rejected, never a false success.
    const changed = await ctx.api("/v1/admin/offers", "POST", Object.assign({}, payload, { sellerSku: sku + "-CHANGED" }), fx.admin);
    assert.ok(changed.status >= 400 && changed.status < 500, "Changed value under the same intent must be denied, got " + changed.status);
    assert.equal(changed.body.errorCode, "offer.create.intent_conflict");
    const detail = await get(ctx, fx, offerId);
    assert.equal(detail.status, 200);
    assert.equal(detail.body.sellerPartyId, fx.seller, "Committed response seller must match the requested seller");
    assert.equal(detail.body.sellerSku, sku, "Changed value must not overwrite the committed intent");
    const state = ctx.observe("recovered");
    assert.equal(state.offers.length, 1, "Lost-response retry and changed-value denial must leave exactly one offer");
    assert.equal(state.offers[0].seller_sku, sku);
    assert.equal(state.history.length, 1);
    const ui = mountRegression([
      "a network save is uncertain: draft kept, no retry button, reload action offered",
      "partial create keeps the created Draft recoverable through MessageBox",
    ]);
    return {
      status: "PASS",
      reason: "After a simulated lost create response, re-sending the exact intent deterministically recovered the same committed Offer; a changed value under the same request id was rejected with offer.create.intent_conflict and the durable state stayed intact; the committed response seller matched the requested seller; the shared-workspace mounted regression kept the uncertain-response draft/recovery paths honest (log " + path.basename(ui.logPath) + ").",
    };
  }),
};

module.exports = {
  ...historyCases,
  ...pricingCases,
  ...groupA,
};
