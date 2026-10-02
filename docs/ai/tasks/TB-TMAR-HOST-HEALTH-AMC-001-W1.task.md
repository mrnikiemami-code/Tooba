PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-HEALTH-AMC-001-W1
Parent-Task: TB-TMAR-HOST-HEALTH-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_HEALTH_STRUCTURE_DI_DISCLOSURE_HYGIENE
Title: Align Host/Health namespace, remove readiness service locator, and sanitize public readiness detail
Estimated-Time-Minutes: 14
Hard-Timebox-Minutes: 18

ARCHITECT REVIEW STATE

Parent Analyze:
ACCEPTED

Architect independently verified:

Host/Health exact production files = 2
HostHealthEndpoints and HostReadinessEvaluator are both legitimate global Host health platform responsibilities
4 routes are active and unique:
/health/live
/health
/health/ready
/ready
liveness is process-only
readiness is configuration/availability oriented, not database connectivity proof
AccessControl dependency is Contracts-only
path↔namespace is currently a real violation:
Tooba.Host under /Health
IServiceProvider.GetService<IBusControl>() is genuine service-locator debt
public readiness currently leaks configuration-internal connection-reference names and messaging schema names
raw Results.Json is an intentional health protocol exception, not a business API presentation violation
machine status strings are operational protocol values, not localizable user-facing prose
MultiTenancy/Errors/Security/Admin certifications remain protected
latest accepted implementation remains:
cfbc94d258de837fdc018ddb29db68233cc25783

ARCHITECT DECISIONS FOR W1

Do NOT create a new Messaging abstraction in this Health wave.
Host/Health may directly inspect MassTransit bus health because both are Host platform concerns.

Remove service locator with explicit DI using:
IEnumerable<IBusControl>
(or an equivalent explicit collection injection supported by DI).

Rationale:

zero services registered when messaging disabled => empty collection
one bus expected when enabled
no IServiceProvider
no optional-service service locator
no new cross-folder seam

Preserve current "all configured tenant connection references" readiness semantics.
Do NOT switch to Active-only in W1.
This matches current startup configuration collection semantics and avoids changing readiness meaning during hygiene.

Do NOT broadly catch unknown exceptions in W1.
Unknown probe/runtime faults continue to the canonical global exception boundary.
Health-specific broad swallowing would violate the general unknown-fault propagation rule without a typed fault contract.

Public readiness output MUST stop exposing:

actual connection reference names
messaging schema name

SKILL — MANDATORY

Apply:

.cursor/skills/tooba-architecture-migrate/SKILL.md

MIGRATE ONLY.

Do NOT certify Health in W1.
Do NOT start Messaging AMC.
Do NOT start Configuration/Persistence AMC.
Do NOT start another Host folder.
Do NOT open module recovery.
Do NOT reopen MultiTenancy/Errors/Security/Admin.

PRIMARY ACTIVE FOLDER

src/backend/Host/Tooba.Host/Health/

EXPECTED TARGET FILES

Keep exactly:

HostHealthEndpoints.cs
HostReadinessEvaluator.cs

No new production file required unless compiler forces a tiny Host/Health-local helper.
Prefer exact 2-file shape.

TARGET NAMESPACE

Both:
Tooba.Host.Health

REQUIRED CHANGES

PATH ↔ NAMESPACE REPAIR

Change both files from:

namespace Tooba.Host;

to:

namespace Tooba.Host.Health;

Update Program/test usings only as required.

Program should import:
using Tooba.Host.Health;

No alias/shim/TypeForwardedTo.

REMOVE ISERVICEPROVIDER FROM HEALTH PATH

Remove:

IServiceProvider from HostHealthEndpoints readiness handler
IServiceProvider from HostReadinessEvaluator.EvaluateAsync
services.GetService<IBusControl>()

Target explicit DI:

readiness endpoint receives IEnumerable<IBusControl> or equivalent DI collection
evaluator receives that explicit bus collection/value

Required final state:

IServiceProvider usage in Host/Health = ZERO
RequestServices in Host/Health = ZERO
GetService<IBusControl> = ZERO
GetRequiredService<IBusControl> = ZERO
no service locator replacement
MESSAGING ENABLED BEHAVIOR

When messagingOptions.Enabled == true:

exactly one active bus is expected

if no bus is available:
checks["messaging"] = "bus-unavailable"
readiness = false

if bus health is Unhealthy:
checks["messaging"] = "unhealthy"
readiness = false

otherwise preserve existing health status semantics

If explicit DI yields multiple IBusControl registrations:
fail deterministically rather than silently select arbitrary transport.
Use SingleOrDefault or an equivalent exactness assertion.

Do NOT redesign MassTransit registration.

MESSAGING DISABLED BEHAVIOR

When messaging disabled:
preserve:

checks["messaging"] = "disabled"
checks["messaging-transport"] = "n/a"

Do not require bus registration.

DISCLOSURE SANITIZATION — CONNECTION REFERENCE

Current unsafe public value:
missing-reference:{actual-reference}

Replace with a stable generic operational value:
missing-reference

Required:

HTTP remains 503
check key remains postgresql
no actual ConnectionReference value in response
no connection string in response
no tenant/deployment internal reference name in response

Do not change internal configuration validation.

DISCLOSURE SANITIZATION — MESSAGING SCHEMA

Remove public readiness key:
messaging-schema

Do not emit MessagingHostOptions.Schema in readiness JSON.

Preserve safe:

messaging
messaging-transport

messaging-transport = postgresql-sql is allowed operational metadata.

READINESS SEMANTICS — PRESERVE

Preserve exact check order:

edition
configured PostgreSQL references
authorization readiness
messaging readiness

Preserve:

edition Unset => not-ready/503
configured reference presence check only
no DB open/connectivity probe
authorization probe semantics
messaging disabled/enabled behavior

Do NOT claim connectivity readiness.

TENANT REFERENCE COLLECTION — PRESERVE ALL

Do NOT change CollectConnectionReferences from all configured SingleStore tenants to Active-only.

Reason:
this W1 is hygiene, not readiness policy redesign.

Document:
CONFIGURED_DEPLOYMENT_REFERENCES_NOT_ACTIVE_TENANT_CONNECTIVITY

UNKNOWN EXCEPTION SEMANTICS — PRESERVE

Do NOT add broad catch (Exception).

Unexpected exceptions from:

authorization readiness probe
MassTransit health
configuration/runtime defects

continue to canonical Host exception handling.

Known readiness states remain explicit machine results.

LIVENESS ROUTES — EXACT PRESERVATION

Preserve:

GET /health/live
GET /health

Response:
{ status: "ok" }

No DB/bus/auth/tenant resolution.

Preserve current CORS behavior on liveness aliases.

READINESS ROUTES — EXACT PRESERVATION

Preserve:

GET /health/ready
GET /ready

Both use same evaluator.

Preserve current response shape:
ready:
{
status: "ready",
checks: ...
}

not ready:
{
status: "not-ready",
checks: ...
}
HTTP 503 on not-ready.

Do NOT wrap with ApiResponseFactory.

HEALTH RAW JSON EXCEPTION

Explicitly retain raw Results.Json.

This is an operational health protocol endpoint, not module/business API presentation.

Do not introduce SemanticError/ProblemDetails for normal readiness=false.

MACHINE STATUS TEXT

Allowed operational protocol values include:

ok
ready
not-ready
configured
unconfigured
disabled
unhealthy
bus-unavailable
postgresql-sql
n/a
missing-reference

These are machine health contract values.
Do NOT localize them.

User-facing runtime prose in Health must remain ZERO.

ACCESSCONTROL CONTRACTS-ONLY

Preserve:
Tooba.AccessControl.Contracts.Readiness.IAuthorizationReadinessProbe

Required ZERO:

AccessControl.Application
AccessControl.Infrastructure
AccessControl.Domain

Do not change AccessControl production code.

MASS TRANSIT BOUNDARY

Direct MassTransit references remain allowed in Host/Health only for transport readiness:

IBusControl
BusHealthStatus
CheckHealth()

No publishing/consuming/business messaging logic in Health.

No Npgsql direct dependency.

CONFIGURATION OWNERSHIP

Health remains read-only consumer of:

ControlPlaneRegistry
ToobaPlatformOptions
MessagingHostOptions

No options mutation.
No validator duplication.
No configuration folder migration.

SENSITIVE DATA

After W1 public health/readiness must expose ZERO:

connection reference names
connection strings
messaging schema name
credentials
tokens
SQL
internal exception.Message
stack trace

Operational edition label is allowed.

OBSERVABILITY

Do not add:

custom ActivitySource
Meter
correlation implementation
logging scope
sensitive logs

Health files currently need no new logging.

DEPENDENCY BOUNDARY

Host/Health must remain ZERO:

module Application
module Infrastructure
module Domain
DbContext
direct persistence query
business command
business workflow mutation

Allowed:

AccessControl.Contracts readiness
Host configuration models
Host messaging options
MassTransit host transport health
ASP.NET primitives
FILE COHESION

Keep:

HostHealthEndpoints = route/presentation composition only
HostReadinessEvaluator = readiness decision/evaluation only
nested Evaluation record may remain

Do not merge files.

PROGRAM UPDATE

Update Program using/reference for:
Tooba.Host.Health

Preserve:
HostHealthEndpoints.Map(app, enableCors: true);

No route reorder needed.

TEST / GUARD REPAIR

Preserve existing:
HostReadinessBoundaryGuardTests

Update namespace expectations if required.

Add:
HostHealthAmcW1GuardTests

At minimum prove:

exact 2 files
exact namespace Tooba.Host.Health
Program uses HostHealthEndpoints from new namespace
exact 4 routes
IServiceProvider absent from Health production
no GetService/GetRequiredService for IBusControl
explicit IEnumerable<IBusControl> (or equivalent) DI
actual connection reference not emitted
missing-reference: interpolation absent
messaging-schema response key absent
raw Results.Json retained
AccessControl Contracts-only
foreign App/Infra/Domain/DbContext zero
hard-coded USER-FACING prose zero
machine health labels permitted
historical protected certs preserved
FOCUSED BEHAVIOR TESTS

Add/update focused tests for:

A. liveness:

/health
/health/live
200
status=ok

B. readiness connection missing:

503
checks.postgresql = "missing-reference"
response does NOT contain actual configured reference token

C. messaging disabled:

no IBusControl required
checks.messaging = disabled

D. messaging enabled with no bus collection entry:

not-ready
messaging = bus-unavailable

E. messaging enabled unhealthy bus:

not-ready
messaging = unhealthy

F. messaging ready:

no messaging-schema property in checks
safe transport/status only

G. authorization not-ready:

existing safe label preserved

No need to force broad exception test.

BUILD / VALIDATION

Focused builds:

Tooba.Host
Tooba.Host.Tests

Focused tests:

HostReadinessBoundaryGuardTests
HostHealthAmcW1GuardTests
health/readiness focused tests
MultiTenancy guards only if namespace/program edits interact
TmarDurableGuard current-state assertions

No solution-wide tests.

One deterministic bounded repair + one rerun max.

PROTECTED CERTIFICATIONS

Must preserve:

HOST_MULTITENANCY_AMC_CERTIFIED
HOST_ERRORS_AMC_CERTIFIED
HOST_SECURITY_AMC_CERTIFIED
HOST_ADMIN_FULLY_CERTIFIED

No production changes in those folders.

HOST-ONLY SCOPE LOCK

User explicitly requested:
FINISH HOST ONLY.

No module recovery.
No Catalog/Fulfillment/Checkout work.
No module production changes.

EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-HEALTH-AMC-001-W1/

Required:

namespace.md
di-hygiene.md
disclosure-sanitization.md
readiness-parity.md
boundary-security.md
validation.md
recovery.md

Persist exact task:

docs/ai/tasks/TB-TMAR-HOST-HEALTH-AMC-001-W1.task.md

RECOVERY / SOT

On PASS:

lastAcceptedTask = TB-TMAR-HOST-HEALTH-AMC-001-W1
lastAcceptedCommit = actual W1 implementation SHA
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
latestAcceptedImplementationWave = TB-TMAR-HOST-HEALTH-AMC-001-W1
currentHostCheckpoint = Health

Record:
healthProductionFileCount = 2
healthProductionTypeCount = 2
routeCount = 4
hostHealthEndpoints = KEEP_AS_GLOBAL_HOST_HEALTH_PLATFORM
hostReadinessEvaluator = KEEP_AS_GLOBAL_HOST_HEALTH_PLATFORM
pathNamespace = EXACT_Tooba.Host.Health
serviceLocator = ZERO
iServiceProvider = ZERO
messagingBusInjection = EXPLICIT_DI_COLLECTION
connectionReferenceDisclosure = ZERO
messagingSchemaDisclosure = ZERO
rawResultsJson = INTENTIONAL_OPERATIONAL_EXCEPTION
connectionReadinessTruth = CONFIGURED_NOT_CONNECTIVITY
tenantReferencePolicy = ALL_CONFIGURED_PRESERVED
hardcodedUserFacingText = ZERO
certificationState = NOT_CERTIFIED_W2_REQUIRED

hostMultiTenancyCertification = HOST_MULTITENANCY_AMC_CERTIFIED_PRESERVED
hostErrorsCertification = HOST_ERRORS_AMC_CERTIFIED_PRESERVED
hostSecurityCertification = HOST_SECURITY_AMC_CERTIFIED_PRESERVED
hostAdminCertification = HOST_ADMIN_FULLY_CERTIFIED_PRESERVED

automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_HEALTH_AMC_001_W1
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
Push main only on PASS.

SUCCESS CRITERIA

PASS only if:

Health exact 2-file shape preserved
namespace exact Tooba.Host.Health
exact 4 routes preserved
liveness behavior preserved
readiness shape/status preserved
IServiceProvider zero
service locator zero
MassTransit bus explicit DI collection
messaging disabled works with empty bus collection
actual connection references not exposed
messaging schema not exposed
connection readiness stays configured-not-connectivity
all configured tenant reference policy unchanged
AccessControl Contracts-only preserved
raw Results.Json preserved intentionally
machine status labels not localized
user-facing runtime text zero
sensitive detail leakage zero
foreign App/Infra/Domain/DbContext zero
no module production changes
MultiTenancy/Errors/Security/Admin certs preserved
focused builds/tests PASS
certification remains pending W2
actual W1 implementation SHA recorded
automatic next NONE
STOP

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-HEALTH-AMC-001-W1
Parent-Task: TB-TMAR-HOST-HEALTH-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Migration-State:
Health-Production-File-Count:
Health-Production-Type-Count:
Route-Count:
HostHealthEndpoints-State:
HostReadinessEvaluator-State:
Path-Namespace-State:
IServiceProvider-State:
ServiceLocator-State:
Messaging-Bus-DI-State:
Liveness-Parity-State:
Readiness-Parity-State:
Connection-Readiness-Truth-State:
Tenant-Reference-Policy-State:
ConnectionReference-Disclosure-State:
MessagingSchema-Disclosure-State:
Raw-ResultsJson-State:
Machine-Status-Text-State:
Hardcoded-User-Facing-Text-State:
AccessControl-Readiness-Boundary-State:
MassTransit-Boundary-State:
Exception-Safety-State:
Sensitive-Data-State:
Observability-State:
Contracts-Boundary-State:
Foreign-Application-Dependency-State:
Foreign-Infrastructure-Dependency-State:
Foreign-Domain-Dependency-State:
Foreign-DbContext-State:
Business-Authority-State:
W1-Guard-State:
Host-MultiTenancy-Certification-State:
Host-Errors-Certification-State:
Host-Security-Certification-State:
Host-Admin-Certification-State:
Schema-Change-State:
Frontend-State:
Focused-Build-State:
Focused-Test-State:
Recovery-State:
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
Do not start another Host folder.
Do not open module recovery.

Wait for Architect/user review.

END_TOOBA_TASK