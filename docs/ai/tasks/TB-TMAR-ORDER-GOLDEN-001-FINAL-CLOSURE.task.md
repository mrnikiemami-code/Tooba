PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ORDER-GOLDEN-001-FINAL-CLOSURE
Parent-Task: TB-TMAR-ORDER-GOLDEN-001-R11-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Track: ORDER_FINAL_CLOSURE
Title: Final Order COMPLETE_REFERENCE_PATTERN Closure
Backend-Only: YES
Implementation-Mode: AUDIT_AND_SOT_CLOSURE_ONLY

Architect decision

R11-R1 is ACCEPTED at commit:
cf1a93abe13cca05abf3fa0f9f3156916e0325a1

Verified closure preconditions:

R4–R11 migrations preserved

ILLEGAL_ORDER_AUTHORITY = 0

DEAD_ORDER_RESIDUE = 0

dead AdminOrderCompletenessModels.cs removed

symbolic Host sweep hardened

current-head full backend build reported PASS

Order is now eligible for FINAL-CLOSURE audit.

Reference pattern

Endpoint/capability ownership:
Fulfillment-style

CQRS/MediatR/Result:
Offer-style

Host:
composition/auth/session/security/background execution shell only

Critical rule

This task MUST NOT perform a new architectural migration.

It is a final verification + evidence + SoT closure task.

If any new illegal Order authority, dead residue, foreign-boundary leak, CQRS regression, or behavior-critical architecture defect is discovered:

Status = INCOMPLETE

Do NOT silently repair it inside FINAL-CLOSURE.
Do NOT mark Order COMPLETE_REFERENCE_PATTERN.

Return exact residual so Architect can issue a repair task.

Final audit scope

Re-verify the entire Order module and Host relationship, not only R11.

1. Endpoint ownership

Verify all Order-owned HTTP surfaces are in Tooba.Order.Endpoints, including the migrated families:

Admin completeness

Admin OrdersGrid

Admin legacy orders list

Admin Order operations

Admin inventory recovery / supply

Admin Order detail / AdminViewAck

Admin customers list/grid

Storefront checkout

Storefront pending payments

Storefront shipping

Customer orders list/detail/retry

Seller orders list/detail

Host may retain only thin cross-module dashboard composition and authorization/session seams.

No duplicate Order route ownership in Host.

2. CQRS / MediatR

Verify Order HTTP flows use:

Endpoint
-> ISender
-> explicit Command/Query
-> real IRequestHandler
-> Result<T> / SemanticError

No:

generic operation dispatcher

string code routing

fake wrapper handlers

direct endpoint business orchestration

manual business Results.Json in Order endpoints

3. Stable error semantics

Verify in Order Application and touched contract boundaries:

Forbidden:

ex.Message business classification

localized message matching

arbitrary exception collapse into business errors

PlatformHttpException inside Order Application

Expected failures:

typed stable codes / Result

Unknown failures:

propagate

4. Time / IDs

Verify Order business paths use:

IClock

IIdGenerator where applicable

No direct:

DateTimeOffset.UtcNow

DateTime.UtcNow

inside Order business/application logic, except explicitly documented infrastructure/framework-only cases if any.

5. Foreign module boundaries

Audit Tooba.Order.Application.

Forbidden direct references to foreign:

.Application

.Infrastructure

.Domain

for:

Catalog

Party

Payment

Fulfillment

Returns

Settlement

Inventory

AccessControl

Cart

AddressBook

other business modules

Allowed:

foreign .Contracts

Order-owned ports

BuildingBlocks

Any exception must be explicitly justified; otherwise INCOMPLETE.

6. Host reverse audit

Run the hardened symbolic discovery over:

src/backend/Host/Tooba.Host/**/*.cs

Discovery must include:

filenames containing Order

type declarations containing Order

OrderDbContext

Tooba.Order.Application

Tooba.Order.Infrastructure

Tooba.Order.Domain

Tooba.Order.Contracts

Tooba.Order.Endpoints

/orders route registrations

Every production Host artifact must be classified.

PASS requires:

ILLEGAL_ORDER_AUTHORITY = 0

DEAD_ORDER_RESIDUE = 0

Tests are excluded from production leakage.

7. Allowed Host references

Re-verify—not blindly accept—the known allowed set, including:

HostOrderAdminAuthorizer

HostOrderAdminEffectiveAccessReader

HostOrderCustomerAuthorizer

HostOrderSellerAuthorizer

HostSellerOrderViewAccessReader

HostOrderStorefrontActor

storefront identity thin adapter

UnpaidOrderExpiryHostedService

UnpaidOrderExpiryHostOptions

Program/module composition

thin Customer/Seller/Admin cross-module dashboard composition

explicit dev seed/migration bootstrap

Each must remain thin.

8. Dead residue sweep

Verify no stale Order-owned DTO/model/helper remains in Host.

Specifically confirm absence of:

AdminOrderCompletenessModels.cs

AdminReservationCycleMapper.cs

old Storefront Order composers

old Admin Order operations/completeness/recovery/supply files

old Customer/Seller Order methods/routes

old Admin customers grid engine

9. Order project structure

Verify actual project structure is coherent:

Domain

Application

Contracts

Infrastructure

Endpoints

Verify path ↔ namespace alignment for current Order CQRS slices.

Do not require cosmetic churn if architecture is already coherent.

10. Behavior preservation evidence

Do not redesign behavior.

Verify existing tests/evidence still cover:

customer ownership

seller scope isolation

admin permissions

checkout parity

pending payment

shipping

inventory recovery

supply

reservation cycle

retry unpaid

admin grids/detail/operations/completeness

If behavior evidence is materially missing, return INCOMPLETE.

Mandatory validation

Run on final current head:

full Tooba.Order.Tests

Host reverse-audit guards

Order architecture guards

Tmar durable guards

focused Host Order regressions

dotnet build src/backend/Tooba.slnx

Result must explicitly say:

Full-Validation: CURRENT_HEAD full slnx build PASS

No prior-build substitution.

Closure evidence

Create:

docs/evidence/TB-TMAR-ORDER-GOLDEN-001-FINAL-CLOSURE/order-final-closure.md

Must contain:

Final status

COMPLETE_REFERENCE_PATTERN or INCOMPLETE_REFERENCE_REPAIR

Endpoint ownership

Exact module ownership summary.

CQRS/MediatR

Exact state.

Contract boundaries

Exact state.

Host removed

Summarize all major Host Order authority removed across R2–R11-R1.

Host remaining

File/category list + why each is allowed.

Reverse audit

Counts:

ILLEGAL_ORDER_AUTHORITY

DEAD_ORDER_RESIDUE

ALLOWED_THIN_HOST_ADAPTER

NON_ORDER_HOST_CONCERN

Known residual defects

Must be none for PASS.

Checkout

Must remain:
PAUSED_AT_SAFE_W5_CHECKPOINT

SoT update on PASS only

If and only if every final audit passes:

Update:
docs/architecture/tmar-current-state.json

Required state:

remove Order from active recovery

add/update Order in completeReferenceModules

Order state = COMPLETE_REFERENCE_PATTERN

HTTP applicability = HTTP_OWNING

endpointOwnership = MODULE_ENDPOINTS

cqrs = MEDIATR_12_5

lastAcceptedTask = TB-TMAR-ORDER-GOLDEN-001-FINAL-CLOSURE

lastAcceptedCommit = final closure commit

Checkout remains PAUSED_AT_SAFE_W5_CHECKPOINT

Update:

TOOBA-TMAR-MASTER-RECOVERY.md

TOOBA-ARCHITECT-BOOTSTRAP.md

final recovery evidence

Do NOT advance Checkout or another module.

No implementation changes

Allowed production changes in this task:

NONE, except a strictly non-business build/metadata fix if unavoidable and explicitly reported.

If a business/code architecture fix is needed:
Status = INCOMPLETE
STOP.
Do not fix it under closure.

PASS criteria

PASS only if all are true:

Order endpoint ownership is module-owned.

Order CQRS is real MediatR 12.5.

Result/stable error semantics are clean.

Order Application foreign boundaries are Contracts/ports only.

Host illegal Order authority count = 0.

Host dead Order residue count = 0.

Remaining Host Order references are thin/allowed and documented.

No duplicate Host Order routes.

No fake CQRS dispatcher.

No message-based business classification.

Canonical clock/id rules hold.

Behavior/security/isolation guards pass.

Current-head full backend build passes.

Checkout remains W5 paused.

Frontend unchanged.

Known residual defects = none.

Only then mark:
Order = COMPLETE_REFERENCE_PATTERN

Otherwise:
Status = INCOMPLETE
and keep:
Order = INCOMPLETE_REFERENCE_REPAIR

Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT

Task-ID: TB-TMAR-ORDER-GOLDEN-001-FINAL-CLOSURE
Parent-Task: TB-TMAR-ORDER-GOLDEN-001-R11-R1
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT

Summary:
Endpoint-Ownership-State:
CQRS-MediatR-State:
Result-Error-State:
Clock-Id-State:
Order-Foreign-Boundary:
Host-Reverse-Audit-State:
Illegal-Order-Authority-Count:
Dead-Order-Residue-Count:
Allowed-Host-References:
Duplicate-Route-State:
Behavior-Security-Validation:
Architecture-Guard-Validation:
Focused-Validation:
Full-Validation:
Host-Removed-Summary:
Host-Remaining-Summary:
Known-Residual-Defects:
Order-Final-State:
Checkout-State:
Frontend-Production-Changes:
Recovery-State:
Git:
Blockers:
User-Work-Preserved:
Next-Recommended-Task:

END_TOOBA_WORKER_RESULT

STOP

After Result:
STOP completely.

Do not start Checkout W6.
Do not start another module.
Do not poll.
Wait for Architect verification and user review.

END_TOOBA_TASK
