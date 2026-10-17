PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-086
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-085
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-TABS
Mode: BOUNDED_ONE_LEVEL_RATES_AND_COVERAGE_TAB
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: a0c320953b83f6daf4547c8fa9f3d4223274f825
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD

ARCHITECT RULING
085 is ACCEPTED for its bounded authorized one-level read bridge; 53-minute overrun is not accepted as a precedent. 086 is task SIX of ELEVEN (081–091). Deliver actual tab-2 «تعرفه و پوشش» functionality behind the existing default-OFF one-level gate, without creating a second selectable merchant level, pretending the live one-level database is migrated, or changing customer Checkout. Keep the legacy screen fully functional with gate OFF. No automatic 087.

PREFLIGHT — STOP ON CONFLICT
Claim exactly one Pending task on tooba-main under BRIDGE-WAKE-V1. Persist received task as docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-086.task.md, UNSTAGED. Read AGENTS.md, docs/ai/TOOBA-PIPELINE-PROTOCOL.md, 081–085 design/evidence and latest recovery; inspect exact source for shipping-one-level-tabs.tsx, API client, shipping rate/coverage dialog, contracts, destination picker, and Fulfillment one-level read bridge. Fetch origin/main; verify Expected-Base-SHA or prove any later commits are strictly nonoverlapping bookkeeping. Preserve dirty/untracked paths without reset/stash/rebase/force push. Stop RECOVERY_CONFLICT on overlapping source drift. Do not restart Host or Next, apply migrations, seed/backfill, switch gates, or touch staging/production.

MISSION A — SINGLE-LEVEL CONTEXT (MANDATORY)

Confirm one-level method identity is the canonical persisted <=32-character method_code. A method is merchant-facing as one row and one selected tab context. Its hidden legacyServiceCode + nullable legacyOptionCode binding is required for reuse of Shipping's EXISTING (service_code, option_code?) APIs. NEVER infer that binding by splitting arbitrary method_code in the browser: optionless post historical fulfillment codes and composite identities make inference unsafe. provider_kind is internal, not a second UI level.
Inspect the 085 public admin DTO: it intentionally excludes internal legacy binding. Choose the narrowest authorized, server-side Contracts-only resolution needed to translate selected method code to its exact legacy binding for shipping-owned read/write operations, without exposing provider nesting as a second configurable level. If safe mapping requires new backend source beyond this bounded task, implement only the minimum authorized bridge within Fulfillment Contracts/Application/Endpoints and tightly corresponding tests. Do NOT allow raw client-provided serviceCode/optionCode to substitute for validated server-owned mapping.
The gate is currently OFF and Development one-level tables are absent. Preserve fail-closed OFF fallback; no fake method list from the legacy hierarchy, no UI launch claim, no uncontrolled live migration/backfill. If tab-2 cannot be wired safely without those prerequisites, deliver bounded ready-to-wire adapter and explicit BLOCKED/PARTIAL with precise remaining work, not fictional PASS.

MISSION B — TAB 2 «تعرفه و پوشش»
4. Replace ONLY tab-2 placeholder with the existing legitimate rate and coverage interactions scoped to the selected ONE-LEVEL method. Reuse established Shipping admin API, validation, destination picker, tier conditions (grams and physical item count), service fallback and option-specific precedence. The selected method must be immutable while an operation is pending; switching methods must not leak stale results or save onto the previous method.
5. Provide real loading/empty/error/403/404 states, save/cancel feedback and conservative unsaved-change handling. Show which one-level method is being edited; NEVER render a second-level option picker or misleading parent-service editable grouping. Respect RTL, keyboard access, and the five-tab URL/context model established in 084.
6. Do not delete old dialogs or row actions if they remain necessary to the legacy OFF route. Do not broaden this task to tabs 3–5, new catalog CRUD, Checkout cutover, or merchant rate seeding. No unauthorized live data writes.

MISSION C — FOCUSED VERIFICATION AND HANDOFF
7. Focused tests must cover mapping for post:standard, inactive post:express, tipax:standard/express, optionless store_courier/in_person and unmapped/malformed code (fail closed); stale method selection; rate tier boundary semantics (1999/2000 grams, [min,max)); fallback vs option precedence; gate OFF legacy parity. Do not invent successful UI/DB tests that cannot run with the gate OFF.
8. Run smallest relevant frontend test file(s), frontend typecheck, and only backend tests affected by actual changes. Avoid whole module reruns if targeted tests suffice. If Host DLLs are locked, do not retry repeatedly and do not restart Host; record the blocker and use isolated outputs only if already established safe. Never rerun the known preexisting FE-SIZE guard merely to rediscover unchanged failures.
9. Record changed paths, exact tests and results, actual elapsed, architecture ownership, gate state, data authority, user-visible proof vs source-only proof, rollback and open prerequisites under docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-086.md and docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-086/. Commit/push ONLY task-owned paths. Do not change master recovery unless separately authorized.

ALLOWED PATHS

src/frontend/app/admin/shipping-services/** (only necessary tab 2 integration and focused tests)
src/frontend/app/admin/shipping-services/page.tsx (only if necessary to preserve server-side binding)
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Contracts/Shipping/** (minimal method-to-binding read contract, if unavoidable)
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/Shipping/** (minimal validated binding query/port, if unavoidable)
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Shipping/** (minimal reader, if unavoidable)
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Endpoints/Shipping/ShippingServiceEndpoints.cs (additive authorized read only, if unavoidable)
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/DependencyInjection/FulfillmentModule.cs (DI only if needed)
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/** (focused regression tests)
src/backend/Host/Tooba.Host.Tests/Architecture/FulfillmentModuleAmsc001W3CertGuardTests.cs (only exact route manifest delta if needed)
docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-086.md
docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-086/**
docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-086.task.md (receipt only; UNSTAGED)

FORBIDDEN
No schema changes, no real Development DDL/DML/backfill, no activation of Tooba:ShippingOneLevelCatalog, ShippingRateAuthority or ShippingOperationalShadow, no Order/Checkout/Payment/Settlement/Geography changes, no invented packed weight/item count, no capacity reservation or COD collection claim, no production/staging actions, no arbitrary admin auth bypass, no fake rate/coverage data, no code splitting as an identity authority, no unrelated source changes, no automatic next task, no restart or force push.

ACCEPTANCE / STOP
PASS only if tab 2 is genuinely functionally integrated for the authorized selected method and focused tests pass, with truthful gate-OFF and unmigrated Development limitations. PARTIAL if a safe connector or tab UI remains incomplete within 30 minutes, BLOCKED for missing authority/environment, RECOVERY_CONFLICT on overlapping drift. Hard stop at 30 minutes even if unfinished. Record exact elapsed and use STOP_FOR_ARCHITECT.

RESULT CONTRACT — BRIDGE-WAKE-V1
Return one complete result with these fields, then a concise evidence body:
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-086
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-085
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-TABS
Mode: BOUNDED_ONE_LEVEL_RATES_AND_COVERAGE_TAB
Bridge-Task-Row: <actual>
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: a0c320953b83f6daf4547c8fa9f3d4223274f825
Base-SHA: <actual>
Code-SHA: <actual or NONE>
Evidence-SHA: <actual or NONE>
automaticNextImplementationTask: NONE
<changed paths, tests, scope, authority mapping, gate/DB evidence, remaining blockers, actual elapsed, rollback>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK