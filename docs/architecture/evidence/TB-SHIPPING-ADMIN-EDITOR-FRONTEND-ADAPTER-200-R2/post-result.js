/* Posts the TB200-R2 worker result to the Bridge results API (complete contract only). */
const content = `PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200-R2
Parent-Task: TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200-R1
Mode: BOUNDED_SAME_DRAFT_RELOAD_AND_UNCERTAIN_WORK_SELECTOR_REPAIR
Base-SHA: 03271c75af1f25ca0faf598ee93b4570deb2dce6
Code-SHA: __CODE_SHA__
Evidence: docs/architecture/evidence/TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200-R2/RESULT.md
Evidence-SHA: __EVIDENCE_SHA__
Parity: HEAD == origin/main == __CODE_SHA__
Stop-State: STOP_FOR_ARCHITECT
Production-Functional-Acceptance: PENDING
Schema-Data-Flag-Changes: NONE
automaticNextImplementationTask: NONE
Automatic-Next-Task: NONE

PRE-FIX ACTUAL REGRESSION (unchanged R1 source, new file written and executed first)
1 passed / 3 failed.
- R2-01 dirty: loadedA -> edited -> sameA loadStarted returned epoch 2, load "loading" (expected same object with
  epoch 1, load "loaded"), so the pending success would clear edits.
- R2-01 saving: loadedA -> edited -> saveStarted(epoch 1, requestId 4) -> sameA loadStarted produced epoch 2,
  load "loading", saveRequestId 0 while save stayed "saving" (valid completion would be rejected as stale).
- R2-02 selector: saveFailed(outcomeUnknown) -> edited({}) gave unsaved false, commitUncertain true, yet
  hasUnsavedWork returned false while closeRequested demanded discard.
- The explicit discard -> fresh load and explicit unknown reconciliation case passed pre-fix (1 pass), confirming
  those paths were already correct and remained untouched.

REPAIR (session/editor-session.ts only)
loadStarted, same-draft branch: when state.unsaved or state.save === "saving" and the target draft is the CURRENT
draft, the reducer now returns the SAME state object. Different-draft targets keep the existing discardUnsaved +
pendingSwitchTo decision. The latched-unknown guard and the explicit discard/reconcile exits are unchanged.
hasUnsavedWork(state) returns state.unsaved || state.commitUncertain || state.save === "saving"; a clean loaded
session still returns false. No new actions, no auto retry, no fabricated success, no clearing of uncertainty.

POST-FIX COUNTS
- R2 regressions alone: 4 passed / 0 failed.
- Final focused session run (new R2 file + existing session file): 10 passed / 0 failed.
Asserted: same-draft dirty reload returns the identical object (epoch/baseline/edits preserved) and an unsolicited
loadSucceeded cannot clear work; same-draft reload during save returns the identical saving state with requestId 4
and the legitimate saveSucceeded is still accepted; explicit discard then fresh load still works; explicit
reconcileStarted/reconcileSucceeded still clears the latch and installs the confirmed document; hasUnsavedWork true
for in-flight save, true for latched unknown, true after edited({}) with Apply blocked uncertain and closeRequested
demanding discard, true after discard until a fresh read, false for clean loaded.

FILES / PUBLIC API
Modified: src/frontend/features/admin-shipping-authoring/session/editor-session.ts (+10/-2).
Added test: session/editor-session-200-r2.test.ts. Docs: received task verbatim, R2 RESULT.md, append-only R1 RESULT
correction, one recovery pointer. EditorSessionState, actions and selector signatures are unchanged; only the
selector's result broadened for unresolved work. No decoder/builder/API/index/backend/UI/route/preview/schema/auth/
shared-transport change. All four retained R1 repairs were not replayed or re-proved.

NOT RUN
No tsc, build, discovery, architecture guard, backend test, browser, server, DB, container or network probe: the
change is a local reducer branch plus a boolean selector and the task forbade a broad validation loop. One
pre-existing off-scope working-tree modification (TB-...-200-R1/post-result.js) was left untouched and uncommitted.
No shared app/port/DB/cache/bin/obj/.next/process/credential operation; no install, download, reset, rebase, stash or
force-push. No task201 emitted. The historical R1 ~27min > 25min deviation stays recorded history, not re-proved.

TIME / CORRECTIONS
Claimed 2026-10-22T23:58Z, result 2026-10-23T00:03Z, elapsed about 5 minutes of a 10-minute box; no harness
corrections were needed beyond the one deliberate test-fixture simplification inside the new file while it was being
authored, and no production code was changed before the regressions failed.

Worker PASS != Architect ACCEPT.
END_TOOBA_WORKER_RESULT`;

fetch("http://127.0.0.1:17321/api/results", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({ channelId: "tooba-main", taskId: "TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200-R2", content }),
})
  .then(async (response) => {
    console.log(`RESULT_STATUS=${response.status} ${(await response.text()).slice(0, 300)}`);
    if (!response.ok) process.exitCode = 1;
  })
  .catch((error) => {
    console.log(`RESULT_FAILED=${String(error)}`);
    process.exitCode = 1;
  });
