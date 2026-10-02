PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-PERSISTENCE-AMC-001-W2-CERT
Parent-Task: TB-TMAR-HOST-PERSISTENCE-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_PERSISTENCE_CERTIFY
Title: Certify Host/Persistence connection-reference resolution as a fail-closed, secret-safe global platform boundary
Estimated-Time-Minutes: 8
Hard-Timebox-Minutes: 12

ARCHITECT REVIEW STATE

Parent Analyze:
ACCEPTED

Architect independently verified current main:

exact Host/Persistence tree = 1 file / 1 type
exact namespace = Tooba.Host.Persistence
DatabaseConnectionResolver is internal sealed
implements IDatabaseConnectionResolver
registered singleton in Program
resolves only from ToobaPlatformOptions.PostgreSQL.ConnectionReferences
validates reference presence/non-empty
validates Npgsql connection string syntax only
opens no DB connection
logs nothing
returns raw connection string only to trusted internal consumers
missing/invalid config maps fail-closed to platform.connection.unconfigured
parser ArgumentException details are discarded
exception-message classification = ZERO
foreign App/Infra/Domain/DbContext = ZERO
business authority = ZERO
Outbox/Observability/Messaging/Health/MultiTenancy/Errors/Security/Admin certifications preserved
latest accepted implementation remains Outbox W1:
382ef10af3a5eb49f519e49cb399809b19844bbc

ARCHITECT DECISION

DIRECT CERT is allowed.

No W1 production migration is required because:

path↔namespace already exact
one-file/one-type cohesion already exact
current PlatformHttpException behavior is accepted as the canonical Host technical fail-closed seam
no sensitive leakage or message classification debt is present

SKILL — MANDATORY

Apply:

.cursor/skills/tooba-architecture-certify/SKILL.md

CERTIFY ONLY.

No production changes.
No exception-type rewrite.
No Configuration edits.
No Host root final certification.
No module recovery.

If a material production defect is found:
Status = INCOMPLETE
Production-Repair-Required-State = YES
STOP.

PRIMARY CERTIFICATION SCOPE

src/backend/Host/Tooba.Host/Persistence/

Expected exact tree:

Persistence/

DatabaseConnectionResolver.cs

Expected:

production files = 1
production types = 1
namespace = Tooba.Host.Persistence

Certification labels on PASS:

HOST_PERSISTENCE_AMC_CERTIFIED
HOST_PERSISTENCE_PLATFORM_BOUNDARY_CERTIFIED

MANDATORY CERTIFICATION AUDITS

EXACT TREE

Verify:

exact one production .cs file
exact filename DatabaseConnectionResolver.cs
exact one top-level type
no subfolder
no duplicate implementation under old namespace
no alias/shim/TypeForwardedTo
PATH ↔ NAMESPACE

Verify exact:
namespace Tooba.Host.Persistence

OWNERSHIP

Certify:
DatabaseConnectionResolver
= GLOBAL_HOST_PERSISTENCE_PLATFORM_CERTIFIED

Reason:

Configuration owns the catalog
Persistence owns reference→connection-string resolution
modules/consumers depend on neutral interface seam only
no business authority
INTERFACE OWNERSHIP

Verify:
IDatabaseConnectionResolver is a neutral BuildingBlocks/platform contract.

Certify:

implementation count = exactly one active Host implementation
no module implementation shadows this in Host process
consumers resolve by interface
PROGRAM REGISTRATION

Verify:
AddSingleton<IDatabaseConnectionResolver, DatabaseConnectionResolver>()

Certify singleton lifetime:

options snapshot read-only
resolver stateless
no scoped dependency
no mutable request state
CONFIGURATION AUTHORITY

Verify resolver reads only:
ToobaPlatformOptions.PostgreSQL.ConnectionReferences

Required ZERO:

environment lookup
IConfiguration direct lookup
fallback to root ConnectionString
hidden second catalog
runtime secret file lookup
REFERENCE LOOKUP

Certify fail-closed for:

blank reference
missing key
blank configured value

Verify dictionary lookup remains case-insensitive through configuration object initialization/validator contract.

CONNECTION STRING SYNTAX

Verify:

NpgsqlConnectionStringBuilder used only as syntax parser
no Open/OpenAsync
no DB connectivity probe
no SQL
no migration
no NpgsqlConnection lifetime

Classification:
CONFIG_SYNTAX_VALIDATION_ONLY_CERTIFIED

RAW STRING RETURN

Certify:

valid configured connection string returned unchanged to trusted infrastructure consumer
no logging
no public serialization
no ProblemDetails detail
no metrics tags
no cache outside startup options snapshot
PLATFORM HTTP EXCEPTION BOUNDARY

Certify current behavior:

missing/blank config => PlatformHttpException 503
malformed syntax => PlatformHttpException 503
error code = platform.connection.unconfigured
title = Service Unavailable
no reference/string embedded

Classification:
CANONICAL_HOST_PLATFORM_FAIL_CLOSED_CERTIFIED

Do NOT introduce alternate exception type in CERT.

ERROR CODE

Verify:
platform.connection.unconfigured

Required:

canonical Foundation/platform error code
no duplicate competing code
presentation maps by code/status, not text
no message parsing
STARTUP FAIL-FAST RELATION

Certify:

PlatformOptionsValidator covers edition-required reference presence/non-empty at startup
resolver remains defensive because syntax failures/non-production/ad-hoc refs may still reach runtime
resolver does NOT own edition policy
TENANT POLICY

Certify resolver as:
POLICY_NEUTRAL

It resolves the supplied ConnectionReference only.

It must not decide:

Active vs Disabled
Marketplace vs SingleStore routing
all-configured vs active-only tenant policy
LEGACY ROOT CONNECTION STRING

Verify:
PostgreSQL.ConnectionString is ignored by DatabaseConnectionResolver.

Classification:
LEGACY_CONFIGURATION_COMPATIBILITY_RESIDUE_OUTSIDE_PERSISTENCE_CERT_SCOPE

Do NOT modify Configuration here.

NPGSQL BOUNDARY

Certify direct Npgsql dependency as:
SYNTAX_PARSER_ONLY_CERTIFIED

Required ZERO:

DbContext
repository
SQL command
DB open
schema authority
UNKNOWN EXCEPTION POLICY

Verify:

only expected ArgumentException from parser is translated
unknown exceptions are not broadly swallowed
no catch(Exception)
EXCEPTION MESSAGE CLASSIFICATION

Required ZERO:

ex.Message
exception.Message
Message.Contains
StartsWith
parser prose interpretation
HARD-CODED TEXT

Current:
Service Unavailable

Certify as canonical HTTP/operator title for this platform technical exception.

Required:

no user-facing business copy
no connection details
no localization-by-message logic
SECRET SAFETY

Required ZERO exposure of:

raw connection string
connection reference
username/password from connection string
parser exception message
host/database name
SSL options

Inspect:

exception path
logs
metrics
tracing
public response
THREAD SAFETY

Certify:

resolver is stateless after construction
IOptions snapshot captured once
no writes to options/dictionary
singleton safe under intended immutable startup config model
CONFIG RELOAD

Certify current model:
IOPTIONS_SNAPSHOT_STATIC_PROCESS_CONFIG_CERTIFIED

No IOptionsMonitor.
No dynamic reload promise.

MICROSERVICE READINESS

Certify:

consumers depend on ConnectionReference abstraction
raw config mapping stays at Host boundary
service can later supply its own resolver implementation
no module code owns connection-string catalog lookup
FOREIGN LAYERS / BUSINESS AUTHORITY

Required ZERO:

module Application
module Infrastructure
module Domain
module DbContext
business command
business policy
cross-module persistence
domain mutation
EXISTING GUARD

Preserve and tighten if needed:
HostPersistenceAmcGuardTests

Do NOT weaken existing assertions.

DURABLE CERT GUARD

Create:
HostPersistenceAmcCertGuardTests

Lock at minimum:

exact 1-file/1-type tree
exact namespace
Program singleton registration
IDatabaseConnectionResolver seam
ConnectionReferences-only lookup
root ConnectionString fallback absent
NpgsqlConnectionStringBuilder parser present
no Open/OpenAsync/DbContext/SQL
exact platform.connection.unconfigured code
no logger/console
no reference/string interpolation into exception
no message classification
no foreign layers
no business authority
prior Host certifications preserved in SoT
implementation SHA remains Outbox W1 SHA
FOCUSED BEHAVIOR TESTS

Add/ensure focused tests for:

valid reference returns exact configured string
blank reference fails 503 + canonical code
missing reference fails 503 + canonical code
blank configured value fails 503 + canonical code
malformed connection string fails 503 + canonical code
failure path does not expose reference value
failure path does not expose connection string
no parser message exposure

No solution-wide tests.

BUILD

Focused:

Tooba.Host
Tooba.Host.Tests

No solution-wide build.

PROTECTED CERTIFICATIONS

Must preserve:

HOST_OUTBOX_AMC_CERTIFIED
HOST_OBSERVABILITY_AMC_CERTIFIED
HOST_MESSAGING_AMC_CERTIFIED
HOST_HEALTH_AMC_CERTIFIED
HOST_MULTITENANCY_AMC_CERTIFIED
HOST_ERRORS_AMC_CERTIFIED
HOST_SECURITY_AMC_CERTIFIED
HOST_ADMIN_FULLY_CERTIFIED

No production edits under certified folders.

PRODUCTION CHANGE RULE

Expected:
Production-Code-Change-State = ZERO
Production-Repair-Required-State = NONE

CERT may modify only:

tests/guards
docs/evidence
Recovery/SoT metadata

If production code must change:
STOP INCOMPLETE.

RECOVERY HYGIENE

Verify:

valid JSON
no duplicate properties
no stale current pointer
currentHostCheckpoint = Persistence
no automatic next folder
implementation SHA remains 382ef10a...
cert/docs stamp separated from implementation commit

Avoid cosmetic whole-file rewrite of tmar-current-state.json.

EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-PERSISTENCE-AMC-001-W2-CERT/

Required:

certification-summary.md
physical-tree.md
resolver-semantics.md
exception-boundary.md
secret-safety.md
configuration-boundary.md
validation.md
recovery.md

Persist exact task:

docs/ai/tasks/TB-TMAR-HOST-PERSISTENCE-AMC-001-W2-CERT.task.md

RECOVERY / SOT

On PASS:

Top-level:

lastAcceptedTask = TB-TMAR-HOST-PERSISTENCE-AMC-001-W2-CERT
lastAcceptedCommit remains:
382ef10af3a5eb49f519e49cb399809b19844bbc
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
latestAcceptedImplementationWave = TB-TMAR-HOST-OUTBOX-AMC-001-W1
currentHostCheckpoint = Persistence
nextTask = USER_REVIEW_HOST_PERSISTENCE_AMC_001_W2_CERT
workflowStop = USER_REVIEW_HOST_PERSISTENCE_AMC_001_W2_CERT
automaticNextImplementationTask = NONE
staleCurrentPointerState = ZERO
nextHostFolderStarted = false

Add/update:
hostPersistenceAmc001W2Cert

Required:

certificationState = HOST_PERSISTENCE_AMC_CERTIFIED
boundaryState = HOST_PERSISTENCE_PLATFORM_BOUNDARY_CERTIFIED
productionFileCount = 1
productionTypeCount = 1
pathNamespace = EXACT_Tooba.Host.Persistence
resolverDisposition = GLOBAL_HOST_PERSISTENCE_PLATFORM_CERTIFIED
interfaceOwnership = BUILDINGBLOCKS_IDATABASECONNECTIONRESOLVER_CERTIFIED
implementationCount = ONE_HOST_CERTIFIED
diLifetime = SINGLETON_CERTIFIED
configurationAuthority = CONFIGURATION_CATALOG_PERSISTENCE_RESOLVE_CERTIFIED
referenceLookup = FAIL_CLOSED_CERTIFIED
connectionStringParse = CONFIG_SYNTAX_VALIDATION_ONLY_CERTIFIED
connectionStringReturn = RAW_TRUSTED_INTERNAL_CERTIFIED
platformExceptionBoundary = CANONICAL_HOST_PLATFORM_FAIL_CLOSED_CERTIFIED
errorCode = FOUNDATION_platform.connection.unconfigured_CERTIFIED
tenantReferencePolicy = POLICY_NEUTRAL_CERTIFIED
legacyRootConnectionString = OUTSIDE_PERSISTENCE_SCOPE_IGNORED
npgsqlBoundary = SYNTAX_PARSER_ONLY_CERTIFIED
messageClassification = ZERO
sensitiveData = ZERO
unknownException = EXPECTED_ARGUMENT_ONLY_UNKNOWN_PROPAGATES
threadSafety = SINGLETON_IMMUTABLE_SNAPSHOT_CERTIFIED
configReload = STATIC_PROCESS_CONFIG_CERTIFIED
microserviceReadiness = CONNECTIONREFERENCE_SEAM_CERTIFIED
foreignApplication = ZERO
foreignInfrastructure = ZERO
foreignDomain = ZERO
foreignDbContext = ZERO
businessAuthority = ZERO
productionRepairRequired = false
productionCodeChange = ZERO
implementationCommit = 382ef10a...
hostOutboxCertification = HOST_OUTBOX_AMC_CERTIFIED_PRESERVED
hostObservabilityCertification = HOST_OBSERVABILITY_AMC_CERTIFIED_PRESERVED
hostMessagingCertification = HOST_MESSAGING_AMC_CERTIFIED_PRESERVED
hostHealthCertification = HOST_HEALTH_AMC_CERTIFIED_PRESERVED
hostMultiTenancyCertification = HOST_MULTITENANCY_AMC_CERTIFIED_PRESERVED
hostErrorsCertification = HOST_ERRORS_AMC_CERTIFIED_PRESERVED
hostSecurityCertification = HOST_SECURITY_AMC_CERTIFIED_PRESERVED
hostAdminCertification = HOST_ADMIN_FULLY_CERTIFIED_PRESERVED
automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_PERSISTENCE_AMC_001_W2_CERT
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

exact 1-file/1-type Persistence tree
exact namespace
singleton registration certified
resolver ownership certified
ConnectionReferences-only lookup
fail-closed semantics exact
syntax-only Npgsql parse exact
raw string internal-only trust boundary certified
PlatformHttpException boundary certified
canonical error code exact
resolver remains tenant-policy neutral
legacy root ConnectionString ignored
secret leakage zero
message classification zero
broad unknown catch zero
foreign App/Infra/Domain/DbContext zero
business authority zero
durable cert guard PASS
focused tests/builds PASS
prior certs preserved
production repair NONE
production change ZERO
recovery hygiene exact
implementation SHA remains 382ef10a...
automatic next NONE
STOP

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-PERSISTENCE-AMC-001-W2-CERT
Parent-Task: TB-TMAR-HOST-PERSISTENCE-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Certification-State:
Boundary-Certification-State:
Persistence-Production-File-Count:
Persistence-Production-Type-Count:
Exact-Tree-State:
Path-Namespace-State:
DatabaseConnectionResolver-State:
Interface-Ownership-State:
Implementation-Count-State:
Program-DI-State:
DI-Lifetime-State:
Configuration-Authority-State:
Reference-Lookup-State:
ConnectionString-Parse-State:
ConnectionString-Return-State:
PlatformHttpException-Boundary-State:
Error-Code-State:
Startup-FailFast-Relation-State:
Tenant-Reference-Policy-State:
Legacy-Root-ConnectionString-State:
Npgsql-Boundary-State:
Exception-Message-Classification-State:
Hardcoded-Runtime-Text-State:
Sensitive-Data-State:
Unknown-Exception-State:
Thread-Safety-State:
Config-Reload-State:
Microservice-Readiness-State:
Contracts-Boundary-State:
Foreign-Application-Dependency-State:
Foreign-Infrastructure-Dependency-State:
Foreign-Domain-Dependency-State:
Foreign-DbContext-State:
Business-Authority-State:
Active-Consumer-State:
Durable-Cert-Guard-State:
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

Do not start Configuration.
Do not start Host root final certification.
Do not modify production code in CERT.
Do not open module recovery.

Wait for Architect/user review.

END_TOOBA_TASK