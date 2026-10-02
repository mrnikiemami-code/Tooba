PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-PERSISTENCE-AMC-001
Parent-Task: TB-TMAR-HOST-OUTBOX-AMC-001-W2-CERT
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_PERSISTENCE_ANALYZE
Title: Analyze Host/Persistence connection-reference resolver ownership, fail-closed semantics, HTTP coupling, and secret safety
Estimated-Time-Minutes: 8
Hard-Timebox-Minutes: 12

ARCHITECT REVIEW STATE

Parent Outbox certification:
ACCEPTED

Architect independently verified:

HOST_OUTBOX_AMC_CERTIFIED
HOST_OUTBOX_PLATFORM_BOUNDARY_CERTIFIED
exact 7-file / 7-type Outbox tree
requested cancellation propagation is durably guarded
options fail-fast validation present
production change in Outbox CERT = ZERO
implementation authority remains Outbox W1:
382ef10af3a5eb49f519e49cb399809b19844bbc
Observability/Messaging/Health/MultiTenancy/Errors/Security/Admin certifications preserved

NEXT ACTIVE HOST UNIT

src/backend/Host/Tooba.Host/Persistence/

Current repository snapshot shows exactly one production file:

Persistence/

DatabaseConnectionResolver.cs

Current visible production type:

DatabaseConnectionResolver

Current namespace:
Tooba.Host.Persistence

So path↔namespace appears already EXACT.

Current behavior:

implements IDatabaseConnectionResolver
reads ToobaPlatformOptions.PostgreSQL.ConnectionReferences
validates reference existence/non-empty
parses connection string with NpgsqlConnectionStringBuilder
returns raw connection string to internal consumers
throws PlatformHttpException 503 / platform.connection.unconfigured on missing/invalid reference
does not log connection string

SKILL — MANDATORY

Apply:

.cursor/skills/tooba-architecture-analyze/SKILL.md

ANALYSIS ONLY.

Do NOT edit production code.
Do NOT migrate.
Do NOT certify.
Do NOT start Configuration.
Do NOT start Host root/final certification.
Do NOT open module recovery.

PROTECTED STATE

Must remain:

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
src/backend/Host/Tooba.Host/Persistence/

Record:

exact production file count
exact filename(s)
exact type count
visibility
namespace
consumers

Expected:
1 file / 1 type

TYPE DISPOSITION

Classify:
DatabaseConnectionResolver

Allowed dispositions:

KEEP_AS_GLOBAL_HOST_PERSISTENCE_PLATFORM
KEEP_AS_THIN_HOST_CONFIGURATION_TO_PERSISTENCE_ADAPTER
MOVE_TO_CONFIGURATION
MOVE_TO_TOOBAPERSISTENCE
DEAD_ZERO_CONSUMER_RESIDUE
BLOCKED_NEEDS_ARCHITECT_DECISION

Do not move in Analyze.

PATH ↔ NAMESPACE

Verify:
path = Host/Persistence/
namespace = Tooba.Host.Persistence

Expected:
EXACT

Also verify no duplicate resolver under old Tooba.Host namespace.

INTERFACE OWNERSHIP

Trace:
IDatabaseConnectionResolver

Determine:

declaring project/namespace
whether interface is neutral persistence/platform contract
all implementations
all consumers

Verify there is exactly one active Host implementation unless another intentionally scoped implementation exists.

PROGRAM REGISTRATION

Verify exact DI:
AddSingleton<IDatabaseConnectionResolver, DatabaseConnectionResolver>()

Determine:

lifetime correctness
thread safety
options snapshot semantics
whether singleton + IOptions value is intentional immutable startup config behavior
CONSUMER INVENTORY

Map all production consumers, including likely:

Host/Messaging SQL transport registration
Host/Outbox dispatcher
any startup migration/bootstrap
any module infrastructure adapter
any tests

For each, classify:

Host platform
neutral infrastructure
foreign module consumer
direct HTTP path vs background/startup path
CONFIGURATION AUTHORITY

Current resolver reads:
ToobaPlatformOptions.PostgreSQL.ConnectionReferences

Determine:

Configuration owns reference catalog
Persistence owns resolution adapter
no duplicate configuration source
no environment variable lookup inside resolver
no fallback to root ConnectionString unless explicitly present elsewhere
REFERENCE LOOKUP SEMANTICS

Audit:

null/blank reference
missing dictionary key
blank connection string
case sensitivity of dictionary
duplicate config keys
canonical reference naming constraints
whether dictionary itself is validated at startup

Determine whether runtime missing-reference should ever occur after PlatformOptionsValidator succeeds.

CONNECTION STRING PARSE VALIDATION

Current:
new NpgsqlConnectionStringBuilder(connectionString)

Audit:

syntax validation only
no network connectivity
no DB open
no mutation
parser side effects
acceptance of unknown/unsupported keywords
normalization not returned

Classification should distinguish:
CONFIG_SYNTAX_VALIDATION vs CONNECTIVITY.

RAW CONNECTION STRING RETURN

Resolver returns raw connection string to trusted internal consumers.

Audit:

interface contract expects string
consumers handle as infrastructure secret
no public response/logging
no caching outside options
no accidental ToString/log scope exposure

Classify secret boundary.

PLATFORM HTTP EXCEPTION COUPLING — CRITICAL

Current resolver throws:
PlatformHttpException(
503,
"Service Unavailable",
"platform.connection.unconfigured")

Analyze carefully.

Questions:

Resolver is used by HTTP request paths?
Resolver is also used by background workers and startup composition?
Is an HTTP-oriented exception appropriate in a general infrastructure resolver?
Does this create presentation semantics below Host HTTP boundary?
Would a typed neutral configuration exception / SemanticException be more canonical?
Is platform.connection.unconfigured already canonical Foundation/Host error code?
How does ToobaExceptionHandler / ExceptionPresentationService map it?
Do background paths catch/log by type only?
Could this exception ever leak HTTP title/prose into non-HTTP semantics?

Do not change in Analyze.

ERROR CODE OWNERSHIP

Trace:
platform.connection.unconfigured

Verify:

registered canonical error code/resource?
localized EN/FA presentation exists?
owner (Host/Errors/Foundation/Configuration)
no duplicate code
no message-text classification
STARTUP FAIL-FAST RELATION

Trace PlatformOptionsValidator validation for:

Marketplace ConnectionReference existence
all active SingleStore tenant ConnectionReference existence
messaging reference if owned elsewhere
connection strings non-empty
parse validity if any

Determine which missing/invalid conditions should be impossible at runtime and which resolver still defensively guards.

ACTIVE VS ALL TENANT SEMANTICS

Compare connection reference validation policies:

MultiTenancy active tenants
Outbox active tenants
Health all configured references
PlatformOptionsValidator current policy

Determine if resolver itself is policy-neutral (expected) and should only resolve the supplied reference.

LEGACY ROOT CONNECTION STRING

ToobaPlatformOptions.PostgreSQL also has:
ConnectionString

Determine:

any production consumer
whether DatabaseConnectionResolver ignores it intentionally
whether it is legacy/dead compatibility residue
whether Persistence folder should care or Configuration task should own cleanup

Do NOT modify Configuration in this task.

NPGSQL DEPENDENCY

Direct NpgsqlConnectionStringBuilder in Host/Persistence.

Classify:

legitimate infrastructure parser
no direct SQL
no DB connection
no DbContext
no module persistence ownership
EXCEPTION MESSAGE CLASSIFICATION

Required ZERO:

ex.Message branching
Message.Contains/StartsWith
parser message interpretation
HARD-CODED TEXT / LOCALIZATION

Current hard-coded title:
"Service Unavailable"

Classify:

HTTP presentation text
operator/internal text
canonical exception title
whether current exception boundary localizes independently
whether this is a quality debt even if code is stable
SENSITIVE DATA

Verify required ZERO exposure:

connection string in exception
connection reference in exception title/detail
connection string in logs
connection reference in public ProblemDetails
Npgsql parser exception text propagation

Important:
catch ArgumentException currently discards parser exception details.

UNKNOWN EXCEPTION POLICY

Current catch only:
ArgumentException

Determine:

whether NpgsqlConnectionStringBuilder can throw other relevant config exceptions
whether unknown exceptions propagate
no broad catch
THREAD SAFETY / IMMUTABILITY

Resolver singleton holds _options = options.Value.

Audit:

no mutation
dictionary could technically be mutable but treated startup-frozen
IOptionsMonitor not used
runtime config reload semantics intentionally absent/present
FOREIGN LAYERS / BUSINESS AUTHORITY

Required ZERO:

module Application
module Infrastructure
module Domain
module DbContext
repository
business command
domain policy
MICROSERVICE EXTRACTION READINESS

Assess:

ConnectionReference abstraction shields callers from raw config lookup
Host implementation is deployment composition
module code should never know raw connection string reference mapping
future service can provide its own resolver implementation

Determine if current boundary is suitable.

TEST / GUARD INVENTORY

Find focused tests for:

valid reference resolves
blank reference fails
missing reference fails
blank configured value fails
malformed connection string fails
valid string returned unchanged
no reference/value leakage
code platform.connection.unconfigured
DI singleton
path/namespace
no DbContext/foreign layers

Identify missing coverage.

HISTORICAL CLAIMS

Inspect:

persistence foundation docs
connection reference architecture docs
current SoT
any prior certification for Host/Persistence or IDatabaseConnectionResolver

Classify:
CURRENT / HISTORICAL / FOUNDATION_ONLY / NOT_FOLDER_CERTIFIED

DECISIVE TARGET PLAN

Recommend fewest safe waves.

Likely possibilities:

A. DIRECT CERT if:

path/namespace exact
PlatformHttpException is accepted as canonical Host platform exception at this boundary
no missing test/behavior debt

B. W1 + CERT if:

exception type/presentation coupling should be repaired
guard/tests missing materially

C. one combined bounded W1 then CERT if only small typed-exception seam required

Do not force migration merely for process symmetry.

Each recommended wave <=20 min.

HOST-ONLY SCOPE LOCK

No module recovery.
No Configuration migration.
No root final certification yet.

PRODUCTION CHANGE RULE

Analyze production change:
ZERO

Docs/evidence/SoT only.

FOCUSED VALIDATION

No solution-wide tests.
No production build unless needed for consumer/interface ambiguity.

EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-PERSISTENCE-AMC-001/

Required:

analyze.md
ownership-consumers.md
resolver-semantics.md
exception-boundary.md
secret-safety.md
configuration-relation.md
path-namespace.md
tests-guards.md
historical-claims.md
migration-plan.md
recovery.md

Persist exact task:

docs/ai/tasks/TB-TMAR-HOST-PERSISTENCE-AMC-001.task.md

RECOVERY / SOT

Analysis-only.
Do NOT advance implementation SHA.

Record:

currentHostCheckpoint = Persistence
mode = ANALYSIS_ONLY_NOT_MIGRATED_NOT_CERTIFIED
productionFileCount = actual
productionTypeCount = actual
pathNamespaceState
resolverOwnershipState
interfaceOwnershipState
consumerState
exceptionBoundaryState
errorCodeState
startupFailFastRelationState
secretSafetyState
legacyConnectionStringState
recommendedWaveCount
recommendedNextTask
Outbox certification = PRESERVED
Observability certification = PRESERVED
Messaging certification = PRESERVED
Health certification = PRESERVED
MultiTenancy certification = PRESERVED
Errors certification = PRESERVED
Security certification = PRESERVED
Admin certification = PRESERVED
automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_PERSISTENCE_AMC_001
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

exact Persistence tree/type verified
disposition decided
path/namespace exactness verified
interface owner + all consumers mapped
DI lifetime assessed
configuration authority mapped
lookup semantics audited
Npgsql parse semantics audited
raw connection string trust boundary audited
PlatformHttpException coupling decisively classified
error code ownership/localization mapped
startup validator relation mapped
active/all tenant policy relation mapped
legacy root ConnectionString classified
sensitive leakage proven zero or blocker identified
unknown exception policy audited
thread-safety/config reload semantics audited
foreign layers/business authority zero
microservice readiness assessed
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
Task-ID: TB-TMAR-HOST-PERSISTENCE-AMC-001
Parent-Task: TB-TMAR-HOST-OUTBOX-AMC-001-W2-CERT
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
DatabaseConnectionResolver-State:
Type-Disposition-State:
Path-Namespace-State:
Interface-Ownership-State:
Implementation-Count-State:
Program-DI-State:
DI-Lifetime-State:
Consumer-Audit-State:
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
Guard-Impact-State:
Historical-Claims-State:
Recommended-Wave-Count:
Recommended-Next-Task:
Production-Code-Change-State:
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

Do not start Persistence W1/CERT.
Do not start Configuration.
Do not start Host root final certification.
Do not open module recovery.

Wait for Architect/user review.

END_TOOBA_TASK