PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-FULFILLMENT-GOLDEN-001

Parent-Task:
TB-TMAR-SETTLEMENT-GOLDEN-001

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
FULFILLMENT_GOLDEN_CLOSURE

Title:
Fulfillment Endpoints Ownership + CQRS Foldering + Host Cleanup Golden Closure

Backend-Only:
YES

Architect decision

Settlement is accepted as COMPLETE_REFERENCE_PATTERN after direct repo verification.

This task is Fulfillment-only.

Golden pattern:
Tooba.Fulfillment.Endpoints
→ ISender
→ Tooba.Fulfillment.Application Commands/Queries/Handlers
→ Result/SemanticError
→ Contracts-only foreign boundaries
→ Infrastructure

Host:

composition only

global auth/session/tenant plumbing only

calls MapFulfillmentEndpoints()

no Fulfillment HTTP ownership

no Fulfillment composer/business/query authority

Do NOT start Returns/Notification/etc.

0. Direct repository findings

Fulfillment currently has:

Tooba.Fulfillment.Domain

Tooba.Fulfillment.Application

Tooba.Fulfillment.Contracts

Tooba.Fulfillment.Infrastructure

Tooba.Fulfillment.Tests

Missing:

Tooba.Fulfillment.Endpoints

Current Fulfillment-owned HTTP surfaces are split across Host:

Host/Tooba.Host/Fulfillment/FulfillmentEndpoints.cs

Host/Tooba.Host/Admin/ShippingServiceEndpoints.cs

Fulfillment shipping-method route inside:
Host/Tooba.Host/Admin/AdminOrderOperationsEndpoints.cs
specifically:
GET /v1/admin/shipping-methods

Current Host also still has:
Host/Tooba.Host/Fulfillment/FulfillmentPanelComposer.cs

Application already has MediatR/use-case code, but foldering is still partially coarse:

Commands/ExecuteAdminFulfillmentBulkCommand.cs

Commands/SellerMutateFulfillmentCommand.cs

Queries/FulfillmentQueries.cs

Shipping/ShippingService*Handlers.cs

Shipping/ListEnabledShippingMethodsTreeQuery.cs

This must be normalized to Cart/Settlement/Order quality: real use-case folders, no giant multi-use-case files.

1. Required end state

Create:
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Endpoints/

All Fulfillment-owned HTTP routes move there.

Host should retain only:

endpoint registration/composition

neutral auth adapters where unavoidable

app.MapFulfillmentEndpoints()

No production Fulfillment endpoints/composer in Host.

2. Create Endpoints project

Create Tooba.Fulfillment.Endpoints and add to src/backend/Tooba.slnx.

Allowed refs:

Fulfillment.Application

BuildingBlocks presentation/security primitives

Fulfillment.Contracts only if needed for wire contracts

Forbidden refs:

Host

Fulfillment.Infrastructure

foreign Application/Infrastructure

any DbContext

Namespace root:
Tooba.Fulfillment.Endpoints

Physical folders based on actual ownership, likely:

Seller/

Admin/

Shipping/

Errors/Resources only if needed

No giant root file except tiny endpoint module.

3. Move ALL Fulfillment HTTP ownership from Host

Move routes from:
Host/Fulfillment/FulfillmentEndpoints.cs

Move shipping service CRUD routes from:
Host/Admin/ShippingServiceEndpoints.cs

Move ONLY the Fulfillment-owned route:
GET /v1/admin/shipping-methods
out of AdminOrderOperationsEndpoints.cs.

Do NOT move unrelated Order routes from AdminOrderOperationsEndpoints.

Preserve exact URLs/verbs and response success shape.

Delete:

Host/Fulfillment/FulfillmentEndpoints.cs

Host/Admin/ShippingServiceEndpoints.cs

Remove only the shipping-method mapping + method from AdminOrderOperationsEndpoints.

Host Program should map module-owned Fulfillment endpoints once:
app.MapFulfillmentEndpoints();

Do not double-map routes.

4. FulfillmentPanelComposer

Audit:
Host/Fulfillment/FulfillmentPanelComposer.cs

Target:
DELETE it.

Its responsibilities must move behind Fulfillment Application query/command boundaries.

If it is only transport composition:
move equivalent logic to Application query handlers.

If it calls foreign data:
use existing Contracts only.

No replacement Host wrapper/composer.

No Host service locator.

5. CQRS / MediatR normalization

MediatR must remain version 12.5.0 via existing foundation.

Every public Fulfillment HTTP use case must go:
Endpoint → ISender → Command/Query → Handler.

Normalize Application physical structure.

Examples:

Commands/

ExecuteAdminFulfillmentBulk/

SellerMutateFulfillment/

CreateShippingService/

UpdateShippingService/

DeactivateShippingService/

EnsureShippingCatalogSeed/

Queries/

seller/admin fulfillment reads as applicable

ListShippingServices/

GetShippingService/

ListEnabledShippingMethodsTree/

Do NOT keep multi-use-case production files such as:

Commands/ExecuteAdminFulfillmentBulkCommand.cs at Commands root

Commands/SellerMutateFulfillmentCommand.cs at Commands root

Queries/FulfillmentQueries.cs

generic Shipping handler dump files

Split request + handler by use-case folder, like Cart/Settlement/Order pattern.

No ceremonial CQRS; preserve existing real behavior.

6. Shipping service wire DTO ownership

Current wire request records live inside Host ShippingServiceEndpoints.cs.

Move wire-only DTOs into:
Tooba.Fulfillment.Endpoints.Shipping (or Admin/Shipping)

Application models remain transport-neutral.

Do not leak ASP.NET types into Application.

7. Authorization boundary

Current Host endpoints likely rely on:

AdminPanelAccess

SellerPanelAccess

CurrentAuthenticatedSession

IAuthorizationGuard

ICurrentTenant

environment

Fulfillment.Endpoints MUST NOT reference Host.

Use shared neutral authorization identity seam if available.

If needed create minimal endpoint authorizer interfaces:

IFulfillmentSellerAuthorizer

IFulfillmentAdminAuthorizer

Host implementations are acceptable only as transport/security adapters:

no Fulfillment business rules

no DbContext

no application query orchestration

no manual error mapping

Prefer reusable generic auth seam if already present; do not over-generalize now.

8. Result / error semantics

Every expected Fulfillment/Shipping failure:

Result / Result<T>

SemanticError

ApiResponseFactory

centralized error catalog/localization

Audit all Fulfillment Application exception mapping.

Forbidden:

message Contains

StartsWith-based broad classification

Persian/English prose heuristics

unknown InvalidOperationException → generic business failure

ex.Message leaking to HTTP

PlatformHttpException for Fulfillment business outcome

manual semantic JSON envelopes

Unknown exceptions propagate.

For success responses currently returning { ok = true }, preserve success contract if clients depend on it; use Result success payload/model rather than manual semantic failure mapping.

9. Foreign module boundaries

Fulfillment currently references Order.Contracts, Inventory.Contracts, Payment.Contracts and Localization.Contracts.

Keep foreign references Contracts-only.

No:

Order.Application/Domain/Infrastructure

Inventory.Application/Domain/Infrastructure

Payment.Application/Domain/Infrastructure

Localization.Application/Infrastructure

foreign DbContexts

Order-owned implementation of IAdminOrderFulfillmentOperations remains in Order.Infrastructure; do not move it back to Host.

ShippingServiceLanguageGate stays Fulfillment.Infrastructure using Localization.Contracts.

Do not reopen Order architecture except compile-only route removal for /v1/admin/shipping-methods.

10. Physical folder standard

Must match Cart/Settlement quality.

Required projects:

Tooba.Fulfillment.Domain

Tooba.Fulfillment.Application

Tooba.Fulfillment.Contracts

Tooba.Fulfillment.Infrastructure

Tooba.Fulfillment.Endpoints

Tooba.Fulfillment.Tests

Application:

Commands/<UseCase>/

Queries/<UseCase>/

Models/

Ports/

Shipping/ only for cohesive shipping domain/application abstractions, not handler dumping

Endpoints:

Seller/

Admin/

Shipping/

Errors/Resources as needed

Infrastructure:

Persistence/

Directories/

Adapters/

Shipping/

Queries/

Gateways/

Bridges/

Events/

Messaging/

DependencyInjection/

Observability/
as applicable

Every handwritten .cs:
path ↔ namespace aligned.
No root dump.
No TypeForwardedTo.
No namespace masquerading.

11. Host cleanup target

After task:

Must NOT exist:

Host/Fulfillment/FulfillmentEndpoints.cs

Host/Fulfillment/FulfillmentPanelComposer.cs

Host/Admin/ShippingServiceEndpoints.cs

Host/Admin/AdminOrderOperationsEndpoints.cs must NOT contain:

/v1/admin/shipping-methods

ListShippingMethodsAsync

ListEnabledShippingMethodsTreeQuery

Host must not have:

FulfillmentDbContext production business/query authority

Fulfillment-specific presentation composer

shipping service business/query logic

Fulfillment-specific manual error mapping

Any remaining Host authorization adapter must be tiny and explicitly transport/security-only.

12. Behavior preservation

Preserve all existing Fulfillment behavior.

Must preserve:

seller fulfillment listing/detail/mutations

admin fulfillment queue/list/detail operations

bulk admin action behavior

shipment/status transitions

tracking/provider fields

Order linkage

inventory/payment contract interactions

shipping services CRUD

shipping service translations/options

enabled shipping methods tree

language behavior

default ordering/sorting

ensure-seed behavior

event/outbox behavior

Accidental behavior change = 0.

Do not redesign fulfillment lifecycle.

13. Architecture guards

Upgrade:
Tooba.Fulfillment.Tests/Architecture/FulfillmentArchitectureGuardTests.cs

Must enforce Endpoints project:

exists

path↔namespace

Application ref present

no Host/Infrastructure ref

no foreign Application/Infrastructure

no DbContext

endpoints use ISender

semantic failures through ApiResponseFactory

no ex.Message/manual business envelopes

Application:

real MediatR handlers

use-case folders

no root command/query dump

foreign Contracts-only

no message/prose heuristics

Host:

old FulfillmentEndpoints absent

FulfillmentPanelComposer absent

ShippingServiceEndpoints absent

AdminOrderOperationsEndpoints has no shipping-method route/query

FulfillmentDbContext only explicit bootstrap/migration allowlist

no Fulfillment business/query authority

General:

no TypeForwardedTo

no clock/id bypass

no hidden DI fallback

no silent catch

no raw StartActivity

no localized exception prose

14. Focused tests only

Required:

Fulfillment.Tests

focused seller Fulfillment route behavior

focused admin Fulfillment behavior

bulk operation behavior

shipping service list/get/create/update/deactivate

ensure-seed

enabled shipping-method tree

auth compatibility

Result/error semantics

architecture guards

Then:
dotnet build src/backend/Tooba.slnx

Do NOT run:

broad Host suite

Checkout workflow

Tax/Pricing

frontend

unrelated module tests

No retry/sleep workaround.

15. Evidence

Create:
docs/evidence/TB-TMAR-FULFILLMENT-GOLDEN-001/

Required:

recovery-start.md

fulfillment-http-ownership-audit.md

fulfillment-cqrs-audit.md

fulfillment-auth-boundary.md

fulfillment-host-authority-audit.md

fulfillment-shipping-ownership-audit.md

fulfillment-error-semantics-audit.md

fulfillment-physical-tree.md

behavior-preservation-audit.md

architecture-guard-audit.md

recovery-sot.md

Physical tree:
every handwritten production Fulfillment .cs:
path | namespace | responsibility

16. Protected state

Must remain:
Cart: COMPLETE_REFERENCE_PATTERN
Settlement: COMPLETE_REFERENCE_PATTERN
Checkout: PAUSED_AT_SAFE_W5_CHECKPOINT
Tax/Pricing: untouched
Frontend: untouched

Do not start Returns or another module.

No reset/clean/force push/broad git add.
Preserve stashes/user files.

17. Completion scan

Before PASS search entire repo for:

FulfillmentEndpoints

FulfillmentPanelComposer

ShippingServiceEndpoints

FulfillmentDbContext

/v1/admin/shipping-methods

ListEnabledShippingMethodsTreeQuery usage in Host

/fulfillment

IAdminOrderFulfillmentOperations implementations

IShippingServiceLanguageGate implementations

Fulfillment Application ports implemented in Host

manual Fulfillment error mapping

exception message heuristics

Any production ownership leak => INCOMPLETE with exact path.

18. Success criteria

PASS only if ALL:

Fulfillment-HTTP-Ownership:
MODULE_ENDPOINTS

Fulfillment-Endpoints-State:
REAL_PROJECT_PRESENT

Fulfillment-CQRS-State:
MEDIATR_12_5_APPLICATION_HANDLERS

Fulfillment-Host-Endpoints:
REMOVED

Fulfillment-Host-Composer:
REMOVED

Fulfillment-Shipping-HTTP-Ownership:
MODULE_ENDPOINTS

Fulfillment-Shipping-Method-Route:
MODULE_ENDPOINTS

Fulfillment-Host-Business-Authority:
NONE

Fulfillment-Host-DbAuthority:
NONE

Fulfillment-CrossModule-Boundary:
CONTRACTS_ONLY

Fulfillment-Result-Adoption:
HTTP_USE_CASES_ADOPTED

Fulfillment-Error-Classification:
STABLE_CODES_ONLY

Fulfillment-Prose-Mapping:
NONE

Fulfillment-Unexpected-Exception-Swallow:
NONE

Fulfillment-Physical-State:
VERIFIED_ON_DISK_AND_NAMESPACE

Fulfillment-Architecture-Guards:
ENFORCED

Fulfillment-Behavior-Preservation:
VERIFIED

Fulfillment-State:
COMPLETE_REFERENCE_PATTERN

19. Canonical Result

Return ONLY BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Track
Recovery-Start
Fulfillment-HTTP-Ownership-Audit
Fulfillment-Endpoints-State
Fulfillment-CQRS-State
Fulfillment-Application-UseCases
Fulfillment-Auth-Boundary
Fulfillment-Shipping-Ownership
Fulfillment-Shipping-Method-Route
Fulfillment-Result-Adoption
Fulfillment-Error-Classification
Fulfillment-Prose-Mapping
Fulfillment-Unexpected-Exception-Swallow
Fulfillment-Host-Authority-Audit
Fulfillment-Host-Endpoints
Fulfillment-Host-Composer
Fulfillment-Host-Business-Authority
Fulfillment-Host-DbAuthority
Fulfillment-CrossModule-Boundary
Fulfillment-Physical-State
Fulfillment-Architecture-Guards
Fulfillment-Behavior-Preservation
Focused-Validation
Skipped-Validation
Full-Validation
AntiPattern-Gate
Residual-Defects
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

If incomplete:
Next-Recommended-Task:
TB-TMAR-FULFILLMENT-GOLDEN-001-R1

If complete:
Next-Recommended-Task:
ARCHITECT_SELECT_NEXT_REOPENED_MODULE

After Result:
STOP completely.
Do NOT start another module.
Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_TASK