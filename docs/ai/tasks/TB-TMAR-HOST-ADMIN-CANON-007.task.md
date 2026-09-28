PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-CANON-007
Parent-Task: TB-TMAR-HOST-ADMIN-CANON-006
Parent-Commit: 031b27c420a63e2532acaa862a86af02333a3e58
Implementation-Commit-Parent: 928e83358bd3618a74dd2c6d4796b7e740e63abb
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Host Canonicalization
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_ADMIN_CANONICALIZATION
Title: Canonicalize AdminDevActorBootstrap dependencies and exception semantics

ARCHITECT ACCEPTANCE OF PARENT

TB-TMAR-HOST-ADMIN-CANON-006 is ARCHITECT-ACCEPTED.

Verified:

HostOrderAdminEffectiveAccessReader -> neutral IPlatformEffectiveAccessReader
direct AccessControl Application/Domain/IAccessControlDirectory = ZERO
Order.Contracts authority preserved
PermissionId/DeniedByCeiling semantics preserved
Host/Admin count = 15

GOAL

Repair ONLY:
src/backend/Host/Tooba.Host/Admin/AdminDevActorBootstrap.cs

Current debts:

direct Tooba.Identity.Infrastructure reference
generic InvalidOperationException catch used as control flow
Development bootstrap semantics are mixed with avoidable lower-layer coupling

This task must keep the bootstrap Development-only and behavior-compatible while removing the foreign Infrastructure dependency and generic expected-flow exception handling.

MANDATORY AUDIT FIRST

Before editing:

inspect AdminDevActorBootstrap.cs
inspect IIdentityAuthenticationService and its Contracts faults
inspect authorization tuple writer contracts and duplicate/idempotency semantics
search for an existing typed exception/result used when a tuple relationship already exists
inspect current Development bootstrap call site

REQUIRED TARGET

After this task:

ZERO using Tooba.Identity.Infrastructure
identity calls only through Tooba.Identity.Contracts
registration duplicate handling remains typed (IdentityDuplicateIdentifierFault or existing canonical equivalent)
ZERO blanket catch (InvalidOperationException) for expected tuple-write flow
tuple write must use an existing idempotent/typed contract path if available

If the tuple writer is already contractually idempotent and the catch is unnecessary:

remove the catch.

If a typed duplicate/already-exists fault exists:

catch only that exact typed fault.

If neither is true:

return INCOMPLETE with the exact boundary gap.
Do NOT invent message parsing.

BEHAVIOR PRESERVATION

Preserve:

Development-only admin actor creation
AdminEmail
current registration/login identifier semantics
tenant membership write
existing snapshot shape and locking semantics
no seller-admin privilege broadening
current bootstrap call count/order

PASSWORD

Do not broaden scope into credentials/config redesign.
Do not change the current Development password behavior in this task unless required by an already-existing canonical Development options seam.

OUT OF SCOPE

Do NOT:

touch other Host/Admin files
touch authorizers
change foldering
change routes
redesign Identity
redesign Authorization
modify frontend/schema
clean unrelated bin/obj artifacts
repair unrelated baseline test debt

ANTI-LOOP / EXECUTION BUDGET

ONE concern only.

Validation:

build Tooba.Host
build Tooba.Host.Tests
run ONLY CANON-007 guard/behavior tests + CANON-006 guard

On failure:

ONE repair attempt maximum
rerun ONLY failed command once
second failure => INCOMPLETE + exact blocker + STOP

MAX_REPAIR_ITERATIONS = 1
MAX_VALIDATION_COMMAND_RUNS = 6
No broad suites.
No loop.

DURABLE GUARD

Prove:

AdminDevActorBootstrap has ZERO Identity.Infrastructure reference
Identity dependency is Contracts-only
ZERO catch (InvalidOperationException)
duplicate identity registration remains typed
tuple membership write remains idempotent/typed
Development bootstrap behavior preserved
Host/Admin count = 15
CANON-006 preserved

EVIDENCE

Create only:
docs/evidence/TB-TMAR-HOST-ADMIN-CANON-007/

analyze.md
validation.md
closure.md

Persist task:
docs/ai/tasks/TB-TMAR-HOST-ADMIN-CANON-007.task.md

RECOVERY SOT

Append/update hostAdminCanon007:

parentCommit
adminDevActorIdentityInfrastructure=ZERO
adminDevActorIdentityBoundary=CONTRACTS
adminDevActorInvalidOperationExpectedFlow=ZERO
tupleWriteSemantics=IDEMPOTENT_OR_TYPED
adminFileCount=15
canon006=PRESERVED
workflowStop=USER_REVIEW_HOST_ADMIN_CANON_007

SUCCESS CRITERIA

PASS only if:

direct Identity.Infrastructure coupling = ZERO
expected-flow InvalidOperationException catch = ZERO
bootstrap behavior preserved
focused validation passes
Host/Admin remains 15
evidence/task/SoT committed and pushed
HEAD == origin/main
tracked working tree clean

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ADMIN-CANON-007
Parent-Task: TB-TMAR-HOST-ADMIN-CANON-006
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Canon006-Preservation-State:
Host-Admin-File-Count-Before:
Host-Admin-File-Count-After:
AdminDevActor-IdentityInfrastructure-State:
AdminDevActor-IdentityContracts-State:
AdminDevActor-RegistrationDuplicate-State:
AdminDevActor-InvalidOperationExpectedFlow-State:
AdminDevActor-TupleWrite-State:
AdminDevActor-Behavior-State:
Focused-Validation:
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

STOP RULE

After result:
STOP COMPLETELY.

Do not start Host/Admin foldering.
Wait for Architect review.

END_TOOBA_TASK