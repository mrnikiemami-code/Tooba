PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-RETURNS-GOLDEN-001

Parent-Task:
TB-TMAR-FULFILLMENT-GOLDEN-001

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
RETURNS_GOLDEN_CLOSURE

Title:
Returns Endpoints Ownership + MediatR CQRS + Host Composer Removal + Golden Closure

Backend-Only:
YES

Architect decision

Fulfillment is accepted after direct repo verification:

real Tooba.Fulfillment.Endpoints exists

Host/Fulfillment removed

Host/Admin/ShippingServiceEndpoints removed

shipping-method route removed from Order Host endpoint

stable-code exception mapping

CQRS/use-case folders present

This task is Returns-only.

Golden target:
Tooba.Returns.Endpoints
→ ISender
→ Tooba.Returns.Application Commands/Queries/Handlers
→ Result/SemanticError
→ Contracts-only foreign boundaries
→ Infrastructure

Host:

composition/security adapters only

calls MapReturnEndpoints()

no Returns endpoint/composer/business/query ownership

Do NOT start Notification/Support/etc.

0. Direct repository findings

Returns currently has:

Tooba.Returns.Domain

Tooba.Returns.Application

Tooba.Returns.Contracts

Tooba.Returns.Infrastructure

Tooba.Returns.Tests

Missing:

Tooba.Returns.Endpoints

Current Returns HTTP/business presentation is still in:

Host/Tooba.Host/Returns/ReturnEndpoints.cs

Host/Tooba.Host/Returns/ReturnPanelComposer.cs

Current Application is NOT yet a proper CQRS application surface.
Architect directly verified it currently contains essentially:

Models/

Ports/

no real Commands/

no real Queries/

no MediatR use-case folders

ReturnPanelComposer still orchestrates:

Get

customer list

seller-scoped get/list

admin list

admin grid normalization via Host AdminListGridPolicies.Returns

Create

Approve

Reject

RetryRefund

This is exactly the Cart-type Host ownership problem.

1. Required end state

Create:
src/backend/Modules/Returns/Tooba.Returns.Endpoints/

Required flow:
Returns.Endpoints
→ ISender
→ Returns.Application Commands/Queries
→ Result
→ Returns ports
→ Infrastructure

Host may only:

register tiny auth adapters if unavoidable

call app.MapReturnEndpoints()

Delete:

Host/Returns/ReturnEndpoints.cs

Host/Returns/ReturnPanelComposer.cs

Preferred:

no production files remain under Host/Returns/

2. Create real Endpoints project

Create Tooba.Returns.Endpoints.
Add to src/backend/Tooba.slnx.

Allowed refs:

Returns.Application

Returns.Contracts if wire/public contracts genuinely needed

BuildingBlocks presentation/security/grid primitives

Forbidden refs:

Host

Returns.Infrastructure

foreign Application/Infrastructure

any DbContext

Namespace root:
Tooba.Returns.Endpoints

Likely physical folders:

Customer/

Seller/

Admin/

Errors/Resources if needed

No giant endpoint root dump.

3. Move ALL Returns HTTP routes

Audit Host/Returns/ReturnEndpoints.cs and move every Returns-owned route to module Endpoints.

Preserve exact:

URLs

verbs

route parameters

request bodies

success response shapes

auth behavior

status/error semantics

Host must only:
app.MapReturnEndpoints();

No duplicate route mapping.

Delete Host ReturnEndpoints.

4. Build real MediatR CQRS

Returns currently lacks proper CQRS. Add real use cases.

At minimum create use-case folders for all HTTP behaviors currently in ReturnPanelComposer / ReturnEndpoints.

Expected Queries:

GetCustomerReturn

ListCustomerReturns

GetSellerReturn

ListSellerReturns

ListAdminReturns

QueryAdminReturnsGrid

Expected Commands:

CreateReturn

ApproveReturn

RejectReturn

RetryReturnRefund

Use exact names that fit existing language if better, but one cohesive folder per use case.

Pattern:
Commands/CreateReturn/...
Queries/ListSellerReturns/...

Each:

Request

Handler

Result/response model as needed

MediatR version:
12.5.0 through existing Tooba CQRS foundation.

Register Returns Application assembly in AddToobaCqrsFoundation(...).

No giant ReturnHandlers.cs.
No ceremonial handlers.

5. Remove ReturnPanelComposer

Delete:
Host/Returns/ReturnPanelComposer.cs

Move its real responsibilities:

Seller ownership filter

Current behavior:
get return and ensure snapshot.SellerPartyId == sellerPartyId

This becomes Application query logic, not Endpoint/Host.

Admin grid normalization

Current behavior:
AdminListGridPolicies.Returns.Normalize(request)

Move Returns-specific policy into Returns Application:
e.g. AdminReturnGridQueryPolicy

Do NOT reference Tooba.Host.Grid.

Create/Approve/Reject/Retry

Move orchestration into Commands/Handlers.

No Host replacement composer.

6. Authorization boundary

Current Returns endpoints likely use:

CustomerPanelAccess/current session

SellerPanelAccess

AdminPanelAccess

CurrentAuthenticatedSession

IAuthorizationGuard

ICurrentTenant

environment

Returns.Endpoints MUST NOT reference Host.

Use shared neutral auth abstractions where available.

If needed introduce tiny endpoint authorizer interfaces:

IReturnCustomerAuthorizer

IReturnSellerAuthorizer

IReturnAdminAuthorizer

Host implementations acceptable only as security adapters:

no Returns business rules

no DbContext

no Returns directory

no query orchestration

no manual business error mapping

Pass explicit actor/customer/seller IDs into Commands/Queries.

7. Result pattern and error semantics

Every expected Returns business outcome must use:

Result / Result<T>

SemanticError

centralized descriptor/catalog/localization

ApiResponseFactory

Current Host endpoint reportedly uses ReturnSemanticMapper; audit it carefully.

Golden target:

expected stable business failures mapped in Application

no Host semantic mapper

no message/prose matching

no Contains/StartsWith heuristic

no unknown InvalidOperationException swallowed

no ex.Message leak

no PlatformHttpException for Returns business outcome

no manual semantic Results.Json

If existing Returns exception sources already emit stable machine codes, map exact codes only.

Unknown/unexpected exceptions must propagate.

8. Refund destination validation

Current ReturnPanelComposer previously carried/reflected refund-destination validation logic.

Ensure validation belongs to:

Endpoint parsing for wire shape only, and/or

Application command validation/business semantics

No localized exception prose.
No Host-owned destination semantics.

Preserve exact accepted destinations and behavior.

9. Admin grid ownership

Move Returns grid normalization out of Host.

Required:

Returns Application owns Returns-specific allowed filters/sorts/defaults

generic Grid primitives from BuildingBlocks are fine

Infrastructure IAdminReturnGridQuery stays module-owned

no Host grid policy dependency

Preserve exact:

search

filters

advanced filters

sort

paging

default queue scope

response shape

Remove AdminListGridPolicies.Returns if no longer used anywhere.

10. Cross-module boundaries

Returns Infrastructure already references:

Order.Contracts

Fulfillment.Contracts

Payment.Contracts

Inventory.Contracts

Wallet.Contracts

Keep foreign refs Contracts-only.

Forbidden:

foreign Application/Domain/Infrastructure

foreign DbContexts

Host callbacks for business logic

Do not broaden-recover foreign modules.

11. Physical folder standard

Must match Cart/Settlement/Fulfillment quality.

Projects:

Tooba.Returns.Domain

Tooba.Returns.Application

Tooba.Returns.Contracts

Tooba.Returns.Infrastructure

Tooba.Returns.Endpoints

Tooba.Returns.Tests

Application:

Commands/<UseCase>/

Queries/<UseCase>/

Models/

Ports/

Errors/

Endpoints:

Customer/

Seller/

Admin/

Errors/Resources as needed

Infrastructure:

Persistence/

Directories/

Queries/

Gateways/

Bridges/

Adapters/

Evaluators/

Events/

Messaging/

DependencyInjection/

Observability/
as applicable

No root dump.
Path↔namespace aligned.
No TypeForwardedTo.
No namespace masquerading.

12. Host cleanup target

After task, must NOT exist:

Host/Returns/ReturnEndpoints.cs

Host/Returns/ReturnPanelComposer.cs

Host must not contain:

ReturnsDbContext production query/business authority

IReturnDirectory business use

IAdminReturnGridQuery use

ReturnSemanticMapper presentation ownership

Returns-specific manual error mapping

Returns grid policy semantics

Preferred:
no production Host/Returns/ directory.

Program may:

register tiny auth adapters

call MapReturnEndpoints()

13. Behavior preservation

Preserve exact:

customer create return

customer list/detail access

seller list/detail scope enforcement

admin list/grid

approve

reject

retry refund

idempotency

return reason/items/quantities

refund destination

return state transitions

payment/refund behavior

inventory/wallet interactions

outbox/events

existing success response shapes

Accidental behavior change = 0.

Do not redesign Returns domain lifecycle.

14. Architecture guards

Upgrade:
Tooba.Returns.Tests/Architecture/ReturnsArchitectureGuardTests.cs

Must enforce:

Endpoints:

project exists

path↔namespace

Application ref only, no Infra/Host

no foreign Application/Infrastructure

no DbContext

uses ISender

uses ApiResponseFactory

no manual semantic envelopes

no ex.Message

no prose/message heuristics

Application:

real Commands/Queries/Handlers

use-case folders

foreign refs Contracts-only

no Host

no foreign DbContexts

no message heuristic mapping

Host:

ReturnEndpoints absent

ReturnPanelComposer absent

ReturnsDbContext only bootstrap/migration allowlist

no Returns grid/business/presentation authority

no Host Returns manual semantic mapper

General:

no TypeForwardedTo

no clock/id bypass

no hidden DI fallback

no silent catch

no raw StartActivity

no localized exception prose

15. Focused tests only

Required high-value coverage:

customer create/list/get

seller scope enforcement

admin list/grid

approve/reject

retry refund

refund destination

idempotency

Result/error semantics

unexpected exception propagation if mapper changes

exact endpoint URLs

architecture guards

Then:
dotnet build src/backend/Tooba.slnx

Do NOT run:

broad Host suite

Checkout workflow

Tax/Pricing

frontend

unrelated modules

No retry/sleep workaround.

16. Evidence

Create:
docs/evidence/TB-TMAR-RETURNS-GOLDEN-001/

Required:

recovery-start.md

returns-http-ownership-audit.md

returns-cqrs-audit.md

returns-auth-boundary.md

returns-host-authority-audit.md

returns-grid-ownership-audit.md

returns-error-semantics-audit.md

returns-physical-tree.md

behavior-preservation-audit.md

architecture-guard-audit.md

recovery-sot.md

Physical tree:
every handwritten production Returns .cs:
path | namespace | responsibility

17. Protected state

Must remain:
Cart: COMPLETE_REFERENCE_PATTERN
Settlement: COMPLETE_REFERENCE_PATTERN
Fulfillment: COMPLETE_REFERENCE_PATTERN
Checkout: PAUSED_AT_SAFE_W5_CHECKPOINT
Tax/Pricing: untouched
Frontend: untouched

Do not start Notification or another module.

No reset/clean/force push/broad git add.
Preserve stashes/user files.

18. Completion scan

Before PASS search entire repo for:

ReturnEndpoints

ReturnPanelComposer

ReturnsDbContext

IReturnDirectory usage outside Returns

IAdminReturnGridQuery usage outside Returns

/return

/returns

AdminListGridPolicies.Returns

ReturnSemanticMapper

Returns Application ports implemented in Host

manual Returns error mapping

exception message heuristics

Any production ownership leak => INCOMPLETE with exact path.

19. Success criteria

PASS only if ALL:

Returns-HTTP-Ownership:
MODULE_ENDPOINTS

Returns-Endpoints-State:
REAL_PROJECT_PRESENT

Returns-CQRS-State:
MEDIATR_12_5_APPLICATION_HANDLERS

Returns-Host-Endpoints:
REMOVED

Returns-Host-Composer:
REMOVED

Returns-Grid-Ownership:
MODULE_OWNED

Returns-Host-Business-Authority:
NONE

Returns-Host-DbAuthority:
NONE

Returns-CrossModule-Boundary:
CONTRACTS_ONLY

Returns-Result-Adoption:
HTTP_USE_CASES_ADOPTED

Returns-Error-Classification:
STABLE_CODES_ONLY

Returns-Prose-Mapping:
NONE

Returns-Unexpected-Exception-Swallow:
NONE

Returns-Physical-State:
VERIFIED_ON_DISK_AND_NAMESPACE

Returns-Architecture-Guards:
ENFORCED

Returns-Behavior-Preservation:
VERIFIED

Returns-State:
COMPLETE_REFERENCE_PATTERN

20. Canonical Result

Return ONLY BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Track
Recovery-Start
Returns-HTTP-Ownership-Audit
Returns-Endpoints-State
Returns-CQRS-State
Returns-Application-UseCases
Returns-Auth-Boundary
Returns-Grid-Ownership
Returns-Result-Adoption
Returns-Error-Classification
Returns-Prose-Mapping
Returns-Unexpected-Exception-Swallow
Returns-Host-Authority-Audit
Returns-Host-Endpoints
Returns-Host-Composer
Returns-Host-Business-Authority
Returns-Host-DbAuthority
Returns-CrossModule-Boundary
Returns-Physical-State
Returns-Architecture-Guards
Returns-Behavior-Preservation
Focused-Validation
Skipped-Validation
Full-Validation
AntiPattern-Gate
Residual-Defects
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

If incomplete:
Next-Recommended-Task:
TB-TMAR-RETURNS-GOLDEN-001-R1

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