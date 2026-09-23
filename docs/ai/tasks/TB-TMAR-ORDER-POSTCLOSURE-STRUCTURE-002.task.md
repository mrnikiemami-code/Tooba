PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ORDER-POSTCLOSURE-STRUCTURE-002
Parent-Task: TB-TMAR-ORDER-POSTCLOSURE-STRUCTURE-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Track: ORDER_INFRASTRUCTURE_STRUCTURE_HARDENING
Title: Reorganize Order.Infrastructure by Capability + Integration Boundary
Backend-Only: YES

Architect decision

STRUCTURE-001 is ACCEPTED.

Order remains:
COMPLETE_REFERENCE_PATTERN

Current remaining structural issue:
Tooba.Order.Infrastructure root still contains many capability-specific and integration-specific files.

This task fixes ONLY Infrastructure organization + namespace alignment.

Do NOT:

change business behavior

change endpoint routes

change Application foldering

change Domain

change Contracts semantics

resume Checkout W6

touch frontend

Reference pattern

Infrastructure must reflect capability ownership visibly in Solution Explorer.

Rules:

root = module composition only

persistence under Persistence

capability implementations under capability folders

foreign integration adapters grouped by integration/owning capability

path ↔ namespace alignment

no miscellaneous root bridge/service dump

Current root files to audit

Current root-level production files include:

CheckoutDirectory.cs

CheckoutProcessTracker.cs

CheckoutSubmitHost.cs

OpenOrderUseCaseGuard.cs

OrderFulfillmentBridge.cs

OrderGridEnrichmentBridge.cs

OrderModule.cs

OrderNotificationBridge.cs

OrderOutboxRegistration.cs

OrderPaymentBridge.cs

OrderPaymentSucceededHandler.cs

OrderPurchaseVerificationGateway.cs

OrderReturnBridge.cs

ReservationCycleCheckoutLineSource.cs

ReservationCycleDirectory.cs

SellerOrderAuthBridge.cs

UnpaidOrderExpiryReconciler.cs

Existing folders:

Admin/

CheckoutAbuse/

Customer/

Events/

Fulfillment/

Payments/

Persistence/

Seller/

Storefront/

Required target principles

Use this target as the baseline:

Tooba.Order.Infrastructure
├─ Admin
├─ Checkout
│  ├─ Abuse
│  ├─ Persistence
│  └─ Services
├─ Customer
├─ Seller
├─ Storefront
├─ ReservationCycle
├─ PurchaseVerification
├─ Integrations
│  ├─ Fulfillment
│  ├─ Payment
│  ├─ Returns
│  └─ Notifications
├─ Events
├─ Guards
├─ Messaging
├─ Persistence
│  ├─ OrderDbContext.cs
│  └─ Migrations/
└─ OrderModule.cs

Adjust only when actual ownership proves a more precise capability location.

Exact relocation expectations
Checkout capability

Move/organize:

CheckoutAbuse/CheckoutAbuseGate.cs
-> Checkout/Abuse/CheckoutAbuseGate.cs

CheckoutDirectory.cs
-> Checkout/Persistence/CheckoutDirectory.cs
OR another more accurate Checkout subfolder if implementation proves it is not persistence-centric

CheckoutProcessTracker.cs
-> Checkout/Persistence/CheckoutProcessTracker.cs

CheckoutSubmitHost.cs
-> Checkout/Services/CheckoutSubmitHost.cs

Do not leave CheckoutAbuse as a top-level sibling capability.

Reservation cycle

Move:

ReservationCycleCheckoutLineSource.cs

ReservationCycleDirectory.cs

UnpaidOrderExpiryReconciler.cs

under:
ReservationCycle/

Use subfolders only if they add clarity, e.g.:

ReservationCycle/Persistence

ReservationCycle/Services

Avoid over-foldering a three-file capability.

Seller

Move:

SellerOrderAuthBridge.cs

under:
Seller/

Keep existing:

Seller/SellerOrderStore.cs

Purchase verification

Move:

OrderPurchaseVerificationGateway.cs

under:
PurchaseVerification/

Integration adapters

Audit each bridge and place by actual foreign boundary.

Expected:

OrderFulfillmentBridge.cs
-> Integrations/Fulfillment/

OrderPaymentBridge.cs
-> Integrations/Payment/

existing Payments/*
should be reviewed for consolidation under either:

Integrations/Payment/
OR

a clearly justified Order-owned Payments/ capability

Do NOT keep two competing Payment adapter locations.

OrderReturnBridge.cs
-> Integrations/Returns/

OrderNotificationBridge.cs
-> Integrations/Notifications/

OrderGridEnrichmentBridge.cs
audit actual usage:

if exclusively Admin OrdersGrid, move to Admin/OrdersGrid/

otherwise place under a clearly named integration/read-model folder

document decision

Events

Audit:

OrderPaymentSucceededHandler.cs

If it is an integration-event consumer, place under:
Events/Payment/
or
Events/

Keep Event ownership clear.

Guards

Move:

OpenOrderUseCaseGuard.cs
-> Guards/OpenOrderUseCaseGuard.cs

Messaging

Move:

OrderOutboxRegistration.cs
-> Messaging/OrderOutboxRegistration.cs

Root

After task, allowed root .cs files should be:

OrderModule.cs

Nothing else unless explicitly justified in evidence.

Existing folders reconciliation

Review these existing top-level folders:

CheckoutAbuse/

Fulfillment/

Payments/

Do not keep redundant duplicate organization.

Expected cleanup:

CheckoutAbuse merged under Checkout/Abuse

root Fulfillment content classified:

if Order-owned Admin fulfillment adapter surface, keep under an explicit capability path such as Admin/Fulfillment OR Integrations/Fulfillment

do not split the same concern between two folders

root Payments content reconciled with OrderPaymentBridge

Goal:
one obvious place per capability/integration.

Namespace alignment

Every moved file must have namespace matching path.

Examples:

Checkout/Abuse/CheckoutAbuseGate.cs
-> Tooba.Order.Infrastructure.Checkout.Abuse

ReservationCycle/ReservationCycleDirectory.cs
-> Tooba.Order.Infrastructure.ReservationCycle

Integrations/Payment/OrderPaymentBridge.cs
-> Tooba.Order.Infrastructure.Integrations.Payment

Messaging/OrderOutboxRegistration.cs
-> Tooba.Order.Infrastructure.Messaging

Forbidden:

moving files but keeping namespace Tooba.Order.Infrastructure;

namespace alias workaround

global using workaround solely to hide folder debt

OrderModule registration

Keep:
OrderModule.cs at root.

Update DI registrations/usings cleanly.

Do not alter service lifetimes unless required to preserve exact existing behavior.

No registration duplication.

Behavior preservation

Absolutely preserve:

all DI registrations

lifetimes

interfaces/ports implemented

EF ownership

DbContext schema/migrations

event handlers

outbox registration

checkout semantics

reservation cycle behavior

payment/fulfillment/return/notification bridges

admin/customer/seller/storefront behavior

No logic changes disguised as move.

No cross-boundary regression

Order Infrastructure may reference foreign Contracts as already allowed.

Do NOT introduce:

foreign DbContext

foreign Infrastructure

foreign Domain entity coupling beyond already approved architecture

new cross-schema joins

Host dependency

Architecture guard

Add durable:
OrderInfrastructureOrganizationGuardTests

Guard must prove:

root .cs allowlist is explicit and ideally only OrderModule.cs

no known capability-specific root files return

path ↔ namespace alignment for moved files

no alias workaround

no duplicate Payment/Fulfillment bridge organization

CheckoutAbuse top-level folder is gone

OrderModule still registers all required services once

Persistence/Migrations path remains unchanged

no Host reference introduced

The guard must fail if future developers add new capability implementation directly to Infrastructure root.

Evidence

Create:

docs/evidence/TB-TMAR-ORDER-POSTCLOSURE-STRUCTURE-002/infrastructure-organization.md

Include:

Before

all root files + existing folders

Ownership classification

for every moved root file:

old path

actual responsibility

new path

reason

After

actual folder tree

Namespace alignment

path -> namespace table

Root allowlist

exact remaining root files + reason

Integration consolidation

explain final location for:

Payment

Fulfillment

Returns

Notifications

Grid enrichment

Validation

Run:

full Tooba.Order.Tests

Order architecture guards

new infrastructure organization guard

Host reverse-audit/endpoint ownership guards

affected integration tests

Tmar durable guards

dotnet build src/backend/Tooba.slnx

SoT

On PASS:

Order remains:
COMPLETE_REFERENCE_PATTERN

Checkout remains:
PAUSED_AT_SAFE_W5_CHECKPOINT

Next task:
TB-TMAR-ORDER-POSTCLOSURE-STRUCTURE-LOCK-001

Do NOT start it.

PASS criteria

PASS only if:

Infrastructure root capability clutter removed.

OrderModule.cs remains root composition entry.

CheckoutAbuse merged under Checkout.

ReservationCycle implementations grouped coherently.

SellerOrderAuthBridge moved under Seller.

PurchaseVerification gateway grouped.

Payment/Fulfillment/Returns/Notification bridge locations are coherent and non-duplicated.

Guards/Messaging grouped.

Path and namespace align.

DI behavior/lifetimes unchanged.

Persistence/Migrations unchanged.

No business behavior changes.

Durable guard prevents root clutter regression.

Full current-head backend build passes.

Frontend unchanged.

Checkout W6 not started.

Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT

Task-ID: TB-TMAR-ORDER-POSTCLOSURE-STRUCTURE-002
Parent-Task: TB-TMAR-ORDER-POSTCLOSURE-STRUCTURE-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT

Summary:
Infrastructure-Folder-State:
Root-Files-Before:
Root-Files-After:
Checkout-Organization-State:
ReservationCycle-Organization-State:
Seller-Organization-State:
PurchaseVerification-State:
Payment-Integration-State:
Fulfillment-Integration-State:
Returns-Integration-State:
Notification-Integration-State:
GridEnrichment-State:
Events-State:
Guards-Messaging-State:
Namespace-Alignment-State:
OrderModule-Registration-State:
Persistence-State:
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

Do not start STRUCTURE-LOCK-001.
Do not resume Checkout W6.
Do not start another module.
Do not poll.

END_TOOBA_TASK