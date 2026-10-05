PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-IDENTITY-AMSC-001-W3-R1
Parent-Task: TB-TMAR-IDENTITY-AMSC-001-W3
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: RECOVERY_SOT_RECONCILIATION_ONLY
Title: Restore Identity AMC historical truth and reconcile AMSC W3 recovery checkpoint

ARCHITECT VERDICT
Identity production architecture/structure and W3 certification are accepted.
Do NOT reopen Identity production code.

STARTING HEAD
e6d467740dc0305c665d72712fbc5f115ba4b4bf

ACCEPTED AMSC LINEAGE
W0 Analyze 91eec1fd363ba25a6b92377709f63a789c1b0942
W1 Migrate 93a6b19268e8fb1133d0b6e3c7b1d4bbdfd5cfc5
W2 Structure 7c79f8c6bd5f373b7f9c9cd334a9f19be22af0f1
W3 Certify e6d467740dc0305c665d72712fbc5f115ba4b4bf

ACCEPTED FINAL
COMPLETE_REFERENCE_PATTERN
ARCH-COMPLETE-002 STRUCTURE_CERTIFIED
13 endpoint-reachable requests
9 VALIDATOR_REQUIRED + 4 NO_VALIDATOR_REQUIRED
13 module-owned routes
HOST_IDENTITY_HTTP_BUSINESS_PERSISTENCE_AUTHORITY_ZERO
GLOBAL_HOST_AUTH_PLATFORM_BOUNDARY_PRESERVED
CONTRACTS_ONLY
FOREIGN_APP_INFRA_DOMAIN_COUPLING_ZERO
CROSS_MODULE_JOIN_ZERO
CROSS_MODULE_PERSISTENCE_ZERO
MICROSERVICE_EXTRACTABLE_TRUE
BLOCKING_RESIDUAL_DEBT_ZERO

DEFECT
Recovery/SoT contains three bounded historical-truth defects.

The pre-existing historical record identityAmc001 was rewritten by W3.

At W3 starting HEAD 7c79f8c6, the historical AMC record truth was:

task = TB-TMAR-IDENTITY-AMC-001
structureState = READY_FOR_CERTIFY
validatorCoverage = COMPLETE_6_OF_6_REQUIRED_PRESENT_7_NO_VALIDATOR_REQUIRED
validatorRequiredCount = 6
noValidatorRequiredCount = 7
implementationCommit = aafd14e0...
docsStampCommit = c7e473cd...

At W3 commit e6d46774, that same historical AMC record was mutated to:

structureState = CERTIFIED
validatorCoverage = COMPLETE_9_OF_9_REQUIRED_PRESENT_4_NO_VALIDATOR_REQUIRED
validatorRequiredCount = 9
noValidatorRequiredCount = 4
AMSC-specific current fields were appended into the AMC record.

This rewrites historical AMC truth. The current AMSC certification already has its own
identityModuleAmsc001W0..W3 records and must remain the authoritative current module state.

Master Recovery records the AMSC W0→W3 lineage but does NOT explicitly record final W3 SHA:
e6d46774

The older Identity AMC lineage is described as historical evidence but is not explicitly locked with:
HISTORICAL / SUPERSEDED FOR CURRENT IDENTITY MODULE RECOVERY

GOAL
Perform one bounded Recovery/SoT-only reconciliation:

restore identityAmc001 to its truthful pre-W3 historical values;
do NOT erase the historical AMC record;
make the AMSC W0→W3 records the authoritative current Identity module state;
explicitly mark the old AMC lineage HISTORICAL / SUPERSEDED FOR CURRENT IDENTITY MODULE RECOVERY;
record final W3 SHA e6d46774 in Master Recovery;
add an additive W3-R1 reconciliation record;
preserve global Host root checkpoint;
preserve automaticNextImplementationTask = NONE;
zero production change.

PRECHECK

Verify HEAD == origin/main.
Verify HEAD is exactly e6d467740dc0305c665d72712fbc5f115ba4b4bf.
Re-read:
.cursor/skills/tooba-architecture-certify/SKILL.md
docs/architecture/tmar-current-state.json at current HEAD
docs/architecture/tmar-current-state.json at 7c79f8c6
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
docs/architecture/tmar-module-structure-manifests.json
docs/architecture/evidence/TB-TMAR-IDENTITY-AMSC-001-W3/certification.md
src/backend/Host/Tooba.Host.Tests/Architecture/IdentityModuleAmsc001W3CertGuardTests.cs
src/backend/Host/Tooba.Host.Tests/Architecture/IdentityModuleAmcW5CertGuardTests.cs
Diff identityAmc001 between 7c79f8c6 and e6d46774.
If repository truth materially differs: RECOVERY_CONFLICT + STOP.

GLOBAL RECOVERY LOCK
Do NOT change repository-global Host recovery authority.

Preserve:

lastAcceptedTask
lastAcceptedCommit
latestAcceptedImplementationWave
currentHostCheckpoint
nextHostFolder
repository-global workflowStop
repository-global automaticNextImplementationTask

ALLOWED FILES

docs/architecture/tmar-current-state.json
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
docs/architecture/evidence/TB-TMAR-IDENTITY-AMSC-001-W3/certification.md ONLY if needed to clarify historical-vs-current truth
existing Identity AMC/W3 cert guards ONLY if needed to lock this exact recovery truth
docs/architecture/evidence/TB-TMAR-IDENTITY-AMSC-001-W3-R1/*
docs/ai/tasks/TB-TMAR-IDENTITY-AMSC-001-W3-R1.task.md

FORBIDDEN
Any Identity production file; any csproj; any other module production file; Host production; frontend; routes; DTOs; handlers; validators; error-code values; descriptors; resx; DI; solution/project structure; schema; migrations; manifest structural fields; source-size baselines; guard weakening; baseline widening; unrelated recovery cleanup; full solution tests; W4; next module.

IMPLEMENTATION

Restore historical identityAmc001
Restore the fields that W3 incorrectly converted from historical AMC truth to current AMSC truth.

At minimum restore:

structureState = READY_FOR_CERTIFY
validatorCoverage = COMPLETE_6_OF_6_REQUIRED_PRESENT_7_NO_VALIDATOR_REQUIRED
validatorRequiredCount = 6
noValidatorRequiredCount = 7

Preserve its original AMC task identity, implementationCommit, docsStampCommit, evidence paths and all other historical AMC fields exactly unless direct diff proves another W3-added AMSC field must be removed.

Remove AMSC-current fields added into the historical AMC record if they did not exist at 7c79f8c6, including:

amsc001Certified
amsc001CertificationNote
amsc001EvidenceRoot
amsc001StopGate

Do NOT change the historical AMC record to make it look current.

Current Identity authority
Preserve identityModuleAmsc001W0..W3 as the authoritative current lineage.

W3 must remain:

state = IDENTITY_AMSC_001_CERTIFIED
verdict = COMPLETE_REFERENCE_PATTERN
lockVersion = ARCH-COMPLETE-002
structureCertified = true
structureState = CERTIFIED
validatorCoverageState = EXHAUSTIVE_9_REQUIRED_PRESENT_4_NO_VALIDATOR_REQUIRED
endpointReachableRequests = 13
foreignAppInfraDomainCoupling = ZERO
crossModuleJoinState = ZERO
crossModulePersistenceState = ZERO
blockingResidualDebt = ZERO
microserviceExtractable = true
automaticNextImplementationTask = NONE
Master Recovery
Record the final W3 SHA explicitly:
TB-TMAR-IDENTITY-AMSC-001-W3 Certify e6d46774

Preserve W0/W1/W2 SHAs exactly.

Historical marker
Preserve the earlier TB-TMAR-IDENTITY-AMC-001 lineage and its evidence.

Add the canonical authority marker:
HISTORICAL / SUPERSEDED FOR CURRENT IDENTITY MODULE RECOVERY

Meaning:

AMC history remains truthful historical evidence;
AMSC W0→W3 is authoritative for current Identity certification;
repository-global Host root checkpoint is NOT superseded.
Additive R1 SoT record
Add identityModuleAmsc001W3R1 with at least:
task = TB-TMAR-IDENTITY-AMSC-001-W3-R1
parentTask = TB-TMAR-IDENTITY-AMSC-001-W3
mode = RECOVERY_SOT_RECONCILIATION_ONLY
state = IDENTITY_AMSC_001_RECOVERY_RECONCILED
productionCodeChanged = false
certifiedCommit = e6d467740dc0305c665d72712fbc5f115ba4b4bf
historicalAmcState = RESTORED_PRE_W3_TRUTH
historicalLineageState = PRESERVED_AND_SUPERSEDED_FOR_CURRENT_MODULE_RECOVERY
masterRecoveryW3ShaState = RECORDED_E6D46774
currentAuthority = IDENTITY_AMSC_001_W0_TO_W3
globalHostCheckpointState = PRESERVED
workflowStop = USER_REVIEW_IDENTITY_AMSC_001_W3_R1
automaticNextImplementationTask = NONE
Manifest
Do NOT change manifest structure.
Identity is already correctly certified.

DURABLE GUARD
Inspect both:

IdentityModuleAmsc001W3CertGuardTests
IdentityModuleAmcW5CertGuardTests

The historical AMC guard must NOT force identityAmc001 to carry current AMSC values.

Adjust only the minimum recovery assertions needed to prove:

identityAmc001.structureState == READY_FOR_CERTIFY
historical validator classification remains 6 REQUIRED + 7 NO_VALIDATOR_REQUIRED
current identityModuleAmsc001W3.structureState == CERTIFIED
current W3 validator classification is 9 REQUIRED + 4 NO_VALIDATOR_REQUIRED
R1 certifiedCommit == e6d467740dc0305c665d72712fbc5f115ba4b4bf
Master Recovery includes W3 SHA e6d46774
historical marker exists
global Host root checkpoint preserved
automaticNextImplementationTask == NONE

If IdentityModuleAmcW5CertGuardTests was modified in W3 to assert current AMSC truth against the historical AMC record, reconcile it so:

historical AMC assertions assert historical AMC truth;
current AMSC assertions target the AMSC W3 record.
This is recovery correction, not guard weakening.

No tautological checks.
No guard deletion.
No baseline widening.

EVIDENCE
Create:
docs/architecture/evidence/TB-TMAR-IDENTITY-AMSC-001-W3-R1/

with at least:

recovery-reconciliation.md
validation.md

Record:

exact identityAmc001 before-W3 vs W3 diff
restored historical fields
current AMSC authority proof
Master Recovery W3 SHA before/after
historical marker proof
global Host checkpoint preservation
production files changed = ZERO
schema/migration/frontend changes = ZERO
manifest structural change = ZERO
exact changed-file list
automatic next = NONE

BOUNDED VALIDATION
Run only:

JSON parse for tmar-current-state.json
IdentityModuleAmsc001W3CertGuardTests
IdentityModuleAmcW5CertGuardTests
exact Master Recovery search for e6d46774
git diff --name-only / diff scope proof

No full solution test.
No unrelated repair.
One reconciliation pass + one validation pass.
At most ONE direct correction for an R1-caused focused failure.

COMMIT/PUSH
If PASS:

exactly one R1 commit
push origin/main
verify HEAD == origin/main
preserve unrelated pre-existing untracked artifacts
do not commit foreign artifacts

EXPECTED FINAL
COMPLETE_REFERENCE_PATTERN
ARCH-COMPLETE-002 STRUCTURE_CERTIFIED
IDENTITY_AMC_HISTORICAL_TRUTH_RESTORED
IDENTITY_AMSC_CURRENT_AUTHORITY_PRESERVED
IDENTITY_W3_SHA_RECORDED
IDENTITY_OLD_LINEAGE_SUPERSEDED_FOR_CURRENT_MODULE_RECOVERY
GLOBAL_HOST_ROOT_CHECKPOINT_PRESERVED
PRODUCTION_CODE_CHANGE_ZERO
SCHEMA_MIGRATION_UNCHANGED
FRONTEND_FROZEN_UNCHANGED
AUTOMATIC_NEXT_IMPLEMENTATION_TASK_NONE

RESULT CONTRACT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-IDENTITY-AMSC-001-W3-R1
Parent-Task: TB-TMAR-IDENTITY-AMSC-001-W3
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary: <bounded summary>
Starting-HEAD-State: E6D46774 | DIVERGED
Production-Scope-State: RECOVERY_SOT_ONLY | VIOLATION
Production-Code-Changed-State: ZERO | NONZERO
Historical-AMC-State-Before-R1: W3_REWRITTEN_CURRENT_AMSC_VALUES | CONFLICT
Historical-AMC-State-After-R1: RESTORED_PRE_W3_TRUTH | STALE | CONFLICT
Historical-AMC-Structure-State: READY_FOR_CERTIFY | CONFLICT
Historical-AMC-Validator-State: 6_REQUIRED_7_NO_VALIDATOR_REQUIRED | CONFLICT
Current-AMSC-W3-Certification-State: IDENTITY_AMSC_001_CERTIFIED | CONFLICT
Current-AMSC-W3-Structure-State: CERTIFIED | CONFLICT
Current-AMSC-W3-Validator-State: 9_REQUIRED_4_NO_VALIDATOR_REQUIRED | CONFLICT
Master-Recovery-W3-SHA-Before-State: MISSING | CONFLICT
Master-Recovery-W3-SHA-After-State: RECORDED_E6D46774 | MISSING | CONFLICT
Historical-Identity-Lineage-State: PRESERVED | REGRESSED
Historical-Identity-Lineage-Authority-State: SUPERSEDED_FOR_CURRENT_MODULE_RECOVERY | CONFLICT
Global-Host-Checkpoint-State: PRESERVED | REGRESSED
Manifest-Structural-State: NOT_TOUCHED | UNCHANGED | REGRESSED
Cross-Module-Boundary-State: CONTRACTS_ONLY | REGRESSED
Foreign-App-Infra-Domain-Coupling-State: ZERO | REGRESSED
Focused-AMC-Guard-State: PASS | FAIL
Focused-AMSC-Guard-State: PASS | FAIL
Json-Parse-State: PASS | FAIL
Evidence-State: COMPLETE | INCOMPLETE
Recovery-SoT-State: RECONCILED | STALE | CONFLICT
Commit-SHA: <sha>
HEAD-Equals-Origin-Main: YES | NO
Working-Tree-State: CLEAN | CLEAN_EXCEPT_PREEXISTING_UNRELATED_ARTIFACTS | DIRTY_UNEXPECTED
User-Work-Preserved: YES | NO
Final-Certification-State: COMPLETE_REFERENCE_PATTERN_ARCH_COMPLETE_002_STRUCTURE_CERTIFIED | NOT_CERTIFIED
Automatic-Next-Implementation-Task-State: NONE
Workflow-Stop-State: USER_REVIEW_IDENTITY_AMSC_001_W3_R1
STOP
END_TOOBA_WORKER_RESULT

STOP RULE
STOP completely after result.
No R2, W4, next module, unrelated repair, or automatic continuation.
Wait for Architect review.

END_TOOBA_TASK
