PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ACCESSCONTROL-CATALOG-BOUNDARY-REPAIR-001
Parent-Task: TB-TMAR-HOST-ACCESSCONTROL-SCOPE-RESOURCES-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FIRST_ACCESSCONTROL
Title: Remove AccessControl.Infrastructure -> Catalog.Application dependency via Catalog.Contracts
Backend-Only: YES

ARCHITECT-ACCEPTED-PARENT:
TB-TMAR-HOST-ACCESSCONTROL-SCOPE-RESOURCES-001

ACCEPTED-PARENT-COMMIT:
40c788a9d2ba5787d2e757527888ddacc10aa73f

TIMEBOX:
Target <= 10 minutes.
Hard maximum 15 minutes.
If clean completion would exceed the hard limit, return INCOMPLETE and STOP.
No retry loop.
No scope expansion.

ONE OBJECTIVE:

Remove the remaining direct architecture leak:

Tooba.AccessControl.Infrastructure
-> Tooba.Catalog.Application
-> ICatalogLookupGateway

AccessControl.Infrastructure must consume only Catalog.Contracts for the two remaining Category-scope needs.

CURRENT VERIFIED LEAK:

File:
src/backend/Modules/AccessControl/Tooba.AccessControl.Infrastructure/AccessControlDirectory.cs

Current using:
using Tooba.Catalog.Application;

Current field:
ICatalogLookupGateway _catalog

Current usages include:

category existence validation:
_catalog.FindCategoryAsync(categoryId, ct)

category display-name lookup:
_catalog.GetCategoryNamesAsync(categoryIds, ct)

Project reference:
src/backend/Modules/AccessControl/Tooba.AccessControl.Infrastructure/Tooba.AccessControl.Infrastructure.csproj

currently references:
....\Catalog\Tooba.Catalog.Application\Tooba.Catalog.Application.csproj

This reference must be removed.

ARCHITECTURE DECISION:

Use the Catalog.Contracts seam introduced by the accepted parent task.

Preferred approach:

Extend:
Tooba.Catalog.Contracts/AccessControlScopeResourceContracts.cs

with the minimum Category-scope operations AccessControl needs, for example:

Task<bool> CategoryExistsAsync(Guid categoryId, CancellationToken ct)

Task<IReadOnlyDictionary<Guid,string>> GetCategoryNamesAsync(
IReadOnlyCollection<Guid> categoryIds,
CancellationToken ct)

Names may vary if the existing contract style suggests a cleaner equivalent.

The contract must remain neutral and dependency-light.

Then extend the existing Catalog-owned implementation:

CatalogAccessControlScopeResourceLookup

to delegate internally to ICatalogLookupGateway.

AccessControl.Infrastructure injects:
IAccessControlScopeResourceLookup

instead of:
ICatalogLookupGateway

Do NOT create a new AccessControl-specific adapter in Host.
Do NOT expose Catalog.Application types through Catalog.Contracts.
Do NOT move business validation into Catalog.

BEHAVIOR PARITY:

SetSellerCeilingAsync:

Category scope resource existence check remains identical
unknown category still maps to:
access.scope.unknown_resource
no change to AccessControlException code/message semantics

ValidateGrantsForOwnerAsync:

Category scope validation remains identical
missing category remains same stable AccessControlException

GetEffectiveAccessAsync:

category display names remain populated from Catalog
dictionary semantics remain equivalent
missing category name remains null as before

No change to:

ceiling rules
permission rules
seller escalation rules
role/assignment behavior
endpoint behavior
HTTP contracts

MANDATORY DEPENDENCY RESULT:

After this task:

AccessControl.Infrastructure -> Catalog.Application = ZERO
AccessControl.Infrastructure -> Catalog.Domain = ZERO

AccessControl.Infrastructure -> Catalog.Contracts = ALLOWED

AccessControl.Application -> Catalog.Contracts = ALLOWED

AccessControl.Endpoints -> Catalog.Application = ZERO
AccessControl.Endpoints -> Catalog.Domain = ZERO

Host AccessControl -> Catalog.Application = ZERO

PROJECT REFERENCES:

Remove:
Catalog.Application project reference
from:
Tooba.AccessControl.Infrastructure.csproj

Add:
Catalog.Contracts project reference
only if not already transitively/explicitly available.

Prefer explicit direct reference when code directly consumes Catalog.Contracts.

CATALOG OWNERSHIP:

Catalog-owned implementation remains in Catalog.Infrastructure.

Catalog.Contracts must not reference:
Catalog.Application
Catalog.Domain
Catalog.Infrastructure

Catalog Infrastructure may depend on Catalog.Application internally.

OUT OF SCOPE:

No route movement.
No endpoint changes unless compile-only using cleanup is strictly required.
No Host demo-preview cleanup.
No DevelopmentSeed/DemoSnapshot work.
No Program.cs cleanup.
No AccessControl certification.
No schema changes.
No frontend.
No Checkout.

TESTING:

Run only essential focused validation.

Mandatory builds:

dotnet build src/backend/Modules/Catalog/Tooba.Catalog.Contracts/Tooba.Catalog.Contracts.csproj --no-restore
dotnet build src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Tooba.Catalog.Infrastructure.csproj --no-restore
dotnet build src/backend/Modules/AccessControl/Tooba.AccessControl.Infrastructure/Tooba.AccessControl.Infrastructure.csproj --no-restore
dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore

Run only an existing directly relevant focused test if one already covers category scope validation/effective display names.

Do NOT create a broad suite.
No solution build.
No broad architecture suite.
No retries.

MANDATORY AUDIT:

Verify repo/project state:

AccessControl.Infrastructure.csproj contains ZERO Catalog.Application reference
AccessControl.Infrastructure source contains ZERO:
Tooba.Catalog.Application
ICatalogLookupGateway
AccessControl.Infrastructure source contains only Catalog.Contracts seam for Catalog dependency
Catalog.Contracts contains no Catalog.Application/Domain/Infrastructure references
Existing CatalogAccessControlScopeResourceLookup owns the implementation
stable AccessControl error codes remain unchanged
no production route ownership changed

CANONICAL TASK ARTIFACT:

Commit this exact Architect-issued task at:

docs/ai/tasks/TB-TMAR-ACCESSCONTROL-CATALOG-BOUNDARY-REPAIR-001.task.md

EVIDENCE:

Create:

docs/evidence/TB-TMAR-ACCESSCONTROL-CATALOG-BOUNDARY-REPAIR-001/catalog-boundary-repair.md

Evidence must include:

before dependency edge
after dependency edge
exact Catalog.Contracts members added/reused
Catalog-owned implementation mapping
project-reference before/after
category-existence parity
category-name parity
stable error-code parity
focused builds/tests
statement that route ownership did not change
remaining AccessControl Host residue

RECOVERY HONESTY:

AccessControl remains IN_PROGRESS.
Do NOT mark COMPLETE_REFERENCE_PATTERN.
Do NOT structure-certify AccessControl.

Expected next step after PASS:
final Host AccessControl cleanup:

demo-preview
AccessControlDevelopmentSeed.cs
AccessControlDemoSnapshot.cs
Program.cs legacy AccessControl residue
Host AccessControl folder -> ZERO

Certification remains a separate task after Host cleanup.

PASS ONLY IF:

Catalog.Application reference removed from AccessControl.Infrastructure
ICatalogLookupGateway removed from AccessControl.Infrastructure
Catalog.Contracts seam used instead
category validation/display behavior preserved
stable error codes preserved
focused builds pass
no route ownership changes
canonical task/evidence committed
hard timebox respected

If clean completion exceeds hard limit:
Status = INCOMPLETE
STOP IMMEDIATELY.
Do not loop.
Do not broaden scope.
Do not auto-start another task.

CANONICAL RESULT — RETURN ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-ACCESSCONTROL-CATALOG-BOUNDARY-REPAIR-001
Parent-Task: TB-TMAR-HOST-ACCESSCONTROL-SCOPE-RESOURCES-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-State:
Catalog-Contracts-State:
Catalog-Implementation-State:
AccessControl-Infrastructure-Dependency-State:
Project-Reference-State:
Category-Existence-Parity-State:
Category-Name-Parity-State:
Stable-Error-Code-State:
Route-Ownership-State:
Boundary-Audit-State:
Focused-Builds:
Focused-Tests:
Canonical-Task-State:
Evidence:
Production-Code-Scope:
Checkout-State:
Frontend-Production-Changes:
Residual-Host-AccessControl-State:
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
