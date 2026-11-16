# Architect review — TB-RETURNS-CREATE-OWNERSHIP-EXACT-REPLAY-AND-QUANTITY-INTEGRITY-272
Date: 2026-11-16
Reviewed main: b9df36a1dcdc63c8d5d5ec61761d0deb3da4bc3c
Code: 4d0ba4e169bff5c529ff387509c4fa920a31930f
Decision: PARTIAL — NOT ARCHITECT ACCEPTED. Retain implemented improvements; issue one bounded 272-R1 repair. 273–276 NOT ISSUED.

## Review performed
Independent GitHub source read of Creation/serialization/domain/DbContext/validator/evaluator, customer endpoint and admin adapter, both new test classes, receipts and creation proof; compared issued base to final main (two commits, no divergence).
No Architect local execution, live database inspection, browser session or rerun of worker tests. Counts and runtime/zero-leftover observations are worker evidence.

## Improvements retained
Ownership precedes ordinary replay, order/customer/origin/line-set/reason/destination comparison exists, duplicate ordinary lines are refused, RefundFailed occupies canonical capacity. Creation uses a Returns transaction and per-order transaction advisory lock before eligibility; exact idempotency-index filtering replaces broad provider classification.
Worker receipts report 13 focused behavior passes and 5 relational passes. The four reported pre-existing failures remain disclosed, not repaired or treated as new failures by this review.

## Source-confirmed repair requirements
### A — quantity precision is normalized only for comparison
ReturnDirectory.Creation.cs: intent Capture/Matches rounds quantities to six decimal places, while the actual quantity check, lineSnapshots and aggregate RefundAmount calculation use the raw incoming decimal. ReturnsDbContext stores quantity numeric(18,6), amount/price numeric(18,4).
For example 1.0000001 and 1.0000002 both compare as 1.000000 although they are different accepted raw intents; a positive 0.0000004 can pass the raw positivity check and persist as zero. First answer maps tracked raw values whereas a subsequent context reads database-rounded facts. These conclusions follow from source/schema mappings, not a locally executed exploit.
Repair by refusing quantities that cannot be represented exactly at six decimals before any write or replay. Do not silently round quantities or redesign money formulas. Preserve legitimate fractional quantities, trailing zeros and ordering equivalence. Include canonical checkout and seller-party ids in the stored binding comparison; these are available from OrderReturnContextSnapshot but absent from OrdinaryCreationIntent.

### B — collision recovery erases the caller's tracking scope
Creation.cs line144 calls ChangeTracker.Clear() for every recognized key collision, including the explicitly supported caller-owned transaction branch (lines68–71). This detaches unrelated tracked caller entities. The code must remove only this failed creation's additions and preserve existing tracked state, transaction ownership and savepoint/failure semantics; an outer transaction must not be rolled back/committed by this method.
ReturnRequest.Create also exposes enforceOneLinePerOrderLine:false publicly. Keep ordinary creation's invariant unconditional on its public API; a private shared factory can support the distinct courier-origin condition identity.
No claim of an observed live failure; these are direct control-flow/API findings.

### C — relational proof violates the issued data boundary
The issued task allowed only task-owned rows in ROLLED-BACK transactions and explicitly allowed reporting committed two-writer proof as NOT proven. The new relational class instead commits cases on the canonical DB then runs ExecuteDeleteAsync cleanup. This is a task-boundary deviation even though worker reports no user-existing row touched and zero leftovers.
Additionally every fact returns normally when TB272_CONNECTION is absent, so an absent environment can report a pass with no relational work. The target restriction is only a comment; Connection accepts arbitrary supplied targets.
Repair the harness so it cannot commit/delete test business records on canonical data, validate permitted target without printing credentials, and explicitly fail or framework-skip missing setup. No further committed race/re-run or cleanup is authorized.
Previous worker evidence remains historical and must not be rewritten. Label prior two-writer observation as reported but obtained outside authorization; future rollback-only tests must state their actual contention/rollback proof and limitations.

## Audit / handoff
Persist the worker's original result and deviations unchanged. Correct current summary claims (ZERO_LIVE_BUSINESS_WRITE/no cleanup were not true for task-owned committed records in business tables). Do not claim pre-existing user rows were changed without evidence.
No restore, cleanup replay, reset, migration or provider/money operation follows from this review.
Single task 272-R1 only, no screenshots/delegation/loop. Shipping acceptance/history remains unchanged.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
