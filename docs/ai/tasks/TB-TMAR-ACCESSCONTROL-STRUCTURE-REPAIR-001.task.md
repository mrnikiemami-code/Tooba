PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ACCESSCONTROL-STRUCTURE-REPAIR-001
Parent-Task: TB-TMAR-ACCESSCONTROL-AMC-001-W5-CERT
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — AccessControl
Mode: STRUCTURE_REPAIR_ONLY
Track: ACCESSCONTROL_STRUCTURE_DRIFT_REPAIR
Title: Repair AccessControl Application foldering to capability-first shallow structure
Estimated-Time-Minutes: 12
Hard-Timebox-Minutes: 15

ARCHITECT VERDICT

Current Structure assessment is ACCEPTED:

Structure-State: REPAIR_REQUIRED
Folder-Granularity-State: MIXED
Primary defects:

TECHNICAL_AXIS_FIRST
OVER_FOLDERED
Certification-Drift-State: CONFIRMED

Current manifest claim structureCertified: true must NOT be treated as proof of current physical correctness.

Solution Explorer, path↔namespace, physical-copy cleanliness, root allowlist, Host final closure, and non-Application layers are currently acceptable and must be preserved.

SKILLS — MANDATORY

Apply in this order:

.cursor/skills/tooba-architecture-structure/SKILL.md
.cursor/skills/tooba-architecture-migrate/SKILL.md only as needed for safe physical moves / namespace repointing
.cursor/skills/tooba-architecture-certify/SKILL.md MUST NOT be run in this task

This task is a bounded STRUCTURE REPAIR, not final recertification.

PRIMARY SCOPE

src/backend/Modules/AccessControl/Tooba.AccessControl.Application/**

Read-only verification may inspect:

AccessControl.Contracts
AccessControl.Domain
AccessControl.Infrastructure
AccessControl.Endpoints
src/backend/Tooba.slnx
docs/architecture/tmar-current-state.json
docs/architecture/tmar-module-structure-manifests.json
AccessControl architecture guards/tests

Do NOT broaden into unrelated modules.
Do NOT reopen Host.
Do NOT modify frontend.

PROTECTED STATE

Preserve:

Host final closure
AccessControl business behavior
AccessControl routes
CQRS request/handler semantics
validator semantics
Result / ApiResponseFactory behavior
localization/error catalog behavior
Contracts-only cross-module boundary
ZERO foreign Application/Infrastructure/Domain coupling
schema/migrations
public Contracts
.slnx /Modules/AccessControl/ grouping
existing endpoint ownership

NO BUSINESS REDESIGN.

CURRENT DEFECT

Current Application tree is technical-axis-first and over-foldered, for example:

Application/
Commands/
CreateRole/
CreateRoleCommand.cs
UpdateRole/
UpdateRoleCommand.cs
Queries/
GetRole/
GetRoleQuery.cs

Each request/use-case leaf contains one production .cs file.

This is non-canonical under the dedicated Structure skill.

TARGET PRINCIPLE

CAPABILITY-FIRST, SHALLOW-BY-DEFAULT.

Use existing AccessControl semantics, not invented names.

Approved capability map for this repair:

Roles
Assignments
Permissions
Ceiling
Access
Bootstrap
Development/Seller

Target shape should be of this form:

Application/
Roles/
Commands/
Queries/
Validators/

Assignments/
Commands/
Queries/
Validators/

Permissions/
Commands/
Queries/
Validators/

Ceiling/
Commands/
Queries/
Validators/

Access/
Queries/
Models/ or Ports/ only if semantically owned there

Bootstrap/
Commands/

Development/
Seller/

Shared/cross-capability folders such as Composition, Exceptions, genuinely shared Models/Ports, or shared validation primitives may remain only when their responsibility is truly cross-capability and the Structure skill agrees.

Do NOT mechanically create empty folders.

DO NOT CREATE PER-USE-CASE LEAF FOLDERS

Forbidden default pattern:

Roles/Commands/CreateRole/CreateRoleCommand.cs

Preferred:

Roles/Commands/CreateRoleCommand.cs
Roles/Commands/UpdateRoleCommand.cs
Roles/Commands/CloneRoleCommand.cs

A deeper use-case folder is permitted only when it contains multiple cohesive production source files and materially improves clarity.

Count source files, not declared types.

MANDATORY REPAIR STEPS

Re-enumerate current AccessControl.Application physical tree.
Produce exact current request/query/validator → capability map.
Move Commands from top-level technical tree into capability-first shallow folders.
Move Queries likewise.
Move Validators so they align with the same capability axis.
Re-evaluate shared:
Authorization
Composition
Exceptions
Models
Permissions
Ports
Development/Seller
Keep only genuinely shared/cross-capability roots.
Update namespaces to exact path-derived namespaces.
Repoint all using statements/references.
Ensure no stale/duplicate physical copies remain.
Preserve project identity / assembly names / .slnx grouping.
Do not change request names, routes, semantics, authorization, validation rules, error codes, or persistence behavior.
Update/add scoped structure guard(s) so the old technical-axis-first + one-file-leaf pattern cannot return.

ACCESSCONTROL DIRECTORY WATCH

AccessControlDirectory.cs (~963 LOC) is currently classified OVERSIZED_ONLY / WATCH.

Do NOT split it in this task unless the Structure skill proves it is actually MULTI_RESPONSIBILITY_COHESION_VIOLATION.

Large-but-cohesive is not sufficient reason to broaden this task.

STRUCTURE SUCCESS STATES

Required at end:

Structure-State: READY_FOR_CERTIFY
Folder-Granularity-State: PROFESSIONAL_SHALLOW
Solution-Explorer-State: CANONICAL
Path-Namespace-State: EXACT
Physical-Copy-State: CLEAN
Root-Allowlist-State: ENFORCED
Technical-Axis-First-State: ZERO
Single-File-Request-Leaf-State: ZERO
Root-Dump-State: ZERO
Host-Final-Closure-State: PRESERVED
Foreign-App-Infra-Domain-Coupling-State: ZERO
Behavior-Change-State: NONE
Schema-Change-State: NONE

CERTIFICATION DRIFT / MANIFEST RULE

Do NOT claim final ARCH-COMPLETE-002 STRUCTURE_CERTIFIED in this task.

Do NOT silently use existing structureCertified: true as current proof.

Record that the prior certification drift was repaired physically and that final recertification is pending a separate Certify task.

Do not flip whole-module certification state unless the existing Structure skill / SoT protocol explicitly requires a drift marker. If SoT is updated, make the minimum honest update:

drift acknowledged
physical repair completed
final recert pending
no Host checkpoint change

DURABLE GUARD

Add or strengthen a scoped AccessControl structure guard that fails if:

Application/Commands/<UseCase>/*.cs returns as the primary tree
Application/Queries/<UseCase>/*.cs returns as the primary tree
a request/use-case leaf contains exactly one production source file without an explicit allowed exception
capability-first roots regress
path↔namespace diverges
stale copies remain

Guard must be semantic/scoped.
Do not use a repo-wide regex that breaks Resources, Migrations, generated folders, or legitimate special-purpose single-file directories.

FOCUSED VALIDATION

Run only focused validation:

AccessControl.Application build
AccessControl.Endpoints build if namespaces/references require it
AccessControl.Infrastructure build if references require it
AccessControl architecture/structure guards
relevant AccessControl module tests needed to prove no behavior break
.slnx grouping check only if touched (it should not need change)

No solution-wide test run.
No frontend tests.
No open-ended repair loop.

EVIDENCE

Create:

docs/evidence/TB-TMAR-ACCESSCONTROL-STRUCTURE-REPAIR-001/

Required:

physical-tree-before.md
physical-tree-after.md
capability-map.md
folder-granularity.md
path-namespace.md
stale-duplicate-copy.md
solution-explorer.md
root-allowlist.md
guard.md
validation.md
recovery.md

Persist exact task:

docs/ai/tasks/TB-TMAR-ACCESSCONTROL-STRUCTURE-REPAIR-001.task.md

RECOVERY / SOT

Preserve:

HOST_ROOT_FINAL_CERTIFIED
HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED
frontend frozen state
existing module behavior certifications not touched by this repair

Record this repair as a bounded structure-drift repair.
No automatic next implementation task.

CANONICAL RESULT CONTRACT

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-ACCESSCONTROL-STRUCTURE-REPAIR-001
Parent-Task: TB-TMAR-ACCESSCONTROL-AMC-001-W5-CERT
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary: <bounded summary>
Structure-State: READY_FOR_CERTIFY | REPAIR_REQUIRED | BLOCKED
Folder-Granularity-State: PROFESSIONAL_SHALLOW | MIXED | OVER_FOLDERED | TECHNICAL_AXIS_FIRST
Solution-Explorer-State: CANONICAL | <state>
Path-Namespace-State: EXACT | MISMATCH
Physical-Copy-State: CLEAN | STALE_COPY | DUPLICATE_COPY
Root-Allowlist-State: ENFORCED | VIOLATION
Technical-Axis-First-State: ZERO | PRESENT
Single-File-Request-Leaf-State: ZERO | PRESENT
Capability-Map-State: EXACT
AccessControlDirectory-State: OVERSIZED_ONLY_WATCH | <state>
Foreign-App-Infra-Domain-Coupling-State: ZERO | <state>
Host-Final-Closure-State: PRESERVED | REGRESSION
Behavior-Change-State: NONE | <state>
Schema-Change-State: NONE | <state>
Final-Recertification-State: PENDING_SEPARATE_CERTIFY_TASK
Durable-Structure-Guard-State: PASS | FAIL
Focused-Build-State: PASS | FAIL
Focused-Test-State: PASS | FAIL
Recovery-State: UPDATED | UNCHANGED_SAFE | CONFLICT
Evidence-Path: docs/evidence/TB-TMAR-ACCESSCONTROL-STRUCTURE-REPAIR-001/
Commit-SHA: <sha>
Push-State: PUSHED_ORIGIN_MAIN | NOT_PUSHED
HEAD-Equals-Origin-Main: YES | NO
Working-Tree-State: <state>
User-Work-Preserved: YES | NO
Automatic-Next-Implementation-Task-State: NONE
Workflow-Stop-State: USER_REVIEW_ACCESSCONTROL_STRUCTURE_REPAIR_001
STOP
END_TOOBA_WORKER_RESULT

STOP RULE

After result:
STOP completely.
Do not run final Certify.
Do not start another module.
Do not repair unrelated AccessControl debt.
Wait for Architect review.

END_TOOBA_TASK