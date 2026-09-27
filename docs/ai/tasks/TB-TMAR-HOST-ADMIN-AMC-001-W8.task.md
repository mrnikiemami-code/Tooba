PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W8
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W7
Parent-Commit: d82d130643aec119a9a282654a4823421b44ff7a
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FOLDER_BY_FOLDER_ADMIN
Title: W8 — evacuate Catalog Attribute Definition Admin slice to Catalog; retain remaining Attribute file members for later waves

ARCHITECT ACCEPTANCE

W7 is ARCHITECT-ACCEPTED.

Verified parent:
d82d130643aec119a9a282654a4823421b44ff7a

Accepted W7 state:

Host/Admin 54 -> 53.
Host CatalogCategoryEndpoints.cs absent.
10 Category routes Catalog-owned exactly once.
Categories capability = Categories/{Commands,Queries,Models,Ports,Validators}.
ICategoryDirectory + CategoryDirectory focused seam.
all 10 operations MediatR/ISender-backed.
duplicate slug is typed catalog.category.slug.duplicate HTTP 409 with ZERO message parsing.
Category hierarchy/domain rules preserved.
route-history/current-slug/history-redirect/storefront eligibility preserved.
Catalog -> Host = ZERO.
Catalog.Endpoints -> Catalog.Infrastructure = ZERO.
StoreAppearance remains deferred.
schema/frontend unchanged.
W8 was not started by W7.

ACTIVE HOST FOLDER

src/backend/Host/Tooba.Host/Admin/

Do not start another Host folder.
Do not start W9.

W8 BOUNDED MEMBER-LEVEL SLICE

Primary mixed Host production file:

Admin/CatalogAttributeEndpoints.cs

W8 does NOT evacuate the whole file.

W8 owns ONLY the Attribute Definition administration group:

Base group:
/v1/admin/catalog/attribute-definitions

Exact W8 routes:

GET /v1/admin/catalog/attribute-definitions/
GET /v1/admin/catalog/attribute-definitions/{definitionId:guid}
POST /v1/admin/catalog/attribute-definitions/
PATCH /v1/admin/catalog/attribute-definitions/{definitionId:guid}
GET /v1/admin/catalog/attribute-definitions/{definitionId:guid}/variant-axis-capability/disable-preview
PUT /v1/admin/catalog/attribute-definitions/{definitionId:guid}/variant-axis-capability
POST /v1/admin/catalog/attribute-definitions/{definitionId:guid}/options

Everything else currently inside CatalogAttributeEndpoints.cs is RETAINED for later waves, including:

category attribute-schema routes
product attribute editor routes
product readiness routes
variant-axis assignment routes
variant editor/preview/apply/readiness routes
category-change-preview / primary-category routes

This is intentionally a member-level split under AMC.
Do not move unrelated retained members.

EXPECTED HOST FILE COUNT

Because CatalogAttributeEndpoints.cs remains for retained members:

Host/Admin production file count is expected to stay 53 -> 53.

Do NOT delete the file in W8.
Instead:

remove the seven migrated route mappings/methods/transport records from the Host file,
leave only retained later-wave members,
prove the seven moved routes are no longer Host-owned.

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
W1/W2/W2-R1/W3/W4/W5/W6/W7 evidence
CatalogAttributeEndpoints.cs completely
all tests/callers for the seven Definition routes
current CatalogDirectory Attribute Definition methods/helpers
Catalog.Domain attribute-definition / variant-axis rules
Offer.Contracts dependencies currently referenced from this Host file
Catalog error catalog/resources
existing Catalog capability folder conventions

MANDATORY ANALYZE FIRST

Before code changes create an exact member-level disposition map for CatalogAttributeEndpoints.cs.

Every member in the file must be classified:

MOVE_W8
RETAIN_FOR_W9_PLUS
SHARED_TRANSPORT_SPLIT
REMOVE_DEAD
REPLACE_WITH_MODULE_ENDPOINT

For W8 specifically classify:

ListDefinitionsAsync
GetDefinitionAsync
CreateDefinitionAsync
UpdateDefinitionAsync
PreviewVariantAxisCapabilityDisableAsync
SetVariantAxisCapabilityAsync
AddOptionAsync
all request/response transport records used exclusively by these seven routes
shared models also used by retained routes
MapCatalogAttributeEndpoints route-registration statements
ToError / MapAttributeInvalid helpers and whether still required by retained Host members
ICatalogDirectory methods used by these seven operations
Catalog Domain validation invoked by these operations
any Offer.Contracts call used by variant-axis capability preview/set behavior
Program mapping implications

Do not move code before this map exists.

TARGET OWNERSHIP

Catalog owns:

Attribute Definition list/get/create/update.
variant-axis capability preview/set for definitions.
option creation for definitions.
request/read models for these use cases.
focused persistence/use-case seam.
stable error codes.
these seven Admin HTTP routes.

Offer owns only its actual cross-module offer/variant data authority.
If the current preview/set operation requires Offer information:

consume Offer.Contracts only.
no Catalog -> Offer.Application/Infrastructure/Domain.
do not duplicate Offer business logic.

Host owns:

no business responsibility for these seven migrated routes.
retained later Attribute routes only until their own waves.

CATALOG APPLICATION STRUCTURE

Capability-first shallow-by-default.

Preferred structure:

Tooba.Catalog.Application/
Attributes/
Definitions/
Commands/
CreateAttributeDefinitionCommand.cs
CreateAttributeDefinitionHandler.cs
UpdateAttributeDefinitionCommand.cs
UpdateAttributeDefinitionHandler.cs
SetVariantAxisCapabilityCommand.cs
SetVariantAxisCapabilityHandler.cs
AddAttributeOptionCommand.cs
AddAttributeOptionHandler.cs
Queries/
ListAttributeDefinitionsQuery.cs
ListAttributeDefinitionsHandler.cs
GetAttributeDefinitionQuery.cs
GetAttributeDefinitionHandler.cs
PreviewVariantAxisCapabilityDisableQuery.cs
PreviewVariantAxisCapabilityDisableHandler.cs
Models/
...
Ports/
IAttributeDefinitionDirectory.cs
Validators/
...

An equally shallow cohesive structure is acceptable, e.g.
Attributes/{Commands,Queries,Models,Ports,Validators} if definitions are the only owned scope there and names remain unambiguous.

Mandatory:

no generic/mixed *Contracts.cs bundle.
no root Application Attribute files introduced.
no one-leaf-folder-per-request tree.
exact path <-> namespace.
authoritative request types unique.
internal DTOs remain Application-owned unless a real foreign-module consumer exists.

FOCUSED PERSISTENCE PORT

Do not keep new handlers calling broad ICatalogDirectory if a focused seam is feasible.

Preferred:

IAttributeDefinitionDirectory
AttributeDefinitionDirectory

Extract only the Definition-related persistence/use-case methods required by W8.

Legacy broad ICatalogDirectory wrappers may remain only if proven necessary for CatalogDemo/tests/retained Host members.
They must be thin one-way wrappers into the focused implementation and recorded as residual debt.

Do not decompose retained category-schema/product-attribute/variant functionality in W8.

CQRS / ENDPOINTS

All seven W8 routes must become Catalog.Endpoints-owned and MediatR-backed.

Flow:
HTTP
-> ICatalogAdminAuthorizer
-> ISender
-> Command/Query
-> Handler
-> focused Attribute Definition port
-> Result

Endpoint destination:
Tooba.Catalog.Endpoints/Admin/Attributes/Definitions/...
or equally cohesive current Catalog.Endpoints path.

Endpoints must NOT inject:

ICatalogDirectory
IAttributeDefinitionDirectory
CatalogDbContext
Offer ports directly

Cross-module access belongs behind Application/Infrastructure boundary as appropriate, using Contracts only.

RESULT / ERROR SEMANTICS

Current Host surface uses:

PlatformHttpException
InvalidOperationException
hard-coded catalog.attribute.invalid
hard-coded catalog.attribute.missing
message-bearing Results.Json/Problem paths

W8 must canonicalize the moved seven-route surface.

Use:

Result / Result<T>
CatalogErrorCodes
CatalogErrorCatalogContributor
CatalogErrors.resx / .fa.resx
ApiResponseFactory

Expected business outcomes must not rely on:

ex.Message
PlatformHttpException
InvalidOperationException catch-and-map
Persian message classification

At minimum classify stable outcomes for:

definition missing
definition code duplicate
localized definition name duplicate per locale
invalid attribute definition input/value-kind combination
invalid variant-axis capability enable/disable transition
option invalid/duplicate if current behavior distinguishes these
preview target missing

Preserve existing externally meaningful status/errorCode semantics where already stable.
Normalize generic bags only when evidence documents the prior ambiguity.

DOMAIN AUTHORITY

Preserve Catalog Domain authority for:

CatalogAttributeValueKind rules
variant-axis eligibility
attribute metadata invariants
option rules
capability enable/disable constraints

If current Domain methods throw expected InvalidOperationException:

convert to typed violation/result-compatible outcomes where required by this W8 surface.
do not move domain rules to validators.
do not parse messages.

OFFER CONTRACT BOUNDARY

CatalogAttributeEndpoints currently references Offer.Contracts.

For the seven W8 routes:

inspect exact preview/set dependencies.
if Offer data is needed, keep dependency Contracts-only.
prefer a narrow Catalog Application port implemented by Infrastructure over Offer.Contracts if that matches established module conventions.
Catalog.Endpoints must not depend directly on Offer.Contracts unless it is purely transport-neutral and architecture rules explicitly permit it.
ZERO dependency on Offer.Application, Offer.Infrastructure, Offer.Domain.

VALIDATION MATRIX

Classify all seven endpoint-reachable requests exactly one:

VALIDATOR_REQUIRED
NO_VALIDATOR_REQUIRED

Expected baseline to verify:

ListAttributeDefinitionsQuery: NO_VALIDATOR_REQUIRED
GetAttributeDefinitionQuery: NO_VALIDATOR_REQUIRED
CreateAttributeDefinitionCommand: VALIDATOR_REQUIRED
UpdateAttributeDefinitionCommand: VALIDATOR_REQUIRED for transport shape only if body invariants exist
PreviewVariantAxisCapabilityDisableQuery: NO_VALIDATOR_REQUIRED
SetVariantAxisCapabilityCommand: VALIDATOR_REQUIRED only if body shape requires it
AddAttributeOptionCommand: VALIDATOR_REQUIRED

Do not duplicate:

DB uniqueness
domain capability rules
option existence
cross-module usage checks
inside FluentValidation.

BEHAVIOR PARITY

Preserve:

route methods/paths.
Admin authorization on all seven routes.
list ordering and response shape.
get missing behavior as canonical 404.
create response 201 and { definitionId } shape.
create with metadata follow-up semantics if current behavior performs create + metadata update.
update response shape.
preview response shape and exact impact semantics.
set capability response shape.
add option response 201 and { optionId }.
localized-name uniqueness behavior.
variant-axis capability rules.
tenant isolation/mutation guard.
Offer-contract usage semantics where relevant.

IMPORTANT CREATE ATOMICITY AUDIT

Current Host CreateDefinition flow may:

create definition
then apply metadata

Analyze whether this is currently one logical operation with possible partial persistence.

W8 must not silently worsen behavior.
If canonical Command can safely make it one atomic application operation using the same DbContext transaction/unit of work, do so and document it.
Do not introduce schema changes.
If parity requires exact existing behavior, explain why and preserve it.

PARTIAL HOST FILE RULE

After W8:
CatalogAttributeEndpoints.cs remains.

It MUST NOT contain:

mappings for the seven W8 routes
seven migrated endpoint methods
Definition-only transport records that have no retained consumer
new aliases/shims back to module endpoints

It MAY contain:

category attribute-schema routes
product attributes/variants routes
helpers/transport records still genuinely required by retained members

If a helper is shared between migrated and retained members:

keep only the retained reason-to-exist in Host
moved route must not call back into Host.

HOST INVENTORY

Start/end recursive production *.cs count:
expected 53 -> 53.

Also record a member-level metric:

W8 Definition route mappings in Host before: 7
W8 Definition route mappings in Host after: 0

And:

CatalogAttributeEndpoints.cs state = RETAINED_PARTIAL_FOR_LATER_WAVES

DO NOT TOUCH

Do not migrate in W8:

category attribute-schema route group
product attributes route group
product variant routes
ProductWorkspace*
StoreAppearanceSettings*
Merchandising*
StoreLanding*
StoreMenu*
Checkout*/Reservation*/HoldPolicy*

Do not start W9.

DURABLE GUARDS

Add/strengthen guards proving:

CatalogAttributeEndpoints.cs still exists only because retained routes remain.
all seven W8 Definition routes are absent from Host mapping.
Catalog.Endpoints owns those seven routes exactly once.
all seven use ICatalogAdminAuthorizer + ISender.
Catalog Endpoints inject no directory/DbContext/Offer implementation.
focused Attribute Definition capability exists.
focused persistence port/directory exists or equally narrow lawful design evidenced.
expected failures use Result/ApiResponseFactory/stable Catalog codes.
no PlatformHttpException on moved Attribute Definition surface.
no InvalidOperationException expected-business/message-as-code on moved surface.
Catalog -> Host = ZERO.
Catalog.Endpoints -> Catalog.Infrastructure = ZERO.
Catalog -> Offer.Application/Infrastructure/Domain = ZERO.
Offer cross-module use, if any, is Contracts-only.
validator matrix exhaustive for seven requests.
no duplicate authoritative request types.
exact path <-> namespace.
Host/Admin file count stays correct.
W1-W7 preserved.
StoreAppearance still deferred.
no schema/frontend change.
W9 / next Host folder not started.

TESTS

Add/update focused tests for:

list definitions ordering/shape.
get existing/missing.
create basic.
create with metadata.
duplicate definition code.
duplicate localized name per locale.
update metadata.
preview variant-axis disable.
set capability valid/invalid.
add option valid/invalid/duplicate as applicable.
route ownership exactly once.
Admin auth.
canonical status/errorCode.
retained Host Attribute routes still mapped exactly once and unaffected.

Focused validation:

Catalog.Contracts build
Catalog.Domain build if touched
Catalog.Application build
Catalog.Infrastructure build
Catalog.Endpoints build
Offer.Contracts build only if touched/referenced boundary changes
Host build
Host.Tests build
focused Attribute Definition tests
W1/W2/W2-R1/W3/W4/W5/W6/W7/W8 guards

Tests are evidence, not navigation.
No open-ended repair loop.

If the seven-route slice cannot be completed safely inside the timebox:
return INCOMPLETE with exact completed/remaining members and STOP.
Do not expand into retained Attribute routes.

EVIDENCE

Create:
docs/evidence/TB-TMAR-HOST-ADMIN-AMC-001-W8/

Required:

analyze.md
disposition-map.md
attribute-definition-capability.md
cqrs.md
result-errors.md
offer-boundary.md
validation.md
behavior-parity.md
partial-host-retention.md
closure.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-ADMIN-AMC-001-W8.task.md

RECOVERY SOT

Add hostAdminAmcW8 to docs/architecture/tmar-current-state.json preserving W1/W2/W2-R1/W3/W4/W5/W6/W7.

Record:

task/parent/parentCommit
activeHostFolder=Admin
Host Admin file count before/after
CatalogAttributeEndpoints partial-retention state
migrated route count=7
remaining Host Attribute route groups
endpoint ownership
CQRS state
focused port/directory state
validator matrix
Result/error state
message classification state
PlatformHttpException state
Offer boundary state
Catalog->Host
Endpoints->Infrastructure
behavior/route parity
path/namespace
StoreAppearance deferred
schema/frontend unchanged
remaining Admin blockers
nextHostFolderStarted=false
workflowStop=USER_REVIEW_HOST_ADMIN_W8_CHECKPOINT

PASS CRITERIA

PASS only if ALL:

W8 seven-route member-level disposition complete.
seven Definition routes removed from Host and Catalog-owned exactly once.
all seven operations MediatR-backed.
all seven Admin routes use ICatalogAdminAuthorizer.
endpoint direct ICatalogDirectory/focused directory/DbContext = ZERO.
focused Attribute Definition capability exists.
focused persistence seam exists or equally narrow lawful design evidenced.
expected failures use Result/stable Catalog codes.
no PlatformHttpException expected-business flow on moved surface.
no InvalidOperationException message-as-code expected flow.
no exception-message parsing.
ApiResponseFactory canonical.
error catalog/resources complete.
domain attribute/variant-axis rules remain authoritative.
Offer access, if required, is Contracts-only.
validator matrix exhaustive.
route/status/JSON/auth behavior preserved.
create metadata semantics audited and preserved/improved atomically without schema change.
retained Attribute Host routes remain untouched and functional.
CatalogAttributeEndpoints.cs retained only for later-wave members.
Host/Admin count = 53.
W1-W7 preserved.
StoreAppearance still deferred.
no schema/migration/frontend change.
focused builds/tests/guards PASS.
evidence/task/SoT persisted.
commit pushed.
HEAD == origin/main.
working tree clean.
W9 not started.
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
Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W8
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W7
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
W7-Preservation-State:
Host-Admin-File-Count-Before:
Host-Admin-File-Count-After:
Attribute-Host-File-State:
Definition-Host-Route-Count-Before:
Definition-Host-Route-Count-After:
Definition-Endpoint-Ownership-State:
Definition-Route-Count:
Definition-CQRS-State:
Definition-Application-Structure-State:
Definition-Persistence-Port-State:
Definition-Validator-Coverage-State:
Definition-Result-State:
Definition-Error-Localization-State:
Definition-Message-Classification-State:
Definition-PlatformHttpException-State:
Definition-InvalidOperationExpectedFlow-State:
Definition-Domain-Rule-State:
Offer-Boundary-State:
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
Do not start W9.
Do not migrate retained Attribute route groups.
Do not inspect/start another Host folder for execution.
Wait for Architect review.

END_TOOBA_TASK