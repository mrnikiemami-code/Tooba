# Architect review — TB-SHIPPING-ADMIN-WEEKLY-SCHEDULE-DRAFTS-193-R1

Date: 2026-10-22
Reviewed source checkpoint: fce7fab3082435eb9fb92092b1b885a73bf98335
Verdict: ACCEPT for TB-193 weekly schedule draft persistence, as corrected by TB-193-R1.
Production functional acceptance: PENDING.

## Independently reviewed
Reviewed the changed concurrency fact and the source diff, not just the Worker PASS. Each participant gates at its first SavingChangesAsync after loading and staging, with two arrivals and each root concurrency ORIGINAL revision asserted to equal 1. The bounded gate does not hold a conflicting row lock. The assertions require exactly one successful outcome and one AuthoringDraftRevisionConflict, fresh persisted revision exactly 2, winner-dependent schedule/rate/currency state and independent raw row counts. The actual losing context must contain no pending tracked changes and re-read the winner state. This closes R193-01; the earlier permissive concurrency claim is superseded, not reused.

The cleared tracker supports the no-pending-write claim; this review does not claim that an additional SaveChanges was executed on the loser. The repaired source is test/evidence only; no production behavior, endpoint, contract, EF mapping, migration or snapshot changed. Existing API, weekly semantics and storage evidence remains applicable by source identity.

## Reported execution versus this review
Worker reports: build with zero errors; five consecutive focused executions; storage 3/3; weekly 20/20; full Shipping suite 609/609, zero failed/skipped on disposable PostgreSQL 17; cleanup completed. Architect reviewed source/evidence remotely and did not re-execute those commands.
The result's phrase "45-minute hard bound" is a reporting error: the R1 task bound was 20 minutes. Its reported 09:15–09:23:30 UTC interval is approximately 8.5 minutes and is within that actual bound.

## Sole next slice
TB-SHIPPING-ADMIN-METHOD-MODE-DRAFTS-194, parent TB-SHIPPING-ADMIN-WEEKLY-SCHEDULE-DRAFTS-193-R1: persist two independent method-root draft selections: freight Prepaid/CollectOnDelivery and calculation Manual/CarrierApi/Free. Reuse the existing freight enum. This is draft authoring only, not runtime pricing, integration or financial activation.

Keep existing rows, currency, geography, translations and weekly schedule intact on mode changes. Free customer shipping eventually means all customer shipping components zero, while underlying costs and an explicitly identified funding party remain accounted for; the selection itself proves neither costs nor funding. CarrierApi selection proves no provider readiness or live quote. No guessed owner/funder, no Promotion threshold or payment/COD side effect.

## Remaining gates
Runtime basket/group amount and count provenance; taxonomy; cutoff and further operational decisions; actual carrier mapping/coordinates/quote integration; free cost/funder accountability; live configured registry and Admin UI integration/acceptance; migration/deployment authority; customer quote activation and final product acceptance remain open. TB-185 stays paused. Baseline architecture certification is historical, not a certification of all subsequent features. Issue one downloadable task and wait; no automatic next implementation.
