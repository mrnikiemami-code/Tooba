PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W7
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W6
Parent-Commit: 988fd489897c0487a3a9951f268fe8743e8d2a5f
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FOLDER_BY_FOLDER_ADMIN
Title: W7 — evacuate Catalog Category Admin+Storefront route slice to Catalog with focused capability CQRS/Result

ARCHITECT ACCEPTANCE

W6 is ARCHITECT-ACCEPTED.

Verified parent:
988fd489897c0487a3a9951f268fe8743e8d2a5f

Accepted W6 state:

Host/Admin 55 -> 54.
Host CatalogFacetEndpoints.cs absent.
6 Facet routes Catalog-owned exactly once.
Facets capability = Facets/{Commands,Queries,Models,Ports,Validators}.
IFacetDirectory + FacetDirectory focused seam.
all 6 operations MediatR/ISender-backed.
Result + ApiResponseFactory + CatalogErrorCodes canonical.
Facet expected failures no longer use PlatformHttpException or InvalidOperationException message-as-code.
Catalog -> Host = ZERO.
Catalog.Endpoints -> Catalog.Infrastructure = ZERO.
StoreAppearance deferred.
schema/frontend unchanged.
W7 not started by W6.

ACTIVE HOST FOLDER

src/backend/Host/Tooba.Host/Admin/

Do not start another Host folder.
Do not start W8.

W7 BOUNDED SLICE

Primary Host production file:

Admin/CatalogCategoryEndpoints.cs

Current route inventory:

GET /v1/admin/catalog/categories/tree
GET /v1/admin/catalog/categories/{id:guid}
POST /v1/admin/catalog/categories/
PATCH /v1/admin/catalog/categories/{id:guid}
PUT /v1/admin/catalog/categories/{id:guid}/translations/{locale}
POST /v1/admin/catalog/categories/{id:guid}/move
POST /v1/admin/catalog/categories/reorder
POST /v1/admin/catalog/categories/{id:guid}/publish
POST /v1/admin/catalog/categories/{id:guid}/archive
GET /v1/storefront/category-routes/resolve

Current Host violations/debt:

Category HTTP ownership in Host.
direct ICatalogDirectory dependency.
AdminPanelAccess security coupling.
PlatformHttpException mapping.
InvalidOperationException expected-business mapping.
message parsing for duplicate slug classification.
hard-coded user-facing Persian/English transport errors.
transport DTOs in Host.
Category use-case authority concentrated in broad CatalogDirectory.
Storefront route resolve mixed into Host.
no focused Categories capability port/directory for this slice.

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
W1/W2/W2-R1/W3/W4/W5/W6 evidence
Admin/CatalogCategoryEndpoints.cs completely
all category-focused tests/callers/DI
current CatalogDirectory category methods/helpers
category Domain rules/tree/slug/translation/publication logic
Catalog error catalog/resources
current Catalog.Endpoints Admin and Storefront conventions

MANDATORY ANALYZE FIRST

Before code changes create a member-level disposition map for CatalogCategoryEndpoints.cs and every category-specific CatalogDirectory member/helper required by these ten routes.

At minimum classify:

MapCatalogCategoryEndpoints
GetTreeAsync
GetWorkspaceAsync
CreateAsync
UpdateCoreAsync
UpsertTranslationAsync
MoveAsync
ReorderAsync
PublishAsync
ArchiveAsync
ResolveRouteAsync
MapCategoryInvalid
ToError
all six Host transport records
GetCategoryTreeAsync
GetCategoryWorkspaceAsync
both CreateCategoryAsync overloads used here
UpdateCategoryCoreAsync
UpsertCategoryTranslationAsync
MoveCategoryAsync
ReorderCategorySiblingsAsync
PublishCategoryAsync
ArchiveCategoryAsync
ResolveCategoryRouteAsync
exact slug uniqueness helpers
category hierarchy depth/self-parent/descendant rules
concurrency/ExpectedUpdatedAt behavior
media clear/set semantics
translation/locale/slug normalization
publish/archive lifecycle
route-history/canonical redirect semantics
storefront eligibility behavior
mutation guard usage
Program route registration

No code move before this map exists.

TARGET OWNERSHIP

Catalog owns:

Category tree/workspace use cases.
Category create/update/translation/move/reorder/publish/archive use cases.
storefront category-route resolve use case.
Category read/write models.
Category persistence/orchestration.
stable Category error codes.
all ten HTTP routes.

Host owns:

no Category business HTTP route.
no Category business adapter.
only neutral platform/composition/security seams.

CATALOG APPLICATION STRUCTURE

Capability-first shallow-by-default, e.g.:

Tooba.Catalog.Application/
Categories/
Commands/
CreateCategoryCommand.cs
CreateCategoryHandler.cs
UpdateCategoryCoreCommand.cs
UpdateCategoryCoreHandler.cs
UpsertCategoryTranslationCommand.cs
UpsertCategoryTranslationHandler.cs
MoveCategoryCommand.cs
MoveCategoryHandler.cs
ReorderCategoriesCommand.cs
ReorderCategoriesHandler.cs
PublishCategoryCommand.cs
PublishCategoryHandler.cs
ArchiveCategoryCommand.cs
ArchiveCategoryHandler.cs
Queries/
GetCategoryTreeQuery.cs
GetCategoryTreeHandler.cs
GetCategoryWorkspaceQuery.cs
GetCategoryWorkspaceHandler.cs
ResolveCategoryRouteQuery.cs
ResolveCategoryRouteHandler.cs
Models/
...
Ports/
ICategoryDirectory.cs
Validators/
...

Equivalent cohesive naming is allowed if current Catalog conventions justify it.

Mandatory:

no generic/mixed *Contracts.cs bundle.
no root Application Category files introduced.
no one-folder-per-single-request tree.
exact path <-> namespace.
authoritative request type exactly once.
internal Category DTOs stay Application-owned unless a real foreign-module consumer exists.

FOCUSED PERSISTENCE PORT

Do not keep migrated handlers calling broad ICatalogDirectory if a focused seam is feasible.

Preferred:

Application uses ICategoryDirectory.
Infrastructure implements CategoryDirectory.
CategoryDirectory uses CatalogDbContext + Catalog.Domain rules.
extract only category-specific helpers required by these ten routes.
legacy ICatalogDirectory may retain thin one-way wrappers for CatalogDemo/tests only if needed.
no duplicate business authority.
do not decompose unrelated Product/Attribute/MegaMenu/Facet code.

CQRS / ENDPOINTS

All ten routes must be Catalog-owned and MediatR-backed.

Admin routes:
HTTP
-> ICatalogAdminAuthorizer
-> ISender
-> Command/Query
-> Handler
-> ICategoryDirectory
-> Result

Storefront route:
HTTP
-> ISender
-> ResolveCategoryRouteQuery
-> Handler
-> ICategoryDirectory
-> Result

Do not inject directory/DbContext into endpoints.

Suggested destinations:

Tooba.Catalog.Endpoints/Admin/Categories/...
Tooba.Catalog.Endpoints/Storefront/Categories/...
or equivalent cohesive current structure.

RESULT / ERROR / LOCALIZATION

Expected failures must NOT use:

PlatformHttpException.
InvalidOperationException as business transport.
ex.Message parsing.
hard-coded Persian/English error titles.
endpoint catch-and-map for expected business failures.

Use:

Result / Result<T>
CatalogErrorCodes
CatalogErrorCatalogContributor
CatalogErrors.resx / CatalogErrors.fa.resx
ApiResponseFactory

Audit current externally visible semantics before normalization.

At minimum classify stable codes for:

category missing.
invalid category input.
locale required/invalid.
slug invalid.
slug duplicate/conflict.
parent missing.
self-parent / descendant-as-parent.
max-depth violation.
reorder invalid/mismatched set.
concurrency conflict where ExpectedUpdatedAt is enforced.
route resolve invalid.
route resolve missing.
publish/archive invalid lifecycle if current Domain exposes such expected outcomes.

Do not create redundant codes without distinct semantics.

DUPLICATE SLUG

Current Host does message parsing:

ex.Message contains "slug"
ex.Message contains "تکراری"
then emits catalog.category.slug.duplicate with 409.

W7 must remove message parsing entirely.

Preserve the client-visible duplicate-slug semantic:

stable code catalog.category.slug.duplicate
HTTP 409 unless an existing canonical lock already says otherwise
localized descriptor/resources
typed/domain/result outcome from Category persistence/rules

TREE RULES

Preserve current Category rules exactly:

self-parent forbidden.
descendant-as-parent forbidden.
max depth = 3.
product-assignable level = 3 remains unchanged.
sibling reorder semantics unchanged.
no hierarchy rule duplicated in FluentValidation.

TRANSLATION / ROUTING

Preserve:

locale normalization behavior.
translation fields and SEO fields.
slug canonicalization/history behavior.
route resolve current-slug priority.
historical slug redirect behavior.
canonical path semantics.
forStorefront eligibility filtering.
route missing behavior.
Storefront route remains unauthenticated unless global policy already says otherwise.

CREATE PARITY

The Host currently supports two create shapes:

structured Translations create path.
legacy LocalizedNames fallback path.

Do not silently remove the fallback unless repository analysis proves it has zero live consumers and task evidence explicitly justifies removal.

Preserve 201 response shape.

UPDATE PARITY

Preserve:

status/sort/visibility updates.
image/icon/banner set/clear behavior.
ExpectedUpdatedAt concurrency behavior.
returned workspace shape.

VALIDATION MATRIX

Classify all ten endpoint-reachable requests exactly one:

VALIDATOR_REQUIRED
NO_VALIDATOR_REQUIRED

Likely baseline to verify:

GetCategoryTreeQuery: VALIDATOR_REQUIRED if locale is required as current HTTP requires nonblank.
GetCategoryWorkspaceQuery: NO_VALIDATOR_REQUIRED.
CreateCategoryCommand: VALIDATOR_REQUIRED.
UpdateCategoryCoreCommand: VALIDATOR_REQUIRED only for transport-shape contradictions if any.
UpsertCategoryTranslationCommand: VALIDATOR_REQUIRED.
MoveCategoryCommand: NO_VALIDATOR_REQUIRED unless transport-shape validation is needed beyond route/body binding.
ReorderCategoriesCommand: VALIDATOR_REQUIRED for collection shape.
PublishCategoryCommand: NO_VALIDATOR_REQUIRED.
ArchiveCategoryCommand: NO_VALIDATOR_REQUIRED.
ResolveCategoryRouteQuery: VALIDATOR_REQUIRED because locale + slug are required.

FluentValidation only for untrusted transport shape.
Business rules stay Domain/Application/Infrastructure Result logic.

BEHAVIOR PARITY

Preserve:

all route methods/paths.
9 Admin routes authorized via ICatalogAdminAuthorizer.
one Storefront resolve route no Admin auth.
success status codes/JSON shapes.
category tree locale/search behavior.
workspace missing = canonical 404.
create structured and legacy fallback semantics.
update + returned workspace.
translation upsert.
move + returned workspace.
reorder success { ok = true }.
publish/archive + returned workspace.
route resolve 400/404 semantics and result shape.
tenant isolation.
mutation guard.
current category history/canonical redirect behavior.

HOST INVENTORY

Re-enumerate Host/Admin recursive production *.cs before/after.

Expected if full file safely evacuated:
54 -> 53

Do not hard-code PASS to 53 if legitimate retained non-business responsibility is discovered; any retained responsibility must be documented and cannot leave Category HTTP/business authority in Host.

DO NOT TOUCH

Do not migrate in W7:

CatalogAttributeEndpoints.cs
StoreAppearanceSettings*
ProductWorkspace*
Merchandising*
StoreLanding*
StoreMenu*
Checkout*/Reservation*/HoldPolicy*
except minimal Program/CatalogEndpointModule registration required for Category routes.

Do not rewrite unrelated Catalog capabilities.

DURABLE GUARDS

Add/strengthen guards proving:

Host/Admin/CatalogCategoryEndpoints.cs absent after full evacuation.
Host no longer maps Category routes.
Catalog.Endpoints owns all 10 routes exactly once.
all 9 Admin routes use ICatalogAdminAuthorizer + ISender.
Storefront route uses ISender and no Host dependency.
endpoints inject no ICatalogDirectory/ICategoryDirectory/CatalogDbContext.
Catalog.Endpoints -> Catalog.Infrastructure = ZERO.
Catalog -> Host = ZERO.
no PlatformHttpException on touched Category surface.
no InvalidOperationException expected-business/message-as-code flow on touched Category surface.
no ex.Message parsing for duplicate slug.
Result + ApiResponseFactory canonical.
Categories capability-first shallow structure exists.
focused ICategoryDirectory/CategoryDirectory exists or equally narrow lawful seam evidenced.
no duplicate authoritative request types.
validator matrix exhaustive for all ten requests.
category tree rules remain Domain/business authority.
slug duplicate maps to stable code without message parsing.
route-history/canonical resolution behavior preserved.
path <-> namespace exact.
Host/Admin file-count delta correct.
W1-W6 preserved.
StoreAppearance still deferred.
schema/frontend unchanged.
W8/other Host folder not started.

TESTS

Add/update focused tests for:

tree locale required.
tree hierarchy/search.
workspace get + missing.
create structured translations.
create legacy LocalizedNames fallback.
duplicate slug => stable 409 code.
update core + media clear/set + concurrency.
translation upsert.
move valid.
self-parent rejected.
descendant-as-parent rejected.
max depth rejected.
reorder exact sibling set.
publish/archive.
resolve current slug.
resolve historical slug redirect.
resolve storefront ineligible behavior.
route invalid/missing.
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
Category focused behavior tests
W1/W2/W2-R1/W3/W4/W5/W6/W7 guards

Tests are evidence, not navigation.
No open-ended repair loop.
If bounded scope cannot be safely completed inside task timebox, return INCOMPLETE with exact remainder and STOP.

EVIDENCE

Create:
docs/evidence/TB-TMAR-HOST-ADMIN-AMC-001-W7/

Required:

analyze.md
disposition-map.md
category-capability.md
cqrs.md
result-errors.md
hierarchy-slug-routing.md
validation.md
behavior-parity.md
closure.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-ADMIN-AMC-001-W7.task.md

RECOVERY SOT

Add hostAdminAmcW7 to docs/architecture/tmar-current-state.json preserving W1/W2/W2-R1/W3/W4/W5/W6.

Record:

task/parent/parentCommit
activeHostFolder=Admin
Host Admin file count before/after
Category endpoint ownership
ten-route inventory
CQRS state
focused port/directory state
validator matrix
Result/error state
slug duplicate typed outcome state
message classification state
PlatformHttpException state
Admin auth state
Storefront route state
hierarchy rule state
route-history/canonical state
Catalog->Host
Endpoints->Infrastructure
route/behavior parity
path/namespace
StoreAppearance deferred
schema/frontend unchanged
remaining Admin blockers
nextHostFolderStarted=false
workflowStop=USER_REVIEW_HOST_ADMIN_W7_CHECKPOINT

PASS CRITERIA

PASS only if ALL:

all live CatalogCategoryEndpoints responsibilities correctly dispositioned.
Host CatalogCategoryEndpoints.cs removable with no lost responsibility.
all 10 routes Catalog-owned exactly once.
all 10 routes MediatR-backed.
all 9 Admin routes use ICatalogAdminAuthorizer.
Storefront resolve route remains Host-free and unauthenticated as before.
endpoint direct broad/focused directory/DbContext access ZERO.
focused Categories Application capability exists.
focused Category persistence seam exists or equally narrow lawful design evidenced.
expected Category failures use Result/stable Catalog codes.
no PlatformHttpException expected-business flow on touched surface.
no InvalidOperationException message-as-code expected flow.
no duplicate-slug message parsing.
duplicate slug preserves stable catalog.category.slug.duplicate conflict semantics.
ApiResponseFactory canonical.
error catalog/resources complete.
validator matrix exhaustive.
tree/hierarchy/business rules preserved.
structured + legacy create behavior preserved unless zero-consumer removal is proven.
route history/canonical redirect semantics preserved.
route/method/status/JSON/auth behavior preserved.
Catalog->Host ZERO.
Endpoints->Infrastructure ZERO.
path<->namespace exact.
Host/Admin final count exact.
W1-W6 preserved.
StoreAppearance still deferred.
no schema/migration/frontend change.
focused builds/tests/guards PASS.
evidence/task/SoT persisted.
commit pushed.
HEAD == origin/main.
working tree clean.
W8 not started.
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
Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W7
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W6
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
W6-Preservation-State:
Host-Admin-File-Count-Before:
Host-Admin-File-Count-After:
Category-Host-File-State:
Category-Endpoint-Ownership-State:
Category-Route-Count:
Category-CQRS-State:
Category-Application-Structure-State:
Category-Persistence-Port-State:
Category-Validator-Coverage-State:
Category-Result-State:
Category-Error-Localization-State:
Category-Slug-Duplicate-State:
Category-Message-Classification-State:
Category-PlatformHttpException-State:
Category-InvalidOperationExpectedFlow-State:
Category-Hierarchy-Rule-State:
Category-Route-History-State:
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
Do not start W8.
Do not inspect/start another Host folder for execution.
Wait for Architect review.

END_TOOBA_TASK