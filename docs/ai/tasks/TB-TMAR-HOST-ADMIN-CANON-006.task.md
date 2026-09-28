PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-CANON-006
Parent-Task: TB-TMAR-HOST-ADMIN-CANON-005
Parent-Commit: bbe8d21404aa162d72037a5e6620998993503dfc
Implementation-Commit-Parent: 44dda80cea43dbff8e0b0be419511d62d90d786c
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Host Canonicalization
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_ADMIN_CANONICALIZATION
Title: Remove HostOrderAdminEffectiveAccessReader direct AccessControl Application/Domain coupling via a Contracts seam

ARCHITECT ACCEPTANCE OF PARENT

TB-TMAR-HOST-ADMIN-CANON-005 is ARCHITECT-ACCEPTED.

Verified:

Order effective-access port/model authority = Tooba.Order.Contracts.Admin.Operations
Host reader Order.Application reference = ZERO
duplicate public authority in Order.Application = NONE
behavior unchanged
Host/Admin = 15
CANON-004 preserved

NOTE:
The worker produced implementation commit
44dda80cea43dbff8e0b0be419511d62d90d786c
and a final closure-record commit
bbe8d21404aa162d72037a5e6620998993503dfc.
Use current origin/main as authority.

GOAL

Repair ONLY:
src/backend/Host/Tooba.Host/Admin/HostOrderAdminEffectiveAccessReader.cs

Current remaining defect:
the Host reader still directly imports/consumes:

Tooba.AccessControl.Application
Tooba.AccessControl.Application.Models
Tooba.AccessControl.Application.Permissions
Tooba.AccessControl.Domain
IAccessControlDirectory
AccessOwnerScope
AccessOwnerScopeKind

Host must not depend directly on a foreign module's Application/Domain.

TARGET

Introduce or reuse the SMALLEST lawful AccessControl cross-module Contracts/Gate seam that exposes exactly the effective-access data needed by the Host Order adapter.

Required semantics to preserve:

actor user id
platform-scoped access
current tenant id when present
effective permission ids
DeniedByCeiling
observable ordering if currently relied upon

Then make HostOrderAdminEffectiveAccessReader depend only on:

BuildingBlocks / neutral platform abstractions
Tooba.AccessControl.Contracts (or an already-existing canonical AccessControl contract/gate if discovered)
Tooba.Order.Contracts.Admin.Operations

MANDATORY AUDIT FIRST

Before creating anything:

search for any existing AccessControl Contracts/Gate project or effective-access public boundary;
search all consumers of IAccessControlDirectory.GetEffectiveAccessAsync;
inspect AccessControl DI ownership;
inspect current AccessOwnerScope / effective-access DTO shape.

If a lawful existing seam exists, REUSE it.

If none exists:
create the smallest focused AccessControl.Contracts seam required for this read only.

DO NOT expose:

AccessControl Domain entities
Application models
IQueryable/DbContext
Host types
Order types

BOUNDARY SHAPE

Prefer a small neutral AccessControl contract, for example conceptually:

effective-access request/scope DTO
effective permission grant DTO (PermissionId, DeniedByCeiling)
effective-access reader interface

Names must follow the repository's existing naming conventions discovered during audit.
Do not blindly use these example names if a canonical convention exists.

IMPLEMENTATION OWNERSHIP

The adapter/implementation of the AccessControl Contracts seam must remain owned by AccessControl.
Host must only consume the contract.

Do not move AccessControl business logic into Host.

HOST RESULT

After this task HostOrderAdminEffectiveAccessReader.cs MUST have:

ZERO Tooba.AccessControl.Application
ZERO Tooba.AccessControl.Domain
ZERO IAccessControlDirectory
ZERO AccessControl Application/Domain model types
Contracts-only AccessControl dependency
Order.Contracts dependency preserved

BEHAVIOR

Preserve exact OrderAdminEffectiveAccess mapping:

PermissionId unchanged
DeniedByCeiling unchanged
same actor/scope semantics
no new authorization decision policy
no new error semantics

This task is read/mapping boundary repair only.

OUT OF SCOPE

Do NOT:

change HostOrderAdminAuthorizer
change Support/Wallet/etc.
redesign AccessControl module broadly
move AccessControl role/permission ownership
fix unrelated Order.Tests baseline
fix reservation.policy.* duplicate catalog debt
folder Host/Admin
touch DevActor
touch frontend/schema

ANTI-LOOP / EXECUTION BUDGET

ONE concern only.

Validation commands:

build the new/reused AccessControl Contracts project (if a project exists/is added)
build Tooba.Host
run ONLY CANON-006 guard + CANON-005 guard

If step 1 is not applicable because an existing contract project is reused, replace it with one focused build of the owning AccessControl project.

On failure:

maximum ONE repair attempt
rerun ONLY the exact failing command once
second failure => INCOMPLETE + exact blocker + STOP

MAX_REPAIR_ITERATIONS = 1
MAX_VALIDATION_COMMAND_RUNS = 6

Do not run broad Order.Tests.
Do not run solution-wide tests.
Do not loop.

DURABLE GUARD

Prove:

HostOrderAdminEffectiveAccessReader has ZERO AccessControl.Application
ZERO AccessControl.Domain
ZERO IAccessControlDirectory
AccessControl dependency is Contracts/Gate only
Order effective-access authority still in Order.Contracts
PermissionId mapping preserved
DeniedByCeiling mapping preserved
AccessControl implementation remains module-owned
no Host type leaks into AccessControl Contracts
Host/Admin = 15
CANON-005 preserved

EVIDENCE

Create only:
docs/evidence/TB-TMAR-HOST-ADMIN-CANON-006/

analyze.md
boundary-map.md
validation.md
closure.md

Persist task:
docs/ai/tasks/TB-TMAR-HOST-ADMIN-CANON-006.task.md

RECOVERY SOT

Append/update hostAdminCanon006:

parentCommit
hostOrderEffectiveAccessAccessControlApplication=ZERO
hostOrderEffectiveAccessAccessControlDomain=ZERO
hostOrderEffectiveAccessDirectory=ZERO
accessControlBoundary=CONTRACTS_OR_CANONICAL_GATE
orderContractsAuthority=PRESERVED
adminFileCount=15
canon005=PRESERVED
workflowStop=USER_REVIEW_HOST_ADMIN_CANON_006

SUCCESS CRITERIA

PASS only if:

Host reader is Contracts/Gate-only toward AccessControl
direct AccessControl Application/Domain coupling = ZERO
DeniedByCeiling semantics preserved exactly
no broad redesign
focused validation passes
Host/Admin remains 15
evidence/task/SoT committed and pushed
HEAD == origin/main
tracked working tree clean

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ADMIN-CANON-006
Parent-Task: TB-TMAR-HOST-ADMIN-CANON-005
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Canon005-Preservation-State:
Host-Admin-File-Count-Before:
Host-Admin-File-Count-After:
AccessControl-Existing-Seam-State:
AccessControl-Contract-Seam-State:
Host-AccessControlApplication-State:
Host-AccessControlDomain-State:
Host-AccessControlDirectory-State:
Host-AccessControlBoundary-State:
OrderContracts-Authority-State:
PermissionId-Semantics-State:
DeniedByCeiling-Semantics-State:
AccessControl-Implementation-Ownership-State:
CrossModule-Forbidden-Edge-State:
Focused-Validation:
Evidence-Path:
SoT-State:
Commit-SHA:
Push-State:
HEAD-Equals-Origin-Main:
Working-Tree-Tracked-State:
User-Work-Preserved:
Repair-Iterations:
Validation-Command-Runs:
STOP
END_TOOBA_WORKER_RESULT

STOP RULE

After result:
STOP COMPLETELY.

Do not start Host/Admin foldering.
Do not start DevActor cleanup.
Wait for Architect review.

END_TOOBA_TASK