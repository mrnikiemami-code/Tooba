# Architect review — TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273-R1
Date: 2026-11-16
Reviewed main: a2ffbf60de3151037c6512461289d0c99bc7ccca
Code: 150179bbc6acae82d915c1919f3fd8cfc4884a93
Decision: PARTIAL — retain repaired reference, positive approval/binding and durable marker restoration; only273-R2 for three remaining gate omissions. Parent273 NOT ACCEPTED.274–276 NOT ISSUED.

## Independent source/evidence review
Compared issued1e4e7cb67 to final main (2 commits ahead, no divergence). Read full recovery policy, recovery directory, outcome/marker/transaction helper, physical helper, relational test source and binding/transaction proof. No local Architect tests, database execution, browser or runtime verification.
Worker24/24 unit and2/2 rollback relational receipts retained. The broader whole-Returns/broad-AMSC validation was explicitly forbidden and remains a disclosed execution deviation; do not repeat it or repair its already-known failures.

## Closed portions retained
Recorded approval is now present in snapshot/policy; payment/amount/currency/key binding, explicit last-marker pending state, scoped item/case reload after persistence failure, null/blank PSP reference storage and EF/ambient transaction refusals are implemented.
Marker-only recovery and interrupted item/final-save controlled tests are useful. No source reset/rewrite or general refund redesign is needed.

## Remaining source-confirmed omissions — only these authorize273-R2
1. **Pending money bypasses courier-source coherence.**
   ReturnRecoveryDescription.cs lines113–130 calls ReadMoney then returns PendingMoney. HasCoherentCourierSource is called only from Physical at line294, after a succeeded attempt. RecoverPendingRefundAsync trusts that description then calls EnsureCourierOriginClosureAsync. The latter can skip completed phases from stored closure flags and does not validate every source/receipt/item link.
   Therefore malformed courier receipt/source/condition evidence can still be advertised as resumable and can reach money. Gate the SAME courier coherence in the common policy before offering ANY effectful courier recovery, including pending money. Preserve a positively bound success fact while blocking inconsistent physical work.

2. **Settled does not require the prior durable closure timestamps.**
   Physical lines301–312 checks RefundReleased + all items disposed. HasCoherentCourierSource validates ids/item links/dispositions but never checks ObligationClosedAt/StockSettledAt. A forged/incomplete final-phase marker with either prior timestamp absent is reported Settled and the physical command returns early; the stricter finalization writer is never reached.
   Require coherent prior marker/timestamp evidence in the read decision too. RefundReleased with missing earlier evidence must be a typed evidence conflict, zero effects, not automatic repair/backfill. Keep legitimate StockSettled + both timestamps + disposed items as pending final-marker work.

3. **Explicit enlisted transaction is not covered.**
   RefuseConstrainedTransaction checks only Database.CurrentTransaction and Transaction.Current. EF provider's explicitly enlisted System.Transactions transaction can exist when BOTH are null (e.g. EnlistTransaction with a CommittableTransaction outside an ambient scope).
   Check actual provider-enlisted state as well using supported EF/provider API. Add one rollback-only real PostgreSQL negative with CurrentTransaction==null and Transaction.Current==null but explicit enlistment present; recover must refuse before any external/write effect and leave the caller transaction/tracker untouched. Do not commit/dispose caller ownership in production.

## Proof and scope boundary
Add targeted tests for invalid courier source in PendingMoney and incomplete prior timestamps with RefundReleased. Keep valid pending/marker-only/settled paths green. One new explicit-enlistment fact; no stress/committed fixtures or broad test reruns.
Existing FOR UPDATE outcome coordination remains NOT relationally exercised; accepting that declared limit does not justify claiming live proof. Do not build another harness to chase that limit.
No new migration, receipt/inspection stage, provider integration, UI or cleanup. No new task besides273-R2. Original results/reviews unchanged;272-R1 acceptance/prior audit preserved.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
