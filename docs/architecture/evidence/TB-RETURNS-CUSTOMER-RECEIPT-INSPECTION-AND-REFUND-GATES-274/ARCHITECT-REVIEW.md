# Architect review — TB-RETURNS-CUSTOMER-RECEIPT-INSPECTION-AND-REFUND-GATES-274
Date: 2026-11-16
Reviewed main/code/evidence: 7e7c2ee3d97506408112432775abeeb56715c3fd
Decision: PARTIAL — NOT ARCHITECT ACCEPTED. Only274-R1 bounded completion/repair next.275/276 NOT ISSUED.

## Independent review and preserved foundation
Compared issuedca2dbed34 to final main (1 commit ahead/no divergence); read receipt operation/aggregate/summary, shared disposition, approval/retry/recovery/decision paths, endpoint parser/DTO, actual migration/model, proof and receipts.
No Architect local runtime/tests/database/browser execution. Worker25/25 new unit and6/6 rollback relational counts and served Host observations are attributed to worker evidence.
Preserve cumulative receipt ledger, partial/mixed inspected quantities, ordinary coverage gate and persisted reason.273-R2 remains accepted within its prior repair scope.

## Source-confirmed blockers within274's issued requirements
### A — omitted inspection is still silently Usable
AdminReturnReceiptLine defaults Condition=Usable; ReturnItemConditionParser.Parse(null/blank) returns success Usable; CustomerReceiptLine also has an optional Usable default.
The task explicitly forbids this. Missing/null/blank inspection must refuse before read/write, and typed construction must require a deliberate condition. Unknown condition must not enter the stored summary as usable either.

### B — false stock completion without Inventory
SettlePhysicalDispositionAsync calls Inventory only if ReservationId exists, then unconditionally marks SaleableRestock. For an inspected usable amount with no binding, no Inventory confirmation occurred but physical completion is announced.
Require a typed pending binding blocker; preserve money, stable item key and Disposition=None. Read and action must agree. Mixed inspected quantity remains split in the authoritative receipt summary, never labelled wholly usable from the old item flag.
Rejected/cancelled cases with recorded received goods currently return recovery Phase=None/no blocker via the unchanged status switch. Required remaining physical-resolution blocker is absent; preserve facts and expose it without inventing return-to-customer or restock.

### C — stored evidence and database integrity are incomplete
ReturnReceivingSummary classifies every non-Damaged enum as usable and checks neither positive received quantities nor defined conditions. With malformed negative/undefined stored facts it can produce a complete usable balance.
The applied migration expressly omits positive-quantity/defined-condition CHECKs required by274. Receipt-line ReturnRequestId is also not tied by FK to both header's and item's case; independent single-id FKs can permit headerCaseA + line/itemCaseB.
Strengthen the shared summary/write/read refusal and add the bounded missing DB constraints/local composite ownership FKs in a NEW additive migration. Do NOT edit/reapply the already-applied274 migration. Do not repair bad data.
Before insertion, already-incoherent cumulative evidence must block instead of adding more lines. Owning seller scope must be checked before writes/replay for the modified commands, including Approve/Reject's supplied SellerPartyId and internal receipt callers via existing authority.

### D — decision proof contradicts actual code
Proof §3 and worker result claim global Clear removed, but ReturnDirectory.Decisions.cs collision catch still calls _db.ChangeTracker.Clear(). It also filters only constraint name (not table/schema) and returns the collision winner without reusing changed-reason comparison. Caller-owned transaction/savepoint recovery remains unhandled.
Fix with exact constraint identity and scoped/savepoint-safe failure handling, then use one winner comparison on both normal read and collision re-read. Wrong reason refuses with no overwrite; unrelated tracked caller work survives. No wholesale cleanup of the separate legacy approval algorithm is authorized.
The discrepancy must be explicitly corrected in new evidence; original report remains unchanged.

## Migration / execution audit
274 applied20261116122318_AddCustomerReceiptsAndDecisionReason. Worker reports a pre-apply private dump,2966890B,SHA256 ADCE728BD0D5732C09F5EECDC219287859CB7FE56173F9F5BD07A10BDC64CD16,time16:13:46 and only-this-migration plan. Receipts provide hash/size/apply summary but no actual archive-list validation or complete backup-before-apply command chronology. Architect cannot independently verify that private file. Do not retroactively manufacture a receipt; verify the original archive now and label current verification honestly.
Fresh actual pre-apply backup with full chronology is mandatory before R1's new additive constraint migration. No restore/drop/reapply/cleanup.

Whole Returns/broad-AMSC/unchanged273 relational reruns, stale migration-list pin repair and characterization-harness maintenance exceeded274's permitted selection. Preserve reported observations without claiming module recertification or independently validated baseline improvement. Do not repeat broad runs or churn historical pins.
Recovery context top pointer was not advanced consistently; repair all CURRENT pointers coherently.

## Next
One274-R1 only. No frontend/provider integration/new business stages. Keep rollback-only test records and empty-live-case proof limits; no committed fixtures or real effects.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
