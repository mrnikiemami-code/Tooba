PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-MEDIA-AMC-001-W4-R1
Parent-Task: TB-TMAR-MEDIA-AMC-001-W4
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Media
Mode: REPAIR
Track: MEDIA_AMSC_CERT_REPAIR
Title: Repair Media certification inconsistencies before Architect acceptance
Estimated-Time-Minutes: 10
Hard-Timebox-Minutes: 15

ARCHITECT VERDICT

Current Media AMSC W4 is NOT accepted yet.

Two concrete certification defects were independently verified on current main:

GLOBAL STRUCTURE LOCK DRIFT
docs/architecture/tmar-module-structure-manifests.json marks Media structureCertified: true.
tmar-current-state.json -> mediaAmc001 also marks Media structure-certified.
BUT tmar-current-state.json -> structureLock.certifiedModules currently omits Media.
This is an SoT inconsistency and must be repaired.
API RESULT CERTIFICATION GAP
src/backend/Modules/Media/Tooba.Media.Endpoints/Admin/MediaAdminEndpoints.cs
still contains:
return Results.Json(new { items = results }, statusCode: StatusCodes.Status200OK);
W4 evidence claims:
Endpoints → ISender + ApiResponseFactory = PASS
MediaModuleAmcW3CqrsGuardTests only rejects the narrow pattern:
Results.Json(new { title
so it does not enforce zero ad-hoc JSON results.
Under current canonical Certify rules, this evidence/guard is too weak.

SCOPE

Repair only the above two certification inconsistencies.

Allowed:

Media Admin upload response/result path
Media focused architecture guards
Media evidence
Media SoT / structureLock certifiedModules
required task/recovery docs

Do NOT:

redesign upload semantics
change route shape
change multi-file partial-success behavior
alter schema/migrations
broaden into binary-serving Results.File / Results.Text unless a concrete canonical rule requires it
touch unrelated modules
reopen Host
touch frontend

API RESULT REPAIR REQUIREMENT

Preserve existing HTTP behavior of multi-file upload:

HTTP 200 aggregate response
per-item ok
success asset
failure fileName, localized/title behavior if still required, and stable errorCode

But remove ad-hoc Results.Json from the endpoint by using the repository's canonical presentation/result mechanism.

Preferred approach:

introduce the smallest Media-owned Result-compatible response shape / Application result needed;
keep transport mapping via ApiResponseFactory;
do not push HTTP concerns into Application;
do not create a parallel response/error factory.

If exact parity cannot be represented by the existing canonical factory without behavior change, STOP and report the blocker instead of weakening the certification rule.

GUARD REPAIR

Strengthen the Media endpoint guard so certification cannot pass while ad-hoc JSON result mapping remains.

The guard must verify, for Media JSON/API endpoints:

ISender dispatch
canonical ApiResponseFactory
no direct Results.Json(...) for ordinary JSON API responses
no ad-hoc error JSON
binary/file/text serving exemptions only where transport semantics genuinely require Results.File / Results.Text

Do not use a blind repository-wide ban.

SOT REPAIR

On successful repair:

ensure structureLock.certifiedModules includes Media exactly once
preserve all existing certified modules
keep mediaAmc001.structureCertifiedUnderArchComplete002 = true
keep manifest Media structureCertified = true
update evidence honestly

VALIDATION

Focused only:

Media.Application build if touched
Media.Endpoints build
Media focused AMC guards
Media focused tests required for upload response parity
JSON/SoT parse validation
no solution-wide test run

SUCCESS CRITERIA

PASS only if:

Media is present exactly once in structureLock.certifiedModules
no ordinary Media API endpoint uses direct Results.Json
upload behavior parity is preserved
W3/W4 guard actually enforces the rule
Media remains capability-first / structure-certified
foreign App/Infra/Domain coupling remains ZERO
Host final closure remains preserved
schema change ZERO
frontend untouched

EVIDENCE

Create/update:
docs/evidence/TB-TMAR-MEDIA-AMC-001-W4-R1/

At minimum:

api-result-repair.md
guard-repair.md
sot-structure-lock.md
validation.md
recovery.md

Persist exact task:
docs/ai/tasks/TB-TMAR-MEDIA-AMC-001-W4-R1.task.md

CANONICAL RESULT CONTRACT

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-MEDIA-AMC-001-W4-R1
Parent-Task: TB-TMAR-MEDIA-AMC-001-W4
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary: <bounded summary>
Media-Structure-Cert-State: PRESERVED | BLOCKED
StructureLock-Media-State: PRESENT_EXACTLY_ONCE | INVALID
ApiResponseFactory-State: CANONICAL | BLOCKED
Direct-ResultsJson-State: ZERO | PRESENT
Upload-Behavior-Parity-State: PRESERVED | CHANGED
Media-Guard-State: STRENGTHENED_PASS | FAIL
Foreign-App-Infra-Domain-Coupling-State: ZERO | <state>
Host-Final-Closure-State: PRESERVED | REGRESSION
Schema-Change-State: NONE | <state>
Frontend-State: UNTOUCHED | CHANGED
Focused-Build-State: PASS | FAIL
Focused-Test-State: PASS | FAIL
Recovery-State: UPDATED | CONFLICT
Evidence-Path: docs/evidence/TB-TMAR-MEDIA-AMC-001-W4-R1/
Implementation-Commit-SHA: <sha>
Docs-Stamp-SHA: <sha>
HEAD-Equals-Origin-Main: YES | NO
Working-Tree-State: CLEAN | <state>
User-Work-Preserved: YES | NO
Automatic-Next-Implementation-Task-State: NONE
Workflow-Stop-State: USER_REVIEW_MEDIA_AMC_001_W4_R1
STOP
END_TOOBA_WORKER_RESULT

STOP RULE

After result:
STOP completely.
Do not start another module/task.
Wait for Architect review.

END_TOOBA_TASK