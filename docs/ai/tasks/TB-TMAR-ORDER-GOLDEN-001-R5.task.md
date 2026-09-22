PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ORDER-GOLDEN-001-R5
Parent-Task: TB-TMAR-ORDER-GOLDEN-001-R4-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Track: ORDER_GOLDEN_IMPLEMENTATION_SLICE_5
Title: Move Storefront Order Surfaces Out of Host
Backend-Only: YES

Architect decision

R4-R1 is ACCEPTED at commit:
70e9e3517b88fa7b0f3213a066b308089f16db91

Order remains:
INCOMPLETE_REFERENCE_REPAIR

Reference pattern

Endpoint ownership/foldering:
Fulfillment-style

CQRS/MediatR/Result semantics:
Offer-style

Required flow:
Order.Endpoints
-> ISender
-> explicit Query/Command
-> IRequestHandler
-> Result / SemanticError
-> Order-owned services/ports
-> Order.Infrastructure / foreign Contracts only

Exact R5 scope

Move ONLY Storefront Order business authority out of Host.

Primary Host files in scope:

Host/Storefront/StorefrontCheckoutComposer.cs

Host/Storefront/StorefrontPendingPaymentComposer.cs

Host/Storefront/StorefrontPendingPaymentProjector.cs

Host/Storefront/StorefrontShippingComposer.cs

Host/Storefront/StorefrontShippingCalculator.cs

Also migrate ONLY the Order-related routes currently inside:

Host/Storefront/StorefrontEndpoints.cs

Routes in scope:

Checkout

POST /v1/storefront/checkout/preview

POST /v1/storefront/checkout

GET /v1/storefront/checkout/{checkoutId}

Pending payment

POST /v1/storefront/pending-payments

POST /v1/storefront/checkout/{checkoutId}/cancel

POST /v1/storefront/checkout/{checkoutId}/hide-pending-card

Shipping

POST /v1/storefront/shipping/projection

PUT /v1/storefront/shipping/selection

POST /v1/storefront/shipping/commit

Do not move unrelated Storefront routes.

Do NOT touch

checkout identity policy route

generic storefront/catalog/template/account endpoints

Fashion/Industry template code

StorefrontComposer unrelated catalog/home logic

Order admin detail

CustomerPanel

ReservationCycleCoordinator unless needed only as a narrow Order-owned seam

Checkout W6

frontend

Tax/Pricing unrelated work

Required CQRS split

Create explicit use cases.

Suggested shape:

Checkout Queries/Commands:

PreviewStorefrontCheckout

SubmitStorefrontCheckout

GetStorefrontCheckout

PendingPayment:

ListStorefrontPendingPayments

CancelPendingCheckout

HidePendingPaymentCard

Shipping:

ProjectStorefrontShipping

SaveStorefrontShippingSelection

CommitStorefrontShipping

No generic dispatcher.
No composer facade that switches by operation code.

Ownership

After R5 Host must NOT own:

checkout preview/submit/get business composition

pending payment projection/cancel/hide decisions

shipping projection/selection/commit business rules

OrderDbContext reads/writes for these routes

CatalogDbContext reads for pending payment

direct foreign Application/Infrastructure/Domain orchestration

message-based HTTP exception classification for these routes

Host may retain:

auth/session/security adapters

environment/dev actor adapter if genuinely security/composition-only

unrelated Storefront endpoints

Foreign boundaries

Order Application must NOT reference:

Cart.Application

AddressBook.Application

Catalog.Application/Infrastructure/Domain

Payment.Application/Infrastructure/Domain

Settlement.Application

Fulfillment.Application

foreign DbContext

Use Contracts or narrow ports/adapters.

Where a required Contracts seam does not exist:
create the minimum stable contract in the owning module.

Do not move foreign business logic into Order.

Session / actor resolution

Current Host Checkout uses:

CurrentAuthenticatedSession

Development/Testing actor header

guest actor fallback

Preserve behavior through a narrow Order storefront actor/access port.

Host implementation may remain ONLY as thin adapter because session/environment/HTTP are Host concerns.

No business decisions in that adapter.

Error semantics

Current StorefrontEndpoints contains message-text classifiers for Checkout/Shipping/PendingPayment.

Remove them for migrated routes.

Required:

typed/stable error codes

Result / SemanticError

ApiResponseFactory

Forbidden:

exception.Message.Contains(...)

localized text classification

manual business Results.Json(...)

PlatformHttpException in Order Application

broad catch that hides unknown faults

Unknown exceptions propagate.

Clock / IDs

Use canonical:

IClock

IIdGenerator where new IDs are required

No direct:

DateTimeOffset.UtcNow

DateTime.UtcNow

Guid.NewGuid for business IDs where canonical generator applies

Shipping helper ownership

Move StorefrontShippingCalculator business calculation into Order Application policy/service if it is truly Order shipping composition.

Do not keep Fulfillment.Application references.

Use Fulfillment.Contracts or stable configuration abstractions only.

Pending payment projection

Move StorefrontPendingPaymentProjector into Order Application policy/projection.

It must not reference Payment.Domain/Infrastructure.

Payment status/provider semantics must come through Payment.Contracts.

Settlement dependency must be Contracts-only.

Catalog product/media lookup must use Catalog.Contracts or a narrow port.

Behavior parity

Preserve existing behavior exactly:

Checkout:

guest/auth ownership

dev actor seam

saved-address ownership

cart version conflict

idempotency

shipping snapshot

coupon forwarding

preview vs persisted behavior

Pending payments:

visibility/hide/cancel behavior

reservation-cycle state

retry-limit behavior

manual payment review

payment state

seller/order references

product title/media projection

Shipping:

draft ownership

cart stale/empty/missing

method availability

destination filtering

rate/free threshold

delivery date/time windows

minimum delivery calculation

selection/commit semantics

note/name validation

Do not simplify behavior to achieve architecture.

Host removal requirements

After R5 these production Host files must be absent OR reduced to unrelated non-Order content only:

StorefrontCheckoutComposer.cs

StorefrontPendingPaymentComposer.cs

StorefrontPendingPaymentProjector.cs

StorefrontShippingComposer.cs

StorefrontShippingCalculator.cs

For StorefrontEndpoints.cs:
Order-related routes and their error mapping helpers must be removed from Host.

Unrelated storefront routes may remain.

Guards

Add durable guards proving:

Host storefront Order composer files absent

migrated routes owned by Order.Endpoints

endpoints use ISender

no message-classification for migrated routes

no Order Application -> foreign Application/Infrastructure/Domain refs

no Host OrderDbContext/CatalogDbContext authority for migrated storefront paths

no fake CQRS dispatcher

IClock used where time is needed

R4 Host removals remain absent

Host delta evidence

Create:

docs/evidence/TB-TMAR-ORDER-GOLDEN-001-R5/host-delta.md

Include:

Removed from Host

exact files/types/routes/helpers removed

Still remaining in Host for Order

all remaining Order-related Host business authority

Allowed Host adapters

auth/session/environment/composition-only adapters with reason

Validation

Run:

Order.Tests

new Storefront Order tests

checkout/shipping/pending-payment regression tests

Order architecture guards

affected Cart/Catalog/AddressBook/Payment/Settlement/Fulfillment guards if Contracts changed

focused Host storefront tests

dotnet build src/backend/Tooba.slnx

Frontend must not run/change.

Recovery

On PASS:

Order remains:
INCOMPLETE_REFERENCE_REPAIR

unless only final admin-detail / reverse-audit work remains.

Expected next evidence-driven task:
TB-TMAR-ORDER-GOLDEN-001-R6

Do NOT start it.

Checkout stays:
PAUSED_AT_SAFE_W5_CHECKPOINT

This task migrates current Order-owned Storefront authority only.
It does NOT resume Checkout program work.

PASS criteria

PASS only if:

StorefrontCheckoutComposer business authority removed from Host.

StorefrontPendingPaymentComposer/Projector business authority removed from Host.

StorefrontShippingComposer/Calculator business authority removed from Host.

Nine listed routes owned by Order.Endpoints via ISender.

Real MediatR CQRS use cases exist.

No migrated route uses Host message-based exception mapping.

Foreign boundaries are Contracts/ports only.

No Host OrderDbContext/CatalogDbContext authority for migrated Storefront paths.

Behavior parity preserved.

Tests/build pass.

R4/R4-R1 Host removals remain intact.

Host delta evidence complete.

Checkout W6 not started.

Frontend unchanged.

Otherwise:
Status = INCOMPLETE

Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT

Task-ID: TB-TMAR-ORDER-GOLDEN-001-R5
Parent-Task: TB-TMAR-ORDER-GOLDEN-001-R4-R1
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT

Summary:
Storefront-Checkout-State:
Pending-Payment-State:
Shipping-State:
Order-Endpoint-State:
Order-CQRS-State:
Order-Host-DbAuthority:
Order-Foreign-Boundary:
Error-Semantics:
Behavior-Parity:
Architecture-Guard-Validation:
Focused-Validation:
Full-Validation:
Host-Removed-This-Task:
Host-Still-Remaining-For-Order:
Allowed-Host-Adapters:
Residual-Defects:
Order-Overall-State:
Checkout-State:
Frontend-Production-Changes:
Recovery-State:
Recovery-Next-Task:
Git:
Blockers:
User-Work-Preserved:
Next-Recommended-Task:

END_TOOBA_WORKER_RESULT

STOP

After Result:
STOP completely.

Do not start R6.
Do not resume Checkout W6.
Do not migrate Admin Order detail.
Do not poll.

END_TOOBA_TASK