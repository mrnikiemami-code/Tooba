PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-CANON-005
Parent-Task: TB-TMAR-HOST-ADMIN-CANON-004
Parent-Commit: e693c432b515a385e97cc7acd275245222f125f3
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Host Canonicalization
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_ADMIN_CANONICALIZATION
Title: Move Order admin effective-access port/model authority from Order.Application to Order.Contracts

ARCHITECT ACCEPTANCE OF PARENT

TB-TMAR-HOST-ADMIN-CANON-004 is ARCHITECT-ACCEPTED at:
e693c432b515a385e97cc7acd275245222f125f3

Verified:

HostOrderAdminAuthorizer -> IAdminPanelAccess + neutral IAuthorizationService
AccessControl Application/Domain coupling = ZERO on authorizer
explicit deny = stable 403
unavailable = stable fail-closed 503
Host/Admin count = 15
CANON-003 preserved

KNOWN BASELINE DEBT (DO NOT REPAIR HERE)

Tooba.Order.Tests has a pre-existing missing AccessControl reference in an unrelated architecture guard.
composed error catalog has pre-existing duplicate reservation.policy.* descriptors.
build artifacts may exist locally; do not mutate/clean user work destructively.

These are OUT OF SCOPE.

GOAL

Repair ONLY the ownership of the Order admin effective-access contract consumed by:

src/backend/Host/Tooba.Host/Admin/HostOrderAdminEffectiveAccessReader.cs

Current defect:
the Host adapter depends on an Order.Application port/model:

Tooba.Order.Application.Admin.Operations.Ports
IOrderAdminEffectiveAccessReader
OrderAdminEffectiveAccess
OrderAdminPermissionGrant

The port/model are cross-boundary contracts and must live in Tooba.Order.Contracts.

THIS TASK DOES NOT YET REMOVE ACCESSCONTROL COUPLING.

That will be the next bounded task.

IN SCOPE

locate the authoritative current definitions of:
IOrderAdminEffectiveAccessReader
OrderAdminEffectiveAccess
OrderAdminPermissionGrant
move/re-home them into a coherent shallow capability path under Tooba.Order.Contracts
update all legitimate consumers to the Contracts namespace
update HostOrderAdminEffectiveAccessReader to consume Order.Contracts types

OUT OF SCOPE

Do NOT:

change behavior
change permission semantics
change AccessControl implementation
remove IAccessControlDirectory yet
remove AccessControl.Application/Domain yet
touch HostOrderAdminAuthorizer
touch other authorizers
change endpoints/routes
folder Host/Admin
touch DevActor
touch frontend/schema

TARGET STRUCTURE

Prefer a focused path such as:

Tooba.Order.Contracts/Admin/Operations/...

Keep:

one interface authority
one model authority
exact namespace/path alignment
no duplicate shim left in Order.Application unless a temporary compatibility shim is strictly required

If a compatibility shim is required:

document why
make it obsolete/internal if possible
do not leave duplicate public authority

BEHAVIOR PRESERVATION

Preserve exactly:

permission id values
DeniedByCeiling semantics
list ordering if observable
reader method signature semantics
all Order admin operations behavior

No new error paths.
No message parsing.
No new exception behavior.

ANTI-LOOP

ONE concern only.

Validation commands:

build Tooba.Order.Contracts
build Tooba.Host
run ONLY the focused CANON-005 guard + CANON-004 guard

If a command fails:

ONE repair attempt maximum
rerun ONLY that failed command once
second failure => INCOMPLETE + exact blocker + STOP

MAX_REPAIR_ITERATIONS = 1
MAX_VALIDATION_COMMAND_RUNS = 6
Do not run Order.Tests broad suite.
Do not run solution-wide tests.
Do not loop.

DURABLE GUARD

Prove:

HostOrderAdminEffectiveAccessReader.cs imports Order.Contracts, not Order.Application
authoritative IOrderAdminEffectiveAccessReader exists in Order.Contracts exactly once
authoritative OrderAdminEffectiveAccess exists in Order.Contracts exactly once
authoritative OrderAdminPermissionGrant exists in Order.Contracts exactly once
no public duplicate authority remains in Order.Application
current AccessControl coupling remains unchanged and explicitly deferred
Host/Admin count = 15
CANON-004 remains intact

EVIDENCE

Create only:
docs/evidence/TB-TMAR-HOST-ADMIN-CANON-005/

analyze.md
validation.md
closure.md

Persist task:
docs/ai/tasks/TB-TMAR-HOST-ADMIN-CANON-005.task.md

RECOVERY SOT

Append/update hostAdminCanon005:

parentCommit
orderEffectiveAccessContractOwner=ORDER_CONTRACTS
hostOrderEffectiveAccessOrderApplicationReference=ZERO
accessControlCoupling=DEFERRED_UNCHANGED
adminFileCount=15
canon004=PRESERVED
workflowStop=USER_REVIEW_HOST_ADMIN_CANON_005

SUCCESS CRITERIA

PASS only if:

Order effective-access cross-boundary authority is in Order.Contracts
Host has ZERO Order.Application reference on this reader path
behavior unchanged
focused validation passes
Host/Admin = 15
evidence/task/SoT committed and pushed
HEAD == origin/main
no tracked working-tree changes remain

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ADMIN-CANON-005
Parent-Task: TB-TMAR-HOST-ADMIN-CANON-004
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Canon004-Preservation-State:
Host-Admin-File-Count-Before:
Host-Admin-File-Count-After:
Order-EffectiveAccess-Port-Authority-State:
Order-EffectiveAccess-Model-Authority-State:
Host-OrderApplication-Reference-State:
OrderApplication-Duplicate-Public-Authority-State:
AccessControl-Coupling-State:
Behavior-State:
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

After result STOP completely.

Do not remove AccessControl coupling yet.
Do not start foldering or DevActor cleanup.
Wait for Architect review.

END_TOOBA_TASK