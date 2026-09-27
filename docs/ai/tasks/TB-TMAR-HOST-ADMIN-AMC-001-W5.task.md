PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W5
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W4
Parent-Commit: ead84e5e9f7c2dd50c8496288523ef087a20a289
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FOLDER_BY_FOLDER_ADMIN
Title: W5 — evacuate Catalog MegaMenu Admin+Storefront slice to Catalog with focused capability CQRS/Result

ARCHITECT ACCEPTANCE

W4 is ARCHITECT-ACCEPTED.

Verified parent:
ead84e5e9f7c2dd50c8496288523ef087a20a289

Accepted W4 state:

Host/Admin 57 -> 56.
Host CatalogTagEndpoints.cs absent.
9 Tag routes Catalog-owned exactly once.
Tag capability = Tags/{Commands,Queries,Models,Ports,Validators}.
ITagDirectory + TagDirectory focused persistence seam.
all 9 HTTP operations MediatR/ISender-backed.
canonical Result + ApiResponseFactory + CatalogErrorCodes.
Tag expected failures no longer use PlatformHttpException / InvalidOperationException message-as-code.
Catalog -> Host = ZERO.
Catalog.Endpoints -> Catalog.Infrastructure = ZERO.
StoreAppearance deferred.
schema/frontend unchanged.
W5 not started by W4.

ACTIVE HOST FOLDER

src/backend/Host/Tooba.Host/Admin/

Do not start another Host folder.
Do not start W6.

W5 BOUNDED SLICE

Primary Host production file:

Admin/CatalogMegaMenuEndpoints.cs

Current route inventory:

GET /v1/admin/catalog/categories/{categoryId:guid}/mega-menu
GET /v1/admin/catalog/categories/{categoryId:guid}/mega-menu/placement-options
PUT /v1/admin/catalog/categories/{categoryId:guid}/mega-menu
DELETE /v1/admin/catalog/categories/{categoryId:guid}/mega-menu
GET /v1/storefront/mega-menu

This file mixes:

Admin authorization in Host.
Storefront route in Host.
direct ICatalogDirectory use.
locale parsing/defaulting.
PlatformHttpException mapping.
InvalidOperationException expected-business mapping.
transport input owned by broad Catalog application surface.
MegaMenu behavior still concentrated inside broad CatalogDirectory.

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
W1/W2/W2-R1/W3/W4 evidence
Admin/CatalogMegaMenuEndpoints.cs completely
all MegaMenu tests/callers/DI
current CatalogDirectory MegaMenu methods/helpers
Catalog.Domain MegaMenu rules/composer/entities
Catalog error catalog/resources
Catalog.Endpoints current Admin and Storefront conventions

MANDATORY ANALYZE FIRST

Before code changes create a member-level disposition map for CatalogMegaMenuEndpoints.cs and every MegaMenu-specific CatalogDirectory member/helper required by these 5 routes.

At minimum classify:

MapCatalogMegaMenuEndpoints
GetCategoryMegaMenuAsync
ListPlacementOptionsAsync
UpsertCategoryMegaMenuAsync
RemoveCategoryMegaMenuAsync
GetStorefrontMegaMenuAsync
ReadLocale
ToError
CategoryMegaMenuBindingInput ownership
GetCategoryMegaMenuConfigurationAsync
ListMegaMenuPlacementOptionsAsync
UpsertCategoryMegaMenuBindingAsync
RemoveCategoryMegaMenuBindingAsync
GetStorefrontMegaMenuAsync
UpsertMegaMenuTranslationAsync
BuildMenuPathAsync / presentation-level helpers used by this capability
ResolveCategoryDisplayNameAsync if used
category existence behavior
tree placement validation behavior
remove-with-children rejection behavior
locale normalization/default behavior
storefront publication/visibility/composition behavior
Program route registration

No code move before this map exists.

TARGET OWNERSHIP

Catalog owns:

MegaMenu Admin use cases.
MegaMenu Storefront read use case.
MegaMenu read/write models.
MegaMenu persistence/composition adapter.
stable MegaMenu error codes.
all 5 HTTP routes.

Host owns:

no MegaMenu business HTTP route.
no MegaMenu business adapter.
only neutral platform/composition/security seams.

CATALOG APPLICATION STRUCTURE

Capability-first shallow-by-default, e.g.:

Tooba.Catalog.Application/
MegaMenu/
Commands/
UpsertCategoryMegaMenuCommand.cs
UpsertCategoryMegaMenuHandler.cs
RemoveCategoryMegaMenuCommand.cs
RemoveCategoryMegaMenuHandler.cs
Queries/
GetCategoryMegaMenuQuery.cs
GetCategoryMegaMenuHandler.cs
ListMegaMenuPlacementOptionsQuery.cs
ListMegaMenuPlacementOptionsHandler.cs
GetStorefrontMegaMenuQuery.cs
GetStorefrontMegaMenuHandler.cs
Models/
...
Ports/
IMegaMenuDirectory.cs
Validators/
...

Equivalent cohesive naming is allowed if current semantics justify it.

Mandatory:

no generic/mixed *Contracts.cs bundle.
no root Application MegaMenu files.
no one-leaf-folder-per-request pattern.
exact path <-> namespace.
authoritative request types unique.
internal MegaMenu DTOs stay Application-owned unless a real foreign-module consumer exists.

FOCUSED PERSISTENCE PORT

Do NOT leave handlers calling broad ICatalogDirectory for this migrated slice if a focused port is feasible.

Preferred:

Application uses IMegaMenuDirectory.
Infrastructure implements MegaMenuDirectory.
MegaMenuDirectory uses CatalogDbContext and existing Catalog.Domain MegaMenu rules/composer.
extract only MegaMenu-specific helpers needed for this capability.
do not decompose unrelated CatalogDirectory areas.
if temporary thin legacy wrappers are needed for existing non-HTTP callers/tests, keep them explicitly legacy and one-way into the focused MegaMenu implementation; do not keep duplicate business authority.

CQRS / ENDPOINTS

All 5 routes must be module-owned and MediatR-backed.

Admin routes:
HTTP
-> ICatalogAdminAuthorizer
-> ISender
-> Command/Query
-> Handler
-> IMegaMenuDirectory
-> Result

Storefront route:
HTTP
-> ISender
-> GetStorefrontMegaMenuQuery
-> Handler
-> IMegaMenuDirectory
-> Result

Do not inject directory/DbContext into endpoints.

Endpoint destinations:

Tooba.Catalog.Endpoints/Admin/MegaMenu/...
Tooba.Catalog.Endpoints/Storefront/MegaMenu/...
or equally cohesive current Catalog.Endpoints structure.

RESULT / ERROR SEMANTICS

Expected business failures must not use:

PlatformHttpException.
InvalidOperationException as transport/business flow.
ex.Message classification.
hard-coded user-facing errors.
parallel Results.Problem expected-failure mapping.

Use:

Result / Result<T>
CatalogErrorCodes
CatalogErrorCatalogContributor
CatalogErrors.resx + CatalogErrors.fa.resx
ApiResponseFactory

Audit and preserve/normalize current externally observable behavior deliberately.

At minimum classify stable outcomes for:

category missing.
invalid MegaMenu placement/tree rule.
removing a MegaMenu item that still has children.
invalid input/translation shape if applicable.

Do not create redundant codes where one semantic code is sufficient.
Unknown exceptions propagate.

VALIDATION MATRIX

Classify every endpoint-reachable request exactly:

VALIDATOR_REQUIRED
or
NO_VALIDATOR_REQUIRED

Likely baseline to verify:

GetCategoryMegaMenuQuery: NO_VALIDATOR_REQUIRED.
ListMegaMenuPlacementOptionsQuery: NO_VALIDATOR_REQUIRED.
UpsertCategoryMegaMenuCommand: VALIDATOR_REQUIRED for transport shape only.
RemoveCategoryMegaMenuCommand: NO_VALIDATOR_REQUIRED.
GetStorefrontMegaMenuQuery: NO_VALIDATOR_REQUIRED unless explicit locale transport constraints exist.

Do not move tree/business rules into FluentValidation.

BEHAVIOR PARITY

Preserve:

locale query default = fa-IR.
locale normalization behavior.
category missing behavior, now canonical Result.
unbound category returns current preview/default configuration behavior.
display title fallback.
parent path computation.
presentation level.
placement option filtering at MaxPresentationDepth.
ordering by SortOrder.
update/create binding semantics.
translation override semantics.
remove absent binding remains idempotent.
remove with children is rejected.
storefront menu composition semantics.
storefront category publication/visibility eligibility.
success response shapes/statuses.
auth behavior on four Admin routes.
storefront route remains unauthenticated unless current global policy says otherwise.

Do not change route paths/methods.

HOST INVENTORY

Re-enumerate Host/Admin recursive production *.cs count at start/end.

Expected if full file evacuated:
56 -> 55

Do not hard-code PASS to 55 if analysis finds a legitimate retained non-business responsibility; any retention must be explicitly justified and cannot leave MegaMenu HTTP/business authority in Host.

DO NOT TOUCH

Do not migrate in W5:

CatalogFacetEndpoints.cs
CatalogCategoryEndpoints.cs
CatalogAttributeEndpoints.cs
StoreAppearanceSettings*
ProductWorkspace*
Merchandising*
StoreLanding*
StoreMenu*
Checkout*/Reservation*/HoldPolicy*
except minimal Program/CatalogEndpointModule registration changes required by this MegaMenu move.

DURABLE GUARDS

Add/strengthen guards proving:

Host/Admin/CatalogMegaMenuEndpoints.cs absent after complete evacuation.
Host no longer maps MegaMenu routes.
Catalog.Endpoints owns all 5 routes exactly once.
four Admin routes use ICatalogAdminAuthorizer + ISender.
Storefront route uses ISender and no Host dependency.
endpoints inject no ICatalogDirectory/IMegaMenuDirectory/CatalogDbContext.
Catalog.Endpoints -> Catalog.Infrastructure = ZERO.
Catalog -> Host = ZERO.
no PlatformHttpException in touched MegaMenu surface.
no InvalidOperationException expected-business/message-as-code flow in touched MegaMenu surface.
canonical Result/ApiResponseFactory.
capability-first shallow MegaMenu structure.
focused IMegaMenuDirectory/MegaMenuDirectory or equally narrow evidenced seam.
no duplicate authoritative request types.
validator classification exhaustive for all 5 requests.
path <-> namespace exact.
error descriptor ownership unique.
Host/Admin file-count delta correct.
W1-W4 preserved.
StoreAppearance still deferred.
schema/frontend unchanged.
W6/another Host folder not started.

TESTS

Add/update focused tests for:

Admin get unbound/bound config.
placement options.
valid upsert.
invalid tree placement.
remove absent idempotent.
remove with children rejection.
category missing.
storefront composed output.
locale default/fallback.
route ownership and auth.
canonical errorCode/status.

Focused validation:

Catalog.Contracts build
Catalog.Domain build if touched
Catalog.Application build
Catalog.Infrastructure build
Catalog.Endpoints build
Host build
Host.Tests build
MegaMenu focused behavior tests
W1/W2/W2-R1/W3/W4/W5 guards

Tests are evidence, not navigation.
No open-ended repair loop.
If bounded slice cannot be completed safely inside task timebox, return INCOMPLETE with exact remainder and STOP.

EVIDENCE

Create:
docs/evidence/TB-TMAR-HOST-ADMIN-AMC-001-W5/

Required:

analyze.md
disposition-map.md
megamenu-capability.md
cqrs.md
result-errors.md
validation.md
behavior-parity.md
closure.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-ADMIN-AMC-001-W5.task.md

RECOVERY SOT

Add hostAdminAmcW5 to docs/architecture/tmar-current-state.json while preserving W1/W2/W2-R1/W3/W4.

Record:

task/parent/parentCommit
activeHostFolder=Admin
Host Admin count before/after
MegaMenu endpoint ownership
five-route inventory
CQRS state
focused port/directory state
validator matrix
Result/error state
PlatformHttpException state
message classification state
Admin auth state
Storefront route state
Catalog->Host
Endpoints->Infrastructure
route/behavior parity
path/namespace
StoreAppearance deferred
schema/frontend
remaining Admin blockers
nextHostFolderStarted=false
workflowStop=USER_REVIEW_HOST_ADMIN_W5_CHECKPOINT

PASS CRITERIA

PASS only if ALL:

all live CatalogMegaMenuEndpoints responsibilities correctly dispositioned.
Host CatalogMegaMenuEndpoints.cs removable with no lost responsibility.
all 5 routes Catalog-owned exactly once.
all 5 routes MediatR-backed.
4 Admin routes authorize through ICatalogAdminAuthorizer.
Storefront route remains lawful and Host-free.
endpoint direct broad directory/DbContext access ZERO.
focused MegaMenu Application capability exists.
focused MegaMenu persistence seam exists or equally narrow lawful design evidenced.
expected failures use Result/stable Catalog codes.
no PlatformHttpException expected-business flow in touched capability.
no InvalidOperationException message-as-code expected flow.
ApiResponseFactory canonical.
error catalog/resources complete.
validator matrix exhaustive.
route/method/status/JSON/auth/locale behavior preserved.
MegaMenu tree/business rules remain authoritative in Domain/Application/Infrastructure, not endpoint validators.
Catalog->Host ZERO.
Endpoints->Infrastructure ZERO.
path<->namespace exact.
Host/Admin final count exact.
W1-W4 preserved.
StoreAppearance still deferred.
no schema/migration/frontend change.
focused builds/tests/guards PASS.
evidence/task/SoT persisted.
commit pushed.
HEAD == origin/main.
working tree clean.
W6 not started.
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
If pre-existing user work conflicts, return RECOVERY_CONFLICT.

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W5
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W4
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
W4-Preservation-State:
Host-Admin-File-Count-Before:
Host-Admin-File-Count-After:
MegaMenu-Host-File-State:
MegaMenu-Endpoint-Ownership-State:
MegaMenu-Route-Count:
MegaMenu-CQRS-State:
MegaMenu-Application-Structure-State:
MegaMenu-Persistence-Port-State:
MegaMenu-Validator-Coverage-State:
MegaMenu-Result-State:
MegaMenu-Error-Localization-State:
MegaMenu-Message-Classification-State:
MegaMenu-PlatformHttpException-State:
MegaMenu-InvalidOperationExpectedFlow-State:
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
Do not start W6.
Do not inspect/start another Host folder for execution.
Wait for Architect review.

END_TOOBA_TASK