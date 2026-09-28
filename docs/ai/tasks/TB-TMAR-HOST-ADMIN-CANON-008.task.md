PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-CANON-008
Parent-Task: TB-TMAR-HOST-ADMIN-CANON-007
Parent-Commit: 9a6be0fb9ae91970038d6a576f20e6f826badb13
Implementation-Commit-Parent: ca11d31e058fdd1704f04a0eec11a749e7461af3
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Host Canonicalization
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_ADMIN_CANONICALIZATION
Title: Canonicalize the remaining Host/Admin 15-file physical structure and namespaces

ARCHITECT ACCEPTANCE OF PARENT

TB-TMAR-HOST-ADMIN-CANON-007 is ARCHITECT-ACCEPTED.

Verified:

AdminDevActor Identity.Infrastructure = ZERO
Identity boundary = Contracts-only
expected-flow InvalidOperationException catch = ZERO
tuple write relies on idempotent upsert semantics
Host/Admin = 15
CANON-006 preserved

GOAL

Do ONLY physical/namespace canonicalization of the remaining 15 Host/Admin platform files.

No ownership migration.
No behavior change.
No policy change.
No new seams/contracts.
No module work.

CURRENT FILE SET

The 15 remaining platform-owned files are expected to be:

AdminDevActorBootstrap.cs
AdminGridQueryEndpoint.cs
AdminPanelAccess.cs
AdminPanelComposer.cs
AdminPanelEndpoints.cs
AdminPanelModels.cs
HostAdminPanelAccess.cs
HostOrderAdminAuthorizer.cs
HostOrderAdminEffectiveAccessReader.cs
HostPaymentAdminAuthorizer.cs
HostPromotionAdminAuthorizer.cs
HostReturnAdminAuthorizer.cs
HostSettlementAdminAuthorizer.cs
HostSupportAdminAuthorizer.cs
HostWalletAdminAuthorizer.cs

First audit actual current disk state.
If the set differs, STOP with INCOMPLETE and report exact difference.

TARGET PHYSICAL STRUCTURE

Use exactly this shallow capability-first structure unless an existing Host-wide canonical convention proves a directly equivalent better path:

Admin/Access/

AdminPanelAccess.cs
HostAdminPanelAccess.cs

Admin/Access/Authorizers/

HostOrderAdminAuthorizer.cs
HostOrderAdminEffectiveAccessReader.cs
HostPaymentAdminAuthorizer.cs
HostPromotionAdminAuthorizer.cs
HostReturnAdminAuthorizer.cs
HostSettlementAdminAuthorizer.cs
HostSupportAdminAuthorizer.cs
HostWalletAdminAuthorizer.cs

Admin/Panel/

AdminPanelComposer.cs
AdminPanelEndpoints.cs
AdminPanelModels.cs

Admin/Grid/

AdminGridQueryEndpoint.cs

Admin/Development/

AdminDevActorBootstrap.cs

PATH / NAMESPACE

Namespaces MUST match physical capability paths:

Tooba.Host.Admin.Access
Tooba.Host.Admin.Access.Authorizers
Tooba.Host.Admin.Panel
Tooba.Host.Admin.Grid
Tooba.Host.Admin.Development

Update only references/usings/registrations/tests required by these namespace moves.

Do not keep compatibility shims in old namespaces.
Do not leave duplicate types/files.
Do not introduce global using hacks just to avoid proper references.

PRESERVE EXACTLY

all 15 type names
all public/internal visibility
all endpoint routes
all DI lifetimes/registrations
all authorization behavior
all error codes/statuses
all Contracts/seams established in CANON-001..007
AdminPanelComposer behavior
Admin grid behavior
DevActor behavior
Host/Admin total C# file count = 15

FORBIDDEN

no module edits except compile-only namespace import fixes if absolutely required by references to Host types
no frontend edits
no schema/migrations
no business logic edits
no behavior refactor
no test-suite broadening
no new abstractions
no file count growth
no compatibility wrappers
no git clean, reset, unsafe restore/checkout, broad add

ANTI-LOOP / EXECUTION BUDGET

ONE concern: physical structure + namespace alignment only.

Validation:

build Tooba.Host
build Tooba.Host.Tests
run ONLY CANON-008 structure guard + CANON-007 + CANON-006 guards

On failure:

ONE repair attempt maximum
rerun ONLY the exact failing command once
second failure => INCOMPLETE + exact blocker + STOP

MAX_REPAIR_ITERATIONS = 1
MAX_VALIDATION_COMMAND_RUNS = 6
Do not run solution-wide tests.
Do not loop.

DURABLE GUARD

Prove:

root Host/Admin/*.cs count = 0
recursive Host/Admin/**/*.cs count = exactly 15
exact folder membership matches target
namespace matches folder for all 15 files
no duplicate old-path file exists
no stale using Tooba.Host.Admin; remains where a more specific moved namespace is required
all CANON-001..007 critical seams remain present
no module-specific business endpoint reintroduced in Host/Admin

EVIDENCE

Create only:
docs/evidence/TB-TMAR-HOST-ADMIN-CANON-008/

analyze.md
structure-map.md
validation.md
closure.md

Persist task:
docs/ai/tasks/TB-TMAR-HOST-ADMIN-CANON-008.task.md

RECOVERY SOT

Append/update hostAdminCanon008:

parentCommit
adminRootFlatFiles=ZERO
adminRecursiveFileCount=15
accessFolder=CANONICAL
authorizersFolder=CANONICAL
panelFolder=CANONICAL
gridFolder=CANONICAL
developmentFolder=CANONICAL
pathNamespace=EXACT
canon001Through007=PRESERVED
workflowStop=USER_REVIEW_HOST_ADMIN_CANON_008

SUCCESS CRITERIA

PASS only if:

all 15 files are in the canonical structure
namespaces exactly match paths
no duplicate/shim remains
behavior unchanged
focused build/tests pass
tracked working tree clean
commit pushed and HEAD == origin/main

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ADMIN-CANON-008
Parent-Task: TB-TMAR-HOST-ADMIN-CANON-007
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Canon007-Preservation-State:
Host-Admin-File-Count-Before:
Host-Admin-File-Count-After:
Host-Admin-Root-Flat-File-State:
Access-Folder-State:
Authorizers-Folder-State:
Panel-Folder-State:
Grid-Folder-State:
Development-Folder-State:
Path-Namespace-State:
Duplicate-Old-Path-State:
Behavior-State:
Canon001-007-Seams-State:
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

Do not start final Host/Admin certification.
Wait for Architect review.

END_TOOBA_TASK