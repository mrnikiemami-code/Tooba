PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ORDER-AMC-001-R1
Parent-Task: TB-TMAR-HOST-ORDER-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Order AMC Repair
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_ORDER_CONTRACTS_BOUNDARY_REPAIR
Title: Remove Order.Application leakage from Host/Order KEEP adapter and reconcile Recovery

BASELINE
Parent implementation:
d93df23ca26b7396ad8a492ddb18e7a49006b803

Parent docs/stamp:
6d40392ba1a8e91930422409b230f324ca6c7ad0

PRESERVE

Host/Order remains PRESENT
disposition remains KEEP_AS_THIN_HOST_ORDER_STOREFRONT_ADAPTER
retained allowlist remains exactly HostOrderStorefrontActor.cs
Guest actor remains Order.Contracts.Fulfillment.StorefrontGuestActor
auth code remains FoundationErrorCodes.CheckoutAuthenticationRequired
Host business authority remains ZERO
schema unchanged
frontend unchanged

R1 BLOCKER
Current HostOrderStorefrontActor.cs directly imports/uses:

Tooba.Order.Application.Storefront
Tooba.Order.Application.Storefront.Ports
IOrderStorefrontActor
IOrderStorefrontCheckoutIdentityGate
StorefrontOrderException

This violates the Contracts-only Host boundary lock.

ARCHITECTURE DECISION
Any interface/fault intended to be implemented or consumed by Host must live in:

Order.Contracts
or
neutral BuildingBlocks

Host/Order must have ZERO references to:

Tooba.Order.Application
Tooba.Order.Domain
Tooba.Order.Infrastructure
Tooba.Order.Persistence

REQUIRED REPAIR

HOST-FACING PORTS TO CONTRACTS
Move/rehome the Host-facing storefront seams from Order.Application to Order.Contracts.

At minimum:

IOrderStorefrontActor
IOrderStorefrontCheckoutIdentityGate

Use capability-first placement under Tooba.Order.Contracts.
Exact path ↔ namespace required.

No aliases.
No shims.
No duplicate compatibility interfaces.

Repoint all consumers.

FAILURE TRANSPORT
HostOrderStorefrontActor currently throws StorefrontOrderException from Order.Application.

This is not allowed.

Replace with a stable Contracts/neutral failure path.

Preferred:

SemanticException(new SemanticError(FoundationErrorCodes.CheckoutAuthenticationRequired))
if the downstream Order flow already supports SemanticException canonically.

Otherwise:

introduce the smallest Order.Contracts-owned typed fault/exception with stable code
convert inside Order.Application owner boundary

Do NOT:

leave Host depending on Application exception types
inspect exception messages
use localized message classification
invent raw HTTP response behavior in Host adapter

Unknown exceptions must propagate.

CHECKOUT IDENTITY GATE
HostOrderStorefrontCheckoutIdentityGate may wrap Host CheckoutIdentityGate,
but the interface it implements must be Contracts/neutral.

Do not move CheckoutIdentityGate business/security authority into Order.
Do not reintroduce Payment.Application leakage.

CART ACCESS
CartAccess currently comes from Cart.Contracts and may remain if it is a stable neutral contract.

Audit and document this dependency as allowed.
No Cart.Application/Domain/Infrastructure leakage.

HOST/ORDER FINAL BOUNDARY
HostOrderStorefrontActor.cs may depend only on:
BuildingBlocks / presentation-neutral errors
Order.Contracts
Cart.Contracts
Host.Authentication / Host.Security thin platform seams as needed
ASP.NET environment/request primitives

It must NOT depend on:

Order.Application
Order.Domain
Order.Infrastructure
Order.Persistence
any foreign module Application/Domain/Infrastructure/Persistence
GUARD REPAIR
Strengthen HostOrderAmcGuardTests.

Must prove:

folder PRESENT
exact one-file allowlist
namespace exact Tooba.Host.Order
Order.Application = ZERO
Order.Domain = ZERO
Order.Infrastructure = ZERO
Order.Persistence = ZERO
all foreign module Application/Domain/Infrastructure/Persistence = ZERO
Guest actor from Order.Contracts
Host-facing actor/gate interfaces from Order.Contracts
no StorefrontOrderException Application type
no DbContext
no service locator
no message-text classification
no raw business HTTP ownership
FoundationErrorCodes auth code preserved
NO SINK-FOLDER REGRESSION
Do not move the same leakage into another Host folder.
Do not create a Host shim that imports Order.Application.

The seam must be module-owned in Contracts.

BEHAVIOR PARITY
Preserve exact storefront actor behavior:
authenticated session user wins
Dev/Testing header seam remains
saved-address unauthenticated production case still rejects with the same stable machine code
guest fallback remains StorefrontGuestActor.ActorId
list actor behavior unchanged
BuildCartAccess unchanged
checkout identity gate delegation unchanged

RECOVERY / SOT — MANDATORY DoD
Update all authoritative surfaces:

docs/architecture/tmar-current-state.json
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md

Required final state:

lastAcceptedTask = TB-TMAR-HOST-ORDER-AMC-001-R1
lastAcceptedCommit = <actual R1 implementation SHA>
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
latestAcceptedImplementationWave = TB-TMAR-HOST-ORDER-AMC-001-R1
currentHostEvacuation.currentTask = TB-TMAR-HOST-ORDER-AMC-001-R1
currentHostEvacuation.activeModule = Order
currentHostEvacuation.currentHostCheckpoint = Order
active state = ORDER_KEEP_THIN_HOST_ADAPTER_R1_USER_REVIEW_REQUIRED
workflowStop = USER_REVIEW_HOST_ORDER_AMC_001_R1_KEEP_THIN_HOST_ADAPTER
nextTask = USER_REVIEW_HOST_ORDER_AMC_001_R1_KEEP_THIN_HOST_ADAPTER
nextTaskState = USER_DECISION_REQUIRED
automaticNextImplementationTask = NONE
staleCurrentPointerState = ZERO

If docs/stamp is a separate commit:

lastAcceptedCommit MUST still point to the R1 IMPLEMENTATION commit.

Historical accepted lineage must remain intact.

SCOPE LIMIT
Do NOT:

make Host/Order HOST_ZERO
move storefront actor policy into Host beyond the existing thin adapter role
redesign Order storefront flows
touch frontend
change schema/migrations
start another Host folder
run solution-wide refactors
touch unrelated user file accesscontrol-first-slice-map.md

FOCUSED VALIDATION ONLY

Build:

Order.Contracts
Order.Application
Host
directly affected tests

Run:

HostOrderAmcGuardTests
focused Order storefront actor/gate tests
focused auth rejection code test
TmarDurableGuardTests

No solution-wide test run.

EVIDENCE
Create:
docs/evidence/TB-TMAR-HOST-ORDER-AMC-001-R1/

Required:

blocker.md
contracts-seam.md
failure-transport.md
host-boundary.md
behavior-parity.md
recovery.md
validation.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-ORDER-AMC-001-R1.task.md

SUCCESS CRITERIA
PASS only if all are true:

Host/Order remains PRESENT
disposition remains KEEP_AS_THIN_HOST_ORDER_STOREFRONT_ADAPTER
exact one-file allowlist preserved
Host → Order.Application = ZERO
Host → Order.Domain = ZERO
Host → Order.Infrastructure = ZERO
Host → Order.Persistence = ZERO
foreign module non-Contracts layers = ZERO
Host-facing actor/gate seams live in Order.Contracts
StorefrontOrderException Application dependency = ZERO
stable auth machine code preserved
behavior parity preserved
no message-text classification
no sink-folder regression
schema unchanged
frontend unchanged
Recovery fully reconciled to Order R1
lastAcceptedCommit points to actual R1 implementation SHA
automaticNextImplementationTask = NONE
staleCurrentPointerState = ZERO

GIT
Work from latest origin/main.
No reset.
No clean.
No rebase.
No force push.
Preserve unrelated user work.
Commit/push main only on PASS.

CANONICAL RESULT
Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ORDER-AMC-001-R1
Parent-Task: TB-TMAR-HOST-ORDER-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Host-Order-State:
Order-Disposition-State:
Host-Order-File-Count:
Host-Order-Application-State:
Host-Order-Domain-State:
Host-Order-Infrastructure-State:
Host-Order-Persistence-State:
Foreign-Module-Layer-State:
Order-Contracts-Seam-State:
Checkout-Gate-Contract-State:
Storefront-Actor-Contract-State:
Failure-Transport-State:
StorefrontOrderException-State:
Guest-Actor-State:
Cart-Contracts-State:
Auth-Code-State:
Business-Authority-State:
Message-Text-Classification-State:
Sink-Folder-Regression-State:
Behavior-Parity-State:
Schema-Change-State:
Frontend-State:
Guard-State:
Focused-Build-State:
Focused-Test-State:
Recovery-State:
Last-Accepted-Commit-State:
Stale-Current-Pointer-State:
Automatic-Next-Implementation-Task-State:
Workflow-Stop-State:
Certification-State:
Evidence-Path:
Commit-SHA:
Push-State:
HEAD-Equals-Origin-Main:
Working-Tree-Tracked-State:
User-Work-Preserved:
STOP
END_TOOBA_WORKER_RESULT

STOP COMPLETELY AFTER RESULT.
Do not start another Host folder.
Wait for Architect/user review.

END_TOOBA_TASK
