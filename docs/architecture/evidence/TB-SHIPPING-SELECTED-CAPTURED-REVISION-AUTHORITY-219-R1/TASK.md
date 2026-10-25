PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-SELECTED-CAPTURED-REVISION-AUTHORITY-219-R1
Parent-Task: TB-SHIPPING-SELECTED-CAPTURED-REVISION-AUTHORITY-219
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: e4e3a1cf7b6fcc0e9ba7e84c77eb1526a49bb101
Mode: BOUNDED_FRESH_POINTER_AND_EXCEPTION_ATOMIC_SELECTION_REPAIR
HARD20 minutes / STOP new implementation12 / final8 focused proof/evidence/commit.
One correction round maximum. Exact defects source-confirmed; no broad discovery/re-audit.

READ219/ARCHITECT-REVIEW.md, TOP AGENTS/recovery and applicable backend skill.
Retain219 schema/selection architecture/Contracts owner resolver. 211–218 closed. No220.

ONLY TWO REPAIRS
1 ShippingSelectedCapturedRevisionDirectory.SelectAsync/ClearAsync read ScopedPointer through tracking
queries. EF identity-map can return a CLEAN STALE pointer instead of current database token/target.
RequireCleanTracker is insufficient. Current same-target/already-cleared early no-ops can therefore
report success from an OLD token after another context advanced the DB; a real current token can
also be falsely rejected.
Before ANY token comparison or no-op decision, obtain the authoritative persisted pointer.
Minimal permitted solution: explicitly reload the ONE clean, owner-scoped pointer on existing context,
preserving its object identity and refreshing only this pointer; handle a disappeared row safely.
Preserve real concurrency token/CAS for actual writes. Do not clear/detach unrelated caller state,
fabricate second context/framework, or disable tracking globally. Dirty caller work must still be
refused FIRST, without refresh/flush/clear. New-pointer creation and exact unique matcher unchanged.

2 Domain ShippingSelectedCapturedRevision.Select/Clear assign SelectedSnapshotId BEFORE calling
NextVersion. At long.MaxValue the version validation throws AFTER target was changed, BEFORE TrySave
cleanup; a later SaveChanges can leak the rejected target/clear without version advance.
Compute/validate next state BEFORE ANY field assignment. Rejection leaves target/version/UpdatedAt
unchanged and tracked entry clean. Keep no-op semantics at current version and stable error code.
No expanded guard/transaction rewrite, API/schema changes or new rules.

EXECUTED REGRESSIONS FIRST (real PostgreSQL, same directory/context)
A. A CLEAN TRACKED pointer v1/oldSnapshot; independent contextB selects newSnapshot v2.
A.Select(oldSnapshot,expected1) must CONFLICT, NOT old-target no-op success.
Control A.Select(oldSnapshot,expected2) succeeds and advances to3 with oldSnapshot (deliberate rollback).
Assert actual stale instance in A existed before B update; don't use AsNoTracking for setup.
Preserve unrelated caller tracked instance identity and values. Refresh of the pointer itself is
intentional/allowed; preserve pointer identity.
B. A CLEAN TRACKED cleared pointer v2; B selects newSnapshot v3.
A.Clear(expected2) must CONFLICT, NOT stale-cleared no-op; current expected3 Clear succeeds v4.
Fresh independent read proves DB target/version; no fabricated result.
C. Real DB fixture sets selection_version=long.MaxValue on a selected pointer (task DB only).
Select a DIFFERENT existing capture at expectedMax and Clear at expectedMax both reject with
selected_capture.invalid; SAME tracked pointer target/version/UpdatedAt unchanged; entry Unchanged,
later SaveChangesAsync returns0, independent DB read unchanged after EACH attempt.
Do not mask failure by context.Dispose/ChangeTracker.Clear/detach or restore inside assertion helper.
No new module/framework/test project. Use existing219 fixture. Execute red first with unchanged production,
then green. One build + directly affected storage class including new facts once after final correction.
No old214/216/218/119/194/wholeShipping/solution/Host/snapshot wildcard replay.

PROVIDER
Same existing ShippingCaptureRelationalTestTarget:
127.0.0.1:15432 / shipping_tb214 / tb214, TOOBA_TB214_POSTGRES.
One task-owned disposable postgres17 from EXISTING local image, verify live identity before DDL,
normal migration chain only task DB, remove container and verify15432 silent afterwards.
No shared5432, no relaxation of isolation, no InMemory substitution for these facts. Do not restart
shared Docker engine/services; unavailable provider is honestly blocked evidence, not loop/reinstall.
No shared migration (214/219 still pending there), no user draft/capture, HTTP/browser, Host/FE restart.

BOUNDS / FINISH
Production only existing219 adapter + Domain pointer (minimal private fresh-load helper allowed);
tests + docs. No Contracts/schema/migration/flags/DI/Order/FE change. Existing target/clear/CAS/no-op/
snapshot immutability and exception/token semantics stay intact.
Record source-confirmed defect versus actual red/green facts; do not relabel worker9 baseline as proving
new cases. Record real elapsed, no timing PASS if exceeded. Do not absorb new features/third defect.
Persist task verbatim and RESULT in evidence/219-R1, append-only correction219 RESULT; update TOP
canonical recovery + AGENTS + new-chat guide, all THREE. Exact paths staged; commit/push/fetch parity,
no gitadd-A/reset/rebase/stash/forcepush or historical post-result.js/Bridge interaction.
STOP_FOR_ARCHITECT, Automatic-Next-Task NONE; no220 envelope.
After this bounded repair closes, next work is actual publication/selected-plan integration, not
another audit/calculator; still NOTLIVE, TB185paused/defaultOFF/PendingAllocation and §29 preserved.