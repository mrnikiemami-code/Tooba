PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_REPAIR_TASK

Task-ID:
TB-TMAR-NEXT-MODULE-BATCH-003-R1

Parent-Task:
TB-TMAR-NEXT-MODULE-BATCH-003

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
REFERENCE_BATCH_REPAIR

Title:
Notification Order Boundary Closure + Guard Correction

Backend-Only:
YES

Architect verdict

Parent PASS is REOPENED for one concrete boundary defect.

Direct repository verification found:

Tooba.Notification.Infrastructure still references:
Tooba.Order.Application

Current file:
src/backend/Modules/Notification/Tooba.Notification.Infrastructure/Tooba.Notification.Infrastructure.csproj

contains:
..\..\Order\Tooba.Order.Application\Tooba.Order.Application.csproj

and:
Notification.Infrastructure/Projectors/NotificationProjector.cs

imports:
Tooba.Order.Application

The parent evidence even says:
Order.Application (recipient reader — allowed; not in remove list)

This contradicts the task's Golden-boundary rule:
Notification Infrastructure must not depend on foreign Application/Domain implementation layers.

The current architecture guard is also incomplete because it checks Payment/Fulfillment/Returns but does NOT reject Order.Application.

Therefore:

Notification-State:
REOPENED_ORDER_APPLICATION_BOUNDARY

Support-State:
PROVISIONALLY_ACCEPTED

Do NOT start BATCH-004.

Tax/Pricing/Checkout/frontend remain untouched.

1. Objective

Close only the remaining Notification → Order implementation-layer dependency and strengthen the guard so it cannot regress.

No broad Order recovery.
No Notification behavior redesign.
No Support redesign.

2. Required boundary

After repair:

Notification.Infrastructure -> Order.Contracts

or another already-existing public Order contract assembly ONLY.

Forbidden:

Order.Application

Order.Domain

Order.Infrastructure

OrderDbContext

Host as intermediary workaround

3. Contract extraction

Current NotificationProjector needs recipient snapshots/read access such as:

IOrderNotificationReader

OrderNotificationRecipientSnapshot

seller-recipient snapshot types

If these currently live in Order.Application:

extract/move ONLY the public notification recipient read contract into Tooba.Order.Contracts

create Tooba.Order.Contracts only if it does not already exist

keep the surface minimal

update Order implementation and Notification consumer atomically

preserve exact behavior and shape

no duplicate compatibility wrappers

no TypeForwardedTo

Do NOT move Order application logic into Contracts.
Contracts contains only cross-module abstraction + DTOs needed by consumers.

4. Behavior preservation

NotificationProjector behavior must remain identical:

GetByCheckoutIdAsync lookup semantics

GetBySellerOrderIdAsync lookup semantics

customer recipient resolution

seller recipient iteration

fallback seller selection

checkout/seller order route derivation

payload enrichment behavior

notification SourceEventId/SourceType semantics

Add/reuse focused test proving at least:

checkout projection creates correct customer + seller recipients

seller-order projection preserves fallback/target behavior

No broad Order integration suite.

5. Order.Contracts structure

If created, physical folders + namespaces are mandatory from day one.

Suggested:
Tooba.Order.Contracts/Notifications/

Types should use:
namespace Tooba.Order.Contracts.Notifications;

No root dump.
No Domain/Application/Infrastructure/Host refs.

Do not create unrelated Order Contracts in this repair.

6. Guard correction

Update NotificationArchitectureGuardTests.

Mandatory:

Notification.Infrastructure MUST reject any project reference containing:

Order.Application

Order.Domain

Order.Infrastructure

Payment.Application

Payment.Domain

Payment.Infrastructure

Fulfillment.Application

Fulfillment.Domain

Fulfillment.Infrastructure

Returns.Application

Returns.Domain

Returns.Infrastructure

allow corresponding *.Contracts

no foreign DbContext references

no source using Tooba.Order.Application

If Order.Contracts is introduced, add a small guard:

path/namespace aligned

no implementation-layer refs

no TypeForwardedTo

7. Preserve accepted BATCH-003 state

Do not reopen already clean parts.

Preserve:

Notification Domain no Contracts ref

Notification Contracts clean

Notification IClock/IIdGenerator

Support→Notification.Contracts only

physical trees

exception codes

SoftDelete behavior already established by parent

Payment/Fulfillment/Returns Contracts event extraction

8. Validation — Fast-Safe

Required:

Notification.Tests focused

Order compile / focused contract test if needed

dotnet build src/backend/Tooba.slnx

Conditional:

Host compile only if DI signature changes

no broad Host integration

no Payment/Fulfillment/Returns suites unless compile seam changed beyond using

Do NOT run:

Tax/Pricing

Checkout

frontend

unrelated suites

9. Evidence

Create:

docs/evidence/TB-TMAR-NEXT-MODULE-BATCH-003-R1/recovery-start.md
order-boundary-audit.md
notification-order-parity.md
guard-delta.md
recovery-sot.md

order-boundary-audit.md must show before/after project references.

notification-order-parity.md must explicitly confirm no notification-recipient logic changed.

10. Success state

Notification-State:
COMPLETE_REFERENCE_PATTERN

Notification-Physical-State:
VERIFIED_ON_DISK_AND_NAMESPACE

Notification-Public-Boundary:
CONTRACTS_ONLY

Notification-Order-Boundary:
CONTRACTS_ONLY

Support-State:
COMPLETE_REFERENCE_PATTERN

Support-Physical-State:
VERIFIED_ON_DISK_AND_NAMESPACE

Support-Notification-Boundary:
CONTRACTS_ONLY

Behavior-Preservation:
VERIFIED

Wallet-State:
COMPLETE_REFERENCE_PATTERN

Payment-State:
COMPLETE_REFERENCE_PATTERN

Foundation-State:
RESULT_PATTERN_FOUNDATION_COMPLETE

Offer-State:
COMPLETE_REFERENCE_PATTERN

Inventory-State:
COMPLETE_REFERENCE_PATTERN

Promotion-State:
COMPLETE_REFERENCE_PATTERN

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
NEXT_REFERENCE_BATCH_003_COMPLETE

Next-Recommended-Task:
TB-TMAR-NEXT-MODULE-BATCH-004

11. Canonical Result

Return ONLY BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Track
Recovery-Start
Order-Boundary-Audit
Order-Contract-Repair
Notification-Order-Parity
Guard-Delta
Notification-Validation
Order-Validation
Focused-Validation
Full-Validation
AntiPattern-Gate
Residual-Defects
Notification-State
Notification-Physical-State
Notification-Public-Boundary
Notification-Order-Boundary
Support-State
Support-Physical-State
Support-Notification-Boundary
Behavior-Preservation
Wallet-State
Payment-State
Foundation-State
Offer-State
Inventory-State
Promotion-State
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

After Result:
STOP.
Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_REPAIR_TASK