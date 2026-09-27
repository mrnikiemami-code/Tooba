PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W20
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W19-R1
Parent-Commit: f6177c8ee72b335e7fead5d894e9575ef3aaf9e0
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FOLDER_BY_FOLDER_ADMIN
Title: W20 — evacuate Catalog-only Admin brand-options read route from Host ProductWorkspace to Catalog

ARCHITECT ACCEPTANCE

W19-R1 is ARCHITECT-ACCEPTED.

Verified parent:
f6177c8ee72b335e7fead5d894e9575ef3aaf9e0

Accepted W19 through R1:

aggregate GET remains ProductWorkspace-owned exactly once.
Host ProductWorkspace routes = 18.
ProductWorkspace routes = 1.
Catalog read gateway category assignability uses canonical non-throwing Domain Try* path.
moved aggregate GET path has zero expected InvalidOperationException control flow.
Contracts-only ProductWorkspace boundaries preserved.
Host/Admin remains 52.
StoreAppearance deferred.

W20 BOUNDED SLICE

Move ONLY:

GET /v1/admin/products/brand-options

from Host ProductWorkspace to Catalog.Endpoints.

Do NOT move:

GET /v1/admin/products/
POST /v1/admin/products/query
any write route
lifecycle
variants
delete

Expected after W20:

Host ProductWorkspace route count: 18 -> 17
ProductWorkspace module route count: stays 1
Catalog gains exactly 1 brand-options route
Host/Admin production .cs: 52 -> 52

WHY THIS SLICE

brand-options is Catalog-only:

Brands
localized brand names
brand status
optional search
deterministic ordering
no Offer/Pricing/Inventory/Tax/Party composition

It should not remain in the ProductWorkspace composition module.

MANDATORY ANALYZE FIRST

Before edit inspect:

Host ProductWorkspaceEndpoints.ListBrandOptionsAsync
Host ProductWorkspaceComposer.ListBrandOptionsAsync
Host ListBrandOptionsInternalAsync
Host AdminBrandOption
current Catalog Brands domain/entity shape
current Catalog Application brand-related code if any
current Catalog Endpoints structure
current Catalog authorization pattern
current Catalog error/result conventions
any existing brand readers/gateways/callers/tests
CatalogContracts.cs for legacy brand-related APIs, but DO NOT expand/duplicate its debt

Create exact disposition before moving code.

CURRENT BEHAVIOR TO PRESERVE

Source behavior:

Load all Catalog brands.
Load localized name rows for those brand ids.
Localized name preference:
fa-IR first
then locale ascending
first row per BrandId
Label fallback:
localized name
SlugSeam
"برند"
Status = brand.Status.ToString()
Optional q/search:
null/blank => no filter
otherwise Trim()
Name.Contains(needle, StringComparison.OrdinalIgnoreCase)
Order:
Name ascending using StringComparer.Ordinal
Take maximum 200.
Empty brands => [].

Exact HTTP query parameter remains:
q

Exact response shape:

Plain text
[
  {
    brandId,
    name,
    status
  }
]

TARGET ARCHITECTURE

Preferred:

Plain text
Tooba.Catalog.Application/
  Brands/
    Queries/
      ListBrandOptionsQuery.cs
      ListBrandOptionsHandler.cs
    Models/
      BrandOptionView.cs
    Ports/
      IBrandOptionReader.cs
    Validators/

Infrastructure:

focused BrandOptionReader
place under coherent capability folder, not Infrastructure root if current structure standard requires capability folders.

Endpoints:

Plain text
Admin/Brands/
  CatalogBrandOptionsAdminEndpoints.cs

Flow:
HTTP
-> ICatalogAdminAuthorizer
-> ISender
-> ListBrandOptionsQuery
-> IBrandOptionReader
-> Result<IReadOnlyList<BrandOptionView>>
-> ApiResponseFactory

VALIDATION MATRIX

ListBrandOptionsQuery
= NO_VALIDATOR_REQUIRED

Reason:

optional free-text q
current behavior has no length/range rejection
trim/filter is use-case behavior

Do not invent validator constraints.

RESULT / ERRORS

This read has no expected business error in current behavior.

empty => success []
no PlatformHttpException
no expected InvalidOperationException
no exception-message parsing

Use Result success path consistently.

AUTHORIZATION

Preserve Admin authorization:

ICatalogAdminAuthorizer

No ProductWorkspace authorizer for this Catalog-only route.

No workspace-scope semantics.

NO ACTOR BINDING

This is read-only.
Do not add actor binding.

HOST DISPOSITION

After W20:

ProductWorkspaceEndpoints.cs

remove:
group.MapGet("/brand-options", ListBrandOptionsAsync)
endpoint method ListBrandOptionsAsync

ProductWorkspaceComposer.cs

remove public/private brand-option methods if zero remaining consumers:
ListBrandOptionsAsync
ListBrandOptionsInternalAsync

ProductWorkspaceModels.cs

remove AdminBrandOption if zero remaining consumers.

Do not delete other ProductWorkspace types/methods.

CATALOG STRUCTURE HYGIENE

Important:

do NOT add new brand behavior to legacy root CatalogContracts.cs.
do NOT create a new mixed BrandsContracts.cs bundle.
brand capability must be shallow/capability-first.
path↔namespace exact.
one query authority.

If existing brand code already has a lawful focused capability, extend it instead of creating duplicate authority.

DURABLE GUARDS

Prove:

Host brand-options route absent.
Catalog owns exact GET /v1/admin/products/brand-options once.
ProductWorkspace aggregate GET /{productId:guid} remains exactly once.
Host ProductWorkspace routes = 17.
ProductWorkspace routes = 1.
Catalog brand-options route = 1.
endpoint uses ICatalogAdminAuthorizer + ISender + ApiResponseFactory.
endpoint injects no reader/DbContext.
IBrandOptionReader focused seam exists.
no Host dependency from Catalog.
no Endpoint -> Infrastructure.
no expected IOE/message parsing.
query validator classification = NO_VALIDATOR_REQUIRED.
localized name precedence preserved.
fallback preserved.
case-insensitive search preserved.
Ordinal sort preserved.
Take(200) preserved.
Host AdminBrandOption removed if dead.
Host/Admin count=52.
W19-R1 ownership/boundaries preserved.
StoreAppearance deferred.
schema/frontend unchanged.
W21 not started.

FOCUSED TESTS

Cover:

empty brands -> [].
fa-IR name wins over other locale.
fallback to another locale if fa missing.
fallback to SlugSeam.
fallback to "برند".
blank q => unfiltered.
q trimmed.
q case-insensitive Contains.
name ordering StringComparer.Ordinal.
max 200.
status string parity.
route owned once.
Admin auth preserved.
response field parity.

FOCUSED BUILDS

Catalog.Application
Catalog.Infrastructure
Catalog.Endpoints
ProductWorkspace.Endpoints
Host
Host.Tests

Run:

W19/W19-R1 preservation guards
W20 guards
focused brand option tests

DB-backed tests may skip only if Docker/Testcontainers unavailable, with explicit evidence.
Non-DB guards/builds must pass.

TIMEBOX

Target 10–12 minutes.
Hard max ~15 minutes.

If safe completion exceeds hard limit:
return INCOMPLETE and STOP.
Do not start list/grid or writes.

EVIDENCE

Create:
docs/evidence/TB-TMAR-HOST-ADMIN-AMC-001-W20/

Required:

analyze.md
disposition-map.md
brands-capability.md
behavior-parity.md
validation.md
partial-host-retention.md
closure.md

Persist:
docs/ai/tasks/TB-TMAR-HOST-ADMIN-AMC-001-W20.task.md

RECOVERY SOT

Add hostAdminAmcW20.

Record:

task/parent/parentCommit
activeHostFolder=Admin
W19R1 preserved
brandOptionsOwner=Catalog
Host route count 18->17
ProductWorkspace route count remains 1
Catalog brand-options route count=1
CQRS/result/read-port state
validator matrix
localized-name parity
search/order/limit parity
Host/Admin=52
StoreAppearance deferred
schema/frontend unchanged
nextHostFolderStarted=false
workflowStop=USER_REVIEW_HOST_ADMIN_W20_CHECKPOINT

PASS CRITERIA

PASS only if:

only brand-options route migrated.
Host routes 18->17.
ProductWorkspace routes remain 1.
Catalog owns brand-options exactly once.
CQRS/Result/ApiResponseFactory.
focused reader.
exact name/search/order/limit/status behavior preserved.
no Host residue for route-only brand option methods/models.
W19-R1 aggregate GET unchanged.
no forbidden dependencies.
Host/Admin=52.
StoreAppearance untouched.
schema/frontend unchanged.
focused validation PASS.
task/evidence/SoT persisted.
commit pushed.
HEAD==origin/main.
clean tree.
W21 not started.

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
Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W20
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W19-R1
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
W19R1-Preservation-State:
Host-Admin-File-Count-Before:
Host-Admin-File-Count-After:
Host-ProductWorkspace-Route-Count-Before:
Host-ProductWorkspace-Route-Count-After:
ProductWorkspace-Endpoint-Route-Count:
BrandOptions-Host-Route-Count-Before:
BrandOptions-Host-Route-Count-After:
BrandOptions-Catalog-Route-Count:
BrandOptions-Ownership-State:
BrandOptions-CQRS-State:
BrandOptions-Application-Structure-State:
BrandOptions-Read-Port-State:
BrandOptions-Validator-Coverage-State:
BrandOptions-Result-State:
BrandOptions-Authorization-State:
BrandOptions-LocalizedName-State:
BrandOptions-Fallback-State:
BrandOptions-Search-State:
BrandOptions-Ordering-State:
BrandOptions-Limit-State:
BrandOptions-Status-State:
BrandOptions-PlatformHttpException-State:
BrandOptions-InvalidOperationExpectedFlow-State:
BrandOptions-MessageClassification-State:
Host-BrandOption-Residue-State:
Catalog-To-Host-State:
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
Do not start W21.
Do not migrate list/grid/writes.
Do not start another Host folder.
Wait for Architect review.

END_TOOBA_TASK