PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W11
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W10-R1
Parent-Commit: a29066024b103c46891fbbcc6c5cce3cfe2b2dfd
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FOLDER_BY_FOLDER_ADMIN
Title: W11 — evacuate Catalog Variant Axes + Variant Matrix Admin slice to Catalog; retain category-change only

ARCHITECT ACCEPTANCE

W10-R1 is ARCHITECT-ACCEPTED.

Verified parent:
a29066024b103c46891fbbcc6c5cce3cfe2b2dfd

Accepted repaired state:

Host/Admin remains 53.
MapAttributeInvalid removed; zero runtime references.
stale W8/W9/W10 guard expectations repaired.
Seller SetProductAttributeRequest moved out of Admin Attribute file to Seller-owned HTTP transport.
CatalogAttributeEndpoints.cs now contains only variant/category-change responsibility.
W10 Product Attribute Catalog-owned routes remain unchanged.
StoreAppearance deferred.
schema/frontend unchanged.
W11 not started.

ACTIVE HOST FOLDER

src/backend/Host/Tooba.Host/Admin/

Do not start another Host folder.
Do not start W12.

W11 BOUNDED MEMBER-LEVEL SLICE

Primary mixed Host production file:

Admin/CatalogAttributeEndpoints.cs

W11 owns the VARIANT AXES + VARIANT MATRIX family:

Base:
/v1/admin/catalog/products/{productId:guid}

Exact W11 routes:

PUT /v1/admin/catalog/products/{productId:guid}/variant-axes
GET /v1/admin/catalog/products/{productId:guid}/variants/editor
POST /v1/admin/catalog/products/{productId:guid}/variants/preview
PUT /v1/admin/catalog/products/{productId:guid}/variants/apply
GET /v1/admin/catalog/products/{productId:guid}/variants/readiness

Retain for W12:
6. POST /v1/admin/catalog/products/{productId:guid}/category-change-preview
7. PUT /v1/admin/catalog/products/{productId:guid}/primary-category

EXPECTED HOST STATE

CatalogAttributeEndpoints.cs remains after W11 because category-change routes remain.

Expected Host/Admin production *.cs count:
53 -> 53

Member-level:

Variant Host route count before: 5
after: 0
Host Attribute file state after W11:
RETAINED_CATEGORY_CHANGE_ONLY

REQUIRED READ

Read:

AGENTS.md
.cursor/skills/tooba-architecture-analyze/SKILL.md
.cursor/skills/tooba-architecture-migrate/SKILL.md
.cursor/skills/tooba-architecture-certify/SKILL.md
docs/architecture/TMAR-HOST-EVACUATION-PROTOCOL.md
docs/architecture/TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md
docs/architecture/tmar-current-state.json
W8/W9/W10/W10-R1 evidence
current Host CatalogAttributeEndpoints.cs
current CatalogDirectory variant methods and all private helpers they require
current Offer.Contracts lookup gateway and existing Catalog/Offer boundary rules
ProductValues actor-binding pattern
all variant-focused tests/callers
Catalog error catalog/resources
Catalog.Domain variant/axis rules

MANDATORY ANALYZE FIRST

Before edits create exact member-level disposition for:

SetProductVariantAxesAsync
GetProductVariantEditorStateAsync
PreviewProductVariantsAsync
ApplyProductVariantsAsync
GetProductVariantReadinessAsync
EnrichVariantEditorWithOfferCountsAsync
SetProductVariantAxesRequest
ProductVariantSelectedAxisRequest
ProductVariantPreviewRequest
ProductVariantPatchRequest
ProductVariantApplyRequest
ICatalogDirectory:
SetProductVariantAxesAsync
GetProductVariantEditorStateAsync
PreviewProductVariantCombinationsAsync
ApplyProductVariantMatrixAsync
GetProductVariantReadinessAsync
BuildDesiredCombinationsAsync
ResolveVariantAxisLabelsAsync
MapVariantListItemsAsync
ResolvePrimaryCategoryIdAsync
ResolveEffectiveBindingsAsync
name/option lookup helpers
MaxVariantCombinations
default-variant logic
patch/status parsing
archive/deactivate behavior
ProductHistory variants-changed behavior
transaction semantics
Offer lookup/enrichment
CatalogActorHttpBinding requirement
ToError and retained category-change-only helpers

No code move before disposition exists.

TARGET OWNERSHIP

Catalog owns:

product variant-axis configuration.
variant editor/readiness.
variant combination preview.
apply variant matrix.
Catalog-side variant persistence/orchestration.
stable variant errors.
five HTTP routes.

Offer owns:

Offer data/count authority.

Catalog must NOT query Offer persistence.

CROSS-MODULE BOUNDARY

Current Host enriches variant editor/preview/apply with:
IOfferLookupGateway from Offer.Contracts.

Move this behavior lawfully.

Preferred:

Catalog Application defines a narrow port if enrichment is part of the use-case response.
Catalog Infrastructure adapter may depend on Offer.Contracts only.
OR
an Application handler may consume Offer.Contracts only if existing architecture rules explicitly permit direct Contracts dependency and this remains clean.

Mandatory:

Catalog -> Offer.Application = ZERO
Catalog -> Offer.Infrastructure = ZERO
Catalog -> Offer.Domain = ZERO
Catalog.Endpoints -> Offer.Contracts direct injection should be avoided; endpoints remain presentation-only.
no Catalog DB join to Offer data.

Preserve:

editor OfferCount.
preview ReferencedByOffers.
apply returned variant OfferCount.

CATALOG APPLICATION STRUCTURE

Preferred:

Tooba.Catalog.Application/
Variants/
Commands/
SetProductVariantAxesCommand.cs
SetProductVariantAxesHandler.cs
ApplyProductVariantMatrixCommand.cs
ApplyProductVariantMatrixHandler.cs
Queries/
GetProductVariantEditorStateQuery.cs
GetProductVariantEditorStateHandler.cs
PreviewProductVariantsQuery.cs
PreviewProductVariantsHandler.cs
GetProductVariantReadinessQuery.cs
GetProductVariantReadinessHandler.cs
Models/
Ports/
IProductVariantDirectory.cs
IVariantOfferLookup.cs (only if needed)
Validators/

Equivalent shallow cohesive structure allowed.

Mandatory:

no mixed *Contracts.cs bundle.
no one-folder-per-request tree.
exact path<->namespace.
authoritative request types unique.

FOCUSED PERSISTENCE PORT

Preferred:

IProductVariantDirectory
ProductVariantDirectory

Extract only variant-specific logic from CatalogDirectory.

Legacy ICatalogDirectory wrappers may remain only where real legacy/tests need them and must delegate one-way.

Do not duplicate variant authority between CatalogDirectory and ProductVariantDirectory.

ACTOR CONTEXT

Current Host group uses:
CatalogActorHttpBinding.BindAsync

Variant apply writes ProductHistory.

Use module-owned CatalogActorRequestBinding from W10 if lawful and sufficient.

Do not copy Host filter.

All five routes should preserve actor/current-context semantics.

CQRS / ENDPOINTS

All five routes:
HTTP
-> ICatalogAdminAuthorizer
-> CatalogActorRequestBinding if required
-> ISender
-> Command/Query
-> handler
-> focused variant ports
-> Result
-> ApiResponseFactory

Endpoints must not inject:

ICatalogDirectory
IProductVariantDirectory
CatalogDbContext
IOfferLookupGateway
Offer implementation

RESULT / ERROR SEMANTICS

Current Host uses generic:

catalog.variant_axes.invalid
catalog.variant.invalid
catalog.variant.preview.invalid
catalog.variant.apply.invalid
catalog.variant.readiness.invalid
with ex.Message.

W11 must replace expected failures with typed codes.

At minimum classify:

product missing
duplicate axis ids
definition missing
definition inactive
variant-axis capability disabled
axis not enabled in effective schema
selected axis malformed
selected option missing/inactive/mismatch
no effective variant axes where operation requires them
combination limit exceeded
variant patch target missing
invalid patch status
default variant missing
archived variant cannot be default
duplicate/fingerprint invalid conditions if present
invalid apply/preview input

No:

PlatformHttpException expected flow.
InvalidOperationException expected flow.
ex.Message classification.
hard-coded endpoint error text.

Preserve informational localized messageFa/warning fields that are part of successful DTO behavior; do not mistake success-payload copy for transport error localization.

STATUS PARSING

Current Host parses patch.Status with Enum.TryParse and throws on invalid value.

Move parsing to Application transport validation or a typed mapper.
No InvalidOperationException.
Preserve accepted enum strings/case-insensitivity.

DOMAIN / BUSINESS AUTHORITY

Preserve:

only definitions with IsVariantAxisAllowed.
active definitions/options.
effective schema IsVariantAxis requirement.
primary category drives schema.
MaxVariantCombinations = 200.
combination fingerprint semantics.
preview New/Unchanged/Deactivate behavior.
apply resurrect archived matching variant as Draft.
removed combinations archived, never hard-deleted.
patch semantics.
single-default invariant.
archived cannot be default.
catalog code seam/sort/status patch behavior.
readiness behavior.
ProductHistory EventVariantsChanged.

TRANSACTION

ApplyProductVariantMatrixAsync currently uses a DB transaction.

Preserve all-or-nothing behavior across:

axis replacement
variant create/reactivate/archive
patches
default selection
history
SaveChanges

Do not weaken atomicity.

VALIDATION MATRIX

Classify all five requests exactly.

Expected baseline to verify:

SetProductVariantAxesCommand: VALIDATOR_REQUIRED for list shape/duplicate primitive shape if appropriate.
GetProductVariantEditorStateQuery: NO_VALIDATOR_REQUIRED.
PreviewProductVariantsQuery: VALIDATOR_REQUIRED for request shape/selected axes/options primitive structure.
ApplyProductVariantMatrixCommand: VALIDATOR_REQUIRED for request shape + patch status parse shape.
GetProductVariantReadinessQuery: NO_VALIDATOR_REQUIRED.

Do not duplicate DB/domain rules in validators.

BEHAVIOR PARITY

Preserve:

locale default fa-IR.
editor shape and localized labels.
axes ordering.
selected options.
OfferCount enrichment.
preview combination counts/actions/message/warning.
ReferencedByOffers enrichment.
apply created/unchanged/deactivated counts.
apply OfferCount enrichment.
readiness shape.
status codes / JSON shapes.
all five Admin-authorized.
actor binding.
tenant isolation.

PARTIAL HOST FILE RULE

After W11, CatalogAttributeEndpoints.cs must contain only category-change responsibility.

It MUST NOT contain:

five variant route mappings
five variant endpoint methods
variant-only transport records
Offer variant enrichment helper
Offer.Contracts usings if no longer needed
SetProductVariantAxesRequest

It MAY contain:

category-change-preview
primary-category
CategoryChangeRequest
CategoryChangePreviewRequest
MapCategoryChangeInvalid
ToError only if still needed by category-change retained routes
CatalogActorHttpBinding for retained category-change group

Program may still map MapCatalogAttributeEndpoints() until W12.

HOST INVENTORY

Expected:
53 -> 53

Record:

Variant route count Host before 5
after 0
Attribute file = RETAINED_CATEGORY_CHANGE_ONLY

DO NOT TOUCH

Do not migrate:

category-change-preview
primary-category
Seller endpoints
ProductWorkspace*
StoreAppearance*
Merchandising*
StoreLanding*
StoreMenu*
checkout/reservation/hold

Do not start W12.

DURABLE GUARDS

Prove:

five variant routes absent from Host mapping.
five variant routes Catalog-owned exactly once.
all five use ICatalogAdminAuthorizer + ISender.
module actor binding preserved.
endpoints inject no directory/DbContext/Offer gateway.
focused Variants capability exists.
focused IProductVariantDirectory/ProductVariantDirectory exists or equivalent narrow seam.
Offer dependency Contracts-only behind lawful boundary.
editor/preview/apply Offer enrichment preserved.
Result + ApiResponseFactory canonical.
no PlatformHttpException on moved variant surface.
no InvalidOperationException expected flow/message-as-code.
no ex.Message parsing.
transaction preserved.
ProductHistory preserved.
MaxVariantCombinations preserved.
Catalog->Host ZERO.
Endpoints->Infrastructure ZERO.
Catalog->Offer.Application/Infrastructure/Domain ZERO.
validator matrix exhaustive.
exact path<->namespace.
retained category-change routes untouched.
Host/Admin count 53.
W1–W10-R1 preserved.
StoreAppearance deferred.
schema/frontend unchanged.
W12 not started.

TESTS

Focused tests for:

axes success.
duplicate axes.
missing/inactive definition.
axis capability disabled.
axis not in effective schema.
editor product missing.
editor locale/default labels.
OfferCount.
preview valid.
preview invalid axis/option.
preview cap behavior.
ReferencedByOffers.
apply create/reactivate/archive.
patch target missing.
invalid patch status.
default missing.
archived default rejection.
single default.
apply transaction rollback.
EventVariantsChanged.
readiness.
route ownership/auth/actor binding.
category-change retained routes unchanged.

Focused builds:

Catalog.Contracts
Catalog.Domain if touched
Catalog.Application
Catalog.Infrastructure
Catalog.Endpoints
Offer.Contracts
Host
Host.Tests
W8-W11 guards + focused variant tests

Tests are evidence, not navigation.
No open-ended loop.
If scope cannot safely finish in timebox, return INCOMPLETE and STOP.

EVIDENCE

Create:
docs/evidence/TB-TMAR-HOST-ADMIN-AMC-001-W11/

Required:

analyze.md
disposition-map.md
variant-capability.md
cqrs.md
offer-boundary.md
actor-context.md
result-errors.md
transaction-history.md
validation.md
behavior-parity.md
partial-host-retention.md
closure.md

Persist:
docs/ai/tasks/TB-TMAR-HOST-ADMIN-AMC-001-W11.task.md

RECOVERY SOT

Add hostAdminAmcW11 preserving all prior states.

Record:

task/parent/parentCommit
Host Admin count before/after
Attribute file retained state
variant Host route count before/after
endpoint ownership
CQRS
focused port/directory
validator matrix
Result/errors
Offer boundary
actor context
transaction/history
retained category-change scope
Catalog->Host
Endpoints->Infrastructure
Catalog->Offer forbidden boundaries
route/behavior parity
path/namespace
StoreAppearance deferred
schema/frontend unchanged
nextHostFolderStarted=false
workflowStop=USER_REVIEW_HOST_ADMIN_W11_CHECKPOINT

PASS CRITERIA

PASS only if all W11 five routes are fully evacuated, canonical, behavior-preserving, Offer-boundary-safe, transactional semantics preserved, retained category-change untouched, Host count remains 53, evidence/SoT/guards pass, commit pushed, HEAD==origin/main, working tree clean, and W12 not started.

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W11
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W10-R1
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
W10R1-Preservation-State:
Host-Admin-File-Count-Before:
Host-Admin-File-Count-After:
Attribute-Host-File-State:
Variant-Host-Route-Count-Before:
Variant-Host-Route-Count-After:
Variant-Endpoint-Ownership-State:
Variant-Route-Count:
Variant-CQRS-State:
Variant-Application-Structure-State:
Variant-Persistence-Port-State:
Variant-Validator-Coverage-State:
Variant-Result-State:
Variant-Error-Localization-State:
Variant-Message-Classification-State:
Variant-PlatformHttpException-State:
Variant-InvalidOperationExpectedFlow-State:
Variant-Domain-Rule-State:
Offer-Boundary-State:
Offer-Enrichment-State:
Actor-Context-State:
Apply-Transaction-State:
Product-History-State:
Admin-Authorization-State:
Endpoints-To-Infrastructure-State:
Catalog-To-Host-State:
Catalog-To-Offer-Forbidden-Boundary-State:
Route-Parity-State:
Behavior-Parity-State:
Retained-Attribute-Host-Scope:
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

STOP.
Do not start W12.
Do not migrate category-change routes.
Wait for Architect review.

END_TOOBA_TASK