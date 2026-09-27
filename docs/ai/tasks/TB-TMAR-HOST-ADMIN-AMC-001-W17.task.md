PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W17
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W16-R1
Parent-Commit: d4088103743693bfb6fa8a775aca95ef16aa6d0b
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FOLDER_BY_FOLDER_ADMIN
Title: W17 — architecture ownership audit for remaining cross-module ProductWorkspace surface before further evacuation

ARCHITECT ACCEPTANCE

W16-R1 is ARCHITECT-ACCEPTED.

Verified parent:
d4088103743693bfb6fa8a775aca95ef16aa6d0b

Accepted W16-R1 state:

W16 publish-readiness migration preserved.
ProductPublishReadinessReader now uses canonical
CatalogCategoryTreeRules.IsAssignableProductCategory.
zero expected InvalidOperationException control flow in moved readiness reader.
no hierarchy duplication.
Host/Admin remains 52.
StoreAppearance deferred.
W17 not started.

WHY W17 IS AUDIT-ONLY

The remaining ProductWorkspace surface is no longer a set of simple Catalog-only slices.

ProductWorkspaceComposer currently composes:

Catalog persistence
Offer.Contracts
Pricing.Contracts
Inventory.Contracts
Tax.Contracts
Party lookup
Admin grid policy/engine
Catalog mutation paths

Several mutation endpoints also return the full cross-module ProductWorkspaceView.

Moving those endpoints blindly into Catalog would make Catalog own/read foreign business data and would violate module boundaries.

Therefore W17 MUST determine the lawful owner and split plan before any further production migration.

THIS TASK CHANGES NO PRODUCTION BEHAVIOR.

ACTIVE HOST FOLDER

src/backend/Host/Tooba.Host/Admin/

Do not start another Host folder.
Do not start W18.

MANDATORY READ

Read completely:

Admin/ProductWorkspaceEndpoints.cs
Admin/ProductWorkspaceComposer.cs
Admin/ProductWorkspaceModels.cs
Admin/AdminProductGridQueryEngine.cs
Admin/AdminProductGridQueryPolicy.cs
Program registration for ProductWorkspace
all remaining ProductWorkspace tests
current Catalog focused capabilities from W10–W16
Offer/Pricing/Inventory/Tax/Party Contracts used by ProductWorkspace
PageComposition module structure and intent
architecture locks:
TMAR-HOST-EVACUATION-PROTOCOL
COMPLETE_REFERENCE_STRUCTURE standard
cross-module Contracts-only rules
Host thin-shell rules
current module manifest / structure locks

MANDATORY INVENTORY

Produce exact current remaining route inventory after W16-R1.

At minimum classify these known routes:

GET /v1/admin/products/
GET /v1/admin/products/brand-options
POST /v1/admin/products/query
POST /v1/admin/products/
GET /v1/admin/products/{productId}
PATCH /v1/admin/products/{productId}/catalog-title
PATCH /v1/admin/products/{productId}/core
PATCH /v1/admin/products/{productId}/quantity-policy
PUT /v1/admin/products/{productId}/category
POST /v1/admin/products/{productId}/categories/additional
DELETE /v1/admin/products/{productId}/categories/additional/{categoryId}
PUT /v1/admin/products/{productId}/brand
POST /v1/admin/products/{productId}/publish
POST /v1/admin/products/{productId}/unpublish
POST /v1/admin/products/{productId}/archive
POST /v1/admin/products/{productId}/restore
DELETE /v1/admin/products/{productId}
POST /v1/admin/products/{productId}/variants
PATCH /v1/admin/products/{productId}/variants/{variantId}

Verify exact current list from source. Do not trust this list if repo differs.

FOR EACH ROUTE CLASSIFY

Record:

read/write
business owner(s)
current Host method
composer method
current direct persistence touched
current foreign Contracts dependencies
response shape
whether response is Catalog-only or cross-module aggregate
workspace-scope semantics
actor/history semantics
stable error codes
whether route can move directly to an existing module
whether route is BLOCKED by cross-module response composition
recommended future wave grouping

MANDATORY CROSS-MODULE COMPOSITION AUDIT

For ProductWorkspaceComposer identify exact usage of:

CatalogDbContext
ICatalogDirectory
IOfferQueryGateway
IPriceQueryGateway
IInventoryQueryGateway
ITaxQueryGateway
IPartyLookupGateway

Classify every dependency:

lawful Contracts dependency
direct module persistence violation
Host-only composition residue
candidate port/gateway for future dedicated composition owner

Special attention:

ListAsync
QueryGridAsync
GetAsync
mutation methods that call RequireWorkspaceAsync after write

MANDATORY RESPONSE-CONTRACT AUDIT

Determine which write endpoints currently return:

full ProductWorkspaceView
partial/list item
no content
another shape

For every route returning ProductWorkspaceView, prove whether moving only the write to Catalog would require:
A. cross-module composition inside Catalog — FORBIDDEN
B. post-command composition in a lawful dedicated composition owner
C. breaking/changing HTTP response contract — NOT ALLOWED without explicit Architect decision

Do not choose A.

OWNERSHIP DECISION

Evaluate these candidate target strategies:

A. Catalog owns all remaining ProductWorkspace endpoints

reject if it requires Catalog -> Offer/Pricing/Inventory/Tax/Party composition.

B. Host remains owner

reject if it leaves business HTTP/composition in thin Host permanently.

C. Existing PageComposition module owns Admin ProductWorkspace composition

inspect actual module intent, architecture, target framework, dependencies, contracts and whether extending it is semantically correct.
do NOT assume from name.

D. New dedicated Backoffice/AdminWorkspace composition module

e.g. ProductWorkspace / Backoffice / AdminWorkspace bounded composition module.
evaluate whether this is the cleanest lawful owner.
must depend only on module Contracts/gateways, never foreign Infrastructure/DbContexts.
if chosen, define minimal project set and responsibility.

E. split ownership:

Catalog-owned mutation commands
dedicated composition module owns aggregate read and post-command response composition
define exact orchestration boundary.

ARCHITECTURE DECISION REQUIRED

W17 must end with ONE recommended target architecture, supported by evidence.

The recommendation must answer:

Who owns:

list
grid
get aggregate
brand-options
Catalog-only writes
lifecycle writes
delete with Offer reference check
variant create/patch

Who owns final HTTP route registration?

How are post-write full ProductWorkspaceView responses preserved?

Which project may depend on:

Catalog.Contracts
Offer.Contracts
Pricing.Contracts
Inventory.Contracts
Tax.Contracts
Party.Contracts/gateway
without violating A.Application -> B.Application lock?

What direct CatalogDbContext usages must be replaced by Catalog Contracts/read ports?

Is PageComposition suitable or not?
Give explicit YES/NO with code evidence.

Is a new module required?
If YES, give exact proposed project names and allowed references.
Do NOT create it in W17.

NO PRODUCTION MIGRATION

W17 MUST NOT:

move any remaining ProductWorkspace route.
create a new module.
alter runtime DI.
change endpoint behavior.
change response contracts.
change schema/frontend.
change Catalog/Offer/Pricing/Inventory/Tax/Party production logic.

Allowed production-code change:
NONE.

Allowed changes:

task file
evidence
architecture/recovery SoT
architecture guard ONLY if it is audit/read-only and does not alter runtime source

FUTURE WAVE PLAN

Produce a concrete ordered sequence after W17.

Example form only — decide from evidence:

W18: establish lawful composition owner + contracts
W19: move list/grid/get
W20: move Catalog core writes with post-command composition
W21: lifecycle
W22: variants
W23: delete
final: delete ProductWorkspace Host files

Do not copy this example blindly.

Every future wave should be bounded ~10–15 min where practical.

STOREAPPEARANCE

Remain deferred.
Do not include StoreAppearance in ProductWorkspace target module decision unless evidence proves shared ownership is necessary.

HOST FILE COUNT

Expected W17:
52 -> 52

No production file moves.

EVIDENCE

Create:
docs/evidence/TB-TMAR-HOST-ADMIN-AMC-001-W17/

Required:

current-route-inventory.md
dependency-map.md
response-contract-map.md
ownership-options.md
target-architecture-decision.md
future-wave-plan.md
closure.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-ADMIN-AMC-001-W17.task.md

RECOVERY SOT

Add hostAdminAmcW17.

Record:

parent task/commit
auditOnly=true
productionCodeChanged=false
Host Admin count=52
remaining route count
crossModuleCompositionState
PageCompositionSuitability
selectedTargetOwner
selectedHttpOwner
directCatalogDbContextEvacuationRequired
postWriteAggregateStrategy
futureWavePlan
StoreAppearance deferred
schema/frontend unchanged
nextHostFolderStarted=false
workflowStop=USER_REVIEW_HOST_ADMIN_W17_ARCHITECTURE_DECISION

PASS CRITERIA

PASS only if:

no production runtime code changed.
exact remaining route inventory complete.
dependency map complete.
response-contract map complete.
PageComposition explicitly evaluated from current code.
one lawful target architecture selected.
direct CatalogDbContext evacuation strategy defined.
post-write response preservation strategy defined.
future wave plan concrete and bounded.
no forbidden cross-module dependency proposed.
Host/Admin remains 52.
StoreAppearance untouched.
schema/frontend unchanged.
commit pushed.
HEAD == origin/main.
tree clean.
W18 not started.

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W17
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W16-R1
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
W16R1-Preservation-State:
Audit-Only-State:
Production-Code-Changed:
Host-Admin-File-Count:
Remaining-ProductWorkspace-Route-Count:
Remaining-Route-Inventory-State:
Cross-Module-Dependency-Map-State:
Direct-CatalogDbContext-State:
Response-Contract-Map-State:
Full-Workspace-PostWrite-Route-Count:
PageComposition-Suitability-State:
Selected-Target-Architecture:
Selected-Composition-Owner:
Selected-Http-Owner:
Selected-Catalog-Write-Owner:
PostWrite-Aggregate-Response-Strategy:
Forbidden-CrossModule-Dependency-State:
Store-Appearance-State:
Schema-Migration-State:
Frontend-State:
Future-Wave-Plan-State:
Next-Recommended-Wave:
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
Do not start W18.
Do not create a new module.
Wait for Architect review.

END_TOOBA_TASK