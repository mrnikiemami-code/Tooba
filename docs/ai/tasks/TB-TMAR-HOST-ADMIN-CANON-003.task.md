PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-CANON-003
Parent-Task: TB-TMAR-HOST-ADMIN-CANON-002
Parent-Commit: de831c331c90660168bd744faf336c8a121465de
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Host Canonicalization
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_ADMIN_CANONICALIZATION
Title: Close Support/Wallet Admin capability fail-open and unify their Host authorization path

ARCHITECT ACCEPTANCE OF PARENT

TB-TMAR-HOST-ADMIN-CANON-002 is ARCHITECT-ACCEPTED at:
de831c331c90660168bd744faf336c8a121465de

Verified:

Payment/Promotion/Return/Settlement adapters delegate to IAdminPanelAccess
in-scope service locator = ZERO
Settlement duplicate marketplace logic = ZERO
Host/Admin count = 15
CANON-001 preserved

GOAL

Fix ONLY the Support and Wallet Host Admin authorizers.

Current defect:
both authorizers perform capability checks after panel authorization, but currently fail-open on:
AuthorizationDecisionKind.Unavailable

This task must make capability enforcement explicit, stable, and fail-closed while preserving existing endpoint contracts.

IN-SCOPE FILES

HostSupportAdminAuthorizer.cs
HostWalletAdminAuthorizer.cs
their tests/guards
DI only if required

OUT OF SCOPE

Do NOT touch:

HostOrderAdminAuthorizer
HostOrderAdminEffectiveAccessReader
HostPaymentAdminAuthorizer
HostPromotionAdminAuthorizer
HostReturnAdminAuthorizer
HostSettlementAdminAuthorizer
AdminPanelAccess
HostAdminPanelAccess
AdminPanelComposer
foldering
DevActor
module business logic

AUTHORIZATION TARGET

Both Support and Wallet must:

obtain the admin actor via existing IAdminPanelAccess;
evaluate the module capability using the existing authorization abstraction;
ALLOW only on AuthorizationDecisionKind.Allow;
DENY on explicit denial using the module’s existing stable error code;
FAIL CLOSED on AuthorizationDecisionKind.Unavailable with a stable 503 semantic error code.

Do NOT silently continue when the capability service is unavailable.

IADMINPANELACCESS

Replace direct AdminPanelAccess.RequireAuthorizedAsync(...) and RequestServices resolution with constructor-injected IAdminPanelAccess where compatible.

Preferred constructor dependencies:

IAdminPanelAccess
IAuthorizationService
ICurrentTenant

No HttpContext service locator.

ERROR CODES

Audit existing Support/Wallet error catalogs before adding anything.

For explicit capability denial:

preserve the existing module stable denial codes.

For authorization service unavailable:

reuse an existing stable platform/module code if one already exists;
otherwise add the smallest appropriate stable code in the owning module’s error catalog/resource surface.

Do NOT use ex.Message.
Do NOT use localized text as classification.
Do NOT reuse a 403 code for 503 unavailability.

EDITION / TENANT SEMANTICS

Preserve current SingleStore capability context semantics unless an existing platform abstraction already provides a more canonical equivalent.

Do not invent Marketplace capability semantics in this task.
If current Support/Wallet capability enforcement cannot lawfully operate for Marketplace without broader design, document that as a precise limitation; do not broaden scope.

SECURITY REQUIREMENT

After this task there must be ZERO fail-open branches in Support/Wallet capability checks.

Specifically forbidden:

C#
if (decision.Kind == AuthorizationDecisionKind.Unavailable)
    return;

and equivalent logic.

ANTI-LOOP / EXECUTION BUDGET

One concern only.

Validation commands:

build Tooba.Host
build Tooba.Host.Tests
run ONLY focused Support/Wallet authorizer tests + CANON-002 guard

For a failing command:

maximum ONE repair attempt
rerun ONLY that exact failing command once
if it fails again: return INCOMPLETE and STOP

MAX_REPAIR_ITERATIONS = 1
MAX_VALIDATION_COMMAND_RUNS = 6

Do not run broad Host tests.
Do not run solution-wide tests.
Do not loop.

DURABLE GUARD

Add one focused guard proving:

Support authorizer depends on IAdminPanelAccess
Wallet authorizer depends on IAdminPanelAccess
ZERO RequestServices
ZERO direct AdminPanelAccess.RequireAuthorizedAsync
ZERO AuthorizationDecisionKind.Unavailable => return
Allow path exists
Deny path uses stable module code
Unavailable path returns/throws stable 503 semantic error
Host/Admin count remains 15
CANON-002 remains intact

BEHAVIOR TESTS

Add/adjust focused tests for BOTH Support and Wallet:

Allow -> actor returned
Deny -> 403 + existing stable denial code
Unavailable -> 503 + stable unavailability code
panel access failure still propagates unchanged

EVIDENCE

Create:
docs/evidence/TB-TMAR-HOST-ADMIN-CANON-003/

Only:

analyze.md
validation.md
closure.md

Persist task:
docs/ai/tasks/TB-TMAR-HOST-ADMIN-CANON-003.task.md

RECOVERY SOT

Append/update:
hostAdminCanon003

Record:

parentCommit
supportPanelGate=IADMINPANELACCESS
walletPanelGate=IADMINPANELACCESS
supportCapabilityUnavailable=FAIL_CLOSED
walletCapabilityUnavailable=FAIL_CLOSED
serviceLocator=ZERO_ON_IN_SCOPE
adminFileCount=15
canon002=PRESERVED
workflowStop=USER_REVIEW_HOST_ADMIN_CANON_003

SUCCESS CRITERIA

PASS only if:

Support/Wallet fail-open = ZERO
Support/Wallet service locator = ZERO
both use IAdminPanelAccess
stable denial and unavailable semantics are proven
Host/Admin remains 15
focused validation passes
evidence/task/SoT committed and pushed
HEAD == origin/main
working tree clean

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ADMIN-CANON-003
Parent-Task: TB-TMAR-HOST-ADMIN-CANON-002
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Canon002-Preservation-State:
Host-Admin-File-Count-Before:
Host-Admin-File-Count-After:
Support-IAdminPanelAccess-State:
Wallet-IAdminPanelAccess-State:
Support-ServiceLocator-State:
Wallet-ServiceLocator-State:
Support-Capability-Allow-State:
Support-Capability-Deny-State:
Support-Capability-Unavailable-State:
Wallet-Capability-Allow-State:
Wallet-Capability-Deny-State:
Wallet-Capability-Unavailable-State:
Support-Error-Code-State:
Wallet-Error-Code-State:
FailOpen-State:
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

After result:
STOP COMPLETELY.

Do not touch Order authorizers.
Do not start foldering.
Do not start DevActor cleanup.
Wait for Architect review.

END_TOOBA_TASK