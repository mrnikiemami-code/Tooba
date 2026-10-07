PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-PRICING-AMSC-001-W3-R2
Parent-Task: TB-TMAR-PRICING-AMSC-001-W3-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: STRUCTURE_REPAIR_INTERNAL_ONLY_APPLICABILITY
Skill: tooba-architecture-structure
Title: Remove ceremonial Pricing Endpoints project and restore INTERNAL_ONLY structure truth

ARCHITECT VERDICT
The current Pricing W3 certification is NOT accepted as final authority.

Independent repository verification found a concrete contradiction with the current AMSC skills:

Pricing W3 classifies the module as:
NOT_HTTP_OWNING_INTERNAL_CAPABILITY_PROVIDER
Pricing owns zero HTTP routes and zero endpoint-reachable requests.
Yet Pricing still has:
Tooba.Pricing.Endpoints project,
PricingEndpointModule,
MapPricingModule(),
an empty /v1/pricing route group,
Host using Tooba.Pricing.Endpoints,
Host AddPricingEndpointPresentation(),
Host MapPricingModule(),
Host ProjectReference to Tooba.Pricing.Endpoints,
/Modules/Pricing/ solution grouping with 6 projects.

Current migrate skill is explicit:

internal-only modules must not create/retain Endpoints/CQRS ceremony where no HTTP/application surface exists;
do not create ceremonial projects that are not applicable;
INTERNAL_ONLY precedent is Inventory.

Current repository precedent:

Inventory is INTERNAL_ONLY with no Endpoints project.
Inventory registers IErrorCatalogContributor + IErrorResourceSet in Infrastructure/DependencyInjection/InventoryModule.cs.

Therefore Pricing's empty Endpoints project is ceremonial structure and the prior W3 certification is structurally stale.

This task is a STRUCTURE repair only.
After this task, Pricing must return READY_FOR_CERTIFY.
Do NOT self-certify.
A fresh Certify wave will be issued separately after Architect review.

STARTING HEAD
2e664bb336f45b8304818b7e5754f9b0fc364f20

SUPERSEDED CURRENT CERTIFICATION
Task: TB-TMAR-PRICING-AMSC-001-W3
Commit: 3c2cc61e7c61813ac72773ccdb8bb16317cafe70
State: SUPERSEDED_PENDING_FRESH_CERTIFY_DUE_INTERNAL_ONLY_ENDPOINTS_CEREMONY

W3-R1
Task: TB-TMAR-PRICING-AMSC-001-W3-R1
Commit: 2e664bb336f45b8304818b7e5754f9b0fc364f20
State: HISTORICAL_RECOVERY_ONLY_FOR_SUPERSEDED_W3

GOAL
Repair Pricing to the current INTERNAL_ONLY structural standard:

Remove the ceremonial Tooba.Pricing.Endpoints project completely.
Remove the empty /v1/pricing route group and all Host mapping/registration ceremony for it.
Preserve Pricing error catalog/resource registration by moving that registration into Pricing-owned Infrastructure composition, following Inventory precedent.
Keep Contracts as the single self-contained stable-code + descriptor + localization boundary.
Keep Pricing behavior/schema/wire codes unchanged.
Keep Pricing as an internal capability provider consumed through Contracts only.
Reduce /Modules/Pricing/ solution grouping from 6 projects to 5:
Contracts, Domain, Application, Infrastructure, Tests.
Make manifest/SoT honest:
Pricing is NOT certified during this repair and must be READY_FOR_CERTIFY.
Preserve global Host root checkpoint.
automaticNextImplementationTask = NONE.

PRECHECK
Before editing:

Verify branch = main.
Verify HEAD == origin/main == 2e664bb336f45b8304818b7e5754f9b0fc364f20.
Read:
.cursor/skills/tooba-architecture-structure/SKILL.md
.cursor/skills/tooba-architecture-migrate/SKILL.md
.cursor/skills/tooba-architecture-certify/SKILL.md
docs/architecture/tmar-current-state.json
docs/architecture/tmar-module-structure-manifests.json
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
Pricing W2/W3/W3-R1 evidence
Verify current truth:
zero Pricing MapGet/MapPost/MapPut/MapDelete/MapPatch routes;
zero endpoint-reachable Pricing requests;
PricingEndpointModule.MapPricingModule() only creates empty /v1/pricing group;
AddPricingEndpointPresentation() only registers:
IErrorCatalogContributor -> PricingErrorCatalogContributor
IErrorResourceSet -> PricingErrorResourceSet
both concrete types already live in Tooba.Pricing.Contracts.Errors;
PricingModule is the existing IToobaModule Infrastructure composition root;
Host already constructs/registers new PricingModule() through module composition.
Enumerate every reference to:
Tooba.Pricing.Endpoints
PricingEndpointModule
AddPricingEndpointPresentation
MapPricingModule
Tooba.Pricing.Endpoints.csproj
Verify no hidden Pricing HTTP route depends on the Endpoints project.
If any real Pricing HTTP endpoint exists: STRUCTURE_CONFLICT + STOP.

ARCHITECTURE TARGET

Canonical Pricing project set after repair:

Tooba.Pricing.Contracts
Tooba.Pricing.Domain
Tooba.Pricing.Application
Tooba.Pricing.Infrastructure
Tooba.Pricing.Tests

No Tooba.Pricing.Endpoints project.

Canonical composition:

PricingModule.AddServices(...) registers:
IErrorCatalogContributor / PricingErrorCatalogContributor
IErrorResourceSet / PricingErrorResourceSet
existing Pricing infrastructure/services
Host composition keeps only the existing Pricing module registration path.
Host has:
no using Tooba.Pricing.Endpoints
no AddPricingEndpointPresentation()
no MapPricingModule()
no Pricing.Endpoints ProjectReference
zero /v1/pricing route group exists.

Preserve:

11 stable codes
11 descriptors
11 EN resource keys
11 FA resource keys
PricingOperation typed-fault seam
schema pricing
migration identity/history
outbox behavior
Contracts-only foreign boundaries
Offer-owned seller price route
Pricing internal boundary validation
no CQRS ceremony invented

IMPLEMENTATION

MOVE PRESENTATION REGISTRATION INTO INFRASTRUCTURE COMPOSITION
Update:
src/backend/Modules/Pricing/Tooba.Pricing.Infrastructure/DependencyInjection/PricingModule.cs

Add the canonical registrations, equivalent to Inventory precedent:

services.AddSingleton<IErrorCatalogContributor, PricingErrorCatalogContributor>();
services.AddSingleton<IErrorResourceSet, PricingErrorResourceSet>();

Add only the required BuildingBlocks / Contracts usings.

Do not move the descriptor/resource classes out of Contracts.
Do not duplicate registration.
Do not alter error semantics.

REMOVE PRICING ENDPOINTS CEREMONY
Delete the entire project:
src/backend/Modules/Pricing/Tooba.Pricing.Endpoints/

After deletion:

no Tooba.Pricing.Endpoints namespace may remain in production;
no empty /v1/pricing route group may remain anywhere.
CLEAN HOST COMPOSITION
Update Host only as required by removal:

src/backend/Host/Tooba.Host/Program.cs

remove using Tooba.Pricing.Endpoints;
remove builder.Services.AddPricingEndpointPresentation();
remove app.MapPricingModule();

Do not change any other Host route or composition behavior.

src/backend/Host/Tooba.Host/Tooba.Host.csproj

remove only the ProjectReference to Tooba.Pricing.Endpoints.csproj
preserve existing Pricing Application/Infrastructure references unless independently proven obsolete and required by this exact repair.
no unrelated reference cleanup.
SOLUTION EXPLORER
Update:
src/backend/Tooba.slnx

/Modules/Pricing/ must contain exactly 5 projects:

Application
Contracts
Domain
Infrastructure
Tests

Remove only Pricing.Endpoints entry.

TEST/GUARD REFERENCES
Update only Pricing-related tests/guards that pin the obsolete Endpoints ceremony.

Required durable structure guard truth:

Pricing is INTERNAL_ONLY / NOT_HTTP_OWNING.
Pricing Endpoints directory/project is absent.
no MapPricingModule.
no /v1/pricing empty route group.
no AddPricingEndpointPresentation.
Host has no Pricing.Endpoints reference.
error catalog/resource registration exists exactly once through Pricing Infrastructure module.
Contracts still owns one catalog contributor + one resource set.
/Modules/Pricing/ contains exactly 5 projects.
path↔namespace remains EXACT.
cross-module dependencies remain Contracts-only.
schema/migrations unchanged.

Do not weaken any unrelated guard.
Do not widen any baseline.

MANIFEST HONESTY
Update:
docs/architecture/tmar-module-structure-manifests.json

Because the prior W3 certification is invalidated by this concrete structure defect:

Pricing must NOT remain currently structureCertified: true during this repair.
Move/demote Pricing to the repository's pre-cert structure state used before fresh Certify.
Pricing project inventory must reflect exactly the 5-project INTERNAL_ONLY structure.
remove Pricing.Endpoints manifest project entry.
record W3-R2 as Structure repair / READY_FOR_CERTIFY.
do not claim current final certification in this wave.

Do not alter other module entries.

SOT HONESTY
Update docs/architecture/tmar-current-state.json additively.

Preserve historical W0/W1/W2/W3/W3-R1 blocks as history.

Add pricingAmsc001W3R2:

task = TB-TMAR-PRICING-AMSC-001-W3-R2
parentTask = TB-TMAR-PRICING-AMSC-001-W3-R1
mode = STRUCTURE_REPAIR_INTERNAL_ONLY_APPLICABILITY
startingHead = 2e664bb3
skill = tooba-architecture-structure
state = PRICING_AMSC_001_STRUCTURE_REPAIRED_READY_FOR_CERTIFY
verdict = READY_FOR_CERTIFY
priorCertificationState = SUPERSEDED_PENDING_FRESH_CERTIFY
supersededCertificationTask = TB-TMAR-PRICING-AMSC-001-W3
supersededCertificationCommit = 3c2cc61e7c61813ac72773ccdb8bb16317cafe70
internalOnlyApplicabilityState = CANONICAL_NO_ENDPOINTS_PROJECT
httpApplicability = NOT_HTTP_OWNING_INTERNAL_CAPABILITY_PROVIDER
endpointProjectState = ABSENT
endpointRouteCount = 0
endpointReachableRequests = 0
validatorCoverageState = NOT_APPLICABLE_INTERNAL_ONLY
cqrsState = NOT_APPLICABLE_INTERNAL_ONLY
presentationRegistrationState = INFRASTRUCTURE_MODULE_COMPOSITION
solutionProjectCount = 5
folderGranularityState = PROFESSIONAL_SHALLOW
pathNamespaceState = EXACT
physicalCopyState = CLEAN
rootAllowlistState = ENFORCED
solutionExplorerState = CANONICAL
foreignAppInfraDomainCoupling = ZERO
crossModuleBoundaryState = LEGAL_CONTRACTS_ONLY_BOTH_DIRECTIONS
crossModuleJoinState = NONE
schemaMigrationState = UNCHANGED
productionBehaviorState = PRESERVED
hostFinalClosure = PRESERVED
guardsWeakened = NONE
baselinesWidened = NONE
workflowStop = USER_REVIEW_PRICING_AMSC_001_W3_R2
automaticNextImplementationTask = NONE

Do NOT add self-referential R2 commit placeholder.

Temporarily remove Pricing from structureLock.certifiedModules until fresh Certify passes.
Preserve global Host checkpoint fields exactly.

MASTER RECOVERY
Append a module-local Pricing W3-R2 structure-repair checkpoint stating:
current skill applicability invalidated original W3 because Pricing was certified INTERNAL_ONLY while retaining a ceremonial Endpoints project/empty route group;
W3 3c2cc61e and W3-R1 2e664bb3 are historical/superseded for current Pricing structure authority;
W3-R2 is Structure authority only and returns READY_FOR_CERTIFY;
Pricing now has 5 projects and no Endpoints project;
presentation registration moved to Pricing Infrastructure module composition with no semantic change;
Pricing is temporarily not in certifiedModules pending fresh Certify;
schema/migrations unchanged;
Host root checkpoint preserved;
automatic next NONE.

Do not rewrite historical W3 text as if it had originally been correct.

ALLOWED FILES
Only files necessary for this bounded repair, expected categories:

Pricing:

Tooba.Pricing.Infrastructure/DependencyInjection/PricingModule.cs
deletion of Tooba.Pricing.Endpoints/**
Pricing-related tests/guards only

Host:

Tooba.Host/Program.cs
Tooba.Host/Tooba.Host.csproj
Pricing-specific architecture guards/tests only

Solution/SoT:

src/backend/Tooba.slnx
docs/architecture/tmar-module-structure-manifests.json
docs/architecture/tmar-current-state.json
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
R2 task/evidence

If another file is required solely because it directly references the removed Pricing.Endpoints project/type, it may be updated minimally and must be listed in evidence.

FORBIDDEN

business/domain behavior changes
error code value changes
descriptor classification/status/localization changes
Contracts API redesign
Pricing schema changes
migration regeneration
new HTTP routes
new CQRS requests/handlers/validators
new Endpoints project under another name
Host business logic
unrelated module cleanup
fixing the pre-existing Catalog.Contracts/Cart global path-namespace failure
weakening guards
widening baselines
fresh certification in this task
next module

EVIDENCE
Create:
docs/architecture/evidence/TB-TMAR-PRICING-AMSC-001-W3-R2/

Required:

structure-repair.md
project-inventory-before-after.md
endpoint-applicability-audit.md
validation.md

Evidence must prove:

before: 6 Pricing projects including ceremonial Endpoints;
after: 5 projects, Endpoints absent;
before: empty /v1/pricing group;
after: no /v1/pricing mapping;
error catalog/resource registration exact one occurrence before/after;
registration moved from Endpoints extension to PricingModule.AddServices;
no error/resource behavior changed;
Host Pricing.Endpoints reference absent;
slnx has exactly 5 Pricing projects;
manifest/SoT demoted honestly to pre-cert/READY_FOR_CERTIFY;
Pricing removed temporarily from certifiedModules;
schema/migrations unchanged;
foreign coupling remains zero;
global Host checkpoint preserved.

BOUNDED VALIDATION
Run focused validation only:

git diff --name-status scope proof.
Search:
Tooba.Pricing.Endpoints => ZERO production references/files
AddPricingEndpointPresentation => ZERO
MapPricingModule => ZERO
"/v1/pricing" => ZERO production route mapping
Assert Pricing project directories = exactly 5 expected production/test projects.
Assert /Modules/Pricing/ slnx entries = exactly 5.
Assert one PricingErrorCatalogContributor class and one PricingErrorResourceSet class.
Assert one DI registration each for IErrorCatalogContributor / IErrorResourceSet through PricingModule.
Assert 11 stable codes / 11 descriptors / 11 EN + 11 FA resource keys unchanged.
Assert Pricing migration file identities unchanged.
Run focused Pricing tests.
Run Pricing W1/W2/W3-related guards as applicable after updating obsolete ceremony assertions.
Run Host.Tests build.
Do NOT require repository-global unrelated failing guard to become green; prove no new Pricing-caused failure.

PASS CRITERIA
All:

Endpoints project absent
zero Pricing HTTP routes/groups
zero Pricing Endpoints Host reference
presentation registration moved exactly once to PricingModule
Contracts error/localization surface unchanged semantically
Pricing project count = 5
slnx count = 5
current structure = PROFESSIONAL_SHALLOW
path↔namespace = EXACT
physical copy = CLEAN
root allowlist = ENFORCED
Contracts-only boundaries preserved
schema/migrations unchanged
behavior preserved
manifest/SoT honestly pre-cert / READY_FOR_CERTIFY
Pricing absent from certifiedModules pending fresh Certify
global Host checkpoint preserved
guards weakened NONE
baselines widened NONE
automatic next NONE

COMMIT/PUSH
If PASS:

exactly one W3-R2 Structure repair commit
push main
verify HEAD == origin/main
STOP

RESULT CONTRACT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-PRICING-AMSC-001-W3-R2
Parent-Task: TB-TMAR-PRICING-AMSC-001-W3-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE | STRUCTURE_CONFLICT
Summary: <bounded summary>
Starting-HEAD-State: 2E664BB3 | DIVERGED
Prior-W3-Certification-State: SUPERSEDED_PENDING_FRESH_CERTIFY | CONFLICT
Structure-State: READY_FOR_CERTIFY | REPAIR_REQUIRED | CONFLICT
Http-Applicability-State: INTERNAL_ONLY_NO_ENDPOINTS | REGRESSED
Pricing-Endpoints-Project-State: ABSENT | PRESENT
Pricing-Http-Route-State: ZERO | NONZERO
Presentation-Registration-State: INFRASTRUCTURE_MODULE_EXACTLY_ONCE | CONFLICT
Pricing-Project-Count-State: EXACT_5 | CONFLICT
Solution-Grouping-State: CANONICAL_5 | CONFLICT
Folder-Granularity-State: PROFESSIONAL_SHALLOW | REGRESSED
Path-Namespace-State: EXACT | REGRESSED
Physical-Copy-State: CLEAN | REGRESSED
Root-Allowlist-State: ENFORCED | REGRESSED
Stable-Error-State: PRESERVED_11_OF_11 | REGRESSED
Localization-State: PRESERVED_11_EN_11_FA | REGRESSED
Cross-Module-Boundary-State: LEGAL_CONTRACTS_ONLY_BOTH_DIRECTIONS | REGRESSED
Foreign-App-Infra-Domain-Coupling-State: ZERO | REGRESSED
Schema-Migration-State: UNCHANGED | REGRESSED
Behavior-Preservation-State: PRESERVED | REGRESSED
Manifest-Certification-State: PRECERT_READY_FOR_CERTIFY | CONFLICT
StructureLock-Pricing-State: TEMPORARILY_REMOVED_PENDING_CERTIFY | CONFLICT
Global-Host-Checkpoint-State: PRESERVED | REGRESSED
Guards-Weakened-State: NONE | NONZERO
Baselines-Widened-State: NONE | NONZERO
Evidence-State: COMPLETE | INCOMPLETE
Commit-SHA: <sha>
HEAD-Equals-Origin-Main: YES | NO
Working-Tree-State: CLEAN | CLEAN_EXCEPT_PREEXISTING_UNRELATED_ARTIFACTS | DIRTY_UNEXPECTED
User-Work-Preserved: YES | NO
Automatic-Next-Implementation-Task-State: NONE
Workflow-Stop-State: USER_REVIEW_PRICING_AMSC_001_W3_R2
STOP
END_TOOBA_WORKER_RESULT

STOP RULE
STOP completely after result.
Do NOT run fresh Certify automatically.
Do NOT create R3 automatically.
Do NOT start another module.
Wait for Architect review.

END_TOOBA_TASK