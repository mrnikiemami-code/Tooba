PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-GEOGRAPHY-MIGRATION-REGISTRY-045
Parent-Task: TB-SHIPPING-GEOGRAPHY-MODULE-COMPOSITION-044
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ARCHITECTURE-PLANNING
Mode: BOUNDED_GEOGRAPHY_MIGRATION_METADATA_NO_APPLY
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 5e07617c04c4098efb7d1a4af99f5d6a619c8448
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD

ARCHITECT AUTHORITY

Architect accepts 044's inert Host composition and DI only; 044 PARTIAL due to deliberate exclusion of unsafe automatic migration registration.
Authorize migration ORDER CONSTANT and MigrationRunner REGISTRY DESCRIPTOR only if verified non-executing during Host startup. No permission to apply Geography DDL to any Tooba DB.
Exactly one task; do not run any follow-up automatically.

PREFLIGHT — STOP ON CONFLICT

Claim ONLY one Bridge task after BRIDGE-WAKE on tooba-main. No idle polling.
Read AGENTS.md, docs/ai/TOOBA-PIPELINE-PROTOCOL.md, .cursor/skills/tooba-backend-feature/SKILL.md, ADR-036, reports 042-044, migration order and runner registry code. Do not scan deferred findings register.
git fetch origin. Require main, HEAD == origin/main == Expected-Base-SHA. Preserve unrelated dirty/untracked files. No stash/reset/rebase/force-push. STOP RECOVERY_CONFLICT on unsafe divergence.
Confirm no Geography order constant/runner descriptor already exists, verify candidate slot 30 against actual definitions and inspect every affected startup/runtime path. STOP and report if any proposed addition auto-applies migration.

MISSION — ONE BOUNDED NON-ACTIVATING SLICE
A. Add Geography = 30 to the single canonical ModuleSchemaMigrationOrder only if 30 is unused and addition does not reorder existing modules. No duplicate order literals in Geography.
B. Add the GeographyDbContext descriptor to the explicit Tooba.MigrationRunner registry if its presence alone cannot execute migration. Preserve all existing descriptors and registration semantics. Minimal csproj ProjectReference only if genuinely necessary.
C. DO NOT add AddModuleSchemaMigrator<GeographyDbContext> to GeographyModule, nor other registration that triggers DevelopmentSchemaMigrator at Host startup. Do not run MigrationRunner apply, migrations, SQL or DB updates.
D. Perform focused build/metadata tests for changed components without service startup or database contact. Verify migration order uniqueness and verify Geography IModuleSchemaMigrator still absent from Host-loaded GeographyModule. Assess any architecture test pinning order/registry; do not weaken guards.
E. Produce a concise report docs/planning/shipping/SHIPPING-GEOGRAPHY-MIGRATION-REGISTRY-045.md documenting actual modified paths, runtime non-activation proof, checks and one next recommendation. Distinguish configured metadata from applied schema; report elapsed time.

ALLOWED PATHS

src/backend/BuildingBlocks/Tooba.Persistence/ModuleSchemaMigrator.cs (single constant only)
src/backend/Host/Tooba.MigrationRunner/ModuleMigrationRegistry.cs (one Geography descriptor only)
src/backend/Host/Tooba.MigrationRunner/Tooba.MigrationRunner.csproj (only required Geography Infrastructure reference)
docs/planning/shipping/SHIPPING-GEOGRAPHY-MIGRATION-REGISTRY-045.md (new)
docs/ai/tasks/TB-SHIPPING-GEOGRAPHY-MIGRATION-REGISTRY-045.task.md (receipt only, NEVER STAGE)
docs/architecture/evidence/TB-SHIPPING-GEOGRAPHY-MIGRATION-REGISTRY-045/** (evidence only)

FORBIDDEN

No GeographyModule migrator registration, Program/DevelopmentSchemaMigrator change, Host service restart, any migration apply or PostgreSQL Tooba DB connection; no migration file edits, model/seed changes, shipping/Checkout/Order/AddressBook/Fulfillment/frontend changes; no extra projects, endpoints or administration; no manifest certification or architecture guard weakening.
No dummy/probe Bridge result POST; no auto next task.

ACCEPTANCE / STOP

PASS only if unique canonical constant and safe non-executing registry descriptor compile, no auto-migrator is registered, no Tooba database contacted, all changes limited to allowed paths, one report exists and task-owned commit(s) are pushed. If unsafe or not feasible, PARTIAL/BLOCKED with exact evidence; do not bypass.
git push origin main; git fetch origin; verify HEAD == origin/main. Preserve unrelated work. Worker PASS != Architect ACCEPT.

RESULT CONTRACT — BRIDGE-WAKE-V1
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-GEOGRAPHY-MIGRATION-REGISTRY-045
Parent-Task: TB-SHIPPING-GEOGRAPHY-MODULE-COMPOSITION-044
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-ARCHITECTURE-PLANNING
Mode: BOUNDED_GEOGRAPHY_MIGRATION_METADATA_NO_APPLY
Bridge-Task-Row: <actual row>
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 5e07617c04c4098efb7d1a4af99f5d6a619c8448
Base-SHA: <actual>
Code-SHA: <actual or NONE>
Evidence-SHA: <actual or NONE>
automaticNextImplementationTask: NONE

<preflight; changed paths; migration metadata and non-activation proof; focused checks; elapsed; push evidence; residual risks>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
