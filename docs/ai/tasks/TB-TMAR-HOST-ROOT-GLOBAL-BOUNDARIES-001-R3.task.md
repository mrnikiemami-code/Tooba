PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ROOT-GLOBAL-BOUNDARIES-001-R3
Parent-Task: TB-TMAR-HOST-ROOT-GLOBAL-BOUNDARIES-001-R2
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Host Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_ROOT_GLOBAL_BOUNDARIES
Title: Close AccessControl.Contracts structural certification gaps and finish Root Global Boundaries

CURRENT MAIN

HEAD: ae05267877110d21c32a77004a8385cabd17140f
R2 dependency cleanup is accepted.
Do NOT reopen Order dependency cleanup unless needed only to fix deterministic fallout.
Do NOT start Authorization cleanup yet.

ARCHITECT REVIEW OF R2

Accepted:

Order.Infrastructure foreign Application = ZERO
Order.Infrastructure foreign Infrastructure = ZERO
Order.Infrastructure foreign Domain = ZERO
Payment.Contracts direct reference = explicit
Cart/Catalog/Payment/Fulfillment/AccessControl boundaries = Contracts-only
six Host root files = ZERO
Authorization evacuation preserved

R2 certification is NOT yet accepted because the new AccessControl.Contracts project has two structural blockers.

BLOCKER 1 — PATH/NAMESPACE

Current file:
src/backend/Modules/AccessControl/Tooba.AccessControl.Contracts/Access/AccessControlEffectiveAccessContracts.cs

Current namespace:
Tooba.AccessControl.Contracts

This violates exact path-derived namespace.

Required repair:

Prefer keeping the capability folder Access/
change namespace to Tooba.AccessControl.Contracts.Access
repoint all consumers/usings exactly
no compatibility namespace alias or duplicate shim

BLOCKER 2 — STRUCTURE MANIFEST

Tooba.AccessControl.Contracts is a new project added to an already ARCH-COMPLETE-002 certified module.

Current docs/architecture/tmar-module-structure-manifests.json does not include this project.

Required repair:

add exactly one Tooba.AccessControl.Contracts project entry under the existing AccessControl module
preserve the single existing AccessControl module certification entry
do NOT create a duplicate AccessControl module entry
rootAllowlist must reflect actual disk
capability-first structure must match disk
path/namespace exact
no forbidden-root workaround

MANDATORY SKILL ORDER

.cursor/skills/tooba-architecture-analyze/SKILL.md
.cursor/skills/tooba-architecture-migrate/SKILL.md
.cursor/skills/tooba-architecture-certify/SKILL.md

This is a structural closure only.

SCOPE
Primary allowed production scope:

Modules/AccessControl/Tooba.AccessControl.Contracts/**
consumers that need namespace repointing
docs/architecture/tmar-module-structure-manifests.json
SoT/evidence/task
focused architecture tests/guards

Do NOT:

redesign AccessControl
change authorization behavior
change SpiceDB behavior
change routes
change schema/migrations
change frontend
start any other Host folder

MANDATORY CERTIFICATION
Verify AccessControl module structure after adding Contracts project:

exactly one AccessControl module manifest entry
projects include the actual canonical AccessControl project inventory, including Tooba.AccessControl.Contracts
root allowlists match disk
Access/AccessControlEffectiveAccessContracts.cs namespace = Tooba.AccessControl.Contracts.Access
zero namespace alias workaround
zero duplicate contract file
zero stale old namespace references
AccessControl.Contracts has no foreign Application/Infrastructure/Domain dependency
Order.Infrastructure remains Contracts-only
Authorization 7-file Infrastructure slice remains untouched and present
Host/Authorization remains absent
six Host root files remain absent

FOCUSED VALIDATION
Run once:

build Tooba.AccessControl.Contracts
build Tooba.AccessControl.Infrastructure
build Tooba.Order.Infrastructure
build Tooba.Host
build Tooba.Host.Tests

Focused tests only:

AccessControl structure/manifest guard(s)
OrderInfrastructureForeignLayerBoundaryGuardTests
HostRootGlobalBoundariesGuardTests
HostAuthorizationEvacuationGuardTests

No solution-wide tests.
No broad module suites.
MAX_REPAIR_ITERATIONS = 1.
If a deterministic failure occurs, repair once and rerun only the failed command.
Second failure => INCOMPLETE + exact blocker + STOP.

SOT / EVIDENCE
Create:
docs/evidence/TB-TMAR-HOST-ROOT-GLOBAL-BOUNDARIES-001-R3/

analyze.md
structural-repair.md
validation.md
certification.md

Update docs/architecture/tmar-current-state.json.

Required final state on PASS:

hostRootGlobalBoundaries001R2.certificationState = SUPERSEDED_BY_R3
hostRootGlobalBoundaries001R3.certificationState = PASS
accessControlContractsStructureState = CERTIFIED
pathNamespaceState = EXACT
accessControlManifestEntryCount = ONE
orderInfrastructureForeignApplication = ZERO
orderInfrastructureForeignInfrastructure = ZERO
orderInfrastructureForeignDomain = ZERO
hostRootGlobalBoundariesFinalState = CERTIFIED
workflowStop = USER_REVIEW_HOST_ROOT_GLOBAL_BOUNDARIES_001_R3

GIT
Work from latest main.
No reset/clean/rebase/force-push.
Preserve user work.
Commit and push main ONLY on PASS.
On INCOMPLETE, do not claim final certification.

CANONICAL RESULT
Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ROOT-GLOBAL-BOUNDARIES-001-R3
Parent-Task: TB-TMAR-HOST-ROOT-GLOBAL-BOUNDARIES-001-R2
Status: PASS | INCOMPLETE
Summary:
R2-Certification-State:
AccessControl-Contracts-Project-State:
AccessControl-Contracts-Path-Namespace-State:
AccessControl-Manifest-State:
AccessControl-Manifest-Entry-Count:
AccessControl-Structure-State:
Order-Infrastructure-Foreign-Application-State:
Order-Infrastructure-Foreign-Infrastructure-State:
Order-Infrastructure-Foreign-Domain-State:
Authorization-Preservation-State:
Six-Host-Root-Files-State:
Focused-Build-State:
Focused-Test-State:
Certification-State:
Evidence-Path:
SoT-State:
Commit-SHA:
Push-State:
HEAD-Equals-Origin-Main:
Working-Tree-Tracked-State:
User-Work-Preserved:
Repair-Iterations:
Validation-Command-Runs:
STOP
END_TOOBA_WORKER_RESULT

STOP COMPLETELY AFTER RESULT.
Do not start Authorization cleanup.
Do not start another Host folder.

END_TOOBA_TASK