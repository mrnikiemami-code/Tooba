PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ADDRESSBOOK-AMSC-001-W3-R1
Parent-Task: TB-TMAR-ADDRESSBOOK-AMSC-001-W3
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: CERTIFICATION_TRUTH_RECONCILIATION
Title: AddressBook W3 certification truth reconciliation

ARCHITECT VERDICT

AddressBook production implementation and structure are ACCEPTED. Do NOT reopen W1/W2 implementation.

Only one certification-truth defect remains:
W1 correctly records the accepted bounded expected-failure repair:

missing/foreign address: 500 platform.unexpected -> 404 customer.address.missing
actor / escaped field-shape faults: 500 -> catalogued 400 customer.address.*

W3/SoT incorrectly records:

behaviorChange = NONE
statusCodesChanged = NONE

This task reconciles documentation/SoT/guard truth only. Runtime behavior must not change.

EXPECTED STARTING HEAD
3312a436c05e96120bb6dc686ae545b5857e029a

PRECHECK

Verify HEAD == origin/main.
Re-read W1 evidence, W3 certification evidence, addressBookModuleAmsc001W1 and addressBookModuleAmsc001W3.
Confirm the contradiction still exists.
If repository truth materially differs, return RECOVERY_CONFLICT and STOP.

ALLOWED SCOPE

docs/architecture/tmar-current-state.json
docs/architecture/evidence/TB-TMAR-ADDRESSBOOK-AMSC-001-W3/*
docs/architecture/evidence/TB-TMAR-ADDRESSBOOK-AMSC-001-W3-R1/*
extend ONE existing focused AddressBook certification guard if needed
task/result artifacts required by repository convention

FORBIDDEN

ANY production source change
Domain/Application/Contracts/Endpoints/Infrastructure behavior change
routes/DTO/error-code values/resources/schema/migrations
Host production changes
manifest structural redesign
unrelated repo-wide drift repair
baseline widening or guard weakening
full repository test suite
another implementation wave

REQUIRED REPAIR

A. BEHAVIOR TRUTH
W3 must no longer claim behaviorChange = NONE.
Prefer repository vocabulary if canonical; otherwise use a precise equivalent of:
BOUNDED_DEFECT_REPAIR_EXPECTED_FAILURE_MAPPING

Evidence must state:

success behavior/payload preserved
routes preserved
DTO shapes preserved
schema preserved
expected failure semantics intentionally changed
missing/foreign address: unexpected 500 -> catalogued 404
actor/escaped field-shape faults: unexpected 500 -> catalogued 400

B. STATUS-CODE TRUTH
W3 must no longer claim statusCodesChanged = NONE where this field means externally observable failure HTTP status.
Prefer repository vocabulary; otherwise use an equivalent of:
BOUNDED_EXPECTED_FAILURE_REMAP_500_TO_404_400

Keep routesChanged=NONE, dtoShapeChanged=NONE, schemaChange=NONE.

C. CODE / DESCRIPTOR COUNT TRUTH
Re-discover before editing and reconcile wording exactly:

W1 new AddressBook-owned stable codes = 11
pre-existing AddressBook-owned customer.address.missing remains
total AddressBook-owned catalog/resource codes after W1 = 12
customer.session.required is shared/Foundation-owned, consumed but NOT registered by AddressBook
duplicate descriptor ownership = ZERO
Do not invent a 13th AddressBook-owned descriptor.

D. CERTIFICATION REMAINS VALID
Do NOT revoke certification because the accepted defect repair changed expected-failure semantics.
Preserve if validation passes:

verdict = COMPLETE_REFERENCE_PATTERN
lockVersion = ARCH-COMPLETE-002
structureCertified = true
structureState = READY_FOR_CERTIFY
manifestDiskReconciliation = EXACT
foreignAppInfraDomainCoupling = ZERO
crossModuleBoundaryState = CONTRACTS_ONLY
microserviceExtractable = true
blockingResidualDebt = ZERO
guardsWeakened = NONE
baselinesWidened = NONE
automaticNextImplementationTask = NONE

Add minimal reconciliation metadata if consistent with current SoT convention:

certificationReconciliation = W3_R1_BEHAVIOR_TRUTH_EXACT
evidenceW3R1 = docs/architecture/evidence/TB-TMAR-ADDRESSBOOK-AMSC-001-W3-R1/
workflowStop = USER_REVIEW_ADDRESSBOOK_AMSC_001_W3_R1

E. DURABLE GUARD
Prefer extending AddressBookModuleAmsc001W3CertGuardTests rather than creating broad machinery.
Only if current guards do not already lock the corrected truth, add assertions proving final certification cannot regress to behaviorChange=NONE/statusCodesChanged=NONE while W1 records the accepted 500->404/400 repair.
No tautological self-comparison.

F. EVIDENCE
Create:
docs/architecture/evidence/TB-TMAR-ADDRESSBOOK-AMSC-001-W3-R1/

At minimum:

certification-truth-reconciliation.md
validation.md

Show:

accepted W1 behavior repair
stale W3 values before repair
corrected W3/SoT values
11 new / 12 total owned / shared session consumed-not-owned count truth
production delta ZERO
migration delta ZERO
final certification preserved

BOUNDED VALIDATION ONLY

Parse changed JSON.
Run focused AddressBook W3 certification guard.
Run only directly relevant AddressBook architecture/presentation guards if necessary.
Build only the project containing a changed guard if compilation is needed.
Git diff prove production source changes ZERO, migrations ZERO, manifest structural changes ZERO.
No full solution test suite.
Do not repair Catalog/SoT/.tmp-baseline repo-wide drift.
Do not touch unrelated Order worker-result artifact.

EXECUTION BOUND
One reconciliation pass + one focused validation pass.
If this R1 itself causes a focused failure, allow ONE direct correction only.
If failure is unrelated/pre-existing, record it and STOP.
No repair/test loop.

COMMIT/PUSH
If PASS:

one dedicated R1 commit
push origin/main
verify HEAD == origin/main
preserve unrelated untracked user artifacts

EXPECTED FINAL STATE
COMPLETE_REFERENCE_PATTERN
ARCH-COMPLETE-002 STRUCTURE CERTIFIED
MANIFEST_DISK_EXACT
CONTRACTS_ONLY
FOREIGN_APP_INFRA_DOMAIN_COUPLING_ZERO
MICROSERVICE_EXTRACTABLE
CERTIFICATION_TRUTH_RECONCILED

CANONICAL RESULT CONTRACT

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-ADDRESSBOOK-AMSC-001-W3-R1
Parent-Task: TB-TMAR-ADDRESSBOOK-AMSC-001-W3
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary: <bounded summary>
Starting-HEAD-State: 3312A436 | DIVERGED
W3-Implementation-State: PRESERVED | REGRESSED
Production-Code-Change-State: ZERO | VIOLATION
Schema-Migration-Change-State: ZERO | VIOLATION
Manifest-Structure-Change-State: ZERO | DOCUMENTATION_ONLY | VIOLATION
W1-Behavior-Repair-State: VERIFIED | CONFLICT
Behavior-Change-Truth-State: BOUNDED_DEFECT_REPAIR_RECORDED | STALE | CONFLICT
Status-Code-Truth-State: BOUNDED_500_TO_404_400_RECORDED | STALE | CONFLICT
AddressBook-New-Code-Count-State: ELEVEN | CONFLICT
AddressBook-Owned-Descriptor-Count-State: TWELVE | CONFLICT
Shared-Session-Code-State: CONSUMED_NOT_OWNED | CONFLICT
Duplicate-Descriptor-Ownership-State: ZERO | CONFLICT
Routes-State: UNCHANGED | REGRESSED
Dto-Shape-State: UNCHANGED | REGRESSED
Cross-Module-Boundary-State: CONTRACTS_ONLY | REGRESSED
Foreign-App-Infra-Domain-Coupling-State: ZERO | REGRESSED
Manifest-Disk-State: EXACT | REGRESSED
Focused-Guard-State: PASS | FAIL
Focused-Build-State: PASS | NOT_REQUIRED | FAIL
Evidence-State: COMPLETE | INCOMPLETE
Recovery-SoT-State: RECONCILED | STALE | CONFLICT
Commit-SHA: <sha>
HEAD-Equals-Origin-Main: YES | NO
Working-Tree-State: CLEAN | CLEAN_EXCEPT_PREEXISTING_UNRELATED_ARTIFACT | DIRTY_UNEXPECTED
User-Work-Preserved: YES | NO
Final-Certification-State: COMPLETE_REFERENCE_PATTERN_ARCH_COMPLETE_002_STRUCTURE_CERTIFIED | NOT_CERTIFIED
Automatic-Next-Implementation-Task-State: NONE
Workflow-Stop-State: USER_REVIEW_ADDRESSBOOK_AMSC_001_W3_R1
STOP
END_TOOBA_WORKER_RESULT

STOP RULE
After result STOP completely.
Do not create W4.
Do not reopen AddressBook implementation.
Do not repair unrelated repository drift.
Do not start another task automatically.
Wait for Architect review.

END_TOOBA_TASK
