PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-AUTHORIZATION-POSTCERT-CLEANUP-001
Parent-Task: TB-TMAR-HOST-AUTHORIZATION-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Authorization Post-Cert Cleanup
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: AUTHORIZATION_POSTCERT_CLEANUP
Title: Remove Host readiness Infrastructure coupling and correct AppliedVersion semantics

CURRENT MAIN

HEAD: 7d8ea21155109def56866eee2acdab2067fb457b
Root Global Boundaries R3 is final certified.
Authorization evacuation remains accepted and preserved.
Do not reopen any other Host folder.

MANDATORY SKILL ORDER

.cursor/skills/tooba-architecture-analyze/SKILL.md
.cursor/skills/tooba-architecture-migrate/SKILL.md
.cursor/skills/tooba-architecture-certify/SKILL.md

SCOPE — EXACTLY TWO AUTHORIZATION DEBTS

DEBT A — HOST READINESS MUST NOT CONSUME ACCESSCONTROL INFRASTRUCTURE TYPES

Current Host readiness directly consumes:

Tooba.AccessControl.Infrastructure.Authorization.SpiceDbAuthorizationOptions
SpiceDbHealthProbe

This is tolerated composition debt from the evacuation, but not the final canonical boundary.

Required target:

Host readiness depends only on a narrow neutral/module contract.
Prefer the now-existing Tooba.AccessControl.Contracts project.
Use a capability-first folder such as Readiness/ with exact path-derived namespace.
Contract may expose only the minimum safe readiness information/operation Host needs.
Do NOT expose token/secret/raw infrastructure options.
Do NOT expose SpiceDB SDK types.
Do NOT move Host health logic into AccessControl.
AccessControl.Infrastructure implements the contract.
Host HostReadinessEvaluator consumes the contract only.
Host must have ZERO source references to:
SpiceDbAuthorizationOptions
SpiceDbHealthProbe
Tooba.AccessControl.Infrastructure.Authorization
for readiness behavior.

Preserve readiness semantics:

non-SpiceDb modes remain ready according to existing behavior
SpiceDb endpoint missing => not ready / same safe check state
SpiceDb token missing => not ready / same safe check state
readiness probe disabled => no remote probe, existing semantics preserved
unreachable SpiceDb => not ready
no secret/token emitted in readiness output or logs

DEBT B — APPLIEDVERSION MUST MEAN SUCCESSFULLY APPLIED

Current ConfiguredAuthorizationSchemaBootstrapper sets:
_appliedVersion = _schema.SchemaVersion
before the actual SpiceDB WriteSchemaAsync(...).

Required semantic:

AppliedVersion becomes non-null ONLY after the schema was actually applied successfully.
If ApplySchemaOnStartup == false: remains null.
If bootstrap is requested but mode is not SpiceDb / no real write occurs: do not falsely mark as applied.
If WriteSchemaAsync throws/fails: remains null.
On successful write: set exactly to SchemaVersion.
Update misleading log wording if necessary so logs distinguish REQUESTED vs APPLIED.
No behavior change outside this semantic correction.

DO NOT broaden into:

authorization permission model redesign
schema version changes
route changes
AccessControl role/capability behavior changes
SpiceDB retry redesign
Host folder migrations
frontend
DB schema/migrations

STRUCTURE / MANIFEST

If Tooba.AccessControl.Contracts gains a new capability folder:

path/namespace exact
update the existing SINGLE AccessControl module manifest entry only if required by manifest semantics
do not create duplicate module/project entries
root allowlist must still match disk
no compatibility namespace alias/shim

DURABLE TESTS / GUARDS

Add focused tests proving at minimum:

Host readiness source has ZERO direct reference to
Tooba.AccessControl.Infrastructure.Authorization,
SpiceDbAuthorizationOptions,
SpiceDbHealthProbe.

The narrow readiness contract lives in AccessControl.Contracts and has no foreign
Application/Infrastructure/Domain dependency.

AccessControl.Infrastructure implementation preserves:

Disabled/InMemory readiness behavior
SpiceDb missing endpoint/token semantics
readiness probe disabled semantics
unreachable semantics

Bootstrapper:

ApplySchemaOnStartup=false => AppliedVersion null
requested but no actual SpiceDb write => AppliedVersion null
write failure => AppliedVersion null
successful write => AppliedVersion == SchemaVersion

Existing HostAuthorizationEvacuationGuardTests still pass.

FOCUSED VALIDATION

Build once:

Tooba.AccessControl.Contracts
Tooba.AccessControl.Infrastructure
Tooba.Host
Tooba.Host.Tests

Run focused tests only:

new readiness boundary tests/guards
new AppliedVersion semantic tests
HostAuthorizationEvacuationGuardTests
AuthorizationFoundationTests
AccessControl structure/manifest guard if Contracts structure changes

No solution-wide tests.
No broad unrelated module suites.
MAX_REPAIR_ITERATIONS = 1.
If one deterministic failure occurs, repair once and rerun only that failed validation.
Second/ambiguous failure => INCOMPLETE + exact blocker + STOP.

CERTIFICATION SUCCESS CRITERIA

PASS only if:

Host readiness -> AccessControl.Infrastructure.Authorization = ZERO
Host readiness -> narrow AccessControl.Contracts seam = YES
no secret exposure
readiness behavior parity preserved
AppliedVersion reflects successful actual apply only
failed/no-op apply leaves AppliedVersion null
path/namespace exact
AccessControl manifest remains truthful
Authorization 7-file Infrastructure ownership remains intact unless a cohesive file split is strictly required
Host/Authorization remains absent
builds/tests pass
no schema/route/frontend changes

EVIDENCE

Create:
docs/evidence/TB-TMAR-AUTHORIZATION-POSTCERT-CLEANUP-001/

analyze.md
readiness-boundary.md
bootstrap-semantics.md
validation.md
certification.md

Persist:
docs/ai/tasks/TB-TMAR-AUTHORIZATION-POSTCERT-CLEANUP-001.task.md

Update SoT with a dedicated block:
authorizationPostcertCleanup001

Required PASS fields:

hostReadinessBoundary = ACCESSCONTROL_CONTRACTS_ONLY
hostAuthorizationInfrastructureReference = ZERO
appliedVersionSemantics = SUCCESS_ONLY
authorizationEvacuationState = PRESERVED
certificationState = PASS
workflowStop = USER_REVIEW_AUTHORIZATION_POSTCERT_CLEANUP_001

GIT

Work from latest main.
No reset/clean/rebase/force-push.
Preserve user work.
Commit and push main only on PASS.
On INCOMPLETE, do not claim certification.

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-AUTHORIZATION-POSTCERT-CLEANUP-001
Parent-Task: TB-TMAR-HOST-AUTHORIZATION-AMC-001
Status: PASS | INCOMPLETE
Summary:
Root-Global-Boundaries-Preservation-State:
Authorization-Evacuation-Preservation-State:
Host-Readiness-Boundary-State:
Host-To-Authorization-Infrastructure-State:
AccessControl-Readiness-Contract-State:
Readiness-Behavior-Parity-State:
Secret-Exposure-State:
AppliedVersion-Semantics-State:
Bootstrap-NoOp-State:
Bootstrap-Failure-State:
Bootstrap-Success-State:
Path-Namespace-State:
Manifest-State:
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
Do not start another Host folder.

END_TOOBA_TASK