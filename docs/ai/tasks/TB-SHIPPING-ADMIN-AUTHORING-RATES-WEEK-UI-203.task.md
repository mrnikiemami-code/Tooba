PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_TASK
Task-ID: TB-SHIPPING-ADMIN-AUTHORING-RATES-WEEK-UI-203
Parent-Task: TB-SHIPPING-ADMIN-AUTHORING-GENERAL-UI-202-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_EXISTING_GROUP_RATES_AND_METHOD_WEEK_EDITOR_UI
Expected-Base-SHA: 44a220dea77c87e3d8c1a80e07b22af4e2846e45
Hard-Timebox-Minutes: 35
Stop-New-Implementation-And-Tests-At-Minute: 25
Automatic-Next-Task: NONE
END_TOOBA_WORKER_TASK

Deliver visible functionality
Enable rate-row editing under EXISTING draft groups and the ONE method-root weekly schedule in the current connected authoring screen. Keep existing geography/group topology READ-ONLY. One deliberate Apply saves General + rate + week intent together via the accepted editor-document PUT. No new backend or transport.

Recover
Read AGENTS, latest canonical Shipping recovery,202-R1 ARCHITECT-REVIEW, frontend feature skill/FRONTEND-ARCHITECTURE-STANDARD, Shipping rules and accepted186-R2 visual reference. Fetch exactbase and verify HEAD==origin/main, safe fast-forward only. Preserve unrelated work. Backend199/readbridge201/frontend200/general202 are accepted; no historical proof replay.
Before fields/validation, inspect actual ShippingAuthoringEditorDocumentContracts, existing rate/schedule validators and Domain PreflightGroupRateRows/PreflightWeeklySchedule. Reuse their units, requiredness, interval boundaries and limits; do not infer business rules from preview fixtures. Server remains authoritative. No backend edits.

Existing-group conditional rate rows
In edit mode render existing rate fields and controls to add/remove rows WITHIN each existing group:
optional min/max weight grams, item count and merchandise amount condition bounds;
required freight/packaging amounts and min/max delivery days as defined by current contract.
Preserve each retained group's id/order/areas/currency and every untouched row id/value. New row gets a client-local key and null rateRowId; never invent server ids. Deleting a row is explicit local intent; deleting the last row produces rows:[] for that group only. No group create/delete/reorder or geography editing here.
Use the existing exact DecimalText/RawNumber pipeline: decimal money stays token text (0.50,1850000.00), never Number/parseFloat/toFixed rounding; integer fields accept only safe integers per existing contract. Blank optional bound becomes null; required blank and malformed/negative/out-of-range input remain visible dirty invalid work, no zero default or silent clamp. Avoid input type=number transformations of money. Preserve untouched text exactly. No price algorithm, currency selector, weight guessing or duplication of geography under rows.
Show real currency from the stored binding, same geography once per group, no raw GUID labels. Type/calculation Free does not clear or zero rows. Localize labels, field errors and row add/remove actions with RTL/LTR and responsive wrapping using existing design-system primitives.
Do not duplicate server pricing/overlap policy; implement enough shape/requiredness feedback to prevent bad serialization, and retain canonical server errors.

ONE method-level week
Make the existing root weekly-schedule section editable once, shared by all groups. Do not copy it into groups/rows/languages.
Configured schedule initializes actual timezone +7 ISO weekdays (1Monday...7Sunday), independent working flag and stored remembered hours.
Unconfigured stays explicit unconfigured until the operator deliberately clicks Configure. Only then initialize local7closed-day inputs with a timezone the operator explicitly supplies; no guessed browser timezone/default working hours or fabricated persisted configuration.
Allow one same-day start/end pair per day and timezone text validated through existing server contract, no new timezone registry/backend. Work/closed toggles retain remembered hours; opening requires a valid explicit pair. Pair presence rules/reversed intervals match backend; no overnight/cutoff/DST/customer-promise invention. Blank invalid working pair blocks Apply and remains dirty; optional untouched wrapper omitted. No clearing of configured schedule (contract has no clear operation); local reset to baseline is allowed. A configured all-closed week is distinct from unconfigured.
Keep unedited TimeOnly tokens, including seconds/fractions, intact; a UI time input must not silently truncate precision or dirty an untouched day. Convert a deliberate HH:mm edit to lawful canonical TimeText explicitly, retaining stored precision where untouched.

Integrate intents without losing another tab
General pushEdits currently replaces the entire controller edit intent; extend local form composition narrowly so changing General after a rate/week edit or vice versa retains all pending capabilities. Build one typed EditorEdits from confirmed baseline + local General/rates/week intent, then controller.edit once. ONE session authority; no global store/localStorage/autosave.
Reuse202-R1 localDirty/formSeed/exit flow. Raw invalid rate/time/number input is dirty even when no valid wire patch exists. Keep values/issues across tabs, content-language switches,403/409/dependency errors and explicit unknown reconciliation. No clean effect may reset unfinished rows.
Adopt actual saved server ids/revision on success; fresh form seed resets only after accepted load/save/discard. A same-revision no-op is allowed. Restore baseline on explicit confirmed discard only. Keep read-only/view mode and saving/reconciling controls inert as currently accepted.
Use the existing baseline-aware builder and validated201 ancestry index for ALL retained areas; unresolved/missing facts block Apply rather than dropping a group.
One Apply -> exactly one PUT with current expectedRevision containing all intended changes and the full retained group tree. No per-capability requests, post-save reload or retry. Keep synchronous duplicate gate, conflict preservation and sticky unknown-outcome recovery.
No duplicated schedule UI: replace current read-only week rendering with one mode-aware component or compose a small edit component at that location. Keep all network/business mapping outside pure field components.

Verification, proportional
Use current Node tests and controlled transport; no new test framework/package.
Focused facts should cover:

two populated groups: edit/add/remove row in one, other group/id/order/areas/currency/rows exact; last row removal affects only that group; new id null/local key never on wire; inactive titles/text untouched.
exact decimal tokens/blank optional bounds/required invalid and unsafe integer input; invalid raw work remains dirty and blocks Apply without a throw or partial save; revert to baseline clean.
General -> rates -> week -> General edit preserves all intent; one PUT with expectedRevision and no post-save GET/double PUT; returned server child ids adopted.
week unconfigured untouched omitted; deliberate configuration7unique ISO days; configured all-closed distinct; independent Saturday/Sunday times and remembered closed hours; partial/reversed pair invalid; untouched fractional TimeText retained.
-403/409/unknown keep new raw inputs, unknown sends no replay, discard reject preserves them and confirm resets.
Run new focused tests, existing feature tests once after final changes, npm run typecheck and touched import/feature/test-discovery guards only, commands<=120s. No build/whole frontend discovery/backend/Host/solution suite or proof battery.
Mounted/browser/Visual NOTRUN is acceptable if no existing usable harness/session exists. Do not install/start/restart anything to obtain it or claim rendered acceptance from Node/typecheck. Report exact test coverage and limitations.

Bounds / delivery
Allowed only existing admin-shipping-authoring feature model/session/components/messages/tests/public boundary, minimal typed extensions needed for the two capabilities. No page/legacy dialog/grid rewrite, Geography picker/new HTTP seam, group topology editing, whole-method creation, activation/status/cutoff/carrier/funding/quote change; no backend/schema/DB/Host/shared-library/design-system/sharedCSS/package/config/skill/AGENTS/baseline change.
No shared ports3000/3419/5088/5432, Redis/RabbitMQ/process stop/restart, server/browser/login/network/live write probe, containers/migrations/download/install/cache/bin/obj/.next purge or credentials. No reset/rebase/stash/force-push/Bridge dummy payload.
Hard35min fromclaim; stop newimplementation/tests25; final10 for evidence/diff/scopedcommit/push/fetchparity/report. At most2 bounded harness corrections, then honest PARTIAL with exact blocker, no confidence loop.
Persist received task verbatim, own RESULT, one worker recovery pointer. Include actual reachable fields, intent merge/onePUT/precision/week proof, command counts vsNOTRUN, exact paths/time/deviations/cleanup. No certification or delivery-complete claim.
Result BEGIN_TOOBA_WORKER_RESULT includes task/parent/mode/base/code/evidence/parity; Production-Functional-Acceptance:PENDING; Schema-Data-Flag-Changes:NONE; Visual: actual state; Stop-State:STOP_FOR_ARCHITECT; automaticNextImplementationTask:NONE; Automatic-Next-Task:NONE.
Only this task. Wait Architect review before next group/destination editor or visual/live acceptance.