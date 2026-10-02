PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-CONFIGURATION-AMC-001-W1
Parent-Task: TB-TMAR-HOST-CONFIGURATION-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_CONFIGURATION_STRUCTURE_FAILFAST_HYGIENE
Title: Split Host/Configuration into exact namespace/files and close fail-fast configuration gaps without changing control-plane semantics
Estimated-Time-Minutes: 16
Hard-Timebox-Minutes: 20

ARCHITECT REVIEW STATE

Persistence W2-CERT:
ACCEPTED

Configuration Analyze:
ACCEPTED WITH ARCHITECT EXPANSION

Architect independently verified:

Persistence certification is docs/tests only and production change ZERO
HOST_PERSISTENCE_AMC_CERTIFIED / HOST_PERSISTENCE_PLATFORM_BOUNDARY_CERTIFIED are coherent
Configuration current tree = 1 file / 9 top-level types
current namespace Tooba.Host violates path-derived Tooba.Host.Configuration
one-file/9-type cohesion is unacceptable
all 9 current types have legitimate Host configuration/control-plane snapshot ownership
BuildRegistry is deterministic and zero-I/O
StoreContext dependency is Contracts-only
Offer dependency is Contracts-only (SalesChannel enum) and is accepted for this Host wave
PostgreSqlOptions.ConnectionString has zero production consumers and is dead compatibility residue
Program separately parses TrustedProxies and silently skips invalid IP entries
PrimaryDomain currently has another fail-open edge: a non-empty invalid value can be ignored and silently fall back to first valid Host
latest accepted implementation remains Outbox W1:
382ef10af3a5eb49f519e49cb399809b19844bbc

ARCHITECT DECISIONS FOR W1

Split all 9 existing Configuration types to one file each.
Target namespace = Tooba.Host.Configuration.
Preserve all 9 current ownership decisions; no type moves outside Host/Configuration.
Remove dead legacy PostgreSqlOptions.ConnectionString.
Make TrustedProxies fail-fast: configured non-empty invalid IP must fail options validation/startup.
Remove Program's silent-skip behavior for invalid TrustedProxies.
Make non-empty invalid PrimaryDomain fail-fast instead of silently falling back.
Preserve case-sensitive TenantId identity semantics in this wave; do NOT case-fold or silently trim identity.
Preserve deployment-id fallback semantics.
Preserve all current edition / connection-reference / StoreCommerce semantics.
Do NOT move SalesChannel enum ownership in this wave.
Do NOT redesign registry immutability; current IReadOnly surface remains accepted unless a concrete mutation path is found.
No module production changes except consumer namespace imports strictly required to compile.

SKILL — MANDATORY

Apply:

.cursor/skills/tooba-architecture-migrate/SKILL.md

MIGRATE ONLY.

Do NOT certify Configuration in W1.
Do NOT start Host root/final certification.
Do NOT start another Host folder.
Do NOT open module recovery.

PRIMARY ACTIVE FOLDER

src/backend/Host/Tooba.Host/Configuration/

CURRENT FILE

ToobaPlatformOptions.cs

CURRENT TOP-LEVEL TYPES

ToobaPlatformOptions
StoreCommerceOptions
MarketplaceOptions
SingleStoreOptions
TenantRecordOptions
PostgreSqlOptions
TenantRecord
ControlPlaneRegistry
PlatformOptionsValidator

TARGET TREE

Exactly nine production files:

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

Exactly nine top-level production types.

TARGET NAMESPACE

All nine:
Tooba.Host.Configuration

No alias.
No shim.
No TypeForwardedTo.
No legacy duplicate type.

REQUIRED W1 CHANGES

SPLIT / COHESION

Split one type per file.

Do not alter public/internal visibility beyond what compilation requires.
Expected all remain internal.

PATH ↔ NAMESPACE

Change all Configuration types to:
namespace Tooba.Host.Configuration;

Update consumers/usings only as required.

Expected consumer zones:

Program
Persistence
MultiTenancy
Health
Outbox
Admin / Development / Composition where registry/options are consumed
MigrationRunner
Host tests
MigrationRunner tests

Do not move consumers.

RAW OPTIONS SHAPE

Preserve ToobaPlatformOptions fields:

Edition
DeploymentId
TrustedProxies
Marketplace
SingleStore
PostgreSQL
StoreCommerce

Preserve SectionName = "Tooba".

REMOVE LEGACY ROOT CONNECTION STRING

In PostgreSqlOptions remove dead property:
ConnectionString

Keep only:
ConnectionReferences

Required proof:

zero production consumer before removal
build/test compile after removal
no fallback reintroduced elsewhere

Do NOT migrate this dead property to another type.

TRUSTED PROXIES — FAIL FAST

PlatformOptionsValidator must validate each configured non-empty TrustedProxies entry.

Rule:

each entry must parse as a valid IP address using canonical .NET parsing
invalid/blank configured element => ValidateOptionsResult failure
failure text is startup/operator prose only
no IP value needs to be echoed in failure text; prefer generic index/count-safe message if convenient

Validation should apply whenever TrustedProxies are configured, not Production-only.

Required state:
INVALID_CONFIGURED_TRUSTED_PROXY_FAILS_STARTUP

PROGRAM TRUSTED PROXY CONFIGURATION

Program must no longer silently ignore invalid configured entries.

Preferred bounded implementation:

because options validation guarantees validity, Program may use IPAddress.Parse(proxy) when building KnownProxies; or
use a small explicit throw on failed parse

Forbidden:

if (IPAddress.TryParse(...)) { add } with silent else
silently skipping bad entries

Preserve:

empty list => forwarded headers not enabled
non-empty valid list => configure XForwardedFor + XForwardedProto + XForwardedHost
KnownNetworks.Clear()
KnownProxies.Clear()
add each validated proxy
UseForwardedHeaders() condition based on configured non-empty list

No proxy/network redesign.

PRIMARY DOMAIN — FAIL FAST

Current non-empty invalid PrimaryDomain must NOT silently fall back.

Required behavior:

null/blank PrimaryDomain remains optional
non-empty PrimaryDomain must normalize successfully
if invalid => BuildRegistry throws InvalidOperationException
if valid and not already in Hosts, add it
duplicate normalized host across tenants remains rejected
normalized PrimaryDomain stored in record

Do not alter host normalization rules.

TENANT HOST RULES

Preserve:

every SingleStore tenant requires TenantId
every SingleStore tenant requires ConnectionReference
every tenant requires at least one resulting host mapping
duplicate TenantId rejected
duplicate normalized host rejected
status parse fail-closed
HostNormalizer remains canonical
PrimaryDomain may supply the first/only host if valid
TENANT ID SEMANTICS

Do NOT introduce case-folding or identity normalization in W1.

Preserve current Ordinal dictionary identity semantics.

Do not trim and rewrite TenantId silently.

If tests expose leading/trailing whitespace ambiguity, record it for Architect review rather than expanding scope.

EDITION PARSING

Preserve exact accepted forms:

blank / Unset => Unset
Marketplace
SingleStore
Single-Store
case-insensitive.

Unknown => fail.

DEPLOYMENT ID

Preserve:
blank => edition.ToString()
nonblank => Trim()

Do not make Production DeploymentId mandatory in W1.

MARKETPLACE

Preserve:
Marketplace edition requires Marketplace.ConnectionReference.

Registry MarketplaceConnectionReference comes only from configured reference.

SINGLE STORE

Preserve current Tenant record building and allowlist semantics.

CONNECTION REFERENCE CATALOG

Preserve:
PostgreSQL.ConnectionReferences
with StringComparer.OrdinalIgnoreCase default.

Production validation:

required edition references must exist and be non-empty
SingleStore collection remains ALL configured tenant refs, including inactive
do not expose connection strings in validation failures
STORE COMMERCE

Preserve:

Marketplace deployment StoreCommerce
SingleStore per-tenant StoreCommerce
Market may fall back from DefaultMarketReference
DefaultCurrency and SalesChannel require explicit config for Production active stores
Production active tenants only for StoreCommerce completeness
no defaults invented
SALES CHANNEL CONTRACT

Keep current:
using Tooba.Offer.Contracts.Dtos;
Enum.TryParse<SalesChannel>

Classification:
CONTRACTS_ONLY_ACCEPTED

Do NOT move enum.
Do NOT introduce duplicate local enum/string list.

STOERCONTEXT BOUNDARY

Preserve StoreContext.Contracts.Current only.

Required ZERO:

StoreContext.Application
StoreContext.Infrastructure
StoreContext.Domain
BUILDREGISTRY

Preserve:

deterministic
zero I/O
zero service locator
zero DB
normalized runtime snapshot creation

No runtime configuration reload.

CONTROL PLANE REGISTRY

Preserve:

process-local configuration snapshot semantics
Edition
DeploymentId
MarketplaceConnectionReference
DeploymentStoreCommerce
Hosts
Tenants

No durable control-plane DB authority claim.

VALIDATEONSTART

Program must preserve:
AddOptions<ToobaPlatformOptions>()
.Bind(Tooba section)
.ValidateOnStart()

Preserve singleton:
IValidateOptions<ToobaPlatformOptions>, PlatformOptionsValidator

Preserve ControlPlaneRegistry singleton build.

MIGRATIONRUNNER PARITY

If MigrationRunner binds/validates/builds the same types:

update namespace imports
preserve behavior
ensure TrustedProxies validation does not break runner tests unexpectedly
no separate duplicate validator
EXCEPTION MESSAGE POLICY

Startup ValidateOptions may use operator prose.

Required ZERO runtime message classification:

no ex.Message branching
no Contains/StartsWith inference

Existing:
catch (InvalidOperationException ex) => ValidateOptionsResult.Fail(ex.Message)
is allowed only as startup validation propagation, not semantic runtime classification.

SENSITIVE DATA

Required ZERO in errors/logs:

connection string
credentials
passwords
tokens
secret values

Connection reference name may be used in startup operator validation.
Connection string value must never be included.

FOREIGN LAYERS / BUSINESS AUTHORITY

Required ZERO:

foreign Application
foreign Infrastructure
foreign Domain
foreign DbContext
business command execution
runtime business mutation

Allowed:

Offer.Contracts enum
StoreContext.Contracts context types
BuildingBlocks platform types
W1 GUARD

Add:
HostConfigurationAmcW1GuardTests

Lock at minimum:

exact 9-file tree
exact 9 top-level types
one top-level type per file
exact namespace Tooba.Host.Configuration
no legacy namespace duplicate
Program import updated
Program AddOptions/Bind/ValidateOnStart preserved
registry singleton preserved
PostgreSqlOptions.ConnectionString absent
ConnectionReferences retained
TrustedProxies validation present
Program no silent invalid-proxy skip
invalid non-empty PrimaryDomain fails
StoreContext Contracts-only
Offer Contracts-only
foreign App/Infra/Domain/DbContext zero
protected cert states preserved in SoT
FOCUSED BEHAVIOR TESTS

Add/update focused tests for:

A. TrustedProxies:

empty list valid
one valid IPv4 valid
one valid IPv6 valid
invalid entry fails validation
blank element fails if explicitly configured

B. PrimaryDomain:

null optional
valid normalized primary stored
valid primary added to host map when absent
invalid non-empty primary fails
duplicate primary across tenants fails

C. Legacy ConnectionString:

compile/guards prove property absent
current reference catalog paths continue working

D. Existing:

edition parsing
duplicate tenant id
duplicate host
invalid status
marketplace ref required
production connection refs
StoreCommerce Production validation

No solution-wide tests.

BUILD

Focused:

Tooba.Host
Tooba.Host.Tests
MigrationRunner project/tests only if namespace split impacts them

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
RECOVERY HYGIENE
minimal semantic SoT edits
valid JSON
no duplicate properties
no stale current pointer
no automatic next task
do not wholesale reformat tmar-current-state.json
HOST-ONLY SCOPE

No module recovery.
No frontend.
No Host root final certification in this task.

EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-CONFIGURATION-AMC-001-W1/

Required:

structure-namespace.md
type-split.md
trusted-proxies.md
primary-domain.md
legacy-connection-string.md
registry-parity.md
boundary-security.md
validation.md
recovery.md

Persist exact task:

docs/ai/tasks/TB-TMAR-HOST-CONFIGURATION-AMC-001-W1.task.md

RECOVERY / SOT

On PASS:

lastAcceptedTask = TB-TMAR-HOST-CONFIGURATION-AMC-001-W1
lastAcceptedCommit = actual W1 implementation SHA
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
latestAcceptedImplementationWave = TB-TMAR-HOST-CONFIGURATION-AMC-001-W1
currentHostCheckpoint = Configuration

Record:
configurationProductionFileCount = 9
configurationProductionTypeCount = 9
pathNamespace = EXACT_Tooba.Host.Configuration
fileCohesion = ONE_TOP_LEVEL_TYPE_PER_FILE
configurationAuthority = HOST_OWNS_BIND_VALIDATE_REGISTRY
trustedProxies = FAIL_FAST_VALIDATED
programTrustedProxyParsing = NO_SILENT_SKIP
primaryDomain = NONEMPTY_INVALID_FAILS_FAST
legacyRootConnectionString = REMOVED_ZERO_CONSUMER_RESIDUE
registry = NORMALIZED_PROCESS_SNAPSHOT_PRESERVED
storeContextBoundary = CONTRACTS_ONLY
offerContractBoundary = CONTRACTS_ONLY_ACCEPTED
foreignApplication = ZERO
foreignInfrastructure = ZERO
foreignDomain = ZERO
foreignDbContext = ZERO
businessAuthority = ZERO_CONTROL_PLANE_CONFIG_ONLY
certificationState = NOT_CERTIFIED_W2_REQUIRED

hostPersistenceCertification = HOST_PERSISTENCE_AMC_CERTIFIED_PRESERVED
hostOutboxCertification = HOST_OUTBOX_AMC_CERTIFIED_PRESERVED
hostObservabilityCertification = HOST_OBSERVABILITY_AMC_CERTIFIED_PRESERVED
hostMessagingCertification = HOST_MESSAGING_AMC_CERTIFIED_PRESERVED
hostHealthCertification = HOST_HEALTH_AMC_CERTIFIED_PRESERVED
hostMultiTenancyCertification = HOST_MULTITENANCY_AMC_CERTIFIED_PRESERVED
hostErrorsCertification = HOST_ERRORS_AMC_CERTIFIED_PRESERVED
hostSecurityCertification = HOST_SECURITY_AMC_CERTIFIED_PRESERVED
hostAdminCertification = HOST_ADMIN_FULLY_CERTIFIED_PRESERVED

automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_CONFIGURATION_AMC_001_W1
staleCurrentPointerState = ZERO
nextHostFolderStarted = false

Docs stamp separate from implementation SHA.

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

exact 9 files / 9 top-level types
one type per file
namespace exact Tooba.Host.Configuration
all consumers compile
Program binding/ValidateOnStart/registry singleton preserved
dead legacy PostgreSQL.ConnectionString removed
invalid configured TrustedProxy fails startup validation
Program cannot silently skip invalid configured proxy
invalid non-empty PrimaryDomain fails fast
host normalization/duplicate rules preserved
TenantId identity semantics unchanged
edition/deployment semantics preserved
connection-ref validation preserved
StoreCommerce semantics preserved
Offer dependency remains Contracts-only
StoreContext remains Contracts-only
BuildRegistry remains deterministic zero-I/O
sensitive data leakage zero
message classification zero
foreign App/Infra/Domain/DbContext zero
business authority remains configuration/control-plane only
focused builds/tests PASS
W1 guard PASS
prior Host certs preserved
certification pending W2
implementation SHA advances to actual W1
automatic next NONE
STOP

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-CONFIGURATION-AMC-001-W1
Parent-Task: TB-TMAR-HOST-CONFIGURATION-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Migration-State:
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
Legacy-Root-ConnectionString-State:
Edition-Parsing-State:
DeploymentId-State:
TenantId-Semantics-State:
TenantStatus-State:
Host-Normalization-State:
ConnectionReferences-State:
StoreCommerce-State:
StoreContext-Boundary-State:
Offer-Contract-Boundary-State:
BuildRegistry-State:
Registry-State:
ControlPlane-Semantics-State:
Exception-Message-Classification-State:
Hardcoded-User-Facing-Text-State:
Sensitive-Data-State:
Contracts-Boundary-State:
Foreign-Application-Dependency-State:
Foreign-Infrastructure-Dependency-State:
Foreign-Domain-Dependency-State:
Foreign-DbContext-State:
Business-Authority-State:
W1-Guard-State:
Host-Persistence-Certification-State:
Host-Outbox-Certification-State:
Host-Observability-Certification-State:
Host-Messaging-Certification-State:
Host-Health-Certification-State:
Host-MultiTenancy-Certification-State:
Host-Errors-Certification-State:
Host-Security-Certification-State:
Host-Admin-Certification-State:
Schema-Change-State:
Frontend-State:
Focused-Build-State:
Focused-Test-State:
Recovery-State:
Recovery-Hygiene-State:
Last-Accepted-Commit-State:
Last-Accepted-Commit-Kind:
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

Do not start W2-CERT.
Do not start Host root final certification.
Do not start another Host folder.
Do not open module recovery.

Wait for Architect/user review.

END_TOOBA_TASK
