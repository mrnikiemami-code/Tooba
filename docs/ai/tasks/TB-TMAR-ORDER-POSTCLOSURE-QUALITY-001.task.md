PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ORDER-POSTCLOSURE-QUALITY-001
Parent-Task: TB-TMAR-ORDER-GOLDEN-001-FINAL-CLOSURE
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Track: ORDER_POST_CLOSURE_QUALITY_HARDENING
Title: Order Application Foldering + MediatR FluentValidation Hardening
Backend-Only: YES

Architect decision

Order business architecture is already closed as:
COMPLETE_REFERENCE_PATTERN

This task is a post-closure quality hardening task with exactly two goals:

Fix Tooba.Order.Application root-level capability organization and namespace alignment.

Verify/restore the canonical MediatR validation pipeline and add Order input validators.

Do NOT change business behavior, endpoint ownership, persistence ownership, or module boundaries.

Reference patterns
Foldering / namespace

Capability-driven organization.

Use the clean existing shapes under:

Application/Admin

Application/Customer

Application/Seller

Application/Storefront

Do not keep capability-specific files at Application root.

Path and namespace MUST align.

Validation

Required flow:

Endpoint
-> ISender
-> ValidationBehavior<TRequest,TResponse>
-> IValidator<TRequest>
-> Handler
-> Domain / Ports

Validation layer is ONLY for syntactic/input validation.

Business validation remains in:

Handler

Domain

Policy

owning service

GOAL 1 — Application folder / namespace cleanup

Current root-level files to audit and relocate:

CheckoutAbuseContracts.cs

CheckoutCommitBarrier.cs

CheckoutPayableInvariant.cs

CheckoutProcessContracts.cs

CheckoutProcessManager.cs

IReservationCycleCoordinator.cs

IUnpaidOrderExpiryReconciler.cs

OrderContracts.cs

ReservationCycleContracts.cs

ReservationCycleCoordinator.cs

ReservationCyclePolicyResolver.cs

SellerOrderCancellationPolicy.cs

Required target shape

Use this as the intended structure, adjusting only when actual ownership proves a better capability location:

Tooba.Order.Application
├─ Admin
├─ Customer
├─ Seller
│  └─ Policies
│     └─ SellerOrderCancellationPolicy.cs
├─ Storefront
├─ Checkout
│  ├─ Abuse
│  │  └─ CheckoutAbuseContracts.cs
│  ├─ Process
│  │  ├─ CheckoutProcessContracts.cs
│  │  ├─ CheckoutProcessManager.cs
│  │  └─ CheckoutCommitBarrier.cs
│  ├─ Policies
│  │  └─ CheckoutPayableInvariant.cs
│  └─ Contracts-or-Ports
├─ ReservationCycle
│  ├─ Contracts-or-Ports
│  │  ├─ IReservationCycleCoordinator.cs
│  │  ├─ IUnpaidOrderExpiryReconciler.cs
│  │  └─ ReservationCycleContracts.cs
│  ├─ Services
│  │  └─ ReservationCycleCoordinator.cs
│  └─ Policies
│     └─ ReservationCyclePolicyResolver.cs
└─ Shared
   └─ only genuinely cross-capability Order application abstractions
OrderContracts.cs special rule

Do NOT blindly move OrderContracts.cs as one monolithic file.

Audit every symbol in it.

At minimum classify:

Checkout snapshots / SubmitCheckoutCommand

Checkout directory/hold policy

operational note contracts

purchase verification contracts

Split by actual capability where appropriate.

Examples:

checkout request/snapshot/directory types -> Checkout/...

note/admin operational contracts -> appropriate Admin/Checkout capability

purchase verification -> a dedicated PurchaseVerification capability or Shared only if truly cross-capability

PASS requires no "miscellaneous root contract dump".

Namespace rule

After moving files, namespaces must match physical capability paths.

Forbidden:

moving file into ReservationCycle/Policies but leaving namespace Tooba.Order.Application;

broad root namespace aliases used to avoid fixing references

Update all references/usings cleanly.

Root-level allowance

After this task, Tooba.Order.Application root may contain only files that are demonstrably cross-capability foundation types.

Create evidence listing every remaining root-level .cs file and why it is allowed.

No unexplained root-level capability file.

GOAL 2 — MediatR + FluentValidation
First: verify current foundation

Historical evidence states:
docs/evidence/TB-TMAR-FND-001/pipeline-foundation.md
claimed:
ValidationBehavior + LoggingBehavior registered

But current code must be authoritative.

Inspect current BuildingBlocks/Host CQRS registration and determine:

Is FluentValidation package currently installed?

Does ValidationBehavior<TRequest,TResponse> currently exist?

Is it registered as MediatR IPipelineBehavior<,>?

Are validators auto-registered from module assemblies?

Is validation result mapped into canonical Result/SemanticError without exceptions/message parsing?

If current canonical foundation exists

Reuse it.

Do NOT add a second validation pipeline.

If missing/regressed

Restore one reusable foundation implementation in the correct BuildingBlocks/CQRS location.

Do NOT make an Order-only pipeline hack.

Expected concepts:

FluentValidation

IValidator<TRequest>

ValidationBehavior<TRequest,TResponse>

MediatR 12.5 compatible registration

validators discovered/registered from module application assemblies

canonical stable validation error codes

Do not upgrade MediatR beyond 12.5.0.

Validation behavior semantics

Validation failures must:

short-circuit before Handler

return canonical Result / Result<T> failure where request response is Result-based

use stable machine codes

not use localized text as identity

not throw PlatformHttpException

not classify by ex.Message

Unknown framework failures propagate.

If generic Result integration requires a constrained adapter/factory, implement it once in BuildingBlocks.

Do NOT use reflection-heavy brittle response construction if a typed generic design is possible.

Order validator scope

Audit all Order MediatR requests.

Add validators where there is real input/schema validation, for example:

required non-empty Guid supplied by transport/context

required strings

max/min string lengths

paging/page size constraints

numeric ranges

empty collections

malformed code/identifier formats

mutually invalid primitive input combinations

Do NOT put business rules in validators.

Must remain OUTSIDE FluentValidation

Examples:

order belongs to current customer

seller can see category

checkout exists

payment is expired

inventory is available

retry limit reached

fulfillment dispatched

settlement payout completed

cancellation allowed by current state

reservation cycle business state

Those stay Handler/Domain/Policy/Service.

Priority validator coverage

At minimum inspect and add validators where applicable for transport-facing Order requests in:

Admin Completeness

Admin OrdersGrid / legacy list / customers grid

Admin Operations

Admin InventoryRecovery / Supply

Admin Detail

Storefront Checkout

Storefront Shipping

Storefront PendingPayment

Customer Orders / RetryUnpaid

Seller Orders

Do not create empty validators merely to increase coverage.

For requests with no meaningful primitive input, document NO_VALIDATOR_REQUIRED.

Error mapping

Validation failure response must flow through existing:

SemanticError

Order error catalog/resources

ApiResponseFactory

Use stable validation codes.

Prefer field-aware codes or a canonical validation code with structured field metadata if current Result model supports it.

Do not return raw FluentValidation English messages directly as API identity.

Preserve

Absolutely preserve:

all Order endpoint routes and ownership

all CQRS command/query behavior

all business semantics

all Contract boundaries

Host authority = NONE

Checkout state = PAUSED_AT_SAFE_W5_CHECKPOINT

frontend frozen

MediatR 12.5.0

current tests/guards

No schema migration.
No DB change.
No API response contract redesign beyond canonical validation failures.

Do NOT touch

Checkout W6

frontend

other module business code

Order Domain behavior

endpoint route paths

Host ownership

pricing/tax/inventory workflows

unrelated modules except shared CQRS validation foundation if current foundation is missing

Architecture guards

Add durable guards proving:

Foldering

no listed capability-specific file remains at Tooba.Order.Application root

path ↔ namespace alignment for moved files

every remaining root-level file is explicitly allowlisted with reason

no namespace alias workaround

Validation

current backend has exactly one canonical ValidationBehavior registration

MediatR remains 12.5.0

Order transport-facing requests with meaningful input have validators

validators contain no DbContext

validators contain no foreign Application/Infrastructure/Domain dependency

validators contain no business state lookup

validators contain no ex.Message / PlatformHttpException

validation pipeline runs before handlers in focused tests

Tests

Add focused tests proving at least:

invalid primitive request is rejected before handler execution

valid request reaches handler

business-rule failure still occurs in handler/domain, not validator

validation error maps to stable SemanticError/API response

multiple validation errors are deterministic

validator registration is assembly-discovered or canonical, not manual one-off for every type

Evidence

Create:

docs/evidence/TB-TMAR-ORDER-POSTCLOSURE-QUALITY-001/application-organization.md

Include:

before root files

after folder tree

remaining root files + justification

namespace alignment result

Create:

docs/evidence/TB-TMAR-ORDER-POSTCLOSURE-QUALITY-001/validation-pipeline.md

Include:

historical claim vs current actual state

current foundation implementation

registration path

validators added

requests audited with VALIDATOR_ADDED or NO_VALIDATOR_REQUIRED

explicit business rules intentionally excluded from validators

Validation commands

Run:

BuildingBlocks/CQRS tests if shared foundation changed

full Order.Tests

Order architecture guards

Host endpoint ownership/reverse audit guards

focused validation pipeline tests

dotnet build src/backend/Tooba.slnx

SoT

On PASS:

Order remains:
COMPLETE_REFERENCE_PATTERN

Add post-closure hardening evidence/history.

Do NOT reopen business recovery if no business defect is found.

Checkout remains:
PAUSED_AT_SAFE_W5_CHECKPOINT

Next task:
USER_REVIEW_ORDER_POSTCLOSURE_QUALITY

Do NOT start another module/task.

If a business architecture defect is discovered while moving files:
Status = INCOMPLETE
STOP and report it; do not silently repair unrelated business behavior.

PASS criteria

PASS only if:

Root-level Order Application capability clutter is removed.

OrderContracts.cs is classified/split by actual capability rather than blindly moved.

path and namespace align.

Remaining root files are genuinely shared and documented.

Current validation foundation is verified.

If missing, one reusable canonical FluentValidation/MediatR behavior is restored.

No duplicate validation pipeline exists.

Meaningful transport-facing Order requests have input validators.

Business validation is NOT moved into FluentValidation.

Validation failures use stable Result/SemanticError semantics.

Invalid input short-circuits before Handler in tests.

MediatR stays 12.5.0.

Host authority remains NONE.

Full current-head backend build passes.

Checkout W6 not started.

Frontend unchanged.

Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT

Task-ID: TB-TMAR-ORDER-POSTCLOSURE-QUALITY-001
Parent-Task: TB-TMAR-ORDER-GOLDEN-001-FINAL-CLOSURE
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT

Summary:
Application-Folder-State:
Root-Level-Files-Before:
Root-Level-Files-After:
OrderContracts-State:
Namespace-Alignment-State:
Validation-Foundation-State:
FluentValidation-State:
ValidationBehavior-State:
Validator-Registration-State:
Order-Requests-Audited:
Validators-Added:
No-Validator-Required:
Business-Validation-Separation:
Validation-Error-Semantics:
MediatR-Version:
Architecture-Guard-Validation:
Focused-Validation:
Full-Validation:
Host-Authority-State:
Order-Final-State:
Checkout-State:
Frontend-Production-Changes:
Residual-Defects:
Git:
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
