PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-CANON-004
Parent-Task: TB-TMAR-HOST-ADMIN-CANON-003
Parent-Commit: 629c54a8abe54ad962a6c037b57a18e1858467b3
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Host Canonicalization
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_ADMIN_CANONICALIZATION
Title: Canonicalize HostOrderAdminAuthorizer onto IAdminPanelAccess + neutral authorization abstraction

ARCHITECT ACCEPTANCE OF PARENT

TB-TMAR-HOST-ADMIN-CANON-003 is ARCHITECT-ACCEPTED at:
629c54a8abe54ad962a6c037b57a18e1858467b3

Verified:

Support/Wallet panel gate = IAdminPanelAccess
Support/Wallet service locator = ZERO
capability Unavailable = fail-closed 503
fail-open = ZERO
Host/Admin count = 15
CANON-002 preserved

GOAL

Repair ONLY:
src/backend/Host/Tooba.Host/Admin/HostOrderAdminAuthorizer.cs

Current defects:

direct AdminPanelAccess.RequireAuthorizedAsync
direct Tooba.AccessControl.Application*
direct Tooba.AccessControl.Domain*
direct IAccessControlDirectory

The Host Order endpoint authorizer must become a thin platform adapter using:

IAdminPanelAccess for panel access
neutral IAuthorizationService / BuildingBlocks authorization primitives for permission checks

OUT OF SCOPE

Do NOT touch:

HostOrderAdminEffectiveAccessReader.cs
any other authorizer
AdminPanel files
foldering
DevActor
Order business logic
AccessControl module structure
frontend/schema

TARGET

Preferred constructor:

IAdminPanelAccess adminAccess
IAuthorizationService authorization
ICurrentTenant tenant

RequireAdminAsync:

delegate to adminAccess.RequireAuthorizedAsync(context.Request, ct)

RequirePermissionAsync:

actor from IAdminPanelAccess
evaluate the requested permission using the existing neutral authorization model
allow only on AuthorizationDecisionKind.Allow
preserve the existing stable Order denial semantics for explicit denial
fail closed on authorization unavailability with stable 503 semantics

MANDATORY AUDIT

Before edit, inspect:

IOrderAdminAuthorizer interface
current Order error-code catalog/contributor
existing Order permission IDs / expected authorization resource convention
existing tests of admin order permission behavior
existing neutral authorization examples in Host

Do not invent a second Order permission model if a canonical one already exists.

BEHAVIOR

Preserve:

actor return
endpoint interface/signatures
requested permissionId
tenant context
current explicit-denial code if already canonical
Order endpoint ownership

No message parsing.
No ex.Message.
No AccessControl Application/Domain references in HostOrderAdminAuthorizer.cs.

UNAVAILABLE

No fail-open.

If authorization service is unavailable:

use existing stable 503 code if already defined;
otherwise add the smallest Order-owned stable error code + descriptor required for this path.

Do not reuse a 403 code for 503.

ANTI-LOOP

ONE concern only.

Validation:

build Tooba.Host
build Tooba.Host.Tests
run ONLY CANON-004 guard/behavior tests + CANON-003 guard

For a failure:

ONE repair attempt maximum
rerun ONLY the failed command once
second failure => INCOMPLETE + exact blocker + STOP

MAX_REPAIR_ITERATIONS = 1
MAX_VALIDATION_COMMAND_RUNS = 6
No broad test suite.
No loop.

DURABLE GUARD

Prove:

HostOrderAdminAuthorizer uses IAdminPanelAccess
ZERO direct AdminPanelAccess.RequireAuthorizedAsync
ZERO Tooba.AccessControl.Application
ZERO Tooba.AccessControl.Domain
ZERO IAccessControlDirectory
ZERO service locator
neutral authorization abstraction used
explicit denial stable code preserved
Unavailable fail-closed 503
Host/Admin count = 15
CANON-003 preserved

EVIDENCE

Create only:
docs/evidence/TB-TMAR-HOST-ADMIN-CANON-004/

analyze.md
validation.md
closure.md

Persist task:
docs/ai/tasks/TB-TMAR-HOST-ADMIN-CANON-004.task.md

RECOVERY SOT

Append/update hostAdminCanon004:

parentCommit
orderAdminPanelGate=IADMINPANELACCESS
orderPermissionBoundary=NEUTRAL_AUTHORIZATION
accessControlApplicationReference=ZERO
accessControlDomainReference=ZERO
unavailable=FAIL_CLOSED
adminFileCount=15
canon003=PRESERVED
workflowStop=USER_REVIEW_HOST_ADMIN_CANON_004

SUCCESS CRITERIA

PASS only if all target states are proven, focused validation passes, commit is pushed, HEAD == origin/main, and working tree clean.

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ADMIN-CANON-004
Parent-Task: TB-TMAR-HOST-ADMIN-CANON-003
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Canon003-Preservation-State:
Host-Admin-File-Count-Before:
Host-Admin-File-Count-After:
OrderAdmin-IAdminPanelAccess-State:
OrderAdmin-NeutralAuthorization-State:
OrderAdmin-AccessControlApplication-State:
OrderAdmin-AccessControlDomain-State:
OrderAdmin-AccessControlDirectory-State:
OrderAdmin-ServiceLocator-State:
OrderAdmin-Allow-State:
OrderAdmin-Deny-State:
OrderAdmin-Unavailable-State:
OrderAdmin-Error-Code-State:
Focused-Validation:
Evidence-Path:
SoT-State:
Commit-SHA:
Push-State:
HEAD-Equals-Origin-Main:
Working-Tree:
User-Work-Preserved:
Repair-Iterations:
Validation-Command-Runs:
STOP
END_TOOBA_WORKER_RESULT

STOP RULE

After result STOP completely.
Do not touch HostOrderAdminEffectiveAccessReader.cs.
Do not start foldering or DevActor cleanup.
Wait for Architect review.

END_TOOBA_TASK