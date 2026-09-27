PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W14
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W13
Parent-Commit: 06ce74d6853714d75ccc1b5395ccee44b87cef89
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FOLDER_BY_FOLDER_ADMIN
Title: W14 — evacuate Product Workspace SEO Admin slice to Catalog with focused ProductSeo capability

ARCHITECT ACCEPTANCE

W13 is ARCHITECT-ACCEPTED.

Verified parent:
06ce74d6853714d75ccc1b5395ccee44b87cef89

Accepted W13 state:

eight Product Workspace Media routes moved from Host to Catalog.Endpoints.
ProductMedia/{Commands,Queries,Models,Ports,Validators} exists.
IProductMediaDirectory + ProductMediaDirectory focused seam exists.
workspace.* media codes preserved.
X-Tooba-Workspace-Scope=view write deny preserved module-side.
primary uniqueness, reorder exact-set, detach-unassign semantics preserved.
ProductWorkspaceEndpoints/Composer/Models remain partial.
Host/Admin count remains 52.
W14 not started.

ACTIVE HOST FOLDER

src/backend/Host/Tooba.Host/Admin/

Do not start another Host folder.
Do not start W15.

W14 BOUNDED MEMBER-LEVEL SLICE

Primary Host files:

Admin/ProductWorkspaceEndpoints.cs
Admin/ProductWorkspaceComposer.cs
Admin/ProductWorkspaceModels.cs

W14 owns ONLY Product Workspace SEO:

Base:
/v1/admin/products/{productId:guid}

Exact routes:

GET /v1/admin/products/{productId:guid}/seo
PUT /v1/admin/products/{productId:guid}/seo
GET /v1/admin/products/{productId:guid}/seo/readiness

Everything else in ProductWorkspace remains for later waves.

EXPECTED HOST STATE

ProductWorkspace files remain partial.

Expected Host/Admin recursive production *.cs:
52 -> 52

Member-level:

SEO Host route count before: 3
after: 0

REQUIRED READ

Read current:

AGENTS.md
.cursor/skills/tooba-architecture-analyze/SKILL.md
.cursor/skills/tooba-architecture-migrate/SKILL.md
.cursor/skills/tooba-architecture-certify/SKILL.md
docs/architecture/TMAR-HOST-EVACUATION-PROTOCOL.md
docs/architecture/TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md
docs/architecture/tmar-current-state.json
W7–W13 evidence
complete current ProductWorkspaceEndpoints.cs
relevant ProductWorkspaceComposer SEO members
ProductWorkspaceModels SEO records
current ICatalogDirectory/CatalogDirectory SEO methods/helpers
ProductSeoRules
CatalogCategorySlugNormalizer
ProductHistoryRules SEO event
current Catalog error catalog/resources
all SEO tests/callers

MANDATORY ANALYZE FIRST

Before code changes create exact disposition map for:

Endpoints:

GetSeoAsync
PutSeoAsync
GetSeoReadinessAsync
route registrations
ReadPermissions usage
CatalogActorHttpBinding requirement

Composer:

GetSeoAsync
UpdateSeoAsync
GetSeoReadinessAsync
MapSeoDetail
any MapSeoReadiness helper
EnsureCatalogEdit / CanView checks relevant to SEO
PlatformHttpException mappings
message parsing:
exact workspace.catalog.stale
Contains("قبلاً استفاده")
Contains("نامعتبر")
product/Tenant message detection
generic workspace.product.seo.rejected

Models:

AdminProductSeoUpdateRequest
ProductSeoDetailView
ProductSeoReadinessView
any SEO model embedded in ProductWorkspaceView

Catalog:

GetProductSeoAsync
UpdateProductSeoAsync
GetProductSeoReadinessAsync
BuildProductSeoDetailAsync
ResolveProductNameForSeoAsync
ResolveLocalizedFieldForSeoAsync
UpsertProductLocalizedFieldForSeoAsync
ProductSeoUpdateInput
ProductSeoDetail
ProductSeoReadiness
ProductSeoRules.NormalizeLocale
ProductSeoRules.Evaluate
ProductSeoRules.BuildPublicPath
CatalogCategorySlugNormalizer.NormalizeSlug
CatalogCategorySlugNormalizer.SlugifyFromName
ProductHistoryRules.EventSeoChanged
SeoTitleSeam/fa-IR behavior
ExpectedUpdatedAt optimistic concurrency
slug uniqueness

No code move before disposition exists.

TARGET OWNERSHIP

Catalog owns:

Admin product SEO read.
Admin product SEO update.
Admin product SEO readiness.
SEO transport/read models.
focused SEO persistence/use-case seam.
stable SEO errors.
three HTTP routes.

Host owns:

ZERO responsibility for these three routes after W14.
ProductWorkspace retains unrelated slices only.

APPLICATION STRUCTURE

Preferred:

Tooba.Catalog.Application/
ProductSeo/
Commands/
UpdateProductSeoCommand.cs
UpdateProductSeoHandler.cs
Queries/
GetProductSeoQuery.cs
GetProductSeoHandler.cs
GetProductSeoReadinessQuery.cs
GetProductSeoReadinessHandler.cs
Models/
Ports/
IProductSeoDirectory.cs
Validators/

Equivalent cohesive shallow foldering is acceptable.

Mandatory:

no mixed *Contracts.cs bundle.
no one-folder-per-request nesting.
exact path↔namespace.
unique authoritative request types.
no Host DTO references from module code.

FOCUSED PERSISTENCE SEAM

Preferred:

IProductSeoDirectory
ProductSeoDirectory

Extract W14 SEO methods/helpers from broad CatalogDirectory.

Legacy ICatalogDirectory SEO wrappers may remain only for proven live callers/tests/bootstrap and must delegate one-way into focused ProductSeoDirectory.

Do not duplicate SEO business authority.

CQRS / ENDPOINT FLOW

All three:
HTTP
-> ICatalogAdminAuthorizer
-> workspace scope policy where required
-> CatalogActorRequestBinding for update/history
-> ISender
-> Query/Command
-> IProductSeoDirectory
-> Result
-> ApiResponseFactory

Endpoints must not inject:

ProductWorkspaceComposer
ICatalogDirectory
IProductSeoDirectory
CatalogDbContext

WORKSPACE SCOPE POLICY

Current Host behavior:
X-Tooba-Workspace-Scope=view

CanView = true
CanEditCatalog = false
GET SEO allowed
GET readiness allowed
PUT SEO denied with workspace.permission.denied 403

Preserve exactly.

W13 already introduced media-specific CatalogWorkspaceMediaScope.

For W14:

analyze whether a small generic Catalog.Endpoints CatalogWorkspaceScope should replace/generalize W13 media-specific helper.
generalization is allowed only if W13 behavior remains identical and guards updated.
otherwise add the smallest SEO-specific helper without Host dependency.

Do NOT copy ProductWorkspacePermissions into Catalog Application.

RESULT / ERROR SEMANTICS

Current external SEO codes to preserve unless strong evidence says otherwise:

workspace.permission.denied → 403
workspace.product.missing → 404
workspace.catalog.stale → 409
workspace.product.slug.duplicate → 409
workspace.product.slug.invalid → 400
workspace.product.seo.rejected → 400

W14 must eliminate message-based classification.

Use:

Result / Result<T>
SemanticError
CatalogErrorCodes
CatalogErrorCatalogContributor
CatalogErrors.resx
CatalogErrors.fa.resx
ApiResponseFactory

No:

PlatformHttpException expected flow on migrated SEO surface.
InvalidOperationException expected flow.
ex.Message equality/Contains as machine classification.
localized-message classification.

SEO BUSINESS AUTHORITY

Preserve:

ProductSeoRules.NormalizeLocale.
locale default = fa-IR when query/update transport omits/blank locale as current behavior permits.
ProductSeoRules.Evaluate readiness.
ProductSeoRules.BuildPublicPath.
localized product name fallback behavior.
localized seo_title / seo_description storage.
fa-IR SeoTitleSeam compatibility behavior.
slug from explicit input using CatalogCategorySlugNormalizer.NormalizeSlug.
slug auto-generation from localized product name via SlugifyFromName when Slug blank.
invalid/missing source name -> slug invalid.
global tenant Catalog product slug uniqueness.
ExpectedUpdatedAt optimistic concurrency.
ProductHistory EventSeoChanged.
returned detail after update.
UpdatedAt semantics.

Do not move SEO rules into FluentValidation.

ATOMICITY

Current update modifies:

localized seo_title
localized seo_description
product slug/SEO seams
UpdatedAt
SEO history
then performs one SaveChanges.

Preserve single logical atomic persistence operation.
Do not introduce partial persistence or multiple commits.

ACTOR / HISTORY

PUT SEO queues:
ProductHistoryRules.EventSeoChanged

Use module-owned CatalogActorRequestBinding before command execution so actor identity/display name is preserved.

Reads do not need actor binding unless existing module conventions require harmless binding; do not add it mechanically.

VALIDATION MATRIX

Classify all three requests exactly.

Expected baseline to verify:

GetProductSeoQuery:
NO_VALIDATOR_REQUIRED
route Guid + optional locale

GetProductSeoReadinessQuery:
NO_VALIDATOR_REQUIRED
route Guid + optional locale

UpdateProductSeoCommand:
VALIDATOR_REQUIRED for transport shape:

Locale required/normalizable if current contract requires it
ExpectedUpdatedAt meaningful/non-default if current transport contract requires it
string size/shape only if existing ProductSeoRules/transport constraints support it

Do NOT duplicate:

slug normalization
slug uniqueness
product existence
optimistic concurrency comparison
SEO readiness
inside FluentValidation.

BEHAVIOR / JSON PARITY

Preserve GET SEO shape:

ProductId
Locale
Slug
SeoTitle
SeoDescription
ProductName
TitleFallback
PublicPath
Readiness
UpdatedAt

Readiness shape:

HasValidSlug
HasSeoTitleOrFallback
HasSeoDescription
HasLocalizedIdentity
IsReady
MessageFa

PUT request fields:

Locale
Slug
SeoTitle
SeoDescription
ExpectedUpdatedAt

Preserve:

GET /seo 200
PUT /seo 200
GET /seo/readiness 200
existing error statuses/codes.

HOST PARTIAL RETENTION

After W14 ProductWorkspaceEndpoints.cs remains.

It MUST NOT contain:

3 SEO route registrations.
GetSeoAsync.
PutSeoAsync.
GetSeoReadinessAsync.

ProductWorkspaceComposer.cs remains, but:

remove public SEO methods if no remaining Host caller.
remove SEO-only mapping helpers if dead.
keep any SEO mapping/model logic genuinely needed by ProductWorkspaceView aggregate GetAsync.

ProductWorkspaceModels.cs:

remove AdminProductSeoUpdateRequest if no remaining Host consumer.
ProductSeoDetailView / ProductSeoReadinessView may remain ONLY if still consumed by ProductWorkspaceView or remaining Host composition.
do not remove live aggregate DTOs merely because SEO route moved.

No dead production code retained for stale guards.

HOST INVENTORY

Expected:

Host/Admin count 52 -> 52
SEO Host route count 3 -> 0
ProductWorkspace files retained partial

DURABLE GUARDS

Prove:

three SEO route mappings absent Host.
three SEO routes Catalog.Endpoints-owned exactly once.
all three use ICatalogAdminAuthorizer + ISender.
PUT preserves workspace view-scope write deny.
GETs remain allowed under view scope.
PUT uses CatalogActorRequestBinding and history actor preserved.
endpoints inject no Composer/directory/DbContext.
focused ProductSeo capability exists.
focused IProductSeoDirectory/ProductSeoDirectory exists or equivalent lawful narrow seam.
Result + ApiResponseFactory canonical.
no PlatformHttpException on moved SEO surface.
no expected IOE/message parsing on moved SEO surface.
stable workspace SEO codes preserved.
slug normalization/uniqueness behavior preserved.
optimistic concurrency preserved.
ProductSeoRules authority preserved.
EventSeoChanged preserved.
atomic persistence preserved.
Catalog→Host ZERO.
Endpoints→Infrastructure ZERO.
validator matrix exhaustive.
exact path↔namespace.
Host/Admin count 52.
W1–W13 preserved.
StoreAppearance deferred.
schema/frontend unchanged.
W15/next Host folder not started.

TESTS

Focused behavior tests:

GET existing/missing product.
locale default/normalization.
localized product-name fallback.
readiness calculation.
public path.
PUT valid explicit slug.
PUT blank slug auto-generates from product name.
invalid slug.
duplicate slug.
missing product.
stale ExpectedUpdatedAt.
localized seo title/description upsert/remove.
fa-IR SeoTitleSeam behavior.
EventSeoChanged actor/history.
workspace scope=view GET allowed.
workspace scope=view PUT denied 403.
response JSON/status/errorCode parity.
route ownership exactly once.

Focused validation:

Catalog.Contracts
Catalog.Domain if touched
Catalog.Application
Catalog.Infrastructure
Catalog.Endpoints
Host
Host.Tests
SEO focused tests
W7–W14 architecture guards

DB/Testcontainers tests may skip only due unavailable Docker and must be recorded distinctly.
Non-DB guards/builds must pass.

EVIDENCE

Create:
docs/evidence/TB-TMAR-HOST-ADMIN-AMC-001-W14/

Required:

analyze.md
disposition-map.md
product-seo-capability.md
cqrs.md
workspace-scope-policy.md
result-errors.md
atomicity-history.md
validation.md
behavior-parity.md
partial-host-retention.md
closure.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-ADMIN-AMC-001-W14.task.md

RECOVERY SOT

Add hostAdminAmcW14 preserving W1–W13.

Record:

task/parent/parentCommit
activeHostFolder=Admin
Host Admin count before/after
ProductWorkspace files retained partial
SEO Host route count before/after
endpoint ownership
CQRS state
focused ProductSeo port/directory
validator matrix
Result/error state
workspace scope state
slug authority state
optimistic concurrency state
atomicity/history state
Catalog→Host
Endpoints→Infrastructure
route/behavior parity
path/namespace
StoreAppearance deferred
schema/frontend unchanged
remaining ProductWorkspace slices
nextHostFolderStarted=false
workflowStop=USER_REVIEW_HOST_ADMIN_W14_CHECKPOINT

PASS CRITERIA

PASS only if ALL:

SEO Host route count 3 -> 0.
three routes Catalog-owned exactly once.
MediatR/Result/ApiResponseFactory canonical.
Admin auth preserved.
view-scope read/write semantics preserved.
focused ProductSeo capability and persistence seam.
no expected exception/message parsing flow.
stable existing workspace SEO error codes preserved.
slug/readiness/localization/concurrency semantics preserved.
EventSeoChanged actor preserved.
single logical persistence atomicity preserved.
no dead Host SEO endpoint/composer code.
remaining ProductWorkspace slices unchanged.
Host/Admin count 52.
W1–W13 preserved.
StoreAppearance deferred.
no schema/frontend change.
focused builds/tests/guards PASS.
evidence/task/SoT persisted.
commit pushed.
HEAD == origin/main.
working tree clean.
W15 not started.
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
Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W14
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W13
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
W13-Preservation-State:
Host-Admin-File-Count-Before:
Host-Admin-File-Count-After:
ProductWorkspace-Endpoint-File-State:
ProductWorkspace-Composer-State:
ProductWorkspace-Models-State:
Seo-Host-Route-Count-Before:
Seo-Host-Route-Count-After:
Seo-Endpoint-Ownership-State:
Seo-Route-Count:
Seo-CQRS-State:
Seo-Application-Structure-State:
Seo-Persistence-Port-State:
Seo-Validator-Coverage-State:
Seo-Result-State:
Seo-Error-Localization-State:
Seo-Message-Classification-State:
Seo-PlatformHttpException-State:
Seo-InvalidOperationExpectedFlow-State:
Workspace-Scope-Policy-State:
Slug-Authority-State:
Optimistic-Concurrency-State:
Seo-Atomicity-State:
Product-History-State:
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
Do not start W15.
Do not migrate remaining ProductWorkspace slices.
Do not start another Host folder.
Wait for Architect review.

END_TOOBA_TASK