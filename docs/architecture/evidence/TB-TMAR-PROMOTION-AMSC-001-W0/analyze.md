# TB-TMAR-PROMOTION-AMSC-001-W0 — Analyze (tooba-architecture-analyze)

## Scope

`src/backend/Modules/Promotion/Tooba.Promotion.*` — full AMSC re-standardization under `ARCH-COMPLETE-002`
(W0 Analyze → W1 Migrate → W2 Structure → W3 Certify), starting head `b89f0b85` on `main`
(`HEAD == origin/main`; the W0 baseline was first recovered by fast-forwarding the local `main` from
`a67ec8e6` to `origin/main` `b89f0b85`, clean tree, `dirty=0`).

Promotion is **not** a member of `structureLock.certifiedModules` in
`docs/architecture/tmar-module-structure-manifests.json` and has **no** module entry in
`tmar-module-structure-manifests.json`; it is listed in `uncertifiedHttpOwningModules`.
It **does** carry a legacy `completeReferenceModules` block in `tmar-current-state.json`
(`state: COMPLETE_REFERENCE_PATTERN`, `lastAcceptedTask: TB-TMAR-PROMOTION-GOLDEN-001`,
`lastAcceptedCommit: 431ca6d21b21fa3af0972a1dafa0c85003abe662`). That block predates the current
`ARCH-COMPLETE-002` structure-manifest regime: it is a `COMPLETE_REFERENCE_PATTERN` claim with **no**
certified structure manifest, no `KnownCodes`/`IsKnown` stable-code guard, no `IErrorResourceSet`, no
validators, and a **currently red** module architecture guard. This run therefore treats the repository
as the un-certified baseline and records the first AMSC lineage for the module; W3 must reconcile the
legacy claim honestly rather than inherit it.

The final objective is stated up front: Promotion must be extractable as an independent microservice,
so **zero invalid coupling** is the hard gate. Because the legacy SoT already publishes Promotion as a
`COMPLETE_REFERENCE_PATTERN` member of `TmarDurableGuardTests`' 12-module list, W1/W2/W3 must not
silently drop that membership; the AMSC run must either re-establish it with real evidence or report the
exact blocker.

## Structured State Fields

1. **Foundation-State**: `FOUNDATION_READY` — 5 production projects (`Contracts`, `Domain`, `Application`,
   `Infrastructure`, `Endpoints`) plus `Tooba.Promotion.Tests`, all physically under
   `src/backend/Modules/Promotion/` and grouped under `<Folder Name="/Modules/Promotion/">` in
   `src/backend/Tooba.slnx` (6 `<Project>` entries, including Tests). No parallel/legacy root dump; every
   project has a real destination for the W1/W2 repairs. Foundation is `READY` for the *migration*; the
   structural quality inside the projects is not (see items 3, 14, 15).
2. **Ownership-State**: `correct` — promotion definition/lifecycle/evaluation (discount kind, percentage
   rate, fixed amount + currency, coupon, stacking policy, priority, effective window, eligibility facts,
   rounding) is Promotion-owned; merchandising campaign definition/lifecycle/membership/translation is
   Promotion-owned. Price truth is Pricing-owned, stock truth is Inventory-owned, offer truth is
   Offer-owned, catalog titles are Catalog-owned, seller display names are Party-owned, cart/order
   lifecycle is Cart/Order-owned, HTTP authorization mechanics are Host-owned platform seams. No
   responsibility in the module belongs to another module and no Promotion truth is implemented elsewhere
   (verified by reading all 42 production files plus their callers and the Host composition root).
3. **File-Cohesion-State**: `MULTI_RESPONSIBILITY_COHESION_VIOLATION` (two files) + `COHESIVE` (rest).
   - `Tooba.Promotion.Infrastructure/Directories/PromotionDirectory.cs` (419 LOC) declares **three**
     unrelated top-level types with three different reasons to change: `OpenPromotionUseCaseGuard`
     (authorization-guard seam), `DeferredPromotionRedemptionLedger` (redemption-ledger seam) and
     `PromotionDirectory` (the actual persistence/evaluation owner). The guard type and the ledger type
     are registered independently in `PromotionModule` and have no relation to the directory's data
     responsibility.
   - `Tooba.Promotion.Application/Merchandising/Admin/MerchandisingCampaignAdminCqrs.cs` (55 LOC,
     single-line-per-type) declares **12** CQRS requests + **12** handlers + `MerchandisingAdminErrorCodes`
     + `MerchandisingAdminResult` in one file — a technical-axis bundle that hides capability ownership and
     is the direct cause of the W2 restructure.
   - `Tooba.Promotion.Application/Ports/PromotionDirectoryPorts.cs` (218 LOC) bundles 4 records + 4 ports
     (`PromotionReference`, `PromotionEvaluationRequest`, `AppliedPromotion`, `PromotionEvaluationResult`,
     `IPromotionUseCaseGuard`, `IPromotionEvaluator`, `IPromotionRedemptionLedger`, `IPromotionDirectory`)
     into a single generic `*Ports.cs` dump — the AMSC "generic mixed ports bundle" smell.
   - `Tooba.Promotion.Application/Merchandising/MerchandisingCampaignPorts.cs` (207 LOC) does the same for
     the merchandising capability (5 records + 1 port + 2 list rows).
   - `Tooba.Promotion.Application/Errors/PromotionErrors.cs` (25 LOC) mixes the stable-code class with the
     exception-mapper class.
   - Largest remaining files are cohesive single-responsibility: `MerchandisingCampaignDirectory.cs`
     (572 LOC, one port over one schema), `MerchandisingCampaignAdminComposer.cs` (468 LOC, one
     cross-module read composer), `PromotionDefinition.cs` (426 LOC, one aggregate),
     `MerchandisingCampaignQuery.cs` (345 LOC, one query port).
4. **Oversized/God-File-State**: `NONE` above the module LOC guard. No file in Promotion appears in
   `src/backend/Host/Tooba.Host.Tests/Baselines/tmar-source-size-baseline.json`. The two
   multi-responsibility files above are split candidates by *cohesion*, not by size.
5. **Localization-State**: `HARDCODED_TEXT` + `MISSING_INFRASTRUCTURE_USE` — this is a real, multi-point
   defect:
   - **No `IErrorResourceSet`, no `.resx`/`.fa.resx` pair** anywhere in the module (grep for
     `IErrorResourceSet|.resx` returns nothing). Promotion therefore has **zero** localized user-facing
     error text, while every AMSC-certified peer (`Offer`, `Party`, `Payment`, `Inventory`, `Pricing`, …)
     owns a resource set.
   - `PromotionErrorCatalogContributor` registers 12 `ErrorDescriptor`s with
     `LocalizationKey = code` and no resource backing → the `IErrorMessageLocalizer` cannot resolve any
     Promotion key; only `SafeTitleFallback` ("Not Found"/"Bad Request") is ever returned.
   - `MerchandisingCampaignAdminComposer` throws **nine** hard-coded Persian user-facing messages as
     exception messages, which the CQRS layer then converts to stable codes by catching
     `InvalidOperationException` (see item 7). Exact lines: 218 `"برای انتشار حداقل یک عنوان ترجمه لازم است."`,
     223 `"بازهٔ زمانی کمپین نامعتبر است."`, 256 `"Offer یافت نشد."`, 259 `"فقط Offer فعال قابل افزودن است."`,
     265 `"این کالا قبلاً به کمپین اضافه شده است."`, 311 `"مبلغ کمپین باید بزرگ‌تر از صفر باشد."`,
     324 `"این Offer عضو کمپین نیست."`, 477 `"زمان پایان باید بعد از زمان شروع باشد."`,
     485 `"عنوان کمپین الزامی است."`.
   - `MerchandisingCampaignAdminComposer` also has label fallbacks `"بدون عنوان"`, `"کالا"`, `"فروشنده"`
     and `MerchandisingCampaignDirectory.cs:60` / `MerchandisingCampaignDevelopmentSeed` carry Persian
     Development seed titles. The label fallbacks match the accepted repository convention
     (`Catalog.Infrastructure/Storefront/StorefrontComposer.cs` uses the same `"کالا"` / `"فروشنده"`
     fallbacks, `Returns` uses `"فروشنده"`), and the Persian strings in
     `MerchandisingCampaignDevelopmentSeed` are Development seed *data* written to the database, not
     API error text. Those are recorded as accepted; the nine thrown messages are not.
   - `PromotionMutationInput` accepts `"تومان"` as a discount-kind input alias — an input alias, not
     user-facing output text. Accepted (documented, tested by `PromotionCqrsNormalizationTests`).
6. **API-Result-Pattern-State**: `AD_HOC` (one endpoint) + `CANONICAL` (rest).
   - `Endpoints/Seller/PromotionSellerEndpoints.cs` `Create` bypasses the factory on the success path:
     `r.IsSuccess ? Results.Json(r.Value, statusCode: 201) : api.From(r)`. The canonical call is
     `api.Created(location, result)`, which returns `Results.Created(location, value)` — identical 201 +
     body shape with the required `Location` header. The raw `Results.Json(...statusCode: 201)` is a
     `RAW_RESULTS` violation and drops `Location`.
   - All other endpoints use `api.From(...)`. No `Results.BadRequest`, no `Results.Problem`, no local
     `ProblemDetails` builder, no `catch`-and-map in endpoints.
   - `PromotionExceptionMapper.TryAsync` is an Application-level exception→`Result` translator. It is
     **not** an endpoint mapper and does not violate endpoint canonicality, but it *is* a
     message-heuristic seam (see item 7).
   - `ApiResponseFactory.From<T>` returns raw `Results.Json(value)` (no envelope) — the shipped success
     contract for Promotion. It must be preserved; no envelope may be introduced.
7. **Stable-Error-Code-State**: `UNREGISTERED_CODES` + `STRING_HEURISTIC` + `DUPLICATE_DESCRIPTOR_OWNERSHIP`
   — three real defects:
   - **(a) `STRING_HEURISTIC` classification.** `PromotionExceptionMapper` classifies failures by
     matching `InvalidOperationException.Message` against a hand-written dictionary, including **legacy
     message aliases** (`promotion_not_found`, `promotion_not_owned_or_missing`, `seller_id_required`,
     `promotion.definition.name_required`, `promotion.definition.window_invalid`,
     `promotion.definition.percent_invalid`, `promotion.definition.percent_no_fixed`,
     `promotion.definition.fixed_amount_invalid`, `promotion.definition.fixed_currency_required`,
     `promotion.definition.fixed_no_percent`, `promotion.definition.min_subtotal_invalid`,
     `promotion.definition.active_immutable`). Failure classification by parsing `ex.Message` is a
     hard AMSC violation; the nine Persian messages thrown by `MerchandisingCampaignAdminComposer` are
     **not** in the dictionary, which is why the CQRS layer has to fall back to bare
     `catch (InvalidOperationException)` → generic code.
   - **(b) `UNREGISTERED_CODES`.** `PromotionErrorCodes` declares 8 codes, but
     `PromotionErrorCatalogContributor` registers only **6** of them plus the 6
     `MerchandisingAdminErrorCodes` = 12 descriptors. The two unregistered declared codes are
     `PromotionErrorCodes.SellerAuthorizationDenied` (`seller.authorization.denied`) and
     `PromotionErrorCodes.AdminAuthorizationDenied` (`admin.authorization.denied`) — and that is
     **correct**: both are Foundation-owned cross-cutting codes
     (`Tooba.BuildingBlocks.Presentation.Errors.FoundationErrorCodes.SellerAuthorizationDenied` /
     `.AdminAuthorizationDenied`) already registered by `FoundationErrorCatalogContributor` and asserted
     by `HostSecurityAmcCertGuardTests`. They are dead duplicate *declarations* inside Promotion and must
     be retired, not registered. There is no `KnownCodes`/`IsKnown(string?)` declared-code guard.
   - **(c) `DUPLICATE_DESCRIPTOR_OWNERSHIP` (real, repo-wide).** `PromotionErrorCatalogContributor`
     registers `MerchandisingAdminErrorCodes.Missing = "merchandising.campaign.missing"`, and
     `Tooba.Promotion.Application` also declares the same code in
     `Application/Merchandising/Admin/MerchandisingCampaignAdminCqrs.cs`. Because the composed
     `ErrorDefinitionCatalog` is fail-fast on duplicates, exactly **one** owner must exist. W3 must
     verify via the composed catalog (`ErrorCatalogUniqueCodeGuardTests`) that no second module
     registers `merchandising.campaign.missing` and that `promotion.*` keys are registered exactly once.
   - All 12 registered descriptors use `LocalizationKey = code` and a generic `SafeTitleFallback`
     ("Not Found"/"Bad Request") with `ErrorSeverity.Warning`; explicit HTTP status is present
     (404 for `promotion.missing` and `merchandising.campaign.missing`, 400 otherwise). HTTP semantics
     are therefore already canonical; only the localization backing and the code hygiene are missing.
8. **Logging-State**: `NON_STANDARD` (thin) — the module has **no** `ILogger<T>` call site at all; no
   `Console.WriteLine`, no `Debug.WriteLine`, no hand-rolled writer, no second telemetry pipeline. The only
   observability usage is `IModuleCallTracer` in `MerchandisingCampaignQuery`. Promotion owns 21 HTTP
   routes and a Development seed but emits zero structured logs; W1/W2 must not invent a parallel logging
   mechanism, and any log added must use `ILogger<T>` + `ObservabilityLogScope`.
9. **Sensitive-Logging-State**: `NONE` — no logging at all in the module, therefore no
   password/token/OTP/header/cookie/payload exposure. The admin coupon code is business data, not a
   credential.
10. **OpenTelemetry-State**: `CANONICAL` — no second `ActivitySource`, no second `Meter`, no direct
    `StartActivity(` in production (the module guard explicitly forbids `StartActivity(`). The single
    cross-module tracer usage is `IModuleCallTracer` injected into `MerchandisingCampaignQuery`
    (Promotion → Offer/Price/Inventory batch reads). `MerchandisingCampaignAdminComposer` issues the same
    family of cross-module reads **without** `IModuleCallTracer` decoration — a `LOST_PROPAGATION`
    finding for the Admin composer path (W1 should decorate it with the canonical tracer, not a new one).
11. **Correlation-Trace-State**: `CANONICAL` — no parallel correlation mechanism, no manual `traceparent`
    parsing, no hand-rolled correlation id, no competing header. Endpoints return through
    `ApiResponseFactory`, so the canonical `ProblemDetailsContextProvider` supplies `traceId` /
    `correlationId` / `requestId`. No endpoint parses `Accept-Language` directly; the `locale` query
    parameter is passed to the merchandising composer as a **data** parameter (content translation
    selection), which is not locale resolution for error messages. Accepted.
12. **CQRS-State**: `PARTIAL` — 21 endpoint-reachable requests all use real `IRequest<T>` /
    `IRequestHandler<,>` and all endpoints dispatch through `ISender`; no endpoint touches
    `IPromotionDirectory` or `DbContext` directly. Defects: (i) the 12 merchandising requests/handlers are
    bundled in one technical file rather than grouped by capability; (ii) the 12 handlers reach a single
    `IMerchandisingCampaignAdminComposer` "god port" instead of narrow capability ports; (iii) the 8
    merchandising handlers classify failures with bare `catch (InvalidOperationException)` and convert
    *every* `InvalidOperationException` — including genuine defects — into a business `Result.Failure`,
    which violates "unknown/unexpected exceptions must not be silently converted to business failures";
    (iv) the 5 promotion command handlers route everything through `PromotionExceptionMapper.TryAsync`
    message-heuristics.
13. **Validator-Coverage-State**: `GAPS` — **0 of 21** endpoint-reachable requests have a FluentValidation
    validator; the module has no `Validators/` folder and no validation-codes class. There is no durable
    validator-classification guard. Per the AMSC matrix, transport-shape validators are required for at
    least the write requests that carry a body (`CreateSellerPromotionCommand`,
    `UpdateSellerPromotionCommand`, `CreateMerchandisingCampaignCommand`,
    `UpdateMerchandisingCampaignCommand`, `AddMerchandisingCampaignMemberCommand`,
    `ReorderMerchandisingCampaignMembersCommand`, `SetMerchandisingCampaignMemberPriceCommand`) and the
    query requests that accept user-supplied paging/filter/search/locale
    (`ListMerchandisingCampaignsQuery`, `ListMerchandisingCampaignTypesQuery`,
    `ListMerchandisingOfferCandidatesQuery`, `GetMerchandisingCampaignQuery`). The full 21-row
    classification matrix is recorded in the Validation classification matrix section below.
    Note that all business/domain validation (name/coupon required, window validity, percentage/fixed
    exclusivity, ownership, membership, campaign state) must **stay** in Domain/Application — the
    validators may only cover transport shape.
14. **Contracts-Boundary-State**: `VIOLATION` — three defects:
    - **(a) Domain leaks into Contracts.** `Tooba.Promotion.Contracts` declares
      `IMerchandisingCampaignPromoPrice` **and** the Domain declares `IMerchandisingCampaignPromoPrice`
      (`Tooba.Promotion.Domain/Merchandising/IMerchandisingCampaignPromoPrice.cs`); the same interface
      identity exists in two layers. Additionally `MerchandisingCampaignContracts.cs` (Contracts) contains
      storefront/runtime projection logic (`MerchandisingCampaignStorefrontEligibility.IsAmazingRailEligible`)
      — business rule text living in the boundary assembly.
    - **(b) Foreign Contracts leakage into Promotion Contracts.** `Tooba.Promotion.Contracts.csproj`
      references `Tooba.Offer.Contracts` **and** `MerchandisingCampaignContracts.cs` uses
      `Tooba.Offer.Contracts.Dtos.SalesChannel` in the public `MerchandisingPriceScope` signature. A
      module's stable boundary therefore cannot be consumed without also referencing Offer. This is a
      microservice blocker for the Contracts assembly (legal as an *Infrastructure* edge, not as a
      *Contracts* edge).
    - **(c) Application/Application-mixed stable-code home.** The stable codes live in
      `Tooba.Promotion.Application/Errors/PromotionErrors.cs` while the certified precedent places them in
      `Tooba.<Module>.Contracts/Errors/<Module>ErrorCodes.cs` (`Payment`, `Party`, `Inventory`, `Pricing`
      all do). The `Endpoints` project consumes them through its `Application` reference.
    - `Contracts` otherwise holds genuine boundary types (checkout port/DTOs, merchandising runtime
      models, schema-migrator port) with no EF entity, no `DbContext`, no repository implementation.
15. **Cross-Module-Coupling-State**: `ILLEGAL` — outbound edges:
    - **Legal Contracts-only**: `Tooba.Promotion.Contracts` → `Tooba.Offer.Contracts`;
      `Tooba.Promotion.Infrastructure` → `Tooba.Offer.Contracts`, `Tooba.Pricing.Contracts`,
      `Tooba.Inventory.Contracts`, `Tooba.Catalog.Contracts`, `Tooba.Party.Contracts`.
    - **ILLEGAL foreign Application/Domain**: `Tooba.Promotion.Infrastructure` references
      `Tooba.Inventory.Application` and `Tooba.Inventory.Domain`, and consumes
      `Tooba.Inventory.Application.Ports.IInventoryDirectory` +
      `Tooba.Inventory.Domain.ValueObjects.StockAdjustmentKind` in
      `Development/MerchandisingCampaignDevelopmentSeed.cs` (imports at lines 6–8 and 15–17). The
      `Tooba.Promotion.Application -> Tooba.Inventory.Application` edge is **baseline-tolerated** by
      `TmarFoundationTests.App_to_app_edges_do_not_expand_beyond_baseline` via
      `Baselines/tmar-app-to-app-edges.json`, but the AMSC hard gate forbids foreign Application/Domain
      references regardless of a baseline. A baseline entry is a documented debt, not a lock.
    - **Also baseline-tolerated**: `Tooba.Promotion.Application -> Tooba.Pricing.Application`
      (`tmar-app-to-app-edges.json` line 8). Note: the W0 grep shows **no** `using Tooba.Pricing.Application`
      and **no** `Tooba.Pricing.Application` csproj reference in the current tree — `Promotion.Infrastructure`
      references `Tooba.Pricing.Contracts` and `Tooba.Pricing.Contracts.Ports`. The baseline entry is
      therefore **stale** (a leftover from the Pricing AMSC W1 repair). W1 must confirm and remove the
      stale entry honestly, not silently.
    - **Inbound**: no other module references `Tooba.Promotion.Application`/`.Domain`/`.Infrastructure`.
      `Order` consumes `Tooba.Promotion.Contracts` only (`CheckoutImplW5PromotionContractTests` asserts
      `Tooba.Promotion.Application` is absent from `Order.Application` and `Order.Infrastructure`).
      `Cart` consumes `Tooba.Promotion.Contracts.Merchandising` (via `ICampaignCartPriceAuthority`
      implemented by `CampaignCartPriceAuthority`). `Catalog` consumes
      `Tooba.Promotion.Contracts.Merchandising.IMerchandisingCampaignQuery`.
    - **Host residue**: `Host/Tooba.Host/Security/Seller/HostPromotionSellerAuthorizer.cs` implements the
      module's `IPromotionSellerAuthorizer` port. This is the repository's **accepted** Host platform
      security-adapter pattern, pinned by `HostSellerAmcR1GuardTests`, `HostSellerAmcR5GuardTests` and
      `HostSecurityAmcCertGuardTests`. `Fulfillment` later moved its equivalent adapter into the module
      (`FulfillmentSellerAuthorizer` lives in `Tooba.Fulfillment.Endpoints/Seller/`), but moving Promotion's
      adapter would break three accepted Host guards and is **out of scope** for this run. The residual
      risk is one-directional and benign: `Host` → `Promotion.Endpoints` is composition; the module never
      references `Host`. Recorded as `ACCEPTED_HOST_SECURITY_ADAPTER_KEEP`, not a blocker.
16. **Cross-Module-Join-State**: `NONE` — the single `PromotionDbContext` touches only the `promotion`
    schema plus its own Outbox table. No foreign `DbSet`, no EF navigation crossing ownership, no raw SQL
    joining foreign schemas, no cross-module transaction. Cross-module data (offer, catalog title, party
    display name, price, availability) is composed through Contracts ports
    (`IOfferQueryGateway`, `IOfferLookupGateway`, `ICatalogVariantLookup`, `IPartyLookup`,
    `IPriceLookupGateway`, `IPriceDirectory`, `IInventoryAvailabilityGateway`) — never by join.
17. **Persistence-Ownership-State**: `CORRECT` — own schema `promotion`, own `PromotionDbContext`, own
    migrations (`20260823210000_InitialPromotion`,
    `20260919134400_MerchandisingCampaignFoundation` + designers + snapshot), own
    `PromotionOutboxRegistration : IOutboxModuleRegistration`, own
    `IPromotionSchemaMigrator` + `PromotionModuleMigration` so Host bootstraps never type
    `PromotionDbContext` (asserted by `PromotionArchitectureGuardTests` and
    `HostDevelopmentMigrationSeamGuardTests`). Two structural nits for W2: the module DI class lives in
    `Infrastructure/DependencyInjection/` while certified peers use `Infrastructure/<Module>.csproj`
    root (`Party`, `Pricing`) — either is acceptable if allowlisted — and `Infrastructure/Events/` +
    `Infrastructure/Messaging/` duplicate the outbox/event concern split used elsewhere.
18. **Endpoint-Ownership-State**: `MODULE_OWNED` — 21 routes, all owned by
    `Tooba.Promotion.Endpoints` and mapped by `PromotionEndpointModule.MapPromotionEndpoints()`, which
    `Host/Program.cs` calls at line 446. Host Promotion route count **ZERO**; no duplicate route
    ownership; no Host `Promotion` folder; `HostModuleEndpointOwnershipTests` records Promotion with
    `hasHostResidue: true` for the historical file `Promotion/PromotionEndpoints.cs` (which does not
    exist — asserted absent by `PromotionArchitectureGuardTests`).
    Route inventory: `/v1/seller/promotions` ×6, `/v1/admin/promotions` ×3,
    `/v1/admin/merchandising-campaigns` ×12.
19. **Host-Residue-State**: `ALLOWED_COMPOSITION_ROOT` + `ALLOWED_SECURITY_ADAPTER` — Host references are
    exactly: `Tooba.Host.csproj` project references to `Promotion.Infrastructure` + `Promotion.Endpoints`;
    `Program.cs` (`using Tooba.Promotion.Endpoints`, `AddPromotionEndpointPresentation()` line 100,
    MediatR assembly registration line 180, `IPromotionSellerAuthorizer` →
    `HostPromotionSellerAuthorizer` line 228, `MapPromotionEndpoints()` line 446);
    `Composition/ToobaModuleComposition.cs` (`new PromotionModule()` line 67);
    `Security/Seller/HostPromotionSellerAuthorizer.cs` (security adapter).
    `ILLEGAL_BUSINESS_AUTHORITY` = ZERO, `ILLEGAL_PERSISTENCE_AUTHORITY` = ZERO (no Host file reads
    `PromotionDbContext`; asserted by the module guard), `ILLEGAL_ENDPOINT_OWNERSHIP` = ZERO.
    Host final closure (`HOST_ROOT_FINAL_CERTIFIED`, `hostRootFinalCert001`) is preserved and untouched.
20. **Schema-Migration-State**: `UNCHANGED` — no migration file is touched in any wave. Migration ids
    `20260823210000_InitialPromotion` and `20260919134400_MerchandisingCampaignFoundation`, their order,
    Up/Down bodies and the model snapshot stay byte-identical. W1/W2/W3 are code-and-documentation waves
    only. `PromotionDbContext.Schema = "promotion"` and every table/column/index/constraint stay as-is.
21. **Behavior-Preservation-Risk**: `MEDIUM` — W1 changes behavior-adjacent seams:
    (i) replacing `PromotionExceptionMapper` message-heuristics with declared stable codes changes which
    codes are produced for the **same** underlying failures — the observable code set is preserved and
    hardened, but a mis-mapping would surface as a different `errorCode`;
    (ii) converting the nine Persian `InvalidOperationException` messages in the Admin composer into
    declared codes removes Persian text from the response path (the client-visible `errorCode` becomes
    stable and the localized text moves to `.resx`) — the observable change is *intended and required* by
    AMSC, and must be recorded honestly in the W1 evidence;
    (iii) adding `Location` to the seller-create 201 response adds a response **header** (status and body
    unchanged) — a wire-visible but additive change required by canonicality;
    (iv) splitting `PromotionDirectory.cs` and the merchandising CQRS file is a pure move, zero logic
    change. Route set, HTTP methods, status codes, response bodies, DTO semantics, business rules, state
    transitions, ordering, idempotency, transaction behavior, persistence, schema, outbox event names,
    telemetry names, tenant/store scoping and public Contracts must all remain identical.
22. **Canonical-Reference-Used**: `Offer` (Endpoints `Resources/OfferErrorResources.cs` +
    `OfferErrors.resx`/`.fa.resx` + `IErrorResourceSet` registration in `OfferEndpointModule`),
    `Payment`/`Party`/`Inventory`/`Pricing` (`Contracts/Errors/<Module>ErrorCodes.cs` with
    `KnownCodes` + `IsKnown(string?)` and `Application/Composition/<Module>Operation.cs` dual-mechanism
    typed-fault seam), `Party`/`Pricing` (`Infrastructure/<Module>.csproj` root DI class),
    `Catalog`/`Returns` (accepted `"کالا"`/`"فروشنده"` label-fallback convention),
    `BuildingBlocks` (`Result`/`Result<T>`/`SemanticError`/`SemanticException`/`ApiResponseFactory`/
    `IErrorDefinitionCatalog`/`IModuleCallTracer`/`ObservabilityLogScope`),
    and Promotion's own `PromotionArchitectureGuardTests` as the authoritative assertion set.
23. **Final-Disposition**: `READY_TO_MIGRATE` (bounded: stable-code hygiene + declared-code guard +
    typed-fault seam, canonical error localization (resource set + bilingual `.resx`), validator
    classification matrix + validators, endpoint `Created` canonicality, removal of the illegal
    Inventory Application/Domain edge and the stale Pricing Application baseline entry, cohesion splits,
    then capability-first structure normalization and certification. Zero ownership move of business
    behavior; zero schema change; the accepted Host security adapter and the legacy
    `COMPLETE_REFERENCE_PATTERN` SoT membership are reconciled honestly rather than silently dropped).

## Target analyzed

`src/backend/Modules/Promotion/` — 6 projects, 42 production `.cs` files (EF migrations/snapshot
excluded), 2 migrations + 2 designers + 1 snapshot, 0 `.resx`, 0 `.gitkeep`.

| Project | Production `.cs` | Root `.cs` | Test files |
|---|---|---|---|
| `Tooba.Promotion.Contracts` | 3 | none | — |
| `Tooba.Promotion.Domain` | 14 | none | — |
| `Tooba.Promotion.Application` | 16 | none | — |
| `Tooba.Promotion.Infrastructure` | 14 (excl. 3 EF migration files) | none | — |
| `Tooba.Promotion.Endpoints` | 6 | `PromotionEndpointModule.cs` | — |
| `Tooba.Promotion.Tests` | — | — | 2 |

## Responsibility map

| Area | Files | Responsibilities | Verdict |
|---|---|---|---|
| `Contracts/Checkout/CheckoutPromotionContracts.cs` | 1 | CONTRACT (checkout evaluation port + DTOs) | COHESIVE |
| `Contracts/Merchandising/MerchandisingCampaignContracts.cs` | 1 | CONTRACT runtime models + **DOMAIN_RULE** (`IsAmazingRailEligible`) + **foreign** `Offer.Contracts` type in signature | MUST_SPLIT (rule → Domain; de-foreign the signature) |
| `Contracts/Merchandising/IPromotionSchemaMigrator.cs` | 1 | CONTRACT (bootstrap migration port) | COHESIVE |
| `Domain/Aggregates/PromotionDefinition.cs` | 1 | DOMAIN_RULE (aggregate + invariants + discount computation) | COHESIVE |
| `Domain/Merchandising/*` (7 files) | 7 | DOMAIN_RULE (campaign aggregate, membership, translations, type, lifecycle, promo-price interface) | COHESIVE; `IMerchandisingCampaignPromoPrice` duplicated vs Contracts |
| `Domain/Policies/*` (2), `Domain/ValueObjects/*` (4), `Domain/Events/*` (4) | 10 | DOMAIN_RULE | COHESIVE |
| `Application/Errors/PromotionErrors.cs` | 1 | CONTRACT (stable codes) + APPLICATION (exception mapper) | MUST_SPLIT + MUST_RELOCATE (codes → `Contracts/Errors`) |
| `Application/Ports/PromotionDirectoryPorts.cs` | 1 | APPLICATION ports + DTOs (generic bundle) | MUST_SPLIT (per-port files, capability folder) |
| `Application/Merchandising/MerchandisingCampaignPorts.cs` | 1 | APPLICATION port + records (generic bundle) | MUST_SPLIT |
| `Application/Merchandising/IMerchandisingCampaignAdminComposer.cs` | 1 | APPLICATION port (12-method god port) | MUST_SPLIT by capability |
| `Application/Merchandising/MerchandisingCampaignAdminModels.cs` | 1 | APPLICATION models | COHESIVE |
| `Application/Merchandising/Admin/MerchandisingCampaignAdminCqrs.cs` | 1 | APPLICATION CQRS (12 requests + 12 handlers + codes + helper) | MUST_SPLIT |
| `Application/Checkout/CheckoutPromotionAdapter.cs` | 1 | APPLICATION adapter (Domain evaluator → checkout contract) | COHESIVE |
| `Application/Models/PromotionMutationInput.cs` | 1 | APPLICATION normalizer + input model | COHESIVE |
| `Application/Commands/*` (5 files) | 5 | APPLICATION use cases | COHESIVE (single-file leaves → W2 capability grouping) |
| `Application/Queries/*` (4 files) | 4 | APPLICATION use cases | COHESIVE (single-file leaves → W2 capability grouping) |
| `Infrastructure/Persistence/*` (1 + 5 EF files) | 6 | PERSISTENCE | COHESIVE |
| `Infrastructure/Directories/PromotionDirectory.cs` | 1 | PERSISTENCE + APPLICATION_ADAPTER + 2 unrelated seams | MUST_SPLIT |
| `Infrastructure/Directories/MerchandisingCampaignDirectory.cs` | 1 | PERSISTENCE (one port, one schema) | COHESIVE (`OVERSIZED_ONLY`-class) |
| `Infrastructure/Queries/MerchandisingCampaignQuery.cs` | 1 | PERSISTENCE READ + INTEGRATION_ADAPTER (traced) | COHESIVE |
| `Infrastructure/Merchandising/MerchandisingCampaignAdminComposer.cs` | 1 | CROSS_MODULE_READ_ORCHESTRATION + **hard-coded FA text** | MUST_REPAIR (text → codes) |
| `Infrastructure/Adapters/*` (3) | 3 | INTEGRATION_ADAPTER / migration tooling | COHESIVE |
| `Infrastructure/Events/*` + `Messaging/*` | 2 | CONTRACT (integration events) + outbox translation | COHESIVE |
| `Infrastructure/Development/MerchandisingCampaignDevelopmentSeed.cs` | 1 | DEVELOPMENT_SEED + **foreign Application/Domain** | MUST_REPAIR (Contracts-only) |
| `Infrastructure/DependencyInjection/*` (2) | 2 | PRESENTATION_COMPOSITION / module DI | COHESIVE |
| `Endpoints/PromotionEndpointModule.cs` | 1 | HTTP composition | COHESIVE |
| `Endpoints/Admin/*` (3), `Endpoints/Seller/*` (2) | 5 | HTTP_ENDPOINT + AUTHORIZATION_ADAPTER ports | COHESIVE; seller create = `RAW_RESULTS` |
| `Endpoints/Errors/PromotionErrorCatalogContributor.cs` | 1 | CONTRACT catalog contribution | COHESIVE; no resource set |

`MUST_SPLIT` decisions: 6 files. `MUST_REPAIR` (not split): 2 files. `MUST_RELOCATE`: 1 (stable codes).

## Ownership map

| Responsibility | Owner | Evidence |
|---|---|---|
| Promotion definition + lifecycle (draft/active/expired) | **Promotion** | `PromotionDefinition`, `promotion.promotions` |
| Discount kind/rate/fixed amount/currency/coupon/stacking/priority/window | **Promotion** | `PromotionDefinition` + `PromotionMutationNormalizer` |
| Eligibility evaluation + rounding + selection order | **Promotion** | `PromotionDirectory.EvaluateAsync`, `PromotionEligibilityFacts`, `PromotionRounding` |
| Checkout discount evaluation seam | **Promotion** (consumed by Order) | `Contracts/Checkout/ICheckoutPromotionPort` + `CheckoutPromotionAdapter` |
| Merchandising campaign definition/lifecycle/membership/translation | **Promotion** | `MerchandisingCampaign*`, `promotion.merchandising_*` |
| Campaign runtime projection for storefront | **Promotion** (consumed by Catalog) | `Contracts/Merchandising/IMerchandisingCampaignQuery` |
| Campaign cart price authority | **Promotion** (consumed by Cart) | `ICampaignCartPriceAuthority` + `CampaignCartPriceAuthority` |
| Schema `promotion` + migrations + outbox | **Promotion** | `PromotionDbContext`, `PromotionOutboxRegistration` |
| `/v1/{seller,admin}/promotions` + `/v1/admin/merchandising-campaigns` | **Promotion** | `PromotionEndpointModule` (21 routes) |
| Auth mechanics (`ISellerPanelAccess`, `IAdminPanelAccess`) | **Host platform seam** | `BuildingBlocks.Security`, `Host/Security/Seller` |
| Price truth | **Pricing** (via Contracts) | `IPriceDirectory`, `IPriceLookupGateway` |
| Stock truth | **Inventory** (via Contracts) | `IInventoryAvailabilityGateway`, `IInventoryQueryGateway` |
| Offer existence/status/ownership | **Offer** (via Contracts) | `IOfferQueryGateway`, `IOfferLookupGateway` |
| Catalog variant titles | **Catalog** (via Contracts) | `ICatalogVariantLookup` |
| Party display names | **Party** (via Contracts) | `IPartyLookup`, `IPartyDevelopmentDirectory` |

## MUST_SPLIT decisions

1. `Application/Merchandising/Admin/MerchandisingCampaignAdminCqrs.cs` → capability-first
   `Application/Merchandising/Admin/Commands/*.cs` + `.../Queries/*.cs` (one authoritative request per
   file, handler co-located per repository precedent), plus a single
   `Application/Merchandising/Admin/MerchandisingAdminErrorCodes.cs` and one typed-fault seam.
2. `Application/Ports/PromotionDirectoryPorts.cs` → `Application/Promotions/{Ports,Models}/*.cs`
   (one port/record family per file), keeping `IPromotionUseCaseGuard`/`IPromotionRedemptionLedger`
   as their own cohesive files.
3. `Application/Merchandising/MerchandisingCampaignPorts.cs` → `Application/Merchandising/{Ports,Models}/*.cs`.
4. `Application/Merchandising/IMerchandisingCampaignAdminComposer.cs` (12-method god port) → narrow
   capability ports (`IMerchandisingCampaignAdminQueryPort`, `IMerchandisingCampaignAdminWritePort`,
   `IMerchandisingCampaignMemberPort`, `IMerchandisingOfferCandidatePort`) — split must be
   behavior-preserving and must not add indirection the handlers do not need.
5. `Infrastructure/Directories/PromotionDirectory.cs` → `PromotionDirectory.cs` (persistence/evaluation)
   + `OpenPromotionUseCaseGuard.cs` + `DeferredPromotionRedemptionLedger.cs`.
6. `Contracts/Merchandising/MerchandisingCampaignContracts.cs` → split the storefront eligibility rule out
   of the boundary assembly and remove the foreign `Offer.Contracts` type from the public signature.

## Current illegal dependencies

| # | Edge | Kind | Evidence | Repair (W1) |
|---|---|---|---|---|
| 1 | `Tooba.Promotion.Infrastructure` → `Tooba.Inventory.Application` (project reference) | foreign `Application` | `Tooba.Promotion.Infrastructure.csproj` line 21 | remove reference; use Inventory Contracts only |
| 2 | `Tooba.Promotion.Infrastructure` → `Tooba.Inventory.Domain` (project reference) | foreign `Domain` | `Tooba.Promotion.Infrastructure.csproj` line 22 | remove reference; replace `StockAdjustmentKind` usage with a Contracts-level seed request |
| 3 | `MerchandisingCampaignDevelopmentSeed` uses `Tooba.Inventory.Application.Ports.IInventoryDirectory` | foreign `Application` type | imports lines 6, 7, 8 | replace with `IInventoryDevelopmentSeedGateway` (already in `Tooba.Inventory.Contracts.Availability`) or a narrow new Inventory Contracts port |
| 4 | `MerchandisingCampaignDevelopmentSeed` uses `Tooba.Inventory.Domain.ValueObjects.StockAdjustmentKind` + `Tooba.Inventory.Domain.Aggregates/Events` | foreign `Domain` type | imports lines 15, 16, 17 | remove; the Contracts seed gateway already owns the "decrease stock" semantics (`SeedDevelopmentStock` with a signed/negative quantity or a dedicated drain request) |
| 5 | `Baselines/tmar-app-to-app-edges.json` entry `Tooba.Promotion.Application -> Tooba.Pricing.Application` | stale baseline debt | no such reference exists in the tree (grep + csproj) | remove the stale entry honestly (it is not a lock); keep the guard's "no NEW edges" intent |
| 6 | `Baselines/tmar-app-to-app-edges.json` entry `Tooba.Promotion.Application -> Tooba.Inventory.Application` | baseline-tolerated foreign `Application` | `tmar-app-to-app-edges.json` line 7 | remove after repairing #1–#4; the AMSC gate forbids foreign Application regardless of baseline |
| 7 | `Tooba.Promotion.Contracts` → `Tooba.Offer.Contracts` used in a public Contracts signature | Contracts self-containment | `MerchandisingCampaignContracts.cs` `SalesChannel` in `MerchandisingPriceScope` | replace the foreign type in the public signature with a Promotion-owned string/enum; keep the Infrastructure Offer edge legal |

`ILLEGAL` count = 7 findings (2 project references, 2 type usages, 2 baseline entries, 1 Contracts
self-containment defect). No inbound illegal edge.

## Cross-module join inventory

`NONE`. Single `PromotionDbContext` → `promotion` schema + own Outbox table. No foreign `DbSet`, no
navigation crossing ownership, no `FromSql`/raw SQL, no cross-module transaction, no shared mutable
aggregate. Admin grids and the campaign runtime projection compose Offer/Catalog/Party/Price/Inventory
rows through Contracts ports instead of joining.

## Contracts-only replacement map

| Need | Current | Replacement (W1) |
|---|---|---|
| Development seed drains an offer's stock | `IInventoryDirectory.AdjustAsync(..., StockAdjustmentKind.Decrease, ...)` (foreign Application/Domain) | `IInventoryDevelopmentSeedGateway` (Contracts) — add a narrow "drain/set available to zero" request to the existing gateway if the current three methods cannot express it; the semantic (`available → 0`) is Inventory-owned |
| Development seed opens a location/position | `IInventoryDirectory.CreateLocationAsync` / `OpenPositionAsync` (foreign Application) | `IInventoryDevelopmentSeedGateway.EnsureDevelopmentLocationAsync` + `IncreaseDevelopmentStockAsync` (already Contracts) |
| Development seed reads a position | `IInventoryQueryGateway.FindPositionByStockItemIdAsync` (already Contracts) | unchanged |
| Merchandising price scope channel | `Tooba.Offer.Contracts.Dtos.SalesChannel` in `MerchandisingPriceScope` (public Contracts signature) | Promotion-owned channel value (string or Promotion enum) mapped in Infrastructure; the Offer edge stays legal where it already is (Infrastructure) |
| Stable error codes | `Application/Errors/PromotionErrorCodes` | `Contracts/Errors/PromotionErrorCodes.cs` (+ `KnownCodes`, `IsKnown(string?)`), consumed by Endpoints and Application |
| Checkout/merchandising/campaign ports | unchanged | unchanged (already Contracts) |
| Catalog/Party/Price reads | unchanged | unchanged (already Contracts) |

No new shared/god-contract project is created; the repair reuses `Tooba.Promotion.Contracts` and the
existing `Tooba.Inventory.Contracts.Availability.IInventoryDevelopmentSeedGateway`.

## CQRS/MediatR gaps

- MediatR `12.5.0` registered once via `AddToobaCqrsFoundation` in `Host/Program.cs` (line 169) with the
  `Tooba.Promotion.Application.Commands.CreateSellerPromotion.CreateSellerPromotionCommand` assembly at
  line 180. No second dispatcher, no custom sender.
- 21/21 requests are real `IRequest<T>` with real `IRequestHandler<,>`; 21/21 endpoints dispatch through
  `ISender`; 0 endpoints touch a directory/DbContext. **No endpoint-logic or Host-bypass defect.**
- Defect: `MerchandisingCampaignAdminCqrs.cs` bundles 12 requests + 12 handlers in one technical file
  (capability ownership hidden) and the handlers depend on a single 12-method god port.
- Defect: 8 merchandising handlers use `catch (InvalidOperationException)` and convert *all*
  `InvalidOperationException`s (including genuine defects) into business `Result.Failure`; 5 promotion
  command handlers use `PromotionExceptionMapper.TryAsync` message heuristics.
- Defect: 0 FluentValidation validators and no validation-codes class.

## Validation classification matrix

| # | Request | Route | Audience | Classification | Reason |
|---|---|---|---|---|---|
| 1 | `ListSellerPromotionsQuery` | `GET /v1/seller/promotions` | Seller | `NO_VALIDATOR_REQUIRED` | auth-scoped read, no user input beyond identity |
| 2 | `GetSellerPromotionQuery` | `GET /v1/seller/promotions/{id}` | Seller | `VALIDATOR_REQUIRED` | route `id` shape (empty Guid) |
| 3 | `CreateSellerPromotionCommand` | `POST /v1/seller/promotions` | Seller | `VALIDATOR_REQUIRED` | body present (name/coupon/discount kind/value/currency/minimum) |
| 4 | `UpdateSellerPromotionCommand` | `PUT /v1/seller/promotions/{id}` | Seller | `VALIDATOR_REQUIRED` | route `id` + body |
| 5 | `ActivateSellerPromotionCommand` | `POST /v1/seller/promotions/{id}/activate` | Seller | `VALIDATOR_REQUIRED` | route `id` shape |
| 6 | `DeactivateSellerPromotionCommand` | `POST /v1/seller/promotions/{id}/deactivate` | Seller | `VALIDATOR_REQUIRED` | route `id` shape |
| 7 | `ListAdminPromotionsQuery` | `GET /v1/admin/promotions` | Admin | `NO_VALIDATOR_REQUIRED` | optional filter only, no shape risk |
| 8 | `GetAdminPromotionQuery` | `GET /v1/admin/promotions/{id}` | Admin | `VALIDATOR_REQUIRED` | route `id` shape |
| 9 | `DeactivateAdminPromotionCommand` | `POST /v1/admin/promotions/{id}/deactivate` | Admin | `VALIDATOR_REQUIRED` | route `id` shape |
| 10 | `ListMerchandisingCampaignsQuery` | `GET /v1/admin/merchandising-campaigns/` | Admin | `VALIDATOR_REQUIRED` | user-supplied `skip`/`take`/`lifecycle`/`runtimeWindow` shape |
| 11 | `ListMerchandisingCampaignTypesQuery` | `GET /v1/admin/merchandising-campaigns/types` | Admin | `NO_VALIDATOR_REQUIRED` | optional locale only |
| 12 | `ListMerchandisingOfferCandidatesQuery` | `GET /v1/admin/merchandising-campaigns/offer-candidates` | Admin | `VALIDATOR_REQUIRED` | user-supplied `skip`/`take` shape |
| 13 | `GetMerchandisingCampaignQuery` | `GET /v1/admin/merchandising-campaigns/{campaignId}` | Admin | `VALIDATOR_REQUIRED` | route `campaignId` shape |
| 14 | `CreateMerchandisingCampaignCommand` | `POST /v1/admin/merchandising-campaigns/` | Admin | `VALIDATOR_REQUIRED` | body present (type/window/priority/translations) |
| 15 | `UpdateMerchandisingCampaignCommand` | `PUT /v1/admin/merchandising-campaigns/{campaignId}` | Admin | `VALIDATOR_REQUIRED` | route `campaignId` + body |
| 16 | `PublishMerchandisingCampaignCommand` | `POST .../{campaignId}/publish` | Admin | `VALIDATOR_REQUIRED` | route `campaignId` shape |
| 17 | `ArchiveMerchandisingCampaignCommand` | `POST .../{campaignId}/archive` | Admin | `VALIDATOR_REQUIRED` | route `campaignId` shape |
| 18 | `AddMerchandisingCampaignMemberCommand` | `POST .../{campaignId}/members` | Admin | `VALIDATOR_REQUIRED` | route `campaignId` + body `sellerOfferId` |
| 19 | `RemoveMerchandisingCampaignMemberCommand` | `DELETE .../{campaignId}/members/{sellerOfferId}` | Admin | `VALIDATOR_REQUIRED` | two route Guids |
| 20 | `ReorderMerchandisingCampaignMembersCommand` | `PUT .../{campaignId}/members/order` | Admin | `VALIDATOR_REQUIRED` | route `campaignId` + ordered Guid collection |
| 21 | `SetMerchandisingCampaignMemberPriceCommand` | `PUT .../{campaignId}/members/{sellerOfferId}/price` | Admin | `VALIDATOR_REQUIRED` | route Guids + amount/currency body |

**Totals: 21 requests, 19 `VALIDATOR_REQUIRED`, 2 `NO_VALIDATOR_REQUIRED`, 0 validators present today.**
W1 must add the 19 transport-shape validators (or a smaller justified set with explicit durable reasons
for each `NO_VALIDATOR_REQUIRED`), emitting stable machine codes via a module
`PromotionValidationCodes` class — never Persian/English prose. All business/domain rules (name/coupon
required, window validity, percentage-vs-fixed exclusivity, currency rules, ownership, membership,
campaign lifecycle) stay in Domain/Application and must not be duplicated into validators.

## Localization findings

- **Missing resource infrastructure**: no `IErrorResourceSet`, no `.resx`/`.fa.resx`. Promotion's 12
  registered `ErrorDescriptor`s use `LocalizationKey = code` with only a generic `SafeTitleFallback`, so
  `IErrorMessageLocalizer` cannot resolve any Promotion key. This is the single largest localization gap
  in the module and a direct `MISSING_INFRASTRUCTURE_USE` violation.
- **Hard-coded Persian user-facing error text** (9 occurrences, all in
  `Infrastructure/Merchandising/MerchandisingCampaignAdminComposer.cs`): lines 218, 223, 256, 259, 265,
  311, 324, 477, 485. Each is thrown as `InvalidOperationException("<Persian prose>")` and then
  converted to a stable code by a bare `catch (InvalidOperationException)`, so the Persian prose never
  reaches the client — but it *is* hard-coded user-facing text in production code and it makes failure
  classification impossible to reason about. W1 replaces each with a declared stable code.
- **`EXCEPTION_MESSAGE_BASED` classification**: `PromotionExceptionMapper` maps failures by exact
  `ex.Message` dictionary lookup, including 12 legacy aliases. This is the AMSC-forbidden
  `when (ex.Message is ...)`-class heuristic and must be replaced by declared-code classification.
- **Dead duplicate cross-cutting code declarations**: `PromotionErrorCodes.SellerAuthorizationDenied` and
  `.AdminAuthorizationDenied` re-declare Foundation-owned codes. They are (correctly) not registered by
  Promotion's contributor; the duplicate declarations should be retired so the module's declared-code
  surface matches its owned surface.
- **Accepted (recorded, not repaired)**: `"کالا"` / `"فروشنده"` / `"بدون عنوان"` label fallbacks match the
  accepted repository convention (`Catalog.Infrastructure/Storefront/StorefrontComposer.cs`,
  `Returns.Infrastructure/Queries/AdminReturnGridQueryEngine.cs`); Persian strings in
  `MerchandisingCampaignDevelopmentSeed` are Development seed data written to the database;
  `"تومان"` in `PromotionMutationNormalizer` is a documented input alias covered by
  `PromotionCqrsNormalizationTests`; `Infrastructure/Directories/MerchandisingCampaignDirectory.cs:60`
  is a Development seed default title.
- No endpoint parses `Accept-Language`; no `exception.Message` is returned to a client.

## API result/error mapping findings

- `Endpoints/Seller/PromotionSellerEndpoints.cs` `Create` uses
  `r.IsSuccess ? Results.Json(r.Value, statusCode: 201) : api.From(r)` — a `RAW_RESULTS` success path that
  also omits `Location`. Replace with `api.Created(location, r)`. Status (201) and body shape (raw DTO)
  are preserved; `Location` is added.
- All other 20 endpoints use `api.From(...)`. Zero `Results.BadRequest`, `Results.Problem`, local
  `ProblemDetails` builders or local error mappers.
- `PromotionExceptionMapper.TryAsync` / `TryMapExact` is an Application-level translator, not an endpoint
  mapper; it violates canonicality by classifying on `ex.Message` and must be replaced by a
  declared-code typed-fault seam (`Application/Composition/PromotionOperation.cs`, mirroring
  `PartyOperation` / `PricingOperation` / `PaymentOperation`).
- Composed-catalog uniqueness is enforced repository-wide by `ErrorCatalogUniqueCodeGuardTests`; Promotion
  contributes 12 unique descriptors. `merchandising.campaign.missing` must be verified as singly owned.
- Success contract: `ApiResponseFactory.From<T>` returns a raw `Results.Json(value)` (no envelope). This is
  the shipped Promotion contract and must not be changed.

## Logging / sensitive-data findings

- Zero `ILogger<T>` call sites, zero `Console.WriteLine` / `Debug.WriteLine`, no second logging framework.
- `IModuleCallTracer` is used in `MerchandisingCampaignQuery` (canonical); `MerchandisingCampaignAdminComposer`
  issues the same cross-module read family without tracer decoration (`LOST_PROPAGATION`).
- `Sensitive-Logging-State = NONE` — nothing is logged, so no secret exposure exists; no secret was found
  in any Promotion production source.

## OpenTelemetry / correlation findings

- No direct `StartActivity(`, no second `ActivitySource`/`Meter`, no manual `traceparent` parsing, no
  competing correlation id (the module guard explicitly forbids `StartActivity(`).
- `MerchandisingCampaignQuery` decorates cross-module calls with `IModuleCallTracer`; the Admin composer
  path does not — W1 should decorate it with the existing tracer (never a new one).
- Promotion returns through `ApiResponseFactory`, so ProblemDetails `traceId`/`correlationId`/`requestId`
  come from the canonical provider.

## File cohesion / splitting plan

| File | Action | Target |
|---|---|---|
| `Application/Merchandising/Admin/MerchandisingCampaignAdminCqrs.cs` | split | `Application/Merchandising/Admin/Commands/*.cs`, `.../Queries/*.cs`, `.../MerchandisingAdminErrorCodes.cs` |
| `Application/Ports/PromotionDirectoryPorts.cs` | split | `Application/Promotions/Ports/*.cs`, `Application/Promotions/Models/*.cs` |
| `Application/Merchandising/MerchandisingCampaignPorts.cs` | split | `Application/Merchandising/Ports/*.cs`, `Application/Merchandising/Models/*.cs` |
| `Application/Merchandising/IMerchandisingCampaignAdminComposer.cs` | split | narrow capability ports under `Application/Merchandising/{Ports,Admin}` |
| `Application/Errors/PromotionErrors.cs` | split + relocate | `Contracts/Errors/PromotionErrorCodes.cs` (+ `KnownCodes`/`IsKnown`), `Application/Composition/PromotionOperation.cs` |
| `Infrastructure/Directories/PromotionDirectory.cs` | split | `Directories/PromotionDirectory.cs` + `Directories/OpenPromotionUseCaseGuard.cs` + `Directories/DeferredPromotionRedemptionLedger.cs` |
| `Contracts/Merchandising/MerchandisingCampaignContracts.cs` | split | runtime models stay in Contracts; storefront eligibility rule moves to Domain; foreign `SalesChannel` removed from the public signature |
| `MerchandisingCampaignDirectory.cs` (572), `MerchandisingCampaignAdminComposer.cs` (468), `PromotionDefinition.cs` (426), `MerchandisingCampaignQuery.cs` (345) | keep | cohesive, `OVERSIZED_ONLY` at most; no cosmetic split |

No new god-file may be created by the splits; each resulting file must have one reason to change.

## Exact target paths/namespaces (W1 → W2 handoff)

```text
Tooba.Promotion.Contracts/
  Errors/PromotionErrorCodes.cs        -> namespace Tooba.Promotion.Contracts.Errors   (W1: single canonical home + KnownCodes + IsKnown)
  Errors/PromotionErrorResourceSet.cs  -> namespace Tooba.Promotion.Contracts.Errors   (W1/W2: IErrorResourceSet)
  Resources/PromotionErrors.resx       -> Tooba.Promotion.Contracts.Resources          (embedded resource, EN)
  Resources/PromotionErrors.fa.resx    -> Tooba.Promotion.Contracts.Resources          (embedded resource, FA)
  Checkout/*.cs                        -> namespace Tooba.Promotion.Contracts.Checkout (unchanged)
  Merchandising/*.cs                   -> namespace Tooba.Promotion.Contracts.Merchandising (foreign type removed from signature)
Tooba.Promotion.Domain/
  Aggregates/, Events/, Merchandising/, Policies/, ValueObjects/   (unchanged folders)
  Merchandising/MerchandisingCampaignStorefrontEligibility.cs  (W1: rule relocated out of Contracts)
  Merchandising/IMerchandisingCampaignPromoPrice.cs            (W1: single authoritative declaration)
Tooba.Promotion.Application/
  Promotions/{Commands,Queries,Models,Ports,Validators}/*.cs
  Merchandising/{Models,Ports}/...   + Merchandising/Admin/{Commands,Queries,Validators}/*.cs
  Checkout/CheckoutPromotionAdapter.cs
  Composition/PromotionOperation.cs    (W1: dual-mechanism typed-fault seam)
  Errors/PromotionValidationCodes.cs   (W1: stable validation machine codes)
Tooba.Promotion.Infrastructure/
  Persistence/ + Persistence/Migrations/  (unchanged)
  Directories/{PromotionDirectory,OpenPromotionUseCaseGuard,DeferredPromotionRedemptionLedger,MerchandisingCampaignDirectory}.cs
  Queries/MerchandisingCampaignQuery.cs
  Merchandising/MerchandisingCampaignAdminComposer.cs  (W1: tracer + stable codes)
  Adapters/, Development/, Events/, Messaging/
  DependencyInjection/PromotionModule.cs  (or Infrastructure root PromotionModule.cs — W2 decides + allowlists)
Tooba.Promotion.Endpoints/
  PromotionEndpointModule.cs           (root allowlist entry)
  Admin/, Seller/, Errors/, Resources/
```

Namespace must exactly match the physical path in every case.

## Behavior-preservation checklist

- Routes/methods: all 21 routes and HTTP verbs preserved exactly.
- Response shapes/status codes: preserved; the only additive change is the `Location` header on
  `POST /v1/seller/promotions` (canonical `api.Created`), status stays 201 and the body stays the raw DTO.
- Stable error codes: `promotion.missing`, `promotion.name.required`, `promotion.coupon.required`,
  `promotion.mutation.rejected`, `promotion.activate.rejected`, `promotion.deactivate.rejected`,
  `merchandising.campaign.missing`, `campaign.validation`, `campaign.publish`, `campaign.member`,
  `campaign.reorder`, `campaign.price` — string values byte-identical, HTTP status and classification
  unchanged; the nine Persian throws map onto the existing merchandising codes (`campaign.validation`,
  `campaign.publish`, `campaign.member`, `campaign.reorder`, `campaign.price`) so no new client-visible
  code appears.
- Localization keys/semantics: 12 keys gain real EN/FA resources under `LocalizationKey = code`; no
  published key renamed or repurposed.
- Request/response DTO semantics: `PromotionReference`, `PromotionMutationInput`,
  `AdminMerchCampaign*`, `CheckoutPromotionEvaluation*`, `MerchandisingCampaignRuntimeModel` unchanged.
- Business rules/state transitions: `PromotionDefinition` create/activate/change/update/expire,
  `IsEffectiveAt`, `IsEligible`, `ComputeDiscount`, `PromotionRounding`, `PromotionCouponNormalizer`,
  campaign lifecycle (draft → published → archived), membership ordering, translation upsert — unchanged.
- Authorization: `IPromotionAdminAuthorizer` (module-owned over `IAdminPanelAccess`) and
  `IPromotionSellerAuthorizer` (Host adapter over `ISellerPanelAccess`) behavior unchanged; the accepted
  Host security adapter stays where it is.
- Persistence/schema: `promotion` schema, tables, columns, indexes, constraints, both migration ids,
  Up/Down, snapshot — untouched.
- Outbox: 4 domain events → 4 integration events with identical `promotion.*.v1` type names and payloads.
- Background workers: none.
- Telemetry/trace: `IModuleCallTracer` decoration preserved and extended to the Admin composer path; no
  new activity/meter.
- Correlation: unchanged (no Promotion-side correlation surface).
- Tenant/store scoping: `ToobaNpgsql.ResolveForContext(ICurrentCommerceContext, IDatabaseConnectionResolver)`
  and `ResolveStoreId()` store-alpha mapping unchanged.
- Development seed: same five stable campaign ids, same member selection, same promo-price fractions, same
  idempotency; only the Inventory access path changes (Contracts instead of Application/Domain).
- Public Contracts: no member removed; `MerchandisingPriceScope` channel representation changes type
  (Promotion-owned) — consumers (`Catalog`) must be updated in the same wave.

## Migration order

1. **W1 (Migrate)** — create the single canonical `Contracts/Errors/PromotionErrorCodes.cs` with
   `KnownCodes` + `IsKnown(string?)`, retire the two dead cross-cutting duplicate declarations, add
   `Contracts/Errors/PromotionErrorResourceSet.cs` + `Contracts/Resources/PromotionErrors.resx` /
   `.fa.resx` and register the resource set once; add `Application/Composition/PromotionOperation.cs`
   (declared-code + `SemanticException` dual mechanism, unknown codes/exceptions propagate) and replace
   `PromotionExceptionMapper` message heuristics at all 5 promotion command call sites; replace the nine
   Persian throws in the Admin composer with declared codes and remove the bare
   `catch (InvalidOperationException)` conversions in the 8 merchandising handlers; convert the seller
   create endpoint to `api.Created`; remove the `Tooba.Inventory.Application` + `Tooba.Inventory.Domain`
   references and re-route `MerchandisingCampaignDevelopmentSeed` through
   `IInventoryDevelopmentSeedGateway` (extending it in Inventory only if the current three methods cannot
   express "drain to zero" — minimum destination-module change); de-foreign the
   `MerchandisingPriceScope` public signature; remove the two stale/illegal `tmar-app-to-app-edges.json`
   entries; add the 19 transport validators + `PromotionValidationCodes`; split the 6 cohesion-violating
   files; decorate the Admin composer with `IModuleCallTracer`; add the durable W1 migrate guard.
2. **W2 (Structure)** — normalize every project to capability-first shallow folders
   (`Application/Promotions/*`, `Application/Merchandising/*`), exact path↔namespace, enforce root
   allowlists, verify `/Modules/Promotion/` solution grouping, resolve the `DependencyInjection/` vs
   project-root DI placement against the certified precedent, update the manifest physical allowlists /
   forbidden lists honestly, and add the durable W2 structure guard.
3. **W3 (Certify)** — audit against `ARCH-COMPLETE-002`, add the durable cert guard, promote the manifest
   entry (`structureCertified: true`, `lockVersion`, `certificationNote`, allowlists), reconcile the
   legacy `completeReferenceModules` Promotion block and the `TmarDurableGuardTests` 12-module list
   honestly, and record the SoT + Master Recovery checkpoint.

## Verification plan

- **W0**: no build/test required beyond the recorded baseline (see known pre-existing failure).
- **W1**: build `Tooba.Promotion.Application`, `Tooba.Promotion.Infrastructure`, `Tooba.Promotion.Endpoints`,
  `Tooba.Promotion.Tests` and the affected `Tooba.Inventory.Contracts`; run `Tooba.Promotion.Tests`
  (module guard must go from red to green), `TmarFoundationTests.App_to_app_edges_do_not_expand_beyond_baseline`,
  `ErrorCatalogUniqueCodeGuardTests`, and the new `PromotionModuleAmsc001W1MigrateGuardTests` pinning the
  canonical code home + `IsKnown` + declared count + `PromotionOperation` shape + validator classification
  matrix + absence of the Inventory Application/Domain edge + `api.Created` on seller create.
- **W2**: build all Promotion projects + Host; run `Tooba.Promotion.Tests` + the new structure guard
  (capability-first, single-file-leaf, namespace exactness, root allowlist, solution grouping, stale/dup
  copies); parse `Tooba.slnx`.
- **W3**: build Host + run `Tooba.Promotion.Tests`, `HostModuleEndpointOwnershipTests`,
  `TmarDurableGuardTests`, `ErrorCatalogUniqueCodeGuardTests`, the W1/W2/W3 Promotion guards, and the
  focused Host Promotion/Cart/Checkout/Merchandising filters
  (`PromotionFoundationTests`, `PromotionPanelTests`, `PromotionCampaignSourceTests`,
  `MerchandisingCampaign*Tests`, `CheckoutImplW5PromotionContractTests`, `CampaignCartPriceIntegrityTests`).
- **Known pre-existing failure to record honestly**: `Tooba.Promotion.Tests` is **red at the W0 baseline**:
  `PromotionArchitectureGuardTests.Promotion_golden_boundaries_and_physical_layout_remain_clean` fails
  because `AllowedInfrastructureFolders` omits `Development`, while
  `Infrastructure/Development/MerchandisingCampaignDevelopmentSeed.cs` legitimately exists. 7/8 tests
  pass. This is stale-guard drift (the `Development/` folder is a canonical Infrastructure folder per the
  Structure skill) and must be corrected inside the AMSC run without weakening the guard's intent.

## Certification blockers

1. `ILLEGAL` foreign `Application`/`Domain` edges `Promotion.Infrastructure → Inventory.Application` and
   `→ Inventory.Domain` (two project references, two type-usage sites).
2. `STRING_HEURISTIC` failure classification (`PromotionExceptionMapper` message dictionary, 12 legacy
   aliases) and bare `catch (InvalidOperationException)` conversion in the 8 merchandising handlers.
3. `HARDCODED_TEXT`: nine Persian user-facing exception messages in `MerchandisingCampaignAdminComposer`.
4. `MISSING_INFRASTRUCTURE_USE`: no `IErrorResourceSet`, no bilingual `.resx`, so no Promotion error text is
   localizable.
5. `UNREGISTERED_CODES`/dead duplicates: two cross-cutting code declarations owned by Foundation.
6. `RAW_RESULTS`: `POST /v1/seller/promotions` success path bypasses `ApiResponseFactory`.
7. `Validator-Coverage-State = GAPS`: 0/21 requests classified in code, no validator, no validation codes,
   no durable classification guard.
8. `Contracts-Boundary-State = VIOLATION`: duplicated `IMerchandisingCampoaignPromoPrice` identity, a
   domain rule inside the Contracts assembly, and a foreign `Offer.Contracts` type in a public Contracts
   signature.
9. `File-Cohesion-State = MULTI_RESPONSIBILITY_COHESION_VIOLATION` in 6 files.
10. Structure: `Application/Commands/<UseCase>` and `Application/Queries/<UseCase>` single-file leaf
    folders (9 of them) — `OVER_FOLDERED` + `TECHNICAL_AXIS_FIRST` for a two-capability module.
11. No certified manifest entry / no `structureLock.certifiedModules` membership (expected; W3 owns the
    promotion).
12. Legacy `completeReferenceModules` Promotion block claims `COMPLETE_REFERENCE_PATTERN` while 1–11 are
    unresolved — W3 must reconcile it honestly.
13. Stale module guard assertion (`AllowedInfrastructureFolders` missing `Development`) blocks a clean
    focused-validation record; repaired inside the AMSC run without weakening intent.

Analysis-only wave: zero production change, zero schema change, zero manifest mutation.
