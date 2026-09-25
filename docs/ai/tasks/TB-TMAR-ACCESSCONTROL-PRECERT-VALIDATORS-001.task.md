PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ACCESSCONTROL-PRECERT-VALIDATORS-001
Parent-Task: TB-TMAR-ACCESSCONTROL-PRECERT-STRUCTURE-REPAIR-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: ACCESSCONTROL_PRECERT
Title: Close AccessControl 6-of-6 transport-validator gap without changing business semantics
Backend-Only: YES

ARCHITECT-ACCEPTED-PARENT:
TB-TMAR-ACCESSCONTROL-PRECERT-STRUCTURE-REPAIR-001

ACCEPTED-PARENT-COMMIT:
681e3639998e1090c7489e2db69c77680502ca26

TIMEBOX:
Target <= 12 minutes.
Hard maximum 15 minutes.
If safe completion exceeds the hard limit, return INCOMPLETE and STOP.
No retry loop.
No scope expansion.

ONE OBJECTIVE:

Close the verified AccessControl transport-validator gap for exactly these six endpoint-reachable requests:

CreateRoleCommand
UpdateRoleCommand
CloneRoleCommand
AssignRoleCommand
SetRolePermissionsCommand
SetSellerCeilingCommand

Current state:
VALIDATOR_REQUIRED = 6
validators present = 0
gap = 6

Target:
VALIDATOR_REQUIRED = 6
validators present = 6
gap = 0

Do NOT certify AccessControl in this task.

ARCHITECTURE RULE:

FluentValidation must cover only transport/input SHAPE.

Business rules remain in AccessControl handlers/directory, including:

role existence
role mutability
duplicate role code
seller ceiling
permission delegation
permission catalog membership
category existence
escalation
assignment uniqueness
domain ownership/state

Do NOT duplicate those business rules into validators.

VALIDATOR LOCATION:

Use an explicit structure-compliant folder, preferably:

Tooba.AccessControl.Application/Validators/

Optionally subfolders:
Role/
Assignment/
Permissions/
Ceiling/

Namespaces must exactly match physical paths.

Do NOT place validator files at project root.

FLUENTVALIDATION DISCOVERY:

AccessControl Application is already registered in the CQRS foundation through:
EnsureAccessControlBootstrapCommand assembly.

Audit existing Tooba.BuildingBlocks CQRS/FluentValidation discovery behavior before adding custom registration.

Prefer the same automatic validator-discovery pattern already used by certified modules.

Do NOT add duplicate/manual validator invocation in endpoints or handlers.

Do NOT create a second validation pipeline.

MINIMUM TRANSPORT-SHAPE INTENT:

CreateRoleCommand:

Name required and <= existing accepted transport/domain max
Code required and primitive lexical/length shape only where already established
owner/actor/tenant values supplied by trusted server context are NOT user-payload validation targets

UpdateRoleCommand:

Name required and <= existing accepted max
do not validate role existence/mutability

CloneRoleCommand:

clone Name required / shape
clone Code required / lexical/length shape
do not validate source role existence/mutability

AssignRoleCommand:

user-controlled UserId / RoleId must not be Guid.Empty if those fields originate from request body
do not validate role existence, archived state, assignment uniqueness or ownership

SetRolePermissionsCommand:

request/grants envelope must not be null
validate only primitive grant shape that is truly transport-level
do NOT validate PermissionCatalog membership
do NOT validate ceiling/delegability/category existence/scope business compatibility in FluentValidation

SetSellerCeilingCommand:

entries envelope must not be null
validate only primitive input shape
do NOT validate PermissionCatalog membership, delegability, category existence, ceiling semantics in FluentValidation

CRITICAL ERROR-CONTRACT AUDIT:

Before implementing, inspect the existing CQRS validation behavior and endpoint exception presentation.

The previous accepted route migrations intentionally preserved stable AccessControlException semantics.

Do NOT casually replace existing stable business error codes with generic validation errors.

For transport-shape failures:

use the platform's established FluentValidation error-code convention
use stable machine-readable validation codes
avoid localized/message-text classification
no ex.Message parsing

If the current validation pipeline would make it impossible to add the required validators without breaking already-established HTTP error contract beyond transport-shape cases:
return INCOMPLETE and document the exact blocker.
Do NOT bypass the pipeline.

SHARED RULES:

If multiple validators need identical rules, create only a small AccessControlFluentRules helper under Validators/.

Do not over-abstract.

A small AccessControlValidationCodes type is allowed if the platform pattern requires stable codes.

VALIDATOR TESTS:

Add only focused validator tests if there is an existing suitable AccessControl test location or a low-cost pattern.

At minimum cover:

one valid request per validator
the key transport-invalid shape per validator

Keep test count small.
Do not stand up a broad new test suite if infrastructure cost is large.

If existing test infrastructure does not provide a cheap Application validator test project, use the narrowest existing host/unit test location and document why.

MANDATORY VALIDATOR INVENTORY UPDATE:

Evidence must list all 19 endpoint-reachable MediatR requests and classify:

6 VALIDATOR_REQUIRED — all 6 present
13 NO_VALIDATOR_REQUIRED

No silent count drift.

If endpoint inventory changed unexpectedly, STOP with RECOVERY_CONFLICT.

STRUCTURE MUST REMAIN CLEAN:

Application root:
ZERO capability-specific .cs files

Endpoints root:
AccessControlEndpointModule.cs only

Infrastructure root:
AccessControlModule.cs only

HOST ZERO MUST REMAIN:

Host/Tooba.Host/AccessControl = absent
namespace Tooba.Host.AccessControl = ZERO
MapAccessControlEndpoints = ZERO
Program contains MapAccessControlModuleEndpoints exactly once

BOUNDARIES MUST REMAIN CLEAN:

Application -> foreign Application/Domain = ZERO
Infrastructure -> Catalog.Application/Domain = ZERO
Endpoints -> Host = ZERO
Endpoints direct IAccessControlDirectory = ZERO

OUT OF SCOPE:

No route migration
No Host cleanup
No structure certification
No Recovery SoT closure
No TMAR repository-wide baseline refresh
No source-size inventory refresh
No changes to other modules except test references strictly required for validator tests
No frontend
No Checkout
No DB/schema

KNOWN REPOSITORY-WIDE DEBT — DO NOT FIX HERE:

TmarSourceSizeAndInfraAppTests repository-wide baselines/inventory are stale from previously accepted module evacuations.

Do not modify:

tmar-source-size-baseline.json
tmar-infra-to-foreign-application.json
source-size-inventory.json

Record as pre-existing debt only.

CANONICAL TASK ARTIFACT:

Commit this exact task at:

docs/ai/tasks/TB-TMAR-ACCESSCONTROL-PRECERT-VALIDATORS-001.task.md

EVIDENCE:

Create:

docs/evidence/TB-TMAR-ACCESSCONTROL-PRECERT-VALIDATORS-001/validator-coverage.md

Evidence must include:

exact 19-request inventory
6 required / 13 no-validator-required
exact six validator file paths
rule classification per validator
business rules deliberately left in Application/Domain
validator discovery mechanism
stable validation/error contract approach
Host ZERO confirmation
structure/root confirmation
boundary audit
focused builds/tests
remaining blockers for certification

FOCUSED BUILDS:

dotnet build src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Tooba.AccessControl.Application.csproj --no-restore
dotnet build src/backend/Modules/AccessControl/Tooba.AccessControl.Infrastructure/Tooba.AccessControl.Infrastructure.csproj --no-restore
dotnet build src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Tooba.AccessControl.Endpoints.csproj --no-restore
dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore

FOCUSED TESTS:

Run only:

directly relevant validator tests added/available
AccessControl boundary/static test

No solution build.
No broad integration suite.
No broad architecture suite.
No retries.

RECOVERY HONESTY:

AccessControl Host evacuation remains COMPLETE.

AccessControl remains:
NOT COMPLETE_REFERENCE_PATTERN
NOT ARCH-COMPLETE-002 STRUCTURE_CERTIFIED

until the next dedicated certification task.

Expected next task after PASS with 6/6 validator coverage:

TB-TMAR-ACCESSCONTROL-FINAL-CERTIFICATION-AND-SOT-CLOSURE-001

PASS ONLY IF:

exactly 6 required validators exist
6/6 coverage verified
13 requests remain explicitly NO_VALIDATOR_REQUIRED
validators are discoverable by existing pipeline
only transport shape is validated
business semantics remain Application/Domain-owned
stable machine-readable validation behavior is preserved
structure remains compliant
Host remains ZERO
boundaries remain clean
focused builds pass
focused tests pass
canonical task/evidence committed
hard timebox respected

If safe completion exceeds hard limit:
Status = INCOMPLETE
STOP.
Do not loop.
Do not start certification automatically.

CANONICAL RESULT — RETURN ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-ACCESSCONTROL-PRECERT-VALIDATORS-001
Parent-Task: TB-TMAR-ACCESSCONTROL-PRECERT-STRUCTURE-REPAIR-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-State:
Endpoint-Reachable-Request-Count:
Validator-Required-Count:
Validators-Present-Count:
No-Validator-Required-Count:
Validator-Gap-State:
Validator-Files:
Transport-Rule-State:
Business-Rule-Boundary-State:
Validation-Discovery-State:
Validation-Error-Contract-State:
Application-Root-State:
Endpoints-Root-State:
Infrastructure-Root-State:
Host-Zero-State:
Boundary-Audit-State:
Focused-Builds:
Focused-Tests:
Repository-Wide-Baseline-Debt-State:
Certification-Ready-State:
Canonical-Task-State:
Evidence:
Production-Code-Scope:
Test-Code-Scope:
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
