PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ACCESSCONTROL-CAPABILITY-GATE-001-R1
Parent-Task: TB-TMAR-HOST-ACCESSCONTROL-ME-CAPABILITIES-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FIRST_ACCESSCONTROL
Title: Canonicalize unsolicited capability-gate move and verify parity
Backend-Only: YES

Architect state:

TB-TMAR-HOST-ACCESSCONTROL-ME-CAPABILITIES-001 is ARCHITECT-ACCEPTED at
50a31d2b3e92739e8290db870c933dc612d15570.
Commit f059c82544c9710c6beb8301f51be2d41930c767 exists on main and moved the capability gate,
but it was NOT issued by the Architect and its canonical task artifact is missing.
Do NOT revert blindly. First verify and canonicalize the current state.

TIMEBOX:

Target <= 15 minutes.
No new migration family.
If a semantic defect is found that cannot be repaired inside the timebox, return INCOMPLETE and STOP.

ONE OBJECTIVE:
Adopt or repair the current capability-gate relocation so repo state is canonical and semantically safe.

VERIFY:

AccessControlCapabilityGate.EnsureAsync is the only production implementation of the former Host capability-gate policy.
All previous Host call sites are re-pointed.
Branch order and behavior are exactly preserved:
Allow => return
accesscontrol.view => return
Unavailable => 503 access.authorization.unavailable
accesscontrol.manage => return
otherwise 403 access.capability.denied
AuthorizationCheck shape is unchanged:
subject/user, permission resource, check relation, SingleStore edition, tenant id or "unknown".
Host EnsureCapabilityAsync definition/references = ZERO.
No HTTP/Host dependency entered AccessControl.Application.
Bootstrap and me/capabilities routes remain unchanged from accepted state.

CANONICALIZATION:

Add this R1 task artifact under docs/ai/tasks/.
Create/update focused evidence:
docs/evidence/TB-TMAR-HOST-ACCESSCONTROL-CAPABILITY-GATE-001-R1/capability-gate-canonicalization.md
Do NOT recreate the missing unissued parent task file.
Record f059c825... as an unsolicited intermediate commit adopted only after this R1 verification.

DO NOT:

migrate permission catalog or any route
alter fail-open behavior in this task
touch DevelopmentSeed/DemoSnapshot
touch Fulfillment/Checkout/frontend
run broad tests
add validators

VALIDATION:

targeted source searches only
dotnet build src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Tooba.AccessControl.Application.csproj --no-restore
dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore
no test suite / no solution build / no retries

PASS only if:

current capability-gate move is semantically identical and architecturally acceptable
canonical R1 task + evidence exist
no unrelated production changes are made
next task remains permission-catalog migration

On PASS:
Recovery-Next-Task = TB-TMAR-HOST-ACCESSCONTROL-PERMISSION-CATALOG-001

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ACCESSCONTROL-CAPABILITY-GATE-001-R1
Parent-Task: TB-TMAR-HOST-ACCESSCONTROL-ME-CAPABILITIES-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Unsolicited-Commit-State:
Capability-Gate-Single-Implementation:
Host-Policy-Residue:
Branch-Parity-State:
AuthorizationCheck-Parity-State:
Application-Boundary-State:
Accepted-Routes-State:
Canonical-Task-State:
Evidence:
Focused-Builds:
Production-Code-Changes:
Checkout-State:
Frontend-Production-Changes:
Residual-Defects:
Git:
User-Work-Preserved:
Recovery-Next-Task:
Next-Recommended-Task:
END_TOOBA_WORKER_RESULT

STOP
Do not auto-start the next task.
Wait for Architect verification.

END_TOOBA_TASK
