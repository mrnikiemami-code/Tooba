PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ORDER-AMC-001-W5-R1
Parent-Task: TB-TMAR-ORDER-AMC-001-W5
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Order AMSC
Mode: CERTIFICATION_RECONCILIATION_REPAIR
Track: ORDER_AMSC
Title: Reconcile Order structure manifest with certified disk state and reassert final certification

ARCHITECT VERDICT

W5 typed-fault implementation is ARCHITECT-ACCEPTED.

The W4 message-classification blocker is closed in production:

Order expected faults use ContractOperationException(code)
StorefrontOrderResult classifies by ex.Code
AdminOrderFulfillmentOperations classifies by ex.Code
production message-text classification blocker is ZERO

Do NOT reopen W5 production implementation.

One certification-package defect remains:
the Order entry in docs/architecture/tmar-module-structure-manifests.json does not fully match the already-verified W4/W5 disk state.

CURRENT VERIFIED MAIN

Expected starting HEAD:
e648f32e521d5a6450346643d743cd99e719ea29

Current accepted implementation state:

COMPLETE_REFERENCE_PATTERN implementation: READY
Structure-State: READY_FOR_CERTIFY
Folder-Granularity-State: PROFESSIONAL_SHALLOW
Solution-Explorer-State: CANONICAL /Modules/Order/
Path-Namespace-State: EXACT
foreignAppInfraDomainCoupling: ZERO
messageClassificationState: ZERO
Host final closure: PRESERVED

KNOWN CERTIFICATION DEFECT

Current Order manifest entry contains only:

Tooba.Order.Application
Tooba.Order.Endpoints
Tooba.Order.Infrastructure

But the certified production surface is five projects:

Tooba.Order.Domain
Tooba.Order.Contracts
Tooba.Order.Application
Tooba.Order.Endpoints
Tooba.Order.Infrastructure

Current disk root .cs state is:

Domain: GlobalUsings.cs
Contracts: none
Application: GlobalUsings.cs
Endpoints: OrderEndpointModule.cs
Infrastructure: GlobalUsings.cs + OrderModule.cs

Current manifest rootAllowlist mismatch:

Application currently [] but disk/W4 evidence = GlobalUsings.cs
Infrastructure currently OrderModule.cs only but disk/W4 evidence = GlobalUsings.cs + OrderModule.cs
Domain missing from manifest
Contracts missing from manifest

This means the W5 claim that the manifest has exact root allowlists is not yet durable.

OBJECTIVE

Perform one bounded certification reconciliation repair only.

Make the Order manifest exactly describe the already-certified current disk state, add one durable manifest↔disk guard, correct the W5 evidence wording, reconcile SoT, and reassert the final Certify verdict.

NO production behavior change.

SCOPE

Allowed:

docs/architecture/tmar-module-structure-manifests.json
docs/architecture/tmar-current-state.json
docs/architecture/evidence/TB-TMAR-ORDER-AMC-001-W5/certification.md
docs/architecture/evidence/TB-TMAR-ORDER-AMC-001-W5/typed-fault-migration.md only if needed for wording consistency
one focused Order architecture guard for manifest↔disk equality
exact task artifact:
docs/ai/tasks/TB-TMAR-ORDER-AMC-001-W5-R1.task.md
R1 evidence under:
docs/architecture/evidence/TB-TMAR-ORDER-AMC-001-W5-R1/

Forbidden:

any production .cs change
Domain/Application/Contracts/Endpoints/Infrastructure implementation change
route/status/response change
schema/migration/database change
frontend
Host production change
Fulfillment production change
Payment production change
refactor/rename/move
unrelated test repair
full repository test suite
broad baseline updates
weakening any existing guard
new architecture exemption

REQUIRED REPAIR

A. MANIFEST — ORDER MUST HAVE EXACTLY FIVE PRODUCTION PROJECT ENTRIES

Reconcile the single Order manifest entry to include exactly:

Tooba.Order.Domain
Tooba.Order.Contracts
Tooba.Order.Application
Tooba.Order.Endpoints
Tooba.Order.Infrastructure

Do not create a duplicate Order module entry.

Required rootAllowlist truth:

Tooba.Order.Domain:
["GlobalUsings.cs"]

Tooba.Order.Contracts:
[]

Tooba.Order.Application:
["GlobalUsings.cs"]

Tooba.Order.Endpoints:
["OrderEndpointModule.cs"]

Tooba.Order.Infrastructure:
["GlobalUsings.cs", "OrderModule.cs"]

Preserve valid existing forbiddenRootFiles / forbiddenTopLevelFolders.
For newly represented Domain/Contracts, derive only minimal truthful structure locks from current disk and W4 structure evidence.
Do not invent cosmetic restrictions unrelated to the certified surface.

Keep:

structureCertified: true
lockVersion: ARCH-COMPLETE-002

B. DURABLE MANIFEST↔DISK GUARD

Add one focused architecture guard:
Order manifest project-set + rootAllowlist must equal current disk truth.

The guard must fail if:

any of the five production projects is missing from manifest
an extra production Order project is represented incorrectly
rootAllowlist differs from actual top-level production .cs files
duplicate Order manifest entries exist
structureCertified != true
lockVersion != ARCH-COMPLETE-002

The guard must read the real manifest and the real project directories.
Do not hard-code a PASS unrelated to disk state.

C. W5 EVIDENCE WORDING CORRECTION

Correct only the inaccurate wording around shipping_service.*.

Do NOT claim the typed code dependency disappeared.

Canonical wording should communicate:

message-text dependency/classification was removed
typed contract-code classification remains allowed/preserved
shipping_service.* is interpreted only from ContractOperationException.Code, never Exception.Message

Do not reopen the W5 blocker.

D. SoT RECONCILIATION

Keep orderAmc001 final state:

state = W5_CERTIFY_COMPLETE_REFERENCE_PATTERN
completeReferencePattern = true
structureState = READY_FOR_CERTIFY
pathNamespace = EXACT
foreignAppInfraDomainCoupling = ZERO
messageClassificationState = ZERO
certificationVerdict = COMPLETE_REFERENCE_PATTERN_STRUCTURE_CERTIFIED
certificationBlockers = []
manifestPromoted = true
microserviceExtractable = true
automaticNextImplementationTask = NONE

Add minimal R1 reconciliation fields only if repository convention supports them, for example:

certificationReconciliation = W5_R1_MANIFEST_DISK_EXACT
evidenceW5R1 = docs/architecture/evidence/TB-TMAR-ORDER-AMC-001-W5-R1/
workflowStop = USER_REVIEW_ORDER_AMC_001_W5_R1

Do not create a new implementation wave.

E. R1 EVIDENCE

Create:
docs/architecture/evidence/TB-TMAR-ORDER-AMC-001-W5-R1/

At minimum:

manifest-reconciliation.md
validation.md

manifest-reconciliation.md must show:

exactly one Order manifest entry
exactly five production project entries
expected vs actual root .cs equality for all five
W5 production files unchanged
certification reassertion rationale

VALIDATION

Focused only.

Required:

JSON parse:

docs/architecture/tmar-module-structure-manifests.json
docs/architecture/tmar-current-state.json

Run the new focused Order manifest↔disk guard.

Run existing focused Order structure/certification guards needed to prove no structural regression.
Do not run the full repository suite.

Build only if the new/changed guard requires compilation:

Tooba.Order.Tests or the exact project that owns the guard
No full solution build unless compilation dependency requires it.

Search proof:

Order manifest entries = exactly 1
Order manifest project entries = exactly 5
production changes in W5-R1 = ZERO
message-based classification in Modules/Order remains ZERO

STOP CONDITIONS

If manifest/disk truth can be reconciled deterministically:

repair once
validate once
commit/push once
return result
STOP

If any unexpected contradiction is found in current disk state:

do NOT start a repair loop
do NOT change production
return RECOVERY_CONFLICT with the exact contradiction
STOP

Do not troubleshoot unrelated pre-existing Host failures.

EXPECTED FINAL STATE

If all gates pass:

COMPLETE_REFERENCE_PATTERN
+
ARCH-COMPLETE-002 STRUCTURE_CERTIFIED
+
MANIFEST_DISK_EXACT

W5 remains the accepted implementation commit.
W5-R1 is certification/docs/guard reconciliation only.

CANONICAL RESULT CONTRACT

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-ORDER-AMC-001-W5-R1
Parent-Task: TB-TMAR-ORDER-AMC-001-W5
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary: <bounded summary>
Starting-HEAD-State: E648F32E | DIVERGED
W5-Implementation-State: PRESERVED | REGRESSED
Production-Code-Change-State: ZERO | VIOLATION
Order-Manifest-Entry-Count-State: ONE | INVALID
Order-Manifest-Project-Count-State: FIVE | INVALID
Order-Domain-Manifest-State: EXACT | INVALID
Order-Contracts-Manifest-State: EXACT | INVALID
Order-Application-Manifest-State: EXACT | INVALID
Order-Endpoints-Manifest-State: EXACT | INVALID
Order-Infrastructure-Manifest-State: EXACT | INVALID
Manifest-Disk-Equality-State: EXACT | MISMATCH
Structure-Certified-State: TRUE | INVALID
Lock-Version-State: ARCH-COMPLETE-002 | INVALID
Message-Classification-State: ZERO | REGRESSION
Foreign-App-Infra-Domain-Coupling-State: ZERO | REGRESSION
W5-Evidence-Wording-State: CORRECTED | STALE
Recovery-SoT-State: RECONCILED | STALE | CONFLICT
Focused-Guard-State: PASS | FAIL
Focused-Build-State: PASS | NOT_REQUIRED | FAIL
Evidence-State: COMPLETE | INCOMPLETE
Docs-Commit-SHA: <sha>
HEAD-Equals-Origin-Main: YES | NO
Working-Tree-State: CLEAN | CLEAN_EXCEPT_PREEXISTING_USER_WORK | DIRTY_UNEXPECTED
User-Work-Preserved: YES | NO
Final-Certification-State: COMPLETE_REFERENCE_PATTERN_ARCH_COMPLETE_002_STRUCTURE_CERTIFIED | NOT_CERTIFIED
Automatic-Next-Implementation-Task-State: NONE
Workflow-Stop-State: USER_REVIEW_ORDER_AMC_001_W5_R1
STOP
END_TOOBA_WORKER_RESULT

STOP RULE

After result:
STOP completely.
Do not start another Order wave.
Do not repair unrelated Host failures.
Do not create another task automatically.
Wait for Architect review.

END_TOOBA_TASK
