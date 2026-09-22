TOOBA TMAR MASTER RECOVERY

Purpose

This file is the durable recovery entry point for the Tooba architecture program.
If chat/session context is lost, this file plus repository architecture docs are the source of truth.

Primary Goal

Tooba must remain a strict Modular Monolith today and be continuously prepared for low-friction, incremental migration to Microservices later.

The goal is NOT a rewrite.

The goal is:

preserve working product behavior

enforce bounded-context ownership

eliminate cross-module structural leakage

move module communication behind stable Contracts/Gates/Events

keep Host as composition root (DI/middleware/global auth/session/tenant/platform endpoints) with module-owned HTTP Endpoints for business routes

use CQRS + MediatR for new application use-cases and for all HTTP use-cases of COMPLETE HTTP-owning modules (ARCH-COMPLETE-001 / ARCH-CQRS-001)

centralize time, ID, error/localization and cache abstractions

prevent giant-file and architecture debt growth

redesign cross-service consistency before physical extraction where shared ACID transactions exist

Worker Protocol

Architect: ChatGPT
Worker: Cursor only (tooba-worker-01)
Channel: tooba-main
Protocol: BRIDGE-WAKE-V1

Worker lifecycle:

User manually gives exact .task.md to Cursor.

Worker executes only that Task-ID.

Worker returns canonical Result through Bridge.

Worker STOPS.

No polling / no automatic next task / no Worker IDLE.

Task-file rule:
filename MUST equal Task-ID exactly.

Repository Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Protected user-work ancestor:
18ca10c9

Never:

git reset

git clean

destructive checkout/restore

unsafe rebase

blind stash manipulation

broad git add .

If user work conflicts:
return RECOVERY_CONFLICT.

Durable Architecture Sources

Inside repository:

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TMAR-architecture-locks.md

docs/architecture/TOOBA-CAPABILITY-MAP.md

docs/architecture/TOOBA-MICROSERVICE-MIGRATION-NOTES.md (must be copied in by next TMAR task if not already present)

latest docs/evidence/<Task-ID>/recovery-sot.md

External reference copies:

D:\Users\User\source\repos\SarvNewVerRequirment\reference\Tooba-Architect-Bootstrap.md

D:\Users\User\source\repos\SarvNewVerRequirment\reference\TOOBA-MICROSERVICE-MIGRATION-NOTES.md

Repository docs override stale chat memory.

Product / TMAR State

Last Product Task:
TB-P10-T022-R21

Accepted TMAR:

TB-TMAR-ARCH-BASELINE

TB-TMAR-FND-001

TB-TMAR-HOST-W1-R1

TB-TMAR-HOST-W2

TB-TMAR-BOUNDARY-V1

TB-TMAR-BOUNDARY-V1-R1

TB-TMAR-CONTRACTS-W1

TB-TMAR-CONTRACTS-W2

TB-TMAR-CONTRACTS-W3

TB-TMAR-CONTRACTS-W4

TB-TMAR-CONTRACTS-W5

TB-TMAR-CONTRACTS-W6

TB-TMAR-FE-BASELINE

TB-TMAR-FE-F1

TB-TMAR-FE-ADMIN-W1

TB-TMAR-FE-ADMIN-W2

TB-TMAR-FE-ADMIN-W3

TB-TMAR-FE-ADMIN-W4

TB-TMAR-FE-ADMIN-W5

TB-TMAR-FE-ADMIN-W6

TB-TMAR-HOST-W3

TB-TMAR-HOST-W4

TB-TMAR-HOST-W5

TB-TMAR-HOST-W6

TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN

TB-TMAR-CHECKOUT-IMPL-W1

TB-TMAR-CHECKOUT-IMPL-W2

TB-TMAR-CHECKOUT-IMPL-W3

TB-TMAR-CHECKOUT-IMPL-W4

TB-TMAR-CHECKOUT-IMPL-W5

TB-TMAR-OFFER-REFERENCE-W1

TB-TMAR-OFFER-REFERENCE-W1-R1
TB-TMAR-OFFER-REFERENCE-W1-R3 — CQRS and contract-boundary repair in progress. Module-Recovery-State: IN_PROGRESS_REFERENCE_REPAIR. Checkout remains paused. Next: TB-TMAR-OFFER-REFERENCE-W1-R4.

TB-TMAR-TAX-REFERENCE-W1

TB-TMAR-PRICING-REFERENCE-W1

TMAR-Execution-Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

Frontend remains frozen until explicit Architect/User release.
Do not modify production frontend code.

Current Product Resume Gate:
SAFE_WITH_TMAR_PARALLEL

User choice:
Continue TMAR for now until user explicitly says to return to product feature work.

Next TMAR task:
TB-TMAR-GOLDEN-WAVE-FINAL-CLOSURE-001

Current recovery state (authoritative — TB-TMAR-INVENTORY-APPLICABILITY-REVERIFY-001):

COMPLETE_REFERENCE_PATTERN (HTTP-owning, module Endpoints + MediatR):
- Cart — TB-TMAR-CART-GOLDEN-001-R1 — 35198728bf17381eaaec5db1e8033478675397fb
- Settlement — TB-TMAR-SETTLEMENT-GOLDEN-001 — f450523d08f1d42a00e28f8972a509bc202516b0
- Fulfillment — TB-TMAR-FULFILLMENT-GOLDEN-001 — 37180cc4df674e1269c1c103647ab5c96aacf64a
- Returns — TB-TMAR-RETURNS-GOLDEN-001 — 9b4bdedf0d2301c5455cf9c0af7d69f3b37039b5
- Notification — TB-TMAR-NOTIFICATION-GOLDEN-001 — 5c947708af5c66a3031786ccdfc34a726ec8746e
- Support — TB-TMAR-SUPPORT-GOLDEN-001 — b2d3e6f7df85750b5b5d9c42b19f3fa996ba5d91
- Wallet — TB-TMAR-WALLET-GOLDEN-001 — f81c11e9b21c4bb5e05385b253db28fdb1c62402
- Payment — TB-TMAR-PAYMENT-GOLDEN-001-R2 — f73b04f516a915f7f182be94d7c6829c86d2de9f
- Promotion — TB-TMAR-PROMOTION-GOLDEN-001 — 431ca6d21b21fa3af0972a1dafa0c85003abe662
- Offer — TB-TMAR-OFFER-FINAL-REVERIFY-001 — 813184b90906489b5654694b60afc96c4803cd3d

COMPLETE_REFERENCE_PATTERN (internal-only, no HTTP endpoint ownership):
- Inventory — TB-TMAR-INVENTORY-APPLICABILITY-REVERIFY-001 — INTERNAL_ONLY / NOT_APPLICABLE / INTERNAL_USE_CASE_BOUNDARIES
- Offer — TB-TMAR-OFFER-FINAL-REVERIFY-001 — implementation 813184b90906489b5654694b60afc96c4803cd3d

Remaining:
- Inventory — NEEDS_APPLICABILITY_REVERIFY (may be internal-only)

Preserved:
- Checkout = PAUSED_AT_SAFE_W5_CHECKPOINT
- Tax = UNTOUCHED in this current repair wave
- Pricing = UNTOUCHED in this current repair wave
- Frontend = BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

Machine-readable: docs/architecture/tmar-current-state.json
Recovery phrase: برگردیم به TMAR؛ TOOBA-TMAR-MASTER-RECOVERY.md و آخرین recovery-sot را مبنا بگیر.

HISTORICAL / SUPERSEDED (do not treat as current authoritative COMPLETE):
Premature Payment COMPLETE / next Promotion wording superseded by R1 Result contract (IN_PROGRESS_GOLDEN_R2_READY).
TB-TMAR-NEXT-MODULE-BATCH-001 Inventory/Promotion COMPLETE claims and next-task TB-TMAR-NEXT-MODULE-BATCH-002 are superseded by the golden wave above and ARCH-COMPLETE-001.

Inventory + Promotion reference batch (HISTORICAL / SUPERSEDED):
TB-TMAR-NEXT-MODULE-BATCH-001 — Host Inventory/Promotion DbContext removed; Contracts query/schema ports; IClock/IIdGenerator; seller SetInventoryAsync Result; Domain stable codes; historically claimed COMPLETE_REFERENCE_PATTERN — SUPERSEDED by ARCH-COMPLETE-001 reopen statuses. Evidence: docs/evidence/TB-TMAR-NEXT-MODULE-BATCH-001/.

Tax + Pricing Result Pattern delta:
TB-TMAR-REFBATCH-TP-RESULT-001 — Pricing seller-write expected failures return Result.Failure (amount/offer/market/currency/overlap); Tax RESULT_DELTA_NOT_APPLICABLE (TaxOutcome remains canonical); Module-Recovery-State REFERENCE_RESULT_DELTA_COMPLETE; Tax/Pricing COMPLETE_REFERENCE_PATTERN. Evidence: docs/evidence/TB-TMAR-REFBATCH-TP-RESULT-001/.

Result Pattern Foundation + Offer Golden:
TB-TMAR-FND-RESULT-001-R1 — Offer COMPLETE was REOPENED for missing Result pattern; BuildingBlocks Result/Result&lt;T&gt; + ApiResponseFactory success/failure mapping; Offer seller CQRS/endpoints adopted; Pricing/Inventory seller-write gateways return Result; Module-Recovery-State RESULT_PATTERN_FOUNDATION_COMPLETE; Offer-State COMPLETE_REFERENCE_PATTERN. Evidence: docs/evidence/TB-TMAR-FND-RESULT-001-R1/.

Tax + Pricing reference revalidation:
TB-TMAR-REFBATCH-TP-001 — REFERENCE_BATCH_COMPLETE architecture cleanup; Result Golden delta closed by TB-TMAR-REFBATCH-TP-RESULT-001. Evidence: docs/evidence/TB-TMAR-REFBATCH-TP-001/.

Foundation Observability/Error Presentation R4:
TB-TMAR-FND-OBSERR-001-R4 — Final integrated verification. Module-Recovery-State FOUNDATION_COMPLETE (observability/error). Offer historically COMPLETE then REOPENED by Result gap. Evidence: docs/evidence/TB-TMAR-FND-OBSERR-001-R4/.

Foundation Observability/Error Presentation R3:
TB-TMAR-FND-OBSERR-001-R3 — Error localization/catalog complete; Module-Recovery-State FOUNDATION_ERROR_LOCALIZATION_COMPLETE; Offer-State READY_FOR_FINAL_REFERENCE_REVERIFY. Evidence: docs/evidence/TB-TMAR-FND-OBSERR-001-R3/.

Foundation Observability/Error Presentation R2:
TB-TMAR-FND-OBSERR-001-R2 — Runtime tracing complete; Module-Recovery-State FOUNDATION_RUNTIME_TRACING_COMPLETE; Offer-State REOPENED_WAITING_CENTRAL_FOUNDATION. Evidence: docs/evidence/TB-TMAR-FND-OBSERR-001-R2/.

Foundation Observability/Error Presentation R1:
TB-TMAR-FND-OBSERR-001-R1 — Phase 1 Core observability/error presentation; Module-Recovery-State FOUNDATION_PHASE1_COMPLETE; Offer-State REOPENED_WAITING_CENTRAL_FOUNDATION. Evidence: docs/evidence/TB-TMAR-FND-OBSERR-001-R1/.

Offer Reference Module W1-R1:
TB-TMAR-OFFER-REFERENCE-W1-R1 — prior Offer COMPLETE REOPENED on visual evidence; physical folders/namespaces aligned; ARCH-MODULE-PHYSICAL-001; Physical-Structure-State VERIFIED_ON_DISK; COMPLETE_REFERENCE_PATTERN revalidated. Evidence: docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R1/.
TB-TMAR-OFFER-REFERENCE-W1-R2 — Offer Golden residual repair: SemanticException codes, IIdGenerator/IClock determinism, full residual scan CLEAN; COMPLETE_REFERENCE_PATTERN. Evidence: docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R2/.
TB-TMAR-OFFER-REFERENCE-W1-R3 — prior COMPLETE claim revoked; CQRS/contract-boundary repair implemented. Module-Recovery-State: IN_PROGRESS_REFERENCE_REPAIR. Next: TB-TMAR-OFFER-REFERENCE-W1-R4. Evidence: docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R3/.

Pricing Reference Module W1:
TB-TMAR-PRICING-REFERENCE-W1 — historically shipped COMPLETE_REFERENCE_PATTERN, then revalidated by TB-TMAR-REFBATCH-TP-001. The stale "wait for USER_REVIEW_OFFER" gate is removed. Evidence: docs/evidence/TB-TMAR-PRICING-REFERENCE-W1/ and docs/evidence/TB-TMAR-REFBATCH-TP-001/.

Tax Reference Module W1:
TB-TMAR-TAX-REFERENCE-W1 — COMPLETE_REFERENCE_PATTERN revalidated by TB-TMAR-REFBATCH-TP-001. Evidence: docs/evidence/TB-TMAR-TAX-REFERENCE-W1/ and docs/evidence/TB-TMAR-REFBATCH-TP-001/.

Offer Reference Repair R4:
TB-TMAR-OFFER-REFERENCE-W1-R4 — fake CQRS and Host Offer BFF removed; owner contract gates established; Module-Recovery-State READY_FOR_FINAL_VERIFICATION; next TB-TMAR-OFFER-REFERENCE-W1-R5. Evidence: docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R4/.

Offer Reference Final Verification R5:
TB-TMAR-OFFER-REFERENCE-W1-R5 — magic offer.not_found exception seam repaired (SemanticException); seller Offer CQRS/guards green; Host Admin/Storefront OfferDbContext residual blocks COMPLETE. Module-Recovery-State: INCOMPLETE. Next: TB-TMAR-OFFER-REFERENCE-W1-R6. Evidence: docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R5/.

Offer Reference Host Persistence Closure R6:
TB-TMAR-OFFER-REFERENCE-W1-R6 — Host/foreign production OfferDbContext leaks removed via IOfferQueryGateway + Offer.Infrastructure adapter; composers/grids/storefront/merch/reservation/seeds rewired; architecture guards green. Historically COMPLETE_REFERENCE_PATTERN then REOPENED for cross-cutting observability/error presentation gap. Offer-State: REOPENED_WAITING_CENTRAL_FOUNDATION. Evidence: docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R6/.

Foundation Observability/Error Presentation R3:
TB-TMAR-FND-OBSERR-001-R3 — ErrorDescriptor catalog + SafeErrorMapper without naming heuristics; Offer .resx localization; IExceptionPresentationService; Offer seller endpoints bubble to global pipeline; Module-Recovery-State FOUNDATION_ERROR_LOCALIZATION_COMPLETE; next TB-TMAR-FND-OBSERR-001-R4. Evidence: docs/evidence/TB-TMAR-FND-OBSERR-001-R3/.

Foundation Observability/Error Presentation R2:
TB-TMAR-FND-OBSERR-001-R2 — Correlation middleware + request log enrichment + TracingBehavior + IModuleCallTracer + Offer gateway topology + MassTransit/outbox correlation; Module-Recovery-State FOUNDATION_RUNTIME_TRACING_COMPLETE; next TB-TMAR-FND-OBSERR-001-R3. Evidence: docs/evidence/TB-TMAR-FND-OBSERR-001-R2/.

Foundation Observability/Error Presentation R1:
TB-TMAR-FND-OBSERR-001-R1 — Correlation + SafeErrorMapper + ApiResponseFactory + locale resolver + ProblemDetails context; Host wired; Offer endpoints use central path; Module-Recovery-State FOUNDATION_PHASE1_COMPLETE; next TB-TMAR-FND-OBSERR-001-R2. Evidence: docs/evidence/TB-TMAR-FND-OBSERR-001-R1/.

Checkout Implementation W5:
TB-TMAR-CHECKOUT-IMPL-W5 — Promotion checkout seam (ICheckoutPromotionPort); Order.Application↛Promotion.Application; TX preserved; W6 READY (intentionally paused); FE freeze intact. Evidence: docs/evidence/TB-TMAR-CHECKOUT-IMPL-W5/.

Checkout Implementation W4:
TB-TMAR-CHECKOUT-IMPL-W4 — Order.Infrastructure Cancel/Restore/PaymentBridge behind Inventory.Contracts (IOrderInventoryLifecyclePort); Infra→Inventory.Application removed; TX preserved; W5 READY; FE freeze intact. Evidence: docs/evidence/TB-TMAR-CHECKOUT-IMPL-W4/.

Host Structure W1:
TB-TMAR-HOST-STRUCTURE-W1 — Host root responsibility folders + HOST-FOLDER-001/HOST-HYGIENE-001; READY_TO_PAUSE; Checkout-W4 resume READY. Evidence: docs/evidence/TB-TMAR-HOST-STRUCTURE-W1/.

Checkout Implementation W3:
TB-TMAR-CHECKOUT-IMPL-W3 — Cart.Contracts conversion seam (ICartConversionPort); Order.Application↛Cart.Application; TX preserved; W4 READY; Orders FE STILL_WAITING_FOR_BACKEND_W4. Evidence: docs/evidence/TB-TMAR-CHECKOUT-IMPL-W3/.

Checkout Implementation W2:
TB-TMAR-CHECKOUT-IMPL-W2 — in-process Process Manager + Inventory.Contracts reservation seam; TX preserved; Order.Application↛Inventory.Application; W3 READY; Orders FE STILL_WAITING_FOR_BACKEND_W3. Evidence: `docs/evidence/TB-TMAR-CHECKOUT-IMPL-W2/`.

Checkout Implementation W1:
TB-TMAR-CHECKOUT-IMPL-W1 — durable Order-owned checkout process state + submission idempotency; TransactionScope preserved; no Saga runtime; W2 READY; Orders FE STILL_WAITING_FOR_BACKEND_W2; Architecture-Priority CHECKOUT_IMPLEMENTATION. Evidence: `docs/evidence/TB-TMAR-CHECKOUT-IMPL-W1/`.

Checkout Consistency Design:
TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN — design+locks only; Order-owned Process Manager; Checkout-Point-Of-No-Return MULTI_STAGE; READY_FOR_IMPLEMENTATION_W1; Architecture-Priority CHECKOUT_IMPLEMENTATION. Evidence: `docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/`.

Host W6:
TB-TMAR-HOST-W6 — ShippingService Create/Update/Deactivate/EnsureSeed → Fulfillment Application/Directory; Host-Exit-State READY_TO_PIVOT; Architecture-Priority CHECKOUT_DESIGN; Checkout design READY. Evidence: `docs/evidence/TB-TMAR-HOST-W6/`.

Host W5:
TB-TMAR-HOST-W5 — UnitOfMeasure Host writes → Catalog Application/Directory; Host-Exit-State NOT_READY; CONTINUE_HOST. Evidence: `docs/evidence/TB-TMAR-HOST-W5/`.

Host W4:
TB-TMAR-HOST-W4 — QuantitySettings Host write → Catalog Application/Directory; Host-write baseline shrink; CONTINUE_HOST. Evidence: `docs/evidence/TB-TMAR-HOST-W4/`.

Host W3:
TB-TMAR-HOST-W3 — StoreAppearanceSettings Host write → Catalog Application/Directory; Host-write baseline shrink; CONTINUE_HOST. Evidence: `docs/evidence/TB-TMAR-HOST-W3/`.

Frontend ADMIN-W6:
TB-TMAR-FE-ADMIN-W6 — admin-dashboard migrated; admin-api 1022→1002; exports 38→35; admin-screens 894→810; Flat exit READY_TO_PIVOT; Architecture-Priority HOST. Evidence: `docs/evidence/TB-TMAR-FE-ADMIN-W6/`.

Frontend ADMIN-W5:
TB-TMAR-FE-ADMIN-W5 — admin-receipts migrated; admin-api 1069→1022; admin-screens 1036→894; Flat exit NOT_READY. Evidence: `docs/evidence/TB-TMAR-FE-ADMIN-W5/`.

Frontend ADMIN-W4:
TB-TMAR-FE-ADMIN-W4 — admin-customers migrated; admin-api 1107→1069; admin-screens 1057→1036. Evidence: `docs/evidence/TB-TMAR-FE-ADMIN-W4/`.

Frontend ADMIN-W3:
TB-TMAR-FE-ADMIN-W3 — admin-sellers migrated; admin-api 1145→1107; admin-screens 1078→1057. Evidence: `docs/evidence/TB-TMAR-FE-ADMIN-W3/`.

Frontend ADMIN-W2:
TB-TMAR-FE-ADMIN-W2 — admin-reviews migrated; admin-api 1234→1145; admin-screens 1120→1078; pattern PROVEN. Evidence: `docs/evidence/TB-TMAR-FE-ADMIN-W2/`.

Frontend ADMIN-W1:
TB-TMAR-FE-ADMIN-W1 — admin-promotions migrated; admin-api 1321→1234; admin-screens 1230→1120. Evidence: `docs/evidence/TB-TMAR-FE-ADMIN-W1/`.

Confirmed Architecture Facts

Foundation:

MediatR 12.5.0

FluentValidation pipeline

IClock

IIdGenerator / UUIDv7 abstraction

SemanticError foundation

architecture freeze guards

Confirmed cross-module debt:

Cart.Domain → Offer.Domain: removed in CONTRACTS-W1

Order.Domain → Offer.Domain: removed in CONTRACTS-W1

Pricing.Domain → Offer.Domain: removed in CONTRACTS-W1

Payment.Infrastructure → Wallet.Domain: removed in CONTRACTS-W1

Payment.Infrastructure → Wallet.Application: removed in CONTRACTS-W2 (IWalletOrderPaymentPort)

Returns.Infrastructure → Wallet.Application: removed in CONTRACTS-W3 (IWalletRefundCreditPort)

Order.Application → Offer.Application: removed in CONTRACTS-W3 (SalesChannel via Offer.Contracts)

Cart.Application → Offer.Application: removed in CONTRACTS-W4 (SalesChannel via Offer.Contracts)

Order.Application → Tax.Application: removed in CONTRACTS-W4 (ITaxCalculator via Tax.Contracts)

Inventory.Application → Offer.Application: removed in CONTRACTS-W5 (Offer.Contracts)

Order.Application → Pricing.Application: removed in CONTRACTS-W5 (IPriceLookupGateway via Pricing.Contracts)

Promotion.Application → Offer.Application: removed in CONTRACTS-W6 (Offer.Contracts)

Cart.Application → Pricing.Application: removed in CONTRACTS-W6 (Pricing.Contracts + campaign authority)

Order.Application is a synchronous hub with remaining foreign Application dependencies

legacy App→App edges exist and are frozen against expansion

legacy Infra→foreign Application edges exist and are frozen against expansion

Host:

StoreLandingPage Host direct writes removed

StoreMenu Host direct writes removed

many legacy Host write sites still remain

new Host business writes/decisions are frozen

Domain:

repository-wide Domain ownership audit exists

some types are likely in wrong bounded contexts

Catalog currently contains transitional StoreAppearance / Landing / Menu / other ownership debt

no Big Bang Domain move

God files:

repository-wide source-size inventory exists

55 oversized legacy files baselined

new handwritten files >800 LOC are rejected

oversized legacy files may not grow above baseline

critical decomposition requires characterization tests first

Contracts Target

Cross-module public boundaries converge toward:
Tooba.<Module>.Contracts

Foreign Application / Domain / Infrastructure must not become public module APIs.

No 31-project Big Bang extraction.
Contracts are extracted in waves by verified coupling and extraction value.

CQRS Target

New use cases:
HTTP Endpoint
→ ISender
→ Command/Query
→ Handler in Application
→ Domain + abstractions
→ Infrastructure implementation

Business MediatR handlers must not live in Infrastructure.

Host Target

SUPERSEDES_OLD_HOST_THIN_TRANSPORT:
Older wording that Host may own generic HTTP transport/endpoint implementation for module business routes is superseded. Module-owned `Tooba.*.Endpoints` is mandatory for HTTP-owning COMPLETE modules (`HOST-MODULE-ENDPOINT-001`, `ARCH-COMPLETE-001`).

Host owns:

process startup / DI / composition root

middleware

global authentication/session/tenant/correlation

platform-level endpoints such as health/readiness

tiny security adapters for module Endpoints

explicit Development bootstrap allowlists

Module owns:

module HTTP route implementation

wire DTOs specific to that module

module success/failure presentation composition

business use-case dispatch via ISender → Application MediatR handlers

Host must not gain:

module-owned business route implementation

direct Directory calls from Host HTTP for module business use-cases

module-specific business response mapping/composers/query engines as durable ownership

business writes / SaveChanges/transactions as module business truth

domain ownership

Domain Ownership Rule

A Domain type belongs to the bounded context that owns its invariant and lifecycle.
Never place a type in a module merely because persistence was convenient.

Examples such as StoreAppearance*, StoreLandingPage*, StoreMenu*, StoreCheckout* are examples only.
Ownership rules apply repository-wide.

Errors / Locale

No hardcoded user-facing localized Domain messages in ANY language.
Domain/Application errors are semantic and stable.
HTTP boundary maps them to localized ProblemDetails.

Locale architecture must support unlimited locales.
Do not hard-code architecture around only FA/EN.

Cache

Canonical:

ICache

ICacheKeyBuilder

ICacheInvalidator

No new direct IMemoryCache bypass.
Redis remains a future provider.

Microservice Migration Critical Note

A full rewrite is NOT required.

Many peripheral modules are relatively extractable after contracts and operational readiness.

The hard area is the Checkout / Cart / Order / Inventory / Payment consistency chain.

Current shared-database TransactionScope patterns cannot be assumed to work after physical service/database separation.

Future extraction requires explicit consistency design:

Saga / Process Manager where appropriate

compensating actions

idempotency

retry semantics

duplicate delivery handling

timeouts/recovery

intermediate states

point-of-no-return

Outbox / Integration Events

DO NOT implement a Saga prematurely.

Canonical rule to add:
No NEW business workflow may depend on one ACID transaction spanning multiple bounded contexts.

Existing cross-context transactions must be inventoried and treated as extraction debt.

Feature Resume Rule

TMAR already reached:
Product-Resume-Safety: SAFE_WITH_TMAR_PARALLEL

Therefore product work MAY resume when the user says so.

Until the user says to return to product work:
continue TMAR tasks sequentially.

When product work resumes:

TMAR continues in parallel

all new product code must obey current TMAR locks

no new legacy debt patterns

Recovery Phrase

If chat is lost, user can say:

برگردیم به TMAR — فایل TOOBA-TMAR-MASTER-RECOVERY.md و آخرین recovery-sot را مبنا بگیر

Then:

read this file

read repository bootstrap/locks/capability map/migration notes

read latest recovery-sot

identify last accepted Task-ID

continue from next task without reconstructing from guesses
