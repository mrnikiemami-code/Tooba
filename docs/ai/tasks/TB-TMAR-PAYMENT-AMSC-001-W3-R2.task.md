PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-PAYMENT-AMSC-001-W3-R2
Parent-Task: TB-TMAR-PAYMENT-AMSC-001-W3-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: RECOVERY_LINEAGE_AND_ERROR_TRUTH_RECONCILIATION_ONLY
Title: Close Payment recovery and reconcile 28/27/24 error-code truth

ARCHITECT VERDICT
Payment current production architecture is accepted for this recovery wave.
Do NOT change production behavior or error ownership in this task.

Independent repository verification confirms current source truth:

semantic commits:
W0 = 6839bb4a5f75a954817719386de04e36ff96c305
W1 = 2d69d82808178e786f0673dc725baeb238987829
W2 = a138ec61bcbbb719fed2949c60e21fa77104cfd1
W3 = 502d73e0e9ccfb277a06ad397b1f0a511f586921
W3-R1 = affdfba4fc5fce402d05e68131bdb4a01899b93c
direct parent chain is W0 -> W1 -> W2 -> W3 -> W3-R1, with no metadata hop between waves.
PaymentErrorCodes declares 28 public const string values.
PaymentErrorCatalogContributor registers 24 Payment-owned descriptors.
four declared constants are foreign-owned/consumed and deliberately not re-registered:
ReservationRetryLimit
SupplyUnavailable
AdminAuthorizationDenied
CheckoutAuthenticationRequired
KnownCodes contains 27 members, NOT 28.
among the four foreign-owned declared constants, exactly three are in KnownCodes:
ReservationRetryLimit
SupplyUnavailable
CheckoutAuthenticationRequired
AdminAuthorizationDenied is declared/consumed but intentionally NOT in KnownCodes.
W1 SoT text saying "28 declared / 24 registered / 3 intentionally consumed" is stale/inaccurate.
W3/Master Recovery wording saying all 4 foreign-owned declarations are "for the known-code guard" is inaccurate.
W3 certification remains current authority.
W3-R1 did not create an independent paymentAmsc001W3R1 recovery block.

STARTING HEAD
affdfba4fc5fce402d05e68131bdb4a01899b93c

CURRENT CERTIFICATION AUTHORITY
Task: TB-TMAR-PAYMENT-AMSC-001-W3
Commit: 502d73e0e9ccfb277a06ad397b1f0a511f586921
Verdict: COMPLETE_REFERENCE_PATTERN
Lock: ARCH-COMPLETE-002
Structure: CERTIFIED

GOAL
One bounded recovery/truth reconciliation only:

record exact W0/W1/W2/W3 semantic commit lineage durably;
create explicit W3-R1 recovery truth including R1 own SHA;
add final R2 recovery closure;
reconcile authoritative error-code counts and semantics to 28 declared / 27 KnownCodes / 24 Payment-owned descriptors;
distinguish 4 foreign-owned declared/consumed codes from the 3 foreign codes actually admitted by PaymentErrorCodes.IsKnown;
preserve W3 certification authority;
zero production behavior change;
zero manifest structural change;
zero schema/migration change;
no automatic next task.

PRECHECK

Verify branch = main.
Verify HEAD == origin/main == affdfba4fc5fce402d05e68131bdb4a01899b93c.
Verify exact parent chain:
2d69d828 parent = 6839bb4a
a138ec61 parent = 2d69d828
502d73e0 parent = a138ec61
affdfba4 parent = 502d73e0
Read:
.cursor/skills/tooba-architecture-migrate/SKILL.md
.cursor/skills/tooba-architecture-certify/SKILL.md
docs/architecture/tmar-current-state.json
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
docs/architecture/tmar-module-structure-manifests.json
PaymentErrorCodes.cs
PaymentErrorCatalogContributor.cs
PaymentOperation.cs
PaymentModuleAmsc001W3CertGuardTests.cs
W0/W1/W2/W3 evidence
Directly assert:
public const string declaration count = 28
Payment descriptor count = 24
KnownCodes initializer member count = 27
four foreign declared codes are absent from Payment descriptor contributor
ReservationRetryLimit, SupplyUnavailable, CheckoutAuthenticationRequired are in KnownCodes
AdminAuthorizationDenied is NOT in KnownCodes
Reconfirm W3:
COMPLETE_REFERENCE_PATTERN
ARCH-COMPLETE-002
structureCertified true
PROFESSIONAL_SHALLOW
endpointReachableRequests 16
validatorRequiredCount 15
noValidatorRequiredCount 1
foreignAppInfraDomainCoupling ZERO
microserviceExtractable true
If repository truth materially differs: RECOVERY_CONFLICT + STOP.

GLOBAL RECOVERY LOCK
Preserve exactly:

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
src/backend/Host/Tooba.Host.Tests/Architecture/PaymentModuleAmsc001W3CertGuardTests.cs
ONLY for minimal truth-lock strengthening; no weakening
docs/architecture/evidence/TB-TMAR-PAYMENT-AMSC-001-W3-R2/*
docs/ai/tasks/TB-TMAR-PAYMENT-AMSC-001-W3-R2.task.md

FORBIDDEN

Payment production files
any other module production file
any csproj
Host production
frontend
tmar-module-structure-manifests.json
error constants/resources/descriptors
routes/DTO/status behavior
schema/migrations
changing W3 verdict/lock/structureCertified
changing ownership semantics
guard weakening
baseline widening
unrelated cleanup
next module

IMPLEMENTATION

DURABLE SEMANTIC COMMIT TRUTH
Add missing self-contained commit truth to historical wave blocks without rewriting their historical verdict/state:
paymentAmsc001W0:
commit = 6839bb4a
commitFull = 6839bb4a5f75a954817719386de04e36ff96c305
paymentAmsc001W1:
commit = 2d69d828
commitFull = 2d69d82808178e786f0673dc725baeb238987829
paymentAmsc001W2:
commit = a138ec61
commitFull = a138ec61bcbbb719fed2949c60e21fa77104cfd1

Preserve all original historical fields.

CREATE EXPLICIT W3-R1 RECOVERY BLOCK
Add paymentAmsc001W3R1:
task = TB-TMAR-PAYMENT-AMSC-001-W3-R1
parentTask = TB-TMAR-PAYMENT-AMSC-001-W3
mode = RECOVERY_LINEAGE_RECONCILIATION_ONLY
startingHead = 502d73e0
state = PAYMENT_AMSC_001_RECOVERY_RECONCILED
productionCodeChanged = false
certifiedTask = TB-TMAR-PAYMENT-AMSC-001-W3
certifiedCommit = 502d73e0e9ccfb277a06ad397b1f0a511f586921
commit = affdfba4
commitFull = affdfba4fc5fce402d05e68131bdb4a01899b93c
acceptedLineage.w0 = 6839bb4a
acceptedLineage.w1 = 2d69d828
acceptedLineage.w2 = a138ec61
acceptedLineage.w3 = 502d73e0
verdict = COMPLETE_REFERENCE_PATTERN
lockVersion = ARCH-COMPLETE-002
structureCertified = true
globalHostCheckpointState = PRESERVED
schemaMigrationState = UNCHANGED
guardsWeakened = NONE
baselinesWidened = NONE
workflowStop = USER_REVIEW_PAYMENT_AMSC_001_W3_R1
automaticNextImplementationTask = NONE
RECONCILE ERROR-CODE TRUTH ADDITIVELY
Do not rewrite W0 historical 27-code analysis as if it were originally 28.
Do not silently rewrite W1 historical text.

Add reconciliation metadata in R2 and, where useful, additive reconciliation fields:

declaredStableCodeCount = 28
knownCodeGuardMemberCount = 27
paymentOwnedDescriptorCount = 24
foreignOwnedDeclaredConsumedCount = 4
foreignOwnedKnownGuardCount = 3
foreignOwnedNotInKnownGuard = admin.authorization.denied
w1StableErrorCodeStateHistorical = STALE_3_CONSUMED_COUNT
currentStableErrorTruth = AUTHORITATIVE_28_DECLARED_27_KNOWN_24_OWNED_DESCRIPTORS
descriptorOwnershipState = UNIQUE_24_PAYMENT_4_FOREIGN_NOT_REREGISTERED
productionErrorCatalogChangedByR2 = false

The four foreign declared/consumed codes are:

inventory.reservation.retry_limit_reached — Order-owned
inventory.supply.unavailable — Inventory-owned
admin.authorization.denied — Foundation-owned
checkout.authentication_required — Foundation-owned

KnownCodes must be recorded truthfully:

includes ReservationRetryLimit
includes SupplyUnavailable
includes CheckoutAuthenticationRequired
excludes AdminAuthorizationDenied

Do NOT change production code in this task.

MASTER RECOVERY TRUTH
Reconcile the current Payment W3 checkpoint wording.

Preserve:

28 declared constants
24 Payment-owned descriptors
descriptor ownership unique
4 foreign-owned declarations are consumed without Payment descriptor re-registration
W3 remains certification authority

Correct the inaccurate wording that all four are "for the known-code guard".
State exact truth:

4 foreign-owned codes are declared/consumed without re-registration;
3 of those foreign codes participate in Payment IsKnown/PaymentOperation expected-fault recognition;
admin.authorization.denied remains declared/consumed but is excluded from KnownCodes and is not mapped through PaymentOperation's ContractOperationException IsKnown branch.

Also record explicit W3-R1 checkpoint:

W3 cert commit 502d73e0 full SHA
R1 commit affdfba4 full SHA
no production/schema change
recovery only
W3 remains authority
ADD FINAL R2 CLOSURE BLOCK
Add paymentAmsc001W3R2 with:
task = TB-TMAR-PAYMENT-AMSC-001-W3-R2
parentTask = TB-TMAR-PAYMENT-AMSC-001-W3-R1
mode = RECOVERY_LINEAGE_AND_ERROR_TRUTH_RECONCILIATION_ONLY
startingHead = affdfba4
state = PAYMENT_AMSC_001_RECOVERY_CLOSED_RECONCILED
productionCodeChanged = false
currentCertificationAuthority = TB-TMAR-PAYMENT-AMSC-001-W3
currentCertifiedCommit = 502d73e0e9ccfb277a06ad397b1f0a511f586921
w0Commit = 6839bb4a5f75a954817719386de04e36ff96c305
w1Commit = 2d69d82808178e786f0673dc725baeb238987829
w2Commit = a138ec61bcbbb719fed2949c60e21fa77104cfd1
w3Commit = 502d73e0e9ccfb277a06ad397b1f0a511f586921
w3R1Commit = affdfba4fc5fce402d05e68131bdb4a01899b93c
actualParentChainState = RECONCILED
declaredStableCodeCount = 28
knownCodeGuardMemberCount = 27
paymentOwnedDescriptorCount = 24
foreignOwnedDeclaredConsumedCount = 4
foreignOwnedKnownGuardCount = 3
foreignOwnedNotInKnownGuard = admin.authorization.denied
stableErrorTruthState = AUTHORITATIVE_28_DECLARED_27_KNOWN_24_OWNED_DESCRIPTORS
descriptorOwnershipState = UNIQUE
structureState = CERTIFIED
folderGranularityState = PROFESSIONAL_SHALLOW
validatorCoverageState = EXHAUSTIVE_15_REQUIRED_1_NO_VALIDATOR
workerOnlyRequestCount = 1
crossModuleBoundaryState = LEGAL_CONTRACTS_ONLY_BOTH_DIRECTIONS
foreignAppInfraDomainCoupling = ZERO
manifestStructuralState = NOT_TOUCHED_THIS_WAVE
schemaMigrationState = UNCHANGED
globalHostCheckpointState = PRESERVED
guardsWeakened = NONE
baselinesWidened = NONE
workflowStop = USER_REVIEW_PAYMENT_AMSC_001_W3_R2
automaticNextImplementationTask = NONE

Do NOT add a self-referential R2 commit placeholder.

MINIMAL DURABLE GUARD STRENGTHENING
Strengthen the existing Payment W3 cert guard only if needed to pin repository truth:
28 declarations
24 Payment descriptors
exact foreign set of 4 is not registered by Payment
KnownCodes count = 27
ReservationRetryLimit / SupplyUnavailable / CheckoutAuthenticationRequired are in KnownCodes
AdminAuthorizationDenied is not in KnownCodes
W3 certified SHA remains 502d73e0...
paymentAmsc001W3R1 commitFull = affdfba4...
paymentAmsc001W3R2 state = PAYMENT_AMSC_001_RECOVERY_CLOSED_RECONCILED

Do not alter existing assertions except to strengthen exact truth.
No guard weakening.

EVIDENCE
Create:
docs/architecture/evidence/TB-TMAR-PAYMENT-AMSC-001-W3-R2/

Required:

recovery-reconciliation.md
error-code-truth-reconciliation.md
validation.md

Evidence must include:

exact parent chain
exact W0/W1/W2/W3/R1 SHAs
proof W3-R1 previously lacked independent SoT block
exact 28 declaration count
exact 27 KnownCodes member count
exact 24 descriptor count
exact four foreign declared/consumed codes
exact three foreign KnownCodes members + AdminAuthorizationDenied exclusion
proof no descriptor duplication
proof production/manifest/schema unchanged
proof W3 remains current certification authority
proof global Host checkpoint unchanged

BOUNDED VALIDATION
Run only:

JSON parse SoT
exact SHA assertions
exact parent-chain assertions
exact 28/27/24 count assertions
exact foreign-code set assertions
focused Payment W3 cert guard if modified
shallow structure spot check
git diff scope proof

No full solution suite.
No unrelated repair.

PASS CRITERIA

explicit W3-R1 recovery block exists with R1 own SHA
semantic wave SHAs are durable and exact
error-code truth is exact 28 declared / 27 KnownCodes / 24 Payment-owned descriptors
four foreign consumed declarations distinguished from three foreign IsKnown members
W3 remains certification authority at 502d73e0
structure remains CERTIFIED / PROFESSIONAL_SHALLOW
validator matrix remains 15 required + 1 no-validator + 1 worker-only
production/manifest/schema unchanged
global Host checkpoint preserved
automatic next NONE

COMMIT/PUSH
If PASS:

exactly one R2 reconciliation commit
push main
verify HEAD == origin/main
preserve unrelated artifacts

RESULT CONTRACT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-PAYMENT-AMSC-001-W3-R2
Parent-Task: TB-TMAR-PAYMENT-AMSC-001-W3-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary: <bounded summary>
Starting-HEAD-State: AFFDFBA4 | DIVERGED
Production-Scope-State: RECOVERY_LINEAGE_ERROR_TRUTH_ONLY | VIOLATION
Production-Code-Changed-State: ZERO | NONZERO
Current-Certification-Authority-State: W3_502D73E0 | CONFLICT
W3-R1-Recovery-Block-State: RECORDED_AFFDFBA4 | MISSING | CONFLICT
Semantic-Lineage-State: RECONCILED | CONFLICT
Declared-Code-State: EXACT_28 | CONFLICT
Known-Code-Guard-State: EXACT_27 | CONFLICT
Payment-Owned-Descriptor-State: EXACT_24 | CONFLICT
Foreign-Declared-Consumed-State: EXACT_4 | CONFLICT
Foreign-Known-Guard-State: EXACT_3_ADMIN_AUTH_EXCLUDED | CONFLICT
Descriptor-Ownership-State: UNIQUE | CONFLICT
Structure-State: CERTIFIED | REGRESSED
Folder-Granularity-State: PROFESSIONAL_SHALLOW | REGRESSED
Validator-State: EXHAUSTIVE_15_REQUIRED_1_NO_VALIDATOR_PLUS_1_WORKER | REGRESSED
Cross-Module-Boundary-State: LEGAL_CONTRACTS_ONLY_BOTH_DIRECTIONS | REGRESSED
Manifest-Structural-State: NOT_TOUCHED | REGRESSED
Schema-Migration-State: UNCHANGED | REGRESSED
Global-Host-Checkpoint-State: PRESERVED | REGRESSED
Guards-Weakened-State: NONE | NONZERO
Baselines-Widened-State: NONE | NONZERO
Json-Parse-State: PASS | FAIL
Evidence-State: COMPLETE | INCOMPLETE
Recovery-SoT-State: CLOSED_RECONCILED | STALE | CONFLICT
Commit-SHA: <sha>
HEAD-Equals-Origin-Main: YES | NO
Working-Tree-State: CLEAN | CLEAN_EXCEPT_PREEXISTING_UNRELATED_ARTIFACTS | DIRTY_UNEXPECTED
User-Work-Preserved: YES | NO
Final-Certification-State: COMPLETE_REFERENCE_PATTERN_ARCH_COMPLETE_002_STRUCTURE_CERTIFIED | NOT_CERTIFIED
Automatic-Next-Implementation-Task-State: NONE
Workflow-Stop-State: USER_REVIEW_PAYMENT_AMSC_001_W3_R2
STOP
END_TOOBA_WORKER_RESULT

STOP RULE
STOP completely after result.
No R3.
No W4.
No next module.
Wait for Architect review.

END_TOOBA_TASK