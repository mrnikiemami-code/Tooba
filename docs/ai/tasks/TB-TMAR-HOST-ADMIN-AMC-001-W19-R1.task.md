PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W19-R1
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W19
Parent-Commit: 186ed01ecf7e789db5998eb2ca6747f6003b62af
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FOLDER_BY_FOLDER_ADMIN
Title: W19-R1 — remove expected InvalidOperationException control flow from Catalog ProductWorkspace read gateway category assignability

ARCHITECT REVIEW

W19 is NOT accepted yet.

The aggregate GET migration is otherwise structurally sound, but one concrete PASS blocker remains in the moved GET data path.

BLOCKER

Current production file:

src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/CatalogAdminProductWorkspaceReadGateway.cs

contains expected control flow:

C#
try
{
    isPrimaryCategoryAssignable = CatalogCategoryTreeRules.IsAssignableProductCategory(
        assignableProbe, parentById);
}
catch (InvalidOperationException)
{
    isPrimaryCategoryAssignable = false;
}

This is still ordinary "not assignable" / invalid-tree-state handling through InvalidOperationException.

W19 required:

no expected InvalidOperationException on the moved aggregate GET path.
no exception/message-based control flow in the migrated surface.

The ProductWorkspace handler/endpoint are clean, but this Catalog gateway is part of the moved GET path and therefore must satisfy the same rule.

REPAIR OBJECTIVE

Provide/use a lawful non-throwing Domain authority for this boolean probe.

Do NOT:

duplicate tree traversal in Infrastructure.
swallow InvalidOperationException.
change category assignability semantics.
change response shape.
change route ownership.
touch list/grid/writes.
start W20.

MANDATORY AUDIT

Before edit:

inspect CatalogCategoryTreeRules completely.
identify why IsAssignableProductCategory may still throw:
missing category in parent map
cyclic parent graph via GetCategoryLevel.
enumerate all current callers of IsAssignableProductCategory.
decide smallest canonical Domain repair:
preferred TryGetCategoryLevel(...) / TryIsAssignableProductCategory(...),
OR a safe overload that returns false for non-resolvable/cyclic category graphs,
while preserving existing throwing API compatibility for unrelated callers.
prove no semantic drift for valid trees.

DOMAIN DESIGN

Preferred pattern:

C#
public static bool TryGetCategoryLevel(
    Guid categoryId,
    IReadOnlyDictionary<Guid, Guid?> parentById,
    out int level)

and/or:

C#
public static bool TryIsAssignableProductCategory(
    Guid categoryId,
    IReadOnlyDictionary<Guid, Guid?> parentById,
    out bool isAssignable)

Requirements:

one canonical traversal core.
valid-tree output identical to existing GetCategoryLevel.
missing category => non-throwing failure/false.
cycle => non-throwing failure/false.
existing GetCategoryLevel and IsAssignableProductCategory throwing behavior remains compatible unless safely implemented via the same shared core.
no Persian message classification.
no duplicate parent-walk algorithm.

CATALOG GATEWAY REPAIR

CatalogAdminProductWorkspaceReadGateway must:

use the canonical non-throwing Domain path.
contain zero catch (InvalidOperationException) for category assignability.
preserve:
IsPrimaryCategoryAssignable
PrimaryCategoryAssignableWarningFa
exact warning behavior when not assignable.

W19 PRESERVATION LOCK

Must preserve ALL accepted implementation state except the blocker:

Host aggregate GET route remains absent.
ProductWorkspace owns exactly one aggregate GET route.
Host ProductWorkspace routes remain 18.
ProductWorkspace route count remains 1.
ProductWorkspace Application uses Contracts-only foreign boundaries.
Catalog read gateway remains ICatalogAdminProductWorkspaceReadGateway.
ProductWorkspace -> Host ZERO.
ProductWorkspace -> Party.Application ZERO.
Party.Contracts IPartyLookup.
Offer/Pricing/Inventory/Tax Contracts.
authoritative response models in ProductWorkspace.Application.
workspace scope behavior.
admin authorization.
response JSON parity.
commercial warnings.
PurchasableHint.
readiness/history shell.
Host composer compatibility residue for writes.
Host/Admin count 52.
StoreAppearance deferred.
no schema/frontend change.

NO BROAD CLEANUP

Do NOT repair:

other Host/ProductWorkspace category-tree exception patterns.
list/grid.
Host composer.
other Catalog callers.

This repair is ONLY the W19 moved aggregate GET path + canonical Domain safe API needed to support it.

GUARDS

Add/update guards proving:

CatalogAdminProductWorkspaceReadGateway contains no:
catch (InvalidOperationException
expected IOE control flow
ex.Message
message parsing
gateway uses canonical Domain non-throwing category-level/assignability API.
no duplicated hierarchy algorithm in gateway.
valid category-tree assignability parity preserved.
missing category path produces false/non-throwing probe.
cyclic graph produces false/non-throwing probe.
existing throwing APIs preserve compatibility.
W19 route ownership/boundaries unchanged.
Host routes=18, ProductWorkspace routes=1.
Host/Admin=52.
W20 not started.

FOCUSED TESTS

Domain:

valid L1 -> not assignable.
valid L2 -> not assignable.
valid L3 -> assignable.
missing category -> safe API returns failure/false; no throw.
cycle -> safe API returns failure/false; no throw.
safe valid level equals GetCategoryLevel.
existing GetCategoryLevel still throws where previously expected.
existing IsAssignableProductCategory compatibility preserved.

W19:

ProductWorkspaceAggregateGetW19 tests remain green.
category assignability/warning response parity remains green.

FOCUSED BUILDS

Catalog.Domain
Catalog.Infrastructure
ProductWorkspace.Application
ProductWorkspace.Endpoints
Host
Host.Tests

Run:

W19 + W19-R1 guards
focused Domain safe-tree tests
aggregate GET focused tests

No broad solution build.

EVIDENCE

Create:
docs/evidence/TB-TMAR-HOST-ADMIN-AMC-001-W19-R1/

Required:

analyze.md
category-tree-safe-probe.md
aggregate-get-preservation.md
guard-repair.md
closure.md

RECOVERY SOT

Add hostAdminAmcW19R1.

Record:

parentTask/parentCommit
W19 migration preserved
categoryTreeSafeProbeState
aggregateReadGatewayInvalidOperationExpectedFlow=ZERO
categoryAssignabilityParity=PRESERVED
Host routes=18
ProductWorkspace routes=1
Host/Admin=52
StoreAppearance deferred
schema/frontend unchanged
nextHostFolderStarted=false
workflowStop=USER_REVIEW_HOST_ADMIN_W19_R1_CHECKPOINT

PASS CRITERIA

PASS only if:

moved aggregate GET data path has zero expected InvalidOperationException control flow.
canonical Domain non-throwing hierarchy probe exists/is reused.
no hierarchy duplication.
valid-tree semantics unchanged.
missing/cyclic cases non-throwing on safe path.
existing throwing API compatibility preserved.
W19 ownership/boundaries/response parity preserved.
focused builds/tests/guards PASS.
task/evidence/SoT persisted.
commit pushed.
HEAD == origin/main.
working tree clean.
W20 not started.

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W19-R1
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W19
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
W19-Migration-Preservation-State:
Category-Tree-Authority-State:
Category-Tree-Safe-Probe-State:
Valid-Level-Parity-State:
Missing-Category-Safe-State:
Cycle-Safe-State:
Existing-Throwing-API-Compatibility-State:
Aggregate-Gateway-InvalidOperationExpectedFlow-State:
Aggregate-Gateway-Message-Classification-State:
Aggregate-Get-Route-Ownership-State:
Catalog-Read-Boundary-State:
ProductWorkspace-Boundary-State:
Response-Contract-Parity-State:
Host-ProductWorkspace-Route-Count:
ProductWorkspace-Endpoint-Route-Count:
Host-Admin-File-Count:
Store-Appearance-State:
Schema-Migration-State:
Frontend-State:
Focused-Validation:
SoT-State:
Evidence-Path:
Commit-SHA:
Push-State:
HEAD-Equals-Origin-Main:
Working-Tree:
User-Work-Preserved:
Next-Host-Folder-Started: false
STOP
END_TOOBA_WORKER_RESULT

STOP RULE

After result:

STOP completely.
Do not start W20.
Do not migrate list/grid/brand-options/writes.
Wait for Architect review.

END_TOOBA_TASK