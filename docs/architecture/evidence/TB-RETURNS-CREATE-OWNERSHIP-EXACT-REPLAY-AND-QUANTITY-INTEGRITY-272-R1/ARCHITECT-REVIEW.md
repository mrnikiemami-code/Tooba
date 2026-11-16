# Architect review — TB-RETURNS-CREATE-OWNERSHIP-EXACT-REPLAY-AND-QUANTITY-INTEGRITY-272-R1
Date: 2026-11-16
Decision: ACCEPT WITHIN THE ISSUED A–C REPAIR SCOPE.
Reviewed source/evidence main: b5d9b9aecdcc13909099c9117887861e2bf0ae46
Code: e2a69f931b03caf9d9fa8189c10ea798359a2960
Parent272: creation-integrity implementation scope is closed through this repair. The original272 PARTIAL review and its unauthorized committed-fixture/delete audit remain historical facts, not retroactively accepted execution.

## Independent review
Compared expected base71f5170f7 to final main (2 commits, no divergence). Read Creation.cs, serialization, aggregate factory, persisted quantity rule, real relational class and evidence/receipts; inspected changed-path set and current refund/recovery seams for next-task planning.
Architect did not execute local tests, database queries or browser/runtime; worker counts and served process observations are attributed to worker receipts.

## A — accepted bounded precision and binding repair
PersistedReturnQuantity centralizes six-fractional-digit equality. Validator, owning directory before stored lookup, and shared private factory refuse over-scale input; accepted fractional values and trailing-zero equality survive.
Intent now binds checkout and seller-party alongside seller-order/customer/origin/destination/reason/ordered lines. No migration or finance formula rewrite.
This acceptance is the issued fractional-scale repair, not a general numeric-range or pricing certification.

## B — accepted scoped failure handling and invariant
Creation records preexisting entity identities, detaches only additions from the failed attempt instead of global ChangeTracker.Clear, and introduces/restores a caller savepoint. Caller transaction is not committed/rolled back by the directory; owned handle controls its own commit/rollback.
Ordinary public Create has no bypass flag. Private Build supports the courier factory's separate line/condition identity.
Controlled exception injection exercises the owning recovery branch; a separate real-index violation proves the provider error shape. This is not a committed concurrent-winner proof and not a production outbox-interceptor certification.

## C — accepted rollback-only harness
All6 facts use explicit framework skips on missing/rejected target, validate client loopback5432/tooba_alpha and server identity, use caller-owned rollback transactions and zero-committed-row observations. No CommitAsync/ExecuteDelete/ExecuteUpdate/TRUNCATE/DELETE path remains in that class.
Prior272 commit/delete tests were not replayed. Their results remain outside the task authorization.

## Evidence and limits
Worker reports: creation behavior18/18, rollback-only relational6/6, updated error-set pins passing; scoped guard run also contains1 known pre-existing migration-list failure. Build/publish0errors; Host5088 refreshed, health/readiness200; FE unchanged.
The repaired harness does NOT exercise successful owned-transaction commits or a committed two-writer race. No eligible live return case/operator walkthrough. No independent Architect runtime verification. Preserve pre-existing characterization/migration-pin failures and previous audit disclosures.
No refund/provider/financial/user/full-Returns acceptance follows from this source/evidence checkpoint. Existing approval destination semantics, money precision/formulas, ordinary receipt/inspection and refund recovery were outside272-R1.

## Next
Only TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273 is to be issued next. It must preserve existing receipt/source authority and refuse an unproven ordinary physical disposition;274–276 remain candidates NOT ISSUED.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
