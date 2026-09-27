PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W4
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W3
Parent-Commit: 662592be6afede7ab982b301f120210ac36da667
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FOLDER_BY_FOLDER_ADMIN
Title: W4 — evacuate Catalog Tag Admin slice to Catalog with capability CQRS/Result and zero Host business ownership

ARCHITECT ACCEPTANCE

W3 is ARCHITECT-ACCEPTED.

Verified parent:
662592be6afede7ab982b301f120210ac36da667

Accepted W3 state:

Host/Admin 58 -> 57.
Host UnitOfMeasureEndpoints.cs = ABSENT.
UoM five routes are Catalog-owned exactly once.
UoM list/get/create/update/deactivate are MediatR ISender-backed.
Catalog UoM uses Result + ApiResponseFactory.
Localization boundary = Localization.Contracts ILanguageLookup only.
HostUnitOfMeasureLanguageGate = REMOVED.
Catalog -> Host = ZERO.
Catalog -> Localization.Application = ZERO.
Catalog.Endpoints -> Catalog.Infrastructure = ZERO.
UoM Application structure = Units/{Commands,Queries,Models,Ports,Validators}.
UoM legacy mixed contracts/handlers = ELIMINATED.
StoreAppearance remains intentionally deferred.
schema/frontend unchanged.
W4 was not started by W3.

ACTIVE HOST FOLDER

src/backend/Host/Tooba.Host/Admin/

Do not start another Host folder.
Do not start W5.

W4 BOUNDED SLICE

Primary Host production file:

Admin/CatalogTagEndpoints.cs

Current surface contains 9 routes:

GET /v1/admin/catalog/tags/
POST /v1/admin/catalog/tags/
GET /v1/admin/catalog/tags/{tagId:guid}
GET /v1/admin/catalog/products/{productId:guid}/tags/
POST /v1/admin/catalog/products/{productId:guid}/tags/{tagId:guid}
DELETE /v1/admin/catalog/products/{productId:guid}/tags/{tagId:guid}
GET /v1/admin/catalog/categories/{categoryId:guid}/tags/
POST /v1/admin/catalog/categories/{categoryId:guid}/tags/{tagId:guid}
DELETE /v1/admin/catalog/categories/{categoryId:guid}/tags/{tagId:guid}

Current Host violations/debt include:

HTTP endpoint ownership in Host.
direct dependency on ICatalogDirectory from Host.
AdminPanelAccess + CatalogActorHttpBinding Host coupling.
expected failures caught from InvalidOperationException.
endpoint-level hard-coded Problem/error mapping.
transport records in Host.
business/use-case behavior concentrated in the broad CatalogDirectory implementation.
legacy Persian exception messages in CatalogDirectory drive endpoint behavior.
tag application capability is not yet structured as Commands/Queries/Models/Ports/Validators.

Current Catalog persistence behavior to preserve includes:

tag creation with localized names.
default display locale behavior.
generated unique code when Code is omitted.
duplicate explicit code rejection.
list order by Code, max existing behavior, search by Name or Code.
get-by-id nullable/missing behavior.
product tag assignment duplicate rejection.
category tag assignment duplicate rejection.
remove product/category assignment is idempotent when assignment is absent.
list product/category tags ordered by Code.
locale fallback semantics used by existing MapTagView/PreferLocaleName behavior.
Catalog mutation guard remains authoritative.

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
W1/W2/W2-R1/W3 evidence
Admin/CatalogTagEndpoints.cs completely
Host.Tests/CatalogTagFoundationTests.cs
CatalogDirectory tag members and all direct callers/tests
Catalog error codes/contributor/resx
Catalog.Endpoints current module pattern
current Catalog capability structures created by Quantity/UoM waves

MANDATORY ANALYZE FIRST

Before code changes, create a member-level disposition map for CatalogTagEndpoints.cs and every CatalogDirectory member required by these 9 routes.

At minimum classify:

MapCatalogTagEndpoints
ListTagsAsync endpoint
CreateTagAsync endpoint
GetTagAsync endpoint
ListProductTagsAsync endpoint
AssignProductTagAsync endpoint
RemoveProductTagAsync endpoint
ListCategoryTagsAsync endpoint
AssignCategoryTagAsync endpoint
RemoveCategoryTagAsync endpoint
CreateTagBody transport shape
CatalogActorHttpBinding dependency
AdminPanelAccess dependency
ICatalogDirectory tag methods
MapTagViewAsync / PreferLocaleNameAsync
SlugifyTagCode / ResolveUniqueTagCodeAsync
mutation guard usage
duplicate tag code outcome
duplicate product assignment outcome
duplicate category assignment outcome
missing tag/product/category outcomes actually observable in current runtime
locale fallback/search/order behavior
Program route mapping/DI implications
tests and current behavior contracts

No code move before this map exists.

TARGET OWNERSHIP

Catalog owns:

Tag application use cases.
tag read/write models.
tag persistence and mapping.
tag code generation/uniqueness policy at the appropriate Catalog layer.
product-tag and category-tag assignment use cases.
stable Catalog error codes.
Admin Tag HTTP endpoints.

Host owns only:

neutral platform/composition/security seams.
no tag business endpoint or tag business adapter.

Do not introduce a foreign module dependency in this wave.

CATALOG APPLICATION STRUCTURE

Capability-first, shallow-by-default.

Expected shape:

Tooba.Catalog.Application/
Tags/
Commands/
CreateTagCommand.cs
CreateTagHandler.cs
AssignProductTagCommand.cs
AssignProductTagHandler.cs
RemoveProductTagCommand.cs
RemoveProductTagHandler.cs
AssignCategoryTagCommand.cs
AssignCategoryTagHandler.cs
RemoveCategoryTagCommand.cs
RemoveCategoryTagHandler.cs
Queries/
ListTagsQuery.cs
ListTagsHandler.cs
GetTagQuery.cs
GetTagHandler.cs
ListProductTagsQuery.cs
ListProductTagsHandler.cs
ListCategoryTagsQuery.cs
ListCategoryTagsHandler.cs
Models/
...
Ports/
ITagDirectory.cs
Validators/
...

Equivalent cohesive naming is allowed if current Catalog conventions justify it.

Mandatory:

no generic/mixed *Contracts.cs bundle.
no root Application Tag files.
no one-folder-per-single-request tree.
exact path <-> namespace.
authoritative request type exactly once.
separate handlers where reasons to change differ.
do not export internal Tag DTOs to Catalog.Contracts merely because they are DTOs.

DIRECTORY / INFRASTRUCTURE

Do NOT simply keep new handlers calling broad ICatalogDirectory for this migrated slice if a focused capability port is feasible.

Preferred:

Application Tags uses focused ITagDirectory.
Catalog.Infrastructure implements ITagDirectory.
implementation may initially reuse the same CatalogDbContext and existing internal Catalog helpers, but Tag public use-case authority should be focused.
do not add another DbContext/schema.
no cross-module persistence.

If extracting tag methods from CatalogDirectory is safe in this bounded wave, do it.
If shared private localization/code helpers are needed by multiple Catalog capabilities, extract a narrowly named internal collaborator only when justified.

Do not perform unrelated CatalogDirectory decomposition.

CQRS

All 9 HTTP operations must be backed by authoritative MediatR requests.

Endpoint flow:
HTTP
-> ICatalogAdminAuthorizer
-> ISender
-> Command/Query
-> Handler
-> focused Tag port/domain/persistence
-> Result

No endpoint direct directory access.

RESULT / ERROR / LOCALIZATION

Expected failures must NOT use:

InvalidOperationException transport mapping.
PlatformHttpException.
exception message parsing.
hard-coded Persian/English response messages.
Results.Problem/Results.Json as a parallel expected-failure protocol.

Use:

Result / Result<T>
Catalog-owned stable semantic error codes
ApiResponseFactory
Catalog error catalog/resources

Audit exact externally visible current errorCode semantics before normalizing.

At minimum establish stable codes for real expected outcomes such as:

invalid tag input.
duplicate explicit tag code.
missing tag where current endpoint semantics expose missing.
product/tag assignment duplicate.
category/tag assignment duplicate.
referenced product/category/tag missing where current persistence currently throws and this is an expected business outcome.

Do NOT invent multiple codes without demonstrated distinct client semantics.
One descriptor owner per code.

Unknown/unexpected exceptions propagate to global boundary.

TRANSPORT / AUTH

Move the 9 routes to:
Tooba.Catalog.Endpoints/Admin/Tags/...

Use:

ICatalogAdminAuthorizer
ISender
ApiResponseFactory

CatalogActorHttpBinding:

inspect whether these routes actually need actor binding for Catalog mutation guard/current context.
preserve required actor/commerce semantics.
do not copy Host filter into Catalog.Endpoints.
if the module already has the lawful Catalog endpoint actor/security mechanism, use it.
if a minimal endpoint-owned binding adapter is required, place it at the lawful Catalog Endpoints boundary and keep it transport/security-only.
no Catalog -> Host dependency.

Preserve route methods, paths, auth behavior, success status codes, success JSON shapes, and locale defaults unless current behavior is demonstrably erroneous and change is explicitly documented as required for canonical Result semantics.

VALIDATION MATRIX

Classify every endpoint-reachable request exactly one of:

VALIDATOR_REQUIRED
NO_VALIDATOR_REQUIRED

Expected likely classification, to verify rather than blindly copy:

ListTagsQuery: validator only if untrusted locale/search constraints have explicit transport limits; otherwise NO_VALIDATOR_REQUIRED.
CreateTagCommand: VALIDATOR_REQUIRED for transport shape (localized names/code/slug/locale shape as applicable).
GetTagQuery: NO_VALIDATOR_REQUIRED if route Guid binding is sufficient.
ListProductTagsQuery: NO_VALIDATOR_REQUIRED.
AssignProductTagCommand: NO_VALIDATOR_REQUIRED unless command carries additional untrusted shape beyond route Guids.
RemoveProductTagCommand: NO_VALIDATOR_REQUIRED.
ListCategoryTagsQuery: NO_VALIDATOR_REQUIRED.
AssignCategoryTagCommand: NO_VALIDATOR_REQUIRED unless additional shape exists.
RemoveCategoryTagCommand: NO_VALIDATOR_REQUIRED.

FluentValidation is for untrusted transport shape only.
DB existence, uniqueness, duplicate assignment, mutation authorization, and business state stay in handler/domain/infrastructure Result logic.

BEHAVIOR PARITY

Preserve and test:

default locale behavior from Host endpoint.
NameFa and NameEn overlay into LocalizedNames on create.
explicit LocalizedNames values.
generated code when Code omitted.
explicit duplicate code rejection.
search semantics.
ordering.
Get missing behavior.
product assignment/list/remove behavior.
category assignment/list/remove behavior.
duplicate assignment errorCode/status semantics.
idempotent remove behavior.
actor/mutation-guard behavior.
tenant/store isolation already provided by Catalog persistence/context.

Do not change schema/migrations.

HOST INVENTORY

Re-enumerate Host/Admin production file count at start and end.

Expected if CatalogTagEndpoints.cs is fully evacuated:
57 -> 56

Do not hard-code PASS to 56 if analysis discovers a legitimate retained non-business responsibility; any retention must be explicit and may not leave Tag HTTP/business authority in Host.

Do NOT touch:

StoreAppearanceSettings*
CatalogAttributeEndpoints.cs
CatalogCategoryEndpoints.cs
CatalogFacetEndpoints.cs
CatalogMegaMenuEndpoints.cs
ProductWorkspace*
Merchandising*
StoreLanding*
StoreMenu*
checkout/reservation/hold settings
except minimal route-registration removal required by this Tag move.

DURABLE GUARDS

Add/strengthen guards proving:

Host/Admin/CatalogTagEndpoints.cs absent after complete evacuation.
Host no longer maps Host CatalogTag endpoints.
Catalog.Endpoints owns all 9 Tag routes exactly once.
Tag endpoints use ICatalogAdminAuthorizer + ISender.
expected failures use ApiResponseFactory/Result.
endpoints do not inject ICatalogDirectory/ITagDirectory/CatalogDbContext.
Catalog.Endpoints -> Catalog.Infrastructure = ZERO.
Catalog -> Host = ZERO.
no PlatformHttpException in touched Tag surface.
no InvalidOperationException message-as-code/catch mapping in touched Tag surface.
Tag Application capability-first shallow structure exists.
no Tag *Contracts.cs bundle.
no duplicate authoritative MediatR request types.
validator classification is exhaustive for all 9 route requests.
path <-> namespace exact.
tag error codes have one catalog descriptor owner and localized resources.
Host/Admin file-count delta matches disposition.
W1/W2/W2-R1/W3 state preserved.
StoreAppearance still deferred.
W5 and another Host folder not started.
schema/frontend unchanged.

TESTS

Update/reuse CatalogTagFoundationTests as behavior proof, but migrate assertions away from Persian exception-message semantics for expected business failures.

Add focused endpoint/use-case tests for:

create/list/get.
generated code.
duplicate explicit code.
product assign duplicate/remove/list.
category assign duplicate/remove/list.
missing tag response if currently route-visible.
canonical errorCode/status.
route ownership exactly once.

Focused validation:

Catalog.Contracts build
Catalog.Domain build if touched
Catalog.Application build
Catalog.Infrastructure build
Catalog.Endpoints build
Host build
Host.Tests build
Tag behavior tests
W1/W2/W2-R1/W3/W4 architecture guards

Tests are evidence, not navigation.
No open-ended test/repair loop.
If scope cannot be completed safely in the task timebox, return INCOMPLETE with exact remaining slice. Do not start W5.

EVIDENCE

Create:
docs/evidence/TB-TMAR-HOST-ADMIN-AMC-001-W4/

Required:

analyze.md
disposition-map.md
tag-capability.md
cqrs.md
result-errors.md
validation.md
behavior-parity.md
closure.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-ADMIN-AMC-001-W4.task.md

RECOVERY SOT

Update docs/architecture/tmar-current-state.json by adding hostAdminAmcW4 and preserving prior W1/W2/W2-R1/W3 state.

Record:

task/parent/parentCommit
activeHostFolder=Admin
Host Admin file count before/after
Tag endpoint ownership
9-route inventory
CQRS state
focused directory/port state
validator matrix
Result/error state
message classification state
PlatformHttpException state
Catalog->Host
Endpoints->Infrastructure
auth/actor binding state
path/namespace
StoreAppearance deferred
schema/frontend unchanged
remaining Admin blockers
nextHostFolderStarted=false
workflowStop=USER_REVIEW_HOST_ADMIN_W4_CHECKPOINT

SUCCESS CRITERIA

PASS only if ALL:

CatalogTagEndpoints live responsibilities fully dispositioned.
Host CatalogTagEndpoints.cs removable with no lost responsibility.
all 9 routes Catalog-owned exactly once.
all 9 HTTP operations MediatR-backed.
endpoint direct ICatalogDirectory/DbContext = ZERO.
Catalog->Host = ZERO.
Catalog.Endpoints->Catalog.Infrastructure = ZERO.
focused Tag application capability exists.
focused Tag persistence port/directory exists or an equally narrow lawful design is evidenced.
expected tag failures use Result/stable Catalog codes.
no expected Tag flow relies on InvalidOperationException or PlatformHttpException.
no exception message parsing.
ApiResponseFactory canonical.
error catalog/resources complete for introduced/reused tag codes.
transport DTO ownership correct.
validator classification exhaustive.
route/method/status/JSON/auth/locale parity preserved.
actor/mutation context preserved lawfully without Host dependency.
path<->namespace exact.
Host/Admin final inventory exact.
W1/W2/W2-R1/W3 preserved.
StoreAppearance still deferred.
no schema/migration/frontend change.
focused builds/tests/guards PASS.
evidence/task/SoT persisted.
commit pushed.
HEAD == origin/main.
working tree clean.
W5 not started.
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
On conflict with pre-existing user work, return RECOVERY_CONFLICT.

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W4
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W3
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
W3-Preservation-State:
Host-Admin-File-Count-Before:
Host-Admin-File-Count-After:
Tag-Host-File-State:
Tag-Endpoint-Ownership-State:
Tag-Route-Count:
Tag-CQRS-State:
Tag-Application-Structure-State:
Tag-Persistence-Port-State:
Tag-Validator-Coverage-State:
Tag-Result-State:
Tag-Error-Localization-State:
Tag-Message-Classification-State:
Tag-PlatformHttpException-State:
Tag-InvalidOperationExpectedFlow-State:
Actor-Binding-State:
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
Do not start W5.
Do not inspect/start another Host folder for execution.
Wait for Architect review.

END_TOOBA_TASK