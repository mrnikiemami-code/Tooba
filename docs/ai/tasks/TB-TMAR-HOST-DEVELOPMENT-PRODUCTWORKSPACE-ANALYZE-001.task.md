PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-ANALYZE-001
Parent-Task: TB-TMAR-HOST-DEVELOPMENT-ENRICHER-CLOSURE-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Development ProductWorkspace Closure
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_DEVELOPMENT_PRODUCTWORKSPACE_ANALYZE
Title: Produce the exact bounded closure map for ProductWorkspaceDevelopmentBootstrap without production changes

CURRENT ACCEPTED CHECKPOINT

Latest accepted implementation wave:

TB-TMAR-HOST-DEVELOPMENT-ENRICHER-CLOSURE-001
Host/Development production file count = 5
CatalogAttributeSchemaSellableEnricher = CLOSED / ABSENT FROM HOST
ProductWorkspaceDevelopmentBootstrap.cs = ONLY remaining open bounded Development debt
automaticNextImplementationTask = NONE

ACTIVE TARGET — READ ONLY

src/backend/Host/Tooba.Host/Development/ProductWorkspaceDevelopmentBootstrap.cs

DO NOT MODIFY PRODUCTION CODE IN THIS TASK.

MANDATORY SKILL ORDER

.cursor/skills/tooba-architecture-analyze/SKILL.md

Do NOT run migrate.
Do NOT run certify as a migration/certification pass.
This task is analysis + closure planning only.

PURPOSE

Produce a precise, evidence-backed closure map for the remaining ProductWorkspaceDevelopmentBootstrap debt so the Architect can issue the minimum number of safe implementation waves without another long-running or open-ended migration.

The analysis must determine exactly which responsibilities:
A. legitimately remain Host-owned platform/composition concerns,
B. belong to Catalog,
C. belong to another module,
D. should call existing module-owned Development seeds,
E. require a narrow Contracts boundary,
F. are stale/dead/redundant and can be removed.

MANDATORY READ

Read completely:

src/backend/Host/Tooba.Host/Development/ProductWorkspaceDevelopmentBootstrap.cs
DevelopmentTenantCommerceContext.cs
MarketplaceDevelopmentBootstrap.cs
Program.cs call sites
current Development guards
current SoT blocks:
hostDevelopmentAmc002
hostDevelopmentAmc002R1
hostDevelopmentEnricherClosure001
current .cursor/skills/tooba-architecture-analyze/SKILL.md
all directly referenced module-owned Development seeds
all directly referenced Contracts/Application/Infrastructure dependencies needed to classify ownership

Do not scan unrelated Host folders.

MANDATORY RESPONSIBILITY DECOMPOSITION

Build an exact map for all code in ProductWorkspaceDevelopmentBootstrap, at minimum separating:

Tenant/commerce-context assignment
Module schema migration orchestration
Catalog product/category/brand/attribute/variant seed creation
Offer creation/activation
Pricing seed behavior
Tax seed behavior
Inventory location/stock seed behavior
Party seller creation/reuse
Existing-product refresh path
Operator-facing copy refresh
Admin R3 preview seed
Seller/Admin development actor bootstrap calls
Reviews seed
Wishlist seed
AddressBook seed
CustomerProfile seed
Settings seed
Content seed
PageComposition seed
Story seed
LandingPage seed
StoreMenu seed
Promotion/Merchandising seed
any direct DbContext read/write/save behavior
any helper method not covered above

For every item return:

current dependency
true owner
current legality
target mechanism
existing Contracts/Development seed reusable?
new Contract needed?
may remain Host?
migration risk
exact target path if moved

HOST OWNERSHIP DECISION RULE

Host may retain only genuine platform/composition responsibilities such as:

obtaining Development tenant/runtime context
invoking module-owned migration/seed entrypoints
ordering cross-module startup composition when there is no business policy
process-level development bootstrap sequencing

Host must NOT retain:

product/category/brand/variant business creation logic
offer/pricing/tax/inventory business setup logic
foreign DbContext/DbSet business reads/writes
cross-module business policy
direct MediatR commands belonging to another module when a module-owned development seed/contract should own them

SCHEMA MIGRATION CLASSIFICATION

Do not automatically classify all DbContext.Database.MigrateAsync() calls as business leakage.

For each migration call determine whether it is:

legitimate Host composition/runtime migration orchestration, or
a module-owned schema responsibility that should be behind a module migrator contract.

Use current repository precedent (IOfferSchemaMigrator, IPricingSchemaMigrator, IInventorySchemaMigrator, ITaxSchemaMigrator, IPromotionSchemaMigrator) to decide.

Do NOT invent migration contracts in this task.

EXISTING-SEED REUSE AUDIT

For every *DevelopmentSeed.ApplyAsync call, determine whether:

Host should keep invoking it as composition only,
it should be composed by a module-owned aggregate development bootstrap,
it is duplicated/redundant,
it requires no change.

Do not move a seed merely because it is called from Host.

DIRECT FOREIGN DEPENDENCY AUDIT

List every direct reference from ProductWorkspaceDevelopmentBootstrap to:

foreign .Application
foreign .Infrastructure
foreign .Domain
foreign DbContext
foreign DbSet
MediatR request owned by another module
module-specific directory/port not in Contracts

For each, give the exact replacement:

existing Contracts port
new narrow Contracts port
module-owned Development seed
legitimate Host composition exception
delete as redundant

CLOSED-FOLDER / DESTINATION INTEGRITY

Apply the new general closed-folder rule.

For each proposed destination classify:

OPEN_FOR_FUTURE_BOUNDED_TASK
LOCKED_BY_ACCEPTED_DISPOSITION
NEW_LOCATION_REQUIRES_ARCHITECT_APPROVAL

Do not propose moving code into another closed Host folder.
Do not use Host/Development as a sink.
Do not propose broad changes to certified/reference modules unless strictly required by a narrow contract.

REQUIRED OUTPUT — IMPLEMENTATION WAVES

Return no more than THREE implementation waves.

Each wave must be:

coherent
independently reviewable
no cross-wave hidden prerequisite
exact file/module scope
exact success condition
no unrelated cleanup

Prefer TWO waves if safe.

For each wave include:

proposed Task-ID
exact files/modules allowed
exact responsibilities closed
files expected deleted/moved/created
Contracts changes required
Host file-count effect
focused validation required
STOP point

FINAL TARGET

The closure plan must end with one of these exact outcomes:

A. ProductWorkspaceDevelopmentBootstrap.cs = ABSENT
and only legitimate Host development composition files remain,

or

B. ProductWorkspaceDevelopmentBootstrap.cs = RETAINED_AS_THIN_HOST_PLATFORM_COMPOSITION
only if all business/persistence logic is removed and the residual file is genuinely thin, with exact responsibilities listed.

Do not accept the current 428-LOC mixed file as final.

NO PRODUCTION CHANGES

This task may create/update only:

task artifact
evidence
recovery/SoT analysis metadata if needed

Do NOT modify:

src/backend/** production files
src/frontend/**
csproj/package references
migrations/schema
architecture guards except no change should be necessary

EVIDENCE

Create:
docs/evidence/TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-ANALYZE-001/

Required:

responsibility-map.md
dependency-map.md
closure-plan.md
destination-integrity.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-ANALYZE-001.task.md

SOT

Add:
hostDevelopmentProductWorkspaceAnalyze001

Required fields:

target = ProductWorkspaceDevelopmentBootstrap.cs
productionCodeChangeState = ZERO
responsibilityMapState = COMPLETE
dependencyMapState = COMPLETE
implementationWaveCount = 1|2|3
finalTargetState = ABSENT | RETAINED_THIN_HOST_PLATFORM_COMPOSITION
automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_DEVELOPMENT_PRODUCTWORKSPACE_ANALYZE_001
analysisState = PASS

TOP-LEVEL POINTERS

Do not replace the latest accepted implementation wave with this analysis-only task.

Keep:

latest accepted implementation wave = TB-TMAR-HOST-DEVELOPMENT-ENRICHER-CLOSURE-001
current Host checkpoint = Development
automaticNextImplementationTask = NONE
nextTaskState = USER_DECISION_REQUIRED

VALIDATION

Only:

JSON parse / SoT consistency
evidence existence
exact target unchanged by hash/content
no production file changed

No builds.
No solution-wide tests.
No migration tests.

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-ANALYZE-001
Parent-Task: TB-TMAR-HOST-DEVELOPMENT-ENRICHER-CLOSURE-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Production-Code-Change-State:
Target-Hash-Preservation-State:
Responsibility-Map-State:
Dependency-Map-State:
Host-Legitimate-Composition-State:
Illegal-Business-Authority-State:
Illegal-Persistence-Authority-State:
Existing-Contract-Reuse-State:
New-Contract-Requirement-State:
Existing-Development-Seed-Reuse-State:
Destination-Integrity-State:
Implementation-Wave-Count:
Wave-1-Task-ID:
Wave-2-Task-ID:
Wave-3-Task-ID:
Final-Target-State:
Latest-Accepted-Implementation-Wave-Preservation-State:
Automatic-Next-Implementation-Task-State:
Focused-Validation-State:
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

Do not execute Wave 1.
Do not create Wave 1 task artifact.
Do not start another Host folder.
Wait for Architect/user review.

END_TOOBA_TASK
