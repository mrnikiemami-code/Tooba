PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-UI-PREVIEW-186-R2
Parent-Task: TB-SHIPPING-ADMIN-UI-PREVIEW-186-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 4a37697eef577963c08519616f44ff035ceb4cb1
Program: TOOBA-SHIPPING-ADMIN-UI-ACCEPTANCE
Mode: BOUNDED_MULTILINGUAL_PER_DAY_SCHEDULE_VISUAL_REPAIR
Hard-Timebox: 45 minutes HARD; STOP and report PARTIAL if exceeded.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Repair only the shipping design preview's weekly operating schedule. The owner says the preview is better but is NOT visually accepted, and specifically cannot assign different operating hours to different weekdays. Current PreviewSchedule has weekdays[] plus one fromTime/toTime pair shared by every selected day. Replace that preview-only representation and its controls with independent weekday hours, using a compact, understandable weekly table. This is one bounded visual repair, not production shipping implementation, delivery-slot construction, or a general editor redesign.

OWNER REQUIREMENT
Each weekday must independently support working/closed and its own start/end time. Example: Saturday 09:00–13:00, Sunday 15:00–18:00, Friday closed. Changing Sunday must not change Saturday. Geography remains selected once in region groups; the weekly method schedule does not add geography selectors. "Shared method schedule" means shared by this method's regions, NOT identical hours across weekdays.

MANDATORY FIRST CHECK
Fetch origin/main and inspect HEAD, dirty files and existing task-owned listeners. Expected base is the full SHA above. A clean, strictly behind main may fast-forward only to the exact expected base; otherwise HEAD == origin/main == Expected-Base-SHA is required before edits. Stop for architect on divergence, changed remote base or conflicting dirty files; no reset/rebase/stash/force-push. Preserve unrelated work and running apps.

Read AGENTS.md, .cursor/skills/tooba-frontend-feature/SKILL.md, docs/architecture/FRONTEND-ARCHITECTURE-STANDARD.md, docs/ai/AGENT-SKILLS.md, docs/architecture/shipping/SHIPPING-ADMIN-RULES-DECISION.md, docs/architecture/shipping/SHIPPING-STOREFRONT-UX-REFERENCE.md, and TB-186/R1 task/evidence. Inspect the current preview model, fixtures, schedule UI, i18n, cloning/draft/reset/apply flows and focused tests. Apply repository naming and ownership rules; existing preview files use kebab-case. Do not create a second feature architecture or design system.

BOUNDED REPAIR

WEEKLY UI
Replace the selected-day checkboxes plus single shared interval with seven clearly labelled weekday rows. Each row has working/closed, start time and end time. Show one interval per day in this slice; no recurrence builder, multi-window editor, calendar or bulk-copy workflow. Closed days clearly say closed and have inactive time controls. Keep the schedule easy to scan in Persian RTL and English LTR. Make schedule editing discoverable; a compact collapse/expand control may remain, but the collapsed summary must truthfully reflect different weekday hours instead of showing a false shared interval. Respect existing View mode.

PREVIEW MODEL AND EXAMPLE
Use a typed per-weekday schedule structure within the existing in-memory preview model. Keep stable weekday identity independent of translated labels and interface locale. Update preview fixtures, new-method defaults, deep clone, reset, apply and summaries together so there is no residual shared-hour writer or aliasing across days/methods/owners. Demonstrate Saturday 09:00–13:00, Sunday 15:00–18:00 and Friday closed in at least one clearly SAMPLE method. Default values for new methods must be explicit sample defaults, not inferred real operating hours. This is not a backend schema migration.

DRAFT AND VALIDATION
Editing a day changes only that day. Switching tabs, interface language or content language preserves every draft value and pending invalid value. Working/closed must not silently erase typed hours; reopening a day restores its draft hours. Active days require valid same-day times with start strictly earlier than end. Validate synchronously before the first Apply, keep invalid values editable, show localized field errors and do not apply an invalid draft. Reject equal/inverted or missing times without silently rounding, reversing or extending overnight. A closed day contributes no operating interval. Do not assert that all days must be active or invent a new business rule requiring at least one active day; describe all-closed truthfully in the preview.
Preserve R1 owner/back discard confirmation, pending rate-row safety, geographic browse-versus-add behavior, cross-group conflicts and raw rate-row validation.

EXISTING SEMANTICS
Keep the existing cutoff meaning and method-level scope; do not redesign it into per-day/per-region rules or invent new cutoff semantics. Preserve seller/product preparation-time separation. These are operating hours, not customer delivery promises or reservable delivery slots. Do not generate dates, capacity, holidays, carrier commitments or time-zone conversions. If timezone context is displayed, use explicitly labelled sample context, never infer it from fa/en or selected destination.

MULTILINGUAL AND RESPONSIVE
Use preview-scoped fa/en messages, localized weekday labels and readable time formatting. Interface language and content language remain independent; translated names never duplicate schedule data. Preserve current draft on both switches. No production language list or currency configuration change. At 390/1024/1440 widths, show readable day/time controls without accidental page-wide horizontal overflow. Reuse existing design-system controls and local styling; no global CSS or shared component changes.

DURABLE OWNER DECISION
Append a short dated clarification to docs/architecture/shipping/SHIPPING-ADMIN-RULES-DECISION.md: method operating schedule supports independently configured working/closed and hours per weekday; sharing across regions does not require identical weekday hours. Record that this owner requirement is confirmed while the revised visual design and all production implementation remain unaccepted/not started. Do not rewrite other decisions.

SCOPE AND ISOLATION
Change only src/frontend/app/admin/shipping-services/design-preview/**, the specified decision clarification, this task receipt and task-scoped evidence. No live /admin/shipping-services replacement, navigation change, backend/DTO/client/schema/migration, dependency installation, feature flag, storage, business network write, carrier call, Storefront or performance work. TB-185 remains outside scope.
Reuse a healthy task-owned :3419 listener if appropriate. Otherwise use a unique free loopback port. Do not overwrite another process, shared :3000/:5088/:5432, shared build cache or running apps. Do not start Host/DB/Redis/RabbitMQ. Preserve existing unrelated dirty files and stashes. Do not claim screenshots unless genuinely obtained; screenshot delivery is not required.

FOCUSED VALIDATION

Run the existing preview model tests with focused schedule regressions for independent weekdays, deep-clone isolation, closed-day semantics and first-Apply invalid times. Add only tests that verify behavior, not implementation-text mirrors.
Run one bounded frontend typecheck (180 seconds maximum). Run any repository-required focused architecture checks for changed files. Report known unrelated baseline failures without fixing or refreshing them.
In an actual Next browser using ordinary UI controls, edit Saturday and Sunday to different times; verify Friday closed, then toggle it and verify other days unchanged. Prove invalid first Apply stays unapplied with a readable error, then correct and apply to PREVIEW ONLY. Verify reset restores sample schedule.
Verify tab round-trip, fa/en interface switch and content-language switch preserve schedule drafts, View cannot mutate, and owner/back discard protection still works.
Inspect RTL/LTR and 390/1024/1440 overflow. Keep evidence concise: real DOM values/status, browser steps and measurements. No JS mutation/forced state as user interaction proof, fabricated screenshots or production save claims. No repeated broad suites or screenshot-capture loops.
If required browser evidence cannot be obtained within bounds, report PARTIAL, not technical PASS.

EVIDENCE AND STOP
Commit the received task to docs/ai/tasks/TB-SHIPPING-ADMIN-UI-PREVIEW-186-R2.task.md.
Write sanitized evidence to docs/architecture/evidence/TB-SHIPPING-ADMIN-UI-PREVIEW-186-R2/RESULT.md with root cause, final weekday structure, actual browser steps/results, commands/test counts, changed files, exact SHAs, preview URL/start command, task-owned listener state and elapsed time. Stage only scoped files. Follow existing authorized Bridge reporting contract; no probe/dummy result submissions.
PASS means this bounded preview repair is technically verified. USER_VISUAL_ACCEPTANCE remains PENDING; PRODUCTION_FUNCTIONAL_IMPLEMENTATION remains NOT_STARTED. Stop for architect after the single complete result. Do not create or run another task.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-UI-PREVIEW-186-R2
Parent-Task: TB-SHIPPING-ADMIN-UI-PREVIEW-186-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-UI-ACCEPTANCE
Mode: BOUNDED_MULTILINGUAL_PER_DAY_SCHEDULE_VISUAL_REPAIR
Bridge-Task-Row: ...
Status: PASS|PARTIAL|BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 4a37697eef577963c08519616f44ff035ceb4cb1
Base-SHA: ...
Code-SHA: ...
Evidence-SHA: ...
automaticNextImplementationTask: NONE
HEAD-Origin-Parity: ...
Per-Day-Schedule-And-Closed-Days: ...
First-Apply-Validation: ...
Draft-And-Owner-Safety: ...
Multilingual-And-Responsive-Checks: ...
Real-Browser-Interaction-Proof: ...
Business-Network-Writes: ZERO
Live-Shipping-Behavior: PRESERVED
Focused-Tests-And-Typecheck: ...
Decision-Clarification: ...
Preview-URL-And-Start-Command: ...
Changed-Files: ...
Isolation-And-Cleanup: ...
Elapsed-Minutes: ...
User-Acceptance-Still-Open: YES
Production-Functional-Implementation: NOT_STARTED
Automatic-Next-Task: NONE
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK