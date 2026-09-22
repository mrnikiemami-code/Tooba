PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-PROMOTION-GOLDEN-001
Parent-Task: TB-TMAR-PAYMENT-GOLDEN-001-R2
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: FAST-SAFE
Track: PROMOTION_GOLDEN_CLOSURE
Title: Promotion Endpoints Ownership + MediatR CQRS + Host Composer Removal
Backend-Only: YES

Architect decision

Payment R2 is accepted after direct repository verification.

Architect directly confirmed:

Payment is in durable COMPLETE HTTP manifest.

Payment Endpoints/CQRS exist.

Host Payment business adapters targeted by R2 are gone.

Media.Contracts exists.

Order.Contracts.Payments exists.

StorefrontPaymentOrchestrator moved to Application/Orchestration.

recovery current state says Payment COMPLETE and next Promotion.

Note on Git state:
current HEAD is the Payment final docs-tip commit, while recovery lastAcceptedCommit records the accepted implementation/recovery parent commit. Do not try to make a JSON file self-reference its own commit hash. Preserve a clear distinction between implementation/accepted commit and current repository tip in evidence when needed.

This task is Promotion-only.

Golden target:
Tooba.Promotion.Endpoints
→ ISender
→ Tooba.Promotion.Application Commands/Queries/Handlers
→ Result/SemanticError
→ Contracts/Gates/Events only
→ Infrastructure

Host = composition/security only.

Do NOT start Offer/Inventory.

0. Direct repository findings

Promotion currently has:

Tooba.Promotion.Domain

Tooba.Promotion.Application

Tooba.Promotion.Contracts

Tooba.Promotion.Infrastructure

Tooba.Promotion.Tests

Missing:

Tooba.Promotion.Endpoints

Current HTTP ownership:
src/backend/Host/Tooba.Host/Promotion/PromotionEndpoints.cs

Current Host composer:
src/backend/Host/Tooba.Host/Promotion/PromotionPanelComposer.cs

Program currently:

registers PromotionPanelComposer

calls MapPromotionEndpoints()

Promotion.Application currently contains:

Checkout/

Merchandising/

Ports/

no real seller/admin Commands/Queries MediatR HTTP use-case surface

1. Current HTTP routes to preserve exactly

Seller:

GET /v1/seller/promotions

POST /v1/seller/promotions

GET /v1/seller/promotions/{id:guid}

PUT /v1/seller/promotions/{id:guid}

POST /v1/seller/promotions/{id:guid}/activate

POST /v1/seller/promotions/{id:guid}/deactivate

Admin:

GET /v1/admin/promotions

GET /v1/admin/promotions/{id:guid}

POST /v1/admin/promotions/{id:guid}/deactivate

Preserve all URL/verb/status/response semantics.

2. Current Host anti-patterns confirmed

PromotionEndpoints.cs currently owns:

seller/admin authorization plumbing

direct calls to PromotionPanelComposer

PlatformHttpException mapping

manual JSON error envelopes

InvalidOperationException mapping

Persian prose heuristic:
ex.Message.Contains("یافت نشد")

ex.Message leaked as response detail

broad fallback to promotion.mutation.rejected

PromotionPanelComposer.cs currently owns:

seller list/get/create/update/activate/deactivate orchestration

admin list/get/deactivate

request/body parsing

discount-kind parsing

percentage normalization

fixed currency normalization

default EffectiveFrom

validation using localized prose exceptions

direct DateTimeOffset.UtcNow

direct Domain type usage from Host

All of this must leave Host.

3. Required end state

Create:
src/backend/Modules/Promotion/Tooba.Promotion.Endpoints/

Required flow:
Promotion.Endpoints
→ ISender
→ Promotion.Application
→ Result
→ Promotion ports/contracts
→ Infrastructure

Delete:

Host/Promotion/PromotionEndpoints.cs

Host/Promotion/PromotionPanelComposer.cs

Preferred:
no production Host/Promotion/ folder remains except tiny security adapters if placed elsewhere under Seller/Admin.

Program:

remove PromotionPanelComposer registration

register endpoint presentation/auth adapters

call module MapPromotionEndpoints() from Promotion.Endpoints namespace

4. Create real Endpoints project

Create:
Tooba.Promotion.Endpoints

Add to:
src/backend/Tooba.slnx

Allowed refs:

Promotion.Application

Promotion.Contracts if needed

BuildingBlocks

Forbidden:

Host

Promotion.Infrastructure

foreign Application/Infrastructure

DbContext

Domain directly if transport does not genuinely need it

Folders:

Seller/

Admin/

Errors/Resources if needed

Endpoint module:
PromotionEndpointModule.cs

No giant root dump.

5. Real MediatR CQRS

Create real MediatR 12.5.0 use cases.

Queries:

ListSellerPromotions

GetSellerPromotion

ListAdminPromotions

GetAdminPromotion

Commands:

CreateSellerPromotion

UpdateSellerPromotion

ActivateSellerPromotion

DeactivateSellerPromotion

DeactivateAdminPromotion

Use:

Commands/<UseCase>/

Queries/<UseCase>/

Each has:

request

handler

Result/Result<T>

typed response where needed

Register Promotion.Application assembly in AddToobaCqrsFoundation(...).

No fake handlers.
No giant PromotionHandlers.cs.
No HTTP Endpoint direct IPromotionDirectory.

6. Move body parsing/normalization into Application use case

Current PromotionPanelComposer.ParseBody behavior must be preserved, but not as Host logic.

Preserve exactly:

Name required

CouponCode required

DiscountKind accepts:

FixedAmountOff

fixed

تومان
as FixedAmountOff

otherwise PercentageOff

percentage input:

> 1 means percent and divides by 100

<= 1 already fraction

FixedAmount currency:

blank => IRR

otherwise trim + uppercase

EffectiveFrom:

specified value preserved

missing => IClock.UtcNow

MinimumSubtotal preserved

Use:

IClock

stable machine validation codes

Do NOT use:

DateTimeOffset.UtcNow

localized exception prose

HTTP DTO parsing in Host

Wire DTO may live in Endpoints and map to Application command fields.

7. Stable error semantics

Create:

PromotionErrorCodes

PromotionExceptionMapper only if needed

Rules:

exact stable machine-code matching only

no Contains

no StartsWith

no localized text detection

unknown InvalidOperationException propagates

no ex.Message in HTTP response

Expected semantic families:

promotion.missing

promotion.name.required

promotion.coupon.required

promotion.mutation.rejected

promotion.activate.rejected

promotion.deactivate.rejected

seller.authorization.denied

admin.authorization.denied

Prefer actual existing Domain/Infrastructure stable codes where present.
Do not invent aliases just to pass tests.

Use:

Result / Result<T>

SemanticError

ApiResponseFactory

centralized error catalog/localization

Preserve existing 404 vs 400 behavior by stable semantics rather than prose matching.

8. Seller authorization boundary

Current Host uses SellerPanelAccess.RequireAuthorizedAsync.

Create:
IPromotionSellerAuthorizer

Host implementation may remain as tiny security adapter only.

It may resolve:

authenticated seller actor

sellerPartyId

existing seller authorization semantics

It must NOT:

call IPromotionDirectory

parse Promotion body

decide Promotion business rules

return Promotion DTOs

Promotion.Endpoints must not reference Host.

9. Admin authorization boundary

Current Host uses AdminPanelAccess.RequireAuthorizedAsync.

Create:
IPromotionAdminAuthorizer

Host adapter:

auth/security only

no Promotion business logic

no Promotion DbContext/directory

Preserve current admin authorization behavior exactly.

If existing capability-specific checks apply elsewhere, reuse without expanding auth design.

10. Seller ownership semantics

Current Promotion directory calls use:

ListBySellerAsync(null, sellerPartyId, ...)

GetForSellerAsync(null, sellerPartyId, ...)

CreateForSellerAsync(null, sellerPartyId, ...)

UpdateForSellerAsync(null, sellerPartyId, ...)

ActivateForSellerAsync(null, sellerPartyId, ...)

DeactivateForSellerAsync(null, sellerPartyId, ...)

Preserve exact seller scoping.

Do not introduce first-seller shortcuts.
Do not allow seller id from request body.

11. Admin semantics

Preserve:

optional sellerPartyId filter on admin list

admin get by promotion id

admin deactivate then return current row

missing remains 404

mutation failures preserve appropriate semantic status/code

No Host composer.

12. Existing Promotion internal boundaries

Promotion Infrastructure already references:

Offer.Contracts

Pricing.Contracts

Inventory.Contracts

ModuleContracts

Keep foreign boundaries Contracts-only.

Promotion Application currently references Offer.Contracts.
Audit whether truly necessary; allowed if real application boundary requires it.

Forbidden:

Offer.Application/Domain/Infrastructure

Pricing.Application/Domain/Infrastructure

Inventory.Application/Domain/Infrastructure

foreign DbContexts

Do NOT touch Tax/Pricing internals.

13. Checkout/Merchandising existing Application areas

Promotion already has:

Application/Checkout

Application/Merchandising

These are legitimate existing responsibilities.

Do not mechanically convert internal adapters/ports to CQRS unless they are HTTP use cases or doing so is required for boundary correctness.

Do not break Checkout promotion contracts.
Checkout remains PAUSED_AT_SAFE_W5_CHECKPOINT.

Do not redesign merchandising campaign behavior.

14. Physical folder standard

Final projects:

Tooba.Promotion.Domain

Tooba.Promotion.Application

Tooba.Promotion.Contracts

Tooba.Promotion.Infrastructure

Tooba.Promotion.Endpoints

Tooba.Promotion.Tests

Application:

Commands/<UseCase>/

Queries/<UseCase>/

Checkout/

Merchandising/

Ports/

Models/ only if real

Errors/

Endpoints:

Seller/

Admin/

Errors/Resources if real

Infrastructure existing responsibility folders remain.

No root dump.
Path↔namespace aligned.
No TypeForwardedTo.
No namespace masquerading.

15. Host cleanup

After task, must NOT exist:

Host/Promotion/PromotionEndpoints.cs

Host/Promotion/PromotionPanelComposer.cs

Host must not:

directly invoke IPromotionDirectory for HTTP behavior

parse Promotion mutation bodies

normalize discount values

use Promotion Domain types in Promotion HTTP path

map Promotion business errors manually

Program may only:

register seller/admin security adapters

AddPromotionEndpointPresentation()

MapPromotionEndpoints()

16. Behavior preservation

Preserve exact functional behavior:

seller list

seller get

seller create

seller update

seller activate

seller deactivate

admin list

admin sellerPartyId filter

admin get

admin deactivate

response shapes based on existing PromotionReference

201 on seller create

get missing behavior

discount kind parsing

percentage normalization

currency normalization

EffectiveFrom default

coupon/name validation

seller ownership

existing Offer/Pricing/Inventory integrations

outbox/events

checkout promotion behavior

merchandising campaign behavior

Accidental behavior change = 0.

17. Architecture guards

Upgrade:
PromotionArchitectureGuardTests

Current stale architecture guard does not require Endpoints/CQRS.

Required guards:

Endpoints project exists

Endpoints path↔namespace

Endpoints references Application

no Endpoints → Host/Infrastructure

no foreign Application/Infrastructure

no DbContext in Endpoints

endpoints use ISender

no direct IPromotionDirectory in Endpoints

no PlatformHttpException in Promotion HTTP flow

no ex.Message HTTP leakage

no Contains/StartsWith prose classification

no DateTimeOffset.UtcNow / DateTime.UtcNow / Guid.NewGuid in protected Promotion orchestration

real MediatR IRequest/IRequestHandler use cases

use-case folders exist

Host PromotionEndpoints absent

Host PromotionPanelComposer absent

Host no Promotion business authority

foreign module refs Contracts-only

physical folder/namespace alignment

no TypeForwardedTo

unknown exception not swallowed

After successful closure:
add Promotion to HostModuleEndpointOwnershipTests COMPLETE HTTP manifest.

18. Focused tests only

Required:

Promotion.Tests

seller create/update/list/get

activate/deactivate

admin list/filter/get/deactivate

missing promotion

validation/normalization

EffectiveFrom uses IClock

exact stable error mapping

unexpected exception propagation

architecture guards

HostModuleEndpointOwnershipTests

TmarDurableGuardTests

final dotnet build src/backend/Tooba.slnx

Do NOT run:

broad Host suite

Checkout workflow

Tax/Pricing

frontend

Offer/Inventory broad tests

No retry/sleep workaround.

19. Recovery SoT update — mandatory same cycle

On PASS update:

docs/architecture/tmar-current-state.json

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

task recovery-sot

Move Promotion:
reopenedModules → completeReferenceModules

Set:
Promotion:

state = COMPLETE_REFERENCE_PATTERN

httpApplicability = HTTP_OWNING

endpointOwnership = MODULE_ENDPOINTS

cqrs = MEDIATR_12_5

lastAcceptedTask = TB-TMAR-PROMOTION-GOLDEN-001

accepted implementation commit recorded unambiguously

Set nextTask:
TB-TMAR-OFFER-FINAL-REVERIFY-001

Remaining:

Offer = NEEDS_FINAL_REVERIFY

Inventory = NEEDS_APPLICABILITY_REVERIFY

Do not attempt impossible self-referential final-tip hashing inside the commit itself.
Evidence may separately state final pushed HEAD.

20. Evidence

Create:
docs/evidence/TB-TMAR-PROMOTION-GOLDEN-001/

Required:

recovery-start.md

promotion-http-ownership-audit.md

promotion-cqrs-audit.md

promotion-auth-boundary.md

promotion-error-semantics-audit.md

promotion-normalization-behavior.md

promotion-cross-module-contract-audit.md

promotion-host-authority-audit.md

promotion-physical-tree.md

behavior-preservation-audit.md

architecture-guard-audit.md

recovery-state-sync.md

recovery-sot.md

Physical tree:
every handwritten Promotion production .cs:
path | namespace | responsibility

21. Protected state

COMPLETE and protected:

Cart

Settlement

Fulfillment

Returns

Notification

Support

Wallet

Payment

Checkout:
PAUSED_AT_SAFE_W5_CHECKPOINT

Tax/Pricing:
untouched in this wave

Frontend:
frozen

Do NOT start Offer/Inventory.

No reset.
No clean.
No force push.
No broad git add.
Stashes/user files untouched.

22. Completion scan

Before PASS scan repo for:

Host/Promotion/PromotionEndpoints.cs

Host/Promotion/PromotionPanelComposer.cs

PromotionPanelComposer

IPromotionDirectory in Host HTTP path

/v1/seller/promotions

/v1/admin/promotions

Promotion PlatformHttpException

Promotion error Contains / StartsWith

Promotion HTTP ex.Message

DateTimeOffset.UtcNow in Promotion production orchestration

foreign Application/Infrastructure refs in Promotion

PromotionDbContext in Host

TypeForwardedTo

Any unresolved production ownership leak => INCOMPLETE.

23. Success criteria

PASS only if ALL:

Promotion-HTTP-Ownership: MODULE_ENDPOINTS
Promotion-Endpoints-State: REAL_PROJECT_PRESENT
Promotion-CQRS-State: MEDIATR_12_5_APPLICATION_HANDLERS
Promotion-Seller-Routes: MODULE_ENDPOINTS
Promotion-Admin-Routes: MODULE_ENDPOINTS
Promotion-Host-Endpoints: REMOVED
Promotion-Host-Composer: REMOVED
Promotion-Host-Business-Authority: NONE
Promotion-Host-DbAuthority: NONE
Promotion-CrossModule-Boundary: CONTRACTS_ONLY
Promotion-Result-Adoption: HTTP_USE_CASES_ADOPTED
Promotion-Error-Classification: STABLE_CODES_ONLY
Promotion-Prose-Mapping: NONE
Promotion-Unexpected-Exception-Swallow: NONE
Promotion-Time: ICLOCK
Promotion-Physical-State: VERIFIED_ON_DISK_AND_NAMESPACE
Promotion-Architecture-Guards: ENFORCED
Promotion-Behavior-Preservation: VERIFIED
Promotion-Microservice-Extraction: READY_WITHOUT_REWRITE
Recovery-State: CURRENT_AND_MACHINE_READABLE
Recovery-Next-Task: TB-TMAR-OFFER-FINAL-REVERIFY-001
Promotion-State: COMPLETE_REFERENCE_PATTERN

24. Canonical Result

Return ONLY BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Track
Recovery-Start
Promotion-HTTP-Ownership
Promotion-Endpoints-State
Promotion-CQRS-State
Promotion-Application-UseCases
Promotion-Seller-Routes
Promotion-Admin-Routes
Promotion-Auth-Boundary
Promotion-Result-Adoption
Promotion-Error-Classification
Promotion-Prose-Mapping
Promotion-Unexpected-Exception-Swallow
Promotion-Time
Promotion-Host-Authority-Audit
Promotion-Host-Endpoints
Promotion-Host-Composer
Promotion-Host-Business-Authority
Promotion-Host-DbAuthority
Promotion-CrossModule-Boundary
Promotion-Physical-State
Promotion-Architecture-Guards
Promotion-Behavior-Preservation
Promotion-Microservice-Extraction
Recovery-State
Recovery-Next-Task
Current-State-Manifest
Focused-Validation
Skipped-Validation
Full-Validation
AntiPattern-Gate
Residual-Defects
Promotion-State
Payment-State
Wallet-State
Support-State
Notification-State
Returns-State
Fulfillment-State
Settlement-State
Cart-State
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
Next-Recommended-Task: TB-TMAR-PROMOTION-GOLDEN-001-R1

If complete:
Next-Recommended-Task: TB-TMAR-OFFER-FINAL-REVERIFY-001

After Result:
STOP completely.
Do NOT start Offer.
Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_TASK