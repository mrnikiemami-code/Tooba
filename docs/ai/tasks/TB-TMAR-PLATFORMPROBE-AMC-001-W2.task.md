PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-PLATFORMPROBE-AMC-001-W2
Parent-Task: TB-TMAR-PLATFORMPROBE-AMC-001-W1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — PlatformProbe
Mode: MIGRATE_PRODUCTION_DETACH
Track: PLATFORMPROBE_AMC
Title: Detach PlatformProbe from production Host composition and migration runtime while preserving deployed schema and test fixture parity

ARCHITECT VERDICT

W1 is ARCHITECT-ACCEPTED.

Independent review confirms:

Host.Tests fixture exists under Fixtures/PlatformProbe
migrated persistence/outbox/messaging tests consume test-owned fixture types
production Modules/PlatformProbe remained unchanged
Host production remained unchanged
MigrationRunner remained unchanged
current production module graph still includes PlatformProbe and is now safe to detach
deployed platform_probe schema must be preserved; no DROP
test parity no longer requires production PlatformProbe types

CURRENT VERIFIED MAIN

Expected starting HEAD:
2d27fde609ca7b1c242bb9b887420df3c193b04c

W0:
REMOVE_FROM_PRODUCTION_REHOME_TEST_FIXTURE

W1:
test fixture parity established; migrated test consumers no longer depend on production PlatformProbe types.

IMPORTANT USER WORK STATE

Previous Worker reported unrelated pre-existing user work.
Preserve it exactly.
Do not delete, stage, overwrite, rename, or absorb unrelated files.

OBJECTIVE

Remove PlatformProbe from the ACTIVE PRODUCTION RUNTIME GRAPH while keeping its source project temporarily present for W3 structure/stale-copy cleanup.

After W2:

Host production no longer references PlatformProbe project
Host module composition no longer instantiates PlatformProbeModule
production runtime no longer registers PlatformProbe DbContext/migrator/outbox registration
MigrationRunner no longer includes PlatformProbe descriptor
application startup no longer loads PlatformProbe as a module
deployed schema/table state is left untouched
source migrations are left untouched
PlatformProbe source project remains on disk and in solution until W3
test fixture remains intact
production business behavior remains unchanged

THIS WAVE IS DETACH, NOT DELETE.

VERIFIED CURRENT PRODUCTION REFERENCES

Host csproj:
src/backend/Host/Tooba.Host/Tooba.Host.csproj
currently contains direct ProjectReference to:
Modules/PlatformProbe/Tooba.PlatformProbe.Infrastructure

Host composition:
src/backend/Host/Tooba.Host/Composition/ToobaModuleComposition.cs
currently:

imports Tooba.PlatformProbe.Infrastructure
includes new PlatformProbeModule()

MigrationRunner:
src/backend/Host/Tooba.MigrationRunner/ModuleMigrationRegistry.cs
currently:

imports Tooba.PlatformProbe.Infrastructure.Persistence
contains
Descriptor<PlatformProbeDbContext>("PlatformProbe", PlatformProbeDbContext.Schema)

Current architecture guards still assert those production references and MUST be updated truthfully in this wave.

SCOPE

Allowed production files:

src/backend/Host/Tooba.Host/Tooba.Host.csproj
src/backend/Host/Tooba.Host/Composition/ToobaModuleComposition.cs
src/backend/Host/Tooba.MigrationRunner/ModuleMigrationRegistry.cs

Allowed tests/guards:

src/backend/Host/Tooba.Host.Tests/ArchitectureBoundaryTests.cs
src/backend/Host/Tooba.Host.Tests/Architecture/HostDevelopmentMigrationSeamGuardTests.cs
focused MigrationRunner/Host architecture tests if they explicitly inventory PlatformProbe
one new focused W2 absence/detach guard if useful

Allowed docs/evidence:

W2 evidence
exact task artifact
minimal SoT update only if current migration/runtime inventory must remain truthful after detach

Forbidden:

deleting src/backend/Modules/PlatformProbe/**
moving PlatformProbe source
editing PlatformProbe migrations
editing PlatformProbe source code
editing test fixture behavior unless a compile-only namespace/reference fix is strictly required
editing .slnx
changing ModuleSchemaMigrationOrder.PlatformProbe
DROP schema/table
database destructive migration
frontend
unrelated modules
broad Host refactor

REQUIRED DETACH

A. HOST PROJECT REFERENCE

Remove the direct PlatformProbe ProjectReference from:
src/backend/Host/Tooba.Host/Tooba.Host.csproj

No replacement production reference.

B. HOST MODULE COMPOSITION

In:
src/backend/Host/Tooba.Host/Composition/ToobaModuleComposition.cs

Remove:

using Tooba.PlatformProbe.Infrastructure
new PlatformProbeModule()

Do not change ordering of unrelated modules.

After W2, PlatformProbe-specific production module registration must be ZERO.

C. MIGRATION RUNNER

In:
src/backend/Host/Tooba.MigrationRunner/ModuleMigrationRegistry.cs

Remove:

PlatformProbe persistence using
PlatformProbe descriptor

Preserve every other descriptor and its relative order.

No fallback/shim/conditional registration.
No hidden reflection loading of PlatformProbe.

D. RUNTIME MIGRATION INVENTORY / GUARDS

Update architecture guards that currently assert PlatformProbe exists in production runtime.

At minimum:

ArchitectureBoundaryTests

stop requiring PlatformProbeModule in ToobaModuleComposition
preferably assert PlatformProbeModule is absent from production composition
remove obsolete using/import

HostDevelopmentMigrationSeamGuardTests

remove PlatformProbe production module file from expected composition inventory
update expected migrator count/inventory from current value to exact post-detach value
remove PlatformProbeDbContext from active runtime migrator expectations
preserve all other module assertions

If another focused migration/runtime guard explicitly inventories PlatformProbe, update it minimally and document it.

Do not weaken guards into generic/non-exact assertions.

E. PRESERVE DEPLOYED SCHEMA

W2 MUST NOT:

run/drop platform_probe
delete migration source
add compensating DROP migration
alter migration history
recycle order number 13
rename another module to PlatformProbe
change MessagingOptionsValidator reserved schema behavior

ModuleSchemaMigrationOrder.PlatformProbe = 13 may remain as a historical reserved constant until W3 determines the clean structural retirement strategy.

F. TEST FIXTURE MUST REMAIN TEST-ONLY

W1 fixture continues to provide persistence/outbox/messaging test semantics.

Do not make Host production reference Host.Tests.
Do not move fixture to production.
Do not reintroduce production PlatformProbe types into migrated tests.

DURABLE W2 DETACH GUARD

Add or strengthen a focused guard proving ALL:

Host csproj has zero PlatformProbe ProjectReference.
ToobaModuleComposition.cs has:
zero PlatformProbeModule
zero Tooba.PlatformProbe using/reference.
MigrationRunner registry has:
zero PlatformProbeDbContext
zero "PlatformProbe" descriptor.
production runtime source under Host/Tooba.Host and Host/Tooba.MigrationRunner contains no PlatformProbe-specific registration.
test fixture path still exists.
production source module still exists for deferred W3 cleanup; W2 is detach-only.

Do not globally ban historical docs or task evidence.

VALIDATION

Focused only.

Required:

build Tooba.Host
build Tooba.MigrationRunner
build Tooba.Host.Tests
run focused:
ArchitectureBoundaryTests
HostDevelopmentMigrationSeamGuardTests
W1 fixture guard
W2 detach guard
run smallest MigrationRunner registry test(s) if existing
search proof:
Host csproj PlatformProbe ref = ZERO
Host composition PlatformProbe = ZERO
MigrationRunner PlatformProbe descriptor = ZERO
W1 migrated test consumers production PlatformProbe refs = ZERO
diff proof:
Modules/PlatformProbe/** = ZERO changes
.slnx = ZERO changes
PlatformProbe migrations = ZERO changes
ModuleSchemaMigrationOrder.PlatformProbe unchanged
frontend = ZERO changes

Do NOT run full repository test suite.
Do NOT run PostgreSQL/Testcontainers integration tests in W2; W1 already established fixture parity and Docker was unavailable.
Do NOT troubleshoot Docker.

TEST DISCIPLINE

Tests are evidence, not navigation.

If one focused build/guard failure has one obvious deterministic detach-inventory cause, perform one bounded correction and rerun only that check.
If failure requires broad refactor or production schema work, STOP with INCOMPLETE.

EVIDENCE

Create:
docs/architecture/evidence/TB-TMAR-PLATFORMPROBE-AMC-001-W2/

At minimum:

production-detach.md
migration-runtime-detach.md
validation.md
runtime-reference-after-w2.md

Persist exact task:
docs/ai/tasks/TB-TMAR-PLATFORMPROBE-AMC-001-W2.task.md

SOFT RECOVERY UPDATE

If tmar-current-state.json contains an exact active migration/module count or migrationOrder list that becomes false after W2, update only that exact current-state surface.

Do not prematurely certify PlatformProbe absence.
Do not add final W4 certification state.
Do not claim source project removed.
Do not claim Structure PASS yet.

CANONICAL RESULT CONTRACT

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-PLATFORMPROBE-AMC-001-W2
Parent-Task: TB-TMAR-PLATFORMPROBE-AMC-001-W1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary: <bounded summary>
Starting-HEAD-State: 2D27FDE6 | DIVERGED
Host-ProjectReference-State: ZERO | PRESENT
Host-Composition-PlatformProbe-State: ZERO | PRESENT
Runtime-PlatformProbe-Module-Registration-State: ZERO | PRESENT
MigrationRunner-PlatformProbe-Descriptor-State: ZERO | PRESENT
Active-PlatformProbe-Migrator-State: ZERO | PRESENT
Deployed-Schema-Destructive-Change-State: ZERO | VIOLATION
PlatformProbe-Source-Tree-State: PRESERVED_FOR_W3 | CHANGED
PlatformProbe-Migration-Source-State: PRESERVED | CHANGED
ModuleSchemaMigrationOrder-PlatformProbe-State: RESERVED_UNCHANGED | CHANGED
Slnx-State: UNCHANGED | CHANGED
Test-Fixture-State: PRESERVED | REGRESSION
Migrated-Test-Consumer-Production-Probe-Reference-State: ZERO | PRESENT
Focused-Host-Build-State: PASS | FAIL
Focused-MigrationRunner-Build-State: PASS | FAIL
Focused-Tests-State: PASS | FAIL
W2-Detach-Guard-State: PASS | FAIL
SoT-Current-Runtime-Inventory-State: ACCURATE | STALE | NOT_APPLICABLE
Frontend-State: UNTOUCHED | CHANGED
Evidence-State: COMPLETE | INCOMPLETE
Implementation-Commit-SHA: <sha>
HEAD-Equals-Origin-Main: YES | NO
Working-Tree-State: CLEAN | CLEAN_EXCEPT_PREEXISTING_USER_WORK | DIRTY_UNEXPECTED
User-Work-Preserved: YES | NO
Automatic-Next-Implementation-Task-State: NONE
Workflow-Stop-State: USER_REVIEW_PLATFORMPROBE_AMC_001_W2
STOP
END_TOOBA_WORKER_RESULT

STOP RULE

After result:
STOP completely.
Do not delete or move PlatformProbe source project.
Do not edit .slnx.
Do not retire migration order constant.
Do not start W3.
Wait for Architect review.

END_TOOBA_TASK
