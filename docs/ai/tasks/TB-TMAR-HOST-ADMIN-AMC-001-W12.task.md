PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W12
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W11
Parent-Commit: 90d59bea79f4d855f6938b898c3daacd001277be
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FOLDER_BY_FOLDER_ADMIN
Title: W12 — evacuate final Catalog Category-Change Admin slice; delete Host CatalogAttributeEndpoints entirely

ARCHITECT ACCEPTANCE

W11 is ARCHITECT-ACCEPTED.

Verified parent:
90d59bea79f4d855f6938b898c3daacd001277be

Accepted W11 state:

five Variant Axes + Variant Matrix routes moved out of Host.
Variants capability exists with CQRS/Result.
IProductVariantDirectory + ProductVariantDirectory.
Offer enrichment uses IVariantOfferLookup -> Offer.Contracts only.
Catalog.Endpoints inject no Offer gateway.
apply transaction and EventVariantsChanged preserved.
Host CatalogAttributeEndpoints.cs now contains ONLY:
POST /category-change-preview
PUT /primary-category
CategoryChangeRequest
CategoryChangePreviewRequest
MapCategoryChangeInvalid
ToError
Host actor binding for that remaining group.
Host/Admin count remains 53.
W12 not started.

ACTIVE HOST FOLDER

src/backend/Host/Tooba.Host/Admin/

Do not start another Host folder.
Do not start W13.

W12 BOUNDED FINAL CATALOGATTRIBUTE SLICE

Primary Host production file:

Admin/CatalogAttributeEndpoints.cs

Exact W12 routes:

POST /v1/admin/catalog/products/{productId:guid}/category-change-preview
PUT /v1/admin/catalog/products/{productId:guid}/primary-category

This is the FINAL live responsibility in CatalogAttributeEndpoints.cs.

If successfully migrated:

delete CatalogAttributeEndpoints.cs entirely.
remove Program MapCatalogAttributeEndpoints().
Host/Admin production file count expected 53 -> 52.

REQUIRED READ

Read:

AGENTS.md
.cursor/skills/tooba-architecture-analyze/SKILL.md
.cursor/skills/tooba-architecture-migrate/SKILL.md
.cursor/skills/tooba-architecture-certify/SKILL.md
docs/architecture/TMAR-HOST-EVACUATION-PROTOCOL.md
docs/architecture/TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md
docs/architecture/tmar-current-state.json
W7 Categories evidence
W8 Definitions evidence
W9 Schema evidence
W10/W10-R1 ProductValues evidence
W11 Variants evidence
current Host CatalogAttributeEndpoints.cs completely
CatalogDirectory category-change methods + exact helpers
CatalogCategoryTreeRules
CatalogCategorySchemaResolver.PreviewCategoryChange
ProductHistoryRules / ProductPublishRules
category/variant/product tests
Catalog error catalog/resources
current CatalogActorRequestBinding

MANDATORY ANALYZE FIRST

Before code changes create exact disposition map for:

PreviewCategoryChangeAsync
ReplacePrimaryCategoryAsync
MapCategoryChangeInvalid
ToError
CategoryChangeRequest
CategoryChangePreviewRequest
Program MapCatalogAttributeEndpoints()
PreviewCategoryChangeAsync in CatalogDirectory
PreviewCategoryChangeReportAsync
ReplaceProductPrimaryCategoryAsync
EnsureAssignableProductCategoryAsync
ResolveEffectiveBindingsAsync
ResolvePrimaryCategoryIdAsync
BuildCategoryPathAsync
GetAttributeDefinitionNamesAsync
GetAttributeOptionNamesAsync
FormatOrphanDisplay
ClearDefaultFlags
QueueProductHistory
ToPersianDigits
CatalogCategorySchemaResolver.PreviewCategoryChange
assignment level rule and stable error code:
CatalogCategoryTreeRules.ProductAssignableLevel
CatalogCategoryTreeRules.AssignmentLevelInvalidErrorCode
variant compatibility semantics
additional-category promotion semantics
orphan value cleanup
required-new-attribute reporting
safety-unpublish behavior
transaction semantics
actor/history behavior

No move before this map exists.

TARGET OWNERSHIP

Catalog owns:

category-change impact preview.
primary-category replacement/migration.
category-change write/read models.
focused persistence/use-case seam.
stable category-change errors.
both HTTP routes.

Host owns:

ZERO Catalog Attribute/category-change HTTP after W12.

TARGET APPLICATION STRUCTURE

Preferred:

Tooba.Catalog.Application/
CategoryChanges/
Commands/
ReplacePrimaryCategoryCommand.cs
ReplacePrimaryCategoryHandler.cs
Queries/
PreviewCategoryChangeQuery.cs
PreviewCategoryChangeHandler.cs
Models/
Ports/
ICategoryChangeDirectory.cs
Validators/

Equivalent shallow cohesive naming allowed.

Mandatory:

no mixed *Contracts.cs bundle.
no one-folder-per-request nesting.
exact path<->namespace.
unique authoritative MediatR request types.

FOCUSED PERSISTENCE PORT

Preferred:

ICategoryChangeDirectory
CategoryChangeDirectory

Extract only category-change behavior needed by these two routes.

Legacy ICatalogDirectory may retain thin wrappers only if real legacy/tests still need them.
No duplicate category-change authority.

Reuse lawful existing Catalog internal helpers where useful, but do not couple Application to Infrastructure.

CQRS / ENDPOINTS

Both routes must be Catalog-owned:

HTTP
-> ICatalogAdminAuthorizer
-> CatalogActorRequestBinding
-> ISender
-> query/command
-> handler
-> ICategoryChangeDirectory
-> Result
-> ApiResponseFactory

Endpoint destination:
Tooba.Catalog.Endpoints/Admin/CategoryChanges/...
or equivalent cohesive current path.

Endpoints must not inject:

ICatalogDirectory
ICategoryChangeDirectory
CatalogDbContext

ACTOR / HISTORY

Current Host group uses CatalogActorHttpBinding.
Replace with module-owned CatalogActorRequestBinding.

Preserve actor on history events:

EventCategoryChanged
EventUnpublished if safety-unpublish occurs

No Host actor filter copy.

RESULT / ERROR SEMANTICS

Current Host maps category-change IOE to:

exact assignment-level rule code if message equals ProductAssignableLevelRequiredMessageFa
otherwise catalog.category_change.invalid

W12 must eliminate message comparison entirely.

Use:

Result / Result<T>
CatalogErrorCodes
CatalogErrorCatalogContributor
resx + fa.resx
ApiResponseFactory

At minimum classify typed outcomes for:

product missing.
target category missing.
target category not assignable to product (must preserve canonical assignment-level code).
invalid hierarchy/assignability if current rule distinguishes it.
invalid category-change state.
preview invalid.
replacement invalid.
migration concurrency/state conflict if current logic exposes one.
any category/attribute/variant dependency failure currently expected.

The canonical existing assignment-level stable code MUST be preserved:
CatalogCategoryTreeRules.AssignmentLevelInvalidErrorCode

No:

PlatformHttpException expected flow.
InvalidOperationException expected flow.
ex.Message comparison.
Persian message as machine classification.

SUCCESS PAYLOAD TEXT

Success DTOs legitimately contain:

messageFa
variantImpactFa
readinessBlockers
localized labels/paths

These are user-facing success/report fields and MUST remain behavior-compatible.
Do not strip them merely because they are Persian.

BUSINESS SEMANTICS — PREVIEW

Preserve exactly:

locale default fa-IR.
product existence check.
target category existence.
Level-3 product-assignability rule.
effective target schema.
orphan attribute detection.
compatible preserved count.
newly required non-variant attributes.
localized orphan/preserved/required labels.
enum option display.
current and target category paths.
current/target variant-axis compatibility.
active variant impact count.
Additional membership promotion indicator.
other display-category remaining count.
readiness blockers.
published-product structural incompatibility warning.
messageFa composition.
report JSON shape.

BUSINESS SEMANTICS — APPLY / REPLACE

Preserve exactly:

mutation guard.
preview before replace.
DB transaction all-or-nothing.
if target category exists as Additional, promote atomically.
remove old Primary.
if same Primary, preserve no-op behavior.
add new Primary.
remove orphan product-attribute values only.
do NOT synthesize new required values.
recompute new schema.
remove invalid ProductVariantAxes.
if axes changed:
preserve variants.
clear default flags.
force active variants to Draft/update timestamp.
do NOT hard-delete variants.
if product is Published and structural incompatibility exists:
unpublish to Draft.
queue EventUnpublished.
queue EventCategoryChanged.
preserve history before/after summaries.
commit transaction.
rollback on failure.

TRANSACTION

ReplaceProductPrimaryCategory is currently transactional.

Preserve all-or-nothing across:

category membership promotion/change
orphan value removal
invalid axis removal
variant state downgrade/default clearing
safety unpublish
history events
SaveChanges

Do not weaken atomicity.

DOMAIN AUTHORITY

Preserve:

CatalogCategoryTreeRules product assignable level = 3.
CatalogCategorySchemaResolver.PreviewCategoryChange.
ProductPublishRules lifecycle semantics.
ProductHistoryRules summaries/event names.
variant compatibility semantics.

Do not move these rules into validators.

VALIDATION MATRIX

Classify both requests.

Expected baseline:

PreviewCategoryChangeQuery: VALIDATOR_REQUIRED for NewCategoryId non-empty and transport shape.
ReplacePrimaryCategoryCommand: VALIDATOR_REQUIRED for NewCategoryId non-empty.

Route ProductId Guid alone is not business validation.
Locale optional/defaulted.

No DB existence or assignability rule in FluentValidation.

BEHAVIOR PARITY

Preserve:

exact methods/paths.
Admin authorization.
module actor binding.
preview locale default fa-IR.
preview JSON shape.
replace JSON shape (CategoryChangeImpact as current behavior).
same-primary no-op behavior.
all category/attribute/variant safety semantics.
tenant isolation.
status behavior unless typed normalization explicitly documented.

HOST FILE DELETION

After successful migration:

DELETE src/backend/Host/Tooba.Host/Admin/CatalogAttributeEndpoints.cs
remove Host Program MapCatalogAttributeEndpoints()
no replacement Host shim.
no Host transport records from this file remain.
no Host category-change helpers remain.

Expected Host/Admin recursive production *.cs:
53 -> 52

If count differs, audit actual cause; do not force number blindly.

PROGRAM / ROUTE OWNERSHIP

Prove:

Host no longer maps CatalogAttributeEndpoints.
both category-change routes Catalog.Endpoints-owned exactly once.
no duplicate route ownership with old Categories capability or ProductWorkspace.

DO NOT TOUCH

Do not migrate:

Seller panel routes
ProductWorkspace*
StoreAppearance*
Merchandising*
StoreLanding*
StoreMenu*
checkout/reservation/hold
unrelated category CRUD

Do not start W13.

DURABLE GUARDS

Prove:

Host/Admin/CatalogAttributeEndpoints.cs absent.
Program no MapCatalogAttributeEndpoints().
both W12 routes Catalog-owned exactly once.
both routes use ICatalogAdminAuthorizer + ISender.
module CatalogActorRequestBinding used.
endpoint injects no directory/DbContext.
focused CategoryChanges capability exists.
focused ICategoryChangeDirectory/CategoryChangeDirectory or equally narrow seam.
canonical Result + ApiResponseFactory.
no PlatformHttpException on moved surface.
no InvalidOperationException expected flow/message classification.
no ex.Message == ProductAssignableLevelRequiredMessageFa.
canonical assignment-level stable code preserved.
category schema/tree/domain authority preserved.
replace transaction preserved.
history EventCategoryChanged/EventUnpublished preserved.
variants not hard-deleted during migration.
safety unpublish preserved.
Catalog->Host ZERO.
Endpoints->Infrastructure ZERO.
validator matrix exhaustive.
exact path<->namespace.
Host/Admin count expected 52.
W1–W11 preserved.
StoreAppearance deferred.
schema/frontend unchanged.
W13/next Host folder not started.

TESTS

Focused tests for:

preview product missing.
target category missing.
level != 3 rejected with canonical assignment-level code.
preview locale default.
orphan detection.
preserved/new-required counts.
variant compatibility/impact.
Additional promotion flag.
readiness blockers.
replace same-primary no-op.
Additional->Primary promotion.
orphan removal only.
invalid axes removal.
variant Draft downgrade / no hard delete.
default clearing.
Published product safety unpublish.
EventUnpublished.
EventCategoryChanged.
transaction rollback on failure.
route ownership/auth/actor binding.
Host file deletion / Program mapping removal.

Focused builds:

Catalog.Contracts
Catalog.Domain if touched
Catalog.Application
Catalog.Infrastructure
Catalog.Endpoints
Host
Host.Tests
W7-W12 guards + category-change tests

Tests are evidence, not navigation.
No open-ended repair loop.
If unsafe within timebox, return INCOMPLETE and STOP.

EVIDENCE

Create:
docs/evidence/TB-TMAR-HOST-ADMIN-AMC-001-W12/

Required:

analyze.md
disposition-map.md
category-change-capability.md
cqrs.md
actor-context.md
result-errors.md
transaction-history.md
behavior-parity.md
validation.md
host-file-deletion.md
closure.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-ADMIN-AMC-001-W12.task.md

RECOVERY SOT

Add hostAdminAmcW12 preserving all previous objects.

Record:

task/parent/parentCommit
activeHostFolder=Admin
Host file count before/after
CatalogAttributeEndpoints state=DELETED
Program mapping state=REMOVED
category-change Host route count before/after
endpoint ownership
CQRS
focused port/directory
validator matrix
Result/errors
assignment-level code preservation
actor context
transaction state
history state
safety-unpublish state
Catalog->Host
Endpoints->Infrastructure
route/behavior parity
path/namespace
StoreAppearance deferred
schema/frontend unchanged
remaining Admin blockers
nextHostFolderStarted=false
workflowStop=USER_REVIEW_HOST_ADMIN_W12_CHECKPOINT

PASS CRITERIA

PASS only if ALL:

two category-change routes evacuated.
CatalogAttributeEndpoints.cs deleted.
Host Program mapping removed.
Host/Admin count correctly reduced.
both routes Catalog-owned exactly once.
both MediatR-backed.
both ICatalogAdminAuthorizer-protected.
actor binding preserved.
focused CategoryChanges capability exists.
focused persistence seam exists.
expected failures use typed Result/stable codes.
no message classification.
assignment-level canonical code preserved.
preview report behavior preserved.
replace transaction semantics preserved.
history/unpublish/variant safety preserved.
Catalog->Host ZERO.
Endpoints->Infrastructure ZERO.
validator matrix exhaustive.
W1–W11 preserved.
StoreAppearance deferred.
no schema migration/frontend change.
builds/tests/guards PASS.
evidence/task/SoT persisted.
commit pushed.
HEAD==origin/main.
working tree clean.
W13 not started.
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
Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W12
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W11
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
W11-Preservation-State:
Host-Admin-File-Count-Before:
Host-Admin-File-Count-After:
CatalogAttribute-Host-File-State:
CatalogAttribute-Program-Mapping-State:
CategoryChange-Host-Route-Count-Before:
CategoryChange-Host-Route-Count-After:
CategoryChange-Endpoint-Ownership-State:
CategoryChange-Route-Count:
CategoryChange-CQRS-State:
CategoryChange-Application-Structure-State:
CategoryChange-Persistence-Port-State:
CategoryChange-Validator-Coverage-State:
CategoryChange-Result-State:
CategoryChange-Error-Localization-State:
CategoryChange-Message-Classification-State:
CategoryChange-PlatformHttpException-State:
CategoryChange-InvalidOperationExpectedFlow-State:
Assignment-Level-Code-State:
CategoryChange-Domain-Rule-State:
Actor-Context-State:
Replace-Transaction-State:
Product-History-State:
Safety-Unpublish-State:
Variant-Safety-State:
Admin-Authorization-State:
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

After result:

STOP completely.
Do not start W13.
Do not start another Host folder.
Wait for Architect review.

END_TOOBA_TASK