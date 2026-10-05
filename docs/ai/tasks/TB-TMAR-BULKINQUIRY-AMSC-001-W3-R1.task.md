PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-BULKINQUIRY-AMSC-001-W3-R1
Parent-Task: TB-TMAR-BULKINQUIRY-AMSC-001-W3
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: RECOVERY_SOT_EVIDENCE_RECONCILIATION_ONLY
Title: Reconcile BulkInquiry AMSC historical Domain→Contracts truth and final recovery checkpoint

ARCHITECT VERDICT
BulkInquiry production architecture/structure and W3 certification are accepted. Do NOT reopen production code or re-run AMSC implementation.

STARTING HEAD
67b5b55a2fecec33f3109b90252152875680edc0

ACCEPTED LINEAGE
W0 Analyze 7b89d81e
W1 Migrate 9e9e37df
W2 Structure 82c2fa8e
W3 Certify 67b5b55a

ACCEPTED FINAL
COMPLETE_REFERENCE_PATTERN
ARCH-COMPLETE-002 STRUCTURE_CERTIFIED
1 endpoint-reachable request / 1 handler
1 VALIDATOR_REQUIRED + 0 NO_VALIDATOR_REQUIRED
1 module-owned route
HOST_BULKINQUIRY_OWNERSHIP_ZERO
CONTRACTS_ONLY
FOREIGN_APP_INFRA_DOMAIN_COUPLING_ZERO
CROSS_MODULE_JOIN_ZERO
SCHEMA_MIGRATIONS_UNCHANGED
BLOCKING_RESIDUAL_DEBT_ZERO

DEFECT
Current committed recovery/evidence truth contains a historical contradiction.

Disk truth at W3:

src/backend/Modules/BulkInquiry/Tooba.BulkInquiry.Domain/Tooba.BulkInquiry.Domain.csproj
contains a self-module ProjectReference to Tooba.BulkInquiry.Contracts.
Domain/Aggregates/BulkPurchaseInquiry.cs uses BulkInquiryErrorCodes.Rejected from own Contracts.
W2 structure evidence explicitly says the durable structure guard accepts:
"BulkInquiry_domain_references_only_buildingblocks_and_own_contracts".
W3 certification/SoT explicitly records this own-module Domain -> Contracts reference as a legitimate self-module reference and cross-module coupling remains ZERO.

But W1 historical records currently claim the opposite:

docs/architecture/evidence/TB-TMAR-BULKINQUIRY-AMSC-001-W1/migrate.md says Domain -> Contracts reference was removed and Domain -> BuildingBlocks only.
docs/architecture/tmar-current-state.json bulkInquiryModuleAmsc001W1 says:
domainContractsReference = REMOVED

Commit 9e9e37df did NOT modify Tooba.BulkInquiry.Domain.csproj, so that claimed removal did not occur.

SECONDARY RECOVERY GAP
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md records the W0→W3 task lineage but does not explicitly record W3 final commit SHA 67b5b55a.

VALIDATION-COUNT DRIFT
Current records also disagree on focused BulkInquiry test counts:

W3 SoT focusedValidation records 19 passed / 0 failed / 2 skipped.
W3 certification evidence records 24 passed / 0 failed / 2 skipped.
external worker summary reported 23 passed / 0 failed / 2 skipped.

This R1 must re-discover the exact focused command/result used now and reconcile metadata honestly. Do NOT invent a number and do NOT run a broad/full solution suite.

GOAL
Perform one bounded Recovery/SoT/evidence reconciliation only:

Correct W1 historical Domain→Contracts truth to match actual commit/disk reality.
Preserve W2/W3 accepted rule: own-module Domain→Contracts reference is allowed here and is not cross-module coupling.
Add W3 final commit SHA 67b5b55a to the authoritative BulkInquiry module recovery checkpoint.
Reconcile focused validation count metadata from one deterministic focused BulkInquiry run.
Preserve W3 certification and all production behavior unchanged.
automaticNextImplementationTask remains NONE.

PRECHECK
Before editing:

Verify HEAD == origin/main.
Verify HEAD is exactly 67b5b55a2fecec33f3109b90252152875680edc0.
Re-read:
.cursor/skills/tooba-architecture-certify/SKILL.md
docs/architecture/tmar-current-state.json
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
docs/architecture/evidence/TB-TMAR-BULKINQUIRY-AMSC-001-W1/migrate.md
docs/architecture/evidence/TB-TMAR-BULKINQUIRY-AMSC-001-W2/structure.md
docs/architecture/evidence/TB-TMAR-BULKINQUIRY-AMSC-001-W3/certification.md
src/backend/Host/Tooba.Host.Tests/Architecture/BulkInquiryModuleAmsc001W3CertGuardTests.cs
current Domain csproj
Inspect commit 9e9e37df diff and prove Domain csproj was not changed by W1.
If current repository truth materially differs: RECOVERY_CONFLICT + STOP.

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

This task is module-local BulkInquiry recovery reconciliation only.

ALLOWED FILES

docs/architecture/tmar-current-state.json
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
docs/architecture/evidence/TB-TMAR-BULKINQUIRY-AMSC-001-W1/migrate.md
docs/architecture/evidence/TB-TMAR-BULKINQUIRY-AMSC-001-W3/certification.md ONLY if focused validation metadata is stale
existing focused BulkInquiry W3 cert guard ONLY if needed to lock this exact recovery truth
docs/architecture/evidence/TB-TMAR-BULKINQUIRY-AMSC-001-W3-R1/*
task/result artifacts required by repository convention

FORBIDDEN
Any BulkInquiry production file; any csproj; any other module production file; Host production; frontend; routes; DTOs; handlers; validators; error code values; descriptors; resx; contracts; DI; solution/project structure; schema; migrations; manifest structural fields; source-size baselines; allowlist widening; unrelated SoT cleanup; full solution tests; W4; next module.

Do NOT remove the legitimate self-module Domain -> Contracts reference in this R1.
Do NOT create a production repair merely to make old W1 prose true.
Repository reality wins; historical documentation must be corrected instead.

IMPLEMENTATION

Correct bulkInquiryModuleAmsc001W1 historical metadata:
Replace the false claim:
domainContractsReference = REMOVED
with truthful wording equivalent to:
PRESERVED_OWN_MODULE_CONTRACTS_REFERENCE_FOR_BulkInquiryErrorCodes_Rejected

Make clear:

this is self-module layering, not cross-module coupling;
foreign App/Infra/Domain coupling remains ZERO;
W1 changed Domain fault type to ContractOperationException but did not remove the Domain csproj reference.

Correct W1 evidence section "Typed Domain/Infrastructure faults":

remove the false before/after statement claiming Domain→Contracts was removed;
state the actual committed result:
Domain retained its own Contracts reference for BulkInquiryErrorCodes.Rejected;
Domain fault mechanism changed SemanticException -> ContractOperationException;
no foreign module reference was introduced.
Preserve route/status/schema/behavior claims that are still true.

Preserve W2 evidence as current structure truth:
"BulkInquiry_domain_references_only_buildingblocks_and_own_contracts".

Preserve W3 certification truth:
nonBlockingWatch R2 (or equivalent) remains the authoritative accepted layering explanation.

In Master Recovery BulkInquiry AMSC checkpoint:
record W3 final SHA explicitly:
TB-TMAR-BULKINQUIRY-AMSC-001-W3 Certify 67b5b55a

Preserve the AMC-001 historical/superseded lineage.

Add an additive SoT reconciliation record if repository convention supports it:
bulkInquiryModuleAmsc001W3R1

Minimal truth:
task = TB-TMAR-BULKINQUIRY-AMSC-001-W3-R1
parentTask = TB-TMAR-BULKINQUIRY-AMSC-001-W3
state = BULKINQUIRY_AMSC_001_RECOVERY_RECONCILED
productionCodeChanged = false
certifiedCommit = 67b5b55a2fecec33f3109b90252152875680edc0
w1DomainContractsTruth = PRESERVED_OWN_MODULE_CONTRACTS_REFERENCE
crossModuleCouplingState = LEGAL_CONTRACTS_ONLY
foreignAppInfraDomainCoupling = ZERO
masterRecoveryState = RECONCILED
workflowStop = USER_REVIEW_BULKINQUIRY_AMSC_001_W3_R1
automaticNextImplementationTask = NONE

Focused validation count reconciliation:
Run exactly one focused command:
dotnet test --filter FullyQualifiedName~BulkInquiry
against the same test project/context used by current BulkInquiry certification convention.

Record the exact Passed / Failed / Skipped from this run in R1 evidence.
If W3 SoT/certification count fields are stale, update only the descriptive focusedValidation text to the newly proven count.
Do not change architectural verdict from count-only drift when all focused tests pass.
If failures are unrelated pre-existing failures, record honestly and STOP only if a required BulkInquiry guard itself fails.

DURABLE GUARD
Inspect the existing W3 cert guard.

Add/extend only if necessary to lock:

Domain csproj contains only BuildingBlocks + own BulkInquiry.Contracts project references.
no foreign module Application/Infrastructure/Domain reference exists.
W1 SoT no longer falsely says domainContractsReference=REMOVED.
Master Recovery contains W3 SHA 67b5b55a.
automaticNextImplementationTask == NONE.
global Host root checkpoint preserved.

No tautological assertions.
No guard weakening.
No baseline widening.

EVIDENCE
Create:
docs/architecture/evidence/TB-TMAR-BULKINQUIRY-AMSC-001-W3-R1/

with at least:

recovery-reconciliation.md
validation.md

Record:

exact contradiction before/after
proof commit 9e9e37df did not alter Domain csproj
current Domain csproj allowed references
proof cross-module coupling remains ZERO
explicit W3 SHA recovery checkpoint
exact focused test command and exact current pass/fail/skip count
production files changed = ZERO
schema/migration/frontend changes = ZERO
automatic next = NONE
exact changed-file list

BOUNDED VALIDATION
Run only:

JSON parse for tmar-current-state.json
dotnet test --filter FullyQualifiedName~BulkInquiry in the focused established test project
BulkInquiryModuleAmsc001W3CertGuardTests
exact text/search proof for Master Recovery W3 SHA
git diff --name-only / git diff proof

No full solution test.
No unrelated repair.
One reconciliation pass + one validation pass.
At most ONE direct correction for an R1-caused focused failure.

COMMIT/PUSH
If PASS:

create exactly one R1 commit
commit message identifies:
TB-TMAR-BULKINQUIRY-AMSC-001-W3-R1
Recovery/SoT/evidence reconciliation
push origin/main
verify HEAD == origin/main
preserve unrelated pre-existing untracked artifacts

EXPECTED FINAL
COMPLETE_REFERENCE_PATTERN
ARCH-COMPLETE-002 STRUCTURE_CERTIFIED
W1_DOMAIN_CONTRACTS_HISTORY_RECONCILED
OWN_MODULE_DOMAIN_TO_CONTRACTS_REFERENCE_ACCEPTED
FOREIGN_APP_INFRA_DOMAIN_COUPLING_ZERO
MASTER_RECOVERY_W3_SHA_RECORDED
FOCUSED_VALIDATION_METADATA_RECONCILED
PRODUCTION_CODE_CHANGE_ZERO
SCHEMA_MIGRATION_UNCHANGED
FRONTEND_FROZEN_UNCHANGED
AUTOMATIC_NEXT_IMPLEMENTATION_TASK_NONE

RESULT CONTRACT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-BULKINQUIRY-AMSC-001-W3-R1
Parent-Task: TB-TMAR-BULKINQUIRY-AMSC-001-W3
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary: <bounded summary>
Starting-HEAD-State: 67B5B55A | DIVERGED
Production-Scope-State: RECOVERY_SOT_EVIDENCE_ONLY | VIOLATION
Production-Code-Changed-State: ZERO | NONZERO
W1-Domain-Contracts-Claim-Before-State: FALSE_REMOVED_CLAIM | CONFLICT
W1-Domain-Contracts-Claim-After-State: PRESERVED_OWN_MODULE_CONTRACTS_REFERENCE | STALE | CONFLICT
W1-Domain-Csproj-Commit-Diff-State: NOT_CHANGED_BY_W1 | CONFLICT
Current-Domain-Reference-State: BUILDINGBLOCKS_PLUS_OWN_BULKINQUIRY_CONTRACTS | CONFLICT
Cross-Module-Boundary-State: CONTRACTS_ONLY | REGRESSED
Foreign-App-Infra-Domain-Coupling-State: ZERO | REGRESSED
Master-Recovery-W3-SHA-State: RECORDED_67B5B55A | MISSING | CONFLICT
Historical-AMC-Lineage-State: PRESERVED | REGRESSED
Focused-BulkInquiry-Test-State: PASS | FAIL
Focused-BulkInquiry-Test-Count-State: <passed_failed_skipped>
Focused-Cert-Guard-State: PASS | FAIL
Json-Parse-State: PASS | FAIL
Routes-State: UNCHANGED | REGRESSED
Dto-Shape-State: UNCHANGED | REGRESSED
Error-Code-State: UNCHANGED | REGRESSED
Resource-State: UNCHANGED | REGRESSED
Schema-Migration-State: UNCHANGED | REGRESSED
Manifest-Structural-State: NOT_TOUCHED | UNCHANGED | REGRESSED
Global-Host-Checkpoint-State: PRESERVED | REGRESSED
Evidence-State: COMPLETE | INCOMPLETE
Recovery-SoT-State: RECONCILED | STALE | CONFLICT
Commit-SHA: <sha>
HEAD-Equals-Origin-Main: YES | NO
Working-Tree-State: CLEAN | CLEAN_EXCEPT_PREEXISTING_UNRELATED_ARTIFACTS | DIRTY_UNEXPECTED
User-Work-Preserved: YES | NO
Final-Certification-State: COMPLETE_REFERENCE_PATTERN_ARCH_COMPLETE_002_STRUCTURE_CERTIFIED | NOT_CERTIFIED
Automatic-Next-Implementation-Task-State: NONE
Workflow-Stop-State: USER_REVIEW_BULKINQUIRY_AMSC_001_W3_R1
STOP
END_TOOBA_WORKER_RESULT

STOP RULE
STOP completely after result.
No R2, W4, next module, unrelated repair, or automatic continuation.
Wait for Architect review.

END_TOOBA_TASK