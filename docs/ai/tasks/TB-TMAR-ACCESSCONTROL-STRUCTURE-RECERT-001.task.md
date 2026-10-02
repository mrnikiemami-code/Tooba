PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ACCESSCONTROL-STRUCTURE-RECERT-001
Parent-Task: TB-TMAR-ACCESSCONTROL-STRUCTURE-REPAIR-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — AccessControl
Mode: CERTIFY_ONLY
Track: ACCESSCONTROL_STRUCTURE_RECERTIFICATION
Title: Re-certify AccessControl after capability-first structure repair
Estimated-Time-Minutes: 10
Hard-Timebox-Minutes: 15

ARCHITECT VERDICT

Parent task is ARCHITECT-ACCEPTED.

Verified on current main:

HEAD = 9e43f469032c44807d9b03eee43d0faa60fae481
AccessControl.Application is capability-first and shallow:
Access/
Assignments/
Bootstrap/
Ceiling/
Permissions/
Roles/
Development/Seller/
top-level technical request trees Commands/ and Queries/ are gone
no per-use-case single-file request leaf pattern remains in the touched Application surface
SoT records:
Structure-State = READY_FOR_CERTIFY
Folder-Granularity-State = PROFESSIONAL_SHALLOW
Technical-Axis-First-State = ZERO
Single-File-Request-Leaf-State = ZERO
certificationDrift = ACKNOWLEDGED_AND_PHYSICALLY_REPAIRED
finalRecertification = PENDING_SEPARATE_CERTIFY_TASK
Host final closure remains preserved
AccessControlDirectory remains OVERSIZED_ONLY/WATCH and is not a blocker for this task

SKILLS

Use:

.cursor/skills/tooba-architecture-structure/SKILL.md
.cursor/skills/tooba-architecture-certify/SKILL.md

Do not run Analyze/Migrate unless a concrete blocker requires STOP.

MANDATORY STRUCTURE GATE

Certify must require current Structure evidence for the same AccessControl touched surface:

Structure-State = READY_FOR_CERTIFY
Folder-Granularity-State = PROFESSIONAL_SHALLOW
Solution-Explorer-State = CANONICAL
Path-Namespace-State = EXACT
Physical-Copy-State = CLEAN
Root-Allowlist-State = ENFORCED
Technical-Axis-First-State = ZERO
Single-File-Request-Leaf-State = ZERO
Host-Final-Closure-State = PRESERVED

Do not infer this from build success alone.

CERTIFICATION SCOPE

Re-certify current Tooba.AccessControl.* under:

COMPLETE_REFERENCE_PATTERN
ARCH-COMPLETE-002 STRUCTURE CERTIFIED

Verify current state, not historical claims.

Mandatory checks:

exact physical tree
path↔namespace exact
root allowlists
no stale/duplicate copies
.slnx /Modules/AccessControl/ grouping
capability-first Application structure
module endpoint ownership
MediatR / ISender
validator coverage
Result / ApiResponseFactory canonical
semantic error codes + catalog/resx canonical
foreign Application/Infrastructure/Domain coupling = ZERO
cross-module persistence/join = ZERO
Host AccessControl residue = ZERO
Host final closure preserved
durable structure guard passes
no schema/behavior change

ACCESSCONTROL DIRECTORY WATCH

AccessControlDirectory.cs remains an OVERSIZED_ONLY WATCH item.
Do not split it unless it is proven to be a true cohesion violation.
Do not broaden scope for size alone.

FOCUSED VALIDATION ONLY

Run only focused AccessControl certification validation:

AccessControl.Contracts build
AccessControl.Domain build
AccessControl.Application build
AccessControl.Infrastructure build
AccessControl.Endpoints build
AccessControl architecture/structure guards
relevant AccessControl focused tests
manifest / SoT validation as needed

No full solution test run.
No frontend tests.
No open-ended repair loop.

CERTIFICATION RESULT

PASS only if all applicable gates pass and current physical structure agrees with Structure skill evidence.

Required final states:

Certification-State: ACCESSCONTROL_STRUCTURE_RECERTIFIED
Architecture-State: COMPLETE_REFERENCE_PATTERN
Lock-Version: ARCH-COMPLETE-002
Structure-Certified: true
Structure-State: READY_FOR_CERTIFY
Folder-Granularity-State: PROFESSIONAL_SHALLOW
Path-Namespace-State: EXACT
Root-Allowlist-State: ENFORCED
Physical-Copy-State: CLEAN
Solution-Explorer-State: CANONICAL
Technical-Axis-First-State: ZERO
Single-File-Request-Leaf-State: ZERO
Foreign-App-Infra-Domain-Coupling-State: ZERO
Cross-Module-Boundary-State: CONTRACTS_ONLY
Cross-Module-Persistence-Join-State: ZERO
Host-AccessControl-State: ZERO
Host-Final-Closure-State: PRESERVED
Behavior-Change-State: NONE
Schema-Change-State: NONE
Microservice-Extractable-State: TRUE

If any current violation exists:

Status must not be PASS
return exact blocker
do not weaken Structure/Certify rules
do not widen allowlists to force PASS

SOT / MANIFEST

On PASS:

update AccessControl certification state honestly to reflect this re-certification
supersede the prior stale structural claim with this new certification checkpoint
preserve historical records; do not erase history
preserve Host root final checkpoint
no automatic next task

EVIDENCE

Create:

docs/evidence/TB-TMAR-ACCESSCONTROL-STRUCTURE-RECERT-001/

At minimum:

structure-gate.md
physical-tree.md
path-namespace.md
solution-explorer.md
root-allowlist.md
coupling.md
endpoint-cqrs-validation.md
result-localization.md
host-closure.md
validation.md
certification-summary.md
recovery.md

Persist exact task:

docs/ai/tasks/TB-TMAR-ACCESSCONTROL-STRUCTURE-RECERT-001.task.md

CANONICAL RESULT CONTRACT

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-ACCESSCONTROL-STRUCTURE-RECERT-001
Parent-Task: TB-TMAR-ACCESSCONTROL-STRUCTURE-REPAIR-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary: <bounded summary>
Certification-State: ACCESSCONTROL_STRUCTURE_RECERTIFIED | NOT_CERTIFIED
Architecture-State: COMPLETE_REFERENCE_PATTERN | <state>
Lock-Version: ARCH-COMPLETE-002
Structure-Certified: true | false
Structure-State: READY_FOR_CERTIFY | <state>
Folder-Granularity-State: PROFESSIONAL_SHALLOW | <state>
Solution-Explorer-State: CANONICAL | <state>
Path-Namespace-State: EXACT | <state>
Physical-Copy-State: CLEAN | <state>
Root-Allowlist-State: ENFORCED | <state>
Technical-Axis-First-State: ZERO | PRESENT
Single-File-Request-Leaf-State: ZERO | PRESENT
Validator-Coverage-State: <exact current state>
Result-Pipeline-State: CANONICAL | <state>
Localization-State: CANONICAL | <state>
Foreign-App-Infra-Domain-Coupling-State: ZERO | <state>
Cross-Module-Boundary-State: CONTRACTS_ONLY | <state>
Cross-Module-Persistence-Join-State: ZERO | <state>
Host-AccessControl-State: ZERO | <state>
Host-Final-Closure-State: PRESERVED | REGRESSION
AccessControlDirectory-State: OVERSIZED_ONLY_WATCH | <state>
Behavior-Change-State: NONE | <state>
Schema-Change-State: NONE | <state>
Microservice-Extractable-State: TRUE | FALSE
Durable-Structure-Guard-State: PASS | FAIL
Focused-Build-State: PASS | FAIL
Focused-Test-State: PASS | FAIL
Manifest-State: UPDATED | UNCHANGED_VALID | CONFLICT
Recovery-State: UPDATED | CONFLICT
Evidence-Path: docs/evidence/TB-TMAR-ACCESSCONTROL-STRUCTURE-RECERT-001/
Implementation-Commit-SHA: <sha>
Docs-Stamp-SHA: <sha>
HEAD-Equals-Origin-Main: YES | NO
Working-Tree-State: CLEAN | <state>
User-Work-Preserved: YES | NO
Automatic-Next-Implementation-Task-State: NONE
Workflow-Stop-State: USER_REVIEW_ACCESSCONTROL_STRUCTURE_RECERT_001
STOP
END_TOOBA_WORKER_RESULT

STOP RULE

After result:
STOP completely.
Do not start another module/task.
Wait for Architect review.

END_TOOBA_TASK