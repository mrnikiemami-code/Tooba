PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-CANON-002
Parent-Task: TB-TMAR-HOST-ADMIN-CANON-001
Parent-Commit: 9a159bbc594735bc78f416ecbd8a8078954ce6a3
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Host Canonicalization
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_ADMIN_CANONICALIZATION
Title: Canonicalize simple Host Admin endpoint-authorizer adapters onto IAdminPanelAccess

ARCHITECT ACCEPTANCE OF PARENT

TB-TMAR-HOST-ADMIN-CANON-001 is ARCHITECT-ACCEPTED at:
9a159bbc594735bc78f416ecbd8a8078954ce6a3

Verified:

AdminPanelComposer foreign Infrastructure/Application/Domain/DbContext = ZERO
Catalog/Party/Order/Offer read path = Contracts-only
sellers grid path = Contracts-only
Host/Admin remains 15 files
W36 preservation intact

GOAL

Canonicalize ONLY the simple Host Admin endpoint-authorizer adapters so they delegate through the existing Host platform abstraction IAdminPanelAccess instead of resolving session/tenant/guard/environment from HttpContext.RequestServices or duplicating tenant/platform access logic.

IN-SCOPE FILES

Audit and, where compatible, repair ONLY:

HostPaymentAdminAuthorizer.cs
HostPromotionAdminAuthorizer.cs
HostReturnAdminAuthorizer.cs
HostSettlementAdminAuthorizer.cs
HostAdminPanelAccess.cs
their DI registrations/tests if required

OUT OF SCOPE

Do NOT touch:

HostOrderAdminAuthorizer
HostOrderAdminEffectiveAccessReader
HostSupportAdminAuthorizer
HostWalletAdminAuthorizer
AdminPanelComposer
AdminPanelEndpoints
AdminGridQueryEndpoint
AdminDevActorBootstrap
folder/namespace moves
any module business logic

ARCHITECTURE TARGET

HostAdminPanelAccess : IAdminPanelAccess is the single Host platform gate for:

normal tenant-bound Admin access
existing Development Marketplace synthetic-tenant behavior

Simple module endpoint authorizers should become thin adapters over that abstraction.

Preferred shape:

C#
public sealed class HostXAdminAuthorizer(IAdminPanelAccess adminAccess) : IXAdminAuthorizer
{
    public Task<Guid> RequireAuthorizedAsync(HttpContext context, CancellationToken ct) =>
        adminAccess.RequireAuthorizedAsync(context.Request, ct);
}

If an endpoint contract returns Task rather than Task<Guid>, await the same gate and discard the actor.

SETTLEMENT

Settlement currently duplicates Marketplace synthetic tenant logic.

If IAdminPanelAccess fully preserves Settlement's current SingleStore + Development Marketplace semantics, remove that duplication and delegate to IAdminPanelAccess.

If any documented Settlement behavior differs, do NOT force convergence; return INCOMPLETE with the exact difference.

SERVICE LOCATOR

For the four in-scope simple adapters:

ZERO HttpContext.RequestServices.GetRequiredService
constructor injection only
ZERO duplicate tenant/authorization decision logic outside HostAdminPanelAccess

BEHAVIOR PRESERVATION

Preserve:

endpoint interfaces
returned actor IDs where required
status/error semantics
SingleStore authorization
Development Marketplace synthetic tenant semantics
existing module endpoint ownership

Do not change permission/capability-specific behavior because these four adapters are panel-gate adapters only.

HOST COUNT

Host/Admin must remain exactly 15 files.

ANTI-LOOP / EXECUTION BUDGET

This task has ONE concern.

Validation commands:

build Tooba.Host
build Tooba.Host.Tests
run ONLY the focused authorizer tests/guard for this task

Do not run broader suites.

For any failing command:

make at most ONE repair attempt
rerun ONLY that failing command once
if it fails again: return INCOMPLETE with exact command/error and STOP

MAX_REPAIR_ITERATIONS = 1
MAX_VALIDATION_COMMAND_RUNS = 6
Do not loop.

DURABLE GUARD

Add one focused guard proving:

Payment/Promotion/Return/Settlement authorizers contain no RequestServices
all four depend on IAdminPanelAccess
Settlement contains no duplicate MarketplacePlatformTenantId
Settlement contains no direct IAuthorizationGuard decision logic
HostAdminPanelAccess remains the platform implementation
Host/Admin count = 15
CANON-001 guard still passes

Do not create a large generic guard framework.

EVIDENCE

Create:
docs/evidence/TB-TMAR-HOST-ADMIN-CANON-002/

Only:

analyze.md
validation.md
closure.md

Persist this exact task:
docs/ai/tasks/TB-TMAR-HOST-ADMIN-CANON-002.task.md

RECOVERY SOT

Append/update:
hostAdminCanon002

Record:

parentCommit
simpleAuthorizers = CANONICAL_IADMINPANELACCESS
serviceLocator = ZERO_ON_IN_SCOPE
settlementDuplicateAccessLogic = ZERO
adminFileCount = 15
canon001 = PRESERVED
workflowStop = USER_REVIEW_HOST_ADMIN_CANON_002

SUCCESS CRITERIA

PASS only if:

all four in-scope authorizers use IAdminPanelAccess;
no in-scope RequestServices service locator remains;
Settlement duplicate platform access logic is removed only if behavior parity is proven;
Host/Admin stays 15;
focused validation passes;
task/evidence/SoT committed and pushed;
HEAD == origin/main;
working tree clean.

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ADMIN-CANON-002
Parent-Task: TB-TMAR-HOST-ADMIN-CANON-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Canon001-Preservation-State:
Host-Admin-File-Count-Before:
Host-Admin-File-Count-After:
Payment-Authorizer-State:
Promotion-Authorizer-State:
Return-Authorizer-State:
Settlement-Authorizer-State:
IAdminPanelAccess-Delegation-State:
InScope-ServiceLocator-State:
Settlement-Duplicate-Platform-Logic-State:
SingleStore-Behavior-State:
Marketplace-Development-Behavior-State:
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

Do not touch Order/Support/Wallet authorizers.
Do not start foldering.
Do not start DevActor cleanup.
Wait for Architect review.

END_TOOBA_TASK