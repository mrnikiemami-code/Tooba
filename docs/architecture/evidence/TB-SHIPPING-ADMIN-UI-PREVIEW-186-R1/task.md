PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-UI-PREVIEW-186-R1
Parent-Task: TB-SHIPPING-ADMIN-UI-PREVIEW-186
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 4b7744eb0f7ba79d93e1ab063802bfa25aa1f14a
Program: TOOBA-SHIPPING-ADMIN-UI-ACCEPTANCE
Mode: BOUNDED_VISUAL_PREVIEW_PICKER_DRAFT_VALIDATION_REPAIR
Hard-Timebox: 45 minutes HARD; STOP and report PARTIAL if exceeded.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Architect disposition for 186 is REPAIR, not ACCEPT. Preserve the implemented visual direction and repair three concrete interaction defects in the existing preview ONLY. Deliver the same browser-openable UI for user inspection. No production/backend integration, migration or redesign. User visual acceptance is still PENDING.

MANDATORY FIRST CHECK
Fetch origin and verify main / HEAD == origin/main == Expected-Base-SHA before edits. A safe fast-forward from a clean, merely behind local main to this exact expected origin SHA is allowed; preserve unrelated dirty artifacts. Divergence, different remote head or unsafe worktree: STOP_FOR_ARCHITECT with exact state, no reset/rebase/stash/force-push.
Read AGENTS.md, .cursor/skills/tooba-frontend-feature/SKILL.md, docs/architecture/FRONTEND-ARCHITECTURE-STANDARD.md, docs/architecture/shipping/SHIPPING-ADMIN-RULES-DECISION.md (including multilingual and visual-only sections 10–11), docs/architecture/shipping/SHIPPING-STOREFRONT-UX-REFERENCE.md, 186 task receipt and 186 RESULT.md. Current code is under src/frontend/app/admin/shipping-services/design-preview/.
Use existing kebab-case filenames and current component/model/i18n naming conventions. Keep the same Program; this is repair R1 of 186, not a new implementation program. Claim one task only.

ARCHITECT FINDINGS — SOURCE VERIFIED

preview-region-picker.tsx disables EACH option whenever previewPlaceConflict(methodSelected, place.id) is non-null. That confuses browsing an ancestor with ADDING it. After adding Golpayegan, picker state resets to empty; the only country option Iran is disabled because it is an ancestor. The user cannot navigate through Iran to add disjoint Qom. The pure model correctly allows Qom, but the UI makes it unreachable. The Architect independently ran the 10 existing model tests (all pass) and reproduced that mismatch using the current predicate. Existing tests do not establish real UI reachability.
design-preview-screen.tsx owner onChange immediately changes owner then calls setEditor(null) / setDraft(null), without the dirty/discard protection used by Back. It silently loses unapplied changes. Pending row-editor inputs can also be lost because they live below tab-level conditionally mounted components.
preview-rate-row-editor.tsx computes deliveryError only when submitted is already true, but submit setsSubmitted(true) then checks the PREVIOUS render's deliveryError. A first submit with minDays > maxDays can call onApply and close the editor. rangeFromInputs also rounds count/amount values BEFORE integer checks, weakening the advertised validation. Repair raw-input validation as a UI concern only; do not build a pricing engine.

BOUNDED REPAIR
A. GEOGRAPHY — BROWSE IS NOT ADD
Allow navigating through ancestors to reach still-available descendants. Block conflicting ADD actions, not necessary hierarchy traversal.

Adding Iran selects the country; adding province/city/area under it remains forbidden.
With Golpayegan already selected and picker reset, normal mouse/keyboard selection must allow Iran -> Qom province -> Qom city -> Add.
Iran and Isfahan must remain NOT ADDABLE while Golpayegan is selected.
Province Tehran added -> navigating to its city must never allow adding the descendant.
Across groups of this same method, duplicate/ancestor/descendant additions stay blocked; other method/owner scopes remain independent.
No automatic deletion/replacement. Show localized readable conflict reason for the candidate ADD action.
Use a clear distinction between navigable and addable states; no hidden developer controls or code-based option selection. Do not simply remove the ancestry validation.

B. DRAFT SAFETY
Owner changes must not silently discard an unapplied method or row draft. Either retain per-owner drafts or provide localized explicit discard/cancel handling; Cancel preserves the owner, route/editor and ALL current field values.
Preserve pending row-editor fields across general/regions tab switches, or guard the transition before unmounting and allow Cancel to keep editing. Do not force the user to Apply a row merely to avoid accidental loss.
Back-to-list and changing owner must use consistent handling of both method-draft and pending row inputs. Existing interface/content language switches must still preserve edits and stay independent.
Do not use persistent browser storage. Keep View-mode non-writability and live production behavior unchanged.

C. FIRST-SUBMIT VALIDATION
Validate raw current values synchronously in the submit handler; compute the errors independently of whether the UI has shown them yet.
For min delivery > max delivery, the FIRST click must show the localized error, keep the editor open and leave existing rows unchanged. Correcting the values then applies once.
Reject non-finite, negative, or fractional count/day input when integer is required BEFORE conversion/rounding. Preserve legitimate kilogram -> integer-gram conversion at the existing supported precision; do not redefine financial rounding or currency scale.
Maintain explicit start-inclusive/end-exclusive range wording. Preserve all other valid edit/create interactions.
This is form correctness in the visual preview, not production server validation or settlement behavior.

VALIDATION — TARGETED, NO LOOPS
Use existing dependencies and existing preview listener on :3419 if safely available; no package installs/browser downloads/new Host/DB/shared restarts/cache purges.
Run relevant focused tests once after the coherent fix, including genuine coverage of these regressions. The old 10 pure-model tests alone are insufficient.
In a real browser, with ordinary user controls and no evaluate-based mutation of input/select state, verify:

Add Golpayegan; reset/close/reopen picker; browse through Iran and add Qom. Then verify adding their parent Iran remains blocked.
Create a second region group; check cross-group duplicate/parent conflict is blocked. No geography in the rate-row editor.
Change a method field, switch owner, Cancel -> all values and owner unchanged. Confirm discard -> switch works.
Start/edit a rate row, type pending values, switch tab and return (or Cancel transition) -> no silent loss. Check Back/owner behavior for this pending state too.
Enter delivery 5 to 2; FIRST Apply rejects it and row count/content stays unchanged. Correct to 2 to 5 -> apply once.
Persian RTL / English LTR messages and content-language independence still work. No business network writes.
Record actual control sequence and results. Native disabled-option reachability must be tested, not bypassed by JS or Playwright force. Use DOM assertions; screenshots are optional and not required deliverables.
At 390 and 1024 widths check no new page-wide overflow. No gallery or screenshot-capture recovery loop.
Run the scoped test file(s) and typecheck once within 180 seconds when feasible. Record pre-existing failures; do not run broad suites, baseline refreshes or unrelated guard repairs. One bounded harness recovery maximum; missing browser evidence => PARTIAL.
Keep the task-owned preview URL reviewable. Record process ownership/start command. Do not use or restart shared :3000/:5088/:5432, shared DB/Redis/RabbitMQ, or delete .next. Any genuinely separate listener must use isolated output; do not repeat 186's lack of a dedicated output directory. No need to restart a safe existing task-owned listener solely for this repair.

AUTHORIZED SCOPE
Existing preview components/model/tests/i18n only plus this receipt and concise evidence. No production shipping path, schema, DTO/API, navigation replacement, Storefront, global CSS, shared design-system redesign, language sweep, carrier integration or financial engine. Preserve the two-tab full-page design, one geographic group with multiple rate rows, owner context and dynamic-production-locale boundary. No security/tenant/auth bypass.
TB-185 performance remains paused. No follow-up task or automatic continuation.

ACCEPTANCE VERDICT
PASS only when all three source defects are corrected, ordinary browser interactions above pass, preview remains temporary/no business writes and production behavior is untouched. USER_VISUAL_ACCEPTANCE=PENDING. PRODUCTION_FUNCTIONAL_IMPLEMENTATION=NOT_STARTED.
PARTIAL when required proof/repair is incomplete; BLOCKED for unsafe base/auth/isolation/architecture conflict. End at STOP_FOR_ARCHITECT. Do not claim 186 accepted or user approval.

EVIDENCE
Persist received artifact verbatim:
docs/ai/tasks/TB-SHIPPING-ADMIN-UI-PREVIEW-186-R1.task.md
Record concise actual evidence:
docs/architecture/evidence/TB-SHIPPING-ADMIN-UI-PREVIEW-186-R1/RESULT.md
Include source fixes, browser sequences, exact test commands/results, changed files, no-write check, process state, elapsed time and exact preview URL. Note that pure-model PASS in 186 did not prove the dropdown traversal.
Commit only scoped work, push main normally, fetch and verify HEAD == origin/main. Preserve all unrelated work. Never invent checks or claim screenshots.

RESULT FORMAT
Use real values or NONE in place of ellipses; no unresolved placeholders in transmitted result.
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-UI-PREVIEW-186-R1
Parent-Task: TB-SHIPPING-ADMIN-UI-PREVIEW-186
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-UI-ACCEPTANCE
Mode: BOUNDED_VISUAL_PREVIEW_PICKER_DRAFT_VALIDATION_REPAIR
Bridge-Task-Row: ...
Status: PASS|PARTIAL|BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 4b7744eb0f7ba79d93e1ab063802bfa25aa1f14a
Base-SHA: ...
Code-SHA: ...
Evidence-SHA: ...
automaticNextImplementationTask: NONE
HEAD-Origin-Parity: ...
Geography-Normal-Control-Reachability: ...
Cross-Group-Geography-Conflict: ...
Owner-And-Pending-Row-Draft-Safety: ...
First-Submit-Delivery-And-Raw-Input-Validation: ...
Real-Browser-Interaction-Proof: ...
Multilingual-And-Responsive-Checks: ...
Business-Network-Writes: ZERO|...
Live-Shipping-Behavior: PRESERVED|...
Focused-Tests-And-Typecheck: ...
Preview-URL-And-Start-Command: ...
Changed-Files: ...
Isolation-And-Cleanup: ...
Elapsed-Minutes: ...
User-Acceptance-Still-Open: YES
Production-Functional-Implementation: NOT_STARTED
Automatic-Next-Task: NONE
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
