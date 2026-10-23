PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_TASK
Task-ID: TB-SHIPPING-ADMIN-AUTHORING-RATES-WEEK-UI-203-R1
Parent-Task: TB-SHIPPING-ADMIN-AUTHORING-RATES-WEEK-UI-203
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_TIME_BOUND_DISCARD_AND_RATE_READ_PRESENTATION_REPAIR
Expected-Base-SHA: f4f95bc8afada6ac528c124a8951ffa9622709c8
Hard-Timebox-Minutes: 25
Stop-New-Implementation-And-Tests-At-Minute: 18
Automatic-Next-Task: NONE
END_TOOBA_WORKER_TASK

Goal
Repair four concrete defects in delivered203. Keep existing scope/features; no group/destination UI yet.
Read AGENTS/latest recovery,203 ARCHITECT-REVIEW, frontend skill/architecture standard and Shipping rules. Verify expectedbase HEAD==origin/main on safe fast-forward tree; preserve unrelated work. No reset/rebase/stash/force-push or Bridge probe.199/200/201/202 closed, no proof replay.

Per-bound time editing
editor-week currently sets ONE hoursEdited on a day then canonicalTime requires HH:mm for BOTH bounds.
Executed Architect reproduction on unchanged module:
stored start09:30:00.2500000/end18:00:00.5000000; withDayTime(...,1,startTime,10:00)
=> weekIssues=[dayTimeInvalid]; buildWeeklyScheduleEdit day endTime=null.
Fix per-bound provenance/canonical parsing: edit start only ->10:00:00 and exact original end; edit end only preserves original start. Working/closed toggle alone preserves BOTH token texts. No forced editing of the other input, precision truncation or fabricated hours.
When checking ranges compare actual time values including fractions, not string spelling (18:00:00 equals18:00:00.0000000; zero interval remains invalid). Keep valid HH:mm deliberate-edit semantics and existing backend rules, no timezone/DST algorithm.
Regression controls: start-only, end-only, fractional untouched peer, toggle closed/open, partial/reversed/equal pair, genuine changed valid pair. Malformed raw input remains dirty+blocked rather than coerced null and partially submitted.

Confirmed Cancel Edit resets the ENTIRE local document
Current screen enterDestination(view) resets only setForm/controller.edit({}); rates/week persist and formSeed doesn't advance.
Use one small reset/reseed helper or equivalent cohesive operation ACTUALLY called by this UI path:

dirty General+rates+week -> Cancel Edit -> reject keeps exact values/session/mode;
confirm restores ALL three from the SAME confirmed baseline, clears intent/localDirty, view displays persisted tree; switching edit again or editing only General never resurrects discarded rows/week;
preserve retained ids/exact money/time/raw inactive text and original revision;
save/reconcile pending stays blocked; Back destination still leaves only after confirmation;
unknown outcome still follows existing explicit discard+fresh read, latch not silently cleared.
Do not implement an unrelated state framework or reset accepted reducer epochs. Extract a testable reset function if needed; test the same function wired to screen, not an unused helper.

Raw pending timezone before Configure is work
withPendingTimeZone(createWeekForm(null),'Typed/Zone') currently weekDirty=false.
Treat changed pending text as localDirty (reverting empty clean), keep warning/confirmation, and keep weeklySchedule omitted until explicit Configure. Raw-only pending text alone must not cause a no-change PUT.
Do not fabricate configured days on typing; Configure with explicit timezone still creates only local7closed days. If operator edits another valid capability while timezone is pending, don't silently clear pending text on successful save: block Apply with a meaningful local configure-or-reset issue or provide an explicit per-section discard, using the minimal consistent current form model. No new server contract.

Read-only rate conditions must stay truthful
New authoring-rates-editor read view renders only weight; count/merchandise-only rows now say noCondition, losing202 behavior.
Render all three actual axes in view (weight/count/merchandise), combinations and null/unbounded bounds, using current messages and exact decimal token text. No duplicated geography, policy/price calculation, GUID labels or invented range. Keep locale/RTL/responsive primitives.
Add focused presentation-model test for count-only, money-only and combined rows, plus noCondition control; wire the function into the actual component. No mounted/browser claim from a pure formatter.

Verification
Write focused regressions first against unchanged203 where possible, then fix.
Use existing Node pattern; no framework/package install. Run new focused regression file and203's7facts after final edit, then typecheck and touched import/feature/test-discovery guards once. Do not re-run whole48 foundation/backend/Host/solution/build/source-size sweeps; existing four source-size failures untouched and no baseline edits.
For confirmation/lifecycle tests, exercise actual extracted transition functions and inspect real screen wiring. State source vs pure-test vs mounted coverage explicitly; no browser/mount harness required or claimed.
At most2 bounded harness corrections; stop honest PARTIAL on broader defect, no confidence loop or deadline extension.

Scope/bounds
Only existing admin-shipping-authoring model/week/intent/related reset helper, affected screen/rate/week presentation/messages/tests/public exports if needed. No backend/DTO/API/transport/DI/DB/schema/migration/Host/legacy dialog/grid/shared library/design-system/sharedCSS/package/config/skill/AGENTS/baseline change.
No new group/geography editing, whole-method create, activation/cutoff/carrier/funding/quote, global navigation framework, autosave/retry/post-saveGET.
No server/browser/login/network/live-write probe, container, install/download, sharedports3000/3419/5088/5432/Redis/RabbitMQ/process or cache/bin/obj/.next mutation.
Hard25min fromclaim, stop newcoding/tests18; final7 only evidence/diff/scopedcommit/push/fetchparity/result.
Persist received task, own RESULT, append-only203 RESULT correction and one recovery pointer. Report exact regressions/counts/files/time/deviations and visual limits.
Result BEGIN_TOOBA_WORKER_RESULT includes task/parent/mode/base/code/evidence/parity; Production-Functional-Acceptance:PENDING; Schema-Data-Flag-Changes:NONE; Visual:NOTRUN unless actually observed; Stop-State:STOP_FOR_ARCHITECT; automaticNextImplementationTask:NONE; Automatic-Next-Task:NONE.
Only this task; wait review before204 or any next envelope.