PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-088
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-087
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-TABS
Mode: BOUNDED_ONE_LEVEL_OPERATING_SCHEDULE_AND_CAPACITY_TAB
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: f87b036852da34b5b70055a8ed9c38db42dea47f
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD

ARCHITECT RULING
087 is ACCEPTED for its bounded, source-and-test proven tab-3 implementation, NOT for live merchant CRUD or commercial readiness. This is task 8 of at most 11 (081–091). Implement tab 4 «برنامه کاری و ظرفیت» in the one-level five-tab Shipping admin shell, building on existing Shipping-owned operating-schedule APIs and the server-owned methodCode -> immutable legacy (serviceCode, optionCode?) binding from 086. Do not create a second merchant configuration level. Configured capacity is NOT enforced, not a booking and not inventory. Never claim atomic slot reservations, delivery promise, settlement, or Checkout authority. No automatic next task.

PREFLIGHT — CONFLICT => STOP
Claim exactly one Pending Bridge task in tooba-main. Preserve received task artifact under docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-088.task.md, UNSTAGED. Read AGENTS.md, docs/ai/TOOBA-PIPELINE-PROTOCOL.md, docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-081.md through -087.md; source/evidence 071, 072, 076, 084–087; existing shipping-operating-schedule-dialog.tsx, model, API clients and tests; shipping-one-level-tabs.tsx and tab 2/3 binding patterns. git fetch origin and verify Expected-Base-SHA or prove ONLY non-overlapping bookkeeping ancestry with explicit changed paths. Source overlap => RECOVERY_CONFLICT/STOP. Preserve existing dirty/untracked files; no reset, stash, rebase, force push. Do not restart Host/Next or contact staging/production. Do not retry a known Host DLL file-lock test loop.

MISSION A — REAL TAB 4 IN THE ONE-LEVEL SHELL

Replace only tab-4 placeholder with working «برنامه کاری و ظرفیت» component. Show selected one-level method code and label, resolve its immutable internal serviceCode/optionCode exclusively via existing authorized GET /v1/admin/shipping-services/shipping-methods/one-level/{methodCode}/binding; do NOT split methodCode or trust user-supplied service/option pair. Fail closed on gate OFF, unavailable table, absent/invalid binding or auth denial. No option selector/provider sublevel, no grouping by options.
Reuse existing Shipping-owned operating-schedule APIs and existing schedule validation/model; preserve the actual backend semantics: ISO weekday, multiple half-open local time windows, explicit IANA timezone, configured capacity (including null where supported), active/inactive state, scope/option-specific selection and any service-level fallback rules. Do not silently change availability interpretation, timezone conversion, schedule precedence or method activation. Show inactive schedules truthfully.
Implement the minimal existing-capability read/create/edit/activation actions that the already-authorized admin API supports, within selected method only. Ensure writes use server-resolved legacy binding and preserve stored identity on edit; reject mismatched service/option records and refuse edits to policies belonging to other methods. Reuse existing admin schedule UI/model rather than duplicating business logic. If the existing API lacks a required action, stop that slice and document the actual missing contract; do not invent endpoints without separate architectural review.
Present configured capacity with an unequivocal persistent Persian label: «ظرفیت تنظیم‌شده (هنوز اعمال نمی‌شود)». Never show «ظرفیت باقی‌مانده», «رزرو شد» or imply capacity enforcement. The only runtime ShippingCapacityEnforcement is NotEnforced. Carrier schedule represents pickup/dispatch window, NOT promised delivery. The UI must explain both distinctions where capacity or windows are displayed.
Robust states: no method, binding loading / unavailable, loading schedules, zero schedules, 401/403 denied, unexpected server failure, service identity mismatch, saving/success/failure, cancel/unsaved changes. Prevent a late response from one method appearing under a newly selected method; do not falsely claim an in-flight server write can be canceled. Maintain RTL, Persian labels, responsive UI, accessibility and 084 keyboard tab semantics. Tabs 1–3 unchanged; tab 5 remains a truthful placeholder for task 089 unless its wiring already exists.

MISSION B — SMALL ESSENTIAL VERIFICATION
6. Focused tests for method identity isolation (post:standard, tipax:express, optionless store_courier, inactive methods), no guessed code split, gate/not-ready/403 refusal, mismatch, stale selection, weekday and multiwindow boundaries, IANA timezone, activation, configured-capacity NotEnforced text, dispatch-vs-delivery distinction, and tab 1–3 preservation. Reuse tests/models rather than broad suites. Run targeted shipping-services frontend tests and TypeScript typecheck only if necessary. NO Host test build against locked output DLLs, no broad backend suite if backend source is untouched.
7. Current Development one-level gate is OFF and its table/backfill is absent; this task must not enable it, migrate Development, seed or backfill records, switch live Host, or claim real tab-4 UI CRUD. Distinguish source/test proof from live browser proof. No production/staging/database write. Keep legacy screen usable while OFF.

BOUNDARY / ALLOWED PATHS

src/frontend/app/admin/shipping-services/shipping-one-level-schedule-*.ts / *.tsx (new; name consistently)
src/frontend/app/admin/shipping-services/shipping-one-level-tabs.tsx
src/frontend/app/admin/shipping-services/shipping-one-level-tabs-model.ts
src/frontend/app/admin/shipping-services/shipping-one-level-tabs.test.ts
src/frontend/app/admin/shipping-services/operating-schedule ONLY if required for safe reuse and with focused regression proof
docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-088.md
docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-088/**
docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-088.task.md (receipt ONLY, never stage)
Any necessary backend or outside-path change: report precisely and STOP or seek Architect authorization; do not self-widen.

FORBIDDEN
No Fulfillment/Shipping/Order/Checkout/Payment/Settlement/Geography backend changes, no Host edits or service restart, no migration/backfill, no config gate activation, no new permission grants, no pricing cutover, no atomic capacity claim/release, no COD ledger, no promised delivery, no derived parcel weight/item-count, no historical order mutation, no provider->option dropdown, no new second level, no broad suite, no touching unrelated dirty files. Maintain ShippingOneLevelCatalog OFF, ShippingRateAuthority OFF and ShippingOperationalShadow OFF.

ACCEPTANCE / STOP
PASS for bounded implementation only when the tab is genuinely wired to the EXISTING schedule read/write APIs using verified binding; per-method isolation, validation, config-only capacity truth and focused tests are demonstrated with committed evidence. If UI remains gate OFF or Development unmigrated, state explicitly NOT LIVE VERIFIED and NOT COMMERCIALLY READY; this limitation by itself need not convert an otherwise complete source/test deliverable into a fabricated production PASS. PARTIAL if functional behavior or evidence remains incomplete; BLOCKED if access/unsafe path. Stop at 30 minutes HARD or earlier. Report actual elapsed, exact changed paths, tests run and not run, release blockers, SHAs, rollback, and STOP_FOR_ARCHITECT. No automatic 089.

RESULT CONTRACT — BRIDGE-WAKE-V1
Return exactly one worker result in this sequence:
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-088
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-087
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-TABS
Mode: BOUNDED_ONE_LEVEL_OPERATING_SCHEDULE_AND_CAPACITY_TAB
Bridge-Task-Row: <actual>
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: f87b036852da34b5b70055a8ed9c38db42dea47f
Base-SHA: <actual>
Code-SHA: <actual or NONE>
Evidence-SHA: <actual or NONE>
automaticNextImplementationTask: NONE
<actual scope, source-level vs live evidence, tests and skipped tests, capacity truth, changed paths, elapsed, blockers, rollback>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK