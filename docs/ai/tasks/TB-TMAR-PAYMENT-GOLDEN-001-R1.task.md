PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-PAYMENT-GOLDEN-001-R1
Parent-Task: TB-TMAR-PAYMENT-GOLDEN-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: FAST-SAFE
Track: PAYMENT_GOLDEN_CLOSURE_R1
Title: Payment Safe Boundary Foundation + Webhook/Admin CQRS + Worker Cleanup
Backend-Only: YES

Architect verification of parent INCOMPLETE

Architect directly inspected current main after the parent Result.

Confirmed:

Tooba.Payment.Endpoints is NOT present in committed main.

PaymentErrorCodes.cs and PaymentExceptionMapper.cs exist.

Host/Payments/PaymentWebhookEndpoints.cs still exists.

Host/Storefront/StorefrontPaymentComposer.cs still exists.

Host/Grid/AdminPaymentsGridQueryEngine.cs still exists.

PaymentReconciliationHostedService still uses Guid.NewGuid, DateTimeOffset.UtcNow, raw StartActivity, and direct IPaymentReconciliationDirectory.

Payment Application still has no real CQRS surface.

tmar-current-state.json still points to parent TB-TMAR-PAYMENT-GOLDEN-001; this must be updated now to R1.

Parent INCOMPLETE is accepted as truthful.
No Payment COMPLETE claim is allowed.

This R1 intentionally stages the financial-critical recovery safely.
Do NOT attempt a risky all-at-once storefront rewrite.

1. Recovery SoT must be current at task start

Immediately update:

docs/architecture/tmar-current-state.json

MASTER recovery current-state section

ARCHITECT bootstrap current-state section

Set:

Payment state = IN_PROGRESS_GOLDEN_R1

nextTask = TB-TMAR-PAYMENT-GOLDEN-001-R1

last accepted COMPLETE remains Wallet

parent incomplete task recorded as latest attempted task/evidence, not accepted COMPLETE

This is required because recovery must remain bootstrap-safe even during an incomplete multi-step repair.

If R1 finishes as planned but Payment is still not COMPLETE:

nextTask = TB-TMAR-PAYMENT-GOLDEN-001-R2

Payment state = IN_PROGRESS_GOLDEN_R2_READY

2. Scope of R1

R1 MUST finish these bounded slices:

A) real Payment.Endpoints project + solution/Host wiring
B) webhook route migration
C) admin payment detail/action routes migration
D) Payment reconciliation worker → scheduler-only Host
E) Payment Application real MediatR CQRS foundation
F) cross-module storefront dependency contract plan + concrete ports/gates needed for R2
G) architecture guards for everything completed in R1
H) recovery SoT sync

R1 does NOT need to move storefront payment routes or Admin payment grid yet if doing so would risk Checkout/payment semantics.

Those remaining surfaces become R2 only.

3. Create real Payment.Endpoints project

Create:
src/backend/Modules/Payment/Tooba.Payment.Endpoints/

Add to:
src/backend/Tooba.slnx

Wire Host project reference as needed.

Folders:

Webhooks/

Admin/

Storefront/ only if real R1 code exists

Errors/ only if real presentation code exists

Allowed refs:

Payment.Application

Payment.Contracts

BuildingBlocks

Forbidden:

Host

Payment.Infrastructure

foreign Application/Infrastructure

DbContext

Add MapPaymentEndpoints() module entry point.

Program must call MapPaymentEndpoints().

Do not leave empty ceremonial folders.

4. Webhook migration MUST complete in R1

Move:
Host/Payments/PaymentWebhookEndpoints.cs

into module Endpoints.

Delete Host file.

Preserve route exactly:
POST /v1/payments/webhooks/{providerCode}

Create real MediatR use case:
Commands/ProcessPaymentWebhook/

Endpoint responsibilities only:

read raw body

read signature header

transport deserialization if truly wire-only

ISender.Send(...)

map Result via ApiResponseFactory

Application owns:

semantic payload validation

signature validation abstraction

provider/event processing orchestration

accepted/duplicate result

Infrastructure implements:

actual signature/provider processing ports

Endpoint MUST NOT reference:

PaymentGatewayOptions

PaymentWebhookSignatureValidator

IPaymentWebhookHandler implementation types

Payment.Infrastructure

Stable expected errors only:

signature missing/invalid

invalid payload

payment.missing

webhook amount/attempt/provider mismatch

other documented stable rejection codes

Unknown exceptions propagate.

5. Admin payment detail/actions MUST complete in R1

Move these from AdminPanelEndpoints.cs into Payment.Endpoints.Admin:

GET /v1/admin/payments/{paymentId:guid}

POST /v1/admin/payments/{paymentId:guid}/reconcile

POST /v1/admin/payments/{paymentId:guid}/confirm-deposit

POST /v1/admin/payments/{paymentId:guid}/reject-deposit

Do NOT move /v1/admin/payments/query in R1 unless grid extraction is already safely complete.

Create real use-case folders:
Queries/GetAdminPayment/
Commands/ReconcileAdminPayment/
Commands/ConfirmAdminDeposit/
Commands/RejectAdminDeposit/

Use:
Endpoint → ISender → Application handler → Payment port

Add minimal IPaymentAdminAuthorizer in Endpoints and Host security adapter.
Host adapter may only perform admin auth/capability resolution.
No Payment directory/business logic in Host adapter.

Remove direct IPaymentAdminDirectory usage for these routes from Host HTTP code.

Preserve exact route/status/success contracts.

No message heuristic catches.

6. Real Payment CQRS foundation

Payment.Application must reference existing MediatR 12.5 foundation correctly.

Register Payment.Application assembly in AddToobaCqrsFoundation(...).

At minimum R1 has real handlers for:

ProcessPaymentWebhook

GetAdminPayment

ReconcileAdminPayment

ConfirmAdminDeposit

RejectAdminDeposit

ReconcileStalePayments (worker use case; see below)

Folder pattern:
Commands/<UseCase>/
Queries/<UseCase>/

No giant handler file.
No fake request with logic still in Host.

7. Reconciliation worker MUST become scheduler-only in R1

Current Host worker directly performs Payment use-case and violates TMAR.

Required end state for R1:

Host PaymentReconciliationHostedService may:

own BackgroundService lifecycle

enumerate tenant targets

create scope / assign commerce context

delay according to existing configured cadence

resolve ISender

dispatch ReconcileStalePaymentsCommand

record process-level success/failure logs/registry

Host worker must NOT:

resolve IPaymentReconciliationDirectory

call Payment directory directly

use DateTimeOffset.UtcNow

use Guid.NewGuid()

create raw Payment ActivitySource span

Application handler:

receives age/batch/tenant-compatible inputs

uses IClock

owns reconciliation use-case

delegates to Payment port

relies on normal MediatR tracing

For commerce-context trace/correlation identifier:
use IIdGenerator or existing approved correlation abstraction.

Do not redesign polling cadence.
This worker is legitimate scheduled processing, not the polling anti-pattern prohibited for UI/workaround code.

8. Error semantics in R1

Use existing new:

PaymentErrorCodes

PaymentExceptionMapper

But audit them.

Rules:

exact stable machine-code match only

no Contains, StartsWith, localized prose detection

no broad unknown InvalidOperationException conversion

unknown exceptions propagate

no ex.Message returned to HTTP

If any stable code names in scaffold are wrong aliases versus actual lower-layer codes, correct them now.

Do NOT create random encoded/opaque aliases to force tests.

Expected failures → Result/SemanticError.

9. Storefront boundary preparation for R2

R1 must NOT leave R2 guessing.

Create:
docs/evidence/TB-TMAR-PAYMENT-GOLDEN-001-R1/payment-storefront-contract-plan.md

Directly inventory every dependency currently used by:
Host/Storefront/StorefrontPaymentComposer.cs

Classify each responsibility:

Payment-owned

Checkout/Order-owned

Wallet-owned

Inventory/Supply-owned

Media-owned

Host security/session-only

provider configuration

For each foreign responsibility define the exact target boundary:

existing Contracts interface to reuse, OR

smallest new Contracts/Gate required

Important:

Wallet: prefer existing Wallet.Contracts; do not use Wallet.Application.

Inventory: prefer existing Inventory.Contracts; do not use Inventory.Application.

Order/Checkout: reuse Order.Contracts/Cart.Contracts where sufficient; if missing, define the minimal contract addition without reopening Checkout W6 semantics.

Media currently has no Contracts project in repo. Do NOT make Payment depend on Media.Application. If proof storage requires a stable cross-module boundary, create the smallest justified Tooba.Media.Contracts project/port OR a neutral shared asset contract only if ownership is clearly Media. This is permitted as boundary extraction, not a Media feature rewrite.

Do not migrate storefront routes in R1 unless all required boundaries are already clean and tests are straightforward.

10. Admin grid preparation

Host/Grid/AdminPaymentsGridQueryEngine.cs remains a known R2 target unless safely migrated now.

R1 must create:
payment-admin-grid-contract-plan.md

Document:

Payment-owned grid data

Order/customer enrichment

supply/reservation enrichment

exact foreign dependencies

target Contracts/read gates

removal plan for OrderDbContext

replacement of DateTimeOffset.UtcNow with IClock

Do NOT create a fake Payment Application query that still delegates back to Host engine.

11. Host state after R1

Must be true:

REMOVED:

Host/Payments/PaymentWebhookEndpoints.cs

Payment admin detail/action implementations from AdminPanelEndpoints

direct reconciliation directory usage in hosted worker

MAY REMAIN for R2:

StorefrontPaymentComposer.cs

storefront payment route methods in StorefrontEndpoints.cs

AdminPaymentsGridQueryEngine.cs

/v1/admin/payments/query

These remaining paths must be explicitly marked R2_REMAINDER, not COMPLETE.

Host must map:
MapPaymentEndpoints()

12. Architecture guards

Upgrade PaymentArchitectureGuardTests for R1:

Must now assert:

Payment.Endpoints exists

Payment.Endpoints ref Application

no Endpoints → Host/Infrastructure

webhook Host endpoint absent

admin payment detail/action Host mappings absent

R1 endpoints use ISender

real MediatR handlers exist

reconciliation Host worker has no IPaymentReconciliationDirectory

reconciliation Host worker has no DateTimeOffset.UtcNow

reconciliation Host worker has no Guid.NewGuid

reconciliation Host worker has no StartActivity

exact-code mapper only

Wallet boundary Contracts-only remains

Do NOT add Payment to COMPLETE manifest in HostModuleEndpointOwnershipTests yet.
Payment is not COMPLETE until R2 closure.

Instead add a clearly named in-progress guard if useful, without falsely marking COMPLETE.

13. Focused tests

Run only:

Payment.Tests focused R1

webhook tests

admin detail/action tests

reconciliation worker/handler tests

Payment architecture guards

TmarDurableGuardTests

final dotnet build src/backend/Tooba.slnx

Do NOT run broad Host suite.
Do NOT run Checkout workflow suite.
No frontend.

14. Evidence

Create:
docs/evidence/TB-TMAR-PAYMENT-GOLDEN-001-R1/

Required:

recovery-start.md

payment-r1-repo-audit.md

payment-webhook-migration.md

payment-admin-route-migration.md

payment-reconciliation-migration.md

payment-cqrs-foundation.md

payment-storefront-contract-plan.md

payment-admin-grid-contract-plan.md

payment-error-semantics-audit.md

architecture-guard-audit.md

recovery-state-sync.md

recovery-sot.md

15. Protected state

Cart/Settlement/Fulfillment/Returns/Notification/Support/Wallet remain COMPLETE.
Checkout remains PAUSED_AT_SAFE_W5_CHECKPOINT.
Tax/Pricing untouched.
Frontend frozen.
Do not start Promotion/Offer/Inventory.

No reset/clean/force push/broad git add.
Stashes untouched.

16. R1 success criteria

R1 PASS only if ALL:

Payment-R1-Endpoints-Project: REAL_AND_WIRED
Payment-R1-Webhook: MODULE_ENDPOINTS_CQRS
Payment-R1-Admin-DetailActions: MODULE_ENDPOINTS_CQRS
Payment-R1-Reconciliation-Host: SCHEDULER_ONLY
Payment-R1-CQRS: MEDIATR_12_5_REAL
Payment-R1-Error-Classification: STABLE_CODES_ONLY
Payment-R1-Storefront-Plan: CONTRACTS_TARGET_EXPLICIT
Payment-R1-AdminGrid-Plan: CONTRACTS_TARGET_EXPLICIT
Payment-R1-Architecture-Guards: ENFORCED
Payment-State: IN_PROGRESS_GOLDEN_R2_READY
Recovery-Next-Task: TB-TMAR-PAYMENT-GOLDEN-001-R2

Payment MUST NOT be reported COMPLETE in R1 unless storefront + admin grid also happen to be fully and safely closed and all parent criteria are met.

17. Canonical Result

Return ONLY BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Track
Recovery-Start
Payment-R1-Endpoints-Project
Payment-R1-Webhook
Payment-R1-Admin-DetailActions
Payment-R1-Reconciliation-Host
Payment-R1-CQRS
Payment-R1-Error-Classification
Payment-R1-Storefront-Plan
Payment-R1-AdminGrid-Plan
Payment-R1-Architecture-Guards
Focused-Validation
Full-Validation
Residual-Defects
Payment-State
Recovery-State
Recovery-Next-Task
Current-State-Manifest
Wallet-State
Support-State
Notification-State
Returns-State
Fulfillment-State
Settlement-State
Cart-State
Checkout-State
Tax-State
Pricing-State
Frontend-Production-Changes
Git
Blockers
User-Work-Preserved
Next-Recommended-Task

Normal R1 PASS:
Next-Recommended-Task: TB-TMAR-PAYMENT-GOLDEN-001-R2

If R1 incomplete:
Next-Recommended-Task: TB-TMAR-PAYMENT-GOLDEN-001-R1-REPAIR

After Result:
STOP completely.
Do NOT start R2.
Do NOT poll.
Do NOT write Worker IDLE.

END_TOOBA_TASK