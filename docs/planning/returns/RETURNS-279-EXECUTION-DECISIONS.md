# Final consolidated Returns delivery279 — execution decisions — 2026-11-17
Read alongside RETURNS-MARKETPLACE-COMPLETION-PLAN-278-279.md. Only279 next;278 core improvements retained but connected durability still PARTIAL. No per-gap micro-task chain or delegation.

## One final result, four cohesive work areas
A Close278 customer durability/lifecycle/same-line-reason/shared-Dialog gaps identified in278/ARCHITECT-REVIEW.md.
B Audited pre-financial decision correction + same-case version-fenced first-refund start.
C Reviewed per-item/portion resolution on legacy multi-item customer cases + truthful rejected-goods custody.
D Per-delivery allocation/window correctness + shared checkout Returns refund budget.
One bounded additive Returns schema increment is authorized in the eventual envelope, after verified fresh private backup. No other module schema/migration or manual business SQL.

## Decided minimal domain shape
- Existing case/receipt/decision/attempt history is immutable. Add Returns-owned append-only decision revisions/resolution facts; read effective current decision through ONE authority with explicit monotonic version, actor, reason and occurrence. Legacy decision rows remain baseline, no backfill.
- ReopenForReview only before ANY financial attempt (including Pending/Failed/unknown), irreversible disposition/stock/courier-closure/custody handover. Reopen is a NEW audited fact; never delete the old decision or reverse a recorded refund.
- Reject->reopen re-acquires original claimed quantities under seller-order serialization. If another claim occupies released capacity, refuse honestly. Original timely claim may only reuse its positively bound original delivery allocation; no fresh/extended window for new units.
- First financial attempt must validate the effective reviewed decision/version and reserve its amount under SAME short owning serialization as correction; no stale-start-after-reopen race. All external gateway/Wallet/Inventory/Fulfillment calls outside transaction/locks.
- For legacy multi-item ordinary cases, prefer a reviewed per-item/condition resolution plan over cloning cases/receipts: accepted/rejected/pending quantities, reasons and exact received-portions binding. Original requested quantity/amount remains visible; immutable approved refund amount = accepted quantities times accepted stored unit money facts under existing supported policy. Do not silently refund the whole original requested case.
- Accepted quantities must have coherent received evidence; an explicitly rejected/unreceived remainder has no fabricated receipt. Case money can start only after its reviewed resolution is complete and accepted coverage is proven. Money amount/plan freezes before first attempt.
- Record physical settlement by portions where necessary: accepted usable alone may restock once under a stable identity/quantity; accepted damaged never becomes saleable; rejected received portions stay active custody work. No single item label may announce that all requested goods were restocked.
- Add a deliberate audited confirmation of ACTUAL handback to the owning customer for rejected received goods (in-person handover or verified delivery evidence). Tracking number alone is not delivery. This records a real Returns custody fact, not a carrier booking/notification or fake Fulfillment shipment status. Partial handback remains partial; no money/Inventory effect. No live handback performed by worker.
- Rejected/cancelled with physically unresolved received quantities must not release those same quantities for another simultaneous return. One capacity/physical-obligation authority; no false history/archive.
- Group positive delivered slices by stable owning identity; if existing LineDeliverySlice lacks shipment identity, an additive Contracts member/owning read may expose it. Allocate new case quantities to eligible slices under seller-order serialization, persist Returns-owned allocation. Window expiry subtracts claims only from their allocated slices, not all historical returns from today's batch.
- No guessed migration/backfill of ambiguous legacy allocations. Unambiguous one-slice legacy may be interpreted with explicit proof; ambiguous historical rows keep typed blocker/owed work, never fabricated availability.
- PaymentReturnSnapshot exposes amount/currency/checkout/status; IPaymentRefundGateway is not a refundable-balance read, and the dev FakePaymentRefundGateway explicitly ignores amount/currency. Therefore Returns must not rely on it as proof that shared checkout budget is protected.
- Before each NEW first/retry monetary attempt, serialize Returns-managed budget per payment, count Pending+Succeeded obligations (definite Failed alone releases its attempt budget), persist reservation via existing attempt rows in owning transaction. Match checkout/payment/currency/case binding. Exact replay never counts twice; external call after commit. This bounds Returns-mediated refunds only; no claim about refunds initiated outside the observed owning facts or real PSP readiness.

## UI/product laws
Use actual template react-toastify via existing AppProviders; shared DS Dialog, no second container/custom overlay. Actual current capabilities only.
Show original claim, approved/rejected/received/remaining quantities, effective decision and immutable history, money/physical progress separately. Reopen requires explicit reason and reviewed version. Unknown money cannot be reset/replanned.
Customer child cases remain seller+line+reason; one line can have multiple reason portions whose total stays bounded. No correct/unselected goods sent. Unknown child bodies survive close/remount/viewer change without replacement.
No broad recertification, unlimited stress harness, reset/new demo or screenshot request. Owning/mounted/rollback-only relational proof; disclose what's controlled vs actually served.
