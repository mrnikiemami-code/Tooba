# CURRENT —262 review PARTIAL; ONLY262-R1 ISSUED —2026-11-11

Read docs/architecture/evidence/TB-SHIPPING-DAMAGED-ORIGIN-RECEIPT-RESOLUTION-AND-REPLACEMENT-262/ARCHITECT-REVIEW.md.
Task docs/ai/tasks/TB-SHIPPING-DAMAGED-ORIGIN-RECEIPT-RESOLUTION-AND-REPLACEMENT-262-R1/TASK.md.
Three SOURCE-CONFIRMED gaps at a67e514ead76a058788add1be571512791db6646:
1 RunDamagedResolutionAsync returns snapshot for recorded incomplete/no-stock/reserved decision; public retry doesn't continue and UI may clear envelope with no parcel.
2 old CancelShipment retry unconditionally detaches resolution link/increments attempt, can erase newer replacement allocation.
3 reserve starts with7day TTL then separate paid commit clears it; crash before commit leaves active-order stock expirable.
Preserve correct262 architecture/UI; ONLY targeted public resume/pending recovery, idempotent current-parcel cancel, atomic non-expiring Inventory hold. User retention rule: active ORDER holds never released by parcel cancel/return/timeout; dispatch consumes once. No broad tests/base parity/green loops/screenshots, no extra DB.
261 accepted within correction scope, user acceptance reopened.262 PARTIAL pending262-R1; not all shipping complete, no automatic next.
Same tooba_alpha5432/3000/5088 SingleStore; user/screenshot rows read-only; labelled production-path fixtures permitted. Preserve keys/CapturedPreview;255 parked/Tipax deferred. Parent-package return/financial automation remain unsupported, no policy redesign.
Older CURRENT blocks superseded.

---

# CURRENT USER OVERRIDE — ORDER RESERVATION RETAINED UNTIL ORDER CANCELLATION —2026-11-11

## USER OVERRIDE — RESERVATION RETENTION —2026-11-11
Latest explicit user rule: «تا سفارش لغو نشده نباید ازاد بشه رزرو».
This supersedes conflicting release-on-parcel-cancellation wording in the original262 issue.
While the ORDER is active, canceling a parcel/replacement attempt, non-delivery, return receipt, damage, retry failure, hold timeout or stopping the replacement workflow MUST NOT release its held stock back to saleable availability. Keep the purpose-bound reservation attached to this order for re-preparation/replacement; do not create a second reservation for the same retained allocation.
Cancelling a Created replacement parcel releases only its PARCEL ALLOCATION, NOT the Inventory stock reservation. A later legitimate attempt reuses/rebinds that same held order stock with exact occurrence history; canceled occurrence itself is not silently recreated.
Only an owning authorized ORDER cancellation (or an explicitly authorized order-line cancellation through the established order policy) can release the corresponding unconsumed order reservation, once. Do not interpret parcel cancellation as order cancellation. No new line-cancellation capability is authorized here.
Normal dispatch CONSUMES the appropriate reservation once; consume is not release. Never resurrect/release an already-consumed original reservation or pretend physically damaged stock remains healthy availability.
Replacement reserve-before-link failures retain/reconcile the held stock for the active order; no automatic compensation that returns it to saleable stock. Pre-reservation refusal/no-stock creates no hold. Duplicate attempts cannot leak extra holds; recover exact existing reservation id through owning evidence.
Scope the retention to order-bound fulfillment/replacement reservations; do not rewrite unrelated abandoned-cart policy as a shortcut. Any existing active-order expiry path that would defeat the rule must be identified and corrected in the owning policy or reported as a concrete blocker.
Focused proof: cancel replacement parcel while order active -> saleable availability does not increase, same held reservation remains; next attempt reuses it; retry/concurrency no extra hold; actual authorized order cancellation releases unconsumed hold exactly once; dispatch consumes new replacement hold once and never releases consumed original stock.

Active task262 remains the ONLY task; read updated TASK.md before continuing. This is a user steering correction, not a new task or approval pause. Preserve prior issued artifact as history; latest override takes precedence. Record it in262 evidence and proof. No user-data mutation, no extra database, no test loop.

---

# CURRENT —261 ACCEPT within correction scope;262 ISSUED —2026-11-11

Read261/ARCHITECT-REVIEW.md under docs/architecture/evidence/TB-SHIPPING-USER-ACCEPTANCE-NOTIFICATIONS-AND-RETURNED-RESEND-261/.
Source/evidence review4cf6bfbe084f7c4834205f6033064202bae2cf17 confirms contextual carrier vocabulary, existing Toast and own-parcel line status. Screenshot source1 Damaged/0 Usable: no healthy resend is CORRECT, reservation banner was not the cause.261 accepted within notification/status/healthy capacity explanation scope; user acceptance NOT claimed.
ONLY task docs/ai/tasks/TB-SHIPPING-DAMAGED-ORIGIN-RECEIPT-RESOLUTION-AND-REPLACEMENT-262/TASK.md.
262: durable source-scoped damage decision + non-saleable custody + explicit same-item replacement via NEW Inventory-owned healthy reservation and linked parcel; original sale reservation never consumed twice. Exact occurrence/recovery/concurrency/current authority. No-stock honest blocker/pending referral, no fake completed refund or damaged→usable. Customer obligation/lineage/queue statuses conserve units.
Correct handoff typo: usable2+damaged1 after healthy resend2 leaves usable remaining0, NOT2; replacement funds damaged1 only from separately reserved healthy stock.
Read SHIPPING-USER-ACCEPTANCE-CORRECTIONS-261-262.md. Earlier5/5 is historical source/evidence milestone; user trial reopened completion.261 accepted;262 active pending result/review. One worker/task, STOP_FOR_ARCHITECT; no automatic next/all-complete claim.
Toast rule persists; no persistent transient-success banners. Durable blockers/field validation/reconcile context retained; no duplicate same-outcome messaging.
Same tooba_alpha5432, FE3000/Host5088 SingleStore/store-alpha ONLY; no extra DB/Docker/15433/edition change. User/screenshot rows read-only; minimal labelled production-path fixtures and necessary backed-up additive owning migrations authorized. Preserve keys/CapturedPreview;255 parked/Tipax deferred; common-package parent exception still unsupported/locked. No broad tests/base parity/green loops/screenshots.
Older CURRENT blocks superseded.

---

# User acceptance correction plan —261–262 —2026-11-11

The previous five-defect milestone was accepted within code/evidence scope, NOT user acceptance. User trial rejected: persistent success banners instead of common Toast, English Returning/etc, returned-to-origin mislabeled customer return, stale group/line/target status, no visible healthy resend, and damaged goods dead end. Do not continue claiming 'all shipping complete'.
Latest user instruction: transient action notifications use ONE existing Toast pattern across touched admin surfaces; no persistent success banners. Durable state/blockers/reconciliation obligations remain contextual, not hidden in disappearing Toast. Field validation stays beside its field; no duplicate toast+inline message for one event. This supersedes earlier no-toast-for-create task conventions where they conflict.
Source confirmed admin-api.ts generic Returned=>'مرجوعی'; this is not correct for carrier return. Source projection has resend_shipment for Returned + own receipt + positive own ResendableQuantity + actor permission. Actual missing-button cause on screenshot is NOT yet proven; expired fixture reservation/payment banner must not be guessed as the cause.
Sequence, one worker/result/review at a time:
1 ISSUED261 — **WORKER RESULT PASS (pending Architect review)**: unified transient notifications on touched authoring/shipping surfaces; context-specific Persian states and coherent summaries; the screenshot fixture's missing resend **proven** to be a correct refusal (receipt `1 Damaged` → usable 0 → resendable 0), with the real defects (stale line state + silence) repaired; positive healthy capacity still offers exactly one deliberate reviewed resend with immediate lineage. Evidence: `docs/architecture/evidence/TB-SHIPPING-USER-ACCEPTANCE-NOTIFICATIONS-AND-RETURNED-RESEND-261/` (RESULT/RECEIPTS/result-contract/tb261-live-fixture/damage-resolution-262-handoff).
2 WORKER RESULT PASS 262 (pending Architect review; retention revision): damaged receipt resolution is now an owned Fulfillment decision (aggregate `DamagedGoodsResolution`) with two explicit outcomes — replace the SAME ordered item from a NEW purpose-bound healthy-stock reservation (`IDamagedReplacementInventoryPort`, offer resolved from the ORIGINAL sale reservation), or an honest pending referral; damaged units never become usable/resend capacity and never raise saleable stock. Following the user override «تا سفارش لغو نشده نباید آزاد بشه رزرو», the replacement hold is bound to the ACTIVE order: it is committed for the paid order (no TTL, so the expired-hold reclaimer cannot free it), cancelling a Created replacement parcel detaches only that parcel and the retry rebinds the SAME reservation, and only an authorized ORDER cancellation releases the unconsumed hold once. Evidence: `docs/architecture/evidence/TB-SHIPPING-DAMAGED-ORIGIN-RECEIPT-RESOLUTION-AND-REPLACEMENT-262/` (TASK/RESULT/RECEIPTS/result-contract/ownership-design/tb262-domain/tb262-relational/tb262-migration-plan).
Do not implement a task speculatively ahead of its issue. 262 was issued from the recorded seams and its worker result is above; the next step is Architect review, not another automatic implementation. No extra DB/edition/runtime, no broad test loops, no real user business writes. tooba_alpha/3000/5088 only.255 parked/Tipax deferred. Parent-package exception remains disclosed unsupported/locked unless separately authorized. Final acceptance pending actual user test.
