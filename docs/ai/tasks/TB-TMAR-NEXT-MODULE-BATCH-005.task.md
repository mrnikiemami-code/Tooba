PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-NEXT-MODULE-BATCH-005

Parent-Task:
TB-TMAR-NEXT-MODULE-BATCH-004-R4

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
Cart + Settlement Golden Recovery Batch

Backend-Only:
YES

TMAR-Execution-Mode:
BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

0. Accepted baseline

Architect directly verified BATCH-004-R4.

Accepted:

Fulfillment COMPLETE_REFERENCE_PATTERN

Fulfillment physical VERIFIED_ON_DISK_AND_NAMESPACE

Fulfillment cross-module CONTRACTS_ONLY

Fulfillment endpoints HOST_THIN_TRANSPORT

Fulfillment Host Db authority NONE

Fulfillment work queue APPLICATION_OWNED

Fulfillment shipping tree APPLICATION_OWNED

Fulfillment language gate INFRASTRUCTURE_OWNED

Order fulfillment operation implementation ORDER_OWNED

Order fulfillment adapter anti-pattern CLEAN

Returns COMPLETE_REFERENCE_PATTERN

Notification COMPLETE_REFERENCE_PATTERN

Support COMPLETE_REFERENCE_PATTERN

Wallet COMPLETE_REFERENCE_PATTERN

Payment COMPLETE_REFERENCE_PATTERN

Inventory COMPLETE_REFERENCE_PATTERN

Promotion COMPLETE_REFERENCE_PATTERN

Offer COMPLETE_REFERENCE_PATTERN

Foundation RESULT_PATTERN_FOUNDATION_COMPLETE

Deferred by USER:

Tax physical-folder/namespace review

Pricing physical-folder/namespace review

Checkout remains:
PAUSED_AT_SAFE_W5_CHECKPOINT

Frontend remains frozen.

Do NOT modify Tax/Pricing.
Do NOT resume Checkout.

1. Why Cart + Settlement next

Direct repository inspection found these are bounded high-value next modules and can be recovered without reopening Checkout.

Cart current defects

Tooba.Cart.Application directly references:

Catalog.Application

Inventory.Application

It already has Contracts seams for Offer/Pricing/Inventory but still leaks implementation layers.

Current physical debt:

Domain root god-file CartDomain.cs (~22 KB)

Application root files:

CartContracts.cs

CartConversionAdapter.cs

CartLifetimeOptions.cs

ICartPersistenceHoursSource.cs

Infrastructure root files:

CartCredentialHasher.cs

CartDirectory.cs (~30 KB)

CartModule.cs

CartOutboxRegistration.cs

Cart is checkout-adjacent, so behavior preservation is critical, but Checkout process semantics must remain frozen.

Settlement current defects

Tooba.Settlement.Infrastructure directly references:

Payment.Application

It already uses:

Order.Contracts

Returns.Contracts

Physical debt:

Domain god-file SettlementDomain.cs (~27 KB)

Application root SettlementContracts.cs (~15 KB)

Infrastructure root files:

SettlementDirectory.cs (~29 KB)

payout gateways

event handlers

instrumentation

module

Order/Payment/Returns bridges

outbox registration

Host owns Settlement HTTP surface:

SettlementEndpoints.cs

SettlementPanelComposer.cs

SettlementAdminAccess.cs

Audit whether Host is thin or still owns business/query/presentation authority.

2. Scope

Recover ONLY:

Cart

Settlement

Allowed bounded contract/compile updates:

Catalog

Inventory

Payment

Order

Returns

Host

ONLY where stable public Contracts are required.

Do NOT:

broadly recover Order

resume Checkout

modify Tax/Pricing

touch frontend

reopen already accepted modules beyond compile-only contract adaptation

3. Mandatory behavior-preservation rule

Before moving/splitting code, characterize behavior.

Cart preserve:

anonymous/authenticated cart identity behavior

add/update/remove item behavior

quantity rules

offer/seller/variant linkage

pricing snapshot behavior

inventory availability checks

cart merge/conversion behavior

expiration/lifetime behavior

credential hashing

outbox/domain events

idempotency/concurrency semantics

checkout-facing public Cart.Contracts behavior

Settlement preserve:

settlement creation/accrual

seller payable accounting

refund/reversal effects

release/hold behavior

payout creation/execution/failure

fake/fail-closed payout gateway semantics

payment/order/returns event handling

outbox events

idempotency

monetary amount/currency semantics

transaction boundaries

Accidental behavior changes = 0.

4. Cross-module boundary rule

After repair, production dependencies may cross module boundaries only through public Contracts.

Cart

Forbidden:

Catalog.Application/Domain/Infrastructure

Inventory.Application/Domain/Infrastructure

Offer.Application/Domain/Infrastructure

Pricing.Application/Domain/Infrastructure

foreign DbContext

Allowed as needed:

Catalog.Contracts

Inventory.Contracts

Offer.Contracts

Pricing.Contracts

If Catalog lacks the exact public lookup contract Cart needs:

extract the smallest stable Cart-facing contract into Catalog.Contracts

update implementation atomically

no broad Catalog recovery

If Inventory functionality currently comes from Inventory.Application:

switch to existing Inventory.Contracts where possible

otherwise extract minimum stable public contract

do not reopen Inventory architecture broadly

Settlement

Forbidden:

Payment.Application/Domain/Infrastructure

Order.Application/Domain/Infrastructure

Returns.Application/Domain/Infrastructure

foreign DbContext

Allowed:

Payment.Contracts

Order.Contracts

Returns.Contracts

If required Payment read/event contract is only in Application:

extract minimum public contract to Payment.Contracts

preserve payload/semantic behavior

no broad Payment recovery

5. Physical Visual Studio structure

Mandatory Definition of Done.

Every handwritten production .cs:

meaningful physical responsibility folder

namespace aligned to module + layer + responsibility

root dump = 0 except explicit tiny justified allowlist

no ceremonial folders

no namespace masquerading

no TypeForwardedTo

Suggested Cart:

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

Conversion

Lifetime

Contracts/

Checkout

Items

Dtos

Events

Ports

Errors

Infrastructure/

Persistence

Directories

Security

Adapters

Messaging

DependencyInjection

Suggested Settlement:

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

Payouts

Accounting

Contracts/
Create only if genuine cross-module public surface exists.
No ceremonial project.

Infrastructure/

Persistence

Directories

Gateways

Bridges

Handlers

Messaging

Observability

DependencyInjection

6. God-file decomposition

Do not merely move giant files unchanged.

Cart:

split CartDomain.cs into cohesive domain types

split CartDirectory.cs by real responsibilities if safe

preserve transaction/behavior exactly

Settlement:

split SettlementDomain.cs

split SettlementContracts.cs

split SettlementDirectory.cs by cohesive responsibilities

avoid over-fragmentation

No behavior rewrite for style.

7. Foundation adoption

Audit and repair:

DateTimeOffset.UtcNow / DateTime.UtcNow

Guid.NewGuid / UuidV7.New

hidden ?? new SystemUtcClock()

hidden ?? new UuidV7IdGenerator()

hidden tracer fallback

raw StartActivity

PlatformHttpException in module business code

expected-flow SemanticException misuse

localized exception prose

raw ex.Message leakage

silent/empty catch

catch(Exception) swallowing

Use:

IClock

IIdGenerator

IModuleCallTracer only where actual module call tracing applies

Pure Domain receives id/now explicitly.

8. Result pattern

Expected business failures at owned application boundaries use:

Result

Result<T>

SemanticError

existing ErrorDescriptor catalog

Do not create second taxonomy.
Do not catch arbitrary system Exception into Result.
Unexpected infrastructure failures remain exceptions.

Examples to characterize, not blindly redesign:
Cart:

cart missing/expired

invalid quantity

unavailable offer

unavailable inventory

seller/variant mismatch

Settlement:

missing settlement

invalid transition

release/hold invalid state

payout invalid state

payout gateway expected rejection

9. CQRS / MediatR / endpoint ownership

Audit actual HTTP ownership.

Do not add MediatR ceremonially.

If Cart owns real HTTP use cases:

Host thin transport → ISender → Cart Application Command/Query → handler

Result + ApiResponseFactory

no Host business authority

If Cart has no independent HTTP surface and is internal/checkout-driven:

document correct Endpoint-State honestly

no empty Endpoints project

Settlement demonstrably has Host HTTP files.
Audit them.

For Settlement-owned HTTP use cases:

Host must be thin transport/auth

use ISender / module-owned query/use-case boundary

Result/ApiResponseFactory

no SettlementDbContext/business query authority in Host

no manual error envelope / ex.Message mapping

MediatR exactly 12.5.0 if added/used.

10. Checkout freeze

CRITICAL.

Cart is checkout-adjacent, but this task MUST NOT:

alter CheckoutProcessManager

alter checkout point-of-no-return

resume W6

redesign cart-to-order transaction consistency

change Checkout process retry/compensation semantics

Cart.Contracts used by Checkout must preserve API/behavior.

Any checkout file update must be compile-only namespace/contract adaptation and documented.

11. Cart behavior characterization

Focused minimum:

create/get cart identity

add item

update quantity

remove item

expiration/lifetime

anonymous→authenticated conversion/merge if present

pricing/offer snapshot preservation

inventory availability result behavior

one outbox/domain event path

checkout-facing contract parity

Reuse existing tests where possible.

12. Settlement behavior characterization

Focused minimum:

accrual/create path

release/hold transition

refund/reversal adjustment

payout success

payout failure/fail-closed path

idempotent event handling

Payment integration contract path

Order/Returns bridge path

outbox event

currency/amount precision

13. Host authority audit
Cart

Search entire Host for:

CartDbContext

CartDirectory concrete use

cart business policy in Host

manual cart error mapping

direct foreign DbContext usage in cart routes

Move production query/business authority into Cart module.

Settlement

Audit:

SettlementEndpoints.cs

SettlementPanelComposer.cs

SettlementAdminAccess.cs

Host may:

authenticate

bind transport

call ISender/module ports

ApiResponseFactory

Host must not:

use SettlementDbContext for business/query

implement settlement state rules

map semantic errors manually

expose ex.Message

own payout/hold/release policy

Bootstrap/migration DbContext use may remain explicitly justified.

14. Contracts / events

Preserve all existing public contracts already consumed by Order/Checkout/Returns/etc.

Do not rename externally consumed event types gratuitously.

If moving types from Application to Contracts:

preserve semantic event names

preserve payload fields

update producer/consumer atomically

no duplicate compatibility classes

15. Architecture guards

Cart guard:

Domain no foreign module refs except BuildingBlocks primitives if approved

Application foreign modules only *.Contracts

Infrastructure no foreign module Application/Domain/Infrastructure

no foreign DbContext

root dump=0

path↔namespace aligned

no TypeForwardedTo

no direct clock/id bypass

no hidden DI fallbacks

no localized exception prose

no silent catch

no raw StartActivity

Host CartDbContext production authority absent

Settlement guard:

same generic checks

Payment/Order/Returns only Contracts

no foreign DbContext

root dump=0

path↔namespace aligned

no TypeForwardedTo

no bypass/fallback/prose/silent catch/raw StartActivity

Host SettlementDbContext production authority absent except bootstrap

If Settlement has HTTP:

guard Host thin transport

no manual Results.Json business-error envelope

no raw ex.Message

16. Evidence

Create under:
docs/evidence/TB-TMAR-NEXT-MODULE-BATCH-005/

Required:

recovery-start.md

cart-audit.md

settlement-audit.md

cart-physical-tree.md

settlement-physical-tree.md

cross-module-boundary-audit.md

behavior-preservation-audit.md

foundation-adoption-scan.md

host-authority-audit.md

host-dbcontext-scan.md

recovery-sot.md

Physical trees list EVERY handwritten production .cs:

relative path

namespace

responsibility

Behavior audit classifications:

structural-only

intentional boundary abstraction

intentional behavior repair

accidental behavior change

Target accidental = 0.

17. Fast-Safe validation

Required:

Cart focused tests/guards

Settlement focused tests/guards

focused contract-owner build/tests only for extracted contracts

focused Host tests only for changed Cart/Settlement endpoint seams

one final:
dotnet build src/backend/Tooba.slnx

Conditional:

Inventory/Catalog/Payment tests only if their public Contracts change

Checkout tests only if compile seam changed and only focused parity tests, NOT workflow resume

Do NOT run:

broad Host suite

full Checkout workflow

Tax/Pricing

frontend

unrelated module suites

No retry/sleep workaround.

18. AntiPattern Gate

Must be CLEAN for:

foreign Application/Domain/Infrastructure references

foreign DbContext

cross-module SQL/FK

root dumping

namespace masquerading

TypeForwardedTo

direct clock/id bypass

hidden fallback implementations

localized exception prose

silent catch

raw StartActivity

Host business/query authority

manual semantic error envelopes

deleted side effects

changed checkout semantics

Tax/Pricing touch

frontend change

19. Success state

Cart-State:
COMPLETE_REFERENCE_PATTERN

Cart-Physical-State:
VERIFIED_ON_DISK_AND_NAMESPACE

Cart-CrossModule-Boundary:
CONTRACTS_ONLY

Cart-Behavior-Preservation:
VERIFIED

Settlement-State:
COMPLETE_REFERENCE_PATTERN

Settlement-Physical-State:
VERIFIED_ON_DISK_AND_NAMESPACE

Settlement-CrossModule-Boundary:
CONTRACTS_ONLY

Settlement-Behavior-Preservation:
VERIFIED

Settlement-Endpoint-State:
HOST_THIN_TRANSPORT_OR_NOT_APPLICABLE

Settlement-Host-DbAuthority:
NONE

Behavior-Preservation:
VERIFIED

Fulfillment-State:
COMPLETE_REFERENCE_PATTERN
Returns-State:
COMPLETE_REFERENCE_PATTERN
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
NEXT_REFERENCE_BATCH_005_COMPLETE

Next-Recommended-Task:
TB-TMAR-NEXT-MODULE-BATCH-006

If one module remains blocked:

preserve completed module

mark blocked module INCOMPLETE

recommend focused R1 only

20. Canonical Result

Return ONLY BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Track
Recovery-Start
Cart-Audit
Cart-Physical-Tree
Cart-Repairs
Cart-CrossModule-Boundary
Cart-Result-Adoption
Cart-CQRS-State
Cart-Endpoint-State
Cart-Foundation-Adoption
Cart-Behavior-Parity
Cart-Guards
Cart-Validation
Cart-State
Cart-Physical-State
Settlement-Audit
Settlement-Physical-Tree
Settlement-Repairs
Settlement-CrossModule-Boundary
Settlement-Result-Adoption
Settlement-CQRS-State
Settlement-Endpoint-State
Settlement-Foundation-Adoption
Settlement-Behavior-Parity
Settlement-Host-DbAuthority
Settlement-Guards
Settlement-Validation
Cross-Module-Boundary-Audit
Host-Authority-Audit
Host-DbContext-Scan
Foundation-Adoption-Scan
Behavior-Preservation-Audit
Focused-Validation
Skipped-Validation
Full-Validation
AntiPattern-Gate
Residual-Defects
Behavior-Preservation
Fulfillment-State
Returns-State
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