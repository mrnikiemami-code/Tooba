PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-PLATFORMPROBE-AMC-001-W1
Parent-Task: TB-TMAR-PLATFORMPROBE-AMC-001-W0
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — PlatformProbe
Mode: MIGRATE_TEST_FIXTURE_FIRST
Track: PLATFORMPROBE_AMC
Title: Extract PlatformProbe foundation behavior into Host.Tests fixture and remove test-source dependency on production probe types

ARCHITECT VERDICT

W0 is ARCHITECT-ACCEPTED for disposition:

REMOVE_FROM_PRODUCTION_REHOME_TEST_FIXTURE

Independent verification confirms:

PlatformProbe has no business capability
production business consumers = ZERO
current production role is composition/migration/outbox registration only
test value is real and concentrated in persistence/outbox/messaging foundation tests
production detach can preserve deployed platform_probe schema without DROP
current PostgreSQL integration tests create the probe schema with EnsureCreatedAsync; they do not require PlatformProbe EF migrations to be copied into the test fixture for parity

W1 must establish test parity FIRST so W2 can detach production safely.

CURRENT VERIFIED MAIN

Expected starting HEAD:
c14d3295e38417412c64ec0983183fc5fa2e1ff6

W0 evidence:
docs/architecture/evidence/TB-TMAR-PLATFORMPROBE-AMC-001-W0/

W0 decision:
REMOVE_FROM_PRODUCTION_REHOME_TEST_FIXTURE

W0 migration safety:
SAFE_TO_DETACH_KEEP_SCHEMA

IMPORTANT USER WORK STATE

Worker reported one prior untracked UserPreference result artifact.

Preserve all unrelated user work.
Do not delete, stage, rename, overwrite, or absorb unrelated untracked files.
A non-clean working tree caused only by pre-existing unrelated user work is acceptable if explicitly reported.

OBJECTIVE

Create a test-only PlatformProbe fixture under Host.Tests and repoint the foundation/integration tests that currently consume production PlatformProbe types.

After W1:

production Modules/PlatformProbe remains completely unchanged
Host production reference remains unchanged
runtime module composition remains unchanged
MigrationRunner remains unchanged
migration order remains unchanged
.slnx remains unchanged
but foundation/outbox/messaging tests no longer depend on Tooba.PlatformProbe.Infrastructure.* source types

This is parity-first extraction, NOT production detach.

CANONICAL TEST FIXTURE DESTINATION

Use:

src/backend/Host/Tooba.Host.Tests/Fixtures/PlatformProbe/

Keep it cohesive and shallow.

Preferred responsibilities:

ProbeEvents.cs

test-only domain event
test-only internal non-translated event
test-only integration event

TestPlatformProbeDbContext.cs

test-only record
test-only DbContext
schema remains exactly platform_probe
test-only record factory / persistence helper

TestPlatformProbeOutboxRegistration.cs

test-only IOutboxModuleRegistration
same translation/type-map semantics needed by foundation tests

Names may differ slightly if existing Host.Tests naming conventions require it, but:

namespace MUST be test-owned
no Tooba.PlatformProbe.Infrastructure.* namespace
no new production project
no new shared production layer
no copy into BuildingBlocks production

PARITY REQUIREMENTS

Fixture must preserve the foundation behavior currently proven by tests:

DbContext / persistence
schema = platform_probe
probe_records
module-local outbox_messages
record uses UUID v7
CreatedAt remains UTC/NodaTime semantics
optional external reference remains non-FK
domain events remain ignored by EF mapping
Outbox
ProbeRecordCreatedDomainEvent translates to one integration event
internal note event does NOT translate
event type remains stable:
platform_probe.record_created.v1
metadata/version semantics preserved
same-transaction outbox interceptor behavior preserved
Test database setup
existing tests currently use EnsureCreatedAsync
W1 must continue using the smallest test-only schema creation mechanism
DO NOT copy production EF migrations merely for ceremony
DO NOT call production PlatformProbeDbContext migrations
production migration files remain untouched for W1

TEST SOURCES TO REPOINT

At minimum inspect and repoint the production PlatformProbe type usage in:

src/backend/Host/Tooba.Host.Tests/OutboxFoundationTests.cs
src/backend/Host/Tooba.Host.Tests/OutboxPostgresTests.cs
src/backend/Host/Tooba.Host.Tests/OutboxTestSupport.cs
src/backend/Host/Tooba.Host.Tests/MassTransitPostgresTests.cs
src/backend/Host/Tooba.Host.Tests/PostgresIntegrationTests.cs
src/backend/Host/Tooba.Host.Tests/PersistenceFoundationTests.cs

Repoint only PlatformProbe-specific types.
Do not refactor unrelated test infrastructure.

ARCHITECTURE TESTS / PRODUCTION GRAPH GUARDS

Do NOT change these in W1 merely because they still refer to production PlatformProbe:

ArchitectureBoundaryTests
HostDevelopmentMigrationSeamGuardTests
guards asserting current production module composition/migration count

Those guards describe the still-current production state and must remain truthful until W2 detaches production.

If another test source directly consumes PlatformProbe only as a test fixture and belongs in this parity wave, include it and document why.
Do not silently alter production-graph guards.

SCOPE

Allowed:

new files under:
src/backend/Host/Tooba.Host.Tests/Fixtures/PlatformProbe/
the six listed test/support files
additional Host.Tests test-only files only if direct PlatformProbe test-consumer inventory proves they are necessary
focused test architecture guard proving test-source decoupling
W1 task/evidence

Forbidden:

src/backend/Modules/PlatformProbe/**
src/backend/Host/Tooba.Host/**
src/backend/MigrationRunner/**
src/backend/BuildingBlocks/** production code
src/backend/Tooba.slnx
ModuleSchemaMigrationOrder
module composition
migration descriptors
schema/migration deletion or edits
frontend
unrelated modules
production behavior changes

DURABLE W1 GUARD

Add a focused test/architecture guard proving:

For the test-consumer surface migrated in W1:

zero using Tooba.PlatformProbe.Infrastructure
zero direct type references to production:
PlatformProbeDbContext
PlatformProbePersistence
PlatformProbeOutboxRegistration
production probe event namespace

The guard must not require production PlatformProbe absence yet.
W2 owns production detachment.

Also prove fixture namespace/path is test-owned and exact.

VALIDATION

Use focused evidence only.

Required:

build Tooba.Host.Tests
run:
OutboxFoundationTests
PersistenceFoundationTests
W1 fixture/decoupling guard
run PostgreSQL/Testcontainers tests only if Docker is already available and the existing focused test command can execute without lengthy environment repair:
smallest relevant PostgresIntegrationTests
smallest relevant OutboxPostgresTests
smallest relevant MassTransitPostgresTests
If Docker is unavailable, preserve existing skip semantics and do NOT enter environment-debug loop.
search proof across migrated test-consumer files:
production PlatformProbe namespaces = ZERO
verify production diff under Modules/PlatformProbe = ZERO
verify Host production diff = ZERO
verify MigrationRunner diff = ZERO
verify migrations/schema source diff = ZERO

TEST DISCIPLINE

Tests are evidence, not navigation.

If one focused compile/test failure has one obvious local fixture parity cause, perform one bounded correction and rerun only that focused validation.
If resolution requires broader production changes or environment troubleshooting, STOP with INCOMPLETE.

Do not run the full repository suite.
Do not troubleshoot Docker beyond confirming availability.
Do not start W2 automatically.

EVIDENCE

Create:

docs/architecture/evidence/TB-TMAR-PLATFORMPROBE-AMC-001-W1/

At minimum:

fixture-extraction.md
test-consumer-parity.md
validation.md
physical-tree-after-w1.md

Persist exact task:
docs/ai/tasks/TB-TMAR-PLATFORMPROBE-AMC-001-W1.task.md

CANONICAL RESULT CONTRACT

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-PLATFORMPROBE-AMC-001-W1
Parent-Task: TB-TMAR-PLATFORMPROBE-AMC-001-W0
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary: <bounded summary>
Starting-HEAD-State: C14D3295 | DIVERGED
Test-Fixture-Path-State: HOST_TESTS_FIXTURES_PLATFORMPROBE | INVALID
Test-Fixture-Ownership-State: TEST_ONLY | PRODUCTION_LEAK
Fixture-Schema-State: PLATFORM_PROBE_PRESERVED | INVALID
Fixture-Outbox-Parity-State: PRESERVED | REGRESSION
Migrated-Test-Consumer-Production-Probe-Reference-State: ZERO | PRESENT
Production-PlatformProbe-Change-State: ZERO | CHANGED
Host-Production-Change-State: ZERO | CHANGED
MigrationRunner-Change-State: ZERO | CHANGED
Migration-Order-Change-State: ZERO | CHANGED
Schema-Migration-Source-Change-State: ZERO | CHANGED
Focused-Build-State: PASS | FAIL
Focused-Unit-Foundation-Tests-State: PASS | FAIL
Focused-Postgres-Test-State: PASS | SKIPPED_DOCKER_UNAVAILABLE | FAIL
Focused-MassTransit-Test-State: PASS | SKIPPED_DOCKER_UNAVAILABLE | FAIL
W1-Decoupling-Guard-State: PASS | FAIL
Frontend-State: UNTOUCHED | CHANGED
Evidence-State: COMPLETE | INCOMPLETE
Implementation-Commit-SHA: <sha>
HEAD-Equals-Origin-Main: YES | NO
Working-Tree-State: CLEAN | CLEAN_EXCEPT_PREEXISTING_USER_WORK | DIRTY_UNEXPECTED
User-Work-Preserved: YES | NO
Automatic-Next-Implementation-Task-State: NONE
Workflow-Stop-State: USER_REVIEW_PLATFORMPROBE_AMC_001_W1
STOP
END_TOOBA_WORKER_RESULT

STOP RULE

After result:
STOP completely.
Do not detach production PlatformProbe.
Do not modify Host production.
Do not modify MigrationRunner.
Do not start W2.
Wait for Architect review.

END_TOOBA_TASK
