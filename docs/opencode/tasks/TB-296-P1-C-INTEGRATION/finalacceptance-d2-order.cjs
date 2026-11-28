// TB-296 P1-FINAL-CLOSURE — D2-ORDER executable oracle.
// REAL authenticated storefront checkout + synthetic customer + real fake-gateway payment,
// then real admin commitment-protection and cancel/restore transitions. No bypass/product change.
const fs = require("node:fs");
const path = require("node:path");
const L = require("./finalacceptance-d2-lib.cjs");
const { check, uuid, psqlJson } = L;

const PAID_OFFER = process.env.P1C_D2O_PAID || "004cb738-0f70-4937-bdd5-d4daca74bcb6";
const CANCELLED_OFFER = process.env.P1C_D2O_CANCELLED || "005e50c2-c260-4cb3-994c-8179e2d5213b";
const FREE_OFFER = process.env.P1C_D2O_FREE || "0066f3d5-1d00-4808-9cfa-f92059ab7894";

async function waitForPaid(checkoutId, timeoutMs = 15000) {
  const start = Date.now();
  let last = null;
  while (Date.now() - start < timeoutMs) {
    last = L.orderState(checkoutId);
    if (last.sellerOrders.length && last.sellerOrders.every(x => x.status === "Paid")) return last;
    await new Promise(r => setTimeout(r, 400));
  }
  return last;
}

async function main() {
  const root = process.env.P1C_EVIDENCE_DIR;
  check(root, "Set P1C_EVIDENCE_DIR");
  fs.mkdirSync(root, { recursive: true });
  const evidence = path.join(root, "D2-ORDER.json");
  check(!fs.existsSync(evidence), "Do not overwrite historical evidence");
  const trace = { id: "D2-ORDER", requests: [], status: "NOT_RUN" };
  const save = () => fs.writeFileSync(evidence, JSON.stringify(trace, null, 2));
  const owned = { checkoutIds: [], cartIds: [] };
  let customer = null;
  const req = async (label, p) => { const r = await p; trace.requests.push({ label, status: r.status, body: r.body }); save(); return r; };

  try {
    const admin = await L.adminContext();
    customer = await L.registerCustomer("p1c-d2-order");
    owned.userId = customer.userId;
    trace.customer = { userId: customer.userId, email: customer.email };

    // ---------- PAID order commitment protection ----------
    const paidDetail0 = await req("paid-offer-before", L.adminOfferDetail(admin, PAID_OFFER));
    const paidRev0 = paidDetail0.body.revisions.offer;
    const paidBefore = L.offerState(PAID_OFFER);
    check(paidBefore.lines === 0, "paid offer already had an order line before the test");
    const paidCo0 = await L.createCheckout(customer, PAID_OFFER, 1);
    const paidCo = L.checkoutOf(paidCo0);
    owned.cartIds.push(paidCo.cartId); owned.checkoutIds.push(paidCo.checkoutId);
    trace.paidCheckout = { checkoutId: paidCo.checkoutId, cartId: paidCo.cartId, submitStatus: paidCo.submit.status };
    save();
    const pay = await req("paid-payment", L.payCheckout(customer, paidCo));
    const paidState = await waitForPaid(paidCo.checkoutId);
    trace.paidOrderState = paidState;
    save();
    check(paidState.sellerOrders.length === 1 && paidState.sellerOrders[0].status === "Paid", "order not Paid: " + JSON.stringify(paidState.sellerOrders));
    check(paidState.lines.length === 1 && paidState.lines[0].offer === PAID_OFFER, "paid order line missing");
    check(paidState.payments.some(p => p.status === "Succeeded" && p.provider === "fake"), "no succeeded real payment");

    const paidAfter = L.offerState(PAID_OFFER);
    const paidDenied = await req("paid-assignment-denied", L.adminAssign(admin, PAID_OFFER, "Seller", paidAfter.updatedAt));
    check(paidDenied.status === 409 && paidDenied.body.errorCode === "offer.fulfillment.change_protected",
      "paid order commitment was not protected: " + paidDenied.status + " " + JSON.stringify(paidDenied.body));
    const paidAfterDenied = L.offerState(PAID_OFFER);
    check(paidAfterDenied.responsibility === paidBefore.responsibility && paidAfterDenied.history === paidBefore.history, "paid denial mutated the offer");

    // ---------- RESTORABLE-CANCELLED order protection ----------
    const cancelledDetail0 = await req("cancelled-offer-before", L.adminOfferDetail(admin, CANCELLED_OFFER));
    const cancelledBefore = L.offerState(CANCELLED_OFFER);
    check(cancelledBefore.lines === 0, "cancelled offer already had an order line");
    const cancelledCo0 = await L.createCheckout(customer, CANCELLED_OFFER, 1);
    const cancelledCo = L.checkoutOf(cancelledCo0);
    owned.cartIds.push(cancelledCo.cartId); owned.checkoutIds.push(cancelledCo.checkoutId);
    trace.cancelledCheckout = { checkoutId: cancelledCo.checkoutId, cartId: cancelledCo.cartId };
    save();
    await req("cancelled-payment", L.payCheckout(customer, cancelledCo));
    const cancelledPaid = await waitForPaid(cancelledCo.checkoutId);
    check(cancelledPaid.sellerOrders.length === 1 && cancelledPaid.sellerOrders[0].status === "Paid", "cancelled fixture not Paid first");
    const sellerOrderId = cancelledPaid.sellerOrders[0].id;
    const cancel = await req("admin-cancel", L.adminCancel(admin, cancelledCo.checkoutId, sellerOrderId));
    check(cancel.status === 200, "admin cancel failed " + cancel.status + " " + JSON.stringify(cancel.body).slice(0, 300));
    let cancelledState = L.orderState(cancelledCo.checkoutId);
    if (cancelledState.sellerOrders[0].status !== "Cancelled") {
      await new Promise(r => setTimeout(r, 1200));
      cancelledState = L.orderState(cancelledCo.checkoutId);
    }
    trace.cancelledOrderState = cancelledState;
    save();
    check(cancelledState.sellerOrders[0].status === "Cancelled", "order did not become Cancelled: " + JSON.stringify(cancelledState.sellerOrders));
    check(cancelledState.sellerOrders[0].cancelledFrom === "Paid", "cancelled order is not restorable-from-paid: " + JSON.stringify(cancelledState.sellerOrders));
    const cancelledAfter = L.offerState(CANCELLED_OFFER);
    const cancelledDenied = await req("cancelled-assignment-denied", L.adminAssign(admin, CANCELLED_OFFER, "Seller", cancelledAfter.updatedAt));
    check(cancelledDenied.status === 409 && cancelledDenied.body.errorCode === "offer.fulfillment.change_protected",
      "restorable-cancelled commitment was not protected: " + cancelledDenied.status + " " + JSON.stringify(cancelledDenied.body));
    const cancelledAfterDenied = L.offerState(CANCELLED_OFFER);
    check(cancelledAfterDenied.responsibility === cancelledBefore.responsibility && cancelledAfterDenied.history === cancelledBefore.history, "cancelled denial mutated the offer");

    // ---------- SAFE NO-COMMIT positive assignment sequence ----------
    const freeBefore = L.offerState(FREE_OFFER);
    check(freeBefore.lines === 0, "free offer unexpectedly has order lines");
    const transitions = ["Seller", "Platform", null, "Seller"];
    const positive = [];
    let detail = await req("free-detail", L.adminOfferDetail(admin, FREE_OFFER));
    let version = detail.body.revisions.offer;
    for (const responsibility of transitions) {
      const r = await req("free-assign-" + responsibility, L.adminAssign(admin, FREE_OFFER, responsibility, version));
      positive.push({ responsibility, status: r.status, errorCode: r.body && r.body.errorCode });
      check(r.status === 200, "no-commit assignment " + responsibility + " failed: " + r.status + " " + JSON.stringify(r.body).slice(0, 200));
      detail = await L.adminOfferDetail(admin, FREE_OFFER);
      version = detail.body.revisions.offer;
      check((detail.body.fulfillmentResponsibility ?? null) === responsibility, "assignment not persisted: " + responsibility);
    }
    const freeAfter = L.offerState(FREE_OFFER);
    check(freeAfter.lines === 0, "no-commit sequence created an order line");
    check(freeAfter.responsibility === "Seller", "final free responsibility not persisted");
    check(freeAfter.history > freeBefore.history, "assignment history missing");
    // restore the demo offer to its original (unassigned) state
    const restore = await req("free-restore-null", L.adminAssign(admin, FREE_OFFER, null, version));
    check(restore.status === 200, "restore free offer failed " + restore.status);
    detail = await L.adminOfferDetail(admin, FREE_OFFER);
    check((detail.body.fulfillmentResponsibility ?? null) === null, "free offer not restored to unassigned");
    trace.positiveAssignments = positive;
    trace.freeOffer = { before: freeBefore, after: L.offerState(FREE_OFFER) };
    save();

    trace.status = "PASS";
    trace.reason = "Real authenticated checkout + fake-gateway payment created a Paid order and a Paid-then-cancelled (restorable) order for two previously uncommitted offers; both real admin fulfillment reassignments were rejected with 409 offer.fulfillment.change_protected and wrote nothing. A third uncommitted offer completed initial assignment/reassignment/clear (Seller->Platform->null->Seller) with persisted state and history, then was restored to unassigned.";
  } catch (error) {
    trace.status = "FAIL";
    trace.reason = String(error && error.stack ? error.stack : error);
  } finally {
    try { trace.cleanup = L.cleanupOwned(owned); }
    catch (error) { trace.status = "FAIL"; trace.cleanupError = String(error && error.stack ? error.stack : error); }
    save();
  }
  console.log(JSON.stringify({ id: "D2-ORDER", status: trace.status, reason: trace.reason, cleanup: trace.cleanup }));
  if (trace.status !== "PASS") process.exit(1);
}

main().catch(error => { console.error(error); process.exit(1); });
