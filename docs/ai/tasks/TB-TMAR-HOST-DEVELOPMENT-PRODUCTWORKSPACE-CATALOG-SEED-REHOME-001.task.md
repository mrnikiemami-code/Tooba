PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-CATALOG-SEED-REHOME-001
Parent-Task: TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-ANALYZE-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Development ProductWorkspace Closure
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_DEVELOPMENT_PRODUCTWORKSPACE_WAVE_1
Title: Rehome ProductWorkspace Catalog demo business seed from Host into Catalog

CURRENT ACCEPTED CHECKPOINT

Latest accepted implementation wave:

TB-TMAR-HOST-DEVELOPMENT-ENRICHER-CLOSURE-001
implementation commit: 44e6dde059a85d749846a403a40d33f87e07ac6e

Accepted analysis:

TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-ANALYZE-001
ProductWorkspaceDevelopmentBootstrap.cs = 428 LOC
final closure plan = TWO waves
this task = Wave 1 ONLY
Wave 2 MUST NOT START

ACTIVE SOURCE

src/backend/Host/Tooba.Host/Development/ProductWorkspaceDevelopmentBootstrap.cs

MANDATORY SKILL ORDER

.cursor/skills/tooba-architecture-analyze/SKILL.md
.cursor/skills/tooba-architecture-migrate/SKILL.md
.cursor/skills/tooba-architecture-certify/SKILL.md

BOUNDARY

This task closes ONLY the ProductWorkspace business-seed responsibilities.

DO NOT:

implement the neutral schema-migrator seam;
touch the 28-context migration architecture beyond leaving it migration-only;
delete ProductWorkspaceDevelopmentBootstrap.cs in this wave;
start Wave 2;
touch Host/Wishlist or Host/Settings;
start another Host folder;
broaden into module recertification.

ARCHITECTURE DECISION

All Catalog demo business authority currently authored in ProductWorkspaceDevelopmentBootstrap must move to Catalog-owned Development code.

Host may retain only:

Development tenant/context composition;
module migration invocation/order;
module-owned seed invocation/order;
accepted Host development actor bootstrap invocation.

Host must NOT retain after PASS:

Catalog product/category/brand/attribute/variant/media/SEO/publish logic;
Admin R3 preview product creation logic;
operator-facing Catalog copy mutation;
Party organization display-name mutation;
Offer create/activate commands;
Pricing business seed logic;
Tax business seed logic;
Inventory business seed logic.

REQUIRED TARGET SHAPE

Create Catalog-owned cohesive development seed(s) under:

src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Development/

Preferred exact files:

WorkspaceDemoProductSeed.cs
WorkspaceDemoMarketplaceSeed.cs

Create one Catalog application-level entry abstraction only if needed by existing composition:

src/backend/Modules/Catalog/Tooba.Catalog.Application/Development/IWorkspaceDemoProductSeed.cs

Do not create more abstractions than necessary.
Do not introduce a new module/project.

CONTRACTS — REUSE FIRST

Reuse the accepted contracts from Enricher Closure:

Tooba.Party.Contracts.IPartyDevelopmentSeedGateway
Tooba.Offer.Contracts.Ports.IOfferDevelopmentSeedGateway
Tooba.Pricing.Contracts.IPricingDevelopmentSeedGateway
Tooba.Inventory.Contracts.Availability.IInventoryDevelopmentSeedGateway
Tooba.Tax.Contracts.ITaxDevelopmentSeedGateway

Allowed narrow additive changes ONLY if required by exact parity:

Party:
add one narrow development method for organization display-name refresh.

Tax:
add one narrow development method to ensure the exact development tax rule currently created/activated by ProductWorkspace.

Inventory:
add one narrow development method for the exact development reservation-hold behavior currently used by ProductWorkspace.

Do NOT expose:

DbContext
DbSet
module entity types
Application services
Domain types
IServiceProvider
generic business CRUD

OFFER

Do not add another Offer abstraction if existing:
IOfferDevelopmentSeedGateway.EnsureActiveSellerOfferAsync
is sufficient.

Offer is a certified/reference module: touch only the exact existing Contracts/Infrastructure surface if compile/parity requires it; no refactor or recertification.

BEHAVIOR PARITY — MUST PRESERVE

Preserve exact current development semantics, including:

Catalog:

workspace-live-shirt
category hierarchy
tooba-live
color
black
LIVE-SHIRT-BLK
existing media GUIDs and ordering
existing SEO copy
publish behavior
Admin R3 preview/draft/archive behavior
existing refresh behavior

Marketplace demo:

both sellers and their display names
seller SKUs:
ARM-LN-01
DGS-LN-01
offer activation
existing price amounts/currency/market/channel/start semantics
tax category/rule semantics
existing inventory locations/codes
stock quantities
reservation reference/reason/count including workspace-live-hold
all current idempotency/reuse behavior

Do not "simplify" values.

IDEMPOTENCY

Do not regress idempotency.

Using Ensure*/reuse gateways is acceptable and preferred when behavior-equivalent or stronger.
Document every intentional behavior-equivalent idempotency strengthening.

PRODUCTWORKSPACE FINAL STATE FOR THIS WAVE

On PASS:

ProductWorkspaceDevelopmentBootstrap.cs remains physically present but is reduced to migration/composition-only responsibility.

It must have ZERO direct references to:

Catalog Application business directory
Party Application
Offer Application/MediatR request
Pricing Application
Tax Application/Domain
Inventory Application/Domain
Catalog/Party business DbSet mutation
business seed constants/details moved to Catalog

It may still contain the existing schema-migration calls because those are Wave 2.

HOST FILE COUNT

Host/Development remains:
5 -> 5

No new Host production file.
No other Host folder grows.

PROGRAM COMPOSITION

Update Program only as needed so the same Development execution order is preserved.

When RunLegacyBootstraps=true, Catalog-owned Workspace seed must run at the same logical point where ProductWorkspace business seeding ran before.

When schema-only mode is used, do NOT accidentally run Catalog business seed.

Do not change production execution.

DESTINATION INTEGRITY

Apply closed-folder guard.

For every destination touched:

classify it
document before/after
no new Host sink
no silent growth of unrelated closed folder
no unrelated certified-module modifications

NO SCHEMA / ROUTE / FRONTEND CHANGE

no EF migration
no DDL/schema change
no route change
no endpoint change
no frontend change

GUARDS

Add/update only focused durable guards proving:

ProductWorkspace Host file still exists but contains no business-seed authority listed above.
Catalog owns Workspace demo seed.
Catalog Workspace seed has no foreign .Application, .Infrastructure, .Domain, DbContext/DbSet references except its own Catalog internals.
Foreign operations are Contracts-only.
Host/Development exact classified set remains 5.
ProductWorkspace migration debt remains explicitly open for Wave 2.
No sink-folder regression.
Exact important seed values remain pinned where practical.

Do not weaken existing guards.

FOCUSED VALIDATION ONLY

Build only touched projects:

Catalog Application/Infrastructure
Party Contracts/Infrastructure if changed
Tax Contracts/Infrastructure if changed
Inventory Contracts/Infrastructure if changed
Offer Contracts/Infrastructure only if changed
Host
Host.Tests

Focused tests only:

Host Development guards
Catalog foundation/development seed tests relevant to this change
exact touched module development-seed gateway tests
durable recovery guard
architecture boundary guard for touched surface

No solution-wide test.
No broad recertification.
No open-ended test/repair loop.

If one deterministic local failure occurs:

repair once;
rerun only affected focused validation;
if still failing, STOP INCOMPLETE.

EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-CATALOG-SEED-REHOME-001/

Required:

analyze.md
migration.md
behavior-parity.md
contract-map.md
validation.md
certification.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-CATALOG-SEED-REHOME-001.task.md

SOT

Add:
hostDevelopmentProductWorkspaceCatalogSeedRehome001

Required PASS fields:

catalogBusinessSeedState = MODULE_OWNED
hostBusinessAuthorityState = ZERO_FOR_PRODUCTWORKSPACE_SEED_SLICE
hostPersistenceAuthorityState = MIGRATION_ONLY_REMAINS_FOR_WAVE_2
foreignApplicationBusinessReferenceState = ZERO_FOR_SEED_SLICE
foreignDomainBusinessReferenceState = ZERO_FOR_SEED_SLICE
crossModuleBoundaryState = CONTRACTS_ONLY_FOR_SEED_SLICE
productWorkspaceState = MIGRATION_ONLY_OPEN_WAVE_2
hostDevelopmentFileCount = 5
sinkFolderRegressionState = NONE
schemaChangeState = NONE
routeChangeState = NONE
frontendState = UNCHANGED
automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_DEVELOPMENT_PRODUCTWORKSPACE_CATALOG_SEED_REHOME_001
certificationState = PASS

TOP-LEVEL POINTERS

On PASS:

latest accepted implementation wave becomes this task
current Host checkpoint remains Development
nextTaskState = USER_DECISION_REQUIRED
automaticNextImplementationTask = NONE
Wave 2 is NOT auto-started

SUCCESS CRITERIA

PASS only if:

Catalog business seed is no longer authored in Host
ProductWorkspace file is migration-only
all seed cross-module calls are Contracts-only
exact development behavior/value parity preserved
no schema/route/frontend change
Host file count remains 5
no sink-folder regression
Wave 2 debt remains explicit
focused validation passes
recovery/SoT updated truthfully

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
Task-ID: TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-CATALOG-SEED-REHOME-001
Parent-Task: TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-ANALYZE-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Catalog-Business-Seed-State:
Host-Business-Authority-State:
Host-Persistence-Authority-State:
ProductWorkspace-State:
ProductWorkspace-LOC-Before:
ProductWorkspace-LOC-After:
Contract-Reuse-State:
New-Contract-Method-State:
Foreign-Application-Business-Reference-State:
Foreign-Infrastructure-Business-Reference-State:
Foreign-Domain-Business-Reference-State:
Cross-Module-Boundary-State:
Behavior-Parity-State:
Idempotency-State:
Host-Development-File-Count:
Wave-2-Debt-State:
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

Do not start Wave 2.
Do not create Wave 2 task artifact.
Do not touch Host/Wishlist or Host/Settings.
Do not inspect/start another Host folder.
Wait for Architect/user review.

END_TOOBA_TASK
