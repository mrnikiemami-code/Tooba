PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-090
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-089
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-TABS
Mode: BOUNDED_DEVELOPMENT_ONE_LEVEL_ROLLOUT_AND_LIVE_PROOF
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 9c233ae5167334047757683c45f3727ee8975b29
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD

ARCHITECT RULING
Task 089 is ACCEPTED for source/test-complete fifth tab, NOT live operation. All five tabs are now wired in source but Tooba:ShippingOneLevelCatalog remains OFF and Development one-level tables/backfill are absent. This task has narrowly bounded authorization to prepare and, ONLY after positive target/provenance and backup/rollback checks, run the new additive Fulfillment migration and controlled idempotent mapping/backfill on the Development tooba_alpha database. It has NO authorization for any production/staging, checkout price cutover, historical order/fulfillment shipment rewrites, ShippingRateAuthority or ShippingOperationalShadow enablement, uncontrolled restart, or financial/capacity features. If the safe live rollout or restart preconditions cannot be proven, STOP with PARTIAL and evidence rather than improvising. Do not claim the new page live unless genuinely observed through a browser.

PREFLIGHT / STOP ON CONFLICT
Claim exactly one Pending task using BRIDGE-WAKE-V1. Preserve the received task artifact as UNSTAGED docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-090.task.md. Read AGENTS.md, docs/ai/TOOBA-PIPELINE-PROTOCOL.md, master recovery latest shipping checkpoint, SHIPPING-ONE-LEVEL-UI-081 through -089.md, 082 migration and backfill planner, 083 isolated migration proof, 085 admin read bridge, 086 server-owned binding, and 084-089 frontend. Fetch origin/main; Expected-Base-SHA must match or drift must be proved strictly task bookkeeping only. Preserve all preexisting dirty/untracked files and running services; no reset/stash/rebase/force push. At first overlapping source drift or uncertain database identity, RECOVERY_CONFLICT/STOP.

MISSION A — PROVENANCE, SAFETY, AND BACKFILL EXECUTION

Before any DDL/DML identify the real runtime Development commerce provenance through GET /__platform-commerce AND read-only database checks. Require exact edition=SingleStore, deploymentId=dev-singlestore, tenantId=store-alpha, connectionReference=tenant-alpha, PostgreSQL database tooba_alpha. Check migration history, existing data counts, method code uniqueness and all referenced checkout/draft/shipment codes. No other environment may be touched.
Establish a trustworthy pre-DDL backup/export of relevant Fulfillment catalog and new tables (if present), exact migration state, and documented rollback steps. A backup path must actually exist and be verified before applying. Do not dump secrets into evidence, Git or logs. Do NOT run a database reset or destructive down migration on populated Development data.
Make a minimal Fulfillment-owned, repeatable persisted backfill executor for ShippingMethodBackfillPlanner, if absent, respecting the module's existing architecture. It must enforce immutable stable methodCode and legacy binding, no option UUID identity dependency, inactive options preserved, optionless services mapped to nullable optionCode, no bare parent method for option-bearing services, no destructive option update, transactional all-or-nothing behavior, dry-run with violation output, and idempotent re-run. Abort on conflicts; never auto-repair conflicting catalog rows or rewrite historical codes. Use the existing Contracts/Domain/Infrastructure boundaries, not Host business logic.
Verify the exact additive migration 20261015120000_AddShippingOneLevelCatalog previously proven only on isolated PostgreSQL. Apply ONLY to positively identified tooba_alpha after backup, via the project's canonical migration tooling; read back schema and EF history. Execute dry-run mapping, verify approved deterministic plan against source rows, then commit backfill transaction ONLY if zero violations. Verify second dry-run yields all Skip; report every method with code, binding and correct active status, including historical post:express inactive and snapp_courier inactive. No synthetic tariff, grant, schedule or packaging data may be added. If migration is applied but backfill fails, keep gate OFF, report exact recovery plan and STOP.

MISSION B — READ GATE / BROWSER PROOF ONLY AFTER SAFE DATA READINESS
5. The gate is separate from customer price authority: enable Tooba:ShippingOneLevelCatalog ONLY in positively identified Development configuration if and only if real migration + backfill + bridge route are ready, and a safe reversible activation plan exists. Never enable Tooba:ShippingRateAuthority or Tooba:ShippingOperationalShadow. Prefer ephemeral, scoped Development-only configuration; do not commit an enabled value into default/global appsettings.
6. Do not silently terminate/restart the running Host or Next process. A controlled Development-only restart/redeploy is permitted only if ownership, actual process, launch command, permission, and reliable rollback to prior binary/config have been positively proven, no unrelated work will be lost, and readiness checks show it is safe. Otherwise defer restart and classify browser-live proof as NOT VERIFIED. Never kill unknown processes or restart a shared/user-managed service speculatively.
7. If the new Host route is live and gate is enabled, use authenticated authorized admin session to inspect GET /v1/admin/shipping-services/shipping-methods/one-level and binding resolver, then open /admin/shipping-services in real browser. Verify exactly five tabs, one independent row per method (including inactive), correct labels, no nested level, and tab 2/3/4 data correctly scoped by method and tab 5 read-only diagnostic with valid server response. Where safe, perform ONE narrowly isolated reversible write/read-back on a task-owned disposable policy/schedule using existing contracts and cleanup; otherwise do read-only checks and say what remains unverified. Do not mutate merchant existing business policies or test pricing/coverage without positive ownership and rollback. No invented PASS; report real request/response evidence without credentials.

MISSION C — FOCUSED VERIFICATION AND HANDOFF
8. Run minimal tests for the persisted executor, idempotency/conflict rollback, migration, and any touched code; limited frontend focused tests/typecheck only if changed. Avoid broad-suite or Host DLL-lock retries; for locked Host output use approved isolated OutDir only if clearly safe, else record BLOCKED. Hard STOP at 30 minutes; do not stretch to finish a long migration or environment restart. A PARTIAL result is acceptable, and must explain whether any database writes occurred.
9. Write docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-090.md and evidence under docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-090/** with exact pre/post provenance, backup verification, migration and backfill results, gate state, environment/build/restart state, five-tab real-browser evidence if available, changed paths, rollback steps, test counts, SHAs, actual elapsed, and blockers. Do not update unrelated recovery history destructively. Commit only task-owned source/documentation. Receipt stays unstaged.

ALLOWED PATHS

src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/Shipping/**
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Shipping/**
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/DependencyInjection/FulfillmentModule.cs only for proven minimal DI requirements
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Behavior/ShippingOneLevel*Tests.cs
docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-090.md
docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-090/**
docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-090.task.md (receipt only; NEVER stage)
Minimal existing Development-only runtime configuration ONLY with positively verified backup and rollback, not global appsettings (report exact path; avoid committing secrets)
Any new file outside these paths requires STOP_FOR_ARCHITECT with a proposal rather than silent scope widening.

FORBIDDEN
No new shipping merchant hierarchy or second-level selector; no loss of existing fulfillment service/option catalog; no dual-write to legacy; no migration unrelated to one-level; no automatic changes to Order, Checkout, Payment, Settlement, Shipping pricing authority, Geography or historical snapshots; no guessed packed weight or item count; no atomic capacity booking claims; no COD settlement claim; no customer delivery promise claim; no live commercial cutover; no fake tariff approvals. No force-push, stash, reset, unsafe migrations, production/staging connection, automatic next task, uncontrolled restarts, or unlimited test loops.

ACCEPTANCE / STOP
PASS only if a real, safely proven Development migration/backfill has been applied, idempotency and referential integrity demonstrated, and authorized real admin five-tab visibility/read operations genuinely verified; any missing live/browser verification is PARTIAL with precise evidence. BLOCKED on unsafe target, missing valid authentication or no credible rollback; RECOVERY_CONFLICT on overlapping drift. Distinguish source/test readiness from commercial readiness. At 30m stop and report. STOP_FOR_ARCHITECT; automaticNextImplementationTask NONE.

RESULT CONTRACT — CANONICAL BRIDGE-WAKE-V1
Return exactly one complete result:
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-090
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-089
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-TABS
Mode: BOUNDED_DEVELOPMENT_ONE_LEVEL_ROLLOUT_AND_LIVE_PROOF
Bridge-Task-Row: <actual>
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 9c233ae5167334047757683c45f3727ee8975b29
Base-SHA: <actual>
Code-SHA: <actual or NONE>
Evidence-SHA: <actual or NONE>
automaticNextImplementationTask: NONE
<exact changed paths, Development provenance, backup artifact verification, DDL/DML and backfill status, gate and process state, tests, real browser proof or explicit lack, SHA and elapsed, rollback, readiness blockers>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK