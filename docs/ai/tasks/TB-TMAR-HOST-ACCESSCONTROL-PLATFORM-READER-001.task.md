PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ACCESSCONTROL-PLATFORM-READER-001
Parent-Task: TB-TMAR-HOST-ACCESSCONTROL-INVENTORY-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FIRST_ACCESSCONTROL
Title: Evacuate HostPlatformEffectiveAccessReader from Host
Backend-Only: YES

Architect verdict:
TB-TMAR-HOST-ACCESSCONTROL-INVENTORY-001 is ARCHITECT-ACCEPTED at
1065a97b8789a9bd34bd4f289c05fa2d7483c983.

Architect ruling:

Do NOT create an AccessControl.Endpoints skeleton that still depends on Host handlers.
No temporary Endpoints -> Host dependency.
Move one complete responsibility at a time.

TIMEBOX:

Target <= 15 minutes.
If broader refactor is required, return INCOMPLETE and STOP.

ONE OBJECTIVE:
Remove HostPlatformEffectiveAccessReader from Host by moving its implementation, unchanged semantically, into AccessControl-owned Infrastructure.

Current source:
src/backend/Host/Tooba.Host/AccessControl/HostPlatformEffectiveAccessReader.cs

Required end state:

Host file absent.
Implementation lives under:
src/backend/Modules/AccessControl/Tooba.AccessControl.Infrastructure/
in a capability/integration folder such as Adapters/Security/ or the closest canonical existing integration location.
Namespace matches physical path exactly.
It continues implementing IPlatformEffectiveAccessReader.
Mapping behavior remains identical:
PlatformAccessOwnerKind -> AccessOwnerScopeKind
seller ownerScopeId propagation
GetEffectiveAccessAsync call
PermissionId
scope mapping
ScopeResourceId
DeniedByCeiling

DI:

Re-point existing Host composition registration to the new AccessControl.Infrastructure type.
Host remains composition root only.
Do not add service locator or duplicate implementation.

Boundaries:

BuildingBlocks.Security seam remains unchanged.
Fulfillment code unchanged.
Do not move or redesign IPlatformEffectiveAccessReader.
Do not touch AccessControlEndpoints.cs.
Do not touch AccessControlDemoSnapshot.cs.
Do not touch AccessControlDevelopmentSeed.cs.
Do not create AccessControl.Contracts or AccessControl.Endpoints in this task.
No CQRS work in this task.

Validation:

targeted repo search: old Host type/file = zero
new implementation = exactly one production implementation
DI registration points to new implementation
dotnet build src/backend/Modules/AccessControl/Tooba.AccessControl.Infrastructure/Tooba.AccessControl.Infrastructure.csproj --no-restore
if Host compilation is required by changed DI, at most one:
dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore
no test suite
no solution build
no retry loop

Evidence:
docs/evidence/TB-TMAR-HOST-ACCESSCONTROL-PLATFORM-READER-001/platform-reader-evacuation.md

PASS only if:

semantic mapping preserved
Host file removed only after destination implementation exists
old Host type references = zero
one production implementation remains
DI resolves to AccessControl.Infrastructure implementation
Fulfillment untouched
Checkout/frontend untouched

On PASS:
Recovery-Next-Task = TB-TMAR-HOST-ACCESSCONTROL-ENDPOINTS-FOUNDATION-001

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ACCESSCONTROL-PLATFORM-READER-001
Parent-Task: TB-TMAR-HOST-ACCESSCONTROL-INVENTORY-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Source-Host-File-State:
Destination-State:
Namespace-Path-State:
Semantic-Parity-State:
Implementation-Count:
DI-State:
Host-AccessControl-Reader-References:
Focused-Builds:
Production-Code-Scope:
Evidence:
Checkout-State:
Frontend-Production-Changes:
Residual-Defects:
Git:
User-Work-Preserved:
Recovery-Next-Task:
Next-Recommended-Task:
END_TOOBA_WORKER_RESULT

STOP
Do not start the endpoint migration automatically.
Wait for Architect verification.

END_TOOBA_TASK
