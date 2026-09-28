PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-CANON-001
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W36-STORE-APPEARANCE
Parent-Commit: e76bcb0d4e56aedad45c10285cb6bf318094383a
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Host Canonicalization
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_ADMIN_CANONICALIZATION
Title: Canonicalize Host AdminPanel composition to Contracts-only boundaries

ARCHITECTURE DECISION

Host/Admin module evacuation is complete at the 15-file platform KEEP floor.

This task does NOT move more Admin files out of Host.
This task begins canonicalization of the remaining Host platform surface.

Highest-priority defect:
src/backend/Host/Tooba.Host/Admin/AdminPanelComposer.cs

Current direct foreign dependencies include:

Catalog.Domain
Catalog.Infrastructure.Persistence / CatalogDbContext
Party.Infrastructure.Persistence / PartyDbContext
Order.Application dashboard query
Order.Application seller-count port

A thin Host cross-module composer MAY compose data from modules, but MUST do so through stable module Contracts / neutral platform abstractions only.

GOAL

Make AdminPanelComposer and its immediate seller/dashboard read path Contracts-only across business modules, while preserving existing HTTP behavior and keeping Host/Admin file count at 15.

TIMEBOX / ANTI-LOOP

Target: 10–12 minutes.
Hard stop: 15 minutes.

Do not retry the same approach repeatedly.
If a required canonical boundary cannot be established safely inside this bounded task, return INCOMPLETE with the exact blocker.
Do not expand into authorizer cleanup, folder moves, Storefront, modules unrelated to this read path, or broad architecture repair.

MANDATORY AUDIT FIRST

Inspect:

Host/Admin/AdminPanelComposer.cs
Host/Admin/AdminPanelEndpoints.cs
Host/Admin/AdminPanelModels.cs
Host/Grid/AdminSellersGridQueryEngine and its dependencies
existing Catalog.Contracts read/query capabilities
existing Party.Contracts lookup/read capabilities
existing Order.Contracts admin/dashboard/seller-count capabilities
existing Offer.Contracts gateways used here
current DI registrations
all tests for admin dashboard/sellers/grid

Before creating any new contract, search for a lawful existing equivalent and reuse it.

TARGET BOUNDARY

After this task, AdminPanelComposer.cs MUST have:

ZERO foreign *.Infrastructure*
ZERO foreign *.Application*
ZERO foreign *.Domain*
ZERO foreign DbContext
business-module dependencies only through *.Contracts or neutral BuildingBlocks
Host-owned composition only

PRESERVE:

GET /v1/admin/dashboard
GET /v1/admin/sellers
POST /v1/admin/sellers/query
exact response shapes
existing counts and semantics
seller status/display-name semantics
grid behavior
sort/filter/paging behavior
authorization behavior
Host/Admin file count = 15

CATALOG READ

Replace direct CatalogDbContext published-product count with an existing Catalog.Contracts read capability if available.

If none exists, add the SMALLEST focused Catalog.Contracts read port + implementation owned by Catalog.
Do not expose IQueryable, EF types, entities, or DbContext.

PARTY READ

Replace direct PartyDbContext read with an existing Party.Contracts capability if it can preserve:

PartyId
DisplayName
Status

If the existing IPartyLookup is insufficient, add the smallest focused Contracts read capability.
Do not make Host depend on Party.Application or Party.Infrastructure.

ORDER READ

Replace:

GetAdminOrderDashboardMetricsQuery direct Order.Application usage
ISellerOrderCountReader direct Order.Application usage

with existing Order.Contracts boundaries if available.

If unavailable, expose the smallest stable Order.Contracts read gateway(s) and implement/adapt them inside Order-owned layers.
Do not leak MediatR request types from Order.Application into Host.

OFFER READ

Existing Offer.Contracts use is allowed.
Do not move Offer composition into Host persistence.

SELLERS GRID

Audit AdminSellersGridQueryEngine.

If it directly depends on foreign Application/Infrastructure/DbContext:

repair ONLY what is necessary for the /admin/sellers/query path to obey the same Contracts-only rule.
prefer a focused module Contract read gateway over pulling persistence into Host.

If it is already lawful, leave it unchanged.

Do not redesign the generic grid platform in this task.

HOST OWNERSHIP

AdminPanelComposer may remain in Host because it is cross-module platform composition.

Do NOT move the dashboard or seller aggregate into Catalog, Order, Party, or Offer.

RESULT / ERROR BEHAVIOR

This task is read-only composition.
Do not introduce message parsing, expected InvalidOperationException flow, or new PlatformHttpException control flow.

Existing endpoint authorization/error mapping remains out of scope unless directly broken by this repair.

STRUCTURE

Do not perform broad folder moves yet.
Do not rename the 15 Host/Admin files unless required for compilation.
Folder canonicalization will be a later bounded task.

FORBIDDEN

No frontend edits.
No schema/migrations.
No Storefront changes.
No authorizer cleanup.
No Support/Wallet fail-open changes yet.
No DevActor changes.
No ProductWorkspace changes.
No new Host Admin MOVE wave.
No direct cross-module DbContext.
No A.Application -> B.Application.
No A -> B.Infrastructure.
No duplicate contract if lawful equivalent exists.
No broad service locator introduction.
No git reset, git clean, unsafe restore/checkout/rebase, blind stash, or broad git add ..

DURABLE GUARDS

Add/update focused guards proving:

AdminPanelComposer.cs contains no .Infrastructure namespace references.
contains no .Application namespace references for foreign business modules.
contains no .Domain namespace references for foreign business modules.
contains no DbContext.
Catalog read is through Contracts.
Party read is through Contracts.
Order dashboard metrics are through Contracts.
Order seller counts are through Contracts.
Offer remains Contracts.
dashboard route behavior preserved.
sellers route behavior preserved.
sellers grid route behavior preserved.
Host/Admin count remains exactly 15.
no additional Host/Admin business endpoint introduced.
W36 StoreAppearance closure remains intact.

FOCUSED VALIDATION

Run focused builds for all changed projects, at minimum:

Tooba.Catalog.Contracts + owning implementation project if changed
Tooba.Party.Contracts + owning implementation project if changed
Tooba.Order.Contracts + owning implementation project if changed
Tooba.Host
Tooba.Host.Tests

Run focused tests:

admin dashboard
admin sellers list
admin sellers grid
new boundary guards
W36 Host Admin count guard

Do not run solution-wide suite unless required by a direct build dependency.

EVIDENCE

Create:
docs/evidence/TB-TMAR-HOST-ADMIN-CANON-001/

Required:

analyze.md
contract-boundary-map.md
behavior-parity.md
validation.md
closure.md

Persist this exact task:
docs/ai/tasks/TB-TMAR-HOST-ADMIN-CANON-001.task.md

RECOVERY SOT

Append/update a focused block:
hostAdminCanon001

Record:

parentCommit
adminFileCountBefore=15
adminFileCountAfter=15
AdminPanelComposer foreignInfrastructure=ZERO
foreignApplication=ZERO
foreignDomain=ZERO
foreignDbContext=ZERO
CatalogBoundary=CONTRACTS
PartyBoundary=CONTRACTS
OrderBoundary=CONTRACTS
OfferBoundary=CONTRACTS
Routes=PRESERVED
Frontend=UNTOUCHED
Schema=UNCHANGED
workflowStop=USER_REVIEW_HOST_ADMIN_CANON_001

SUCCESS CRITERIA

PASS only if:

behavior parity is preserved;
direct foreign persistence/Application/Domain dependencies on this Admin panel composition path are zero;
no duplicate unnecessary contracts were created;
Host/Admin remains 15 files;
focused builds/tests/guards pass;
evidence + SoT + task are persisted;
commit is pushed;
HEAD == origin/main;
working tree clean.

If a safe contract boundary requires a larger redesign:
return INCOMPLETE, document exact blocker, and STOP.
Do not loop.

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ADMIN-CANON-001
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W36-STORE-APPEARANCE
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-Preservation-State:
Host-Admin-File-Count-Before:
Host-Admin-File-Count-After:
AdminPanelComposer-Ownership-State:
AdminPanelComposer-Foreign-Infrastructure-State:
AdminPanelComposer-Foreign-Application-State:
AdminPanelComposer-Foreign-Domain-State:
AdminPanelComposer-Foreign-DbContext-State:
Catalog-Read-Boundary-State:
Party-Read-Boundary-State:
Order-Dashboard-Boundary-State:
Order-SellerCount-Boundary-State:
Offer-Boundary-State:
Sellers-Grid-Boundary-State:
Dashboard-Behavior-State:
Seller-List-Behavior-State:
Seller-Grid-Behavior-State:
New-Duplicate-Contract-State:
Frontend-State:
Schema-Migration-State:
Focused-Validation:
SoT-State:
Evidence-Path:
Commit-SHA:
Push-State:
HEAD-Equals-Origin-Main:
Working-Tree:
User-Work-Preserved:
Loop-Retry-Count:
STOP
END_TOOBA_WORKER_RESULT

STOP RULE

After returning the result:
STOP COMPLETELY.

Do not start authorizer cleanup.
Do not start foldering.
Do not start DevActor cleanup.
Do not start any Module audit.
Wait for Architect review.

END_TOOBA_TASK