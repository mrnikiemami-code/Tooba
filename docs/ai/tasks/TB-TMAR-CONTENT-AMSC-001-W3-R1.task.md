PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-CONTENT-AMSC-001-W3-R1
Parent-Task: TB-TMAR-CONTENT-AMSC-001-W3
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: RECOVERY_SOT_EVIDENCE_RECONCILIATION_ONLY
Title: Reconcile Content W3 commit, focused validation truth, and historical recovery lineage

ARCHITECT VERDICT
Content production architecture/structure and W3 certification are accepted. Do NOT reopen Content production code, structure, routes, CQRS, validators, contracts, localization, persistence, schema, or behavior.

STARTING HEAD
c012345d866fec6ecb32ca6f3ff4aef616d877ae

ACCEPTED LINEAGE
W0 Analyze 702537bee0fda591ca71b1d67ec51b874f63f857
W1 Migrate deb13ae8edcdbfe9108bfea2359f589a0ac5c58f
W2 Structure ce7b393871b62f295d565a24795208719757f71a
W3 Certify c012345d866fec6ecb32ca6f3ff4aef616d877ae

ACCEPTED FINAL
COMPLETE_REFERENCE_PATTERN
ARCH-COMPLETE-002 STRUCTURE_CERTIFIED
51 endpoint-reachable requests
17 VALIDATOR_REQUIRED + 34 NO_VALIDATOR_REQUIRED
HOST_CONTENT_OWNERSHIP_ZERO
CONTRACTS_ONLY
FOREIGN_APP_INFRA_DOMAIN_COUPLING_ZERO
CROSS_MODULE_JOIN_ZERO
CROSS_MODULE_PERSISTENCE_ZERO
SCHEMA_MIGRATIONS_UNCHANGED
BLOCKING_RESIDUAL_DEBT_ZERO
MICROSERVICE_EXTRACTABLE_TRUE

DEFECT
Current committed recovery truth contains bounded governance drift:

Master Recovery records:
TB-TMAR-CONTENT-AMSC-001-W3 Certify (this wave)
but does NOT record final W3 commit SHA:
c012345d

Validation metadata disagrees:

contentModuleAmsc001W3.focusedValidation:
Content filter 66 passed / 0 failed / 14 skipped
W3 certification evidence:
Content filter 54 passed / 0 failed / 14 skipped
Master Recovery:
Content filter 54 passed / 0 failed / 14 skipped
external final report:
66 passed / 0 failed / 14 skipped

Re-discover current focused truth once and reconcile it. Do not guess.

The current Master Recovery prose says the new AMSC lineage supersedes the earlier Content AMC lineage,
but the historical lineage is not explicitly labeled with the same durable
HISTORICAL / SUPERSEDED FOR CURRENT CONTENT MODULE RECOVERY convention used for other reconciled modules.

GOAL
Perform one bounded Recovery/SoT/evidence-only reconciliation:

record W3 final SHA c012345d explicitly;
reconcile focused Content validation metadata from deterministic current evidence;
preserve the old Content Host/AMC lineage as historical evidence and explicitly mark it superseded only for current Content module recovery;
preserve global Host checkpoint;
preserve W3 certification;
automaticNextImplementationTask = NONE;
zero production change.

PRECHECK
Before editing:

Verify HEAD == origin/main.
Verify HEAD is exactly c012345d866fec6ecb32ca6f3ff4aef616d877ae.
Re-read:
.cursor/skills/tooba-architecture-certify/SKILL.md
docs/architecture/tmar-current-state.json
docs/architecture/tmar-module-structure-manifests.json
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
docs/architecture/evidence/TB-TMAR-CONTENT-AMSC-001-W3/certification.md
src/backend/Host/Tooba.Host.Tests/Architecture/ContentModuleAmsc001W3CertGuardTests.cs
Confirm W0/W1/W2/W3 SHAs from git history.
Confirm W3 production certification remains COMPLETE_REFERENCE_PATTERN / ARCH-COMPLETE-002.
If repository truth materially differs: RECOVERY_CONFLICT + STOP.

GLOBAL RECOVERY LOCK
Do NOT displace repository-global Host recovery authority.

Preserve:

lastAcceptedTask
lastAcceptedCommit
latestAcceptedImplementationWave
currentHostCheckpoint
nextHostFolder
repository-global workflowStop
repository-global automaticNextImplementationTask

This is Content module-local recovery reconciliation only.

ALLOWED FILES

docs/architecture/tmar-current-state.json
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
docs/architecture/evidence/TB-TMAR-CONTENT-AMSC-001-W3/certification.md
existing Content W3 cert/recovery guard ONLY if needed to lock this exact reconciliation
docs/architecture/evidence/TB-TMAR-CONTENT-AMSC-001-W3-R1/*
docs/ai/tasks/TB-TMAR-CONTENT-AMSC-001-W3-R1.task.md
Content manifest certificationNote wording ONLY if proven stale by the same commit-lineage reconciliation; NO structural manifest changes

FORBIDDEN
Any Content production file; any csproj; any other module production file; Host production; frontend; routes; DTOs; handlers; validators; error-code values; descriptors; resx; contracts; DI; solution/project structure; schema; migrations; manifest projects/rootAllowlist/forbiddenRootFiles/forbiddenTopLevelFolders/structureCertified/lockVersion; source-size baseline; guard weakening; baseline widening; unrelated recovery cleanup; full solution tests; W4; next module.

IMPLEMENTATION

Master Recovery
Update the current Content AMSC lineage to record:
TB-TMAR-CONTENT-AMSC-001-W3 Certify c012345d

Preserve W0/W1/W2 SHAs exactly.

Historical lineage
Preserve all earlier Host Content / AMC records.
Add an explicit recovery marker equivalent to:
HISTORICAL / SUPERSEDED FOR CURRENT CONTENT MODULE RECOVERY

This must mean:

historical records remain valid historical evidence;
the AMSC W0→W3 lineage is authoritative for the current Content module certification;
global Host recovery checkpoint is NOT superseded or displaced.

Focused validation truth
Run exactly the established focused Content test command used by the current certification convention:
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --filter FullyQualifiedName~Content

Record exact Passed / Failed / Skipped.

Reconcile only stale descriptive metadata in:

contentModuleAmsc001W3.focusedValidation
W3 certification evidence
Master Recovery Content checkpoint

Do NOT rewrite historical W1/W2 validation numbers; those are wave-local historical results.

If the final count differs from both 54 and 66 because the R1 guard itself adds a new matching test,
record BOTH:

certified-head/pre-R1 focused count, if deterministically proven;
final-R1-tree focused count.
Do not create a false contradiction by replacing historical W3 count with the post-R1 guard count without explanation.

W3 commit truth
Prefer additive recovery modeling.
Add:
contentModuleAmsc001W3R1

with at least:
task = TB-TMAR-CONTENT-AMSC-001-W3-R1
parentTask = TB-TMAR-CONTENT-AMSC-001-W3
mode = RECOVERY_SOT_EVIDENCE_RECONCILIATION_ONLY
state = CONTENT_AMSC_001_RECOVERY_RECONCILED
productionCodeChanged = false
certifiedCommit = c012345d866fec6ecb32ca6f3ff4aef616d877ae
masterRecoveryW3ShaState = RECORDED_C012345D
historicalLineageState = PRESERVED_AND_SUPERSEDED_FOR_CURRENT_MODULE_RECOVERY
focusedValidationState = <exact proven truth>
workflowStop = USER_REVIEW_CONTENT_AMSC_001_W3_R1
automaticNextImplementationTask = NONE

Preserve W3 architectural truth unchanged:
state = CONTENT_AMSC_001_CERTIFIED
verdict = COMPLETE_REFERENCE_PATTERN
lockVersion = ARCH-COMPLETE-002
structureCertified = true
structureState = CERTIFIED
endpointReachableRequests = 51
validatorCoverageState = EXHAUSTIVE_17_REQUIRED_34_NO_VALIDATOR_REQUIRED
crossModuleBoundaryState = CONTRACTS_ONLY
foreignAppInfraDomainCoupling = ZERO
blockingResidualDebt = ZERO
microserviceExtractable = true

MANIFEST
Do NOT change manifest structure.

If the Content certificationNote is updated, it may only append the final W3 SHA/recovery wording.
Never change:

projects
rootAllowlist
forbiddenRootFiles
forbiddenTopLevelFolders
structureCertified
lockVersion

If no manifest wording change is necessary, leave manifest untouched.

DURABLE GUARD
Inspect ContentModuleAmsc001W3CertGuardTests first.

Add/extend only the smallest focused recovery assertion needed to prove:

W3 remains COMPLETE_REFERENCE_PATTERN / CERTIFIED / ARCH-COMPLETE-002
R1 certifiedCommit == c012345d866fec6ecb32ca6f3ff4aef616d877ae
Master Recovery contains:
TB-TMAR-CONTENT-AMSC-001-W3 Certify c012345d
historical Content lineage remains present and is explicitly marked historical/superseded for current module recovery
global Host root checkpoint is preserved
automaticNextImplementationTask == NONE

Do not lock volatile test counts in a brittle guard unless repository precedent explicitly requires it.

No tautological assertions.
No guard weakening.
No baseline widening.

EVIDENCE
Create:
docs/architecture/evidence/TB-TMAR-CONTENT-AMSC-001-W3-R1/

with at least:

recovery-reconciliation.md
validation.md

Record:

W3 SHA gap before/after
validation metadata drift before/after
exact focused test command and exact result
if applicable, pre-R1 vs post-R1 guard-count distinction
historical lineage preservation proof
global Host checkpoint preservation proof
production files changed = ZERO
schema/migration/frontend changes = ZERO
manifest structural change = ZERO
exact changed-file list
automatic next = NONE

BOUNDED VALIDATION
Run only:

JSON parse for tmar-current-state.json
focused Content test filter
ContentModuleAmsc001W3CertGuardTests
exact Master Recovery search for c012345d
git diff --name-only / git diff scope proof

No full solution test.
No unrelated repair.
One reconciliation pass + one validation pass.
At most ONE direct correction for an R1-caused focused failure.

COMMIT/PUSH
If PASS:

exactly one R1 commit
push origin/main
verify HEAD == origin/main
preserve unrelated pre-existing untracked RESULT.bridge.txt/post-result.js artifacts
do not commit unrelated artifacts

EXPECTED FINAL
COMPLETE_REFERENCE_PATTERN
ARCH-COMPLETE-002 STRUCTURE_CERTIFIED
CONTENT_W3_SHA_RECORDED
CONTENT_FOCUSED_VALIDATION_METADATA_RECONCILED
CONTENT_HISTORICAL_LINEAGE_PRESERVED
CONTENT_OLD_LINEAGE_SUPERSEDED_FOR_CURRENT_MODULE_RECOVERY
GLOBAL_HOST_ROOT_CHECKPOINT_PRESERVED
PRODUCTION_CODE_CHANGE_ZERO
SCHEMA_MIGRATION_UNCHANGED
FRONTEND_FROZEN_UNCHANGED
AUTOMATIC_NEXT_IMPLEMENTATION_TASK_NONE

RESULT CONTRACT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-CONTENT-AMSC-001-W3-R1
Parent-Task: TB-TMAR-CONTENT-AMSC-001-W3
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary: <bounded summary>
Starting-HEAD-State: C012345D | DIVERGED
Production-Scope-State: RECOVERY_SOT_EVIDENCE_ONLY | VIOLATION
Production-Code-Changed-State: ZERO | NONZERO
W3-Certification-State: CONTENT_AMSC_001_CERTIFIED | CONFLICT
W3-Structure-State: CERTIFIED | CONFLICT
Master-Recovery-W3-SHA-Before-State: MISSING | CONFLICT
Master-Recovery-W3-SHA-After-State: RECORDED_C012345D | MISSING | CONFLICT
Focused-Validation-Metadata-Before-State: DRIFT_54_VS_66 | CONFLICT
Focused-Content-Test-State: PASS | FAIL
Focused-Content-Test-Count-State: <passed_failed_skipped>
Focused-Validation-Metadata-After-State: RECONCILED | STALE | CONFLICT
Historical-Content-Lineage-State: PRESERVED | REGRESSED
Historical-Content-Lineage-Authority-State: SUPERSEDED_FOR_CURRENT_MODULE_RECOVERY | CONFLICT
Global-Host-Checkpoint-State: PRESERVED | REGRESSED
Routes-State: UNCHANGED | REGRESSED
Dto-Shape-State: UNCHANGED | REGRESSED
Error-Code-State: UNCHANGED | REGRESSED
Resource-State: UNCHANGED | REGRESSED
Schema-Migration-State: UNCHANGED | REGRESSED
Manifest-Structural-State: NOT_TOUCHED | UNCHANGED | REGRESSED
Cross-Module-Boundary-State: CONTRACTS_ONLY | REGRESSED
Foreign-App-Infra-Domain-Coupling-State: ZERO | REGRESSED
Focused-Cert-Guard-State: PASS | FAIL
Json-Parse-State: PASS | FAIL
Evidence-State: COMPLETE | INCOMPLETE
Recovery-SoT-State: RECONCILED | STALE | CONFLICT
Commit-SHA: <sha>
HEAD-Equals-Origin-Main: YES | NO
Working-Tree-State: CLEAN | CLEAN_EXCEPT_PREEXISTING_UNRELATED_ARTIFACTS | DIRTY_UNEXPECTED
User-Work-Preserved: YES | NO
Final-Certification-State: COMPLETE_REFERENCE_PATTERN_ARCH_COMPLETE_002_STRUCTURE_CERTIFIED | NOT_CERTIFIED
Automatic-Next-Implementation-Task-State: NONE
Workflow-Stop-State: USER_REVIEW_CONTENT_AMSC_001_W3_R1
STOP
END_TOOBA_WORKER_RESULT

STOP RULE
STOP completely after result.
No R2, W4, next module, unrelated repair, or automatic continuation.
Wait for Architect review.

END_TOOBA_TASK
