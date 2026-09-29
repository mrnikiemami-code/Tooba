PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-SELLER-AMC-001-R2
Parent-Task: TB-TMAR-HOST-SELLER-AMC-001-R1B
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Seller AMC
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: SELLER_R2_CATALOG
Title: Evacuate Seller Catalog routes and Catalog persistence composition from Host/Seller

CURRENT ACCEPTED STATE

Latest accepted Seller implementation:

TB-TMAR-HOST-SELLER-AMC-001-R1A
implementation commit: 520c9918fefeedd245e54b28792fb16c5d0da41d

Recovery closure:

TB-TMAR-HOST-SELLER-AMC-001-R1B
currentHostCheckpoint = Seller
Host/Seller = OPEN, exactly 5 business files
Host/Security/Seller = canonical boundary
automaticNextImplementationTask = NONE

MANDATORY SKILL ORDER

.cursor/skills/tooba-architecture-analyze/SKILL.md
.cursor/skills/tooba-architecture-migrate/SKILL.md
.cursor/skills/tooba-architecture-certify/SKILL.md

EXACT R2 SCOPE — CATALOG ONLY

Evacuate ONLY these three Host-owned seller Catalog routes:

GET /v1/seller/catalog-variants
PUT /v1/seller/products/{productId:guid}/attributes/{definitionId:guid}
PUT /v1/seller/products/{productId:guid}/variant-axes

Also evacuate ONLY the Catalog-owned portion of:
Host/Seller/SellerPanelComposer.cs

Specifically:

direct CatalogDbContext
Catalog Domain/Persistence usage
ListCatalogVariantsAsync
SellerCatalogVariantOption

Do NOT touch:

GET /v1/seller/dashboard
GET /v1/seller/dev-contexts
SellerSettingsEndpoints.cs
SellerDevActorBootstrap.cs
Order dashboard query
Party/settings work
frontend

ARCHITECTURE TARGET

Catalog owns these seller Catalog capabilities.

Use:

Tooba.Catalog.Endpoints
Tooba.Catalog.Application
ISender from Endpoints
MediatR 12.5
validators for endpoint-reachable requests when validation is required
canonical Result/SemanticError + ApiResponseFactory
exact path ↔ namespace

Create/use a Seller capability under Catalog where appropriate.

Expected ownership:

Catalog variants list query -> Catalog Application Query/Handler
set product attribute -> Catalog Application Command/Handler
set product variant axes -> Catalog Application Command/Handler
transport request/response models -> Catalog Endpoints seller surface, not Host

Do NOT call ICatalogDirectory directly from Catalog Endpoints if the canonical endpoint pattern is ISender.
Do NOT put DbContext in Endpoints.
Infrastructure may own the persistence implementation/handler dependency as established by Catalog architecture.

AUTHORIZATION

Do NOT move seller platform security logic into Catalog.

Catalog Endpoints may define a narrow seller authorizer port if required.
Host may implement only a thin adapter under:
Host/Security/Seller

Prefer reuse of the already canonical Host seller security boundary.

Any new Host security adapter:

exact namespace Tooba.Host.Security.Seller
ZERO foreign Application/Domain/Infrastructure/Persistence
no service locator
no business logic

BEHAVIOR PARITY

Preserve exactly:

route paths
HTTP verbs
route parameters
seller auth requirement
status codes
response DTO shapes
success { ok = true } semantics where currently emitted
catalog variant list semantics:
Published products only
newest 100 products by UpdatedAt
localized product name preference (fa* before other locale)
variants ordered by CatalogCodeSeam
ProductStatus value parity
existing error codes:
catalog.attribute.invalid
catalog.variant_axes.invalid
seller.missing

Remove ex.Message as HTTP title classification if the canonical Catalog error pipeline already supplies stable semantic errors.
Do not introduce a second error system.

HOST FINAL STATE FOR R2

SellerPanelEndpoints.cs after R2 must contain NO Catalog routes and NO Catalog Application dependency.

SellerPanelComposer.cs after R2:

MUST contain no CatalogDbContext
MUST contain no Catalog Domain
MUST contain no Catalog Infrastructure/Persistence
may remain temporarily ONLY if still needed by dashboard/Party display composition for R4

SellerPanelModels.cs:

remove SellerCatalogVariantOption from Host if it is no longer consumed
do not touch unrelated Offer aliases except if compilation requires deleting dead aliases proven zero-consumer
do not perform R3/R4/R5 work

EXPECTED HOST ROUTE REDUCTION

Host/Seller seller routes before R2 = 7.

On PASS, these 3 Catalog routes are module-owned.
Host/Seller should retain only:

/v1/seller/dashboard
/v1/seller/dev-contexts
/v1/seller/settings GET
/v1/seller/settings PUT

Expected Host-owned Seller route count after R2 = 4.

No duplicate route mapping.

CROSS-MODULE RULES

Catalog module:

no Host dependency
foreign module consumption only through Contracts/neutral seams
no Host types in Catalog Application/Infrastructure
no direct cross-module DbContext

Host/Seller:

no Catalog Application/Domain/Infrastructure/Persistence after R2 for this slice

GUARDS

Add/update focused Seller R2 guard proving:

three exact Catalog seller routes are absent from Host.
same three exact routes are owned by Catalog.Endpoints.
no duplicate route ownership.
Catalog Endpoints use ISender.
no DbContext in Catalog Endpoints.
endpoint-reachable Catalog seller requests have correct validator classification.
Host/Seller has no CatalogDbContext.
Host/Seller has no Catalog Domain/Infrastructure/Persistence reference.
SellerCatalogVariantOption Host ownership removed if dead.
behavior/error-code parity preserved.
Host/Security/Seller R1A boundary unchanged/canonical.
no sink-folder regression.

Do not weaken existing Catalog or Seller guards.

FOCUSED VALIDATION ONLY

Build only:

Tooba.Catalog.Application
Tooba.Catalog.Infrastructure if touched
Tooba.Catalog.Endpoints
Tooba.Host
relevant test projects required by focused guards

Run only:

new Seller R2 guard
relevant Catalog seller endpoint/CQRS tests
Catalog validator coverage guard relevant to new requests
HostSellerAmcR1GuardTests
HostModuleEndpointOwnershipTests if route ownership assertion applies
TmarDurableGuardTests

No solution-wide tests.
No unrelated module suites.

RECOVERY / SOT — MANDATORY

On PASS, Recovery must be current in the SAME task.

tmar-current-state.json:

top-level lastAcceptedTask = TB-TMAR-HOST-SELLER-AMC-001-R2
lastAcceptedCommit = actual R2 implementation commit
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
currentHostEvacuation.activeModule = Seller
currentHostEvacuation.currentTask = TB-TMAR-HOST-SELLER-AMC-001-R2
currentHostEvacuation.latestAcceptedImplementationWave = TB-TMAR-HOST-SELLER-AMC-001-R2
currentHostEvacuation.currentHostCheckpoint = Seller
workflowStop = USER_REVIEW_HOST_SELLER_AMC_001_R2
nextTask = USER_REVIEW_HOST_SELLER_AMC_001_R2
nextTaskState = USER_DECISION_REQUIRED
automaticNextImplementationTask = NONE
staleCurrentPointerState = ZERO
full Host/Seller remains OPEN
Seller-R3 NOT_STARTED

Add SoT block:
hostSellerAmcR2

Record:

Catalog routes migrated = 3
Host Seller route count 7 -> 4
Catalog persistence in Host/Seller = ZERO
fullSellerFolderCertification = NOT_YET
sellerR3State = NOT_STARTED
automaticNextImplementationTask = NONE

Reconcile CURRENT/AUTHORITATIVE sections only:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md

Preserve R1/R1A/R1B and Development as HISTORICAL lineage.

Do NOT create a separate recovery repair if R2 itself passes.
R2 is not PASS until Recovery is synchronized.

EVIDENCE

Create:
docs/evidence/TB-TMAR-HOST-SELLER-AMC-001-R2/

Required:

analyze.md
migration.md
route-ownership.md
behavior-parity.md
validation.md
certification.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-SELLER-AMC-001-R2.task.md

SUCCESS CRITERIA

PASS only if:

all 3 Catalog routes are Catalog-owned
Host duplicate routes = ZERO
Catalog Endpoints use ISender
Host/Seller CatalogDbContext = ZERO
Host/Seller Catalog Domain/Infrastructure/Persistence = ZERO
exact behavior parity preserved
validator classification correct
R1A Host security boundary preserved
Host-owned seller routes = 4
Seller R3 not started
Recovery/SoT fully synchronized
automaticNextImplementationTask = NONE
frontend unchanged
user work preserved

GIT

Work from latest main.
No reset/clean/rebase/force-push.
Commit and push main only on PASS.

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-SELLER-AMC-001-R2
Parent-Task: TB-TMAR-HOST-SELLER-AMC-001-R1B
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
R1B-Recovery-State:
Catalog-Seller-Route-Ownership-State:
Catalog-Seller-Route-Count-Migrated:
Host-Seller-Route-Count-Before:
Host-Seller-Route-Count-After:
Duplicate-Route-State:
Catalog-Endpoints-ISender-State:
Catalog-Endpoint-DbContext-State:
Catalog-Seller-CQRS-State:
Validator-State:
Host-Seller-Catalog-Persistence-State:
Host-Seller-Catalog-Layer-Leakage-State:
Seller-Panel-Composer-State:
Seller-Catalog-Model-State:
Behavior-Parity-State:
Error-Code-State:
Host-Security-Seller-R1A-State:
Schema-Change-State:
Frontend-State:
Sink-Folder-Regression-State:
Full-Seller-Folder-Certification-State:
Seller-R3-State:
Focused-Build-State:
Focused-Test-State:
Recovery-State:
Stale-Current-Pointer-State:
Automatic-Next-Implementation-Task-State:
Workflow-Stop-State:
Certification-State:
Evidence-Path:
Commit-SHA:
Push-State:
HEAD-Equals-Origin-Main:
Working-Tree-Tracked-State:
User-Work-Preserved:
STOP
END_TOOBA_WORKER_RESULT

STOP COMPLETELY AFTER RESULT.
Do not start Seller-R3.
Wait for Architect/user review.

END_TOOBA_TASK
