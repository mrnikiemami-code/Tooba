PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-GEOGRAPHY-INTEGRITY-MIGRATION-041
Parent-Task: TB-SHIPPING-GEOGRAPHY-COUNTRY-INTEGRITY-040
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ARCHITECTURE-PLANNING
Mode: BOUNDED_INERT_HIERARCHY_INTEGRITY_AND_INITIAL_MIGRATION_SOURCE
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 097720258721d72bca08ad44ec86c9cc4b6fd3fe
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD

ARCHITECT AUTHORITY

Architect ACCEPT of 040 is limited to cross-country composite FKs; 039 translation integrity remains accepted.
This replaces ALL earlier 041 task-file versions. Only the exact received Bridge artifact is operative; no historical file is an execution queue.
Goal: close the remaining pre-migration cycle-integrity concern; generate FIRST migration files ONLY if the selected enforcement is demonstrably safe. A PARTIAL/BLOCKED result is preferable to an unsupported PASS.
Permission is limited to changes inside Geography and ONE evidence report. Do NOT run or apply any migration. Geography remains inert and NOT ARCH-COMPLETE-002 certified.

PREFLIGHT — STOP ON CONFLICT

Act only after BRIDGE-WAKE and claim exactly one task on tooba-main. Do not idle-poll or self-authorize a subsequent task.
Read AGENTS.md, docs/ai/TOOBA-PIPELINE-PROTOCOL.md, .cursor/skills/tooba-backend-feature/SKILL.md, docs/planning/shipping/SHIPPING-GEOGRAPHY-BOUNDARY-ADR-036.md and reports 039-040, plus current Geography code and module migration conventions. Do not scan unrelated findings register.
git fetch origin, verify main and HEAD == origin/main == Expected-Base-SHA. Record all pre-existing dirty/untracked paths; preserve them untouched. On drift, unsafe conflict or uncertain prior work: RECOVERY_CONFLICT + STOP; never reset, force-push, stash, rebase or silently fast-forward.
Check proposed output paths for collisions before edits. Respect Contracts-only cross-module boundaries, capability-first shallow structure and path/namespace alignment.

MISSION — ONE BOUNDED SLICE
A. AdministrativeArea parent-chain consistency

Inspect current AdministrativeArea.Reparent semantics and GeographyDbContext. Reject self-parent and indirect ancestor cycles (A->B->...->A) for every authorized future write path.
Do not claim that ordinary EF CHECK or FK can recursively prevent cycles. Compare two concrete viable strategies: application/domain ancestor-walk with transaction serialization and a PostgreSQL enforcement mechanism (constraint trigger or equivalent). Select the smallest valid and concurrency-safe strategy, recording reasoning and transactional assumptions.
If PostgreSQL-level enforcement is chosen, migration source must include the real mechanism with safe recursive traversal and a concurrency serialization rule; prove how concurrent reparenting cannot create a cycle. If correctness cannot be established WITHOUT accessing a live database in 30 minutes, STOP as PARTIAL and report precise blocker; DO NOT generate an unsafe migration.
Keep country-consistent composite FKs, nullable roots, four translation FK tables, codes and D8 legacy compatibility unchanged.

B. Initial migration SOURCE ONLY, conditional on A

Once A is convincingly closed, generate the FIRST EF Core Geography migration and model snapshot via the module's existing EF design-time conventions. Review migration operations and model-derived SQL for ONLY geography schema, four translation tables, composite FKs, unique constraints, outbox and the enforceable cycle strategy.
No Host composition, schema migration ordering, runtime registration, DB connection, database update, seeds or regional coverage defaults. If an EF command needs additional Host changes or a real DB, STOP and report rather than expanding scope.

C. Bounded verification and evidence

Build only changed Geography project(s); inspect EF model metadata and generated SQL locally without a DB connection. Add a minimal targeted deterministic proof/test of non-cycle, self-cycle and multi-node cycle where meaningful. Never label static assertions as proof of PostgreSQL concurrent behavior.
Create exactly one report: docs/planning/shipping/SHIPPING-GEOGRAPHY-INTEGRITY-MIGRATION-041.md, with path/line evidence, strategy, transaction/race reasoning, migration contents or explicit STOP rationale, actual elapsed time and remaining risks.
Commit and push ONLY allowed paths if validation supports a truthful PASS or clearly bounded source-only partial report; fetch after push and report actual SHA. If a partial state is unsafe to commit, leave it uncommitted and STOP with evidence.

ALLOWED PATHS

src/backend/Modules/Geography/Tooba.Geography.Domain/** (only if necessary for cycle invariant)
src/backend/Modules/Geography/Tooba.Geography.Infrastructure/Persistence/GeographyDbContext.cs (only if necessary)
src/backend/Modules/Geography/Tooba.Geography.Infrastructure/Persistence/Migrations/** (new first migration and snapshot only)
src/backend/Modules/Geography/Tooba.Geography.Infrastructure/** (strictly necessary module-owned cycle enforcement support, no DI)
docs/planning/shipping/SHIPPING-GEOGRAPHY-INTEGRITY-MIGRATION-041.md (new evidence report)
docs/ai/tasks/TB-SHIPPING-GEOGRAPHY-INTEGRITY-MIGRATION-041.task.md (received task receipt only; never replay)
docs/architecture/evidence/TB-SHIPPING-GEOGRAPHY-INTEGRITY-MIGRATION-041/** (task evidence only, if required by Bridge)

FORBIDDEN

NO migration APPLY, live/staging DB connection, seed/import, deploy, Host/DI/module wiring, new Endpoint, frontend, Checkout, AddressBook, Order, Fulfillment, Payment, Settlement, Catalog, Offer, Shipping pricing or coverage changes.
NO edit to StorefrontIranGeography.Provinces, old geography route/DTO, pinned Host guard, certified manifest or architecture locks. No unrelated files or technical-axis root folders.
NO weakening tests/guards, broad test suites, runtime restart, background work, automatic next Task, Results-API dummy/probe POST or continuous idle polling.

ACCEPTANCE / STOP

PASS only when cycle integrity including concurrency strategy is defensible, generated migration/model snapshot are limited to geography, targeted build/proofs are successful, and no DB has been touched.
If cycle integrity or migration safety remains unproven: Status PARTIAL or BLOCKED, specify missing proof and STOP_FOR_ARCHITECT. Never imply migration is ready merely because it compiles.
Push only task-owned validated files, record exact files/line evidence, Base-SHA, Code-SHA, Evidence-SHA, validation commands/results, real elapsed minutes and exclusions. Worker PASS != Architect ACCEPT.

RESULT CONTRACT — BRIDGE-WAKE-V1
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-GEOGRAPHY-INTEGRITY-MIGRATION-041
Parent-Task: TB-SHIPPING-GEOGRAPHY-COUNTRY-INTEGRITY-040
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-ARCHITECTURE-PLANNING
Mode: BOUNDED_INERT_HIERARCHY_INTEGRITY_AND_INITIAL_MIGRATION_SOURCE
Bridge-Task-Row: <actual row>
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 097720258721d72bca08ad44ec86c9cc4b6fd3fe
Base-SHA: <actual>
Code-SHA: <actual or NONE>
Evidence-SHA: <actual or NONE>
automaticNextImplementationTask: NONE

<preflight; exact changed paths; enforcement and race proof; migrations or blocker; build/SQL verification; DB non-actions; elapsed minutes; push evidence; remaining risks>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
