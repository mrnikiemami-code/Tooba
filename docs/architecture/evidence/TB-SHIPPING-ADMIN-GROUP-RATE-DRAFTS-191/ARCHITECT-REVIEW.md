# Architect review — TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191
Date: 2026-10-22 (Asia/Tehran)
Reviewed HEAD: 9384b017b98a7406c7c0813c8ba21593fd9518d1
Code: eefaa4a97611bfc4f5599f6179262839a8157e56
Evidence: 7ac0dbec369149ca559dee864f384f654bf1e792
Verdict: REPAIR_REQUIRED. Worker PARTIAL is not Architect ACCEPT.

## Bounded positive review
Source shows typed group-owned rate rows without geography duplication; full-tuple domain overlap, required collection validation, server currency binding, independent fees/duration, shared root revision and canonical owner-authorized GET/PUT. No live evaluator/activation is present. Worker reports Shipping 574/574 and focused PostgreSQL proof; Architect read code/diff/evidence but did not execute .NET/PostgreSQL here.

## R191-01 — database CHECK predicates do not mirror domain invariants
Actual SQL in 20261022065333_AuthoringGroupRateRows and the current EF mapping:
- weight/count/merchandise interval CHECKs use (min IS NULL AND max IS NULL) OR (min >= lower ...). They omit an explicit min IS NOT NULL in the second branch. With min=NULL and a positive max, the predicate can evaluate UNKNOWN, which a PostgreSQL CHECK accepts. Domain/transport reject that max-only state.
- ck_authoring_rate_rows_amount_within_max checks fees and max_merchandise_amount but not min_merchandise_amount. A min merchandise amount above 1e9 with max=NULL satisfies the SQL guards while Domain rejects it.
- currency canonical CHECK only checks length and upper(currency). Values such as '123' or 'A$!' satisfy it, while NormalizeTrustedCurrency requires exactly three ASCII letters.

These are source/SQL-semantic findings; no independent runtime insert was executed by Architect. Require direct negative PostgreSQL storage tests bypassing the domain, a new corrective migration and current-model parity. Keep TB-191 and older migrations byte-identical; do not edit published history or invent a new money/currency policy. numeric(18,2) coercion does not itself prove rejection of source precision before rounding; the existing API/domain precision guard remains necessary.

## R191-02 — requested cross-operation concurrency proof is missing
A_same_revision_rate_row_race_leaves_exactly_one_winner_and_only_its_rows races two ReplaceGroupRateRowsAsync calls. The sequential stale check and reorder/delete test do not demonstrate the requested race between ReplaceGroupRateRowsAsync and ReplaceRegionGroupsAsync. Add a deterministic two-reader-before-write test for those TWO DIFFERENT operations using the same root revision, with exact winner-specific whole-tree/rate/currency/identity assertions and actual-loser tracker/rollback checks. Include retained-group edit and group removal interaction without sleeps or lock-holding barriers. Do not claim a runtime defect unless this regression demonstrates one.

## Process deviation — separate from technical verdict
Worker reports ~60 minutes against a 45-minute HARD task, plus a forbidden whole Tooba.Host.Tests run consuming ~33 minutes. This violates the task; the broad run is not acceptance evidence. The claim that all 132 Host failures are pre-existing/nonregressions is NOT independently established here: no same-base controlled comparison was supplied. Label attribution UNVERIFIED; do not rerun/repair Host or widen scope to prove it.
Preserve historic counts/result and append this correction. Future repair has explicit command allowlist, per-command deadline and reserved cleanup/report time. Do not continue after its deadline.

## Next and retained gates
Only TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191-R1, parent TB-191, is authorized next: constraint parity plus missing cross-operation proof. No TB-192, frontend, metadata, taxonomy, schedule, modes, evaluator, financial/deployment change or recertification.
Runtime merchandise measurement/discount/tax/group-allocation gate stays OPEN_NOT_IMPLEMENTED; TB-185 paused. TB-190 acceptance is retained; TB-191 acceptance is pending this repair.
