PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_TASK
Task-ID: TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200-R2
Parent-Task: TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_SAME_DRAFT_RELOAD_AND_UNCERTAIN_WORK_SELECTOR_REPAIR
Expected-Base-SHA: 03271c75af1f25ca0faf598ee93b4570deb2dce6
Hard-Timebox-Minutes: 10
Stop-New-Coding-And-Tests-At-Minute: 6
Automatic-Next-Task: NONE
END_TOOBA_WORKER_TASK

Only repair the TWO local session defects in200-R1 ARCHITECT-REVIEW.md. All original R1 fixes remain retained. Do not replay previous wire/builder/epoch proof or add UI work. Follow AGENTS/recovery/frontend skill/standard; fetch HEAD==origin/main, fast-forward only, preserve unrelated work/stashes.

Allowed production file ONLY:
src/frontend/features/admin-shipping-authoring/session/editor-session.ts
Allowed tests: current session test file (or one compact new session regression file in same feature).
Docs: received task verbatim; own evidence RESULT.md; append-only R1 result correction; one recovery worker pointer. No AGENTS/skill/config/baseline/decoder/builder/API/index/backend/UI/route/preview/shared transport/schema changes.

Same-draft loadStarted guard:
Current branch only returns a discard decision for a DIFFERENT draft; same draft falls through when state.unsaved or save===saving. It starts a reload; loadSucceeded clears edits. During saving it increments epoch and makes the valid save response stale.
Restore SAME state object for same-draft loadStarted while dirty or saving. Keep different-draft discard decision and explicit discard/reconcile behavior. No new API/actions/framework needed.
Write tests first on unchanged base:
loadedA -> edited(nonempty) -> sameA loadStarted => SAME state/epoch/baseline/edits; unsolicited loadSucceeded must not clear work;
loadedA -> edited -> saveStarted -> sameA loadStarted => SAME saving state/epoch/requestId; legitimate current saveSucceeded still accepted.
explicit confirmed discard then fresh load remains possible; explicit unknown-commit reconciliation remains valid (reuse current narrow case, don't rerun all25).
Public warning selector:
hasUnsavedWork(state) currently returns only unsaved. After saveFailed(outcomeUnknown) then edited({}), commitUncertain remains true but public selector=false although closeRequested requires confirmation. Include uncertain commit and in-flight save in this selector's protection. Keep ordinary clean loaded state=false; uncertainty must not disappear merely because editable values empty.
Test unknown->edited({}) => hasUnsavedWork true, Apply stillblockeduncertain, closeRequested demandsdiscard; saving=>true; clean=>false. No fabricated success, auto retry or clearing uncertainty.

Execute new exact regressions pre-fix and post-fix, report observed counts. One focused session file run after fix; no feature25 confidence repeat, fulltypecheck/build/discovery/architecture/backend tests/browser/server/DB/container/network probe. This change is local reducer/boolean typing; no broad validation needed. Commands<=60s. At mostONE harness-only correction, otherwise precisePARTIAL.
No sharedapps/ports/DB/cache/bin/obj/.next/process/credential operations, install/download/reset/rebase/stash/force-push. No task201 emitted.

10min HARD: stop new coding/tests at6, reserve4 for diff/evidence/scopedcommit/push/fetchparity/result. Do not prolong for cosmetic evidence. Record actualUTC/time/corrections honestly; oldR1~27>25 processdeviation stayshistory, not a new proof task.

Result BEGIN_TOOBA_WORKER_RESULT with task/parent/mode/base/code/evidenceSHA/parity; pre/post actual regressions; same-state dirty/saving guard and warning selector assertions; exact changedfiles; public API unchanged; focusedtestcount; UTC/time; Production-Functional-Acceptance:PENDING; Schema-Data-Flag-Changes:NONE; Stop-State:STOP_FOR_ARCHITECT; automaticNextImplementationTask:NONE; Automatic-Next-Task:NONE.