PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_TASK
Task-ID: TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204-R1
Parent-Task: TB-SHIPPING-ADMIN-AUTHORING-GROUPS-DESTINATIONS-UI-204
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_DESTINATION_PICKER_REQUEST_AND_CHAIN_CORRECTNESS_REPAIR
Expected-Base-SHA: 5eaf479417a1d771f6c428f242e901e35b2d76f3
Hard-Timebox-Minutes: 30
Stop-New-Implementation-And-Tests-At-Minute: 22
Automatic-Next-Task: NONE
END_TOOBA_WORKER_TASK

Outcome
Repair the ACTUAL204 picker workflow. Preserve group/title/rates/week/General collection functionality and ONE PUT. No new capability or backend change.
Read AGENTS/latest recovery,204 ARCHITECT-REVIEW, frontend skill/architecture standard and Shipping rules. Fetch/verify expectedbase HEAD==origin/main safe fast-forward; preserve unrelated work. No reset/rebase/stash/force-push/Bridge probe.199/200/201/202/203 closed; no replay.

Request orchestration that actually works
Current four useEffects increment one epoch.current. Selecting country launches area read (epochN), then city read (N+1), so the legitimate area response is ALWAYS discarded. City reads capture old/empty lists.areas, intentionally exclude it from dependencies, and pass undefined known parents when empty; decoder then accepts any city parent.
Replace this with a small cohesive coordinator/guard used by the REAL component:
independent branch request identities AND whole parent/language/session identity, or a sequential dependency flow; do not let sibling requests invalidate correct sibling results.
country-wide city decode requires a successfully verified area set for THAT country/language. Load/await that prerequisite before trusting cities, or retain/revalidate pending city responses only after it succeeds. A successful empty area set is a KNOWN EMPTY set, never undefined/permissive.
prerequisite failure leaves cities unavailable/untrusted, no catch-to-success[] or skipped parent check; non-null city parent outside verified set is rejected.
filtered city read rejects parent inconsistent with the actual area filter. Null-admin city is legitimate only on lawful country-wide branch, not a response contradicting an explicit admin filter.
immediately clear/invalidate obsolete dependent lists/choice evidence on parent change. Do not leave old country's area/city/service options selectable while awaiting new rows.
country A->B, area A->B, city A->B, language change, picker unmount/disposal: old completions cannot mutate current lists/failure/loading or re-enable Add. Ancestor changes invalidate descendants but sibling results for the SAME context coexist.
opening/reading only selected branch, no world scan/global cache/new endpoint. Loading/error statuses truthful per active work.
Write executable DEFERRED-PROMISE tests against the SAME coordinator wired to component, not regex /epoch/ as race evidence. No React framework required/installed.
Active verified terminal path
Inactive rows currently selectable and Add/choiceAncestry never checks IsActive.
show inactive options truthfully disabled, and enforce active/chain coherence again in the actual Add guard/helper, including required ancestors. A forged programmatic change must not bypass the guard.
retain stored inactive/unresolved draft selections visibly; this repair must NOT silently remove them.
country-wide city derives its actual nullable parent from the VERIFIED row; no current-highlighted-area substitution. Verify non-null parent membership and activity using actual loaded areas.
service-area parent must equal selected verified city; country/admin parent identities and kinds remain exact.
choiceAncestry currently picks choice.city.parentId even when terminal=AdministrativeArea. Use the chosen terminal's OWN path, ignoring deeper irrelevant selections.
Executed reproduction: country+admin +null-admin city, switch level AdministrativeArea -> terminal=admin but ancestry=null. Fix this and city-under-different-admin control; terminal id MUST equal serialized tuple's terminal.
Regression inactive country/admin/city/service and wrong service parent reject; active null-admin city+service succeed; switch Country/Admin/City/Service creates correct tuple.
Clear cascade and label behavior
Area/city/service placeholder currently returns previous on null. Implement actual clear functions:
clear country -> all dependents empty; clear area -> area/city/service cleared, allows country-wide cities; clear city -> city/service cleared; clear service -> only service cleared. Clearing parent invalidates old requests/lists as above. Existing group chips unaffected until deliberate Add/remove.
OptionSelect currently uses option.code when label blank. Replace with localized unavailable name, never code or GUID fallback; no fabricated label. Existing server-side label fallback is not reimplemented or expanded.

Verification/reporting
Regressions first where feasible, then focused new coordinator/chain tests and204's8facts after repair. Include an executed two-sibling area/city result scenario that failed under one epoch, missing/empty/failing parent prerequisite, out-of-order contexts, clear+late completion and inactive Add. Keep actual component wiring inspected and honestly distinguish it from a mounted test. Source regex alone is NOT the asynchronous proof.
Run typecheck and touched feature/import/test-discovery/flat-folder guards once; commands<=120s. No whole55/root48/nested foundation sweep, source-size/backend/Host/solution/build/discovery/browser suite or package install.
Correct204 evidence append-only:

actual route is /admin/shipping-services/authoring/[draftId], not /drafts/{id}; no route source change.
root *.test.ts55 did not include nested adapter/session files; list exact matched files and what was NOT run rather than falsely claiming full foundation. Do not rerun omitted suites to repair wording.
original late-epoch statement was source-pattern evidence, not an executed race test; supersede with new actual result.
Two bounded harness corrections max; stop PARTIAL with exact blocker instead of a confidence loop. No expectation weakening or unrelated scope absorption.

Allowed
Existing feature's picker, its small model/coordinator/strict Geography adapter, minimal related messages and tests/public exports. Screen/groups props only if necessary to scope picker lifecycle; preserve group/rate/week composition, discard and server-owned authority. No backend/Contracts/DTO/API route/transport-auth replacement/DI/schema/DB/Host/legacy dialog/grid/shared library/design-system/sharedCSS/package/config/skill/AGENTS/baseline change.
No new group functionality, whole-method create, activation/readiness/cutoff/carrier/funding/quote, autosave/retry/post-save GET.
No server/browser/login/network/live write probe, container/migration/download/install, sharedports3000/3419/5088/5432/Redis/RabbitMQ/process/cache/bin/obj/.next mutation.
Hard30minutes fromclaim; stop newimplementation/tests22; last8 only diff/evidence/scopedcommit/push/fetchparity/result.
Persist received task verbatim, own RESULT, append-only204 correction and one recovery pointer. Exact actualcommands/files/time/deviations/limits. No visual/mounted/certification acceptance from pure tests.
Result BEGIN_TOOBA_WORKER_RESULT includes IDs/base/code/evidence/parity; Production-Functional-Acceptance:PENDING; Schema-Data-Flag-Changes:NONE; Visual:NOTRUN unless actually observed; Stop-State:STOP_FOR_ARCHITECT; automaticNextImplementationTask:NONE; Automatic-Next-Task:NONE.
Only this task. No205/visual envelope until Architect review.