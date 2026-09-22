PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-PAYMENT-GOLDEN-001-R2
Parent-Task: TB-TMAR-PAYMENT-GOLDEN-001-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: FAST-SAFE
Track: PAYMENT_GOLDEN_CLOSURE_R2
Title: Payment Final Microservice-Ready Boundary Closure + Host Adapter Debt Removal + COMPLETE
Backend-Only: YES

Architect verification of R1

Architect directly inspected current main after R1 PASS.

Confirmed:

Tooba.Payment.Endpoints is real and wired.

Host webhook endpoint is removed.

Payment storefront routes are already in Payment.Endpoints.Storefront.

Payment admin routes, including /v1/admin/payments/query, are already in Payment.Endpoints.Admin.

StorefrontPaymentComposer.cs is removed.

AdminPaymentsGridQueryEngine.cs is removed.

real Payment Commands/Queries exist.

reconciliation Host worker is now ISender-based and scheduler-only by R1 guard.

tmar-current-state.json correctly says:
Payment = IN_PROGRESS_GOLDEN_R2_READY
nextTask = TB-TMAR-PAYMENT-GOLDEN-001-R2

R1 did more than its minimum scope. Good.

However Payment is NOT yet accepted COMPLETE because direct repo inspection found remaining microservice-readiness debt in Host adapters and cross-module seams.

1. Critical R2 findings to fix
A. HostPaymentAdminOrderEnrichmentAdapter is NOT composition-only

Current:
Host/Admin/HostPaymentAdminOrderEnrichmentAdapter.cs

It directly uses:

OrderDbContext

OrderSupplyComposer

IReservationCycleDirectory

Order Application

Order Infrastructure

Host Grid/Storefront mapping helpers

It performs:

search queries

supply filtering

reservation filtering

order/customer mapping

payment admin row enrichment

This is real cross-module business/read authority in Host.

It violates final target:
Host = composition/security only.

This adapter MUST NOT remain in final Payment COMPLETE state.

B. HostPaymentUnpaidRetrySupplyAdapter still owns business orchestration

Current:
Host/Storefront/HostPaymentUnpaidRetrySupplyAdapter.cs

It directly uses:

OrderSupplyComposer

ReservationCycleCoordinator

Inventory.Application enum/types

business retry/supply decisions

exception translation

This is NOT a tiny security/composition adapter.

Must move behind owning-module Contracts/Gate implementation outside Host.

C. HostPaymentProofMediaAdapter depends on Media.Application

Current:
Host/Storefront/HostPaymentProofMediaAdapter.cs

Payment Application is clean, but the cross-module seam is still Host → Media.Application.

For easy microservice extraction, Media must expose a stable contract/gateway.
Media currently has no Contracts project.

R2 must create the smallest justified Media cross-module contract or equivalent stable module-owned boundary.

D. HostStorefrontCheckoutPaymentAccessAdapter depends on Host checkout composer

Current:
Host/Storefront/HostStorefrontCheckoutPaymentAccessAdapter.cs

It calls:

StorefrontCheckoutComposer

CheckoutIdentityGate

The security/actor piece may remain Host-owned.
The checkout/payable/ownership read is business authority and must move to an Order/Checkout/Cart-owned Contracts boundary.

Do NOT open Checkout W6 semantics.
This is boundary extraction only.

E. Host still consumes Payment Application ports for pending-payment UI

StorefrontPendingPaymentComposer and Program still reference Payment Application read/admin ports.

Payment COMPLETE requires inbound consumers to depend on Payment.Contracts, not Payment.Application.

Pending-payment UI can remain owned by Order/Storefront for now, but its Payment reads must go through stable Payment.Contracts read contracts.

F. StorefrontPaymentOrchestrator responsibility/folder

Current:
Payment.Application/Models/StorefrontPaymentOrchestrator.cs

This is orchestration, not a model.

Move to a real responsibility folder, e.g.:
Application/Orchestration/StorefrontPaymentOrchestrator.cs
or split into cohesive use-case services if necessary.

Do not keep orchestrator under Models just because guard currently allows it.

2. Final architecture target

After R2:

Payment.Endpoints
→ ISender
→ Payment.Application Commands/Queries
→ Payment-owned ports + foreign Contracts only
→ Payment.Infrastructure / foreign module Contracts implementations

Host may contain ONLY:

IPaymentStorefrontAuthorizer security/session adapter

IPaymentAdminAuthorizer security/capability adapter

BackgroundService lifecycle for reconciliation

Program DI/composition

global/platform concerns

Host must NOT contain:

Payment business/read orchestration

Payment cross-module data composition

foreign DbContext used on behalf of Payment

payment-specific supply retry logic

payment-specific media business adapter

payment-specific checkout business read adapter

3. Checkout/Order payment-access contract

Replace HostStorefrontCheckoutPaymentAccessAdapter business read responsibilities with an owning-module stable contract.

Preferred owner:
Order/Checkout boundary, not Payment and not Host.

Create/reuse minimal contract under:
Tooba.Order.Contracts.Payments
or the already correct owning Contracts module.

Contract must provide exactly what Payment needs:

owned checkout lookup for mutation

owned checkout lookup for payment result

checkout payable amount/currency/order reference

guest/cart ownership semantics needed by current behavior

Implementation belongs to Order Application/Infrastructure boundary, not Host.

Host may still provide the authenticated/guest actor identity/security context to Payment Endpoints via IPaymentStorefrontAuthorizer.

Do NOT:

open Checkout W6

redesign checkout workflow

change transaction semantics

change guest access semantics

expose OrderDbContext across boundary

Payment.Application must consume the new Contracts interface, not IStorefrontCheckoutPaymentAccessPort if that port is merely masking Host business authority.

If keeping a Payment-owned port for anti-corruption is useful, its concrete implementation must be outside Host and use foreign Contracts only.

4. Inventory/Supply retry contract

Replace HostPaymentUnpaidRetrySupplyAdapter.

Use/create minimal stable contract under:
Tooba.Inventory.Contracts and/or owning Order contract if reservation-cycle ownership belongs there.

Contract behavior must preserve:

ensure/reacquire retry semantics

retry limit

unavailable/partially unavailable outcome

reservation retry state

exact stable machine error codes

Implementation belongs to owning module Infrastructure/Application adapter.

Payment must not depend on:

Inventory.Application

Host OrderSupplyComposer

ReservationCycleCoordinator concrete Host path

Host adapter must disappear.

5. Media proof contract

Media currently has no Contracts project.

Create the smallest justified:
Tooba.Media.Contracts

It should contain only stable cross-module asset upload/read contract needed by Payment (and reusable by future modules if generic).

Example intent:

upload stream/file metadata for a known actor

return asset id

Do NOT move Media business logic into Payment.
Do NOT make Payment depend on Media.Application.

Implement contract in Media.Infrastructure/Application boundary.
Register via Media module DI.

Then delete:
HostPaymentProofMediaAdapter

Payment consumes Media.Contracts directly or through a Payment Infrastructure anti-corruption adapter that itself depends only on Media.Contracts.

6. Admin payment enrichment contract

Delete:
HostPaymentAdminOrderEnrichmentAdapter

Payment admin grid must not get OrderDbContext/business composition from Host.

Create/reuse an Order-owned stable read contract for Payment admin enrichment.

This contract may internally orchestrate:

order reference/customer display

supply status

reservation summary

If its implementation needs Inventory/Fulfillment contracts, use Contracts only.

The implementation may live in Order Infrastructure/Application boundary, but its PUBLIC surface belongs to Order.Contracts.

Payment.Application consumes only the contract DTO/interface.

No:

OrderDbContext in Host for Payment

Order.Application ref from Payment

Host mapping helper dependency from Payment

Preserve exact admin grid:

search

filters

sort

page/pageSize

customer name

order reference

supply status

reservation labels/state/cycle/retry flags

Use IClock where time is required.

7. Pending-payment consumer boundary

Audit:
Host/Storefront/StorefrontPendingPaymentComposer.cs

It currently consumes Payment Application ports.

Payment COMPLETE requires external consumers to use Payment.Contracts.

Create/reuse Payment.Contracts read interface(s) for:

latest payment by checkout(s)

operational payment state needed by pending-payment UI

any other read currently consumed outside Payment module

Implement in Payment.Infrastructure.

Update Host/Order-owned consumer to use Tooba.Payment.Contracts, not Tooba.Payment.Application.

Do NOT move the whole pending-payment/order UI into Payment if its primary ownership is Order/Checkout.
The goal is boundary correctness, not arbitrary relocation.

After R2, production Host should have no direct using Tooba.Payment.Application.Ports for business reads except reconciliation command dispatch through MediatR/composition if unavoidable.

8. Storefront Payment orchestration physical ownership

Move:
Payment.Application/Models/StorefrontPaymentOrchestrator.cs

to:
Payment.Application/Orchestration/StorefrontPaymentOrchestrator.cs
or a better cohesive responsibility folder.

Update namespace accordingly.

Models folder must contain models/DTOs, not orchestration services.

If the orchestrator is too multi-responsibility, split only along clear cohesive seams:

initiation/payment method policy

retrieval/manual flow

retry/proof flow

Do NOT over-split into ceremony.
Do NOT rewrite financial logic.

Add Orchestration to allowed Application folders only if real code exists.

9. Final Host payment authority scan

After R2, scan all Host production files.

Allowed Payment-specific Host references:

MapPaymentEndpoints()

AddPaymentEndpointPresentation()

Payment endpoint authorizer interfaces + Host security adapters

ReconcileStalePaymentsCommand dispatch in scheduler

Payment.Contracts consumption by non-Payment owning UI

module DI/composition

Forbidden:

IPaymentDirectory

IPaymentAdminDirectory

IPaymentQueryDirectory

IPaymentExpiryDirectory

IOrderPaymentProjection
from Host business/UI code

Payment.Domain

Payment.Infrastructure
from Host business/UI code

Payment-specific business adapter backed by foreign Application/Infrastructure/DbContext

Explicitly inspect:

Program.cs

StorefrontPendingPaymentComposer.cs

StorefrontPendingPaymentProjector.cs

AdminPanelComposer / Admin routes

reconciliation worker

any Payment-named Host adapters

Security-only authorizers are allowed.

10. Cross-module project reference target

Payment.Application may reference:

BuildingBlocks

Payment.Domain

Payment.Contracts

foreign Contracts only

Payment.Infrastructure may reference:

Payment Application/Domain/Contracts

foreign Contracts only

Payment.Endpoints may reference:

Payment.Application

Payment.Contracts

BuildingBlocks

No foreign Application/Infrastructure in Payment module.

Foreign owning modules may add Contracts projects/ports as needed, but no broad module refactor.

11. Result/error semantics final verification

Re-audit every Payment HTTP Command/Query.

Requirements:

expected business outcomes => Result/Result<T>

exact stable code mapper only

no Contains/StartsWith/prose heuristic

no broad fallback to payment.rejected

no localized exception prose as classification

no ex.Message in HTTP

unknown exceptions propagate

Preserve external error-code/status compatibility.

Webhook, storefront, admin and proof upload all included.

12. Reconciliation final verification

Host worker stays scheduler-only.

Verify:

ISender dispatch

no Payment directory

no UtcNow

no Guid.NewGuid

no raw StartActivity

no business state branching beyond scheduler/process logging

Application owns reconciliation semantics and uses IClock.

13. Physical/folder final verification

Payment must end as:

Tooba.Payment.Domain

Tooba.Payment.Application

Tooba.Payment.Contracts

Tooba.Payment.Infrastructure

Tooba.Payment.Endpoints

Tooba.Payment.Tests

Application:

Commands/<UseCase>/

Queries/<UseCase>/

Orchestration/ if used

Models/

Ports/

Errors/

Endpoints:

Storefront/

Admin/

Webhooks/

Errors/Resources if used

No root dump.
Path↔namespace aligned.
No TypeForwardedTo.

Also ensure any newly introduced Media.Contracts / Order.Contracts / Inventory.Contracts code has real physical folder/namespace alignment.

14. Durable guards

Strengthen PaymentArchitectureGuardTests to fail if any R2 debt returns.

Must enforce:

no HostPaymentAdminOrderEnrichmentAdapter

no HostPaymentUnpaidRetrySupplyAdapter

no HostPaymentProofMediaAdapter

no HostStorefrontCheckoutPaymentAccessAdapter business adapter

no Payment business port implementation in Host except explicit security authorizers

Host non-Payment consumers use Payment.Contracts, not Payment.Application ports

Payment Application/Infrastructure foreign refs are Contracts-only

StorefrontPaymentOrchestrator not under Models

Payment Endpoints all route through ISender

no Host payment routes

no Host payment composers/grid engine/webhook

scheduler-only reconciliation

stable-code-only errors

Update HostModuleEndpointOwnershipTests:
add Payment to canonical COMPLETE HTTP module manifest ONLY at final successful R2 closure.

15. Behavior preservation

Financial behavior must remain identical:

gateway/manual/wallet initiation

full-wallet behavior

payment status transitions

manual evidence/proof

sandbox

retries

idempotency

ownership/guest semantics

admin actions

admin grid

webhook validation/dedup

reconciliation

order/payment projection

hold/retry behavior

outbox/events

refunds/settlement integration

No shortcut.
No first-item/first-order shortcut.
No fake adapter returning simplified data.

16. Focused validation only

Required:

Payment.Tests

focused storefront Payment tests

webhook tests

admin detail/action/grid tests

reconciliation tests

cross-module contract adapter tests added by R2

Payment architecture guards

HostModuleEndpointOwnershipTests

TmarDurableGuardTests

dotnet build src/backend/Tooba.slnx

Do not run broad Host suite.
Do not run Checkout W6/workflow suite.
Do not touch frontend/Tax/Pricing.

17. Recovery SoT — MANDATORY SAME CYCLE

If and only if Architect criteria are satisfied by implementation, Worker may prepare PASS state:

Update:

docs/architecture/tmar-current-state.json

TOOBA-TMAR-MASTER-RECOVERY.md

TOOBA-ARCHITECT-BOOTSTRAP.md

R2 recovery-sot

Set:
Payment:

state = COMPLETE_REFERENCE_PATTERN

httpApplicability = HTTP_OWNING

endpointOwnership = MODULE_ENDPOINTS

cqrs = MEDIATR_12_5

lastAcceptedTask = TB-TMAR-PAYMENT-GOLDEN-001-R2

commit = final R2 tip

Move Payment from reopenedModules → completeReferenceModules.

Set:
nextTask = TB-TMAR-PROMOTION-GOLDEN-001

Remaining:

Promotion = REOPENED_ENDPOINT_CQRS_OWNERSHIP

Offer = NEEDS_FINAL_REVERIFY

Inventory = NEEDS_APPLICABILITY_REVERIFY

Checkout remains paused.
Tax/Pricing untouched.
Frontend frozen.

18. Evidence

Create:
docs/evidence/TB-TMAR-PAYMENT-GOLDEN-001-R2/

Required:

recovery-start.md

payment-r2-boundary-audit.md

checkout-order-contract-audit.md

inventory-supply-contract-audit.md

media-contract-audit.md

admin-grid-contract-audit.md

pending-payment-consumer-audit.md

host-authority-audit.md

payment-error-semantics-audit.md

payment-physical-tree.md

behavior-preservation-audit.md

architecture-guard-audit.md

recovery-state-sync.md

recovery-sot.md

19. Protected state

Cart/Settlement/Fulfillment/Returns/Notification/Support/Wallet remain COMPLETE.
Checkout = PAUSED_AT_SAFE_W5_CHECKPOINT.
Tax/Pricing untouched.
Frontend frozen.

Do NOT start Promotion/Offer/Inventory.

No reset.
No clean.
No force push.
No broad git add.
Stashes/user files untouched.

20. Completion scan

Before PASS scan entire repo for:

HostPaymentAdminOrderEnrichmentAdapter

HostPaymentUnpaidRetrySupplyAdapter

HostPaymentProofMediaAdapter

HostStorefrontCheckoutPaymentAccessAdapter

StorefrontPaymentComposer

AdminPaymentsGridQueryEngine

PaymentWebhookEndpoints under Host

Payment business route mapping in Host

using Tooba.Payment.Application.Ports in Host business/UI consumers

direct foreign Application/Infrastructure refs in Payment

Payment DbContext in Host

Payment Domain/Infrastructure in Host business/UI

Payment Contains/StartsWith exception heuristics

ex.Message HTTP leakage

Guid.NewGuid / DateTimeOffset.UtcNow / StartActivity in Payment protected orchestration

orchestrator under Application/Models

Any unresolved item must be classified:

valid security/composition allowlist with proof, OR

defect => INCOMPLETE.

21. Success criteria

PASS only if ALL:

Payment-HTTP-Ownership: MODULE_ENDPOINTS
Payment-Endpoints-State: REAL_PROJECT_PRESENT
Payment-CQRS-State: MEDIATR_12_5_APPLICATION_HANDLERS
Payment-Storefront-Routes: MODULE_ENDPOINTS
Payment-Admin-Routes: MODULE_ENDPOINTS
Payment-Webhook-Route: MODULE_ENDPOINTS
Payment-Host-Business-Authority: NONE
Payment-Host-DbAuthority: NONE_EXCEPT_PLATFORM_BOOTSTRAP_ALLOWLIST
Payment-Host-CrossModule-BusinessAdapters: NONE
Payment-Reconciliation-Host-Role: SCHEDULER_ONLY
Payment-CheckoutOrder-Boundary: CONTRACTS_ONLY
Payment-Inventory-Boundary: CONTRACTS_ONLY
Payment-Media-Boundary: CONTRACTS_ONLY
Payment-Wallet-Boundary: CONTRACTS_ONLY
Payment-External-Consumers: PAYMENT_CONTRACTS_ONLY
Payment-Result-Adoption: HTTP_USE_CASES_ADOPTED
Payment-Error-Classification: STABLE_CODES_ONLY
Payment-Prose-Mapping: NONE
Payment-Unexpected-Exception-Swallow: NONE
Payment-Physical-State: VERIFIED_ON_DISK_AND_NAMESPACE
Payment-Architecture-Guards: ENFORCED
Payment-Behavior-Preservation: VERIFIED
Payment-Microservice-Extraction: READY_WITHOUT_REWRITE
Recovery-State: CURRENT_AND_MACHINE_READABLE
Recovery-Next-Task: TB-TMAR-PROMOTION-GOLDEN-001
Payment-State: COMPLETE_REFERENCE_PATTERN

22. Canonical Result

Return ONLY BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Track
Recovery-Start
Payment-HTTP-Ownership
Payment-Endpoints-State
Payment-CQRS-State
Payment-Application-UseCases
Payment-Storefront-Routes
Payment-Admin-Routes
Payment-Webhook-Route
Payment-Host-Authority-Audit
Payment-Host-Business-Authority
Payment-Host-DbAuthority
Payment-Host-CrossModule-BusinessAdapters
Payment-Reconciliation-Host-Role
Payment-CheckoutOrder-Boundary
Payment-Inventory-Boundary
Payment-Media-Boundary
Payment-Wallet-Boundary
Payment-External-Consumers
Payment-Result-Adoption
Payment-Error-Classification
Payment-Prose-Mapping
Payment-Unexpected-Exception-Swallow
Payment-Physical-State
Payment-Architecture-Guards
Payment-Behavior-Preservation
Payment-Microservice-Extraction
Recovery-State
Recovery-Next-Task
Current-State-Manifest
Focused-Validation
Skipped-Validation
Full-Validation
AntiPattern-Gate
Residual-Defects
Payment-State
Wallet-State
Support-State
Notification-State
Returns-State
Fulfillment-State
Settlement-State
Cart-State
Promotion-State
Offer-State
Inventory-State
Checkout-State
Tax-State
Pricing-State
Frontend-Production-Changes
Git
Blockers
User-Work-Preserved
Next-Recommended-Task

If incomplete:
Next-Recommended-Task: TB-TMAR-PAYMENT-GOLDEN-001-R2-REPAIR

If complete:
Next-Recommended-Task: TB-TMAR-PROMOTION-GOLDEN-001

After Result:
STOP completely.
Do NOT start Promotion.
Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_TASK