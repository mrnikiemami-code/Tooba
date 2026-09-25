PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-RECOVERY-SOT-SYNC-ACCESSCONTROL-001
Parent-Task: TB-TMAR-HOST-ACCESSCONTROL-USER-SEARCH-EFFECTIVE-SEAM-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: RECOVERY_SOT_SYNC
Title: Sync TMAR Recovery SoT to current AccessControl Host evacuation state
Backend-Only: YES
Docs-Only: YES

ARCHITECT-ACCEPTED-PARENT:
TB-TMAR-HOST-ACCESSCONTROL-USER-SEARCH-EFFECTIVE-SEAM-001

ACCEPTED-PARENT-COMMIT:
4a6074e62fbaf557f57aa2770d76b8d14164dc72

TIMEBOX:
Target <= 5 minutes.
Hard maximum 10 minutes.
If safe completion would exceed the timebox, return INCOMPLETE and STOP.
No retries.
No production code changes.

ONE OBJECTIVE:

Bring the canonical TMAR Recovery SoT up to the real accepted repository state so a new chat can resume accurately without relying on conversation memory.

MANDATORY FILES:

docs/architecture/tmar-current-state.json
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

DO NOT touch production code.
DO NOT touch tests.
DO NOT alter architecture locks.
DO NOT start the next AccessControl implementation task.

CURRENT ACCEPTED REALITY TO RECORD:

Latest accepted task:
TB-TMAR-HOST-ACCESSCONTROL-USER-SEARCH-EFFECTIVE-SEAM-001

Latest accepted commit:
4a6074e62fbaf557f57aa2770d76b8d14164dc72

Workflow:
HOST_FIRST_FOLDER_BY_FOLDER

Current active Host folder/module:
AccessControl

AccessControl state:
IN_PROGRESS
NOT COMPLETE_REFERENCE_PATTERN
NOT STRUCTURE_CERTIFIED
Host residue is still NON-ZERO

Accepted AccessControl progress that must be recoverable from SoT:

Permission catalog module-owned
Admin platform role reads/writes/clone/archive/permissions module-owned
AdminSeller complete role + permissions family module-owned
Seller complete role + permissions family module-owned
Seller ceiling + assignments module-owned
AdminSeller ceiling + assignments + effective module-owned
Admin platform assignments + effective module-owned
Admin + Seller user search module-owned
Seller effective module-owned
user-search enrichment boundary is Contracts-only:
Identity.Contracts/IActorContactLookup
Identity.Contracts/IActorIdentifierResolver
OperatorProfile.Contracts/IActorDisplayLookup
no AccessControl.Application dependency on Identity.Application/Domain or OperatorProfile.Application/Domain

CURRENT HOST ACCESSCONTROL RESIDUE TO RECORD:

AccessControlEndpoints.cs still owns:

Admin scope-resources:
categories
brands
products
warehouses deferred
stores deferred
order-segments deferred
Seller scope-resources:
categories
brands
products
warehouses deferred
stores deferred
order-segments deferred
Admin demo-preview
residual shared helpers only as actually still used

Separate Host folder files still present:

AccessControlDevelopmentSeed.cs
AccessControlDemoSnapshot.cs
AccessControlEndpoints.cs

Program still has legacy AccessControl Host mapping/bootstrap residue until final cleanup.

IMPORTANT KNOWN DEFECT / TEST DEBT TO RECORD:

Tooba.Host.Tests/AccessControlFoundationTests.AccessControl_module_boundary_static_checks is stale.

It still expects old Host route/group text such as:

/v1/admin/sellers/{sellerId:guid}/access-control
/me/capabilities

Those routes were already correctly evacuated in previously Architect-accepted tasks.

This is TEST-MAINTENANCE DEBT, not a production regression.

Do NOT fix the test in this docs-only task.
Record it as a pending focused maintenance item so it is not lost.

NEXT IMPLEMENTATION TASK TO RECORD:

TB-TMAR-HOST-ACCESSCONTROL-SCOPE-RESOURCES-001

Intent:
Evacuate Admin + Seller scope-resources family from Host, with a proper Catalog Contracts/shared-neutral seam rather than moving ICatalogLookupGateway from Catalog.Application into AccessControl.

The stale AccessControlFoundationTests assertion may be repaired in that task only if explicitly scoped and tiny; otherwise use a separate focused test-maintenance task.

RECOVERY SOT REQUIRED TOP-LEVEL CORRECTIONS:

In tmar-current-state.json update stale fields including, at minimum:

lastAcceptedTask
lastAcceptedCommit
lastAcceptedSoTStamp
lastAcceptedNote
nextTask
nextTaskGate

Update currentHostEvacuation so it no longer says Fulfillment is active.

It must identify:
activeModule = AccessControl
active/current accepted state aligned to the latest accepted AccessControl task
remaining Host residue honestly represented
next task = TB-TMAR-HOST-ACCESSCONTROL-SCOPE-RESOURCES-001

Do NOT delete historical Fulfillment certification data.
Only correct current/live recovery pointers and add/update AccessControl recovery state.

MASTER RECOVERY UPDATE:

TOOBA-TMAR-MASTER-RECOVERY.md must contain a concise current AccessControl recovery section with:

latest accepted task + commit
current track/folder
accepted migration summary
current residual Host files/routes
Contracts-only user-search boundary
stale focused test assertion debt
exact next task
frontend/checkout state

Keep historical material.
Do not rewrite the document broadly.

LOCKED GLOBAL STATE TO PRESERVE:

Mode:
BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

Frontend:
FROZEN

Checkout:
PAUSED_AT_SAFE_W5_CHECKPOINT

Strict ARCH-COMPLETE-002 structure-certified modules remain:
Order
Cart
StoreContext
Offer
Payment
Settlement
Fulfillment

Do NOT add AccessControl to the certified list.

CANONICAL TASK ARTIFACT:

Commit this exact task at:

docs/ai/tasks/TB-TMAR-RECOVERY-SOT-SYNC-ACCESSCONTROL-001.task.md

EVIDENCE:

Create:

docs/evidence/TB-TMAR-RECOVERY-SOT-SYNC-ACCESSCONTROL-001/recovery-sot-sync.md

Evidence must state:

stale before-state
corrected last accepted task/commit
corrected currentHostEvacuation
AccessControl residue summary
pending stale-test debt
next task
production code changed = NO
tests changed = NO

VALIDATION:

Docs-only validation only.

Mandatory:

parse docs/architecture/tmar-current-state.json successfully
verify no production code files changed
verify no test files changed
verify AccessControl is NOT marked structure-certified
verify current active Host evacuation is AccessControl
verify latest accepted task/commit are exact
verify next task is exact

Do NOT run dotnet build.
Do NOT run tests.
Do NOT run solution validation.

PASS ONLY IF:

both canonical Recovery SoT files are synchronized
historical data preserved
current pointers no longer stale
AccessControl state remains honestly IN_PROGRESS
stale test debt recorded
exact next task recorded
canonical task + evidence committed
zero production/test changes

CANONICAL RESULT — RETURN ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-RECOVERY-SOT-SYNC-ACCESSCONTROL-001
Parent-Task: TB-TMAR-HOST-ACCESSCONTROL-USER-SEARCH-EFFECTIVE-SEAM-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-State:
Current-State-Json-State:
Master-Recovery-State:
Last-Accepted-State:
Current-Host-Evacuation-State:
AccessControl-Recovery-State:
AccessControl-Residue-State:
Contracts-Boundary-State:
Stale-Test-Debt-State:
Next-Task-State:
Architecture-Certification-State:
Frontend-State:
Checkout-State:
Production-Code-Changes:
Test-Code-Changes:
Json-Parse-State:
Canonical-Task-State:
Evidence:
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
