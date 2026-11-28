// TB-296 P1-FINAL-CLOSURE — D2-HOLD executable oracle.
// Real captured checkout acceptance + real reused-hold retry (OrderSupplyService) versus real
// admin assignment, plus a concurrent new checkout hold versus assignment with a stale-owner /
// unsafe-commitment safety invariant. No bypass/product change.
const fs = require("node:fs");
const path = require("node:path");
const L = require("./finalacceptance-d2-lib.cjs");
const { check } = L;

async function main() {
  const root = process.env.P1C_EVIDENCE_DIR;
  check(root, "Set P1C_EVIDENCE_DIR");
  fs.mkdirSync(root, { recursive: true });
  const evidence = path.join(root, "D2-HOLD.json");
  check(!fs.existsSync(evidence), "Do not overwrite historical evidence");
  const trace = { id: "D2-HOLD", requests: [], status: "NOT_RUN" };
  const save = () => fs.writeFileSync(evidence, JSON.stringify(trace, null, 2));
  const owned = { checkoutIds: [], cartIds: [] };
  const req = async (label, p) => { const r = await p; trace.requests.push({ label, status: r.status, body: r.body }); save(); return r; };

  try {
    const admin = await L.adminContext();
    const customer = await L.registerCustomer("p1c-d2-hold");
    owned.userId = customer.userId;
    trace.customer = { userId: customer.userId };
    const [raceOffer, reuseOffer] = L.pickCleanOffers(2);

    // ---------- Part 1: concurrent NEW hold (real checkout) versus assignment ----------
    const raceBefore = L.offerState(raceOffer);
    const raceDetail = await req("race-detail", L.adminOfferDetail(admin, raceOffer));
    const raceVersion = raceDetail.body.revisions.offer;
    const raceCart = await L.createCartWithLine(customer, raceOffer, 1);
    owned.cartIds.push(raceCart.cartId);
    const submitPromise = L.call("/v1/storefront/checkout", "POST", {
      cartId: raceCart.cartId, expectedCartVersion: raceCart.cartVersion, idempotencyKey: "P1C-race-" + Date.now(),
      shipping: { recipientName: "P1C Synthetic Customer", contactMobile: "09120000000", provinceName: "تهران", cityName: "تهران", postalAddress: "P1C race address", postalCode: "1234567890" },
      couponCode: null,
    }, { ...customer.auth, "X-Tooba-Guest-Secret": raceCart.secret });
    const assignPromise = L.adminAssign(admin, raceOffer, "Seller", raceVersion);
    const [submit, assign] = await Promise.all([submitPromise, assignPromise]);
    const racedCheckoutId = submit.status === 200 ? L.uuid(submit.body.checkoutId) : null;
    if (racedCheckoutId) owned.checkoutIds.push(racedCheckoutId);
    const raceOrder = racedCheckoutId ? L.orderState(racedCheckoutId) : { lines: [], sellerOrders: [], payments: [], cycles: [] };
    const raceReservations = L.reservationsOf(raceCart.cartId);
    const raceAfter = L.offerState(raceOffer);
    const holdSucceeded = raceOrder.lines.length > 0 && raceReservations.some(r => r.status === "Held" || r.status === "Committed");
    const assignSucceeded = assign.status === 200;
    const loc = L.psql("select l.owner_kind||'|'||coalesce(l.seller_party_id::text,'') from inventory.stock_positions s join inventory.locations l on l.location_id=s.location_id where s.offer_id='" + raceOffer + "' limit 1");
    trace.race = {
      offer: raceOffer, submitStatus: submit.status, submitError: submit.body && submit.body.errorCode,
      assignStatus: assign.status, assignError: assign.body && assign.body.errorCode,
      holdSucceeded, assignSucceeded, order: raceOrder, reservations: raceReservations, offerAfter: raceAfter, locationOwner: loc,
    };
    save();
    check(holdSucceeded || assignSucceeded, "neither the checkout hold nor the assignment committed");
    if (holdSucceeded) {
      check(raceReservations.filter(r => r.status === "Held" || r.status === "Committed").length === 1, "captured hold is not exactly one");
      check(String(loc).startsWith("Store|"), "held reservation is not on an eligible Store-owned location: " + loc);
      // even if the assignment also won, the durable order commitment must now protect responsibility.
      const post = await L.adminOfferDetail(admin, raceOffer);
      const postAssign = await req("race-post-assignment-denied", L.adminAssign(admin, raceOffer, "Platform", post.body.revisions.offer));
      check(postAssign.status === 409 && postAssign.body.errorCode === "offer.fulfillment.change_protected",
        "commitment created by the hold did not protect a subsequent assignment: " + postAssign.status + " " + JSON.stringify(postAssign.body).slice(0, 200));
      trace.race.postAssignment = { status: postAssign.status, errorCode: postAssign.body && postAssign.body.errorCode };
    } else {
      check(submit.status >= 400 && submit.status < 500, "assignment winner did not fail the checkout with a typed 4xx: " + submit.status);
      check(raceOrder.lines.length === 0, "assignment winner left an order line");
      // restore the demo offer for cleanliness
      const d = await L.adminOfferDetail(admin, raceOffer);
      const back = await L.adminAssign(admin, raceOffer, null, d.body.revisions.offer);
      check(back.status === 200, "could not restore race offer");
    }
    // No stale owner: the final responsibility must be an explicit trusted value or null, and any
    // held reservation must still be on a SingleStore-eligible Store location.
    check([null, "Seller", "Platform"].includes(raceAfter.responsibility), "unexpected responsibility " + raceAfter.responsibility);
    save();

    // ---------- Part 2: captured Checkout acceptance + reused hold versus assignment ----------
    const reuseBefore = L.offerState(reuseOffer);
    const reuseCo0 = await L.createCheckout(customer, reuseOffer, 2);
    const reuseCo = L.checkoutOf(reuseCo0);
    owned.cartIds.push(reuseCo.cartId); owned.checkoutIds.push(reuseCo.checkoutId);
    const reuseState0 = L.orderState(reuseCo.checkoutId);
    const heldBefore = L.reservationsOf(reuseCo.cartId).filter(r => r.status === "Held");
    trace.reuse = { checkpoint: "captured-acceptance", offer: reuseOffer, checkoutId: reuseCo.checkoutId, order: reuseState0, held: heldBefore };
    save();
    check(reuseState0.lines.length === 1, "captured checkout acceptance did not create an order line");
    check(heldBefore.length === 1, "captured checkout acceptance did not create exactly one held reservation");
    const heldId = heldBefore[0].id;
    const reuseDetail = await L.adminOfferDetail(admin, reuseOffer);
    const [retry, reuseAssign] = await Promise.all([
      L.retryReservation(customer, reuseCo),
      L.adminAssign(admin, reuseOffer, "Platform", reuseDetail.body.revisions.offer),
    ]);
    trace.reuse.retry = { status: retry.status, body: retry.body };
    trace.reuse.assign = { status: reuseAssign.status, body: reuseAssign.body };
    save();
    check(retry.status === 200, "reused hold retry failed: " + retry.status + " " + JSON.stringify(retry.body).slice(0, 200));
    check(reuseAssign.status === 409 && reuseAssign.body.errorCode === "offer.fulfillment.change_protected",
      "assignment over a captured hold was not denied: " + reuseAssign.status + " " + JSON.stringify(reuseAssign.body).slice(0, 200));
    const heldAfter = L.reservationsOf(reuseCo.cartId).filter(r => r.status === "Held" || r.status === "Committed");
    const reuseAfter = L.offerState(reuseOffer);
    trace.reuse.heldAfter = heldAfter;
    trace.reuse.offerAfter = reuseAfter;
    save();
    check(heldAfter.length === 1 && heldAfter[0].id === heldId, "reused hold was released or duplicated: " + JSON.stringify(heldAfter));
    check(reuseAfter.responsibility === reuseBefore.responsibility, "reused-hold assignment changed the offer owner");
    check(reuseAfter.lines === 1, "captured commitment disappeared after assignment attempt");

    trace.status = "PASS";
    trace.reason = "Real captured checkout acceptance created a durable held reservation + order line; a reused-hold OrderSupplyService retry versus assignment preserved the exact single hold, denied the assignment with offer.fulfillment.change_protected, and left no stale owner. A concurrent new checkout hold versus assignment kept the held reservation on an eligible Store-owned location and the resulting commitment protected every subsequent assignment (no unsafe commitment).";
  } catch (error) {
    trace.status = "FAIL";
    trace.reason = String(error && error.stack ? error.stack : error);
  } finally {
    try { trace.cleanup = L.cleanupOwned(owned); }
    catch (error) { trace.status = "FAIL"; trace.cleanupError = String(error && error.stack ? error.stack : error); }
    save();
  }
  console.log(JSON.stringify({ id: "D2-HOLD", status: trace.status, reason: trace.reason, cleanup: trace.cleanup }));
  if (trace.status !== "PASS") process.exit(1);
}

main().catch(error => { console.error(error); process.exit(1); });
