PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-083
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-082
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-TABS
Mode: BOUNDED_IDENTITY_COMPATIBILITY_AND_ISOLATED_MIGRATION_PROOF
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 64ff456bf547dc5cdbdee8ffb60a397b25addf67
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD

ARCHITECT RULING
082 is ACCEPTED for its bounded source changes, not as a successful database migration or live deployment. Existing Fulfillment shipping service/option catalog remains the sole operational read/write authority. NO dual-write is authorized; no activation of Tooba:ShippingOneLevelCatalog or ShippingRateAuthority or ShippingOperationalShadow. New one-level catalog is additive, inactive/default-OFF, with immutable legacy service/option binding and stable method_code <= 32 characters. No live Development database backfill/write authorized. This task proves persistence and identity compatibility in an isolated, disposable PostgreSQL database and diagnoses/fixes objectively demonstrated defects only within its bounded scope. No Checkout or paid snapshots may change.

PREFLIGHT — FAIL CLOSED
Claim exactly one pending task on tooba-main under BRIDGE-WAKE-V1 and preserve the received task file unstaged. Read AGENTS.md, docs/ai/TOOBA-PIPELINE-PROTOCOL.md, docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-081.md and -082.md, previous 082 evidence, Fulfillment EF migration registration and current catalog/backfill implementation. git fetch origin; verify HEAD and origin/main against Expected-Base-SHA; if changed, prove nonoverlap or STOP RECOVERY_CONFLICT. Preserve all pre-existing dirty/untracked paths; no reset, stash, rebase, force push, unauthorized restart. No live/staging/production write. Confirm disposable connection/database positively before any DDL/DML.

MISSION A — ISOLATED REAL DATABASE PROOF (PRIMARY)

Review actual raw-SQL migration 20261015120000_AddShippingOneLevelCatalog and FulfillmentDbContext mappings for table names, check/index constraints, translations, timestamps and EF history order. Investigate known stale FulfillmentDbContextModelSnapshot without broad regeneration. Validate isolated PostgreSQL UP, insert representative methods/translations, uniqueness/invalid-code rejection, read through IShippingOneLevelCatalogReader, and DOWN. Confirm no shipping_services/options or existing historical rows are modified. If no safe disposable database exists, STOP/PARTIAL rather than point at tooba_alpha.
Prove the pure backfill planner against actual legacy shape in an isolated fixture: post:standard active, post:express inactive, tipax:standard/express active, store_courier/in_person optionless, snapp_courier inactive; verify legacy mapping is byte-identical, no standalone parent for services with options, nullable binding for optionless services, repeat idempotency, duplicate/binding conflicts fail closed. Do not execute backfill on Development. If planner has no write executor, document clearly: planning proof is not data persistence; do not claim backfill finished.
Test the 32-character persisted identity boundary and composite method compatibility, including legacy stored drafts, checkout/shipment strings as a read-only compatibility audit. Do not widen columns or rewrite history. Check malformed and duplicate/case-folded codes and service-level fallback remains a rule, not a sellable method.

MISSION B — NARROW CODE GUARDS
4. Fix only verified critical issues found in the new Fulfillment one-level source/migration/reader; add focused regression tests. Prefer no speculative refactors. Check no leaked EF models in Contracts, no Host business authority, no foreign DbContexts, and no break in legacy catalog routes.
5. Run targeted Fulfillment tests and the affected Host certification guard using isolated output/intermediate paths if feasible without touching/rerunning the live Host. If blocked by file locks, report blocked explicitly, do not restart the process. Avoid broad suites and long loops.

MISSION C — EVIDENCE AND HANDOFF
6. Record test commands, real pass/fail counts, exact SQL/DB evidence from isolated target, migrations applied/rolled back, unchanged live environment, code/evidence SHA, and explicit limitations under docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-083.md and docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-083/. State whether isolated migration is production-safe in principle; do not call it live-ready.
7. Recommend scope for 084 (admin five-tab shell and service list) but do not issue/run it. The current planned 11-task ceiling remains. UI must display one independent method per row (post:standard, post:express, etc.), with hidden provider_kind and no second-level selection; inactive remains inactive. User-visible labels independently editable in a later authorized task, not by changing historical code.

ALLOWED PATHS

src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain/Aggregates/ShippingMethod*.cs
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/Shipping/ShippingMethodBackfillPlanner.cs
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Persistence/FulfillmentDbContext.cs
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Persistence/Migrations/20261015120000_AddShippingOneLevelCatalog.cs
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Shipping/ShippingOneLevelCatalogReader.cs
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Behavior/ShippingOneLevelMethodCatalogTests.cs
tests strictly targeted to new catalog persistence under existing Fulfillment test conventions, if necessary
docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-083.md
docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-083/**
docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-083.task.md (receipt only; DO NOT stage)

FORBIDDEN
NO tooba_alpha live DDL/DML or backfill; no Checkout/Order/Payment/Settlement/Geography/Shipping module mutation; no changed storefront projections; no active merchant rate/coverage changes; no dual-write; no gate enable; no production/staging access; no new module architecture, no non-task dirt cleanup; no service restart, no historical order rewrite, no auto next task. Avoid writing a general new migration outside the listed path; if required STOP and request architectural decision.

ACCEPTANCE
PASS only if isolated DB up/down plus constraints/reader and core identity compatibility have objective evidence with focused tests, or if inability is honestly reported as PARTIAL/BLOCKED. 082 claims are independently checked. Save task-owned evidence, commit/push only authorized files; return elapsed time and unchanged/changed paths. STOP_FOR_ARCHITECT after a maximum of 30 minutes; do not continue automatically.

RESULT CONTRACT — CANONICAL BRIDGE-WAKE-V1
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-083
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-082
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-TABS
Mode: BOUNDED_IDENTITY_COMPATIBILITY_AND_ISOLATED_MIGRATION_PROOF
Bridge-Task-Row: <actual>
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 64ff456bf547dc5cdbdee8ffb60a397b25addf67
Base-SHA: <actual>
Code-SHA: <actual or UNCHANGED>
Evidence-SHA: <actual or NONE>
automaticNextImplementationTask: NONE
<concrete test results, migration evidence and isolated provenance, failures, modified paths, actual elapsed, remaining blockers, rollback, recommended 084 scope>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK