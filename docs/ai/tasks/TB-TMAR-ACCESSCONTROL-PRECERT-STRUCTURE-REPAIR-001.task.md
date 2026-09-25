PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ACCESSCONTROL-PRECERT-STRUCTURE-REPAIR-001
Parent-Task: TB-TMAR-HOST-ACCESSCONTROL-FINAL-CLEANUP-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: ACCESSCONTROL_PRECERT
Title: Final AccessControl ARCH-COMPLETE-002 structure repair before certification
Backend-Only: YES

ARCHITECT-ACCEPTED-PARENT:
TB-TMAR-HOST-ACCESSCONTROL-FINAL-CLEANUP-001

ACCEPTED-PARENT-COMMIT:
ceb8ac85d25de328c22f5c3e9160fddbbbb7aed5

TIMEBOX:
Target <= 12 minutes.
Hard maximum 15 minutes.
If safe completion exceeds hard limit, return INCOMPLETE and STOP.
No retry loop.
No scope expansion.

PURPOSE:

Do NOT certify AccessControl yet.

Repair the remaining obvious ARCH-COMPLETE-002 physical-structure violations so the next task can perform a clean certification + Recovery SoT closure.

VERIFIED CURRENT STRUCTURE DEBT:

Application root currently contains capability-specific files:

src/backend/Modules/AccessControl/Tooba.AccessControl.Application/AccessControlContracts.cs
src/backend/Modules/AccessControl/Tooba.AccessControl.Application/PermissionCatalog.cs

Infrastructure root currently contains:

src/backend/Modules/AccessControl/Tooba.AccessControl.Infrastructure/AccessControlDirectory.cs
src/backend/Modules/AccessControl/Tooba.AccessControl.Infrastructure/AccessControlInstrumentation.cs
src/backend/Modules/AccessControl/Tooba.AccessControl.Infrastructure/AccessControlModule.cs
src/backend/Modules/AccessControl/Tooba.AccessControl.Infrastructure/AccessControlOutboxRegistration.cs

Under ARCH-COMPLETE-002:

capability-specific Application files must not live at root
capability/integration-specific Infrastructure implementation files must not live at root
Infrastructure root may retain the module composition entry only
path <-> namespace must match exactly
no namespace alias workaround

ONE OBJECTIVE:

Rehome ONLY these structural root files into explicit capability/infrastructure folders with exact namespace alignment, preserving all runtime behavior.

APPLICATION TARGET:

Rehome AccessControlContracts.cs into an explicit shared/capability location.

Preferred:
Application/Models/AccessControlContracts.cs
or another exact capability folder if the file contents clearly split better.

Rehome PermissionCatalog.cs into an explicit capability location.

Preferred:
Application/Permissions/PermissionCatalog.cs

Do NOT split the files unless necessary.
Do NOT redesign DTOs/contracts in this task.

INFRASTRUCTURE TARGET:

Keep only:
AccessControlModule.cs
at Infrastructure root as composition entry.

Rehome:
AccessControlDirectory.cs
preferred:
Infrastructure/Directories/AccessControlDirectory.cs

AccessControlInstrumentation.cs
preferred:
Infrastructure/Observability/AccessControlInstrumentation.cs

AccessControlOutboxRegistration.cs
preferred:
Infrastructure/Messaging/AccessControlOutboxRegistration.cs

Update namespaces to exactly match paths.

Update references/usings only as required for compile.

Do NOT use namespace aliases to hide path debt.

DO NOT CHANGE:

public HTTP routes
command/query semantics
error codes/messages
DI lifetimes
Catalog.Contracts boundary
Identity/OperatorProfile Contracts boundary
AccessControl Host evacuation state
DB schema/migrations
endpoint behavior
frontend
Checkout

ROOT ALLOWLIST TARGET:

Application root .cs files after repair:
ZERO capability-specific root .cs files.

Endpoints root:
AccessControlEndpointModule.cs only.

Infrastructure root:
AccessControlModule.cs only.

Do not invent GlobalUsings unless truly needed.

PATH/NAMESPACE AUDIT:

For all moved files:
physical folder path and namespace must match exactly.

Examples:

...Application/Permissions/PermissionCatalog.cs
namespace Tooba.AccessControl.Application.Permissions;

...Infrastructure/Directories/AccessControlDirectory.cs
namespace Tooba.AccessControl.Infrastructure.Directories;

...Infrastructure/Observability/AccessControlInstrumentation.cs
namespace Tooba.AccessControl.Infrastructure.Observability;

...Infrastructure/Messaging/AccessControlOutboxRegistration.cs
namespace Tooba.AccessControl.Infrastructure.Messaging;

Update consumers with normal using directives.

No namespace alias workaround.

VALIDATOR PRECERT AUDIT — AUDIT ONLY:

While doing the structural move, inventory all endpoint-reachable AccessControl MediatR requests.

Classify each as:
VALIDATOR_REQUIRED
or
NO_VALIDATOR_REQUIRED

Do NOT launch a broad validator implementation wave in this task.

If there is a clear missing REQUIRED transport validator, record it in Evidence and return PASS only for structure repair with:
Certification-Ready-State = NO_VALIDATOR_GAP_PENDING

If no validator gap exists:
Certification-Ready-State = YES

Do not falsely certify.

Known accepted semantics:
many current AccessControl requests are intentionally DOMAIN/APPLICATION-ENFORCED and may legitimately be NO_VALIDATOR_REQUIRED.

The next certification task will make the final validator decision.

HOST ZERO MUST REMAIN:

src/backend/Host/Tooba.Host/AccessControl = absent / ZERO

namespace Tooba.Host.AccessControl = ZERO

MapAccessControlEndpoints = ZERO

Program must retain:
app.MapAccessControlModuleEndpoints();

BOUNDARY AUDIT MUST REMAIN CLEAN:

AccessControl.Application:

no Host
no foreign Application/Domain
Catalog.Contracts allowed
Identity.Contracts allowed
OperatorProfile.Contracts allowed

AccessControl.Infrastructure:

no Catalog.Application
no Catalog.Domain
Catalog.Contracts allowed

AccessControl.Endpoints:

no Host
no direct IAccessControlDirectory
ISender application dispatch

CANONICAL TASK ARTIFACT:

Commit this exact task at:

docs/ai/tasks/TB-TMAR-ACCESSCONTROL-PRECERT-STRUCTURE-REPAIR-001.task.md

EVIDENCE:

Create:

docs/evidence/TB-TMAR-ACCESSCONTROL-PRECERT-STRUCTURE-REPAIR-001/precert-structure-repair.md

Evidence must include:

root files before/after
exact move map
path/namespace proof
Application root state
Endpoints root state
Infrastructure root state
Host ZERO confirmation
boundary audit
endpoint-reachable request inventory
validator classification counts/list
any validator gap
whether module is ready for certification
focused build/test results

FOCUSED VALIDATION ONLY:

dotnet build src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Tooba.AccessControl.Application.csproj --no-restore
dotnet build src/backend/Modules/AccessControl/Tooba.AccessControl.Infrastructure/Tooba.AccessControl.Infrastructure.csproj --no-restore
dotnet build src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Tooba.AccessControl.Endpoints.csproj --no-restore
dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore

Run ONLY directly relevant AccessControl boundary/static tests.

No solution build.
No broad integration suite.
No broad architecture suite.
No retries.

RECOVERY HONESTY:

AccessControl Host evacuation is COMPLETE and must remain so.

AccessControl is still:
NOT COMPLETE_REFERENCE_PATTERN
NOT ARCH-COMPLETE-002 STRUCTURE_CERTIFIED

Do NOT update certifiedModules.
Do NOT perform final Recovery SoT closure here.

EXPECTED NEXT TASK AFTER PASS:

TB-TMAR-ACCESSCONTROL-FINAL-CERTIFICATION-AND-SOT-CLOSURE-001

That task should be the actual final AccessControl certification/closure if this pre-cert repair reports no remaining blockers.

PASS ONLY IF:

root structural debt above is repaired
moved namespaces exactly match physical paths
Application root contains no capability-specific .cs files
Endpoints root contains only AccessControlEndpointModule.cs
Infrastructure root contains only AccessControlModule.cs
Host AccessControl remains ZERO
boundaries remain clean
endpoint-reachable request/validator inventory is documented
focused builds pass
focused boundary test passes if present
canonical task/evidence committed
hard timebox respected

If structure repair cannot safely finish inside 15 minutes:
Status = INCOMPLETE
STOP.
Do not loop.
Do not auto-start certification.

CANONICAL RESULT — RETURN ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-ACCESSCONTROL-PRECERT-STRUCTURE-REPAIR-001
Parent-Task: TB-TMAR-HOST-ACCESSCONTROL-FINAL-CLEANUP-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-State:
Application-Root-State:
Endpoints-Root-State:
Infrastructure-Root-State:
Move-Map-State:
Path-Namespace-State:
Host-Zero-State:
Application-Boundary-State:
Infrastructure-Boundary-State:
Endpoint-Boundary-State:
Endpoint-Reachable-Request-Count:
Validator-Required-Count:
No-Validator-Required-Count:
Validator-Gaps:
Validator-Inventory-State:
Certification-Ready-State:
Focused-Builds:
Focused-Tests:
Canonical-Task-State:
Evidence:
Production-Code-Scope:
Checkout-State:
Frontend-Production-Changes:
Residual-Defects:
Git:
User-Work-Preserved:
Recovery-Next-Task:
Next-Recommended-Task:
END_TOOBA_WORKER_RESULT

STOP
Do not auto-start another task.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK
