PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-MIGRATION-SEAM-001
Parent-Task: TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-CATALOG-SEED-REHOME-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Development ProductWorkspace Closure
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_DEVELOPMENT_PRODUCTWORKSPACE_WAVE_2_FINAL
Title: Replace Host foreign-DbContext migration orchestration with a neutral module schema migrator seam and delete ProductWorkspaceDevelopmentBootstrap

CURRENT ACCEPTED STATE

Architect has ACCEPTED Wave 1:

Task: TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-CATALOG-SEED-REHOME-001
Commit: e16781dc1899456aec20824afc01e19f53c9a70b
ProductWorkspaceDevelopmentBootstrap.cs: 428 -> 175 LOC
Business seed authority: ZERO in Host
Remaining debt: schema migration/composition only
Host/Development file count: 5

IMPORTANT RECOVERY NOTE

Current top-level SoT pointer may still reference the prior accepted implementation wave because Wave 1 intentionally waited for Architect acceptance.

This Task MUST first reconcile the accepted Wave 1 pointer before applying Wave 2, and on PASS promote this Task as the latest accepted implementation wave.

Do NOT create a separate R1 for this.

MANDATORY SKILL ORDER

.cursor/skills/tooba-architecture-analyze/SKILL.md
.cursor/skills/tooba-architecture-migrate/SKILL.md
.cursor/skills/tooba-architecture-certify/SKILL.md

ACTIVE TARGET

src/backend/Host/Tooba.Host/Development/ProductWorkspaceDevelopmentBootstrap.cs

FINAL REQUIRED STATE

On PASS:

ProductWorkspaceDevelopmentBootstrap.cs = ABSENT

Host/Development must contain only:

DevelopmentTenantCommerceContext.cs
DevelopmentSchemaMigrator.cs
MarketplaceDevelopmentBootstrap.cs
MarketplaceAdminDevBootstrap.cs
MarketplaceSellerDevBootstrap.cs

All five must be classified as allowed Host development composition/runtime seams.

ARCHITECT REFINEMENT TO PRIOR ANALYSIS

Do NOT implement one hand-written migration adapter class per module unless a specific module requires special migration behavior.

Preferred design:

Introduce a neutral persistence seam in:
src/backend/BuildingBlocks/Tooba.Persistence/

Preferred abstractions:

IModuleSchemaMigrator
a small immutable descriptor/metadata carrying stable module identity + explicit order
one generic EF implementation such as EfModuleSchemaMigrator<TContext>
one registration extension such as AddModuleSchemaMigrator<TContext>(...)

Each owning module registers its own DbContext with this neutral seam from its existing Infrastructure composition root.

Host resolves only IEnumerable<IModuleSchemaMigrator> / neutral descriptors.
Host must not name foreign DbContext types.

Preserve explicit deterministic migration order.

Reuse existing module-specific schema migrators where their behavior is not equivalent to generic DbContext.Database.MigrateAsync().

Do not create 20+ trivial adapter files if one generic neutral implementation can safely cover them.

NEUTRAL SEAM RULES

IModuleSchemaMigrator must:

live in neutral persistence/building-block infrastructure
expose no module entity
expose no foreign DbContext
expose no IServiceProvider
expose no module Application/Domain type
have stable module identity
support deterministic ordering
accept CancellationToken
perform only schema migration responsibility

No business logic.

SCHEMA MIGRATION BEHAVIOR

Preserve the current Development schema migration behavior and ordering.

Current ProductWorkspace migration path must remain semantically equivalent for:

Catalog
Offer
Pricing
Inventory
Tax
Party
Identity
Cart
Order
Payment
Fulfillment
Promotion
PlatformProbe
Reviews
ProductQnA
BulkInquiry
Wishlist
AddressBook
CustomerProfile
UserPreference
OperatorProfile
Content
Media
PageComposition
Story
Notification
AccessControl
Support

Do NOT silently add unrelated modules to Development migration just because MigrationRunner has a broader production registry.

Do NOT remove any currently migrated module.

Do NOT reorder the current Development sequence unless exact current behavior proves the order is irrelevant and a canonical existing order is already authoritative.

PRESERVE EXISTING SPECIAL MIGRATORS

Inspect and reuse where applicable:

IOfferSchemaMigrator
IPricingSchemaMigrator
IInventorySchemaMigrator
ITaxSchemaMigrator
IPromotionSchemaMigrator

If their actual runtime behavior is exactly generic EF migrate, they may implement/bridge the neutral seam without changing their existing public contract.

Do NOT delete their existing contracts merely for uniformity.

GENERIC REGISTRATION

For contexts with ordinary:
context.Database.MigrateAsync(ct)

prefer one shared generic implementation rather than per-module adapter classes.

Each module must own registration of its own context into the neutral seam.

Allowed changes:

existing module Infrastructure composition root / module registration file
neutral Tooba.Persistence seam
Host DevelopmentSchemaMigrator
Program call sites
focused guards
MigrationRunner only if needed to remove duplicated order knowledge safely

Do not restructure module folders.

MIGRATIONRUNNER

Read:

src/backend/Host/Tooba.MigrationRunner/ModuleMigrationRegistry.cs
MigrationOrchestrator
relevant tests

Goal: avoid introducing a second contradictory migration ordering model.

Preferred outcome:

one canonical stable migration-order metadata source is reused/shared where practical.

But do NOT force a broad MigrationRunner redesign if it expands scope.

Acceptable bounded outcome:

DevelopmentSchemaMigrator has its explicit accepted Development order;
a durable guard proves that order remains deliberate and pinned;
MigrationRunner remains untouched if sharing would create circular/project-reference redesign.

Never make Tooba.Host reference Tooba.MigrationRunner.

HOST DEVELOPMENT SEAM

Create:

src/backend/Host/Tooba.Host/Development/DevelopmentSchemaMigrator.cs

Responsibilities ONLY:

resolve the neutral module schema migrators
order them by their explicit stable order
invoke migration
preserve CancellationToken
no business policy
no DbContext type references
no module Application/Infrastructure/Domain imports

This is:
ALLOWED_DEVELOPMENT_COMPOSITION

PRODUCTWORKSPACE DELETION

After both remaining responsibilities have replacements:

DELETE:
src/backend/Host/Tooba.Host/Development/ProductWorkspaceDevelopmentBootstrap.cs

Update Program:

schema-only path -> DevelopmentSchemaMigrator
legacy bootstrap path:
assign/retain same development commerce context semantics
migrate schemas
invoke the already Catalog-owned IWorkspaceDemoSeed
preserve the same remaining module-owned seed invocation ordering

Do NOT duplicate Wave 1 business seed logic back into Host.

TRACE / CONTEXT PARITY

Preserve:

workspace-dev-seed
workspace-dev-schema
active tenant store-alpha behavior
same failure/skip semantics
same development-only gating

Prefer reusing DevelopmentTenantCommerceContext rather than rebuilding tenant context logic.

HOST FILE COUNT

Before: 5
After: 5

Delete:

ProductWorkspaceDevelopmentBootstrap.cs

Create:

DevelopmentSchemaMigrator.cs

No other Host production file/folder growth.

NO FOREIGN HOST PERSISTENCE

On PASS, Host/Tooba.Host/Development must have ZERO direct references to:

foreign DbContext
foreign DbSet
foreign module .Infrastructure.Persistence
foreign module .Application
foreign module .Domain

Allowed:

neutral Tooba.Persistence seam
stable Contracts
legitimate Host development composition/runtime seams

MODULE REGISTRATION SCOPE

Touch only the minimum module composition files needed to register the current Development migration set.

Do not:

re-certify modules
rename folders
reorganize Infrastructure
introduce module-specific adapter files when the generic seam suffices
modify unrelated services

CLOSED-FOLDER INTEGRITY

This task may modify module composition roots because they are the natural owners of registration.

It must NOT:

add production files into another closed Host folder
create another Host folder
resurrect deleted Host files
grow unrelated retained Host allowlists
move migration debt into MigrationRunner

SINK_FOLDER_REGRESSION = NONE required.

SCHEMA SAFETY

Architecture changes only.

No:

new EF migration
schema snapshot change
migration identifier change
table/column/index/FK change
migration Up/Down change
database behavior redesign

GUARDS

Add/update focused guards proving:

ProductWorkspaceDevelopmentBootstrap.cs absent.
DevelopmentSchemaMigrator.cs contains no foreign module Application/Infrastructure/Domain/Persistence references.
Host/Development exact classified file set = 5.
ProductWorkspace business seed remains Catalog-owned.
Every currently required Development schema module is represented exactly once.
Migration order is explicit and deterministic.
Duplicate module migration registration fails or is guard-detected.
No foreign DbContext names remain in Host/Development.
no sink-folder regression.
Program preserves both Development modes.
Wave 1 Contracts-only seed boundary remains intact.

Do not weaken guards.

FOCUSED VALIDATION ONLY

Build:

Tooba.Persistence
Tooba.Host
only touched module Infrastructure projects
Tooba.MigrationRunner only if touched
Tooba.Host.Tests
MigrationRunner.Tests only if MigrationRunner touched

Focused tests:

Host Development guards
architecture boundary guard for Host/Development
migration ordering/registration guard
durable recovery guard
any touched existing module-specific schema-migrator tests

No solution-wide test.
No frontend tests.
No broad certified-module test fleet.
No open-ended repair loop.

One deterministic local repair maximum before rerunning the affected check.
If unresolved: STOP INCOMPLETE.

RECOVERY / SOT

First record Architect acceptance of Wave 1.

On final PASS, top-level must become:

lastAcceptedTask = TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-MIGRATION-SEAM-001
lastAcceptedCommit = <implementation commit>
currentHostCheckpoint = Development
latestAcceptedImplementationWave = TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-MIGRATION-SEAM-001
automaticNextImplementationTask = NONE
nextTaskState = USER_DECISION_REQUIRED
workflowStop = USER_REVIEW_HOST_DEVELOPMENT_PRODUCTWORKSPACE_MIGRATION_SEAM_001

Add SoT block:
hostDevelopmentProductWorkspaceMigrationSeam001

Required fields:

productWorkspaceBootstrapState = ABSENT
developmentSchemaMigratorState = ALLOWED_DEVELOPMENT_COMPOSITION
hostDevelopmentBusinessAuthorityState = ZERO
hostDevelopmentForeignPersistenceState = ZERO
moduleSchemaMigrationBoundaryState = NEUTRAL_SEAM
migrationOrderState = PRESERVED_DETERMINISTIC
hostDevelopmentFileCountBefore = 5
hostDevelopmentFileCountAfter = 5
sinkFolderRegressionState = NONE
schemaChangeState = NONE
routeChangeState = NONE
frontendState = UNCHANGED
catalogSeedWave1State = PRESERVED
automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_DEVELOPMENT_PRODUCTWORKSPACE_MIGRATION_SEAM_001
certificationState = PASS

EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-MIGRATION-SEAM-001/

Required:

analyze.md
migration-design.md
registration-map.md
migration-order-parity.md
validation.md
certification.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-MIGRATION-SEAM-001.task.md

SUCCESS CRITERIA

PASS only if:

ProductWorkspaceDevelopmentBootstrap.cs is deleted
DevelopmentSchemaMigrator is thin Host composition
Host/Development has no foreign DbContext/persistence dependency
neutral generic seam is used where applicable
per-module trivial adapter explosion is avoided
special existing migrators preserved/reused
current Development migration set unchanged
migration order deterministic and parity-proven
Wave 1 Catalog business seed remains intact
Host/Development file count remains 5
no sink-folder regression
no schema/route/frontend change
focused validation passes
SoT fully reconciled
no automatic next task
user work preserved

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
Task-ID: TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-MIGRATION-SEAM-001
Parent-Task: TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-CATALOG-SEED-REHOME-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Wave-1-Acceptance-Reconciliation-State:
ProductWorkspace-Bootstrap-State:
Development-Schema-Migrator-State:
Neutral-Migration-Seam-State:
Generic-Migrator-Reuse-State:
Per-Module-Adapter-Explosion-State:
Special-Migrator-Preservation-State:
Host-Development-Business-Authority-State:
Host-Development-Foreign-Persistence-State:
Migration-Module-Set-State:
Migration-Order-State:
Trace-Context-Parity-State:
Catalog-Seed-Wave1-Preservation-State:
Host-Development-File-Count-Before:
Host-Development-File-Count-After:
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

Do not start another Host folder.
Do not touch Host/Wishlist.
Do not touch Host/Settings.
Do not create a follow-up task.
Wait for Architect/user review.

END_TOOBA_TASK
