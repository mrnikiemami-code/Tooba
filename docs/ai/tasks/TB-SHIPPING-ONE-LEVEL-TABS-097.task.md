PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-097
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-096
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_OPTION_BACKED_ACTIVATION_UI
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: d9773e21700ffc3c7ba6d8df75d3e15efc21161f
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD

ARCHITECT RULING
096 PASS accepted for narrow Fulfillment-owned option-backed activation authority; legacy option activation is the effective storefront authority, with the one-level projection aligned transactionally. In this task expose ONLY that existing authorized endpoint from tab 1. NEVER pretend optionless service-level activation is supported. No live Development activation write, including temporarily toggling and restoring post:express. Source/test proof and read-only live proof are acceptable; report live write as NOT VERIFIED. A real UI toggle must work for authorized admins after deployment; no fake success.

PREFLIGHT — STOP ON CONFLICT
Claim exactly one pending bridge task on tooba-main; preserve docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-097.task.md as UNSTAGED receipt. Read AGENTS.md, protocol, 095/096 evidence and 094 tab-1 implementation. Fetch origin/main. Expected SHA must be ancestor; classify drift. Stop RECOVERY_CONFLICT for overlapping source changes, preserving all dirty/untracked paths. No reset/stash/rebase/force push. Verify Host provenance before read-only live probes. The shared Host may not yet contain 096; do not restart it to demonstrate this task.

MISSION A — ONE-LEVEL TAB 1 ACTIVATION CONTROL

Reuse existing 096 PUT /v1/admin/shipping-services/shipping-methods/one-level/{methodCode}/activation with body {isActive}; first inspect actual endpoint response/status/ErrorCodes and module permissions. Do not invent another endpoint, client legacy service/option identities or broad service update.
Add one concise activation/deactivation action for OPTION-BACKED methods only, using server-resolved binding (legacyOptionCode non-null) and the immutable methodCode; display current active state and target action in Persian. For deactivate show an explicit confirmation that it affects the storefront eligibility. For activation likewise make impact clear. The pending/busy state must prevent duplicate submissions; success only after a confirmed server response followed by catalog re-read. Auth 401/403, gate off, inactive parent, missing option, optionless/refusal and unexpected errors are explicit and fail closed.
Optionless methods (snapp_courier, store_courier, in_person) MUST have no functional toggle; show 'فعال‌سازی این نوع سرویس هنوز پشتیبانی نمی‌شود' or equivalent. No automatic parent activation. The 094 name editor, five tabs, RTL, selection and keyboard semantics remain unchanged.
Guard against stale method selection and response races: an in-flight result must not appear as another method's success. No optimistic local isActive state and no client-side code splitting. Preserve any unrelated dirty frontend files.

MISSION B — FOCUSED VERIFICATION
Add minimal model/API/component tests for exact endpoint and payload, option-backed vs optionless eligibility, confirmation, duplicate prevention, denial/refusal, immutable identity and stale response, refreshed server truth. Run focused shipping one-level frontend tests and typecheck; avoid unrelated broad suite with known baseline failures. If backend is untouched, don't rebuild backend/Host. On shared Development run read-only catalog/DOM checks only, if practical. Do NOT change any service activation in real tooba_alpha, even as a reversible experiment; use mocks or isolated disposable DB for a write-proof if feasible within timebox. No restart of Host/Next; no enabling gates.

ALLOWED PATHS
src/frontend/app/admin/shipping-services/shipping-one-level-activation-.ts
src/frontend/app/admin/shipping-services/shipping-one-level-activation-.tsx
src/frontend/app/admin/shipping-services/shipping-one-level-tabs.tsx
src/frontend/app/admin/shipping-services/shipping-one-level-tabs-model.ts
src/frontend/app/admin/shipping-services/shipping-one-level-tabs.test.ts
src/frontend/app/admin/shipping-services/shipping-one-level-.test.ts
docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-097.md
docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-097/*
docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-097.task.md (UNSTAGED RECEIPT ONLY)
If an additional path is essential, STOP and request approval rather than expand silently.

FORBIDDEN
No backend/Host/API modification; no optionless toggle; no independent writes to shipping_methods; no legacy full-service PUT for activation; no migration, seed, DB DML to shared Development, forced live activation, checkout/rate/COD/capacity/payment changes, live restart, credential exposure or mutation of unrelated dirty paths. No invented PASS.

ACCEPTANCE / STOP
PASS: functional tab-1 option-backed activation UI correctly calls 096 endpoint, server-confirmed refresh, guarded optionless method and error states, focused tests + frontend typecheck green. PARTIAL if source/test proof complete but live current-code endpoint unavailable, describing precise limitation. BLOCKED if the endpoint contract cannot safely support the UI. Timebox 30 minutes HARD and STOP_FOR_ARCHITECT, no subsequent task automatically.

RESULT CONTRACT — BRIDGE-WAKE-V1
Return one BEGIN_TOOBA_WORKER_RESULT ... END_TOOBA_WORKER_RESULT with Task-ID, Parent-Task, Channel, Worker-Id, Agent-Type, Program, Mode, Bridge-Task-Row, Status, Stop-State, Expected-Base-SHA, Base-SHA, Code-SHA, Evidence-SHA, automaticNextImplementationTask: NONE; exact paths and test commands/counts, live gate/provenance, no-live-DML statement, UI contract decisions, known limitations, actual elapsed and rollback.
END_TOOBA_TASK
