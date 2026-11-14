# Architect review — TB-SHIPPING-COURIER-ORIGIN-RETURNS-LINKAGE-AND-OBLIGATION-CLOSURE-265
Date:2026-11-14
Decision: PARTIAL — NOT ACCEPTED; ONLY265-R1 repair of introduced seams.
Reviewed main d6bdc88c99502e115ebd3e3d4e7f64087dadc0f2, implementation bf5ebe5e4.
Basis independent repository source/diff and supplied evidence; no independent Windows runtime tests.

## Source-confirmed blockers
A OrderReturnObligationClosure has ONLY IOrderReturnReader, no writer/store. CloseLineObligationAsync returns line.Quantity-request.Quantity and Closed=true without persisting case/claim/line closure or accounting prior closure. It is validation, not reported durable monotonic Order cancellation.
B CourierReturnSourceDirectory.SettleApprovedAsync selects ALL damaged resolutions matching any claimed OrderLineId, not source shipment/receipt, and cancels whole Created parcels. It retains ONLY first reservation with ??= and releases claim.Lines.Sum quantities from that reservation. Multi-source/line/partial cases can cancel siblings or free the wrong amount; no per-resolution receipt/quantity allocation. Claim->case partial-link retry returns existing case without repairing link; paid validation happens after durable Claim. Shared claim-vs-resend/replacement race and package-lock command boundary are not proven by claim-vs-claim test.
C InventoryReturnObligationRelease executes Reserved decrement before SaveChanges with no explicit transaction covering position/reservation/inbox. A fault before Save leaves released availability without its durable identity. Replay checks key only, not reservation+requested quantity. Zero/Consumed result is labelled Released even if still Consumed. New partial release needs owning atomic identity and actual remaining quantities.
D Courier approval/refund is not complete safe recovery:
- Returns RejectAsync now silently skips Reject when status!=Requested but still calls ReleaseCourierOriginClaimAsync; a still-Requested source claim can be released while case is already RefundProcessing/closure in progress.
- ExecuteRefundAsync marks money success then calls SettlePhysicalDispositionAsync inside same catch. Item is marked SaleableRestock BEFORE owning restock call; downstream failure is caught as refund failure (or invalid Completed->RefundFailed transition). RetryRefundAsync mints another provider key and does not ensure courier closure; stock failure/lost refund response must not cause second money movement.
- completed replay with final phase is not explicitly answered before ordinary Approve transition; concurrent approval/reject/stock-settle boundaries need actual owning serialization/reconciliation.
E Actual operator handoff incomplete: diff operational section adds Create button only, no persisted View-case link; no FE Return detail mapping/render update for origin/phase/condition/disposition. New typed reasons not visibly localized in changed error/resource paths. Mounted Create + live PendingPayment/no-action GET do not demonstrate actual create->existing case->approval/user path.

## Retain
Explicit courier-origin model, Contracts separation, source claim intent, condition distinction and existing UI pattern are useful. Keep scheduling/mobile accepted; no global rewrite. Financial/user acceptance not claimed.
One bounded265-R1 repairs A–E with targeted public-path fault/partial/source tests and one real task-owned operator handoff; no broad/base parity/green reruns. No live refund or user-data mutation. Parent-package/provider remain deferred, package child guards still required.
