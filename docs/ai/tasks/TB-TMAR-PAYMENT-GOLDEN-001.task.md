PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-PAYMENT-GOLDEN-001
Parent-Task: TB-TMAR-RECOVERY-LOCK-HARDEN-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: FAST-SAFE
Track: PAYMENT_GOLDEN_CLOSURE
Title: Payment Endpoints Ownership + MediatR CQRS + Host Payment Authority Removal
Backend-Only: YES

Architect decision

Recovery lock hardening is accepted after direct repo verification:

ARCH-COMPLETE-001 exists

ARCH-CQRS-001 now applies to COMPLETE HTTP modules

HOST-MODULE-ENDPOINT-001 now requires Module.Endpoints → ISender → Application

tmar-current-state.json exists and next task is this Task-ID

umbrella HostModuleEndpointOwnershipTests is multi-module, not Offer-only

This task is Payment-only.

Golden target:
Tooba.Payment.Endpoints
→ ISender
→ Tooba.Payment.Application Commands/Queries/Handlers
→ Result/SemanticError
→ Contracts/Gates/Events only
→ Infrastructure

Host:

composition/security/platform worker hosting only

no Payment HTTP/business/query/presentation ownership

Do NOT start Promotion/Offer/Inventory.

0. Direct repository findings

Payment currently has:

Tooba.Payment.Domain

Tooba.Payment.Application

Tooba.Payment.Contracts

Tooba.Payment.Infrastructure

Tooba.Payment.Tests

Missing:

Tooba.Payment.Endpoints

Current Payment.Application currently has only:

Models/

Ports/

no Commands/

no Queries/

no MediatR HTTP use-case boundary

Existing architecture test even explicitly asserts:
Assert.False(Directory.Exists(... Tooba.Payment.Endpoints))
This stale assertion must be reversed.

Payment-owned HTTP/business presentation is spread across Host:

A. Dedicated webhook endpoint

Host/Tooba.Host/Payments/PaymentWebhookEndpoints.cs

Route:

POST /v1/payments/webhooks/{providerCode}

Current anti-patterns:

direct IPaymentWebhookHandler

direct Payment.Infrastructure types

signature validator/provider options owned in Host endpoint

manual JSON semantic errors

hardcoded localized webhook details

no ISender/MediatR

B. Storefront payment HTTP routes currently inside StorefrontEndpoints.cs

Directly verified routes:

POST /v1/storefront/checkout/{checkoutId:guid}/payments

GET /v1/storefront/checkout/{checkoutId:guid}/wallet-quote

GET /v1/storefront/payment-methods

GET /v1/storefront/payments/{paymentId:guid}

GET /v1/storefront/payments/{paymentId:guid}/sandbox

POST /v1/storefront/payments/{paymentId:guid}/sandbox/complete

POST /v1/storefront/payments/{paymentId:guid}/manual-evidence

POST /v1/storefront/payments/{paymentId:guid}/manual-retry

POST /v1/storefront/payments/{paymentId:guid}/unpaid-retry

POST /v1/storefront/payments/{paymentId:guid}/proof

StorefrontEndpoints.cs also contains:

ExecutePaymentAsync

MapPaymentException

MapPaymentCustomerDetail

message/Contains based exception classification

manual localized semantic responses

C. Storefront Payment business composer

Host/Tooba.Host/Storefront/StorefrontPaymentComposer.cs

It directly depends on:

Payment Application ports

Payment Domain

Payment Infrastructure adapters/providers/directories/messaging

Wallet Application ports/models

Inventory Application

Media Application

Host OrderSupplyComposer

Host checkout/session/environment

This is not acceptable for COMPLETE_REFERENCE_PATTERN.

D. Admin Payment routes inside AdminPanelEndpoints.cs

Directly verified:

GET /v1/admin/payments/{paymentId:guid}

POST /v1/admin/payments/{paymentId:guid}/reconcile

POST /v1/admin/payments/{paymentId:guid}/confirm-deposit

POST /v1/admin/payments/{paymentId:guid}/reject-deposit

POST /v1/admin/payments/query

Current implementation directly uses IPaymentAdminDirectory and includes InvalidOperationException message matching/manual error responses.

E. Admin payment grid ownership

Host/Tooba.Host/Grid/AdminPaymentsGridQueryEngine.cs

Current problems:

Payment query ownership in Host

direct OrderDbContext

Order Application/Host composer dependencies

DateTimeOffset.UtcNow

cross-module read composition inside Host

Payment-specific grid semantics must move behind Payment Application-owned query boundary.
Order/Fulfillment enrichment must use Contracts/read gateways, not foreign DbContext.

F. Payment reconciliation hosted worker

Host/Tooba.Host/PaymentReconciliationHostedService.cs

This is process-level hosting and may legitimately remain in Host, BUT direct audit shows:

Guid.NewGuid()

DateTimeOffset.UtcNow

direct ToobaTelemetry.ActivitySource.StartActivity

direct Payment Application directory resolution

Keep Host as scheduler/lifecycle only.
Move one reconciliation iteration/use-case behind Payment Application CQRS/port as appropriate.
Use IClock, IIdGenerator, and existing tracing abstraction.
Do not implement a new polling architecture; existing worker cadence may remain because it is a legitimate scheduled reconciliation worker, not a workaround.

1. Required end state

Create:
src/backend/Modules/Payment/Tooba.Payment.Endpoints/

Payment-owned HTTP routes move into module Endpoints.

Required flow:
Payment.Endpoints
→ ISender
→ Payment.Application
→ Result
→ ports/contracts
→ Infrastructure

Host:

maps MapPaymentEndpoints()

security/session adapters only

hosted service lifecycle/composition only

no Payment HTTP semantic mapping

no Payment business composer

2. Create Endpoints project

Create Tooba.Payment.Endpoints.
Add to src/backend/Tooba.slnx.

Allowed refs:

Payment.Application

Payment.Contracts if needed

BuildingBlocks presentation/security

Microsoft.AspNetCore.App as needed

Forbidden:

Host

Payment.Infrastructure

foreign Application/Infrastructure

DbContexts

Folders based on real surfaces:

Storefront/

Admin/

Webhooks/

Errors/Resources if needed

No giant endpoint root dump.

3. Move dedicated webhook endpoint

Move:
Host/Payments/PaymentWebhookEndpoints.cs
to module Endpoints.

Preserve exact route:
POST /v1/payments/webhooks/{providerCode}

Endpoint may own transport-only concerns:

buffering body

reading raw body/header

deserializing wire payload

But signature/business processing must not require Endpoints → Infrastructure.

Create appropriate Application command, e.g.:
ProcessPaymentWebhookCommand

Application/ports own:

signature verification abstraction/config-neutral contract as appropriate

payload semantic validation

webhook processing

Result mapping

Do NOT leak PaymentGatewayOptions/Infrastructure validator into Endpoints.

Preserve exact accepted/duplicate success JSON and existing status semantics:

unauthorized signature

invalid payload

missing payment

amount/attempt/provider mismatch

general rejected

Use centralized Result/SemanticError/ApiResponseFactory for expected failures.

No hardcoded localized detail in endpoint.

Delete Host PaymentWebhookEndpoints.

4. Move storefront Payment routes

Move Payment-owned route implementations out of Host/Storefront/StorefrontEndpoints.cs into Payment.Endpoints.Storefront.

Preserve exact public URLs above.

Do NOT move unrelated storefront routes.

Remove from Host:

Payment route mappings

ExecutePaymentAsync

MapPaymentException

MapPaymentCustomerDetail

payment-specific wire DTOs/methods

Host StorefrontEndpoints must no longer own Payment business HTTP behavior.

5. Remove StorefrontPaymentComposer business authority

Target:
delete Host/Storefront/StorefrontPaymentComposer.cs.

Its Payment business responsibilities become Application Commands/Queries.

Expected storefront use cases include at least:
Queries:

GetStorefrontWalletQuote

ListStorefrontPaymentMethods

GetStorefrontPayment

GetStorefrontPaymentSandboxContext

Commands:

InitiateStorefrontPayment

CompleteSandboxPayment

SubmitManualPaymentEvidence

RetryManualPayment

RetryUnpaidPayment

UploadManualPaymentProof

Use exact names consistent with existing behavior if better.

Critical:
Do NOT move Host/foreign dependencies wholesale into Payment.Application.

Replace with declared ports/contracts:

checkout/order read/access gate

wallet quote/pay gate via Wallet.Contracts

inventory/supply retry gate via Contracts

media proof storage/read gate via appropriate Contracts/port

environment/payment-method configuration port

No Payment.Application → Wallet.Application
No Payment.Application → Inventory.Application
No Payment.Application → Media.Application
No Payment.Application → Host
No Payment.Application → foreign Infrastructure.

This is the microservice-readiness requirement.

6. Checkout freeze compatibility

Checkout remains:
PAUSED_AT_SAFE_W5_CHECKPOINT

Do NOT redesign checkout/process manager.

Payment extraction may introduce or reuse narrow Contracts/Gates needed by Payment, but:

no checkout workflow semantic changes

no transaction boundary redesign

no W6 work

no route contract changes

no order state-machine redesign

If a required cross-module seam cannot be introduced without opening Checkout semantics, return INCOMPLETE with exact blocker instead of hacking around it.

7. Customer/storefront actor boundary

Payment.Endpoints must not depend on Host session types.

Create/reuse minimal payment storefront authorizer/access resolver.

It must preserve:

authenticated actor behavior

guest checkout behavior

guest secret/cart ownership behavior

development/testing seams where currently supported

Host adapter is allowed only for security/session context.
No Payment business logic in Host adapter.

8. Admin Payment routes

Move Payment-owned admin routes from AdminPanelEndpoints.cs into Payment.Endpoints.Admin.

Preserve exact:

GET /v1/admin/payments/{paymentId:guid}

POST /v1/admin/payments/{paymentId:guid}/reconcile

POST /v1/admin/payments/{paymentId:guid}/confirm-deposit

POST /v1/admin/payments/{paymentId:guid}/reject-deposit

POST /v1/admin/payments/query

Host AdminPanel must no longer implement these routes.

Create real MediatR use cases:

GetAdminPayment

ReconcileAdminPayment

ConfirmAdminDeposit

RejectAdminDeposit

QueryAdminPaymentsGrid

Admin authorization via small IPaymentAdminAuthorizer adapter.
Do not reference Host from module.

9. Admin payments grid

Remove Payment-specific query engine ownership from Host:
Host/Grid/AdminPaymentsGridQueryEngine.cs

Target:

Payment.Application owns query policy/orchestration

Payment.Infrastructure owns Payment persistence query implementation

foreign enrichments through Contracts/read gates

no OrderDbContext in Payment module

no Host composer dependencies

no DateTimeOffset.UtcNow; use IClock

Preserve exact filters/sorts/search/paging and response shape, including supply/reservation display fields.

If the final DTO is truly Admin UI projection spanning several modules:
Payment Application may orchestrate through declared read contracts.
Do NOT use foreign DbContext or foreign Application.

Delete Host AdminPaymentsGridQueryEngine if no longer used.

10. Real CQRS / MediatR 12.5

Payment Application must gain real:

Commands/<UseCase>/

Queries/<UseCase>/

Handlers

Every Payment-owned HTTP business route:
Endpoint → ISender → exactly one Command/Query.

No direct directory calls from Endpoints.

Register Payment.Application assembly in CQRS foundation.

No ceremonial handlers.
No giant PaymentHandlers.cs.

11. Result/error semantics

Replace all Payment HTTP message-based mappings with:

Result / Result<T>

SemanticError

exact stable machine codes

centralized ApiResponseFactory/localization

Explicitly remove:

MapPaymentException

text Contains(...)

Persian/English exception prose heuristics

manual semantic JSON

ex.Message

generic InvalidOperationException fallback to payment.rejected

Unknown exceptions propagate.

Audit Payment Domain/Application/Infrastructure codes and map exact codes only.

Preserve externally visible codes/status as much as existing stable contract requires:

payment.already_succeeded / existing alias compatibility if shipped

payment.missing

payment.access.denied

payment.guest.invalid

payment.wallet.mixed_deferred

payment.method.unavailable

payment.tracking.required

payment.proof.required

payment.proof.foreign

payment.sandbox.unavailable

payment.unpaid.supply_unavailable

payment.unpaid.retry.invalid

webhook codes

admin payment codes

Do not preserve prose heuristics; preserve semantic behavior.

12. Payment proof/media boundary

Proof upload route currently belongs to payment flow.

Payment Application must not reference Media.Application.
Use existing Media.Contracts if available or introduce smallest stable port in Payment.Application/Contracts implemented by Host-neutral infrastructure adapter.

No Host business callback.

13. Wallet boundary

Payment already has Wallet.Contracts integration in Infrastructure.
Preserve and extend Contracts-only if storefront wallet quote/pay path needs it.

No Wallet.Application in Payment Application/Endpoints/Infrastructure target state.

14. Reconciliation worker

PaymentReconciliationHostedService may remain in Host only as host lifecycle/scheduler.

Refactor target:
Host worker:

gets tenant targets

establishes neutral commerce context

dispatches one ReconcileStalePaymentsCommand via ISender (or a Payment application service boundary if MediatR scheduling compatibility requires)

waits according to configured interval

logs process-level outcome

Payment Application/Infrastructure:

owns reconciliation business use-case

uses IClock

uses IIdGenerator where IDs/correlation IDs needed

uses IModuleCallTracer / MediatR tracing, no raw StartActivity in business path

Remove from Host worker:

direct IPaymentReconciliationDirectory

DateTimeOffset.UtcNow

Guid.NewGuid()

raw ToobaTelemetry.ActivitySource.StartActivity

Existing worker loop is legitimate scheduled background processing and may remain.
No magic new sleeps/retries.

15. Infrastructure dependency cleanup

Payment.Endpoints must not reference Infrastructure.

Payment.Application must not reference foreign Application/Infrastructure.

Payment.Infrastructure may implement Payment ports and depend on external Contracts only.

Keep current clean Wallet.Contracts boundary.

Audit direct references to:

Payment.Infrastructure from Host storefront/admin/webhook route implementations

Payment.Domain from Host payment composer
and remove those business-path leaks.

16. Physical folder standard

Must match Cart/Settlement/Fulfillment/Returns/Notification/Support/Wallet.

Projects:

Tooba.Payment.Domain

Tooba.Payment.Application

Tooba.Payment.Contracts

Tooba.Payment.Infrastructure

Tooba.Payment.Endpoints

Tooba.Payment.Tests

Application:

Commands/<UseCase>/

Queries/<UseCase>/

Models/

Ports/

Errors/

Endpoints:

Storefront/

Admin/

Webhooks/

Errors/Resources as needed

Infrastructure:

Persistence/

Directories/

Adapters/

Providers/

Events/

Messaging/

Gateways/

DependencyInjection/

Migrations/

No root dump.
Path↔namespace aligned.
No TypeForwardedTo.
No namespace masquerading.

17. Architecture guards

Upgrade PaymentArchitectureGuardTests.

Remove stale assertion:
Assert.False(Directory.Exists(Tooba.Payment.Endpoints))

Replace with required Endpoints existence and ownership.

Must enforce:

Endpoints project exists

Endpoints → Application

no Endpoints → Host/Infrastructure

no foreign Application/Infrastructure

no DbContext

no direct business directories in Endpoints

ISender used for Payment business routes

Application has real MediatR handlers

use-case folders present

no message/prose heuristics

no ex.Message semantic mapping

no direct Guid.NewGuid/DateTimeOffset.UtcNow in protected Payment orchestration

no raw StartActivity

Wallet Contracts-only

no Host PaymentWebhookEndpoints

no Host StorefrontPaymentComposer

no Host AdminPaymentsGridQueryEngine

Host StorefrontEndpoints does not map Payment-owned routes

Host AdminPanelEndpoints does not map Payment-owned routes

Payment reconciliation Host worker is scheduler-only

Host PaymentDbContext authority remains only explicit bootstrap/migration allowlist

Also add Payment to durable HostModuleEndpointOwnershipTests COMPLETE manifest only after module passes.

18. Behavior preservation

Preserve exact behavior for:

payment initiation

wallet-full-payment path

payment methods availability

manual card-to-card

payment retrieval/ownership

sandbox flow

manual evidence

proof upload

retry manual/unpaid payment

webhook signature/dedup

admin get/reconcile/confirm/reject

admin payment grid

reconciliation worker

payment expiry interactions

outbox/events

Wallet payment/refund contracts

Order projection effects

idempotency

provider references/status transitions

Accidental behavior change = 0.

Financial correctness is critical.
Do not simplify payment state transitions to make tests pass.

19. Focused validation only

Required:

Payment.Tests

Payment architecture guards

storefront Payment route characterization/focused tests

webhook signature/payload/dedup

admin payment operations

admin grid behavior

reconciliation one-cycle behavior

Result/error semantics

unexpected exception propagation

HostModuleEndpointOwnershipTests

TmarDurableGuardTests

final dotnet build src/backend/Tooba.slnx

Do NOT run:

broad Host suite

Checkout workflow suite

Tax/Pricing

frontend

Promotion/Offer/Inventory tests

20. Recovery SoT update is part of DoD

ARCH-COMPLETE-001 requires recovery state update in the same accepted task cycle.

If Payment reaches COMPLETE in this task, update:

docs/architecture/tmar-current-state.json

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

this task's recovery-sot.md

Current state after PASS must become:

Payment: COMPLETE_REFERENCE_PATTERN

nextTask: TB-TMAR-PROMOTION-GOLDEN-001

Promotion: REOPENED_ENDPOINT_CQRS_OWNERSHIP

Offer: NEEDS_FINAL_REVERIFY

Inventory: NEEDS_APPLICABILITY_REVERIFY

lastAcceptedTask = TB-TMAR-PAYMENT-GOLDEN-001
lastAcceptedCommit must be final implementation tip commit (if docs-tip commit follows, record implementation + final tip clearly and keep machine state unambiguous).

Do not leave stale Payment REOPENED state after PASS.

21. Evidence

Create:
docs/evidence/TB-TMAR-PAYMENT-GOLDEN-001/

Required:

recovery-start.md

payment-http-ownership-audit.md

payment-cqrs-audit.md

payment-storefront-boundary.md

payment-admin-boundary.md

payment-webhook-boundary.md

payment-grid-ownership-audit.md

payment-reconciliation-worker-audit.md

payment-error-semantics-audit.md

payment-cross-module-contract-audit.md

payment-physical-tree.md

behavior-preservation-audit.md

architecture-guard-audit.md

recovery-state-sync.md

recovery-sot.md

Physical tree lists every handwritten Payment production .cs:
path | namespace | responsibility

22. Protected state

Must remain:
Cart/Settlement/Fulfillment/Returns/Notification/Support/Wallet = COMPLETE_REFERENCE_PATTERN
Checkout = PAUSED_AT_SAFE_W5_CHECKPOINT
Tax/Pricing = untouched in this wave
Frontend = frozen

Do not start Promotion/Offer/Inventory.

No reset.
No clean.
No force push.
No broad git add.
Stashes/user files untouched.

23. Completion scan

Before PASS scan entire repo for:

PaymentWebhookEndpoints

StorefrontPaymentComposer

AdminPaymentsGridQueryEngine

IPaymentDirectory in Host HTTP

IPaymentAdminDirectory in Host HTTP

IPaymentQueryDirectory in Host Payment UI path

Payment Domain/Infrastructure references in Host Payment HTTP path

/payments/

/payment-methods

payment MapPaymentException

MapPaymentCustomerDetail

Payment message Contains/StartsWith heuristics

Payment manual semantic JSON

Payment ex.Message

Guid.NewGuid / DateTimeOffset.UtcNow / raw StartActivity in Payment protected flow

foreign Application/Infrastructure refs from Payment

Any unresolved production business ownership leak => INCOMPLETE with exact path.

24. Success criteria

PASS only if ALL:

Payment-HTTP-Ownership: MODULE_ENDPOINTS
Payment-Endpoints-State: REAL_PROJECT_PRESENT
Payment-CQRS-State: MEDIATR_12_5_APPLICATION_HANDLERS
Payment-Storefront-Routes: MODULE_ENDPOINTS
Payment-Admin-Routes: MODULE_ENDPOINTS
Payment-Webhook-Route: MODULE_ENDPOINTS
Payment-Host-Webhook: REMOVED
Payment-Host-StorefrontComposer: REMOVED
Payment-Host-AdminGridEngine: REMOVED
Payment-Host-Business-Authority: NONE
Payment-Host-DbAuthority: NONE_EXCEPT_BOOTSTRAP_ALLOWLIST
Payment-Reconciliation-Host-Role: SCHEDULER_ONLY
Payment-CrossModule-Boundary: CONTRACTS_ONLY
Payment-Result-Adoption: HTTP_USE_CASES_ADOPTED
Payment-Error-Classification: STABLE_CODES_ONLY
Payment-Prose-Mapping: NONE
Payment-Unexpected-Exception-Swallow: NONE
Payment-Physical-State: VERIFIED_ON_DISK_AND_NAMESPACE
Payment-Architecture-Guards: ENFORCED
Payment-Behavior-Preservation: VERIFIED
Recovery-State: CURRENT_AND_MACHINE_READABLE
Recovery-Next-Task: TB-TMAR-PROMOTION-GOLDEN-001
Payment-State: COMPLETE_REFERENCE_PATTERN

25. Canonical Result

Return ONLY BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Track
Recovery-Start
Payment-HTTP-Ownership-Audit
Payment-Endpoints-State
Payment-CQRS-State
Payment-Application-UseCases
Payment-Storefront-Boundary
Payment-Admin-Boundary
Payment-Webhook-Boundary
Payment-Grid-Ownership
Payment-Reconciliation-Host-Role
Payment-Result-Adoption
Payment-Error-Classification
Payment-Prose-Mapping
Payment-Unexpected-Exception-Swallow
Payment-Host-Authority-Audit
Payment-Host-Webhook
Payment-Host-StorefrontComposer
Payment-Host-AdminGridEngine
Payment-Host-Business-Authority
Payment-Host-DbAuthority
Payment-CrossModule-Boundary
Payment-Physical-State
Payment-Architecture-Guards
Payment-Behavior-Preservation
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
Next-Recommended-Task: TB-TMAR-PAYMENT-GOLDEN-001-R1

If complete:
Next-Recommended-Task: TB-TMAR-PROMOTION-GOLDEN-001

After Result:
STOP completely.
Do NOT start Promotion.
Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_TASK