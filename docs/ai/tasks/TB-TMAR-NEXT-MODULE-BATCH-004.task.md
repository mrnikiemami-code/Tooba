PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-NEXT-MODULE-BATCH-004

Parent-Task:
TB-TMAR-NEXT-MODULE-BATCH-003-R1

Channel:
tooba-main

WorkerId:
tooba-worker-01

AgentType:
cursor

Program:
TMAR — Tooba Microservice-Ready Architecture Recovery

Mode:
FAST-SAFE

Track:
NEXT_REFERENCE_MODULE_BATCH

Title:
Fulfillment + Returns Golden Recovery Batch

Backend-Only:
YES

TMAR-Execution-Mode:
BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

0. Accepted baseline

Architect directly verified BATCH-003-R1.

Accepted:

Notification-State: COMPLETE_REFERENCE_PATTERN

Notification-Physical-State: VERIFIED_ON_DISK_AND_NAMESPACE

Notification-Public-Boundary: CONTRACTS_ONLY

Notification-Order-Boundary: CONTRACTS_ONLY

Support-State: COMPLETE_REFERENCE_PATTERN

Support-Physical-State: VERIFIED_ON_DISK_AND_NAMESPACE

Support-Notification-Boundary: CONTRACTS_ONLY

Wallet-State: COMPLETE_REFERENCE_PATTERN

Payment-State: COMPLETE_REFERENCE_PATTERN

Inventory-State: COMPLETE_REFERENCE_PATTERN

Promotion-State: COMPLETE_REFERENCE_PATTERN

Offer-State: COMPLETE_REFERENCE_PATTERN

Foundation-State: RESULT_PATTERN_FOUNDATION_COMPLETE

Checkout-State: PAUSED_AT_SAFE_W5_CHECKPOINT

Frontend frozen

Deferred by USER:

Tax physical-folder/namespace review

Pricing physical-folder/namespace review

DO NOT modify Tax/Pricing.

Current origin/main must be refreshed before starting.

1. Why Fulfillment + Returns next

Direct repository inspection found strong cross-module implementation coupling and root-dump debt in both modules.

Fulfillment current concrete defects

Tooba.Fulfillment.Infrastructure directly references:

Order.Application

Inventory.Application

Payment.Application

Current root-dumped production files include:

FulfillmentDirectory.cs

FulfillmentInstrumentation.cs

FulfillmentInventoryGateway.cs

FulfillmentModule.cs

FulfillmentOutboxRegistration.cs

FulfillmentPaymentSucceededHandler.cs

FulfillmentReturnBridge.cs

FulfillmentSellerOrderCancelGate.cs

ShippingServiceDirectory.cs

FulfillmentDirectory.cs is very large (~47 KB).
Application also contains broad/root files such as shipping method registry/write contracts/handlers.

Returns current concrete defects

Tooba.Returns.Infrastructure directly references:

Order.Application

Fulfillment.Application

Payment.Application

Inventory.Application

It already references Wallet.Contracts.

Current root-dumped production files include:

ReturnDirectory.cs

ReturnEligibilityEvaluator.cs

ReturnInventoryGateway.cs

ReturnSettlementBridge.cs

ReturnsInstrumentation.cs

ReturnsModule.cs

ReturnsOutboxRegistration.cs

Application is root-dumped in broad contract files.

Goal:
make both modules strict Golden reference modules using Contracts-only cross-module boundaries while preserving business behavior.

2. Scope

Recover ONLY:

Fulfillment

Returns

Allowed bounded compile/contract updates:

Order

Inventory

Payment

Wallet

Notification

Settlement

Host

ONLY where public contract extraction/namespace updates are required.

Do NOT:

recover Order broadly

recover Settlement broadly

resume Checkout

modify Tax/Pricing

touch frontend

3. Mandatory behavior-preservation rule

This batch moves high-risk fulfillment/return logic.

Before changing code, characterize behavior.

Preserve:

fulfillment creation semantics

payment-succeeded fulfillment trigger semantics

seller-order linkage

inventory reservation/release/consumption semantics

shipping method/service behavior

dispatch/shipment transitions

cancellation eligibility/gates

return eligibility

return creation/approval/rejection lifecycle

refund initiation/result handling

wallet refund-credit path

inventory return/restock path

settlement bridge behavior

outbox integration events

idempotency

concurrency/transaction boundaries

event type names/payloads

Accidental behavior changes = blocker.

No logic may be removed merely to eliminate dependency edges.

4. Cross-module boundary rule

After repair:

Fulfillment may depend on foreign modules ONLY through public Contracts:

Order.Contracts

Inventory.Contracts

Payment.Contracts

Returns.Contracts where needed

Returns may depend on foreign modules ONLY through public Contracts:

Order.Contracts

Fulfillment.Contracts

Payment.Contracts

Wallet.Contracts

Inventory.Contracts

Settlement.Contracts if such public contract is genuinely needed

Forbidden from Fulfillment/Returns production projects:

foreign .Application

foreign .Domain

foreign .Infrastructure

foreign DbContext

Host as service-locator/intermediary

cross-module SQL

If a needed abstraction currently exists only in a foreign Application:
extract ONLY the smallest true cross-module port/DTO to the owning module Contracts project.

Do not broadly recover the owner module.

No duplicate wrappers.
No TypeForwardedTo.

5. Fulfillment ownership

Fulfillment owns:

fulfillment aggregate/lifecycle

shipment lifecycle

shipping services/method configuration that is truly fulfillment-owned

fulfillment persistence/schema/migrations

fulfillment integration events

fulfillment-specific observability

Fulfillment must NOT own:

Order state

Payment state

Inventory stock mutation internals

Returns business state

Use public ports/contracts.

6. Returns ownership

Returns owns:

return request lifecycle

eligibility/policy

return persistence/schema/migrations

refund orchestration decisions belonging to Returns

return integration events

returns observability

Returns must NOT:

mutate Payment internals

mutate Wallet internals directly

access Inventory internals

access Fulfillment internals

read Order implementation state directly

All through Contracts.

7. Physical Visual Studio structure

Definition of Done from start.

Every handwritten production .cs:

meaningful physical responsibility folder

matching namespace

no root dump except explicit tiny justified allowlist

no ceremonial empty folders

Suggested Fulfillment:

Domain/

Aggregates

Entities

ValueObjects

Events

Policies

Application/

Ports

Models

Commands

Queries

Shipping

Orders

Returns

Contracts/

Events

Orders

Inventory

Returns

Shipping

Dtos

Infrastructure/

Persistence

Directories

Gateways

Adapters

Handlers

Messaging

Observability

DependencyInjection

Suggested Returns:

Domain/

Aggregates

Entities

ValueObjects

Events

Policies

Application/

Ports

Models

Commands

Queries

Eligibility

Refunds

Inventory

Contracts/

Events

Fulfillment

Refunds

Inventory

Dtos

Infrastructure/

Persistence

Directories

Evaluators

Gateways

Bridges

Messaging

Observability

DependencyInjection

Only create folders with real code.

8. God-file decomposition

Do not merely move giant files unchanged.

Fulfillment:

split FulfillmentDirectory.cs by cohesive orchestration responsibilities

split broad shipping registry/contracts where responsibility is clear

Returns:

split ReturnDirectory.cs / evaluator only where cohesion clearly benefits

do not over-fragment

Preserve method behavior exactly unless an intentional repair is explicitly documented.

9. Foundation adoption

Audit both modules for:

DateTimeOffset.UtcNow / DateTime.UtcNow

Guid.NewGuid / UuidV7.New

hidden fallback clock/id/tracer

PlatformHttpException

SemanticException expected-flow misuse

localized exception prose

silent/empty catch

catch(Exception) swallowing

raw StartActivity / ActivitySource

raw ex.Message leakage

manual ProblemDetails mapping

Use:

IClock

IIdGenerator

IModuleCallTracer only for real module calls

Pure Domain receives explicit id/now.

No hidden fallback implementations.

10. Result pattern

Expected business outcomes at module-owned application boundaries:

Result

Result<T>

Use existing:

SemanticError

ErrorDescriptor

ApiResponseFactory where HTTP is owned

Do NOT:

add new error taxonomy

catch arbitrary Exception into Result

use exceptions for expected flow where existing Result pattern naturally applies

rewrite all legacy directory methods solely for cosmetics

Document Result-Adoption state honestly.

11. CQRS / MediatR / endpoints

Audit actual HTTP ownership.

Do not add MediatR ceremonially.

If Fulfillment/Returns own real HTTP use cases:

Endpoint -> ISender -> Command/Query -> Handler

MediatR exactly 12.5.0

Result/ApiResponseFactory

If Host currently owns transport and module remains internal directory/ports:

Endpoint-State: NOT_APPLICABLE

no empty Endpoints project

no fake commands/handlers

If some real use cases already use MediatR, preserve/complete them instead of regressing to Directory calls.

12. Event contracts

BATCH-003 already extracted public Fulfillment and Returns integration events into:

Tooba.Fulfillment.Contracts.Events

Tooba.Returns.Contracts.Events

Preserve those exact event names/payload semantics.

Do not move them back into Application/Infrastructure.
Do not duplicate event classes.

Outbox registrations must publish the same semantic contracts.

13. Payment / Wallet / Inventory boundaries

Fulfillment:

Payment success consumption must use Payment.Contracts

Inventory mutation must use Inventory.Contracts

Order interaction must use Order.Contracts

Returns:

refund/payment operations must use Payment.Contracts

wallet credit must use Wallet.Contracts

inventory return/restock must use Inventory.Contracts

fulfillment state lookup/action must use Fulfillment.Contracts

order lookup must use Order.Contracts

If a contract does not yet exist:
extract only the minimum stable port/DTO.

14. Settlement boundary

ReturnSettlementBridge must be audited.

If it calls Settlement via foreign Application:

prefer a minimal Settlement.Contracts port if cross-module and stable

do NOT broadly recover Settlement

preserve settlement behavior

If no Settlement dependency exists, document N/A.

15. Host leaks

Audit Host production for:

FulfillmentDbContext

ReturnsDbContext

Allowed only:

migration/bootstrap/seed if explicitly justified

Production query/business authority belongs inside module ports/directories.

Do not expand Host responsibilities.

16. Architecture guards

Fulfillment guards:

Domain no Contracts/Application/Infrastructure/Host

Contracts no Domain/Application/Infrastructure/Host

Application no foreign Application/Infrastructure

Infrastructure foreign-module refs only *.Contracts

reject Order.Application/Domain/Infrastructure

reject Inventory.Application/Domain/Infrastructure

reject Payment.Application/Domain/Infrastructure

no foreign DbContext

root dump=0

path↔namespace aligned

no TypeForwardedTo

no clock/id bypass

no DI fallback

no silent catch

no localized exception prose

no raw StartActivity

Returns guards:

same generic rules

reject Order/Fulfillment/Payment/Inventory Application/Domain/Infrastructure

Wallet.Contracts only

Settlement.Contracts only if used

no foreign DbContext

root dump=0

path↔namespace aligned

no bypass/fallback/silent catch/prose

17. Behavior characterization tests

High-value only.

Fulfillment required minimum:

payment-succeeded trigger creates/advances fulfillment exactly once or existing idempotency semantics

inventory contract call preserves quantity/order/item semantics

seller-order cancellation gate behavior

shipment/dispatch transition

one shipping service write/read behavior

Returns required minimum:

eligibility allowed case

eligibility denied case

create request lifecycle

approve/reject transition

refund path preserves amount/currency/payment/return IDs

wallet refund-credit semantics if used

inventory return/restock semantics

duplicate/idempotent path where present

Reuse existing tests instead of duplicating.

18. Behavior preservation audit

Create explicit before/after audit against current baseline main at task start.

Classify every major flow:

structural-only

intentional boundary abstraction

intentional behavior repair

accidental behavior change

Target:
accidental behavior change = 0

Any intentional behavior repair must be:

small

justified

tested

called out in Result

19. Evidence

Create:

docs/evidence/TB-TMAR-NEXT-MODULE-BATCH-004/recovery-start.md
fulfillment-audit.md
returns-audit.md
fulfillment-physical-tree.md
returns-physical-tree.md
cross-module-boundary-audit.md
behavior-preservation-audit.md
foundation-adoption-scan.md
host-leak-scan.md
recovery-sot.md

Physical tree docs list EVERY handwritten production .cs:

path

namespace

responsibility

20. Fast-Safe validation

Required:

Fulfillment focused tests/guards

Returns focused tests/guards

focused contract-owner tests/builds only for extracted contracts

one final:
dotnet build src/backend/Tooba.slnx

Conditional:

Order/Payment/Inventory/Wallet tests only if public contract behavior changes

Host focused tests only if DI/composition changes

Do NOT run:

broad Host integration suite

unrelated module suites

Tax/Pricing

frontend

Checkout workflow suite unless a compile-only seam truly requires it

No retry/sleep workaround.

21. Checkout freeze

CRITICAL:

Checkout remains:
PAUSED_AT_SAFE_W5_CHECKPOINT

Do not:

alter CheckoutProcessManager semantics

resume W6

change point-of-no-return rules

change checkout transaction orchestration

If Fulfillment/Returns contract extraction touches Order files used by Checkout:

compile-only namespace update only

preserve exact behavior

document it

22. AntiPattern gate

Must be CLEAN:

foreign Application/Domain/Infrastructure refs

foreign DbContext

cross-module SQL/FK

root dumping

namespace masquerading

TypeForwardedTo

direct clock/id bypass

hidden DI fallback

silent catch

localized exception prose

raw StartActivity

deleted side effects

changed event contracts

ceremonial Endpoints/MediatR

Checkout resume

Tax/Pricing touch

frontend changes

23. Success state

Fulfillment-State:
COMPLETE_REFERENCE_PATTERN

Fulfillment-Physical-State:
VERIFIED_ON_DISK_AND_NAMESPACE

Fulfillment-CrossModule-Boundary:
CONTRACTS_ONLY

Returns-State:
COMPLETE_REFERENCE_PATTERN

Returns-Physical-State:
VERIFIED_ON_DISK_AND_NAMESPACE

Returns-CrossModule-Boundary:
CONTRACTS_ONLY

Behavior-Preservation:
VERIFIED

Notification-State:
COMPLETE_REFERENCE_PATTERN

Support-State:
COMPLETE_REFERENCE_PATTERN

Wallet-State:
COMPLETE_REFERENCE_PATTERN

Payment-State:
COMPLETE_REFERENCE_PATTERN

Inventory-State:
COMPLETE_REFERENCE_PATTERN

Promotion-State:
COMPLETE_REFERENCE_PATTERN

Offer-State:
COMPLETE_REFERENCE_PATTERN

Foundation-State:
RESULT_PATTERN_FOUNDATION_COMPLETE

Tax-State:
DEFERRED_PHYSICAL_REVIEW_BY_USER

Pricing-State:
DEFERRED_PHYSICAL_REVIEW_BY_USER

Checkout-State:
PAUSED_AT_SAFE_W5_CHECKPOINT

Frontend-Production-Changes:
NONE

Batch-State:
COMPLETE

Module-Recovery-State:
NEXT_REFERENCE_BATCH_004_COMPLETE

Next-Recommended-Task:
TB-TMAR-NEXT-MODULE-BATCH-005

If one module remains blocked:

preserve completed module

mark blocked module INCOMPLETE

recommend focused R1 only

24. Canonical Result

Return ONLY BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Track
Recovery-Start
Fulfillment-Audit
Fulfillment-Physical-Tree
Fulfillment-Repairs
Fulfillment-CrossModule-Boundary
Fulfillment-Result-Adoption
Fulfillment-CQRS-State
Fulfillment-Endpoint-State
Fulfillment-Foundation-Adoption
Fulfillment-Behavior-Parity
Fulfillment-Guards
Fulfillment-Validation
Fulfillment-State
Fulfillment-Physical-State
Returns-Audit
Returns-Physical-Tree
Returns-Repairs
Returns-CrossModule-Boundary
Returns-Result-Adoption
Returns-CQRS-State
Returns-Endpoint-State
Returns-Foundation-Adoption
Returns-Behavior-Parity
Returns-Guards
Returns-Validation
Cross-Module-Boundary-Audit
Host-Leak-Scan
Foundation-Adoption-Scan
Behavior-Preservation
Focused-Validation
Skipped-Validation
Full-Validation
AntiPattern-Gate
Residual-Defects
Notification-State
Support-State
Wallet-State
Payment-State
Inventory-State
Promotion-State
Offer-State
Foundation-State
Tax-State
Pricing-State
Checkout-State
Frontend-Production-Changes
Batch-State
Git
Blockers
User-Work-Preserved
Module-Recovery-State
Next-Recommended-Task

After canonical Result:
STOP completely.
Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_TASK