PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-USERPREFERENCE-AMC-001-W4-R1
Parent-Task: TB-TMAR-USERPREFERENCE-AMC-001-W4
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — UserPreference
Mode: CERTIFICATION_DEFECT_REPAIR
Track: USERPREFERENCE_AMSC_CERT_REPAIR
Title: Remove endpoint catch-and-map faults and close canonical W4 recovery checkpoint

ARCHITECT VERDICT

W4 is NOT architect-accepted yet.

Independent review confirms the structure, CQRS/Result migration, validator inventory, Contracts-only module boundary, Host closure, and microservice boundary are substantially correct.

Two certification defects remain:

Admin endpoints still contain endpoint-level catch-and-map blocks for expected PlatformHttpException failures.
Canonical SoT records W4 as COMPLETE_REFERENCE_PATTERN but does not record the W4 implementation SHA itself.

CURRENT VERIFIED MAIN

Expected starting HEAD:
f2c3c2f28e1a7db9075992cd4bf3f41300562c4d

Verified lineage:

W0 813ab0ec — Analyze
W1 8cce5a19 — /Modules/UserPreference/ solution grouping
W2 921611f7 — capability-first Domain/Application/Infrastructure/Contracts
W3 df86f93b — CQRS + Result + Contracts error catalog
W4 f2c3c2f2 — Structure + Certify claim

Verified good and MUST preserve:

/Modules/UserPreference/ canonical solution grouping
5 projects: Domain / Application / Contracts / Infrastructure / Endpoints
Application capability-first:
LocalePreferences/{Commands,Queries,Validators}
UiPreferences/{Commands,Queries,Validators}
Composition
Models
Ports
Domain aggregates under Aggregates/
Infrastructure under Directories/, Persistence/, Development/
Endpoints under Customer/ + Admin/
Contracts error catalog/resources
Path-Namespace EXACT
Physical-Copy CLEAN
Root allowlists ENFORCED
4 endpoint-reachable request types
validator matrix = 3 required present + 1 explicitly no-validator
Result pipeline through UserPreferenceOperation
Domain/Infrastructure expected semantic failures use SemanticException + stable codes
foreign Application/Infrastructure/Domain coupling = ZERO
only foreign production module seam is Order.Contracts.Fulfillment in Customer actor resolver
Host Preferences business residue = ZERO
schema/migrations unchanged
frontend untouched

INDEPENDENTLY VERIFIED BLOCKER 1 — ENDPOINT CATCH-AND-MAP

The Certify skill explicitly requires:

no catch-and-map blocks in endpoints for expected failures

Current production code violates that in:

src/backend/Modules/UserPreference/Tooba.UserPreference.Endpoints/Admin/UserPreferenceAdminEndpoints.cs

Both GET and PUT contain:

catch (PlatformHttpException ex)
return api.FromPlatformException(ex);

Current production code also violates that in:

src/backend/Modules/UserPreference/Tooba.UserPreference.Endpoints/Admin/UiPreferenceAdminEndpoints.cs

Both GET and PUT contain the same endpoint catch-and-map pattern.

IUserPreferenceAdminAuthorizer.RequireAuthorizedAsync(...) is the trusted authorization adapter seam and may throw the platform exception.
The global exception boundary already uses the canonical presentation pipeline.

Required architecture:

endpoint invokes authorizer
expected authorization failure propagates to the global exception boundary
endpoint does NOT catch/map PlatformHttpException
successful flow remains ISender + ApiResponseFactory.From

Do NOT replace this with another local catch, local Result wrapper, custom mapper, or duplicated authorization response logic.

INDEPENDENTLY VERIFIED BLOCKER 2 — CANONICAL W4 CHECKPOINT

Current userPreferenceAmc001 SoT block contains:

state = COMPLETE_REFERENCE_PATTERN
structureCertifiedUnderArchComplete002 = true
implementationCommitKind = IMPLEMENTATION_COMMIT
workflowStop = USER_REVIEW_USERPREFERENCE_AMC_001_W4

But it does NOT record:

implementationCommit = f2c3c2f28e1a7db9075992cd4bf3f41300562c4d

For an architect-accepted certification checkpoint the implementation SHA must be explicit and recoverable.

Because this repair changes production after W4, preserve the historical W4 block and add a bounded W4-R1 repair checkpoint after implementation.

SCOPE

Allowed production files:

src/backend/Modules/UserPreference/Tooba.UserPreference.Endpoints/Admin/UserPreferenceAdminEndpoints.cs
src/backend/Modules/UserPreference/Tooba.UserPreference.Endpoints/Admin/UiPreferenceAdminEndpoints.cs

Allowed tests/guards:

focused UserPreference W3/W4 architecture guard(s) only

Allowed docs/SoT:

docs/architecture/evidence/TB-TMAR-USERPREFERENCE-AMC-001-W4/* only if certification wording requires correction
docs/architecture/evidence/TB-TMAR-USERPREFERENCE-AMC-001-W4-R1/*
docs/architecture/tmar-current-state.json
exact task artifact

Forbidden:

Domain changes
Application changes
Infrastructure changes
Contracts changes
route changes
authorization policy changes
response schema changes
schema/migrations
Host production
frontend
other modules
broad test rewrites
full repository test suite
weakening existing guards
aliases/shims
unrelated refactor

REQUIRED REPAIR

A. Remove endpoint-level catch-and-map.

For both Admin endpoint files:

remove try/catch (PlatformHttpException ...)
call RequireAuthorizedAsync(...) directly
let PlatformHttpException propagate to the global canonical exception boundary
keep success path unchanged:
authorize
ISender.Send(...)
ApiResponseFactory.From(...)

Do not catch SemanticException or PlatformHttpException in these endpoints after repair.

B. Strengthen the focused UserPreference architecture guard.

Add a durable assertion scoped to UserPreference Endpoints that prevents recurrence of:

catch (PlatformHttpException
FromPlatformException(

Preserve existing checks for:

zero Results.Json
zero endpoint catch (SemanticException
module endpoint ownership
Contracts-only boundary

Do not create a global repository-wide brittle text ban.

C. Preserve behavior.

Authorization behavior must remain equivalent:

same authorizer
same PlatformHttpException
same canonical safe error mapper / global exception presentation
no change to permission semantics
no change to route/status/error-code contract

D. Record canonical W4-R1 repair checkpoint in SoT.

Preserve historical:
userPreferenceAmc001

Do not rewrite its W0-W4 history.

After implementation commit exists, add:
userPreferenceAmc001W4R1

Record at minimum:

task = TB-TMAR-USERPREFERENCE-AMC-001-W4-R1
parentTask = TB-TMAR-USERPREFERENCE-AMC-001-W4
mode = CERTIFICATION_DEFECT_REPAIR
state = USERPREFERENCE_AMC_W4_CERT_DEFECTS_REPAIRED
endpointCatchAndMapState = ZERO
apiResponseFactoryState = CANONICAL
foreignAppInfraDomainCoupling = ZERO
orderContractsBoundary = CUSTOMER_ACTOR_RESOLVER_ONLY
structureState = READY_FOR_CERTIFY_PRESERVED
pathNamespace = EXACT
hostFinalClosure = PRESERVED
schemaChange = NONE
frontendState = UNTOUCHED
implementationCommit = <W4-R1 implementation SHA>
implementationCommitKind = IMPLEMENTATION_COMMIT
evidenceRoot = docs/architecture/evidence/TB-TMAR-USERPREFERENCE-AMC-001-W4-R1/
taskArtifact = docs/ai/tasks/TB-TMAR-USERPREFERENCE-AMC-001-W4-R1.task.md
workflowStop = USER_REVIEW_USERPREFERENCE_AMC_001_W4_R1
automaticNextImplementationTask = NONE

Also add/update the existing module-specific latest post-cert repair pointer if repository convention uses one.

Do not falsely set the original W4 commit to the W4-R1 implementation commit.

VALIDATION

Run only focused validation:

build Tooba.UserPreference.Endpoints
run focused UserPreference W3/W4 architecture guards
run the smallest existing UserPreference endpoint/foundation tests that exercise Admin authorization error behavior and successful preference routes
JSON parse SoT after update
search proof:
zero catch (PlatformHttpException under UserPreference.Endpoints
zero FromPlatformException( under UserPreference.Endpoints
zero catch (SemanticException under UserPreference.Endpoints
zero direct foreign Application/Infrastructure/Domain references
Order.Contracts.Fulfillment remains the only foreign production module Contracts seam
no schema/migration changes
no Host production changes
no frontend changes

TEST DISCIPLINE

Tests are evidence, not navigation.

If one focused test fails for one clear local deterministic reason, perform one bounded correction and rerun only the affected validation.
If resolution requires speculation or broader scope, STOP with INCOMPLETE.
Do not run the full repository suite.
Do not enter an open-ended test/fix loop.

EVIDENCE

Create:
docs/architecture/evidence/TB-TMAR-USERPREFERENCE-AMC-001-W4-R1/

At minimum:

endpoint-fault-mapping-repair.md
recovery-sot.md
validation.md

Persist exact task:
docs/ai/tasks/TB-TMAR-USERPREFERENCE-AMC-001-W4-R1.task.md

CANONICAL RESULT CONTRACT

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-USERPREFERENCE-AMC-001-W4-R1
Parent-Task: TB-TMAR-USERPREFERENCE-AMC-001-W4
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary: <bounded summary>
Starting-HEAD-State: F2C3C2F2 | DIVERGED
Endpoint-PlatformHttp-Catch-State: ZERO | PRESENT
Endpoint-FromPlatformException-State: ZERO | PRESENT
Endpoint-Semantic-Catch-State: ZERO | PRESENT
Authorization-Behavior-State: PRESERVED | REGRESSION
ApiResponseFactory-State: CANONICAL | INVALID
Foreign-App-Infra-Domain-Coupling-State: ZERO | VIOLATION
OrderContracts-Seam-State: CUSTOMER_ACTOR_RESOLVER_ONLY | INVALID
Structure-State: READY_FOR_CERTIFY_PRESERVED | REGRESSION
Path-Namespace-State: EXACT | MISMATCH
Schema-Change-State: NONE | CHANGED
Host-Final-Closure-State: PRESERVED | REGRESSION
Frontend-State: UNTOUCHED | CHANGED
Focused-Guard-State: PASS | FAIL
Focused-Behavior-State: PASS | FAIL
W4R1-Canonical-SoT-State: RECORDED | MISSING
W4R1-Implementation-Commit-State: RECORDED | INVALID
W4R1-Evidence-Path-State: RECORDED | MISSING
Recovery-State: UPDATED | STALE | CONFLICT
Implementation-Commit-SHA: <sha>
Docs-Commit-SHA: <sha-or-SAME_IF_SINGLE_COMMIT_CONVENTION>
HEAD-Equals-Origin-Main: YES | NO
Working-Tree-State: CLEAN | <state>
User-Work-Preserved: YES | NO
Automatic-Next-Implementation-Task-State: NONE
Workflow-Stop-State: USER_REVIEW_USERPREFERENCE_AMC_001_W4_R1
STOP
END_TOOBA_WORKER_RESULT

STOP RULE

After result:
STOP completely.
Do not start another UserPreference task.
Do not start PlatformProbe.
Do not start another module.
Wait for Architect review.

END_TOOBA_TASK
