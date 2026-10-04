PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-CART-AMSC-001-W3-R1
Parent-Task: TB-TMAR-CART-AMSC-001-W3
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: CERTIFICATION_TRUTH_RECONCILIATION
Title: Cart W3 certification truth reconciliation

ARCHITECT VERDICT

Cart production implementation and structure are ACCEPTED. Do NOT reopen W1/W2.

Accepted:

COMPLETE_REFERENCE_PATTERN
ARCH-COMPLETE-002 STRUCTURE_CERTIFIED
MANIFEST_DISK_EXACT
CONTRACTS_ONLY
foreign Application/Infrastructure/Domain coupling ZERO
microserviceExtractable=true
7 module-owned routes; Host HTTP ownership ZERO
canonical Result/ApiResponseFactory
message-text classification ZERO
capability-first Application/Carts/{Commands,Queries,Validators}

ONLY DEFECT

W1 records:
behaviorChangeState =
PREVIOUSLY_500_PLATFORM_UNEXPECTED_PATHS_NOW_TYPED_LOCALIZED_400_409_503_ALL_PREVIOUSLY_MAPPED_CODES_UNCHANGED

W3 incorrectly records:
behaviorChange = NONE

W3 certification/manifest wording also implies the complete AMSC preserved behavior.

This task reconciles certification truth ONLY. Runtime behavior must not change.

EXPECTED STARTING HEAD
3c2119d791cbea5d658d9ce13cbcbd80ffebe1d4

PRECHECK

Verify HEAD == origin/main.
Re-read W1 evidence, W3 evidence, cartModuleAmsc001W1, cartModuleAmsc001W3 and Cart manifest certificationNote.
Re-discover the exact W1 failure/status remaps before editing.
Material repository disagreement => RECOVERY_CONFLICT + STOP.

ALLOWED

docs/architecture/tmar-current-state.json
Cart manifest certificationNote wording ONLY
docs/architecture/evidence/TB-TMAR-CART-AMSC-001-W3/*
docs/architecture/evidence/TB-TMAR-CART-AMSC-001-W3-R1/*
extend ONE existing focused Cart certification guard if required
task/result artifacts required by repo convention

FORBIDDEN

ANY production source
Domain/Application/Contracts/Endpoints/Infrastructure changes
routes, DTOs, error-code values, resources/resx
schema/migrations
manifest projects/rootAllowlist/forbidden folders/structureCertified/lockVersion
Host production
unrelated drift/.tmp-baseline
baseline widening or guard weakening
full repository test suite
refactor/cleanup
another implementation wave

A. BEHAVIOR TRUTH

W3 must not claim behaviorChange=NONE for the complete AMSC.

Prefer canonical repository vocabulary; otherwise:
behaviorChange = BOUNDED_DEFECT_REPAIR_EXPECTED_FAILURE_MAPPING

Detail must truthfully state:

success behavior/payload preserved
routes/DTO shapes/schema preserved
previously mapped expected failures preserved
only previously unexpected defective failure paths became typed/localized expected failures
W1 evidence includes unexpected 500 -> expected 400/409/503

Do not invent route-by-route mappings not proven by W1 evidence.

B. STATUS TRUTH

If final SoT has/introduces statusCodesChanged, it must not be NONE.

After re-discovery use the narrowest accurate value equivalent to:
statusCodesChanged = BOUNDED_EXPECTED_FAILURE_REMAP_500_TO_400_409_503

If evidence proves a more precise matrix/subset, use that exact truth.

Keep:
routesChanged=NONE
dtoShapeChanged=NONE
schemaChange=NONE

C. CODE/DESCRIPTOR OWNERSHIP TRUTH

Re-discover from disk.

Expected semantics:

27 declared/consumed Cart boundary codes/constants
26 Cart-owned descriptors registered by Cart
checkout.authentication_required = shared/Foundation-owned, consumed by Cart, NOT registered by Cart
duplicate descriptor ownership ZERO

Do NOT ambiguously call all 27 codes Cart-owned.

Prefer fields equivalent to:
declaredOrConsumedCodeCount=27
cartOwnedDescriptorCount=26
sharedConsumedCode=checkout.authentication_required
sharedConsumedCodeOwner=Foundation
sharedConsumedCodeRegisteredByCart=false
duplicateDescriptorOwnership=ZERO

If cartOwnedCodeCount=27 is misleading, reconcile/rename it and update only the focused guard depending on it.

D. MANIFEST NOTE

Correct ONLY Cart certificationNote truth wording.

It must distinguish:

W3 itself introduced no runtime behavior change
W1 contained the accepted bounded expected-failure repair

Structural manifest meaning must remain unchanged.

E. CERTIFICATION PRESERVED

Preserve:
verdict=COMPLETE_REFERENCE_PATTERN
lockVersion=ARCH-COMPLETE-002
structureCertified=true
structureState=READY_FOR_CERTIFY
manifestDiskReconciliation=EXACT
foreignAppInfraDomainCoupling=ZERO
crossModuleBoundaryState=CONTRACTS_ONLY
microserviceExtractable=true
blockingResidualDebt=ZERO
guardsWeakened=NONE
baselinesWidened=NONE
automaticNextImplementationTask=NONE

Add minimal reconciliation metadata:
certificationReconciliation=W3_R1_BEHAVIOR_TRUTH_EXACT
evidenceW3R1=docs/architecture/evidence/TB-TMAR-CART-AMSC-001-W3-R1/
workflowStop=USER_REVIEW_CART_AMSC_001_W3_R1

F. DURABLE GUARD

Prefer extending CartModuleAmsc001W3CertGuardTests.

Minimum meaningful assertions:

W1 records bounded behavior repair.
final W3 behaviorChange != NONE.
final status truth != NONE where W1 proves 500->expected status remap.
27 declared/consumed != 26 Cart-owned descriptors; shared checkout code is consumed-not-registered.

No tautological assertions. No broad repo machinery.

G. EVIDENCE

Create:
docs/architecture/evidence/TB-TMAR-CART-AMSC-001-W3-R1/

At minimum:

certification-truth-reconciliation.md
validation.md

Record:

accepted W1 repair
stale W3/manifest wording
corrected final truth
exact status truth supported by W1
27/26/shared ownership truth
production delta ZERO
routes/DTO/error-code values/resources delta ZERO
schema/migration delta ZERO
manifest structural delta ZERO
certification preserved

BOUNDED VALIDATION ONLY

Parse changed JSON.
Validate manifest JSON.
Run focused Cart W3 certification guard.
Run only directly relevant Cart presentation/catalog guard if required for ownership proof.
Build only project containing changed guard if needed.
Git diff prove production/migrations/resx ZERO and manifest structural fields unchanged.
Verify commit/push state.

DO NOT run full solution tests.
DO NOT repair TmarSourceSizeAndInfraAppTests.
DO NOT touch .tmp-baseline.
DO NOT touch unrelated Order/AddressBook artifacts.
DO NOT repair other modules.

EXECUTION BOUND

One reconciliation pass + one focused validation pass.
If this R1 causes a focused failure, ONE direct correction only.
Unrelated/pre-existing failure => record and STOP.
No repair/test loop.

COMMIT/PUSH

If PASS:

one dedicated R1 commit
push origin/main
verify HEAD == origin/main
preserve unrelated untracked artifacts

EXPECTED FINAL STATE

COMPLETE_REFERENCE_PATTERN
ARCH-COMPLETE-002 STRUCTURE_CERTIFIED
MANIFEST_DISK_EXACT
CONTRACTS_ONLY
FOREIGN_APP_INFRA_DOMAIN_COUPLING_ZERO
MICROSERVICE_EXTRACTABLE
CERTIFICATION_TRUTH_RECONCILED

CANONICAL RESULT CONTRACT

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-CART-AMSC-001-W3-R1
Parent-Task: TB-TMAR-CART-AMSC-001-W3
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary: <bounded summary>
Starting-HEAD-State: 3C2119D7 | DIVERGED
W3-Implementation-State: PRESERVED | REGRESSED
Production-Code-Change-State: ZERO | VIOLATION
Schema-Migration-Change-State: ZERO | VIOLATION
Resource-Change-State: ZERO | VIOLATION
Manifest-Structural-Change-State: ZERO | VIOLATION
W1-Behavior-Repair-State: VERIFIED | CONFLICT
Behavior-Change-Truth-State: BOUNDED_DEFECT_REPAIR_RECORDED | STALE | CONFLICT
Status-Code-Truth-State: BOUNDED_EXPECTED_FAILURE_REMAP_RECORDED | STALE | CONFLICT
Declared-Or-Consumed-Code-Count-State: TWENTY_SEVEN | CONFLICT
Cart-Owned-Descriptor-Count-State: TWENTY_SIX | CONFLICT
Shared-Checkout-Authentication-State: FOUNDATION_OWNED_CONSUMED_NOT_REGISTERED | CONFLICT
Duplicate-Descriptor-Ownership-State: ZERO | CONFLICT
Routes-State: UNCHANGED | REGRESSED
Dto-Shape-State: UNCHANGED | REGRESSED
Error-Code-Value-State: UNCHANGED | REGRESSED
Cross-Module-Boundary-State: CONTRACTS_ONLY | REGRESSED
Foreign-App-Infra-Domain-Coupling-State: ZERO | REGRESSED
Manifest-Disk-State: EXACT | REGRESSED
Focused-Guard-State: PASS | FAIL
Focused-Build-State: PASS | NOT_REQUIRED | FAIL
Evidence-State: COMPLETE | INCOMPLETE
Recovery-SoT-State: RECONCILED | STALE | CONFLICT
Commit-SHA: <sha>
HEAD-Equals-Origin-Main: YES | NO
Working-Tree-State: CLEAN | CLEAN_EXCEPT_PREEXISTING_UNRELATED_ARTIFACTS | DIRTY_UNEXPECTED
User-Work-Preserved: YES | NO
Final-Certification-State: COMPLETE_REFERENCE_PATTERN_ARCH_COMPLETE_002_STRUCTURE_CERTIFIED | NOT_CERTIFIED
Automatic-Next-Implementation-Task-State: NONE
Workflow-Stop-State: USER_REVIEW_CART_AMSC_001_W3_R1
STOP
END_TOOBA_WORKER_RESULT

STOP RULE

STOP completely after result.
Do not create W4/R2.
Do not reopen Cart implementation.
Do not repair unrelated repository drift.
Do not start another task automatically.
Wait for Architect review.

END_TOOBA_TASK
