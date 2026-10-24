PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_TASK
Task-ID: TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205-R3
Parent-Task: TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205-R2
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_MOUNTED_DISCARD_CONFLICT_AND_UNKNOWN_WORK_PROTECTION
Expected-Base-SHA: 61043bb5d699a55145ec18fb8d03ce3bd82e2f28
Hard-Timebox-Minutes: 15
Stop-New-Interaction-And-Tests-At-Minute: 10
Maximum-Runtime-Setup-Minutes: 3
Automatic-Next-Task: NONE
END_TOOBA_WORKER_TASK

Outcome
Finish ONLY the3 critical mounted work-protection controls not executed in205-R2. Successful save/duplicate/newids/FAENcaptures are accepted and MUST NOT be repeated. No full geography matrix, cosmetic work or new feature.
Read AGENTS/latest recovery,205-R2 ARCHITECT-REVIEW and mandatory frontend skill. Verify expectedbase HEAD==origin/main safe tree; preserve unrelated work. No reset/rebase/stash/forcepush/Bridge probe.

Known runtime, setup<=3min
Reuse the installed Chrome/Node/Next and205-R2 CDP harness, finite timeouts, BOM-safe command helper, scoped whitelist and lawful initial fixture. Task-owned frontend127.0.0.1:3517 +fresh browserprofile/CDP9555, installed dependencies/junction and isolated output, TOOBA_HOST_ORIGIN=http://127.0.0.1:1. Proveportsfree first. No shared:3000/.next/tsconfig/next-env/process/userprofile mutation.
Install interception BEFORE navigation to actual /admin/shipping-services/authoring/<IDS.draft>?mode=edit.
All /v1 synthetic fail-closed; real merchantwrites forbidden. If runtime cannot be ready in3min, STOP BLOCKED exactreason; no setup investigation.
Only minimal harness error-plan commands permitted: fulfill canonical409 for a HELD PUT or fail that HELD request as transport error. No success-response/fixture redesign. Error-path request edits need not equal the R2 SUCCESS scenario; do not return a fake200 for mismatch. No prepared success fixture change.

Execute these3 controls with real clicks/typing on production components

Invalid + reject/confirm Cancel Edit
Start fresh initial revision7. Make local General text edit, edit one existing rate amount and one week bound; then blank the stored name.
Assert visible name-required issue, Apply disabled and dirty indication. Cancel Edit ->confirmation; Reject ->exact blank name and pending rate/week retained, no PUT.
Now explicitly Confirm Cancel Edit. Assert view restores original General name, rate amount, week, group count/ids; re-enter Edit has no discarded residue and Apply remains disabled/clean. No PUT in this case.
Use actual Dialog/button actions, not controller/helpers/React state writes.

2.409 preserves work, no retry
Fresh initial document, edit a valid name. Apply->heldPUT then fulfill409 using canonical shipping.authoring.draft_revision_conflict envelope grounded in existing adapter/error contract (not invented RevisionMismatch semantics).
Assert conflict shown, name/local work still present and dirty, server revision unchanged. Repeat/delay UI observation only; request count remains1 for this attempt, no automatic retry or post-save GET. Don't click another intentional Apply during this control.

TransportUnknown -> explicit reconcile, zero replay
Fresh initial document (reset browser trial deliberately), edit valid name; Apply->heldPUT then Fetch.failRequest as actual transport failure.
Assert unknown message +explicit read action, edits kept, Apply blocked. Additional local edit must not clear the latch or send another PUT.
Click explicit reconcile/read-server once; fulfill INITIAL valid read document (this fixture branch models no committed write, not a guessed real outcome). Assert GET delta only from that explicit action, uncertainty clears for operator review, typed work retained, no automatic PUT/replay. Stop before another deliberate save.
Track initial StrictMode GETs separately; assert per-control PUT/GET deltas, not global totals confused across reloads.

Evidence/finish
Use existing CDP commands/testids; no new framework/mock engine/Node proof suite/typecheck/build. One harness correction MAX.
Record actual browser DOM +request counters and first console/fixture/product failure. If a real product defect appears, reproduce once and STOP that control with exact evidence; NO production repair/loop. Screenshots optional only to document the3 states, no six-locale repeat.
Do not claim full A-E/full geography/group move matrix from these3 checks. These broader combinations remain pure-model tested, not mounted-certified. No new tasks to fill those matrices inside this box.
15min HARD fromclaim; stop newinteractions10; final5cleanup/evidence/diff/scopedcommit/push/fetchparity/result. Missing cases explicit PARTIAL, no deadline extension.
Allowed evidence/scripts/logs/synthetic screenshots under205-R3, received task, own RESULT and one recovery pointer ONLY. No production/page/model/api/route/backend/Host/DB/schema/flag/sharedlibrary/CSS/package/config/skill/AGENTS/baseline changes.
No install/download/login/credential/livewriteprobe/sharedport/process restart, cache/bin/obj/.next purge or historical post-result.js execution.
Stop exact task-ownedruntime/browser/harness and verify3517/9555 free, shared:3000 untouched.
Result exact3case outcomes/request deltas/sourceSHA/time/deviations/cleanup; Production-Functional-Acceptance:PENDING; Shared-Database-Data-Flag-Changes:NONE; Visual:existingR2evidence reused, no newVisualACCEPT; Stop-State:STOP_FOR_ARCHITECT; automaticNextImplementationTask:NONE; Automatic-Next-Task:NONE.
After result: Architect review and handoff of the existing-draft authoring slice for USER TRIAL with real-data/release/whole-method-create/quote limits. No automatic next task.
