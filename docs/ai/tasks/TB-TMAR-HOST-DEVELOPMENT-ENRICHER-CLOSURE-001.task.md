PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-DEVELOPMENT-ENRICHER-CLOSURE-001
Parent-Task: TB-TMAR-HOST-DEVELOPMENT-AMC-002-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Development Enricher Closure
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_DEVELOPMENT_ENRICHER_CLOSURE
Title: Remove CatalogAttributeSchemaSellableEnricher cross-module debt without creating a new Host sink

CURRENT ACCEPTED CHECKPOINT

Latest accepted implementation wave:

TB-TMAR-HOST-DEVELOPMENT-AMC-002
implementation commit: ba6cf54c738d443dcb61efc4264aedc8608f2b63
SoT stamp: 5919039b2313ddc8d02864e47e9f636328990cfe

Recovery/governance closure:

TB-TMAR-HOST-DEVELOPMENT-AMC-002-R1
commit: 2a51556a5ac562b7f0ec3ee0679ec9f31ed45dce

Current Development folder accepted state:

6 production files
4 allowed Host platform/composition files
2 open bounded debts:
CatalogAttributeSchemaSellableEnricher.cs
ProductWorkspaceDevelopmentBootstrap.cs

THIS TASK MAY TOUCH ONLY DEBT #1.

Do NOT touch ProductWorkspaceDevelopmentBootstrap.cs except read-only dependency inspection if required.
Do NOT start another Host folder.

MANDATORY SKILL ORDER

.cursor/skills/tooba-architecture-analyze/SKILL.md
.cursor/skills/tooba-architecture-migrate/SKILL.md
.cursor/skills/tooba-architecture-certify/SKILL.md

ACTIVE SOURCE

src/backend/Host/Tooba.Host/Development/CatalogAttributeSchemaSellableEnricher.cs

CURRENT CLASSIFICATION

CROSS_MODULE_DEVELOPMENT_ORCHESTRATION
STRUCTURAL_DEBT_ONLY
NEEDS_ARCHITECT_DECISION from AMC-002 because the current implementation reaches:

Catalog
Offer
Party
Pricing
Inventory
Tax

and directly consumes Application/Infrastructure/Persistence surfaces.

ARCHITECT DECISION FOR THIS TASK

Close this debt now.

The final state MUST NOT leave CatalogAttributeSchemaSellableEnricher in Host.

Do NOT solve this by:

moving the same god-file unchanged into Catalog;
adding Catalog -> foreign Application/Infrastructure/Domain references;
creating a new Host folder;
growing any previously closed Host folder;
weakening the closed-folder immutability guard;
adding a generic shared "dev-seed god service";
using foreign DbContexts from Catalog.

TARGET DESIGN

The Catalog-owned development seed may coordinate only through narrow stable module Contracts.

Preferred shape:

Catalog owns the schema/demo-product development seed workflow.
Each foreign bounded context that needs to make the demo product sellable exposes the smallest development-support contract needed by this exact seed.
Infrastructure implements those ports inside the owning module.
Catalog consumes only those Contracts.
Host remains composition-only and invokes the Catalog-owned development seed through the already accepted Development tenant context seam.

Do not create broader public contracts than necessary.
Do not expose DbContext/entity/Application internals.

REFERENCE-MODULE EXCEPTION

Offer is normally a read-only reference module, but for THIS task the Architect explicitly authorizes the minimum Offer Contracts + Infrastructure changes required to expose the narrow development-support port.

This exception is ONLY for the exact capability needed by this seed.
Do not re-audit, reorganize, or otherwise refactor Offer.

MANDATORY ANALYSIS BEFORE EDITING

Read completely:

CatalogAttributeSchemaSellableEnricher.cs
the Catalog development seed that consumes ICatalogAttributeSchemaSellableEnricher
its interface definition and registration
all exact foreign services currently consumed
current Contracts surfaces in Offer / Party / Pricing / Inventory / Tax
corresponding Infrastructure registration/composition
current SoT/manifest/guards affecting those modules
closed-folder / destination-integrity baselines

For each foreign dependency, decide:

existing Contract reusable
minimal new Contract port required
no longer required

Prefer reusing an existing lawful Contracts port over creating a new one.

NO CROSS-MODULE PERSISTENCE

Final Catalog implementation must have ZERO:

foreign DbContext
foreign DbSet
foreign Application dependency
foreign Infrastructure dependency
foreign Domain dependency
cross-module EF/SQL join

Party selection/creation, offer existence/creation/activation, pricing activation, inventory location/stock, and tax setup must cross module boundaries only through Contracts.

BEHAVIOR TO PRESERVE

Preserve current development behavior unless a current bug is proven:

schema demo product lookup by canonical demo slug
publish all assigned categories
publish product only when assignability/media/SEO requirements are satisfied
create variants only as currently owned by Catalog seed
seller reuse/create semantics
deterministic seller SKU prefix
idempotency: existing seller SKU prevents duplicate offer creation
offer activation
price creation + activation
inventory location reuse/create
inventory position/open + increase
tax category/rule semantics currently required for sellability
deterministic development-only values
cancellation propagation
no production-path behavior change

If one of these responsibilities is actually already owned by the Catalog seed rather than the enricher, keep ownership canonical and do not duplicate it.

CONTRACT QUALITY

Any new cross-module development contract must:

live under the natural module's Contracts project
be capability-first
have exact path-derived namespace
expose IDs/DTOs/operations only
not expose implementation types
not expose EF types
not expose module entities
not expose arbitrary IServiceProvider
be named by capability, not by this Host class name where avoidable
be narrow enough that production code is not accidentally coupled to development-only implementation details

If development-only semantics are explicit in the repository, place/name them consistently without inventing a second architectural pattern.

HOST FINAL STATE

On PASS:

src/backend/Host/Tooba.Host/Development/CatalogAttributeSchemaSellableEnricher.cs = ABSENT
no replacement Host business/orchestration file is created
no other Host folder grows
DevelopmentTenantCommerceContext.cs remains the single accepted tenant-commerce development seam
the 3 previously retained Marketplace files remain untouched unless a compile-only import adjustment is strictly required
ProductWorkspaceDevelopmentBootstrap.cs remains open debt and is NOT modified

DESTINATION INTEGRITY

Apply the new general closed-folder guard.

For every destination:

classify OPEN_FOR_CURRENT_TASK / LOCKED_BY_ACCEPTED_DISPOSITION / NEW_LOCATION
do not grow any locked Host destination
module changes must be only the minimum required capability surface
no SINK_FOLDER_REGRESSION

CQRS / HTTP

This is development-seed infrastructure, not an HTTP use case.
Do NOT add endpoints, HTTP routes, or CQRS ceremony unless an existing canonical module contract already requires it.
Do NOT create user-facing API error mappings for internal development seed failures.

SCHEMA / DATA SAFETY

no DB schema change
no EF migration
no table/column/index/FK change
no production seed behavior change
no frontend change

REGISTRATION

Update DI/composition only as required so:

owning module Infrastructure implements its Contracts port
Catalog development seed receives lawful Contracts dependencies
Host no longer registers/owns the enricher implementation
no circular project reference is introduced

GUARDS

Add/update focused durable guards proving:

Host enricher file absent.
Catalog development code has ZERO foreign Application/Infrastructure/Domain references for this capability.
New/reused ports live in Contracts.
No new Host production file/folder created.
Development folder matches its newly accepted post-task classified set.
ProductWorkspaceDevelopmentBootstrap remains unchanged/open debt.
No schema/migration change.
No sink-folder regression.

Do not weaken any existing guard.

FOCUSED VALIDATION

Run only focused builds for changed projects:

changed Contracts projects
changed Infrastructure projects
Catalog Infrastructure/Application if changed
Host
Host.Tests / exact module guard tests needed

Run only focused tests/guards for:

Development closure
touched Contract boundary
Catalog development seed behavior/idempotency if tests exist
structure/path namespace for touched files

No solution-wide test.
No broad module recertification.
No open-ended repair loop.

EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-DEVELOPMENT-ENRICHER-CLOSURE-001/

Required:

analyze.md
ownership-and-contract-map.md
migration.md
validation.md
certification.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-DEVELOPMENT-ENRICHER-CLOSURE-001.task.md

SOT

Add block:
hostDevelopmentEnricherClosure001

Required PASS fields:

hostEnricherState = ABSENT
catalogDevelopmentOwnershipState = MODULE_OWNED
crossModuleBoundaryState = CONTRACTS_ONLY
foreignApplicationReferenceState = ZERO
foreignInfrastructureReferenceState = ZERO
foreignDomainReferenceState = ZERO
foreignPersistenceReferenceState = ZERO
sinkFolderRegressionState = NONE
productWorkspaceDebtState = OPEN_UNCHANGED
schemaChangeState = NONE
frontendState = UNCHANGED
certificationState = PASS
workflowStop = USER_REVIEW_HOST_DEVELOPMENT_ENRICHER_CLOSURE_001

Top-level current checkpoint:

latest accepted implementation task becomes this task only on PASS
current Host checkpoint remains Development
automaticNextImplementationTask = NONE
nextTaskState = USER_DECISION_REQUIRED

SUCCESS CRITERIA

PASS only if:

Host enricher is gone
behavior preserved
Catalog development workflow owns the capability
all cross-module calls are Contracts-only
no foreign persistence leakage
no sink-folder regression
no schema/route/frontend change
ProductWorkspace debt remains untouched
focused builds/tests pass
recovery/SoT updated truthfully
user work preserved

If a required narrow Contracts surface cannot be introduced without a broader product/domain decision:
return INCOMPLETE with exact blocker and STOP.
Do not fall back to Host orchestration.

GIT

Work from latest main.
No reset.
No clean.
No rebase.
No force-push.
Preserve user work.

Commit and push main only on PASS.

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-DEVELOPMENT-ENRICHER-CLOSURE-001
Parent-Task: TB-TMAR-HOST-DEVELOPMENT-AMC-002-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Development-Checkpoint-Preservation-State:
Host-Enricher-State:
Catalog-Development-Ownership-State:
Contract-Reuse-State:
New-Contracts-State:
Offer-Reference-Module-Exception-State:
Foreign-Application-Reference-State:
Foreign-Infrastructure-Reference-State:
Foreign-Domain-Reference-State:
Foreign-Persistence-Reference-State:
Cross-Module-Boundary-State:
Behavior-Parity-State:
Idempotency-State:
Host-Development-File-Count:
ProductWorkspace-Debt-State:
Sink-Folder-Regression-State:
Schema-Change-State:
Route-Change-State:
Frontend-State:
Focused-Build-State:
Focused-Test-State:
Certification-State:
Evidence-Path:
SoT-State:
Commit-SHA:
Push-State:
HEAD-Equals-Origin-Main:
Working-Tree-Tracked-State:
User-Work-Preserved:
STOP
END_TOOBA_WORKER_RESULT

STOP COMPLETELY AFTER RESULT.

Do not start ProductWorkspaceDevelopmentBootstrap.
Do not inspect/start another Host folder.
Do not create another task.
Wait for Architect/user review.

END_TOOBA_TASK