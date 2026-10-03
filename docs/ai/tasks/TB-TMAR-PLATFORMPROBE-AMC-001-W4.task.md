PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-PLATFORMPROBE-AMC-001-W4
Parent-Task: TB-TMAR-PLATFORMPROBE-AMC-001-W3
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — PlatformProbe
Mode: CERTIFY_PRODUCTION_ABSENCE
Track: PLATFORMPROBE_AMC
Title: Certify PlatformProbe production absence and close canonical recovery state

ARCHITECT VERDICT

W3 is ARCHITECT-ACCEPTED.

Independent verification on current main confirms:

src/backend/Modules/PlatformProbe is physically ABSENT
PlatformProbe project is absent from Tooba.slnx
Host production contains no PlatformProbe project reference or composition registration
MigrationRunner contains no PlatformProbe descriptor/reference
active runtime migrator registration is ZERO
ModuleSchemaMigrationOrder.PlatformProbe is retired
order 13 is not reused
later migration orders remain unchanged (Promotion=12, Reviews=14, ProductQnA=15, ... Support=29)
Host.Tests fixture remains present under Fixtures/PlatformProbe
fixture namespace is test-owned and path-exact
deployed platform_probe schema was not dropped
SoT W3 state is READY_FOR_FINAL_ABSENCE_CERTIFICATION

PlatformProbe is NOT a business module and must NOT be certified as COMPLETE_REFERENCE_PATTERN.
The final certification target is production absence + test-fixture-only ownership.

CURRENT VERIFIED MAIN

Expected starting HEAD:
f6bf91b386150f7a17af5d7142bc23d5fa8f4c9a

W0:
REMOVE_FROM_PRODUCTION_REHOME_TEST_FIXTURE

W1:
test-fixture parity established

W2:
production runtime detached

W3:
production source/solution/order constant removed; structure handoff ready

OBJECTIVE

Perform final Certify pass for PlatformProbe and close recovery/SoT.

No production implementation should be required in W4 unless certification uncovers one concrete defect.

Expected final architectural state:

Business Capability: NONE
Production Module: ABSENT
Production Runtime Registration: ZERO
Host ProjectReference: ZERO
Host Composition: ZERO
MigrationRunner Descriptor: ZERO
Active Runtime Migrator: ZERO
Production PlatformProbe Source: ZERO
Solution Project: ZERO
ModuleSchemaMigrationOrder.PlatformProbe: ABSENT
Order 13 Reuse: ZERO
Test Fixture: PRESENT / TEST_ONLY
Deployed Schema: PRESERVED_NO_DROP
Final Classification: TEST_FIXTURE_ONLY
COMPLETE_REFERENCE_PATTERN Applicability: NOT_APPLICABLE
ARCH-COMPLETE-002 Module Certification Applicability: NOT_APPLICABLE
Final Certification: PRODUCTION_ABSENCE_CERTIFIED

CERTIFICATION SCOPE

Re-read and certify the entire touched PlatformProbe surface from W0-W3:

runtime composition
Host project dependencies
MigrationRunner
migration-order seam
physical Modules tree
solution tree
Host.Tests fixture ownership
migrated test consumers
durable W1/W2/W3 guards
current SoT
canonical foundation docs touched by W3
historical deployed-schema safety statement

Do not trust prior PASS labels without checking current files.

CERTIFICATION GATES

A. PRODUCTION ABSENCE

Verify:

no src/backend/Modules/PlatformProbe
no production .csproj named PlatformProbe
no production Tooba.PlatformProbe.* namespace
no Host production ProjectReference containing PlatformProbe
no Host composition registration containing PlatformProbe
no MigrationRunner descriptor/type/reference containing PlatformProbe
no production PlatformProbeDbContext
no production PlatformProbeOutboxRegistration
no production probe domain/integration events
no active AddModuleSchemaMigrator<PlatformProbeDbContext>

Historical docs/task/evidence references are allowed.

B. TEST FIXTURE OWNERSHIP

Verify:
src/backend/Host/Tooba.Host.Tests/Fixtures/PlatformProbe/

is:

PRESENT
test-owned namespace
not referenced by production projects
not moved into BuildingBlocks production
not a new production/shared architecture layer

Verify migrated test consumers remain independent of deleted production PlatformProbe namespaces/types.

C. MIGRATION SAFETY

Verify:

no ModuleSchemaMigrationOrder.PlatformProbe
no = 13; module order reuse
later order values were not renumbered
no DROP migration/script was introduced
no schema-destructive code was introduced
SoT explicitly preserves deployed schema as historical/orphaned-safe
historical EF migrations were removed only as production source cleanup after runtime detach; no claim that already-deployed database objects were removed

D. SOLUTION / PHYSICAL STRUCTURE

Verify:

.slnx contains no PlatformProbe project/folder
no stale production source copy
no archive/shim/disabled duplicate under another production path
fixture path/namespace exact
PlatformProbe absent from structure manifest certified module list
PlatformProbe absent from structureLock.certifiedModules
no fake five-project module scaffold created

E. RUNTIME BEHAVIOR / BOUNDARIES

Verify:

removing PlatformProbe did not change customer/admin/seller business behavior
generic persistence/outbox/messaging foundation abstractions remain independent of probe-specific production code
Host and MigrationRunner build without PlatformProbe production project
no production dependency from another business module to the deleted probe remains

F. GUARDS

Existing W1/W2/W3 guards must remain strict and truthful.

Add a final W4 certification guard that locks:

production source absence
Host runtime absence
MigrationRunner absence
solution absence
order constant/order-13 absence
fixture presence/test ownership
SoT final state
PlatformProbe NOT present in certifiedModules/manifest module list

Do not weaken earlier guards.

FINAL SOT / RECOVERY

Update platformProbeAmc001 to final canonical state.

Required fields/state at minimum:

task = TB-TMAR-PLATFORMPROBE-AMC-001
mode = ARCHITECT_DIRECT_AMSC
skills = Analyze,Migrate,Structure,Certify
disposition = REMOVE_FROM_PRODUCTION_REHOME_TEST_FIXTURE
w0State = ANALYZE_ACCEPTED
w1State = TEST_FIXTURE_PARITY_ESTABLISHED
w2State = PRODUCTION_RUNTIME_DETACHED
w3State = PRODUCTION_SOURCE_REMOVED_STRUCTURE_CLEAN
w4State = PRODUCTION_ABSENCE_CERTIFIED
businessCapability = NONE
ownershipState = TEST_FIXTURE_ONLY
completeReferencePatternApplicability = NOT_APPLICABLE
archComplete002Applicability = NOT_APPLICABLE
productionModule = ABSENT
hostProjectReference = ZERO
hostCompositionRegistration = ZERO
migrationRunnerDescriptor = ZERO
activeRuntimeMigrator = ZERO
productionSourceTree = ABSENT
solutionEntry = ABSENT
moduleSchemaMigrationOrderConstant = RETIRED_ORDER_13_NOT_REUSED
testFixture = Host.Tests/Fixtures/PlatformProbe
deployedSchema = PRESERVED_NO_DROP
finalCertification = PRODUCTION_ABSENCE_CERTIFIED
structureState = PRODUCTION_ABSENCE_STRUCTURE_CLEAN
structureCertified = false
implementationCommitW1 = 2d27fde609ca7b1c242bb9b887420df3c193b04c
implementationCommitW2 = f82a81ba461d2e152bd40ff886237072aa1a3a2d
implementationCommitW3 = f6bf91b386150f7a17af5d7142bc23d5fa8f4c9a
evidenceW0 = docs/architecture/evidence/TB-TMAR-PLATFORMPROBE-AMC-001-W0/
evidenceW1 = docs/architecture/evidence/TB-TMAR-PLATFORMPROBE-AMC-001-W1/
evidenceW2 = docs/architecture/evidence/TB-TMAR-PLATFORMPROBE-AMC-001-W2/
evidenceW3 = docs/architecture/evidence/TB-TMAR-PLATFORMPROBE-AMC-001-W3/
evidenceW4 = docs/architecture/evidence/TB-TMAR-PLATFORMPROBE-AMC-001-W4/
workflowStop = USER_REVIEW_PLATFORMPROBE_AMC_001_W4
automaticNextImplementationTask = NONE

If repository convention prefers additional explicit final checkpoint fields, add only minimal compatible fields.

Do NOT add PlatformProbe to:

structureLock.certifiedModules
tmar-module-structure-manifests.json as a certified module

This is absence certification, not module structure certification.

SCOPE

Allowed:

PlatformProbe W4 certification guard
minimal updates to prior PlatformProbe guards if a current-truth assertion is missing
docs/architecture/tmar-current-state.json
W4 evidence
exact task artifact
minimal canonical foundation doc correction only if W4 finds one stale present-tense claim

Forbidden:

production business code
Host production behavior changes
MigrationRunner behavior changes
BuildingBlocks production changes
test fixture semantic rewrite
other modules
.slnx changes unless correcting an unexpected stale PlatformProbe entry found during certification
schema/migration/database changes
frontend
adding PlatformProbe back as a module/project
adding PlatformProbe to structure-certified module lists
creating Domain/Application/Contracts/Endpoints
full repository test suite

VALIDATION

Focused only.

Required:

build Tooba.Host
build Tooba.MigrationRunner
build Tooba.Host.Tests
run focused:
PlatformProbeAmcW1FixtureGuardTests
PlatformProbeAmcW2DetachGuardTests
PlatformProbeAmcW3StructureGuardTests
new W4 final certification guard
HostDevelopmentMigrationSeamGuardTests
relevant structure-lock/manifest guard proving PlatformProbe is NOT certified as module
JSON parse SoT
search proof:
production Modules PlatformProbe = ZERO
production Tooba.PlatformProbe. namespace = ZERO
Host production PlatformProbe = ZERO
MigrationRunner PlatformProbe = ZERO
.slnx PlatformProbe = ZERO
migration order PlatformProbe = ZERO
order 13 reuse = ZERO
production references to Host.Tests fixture = ZERO
migrated test consumer references to deleted production namespace = ZERO
verify structureLock.certifiedModules contains PlatformProbe ZERO times
verify structure manifest contains PlatformProbe ZERO certified module entries

Do not run PostgreSQL/Testcontainers.
Do not run full repository tests.
Do not troubleshoot Docker.

CERTIFICATION RESULT

If every applicable gate passes, final result is:

PRODUCTION_ABSENCE_CERTIFIED
+
TEST_FIXTURE_ONLY

Do NOT label PlatformProbe:

COMPLETE_REFERENCE_PATTERN
ARCH-COMPLETE-002 STRUCTURE_CERTIFIED

Those labels are not applicable because no production business module remains.

If any production reference, stale physical copy, migration-order reuse, or production fixture leak remains:
Status must be INCOMPLETE with exact blocker.
Do not patch beyond one deterministic bounded certification repair.

EVIDENCE

Create:

docs/architecture/evidence/TB-TMAR-PLATFORMPROBE-AMC-001-W4/

At minimum:

final-certification.md
production-absence-proof.md
test-fixture-ownership-proof.md
migration-safety-final.md
validation.md

Persist exact task:
docs/ai/tasks/TB-TMAR-PLATFORMPROBE-AMC-001-W4.task.md

CANONICAL RESULT CONTRACT

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-PLATFORMPROBE-AMC-001-W4
Parent-Task: TB-TMAR-PLATFORMPROBE-AMC-001-W3
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary: <bounded summary>
Starting-HEAD-State: F6BF91B3 | DIVERGED
Business-Capability-State: NONE | INVALID
Final-Ownership-State: TEST_FIXTURE_ONLY | INVALID
Production-PlatformProbe-Source-State: ABSENT | PRESENT
Host-ProjectReference-State: ZERO | PRESENT
Host-Composition-State: ZERO | PRESENT
MigrationRunner-State: ZERO | PRESENT
Active-Runtime-Migrator-State: ZERO | PRESENT
Slnx-PlatformProbe-State: ZERO | PRESENT
ModuleSchemaMigrationOrder-PlatformProbe-State: ZERO | PRESENT
Migration-Order-13-Reuse-State: ZERO | REUSED
Deployed-Schema-Destructive-Change-State: ZERO | VIOLATION
Test-Fixture-State: PRESENT_TEST_ONLY | REGRESSION
Production-To-TestFixture-Dependency-State: ZERO | PRESENT
Migrated-Test-Consumer-Deleted-Namespace-State: ZERO | PRESENT
Physical-Stale-Copy-State: CLEAN | STALE_COPY
StructureLock-PlatformProbe-State: ABSENT | INVALID
StructureManifest-PlatformProbe-State: ABSENT | INVALID
Complete-Reference-Pattern-Applicability: NOT_APPLICABLE | INVALID
ArchComplete002-Applicability: NOT_APPLICABLE | INVALID
Final-Certification-State: PRODUCTION_ABSENCE_CERTIFIED | NOT_CERTIFIED
Focused-Build-State: PASS | FAIL
Focused-Tests-State: PASS | FAIL
W4-Cert-Guard-State: PASS | FAIL
Recovery-SoT-State: FINALIZED | STALE | CONFLICT
Evidence-State: COMPLETE | INCOMPLETE
Docs-Commit-SHA: <sha>
HEAD-Equals-Origin-Main: YES | NO
Working-Tree-State: CLEAN | CLEAN_EXCEPT_PREEXISTING_USER_WORK | DIRTY_UNEXPECTED
User-Work-Preserved: YES | NO
Automatic-Next-Implementation-Task-State: NONE
Workflow-Stop-State: USER_REVIEW_PLATFORMPROBE_AMC_001_W4
STOP
END_TOOBA_WORKER_RESULT

STOP RULE

After result:
STOP completely.
Do not start another module.
Do not create another PlatformProbe wave automatically.
Wait for Architect review.

END_TOOBA_TASK
