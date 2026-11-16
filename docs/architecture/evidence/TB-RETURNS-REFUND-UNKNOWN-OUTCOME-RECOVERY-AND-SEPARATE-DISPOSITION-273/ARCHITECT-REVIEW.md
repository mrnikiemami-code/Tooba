# Architect review — TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273
Date: 2026-11-16
Reviewed main/evidence: bf2dc514fdd68ab83b512aa78d5388c55975fa6f
Code: 05cafa54aa4e2f9347ff37f4ce7295cfab1b12b2
Decision: PARTIAL — NOT ARCHITECT ACCEPTED. Issue only273-R1 bounded repair;274–276 NOT ISSUED.

## Review scope
Independent compare against issued fcb2fe1cd, read Recovery policy/command/directory/outcome/physical helper, endpoint/Contracts, 15-test source and receipts/proof. Two commits ahead, no divergence.
No Architect local tests, database access or runtime/browser execution. Worker15/15 and health/wire results remain worker evidence. The empty/nonexistent-case route probes did NOT exercise RecordRefundOutcomeAsync/FOR UPDATE; simply running a relational Host does not prove that branch.

## Retain
Existing-attempt keys, distinct physical intent, typed unavailable classification, additive backend CQRS/endpoint and money-versus-stock fields are the correct direction. Unknown exceptions do not mint new attempts; Wallet keeps case key; ordinary physical recovery is blocked. No frontend work/migration/live business mutation claimed.
The broader whole-Returns and broad AMSC runs were outside the named focused validation scope; their four known failures are disclosed. Do not repeat those broader runs in repair.

## Source-confirmed defects
### A — read policy does not establish the evidence it promises
ReturnRecoveryPolicy.Describe calls any single Pending attempt "bound" without validating payment, amount, currency or nonempty idempotency identity; the money command then performs extra checks. It also never sees the durable approval decision: RecoverAsync rejects an opposite decision only when a row exists, while an absent decision is accepted. The positive tests seed no ReturnDecisionRecord, so this absence is not covered by15/15.
Completed accepts ANY Succeeded row, and RecoverPhysicalDispositionAsync relies only on that FinancialComplete boolean before stock operations. A mismatched succeeded attempt can therefore authorize physical recovery; ambiguity is not closed at the common policy.
The Settled early return uses item dispositions alone and ignores courier ClosurePhase/ObligationClosedAt/StockSettledAt/source binding. If all item writes land but the last RefundReleased save is interrupted, read becomes Settled and RecoverAsync returns early forever, skipping the unfinished final marker.
Repair common stored-fact evidence/binding/closure authority for both reads and mutations. A positive recorded money fact may be reported while other inconsistent work is blocked; do not invent a new approval/receipt or erase durable money.

### B — caller/ambient transaction can span external effects
RecoverAsync/ExecuteRefundAsync/physical recovery do not refuse an existing caller transaction. RecordRefundOutcomeAsync explicitly joins CurrentTransaction and does not commit it, then ApplyRefundSuccessAsync can call Inventory and RecoverPendingRefundAsync returns a snapshot before caller commit. The advertised "no transaction spans external call / durable before stock" guarantee therefore holds only for the no-caller branch.
Repair by refusing effectful recovery under caller/enlisted/ambient transaction before any external effect (or an equally bounded lawful isolation path). Never commit/rollback a caller transaction to escape it. Recorded read-only completion can still return if validated. Preserve caller tracker/state and exact attempt.

### C — fabricated provider reference remains
ReturnDirectory.cs line499 still uses result.ProviderReference ?? $"refund-{attempt.RefundAttemptId:N}". This is precisely the synthetic PSP reference forbidden by273.
GatewayRefundResult.ProviderReference is nullable: preserve an explicit success fact truthfully even when no reference is supplied, e.g. nullable/blank reference through the existing nullable column, without fabricating an id or declaring a real provider rejection. Keep Wallet's own distinct ledger/case identity separate.

### D — proof gaps around final closure and relational coordination
Existing success/lost-save tests are useful but do not cover missing approval, incoherent succeeded row, caller transaction effects, null reference or interrupted final closure marker. The SQL row-lock/outcome branch was not automatically exercised; no valid live case exercised it either.
Add targeted regression coverage for A–C and the last-marker interruption. Use rollback-only relational evidence for actual guard/coordination where needed; no committed fixtures/deletes or two-writer/stress demand.
Stock/disposition save failures must not leave unsaved tracked flags that allow a same-scope retry to announce completion; restore/reload affected owned state with scoped handling, never global Clear.

## Constraints / next
No receipts/inspection lifecycle/UI/provider integration/schema expansion in repair. Retain272-R1 acceptance and previous audit/history. No live case, money/stock/provider call or fixture to prove action.
Original273 result/evidence unchanged. Repair outcome must distinguish controlled tests, actual relational coverage and merely served route evidence.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
