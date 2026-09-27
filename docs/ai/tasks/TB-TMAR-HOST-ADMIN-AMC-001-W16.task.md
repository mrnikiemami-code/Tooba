PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W16
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W15
Parent-Commit: 99fecfa66ebd1534d22b77cdaacc4b7aad93a2e0
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FOLDER_BY_FOLDER_ADMIN
Title: W16 — evacuate Product Workspace Publish Readiness GET to Catalog with focused ProductPublishReadiness query

ARCHITECT ACCEPTANCE

W15 is ARCHITECT-ACCEPTED.

Verified parent:
99fecfa66ebd1534d22b77cdaacc4b7aad93a2e0

Accepted W15 state:

Host history route 1 -> 0.
Catalog owns GET /v1/admin/products/{productId}/history exactly once.
ProductHistory/{Queries,Models,Ports,Validators} exists.
IProductHistoryReader + ProductHistoryReader focused seam exists.
paging/filter/order/Actor fallback preserved.
Host aggregate Activity/Audit shell preserved.
Host/Admin remains 52.
StoreAppearance deferred.
W16 not started.

ACTIVE HOST FOLDER

src/backend/Host/Tooba.Host/Admin/

Do not start another Host folder.
Do not start W17.

W16 BOUNDED SLICE

Primary Host files:

Admin/ProductWorkspaceEndpoints.cs
Admin/ProductWorkspaceComposer.cs
Admin/ProductWorkspaceModels.cs

W16 owns ONLY:

GET /v1/admin/products/{productId:guid}/publish/readiness

Query:

route productId
optional locale

Everything else remains in Host for later waves.

EXPECTED HOST STATE

ProductWorkspace files remain partial.
Host/Admin recursive production *.cs expected:
52 -> 52

Publish-readiness Host route count:
1 -> 0

WHY THIS SLICE

This GET is Catalog-owned and read-only. It depends only on Catalog product/category/attributes/variants/media/SEO readiness and does not need Offer/Pricing/Inventory/Party/Tax composition.

REQUIRED READ

Read:

AGENTS.md
.cursor/skills/tooba-architecture-analyze/SKILL.md
.cursor/skills/tooba-architecture-migrate/SKILL.md
.cursor/skills/tooba-architecture-certify/SKILL.md
docs/architecture/TMAR-HOST-EVACUATION-PROTOCOL.md
docs/architecture/TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md
docs/architecture/tmar-current-state.json
W10–W15 evidence
current ProductWorkspaceEndpoints.cs
current ProductWorkspaceComposer.cs readiness members
ProductWorkspaceModels readiness records
ICatalogDirectory.GetProductPublishReadinessAsync
CatalogDirectory.GetProductPublishReadinessAsync
ProductPublishRules
ProductSeoRules
ProductMedia readiness focused seam
ProductVariants readiness focused seam
ProductAttributes readiness focused seam
Category assignability logic
current Catalog error catalog/resources
publish-readiness tests/callers

MANDATORY ANALYZE FIRST

Before edit create disposition map for:

Endpoint:

MapGet("/{productId:guid}/publish/readiness", GetPublishReadinessAsync)
GetPublishReadinessAsync
ReadPermissions / X-Tooba-Workspace-Scope semantics
AdminPanelAccess.RequireAuthorizedAsync
ToError

Composer:

GetPublishReadinessAsync
MapPublishReadiness
EnsureProductExistsAsync if relevant
ProductPublishReadinessView
ProductPublishMissingRequirementView
aggregate GetAsync use of publication/readiness and MapPublishReadiness

Catalog:

ICatalogDirectory.GetProductPublishReadinessAsync
CatalogDirectory.GetProductPublishReadinessAsync
IsProductPrimaryCategoryAssignableAsync
ProductSeoRules.NormalizeLocale
GetProductSeoAsync / ProductSeo focused seam
GetProductAttributeReadinessAsync / ProductValues focused seam
GetProductVariantReadinessAsync / Variants focused seam
GetProductMediaReadinessAsync / ProductMedia focused seam
ProductPublishRules messages and tabs
ProductPublishReadiness / ProductPublishMissingRequirement

No code move before disposition exists.

TARGET OWNERSHIP

Catalog owns:

Admin publish-readiness GET.
Catalog-only readiness composition.
stable product-missing error.
focused query/use-case.

Host owns:

ZERO HTTP responsibility for publish-readiness route after W16.
aggregate ProductWorkspace.GetAsync may still compose/readiness internally until later wave.

APPLICATION STRUCTURE

Preferred:

Tooba.Catalog.Application/
ProductPublishing/
Queries/
GetProductPublishReadinessQuery.cs
GetProductPublishReadinessHandler.cs
Models/
Ports/
IProductPublishReadinessReader.cs
Validators/

Equivalent cohesive name accepted, e.g. ProductPublication.

Important:

W16 is READ ONLY.
do not migrate Publish/Unpublish/Archive/Restore.
do not create lifecycle commands.
no mixed *Contracts.cs.
exact path↔namespace.

FOCUSED READ SEAM

Preferred:

IProductPublishReadinessReader
ProductPublishReadinessReader

The reader may compose existing lawful focused Catalog ports/readers:

ProductSeo
ProductMedia
ProductVariants
ProductAttributes
Category readiness

Do NOT duplicate their business logic.

If direct composition in Infrastructure is cleaner, keep:
Application -> focused port
Infrastructure -> Catalog persistence/focused internal seams.

No Application -> Infrastructure.

Legacy ICatalogDirectory.GetProductPublishReadinessAsync may remain only for proven live callers such as Host aggregate/tests/publish command and should delegate one-way to focused readiness reader.

Do not duplicate readiness authority.

WORKSPACE SCOPE

Current Host:
X-Tooba-Workspace-Scope=view
=> CanView=true
=> GET publish/readiness allowed.

Preserve:

GET allowed in view scope.
no edit gate.
ICatalogAdminAuthorizer remains mandatory.

Do NOT call CatalogWorkspaceScope.AllowsCatalogEdit on this GET.

RESULT / ERROR

Current external missing-product behavior:
workspace.product.missing -> 404

W16:

typed Result.
preserve workspace.product.missing.
no PlatformHttpException expected flow.
no InvalidOperationException expected flow in new endpoint/reader.
no message parsing.

If legacy wrapper must throw for old internal callers, isolate it outside new HTTP seam.

READINESS SEMANTICS

Preserve exactly:

locale normalization/default behavior.
categoryReady = primary category assignable.
translationReady from localized product identity.
attributeReady from Product Attribute readiness.
variantReady from Product Variant readiness.
mediaReady from Product Media readiness.
seoReady from Product SEO readiness.

Missing requirement order must remain:

category
identity
attributes
variants
media
seo

Preserve exact fields for each missing requirement:

Code
MessageFa
WorkspaceTab

Preserve existing ProductPublishRules authority:

MessageCategoryIncompleteFa
MessageIdentityIncompleteFa
MessageAttributesIncompleteFa
MessageVariantsIncompleteFa
MessageMediaIncompleteFa
MessageSeoIncompleteFa
MessageReadyFa
SummarizeMissingFa

Preserve attribute missing-code detail suffix behavior.
Preserve media/seo own MessageFa fallback behavior.

isReady:
missing.Count == 0

messageFa:

ready -> ProductPublishRules.MessageReadyFa
not ready -> ProductPublishRules.SummarizeMissingFa(missing.Count)

Do not add Offer/price/stock checks. This readiness is Catalog-only.

RESPONSE SHAPE PARITY

Current ProductPublishReadinessView:

IsReady
CategoryReady
TranslationReady
AttributeReady
VariantReady
MediaReady
SeoReady
MissingRequirements
MessageFa

Each missing item:

Code
MessageFa
WorkspaceTab

Preserve exact JSON names/order semantics as serialized record shape.

VALIDATION MATRIX

GetProductPublishReadinessQuery:
NO_VALIDATOR_REQUIRED

Reason:

route Guid constrained.
locale optional/normalized by domain/use-case.

No FluentValidation for DB existence/readiness rules.

HOST PARTIAL RETENTION

After W16:

ProductWorkspaceEndpoints.cs:

remove publish/readiness mapping.
remove GetPublishReadinessAsync endpoint method.
retain lifecycle write routes and all other remaining workspace routes.

ProductWorkspaceComposer.cs:

remove public GetPublishReadinessAsync if no remaining caller.
MapPublishReadiness may remain ONLY if aggregate GetAsync still uses it.
ProductPublishReadinessView/ProductPublishMissingRequirementView may remain if ProductWorkspace aggregate still uses them.

Do not delete live aggregate readiness shape.
Do not keep route-only dead code.

HOST INVENTORY

Expected:

Host/Admin count 52 -> 52
publish-readiness route 1 -> 0
ProductWorkspace files retained partial

DURABLE GUARDS

Prove:

Host publish/readiness route absent.
Catalog owns exact route once.
endpoint uses ICatalogAdminAuthorizer + ISender.
no edit-scope gate on GET.
endpoint injects no Composer/directory/DbContext.
focused ProductPublishing readiness capability exists.
focused reader/seam exists.
Result + ApiResponseFactory canonical.
no PlatformHttpException on moved surface.
no expected InvalidOperationException/message parsing on moved seam.
workspace.product.missing preserved.
category readiness semantics preserved.
translation readiness semantics preserved.
Product Attribute readiness reused, not duplicated.
Product Variant readiness reused, not duplicated.
Product Media readiness reused, not duplicated.
Product SEO readiness reused, not duplicated.
missing order preserved.
ProductPublishRules authority preserved.
aggregate Workspace publication/readiness still compiles.
Catalog->Host ZERO.
Endpoints->Infrastructure ZERO.
validator matrix exact.
path↔namespace exact.
Host/Admin count 52.
W1–W15 preserved.
StoreAppearance deferred.
schema/frontend unchanged.
W17/next Host folder not started.

TESTS

Focused tests:

missing product -> workspace.product.missing 404.
locale null/blank/default.
category incomplete.
identity incomplete.
attributes incomplete + missing codes suffix.
variants incomplete.
media incomplete and MessageFa fallback.
seo incomplete and MessageFa fallback.
multiple missing requirement order.
ready state.
MessageReadyFa.
SummarizeMissingFa count.
view scope GET allowed.
exact JSON response shape.
route ownership exactly once.
aggregate ProductWorkspace publication/readiness still compiles.

Focused builds:

Catalog.Contracts
Catalog.Domain if touched
Catalog.Application
Catalog.Infrastructure
Catalog.Endpoints
Host
Host.Tests
W13–W16 guards + focused readiness tests

DB tests may skip only due unavailable Docker/Testcontainers and must be recorded distinctly.
Non-DB builds/guards must pass.

EVIDENCE

Create:
docs/evidence/TB-TMAR-HOST-ADMIN-AMC-001-W16/

Required:

analyze.md
disposition-map.md
publish-readiness-capability.md
cqrs.md
result-errors.md
readiness-parity.md
validation.md
behavior-parity.md
partial-host-retention.md
closure.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-ADMIN-AMC-001-W16.task.md

RECOVERY SOT

Add hostAdminAmcW16, preserving prior states.

Record:

task/parent/parentCommit
activeHostFolder=Admin
Host Admin count before/after
ProductWorkspace retained partial
publish-readiness Host route before/after
endpoint ownership
CQRS
focused reader
validator matrix
Result/error state
readiness dependency reuse
missing-order parity
workspace view-scope
aggregate readiness preservation
Catalog->Host
Endpoints->Infrastructure
StoreAppearance deferred
schema/frontend unchanged
remaining ProductWorkspace slices
nextHostFolderStarted=false
workflowStop=USER_REVIEW_HOST_ADMIN_W16_CHECKPOINT

PASS CRITERIA

PASS only if:

Host publish-readiness route 1 -> 0.
Catalog owns route exactly once.
canonical CQRS/Result/ApiResponseFactory.
Admin auth preserved.
view-scope GET behavior preserved.
focused ProductPublishing readiness seam.
no expected exception/message classification.
exact readiness semantics/order/messages preserved.
focused ProductMedia/ProductSeo/ProductVariants/ProductAttributes reused, not duplicated.
aggregate ProductWorkspace readiness remains intact.
Host/Admin count 52.
W1–W15 preserved.
StoreAppearance deferred.
no schema/frontend change.
focused builds/tests/guards PASS.
task/evidence/SoT persisted.
commit pushed.
HEAD == origin/main.
working tree clean.
W17 not started.
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
On collision return RECOVERY_CONFLICT.

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W16
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W15
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
W15-Preservation-State:
Host-Admin-File-Count-Before:
Host-Admin-File-Count-After:
ProductWorkspace-Endpoint-File-State:
ProductWorkspace-Composer-State:
ProductWorkspace-Models-State:
PublishReadiness-Host-Route-Count-Before:
PublishReadiness-Host-Route-Count-After:
PublishReadiness-Endpoint-Ownership-State:
PublishReadiness-Route-Count:
PublishReadiness-CQRS-State:
PublishReadiness-Application-Structure-State:
PublishReadiness-Read-Port-State:
PublishReadiness-Validator-Coverage-State:
PublishReadiness-Result-State:
PublishReadiness-Error-Localization-State:
PublishReadiness-Message-Classification-State:
PublishReadiness-PlatformHttpException-State:
PublishReadiness-InvalidOperationExpectedFlow-State:
Workspace-View-Scope-State:
Category-Readiness-State:
Translation-Readiness-State:
Attribute-Readiness-State:
Variant-Readiness-State:
Media-Readiness-State:
Seo-Readiness-State:
Missing-Requirement-Order-State:
ProductPublishRules-State:
Aggregate-Workspace-Readiness-State:
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
Remaining-ProductWorkspace-Slices:
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
Do not start W17.
Do not migrate publish/unpublish/archive/restore.
Do not start another Host folder.
Wait for Architect review.

END_TOOBA_TASK