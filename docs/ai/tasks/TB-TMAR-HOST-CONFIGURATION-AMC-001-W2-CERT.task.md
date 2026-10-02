PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-CONFIGURATION-AMC-001-W2-CERT
Parent-Task: TB-TMAR-HOST-CONFIGURATION-AMC-001-W1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_CONFIGURATION_CERTIFY
Title: Certify Host/Configuration as fail-fast process control-plane configuration boundary
Estimated-Time-Minutes: 10
Hard-Timebox-Minutes: 14

ARCHITECT REVIEW STATE

W1:
ACCEPTED FOR CERTIFICATION ENTRY

Architect independently verified current main:

Implementation commit:
32719977bc6408490fe5945d75dedaa5c2f7af4c

Current Configuration production truth:

exact 9 files / 9 top-level types
namespace exact Tooba.Host.Configuration
one top-level type per file
legacy PostgreSqlOptions.ConnectionString removed
TrustedProxies validation is fail-fast
Program uses IPAddress.Parse; no silent TryParse skip
non-empty invalid PrimaryDomain fails fast
AddOptions + Bind + ValidateOnStart preserved
BuildRegistry remains pure/deterministic zero I/O
StoreContext boundary Contracts-only
Offer dependency Contracts-only through SalesChannel enum
foreign App/Infra/Domain/DbContext zero
business authority limited to process configuration/control-plane snapshot
Persistence/Outbox/Observability/Messaging/Health/MultiTenancy/Errors/Security/Admin certifications preserved

SKILL — MANDATORY

Apply:
.cursor/skills/tooba-architecture-certify/SKILL.md

CERTIFY ONLY.

No production migration.
No production repair unless material defect found.
No next Host folder.
No Host root/final certification yet.
No module recovery.

If material production defect is found:
Status = INCOMPLETE
Production-Repair-Required-State = YES
STOP.

PRIMARY CERTIFICATION SCOPE

src/backend/Host/Tooba.Host/Configuration/

Expected exact tree:

Configuration/

ControlPlaneRegistry.cs
MarketplaceOptions.cs
PlatformOptionsValidator.cs
PostgreSqlOptions.cs
SingleStoreOptions.cs
StoreCommerceOptions.cs
TenantRecord.cs
TenantRecordOptions.cs
ToobaPlatformOptions.cs

Expected:

9 production files
9 top-level types
one type per file
namespace Tooba.Host.Configuration

Certification labels on PASS:
HOST_CONFIGURATION_AMC_CERTIFIED
HOST_CONFIGURATION_PLATFORM_BOUNDARY_CERTIFIED

MANDATORY CERTIFICATION AUDITS

EXACT TREE / NAMESPACE / COHESION

Verify:

exact 9 .cs files
exact filenames
exact 9 top-level types
one type per file
exact namespace
no duplicate legacy Tooba.Host copies
no alias/shim/TypeForwardedTo
no unexpected subfolders
TYPE OWNERSHIP

Certify all nine:

ToobaPlatformOptions
StoreCommerceOptions
MarketplaceOptions
SingleStoreOptions
TenantRecordOptions
PostgreSqlOptions
TenantRecord
ControlPlaneRegistry
PlatformOptionsValidator

As Host process configuration/control-plane platform types.

PROGRAM BINDING

Verify exact:

AddOptions<ToobaPlatformOptions>()
Bind(ToobaPlatformOptions.SectionName)
ValidateOnStart()
singleton IValidateOptions<ToobaPlatformOptions>, PlatformOptionsValidator
singleton ControlPlaneRegistry built from PlatformOptionsValidator.BuildRegistry

No duplicate binding/registry build path.

TRUSTED PROXIES

Certify:

blank list allowed
configured blank element rejected
malformed IPv4/IPv6 rejected
valid IPv4 accepted
valid IPv6 accepted
Program uses IPAddress.Parse after validation
no silent skip
no untrusted wildcard fallback
PRIMARY DOMAIN

Certify:

null/blank optional
valid normalized via HostNormalizer
added to host list if missing
invalid non-empty fails
duplicate mapping fails
first host remains fallback when primary absent
EDITION PARSING

Certify:

Marketplace
SingleStore / Single-Store
Unset
unsupported value fails
no silent coercion
TENANT STATUS

Certify:

Active
Disabled
Suspended
unknown status fails
TENANT ID / HOST MAP

Verify:

TenantId required
ConnectionReference required
duplicate TenantId fails
no host mappings fails
invalid host fails
duplicate normalized host mapping fails
current ordinal TenantId semantics preserved
CONNECTION REFERENCES

Certify:

PostgreSQL.ConnectionReferences is case-insensitive
registry stores logical ConnectionReference only
no raw connection string in registry
no connection string in validation errors
legacy root PostgreSQL.ConnectionString remains absent
no production consumer of removed field
PRODUCTION FAIL-FAST

Verify in Production:

Edition cannot remain Unset
SingleStore requires at least one tenant
configured edition connection refs must exist/nonblank
effective StoreCommerce required for Marketplace
effective StoreCommerce required for active SingleStore tenants
no late default invention
STORE COMMERCE

Certify:

Marketplace uses deployment-level StoreCommerce
SingleStore uses tenant-specific StoreCommerce
Market may inherit DefaultMarketReference
DefaultCurrency requires explicit configured value
SalesChannel requires canonical enum name
no module default fallback
no normalization beyond trim/inheritance rule
OFFER CONTRACT COUPLING

Verify only:
Tooba.Offer.Contracts.Dtos.SalesChannel

Required ZERO:

Offer.Application
Offer.Infrastructure
Offer.Domain

Classification:
CONTRACTS_ONLY_ACCEPTED_PLATFORM_ENUM_COUPLING

STORECONTEXT BOUNDARY

Verify only:
Tooba.StoreContext.Contracts.Current

Required ZERO:

StoreContext.Application
StoreContext.Infrastructure
StoreContext.Domain
BUILDREGISTRY PURITY

Certify:

deterministic for same options
no DB
no network
no file I/O
no IServiceProvider
no service locator
no module callouts
CONTROL PLANE SNAPSHOT

Certify:

process-local startup snapshot
not durable control-plane DB
no runtime mutable business authority
singleton snapshot usage
no IOptionsMonitor reload semantics
REGISTRY IMMUTABILITY

Audit current public/mutable surfaces.
Certify current shape as startup-frozen process snapshot if no active mutation path exists.

If a live mutation path exists:
INCOMPLETE.

DEPLOYMENT ID

Verify:

explicit trimmed value used when provided
fallback remains edition string
not treated as tenant id
no sensitive value
HARD-CODED TEXT

Startup/operator validation prose is allowed.
User-facing runtime text required ZERO.

No localization ceremony for startup validator errors.

EXCEPTION MESSAGE CLASSIFICATION

Required ZERO runtime classification by message.

Validator may return ex.Message from its own deterministic InvalidOperationException branches only as startup validation propagation; no business branching by arbitrary exception text.

SENSITIVE DATA

Required ZERO exposure of:

connection string
password
credential
token
secret
Authorization/cookie
payload

Connection reference names in startup operator errors are acceptable; raw values are not.

FOREIGN LAYERS / BUSINESS AUTHORITY

Required ZERO:

foreign Application
foreign Infrastructure
foreign Domain
DbContext
repository
business command
business workflow

Business authority:
ZERO except process control-plane configuration normalization/validation.

ACTIVE CONSUMERS

Verify all nine types are actively part of binding/validation/registry/consumption or intentionally nested config DTO flow.
No dead production type.

MIGRATION RUNNER COMPATIBILITY

Because namespace split touched MigrationRunner consumers:
verify focused compile/runtime config contract remains intact.
No MigrationRunner redesign.

PERSISTENCE COMPATIBILITY

Verify DatabaseConnectionResolver continues consuming ToobaPlatformOptions correctly.
HOST_PERSISTENCE_AMC_CERTIFIED preserved.

MULTITENANCY COMPATIBILITY

Verify TenantResolutionMiddleware consumes ControlPlaneRegistry semantics unchanged.
HOST_MULTITENANCY_AMC_CERTIFIED preserved.

HEALTH / OUTBOX / MESSAGING COMPATIBILITY

Verify their configuration-facing consumers compile and behavior remains unchanged except intended trusted-proxy/primary-domain fail-fast behavior.

W1 GUARD

Do NOT weaken:
HostConfigurationAmcW1GuardTests

DURABLE CERT GUARD

Create:
HostConfigurationAmcCertGuardTests

Lock at minimum:

exact 9-file/9-type tree
one type per file
exact namespace
Program AddOptions/Bind/ValidateOnStart/validator/registry
TrustedProxy fail-fast + Program Parse
PrimaryDomain fail-fast
no legacy root ConnectionString
production fail-fast requirements
StoreCommerce rules
Offer.Contracts only
StoreContext.Contracts only
no foreign App/Infra/Domain/DbContext
BuildRegistry zero I/O/service locator
protected certification labels in SoT
implementation SHA remains W1 SHA
FOCUSED TESTS

Run:

HostConfigurationAmcW1GuardTests
HostConfigurationAmcCertGuardTests
HostConfigurationAmcW1BehaviorTests
PlatformOptionsValidatorTests
PersistenceAmcCertBehaviorTests
TenantResolutionTests
HostReadinessEvaluatorW1Tests
OutboxAmcW1BehaviorTests
MigrationRunner focused tests
TmarDurableGuardTests

No solution-wide tests.

BUILD

Focused:

Tooba.Host
Tooba.Host.Tests
Tooba.MigrationRunner
Tooba.MigrationRunner.Tests

No solution-wide build.

PROTECTED CERTIFICATIONS

Must preserve:

HOST_PERSISTENCE_AMC_CERTIFIED
HOST_OUTBOX_AMC_CERTIFIED
HOST_OBSERVABILITY_AMC_CERTIFIED
HOST_MESSAGING_AMC_CERTIFIED
HOST_HEALTH_AMC_CERTIFIED
HOST_MULTITENANCY_AMC_CERTIFIED
HOST_ERRORS_AMC_CERTIFIED
HOST_SECURITY_AMC_CERTIFIED
HOST_ADMIN_FULLY_CERTIFIED
PRODUCTION CHANGE RULE

Expected:
Production-Code-Change-State = ZERO
Production-Repair-Required-State = NONE

Cert may modify only:

tests/guards
docs/evidence
Recovery/SoT metadata
RECOVERY HYGIENE

Verify:

valid JSON
no duplicate properties
no stale pointer
no automatic next folder
currentHostCheckpoint remains Configuration
implementation SHA remains W1
cert/docs stamp separate
EVIDENCE

Create:
docs/evidence/TB-TMAR-HOST-CONFIGURATION-AMC-001-W2-CERT/

Required:

certification-summary.md
physical-tree.md
binding-failfast.md
trusted-proxies-primary-domain.md
control-plane-storecommerce.md
boundary-contracts.md
validation.md
recovery.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-CONFIGURATION-AMC-001-W2-CERT.task.md

RECOVERY / SOT

On PASS:

Top-level:

lastAcceptedTask = TB-TMAR-HOST-CONFIGURATION-AMC-001-W2-CERT
lastAcceptedCommit remains:
32719977bc6408490fe5945d75dedaa5c2f7af4c
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
latestAcceptedImplementationWave = TB-TMAR-HOST-CONFIGURATION-AMC-001-W1
currentHostCheckpoint = Configuration
nextTask = USER_REVIEW_HOST_CONFIGURATION_AMC_001_W2_CERT
workflowStop = USER_REVIEW_HOST_CONFIGURATION_AMC_001_W2_CERT
automaticNextImplementationTask = NONE
staleCurrentPointerState = ZERO
nextHostFolderStarted = false

Add/update:
hostConfigurationAmc001W2Cert

Required:

certificationState = HOST_CONFIGURATION_AMC_CERTIFIED
boundaryState = HOST_CONFIGURATION_PLATFORM_BOUNDARY_CERTIFIED
productionFileCount = 9
productionTypeCount = 9
pathNamespace = EXACT_Tooba.Host.Configuration
fileCohesion = ONE_TOP_LEVEL_TYPE_PER_FILE_CERTIFIED
binding = VALIDATE_ON_START_CERTIFIED
trustedProxies = FAIL_FAST_CERTIFIED
primaryDomain = FAIL_FAST_CERTIFIED
legacyRootConnectionString = ZERO
editionParsing = FAIL_CLOSED_CERTIFIED
tenantStatus = FAIL_CLOSED_CERTIFIED
connectionReferences = LOGICAL_REFERENCE_CATALOG_CERTIFIED
storeCommerce = FAIL_FAST_CONTROL_PLANE_CERTIFIED
buildRegistry = PURE_DETERMINISTIC_CERTIFIED
controlPlaneSnapshot = PROCESS_LOCAL_CERTIFIED
offerContractBoundary = CONTRACTS_ONLY_CERTIFIED
storeContextBoundary = CONTRACTS_ONLY_CERTIFIED
sensitiveData = ZERO
messageClassification = ZERO
foreignApplication = ZERO
foreignInfrastructure = ZERO
foreignDomain = ZERO
foreignDbContext = ZERO
businessAuthority = ZERO_CONTROL_PLANE_CONFIG_ONLY
productionRepairRequired = false
productionCodeChange = ZERO
implementationCommit = 32719977...
all protected cert states = PRESERVED
automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_CONFIGURATION_AMC_001_W2_CERT
GIT

Latest origin/main.
No reset.
No clean.
No rebase.
No force push.

Preserve unrelated user work.
Push only on PASS.

SUCCESS CRITERIA

PASS only if:

exact 9-file/9-type tree
exact namespace
one type per file
all nine ownership dispositions certified
Program binding exact
TrustedProxies fail-fast
PrimaryDomain fail-fast
edition/status/host/tenant fail-closed
legacy root ConnectionString zero
connection reference catalog secret-safe
Production StoreCommerce fail-fast
BuildRegistry pure/deterministic
Offer.Contracts only
StoreContext.Contracts only
no foreign App/Infra/Domain/DbContext
business authority limited to control-plane configuration
sensitive leakage zero
message classification zero
focused builds/tests PASS
durable cert guard PASS
prior Host certs preserved
production repair NONE
production change ZERO
recovery hygiene exact
implementation SHA remains 32719977...
automatic next NONE
STOP

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-CONFIGURATION-AMC-001-W2-CERT
Parent-Task: TB-TMAR-HOST-CONFIGURATION-AMC-001-W1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Certification-State:
Boundary-Certification-State:
Configuration-Production-File-Count:
Configuration-Production-Type-Count:
Exact-Tree-State:
Path-Namespace-State:
File-Cohesion-State:
ToobaPlatformOptions-State:
StoreCommerceOptions-State:
MarketplaceOptions-State:
SingleStoreOptions-State:
TenantRecordOptions-State:
PostgreSqlOptions-State:
TenantRecord-State:
ControlPlaneRegistry-State:
PlatformOptionsValidator-State:
Program-Binding-State:
DI-Lifetime-State:
TrustedProxies-Validation-State:
Program-TrustedProxy-State:
PrimaryDomain-State:
Edition-Parsing-State:
DeploymentId-State:
TenantId-Semantics-State:
TenantStatus-State:
Host-Normalization-State:
ConnectionReferences-State:
Legacy-Root-ConnectionString-State:
Production-Validation-State:
StoreCommerce-State:
BuildRegistry-State:
ControlPlane-Snapshot-State:
Registry-Immutability-State:
Offer-Contract-Boundary-State:
StoreContext-Boundary-State:
MigrationRunner-Compatibility-State:
Persistence-Compatibility-State:
MultiTenancy-Compatibility-State:
Health-Outbox-Messaging-Compatibility-State:
Hardcoded-User-Facing-Text-State:
Exception-Message-Classification-State:
Sensitive-Data-State:
Contracts-Boundary-State:
Foreign-Application-Dependency-State:
Foreign-Infrastructure-Dependency-State:
Foreign-Domain-Dependency-State:
Foreign-DbContext-State:
Business-Authority-State:
Active-Consumer-State:
Durable-Cert-Guard-State:
Host-Persistence-Certification-State:
Host-Outbox-Certification-State:
Host-Observability-Certification-State:
Host-Messaging-Certification-State:
Host-Health-Certification-State:
Host-MultiTenancy-Certification-State:
Host-Errors-Certification-State:
Host-Security-Certification-State:
Host-Admin-Certification-State:
Production-Repair-Required-State:
Production-Code-Change-State:
Focused-Build-State:
Focused-Test-State:
Recovery-State:
Recovery-Hygiene-State:
Last-Accepted-Implementation-Commit-State:
Certification-Commit-State:
Stale-Current-Pointer-State:
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

Do not start Host root/final certification.
Do not start another folder.
Do not modify production code in Cert.
Do not open module recovery.

Wait for Architect/user review.

END_TOOBA_TASK