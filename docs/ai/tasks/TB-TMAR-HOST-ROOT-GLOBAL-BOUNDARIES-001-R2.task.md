PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-TMAR-HOST-ROOT-GLOBAL-BOUNDARIES-001-R2
Parent-Task: TB-TMAR-HOST-ROOT-GLOBAL-BOUNDARIES-001-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Host Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_ROOT_GLOBAL_BOUNDARIES
Title: Repair R1 false-positive certification by making touched Order.Infrastructure boundaries genuinely canonical

CURRENT MAIN

HEAD: 87a22d7c1439809e89938cc80bb89020eab2ffbf
R1 is already on main.
Authorization must remain preserved.

ARCHITECT REVIEW
R1 migration behavior is accepted, but R1 certification is REJECTED.

Reason:
Tooba.Order.Infrastructure is a touched destination PROJECT because R1 added:

ReservationCycle/ReservationCycleRegistration.cs
ReservationCycle/UnpaidOrderExpiryWorker.cs
ReservationCycle/UnpaidOrderExpiryWorkerOptions.cs
Integrations/Payment/CheckoutReservationHoldPolicyAdapter.cs

Therefore the destination project graph is part of certification. Current Tooba.Order.Infrastructure.csproj still has direct foreign Application references:

Cart.Application
Catalog.Application
Payment.Application
Fulfillment.Application
AccessControl.Application

R1 incorrectly excluded the dirty .csproj / OrderModule from certification merely because they were byte-identical to pre-task main. The certify skill does not permit that for a touched destination project.

Also, the new CheckoutReservationHoldPolicyAdapter uses Tooba.Payment.Contracts.Hold, but Order.Infrastructure has no explicit direct Payment.Contracts ProjectReference; it reaches it transitively via the illegal Payment.Application reference. This is not an acceptable canonical dependency declaration.

MANDATORY SKILLS
Run exactly:

tooba-architecture-analyze
tooba-architecture-migrate
tooba-architecture-certify

GOAL
Make the touched Tooba.Order.Infrastructure project genuinely certifiable.

FINAL REQUIRED PROJECT BOUNDARY
Order.Infrastructure may depend on:

Order.Application
Order.Contracts
neutral BuildingBlocks/Persistence
foreign *.Contracts only

Final state:

foreign Application = ZERO
foreign Infrastructure = ZERO
foreign Domain = ZERO

MANDATORY AUDIT
Audit all source/project dependencies for at least:

Cart.Application
Catalog.Application
Payment.Application
Fulfillment.Application
AccessControl.Application

Classify and repair each with existing Contracts seams where possible.
If a true boundary contract is missing, create the narrowest lawful contract in the natural owning module.
Do NOT move application-internal types into Contracts merely to silence guards.
If one edge cannot be repaired lawfully within bounded scope => INCOMPLETE + exact blocker + STOP.

PAYMENT HOLD REQUIREMENT
If CheckoutReservationHoldPolicyAdapter remains in Order.Infrastructure:

add explicit direct Tooba.Payment.Contracts ProjectReference
no transitive dependency reliance via Payment.Application

PRESERVE R1 BEHAVIOR

six Host root files remain absent
Settlement global-usings remain absent
Commerce hold owner stays Payment
Payment -> Catalog stays Contracts-only
checkout hold behavior unchanged
unpaid expiry worker ownership/behavior unchanged
config Tooba:UnpaidOrderExpiry
Enabled=true
PollIntervalSeconds=15
BatchSize=20
worker name unpaid-order-expiry
metric tooba.unpaid_expiry.expired
hold precedence method > store > gateway
no schema/migration/frontend change

AUTHORIZATION PRESERVATION
Verify only:

Host/Authorization absent
AccessControl Authorization 7-file slice present
no Authorization regression
Do not reopen Authorization cleanup in this task.

DURABLE GUARD
Add/strengthen a focused guard proving BOTH:

Tooba.Order.Infrastructure.csproj has ZERO foreign Application/Infrastructure/Domain ProjectReference
source files under Order.Infrastructure have ZERO foreign Application/Infrastructure/Domain imports/FQNs

No whitelist of current violations.

FOCUSED VALIDATION
Build once:

Order.Contracts
Order.Application
Order.Infrastructure
Payment.Contracts
Host
Host.Tests
any Contracts project modified

Focused tests:

HostRootGlobalBoundariesGuardTests
UnpaidOrderExpiryTests
ReservationCycleFoundationTests
CommerceHoldPolicySourceTests
new Order infrastructure foreign-layer boundary guard

No solution-wide tests.
MAX_REPAIR_ITERATIONS=1.
Second/ambiguous failure => INCOMPLETE + STOP.

CERTIFICATION PASS ONLY IF

six Host root files ZERO
Settlement global-usings ZERO
Payment->Catalog Contracts-only
Order checkout hold->Payment explicit Contracts-only
Order.Infrastructure foreign Application ZERO
Order.Infrastructure foreign Infrastructure ZERO
Order.Infrastructure foreign Domain ZERO
cross-module DbContext/join ZERO
focused builds/tests PASS
Authorization preserved
no schema/route/frontend regression

EVIDENCE
Create:
docs/evidence/TB-TMAR-HOST-ROOT-GLOBAL-BOUNDARIES-001-R2/

analyze.md
dependency-matrix.md
migration.md
validation.md
certification.md

Update SoT:

R1 certification => SUPERSEDED_BY_R2
R2 certification PASS only after genuine closure
orderInfrastructureForeignApplication=ZERO
orderInfrastructureForeignInfrastructure=ZERO
orderInfrastructureForeignDomain=ZERO
paymentContractsReference=EXPLICIT_DIRECT
workflowStop=USER_REVIEW_HOST_ROOT_GLOBAL_BOUNDARIES_001_R2

GIT
Work from latest main.
No reset/clean/rebase/force-push.
Commit/push main only on PASS.
Preserve user work.

CANONICAL RESULT
Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ROOT-GLOBAL-BOUNDARIES-001-R2
Parent-Task: TB-TMAR-HOST-ROOT-GLOBAL-BOUNDARIES-001-R1
Status: PASS | INCOMPLETE
Summary:
R1-Certification-State:
Order-Infrastructure-Touched-Surface-State:
Order-Infra-Foreign-Application-State:
Order-Infra-Foreign-Infrastructure-State:
Order-Infra-Foreign-Domain-State:
Payment-Contracts-Reference-State:
Cart-Boundary-State:
Catalog-Boundary-State:
Payment-Boundary-State:
Fulfillment-Boundary-State:
AccessControl-Boundary-State:
CrossModule-Persistence-State:
Six-Host-Root-Files-State:
Authorization-Preservation-State:
Behavior-Parity-State:
Focused-Build-State:
Focused-Test-State:
Durable-Guard-State:
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
Do not start Authorization cleanup yet.
Do not start another Host folder.
END_TOOBA_TASK
