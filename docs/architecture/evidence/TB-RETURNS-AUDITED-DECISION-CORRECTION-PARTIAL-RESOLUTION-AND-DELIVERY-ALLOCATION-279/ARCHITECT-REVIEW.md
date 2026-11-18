# TB-279 — Architect source review — PARTIAL
Date: 2026-11-18 (Asia/Tehran)
Reviewed code: 9c0885a5943e7f1a4e1a09a17fac417d23037aa6
Expected code base: 57cccbbe7cffea23c30ee0f474291cb3dda7bc58; actual claim 6463fdac85a5db66c6d454d4eac5b5e5f89db21b is the permitted docs-only issuance.
Verdict: **PARTIAL / NOT ACCEPTED**. Preserve the worker result and historical acceptance records unchanged.
Method: independent GitHub source reads at the exact code SHA. Worker test/runtime/backup results are reported evidence, not Architect-executed checks. No Architect live POST, SQL, test run or screenshot.

## Retained work
The additive decision history, effective-decision read, resolution/custody/allocation structures, explicit first-refund separation, template react-toastify usage, and customer same-line second portion are useful. Existing completed cases are historical facts, not fixtures to reset. Successful schema application does not prove effectful business branches.

## Blocking findings (one consolidated repair boundary)

### F1 — shared budget does not cover retry; first-start budget lookup fails open
ReturnDirectory.cs: RetryRefundAsync, lines 330–397, persists a NEW child with SaveChanges and executes it without ReturnBudgetSerialization or the first-start budget reservation transaction.
Concrete consequence: A's definite failure releases its budget; B reserves that payment's capacity; A's reviewed retry can reserve/send again without counting B.
ReturnDirectory.FirstRefund.cs: PersistFirstAttemptUnderLockAsync calls _payments.GetAsync while owning seller/case/payment locks and its transaction are held. The guard is `budget is not null && ...`; a missing second lookup skips the ceiling. Moving the money call outside the transaction does not remove this cross-module call from the lock.
Required: one owning reservation protocol for EVERY new first/retry attempt, Pending+Succeeded included, exact terminal replay first, positive immutable payment/customer/checkout/currency/amount binding, missing authority refused. Acquire external owning facts before short database coordination; no external port call under the transaction. No universal balance claim for refunds outside Returns.

### F2 — partial resolution does not govern actual Inventory effects or partial receiving gates
ReturnDirectory.CourierOrigin.cs:263 onward, SettlePhysicalDispositionAsync still uses receiving.Lines.Usable and Damaged for the entire received line. It does not read accepted quantities from the reviewed resolution.
Concrete example: requested/received 3 usable, accepted 2, rejected 1: the actual restock call still receives 3, including the customer's rejected portion.
ReturnDirectory.cs:213 and ReturnDirectory.FirstRefund.cs ordinary branch still require receiving.IsComplete for the WHOLE requested quantity. A reviewed accepted 2 received / rejected 1 never received cannot approve/start despite a complete resolution.
ReturnCorrectionModels.cs derives AcceptedUsable as Min(Accepted, Usable), silently choosing the accepted condition; physical completion tests only one coarse Disposition per item. Mixed accepted usable/damaged portions do not each obtain a positive durable physical confirmation.
Required: read/action/Inventory all consume the SAME reviewed per-condition portion authority. Explicit accepted/rejected condition quantities; no inferred usable-first split. Require evidence for accepted portions, account for unreceived rejected remainder without inventing receipt, restock accepted usable only, persist each required physical result, keep handback separate. Legacy completed facts stay unchanged.

### F3 — rejected received goods release capacity too early; custody condition is not bounded
ReturnDirectory.cs:285–288 releases ALL allocations on rejection. ReturnEligibilityEvaluator.cs occupancy selects statuses Requested/Approved/RefundProcessing/RefundFailed/Completed and omits Rejected, even when goods remain physically held. A second case can claim capacity while the original still owes handback.
RecordCustodyAsync checks total OwedCustody, but does not bound the command condition against the corresponding rejected received usable/damaged balance. A nonblank proof string is accepted; the comment that tracking alone is insufficient is not an implemented proof-kind rule.
Required: hold received rejected portions until actual evidenced handback; release only positively eligible portions under seller serialization. Condition-specific handback ceilings and deliberate proof semantics. Whole-case rejection with received goods must expose a lawful handback path without requiring a prior partial plan or falsely closing the obligation. Never infer delivery from tracking alone.

### F4 — reviewed decision fencing and atomic transition are incomplete
StartReturnRefundCommand contains only ReturnRequestId and ActorUserId. StartFirstRefundAsync chooses expectedDecisionVersion from a fresh POST-side snapshot; this is not the version frozen in the operator's reviewed command. A stale review can act on a newer reopened/reapproved decision.
RecordReviewedDecisionAsync validates expectedVersion only when supplied. It commits its own decision transaction before ApproveAsync changes status/approved amount and before RejectAsync changes status/releases allocation. These later writes are outside that decision lock and can race correction. BuildDecisionOccurrenceKey uses process-randomized string.GetHashCode instead of a durable exact reviewed identity.
Required: freeze and transport required decision/resolution versions and exact occurrence identity; exact recorded replay precedes mutable gates. Persist the decision, case transition/frozen amount and allocation change in the SAME owning short transaction; re-read after its lock. Fence correction versus attempt persistence. Stable canonical identities; no GetHashCode-based durable key.

### F5 — ambiguous legacy allocation is guessed
ReturnAllocationPolicy.Describe subtracts unbound legacy quantities through oldest slices, including expired slices. The task explicitly forbids guessed attribution when the originating delivery slice is ambiguous. Different valid legacy origins can imply different remaining capacity; an oldest-first assumption is not positive evidence.
Required: retain deterministic oldest eligible allocation for NEW claims, but block ambiguous unbound legacy attribution with a typed explanation. Allow only positively provable unambiguous legacy cases; no backfill/data repair or newly invented delivery date.

### F6 — 278 customer durability repair remains largely absent
return-request-modal.tsx:153 reads window.sessionStorage without catching the property getter, turns unreadable into retained=null, and resets inFlight on open/checkout/viewer change. submit() unconditionally builds/stores a NEW act. dispatchChildren (306–370), callbacks, notifications and finally have NO epoch fence. Closing/unmounting/retargeting can therefore leave a possibly sent act replaceable and permit stale continuation.
return-submission.ts parseChild accepts an arbitrary body without comparing it to seller/line/quantity/reason/key/destination; clear compares submissionKey only; outcome lookup scans every viewer's stored keys. viewerId remains optional with a shared-key fallback. The customer modal still renders a custom fixed div role=dialog rather than the shared DS Dialog.
Required: protect pending/unreadable evidence before any new write; safe storage acquisition; required authenticated viewer scope; exact body/envelope validation and compare/update/clear; epoch fence all sends and late callbacks without allowing a stale request to release a newer lock; preserve known outcomes on their exact originating envelope. Use the existing shared Dialog and the existing single template toast provider. No new modal/toast framework.

## Proof boundary
The reported 172/172 and mounted suites do not cover the counterexamples above. No relational business fact was added for the newly introduced case/payment coordination. A healthy Host and applied migration do NOT exercise FOR UPDATE/advisory reservation branches. Add focused owning-directory regressions and narrowly targeted rollback-only real-database coordination proof; do not claim a committed race or real provider effect.

Worker disclosed three failed first-migration applies, the later normalization migration, three Host refreshes and broad suite runs. Preserve those receipts and verified backup chronology. Live POST refusals on settled cases also departed from the issued GET-only boundary, even if no rows changed; do not repeat them. The stale 276 mounted fixture remains an open small check, not hidden evidence of acceptance.

## Next state
All serious and small remaining work is collected in docs/planning/returns/RETURNS-279-CONSOLIDATED-CLOSURE-CHECKLIST.md.
**No new task is issued by this review.** No automatic implementation, no new live review case, no existing-case reset, no further migration/apply authority. STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE.
