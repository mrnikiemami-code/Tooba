PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W19
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W18
Parent-Commit: c52e4b51a10010524419c4c1506b04caa8d359c5
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FOLDER_BY_FOLDER_ADMIN
Title: W19 — migrate only aggregate GET /v1/admin/products/{productId} to ProductWorkspace module using Contracts-only composition

ARCHITECT ACCEPTANCE

W18 is ARCHITECT-ACCEPTED.

Verified parent:
c52e4b51a10010524419c4c1506b04caa8d359c5

Accepted W18 state:

canonical ProductWorkspace 5-project skeleton exists.
ProductWorkspace route count = 0.
Host still owns 19 ProductWorkspace routes.
ProductWorkspace.Application foreign Application/Infrastructure edges = ZERO.
ProductWorkspace.Infrastructure foreign DbContext = ZERO.
ProductWorkspace.Endpoints -> Infrastructure = ZERO.
module is NOT ARCH-COMPLETE-002 certified yet.
StoreAppearance deferred.
W19 not started.

SCOPE DECISION

W17 originally grouped list/grid/get together. After W18 verification, W19 is intentionally narrowed to ONE aggregate read route because GET aggregate has the largest boundary-definition cost.

W19 owns ONLY:

GET /v1/admin/products/{productId:guid}

Do NOT migrate:

GET /v1/admin/products/
POST /v1/admin/products/query
GET /v1/admin/products/brand-options
any write route
any lifecycle/variant/delete route

Expected Host remaining ProductWorkspace routes:
19 -> 18

Expected Host/Admin production *.cs:
52 -> 52

ACTIVE HOST FOLDER

src/backend/Host/Tooba.Host/Admin/

Do not start another Host folder.
Do not start W20.

MANDATORY ANALYZE FIRST

Read and disposition before edits:

Host:

ProductWorkspaceEndpoints.GetAsync
ProductWorkspaceComposer.GetAsync
ProductWorkspaceModels.ProductWorkspaceView
every model nested by ProductWorkspaceView
ReadPermissions
CatalogActorHttpBinding impact on GET
ToError

Catalog data used by aggregate:

Product
Localized product name/translations
Variants
VariantAttributeValues
AttributeDefinitions
ProductAttributeValues
MediaReferences
ProductCategories
Category names/path
Brand
quantity policy
UnitOfMeasure + translations
Catalog publish readiness
Activity/Audit history shell

Foreign Contracts:

IOfferQueryGateway
IPriceQueryGateway
IInventoryQueryGateway
ITaxQueryGateway
current Party dependency and canonical Tooba.Party.Contracts.IPartyLookup

Current response semantics:

404 workspace.product.missing
view/full ProductWorkspacePermissions
ReadinessWarnings
UnsupportedMutations
PurchasableHint
all nested fields and ordering

No code move before disposition exists.

ARCHITECTURE LOCK

Target flow:

HTTP
-> IProductWorkspaceAdminAuthorizer
-> ProductWorkspace-owned workspace-scope parser
-> ISender
-> GetProductWorkspaceQuery
-> ProductWorkspace Application composition
-> foreign module Contracts only
-> Result<ProductWorkspaceView>
-> ProductWorkspace-owned ApiResponseFactory path / error catalog integration
-> same HTTP JSON

No ProductWorkspace project may reference:

Host
Catalog.Application / Infrastructure / Domain
Offer.Application / Infrastructure / Domain
Pricing.Application / Infrastructure / Domain
Inventory.Application / Infrastructure / Domain
Tax.Application / Infrastructure / Domain
Party.Application / Infrastructure / Domain
any foreign DbContext

CATALOG READ BOUNDARY

W19 must establish the minimal stable Catalog.Contracts read boundary needed for aggregate GET.

Preferred design:

one or a few cohesive Admin-read contracts that expose Catalog-owned projections, NOT EF/domain entities.

Acceptable example:
ICatalogAdminProductWorkspaceReadGateway

The return type may be a cohesive Catalog-owned snapshot if it is a semantic projection rather than a raw persistence mirror.

It must provide enough to preserve aggregate GET:

product identity/status/kind/updatedAt
localized title / translations
variants and Catalog variant fields
product attributes + variant axes
media refs
category assignments + names/path
brand
SEO seams
Catalog publication/readiness
Activity/Audit history shell
quantity policy + unit options
primary-category assignability

Do NOT expose:

CatalogDbContext
EF entities
Domain entities
IQueryable
foreign-module data

Implementation belongs in Catalog.Infrastructure and may reuse existing focused Catalog seams internally.

CATALOG CONTRACTS SEMANTICS

This new boundary is allowed because ProductWorkspace is a real cross-module consumer.

Keep types:

stable
immutable DTOs/records
Catalog-owned terminology
no UI-only Persian labels except existing legitimate Catalog readiness/history display fields already part of stable behavior.

PRODUCTWORKSPACE APPLICATION

Create canonical structure:

Plain text
Composition/
  Queries/
    GetProductWorkspaceQuery.cs
    GetProductWorkspaceHandler.cs
  Models/
    ProductWorkspaceModels.cs
  Ports/            (only if module-local adapters are needed)
  Validators/

GetProductWorkspaceQuery:

ProductId
workspace permission/scope information needed for response
NO validator required if only route Guid + module-owned parsed scope.

Application may reference foreign *.Contracts projects required for live composition.

Application MUST NOT reference foreign Application/Infrastructure/Domain.

PRODUCTWORKSPACE RESPONSE MODELS

Move authoritative aggregate read DTOs from Host to ProductWorkspace.Application.Models for the GET route.

At minimum:

ProductWorkspaceView
nested response models needed exclusively or primarily by aggregate GET

However, Host write routes still return ProductWorkspaceView until later waves.

Therefore:

DO NOT delete Host models if remaining Host routes still compile against them.
avoid two divergent model authorities.

Preferred compatibility strategy:

make ProductWorkspace.Application model the new authoritative type;
update Host remaining write/composer signatures to use the module model where safe and mechanical;
delete duplicate Host records only when all Host consumers are repointed in this same task and behavior stays identical.

If that is too broad for W19 timebox:

allow a temporary Host compatibility mapping ONLY if documented as one-way and exact;
do NOT create two independently evolving shapes without durable parity guard.

No HTTP property drift.

FOREIGN COMPOSITION

ProductWorkspace Application composes:

Offer:

IOfferQueryGateway

Pricing:

IPriceQueryGateway

Inventory:

IInventoryQueryGateway

Tax:

ITaxQueryGateway

Party:

MUST use Tooba.Party.Contracts.IPartyLookup
ProductWorkspace must NOT reference Party.Application.

Catalog:

new/reused Catalog.Contracts read gateway only.

Preserve current query ordering and interpretation.

SELLER DISPLAY

Current:
Party lookup per offer.

W19 may use IPartyLookup.GetDisplayNamesAsync batch optimization if and only if response parity is exact.
Fallback label remains exactly current behavior ("فروشنده") when no display name exists.

AGGREGATE SEMANTICS TO PRESERVE

Catalog:

title fallback: localized name -> SlugSeam -> "untitled"
variant fields:
VariantId, Fingerprint, Status, CatalogCodeSeam, OfferCount, LocationCount
attributes include product values + variant-axis values
media primary-first then DisplayOrder
category assignments ordered and role labels
primary category + full path
brand display
localized translations
SEO seams
quantity policy and unit options
Activity/Audit history
Catalog publish readiness

Commercial composition:

Offers
Prices
TaxClassifications
Stock
Seller display names

Commercial warnings exact behavior:

no active Offer
no price
no sellable stock

PurchasableHint exact behavior:
active offer AND price exists AND available stock > 0

ReadinessWarnings:
Catalog readiness messages first, then commercial warnings.

UnsupportedMutations remains exact:

media-binary-upload
product-video-upload
promotion-write
full-content-studio

WORKSPACE SCOPE

Move transport parsing for:
X-Tooba-Workspace-Scope

into ProductWorkspace.Endpoints.

Preserve exact behavior:

header view => view-only permissions
otherwise full current permissions values

Response Permissions object must be byte/JSON-shape compatible.

GET has no edit/publish rejection based on scope.

AUTHORIZATION

Use:
IProductWorkspaceAdminAuthorizer

Host direct AdminPanelAccess must no longer own GET aggregate route authorization after migration.

ACTOR BINDING

GET aggregate is read-only.
Do NOT add Catalog actor binding unless proven necessary.

ERROR PIPELINE

ProductWorkspace must own composition-level transport errors.

At minimum:

product missing => workspace.product.missing 404

Do NOT duplicate Catalog business error semantics if errors are propagated from Catalog Contracts.

Use:

Result
stable error code
error catalog contributor/resources if needed
ApiResponseFactory

No:

PlatformHttpException on moved GET
expected InvalidOperationException
ex.Message parsing

ROUTE OWNERSHIP

After W19:

Host MapGet("/{productId:guid}", GetAsync) = REMOVED
Host endpoint GetAsync method = REMOVED
ProductWorkspace.Endpoints maps GET /{productId:guid} under /v1/admin/products
Host remaining ProductWorkspace routes = 18
ProductWorkspace module endpoint route count = 1

Host must start calling:

AddProductWorkspaceEndpointPresentation()
MapProductWorkspaceModuleEndpoints()

only as needed for the newly owned route.

No duplicate exact route.

PRODUCTWORKSPACE INFRASTRUCTURE

Do not put composition business logic in Infrastructure merely because gateways live there.

Preferred:

Application handler orchestrates Contracts gateways.
Infrastructure only contains module adapters/registration where required.

No persistence schema.
No ProductWorkspace DbContext.

HOST COMPOSER DISPOSITION

After W19:

ProductWorkspaceComposer.GetAsync may still be needed by Host write routes for post-write aggregate responses.
If retained, it must be explicitly temporary compatibility residue.
Do NOT delete it yet if write routes depend on it.
Do NOT let new ProductWorkspace GET call Host composer.

W19 should reduce direct Host ownership only for GET route.
Host write route behavior remains unchanged.

VALIDATION MATRIX

GetProductWorkspaceQuery
= NO_VALIDATOR_REQUIRED

Route Guid is constrained.
Scope is parsed by Endpoints transport policy.

TESTS / PARITY

Focused tests must cover:

Route:

exact GET route owned once by ProductWorkspace.
Host GET aggregate route zero.
auth via IProductWorkspaceAdminAuthorizer.
view scope vs full scope permission JSON.
404 missing product code.

Catalog slice:

title fallback.
category path/assignability.
variant/attribute/media mapping.
translations.
quantity/unit options.
readiness/history inclusion.

Foreign composition:

seller name + fallback.
Offer mapping.
Pricing mapping.
Inventory stock/location mapping.
Tax mapping.
OfferCount/LocationCount per variant.
commercial warnings.
PurchasableHint.

Response:

exact top-level field set.
nested field set parity.
ordering parity where current behavior defines order.

BOUNDARY GUARDS

Prove:

ProductWorkspace GET route exactly once.
Host aggregate GET route absent.
Host remaining ProductWorkspace route count=18.
ProductWorkspace endpoint route count=1.
ProductWorkspace.Application -> foreign Application ZERO.
ProductWorkspace.Application -> foreign Infrastructure ZERO.
ProductWorkspace.Infrastructure -> foreign DbContext ZERO.
ProductWorkspace -> Host ZERO.
ProductWorkspace -> Party.Application ZERO.
Party.Contracts lookup used.
Catalog read boundary is Contracts-only.
no EF/domain entity leaks through Catalog.Contracts.
Endpoints -> Infrastructure ZERO.
no PlatformHttpException/IOE/message parsing on moved GET.
no duplicate response-property drift.
Host/Admin count=52.
W18 skeleton protections preserved.
StoreAppearance deferred.
schema/frontend unchanged.
W20 not started.

TIMEBOX

Target 10–12 minutes.
Hard max ~15 minutes.

This is intentionally ONE route.

If Catalog boundary + aggregate GET cannot be migrated safely within the hard limit:
return INCOMPLETE and STOP.
Do NOT broaden into list/grid.
Do NOT leave half-owned route mappings.

FOCUSED VALIDATION

Build:

Catalog.Contracts
Catalog.Infrastructure
ProductWorkspace.Contracts
ProductWorkspace.Domain
ProductWorkspace.Application
ProductWorkspace.Infrastructure
ProductWorkspace.Endpoints
Host
Host.Tests

Run:

W18/W19 architecture guards
focused ProductWorkspace aggregate GET tests only

No solution-wide build/suite.

EVIDENCE

Create:
docs/evidence/TB-TMAR-HOST-ADMIN-AMC-001-W19/

Required:

analyze.md
disposition-map.md
catalog-read-boundary.md
composition-dependencies.md
response-model-parity.md
scope-authorization.md
result-errors.md
behavior-parity.md
partial-host-retention.md
closure.md

Persist:
docs/ai/tasks/TB-TMAR-HOST-ADMIN-AMC-001-W19.task.md

RECOVERY SOT

Add hostAdminAmcW19.

Record:

task/parent/parentCommit
activeHostFolder=Admin
W18 skeleton preserved
aggregateGetOwner=ProductWorkspace
Host route count 19->18
ProductWorkspace route count 0->1
CatalogReadBoundary
foreign Contracts dependencies
Party boundary state
workspace scope state
response parity state
Host composer compatibility residue
ProductWorkspace foreign Application/Infrastructure = ZERO
foreign DbContext=ZERO
Host/Admin=52
StoreAppearance deferred
schema/frontend unchanged
nextHostFolderStarted=false
workflowStop=USER_REVIEW_HOST_ADMIN_W19_CHECKPOINT

PASS CRITERIA

PASS only if:

exactly one route migrated: GET aggregate.
Host route 19->18.
ProductWorkspace routes 0->1.
response contract parity preserved.
ProductWorkspace composes only through Contracts boundaries.
CatalogDbContext absent from ProductWorkspace.
Party.Application absent from ProductWorkspace.
ProductWorkspace GET no Host dependency.
auth/scope/error behavior preserved.
no route duplication.
remaining Host writes unchanged.
Host/Admin=52.
W18 preserved.
StoreAppearance untouched.
no schema/frontend change.
focused validation PASS.
task/evidence/SoT persisted.
commit pushed.
HEAD==origin/main.
clean tree.
W20 not started.

GIT / USER WORK SAFETY

Never:

git reset
git clean
unsafe checkout --
unsafe restore
unsafe rebase
blind stash manipulation
broad git add .

Preserve user work.
On collision return RECOVERY_CONFLICT.

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W19
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W18
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
W18-Preservation-State:
Host-Admin-File-Count-Before:
Host-Admin-File-Count-After:
Host-ProductWorkspace-Route-Count-Before:
Host-ProductWorkspace-Route-Count-After:
ProductWorkspace-Endpoint-Route-Count-Before:
ProductWorkspace-Endpoint-Route-Count-After:
Aggregate-Get-Ownership-State:
Catalog-Read-Boundary-State:
Catalog-EF-Leak-State:
ProductWorkspace-Application-Foreign-Application-State:
ProductWorkspace-Application-Foreign-Infrastructure-State:
ProductWorkspace-Foreign-DbContext-State:
ProductWorkspace-To-Host-State:
Party-Boundary-State:
Offer-Boundary-State:
Pricing-Boundary-State:
Inventory-Boundary-State:
Tax-Boundary-State:
Workspace-Scope-State:
Admin-Authorization-State:
Response-Model-Authority-State:
Response-Contract-Parity-State:
Commercial-Warnings-State:
PurchasableHint-State:
Readiness-History-State:
Aggregate-Get-Result-State:
Aggregate-Get-PlatformHttpException-State:
Aggregate-Get-InvalidOperationExpectedFlow-State:
Aggregate-Get-Message-Classification-State:
Host-Composer-Compatibility-Residue-State:
Endpoints-To-Infrastructure-State:
Path-Namespace-State:
Store-Appearance-State:
Schema-Migration-State:
Frontend-State:
Focused-Validation:
Remaining-ProductWorkspace-Routes:
Residual-Debt:
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
Do not move list/grid/brand-options/writes.
Do not start another Host folder.
Wait for Architect review.

END_TOOBA_TASK