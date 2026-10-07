PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-PRICING-AMSC-001-W3-R3
Parent-Task: TB-TMAR-PRICING-AMSC-001-W3-R2
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: FRESH_CERTIFY_AFTER_INTERNAL_ONLY_STRUCTURE_REPAIR
Skill: tooba-architecture-certify
Title: Fresh certification of Pricing after INTERNAL_ONLY Endpoints removal

ARCHITECT VERDICT
W3-R2 structure repair is independently accepted.

Current Pricing truth at starting head:

INTERNAL_ONLY / NOT_HTTP_OWNING_INTERNAL_CAPABILITY_PROVIDER
no Tooba.Pricing.Endpoints project
no Pricing HTTP route/group
no MapPricingModule
no AddPricingEndpointPresentation
no Host Pricing.Endpoints ProjectReference
error catalog/resource registration lives exactly once in PricingModule.AddServices
Contracts owns stable codes + catalog contributor + resource set + bilingual resources
solution grouping = 5 projects
manifest = preCert READY_FOR_CERTIFY
Pricing absent from structureLock.certifiedModules pending this fresh Certify
prior W3 3c2cc61e and W3-R1 2e664bb3 are historical/superseded for current architecture authority.

STARTING HEAD
7159c8f773c1faa9b4b6d425b19067f50ca27572

STRUCTURE AUTHORITY
TB-TMAR-PRICING-AMSC-001-W3-R2
7159c8f773c1faa9b4b6d425b19067f50ca27572
READY_FOR_CERTIFY

SUPERSEDED CERTIFICATION
TB-TMAR-PRICING-AMSC-001-W3
3c2cc61e7c61813ac72773ccdb8bb16317cafe70

GOAL
Perform a fresh independent Certify wave against the repaired 5-project INTERNAL_ONLY Pricing surface.

PRECHECK

Verify branch = main.
Verify HEAD == origin/main == 7159c8f773c1faa9b4b6d425b19067f50ca27572.
Read current Certify/Structure/Migrate skills plus SoT, manifest, Master Recovery and W3-R2 evidence.
Re-enumerate Pricing physical tree directly.
Verify project set exactly Contracts, Domain, Application, Infrastructure, Tests.
Verify absent:
Tooba.Pricing.Endpoints directory/project
Tooba.Pricing.Endpoints namespace
MapPricingModule
AddPricingEndpointPresentation
/v1/pricing group
Host Pricing.Endpoints ProjectReference
Verify PricingModule.AddServices has exactly one:
IErrorCatalogContributor -> PricingErrorCatalogContributor
IErrorResourceSet -> PricingErrorResourceSet
If truth differs: CERTIFICATION_BLOCKED + STOP.

CERTIFICATION SCOPE
Verify independently:

correct Pricing business ownership;
INTERNAL_ONLY applicability;
zero module/Host Pricing HTTP routes;
zero endpoint-reachable requests;
CQRS/validators NOT_APPLICABLE_INTERNAL_ONLY;
PROFESSIONAL_SHALLOW structure;
exact path↔namespace;
CLEAN physical copies;
ENFORCED root allowlists;
CANONICAL five-project solution grouping;
one PricingErrorCodes home, 11 declared codes, KnownCodes + IsKnown;
one catalog contributor, 11 descriptors;
one resource set, 11 EN + 11 FA keys;
exact-once registration in PricingModule.AddServices;
PricingOperation dual typed-fault seam, zero ex.Message classification;
Contracts-only both directions;
Promotion inbound edge remains Pricing.Contracts-only;
foreign App/Infra/Domain coupling ZERO;
cross-module join NONE;
own pricing schema, PricingDbContext, schema migrator, outbox and existing migration;
Host business/persistence/HTTP authority ZERO;
global HOST_ROOT_FINAL_CERTIFIED preserved;
zero production/schema behavior change during Certify.

IMPLEMENTATION

FRESH CERT GUARD
Create a current Pricing cert guard:
PricingModuleAmsc001W3R3CertGuardTests

Pin:

INTERNAL_ONLY applicability
Endpoints absent
zero Pricing HTTP mapping
exact 5-project solution group
path↔namespace EXACT
root allowlists
one stable-code home / 11 codes / IsKnown
one catalog contributor / 11 descriptors
one resource set / 11 EN / 11 FA
exact-once registration in PricingModule
PricingOperation dual typed-fault / no message parsing
Contracts-only both directions
Promotion->Pricing.Application absent
own schema/outbox/migration
Host business/HTTP authority zero
W3-R2 structure authority at 7159c8f7
global Host checkpoint preserved

Do not weaken prior guards.

MANIFEST PROMOTION
Only if all certification checks pass:
move Pricing from preCertModules to certified modules
structureCertified=true
lockVersion=ARCH-COMPLETE-002
exactly 5 project entries
no Endpoints project
fresh W3-R3 certification note
remove Pricing from preCertModules
add Pricing exactly once to structureLock.certifiedModules
Do not alter other module truth.
SOT FRESH CERT BLOCK
Add pricingAmsc001W3R3:
task = TB-TMAR-PRICING-AMSC-001-W3-R3
parentTask = TB-TMAR-PRICING-AMSC-001-W3-R2
mode = FRESH_CERTIFY_AFTER_INTERNAL_ONLY_STRUCTURE_REPAIR
skill = tooba-architecture-certify
startingHead = 7159c8f7
state = PRICING_AMSC_001_RECERTIFIED
verdict = COMPLETE_REFERENCE_PATTERN
lockVersion = ARCH-COMPLETE-002
structureCertified = true
currentStructureAuthority = TB-TMAR-PRICING-AMSC-001-W3-R2
currentStructureCommit = 7159c8f773c1faa9b4b6d425b19067f50ca27572
supersededCertificationTask = TB-TMAR-PRICING-AMSC-001-W3
supersededCertificationCommit = 3c2cc61e7c61813ac72773ccdb8bb16317cafe70
httpApplicability = NOT_HTTP_OWNING_INTERNAL_CAPABILITY_PROVIDER
endpointProjectState = ABSENT
endpointOwnershipState = NOT_APPLICABLE_INTERNAL_ONLY
moduleOwnedRouteCount = 0
hostOwnedRouteCount = 0
endpointReachableRequests = 0
cqrsState = NOT_APPLICABLE_INTERNAL_ONLY
validatorCoverageState = NOT_APPLICABLE_INTERNAL_ONLY
solutionProjectCount = 5
folderGranularityState = PROFESSIONAL_SHALLOW
pathNamespaceState = EXACT
physicalCopyState = CLEAN
rootAllowlistState = ENFORCED
solutionExplorerState = CANONICAL
declaredCodeCount = 11
registeredDescriptorCount = 11
resourceKeyCountEn = 11
resourceKeyCountFa = 11
presentationRegistrationState = INFRASTRUCTURE_MODULE_EXACTLY_ONCE
messageClassification = ZERO
crossModuleBoundaryState = LEGAL_CONTRACTS_ONLY_BOTH_DIRECTIONS
foreignAppInfraDomainCoupling = ZERO
crossModuleJoinState = NONE
persistenceOwnershipState = CORRECT_OWN_PRICING_SCHEMA_OWN_OUTBOX
schemaMigrationState = UNCHANGED
microserviceExtractable = true
hostFinalClosure = PRESERVED
productionCodeChanged = false
guardsWeakened = NONE
baselinesWidened = NONE
workflowStop = USER_REVIEW_PRICING_AMSC_001_W3_R3
automaticNextImplementationTask = NONE

Do not fabricate current commit SHA or self-referential PENDING placeholder.

MASTER RECOVERY
Append fresh W3-R3 checkpoint:
original W3 3c2cc61e historical/superseded
W3-R2 7159c8f7 current Structure authority
W3-R3 fresh certification over 5-project INTERNAL_ONLY surface
zero Endpoints / zero HTTP routes
presentation registration in Infrastructure composition
Pricing promoted back to certified set only by W3-R3
Host checkpoint preserved
schema/migrations unchanged
automatic next NONE
RECOVERY DEBT DISCLOSURE
Do not hide known historical recovery debt:
pricingAmsc001W0 still has unresolved historical self-commit placeholders
W1/W2 blocks lack their own final commitFull fields
historical W3-R1 lacks its own final commit SHA

Do NOT repair those in this Certify wave.
Record:
POST_CERT_RECOVERY_RECONCILIATION_REQUIRED

ALLOWED FILES

Pricing fresh cert guard/test files
docs/architecture/tmar-current-state.json
docs/architecture/tmar-module-structure-manifests.json
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
docs/architecture/evidence/TB-TMAR-PRICING-AMSC-001-W3-R3/*
docs/ai/tasks/TB-TMAR-PRICING-AMSC-001-W3-R3.task.md
minimal global structure guard updates required solely to re-add Pricing to certified set

FORBIDDEN

production code changes
project structure changes
reintroducing Endpoints
Host production changes
schema/migration changes
error/localization changes
Contracts API changes
unrelated module repairs
historical lineage repair in this Certify wave
guard weakening
baseline widening
next module

EVIDENCE
Create:
docs/architecture/evidence/TB-TMAR-PRICING-AMSC-001-W3-R3/

Required:

certification.md
internal-only-applicability.md
structure-verification.md
boundary-verification.md
validation.md
recovery-debt.md

BOUNDED VALIDATION
Run focused:

Tooba.Pricing.Tests
Pricing W1/W2/W3-R2 guard family
fresh W3-R3 cert guard
relevant ErrorCatalogUniqueCodeGuardTests
Host.Tests build
If broad suite is run, compare failing identifiers to starting-head baseline and require zero new Pricing-caused failures.

PASS CRITERIA
All applicable Certify gates green, Pricing promoted honestly to certified 5-project INTERNAL_ONLY truth, Host checkpoint preserved, recovery debt disclosed, automatic next NONE.

COMMIT/PUSH
If PASS:

exactly one fresh W3-R3 certification commit
push main
verify HEAD == origin/main
STOP

RESULT CONTRACT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-PRICING-AMSC-001-W3-R3
Parent-Task: TB-TMAR-PRICING-AMSC-001-W3-R2
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE | CERTIFICATION_BLOCKED
Summary: <bounded summary>
Starting-HEAD-State: 7159C8F7 | DIVERGED
Structure-Authority-State: W3_R2_7159C8F7 | CONFLICT
Prior-W3-State: SUPERSEDED | CONFLICT
Certification-State: COMPLETE_REFERENCE_PATTERN | BLOCKED
Structure-State: CERTIFIED | REGRESSED
Http-Applicability-State: INTERNAL_ONLY_NO_ENDPOINTS | REGRESSED
Pricing-Endpoints-Project-State: ABSENT | PRESENT
Pricing-Http-Route-State: ZERO | NONZERO
Pricing-Project-Count-State: EXACT_5 | CONFLICT
Solution-Grouping-State: CANONICAL_5 | CONFLICT
Folder-Granularity-State: PROFESSIONAL_SHALLOW | REGRESSED
Path-Namespace-State: EXACT | REGRESSED
Physical-Copy-State: CLEAN | REGRESSED
Root-Allowlist-State: ENFORCED | REGRESSED
Stable-Error-State: EXACT_11 | CONFLICT
Descriptor-State: EXACT_11 | CONFLICT
Localization-State: EXACT_11_EN_11_FA | CONFLICT
Presentation-Registration-State: INFRASTRUCTURE_MODULE_EXACTLY_ONCE | CONFLICT
Typed-Fault-State: CANONICAL_NO_MESSAGE_CLASSIFICATION | REGRESSED
Cross-Module-Boundary-State: LEGAL_CONTRACTS_ONLY_BOTH_DIRECTIONS | REGRESSED
Foreign-App-Infra-Domain-Coupling-State: ZERO | REGRESSED
Cross-Module-Join-State: NONE | REGRESSED
Persistence-State: OWN_PRICING_SCHEMA_OUTBOX_MIGRATION | REGRESSED
Host-Authority-State: ZERO_BUSINESS_ZERO_HTTP | REGRESSED
Schema-Migration-State: UNCHANGED | REGRESSED
Manifest-Certification-State: CERTIFIED_5_PROJECT_INTERNAL_ONLY | CONFLICT
StructureLock-Pricing-State: PRESENT_ONCE | CONFLICT
Global-Host-Checkpoint-State: PRESERVED | REGRESSED
Guards-Weakened-State: NONE | NONZERO
Baselines-Widened-State: NONE | NONZERO
Post-Cert-Recovery-State: REQUIRED | CONFLICT
Evidence-State: COMPLETE | INCOMPLETE
Commit-SHA: <sha>
HEAD-Equals-Origin-Main: YES | NO
Working-Tree-State: CLEAN | CLEAN_EXCEPT_PREEXISTING_UNRELATED_ARTIFACTS | DIRTY_UNEXPECTED
User-Work-Preserved: YES | NO
Automatic-Next-Implementation-Task-State: NONE
Workflow-Stop-State: USER_REVIEW_PRICING_AMSC_001_W3_R3
STOP
END_TOOBA_WORKER_RESULT

STOP RULE
STOP completely after result.
Do NOT create recovery reconciliation automatically.
Do NOT start another module.
Wait for Architect review.

END_TOOBA_TASK