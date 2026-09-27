PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W9
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W8
Parent-Commit: e1838903265fc11b6172fe73c39641c50c677de3
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FOLDER_BY_FOLDER_ADMIN
Title: W9 — evacuate Catalog Category Attribute-Schema Admin slice to Catalog; retain product/variant Attribute members for later waves

ARCHITECT ACCEPTANCE

W8 is ARCHITECT-ACCEPTED.

Verified parent:
e1838903265fc11b6172fe73c39641c50c677de3

Accepted W8 state:

Host/Admin stays 53 -> 53 because CatalogAttributeEndpoints.cs remains partial.
seven Attribute Definition routes Host-owned count 7 -> 0.
seven routes Catalog.Endpoints-owned exactly once.
Attributes/Definitions capability created.
IAttributeDefinitionDirectory + AttributeDefinitionDirectory focused seam.
all seven operations MediatR/ISender-backed and ICatalogAdminAuthorizer-protected.
canonical Result + ApiResponseFactory + CatalogErrorCodes.
create + metadata made atomic with single SaveChanges without schema change.
moved Definition surface has zero PlatformHttpException / expected InvalidOperationException message-as-code.
Offer dependency on moved Definition surface = ZERO.
retained Host scope is category-schema + product attributes/variants/category-change only.
W9 not started by W8.

ACTIVE HOST FOLDER

src/backend/Host/Tooba.Host/Admin/

Do not start another Host folder.
Do not start W10.

W9 BOUNDED MEMBER-LEVEL SLICE

Primary mixed Host production file:

Admin/CatalogAttributeEndpoints.cs

W9 owns ONLY the CATEGORY ATTRIBUTE-SCHEMA route group:

Base group:
/v1/admin/catalog/categories/{categoryId:guid}/attribute-schema

Exact W9 routes:

GET /v1/admin/catalog/categories/{categoryId:guid}/attribute-schema/effective
POST /v1/admin/catalog/categories/{categoryId:guid}/attribute-schema/bindings
PATCH /v1/admin/catalog/categories/{categoryId:guid}/attribute-schema/bindings/{definitionId:guid}
DELETE /v1/admin/catalog/categories/{categoryId:guid}/attribute-schema/bindings/{definitionId:guid}
PUT /v1/admin/catalog/categories/{categoryId:guid}/attribute-schema/bindings/order

Everything else in CatalogAttributeEndpoints.cs remains for later waves:

product attributes
product readiness
per-definition product value set
variant-axis assignment
variants editor/preview/apply/readiness
category-change-preview
primary-category replacement

EXPECTED HOST FILE COUNT

CatalogAttributeEndpoints.cs remains after W9.

Expected Host/Admin production file count:
53 -> 53

Member-level metrics:

W9 schema route mappings in Host before: 5
after: 0

Attribute host file state:
RETAINED_PARTIAL_FOR_PRODUCT_AND_VARIANT_WAVES

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
W1–W8 evidence
current CatalogAttributeEndpoints.cs completely
current CatalogDirectory schema methods/helpers
Catalog.Domain category-attribute assignment rules
Attribute Definition capability added in W8
Facet capability from W6 because schema/Facet effective-binding semantics may share logic
all category attribute-schema tests/callers
Catalog error catalog/resources

MANDATORY ANALYZE FIRST

Create a member-level disposition map before code changes.

Classify:

GetEffectiveSchemaAsync
BindAsync
UpdateBindingAsync
UnbindAsync
ReorderBindingsAsync
BindCategoryAttributeRequest
UpdateCategoryAttributeBindingRequest
ReorderCategoryBindingsRequest
GetEffectiveCategorySchemaAsync
BindCategoryAttributeAsync
UpdateCategoryAttributeBindingAsync
UnbindCategoryAttributeAsync
ReorderCategoryAttributeBindingsAsync
ResolveEffectiveBindingsAsync and exact helpers needed by schema
CatalogCategoryAttributeAssignmentRules.ValidateVariantAxis
category existence semantics
definition existence semantics
duplicate binding semantics
missing binding semantics
reorder exact-set semantics
inherited/override effective schema semantics
mutation guard
Program/route mapping
helpers such as ToError/MapAttributeInvalid and whether retained only for later product/variant Host members

No code move before the map exists.

TARGET OWNERSHIP

Catalog owns:

category attribute-schema effective read.
bind/update/unbind/reorder category-attribute bindings.
schema read/write models.
focused schema persistence/use-case seam.
stable schema error codes.
the five HTTP routes.

Host owns:

no business responsibility for these five migrated routes.
remaining product/variant attribute routes only until later waves.

CATALOG APPLICATION STRUCTURE

Preferred:

Tooba.Catalog.Application/
Attributes/
Schema/
Commands/
BindCategoryAttributeCommand.cs
BindCategoryAttributeHandler.cs
UpdateCategoryAttributeBindingCommand.cs
UpdateCategoryAttributeBindingHandler.cs
UnbindCategoryAttributeCommand.cs
UnbindCategoryAttributeHandler.cs
ReorderCategoryAttributeBindingsCommand.cs
ReorderCategoryAttributeBindingsHandler.cs
Queries/
GetEffectiveCategorySchemaQuery.cs
GetEffectiveCategorySchemaHandler.cs
Models/
...
Ports/
ICategoryAttributeSchemaDirectory.cs
Validators/
...

Equivalent cohesive shallow structure acceptable.

Mandatory:

no mixed *Contracts.cs bundle.
no one-folder-per-request.
exact path<->namespace.
unique authoritative MediatR requests.
internal schema DTOs remain Application-owned unless real external consumer exists.

FOCUSED PERSISTENCE PORT

Preferred:

ICategoryAttributeSchemaDirectory
CategoryAttributeSchemaDirectory

Extract only schema-specific methods/helpers required by W9.

Do not keep migrated handlers calling broad ICatalogDirectory if focused extraction is feasible.

Legacy ICatalogDirectory wrappers may remain only for CatalogDemo/tests/retained callers and must be thin one-way wrappers into focused schema directory.

Avoid duplicating ResolveEffectiveBindings logic if there is now an existing focused collaborator from Facets that can lawfully be shared inside Catalog Infrastructure.
If shared extraction is justified:

use a narrowly named internal Catalog collaborator.
do not create cross-module dependency.
do not couple Application to Infrastructure.

CQRS / ENDPOINTS

All five W9 routes:
HTTP
-> ICatalogAdminAuthorizer
-> ISender
-> Command/Query
-> Handler
-> ICategoryAttributeSchemaDirectory
-> Result

Endpoint destination:
Tooba.Catalog.Endpoints/Admin/Attributes/Schema/...
or equivalent cohesive path.

Endpoints must not inject:

ICatalogDirectory
focused directory
CatalogDbContext

RESULT / ERROR SEMANTICS

Current Host surface maps most expected failures into generic:
catalog.schema.invalid 400

W9 must normalize without message parsing.

Use:

Result / Result<T>
CatalogErrorCodes
CatalogErrorCatalogContributor
CatalogErrors.resx/.fa.resx
ApiResponseFactory

At minimum classify stable outcomes for:

category missing
definition missing
duplicate binding
binding missing
variant-axis capability/rule invalid
reorder invalid/mismatched set
invalid input shape

Preserve existing stable wire semantics where meaningful.
You may split generic catalog.schema.invalid into typed codes only where documented and useful.

No:

PlatformHttpException
InvalidOperationException expected flow
ex.Message classification
hard-coded user-facing endpoint errors

DOMAIN AUTHORITY

CatalogCategoryAttributeAssignmentRules remains authoritative.

Preserve:

definition value-kind compatibility.
definition IsVariantAxisAllowed.
flags: required/filterable/variantAxis/comparable.
effective inheritance/override semantics.
display order semantics.

If domain rule currently throws expected InvalidOperationException:
convert to typed domain violation/result-compatible outcome for W9 surface.
Do not move business rules into FluentValidation.

VALIDATION MATRIX

Classify all five requests exactly:

VALIDATOR_REQUIRED
or
NO_VALIDATOR_REQUIRED

Expected baseline to verify:

GetEffectiveCategorySchemaQuery: NO_VALIDATOR_REQUIRED
BindCategoryAttributeCommand: VALIDATOR_REQUIRED only for body/shape where appropriate
UpdateCategoryAttributeBindingCommand: VALIDATOR_REQUIRED only for body/shape if appropriate
UnbindCategoryAttributeCommand: NO_VALIDATOR_REQUIRED
ReorderCategoryAttributeBindingsCommand: VALIDATOR_REQUIRED for non-null collection transport shape

Do not duplicate DB existence, duplicate binding, exact persisted-set reorder, or variant-axis business rules in validators.

BEHAVIOR PARITY

Preserve:

exact five route methods/paths.
Admin authorization on all five.
effective schema JSON shape.
inherited/overridden flags and source ids.
bind returns HTTP 201 with { ok = true }.
update returns { ok = true }.
unbind returns { ok = true }.
reorder returns { ok = true }.
duplicate binding rejection semantics.
missing binding rejection semantics.
variant-axis rule enforcement.
exact reorder-set semantics.
mutation guard/tenant isolation.

PARTIAL HOST FILE RULE

After W9 CatalogAttributeEndpoints.cs remains.

It MUST NOT contain:

five schema route mappings
five migrated schema endpoint methods
schema-only transport records with no retained consumer

It MAY contain only genuine product/variant/category-change retained members.

Program still maps MapCatalogAttributeEndpoints() while retained routes remain.

HOST INVENTORY

Expected:
Host/Admin production file count 53 -> 53

Record:

Schema Host route count before 5
after 0
CatalogAttributeEndpoints.cs = RETAINED_PARTIAL_PRODUCT_VARIANT_ONLY

DO NOT TOUCH

Do not migrate in W9:

product attributes/readiness
SetProductAttribute
variant axes
variant editor
variant preview/apply/readiness
category change preview
primary category replacement
ProductWorkspace*
StoreAppearance*
Merchandising*
StoreLanding*
StoreMenu*
checkout/reservation/hold

Do not start W10.

DURABLE GUARDS

Prove:

five schema routes absent from Host mapping.
five schema routes Catalog.Endpoints-owned exactly once.
all five use ICatalogAdminAuthorizer + ISender.
endpoints inject no directory/DbContext.
focused schema capability exists.
focused schema port/directory exists or equally lawful narrow design evidenced.
Result + ApiResponseFactory canonical.
no PlatformHttpException on moved schema surface.
no InvalidOperationException expected-business/message-as-code on moved surface.
CatalogCategoryAttributeAssignmentRules remains domain authority.
Catalog -> Host ZERO.
Endpoints -> Infrastructure ZERO.
validator matrix exhaustive.
unique request types.
path<->namespace exact.
retained product/variant Host route groups untouched.
Host/Admin count stays 53.
W1–W8 preserved.
StoreAppearance deferred.
schema/database/frontend unchanged.
W10/next Host folder not started.

TESTS

Add/update focused tests for:

effective schema inheritance/override.
bind success.
duplicate bind.
category missing.
definition missing.
invalid variant-axis rule.
update success.
update missing binding.
unbind success/missing.
reorder exact set success.
reorder invalid set.
route ownership/auth.
canonical errorCode/status.
retained product/variant routes still exactly once.

Focused validation:

Catalog.Contracts build
Catalog.Domain build if touched
Catalog.Application build
Catalog.Infrastructure build
Catalog.Endpoints build
Host build
Host.Tests build
category schema focused tests
W1–W9 guards

Tests are evidence, not navigation.
No open-ended repair loop.

EVIDENCE

Create:
docs/evidence/TB-TMAR-HOST-ADMIN-AMC-001-W9/

Required:

analyze.md
disposition-map.md
category-schema-capability.md
cqrs.md
result-errors.md
validation.md
behavior-parity.md
partial-host-retention.md
closure.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-ADMIN-AMC-001-W9.task.md

RECOVERY SOT

Add hostAdminAmcW9 preserving W1–W8.

Record:

task/parent/parentCommit
activeHostFolder=Admin
Host file count before/after
Attribute host file retained state
schema Host route count before/after
endpoint ownership
CQRS state
focused schema port/directory state
validator matrix
Result/error state
domain-rule state
Catalog->Host
Endpoints->Infrastructure
retained product/variant scope
route/behavior parity
path/namespace
StoreAppearance deferred
schema/frontend unchanged
remaining Admin blockers
nextHostFolderStarted=false
workflowStop=USER_REVIEW_HOST_ADMIN_W9_CHECKPOINT

PASS CRITERIA

PASS only if ALL:

W9 five-route schema member-level disposition complete.
five schema routes Host-owned count 5 -> 0.
five routes Catalog-owned exactly once.
all five MediatR-backed.
all five Admin routes use ICatalogAdminAuthorizer.
endpoints have zero directory/DbContext injection.
focused schema Application capability exists.
focused schema persistence seam exists or equally narrow design evidenced.
expected schema failures use Result/stable Catalog codes.
no PlatformHttpException expected flow.
no InvalidOperationException message-as-code expected flow.
no message parsing.
ApiResponseFactory canonical.
CatalogCategoryAttributeAssignmentRules preserved as domain authority.
effective/inherited schema semantics preserved.
validator matrix exhaustive.
response status/shape parity preserved.
retained product/variant Host routes untouched.
CatalogAttributeEndpoints.cs retained only for product/variant/category-change members.
Host/Admin count 53.
W1–W8 preserved.
StoreAppearance deferred.
no schema migration/frontend change.
focused builds/tests/guards PASS.
evidence/task/SoT persisted.
commit pushed.
HEAD==origin/main.
working tree clean.
W10 not started.
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
Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W9
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W8
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
W8-Preservation-State:
Host-Admin-File-Count-Before:
Host-Admin-File-Count-After:
Attribute-Host-File-State:
Schema-Host-Route-Count-Before:
Schema-Host-Route-Count-After:
Schema-Endpoint-Ownership-State:
Schema-Route-Count:
Schema-CQRS-State:
Schema-Application-Structure-State:
Schema-Persistence-Port-State:
Schema-Validator-Coverage-State:
Schema-Result-State:
Schema-Error-Localization-State:
Schema-Message-Classification-State:
Schema-PlatformHttpException-State:
Schema-InvalidOperationExpectedFlow-State:
Schema-Domain-Rule-State:
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
Do not start W10.
Do not migrate retained product/variant Attribute groups.
Do not inspect/start another Host folder for execution.
Wait for Architect review.

END_TOOBA_TASK