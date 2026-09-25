PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ACCESSCONTROL-FINAL-CLEANUP-001
Parent-Task: TB-TMAR-ACCESSCONTROL-CATALOG-BOUNDARY-REPAIR-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FIRST_ACCESSCONTROL
Title: Final AccessControl Host cleanup — retire legacy dev demo and reach Host folder ZERO
Backend-Only: YES

ARCHITECT-ACCEPTED-PARENT:
TB-TMAR-ACCESSCONTROL-CATALOG-BOUNDARY-REPAIR-001

ACCEPTED-PARENT-COMMIT:
6dd61ab5461cf6628d5a1c4b4497c02adcf6310c

TIMEBOX:
Target <= 10 minutes.
Hard maximum 15 minutes.
If safe completion exceeds the hard limit, return INCOMPLETE and STOP.
No retry loop.
No scope expansion.

ONE OBJECTIVE:

Reach:

src/backend/Host/Tooba.Host/AccessControl = ZERO files

by retiring the remaining AccessControl-specific legacy Development demo surface and removing Program.cs Host residue.

CURRENT VERIFIED HOST FILES:

src/backend/Host/Tooba.Host/AccessControl/AccessControlEndpoints.cs
src/backend/Host/Tooba.Host/AccessControl/AccessControlDevelopmentSeed.cs
src/backend/Host/Tooba.Host/AccessControl/AccessControlDemoSnapshot.cs

CURRENT VERIFIED PROGRAM RESIDUE:

Development-only legacy call:

await AccessControlDevelopmentSeed.ApplyAsync(app.Services);

inside the legacy Catalog bootstrap branch guarded by:

app.Environment.IsDevelopment()
catalogDemoOptions.RunLegacyBootstraps

Host-specific route mapping:

app.MapAccessControlEndpoints();

Module mapping that MUST REMAIN:

app.MapAccessControlModuleEndpoints();

ARCHITECTURE DECISION:

Do NOT rehome AccessControlDevelopmentSeed.

The remaining seed/snapshot/preview form one legacy Development-only demo feature with broad cross-module orchestration.

Retire that feature completely rather than moving its Host coupling into AccessControl or another module.

This task intentionally removes:

GET /v1/admin/access-control/demo-preview

and its Development-only snapshot/seed infrastructure.

This is acceptable because:

the route is already Development-only in behavior (404 outside Development)
it depends solely on AccessControlDemoSnapshot
the snapshot is populated solely by AccessControlDevelopmentSeed
the seed is invoked solely from the legacy Development bootstrap branch
none of this is production AccessControl authority

Do NOT recreate demo-preview elsewhere.
Do NOT move the 33KB seed into AccessControl.Application/Infrastructure.
Do NOT create a generic demo abstraction just to preserve dead legacy behavior.

MANDATORY REMOVALS:

Delete:

Host/Tooba.Host/AccessControl/AccessControlEndpoints.cs
Host/Tooba.Host/AccessControl/AccessControlDevelopmentSeed.cs
Host/Tooba.Host/AccessControl/AccessControlDemoSnapshot.cs

Program.cs:

remove using/reference needed only for Tooba.Host.AccessControl
remove the AccessControlDevelopmentSeed.ApplyAsync(...) try/catch block
remove app.MapAccessControlEndpoints()
KEEP app.MapAccessControlModuleEndpoints()

If deleting the seed makes any using in Program.cs dead, remove only those dead usings.

DO NOT modify unrelated bootstraps.

MANDATORY ZERO AUDIT:

After cleanup, production repo must have:

namespace Tooba.Host.AccessControl = ZERO
MapAccessControlEndpoints = ZERO
AccessControlDevelopmentSeed = ZERO
AccessControlDemoSnapshot = ZERO
AccessControlDemoContext = ZERO

Directory:
src/backend/Host/Tooba.Host/AccessControl

must contain ZERO files / be absent.

Program.cs must contain:
app.MapAccessControlModuleEndpoints();

exactly as the live AccessControl module mapping.

HTTP OWNERSHIP:

All non-demo AccessControl HTTP ownership is already module-owned.

Do NOT alter existing module routes.

Do NOT delete:
MapAccessControlModuleEndpoints

Do NOT remove:
IAdminPanelAccess
ISellerPanelAccess
IPlatformEffectiveAccessReader
or other generic/shared platform security seams used by AccessControl or other modules.

TEST MAINTENANCE:

Inspect the focused AccessControl static/boundary test for references to:

demo-preview
MapAccessControlEndpoints
Host/AccessControl folder/files
AccessControlDevelopmentSeed
AccessControlDemoSnapshot

Update ONLY stale assertions directly caused by this final cleanup.

Preferred final assertions:

Host AccessControl folder/file residue = ZERO
namespace Tooba.Host.AccessControl = ZERO
Host MapAccessControlEndpoints = ZERO
module MapAccessControlModuleEndpoints remains present

No broad test rewrite.
No unrelated assertions.

FOCUSED VALIDATION ONLY:

Mandatory build:
dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore

Also build:
dotnet build src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Tooba.AccessControl.Endpoints.csproj --no-restore

Run ONLY directly relevant AccessControl static/boundary tests.

No solution build.
No broad integration suite.
No broad architecture suite.
No retries.

RECOVERY / EVIDENCE:

Create:

docs/evidence/TB-TMAR-HOST-ACCESSCONTROL-FINAL-CLEANUP-001/accesscontrol-host-final-cleanup.md

Evidence must include:

3 Host files before -> ZERO after
Program residue removed
MapAccessControlModuleEndpoints preserved
demo-preview/seed/snapshot retirement decision
explicit Development-only proof
namespace/type/string zero audit
focused build/test results
remaining generic Host security seams, if any, classified as shared platform seams rather than AccessControl Host ownership
next step = AccessControl structure/certification audit

CANONICAL TASK ARTIFACT:

Commit this exact task at:

docs/ai/tasks/TB-TMAR-HOST-ACCESSCONTROL-FINAL-CLEANUP-001.task.md

Do not omit it.

PROTECTED STATE:

Do NOT touch:
Frontend
Checkout
Fulfillment
Order
Payment
Settlement
Cart
StoreContext
Offer
other Host folders
DB/schema/migrations
AccessControl business semantics
Catalog boundary already repaired

RECOVERY HONESTY:

PASS of this task means:

Host AccessControl evacuation = COMPLETE
Host AccessControl folder = ZERO

But PASS does NOT yet mean:
COMPLETE_REFERENCE_PATTERN
or
ARCH-COMPLETE-002 STRUCTURE_CERTIFIED

Those require a separate final AccessControl certification/audit task.

Do NOT add AccessControl to certifiedModules in this task.

Do NOT falsely mark structure certification complete.

RECOVERY SOT NOTE:

Do not broadly rewrite Recovery SoT in this production cleanup task.

Record in evidence:
next recommended step = focused AccessControl final certification + Recovery SoT closure.

A dedicated final certification/closure task will update canonical recovery pointers after Architect acceptance.

PASS ONLY IF:

all 3 Host AccessControl files are gone
Host AccessControl directory is ZERO/absent
Program AccessControlDevelopmentSeed call is gone
Program MapAccessControlEndpoints call is gone
Program MapAccessControlModuleEndpoints remains
namespace Tooba.Host.AccessControl is ZERO production
AccessControlDevelopmentSeed/DemoSnapshot/DemoContext are ZERO production
no non-demo module route ownership changes
focused builds pass
focused AccessControl boundary test passes if present
canonical task/evidence committed
hard timebox respected

If safe completion exceeds hard limit:
Status = INCOMPLETE
STOP IMMEDIATELY.
Do not loop.
Do not rehome the legacy demo.
Do not auto-start certification.

CANONICAL RESULT — RETURN ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ACCESSCONTROL-FINAL-CLEANUP-001
Parent-Task: TB-TMAR-ACCESSCONTROL-CATALOG-BOUNDARY-REPAIR-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-State:
Legacy-Demo-Retirement-State:
Demo-Preview-State:
Development-Seed-State:
Demo-Snapshot-State:
Program-Bootstrap-Residue-State:
Program-Host-Map-State:
Module-Map-State:
Host-AccessControl-Folder-State:
Host-AccessControl-File-Count:
Host-Namespace-Residue:
Host-Type-Residue:
Route-Ownership-State:
Shared-Platform-Seams-State:
Focused-Builds:
Focused-Tests:
Canonical-Task-State:
Evidence:
Production-Code-Scope:
Test-Code-Scope:
Checkout-State:
Frontend-Production-Changes:
Host-Evacuation-State:
Structure-Certification-State:
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
