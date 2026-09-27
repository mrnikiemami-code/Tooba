PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W10
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W9
Parent-Commit: 196d4731552522fb04c3b9b2bec8c51dc5a5501b
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FOLDER_BY_FOLDER_ADMIN
Title: W10 — evacuate Catalog Product Attribute Editor/Readiness slice to Catalog; retain variant/category-change members

ARCHITECT ACCEPTANCE

W9 is ARCHITECT-ACCEPTED.

Verified parent:
196d4731552522fb04c3b9b2bec8c51dc5a5501b

Accepted W9 state:

Host/Admin remains 53 -> 53.
five Category Attribute-Schema Host routes removed.
five schema routes Catalog-owned exactly once.
Attributes/Schema capability created.
ICategoryAttributeSchemaDirectory + CategoryAttributeSchemaDirectory focused seam.
all five routes MediatR/ISender + ICatalogAdminAuthorizer.
canonical Result + ApiResponseFactory + typed schema error codes.
moved schema surface has zero PlatformHttpException / expected IOE message-as-code.
CatalogCategoryAttributeAssignmentRules authority preserved.
CatalogAttributeEndpoints.cs now retains product attributes, variants, category-change only.
W10 not started.

ACTIVE HOST FOLDER

src/backend/Host/Tooba.Host/Admin/

Do not start another Host folder.
Do not start W11.

W10 BOUNDED MEMBER-LEVEL SLICE

Primary mixed Host production file:

Admin/CatalogAttributeEndpoints.cs

W10 owns ONLY the PRODUCT ATTRIBUTE EDITOR / READINESS slice:

Base group:
/v1/admin/catalog/products/{productId:guid}

Exact W10 routes:

GET /v1/admin/catalog/products/{productId:guid}/attributes
PUT /v1/admin/catalog/products/{productId:guid}/attributes
GET /v1/admin/catalog/products/{productId:guid}/attributes/readiness
PUT /v1/admin/catalog/products/{productId:guid}/attributes/{definitionId:guid}

Everything else remains for later waves:

PUT /variant-axes
GET /variants/editor
POST /variants/preview
PUT /variants/apply
GET /variants/readiness
POST /category-change-preview
PUT /primary-category

EXPECTED HOST FILE COUNT

CatalogAttributeEndpoints.cs remains after W10.

Expected Host/Admin file count:
53 -> 53

Member-level metrics:

W10 product-attribute route mappings in Host before: 4
after: 0

Attribute host file state after W10:
RETAINED_PARTIAL_VARIANT_CATEGORY_CHANGE_ONLY

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
W1–W9 evidence
current CatalogAttributeEndpoints.cs completely
current CatalogDirectory product-attribute methods/helpers
Attributes/Definitions W8 capability
Attributes/Schema W9 capability
Catalog.Domain canonicalization/validation rules
all product-attribute tests/callers
Catalog error catalog/resources

MANDATORY ANALYZE FIRST

Create a member-level disposition map before any code change.

Classify:

GetProductAttributeEditorStateAsync
SetProductAttributesAsync
GetProductAttributeReadinessAsync
SetProductAttributeAsync
SetProductAttributeRequest
ProductAttributeValueRequest
SetProductAttributesRequest
CatalogActorHttpBinding use on products group
ToError / MapAttributeInvalid and whether still required by retained variant Host members
ICatalogDirectory methods:
GetProductAttributeEditorStateAsync
SetProductAttributesAsync
GetProductAttributeReadinessAsync
SetProductAttributeAsync
ApplyProductAttributeValueAsync
EnsureDefinitionAllowedForProductSchemaAsync
EnsureNotEffectiveVariantAxisOnProductAsync
ResolvePrimaryCategoryIdAsync
BuildCategoryPathAsync
GetAttributeDefinitionNamesAsync
GetAttributeOptionNamesAsync
FormatAttributeDisplay
BuildReadiness
CatalogAttributeCanonicalizer
CatalogAttributeCanonicalizer.EnforceValidationBounds
product existence semantics
definition existence/active semantics
enum option ownership/active semantics
effective schema eligibility semantics
variant-axis exclusion semantics
bulk transaction semantics
product history semantics
locale defaulting
actor-binding/mutation-context semantics

No code move before this map exists.

TARGET OWNERSHIP

Catalog owns:

product attribute editor state.
bulk attribute set.
single attribute set.
attribute readiness.
focused product-attribute persistence/use-case seam.
stable product-attribute error codes.
the four HTTP routes.

Host owns:

no business responsibility for these four migrated routes.
retained variant/category-change routes only until later waves.

CATALOG APPLICATION STRUCTURE

Preferred:

Tooba.Catalog.Application/
Attributes/
ProductValues/
Commands/
SetProductAttributesCommand.cs
SetProductAttributesHandler.cs
SetProductAttributeCommand.cs
SetProductAttributeHandler.cs
Queries/
GetProductAttributeEditorStateQuery.cs
GetProductAttributeEditorStateHandler.cs
GetProductAttributeReadinessQuery.cs
GetProductAttributeReadinessHandler.cs
Models/
...
Ports/
IProductAttributeDirectory.cs
Validators/
...

Equivalent cohesive shallow naming is allowed.

Mandatory:

no mixed *Contracts.cs bundle.
no one-folder-per-request.
exact path<->namespace.
unique authoritative requests.
internal DTOs stay Application-owned unless a real external consumer exists.

FOCUSED PERSISTENCE PORT

Preferred:

IProductAttributeDirectory
ProductAttributeDirectory

Extract only W10 methods/helpers required for product attribute editor/readiness/write.

Do not keep migrated handlers calling broad ICatalogDirectory if focused extraction is feasible.

Legacy ICatalogDirectory wrappers may remain thin one-way wrappers only if required by CatalogDemo/tests/retained callers.

Avoid duplicating shared schema-resolution logic if lawful internal Catalog collaborators from W9 can be reused.

CQRS / ENDPOINTS

All four routes:
HTTP
-> ICatalogAdminAuthorizer
-> ISender
-> Command/Query
-> Handler
-> IProductAttributeDirectory
-> Result

Endpoint destination:
Tooba.Catalog.Endpoints/Admin/Attributes/ProductValues/...
or equivalent cohesive current structure.

Endpoints must not inject:

ICatalogDirectory
IProductAttributeDirectory
CatalogDbContext

ACTOR BINDING / CURRENT CONTEXT

Current Host product group uses:
products.AddEndpointFilter(CatalogActorHttpBinding.BindAsync)

W10 must analyze whether the four moved product-attribute routes require actor-binding for mutation guard/current actor semantics.

Rules:

do not copy Host filter into module endpoints.
if a lawful Catalog endpoint actor binding already exists, use it.
if minimal module-owned transport/security binding is required, place it at Catalog.Endpoints boundary.
reads must preserve current actor/tenant behavior.
writes must preserve mutation authorization/context.
Catalog -> Host remains ZERO.

RESULT / ERROR SEMANTICS

Current Host collapses moved failures into:
catalog.attribute.invalid 400 + ex.Message
plus PlatformHttpException.

W10 must canonicalize moved surface.

Use:

Result / Result<T>
CatalogErrorCodes
CatalogErrorCatalogContributor
CatalogErrors.resx/.fa.resx
ApiResponseFactory

At minimum classify typed outcomes for:

product missing.
definition missing.
definition inactive.
definition not allowed by effective schema.
effective variant-axis definition cannot be edited as normal product attribute.
enum option required.
enum option not belonging to definition.
enum option inactive.
canonicalization invalid.
validation bounds invalid.
malformed bulk input if transport shape requires it.

No:

PlatformHttpException expected flow.
InvalidOperationException expected flow.
ex.Message classification.
hard-coded user-facing endpoint error text.

DOMAIN / APPLICATION AUTHORITY

Preserve:

CatalogAttributeCanonicalizer authority.
validation bounds.
schema eligibility.
variant-axis exclusion.
enum option ownership/active rules.
readiness semantics.
product history event for bulk update.
transaction semantics for bulk set.

If canonicalizer/domain methods currently throw expected InvalidOperationException:

adapt to typed Result-compatible outcome on W10 surface.
do not parse messages.
do not move business rules to FluentValidation.

VALIDATION MATRIX

Classify all four requests exactly:

VALIDATOR_REQUIRED
or
NO_VALIDATOR_REQUIRED

Expected baseline to verify:

GetProductAttributeEditorStateQuery: NO_VALIDATOR_REQUIRED
SetProductAttributesCommand: VALIDATOR_REQUIRED for body/list shape
GetProductAttributeReadinessQuery: NO_VALIDATOR_REQUIRED
SetProductAttributeCommand: VALIDATOR_REQUIRED for raw transport shape only if needed

Do not duplicate:

product/definition existence
schema eligibility
enum option ownership
business canonicalization
validation bounds
inside FluentValidation.

BEHAVIOR PARITY

Preserve:

GET attributes locale default = fa-IR.
editor JSON shape.
category path.
effective schema ordering.
localized definition/option names.
current value formatting.
readiness embedded in editor.
bulk set transaction semantics.
bulk update history event.
bulk response = refreshed editor state using requested/default locale.
readiness behavior when no primary category.
single attribute upsert semantics.
enum handling.
active definition/option constraints.
route methods/paths.
Admin auth.
actor/context semantics.
tenant isolation.

BULK TRANSACTION

Current bulk SetProductAttributesAsync starts a DB transaction and applies all values before commit.

Preserve all-or-nothing behavior.
No partial writes.

Do not introduce multiple SaveChanges/commits that weaken atomicity.

PARTIAL HOST FILE RULE

After W10 CatalogAttributeEndpoints.cs remains.

It MUST NOT contain:

four W10 route mappings
four W10 endpoint methods
product-attribute-only transport records with no retained consumer

It MAY contain only:

variant-axis route
variant editor/preview/apply/readiness
category-change-preview
primary-category replacement
helpers/transport records genuinely required by retained routes

Program still maps MapCatalogAttributeEndpoints() while retained routes remain.

HOST INVENTORY

Expected:
Host/Admin count 53 -> 53

Record:

Product Attribute Host route count before = 4
after = 0
CatalogAttributeEndpoints.cs = RETAINED_PARTIAL_VARIANT_CATEGORY_CHANGE_ONLY

DO NOT TOUCH

Do not migrate in W10:

SetProductVariantAxesAsync
GetProductVariantEditorStateAsync
PreviewProductVariantsAsync
ApplyProductVariantsAsync
GetProductVariantReadinessAsync
PreviewCategoryChangeAsync
ReplacePrimaryCategoryAsync
Offer enrichment
ProductWorkspace*
StoreAppearance*
Merchandising*
StoreLanding*
StoreMenu*
checkout/reservation/hold

Do not start W11.

DURABLE GUARDS

Prove:

four W10 product-attribute routes absent from Host mapping.
four routes Catalog.Endpoints-owned exactly once.
all four use ICatalogAdminAuthorizer + ISender.
endpoints inject no directory/DbContext.
lawful actor/context binding preserved without Host dependency.
focused ProductValues capability exists.
focused IProductAttributeDirectory/ProductAttributeDirectory exists or equally narrow lawful design evidenced.
Result + ApiResponseFactory canonical.
no PlatformHttpException on moved surface.
no InvalidOperationException expected-business/message-as-code on moved surface.
CatalogAttributeCanonicalizer remains authoritative.
bulk transaction all-or-nothing preserved.
product history behavior preserved.
Catalog -> Host ZERO.
Endpoints -> Infrastructure ZERO.
validator matrix exhaustive.
unique request types.
exact path<->namespace.
retained variant/category-change routes untouched.
Host/Admin count remains 53.
W1–W9 preserved.
StoreAppearance deferred.
no schema/frontend change.
W11/next Host folder not started.

TESTS

Add/update focused tests for:

editor state product missing.
editor locale default.
effective schema ordering.
enum options/localized display.
readiness no-primary-category.
readiness missing required fields.
single set success.
definition missing/inactive.
definition not in effective schema.
effective variant-axis exclusion.
enum option required/wrong/inactive.
bounds/canonicalization invalid.
bulk all-or-nothing transaction.
bulk refreshed editor response.
product history event unchanged.
route ownership/auth/actor binding.
canonical errorCode/status.
retained variant/category-change routes still exactly once.

Focused validation:

Catalog.Contracts build
Catalog.Domain build if touched
Catalog.Application build
Catalog.Infrastructure build
Catalog.Endpoints build
Host build
Host.Tests build
Product Attribute focused tests
W1–W10 guards

Tests are evidence, not navigation.
No open-ended repair loop.

EVIDENCE

Create:
docs/evidence/TB-TMAR-HOST-ADMIN-AMC-001-W10/

Required:

analyze.md
disposition-map.md
product-attribute-capability.md
cqrs.md
actor-context.md
result-errors.md
transaction-history.md
validation.md
behavior-parity.md
partial-host-retention.md
closure.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-ADMIN-AMC-001-W10.task.md

RECOVERY SOT

Add hostAdminAmcW10 preserving W1–W9.

Record:

task/parent/parentCommit
activeHostFolder=Admin
Host file count before/after
Attribute host file retained state
product attribute Host route count before/after
endpoint ownership
CQRS state
focused product-attribute port/directory
validator matrix
Result/error state
actor/context state
bulk transaction state
product history state
Catalog->Host
Endpoints->Infrastructure
retained variant/category-change scope
route/behavior parity
path/namespace
StoreAppearance deferred
schema/frontend unchanged
remaining Admin blockers
nextHostFolderStarted=false
workflowStop=USER_REVIEW_HOST_ADMIN_W10_CHECKPOINT

PASS CRITERIA

PASS only if ALL:

four product-attribute routes Host-owned 4 -> 0.
four routes Catalog-owned exactly once.
all four MediatR-backed.
all four use ICatalogAdminAuthorizer.
actor/current-context semantics preserved lawfully.
endpoint directory/DbContext injection ZERO.
focused ProductValues capability exists.
focused persistence seam exists or equally lawful narrow design evidenced.
expected failures use Result/stable Catalog codes.
no PlatformHttpException expected flow.
no InvalidOperationException message-as-code expected flow.
no message parsing.
ApiResponseFactory canonical.
canonicalizer/schema/enum business rules preserved.
bulk transaction remains atomic.
history behavior preserved.
validator matrix exhaustive.
response status/JSON/locale/auth parity preserved.
retained variant/category-change Host routes untouched.
CatalogAttributeEndpoints.cs retained only for variant/category-change members.
Host/Admin count 53.
W1–W9 preserved.
StoreAppearance deferred.
no schema migration/frontend change.
focused builds/tests/guards PASS.
evidence/task/SoT persisted.
commit pushed.
HEAD==origin/main.
working tree clean.
W11 not started.
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
Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W10
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W9
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
W9-Preservation-State:
Host-Admin-File-Count-Before:
Host-Admin-File-Count-After:
Attribute-Host-File-State:
ProductAttribute-Host-Route-Count-Before:
ProductAttribute-Host-Route-Count-After:
ProductAttribute-Endpoint-Ownership-State:
ProductAttribute-Route-Count:
ProductAttribute-CQRS-State:
ProductAttribute-Application-Structure-State:
ProductAttribute-Persistence-Port-State:
ProductAttribute-Validator-Coverage-State:
ProductAttribute-Result-State:
ProductAttribute-Error-Localization-State:
ProductAttribute-Message-Classification-State:
ProductAttribute-PlatformHttpException-State:
ProductAttribute-InvalidOperationExpectedFlow-State:
Actor-Context-State:
Canonicalizer-Rule-State:
Bulk-Transaction-State:
Product-History-State:
Admin-Authorization-State:
Endpoints-To-Infrastructure-State:
Catalog-To-Host-State:
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

After returning:

STOP completely.
Do not start W11.
Do not migrate retained variant/category-change routes.
Do not inspect/start another Host folder for execution.
Wait for Architect review.

END_TOOBA_TASK