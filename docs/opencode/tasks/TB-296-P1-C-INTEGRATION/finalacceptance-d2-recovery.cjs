// TB-296 P1-FINAL-CLOSURE — D2-RECOVERY executable oracle.
// Real unpaid checkout -> real OrderSupplyService reacquire via the storefront retry-reservation
// path; exercise the MaxReservationCycles cap; prove paid obligations are preserved with no
// second commercial debit. No bypass/product change.
const fs = require("node:fs");
const path = require("node:path");
const L = require("./finalacceptance-d2-lib.cjs");
const { check } = L;

function cycles(checkoutId) {
  return L.psqlJson("SELECT coalesce(jsonb_agg(jsonb_build_object('n',cycle_number,'status',status,'expires',expires_at,'reservations',reservation_ids) ORDER BY cycle_number),'[]') FROM \"order\".reservation_cycles WHERE checkout_id='" + checkoutId + "'");
}
function forceExpire(checkoutId, cartId) {
  L.psql("UPDATE \"order\".reservation_cycles SET expires_at = now() - interval '1 minute' WHERE checkout_id='" + checkoutId + "' AND status='Active';");
  L.psql("UPDATE inventory.reservations SET expires_at = now() - interval '1 minute' WHERE status='Held' AND reservation_id IN (" +
    "SELECT reservation_id FROM \"order\".order_lines WHERE reservation_id IS NOT NULL AND seller_order_id IN (" +
    "SELECT seller_order_id FROM \"order\".seller_orders WHERE checkout_id='" + checkoutId + "'));");
}
async function waitReleased(reservationId, timeoutMs = 30000) {
  const start = Date.now(); let row = null;
  while (Date.now() - start < timeoutMs) {
    row = L.reservationRow(reservationId);
    if (!row || row.status !== "Held") return row;
    await new Promise(r => setTimeout(r, 1000));
  }
  return row;
}
async function waitPaid(checkoutId, timeoutMs = 15000) {
  const start = Date.now(); let last = null;
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
  const evidence = path.join(root, "D2-RECOVERY.json");
  check(!fs.existsSync(evidence), "Do not overwrite historical evidence");
  const trace = { id: "D2-RECOVERY", requests: [], status: "NOT_RUN" };
  const save = () => fs.writeFileSync(evidence, JSON.stringify(trace, null, 2));
  const owned = { checkoutIds: [], cartIds: [] };
  const req = async (label, p) => { const r = await p; trace.requests.push({ label, status: r.status, body: r.body }); save(); return r; };

  try {
    const admin = await L.adminContext();
    const customer = await L.registerCustomer("p1c-d2-recovery");
    owned.userId = customer.userId;
    const [retryOffer, paidOffer] = L.pickCleanOffers(2);

    // ---------- Unpaid retry via the real OrderSupplyService ----------
    const stock0 = L.stockState(retryOffer);
    const co0 = await L.createCheckout(customer, retryOffer, 2);
    const co = L.checkoutOf(co0);
    owned.cartIds.push(co.cartId); owned.checkoutIds.push(co.checkoutId);
    const initial = L.orderState(co.checkoutId);
    const initialCycles = cycles(co.checkoutId);
    const initialHeld = L.reservationsOf(co.cartId).filter(r => r.status === "Held");
    trace.unpaid = { checkoutId: co.checkoutId, order: initial, cycles: initialCycles, held: initialHeld };
    save();
    check(initial.sellerOrders.length === 1 && initial.sellerOrders[0].status === "PendingPayment", "unpaid checkout is not PendingPayment");
    check(initialCycles.length === 1 && initialCycles[0].n === 1 && initialCycles[0].status === "Active", "cycle 1 not active: " + JSON.stringify(initialCycles));
    check(initialHeld.length === 1 && Number(initialHeld[0].qty) === 2, "initial held reservation qty wrong: " + JSON.stringify(initialHeld));
    const oldReservation = initialHeld[0].id;
    let currentReservation = initial.lines.length === 1 ? initial.lines[0].reservation : oldReservation;

    // Drive the real OrderSupplyService reacquire repeatedly until the policy cap denies it.
    const reacquires = [];
    let cap = null;
    let lastCycle = 1;
    for (let attempt = 2; attempt <= 6; attempt += 1) {
      forceExpire(co.checkoutId, co.cartId);
      // Let the real CartExpiry/Inventory reclaimer release the expired hold before retry.
      const released = await waitReleased(currentReservation, 30000);
      const r = await req("retry-cycle" + attempt, L.retryReservation(customer, co));
      if (r.status === 200) {
        check(Number(r.body.cycleNumber) === attempt, "reacquire cycle number " + r.body.cycleNumber + " != " + attempt);
        const st = L.orderState(co.checkoutId);
        const cs = cycles(co.checkoutId);
        const held = st.lines.length === 1 && st.lines[0].reservation ? [L.reservationRow(st.lines[0].reservation)].filter(x => x && x.status === "Held") : [];
        const stock = L.stockState(retryOffer);
        reacquires.push({ attempt, expiredReservation: released, retry: r.body, cycles: cs, held, stock });
        save();
        check(released && released.status !== "Held", "expired hold was not released by the real reclaimer before cycle " + attempt + ": " + JSON.stringify(released));
        check(cs.length === attempt && cs[attempt - 1].n === attempt && cs[attempt - 1].status === "Active", "cycle " + attempt + " not active: " + JSON.stringify(cs));
        check(held.length === 1 && Number(held[0].qty) === 2, "cycle " + attempt + " did not hold exactly one 2-unit reservation: " + JSON.stringify(held));
        check(st.lines.length === 1 && st.lines[0].reservation === held[0].id, "order line not bound to the recovered reservation at cycle " + attempt);
        check(Number(stock.reserved) === Number(stock0.reserved) + 2, "stock reserved delta is not exactly 2 at cycle " + attempt + " (double debit?): " + JSON.stringify({ before: stock0, after: stock }));
        currentReservation = held[0].id;
        lastCycle = attempt;
      } else {
        cap = { attempt, expiredReservation: released, status: r.status, errorCode: r.body && r.body.errorCode, cycles: cycles(co.checkoutId) };
        save();
        break;
      }
    }
    trace.reacquires = reacquires;
    trace.cap = cap;
    save();
    check(reacquires.length >= 1, "no real OrderSupplyService reacquire occurred");
    check(cap && cap.status === 409 && cap.errorCode === "inventory.reservation.retry_limit_reached",
      "unpaid retry cap was not enforced with the stable code: " + JSON.stringify(cap && { status: cap.status, errorCode: cap.errorCode }));
    check(cap.cycles.length === lastCycle, "retry cap created an extra cycle: " + JSON.stringify(cap.cycles));


    // ---------- Paid obligations preserved, no second commercial debit ----------
    const paidCo0 = await L.createCheckout(customer, paidOffer, 1);
    const paidCo = L.checkoutOf(paidCo0);
    owned.cartIds.push(paidCo.cartId); owned.checkoutIds.push(paidCo.checkoutId);
    await req("paid-payment", L.payCheckout(customer, paidCo));
    const paidState = await waitPaid(paidCo.checkoutId);
    check(paidState.sellerOrders.length === 1 && paidState.sellerOrders[0].status === "Paid", "paid fixture not Paid");
    const paidStockBefore = L.stockState(paidOffer);
    const paidPaymentsBefore = paidState.payments.length;
    const paidLinesBefore = paidState.lines.length;
    const paidReservationsBefore = L.reservationsOf(paidCo.cartId);
    trace.paid = { checkoutId: paidCo.checkoutId, order: paidState, stock: paidStockBefore, reservations: paidReservationsBefore };
    save();
    const paidRetry = await req("paid-retry-denied", L.retryReservation(customer, paidCo));
    check(paidRetry.status === 409 && (paidRetry.body.errorCode === "pending.retry.not_allowed" || paidRetry.body.errorCode === "storefront.pending_retry_not_allowed"),
      "paid order retry was not denied: " + paidRetry.status + " " + JSON.stringify(paidRetry.body).slice(0, 300));
    const paidStateAfter = L.orderState(paidCo.checkoutId);
    const paidStockAfter = L.stockState(paidOffer);
    trace.paid.after = { order: paidStateAfter, stock: paidStockAfter, retry: { status: paidRetry.status, errorCode: paidRetry.body.errorCode } };
    save();
    check(paidStateAfter.payments.length === paidPaymentsBefore, "paid order gained another payment attempt");
    check(paidStateAfter.payments.filter(p => p.status === "Succeeded").length === 1, "paid order does not have exactly one succeeded payment");
    check(paidStateAfter.lines.length === paidLinesBefore && Number(paidStateAfter.lines[0].qty) === 1, "paid order line changed");
    check(JSON.stringify(paidStockAfter) === JSON.stringify(paidStockBefore), "paid order stock changed (second commercial debit): " + JSON.stringify({ before: paidStockBefore, after: paidStockAfter }));

    trace.status = "PASS";
    trace.reason = "Real unpaid checkout drove the actual OrderSupplyService through the storefront retry path: cycle 1 expired and was released by the real expiry reclaimer -> real reacquire to cycle " + reacquires[0].attempt + " (new Held reservation on the same order line, old released, reserved delta exactly 2) -> the next retry was denied with inventory.reservation.retry_limit_reached at the resolved policy cap of " + cap.cycles.length + " cycles (no extra cycle). A real paid order denied retry with pending.retry.not_allowed and kept exactly one succeeded payment, unchanged line/stock (no second commercial debit).";
  } catch (error) {
    trace.status = "FAIL";
    trace.reason = String(error && error.stack ? error.stack : error);
  } finally {
    try { trace.cleanup = L.cleanupOwned(owned); }
    catch (error) { trace.status = "FAIL"; trace.cleanupError = String(error && error.stack ? error.stack : error); }
    save();
  }
  console.log(JSON.stringify({ id: "D2-RECOVERY", status: trace.status, reason: trace.reason, cleanup: trace.cleanup }));
  if (trace.status !== "PASS") process.exit(1);
}

main().catch(error => { console.error(error); process.exit(1); });
