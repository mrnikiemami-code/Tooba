PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W6
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W5
Parent-Commit: 6642f818cd9cbdd106d8dfdc7a37a053acc00c79
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FOLDER_BY_FOLDER_ADMIN
Title: W6 — evacuate Catalog Facet Admin+Storefront slice to Catalog with focused capability CQRS/Result

ARCHITECT ACCEPTANCE

W5 is ARCHITECT-ACCEPTED.

Verified parent:
6642f818cd9cbdd106d8dfdc7a37a053acc00c79

Accepted W5 state:

Host/Admin 56 -> 55.
Host CatalogMegaMenuEndpoints.cs absent.
5 MegaMenu routes Catalog-owned exactly once.
MegaMenu capability = MegaMenu/{Commands,Queries,Models,Ports,Validators}.
IMegaMenuDirectory + MegaMenuDirectory focused seam.
all 5 HTTP operations MediatR/ISender-backed.
canonical Result + ApiResponseFactory + CatalogErrorCodes.
PlatformHttpException / InvalidOperationException expected-business flow absent on MegaMenu surface.
Catalog -> Host = ZERO.
Catalog.Endpoints -> Catalog.Infrastructure = ZERO.
StoreAppearance remains deferred.
schema/frontend unchanged.
W6 not started by W5.

ACTIVE HOST FOLDER

src/backend/Host/Tooba.Host/Admin/

Do not start another Host folder.
Do not start W7.

W6 BOUNDED SLICE

Primary Host production file:

Admin/CatalogFacetEndpoints.cs

Current route inventory:

GET /v1/admin/catalog/categories/{categoryId:guid}/facets/effective
GET /v1/admin/catalog/categories/{categoryId:guid}/facets/local
PUT /v1/admin/catalog/categories/{categoryId:guid}/facets/{definitionId:guid}
DELETE /v1/admin/catalog/categories/{categoryId:guid}/facets/{definitionId:guid}
PUT /v1/admin/catalog/categories/{categoryId:guid}/facets/order
GET /v1/storefront/categories/{categoryId:guid}/facets

Current violations/debt:

Host-owned Admin + Storefront Facet HTTP.
direct ICatalogDirectory from Host.
AdminPanelAccess in Host.
PlatformHttpException transport mapping.
InvalidOperationException message-driven expected-failure mapping.
hard-coded catalog.facet.* endpoint mapping.
transport records in Host.
Facet use-case authority remains in broad CatalogDirectory.
Persian exception messages inside broad CatalogDirectory represent expected business outcomes.
no focused Facets Application capability / persistence port.

REQUIRED SKILLS

Read current:

.cursor/skills/tooba-architecture-analyze/SKILL.md
.cursor/skills/tooba-architecture-migrate/SKILL.md
.cursor/skills/tooba-architecture-certify/SKILL.md

Also read:

AGENTS.md
docs/architecture/TMAR-HOST-EVACUATION-PROTOCOL.md
docs/architecture/TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md
docs/architecture/tmar-current-state.json
W1/W2/W2-R1/W3/W4/W5 evidence
Admin/CatalogFacetEndpoints.cs completely
all Facet-focused tests/callers/DI
current CatalogDirectory Facet methods + ResolveEffectiveFacets dependencies
current Catalog.Domain facet rules/entities/value kinds
Catalog error catalog/resources
current Catalog.Endpoints Admin/Storefront patterns

MANDATORY ANALYZE FIRST

Before code changes create a member-level disposition map for CatalogFacetEndpoints.cs and every Facet-specific CatalogDirectory member/helper required by the six routes.

At minimum classify:

MapCatalogFacetEndpoints
GetEffectiveFacetsAsync
ListLocalFacetsAsync
UpsertFacetAsync
RemoveFacetOverrideAsync
ReorderFacetsAsync
GetStorefrontFacetsAsync
ToError
UpsertCategoryFacetRequest
ReorderCategoryFacetsRequest
GetEffectiveCategoryFacetsAsync
ListLocalFacetConfigurationsAsync
UpsertCategoryFacetConfigurationAsync
RemoveCategoryFacetOverrideAsync
ReorderCategoryFacetConfigurationsAsync
ResolveEffectiveFacetsAsync and exact dependent helpers
GetAttributeDefinitionNamesAsync if used by Facets
mutation guard usage
category existence semantics
definition existence semantics
effective-schema membership semantics
filterable-only rule
CatalogCategoryFacetRules.ValidateDisplayType behavior
searchable coercion behavior
local override missing behavior
exact reorder-set validation
inherited/effective facet composition semantics
locale/default behavior
Program route registration

No code move before this map exists.

TARGET OWNERSHIP

Catalog owns:

Facet Admin use cases.
Facet Storefront read use case.
Facet read/write models.
Facet persistence/effective resolution.
stable Facet error codes.
all six HTTP routes.

Host owns:

no Facet HTTP business route.
no Facet business adapter.
only neutral platform/composition/security seams.

CATALOG APPLICATION STRUCTURE

Capability-first shallow-by-default, e.g.:

Tooba.Catalog.Application/
Facets/
Commands/
UpsertCategoryFacetCommand.cs
UpsertCategoryFacetHandler.cs
RemoveCategoryFacetOverrideCommand.cs
RemoveCategoryFacetOverrideHandler.cs
ReorderCategoryFacetsCommand.cs
ReorderCategoryFacetsHandler.cs
Queries/
GetEffectiveCategoryFacetsQuery.cs
GetEffectiveCategoryFacetsHandler.cs
ListLocalCategoryFacetsQuery.cs
ListLocalCategoryFacetsHandler.cs
GetStorefrontCategoryFacetsQuery.cs
GetStorefrontCategoryFacetsHandler.cs
Models/
...
Ports/
IFacetDirectory.cs
Validators/
...

Equivalent cohesive naming is allowed if current Catalog terminology strongly prefers another name.

Mandatory:

no generic/mixed *Contracts.cs bundle.
no root Application Facet files.
no one-folder-per-single-request tree.
exact path <-> namespace.
authoritative request types unique.
internal Facet DTOs remain Application-owned unless a real foreign-module consumer requires Contracts.

FOCUSED PERSISTENCE PORT

Do not keep migrated handlers calling broad ICatalogDirectory if a focused seam is feasible.

Preferred:

Application uses IFacetDirectory.
Infrastructure implements FacetDirectory.
FacetDirectory uses CatalogDbContext + Catalog.Domain facet rules.
move only Facet-specific behavior/helpers from CatalogDirectory.
do not duplicate business authority.
if broad CatalogDirectory has legacy non-HTTP consumers, retain thin one-way wrappers into the focused Facet implementation only where necessary and record them as residual debt.
do not decompose unrelated attribute/category/product code in this wave.

CQRS / ENDPOINTS

All six routes must be Catalog-owned and MediatR-backed.

Admin:
HTTP
-> ICatalogAdminAuthorizer
-> ISender
-> Command/Query
-> Handler
-> IFacetDirectory
-> Result

Storefront:
HTTP
-> ISender
-> GetStorefrontCategoryFacetsQuery
-> Handler
-> IFacetDirectory
-> Result

Endpoints must not inject ICatalogDirectory / IFacetDirectory / CatalogDbContext.

Destination:

Tooba.Catalog.Endpoints/Admin/Facets/...
Tooba.Catalog.Endpoints/Storefront/Facets/...
or equally cohesive current Catalog structure.

RESULT / ERROR SEMANTICS

Expected business failures must not use:

PlatformHttpException.
InvalidOperationException as expected flow.
ex.Message parsing/classification.
hard-coded user-facing text in endpoint responses.
parallel Results.Json/Problem expected-failure mapping.

Use:

Result / Result<T>
CatalogErrorCodes
CatalogErrorCatalogContributor
CatalogErrors.resx + CatalogErrors.fa.resx
ApiResponseFactory

Audit prior externally visible errorCode behavior before normalization.

At minimum classify stable outcomes for:

category missing.
attribute definition missing.
definition not present in effective schema.
definition not filterable.
invalid display type for value kind.
local facet override missing.
reorder list invalid/incomplete/duplicate/mismatched.

Prefer semantically distinct codes only where client behavior truly differs.
Unknown exceptions propagate.

DOMAIN RULE HANDLING

CatalogCategoryFacetRules must remain authoritative for business validity.

If current ValidateDisplayType throws expected exceptions:

convert to a typed/domain outcome or Result-compatible violation style rather than catching message text.
do not move domain business rules into FluentValidation.

Preserve:

display type compatibility.
IsSearchable coercion based on allowed display type.
inheritance/effective resolution semantics.
SourceCategoryId / IsInherited semantics.
order behavior.
local override semantics.

VALIDATION MATRIX

Classify all six endpoint-reachable requests exactly one:

VALIDATOR_REQUIRED
NO_VALIDATOR_REQUIRED

Expected baseline to verify:

GetEffectiveCategoryFacetsQuery: NO_VALIDATOR_REQUIRED.
ListLocalCategoryFacetsQuery: NO_VALIDATOR_REQUIRED.
UpsertCategoryFacetCommand: VALIDATOR_REQUIRED for transport shape only.
RemoveCategoryFacetOverrideCommand: NO_VALIDATOR_REQUIRED.
ReorderCategoryFacetsCommand: VALIDATOR_REQUIRED for transport shape (non-null collection; malformed/duplicate primitive-shape checks only if genuinely transport-level).
GetStorefrontCategoryFacetsQuery: NO_VALIDATOR_REQUIRED.

Do not duplicate DB existence, effective-schema membership, filterability, display-type business rules, or exact persisted-set reorder rules inside validators.

BEHAVIOR PARITY

Preserve:

default locale = fa-IR.
effective facet resolution/inheritance.
localized definition names and fallback currently used by Catalog.
empty effective list behavior.
local list ordering by SortOrder.
upsert create/update behavior.
IsSearchable coercion.
remove override behavior.
reorder assigns sequential SortOrder based on supplied exact set.
Storefront route uses same effective facet semantics.
methods/paths.
success statuses and JSON shapes.
four? actually FIVE Admin routes all retain Admin authorization.
Storefront route remains unauthenticated unless global policy already says otherwise.
tenant isolation/mutation guard semantics.

ROUTE AUTH COUNT

There are FIVE Admin routes:

effective
local
upsert
delete override
reorder

All five must use ICatalogAdminAuthorizer.

One Storefront route:

no Admin authorization.

HOST INVENTORY

Re-enumerate Host/Admin recursive production *.cs count at task start/end.

Expected if file fully evacuated:
55 -> 54

Do not hard-code PASS to 54 if analysis discovers a legitimate retained non-business responsibility; any retention must be justified and cannot leave Facet HTTP/business ownership in Host.

DO NOT TOUCH

Do not migrate in W6:

CatalogCategoryEndpoints.cs
CatalogAttributeEndpoints.cs
StoreAppearanceSettings*
ProductWorkspace*
Merchandising*
StoreLanding*
StoreMenu*
Checkout*/Reservation*/HoldPolicy*
except minimal Program/CatalogEndpointModule registration required by this Facet move.

Do not rewrite global category-schema architecture beyond what Facet extraction requires.

DURABLE GUARDS

Add/strengthen guards proving:

Host/Admin/CatalogFacetEndpoints.cs absent after full evacuation.
Host no longer maps Facet routes.
Catalog.Endpoints owns all 6 routes exactly once.
all 5 Admin routes use ICatalogAdminAuthorizer + ISender.
Storefront Facet route uses ISender and is Host-free.
endpoints inject no broad/focused directory or DbContext.
Catalog.Endpoints -> Catalog.Infrastructure = ZERO.
Catalog -> Host = ZERO.
no PlatformHttpException on touched Facet surface.
no InvalidOperationException expected-business/message-as-code flow on touched Facet surface.
Result + ApiResponseFactory canonical.
Facets capability-first shallow structure exists.
focused IFacetDirectory/FacetDirectory exists or equally narrow lawful seam evidenced.
no duplicate authoritative MediatR requests.
validator matrix exhaustive for all six requests.
CatalogCategoryFacetRules business authority preserved.
one descriptor owner per facet error code.
exact path <-> namespace.
Host/Admin count delta matches disposition.
W1-W5 preserved.
StoreAppearance still deferred.
schema/frontend unchanged.
W7/other Host folder not started.

TESTS

Add/update focused tests for:

effective facets including inherited/local state.
local facet list.
valid upsert.
category missing.
definition missing.
non-effective-schema definition.
non-filterable definition.
invalid display type.
searchable coercion.
remove missing override.
valid remove.
reorder exact set success.
reorder malformed/mismatched set failure.
storefront effective facets.
locale default.
route ownership/auth.
canonical errorCode/status.

Focused validation:

Catalog.Contracts build
Catalog.Domain build if touched
Catalog.Application build
Catalog.Infrastructure build
Catalog.Endpoints build
Host build
Host.Tests build
Facet focused behavior tests
W1/W2/W2-R1/W3/W4/W5/W6 architecture guards

Tests are evidence, not navigation.
No open-ended test/repair loop.
If bounded scope cannot be completed safely in task timebox, return INCOMPLETE with exact remainder and STOP.

EVIDENCE

Create:
docs/evidence/TB-TMAR-HOST-ADMIN-AMC-001-W6/

Required:

analyze.md
disposition-map.md
facet-capability.md
cqrs.md
result-errors.md
validation.md
behavior-parity.md
closure.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-ADMIN-AMC-001-W6.task.md

RECOVERY SOT

Add hostAdminAmcW6 to docs/architecture/tmar-current-state.json preserving W1/W2/W2-R1/W3/W4/W5.

Record:

task/parent/parentCommit
activeHostFolder=Admin
Host Admin count before/after
Facet endpoint ownership
six-route inventory
CQRS state
focused port/directory state
validator matrix
Result/error state
message classification state
PlatformHttpException state
Admin authorization state
Storefront route state
domain rule state
Catalog->Host
Endpoints->Infrastructure
route/behavior parity
path/namespace
StoreAppearance deferred
schema/frontend unchanged
remaining Admin blockers
nextHostFolderStarted=false
workflowStop=USER_REVIEW_HOST_ADMIN_W6_CHECKPOINT

PASS CRITERIA

PASS only if ALL:

all live CatalogFacetEndpoints responsibilities correctly dispositioned.
Host CatalogFacetEndpoints.cs removable with no lost responsibility.
all 6 routes Catalog-owned exactly once.
all 6 routes MediatR-backed.
all 5 Admin routes authorize through ICatalogAdminAuthorizer.
Storefront route remains lawful and Host-free.
endpoint direct ICatalogDirectory/IFacetDirectory/DbContext access ZERO.
focused Facets Application capability exists.
focused Facet persistence seam exists or equally narrow lawful design evidenced.
expected Facet failures use Result/stable Catalog codes.
no PlatformHttpException expected-business flow on touched surface.
no InvalidOperationException message-as-code expected flow.
ApiResponseFactory canonical.
error catalog/resources complete.
CatalogCategoryFacetRules remains business authority.
validator classification exhaustive.
route/method/status/JSON/auth/locale behavior preserved.
effective/inherited facet semantics preserved.
Catalog->Host ZERO.
Endpoints->Infrastructure ZERO.
path<->namespace exact.
Host/Admin final inventory exact.
W1-W5 preserved.
StoreAppearance still deferred.
no schema/migration/frontend change.
focused builds/tests/guards PASS.
evidence/task/SoT persisted.
commit pushed.
HEAD == origin/main.
working tree clean.
W7 not started.
next Host folder not started.

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
On conflict return RECOVERY_CONFLICT.

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W6
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W5
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
W5-Preservation-State:
Host-Admin-File-Count-Before:
Host-Admin-File-Count-After:
Facet-Host-File-State:
Facet-Endpoint-Ownership-State:
Facet-Route-Count:
Facet-CQRS-State:
Facet-Application-Structure-State:
Facet-Persistence-Port-State:
Facet-Validator-Coverage-State:
Facet-Result-State:
Facet-Error-Localization-State:
Facet-Message-Classification-State:
Facet-PlatformHttpException-State:
Facet-InvalidOperationExpectedFlow-State:
Facet-Domain-Rule-State:
Admin-Authorization-State:
Storefront-Route-State:
Endpoints-To-Infrastructure-State:
Catalog-To-Host-State:
Route-Parity-State:
Behavior-Parity-State:
Path-Namespace-State:
Store-Appearance-State:
Schema-Migration-State:
Frontend-State:
Focused-Validation:
Remaining-Admin-Blockers:
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

After returning:

STOP completely.
Do not start W7.
Do not inspect/start another Host folder for execution.
Wait for Architect review.

END_TOOBA_TASK