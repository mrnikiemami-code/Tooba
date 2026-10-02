PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-CONFIGURATION-AMC-001
Parent-Task: TB-TMAR-HOST-PERSISTENCE-AMC-001-W2-CERT
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_CONFIGURATION_ANALYZE
Title: Analyze Host/Configuration control-plane options, registry construction, production fail-fast validation, structural cohesion, and foreign contract seams
Estimated-Time-Minutes: 14
Hard-Timebox-Minutes: 18

ARCHITECT REVIEW STATE

Parent Persistence certification:
ACCEPTED

Architect independently verified current main:

HOST_PERSISTENCE_AMC_CERTIFIED
HOST_PERSISTENCE_PLATFORM_BOUNDARY_CERTIFIED
exact 1-file / 1-type Persistence tree
exact namespace Tooba.Host.Persistence
secret leakage ZERO
PlatformHttpException fail-closed boundary certified
production change in Persistence CERT = ZERO
implementation authority remains Outbox W1:
382ef10af3a5eb49f519e49cb399809b19844bbc
Outbox/Observability/Messaging/Health/MultiTenancy/Errors/Security/Admin certifications preserved

NEXT ACTIVE HOST UNIT

src/backend/Host/Tooba.Host/Configuration/

Current repository snapshot shows exactly one production file:

Configuration/

ToobaPlatformOptions.cs

Current top-level production types discovered in that file:

ToobaPlatformOptions
StoreCommerceOptions
MarketplaceOptions
SingleStoreOptions
TenantRecordOptions
PostgreSqlOptions
TenantRecord
ControlPlaneRegistry
PlatformOptionsValidator

Current namespace:
Tooba.Host

Physical path:
/Configuration/

Likely path↔namespace violation:
target if retained = Tooba.Host.Configuration

Current file has 9 top-level types:
major cohesion debt likely.

Current imports include:

Microsoft.Extensions.Options
Tooba.BuildingBlocks
Tooba.Offer.Contracts.Dtos
Tooba.StoreContext.Contracts.Current

SKILL — MANDATORY

Apply:

.cursor/skills/tooba-architecture-analyze/SKILL.md

ANALYSIS ONLY.

Do NOT edit production code.
Do NOT migrate.
Do NOT certify.
Do NOT start Host root final certification.
Do NOT start another Host folder.
Do NOT open module recovery.

PROTECTED STATE

Must remain:

HOST_PERSISTENCE_AMC_CERTIFIED
HOST_OUTBOX_AMC_CERTIFIED
HOST_OBSERVABILITY_AMC_CERTIFIED
HOST_MESSAGING_AMC_CERTIFIED
HOST_HEALTH_AMC_CERTIFIED
HOST_MULTITENANCY_AMC_CERTIFIED
HOST_ERRORS_AMC_CERTIFIED
HOST_SECURITY_AMC_CERTIFIED
HOST_ADMIN_FULLY_CERTIFIED
frontend frozen
Checkout paused unchanged

MANDATORY ANALYSIS

EXACT TREE / TYPE ENUMERATION

Enumerate:
src/backend/Host/Tooba.Host/Configuration/

Record:

exact production file count
exact filename(s)
exact top-level type count
nested type count
type names
visibility
namespace
consumers

Expected current:
1 file / 9 top-level types

PER-TYPE DISPOSITION

Classify each separately:

ToobaPlatformOptions
StoreCommerceOptions
MarketplaceOptions
SingleStoreOptions
TenantRecordOptions
PostgreSqlOptions
TenantRecord
ControlPlaneRegistry
PlatformOptionsValidator

Allowed dispositions:

KEEP_AS_GLOBAL_HOST_CONFIGURATION_PLATFORM
KEEP_AS_GLOBAL_HOST_CONTROL_PLANE_SNAPSHOT
KEEP_AS_GLOBAL_HOST_CONFIGURATION_VALIDATOR
MOVE_TO_STORECONTEXT
MOVE_TO_PERSISTENCE
MOVE_TO_MULTITENANCY
MOVE_TO_BUILDINGBLOCKS
DEAD_COMPATIBILITY_RESIDUE
MUST_SPLIT
BLOCKED_NEEDS_ARCHITECT_DECISION

Do not classify file/folder as a whole first.

PATH ↔ NAMESPACE

Current:
namespace Tooba.Host

Target if retained:
Tooba.Host.Configuration

Determine all required consumer import changes.

No migration in Analyze.

FILE COHESION / TARGET TREE

Assess whether one top-level type per file should be enforced.

Likely canonical target tree if all retained:

Configuration/

ToobaPlatformOptions.cs
StoreCommerceOptions.cs
MarketplaceOptions.cs
SingleStoreOptions.cs
TenantRecordOptions.cs
PostgreSqlOptions.cs
TenantRecord.cs
ControlPlaneRegistry.cs
PlatformOptionsValidator.cs

Do not force if strong reason exists to group a tiny immutable type pair, but architecture default is one top-level type per file.

PROGRAM BINDING / DI

Trace exact Program registration:

AddOptions<ToobaPlatformOptions>()
Bind(section)
ValidateOnStart()
IValidateOptions<ToobaPlatformOptions>
ControlPlaneRegistry singleton build
any consumers of raw ToobaPlatformOptions
any consumers of ControlPlaneRegistry

Record lifetimes and startup semantics.

CONFIGURATION AUTHORITY

Define exact ownership among:

raw config binding
validation
normalized registry
connection reference catalog
trusted proxies
edition/deployment
tenant host allowlist
StoreCommerce defaults

Determine which belong in Configuration and which are only referenced by other Host folders.

RAW OPTIONS VS NORMALIZED REGISTRY

Audit semantic boundary:

ToobaPlatformOptions = raw mutable bind model
ControlPlaneRegistry = normalized immutable runtime snapshot

Verify:

request/runtime code prefers registry where appropriate
raw options are not mutated after startup
registry is not a tenant database/control-plane source of truth
no hidden runtime reload behavior
EDITION PARSING

Audit:
Marketplace
SingleStore
Unset
unsupported values
case sensitivity
normalization

Determine:

fail-fast semantics
production requirements
test/development behavior
DEPLOYMENT ID

Audit:

default blank
production requirement if any
telemetry/runtime consumers
uniqueness assumptions
whether blank is allowed outside production
TRUSTED PROXIES

Audit:
ToobaPlatformOptions.TrustedProxies

Trace:

Program parsing
IPAddress validation
duplicate handling
empty behavior
forwarded headers enablement

Determine:

whether invalid proxy config fails fast
whether trusted proxy strings are normalized
whether this belongs in PlatformOptionsValidator or Program
MARKETPLACE OPTIONS

Audit:
MarketplaceOptions.ConnectionReference

Trace:

registry build
production validation
Persistence resolver relationship
MultiTenancy
Outbox
Health
Messaging if applicable

Determine:

required in production?
fail-fast relation
connection reference only, never raw connection string
SINGLE-STORE OPTIONS

Audit:
SingleStoreOptions.Tenants

Determine:

zero tenants allowed in development/testing?
production requirement
duplicate TenantId
duplicate host
invalid status
missing host
PrimaryDomain handling
active/disabled/suspended behavior
TENANT RECORD OPTIONS

Audit all fields:

TenantId
DisplayName
Status
ConnectionReference
ThemeReference
DefaultMarketReference
StoreCommerce
PrimaryDomain
Hosts

For each:

required/optional
normalization
validation
sensitive/non-sensitive
runtime owner
HOST NORMALIZATION

Inspect exact host normalization implementation.

Verify:

trim
lowercase/case-insensitive semantics
port removal if applicable
IPv6 handling
punycode/IDN handling
trailing dot
scheme/path rejection
duplicate host detection
PrimaryDomain inclusion
invalid host behavior

This is critical because MultiTenancy routing depends on it.

TENANT ID NORMALIZATION

Audit:

trim
empty rejection
case sensitivity
uniqueness comparer
whether same ID with casing variation is allowed
TENANT STATUS PARSING

Audit:
Active
Disabled
Suspended
unknown values

Determine:

case sensitivity
fail-fast behavior
runtime semantics
CONNECTION REFERENCES

Audit:
PostgreSqlOptions.ConnectionReferences

Verify:

dictionary comparer
key restrictions (: note)
duplicate reference behavior
blank key/value
syntax validation
production validation scope
secret exposure prevention

Compare with certified Persistence resolver.

LEGACY ROOT CONNECTION STRING

PostgreSqlOptions.ConnectionString

Audit all production consumers.

Classify:

active compatibility
dead compatibility residue
tests only
safe to remove later
must remain for migration compatibility

Do NOT remove in Analyze.

STORE COMMERCE RAW OPTIONS

Audit:
StoreCommerceOptions

Market
DefaultCurrency
SalesChannel

Trace both:

Marketplace deployment-level
SingleStore tenant-level

Determine:

fallback from TenantRecordOptions.DefaultMarketReference
currency normalization
sales channel normalization
fail-fast production requirements
whether Configuration is correct owner vs StoreContext
STORECONTEXT BOUNDARY

Current import:
Tooba.StoreContext.Contracts.Current

Audit exact use of:
StoreCommerceContext

Verify:

Contracts-only
no StoreContext Application/Infrastructure/Domain
Configuration builds immutable platform contract only
no business defaults invented beyond explicit control-plane configuration
OFFER CONTRACT DEPENDENCY — CRITICAL

Current file imports:
Tooba.Offer.Contracts.Dtos

Find exact type(s) used and why.

Determine:

is Configuration depending on Offer contract merely for a stable sales-channel/value enum?
is that cross-module ownership appropriate?
should a neutral BuildingBlocks/StoreContext contract own the value instead?
does this make Host Configuration coupled to a business module?
is dependency one-way and Contracts-only?
is it a blocker for Host Configuration certification?

Do NOT move Offer types in Analyze.

PLATFORM OPTIONS VALIDATOR MESSAGE USAGE

Current validator catches:
InvalidOperationException ex
and returns:
ValidateOptionsResult.Fail(ex.Message)

This is message propagation, but not message classification.

Classify:

startup operator validation acceptable?
internal exception prose becomes startup validation text
any sensitive values embedded?
any risk of raw connection string or secrets appearing in exception messages from BuildRegistry?

Distinguish clearly from forbidden ex.Message classification.

PRODUCTION REQUIREMENTS

Audit exact ValidateProductionRequirements behavior:

edition not Unset
SingleStore at least one tenant
all configured required connection references present
StoreCommerce completeness
any DeploymentId requirement
trusted proxy validity
tenant host validity
active vs disabled tenant connection refs

List all gaps.

CONNECTION REFERENCE COLLECTION POLICY

Inspect CollectConfiguredConnectionReferences.

Verify exact policy:

Marketplace reference
which SingleStore tenant statuses
messaging ref if reused
duplicate elimination
health parity

Compare:

Health = all configured refs
MultiTenancy = active-serving
Outbox = active tenants
startup validation = ? exact

Decide if current policy is intentional.

BUILD REGISTRY

Audit PlatformOptionsValidator.BuildRegistry.

Verify:

pure deterministic transform
no service locator
no I/O
no DB
no environment reads
no runtime mutation
fail-closed duplicate/invalid values
normalized dictionaries immutable/read-only where possible
REGISTRY IMMUTABILITY

Audit types and collections:

required init
IReadOnlyDictionary
backing mutable dictionaries/lists after construction
whether raw references can still mutate registry indirectly

Classify:
IMMUTABLE_ENOUGH / MUTABILITY_DEBT

CONTROL PLANE SEMANTICS

ControlPlaneRegistry comment says:
not production tenant source, only config snapshot.

Verify runtime usage in:

MultiTenancy
Outbox
Health
Configuration/Persistence
Development

Ensure no false claim that this is a durable central control plane.

STARTUP EXCEPTION SAFETY

Audit every throw from BuildRegistry/helper methods.

Verify:

no raw connection string
no password/token
no sensitive secret
tenant IDs/hosts/operator config may be present as safe operator metadata
no stack trace persisted
HARD-CODED STARTUP TEXT

Classify validation strings as:
startup/operator configuration text.

Required:
user-facing HTTP/runtime business text = ZERO.

EXCEPTION MESSAGE CLASSIFICATION

Required ZERO:

branch on ex.Message
Contains/StartsWith matching
infer semantic code from prose

Returning ex.Message as validation failure is NOT classification, but document it explicitly.

FOREIGN LAYERS / BUSINESS AUTHORITY

Required ZERO:

foreign Application
foreign Infrastructure
foreign Domain
DbContext
repository
business command
business mutation

Contracts-only dependencies must be explicitly justified.

DIRECT MODULE CONTRACT INVENTORY

Enumerate every non-BuildingBlocks module contract dependency from Configuration.

Expected:

StoreContext.Contracts
Offer.Contracts

Determine whether both are architecturally legitimate.

SECURITY / SECRET HANDLING

Audit:

connection strings
connection references
deployment ID
tenant IDs
hosts
trusted proxy IPs

Required:
connection string never logged/presented in validation error.
Reference names may appear only in operator startup validation if explicitly acceptable.

TEST / GUARD INVENTORY

Map focused tests for:

edition parsing
marketplace config
SingleStore config
tenant status
duplicate tenant ID
duplicate hosts
invalid host
PrimaryDomain
connection refs
StoreCommerce
production fail-fast
trusted proxies
BuildRegistry
Offer/StoreContext boundary
secret leakage
path/namespace/file cohesion

Identify missing coverage.

HISTORICAL CLAIMS

Inspect:

Configuration foundation docs
platform options fail-fast task history
StoreCommerce fail-fast history
current SoT
prior Host Configuration certification claims if any

Classify:
CURRENT / HISTORICAL / FOUNDATION_ONLY / NOT_FOLDER_CERTIFIED

DECISIVE TARGET PLAN

Recommend the fewest safe waves <=20 min each.

Likely possibilities:

A. W1 structural split + namespace only, W2-CERT
if semantics are already clean.

B. W1 structure + bounded validation/hygiene repair, W2-CERT
if missing production validation is small.

C. W1 structure, W2 semantic repair, W3-CERT
only if Offer contract ownership or registry mutability is a material blocker.

Do not create extra waves for cosmetic reasons.

HOST-ONLY SCOPE LOCK

No module recovery.
No Offer migration.
No StoreContext recovery.
No root final certification yet.

PRODUCTION CHANGE RULE

Analyze production change:
ZERO

Docs/evidence/SoT only.

FOCUSED VALIDATION

No solution-wide tests.
No production build unless required to resolve type/consumer ambiguity.

EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-CONFIGURATION-AMC-001/

Required:

analyze.md
type-dispositions.md
binding-registry.md
tenant-host-normalization.md
production-validation.md
connection-secret-safety.md
storecommerce-boundary.md
offer-contract-boundary.md
path-namespace-cohesion.md
tests-guards.md
historical-claims.md
migration-plan.md
recovery.md

Persist exact task:

docs/ai/tasks/TB-TMAR-HOST-CONFIGURATION-AMC-001.task.md

RECOVERY / SOT

Analysis-only.
Do NOT advance implementation SHA.

Record:

currentHostCheckpoint = Configuration
mode = ANALYSIS_ONLY_NOT_MIGRATED_NOT_CERTIFIED
productionFileCount = actual
productionTypeCount = actual
pathNamespaceState
fileCohesionState
configurationAuthorityState
registryState
tenantHostNormalizationState
productionValidationState
connectionSecretSafetyState
storeCommerceBoundaryState
offerContractBoundaryState
legacyConnectionStringState
recommendedWaveCount
recommendedNextTask
Persistence certification = PRESERVED
Outbox certification = PRESERVED
Observability certification = PRESERVED
Messaging certification = PRESERVED
Health certification = PRESERVED
MultiTenancy certification = PRESERVED
Errors certification = PRESERVED
Security certification = PRESERVED
Admin certification = PRESERVED
automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_CONFIGURATION_AMC_001
staleCurrentPointerState = ZERO
nextHostFolderStarted = false

Latest accepted implementation remains:
382ef10af3a5eb49f519e49cb399809b19844bbc

GIT

Latest origin/main.
No reset.
No clean.
No rebase.
No force push.

Docs-only task/evidence/SoT stamp allowed.
No production code.

Preserve unrelated user work.

SUCCESS CRITERIA

PASS only if:

exact Configuration tree/types enumerated
all 9 types dispositioned
path/namespace debt classified
file cohesion target decided
Program binding/DI mapped
raw options vs normalized registry semantics proven
edition/deployment/trusted proxy behavior audited
tenant ID/status/host normalization audited
Marketplace/SingleStore config semantics audited
connection reference policy audited
legacy root ConnectionString classified
StoreCommerce ownership and fail-fast semantics audited
StoreContext Contracts-only boundary proven
Offer.Contracts dependency decisively classified
ex.Message startup propagation distinguished from classification
production validation completeness audited
BuildRegistry purity/immutability audited
sensitive startup error leakage audited
foreign App/Infra/Domain/DbContext zero
business authority classified
tests/guards mapped
historical claims reconciled
fewest safe waves proposed
protected certs preserved
production change ZERO
implementation SHA unchanged
automatic next NONE
STOP

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-CONFIGURATION-AMC-001
Parent-Task: TB-TMAR-HOST-PERSISTENCE-AMC-001-W2-CERT
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Mode-State:
Active-Host-Folder:
Production-File-Count:
Production-Type-Count:
File-Enumeration-State:
ToobaPlatformOptions-State:
StoreCommerceOptions-State:
MarketplaceOptions-State:
SingleStoreOptions-State:
TenantRecordOptions-State:
PostgreSqlOptions-State:
TenantRecord-State:
ControlPlaneRegistry-State:
PlatformOptionsValidator-State:
Type-Disposition-State:
Path-Namespace-State:
File-Cohesion-State:
Program-Binding-State:
DI-Lifetime-State:
Configuration-Authority-State:
RawOptions-State:
Registry-State:
Edition-Parsing-State:
DeploymentId-State:
TrustedProxies-State:
Marketplace-Options-State:
SingleStore-Options-State:
TenantRecordOptions-Validation-State:
TenantId-Normalization-State:
TenantStatus-Parsing-State:
Host-Normalization-State:
PrimaryDomain-State:
ConnectionReferences-State:
Legacy-Root-ConnectionString-State:
StoreCommerce-State:
StoreContext-Boundary-State:
Offer-Contract-Boundary-State:
Validator-ExceptionMessage-State:
Production-Validation-State:
ConnectionReference-Collection-State:
BuildRegistry-State:
Registry-Immutability-State:
ControlPlane-Semantics-State:
Startup-Exception-Safety-State:
Hardcoded-Runtime-Text-State:
Exception-Message-Classification-State:
Sensitive-Data-State:
Direct-Module-Contract-State:
Contracts-Boundary-State:
Foreign-Application-Dependency-State:
Foreign-Infrastructure-Dependency-State:
Foreign-Domain-Dependency-State:
Foreign-DbContext-State:
Business-Authority-State:
Guard-Impact-State:
Historical-Claims-State:
Recommended-Wave-Count:
Recommended-Next-Task:
Production-Code-Change-State:
Host-Persistence-Certification-State:
Host-Outbox-Certification-State:
Host-Observability-Certification-State:
Host-Messaging-Certification-State:
Host-Health-Certification-State:
Host-MultiTenancy-Certification-State:
Host-Errors-Certification-State:
Host-Security-Certification-State:
Host-Admin-Certification-State:
Recovery-State:
Last-Accepted-Implementation-Commit-State:
Automatic-Next-Implementation-Task-State:
Workflow-Stop-State:
Evidence-Path:
Commit-SHA:
Push-State:
HEAD-Equals-Origin-Main:
Working-Tree-Tracked-State:
User-Work-Preserved:
STOP
END_TOOBA_WORKER_RESULT

STOP COMPLETELY AFTER RESULT.

Do not start Configuration W1.
Do not start Host root final certification.
Do not open module recovery.

Wait for Architect/user review.

END_TOOBA_TASK