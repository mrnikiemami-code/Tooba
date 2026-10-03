PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-PLATFORMPROBE-AMC-001-W3
Parent-Task: TB-TMAR-PLATFORMPROBE-AMC-001-W2
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — PlatformProbe
Mode: STRUCTURE_CLEANUP
Track: PLATFORMPROBE_AMC
Title: Remove stale production PlatformProbe source project and solution entry after runtime detach

ARCHITECT VERDICT

W2 is ARCHITECT-ACCEPTED.

Independent review confirms:

Host ProjectReference to PlatformProbe = ZERO
Host composition PlatformProbe registration = ZERO
MigrationRunner PlatformProbe descriptor = ZERO
active runtime PlatformProbe migrator = ZERO
deployed platform_probe schema was not modified
Host.Tests fixture remains test-owned and independent
production source tree remains only as stale detached source

W3 now owns structural cleanup.

CURRENT VERIFIED MAIN

Expected starting HEAD:
f82a81ba461d2e152bd40ff886237072aa1a3a2d

Current detached-but-stale production tree:

src/backend/Modules/PlatformProbe/Tooba.PlatformProbe.Infrastructure/

contains:

PlatformProbeModule.cs
PlatformProbeOutboxRegistration.cs
Events/ProbeEvents.cs
Persistence/PlatformProbeDbContext.cs
Persistence/Migrations/*
Tooba.PlatformProbe.Infrastructure.csproj

Current solution still contains:
Modules/PlatformProbe/Tooba.PlatformProbe.Infrastructure/Tooba.PlatformProbe.Infrastructure.csproj

Current migration-order seam still contains:
ModuleSchemaMigrationOrder.PlatformProbe = 13

No active production runtime registration uses any of the above.

OBJECTIVE

Complete physical/solution cleanup:

remove stale production PlatformProbe project/tree
remove stale .slnx project entry
retire the now-unused PlatformProbe migration-order constant
preserve all later migration-order numeric values exactly as-is
preserve deployed database schema/table state
preserve Host.Tests fixture
preserve historical migration identity in evidence/SoT, not as dead production source

This wave is structural cleanup, not final certification.

IMPORTANT MIGRATION SAFETY RULE

Removing source files MUST NOT imply database destruction.

Do NOT:

create DROP migration
connect to database
drop platform_probe
alter deployed EF history
renumber later module migration constants
reuse numeric order 13
migrate another module into order 13

Order 13 becomes a historical gap.

SCOPE

Allowed:

delete src/backend/Modules/PlatformProbe/**
edit src/backend/Tooba.slnx
edit src/backend/BuildingBlocks/Tooba.Persistence/ModuleSchemaMigrator.cs only to remove the dead PlatformProbe constant/comment
update PlatformProbe W1/W2/W3 guards so current truth is source ABSENT
minimal architecture guards that reference production PlatformProbe source presence
docs/architecture/tmar-current-state.json
W3 evidence
exact task artifact
foundation docs only if they currently describe PlatformProbe as an active production sample rather than historical foundation proof

Forbidden:

Host production behavior changes
MigrationRunner behavior changes beyond already-detached state
test fixture deletion or semantic rewrite
other modules
frontend
schema/database changes
renumbering migration orders
creating archive/source copies under another production path
creating a new project to preserve old PlatformProbe source
copying old EF migrations into test fixture
touching business routes/use-cases

REQUIRED STRUCTURE CLEANUP

A. REMOVE PRODUCTION PROJECT/TREE

Delete the entire:

src/backend/Modules/PlatformProbe/

After W3:

production PlatformProbe project count = ZERO
production PlatformProbe .csproj = ZERO
production PlatformProbe DbContext = ZERO
production PlatformProbe migrations = ZERO
production PlatformProbe events/outbox registration/module root = ZERO

Do not leave aliases, shims, archived .cs, renamed copies, or disabled project files.

Historical migration names/SHAs remain recoverable from git history and W0-W3 evidence; no live source archive is required.

B. SOLUTION CLEANUP

Remove the PlatformProbe project entry from:

src/backend/Tooba.slnx

Do not create /Modules/PlatformProbe/.
Final production solution state for PlatformProbe is ABSENT.

C. MIGRATION ORDER CLEANUP

In:

src/backend/BuildingBlocks/Tooba.Persistence/ModuleSchemaMigrator.cs

Remove only:

PlatformProbe migration-order comment
public const int PlatformProbe = 13;

Do NOT renumber values after 13.
The numeric gap remains intentional historical space.

Add/update focused guard to prove:

no PlatformProbe constant
later known constants retain their pre-W3 numeric values
no order 13 reuse

D. UPDATE PREVIOUS PLATFORMPROBE GUARDS

W1/W2 guards currently assert production source is preserved for deferred W3 cleanup.

Update them truthfully:

W1 fixture guard: fixture remains and migrated test consumers stay decoupled; remove expectation that production source exists
W2 detach guard: runtime absence remains; production source now must be absent
add W3 structure guard proving source + solution + order constant absence

Do not weaken runtime-detach guarantees from W2.

E. DOCUMENT CURRENT STRUCTURE STATE

Update platformProbeAmc001 SoT minimally:

Expected W3 state:

w3State = PRODUCTION_SOURCE_REMOVED_STRUCTURE_CLEAN
sourceTree = ABSENT
solutionEntry = ABSENT
moduleSchemaMigrationOrderConstant = RETIRED_ORDER_13_NOT_REUSED
testFixture = Host.Tests/Fixtures/PlatformProbe
deployedSchema = PRESERVED_NO_DROP
structureState = READY_FOR_FINAL_ABSENCE_CERTIFICATION
structureCertified = false
workflowStop = USER_REVIEW_PLATFORMPROBE_AMC_001_W3
automaticNextImplementationTask = NONE
evidenceW3 = ...

Do NOT mark final certification yet.
W4 owns final absence certification/recovery closure.

F. FOUNDATION DOCS

Inspect only the canonical foundation docs that describe PlatformProbe as a disposable sample.

If wording implies it is still an active production module, minimally change present-tense wording to historical status:

PlatformProbe was the original disposable foundation proof
production module has been retired
test-owned fixture now carries foundation proof
deployed schema may remain intentionally orphaned

Do not rewrite historical task/evidence files.

STRUCTURE CLASSIFICATION AFTER W3

Expected:

Production PlatformProbe module: ABSENT
Solution Explorer PlatformProbe project: ABSENT
Test fixture: PRESENT / TEST_ONLY
Stale physical production copy: ZERO
Path-Namespace for fixture: EXACT
BuildingBlocks module-specific PlatformProbe code: ZERO
Host production PlatformProbe code/reference: ZERO
MigrationRunner PlatformProbe code/reference: ZERO
Structure-Handoff-State: READY_FOR_FINAL_ABSENCE_CERTIFICATION

VALIDATION

Focused only.

Required:

build Tooba.Host
build Tooba.MigrationRunner
build Tooba.Host.Tests
run focused:
W1 fixture guard
W2 detach guard
W3 structure cleanup guard
HostDevelopmentMigrationSeamGuardTests
relevant complete-reference structure guard if it enumerates physical modules
search proof:
src/backend/Modules/PlatformProbe = ABSENT
PlatformProbe project in .slnx = ZERO
ModuleSchemaMigrationOrder.PlatformProbe = ZERO
order 13 reuse = ZERO
Host production PlatformProbe refs = ZERO
MigrationRunner PlatformProbe refs = ZERO
migrated test consumers production PlatformProbe refs = ZERO
JSON parse SoT
test fixture remains present

Do not run PostgreSQL/Testcontainers.
Do not run full repository suite.
Do not troubleshoot Docker.

TEST DISCIPLINE

Tests are evidence, not navigation.

If one focused failure is caused by a stale assertion tied directly to the W3 deletion, repair only that assertion.
If failure requires broader production design or another module change, STOP with INCOMPLETE.

EVIDENCE

Create:

docs/architecture/evidence/TB-TMAR-PLATFORMPROBE-AMC-001-W3/

At minimum:

structure-cleanup.md
physical-tree-after-w3.md
migration-order-retirement.md
validation.md

Persist exact task:
docs/ai/tasks/TB-TMAR-PLATFORMPROBE-AMC-001-W3.task.md

CANONICAL RESULT CONTRACT

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-PLATFORMPROBE-AMC-001-W3
Parent-Task: TB-TMAR-PLATFORMPROBE-AMC-001-W2
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary: <bounded summary>
Starting-HEAD-State: F82A81BA | DIVERGED
Production-PlatformProbe-Source-State: ABSENT | PRESENT
Production-PlatformProbe-Project-State: ABSENT | PRESENT
Slnx-PlatformProbe-State: ABSENT | PRESENT
ModuleSchemaMigrationOrder-PlatformProbe-State: RETIRED | PRESENT
Migration-Order-13-Reuse-State: ZERO | REUSED
Later-Migration-Order-Renumber-State: ZERO | CHANGED
Deployed-Schema-Destructive-Change-State: ZERO | VIOLATION
Host-Production-PlatformProbe-State: ZERO | PRESENT
MigrationRunner-PlatformProbe-State: ZERO | PRESENT
Test-Fixture-State: PRESENT_TEST_ONLY | REGRESSION
Migrated-Test-Consumer-Production-Probe-Reference-State: ZERO | PRESENT
Physical-Stale-Copy-State: CLEAN | STALE_COPY
Fixture-Path-Namespace-State: EXACT | MISMATCH
Structure-Handoff-State: READY_FOR_FINAL_ABSENCE_CERTIFICATION | REPAIR_REQUIRED
Focused-Build-State: PASS | FAIL
Focused-Tests-State: PASS | FAIL
W3-Structure-Guard-State: PASS | FAIL
SoT-W3-State: UPDATED | STALE
Frontend-State: UNTOUCHED | CHANGED
Evidence-State: COMPLETE | INCOMPLETE
Implementation-Commit-SHA: <sha>
HEAD-Equals-Origin-Main: YES | NO
Working-Tree-State: CLEAN | CLEAN_EXCEPT_PREEXISTING_USER_WORK | DIRTY_UNEXPECTED
User-Work-Preserved: YES | NO
Automatic-Next-Implementation-Task-State: NONE
Workflow-Stop-State: USER_REVIEW_PLATFORMPROBE_AMC_001_W3
STOP
END_TOOBA_WORKER_RESULT

STOP RULE

After result:
STOP completely.
Do not perform final W4 certification.
Do not start another module.
Wait for Architect review.

END_TOOBA_TASK
