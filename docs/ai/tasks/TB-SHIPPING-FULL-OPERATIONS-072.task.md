PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-FULL-OPERATIONS-072
Parent-Task: TB-SHIPPING-FULL-OPERATIONS-071
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-FULL-OPERATIONS
Mode: BOUNDED_EXISTING_ADMIN_WEEKLY_SCHEDULE_EDITOR
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 5ec3a37e1dbdfab723d093a3ec6e9c4edf18975c
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 minutes HARD

ARCHITECT RULING
071 ACCEPT for Shipping-owned persistent schedule/API and 154 targeted green tests; NOT for live capacity enforcement or Checkout promised delivery. This is task 2 of maximum 10 tasks (071-080), not authorization to start any subsequent task. Deliver an ACTUALLY USABLE, Persian/RTL, straightforward weekly scheduling editor inside the existing /admin/shipping-services area. The user expects e.g. Sundays only, 13:00-14:00, several windows per day, and optional capacity. Preserve separation between carrier operating/dispatch windows and customer delivery appointment slots. No invented availability or promises. Current tooba_alpha is disposable Development data. Do not create a second database or repeat 071 backend tests. Avoid time-draining build/restoration loops.

PREFLIGHT — STOP ON CONFLICT
On BRIDGE-WAKE claim exactly one Pending task on tooba-main, preserve received .task.md verbatim and unstaged, then read AGENTS.md, docs/ai/TOOBA-PIPELINE-PROTOCOL.md, docs/planning/shipping/SHIPPING-FULL-OPERATIONS-071.md and current Shipping schedule Contracts/API, existing shipping services UI/dialog/model/test patterns and Fulfillment tree access. Fetch origin/main. Expected base is the 071 result-bookkeeping commit; permit only individually verified docs/evidence fast-forward. Source drift overlapping scope => RECOVERY_CONFLICT/STOP. Never reset/stash/rebase/force-push; preserve all unrelated dirty paths. Positively identify the running frontend/backend when doing live UI proof; do not restart working processes without necessity. Do not run Host.Tests while Host holds its DLLs.

MISSION — USABLE SCHEDULING UI, NOT JUST A MOCK
A. Extend the EXISTING /admin/shipping-services service management surface with an obvious Persian label «برنامه کاری ارسال» or «زمان‌بندی سرویس». The schedule editor must be discoverable from the relevant service row, alongside existing rate/coverage controls; use one focused dialog/section, not a new route/page or a giant combined form. Reuse the design-system Dialog/button/form primitives and current authorization/notice conventions. For multi-option services (e.g. tipax standard/express), obtain selectable options from the EXISTING Fulfillment service tree; offer service-level schedule separately with a clearly explained fallback/identity. Do not invent option names or silently change other option schedules.
B. Bind real existing APIs, no fake data: GET /v1/admin/shipping-services/{serviceCode}/operating-schedules; PUT the same route using {serviceCode,timeZoneId,windows:[{weekday,startTime,endTime,capacity}],optionCode}; PATCH /{scheduleId}/active with {isActive}. Verify actual casing/time serialization against backend source; TimeOnly must round-trip correctly. Display saved schedules by option, active status and timezone. Explicitly communicate that empty schedule means no explicit hours configured, NOT a forced block on current Checkout. Disallow destructive overwrites of a different option when editing.
C. Persian RTL weekday editor, ISO mapping Monday=1 through Sunday=7 (یکشنبه=7), multiple start/end rows for an enabled day, add/remove rows, unambiguous 24-hour local HH:mm fields, timezone selector or a validated timezone input defaulting explicitly to Asia/Tehran (not browser-inferred silently), optional positive capacity. Validate locally: weekday valid, start<end (overnight not supported), same-day overlap rejected while adjacency accepted, invalid time, invalid capacity, nonexistent option refused. Provide accessible, field-adjacent Persian errors and real API stable-code mapping. Keep buttons, labels and layout usable on narrow screens with no clipped save/cancel. Clearly label capacity as «ظرفیت تنظیم‌شده (هنوز اعمال نمی‌شود)»; never call it remaining slots or claim actual reservations are enforced.
D. Make saves safe and auditable: clear loading, save, retry and error state; disable repeat submission while pending; upsert replaces ONLY the selected service/option window set as the 071 backend contract specifies. Do not mutate rate, grant, customer checkout or other service. Active toggle must persist and refresh. Handle 401/403 visibly, never bypass. Avoid fake success on network failures. If no schedule exists, show a sensible empty state and support creating it.
E. Focused proof on the EXISTING Development environment: use the saved 071 real tipax/standard Sunday windows (13:00–14:00 and 16:30–18:00 with configured capacity 25) to verify read/display, edit/update and active toggle through the actual UI plus read-after-write (restoring the fixture to original state). Independently verify tipax/express and other services are unmodified. Verify proper day mapping, adjacent vs overlapping, error rendering, service/option separation, and no change in Checkout. Tests: a few pure model/API tests and focused existing shipping frontend tests + TypeScript check; no backend rebuild or broad suites. If browser automation unavailable, give exact limitation and mark PARTIAL instead of saying UI was verified. No unnecessary database writes or parallel instances.

ALLOWED PATHS

src/frontend/app/admin/shipping-services/** (only shipping service UI/model/API and focused tests)
src/frontend/app/admin/shipping-services-api.ts (only if required by existing tree API; preserve compatibility)
docs/planning/shipping/SHIPPING-FULL-OPERATIONS-072.md
docs/architecture/evidence/TB-SHIPPING-FULL-OPERATIONS-072/**
docs/ai/tasks/TB-SHIPPING-FULL-OPERATIONS-072.task.md (receipt only, NEVER STAGE)
No backend production source or migration change is authorized. If a genuine backend defect emerges, preserve exact evidence and STOP for architect decision.

FORBIDDEN
No new frontend page, no Checkout/payment/order/snapshot code or customer shipping quote changes, no schema or migration, no custom permissions, no Results.Json, no invented service options, no fake slots/capacity enforcement, no production/staging, no parallel database, no pointless processes/restarts, no mass test suites or waiting loops, no unrelated edits, no automatic next task.

ACCEPTANCE / STOP
PASS requires a discoverable working Persian scheduling UI in the existing Shipping service management screen, genuinely wired to 071 GET/PUT/PATCH, accurate Sunday window display/edit/toggle and persistence, correct option isolation and validation, useful failures/authorization handling, focused green tests and real-browser interaction proof on existing Development. PARTIAL if critical live UI proof or functionality is missing; BLOCKED only for a real hard blocker. Stop at 45 minutes HARD even if unfinished; record precise remaining work. Preserve all unrelated dirt and report actual elapsed and HEAD/origin/main. Worker PASS != Architect ACCEPT.

RESULT CONTRACT — BRIDGE-WAKE-V1
Return one result string with real values; every placeholder in this TEMPLATE must be resolved before POST /api/results. Do not append text after END_TOOBA_TASK.
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-FULL-OPERATIONS-072
Parent-Task: TB-SHIPPING-FULL-OPERATIONS-071
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-FULL-OPERATIONS
Mode: BOUNDED_EXISTING_ADMIN_WEEKLY_SCHEDULE_EDITOR
Bridge-Task-Row: <actual Bridge row>
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 5ec3a37e1dbdfab723d093a3ec6e9c4edf18975c
Base-SHA: <actual>
Code-SHA: <actual or NONE>
Evidence-SHA: <actual or NONE>
automaticNextImplementationTask: NONE
<actual UI/API implementation; service-option identity; Sunday 13:00-14:00 and 16:30-18:00 browser proof; timezone and capacity disclosure; validation refusals; 401/403 handling; before/after data; focused tests; timing; changed paths; commit/push; remaining blockers>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
