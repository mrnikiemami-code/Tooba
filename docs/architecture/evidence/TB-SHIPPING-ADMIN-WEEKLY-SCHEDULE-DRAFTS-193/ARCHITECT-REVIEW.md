# Architect review — TB-SHIPPING-ADMIN-WEEKLY-SCHEDULE-DRAFTS-193
Date: 2026-10-22 (Asia/Tehran)
Reviewed HEAD / code/evidence: 87d431cfe78b6c1a2af6ac7c55ccc8e8373587dd
Disposition: PARTIAL_PENDING_DETERMINISTIC_CONCURRENCY_PROOF — not Architect ACCEPT.

## Reviewed positive scope
Source shows one root-owned weekly draft schedule, explicit unconfigured/configured-all-closed distinction, seven ISO weekdays, remembered closed-day hours, server timezone validation extracted with legacy parity, canonical authorized GET/PUT and typed storage. Endpoint facts and raw PostgreSQL constraints/roundtrip cover substantial required behavior. Worker reports 609/609, including 20 new schedule cases and 74 legacy schedule tests; Architect did not execute those tests here.

## R193-01 — race evidence contradicts the actual test
In ShippingAuthoringWeeklyScheduleStorageTests.Concurrent_schedule_and_rate_writes_leave_no_hybrid_state_and_report_a_canonical_conflict:
- TaskCompletionSource gate is awaited BEFORE either Replace... directory method reads the root. Releasing it starts two operations; it does not prove two reads of revision 1 before either writes.
- There is no bounded post-read/pre-write barrier or original-token assertion.
- Assert.Contains(results, result => result.IsSuccess) requires only one-or-more successes. It never asserts exactly one winner and exactly one loser.
- The loser loop accepts an empty loser set. Assert.Equal(results.Count(success)+1, stored.Revision) explicitly permits BOTH operations succeeding and revision 3.
- Final checks conditionally validate whichever capabilities exist, rather than assert exactly the actual winner's state and absence of the loser's changes.
- Both contexts are disposed inside the participant functions; the actual loser tracker is never inspected.

Thus the reported exact winner/loser and safe-loser-tracker proof is not established by this fact. This is a deficient test, not evidence that production has a concurrency flaw. Replace the weak fact with the already-established real SavingChanges pre-write barrier pattern and strict winner/loser state assertions.

## Next bounded task / retained gates
Only TB-SHIPPING-ADMIN-WEEKLY-SCHEDULE-DRAFTS-193-R1, parent TB-193: one test repair plus evidence correction, no executable production/schema/API change. If it reveals a production failure, report PARTIAL with exact proof rather than broaden scope.
Keep original 609/609 as reported history and append a correction to its concurrency claim. Existing schedule API/storage/legacy evidence can be reused by source identity. No TB-194 yet.
Owner/regions, corrected rates and multilingual metadata acceptances remain. Runtime merchandise valuation/measurement, live merchant registry/browser integration, cutoff/holidays/capacity/customer promise and full product/activation remain open. TB-185 stays paused.
