PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-HEALTH-AMC-001
Parent-Task: TB-TMAR-HOST-MULTITENANCY-AMC-001-W2-CERT
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_HEALTH_ANALYZE
Title: Analyze Host/Health liveness-readiness platform boundary, service-location debt, and safe readiness disclosure
Estimated-Time-Minutes: 11
Hard-Timebox-Minutes: 15

ARCHITECT REVIEW STATE

Parent MultiTenancy certification:
ACCEPTED

Architect independently verified:

HOST_MULTITENANCY_AMC_CERTIFIED
HOST_MULTITENANCY_PLATFORM_BOUNDARY_CERTIFIED
exact MultiTenancy tree = 2 files / 2 types
exact namespace = Tooba.Host.MultiTenancy
RequestServices/service locator = ZERO
scoped StoreCommerce assigner via InvokeAsync certified
anti-enumeration / 503-503-404 / canonical presentation preserved
production change in W2-CERT = ZERO
implementation authority remains W1:
cfbc94d258de837fdc018ddb29db68233cc25783
Host/Errors certification preserved
Host/Security certification preserved
Host/Admin certification preserved

NEXT ACTIVE HOST UNIT

src/backend/Host/Tooba.Host/Health/

Current repository snapshot shows exactly two production files:

HostHealthEndpoints.cs
HostReadinessEvaluator.cs

Do NOT assume; re-enumerate from disk.

CURRENT OBSERVATIONS TO VERIFY

Both files currently appear to use:
namespace Tooba.Host

while physical path is:
/Health/

Likely path↔namespace violation:
required if retained = Tooba.Host.Health

Health endpoint current routes:

/health/live
/health
/health/ready
/ready

Current readiness dependencies include:

ControlPlaneRegistry
ToobaPlatformOptions
MessagingHostOptions
AccessControl.Contracts.Readiness.IAuthorizationReadinessProbe
IServiceProvider
MassTransit IBusControl

Potential concerns requiring analysis:

IServiceProvider service-location inside readiness evaluator
direct MassTransit transport health dependency
response checks may expose missing-reference:{reference}
response exposes messaging schema name
raw Results.Json is used intentionally for operational health endpoints
liveness/readiness bypass tenant resolution by design

SKILL — MANDATORY

Apply:

.cursor/skills/tooba-architecture-analyze/SKILL.md

ANALYSIS ONLY.

Do NOT edit production code.
Do NOT migrate.
Do NOT certify.
Do NOT start another Host folder.
Do NOT reopen MultiTenancy/Errors/Security/Admin.

PROTECTED STATE

Must remain:

HOST_MULTITENANCY_AMC_CERTIFIED
HOST_ERRORS_AMC_CERTIFIED
HOST_SECURITY_AMC_CERTIFIED
HOST_ADMIN_FULLY_CERTIFIED
frontend frozen
Checkout paused state unchanged

MANDATORY ANALYSIS

EXACT TREE / TYPE ENUMERATION

Enumerate:
src/backend/Host/Tooba.Host/Health/

Record:

exact production .cs count
exact filenames
all production types
nested record/types
namespaces
visibility

Expected current files:
2

PER-TYPE DISPOSITION

Classify separately:

HostHealthEndpoints
HostReadinessEvaluator
HostReadinessEvaluator.Evaluation if materially relevant

Allowed dispositions:

KEEP_AS_GLOBAL_HOST_HEALTH_PLATFORM
KEEP_AS_THIN_HOST_HEALTH_ADAPTER
MOVE_TO_BUILDINGBLOCKS
MOVE_TO_MODULE
DEAD_ZERO_CONSUMER_RESIDUE
MUST_SPLIT
BLOCKED_NEEDS_ARCHITECT_DECISION

Do not move anything in Analyze.

ROUTE OWNERSHIP / CONSUMERS

Map exact production registration:

Program call site
route count
route paths
CORS behavior
auth requirements or explicit unauthenticated behavior
tenant-resolution bypass behavior

Prove no duplicate health/readiness routes elsewhere.

LIVENESS SEMANTICS

Verify:

/health/live
/health

Expected behavior:

process liveness only
no DB open
no bus health
no tenant resolution
no auth dependency
safe static response

Determine whether both aliases are deliberate compatibility endpoints.

READINESS SEMANTICS

Verify exact checks/order:

edition configured
required PostgreSQL connection references configured
authorization readiness probe
messaging bus when enabled

Document:

which failures return 503
exact check labels
whether any dependency is only configuration-presence vs actual connectivity
whether readiness claims more than it truly verifies

Do not redesign in Analyze.

CROSS-MODULE BOUNDARY

Audit AccessControl readiness dependency:

IAuthorizationReadinessProbe

Verify:

Contracts-only
no AccessControl Application/Infra/Domain
readiness abstraction is intentionally narrow
Host Health does not own authorization business logic

Required foreign App/Infra/Domain/DbContext = ZERO.

MASS TRANSIT BOUNDARY

Analyze direct:
IBusControl
CheckHealth()

Classify whether direct MassTransit use belongs:

Host/Health platform boundary
Host/Messaging abstraction
BuildingBlocks neutral health seam

Determine whether Health should depend on a narrow Host messaging readiness abstraction instead of transport implementation.

Do NOT create abstraction in Analyze.

ISERVICEPROVIDER / SERVICE LOCATOR

Current evaluator accepts:
IServiceProvider services

and resolves:
services.GetService<IBusControl>()

Classify against architecture lock.

Explicitly answer:

Is this genuine service-locator debt?
Can IBusControl? or a narrow readiness seam be injected directly?
Is optional registration the only reason service provider is used?
What is the minimum clean target?

Architecture preference:
avoid IServiceProvider when explicit DI/optional abstraction is feasible.

SAFE DISCLOSURE AUDIT

Current readiness may emit:

missing-reference:{reference}

and:
messaging-schema = messagingOptions.Schema

Assess whether unauthenticated readiness output leaks:

connection reference names
database topology hints
tenant-specific identifiers
schema names
deployment internals

Differentiate:

safe operational label
sensitive/config-internal detail that should be hidden
credential/secret exposure

No speculation; inspect actual values/config conventions where necessary.

RESPONSE CONTRACT

Document exact JSON shapes for:

ready
not-ready
live

Assess whether raw Results.Json is appropriate for health platform endpoints and an intentional exception to ApiResponseFactory.

Do NOT force business API response envelope onto health endpoints unless evidence requires it.

ERROR / LOCALIZATION RULE

Health responses use machine-operational English labels:

ok
ready
not-ready
configured
disabled
unhealthy
etc.

Classify whether these are:

operational protocol/status values, allowed
user-facing presentation text requiring localization

Do not blindly apply localization rule to machine health contracts.

PATH ↔ NAMESPACE

Current likely:
path = Host/Health/
namespace = Tooba.Host

Classify exact violation.

If retained under Health:
target namespace expected:
Tooba.Host.Health

Map Program/test impact.

FILE COHESION

Analyze whether:

endpoints and evaluator are appropriately separate
nested Evaluation record should remain nested
evaluator is too transport-aware
additional split/abstraction needed

Prefer fewest files/waves.

CONFIGURATION OWNERSHIP

Dependencies:

ControlPlaneRegistry
ToobaPlatformOptions
MessagingHostOptions

Classify:

Health reads only
no configuration authority
no options mutation
no duplicate validation logic

Check whether CollectConnectionReferences duplicates PlatformOptionsValidator logic materially or merely builds readiness check inputs.

CONNECTION READINESS TRUTH

Current code checks configured references exist/non-empty in options, but does not open DB.

Document exact semantics:

CONFIGURED readiness vs CONNECTIVITY readiness

Determine whether route naming or checks might mislead operators.

No behavior change in Analyze.

TENANT / EDITION READINESS

For SingleStore:
it iterates all registry tenants.

Verify whether:

disabled/suspended tenants are included
only active tenants should matter
current behavior can make readiness fail because of inactive tenant config
this matches PlatformOptionsValidator startup semantics

This is important because MultiTenancy accepts only Active tenants.

Do not repair yet; classify parity/debt.

MESSAGING OPTIONS / READINESS

Verify:

messaging disabled path
bus unavailable path
health unhealthy path
degraded/healthy handling
schema disclosure
connection-reference inclusion

Classify fail-closed behavior.

AUTHORIZATION READINESS

Inspect contract/result:

fields
possible labels
whether labels are safe
whether readiness call can throw
timeout/cancellation semantics

Determine how exceptions are handled currently.

EXCEPTION SAFETY

Audit if readiness dependency throws:

authorizationReadiness.EvaluateAsync
bus.CheckHealth
options/registry unexpected state

Does endpoint:

propagate to canonical global exception handler
return 500
accidentally expose details
need bounded readiness-safe handling

Analyze only.

OBSERVABILITY / LOGGING

Audit Health files for:

logging
ActivitySource
Meter
trace/correlation duplication
sensitive values

Expected likely none.

HARD-CODED RUNTIME TEXT

Classify each literal into:

protocol/health machine status
internal check key
user-facing text
sensitive detail

Report hard-coded USER-FACING runtime text separately from machine protocol values.

DEPENDENCY BOUNDARY

Required ZERO:

module Application
module Infrastructure
module Domain
DbContext
direct persistence query
business command
business workflow mutation

Allowed only if justified:

AccessControl.Contracts readiness
Host Configuration
Host Messaging/platform
MassTransit host transport
TEST / GUARD INVENTORY

Find tests/guards for:

/health/live
/health
/ready
/health/ready
readiness evaluator
messaging enabled/disabled
auth readiness
connection reference checks
route duplicates
path namespace

Identify missing coverage.

HISTORICAL CLAIMS

Inspect SoT/docs for prior Health certification or KEEP claims.

Classify:

CURRENT
HISTORICAL
STALE_METADATA
NOT_CERTIFIED

Do not accept historical certification without live proof.

DECISIVE TARGET PLAN

Recommend fewest safe waves <=20 min.

Possible outcomes:

A. one migrate wave + cert:

namespace repair
remove IServiceProvider
narrow messaging readiness seam if needed
safe response disclosure repair if needed
focused guards
then CERT

B. direct CERT if analysis proves no material production debt except namespace and namespace already intentionally exempted (unlikely under current lock)

C. two migration waves + CERT only if transport/disclosure issues cannot safely combine.

Every wave must specify:

exact files
exact behavior change/preservation
focused validation
estimate <=20 min
HOST-ONLY SCOPE

User explicitly requested finish Host only.

No module recovery.
No Catalog/Fulfillment/Checkout work.
Module files may only be inspected for contracts/consumer truth.

PRODUCTION CHANGE RULE

Analyze production change:
ZERO

Docs/evidence/SoT only.

FOCUSED VALIDATION

No solution-wide tests.
No production build unless required to resolve a consumer ambiguity.

EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-HEALTH-AMC-001/

Required:

analyze.md
route-semantics.md
readiness-semantics.md
dependency-boundary.md
service-locator.md
disclosure-security.md
path-namespace.md
tests-guards.md
historical-claims.md
migration-plan.md
recovery.md

Persist exact task:

docs/ai/tasks/TB-TMAR-HOST-HEALTH-AMC-001.task.md

RECOVERY / SOT

Analysis-only.

Do NOT advance implementation SHA.

Record:

currentHostCheckpoint = Health
mode = ANALYSIS_ONLY_NOT_MIGRATED_NOT_CERTIFIED
productionFileCount = actual
productionTypeCount = actual
routeCount = actual
pathNamespaceState
serviceLocatorState
disclosureState
messagingReadinessBoundary
recommendedWaveCount
recommendedNextTask
MultiTenancy certification = PRESERVED
Errors certification = PRESERVED
Security certification = PRESERVED
Admin certification = PRESERVED
automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_HEALTH_AMC_001
staleCurrentPointerState = ZERO
nextHostFolderStarted = false

Latest accepted implementation remains:
cfbc94d258de837fdc018ddb29db68233cc25783

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

exact Health tree enumerated
all types dispositioned
exact routes and registration mapped
liveness semantics proven
readiness check semantics/order proven
AccessControl Contracts-only boundary proven
MassTransit direct dependency classified
IServiceProvider/service locator classified
safe disclosure risk classified
raw Results.Json exception classified
machine-status vs user-facing text classified
path↔namespace debt classified
config duplication risk classified
active/inactive tenant readiness parity classified
exception safety classified
tests/guards mapped
historical claims reconciled
fewest safe waves proposed
protected certifications preserved
production change ZERO
implementation SHA unchanged
automatic next NONE
STOP for Architect review

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-HEALTH-AMC-001
Parent-Task: TB-TMAR-HOST-MULTITENANCY-AMC-001-W2-CERT
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
HostHealthEndpoints-State:
HostReadinessEvaluator-State:
Route-Inventory-State:
Liveness-State:
Readiness-State:
AccessControl-Readiness-Boundary-State:
MassTransit-Readiness-Boundary-State:
IServiceProvider-State:
ServiceLocator-State:
Disclosure-State:
Raw-ResultsJson-State:
Machine-Status-Text-State:
Path-Namespace-State:
File-Cohesion-State:
Configuration-Ownership-State:
Connection-Readiness-Truth-State:
Tenant-Readiness-Parity-State:
Messaging-Readiness-State:
Authorization-Readiness-State:
Exception-Safety-State:
Observability-State:
Sensitive-Data-State:
Hardcoded-User-Facing-Text-State:
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

Do not start Health W1.
Do not start another Host folder.
Do not open module recovery.

Wait for Architect/user review.

END_TOOBA_TASK