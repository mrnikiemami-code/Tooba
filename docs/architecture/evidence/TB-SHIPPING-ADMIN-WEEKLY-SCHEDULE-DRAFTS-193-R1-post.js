const base = '68d93e851bcb296bf12222c886d32696d1d4bb23';
const code = 'fce7fab3082435eb9fb92092b1b885a73bf98335';

const content = `PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-WEEKLY-SCHEDULE-DRAFTS-193-R1
Parent-Task: TB-SHIPPING-ADMIN-WEEKLY-SCHEDULE-DRAFTS-193
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_STRICT_SCHEDULE_RATE_CONCURRENCY_PROOF_REPAIR
Bridge-Task-Row: 0da9377f-295c-4424-b89a-462738654679
Status: PASS
Expected-Base-SHA: ${base}
Base-SHA: ${base}
Code-SHA: ${code}
Evidence-SHA: ${code}
HEAD-Origin-Parity: HEAD == origin/main == ${code}; no modified tracked files remain (only pre-existing unrelated untracked evidence files).
R193-01-Barrier-After-Read: PASS - the race no longer gates before either read. Each participant is intercepted at its FIRST SavingChangesAsync, i.e. after it loaded the draft root with revision 1 and staged its rows and before either wrote; a bounded two-arrival gate (20 s timeout) releases both. A bare open transaction holds no row lock, so nobody waits while holding a conflicting lock.
R193-01-Original-Token-Assertion: PASS - the interceptor records the ChangeTracker Entry<ShippingAuthoringDraft>.OriginalValues[Revision]; the fact asserts gate.Arrivals == 2, scheduleInterceptor.OriginalRevision == 1 and rateInterceptor.OriginalRevision == 1, exactly the predicate the later UPDATE uses.
R193-01-Exactly-One-Winner-One-Loser: PASS - Assert.Single(outcomes, !IsFailure) plus Assert.Single(outcomes, IsFailure); the loser's single error is AuthoringDraftRevisionConflict. A both-succeed outcome now fails the fact, and an empty loser set is impossible.
R193-01-Revision-Advanced-Once: PASS - the verification context reads revision EXACTLY 2 (previously "successes + 1", which tolerated revision 3).
R193-01-Winner-State-And-Loser-Absence: PASS - winner-conditional assertions in both directions: schedule-winner -> header with Asia/Tehran and seven rows, Saturday 09:00-13:00 working, other six closed, AND empty rate rows with CurrencyCode null; rate-winner -> one 12.50 row with USD AND WeeklySchedule null. Independently of the projection, raw counts assert authoring_weekly_schedule_days = 7|0 and authoring_rate_rows = 0|1 matching the winner, so no hybrid row can pass.
R193-01-Actual-Loser-Tracker: PASS - the contexts are no longer disposed inside the participants; the fact selects the ACTUAL losing DbContext via loser.Index, asserts it has no ChangeTracker entry in a non-Unchanged state (a later SaveChanges on it can publish nothing), and re-reads it to show the winner's revision 2 and winner capability shape.
Determinism: PASS - the repaired fact ran five consecutive focused executions on the live disposable database (5/5, ~0.86 s each) and inside the whole suite; no flake, no timeout, no skipped case.
Scope-Of-Repair: TEST_AND_EVIDENCE_ONLY - one test fact replaced plus the R193-01 evidence correction; no production executable behavior, contract, error code, validation, endpoint, EF mapping, migration, snapshot or API change; migration/designer files byte-identical to TB-193.
Preserved-Evidence: PASS - the original 609/609 report and the endpoint/constraint/roundtrip facts remain as history by source identity; the concurrency claim is explicitly superseded by the corrected section 6/6.1 so no overstated proof remains.
Focused-Commands-And-Test-Counts: dotnet build Shipping.Tests 0 errors; repaired fact 5/5 consecutive focused runs; ShippingAuthoringWeeklyScheduleStorageTests 3/3; ShippingAuthoringWeeklySchedule 20/20; ONE whole Tooba.Shipping.Tests 609/609 passed, 0 failed, 0 skipped, 18 s on the isolated 127.0.0.1:55432 PostgreSQL 17 container.
Changed-Files: Tests/Authoring/ShippingAuthoringWeeklyScheduleStorageTests.cs (fact replaced, SaveChangesInterceptor barrier + bounded gate added, interceptor-aware context factory); docs/architecture/evidence/TB-SHIPPING-ADMIN-WEEKLY-SCHEDULE-DRAFTS-193/RESULT.md (R193-01 correction + section 6.1); docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md (R1 pointer); docs/ai/tasks/TB-SHIPPING-ADMIN-WEEKLY-SCHEDULE-DRAFTS-193-R1.task.md (received artifact).
Claim-UTC: 2026-10-22T09:15:00Z
Result-UTC: 2026-10-22T09:23:30Z
Elapsed-Minutes: 8
Hard-Deadline-Compliance: PASS - repair, live PostgreSQL proof, cleanup, commit, push and report well inside the 45-minute hard bound.
Out-Of-Scope-Commands: NONE
Shared-Database-Data-Flag-Changes: NONE
Isolation-And-Cleanup: fresh task-owned postgres:17-alpine container removed and port 55432 verified closed; no shared :3000/:3419/:5088/:5432, tooba_alpha, Redis/RabbitMQ or shared bin/obj/.next/cache touched; no reset/rebase/stash/force-push; no dummy or probe POST to Bridge /api/results.
Runtime-Merchandise-Basis-And-Measurement-Gate: OPEN_NOT_IMPLEMENTED
Production-Functional-Acceptance: PENDING
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
Automatic-Next-Task: NONE
END_TOOBA_WORKER_RESULT`;

(async () => {
  const response = await fetch('http://127.0.0.1:17321/api/results', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ channelId: 'tooba-main', taskId: 'TB-SHIPPING-ADMIN-WEEKLY-SCHEDULE-DRAFTS-193-R1', content }),
  });
  console.log('RESULT', response.status, (await response.text()).slice(0, 200));
  if (response.status >= 200 && response.status < 300) {
    const done = await fetch('http://127.0.0.1:17321/api/tasks/0da9377f-295c-4424-b89a-462738654679/complete', { method: 'POST' });
    console.log('COMPLETE', done.status, (await done.text()).slice(0, 200));
  }
})();
