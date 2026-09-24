Tooba Architect Bootstrap

Canonical bootstrap for recovering the Tooba architecture context after chat/session loss.

## Current Order Recovery (authoritative)

- Locks: ARCH-COMPLETE-002, HOST-MODULE-ENDPOINT-001, ARCH-CQRS-001/002
- Golden wave = COMPLETE + USER_ACCEPTED; backend-only; frontendFrozen = true
- Complete modules: Cart, Settlement, Fulfillment, Returns, Notification, Support, Wallet, Payment, Promotion, Offer, Order, Inventory
- Eleven HTTP-owning modules use MODULE_ENDPOINTS + MEDIATR_12_5.
- Inventory = INTERNAL_ONLY / NOT_APPLICABLE / INTERNAL_USE_CASE_BOUNDARIES
- reopenedModules = empty; internalApplicabilityReviewModules = empty; activeModuleRecovery = empty
- Checkout = PAUSED_AT_SAFE_W5_CHECKPOINT
- Order = COMPLETE_REFERENCE_PATTERN after TB-TMAR-ORDER-GOLDEN-001-FINAL-CLOSURE (prior: TB-TMAR-ORDER-GOLDEN-001-R11-R1, TB-TMAR-ORDER-GOLDEN-001-R11, TB-TMAR-ORDER-GOLDEN-001-R10, TB-TMAR-ORDER-GOLDEN-001-R9, TB-TMAR-ORDER-GOLDEN-001-R8, TB-TMAR-ORDER-GOLDEN-001-R7, TB-TMAR-ORDER-GOLDEN-001-R6, TB-TMAR-ORDER-GOLDEN-001-R3, TB-TMAR-ORDER-GOLDEN-001-R3B, TB-TMAR-ORDER-GOLDEN-001-R4, TB-TMAR-ORDER-GOLDEN-001-R4-R1, TB-TMAR-ORDER-GOLDEN-001-R5, TB-TMAR-ORDER-GOLDEN-001-R5-R1)
- Post-closure quality: TB-TMAR-ORDER-POSTCLOSURE-QUALITY-001-R1 (complete transport FluentValidation coverage; Order remains COMPLETE_REFERENCE_PATTERN)
- Post-closure structure: TB-TMAR-ORDER-POSTCLOSURE-STRUCTURE-001 (Order.Endpoints capability foldering); TB-TMAR-ORDER-POSTCLOSURE-STRUCTURE-002 (Order.Infrastructure capability + integration foldering); TB-TMAR-ORDER-POSTCLOSURE-STRUCTURE-LOCK-001 (ARCH-COMPLETE-002 structure lock; Order STRUCTURE_CERTIFIED); TB-TMAR-CART-ARCH-COMPLETE-002-REVERIFY-001 (Cart STRUCTURE_CERTIFIED under ARCH-COMPLETE-002 + complete endpoint-reachable validator coverage); TB-TMAR-CART-ARCH-COMPLETE-002-REVERIFY-001-R1 (Cart ARCH-COMPLETE-002 SoT consistency repair + durable coherence guard)
- Cart Host residual: TB-TMAR-CART-HOST-RESIDUAL-REVERIFY-001 (HOST_CART_ILLEGAL_AUTHORITY = 0; Cart expiry + persistence policy Cart-owned; Host worker shell only; no broad Cart global usings)
- Cart post-certification semantic/ownership repair: TB-TMAR-CART-POSTCERT-SEMANTIC-HOST-CLOSURE-001 (Host owns zero Cart implementation classes; Cart expiry worker/options + persistence adapter Cart-owned; persistence-hours fully async; CreateGuestCart uses canonical commerce context; no hardcoded Persian presentation fallbacks)
- Cart commerce-authority repair: TB-TMAR-CART-POSTCERT-SEMANTIC-HOST-CLOSURE-001-R1 (Cart owns no commerce policy default; platform-owned StoreCommerceContext is the effective storefront commerce authority consumed by Cart; fail-closed cart.commerce.* codes; Cart remains COMPLETE_REFERENCE_PATTERN + STRUCTURE_CERTIFIED)
- Cart store-commerce fail-fast: TB-TMAR-CART-STORE-COMMERCE-FAILFAST-001 (Production startup fails fast on incomplete/invalid StoreCommerce; Marketplace validates deployment Market/Currency/SalesChannel; SingleStore validates every ACTIVE tenant (Disabled/Suspended skipped); SalesChannel validated against canonical Tooba.Offer.Contracts.Dtos.SalesChannel; Cart code untouched; Cart remains COMPLETE_REFERENCE_PATTERN + STRUCTURE_CERTIFIED)
- StoreContext foundation extraction: TB-TMAR-STORECONTEXT-FOUNDATION-001 (effective store commerce context extracted from Tooba.BuildingBlocks to Tooba.StoreContext.Contracts + Tooba.StoreContext.Infrastructure; request and worker paths assign StoreContext separately from technical CommerceContext; Cart consumes only StoreContext.Contracts with zero Host dependency; parent fail-fast preserved; no new default; no Shared-DB claim; StoreContext = FOUNDATION_EXTRACTED_NOT_YET_STRUCTURE_CERTIFIED)
- StoreContext golden hardening: TB-TMAR-STORECONTEXT-GOLDEN-001 (StoreCommerceContext.Currency -> DefaultCurrency: default storefront selection input only, never a single-currency transaction/line/order/payment invariant; canonical key StoreCommerce:DefaultCurrency, no silent Currency alias; Cart adapter wording only; StoreContext = PLATFORM_CONTEXT_REFERENCE_PATTERN / INTERNAL_ONLY / NOT_APPLICABLE endpoints / NOT_APPLICABLE_NO_APPLICATION_USE_CASE, ARCH-COMPLETE-002 STRUCTURE_CERTIFIED, no ceremonial Application/Endpoints/MediatR; Cart single pricing-currency residual debt explicitly NOT repaired; Cart/Order certifications preserved)
- Current next task: USER_REVIEW_STORECONTEXT_GOLDEN_001
- Gate: USER_REVIEW_REQUIRED_BEFORE_NEXT_TMAR_WAVE
- Closed by: TB-TMAR-GOLDEN-WAVE-FINAL-CLOSURE-001
- Order closed by: TB-TMAR-ORDER-GOLDEN-001-FINAL-CLOSURE

1. Primary Goal

The highest architectural goal of Tooba is:

Keep Tooba as a strict Modular Monolith today while making future migration to Microservices low-friction, incremental, and without painful rewrites.

Every architecture decision must be evaluated against this goal.

2. Current Architecture Recovery Program

Program:
TMAR — Tooba Microservice-Ready Architecture Recovery

TMAR-Execution-Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Frontend remains frozen until explicit Architect/User release.
Do not modify production frontend code.

Last accepted Product task:
TB-P10-T022-R21

Last accepted TMAR task:
TB-TMAR-STORECONTEXT-FOUNDATION-001

Accepted architecture baseline:
TB-TMAR-ARCH-BASELINE

Current next task:
USER_REVIEW_STORECONTEXT_GOLDEN_001

Current recovery summary (consistent with the authoritative closure above):
COMPLETE HTTP-owning: Cart, Settlement, Fulfillment, Returns, Notification, Support, Wallet, Payment, Promotion, Offer, Order.
Inventory COMPLETE_REFERENCE_PATTERN as INTERNAL_ONLY / NOT_APPLICABLE / INTERNAL_USE_CASE_BOUNDARIES.
Checkout PAUSED_AT_SAFE_W5_CHECKPOINT. Tax/Pricing UNTOUCHED in this wave. Frontend BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE.
Machine-readable: docs/architecture/tmar-current-state.json
Current gate: USER_REVIEW_REQUIRED_BEFORE_NEXT_TMAR_WAVE.

HISTORICAL / SUPERSEDED next-task wording (do not use as current):
TB-TMAR-PROMOTION-GOLDEN-001 after premature Payment COMPLETE; TB-TMAR-NEXT-MODULE-BATCH-002 after TB-TMAR-NEXT-MODULE-BATCH-001. Inventory/Promotion COMPLETE_REFERENCE_PATTERN. Module-Recovery-State NEXT_REFERENCE_BATCH_COMPLETE.

Product development rule:
Foundation (TB-TMAR-FND-001) is ACCEPTED.
Host Wave 1 Slice 1 accepted after TB-TMAR-HOST-W1 + TB-TMAR-HOST-W1-R1.
Host Wave 2 (TB-TMAR-HOST-W2) PASS.
Boundary verification (TB-TMAR-BOUNDARY-V1) PASS — cross-module leaks confirmed and frozen.
Boundary repair (TB-TMAR-BOUNDARY-V1-R1) PASS — god-file growth frozen; Infra→foreign Application growth frozen.
Contracts Wave 1 (TB-TMAR-CONTRACTS-W1) PASS — Offer/Wallet Contracts; Domain→Offer Domain and Payment→Wallet.Domain removed.
Contracts Wave 2 (TB-TMAR-CONTRACTS-W2) PASS — Wallet payment port; Offer lookup Contracts; ARCH-TX-001.
Contracts Wave 3 (TB-TMAR-CONTRACTS-W3) PASS — Returns→Wallet.Contracts refund port; Order.App→Offer.Contracts SalesChannel.
Contracts Wave 4 (TB-TMAR-CONTRACTS-W4) PASS — Cart→Offer.Contracts; Tax.Contracts calculator; Order.App→Tax.Contracts.
Contracts Wave 5 (TB-TMAR-CONTRACTS-W5) PASS — Inventory→Offer.Contracts; Pricing.Contracts lookup; Order.App→Pricing.Contracts.
Contracts Wave 6 (TB-TMAR-CONTRACTS-W6) PASS — Promotion→Offer.Contracts; Cart→Pricing.Contracts; FE READY.
Frontend Baseline (TB-TMAR-FE-BASELINE) PASS — inventory/ownership/target arch; FE-SIZE/SEO/BOUNDARY locks+guards; root `src/frontend`; no broad refactor.
Frontend F1 (TB-TMAR-FE-F1) PASS — FE-FOLDER freezes; canonical test discovery; admin-languages migrated to features/.
Frontend ADMIN-W1 (TB-TMAR-FE-ADMIN-W1) PASS — admin-promotions migrated; admin-api/screens shrink.
Frontend ADMIN-W2 (TB-TMAR-FE-ADMIN-W2) PASS — admin-reviews migrated; admin-api/screens shrink; Admin-Migration-Pattern PROVEN.
Frontend ADMIN-W3 (TB-TMAR-FE-ADMIN-W3) PASS — admin-sellers migrated; admin-api/screens shrink; Flat CONTINUE_FEATURE_MIGRATION.
Frontend ADMIN-W4 (TB-TMAR-FE-ADMIN-W4) PASS — admin-customers migrated; admin-api/screens shrink; Flat CONTINUE_FEATURE_MIGRATION.
Frontend ADMIN-W5 (TB-TMAR-FE-ADMIN-W5) PASS — admin-receipts migrated; admin-screens 1036→894; Flat exit NOT_READY; Architecture-Priority FE_ADMIN.
Frontend ADMIN-W6 (TB-TMAR-FE-ADMIN-W6) PASS — admin-dashboard migrated; admin-api 1022→1002; exports 38→35; admin-screens 894→810; Flat exit READY_TO_PIVOT; Architecture-Priority HOST.
Host W3 (TB-TMAR-HOST-W3) PASS — StoreAppearanceSettings write → Catalog CQRS Directory; Host-write baseline shrink; CONTINUE_HOST; next HOST-W4.
Host W4 (TB-TMAR-HOST-W4) PASS — QuantitySettings write → Catalog CQRS Directory; Host-write baseline shrink; CONTINUE_HOST; next HOST-W5.
Host W5 (TB-TMAR-HOST-W5) PASS — UnitOfMeasure writes → Catalog CQRS Directory; Host-Exit-State NOT_READY; CONTINUE_HOST; next HOST-W6.
Host W6 (TB-TMAR-HOST-W6) PASS — ShippingService Create/Update/Deactivate/EnsureSeed → Fulfillment CQRS Directory; Host-Exit-State READY_TO_PIVOT; Architecture-Priority CHECKOUT_DESIGN; next CHECKOUT-CONSISTENCY-DESIGN.
Checkout Consistency Design (TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN) PASS — design+locks only; Process Manager owned by Order; PONR MULTI_STAGE; READY_FOR_IMPLEMENTATION_W1; Architecture-Priority CHECKOUT_IMPLEMENTATION; next CHECKOUT-IMPL-W1.
Checkout Implementation W1 (TB-TMAR-CHECKOUT-IMPL-W1) PASS — durable process state + idempotency foundation; TransactionScope preserved; no Saga runtime; W2 READY; Orders FE STILL_WAITING_FOR_BACKEND_W2; Architecture-Priority CHECKOUT_IMPLEMENTATION; next CHECKOUT-IMPL-W2.
Checkout Implementation W2 (TB-TMAR-CHECKOUT-IMPL-W2) PASS — Process Manager + Inventory.Contracts; TX preserved; W3 READY; next CHECKOUT-IMPL-W3.
Checkout Implementation W3 (TB-TMAR-CHECKOUT-IMPL-W3) PASS — Cart.Contracts ICartConversionPort; Order.Application↛Cart.Application; TX preserved; W4 READY; next CHECKOUT-IMPL-W4.
Host Structure W1 (TB-TMAR-HOST-STRUCTURE-W1) PASS — Host folder reorganization; HOST-FOLDER-001; READY_TO_PAUSE; next CHECKOUT-IMPL-W4.
Checkout Implementation W4 (TB-TMAR-CHECKOUT-IMPL-W4) PASS — IOrderInventoryLifecyclePort; Order.Infrastructure↛Inventory.Application; TX preserved; W5 READY; next CHECKOUT-IMPL-W5.
Checkout Implementation W5 (TB-TMAR-CHECKOUT-IMPL-W5) PASS — ICheckoutPromotionPort; Order.Application↛Promotion.Application; TX preserved; W6 READY; next CHECKOUT-IMPL-W6.
Offer Reference Module W1 (TB-TMAR-OFFER-REFERENCE-W1) PASS then REOPENED — physical structure mismatch found by user visual inspection.
Offer Reference Module Repair R1 (TB-TMAR-OFFER-REFERENCE-W1-R1) PASS — Physical-Structure-State VERIFIED_ON_DISK; ARCH-MODULE-PHYSICAL-001; COMPLETE_REFERENCE_PATTERN revalidated; Checkout paused at W5. The R1-era Pricing gate is closed by TB-TMAR-REFBATCH-TP-001.
Offer Reference Repair R3 (TB-TMAR-OFFER-REFERENCE-W1-R3) — Module-Recovery-State IN_PROGRESS_REFERENCE_REPAIR; CQRS and contract-boundary repair; Checkout paused; next TB-TMAR-OFFER-REFERENCE-W1-R4.
Offer Reference Repair R4 (TB-TMAR-OFFER-REFERENCE-W1-R4) — Module-Recovery-State READY_FOR_FINAL_VERIFICATION; real CQRS reads, Host BFF removal, and Pricing/Inventory owner gates; next TB-TMAR-OFFER-REFERENCE-W1-R5.
Offer Reference Final Verification R5 (TB-TMAR-OFFER-REFERENCE-W1-R5) — Module-Recovery-State INCOMPLETE; magic exception seam repaired; Host OfferDbContext residual remains; next TB-TMAR-OFFER-REFERENCE-W1-R6.
Offer Reference Host Persistence Closure R6 (TB-TMAR-OFFER-REFERENCE-W1-R6) — historically COMPLETE_REFERENCE_PATTERN; REOPENED_WAITING_CENTRAL_FOUNDATION after user review found cross-cutting observability/error presentation gap.
Foundation Observability/Error Presentation R1 (TB-TMAR-FND-OBSERR-001-R1) — FOUNDATION_PHASE1_COMPLETE; central Correlation/SafeErrorMapper/ApiResponseFactory/locale/ProblemDetails.
Foundation Observability/Error Presentation R2 (TB-TMAR-FND-OBSERR-001-R2) — FOUNDATION_RUNTIME_TRACING_COMPLETE; runtime correlation/log-scope/MediatR/module topology/messaging; next TB-TMAR-FND-OBSERR-001-R3.
Foundation Observability/Error Presentation R3 (TB-TMAR-FND-OBSERR-001-R3) — FOUNDATION_ERROR_LOCALIZATION_COMPLETE; ErrorDescriptor catalog + .resx localization + exception presentation; Offer READY_FOR_FINAL_REFERENCE_REVERIFY; next TB-TMAR-FND-OBSERR-001-R4.
Tax Reference Module W1 (TB-TMAR-TAX-REFERENCE-W1) PASS — COMPLETE_REFERENCE_PATTERN revalidated by TB-TMAR-REFBATCH-TP-001.
Pricing Reference Module W1 (TB-TMAR-PRICING-REFERENCE-W1) PASS — COMPLETE_REFERENCE_PATTERN revalidated by TB-TMAR-REFBATCH-TP-001. The stale wait for USER_REVIEW_OFFER is removed.
Product-Resume-Safety = SAFE_WITH_TMAR_PARALLEL (Order hub still frozen; do not expand App→App / Infra→App; no NEW cross-context ACID).
Last Product Task = TB-P10-T022-R21.
Architecture Baseline = TB-TMAR-ARCH-BASELINE.
Primary goal = painless future Microservice migration.
After Foundation PASS, product development may continue in parallel with gradual TMAR refactoring, but all new code must follow the new architecture locks.

3. Worker Protocol

Architect:
ChatGPT

Execution Worker:
Cursor only — tooba-worker-01

Channel:
tooba-main

Protocol:
BRIDGE-WAKE-V1

Rules:

Worker executes only when user explicitly sends a task.

Worker claims exact Task-ID.

Worker executes only that task.

Worker sends canonical Result through Bridge.

Worker then STOPS completely.

No polling.

No fetching the next task automatically.

Never write Worker IDLE.

4. Task File Rule

For every Tooba task file:

filename MUST exactly match Task-ID.

Example:
TB-TMAR-FND-001.task.md

Inside the file:
Task-ID: TB-TMAR-FND-001

Before handing a task to the user, verify filename ↔ Task-ID equality.

5. Git / User-Work Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Protected user-work ancestor:
18ca10c9

Never use destructive recovery over user work:

no git reset

no git clean

no unsafe checkout --

no unsafe restore

no unsafe rebase

no blind stash manipulation

no broad git add .

If pre-existing user work conflicts with a task:
return RECOVERY_CONFLICT.

6. Target Module Architecture

Each business module should converge toward:

src/Modules/<Module>/
  Tooba.<Module>.Domain/
  Tooba.<Module>.Application/
  Tooba.<Module>.Contracts/
  Tooba.<Module>.Infrastructure/

Physical folder moves are NOT the first step.
Ownership and dependency boundaries must be corrected first.

7. Bounded-Context Ownership Rule

A Domain type belongs to the bounded context that owns its:

business invariant

lifecycle

business responsibility

It must NOT be placed in a module merely because:

persistence was convenient there

that module already had a DbContext

UI happened to consume it there

a historical implementation started there

Known suspicious names such as:
StoreAppearance*, StoreLandingPage*, StoreMenu*, StoreCheckout*
are EXAMPLES ONLY.

Ownership audit is repository-wide across ALL *.Domain projects.

8. Persistence Boundaries

Required:

one schema per module

one DbContext per module

migrations owned by the module

no global business DbContext

no cross-schema FK

no cross-module SQL JOIN

no foreign-module DbContext in business write paths

9. Cross-Module Contracts

Target:
Tooba.<Module>.Contracts

Cross-module dependencies should use Contracts/Gates/Events.

Forbidden target state:

Module A.Application → Module B.Application

Module A → Module B.Infrastructure

foreign Domain entities crossing module boundaries

Existing Application→Application debt is legacy and should shrink during TMAR.

10. CQRS + MediatR

All NEW application use-cases must converge to:

HTTP Endpoint
  → ISender
  → Command / Query
  → Handler
  → Domain + Repository/Gates/Contracts

Approved MediatR version:
12.5.0

Validation:
FluentValidation through MediatR pipeline.

Migration strategy:

first wrap existing Directory/Application Services behind handlers

then gradually move orchestration into handlers

narrow/remove Directories only when safe

No Big Bang rewrite.

11. Host Rule

SUPERSEDES_OLD_HOST_THIN_TRANSPORT:
Older Host "HTTP transport / endpoint mapping" ownership for module business routes is superseded by module-owned Endpoints (`HOST-MODULE-ENDPOINT-001`, `ARCH-COMPLETE-001`).

Host must converge to:

process startup / DI / composition root

middleware

global authentication/session/tenant/correlation

platform-level endpoints (health/readiness)

tiny security adapters for module Endpoints

explicit Development bootstrap allowlists

Module owns module HTTP routes, wire DTOs, presentation composition, and ISender dispatch to Application MediatR handlers.

Host must NOT gain new:

module-owned business routes

business writes

SaveChanges

transactions

pricing decisions

inventory decisions

seller/buy-box decisions

campaign eligibility decisions

domain ownership

Legacy Host debt is migrated in waves.

12. Read-Side Microservice Readiness

Cross-module reads should converge to contracts/gateways:

Query Handler / Composer
  → ICatalogReadGateway
  → IPricingReadGateway
  → IInventoryReadGateway
  → ...

Today:
in-process implementations are allowed.

Future:
same contracts may be backed by HTTP/gRPC/read services.

Host/composers must compose authoritative projections, not calculate business truth.

13. Clock and ID

Time:
use canonical IClock at Application/Infrastructure orchestration boundaries.

Pure Domain methods may receive now explicitly.

ID:
use canonical IIdGenerator / UUID abstraction.
UUIDv7 implementation may live behind the abstraction.

Do not inject IClock into every Domain entity.
Do not add interfaces around pure deterministic helpers merely for style.

14. Errors + Localization

Domain/Application errors must be semantic and stable.

Target:
catalog.category.invalid_slug

NOT:
hardcoded Persian/English user-facing Domain messages.

HTTP boundary maps semantic errors to localized ProblemDetails.detail.

Locale design must support unlimited locales, not only fa/en.

Locale normalization/fallback must be centralized, not duplicated across features.

15. Cache

Canonical abstractions:

ICache

ICacheKeyBuilder

ICacheInvalidator

No new direct IMemoryCache bypass.

Redis is a future provider, not a module dependency.

Redis work must also cover:

distributed invalidation

stampede protection

jittered TTL

tenant/store/locale-aware keys

metrics

16. Architecture Enforcement

Architecture rules must be enforced through tests/CI, not only documentation.

Important guards include:

Domain ↛ Infrastructure/Host

Application ↛ foreign Infrastructure

Infra A ↛ Infra B

no new App→App edges

no new Host business writes

no new direct system clock in protected layers

no new direct UUID implementation calls in protected layers

no new localized Domain exception text

no new direct IMemoryCache bypass

no cross-schema FK/JOIN

Legacy debt should be explicitly baselined and the baseline should only shrink.

17. Migration Strategy

Never Big Bang.

Order:

Architecture Foundation

Host dangerous-write removal

CQRS adoption

Contracts extraction

Domain ownership corrections

Read-gateway migration

Error/locale standardization

Cache adoption cleanup

Physical folder reorganization

Microservice-readiness verification

New code follows target architecture immediately after Foundation PASS.
Old code migrates when touched, high-risk, or extraction-critical.

18. Recovery Trigger

If a chat/session is lost, user can say:

برگردیم به TMAR

or:

Bootstrap Tooba from TOOBA-ARCHITECT-BOOTSTRAP.md

Then restore:

program = TMAR

last product task = TB-P10-T022-R21

baseline = TB-TMAR-ARCH-BASELINE

foundation = TB-TMAR-FND-001 ACCEPTED

host-wave-1-slice-1 = TB-TMAR-HOST-W1 + TB-TMAR-HOST-W1-R1 PASS (handlers in Application)

host-wave-2 = TB-TMAR-HOST-W2 PASS (Store Menu CQRS)

boundary-v1 = TB-TMAR-BOUNDARY-V1 PASS (claims verified; foreign Domain edges frozen)

boundary-v1-r1 = TB-TMAR-BOUNDARY-V1-R1 PASS (source-size + Infra→foreign Application frozen)

contracts-w1 = TB-TMAR-CONTRACTS-W1 PASS (Offer.Contracts + Wallet.Contracts; Domain/Infra foreign Domain baselines empty)

contracts-w2 = TB-TMAR-CONTRACTS-W2 PASS (Wallet payment port + Offer lookup; ARCH-TX-001)

contracts-w3 = TB-TMAR-CONTRACTS-W3 PASS (Returns Wallet refund port + Order→Offer.Contracts)

contracts-w4 = TB-TMAR-CONTRACTS-W4 PASS (Cart→Offer.Contracts + Tax.Contracts calculator)

contracts-w5 = TB-TMAR-CONTRACTS-W5 PASS (Inventory→Offer.Contracts + Pricing.Contracts lookup)

contracts-w6 = TB-TMAR-CONTRACTS-W6 PASS (Promotion→Offer.Contracts + Cart→Pricing.Contracts; FE READY)
fe-baseline = TB-TMAR-FE-BASELINE PASS (FE architecture baseline + guards; locks FE-ARCH/SIZE/SEO/BOUNDARY)
fe-f1 = TB-TMAR-FE-F1 PASS (flat freezes + discovery + admin-languages feature slice)
fe-admin-w1 = TB-TMAR-FE-ADMIN-W1 PASS (admin-promotions feature; admin-api shrink)
fe-admin-w2 = TB-TMAR-FE-ADMIN-W2 PASS (admin-reviews feature; admin-api shrink; pattern PROVEN)
fe-admin-w3 = TB-TMAR-FE-ADMIN-W3 PASS (admin-sellers feature; admin-api shrink; Flat CONTINUE)
fe-admin-w4 = TB-TMAR-FE-ADMIN-W4 PASS (admin-customers feature; admin-api shrink; Flat CONTINUE)
fe-admin-w5 = TB-TMAR-FE-ADMIN-W5 PASS (admin-receipts feature; screens→894; Flat exit NOT_READY; Priority FE_ADMIN)
fe-admin-w6 = TB-TMAR-FE-ADMIN-W6 PASS (admin-dashboard feature; screens→810; Flat exit READY_TO_PIVOT; Priority HOST)
host-w3 = TB-TMAR-HOST-W3 PASS (StoreAppearanceSettings CQRS; Host-write baseline shrink; CONTINUE_HOST)
host-w4 = TB-TMAR-HOST-W4 PASS (QuantitySettings CQRS; Host-write baseline shrink; CONTINUE_HOST)
host-w5 = TB-TMAR-HOST-W5 PASS (UnitOfMeasure CQRS; Host exit NOT_READY; CONTINUE_HOST)
host-w6 = TB-TMAR-HOST-W6 PASS (ShippingService CQRS; Host exit READY_TO_PIVOT; Priority CHECKOUT_DESIGN)
checkout-consistency-design = TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN PASS (design+locks; READY_FOR_IMPLEMENTATION_W1; Priority CHECKOUT_IMPLEMENTATION)
checkout-impl-w1 = TB-TMAR-CHECKOUT-IMPL-W1 PASS (process state + idempotency; TX preserved; W2 READY; Priority CHECKOUT_IMPLEMENTATION)
checkout-impl-w2 = TB-TMAR-CHECKOUT-IMPL-W2 PASS (PM + Inventory.Contracts; TX preserved; W3 READY)
checkout-impl-w3 = TB-TMAR-CHECKOUT-IMPL-W3 PASS (Cart.Contracts conversion; TX preserved; W4 READY)
host-structure-w1 = TB-TMAR-HOST-STRUCTURE-W1 PASS (folder map + guards; READY_TO_PAUSE; next CHECKOUT-IMPL-W4)
host-structure-w1-r1 = TB-TMAR-HOST-STRUCTURE-W1-R1 PASS (durable locks + FE freeze; BACKEND_ONLY; next CHECKOUT-IMPL-W4)
checkout-impl-w4 = TB-TMAR-CHECKOUT-IMPL-W4 PASS (Order.Infra Inventory lifecycle Contracts; Infra→Inventory.Application removed; W5 READY)
checkout-impl-w5 = TB-TMAR-CHECKOUT-IMPL-W5 PASS (Promotion checkout Contracts; Order.App→Promotion.Application removed; W6 READY)
offer-reference-w1 = TB-TMAR-OFFER-REFERENCE-W1 PASS then REOPENED (physical mismatch)
offer-reference-w1-r1 = TB-TMAR-OFFER-REFERENCE-W1-R1 PASS (Physical VERIFIED_ON_DISK; ARCH-MODULE-PHYSICAL-001; COMPLETE_REFERENCE_PATTERN revalidated; Checkout paused at W5; R1-era Pricing gate closed by TB-TMAR-REFBATCH-TP-001)
offer-reference-w1-r2 = TB-TMAR-OFFER-REFERENCE-W1-R2 PASS (semantic/determinism residual repair; COMPLETE_REFERENCE_PATTERN)
offer-reference-w1-r6 = TB-TMAR-OFFER-REFERENCE-W1-R6 historically COMPLETE then REOPENED_WAITING_CENTRAL_FOUNDATION
fnd-obserr-001-r1 = TB-TMAR-FND-OBSERR-001-R1 FOUNDATION_PHASE1_COMPLETE (Correlation/SafeErrorMapper/ApiResponseFactory/locale)
fnd-obserr-001-r2 = TB-TMAR-FND-OBSERR-001-R2 FOUNDATION_RUNTIME_TRACING_COMPLETE (runtime tracing/messaging; next R3)
fnd-obserr-001-r3 = TB-TMAR-FND-OBSERR-001-R3 FOUNDATION_ERROR_LOCALIZATION_COMPLETE (catalog/localization/presentation; next R4)
fnd-obserr-001-r4 = TB-TMAR-FND-OBSERR-001-R4 FOUNDATION_COMPLETE (integrated verification + Offer Golden reverify)
tax-reference-w1 = TB-TMAR-TAX-REFERENCE-W1 PASS, revalidated COMPLETE_REFERENCE_PATTERN by TB-TMAR-REFBATCH-TP-001
pricing-reference-w1 = TB-TMAR-PRICING-REFERENCE-W1 PASS, revalidated COMPLETE_REFERENCE_PATTERN by TB-TMAR-REFBATCH-TP-001 (USER_REVIEW_OFFER gate removed)
refbatch-tp-001 = TB-TMAR-REFBATCH-TP-001 REFERENCE_BATCH_COMPLETE (architecture cleanup)
fnd-result-001-r1 = TB-TMAR-FND-RESULT-001-R1 RESULT_PATTERN_FOUNDATION_COMPLETE; Offer COMPLETE_REFERENCE_PATTERN with Result/ApiResponseFactory
refbatch-tp-result-001 = TB-TMAR-REFBATCH-TP-RESULT-001 REFERENCE_RESULT_DELTA_COMPLETE; Tax/Pricing COMPLETE_REFERENCE_PATTERN vs Result Golden
next-module-batch-001 = TB-TMAR-NEXT-MODULE-BATCH-001 NEXT_REFERENCE_BATCH_COMPLETE; Inventory/Promotion historically COMPLETE — SUPERSEDED by ARCH-COMPLETE-001 reopen statuses (HISTORICAL)
next task = TB-TMAR-GOLDEN-WAVE-FINAL-CLOSURE-001; Inventory COMPLETE_REFERENCE_PATTERN as INTERNAL_ONLY / NOT_APPLICABLE / INTERNAL_USE_CASE_BOUNDARIES by TB-TMAR-INVENTORY-APPLICABILITY-REVERIFY-001; Offer remains COMPLETE_REFERENCE_PATTERN at implementation 813184b90906489b5654694b60afc96c4803cd3d; Checkout remains PAUSED_AT_SAFE_W5_CHECKPOINT; Frontend frozen
recovery-lock-harden-001 = TB-TMAR-RECOVERY-LOCK-HARDEN-001 ARCH-COMPLETE-001 + durable multi-module Host endpoint guard + tmar-current-state.json

primary goal = painless future Microservice migration

Always prefer current repository Recovery SoT over stale chat memory.

19. Canonical Recovery Files

The repository should maintain:

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TOOBA-CAPABILITY-MAP.md

docs/architecture/TOOBA-DOMAIN-OWNERSHIP.yaml (after TMAR ownership task)

docs/architecture/TMAR-architecture-locks.md

per-task docs/evidence/<Task-ID>/recovery-sot.md

These files are the durable source of truth; chat memory is secondary.
