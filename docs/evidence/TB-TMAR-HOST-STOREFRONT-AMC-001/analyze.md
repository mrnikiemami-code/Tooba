# TB-TMAR-HOST-STOREFRONT-AMC-001 — Analyze

## Target analyzed

`src/backend/Host/Tooba.Host/Storefront/` — **12 production files**, ~3785 LOC total.

| File | LOC | Primary role |
| --- | ---: | --- |
| `StorefrontComposer.cs` | 1494 | Cross-module storefront read composition (home/PLP/PDP/brands/sellers/merchandising) |
| `StorefrontDemoCatalogBootstrap.cs` | 616 | Development demo seed orchestration |
| `StorefrontModels.cs` | 412 | Host presentation DTOs |
| `FashionTemplatePreviewQuery.cs` | 340 | Fashion template catalog preview read |
| `IndustryTemplatePreviewQuery.cs` | 283 | Industry template catalog preview read |
| `StorefrontDemoCatalogMatrix.cs` | 281 | Demo seed matrix data |
| `StorefrontEndpoints.cs` | 212 | Host HTTP boundary for remaining `/v1/storefront/*` |
| `IndustryPersistedTemplateCatalog.cs` | 51 | Facade over Catalog.Infrastructure.Development template seeds |
| `CheckoutIdentityGate.cs` | 38 | Checkout identity policy from CatalogDbContext + session enforce |
| `StorefrontAccountIdentity.cs` | 35 | Pure display-name helper |
| `HostPaymentStorefrontAuthorizer.cs` | 15 | Thin Payment storefront actor adapter |
| `HostCheckoutActorPolicyAdapter.cs` | 8 | Thin Payment `ICheckoutActorPolicyPort` → gate |

Re-enumerated at analyze end: **12/12 files** covered; folder has no additional production residue.

## Already-evacuated storefront surface (context)

Module Endpoints already own large `/v1/storefront` slices:

- **Order** — checkout / shipping / pending-payment
- **Payment** — storefront payment routes
- **Cart** — storefront cart
- **Catalog** — mega-menu, facets, category-routes, store menus, landing pages
- **Content** — storefront content routes

Host `StorefrontEndpoints` is the **residual read BFF** plus appearance / media / checkout-identity-policy / geography / template preview.

There is **no** `Modules/Storefront` project today.

---

## Structured State Fields

1. **Foundation-State**
   - Catalog: `FOUNDATION_PARTIAL` — already owns several `Endpoints/Storefront` slices, but **Catalog is not** in `tmar-module-structure-manifests.json` / not ARCH-COMPLETE-002 certified. Migration may land into Catalog Storefront capability; do **not** treat that as full Catalog re-certification unless scoped.
   - Content / Order / Cart / Payment: `FOUNDATION_READY` (structure-certified where listed in manifests).
   - Reviews / Promotion / Party: destination exists; Contracts gaps for composer enrichment must be closed during migrate; Reviews/Promotion may be uncertified.
   - Dedicated Storefront BFF module: `FOUNDATION_MISSING` (would require Architect authorization to create)

2. **Ownership-State** — `MUST_SPLIT` (folder and `StorefrontComposer` individually)

3. **File-Cohesion-State**
   - `StorefrontComposer.cs` — `MULTI_RESPONSIBILITY_COHESION_VIOLATION`
   - `StorefrontEndpoints.cs` — `MULTI_RESPONSIBILITY_COHESION_VIOLATION`
   - `StorefrontDemoCatalogBootstrap.cs` — `MULTI_RESPONSIBILITY_COHESION_VIOLATION` (Catalog/Offer/Inventory/Pricing/Tax/Party/Content orchestration)
   - Others — mostly `COHESIVE` within their slice

4. **Oversized/God-File-State**
   - `StorefrontComposer.cs` — `CRITICAL_GOD_FILE` in `tmar-source-size-baseline.json` (~1494–1579 LOC), multi-audience composition
   - `StorefrontDemoCatalogBootstrap.cs` 616 LOC — oversized multi-module seed
   - `StorefrontModels.cs` 412 LOC — presentation dump (split by page/capability with composer)

5. **Localization-State** — `HARDCODED_TEXT`
   - Hard-coded Persian in composer (`"فروشگاه توبا"`, `"کالای واقعی..."`, `"پرفروش‌ها"`, facet `"بله"`/`"خیر"`)
   - Hard-coded English/Persian titles in raw `Results.Json` endpoints

6. **API-Result-Pattern-State** — `RAW_RESULTS`
   - `StorefrontEndpoints` returns `Results.Json(...)` / ad-hoc 404 objects; no `ApiResponseFactory` / `Result<T>`

7. **Stable-Error-Code-State** — `UNREGISTERED_CODES` / mixed
   - Codes such as `storefront.brand.missing`, `storefront.product.missing`, `storefront.category.missing`, `storefront.seller.missing`, `template_catalog.*.missing`, `checkout.authentication_required` (thrown as `InvalidOperationException` message)
   - Not proven catalogue-backed via module `IErrorCatalogContributor`

8. **Logging-State** — `CANONICAL` (no non-standard logger found in this folder)

9. **Sensitive-Logging-State** — `NONE`

10. **OpenTelemetry-State** — `CANONICAL` / not locally owned (no parallel ActivitySource)

11. **Correlation-Trace-State** — `CANONICAL` (no parallel correlation)

12. **CQRS-State** — `MISSING` for Host residual routes (direct composer/query/DbContext from endpoints)

13. **Validator-Coverage-State** — `GAPS`
    - Listing/PLP/detail query params currently parsed ad hoc in endpoints; no FluentValidation classification

14. **Contracts-Boundary-State** — `VIOLATION`

15. **Cross-Module-Coupling-State** — `ILLEGAL`

    Direct foreign Application / Domain / Infrastructure:

    | File | Illegal layers |
    | --- | --- |
    | `StorefrontComposer` | Catalog Application/Domain/Infrastructure (`CatalogDbContext`), Party.Application, Promotion.Application, Reviews.Application, Content.Application+Domain |
    | `CheckoutIdentityGate` | Catalog Domain + Infrastructure (`CatalogDbContext`) |
    | `FashionTemplatePreviewQuery` | Catalog Domain + Infrastructure (+ Development) |
    | `IndustryTemplatePreviewQuery` | Catalog Domain + Infrastructure |
    | `IndustryPersistedTemplateCatalog` | Catalog.Infrastructure.Development |
    | `StorefrontDemoCatalogBootstrap` | Catalog App/Domain/Infra, Inventory App/Domain, Offer.Application, Party.Application, Pricing.Application, Tax App/Domain, Content.Infrastructure |
    | `StorefrontEndpoints` | Catalog.Infrastructure.StoreAppearance, Media.Application (+ Media.Endpoints.Admin serving helper), Order.Application.Storefront.Services (using) |

    Legal Contracts already used by composer: Offer / Pricing / Inventory / Tax Contracts (+ `IPrimaryOfferSelectionPolicy`).

16. **Cross-Module-Join-State** — `NONE` claimed by composer comments (separate reads, no SQL cross-schema JOIN). **Must re-verify** during migrate; template preview files do multi-DbSet joins **inside Catalog** (same module — OK).

17. **Persistence-Ownership-State** — `FOREIGN_ACCESS` / `HOST_OWNED`
    - Host reaches `CatalogDbContext` from Composer / CheckoutIdentityGate / template queries / demo bootstrap
    - Demo bootstrap performs writes (`SaveChanges` residue historically flagged)

18. **Endpoint-Ownership-State** — `HOST_OWNED` residual + `DUPLICATED` group prefix (`/v1/storefront` shared across modules — accepted pattern when routes do not collide)

19. **Host-Residue-State**
    - `REMOVE_TO_CATALOG` — template preview trio, CheckoutIdentityGate (Catalog-owned settings), appearance endpoint coupling, most composer Catalog/PLP/PDP/home/brand/category surfaces
    - `REMOVE_TO_CONTENT` — latest-articles composition slice
    - `REMOVE_TO_REVIEWS` — featured-reviews composition slice
    - `REMOVE_TO_PARTY` — public sellers composition (via Party.Contracts, not Party.Application)
    - `REMOVE_TO_PROMOTION` / Catalog+Promotion Contracts — merchandising
    - `REMOVE_TO_MEDIA` — `/media/{assetId}` serving (already Media capability; Host only proxies)
    - `DEVELOPMENT_SEED` — DemoCatalogBootstrap/Matrix → Catalog/Offer/Inventory Development ownership (no Host/Development sink)
    - `KEEP_AS_EXPLICIT_THIN_HOST_SECURITY_ADAPTER` — `HostPaymentStorefrontAuthorizer` (relocate under `Host/Security/...`, not delete)
    - `KEEP_AS_THIN_HOST_ADAPTER` (after gate move) — `HostCheckoutActorPolicyAdapter` (+ Order’s existing `HostOrderStorefront*` in `Host/Order`, out of this folder but related)
    - `KEEP_AS_HOST_AUTH_PLATFORM` — `StorefrontAccountIdentity` (consumer is `Host/Authentication/AuthenticationHttpBoundary.cs`; relocate under `Host/Authentication/`, not into Catalog)
    - Downstream (outside folder, must update when composer moves): `Host/Wishlist/WishlistComposer.cs`, `Host/CatalogAdapters/StoreLandingShellAdapter.cs` both call `StorefrontComposer.ComposeProductCardsAsync`

20. **Schema-Migration-State** — `UNCHANGED` required (no schema/migration in Storefront AMC)

21. **Behavior-Preservation-Risk** — `HIGH`
    - Public storefront read contract (home/PLP/PDP/filters/facets/cards)
    - Template preview DTOs for Admin/Builder
    - Checkout identity policy semantics (`GuestAllowed` vs `AuthenticatedOnly`)
    - Appearance token envelope
    - Demo seed idempotency (`SentinelProductSlug`)

22. **Canonical-Reference-Used**
    - API/result: Offer Endpoints + `ApiResponseFactory`
    - Security adapter pattern: `Host/Security/Seller` (R1A)
    - Development seed rehome: Catalog.Infrastructure/Development + Host Development closure pattern
    - Cross-module enrichment: Order seller dashboard (`Party.Contracts` only)

23. **Final-Disposition** — `NEEDS_ARCHITECT_DECISION`

---

## Responsibility map

| Responsibility | Classification | Proposed owner |
| --- | --- | --- |
| Home / listing / detail / category PLP / brands / categories | PRESENTATION_COMPOSITION + APPLICATION_USE_CASE | **Catalog** (with Contracts enrichment) |
| Product card composition (Offer/Price/Inventory/Tax/Promotion) | CROSS_MODULE_ORCHESTRATION | Catalog Application consuming **Contracts only** |
| Featured reviews on home | PRESENTATION_COMPOSITION | Reviews (Contracts port) or Catalog reading Reviews.Contracts |
| Latest articles on home | PRESENTATION_COMPOSITION | Content.Contracts |
| Public sellers list/detail | PRESENTATION_COMPOSITION | Party.Contracts |
| Merchandising pages | APPLICATION_USE_CASE | Promotion + Catalog Contracts |
| Fashion/Industry template preview | APPLICATION_USE_CASE + HTTP_ENDPOINT | Catalog Endpoints/Application/Infrastructure Development |
| Store appearance GET | HTTP_ENDPOINT | Catalog (prior Admin wave deferred specifically for Host.Storefront coupling) |
| Media GET `/storefront/media/{id}` | HTTP_ENDPOINT | Media.Endpoints |
| Checkout identity policy GET + enforce | APPLICATION_USE_CASE + AUTHORIZATION_ADAPTER | Catalog policy + Host session adapter |
| Geography provinces static | HTTP_ENDPOINT | Type already lives in **Order.Application** (`StorefrontIranGeography`); Host route is a duplicate seam — decide Order Endpoints vs Catalog static |
| Payment storefront user resolve | AUTHORIZATION_ADAPTER | Keep Host thin security adapter (optionally `Host/Security/Storefront` or `Host/Security/Payment`) |
| Account header display name helper | HOST_AUTH_RUNTIME_PLUMBING | `Host/Authentication` (not Catalog) |
| Demo catalog matrix/bootstrap | DEVELOPMENT_SEED | Catalog/Offer/Inventory Development (not Host/Development sink). Production callers of `StorefrontDemoCatalogBootstrap` are currently test-only (`StorefrontDemoCatalogSeedTests`); still evacuate with seed ownership |

## MUST_SPLIT decisions

1. **`StorefrontComposer`** — split by capability (Home, Listing, Detail, Brand, Seller, Merchandising, ProductCardComposer); foreign Application/Domain/DbContext removed; enrichment via Contracts ports.
2. **`StorefrontEndpoints`** — split routes to owning module Endpoints; Host mapping removed.
3. **`StorefrontModels`** — follow composer capabilities; Application models where CQRS-owned; no Host presentation dump.
4. **`StorefrontDemoCatalogBootstrap`** — split writes by owner module Development seeds; Host orchestration deleted.

## Current illegal dependencies (summary)

- Host → `CatalogDbContext` / Catalog Domain / Catalog Application
- Host → Party.Application, Promotion.Application, Reviews.Application, Content.Application/Domain
- Host → Inventory/Offer/Pricing/Tax Application+Domain in demo bootstrap
- Host → Media.Application from Host endpoint (should be Media Endpoints ownership)
- Raw `Results.Json` + exception-message auth (`checkout.authentication_required`)

## Contracts-only replacement map (migrate)

| Today | Replace with |
| --- | --- |
| `CatalogDbContext` in Host | Catalog-owned queries/handlers; Host ZERO |
| `IPartyLookupGateway` (Party.Application) | `Party.Contracts` lookup (existing Seller dashboard pattern) |
| `IReviewDirectory` / Content directories from Host | Reviews.Contracts / Content.Contracts narrow ports |
| `IPromotionEvaluator` from Host | Promotion.Contracts port if missing |
| `StoreAppearanceProjector` from Host endpoint | Catalog Endpoints query |
| `CheckoutIdentityGate` + CatalogDbContext | Catalog Application query + Host thin session adapter implementing Catalog/Payment ports |
| Demo bootstrap foreign Applications | Module Development seeds + Contracts/ports already used by Development closure |

## CQRS / Endpoints gaps

Every remaining Host storefront GET should become:

`Module.Endpoints` → `ISender` → `IRequest`/`IRequestHandler` → Contracts for foreign data → `ApiResponseFactory`.

Validator matrix (initial):

| Request | Classification |
| --- | --- |
| Home / Categories / Brands list / Sellers list / Appearance / Geography / CheckoutIdentityPolicy | `NO_VALIDATOR_REQUIRED` (no / auth-scoped / no untrusted shape) |
| Brand-by-slug / Seller-by-id / Product-by-slug / Category-PLP / Listing query / Template-by-key | `VALIDATOR_REQUIRED` (slug/key/paging/filter shape) |
| Media-by-assetId | `VALIDATOR_REQUIRED` (Guid) or Media-owned existing rules |

## Prior SoT constraints (must honor)

- Admin W2: `storeAppearanceDisposition = NOT_MOVED_DEFERRED_HOST_STOREFRONT_PROJECTOR_COUPLING` — Storefront AMC is the reopening moment for appearance.
- Admin W23: `checkoutIdentityGate = RETAINED_HOST_STOREFRONT` — Storefront AMC is the reopening moment for the gate.
- Offer residue repair: HostOfferSellerAuthorizer stays thin Host security; composer must keep consuming `IPrimaryOfferSelectionPolicy` Contracts.
- Host/Development is a **closed** allowlist — demo seed must **not** sink into Host/Development.
- Host/Security/Seller pattern is the template for relocating `HostPaymentStorefrontAuthorizer`.

## Exact target paths (recommended, pending Architect lock)

```text
Catalog.Application/Storefront/{Home,Listing,Detail,Brands,Categories,Appearance,CheckoutIdentity}/...
Catalog.Endpoints/Storefront/...
Catalog.Infrastructure/Development/TemplatePreview/...
Catalog.Infrastructure/Development/StorefrontDemo/...   # or split Offer/Inventory Development collaborators
Media.Endpoints/Storefront/MediaServing...
Host/Security/Payment/HostPaymentStorefrontAuthorizer.cs
Host/Security/Payment/HostCheckoutActorPolicyAdapter.cs  # or Host/Security/Checkout
```

Do **not** invent `Modules/Storefront` unless Architect explicitly authorizes a BFF module.

## Behavior-preservation checklist

- All current Host `/v1/storefront` routes, verbs, status codes, DTO field names
- Template preview sample payloads / error codes
- Appearance token envelope
- Checkout identity policy JSON + enforce semantics
- PLP filter query grammar (`f_` / `r_` / `b_`)
- Demo seed sentinel idempotency
- No schema/migration change; frontend unchanged

## Migration order (recommended multi-wave after Architect decision)

| Wave | Scope | Host Storefront delta |
| --- | --- | --- |
| **R1** | Relocate thin security adapters → `Host/Security/...`; evacuate template preview + Industry facade → Catalog; evacuate CheckoutIdentityGate → Catalog + Host adapter; media route → Media.Endpoints; appearance GET → Catalog | files ↓, illegal CatalogDbContext usage ↓ |
| **R2** | Evacuate StorefrontComposer/Models/Endpoints read surface → Catalog CQRS + Contracts enrichment (Party/Content/Reviews/Promotion) | Host residual → near-zero |
| **R3** | Rehome DemoCatalogBootstrap/Matrix → module Development; Host/Storefront directory ABSENT; Certify Catalog storefront capability + durable guards + SoT | Host ZERO |

## Verification plan

- Focused builds: Catalog / Media / Payment / Host / Host.Tests
- Guards: new `HostStorefrontAmcR*GuardTests`; update Host module endpoint ownership, Development allowlist, Payment adapter location, Catalog structure if touched
- Focused behavior tests for home/listing/detail/template/appearance/checkout-identity
- `TmarDurableGuardTests` SoT pointer update only on accepted wave PASS
- No solution-wide suite

## Host residual routes (`StorefrontEndpoints`)

16 `MapGet` registrations under `/v1/storefront` (home, 2× template preview, categories, brands×2, sellers×2, merchandising, products×2, category-plp, media, checkout-identity-policy, appearance, geography/provinces).

## Certification blockers (until Architect decides)

1. **Composer home module:** Catalog (recommended) vs new Storefront BFF module.
2. **Catalog not ARCH-COMPLETE-002 certified** — accept `FOUNDATION_PARTIAL` landing for storefront slices vs certify Catalog first.
3. **Cross-module home aggregation:** single Catalog query composing Reviews/Content/Party via Contracts vs separate module endpoints + FE aggregation (FE frozen → backend composition preferred).
4. **Geography static data owner** (Order already owns the type).
5. **Checkout identity:** collapse `HostOrderStorefrontCheckoutIdentityGate` + `HostCheckoutActorPolicyAdapter` to one Host enforcement adapter after Catalog owns policy read.
6. **Whether R1 may ship without full composer evacuation** (recommended YES — template catalog first; lowest coupling).

## Destination-integrity check

| Destination | State |
| --- | --- |
| `Host/Development` | LOCKED — do not sink demo seed |
| `Host/Security/Seller` | LOCKED — do not mix Payment adapters into Seller |
| `Host/Security/Payment` (or Checkout) | NEW_LOCATION within Host security pattern — Architect-OK as thin adapters only |
| Catalog Storefront Endpoints | OPEN / existing capability |
| Host/Storefront | ACTIVE recovery folder |

## Final-Disposition

```text
NEEDS_ARCHITECT_DECISION
```

**Recommended decision package for Architect/user:**

1. Confirm **Catalog** as owner of residual storefront read composition (no new Storefront module).
2. Authorize **multi-wave** Storefront AMC starting with **R1** (security relocate + templates + checkout-identity + media + appearance).
3. Keep `automaticNextImplementationTask = NONE` between waves; stop at user review after each wave PASS.

Until (1)+(2) are explicit, Migrate/Certify skills must not start production moves.
