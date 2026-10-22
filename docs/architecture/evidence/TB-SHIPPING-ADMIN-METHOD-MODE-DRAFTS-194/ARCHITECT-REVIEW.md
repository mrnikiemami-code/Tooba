# Architect review — TB-SHIPPING-ADMIN-METHOD-MODE-DRAFTS-194

Date: 2026-10-22
Source checkpoint: 6fde6ee768a9b6639d1f4df10600e19c327bd9c1
Verdict: ACCEPT for bounded method-root draft settings only.
Production functional acceptance: PENDING.

## Reviewed source and limits
Reviewed the complete changed production surface, endpoint harness and new PostgreSQL tests. One own optional settings row holds two independent typed choices. Reuses ShippingFreightPayerMode; required nullable transport values reject absence and undefined enum members before mutation. Unconfigured reads remain explicit. Owner authorizer precedes dispatch on both audiences; scope comes from server authority. Existing module-owned CQRS/Result/error/resource and Contracts-only patterns remain intact; no new project/dependency or Host business code.

ReplaceMethodSettingsAsync checks expectedRevision before preflight, only applies/saves on changes, uses the existing root concurrency token, and clears tracking on persistence failure. Source therefore preserves UpdatedAt on identical PUT; however the endpoint and storage no-op facts assert revision only, NOT timestamp. Do not describe timestamp invariance as an independently executed assertion.

The real-provider race gates first SavingChanges after both reads, asserts both ORIGINAL revisions 1, exactly one winner and canonical loser, final root revision 2, winner-dependent settings/rate state, raw row counts and the ACTUAL loser tracker/reread. Raw storage tests assert NOT NULL, exact enum CHECKs, PK and own FK errors in independent rolled-back statements. Migration is additive and prior migrations/designers are unchanged in the diff.

Executed preservation evidence configures rates/presentation/schedule BEFORE settings and executes region replacement AFTER settings. The transport result's claim of later rate/presentation/schedule writes preserving settings is not demonstrated by these new facts. Their preservation is source-supported: settings are a separate own relationship and the other writers do not assign/remove it. No claim of those three post-settings execution sequences is accepted. No production defect was found requiring another repair.

## Reported commands, not architect execution
Worker reports build zero errors, endpoint 4/4, PostgreSQL storage 3/3 (three repeated focused runs), final Shipping 616/616, zero failed/skipped; disposable target removed. Architect reviewed source remotely and did not rerun commands or inspect the user's live DB/browser.
Corrections: actual route universe is 39 -> 43 (four added routes), not transport text 41 -> 43. Request count 28 -> 30. Reported claim/result interval is 21.3 minutes; the phrase "final focused-suite window was minute 38-45" is incompatible with that interval and is not used as timing evidence. Reported duration fits the 45-minute bound; independent process timestamps unavailable.
The appended TB-194 rules heading is renumbered 25 to avoid collision with existing section 16; body semantics unchanged.

## Accepted scope and open gates
Accept persistence/authorization/explicit choices and reviewed concurrency/storage proof. Manual/CarrierApi/Free selection is not runtime evaluation, carrier readiness, real costs/funding accountability or activation. Free retains stored manual configuration; approved eventual customer components are all zero with explicit real funder/cost accountability still open. No product COD/packaging timing inference.
Admin UI/configured live registry, taxonomy, cutoff, basket/group measurements and amount basis, financial/carrier readiness, customer quote/promise, rollout/migrations and production acceptance remain open. TB-185 stays paused.

## Single next task
TB-SHIPPING-ADMIN-SHIPPING-TYPE-ANALYSIS-195, parent TB-SHIPPING-ADMIN-METHOD-MODE-DRAFTS-194: bounded AMSC Analyze only for D2 shipping-type ownership and reuse. Read actual Fulfillment/Shipping/offer/product assignment seams and the older no-Types-of-Service lock, reconcile its exact scope with the user's approved operational-category requirement, recommend one canonical owner and a minimal Contracts-only implementation slice. No production/schema/manifest/skill/frontend changes, no tests/build/DB, no automatic implementation or new global enum. Worker returns source-backed recommendation for architect decision; issue only this task and wait.
