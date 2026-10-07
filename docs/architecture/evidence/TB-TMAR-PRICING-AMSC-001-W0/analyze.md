# TB-TMAR-PRICING-AMSC-001-W0 — Analyze (tooba-architecture-analyze)

## Scope

`src/backend/Modules/Pricing/Tooba.Pricing.*` — full AMSC re-standardization under `ARCH-COMPLETE-002`
(W0 Analyze → W1 Migrate → W2 Structure → W3 Certify), starting head `a1d9ecfb` on `main`
(`HEAD == origin/main`). Pricing has **no** prior `structureCertified` entry in
`docs/architecture/tmar-module-structure-manifests.json` and is **not** a member of
`structureLock.certifiedModules` (24 members, Pricing absent); it also has no prior AMSC/AMC lineage block in
`tmar-current-state.json`. This run therefore treats the current repository state as the un-certified baseline and
records the first AMSC lineage for the module. The final objective is stated up front: Pricing must be extractable
as an independent microservice, so **zero invalid coupling** is the hard gate.

## Structured State Fields

1. **Foundation-State**: `FOUNDATION_READY` — 5 production projects (`Contracts`, `Domain`, `Application`, `Infrastructure`, `Endpoints`) plus `Tooba.Pricing.Tests`, all grouped under `/Modules/Pricing/` in `src/backend/Tooba.slnx` (lines 141–148). No parallel/legacy root dump; every project has a real destination for the W1/W2 repairs.
2. **Ownership-State**: `correct` — authored-price truth (amount/currency/market/channel/validity/qualifier/status) is Pricing-owned. Tax is Tax-owned, FX is not Pricing's, offer selection/offer lifecycle is Offer-owned, promotion definition/discount evaluation is Promotion-owned, cart line state is Cart-owned. No responsibility in the module belongs to another module and none of Pricing's truth is implemented elsewhere (verified by reading all 29 production files plus their callers).
3. **File-Cohesion-State**: `COHESIVE` — 29 production `.cs` files (EF migrations + snapshot excluded). Largest: `Infrastructure/Adapters/PriceDirectory.cs` 477 LOC (single `IPriceDirectory`/`IPriceLookupGateway`/`ISellerOfferPricingGateway`/`IPriceQueryGateway` implementation over one schema, guard `<800`), `Domain/Aggregates/AuthoredPrice.cs` 258 LOC (single aggregate), `Infrastructure/Outbox/PricingOutboxRegistration.cs` 105 LOC, `Domain/Events/PricingDomainEvents.cs` 105 LOC. No `ARCH-SIZE-001` baseline entry required; no multi-responsibility god-file; no artificial split.
4. **Oversized/God-File-State**: `NONE` — each large file has exactly one reason to change. `PriceDirectory.cs` at 477 LOC is `OVERSIZED_ONLY`-class but cohesive (one port family, one schema, one Offer-lookup seam); no split planned.
5. **Localization-State**: `CANONICAL` — `PricingErrorResourceSet : IErrorResourceSet` (owns the `pricing.` keyspace) + `PricingErrors.resx` / `PricingErrors.fa.resx` (11 keys each, byte-distinct EN/FA text for all 11 declared codes) + `PricingErrorCatalogContributor` registering 11 `ErrorDescriptor`s with `LocalizationKey = code` and explicit HTTP status (400/404/409). Registered once in `PricingEndpointModule.AddPricingEndpointPresentation()`. No hard-coded Persian/English user-facing prose anywhere in the module (zero Persian string literals in production sources), no `ex.Message` classification, no `Accept-Language` parsing. Note for W2: the resource set physically lives in `Tooba.Pricing.Endpoints/Resources/` (the `Order`/`Cart`/`Offer`/`AccessControl`/`Content`/`Fulfillment`/`Support`/`AddressBook`/`CustomerProfile`/`Story`/`Wallet`/`Reviews` precedent), while the newer `Media`/`Inventory`/`Party`/`Payment`/`PageComposition`/`OperatorProfile`/`Catalog`/`Identity`/`Localization`/`BulkInquiry`/`Wishlist`/`UserPreference`/`ProductQnA`/`CustomerProfile` precedent places the resource set next to the stable codes in `Contracts/Errors/`. Both are accepted in-repo precedents; W2 may normalize to `Contracts/Errors/` + `Contracts/Resources/` for a single self-contained code+text boundary, and if it does so the Endpoints contributor/resource files are retired with the catalog registration preserved.
6. **API-Result-Pattern-State**: `CANONICAL` — no ad-hoc result building in the module: zero `Results.Json` / `Results.BadRequest` / `Results.Problem` / local `ProblemDetails` builder / `catch`-and-map in Pricing production sources. `ISellerOfferPricingGateway.SetPriceAsync` returns `Result` and the *consumer* (`Offer.Endpoints.Seller.OfferSellerEndpoints.WriteOfferPriceAsync`) maps it with `api.From(result)`. Pricing itself owns no HTTP route, so it owns no `ApiResponseFactory` call site today; W1's `PricingOperation` seam must keep returning `Result` (never `IResult`) so the boundary stays correct.
7. **Stable-Error-Code-State**: `CATALOGUED_11_DECLARED_11_REGISTERED_DUPLICATED_DECLARATION_NO_ISKNOWN_GUARD` — three real defects found:
   - **(a) Duplicated declaration.** The same 11 constants are declared **twice** with identical string values: `Tooba.Pricing.Contracts/PricingErrorCodes.cs` (`namespace Tooba.Pricing.Contracts`) and `Tooba.Pricing.Domain/Errors/PricingErrorCodes.cs` (`namespace Tooba.Pricing.Domain`). Two parallel stable-code identities for one module is a boundary defect and a microservice blocker.
   - **(b) Wrong home / no canonical `Errors` folder.** `Tooba.Pricing.Contracts` has **no `Errors/` folder**; the boundary code file sits at the project root and the `Ports/`, `Dtos/` folders declare the *project-level* namespace (`Tooba.Pricing.Contracts`) instead of path-derived namespaces. The certified precedent keeps stable codes at `Contracts/Errors/<Module>ErrorCodes.cs` with the declared-code guard next to them.
   - **(c) No declared-code guard.** `PricingErrorCodes` has no `KnownCodes` HashSet and no `public static bool IsKnown(string? code)`; the certified seam pattern (`Media`/`Inventory`/`Notification`/`OperatorProfile`/`PageComposition`/`Party`/`Payment`) requires both so the typed-fault seam can classify by declared code only.
   - Descriptor ownership is honest: exactly 11 declared → 11 registered by the single `PricingErrorCatalogContributor`; no foreign-owned code is re-registered by Pricing and no Pricing code is registered by another module.
8. **Logging-State**: `CANONICAL` — zero log call sites in Pricing production (no `ILogger<T>`, no `Console.WriteLine`, no `Debug.WriteLine`, no hand-rolled writer). Nothing to repair; nothing to add (the module owns no endpoint/worker that needs operational logging).
9. **Sensitive-Logging-State**: `NONE` — no logging at all in the module, therefore no password/token/OTP/header/cookie/payload exposure.
10. **OpenTelemetry-State**: `CANONICAL` — no second `ActivitySource`, no second `Meter`, no direct `StartActivity(` in production. The only cross-module call (`PriceDirectory.FindOfferAsync` → Offer) is decorated through the canonical `IModuleCallTracer.Begin("Pricing","Offer","LookupOffer")` with `SetOk()`/`SetError(ex)`.
11. **Correlation-Trace-State**: `CANONICAL` — no parallel correlation mechanism, no manual `traceparent` parsing, no hand-rolled correlation id. Pricing exposes no HTTP response surface of its own, so the canonical `ProblemDetailsContextProvider` path is not bypassed.
12. **CQRS-State**: `NOT_APPLICABLE_TODAY_REQUIRED_AS_STRUCTURE_REPAIR` — Pricing has **zero** `IRequest<,>` / `IRequestHandler<,>` / MediatR request, and that is *correct* for its current shape: it is an internal capability provider reached only through `Tooba.Pricing.Contracts` ports, and it owns **zero** HTTP routes (`MapPricingModule` maps the empty group `_ = app.MapGroup("/v1/pricing")` with no endpoints). The seller price write is owned by Offer (`POST|PUT /v1/seller/offers/{offerId}/price` → `SetOfferPriceCommand` → `ISellerOfferPricingGateway`). No `ISender` bypass exists because there is no Pricing endpoint. W1/W2 must therefore not invent CQRS ceremony for Pricing; if W2 chooses to normalize `Application/Ports/IPriceDirectory.cs` it must keep the port surface Application-owned.
13. **Validator-Coverage-State**: `EXHAUSTIVE_0_OF_0_NO_VALIDATOR_REQUIRED` — Pricing owns no endpoint-reachable request, so the required-validator matrix is empty. Transport validation of the only Pricing-reachable write (`SetSellerOfferPrice`) is owned by Offer's `SetOfferPriceCommandValidator`; Pricing additionally re-validates its own boundary inputs *inside* the directory (`MarketCode.TryParse` / `CurrencyCode.TryParse` → `Result.Failure(marketError/currencyError)`), which is business/domain validation and correctly not FluentValidation. A durable classification guard for a module with zero HTTP requests is not applicable; the Pricing architecture guard already pins the `Result`-not-exception contract for `SetPriceAsync`.
14. **Contracts-Boundary-State**: `VIOLATION` — two defects: (i) the stable-code identity is duplicated across `Contracts` and `Domain` instead of having one canonical `Contracts/Errors` home; (ii) `Contracts/{Dtos,Ports}` and `Domain/*` declare the project-level namespace instead of the path-derived namespace (`Tooba.Pricing.Contracts.Dtos`, `Tooba.Pricing.Contracts.Ports`, `Tooba.Pricing.Domain.Aggregates`, …), which fails `PATH_NAMESPACE_ALIGNMENT`. `Contracts` is otherwise a genuine boundary surface (cross-module DTOs/ports only, no EF/entity/implementation leakage).
15. **Cross-Module-Coupling-State**: `ILLEGAL` (one inbound edge) — outbound edges are `LEGAL_CONTRACTS_ONLY` (`Offer.Contracts` for `IOfferLookupGateway`/`SalesChannel`/`OfferErrorCodes`, plus `BuildingBlocks`/`ModuleContracts`/`Persistence` foundations); zero foreign `Application`/`Infrastructure`/`Domain` reference in any Pricing project. **Inbound defect:** `Tooba.Promotion.Infrastructure` references `Tooba.Pricing.Application` and consumes `Tooba.Pricing.Application.IPriceDirectory` + `Tooba.Pricing.Application.IPriceQueryGateway` in two files (`Merchandising/MerchandisingCampaignAdminComposer.cs`, `Development/MerchandisingCampaignDevelopmentSeed.cs`). Promotion's own architecture guard asserts this must **not** exist (`PromotionArchitectureGuardTests.Infrastructure_uses_contracts_not_foreign_application`: `Assert.DoesNotContain(refs, r => r.Contains("Pricing.Application"))`) — the guard is currently failing against the repository, which is direct proof of the violation. It is also a microservice blocker in both directions: extracting Pricing breaks Promotion, and Promotion can never be extracted while it compiles against a foreign Application assembly. W1 repairs it by moving the *write* port to `Tooba.Pricing.Contracts` (implemented by `PriceDirectory`, which already implements the other three Pricing Contracts ports) and reusing the existing `Tooba.Pricing.Contracts.IPriceQueryGateway`, then removing the `Tooba.Pricing.Application` project reference from `Tooba.Promotion.Infrastructure`. All other inbound consumers (Cart, Catalog, Offer, Order, ProductWorkspace, Host.Tests) already reference `Tooba.Pricing.Contracts` only.
16. **Cross-Module-Join-State**: `NONE` — the single `PricingDbContext` touches only the `pricing` schema plus its own Outbox table (`OutboxMessageMapping.Map(modelBuilder, Schema)`); no foreign `DbSet`, no navigation crossing ownership, no raw SQL joining foreign schemas, no distributed transaction. Cross-module data for admin grids is composed by the consumer through Contracts ports.
17. **Persistence-Ownership-State**: `CORRECT` — own schema `pricing`, own `PricingDbContext` + `PricingDbContextFactory`, own single migration `20260823085546_InitialPricing` + designer + snapshot, own `PricingOutboxRegistration : IOutboxModuleRegistration`, own `IPricingSchemaMigrator` + `PricingModuleMigration` so Host bootstraps never type `PricingDbContext`.
18. **Endpoint-Ownership-State**: `MODULE_OWNED_ZERO_ROUTES_HOST_ZERO` — `PricingEndpointModule.MapPricingModule` creates the empty group `/v1/pricing` with no endpoint; `Host/Program.cs` only calls `AddPricingEndpointPresentation()` (line 103) and `MapPricingModule()` (line 426). Host Pricing HTTP ownership ZERO, no duplicate route ownership, no Host `Pricing` folder. The only Pricing-reachable HTTP write is Offer-owned and calls the Pricing Contracts port.
19. **Host-Residue-State**: `ALLOWED_COMPOSITION_ROOT` — exactly three references, all composition: `Program.cs` (`AddPricingEndpointPresentation()`, `MapPricingModule()`, `Tooba.Pricing.Endpoints` using) and `Composition/ToobaModuleComposition.cs` (`new PricingModule()`); `Host/Tooba.Host.csproj` references `Pricing.Application`/`Pricing.Endpoints`/`Pricing.Infrastructure` for that composition. `ILLEGAL_BUSINESS_AUTHORITY` / `ILLEGAL_PERSISTENCE_AUTHORITY` / `ILLEGAL_ENDPOINT_OWNERSHIP` = ZERO; no Host production file reads `PricingDbContext`. Host final closure (`HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED` / `HOST_ROOT_FINAL_CERTIFIED`) is preserved and untouched.
20. **Schema-Migration-State**: `UNCHANGED` — no migration file is planned to be touched in any wave; migration id `20260823085546_InitialPricing`, order, Up/Down, and snapshot semantics stay byte-identical. W1/W2 are code-and-documentation waves only.
21. **Behavior-Preservation-Risk**: `LOW` — W1 removes a *duplicate* declaration (identical string values, so no wire-visible change), adds a dormant `KnownCodes`/`IsKnown` guard plus an additive `Composition/PricingOperation` seam that is only consumed by the decoupled Promotion call sites, and repoints two Promotion call sites from `IPriceDirectory`/`IPriceQueryGateway` (Application) to the same `IPriceDirectory`/`IPriceQueryGateway` (Contracts) — the exact same methods with the same implementations, resolved through the same `PriceDirectory` singleton. W2 is a physical move of code files plus a namespace correction (`Tooba.Pricing.Contracts.Ports.*` etc.) with zero logic change; the 11 stable code values, the 11 descriptors, the 11×2 resource entries, the `Result`/`SemanticError` classification, the `/v1/pricing` empty group, the `pricing` schema, the Offer lookup seam and the outbox translation all remain identical. Residual risk is compile-surface only (namespace-qualified references in consumers), which the focused builds cover.
22. **Canonical-Reference-Used**: `Media`/`Inventory`/`Notification`/`OperatorProfile`/`PageComposition`/`Party`/`Payment` `Contracts/Errors/<Module>ErrorCodes.cs` + `KnownCodes`/`IsKnown(string?)` + `Application/Composition/<Module>Operation.cs` dual-mechanism typed-fault seam; `Party`/`Payment`/`Inventory` `IErrorResourceSet` + bilingual `.resx` pair + `ErrorDescriptor(LocalizationKey = code)` catalog; `Promotion`'s own `PromotionArchitectureGuardTests` as the authoritative assertion of the inbound Contracts-only rule; `BuildingBlocks` `Result`/`Result<T>`/`SemanticError`/`SemanticException`/`IErrorDefinitionCatalog`; the module's own `PricingModule` Contracts-port registration idiom.
23. **Final-Disposition**: `READY_TO_MIGRATE` (bounded: inbound Contracts-only repair + single canonical stable-code home with declared-code guard + typed-fault seam + path↔namespace/structure normalization; zero ownership move of business behavior, zero schema change).

## Target analyzed

`src/backend/Modules/Pricing/` — 6 projects, 29 production `.cs` files (EF migrations/snapshot excluded), 2 `.resx` files, 1 EF migration + designer + snapshot.

| Project | Production files | Root `.cs` |
|---|---|---|
| `Tooba.Pricing.Contracts` | 8 | `PricingErrorCodes.cs`, `SellerOfferPricingContracts.cs` |
| `Tooba.Pricing.Domain` | 8 | none |
| `Tooba.Pricing.Application` | 1 | none |
| `Tooba.Pricing.Infrastructure` | 9 | none |
| `Tooba.Pricing.Endpoints` | 3 | `PricingEndpointModule.cs` |
| `Tooba.Pricing.Tests` | 6 | n/a |

## Responsibility map

| Area | Files | Responsibilities | Verdict |
|---|---|---|---|
| `Contracts/PricingErrorCodes.cs` (root) + `Domain/Errors/PricingErrorCodes.cs` | 2 | CONTRACT stable codes — **declared twice** | MUST_RELOCATE + MUST_DEDUPE → one `Contracts/Errors/PricingErrorCodes.cs` |
| `Contracts/Dtos/CurrencyCode.cs` | 1 | CONTRACT value object (ISO currency + scale) | COHESIVE (namespace must become path-derived) |
| `Contracts/Ports/*` (5 files) | 5 | CONTRACT ports: lookup, query, campaign authority, development seed, schema migrator | COHESIVE (namespace must become path-derived) |
| `Contracts/SellerOfferPricingContracts.cs` (root) | 1 | CONTRACT seller price-write request + gateway | COHESIVE (root placement allowed only if allowlisted) |
| `Domain/Aggregates/AuthoredPrice.cs` | 1 | DOMAIN_RULE (authored price aggregate + invariants) | COHESIVE |
| `Domain/ValueObjects/{Money,MarketCode,AuthoredCurrency}.cs` | 3 | DOMAIN_RULE | COHESIVE |
| `Domain/Enums/{PriceChannel,PriceEnums}.cs` | 2 | DOMAIN_RULE | COHESIVE |
| `Domain/Events/PricingDomainEvents.cs` | 1 | DOMAIN_RULE (4 domain events) | COHESIVE |
| `Application/Ports/IPriceDirectory.cs` | 1 | APPLICATION write/use-case port + use-case guard seam | COHESIVE — must stay Application-owned; the *Promotion-consumed* write surface moves to Contracts in W1 |
| `Infrastructure/Adapters/PriceDirectory.cs` | 1 | PERSISTENCE + INTEGRATION_ADAPTER (4 ports over one schema + traced Offer lookup) | COHESIVE (`OVERSIZED_ONLY`-class, one reason to change) |
| `Infrastructure/Adapters/{PricingDevelopmentSeedGateway,PricingSchemaMigrator,PricingModuleMigration}.cs` | 3 | DEVELOPMENT_SEED / PERSISTENCE composition | COHESIVE |
| `Infrastructure/DependencyInjection/PricingModule.cs` | 1 | PRESENTATION_COMPOSITION / module DI | COHESIVE |
| `Infrastructure/Persistence/*` + `Migrations/*` + `Outbox/PricingOutboxRegistration.cs` | 5 | PERSISTENCE | COHESIVE |
| `Infrastructure/Events/PricingEvents.cs` | 1 | CONTRACT (4 integration events) | COHESIVE |
| `Endpoints/PricingEndpointModule.cs` | 1 | HTTP composition (empty `/v1/pricing` group) | COHESIVE |
| `Endpoints/Errors/PricingErrorCatalogContributor.cs` | 1 | CONTRACT catalog contribution | COHESIVE |
| `Endpoints/Resources/PricingErrorResources.cs` + `.resx` + `.fa.resx` | 3 | LOCALIZATION | COHESIVE (W2 may relocate to `Contracts/Errors` + `Contracts/Resources` to match the newer certified precedent) |

No `MUST_SPLIT` decision. Two `MUST_RELOCATE`/`MUST_DEDUPE` decisions (stable-code identity, inbound Promotion coupling) and one path↔namespace normalization decision.

## Ownership map

| Responsibility | Owner | Evidence |
|---|---|---|
| Authored price amount/currency/market/channel/validity/qualifier/status | **Pricing** | `AuthoredPrice`, `pricing` schema, `PricingDbContext` |
| Base vs merchandising-campaign price qualification | **Pricing** | `PriceQualifierKind` + `CreateMerchandisingCampaign` |
| Price overlap / validity / currency-immutability / retired-state invariants | **Pricing** | `AuthoredPrice` + `PriceDirectory.EnsureNoOverlapAsync` |
| Seller price write entry (route + command + validator) | **Offer** (consumes Pricing port) | `OfferSellerEndpoints.WriteOfferPriceAsync` → `SetPriceAsync` |
| Tax/VAT computation | **Tax** (explicitly not Pricing) | module docs + no tax code in Pricing |
| FX conversion | **not Pricing** | `Money`/`AuthoredCurrency` doc comments; no FX code |
| Campaign definition/lifecycle/membership | **Promotion** (consumes Pricing price ports) | `MerchandisingCampaign*` |
| Cart line state/currency selection | **Cart** (consumes Pricing quote ports) | `CartLineCurrency`, `CartQuoteValidator` |
| Offer existence/seller ownership/status | **Offer** (via `IOfferLookupGateway`) | `PriceDirectory.FindOfferAsync` |
| Schema `pricing` + migrations + outbox | **Pricing** | `PricingDbContext`, `PricingOutboxRegistration` |
| `/v1/pricing` group composition | **Pricing** (`PricingEndpointModule`) | Host calls `MapPricingModule()` |

## MUST_SPLIT decisions

None. No Pricing file mixes responsibilities owned by two modules. `PriceDirectory.cs` (477 LOC) implements four Pricing ports over one Pricing schema with one traced Offer lookup — single responsibility, one reason to change.

## Current illegal dependencies

| # | Edge | Kind | Evidence | Repair |
|---|---|---|---|---|
| 1 | `Tooba.Promotion.Infrastructure` → `Tooba.Pricing.Application` (project reference) | foreign `Application` | `Tooba.Promotion.Infrastructure.csproj` `<ProjectReference Include="..\..\Pricing\Tooba.Pricing.Application\..." />` | remove reference; consume `Tooba.Pricing.Contracts` only |
| 2 | `MerchandisingCampaignAdminComposer` uses `Tooba.Pricing.Application.IPriceDirectory` | foreign `Application` type | `using Tooba.Pricing.Application;` + `private readonly IPriceDirectory _prices;` (write: `ChangeAmountAsync`, `CreateCampaignPriceAsync`, `ActivateAsync`) | move the write port to `Tooba.Pricing.Contracts` (`IPriceDirectory`), implemented by the same `PriceDirectory` |
| 3 | `MerchandisingCampaignDevelopmentSeed` uses `Tooba.Pricing.Application.IPriceDirectory` | foreign `Application` type | `using Tooba.Pricing.Application;` + `provider.GetRequiredService<IPriceDirectory>()` | same Contracts port (already resolves from `IPriceQueryGateway` for reads) |

Self-contradiction proof: `Tooba.Promotion.Tests/Architecture/PromotionArchitectureGuardTests.cs` asserts `Assert.DoesNotContain(refs, r => r.Contains("Pricing.Application"))` and `Assert.Contains(refs, r => r.Contains("Pricing.Contracts"))`. The guard is red on the current repository.

No other illegal edge exists. Outbound Pricing edges (`Offer.Contracts`, `BuildingBlocks`, `ModuleContracts`, `Persistence`) are legal; `Tooba.Pricing.Domain` currently references only `BuildingBlocks` (W1 may add its **own** `Tooba.Pricing.Contracts` for the single canonical code home, which is own-module layering, never a foreign Contracts reference).

## Cross-module join inventory

`NONE`. Single `PricingDbContext` → `pricing` schema + own Outbox table. No foreign `DbSet`, no navigation crossing ownership, no `FromSql`/raw SQL, no cross-module transaction, no shared mutable entity. Consumer-side admin/workspace grids compose Pricing rows through `IPriceQueryGateway` (Contracts) instead of joining.

## Contracts-only replacement map

| Need | Current | Replacement (W1) |
|---|---|---|
| Promotion writes/activates campaign prices | `Pricing.Application.IPriceDirectory` | `Pricing.Contracts.IPriceDirectory` (same method names/signatures/values, same `PriceDirectory` implementation) |
| Promotion reads existing campaign/base prices | `Pricing.Contracts.IPriceQueryGateway` (already Contracts) | unchanged |
| Offer seller price write | `Pricing.Contracts.ISellerOfferPricingGateway` | unchanged |
| Cart/Order/Catalog/ProductWorkspace price reads | `Pricing.Contracts.IPriceLookupGateway` / `IPriceQueryGateway` / `ICampaignCartPriceAuthority` | unchanged |
| Catalog development seed | `Pricing.Contracts.IPricingDevelopmentSeedGateway` | unchanged |
| Host bootstrap migration | `Pricing.Contracts.IPricingSchemaMigrator` | unchanged |

No new shared/god-contract project is created; the repair reuses the module's own existing Contracts assembly.

## CQRS/MediatR gaps

None for the current shape. Pricing is an internal capability provider with zero HTTP routes and zero endpoint-reachable requests; it must not receive invented MediatR ceremony. The single Pricing-reachable write is dispatched by Offer through `ISender` (`SetOfferPriceCommand` → `ISellerOfferPricingGateway`), and no endpoint in the repository calls a Pricing directory directly. If a future task gives Pricing its own HTTP surface, that task owns the CQRS/validator/endpoint work.

## Validation classification matrix

| Request | Owner | Classification | Validator |
|---|---|---|---|
| `SetOfferPriceCommand` (`POST\|PUT /v1/seller/offers/{offerId}/price`) | Offer | `VALIDATOR_REQUIRED` | Offer-owned `SetOfferPriceCommandValidator` |
| Pricing-internal boundary inputs (`market`, `currency`, `amount`, `validFrom/validTo`, `campaignId`, `priceId`) | Pricing | business/domain validation — **not** FluentValidation | `MarketCode.TryParse` / `CurrencyCode.TryParse` → `Result.Failure`; `AuthoredPrice` invariants → `SemanticException(SemanticError(code))` |

Pricing-owned endpoint-reachable requests: **0**, therefore required-validators **0/0** and no module validator-coverage guard is applicable.

## Localization findings

- Canonical: `PricingErrorResourceSet` (`Owns("pricing.")`), `PricingErrors.resx` + `PricingErrors.fa.resx` with 11 keys each, all 11 declared codes registered as `ErrorDescriptor`s with `LocalizationKey = code` and explicit 400/404/409 status, severity `Warning`, safe English fallback.
- Zero hard-coded Persian/English user-facing strings in Pricing production (the only Persian text in the module is XML-doc prose, which is documentation, not user-facing output).
- Zero `ex.Message`/`exception.Message` classification; zero `Accept-Language` parsing.
- Structural note (W2 candidate): the resource set lives in `Tooba.Pricing.Endpoints/Resources/` (older precedent shared by Order/Cart/Offer/AccessControl/Content/Fulfillment/Support/AddressBook/CustomerProfile/Story/Wallet/Reviews) while the newer AMSC precedent (Media/Inventory/Party/Payment/PageComposition/OperatorProfile/Catalog/Identity/Localization/BulkInquiry/Wishlist/UserPreference/ProductQnA) places `IErrorResourceSet` + `.resx` under `Contracts/Errors` + `Contracts/Resources`. Either is accepted in-repo; W2 normalizes to the newer precedent so the stable code and its text form one self-contained boundary the module can carry out of the monolith.

## API result/error mapping findings

- `ISellerOfferPricingGateway.SetPriceAsync` returns `Tooba.BuildingBlocks.Results.Result`; expected failures are `Result.Failure(new SemanticError(code))` with declared codes (`pricing.amount.invalid`, `pricing.overlap`, and `Offer.Contracts.Errors.OfferErrorCodes.NotFound` for a foreign lookup miss — a foreign-owned code consumed without re-registration, which is correct).
- The consumer maps with `api.From(result)`. Pricing owns no `ApiResponseFactory` call site because it owns no route.
- No `Results.Json`/`Results.BadRequest`/`Results.Problem`, no local `ProblemDetails` builder, no `catch`-and-map, no message heuristics, no duplicate-suppression behaviour.
- Composed-catalog uniqueness is enforced repository-wide by `ErrorCatalogUniqueCodeGuardTests`; Pricing contributes 11 unique `pricing.*` codes.
- Defect: domain/infrastructure code throws `SemanticException` for expected failures inside directory methods (`Overlap`, `OfferMissing`, `RetireActivate/Immutable`, `CurrencyChangeForbidden`, `AmountInvalid`, `ValidityInverted`, `MarketInvalid`, `CurrencyInvalid`, `CurrencyDisplayUnit`). The caller is currently Promotion, which does not translate them, so these surface as exceptions rather than `Result` failures. W1's `PricingOperation` seam fixes that at the Promotion call sites **without** changing Pricing's own throwing semantics (no behaviour redesign), and the `SetPriceAsync` `Result` contract stays exactly as the existing guard pins it.
- Residual non-blocking: `AuthoredPrice.CreateCore` throws `InvalidOperationException("pricing.price.id_required")` for an empty `priceId` — the same defensive-invariant idiom used by every other module (`Notification`, `Promotion`, `Support`, `Tax`), which is *not* a registered stable code and never reaches a client. W1 may promote it to a declared code; it must not change the thrown type or message if it does not.

## Logging / sensitive-data findings

Zero log call sites, zero `Console.WriteLine`/`Debug.WriteLine`, no second logging framework, no secrets available to log. `NONE`.

## OpenTelemetry / correlation findings

- Canonical `IModuleCallTracer` decoration on the single cross-module call (`Pricing` → `Offer`, operation `LookupOffer`) with `SetOk()`/`SetError(ex)`.
- No direct `StartActivity(`, no second `ActivitySource`/`Meter`, no manual `traceparent` parsing, no competing correlation id.
- Pricing produces no HTTP response of its own, so no ProblemDetails trace/correlation path is bypassed.

## File cohesion / splitting plan

No split. `PriceDirectory.cs` (477 LOC) is cohesive; `AuthoredPrice.cs` (258 LOC) is one aggregate; `PricingDomainEvents.cs` (105 LOC) is the module's four domain events co-located by precedent. W1/W2 must not split cosmetically and must not create a god-file; `PricingOperation.cs` will be a ~50-line seam mirroring the certified `*Operation` shape.

## Exact target paths/namespaces (W1 → W2 handoff)

```text
Tooba.Pricing.Contracts/
  Errors/PricingErrorCodes.cs            -> namespace Tooba.Pricing.Contracts.Errors   (single canonical home; KnownCodes + IsKnown)
  Errors/PricingErrorResourceSet.cs      -> namespace Tooba.Pricing.Contracts.Errors   (W2: IErrorResourceSet moves next to the codes)
  Resources/PricingErrors.resx           -> Tooba.Pricing.Contracts.Resources          (W2: embedded resource, EN)
  Resources/PricingErrors.fa.resx        -> Tooba.Pricing.Contracts.Resources          (W2: embedded resource, FA)
  Dtos/CurrencyCode.cs                   -> namespace Tooba.Pricing.Contracts.Dtos     (W2 namespace repair)
  Ports/*.cs                             -> namespace Tooba.Pricing.Contracts.Ports    (W2 namespace repair)
  SellerOfferPricingContracts.cs         -> namespace Tooba.Pricing.Contracts          (root allowlist entry)
  Ports/IPriceDirectory.cs               -> namespace Tooba.Pricing.Contracts.Ports    (W1: write port moved from Application)
Tooba.Pricing.Domain/
  Aggregates/AuthoredPrice.cs            -> namespace Tooba.Pricing.Domain.Aggregates  (W2 namespace repair)
  ValueObjects/*.cs                      -> namespace Tooba.Pricing.Domain.ValueObjects (W2 namespace repair)
  Enums/*.cs                             -> namespace Tooba.Pricing.Domain.Enums       (W2 namespace repair)
  Events/PricingDomainEvents.cs          -> namespace Tooba.Pricing.Domain.Events      (W2 namespace repair)
  Errors/PricingErrorCodes.cs            -> DELETED (duplicate declaration retired)
Tooba.Pricing.Application/
  Ports/IPriceDirectory.cs               -> namespace Tooba.Pricing.Application.Ports  (W2 namespace repair; Application-internal port stays Application-owned)
  Composition/PricingOperation.cs        -> namespace Tooba.Pricing.Application.Composition (W1: dual-mechanism typed-fault seam)
Tooba.Pricing.Infrastructure/
  Adapters/PriceDirectory.cs             -> namespace Tooba.Pricing.Infrastructure.Adapters (W2 namespace repair)
  DependencyInjection/PricingModule.cs   -> namespace Tooba.Pricing.Infrastructure.DependencyInjection (W2 namespace repair)
  Outbox/PricingOutboxRegistration.cs    -> namespace Tooba.Pricing.Infrastructure.Outbox (W2 namespace repair)
Tooba.Pricing.Endpoints/
  PricingEndpointModule.cs               -> namespace Tooba.Pricing.Endpoints (root allowlist entry)
  Errors/PricingErrorCatalogContributor.cs / Resources/* -> W2 relocate-or-retain per localization precedent decision
Tooba.Promotion.Infrastructure/          -> remove Tooba.Pricing.Application reference; consume Contracts only (W1)
```

## Behavior-preservation checklist

- Routes/methods: `/v1/pricing` empty group preserved; Offer's `POST|PUT /v1/seller/offers/{offerId}/price` untouched.
- Response shapes/status codes: unchanged (Pricing owns none; the Offer-owned response is untouched).
- Stable error codes: all 11 string values byte-identical; zero rename/repurpose; descriptor HTTP status/classification unchanged.
- Localization keys/semantics: 11 keys × 2 cultures byte-identical (including after any W2 physical move).
- Request/response DTO semantics: `SetSellerOfferPrice`, `PriceQuote`, `PriceResolutionQuery`, `AuthoredPriceSnapshot`, `OfferAmountRow`, `SetDevelopmentBasePrice` unchanged.
- Business rules/state transitions: `AuthoredPrice` create/activate/change-amount/expire/overlap semantics unchanged; `Money` rounding (`AwayFromZero` at currency scale) unchanged; `MarketCode`/`CurrencyCode` normalization and `TMN/IRT/TOMAN` rejection unchanged.
- Authorization: `IPricingUseCaseGuard.EnsureCanMutateAsync` seam + `OpenPricingUseCaseGuard` behaviour unchanged.
- Persistence/schema: `pricing` schema, table, columns, indexes, constraints, migration id `20260823085546_InitialPricing`, Up/Down, snapshot — untouched.
- Outbox: 4 domain events → 4 integration events with identical `pricing.*.v1` type names and payloads.
- Background workers: none.
- Telemetry/trace: `IModuleCallTracer` `Pricing`→`Offer` `LookupOffer` decoration preserved; no new activity/meter.
- Correlation: unchanged (no Pricing-side correlation surface).
- Tenant/store scoping: `ToobaNpgsql.ResolveForContext(ICurrentCommerceContext, IDatabaseConnectionResolver)` wiring unchanged.
- Public Contracts: no member removed; one port added to Contracts (W1) and the Application duplicate retired.

## Migration order

1. **W1 (Migrate)** — create the single canonical `Contracts/Errors/PricingErrorCodes.cs` with `KnownCodes` + `IsKnown(string?)`; delete `Domain/Errors/PricingErrorCodes.cs` and align all internal consumers; move the Promotion-consumed write port to `Contracts/Ports/IPriceDirectory.cs` and remove the `Tooba.Promotion.Infrastructure` → `Tooba.Pricing.Application` reference (repointing both Promotion call sites); add `Application/Composition/PricingOperation.cs` (dual-mechanism typed-fault seam, value-less overload, unknown codes/exceptions propagate) and use it at the Promotion call sites; register the Contracts port in `PricingModule`; add the durable W1 migrate guard.
2. **W2 (Structure)** — normalize `Contracts`/`Domain`/`Application`/`Infrastructure`/`Endpoints` to path-derived namespaces; move the localization surface to `Contracts/Errors` + `Contracts/Resources` (single self-contained code+text boundary) and retire the Endpoints duplicates while keeping exactly one `IErrorCatalogContributor` + one `IErrorResourceSet` registration; verify `/Modules/Pricing/` solution grouping; update manifest physical allowlists/forbidden lists; add the durable W2 structure guard.
3. **W3 (Certify)** — audit against `ARCH-COMPLETE-002`, add the durable cert guard, promote the manifest entry (`structureCertified: true`, `lockVersion`, `certificationNote`, allowlists) and add `Pricing` to `structureLock.certifiedModules`, record SoT + Master Recovery checkpoint.

## Verification plan

- W1: build `Tooba.Pricing.Endpoints`, `Tooba.Promotion.Infrastructure`, `Tooba.Pricing.Tests`; run `Tooba.Pricing.Tests` and the Promotion architecture guard (must go from red to green); new `PricingModuleAmsc001W1MigrateGuardTests` pinning the canonical code home + `IsKnown` + declared count + the `PricingOperation` shape + the absence of the Promotion→Pricing.Application edge.
- W2: build all Pricing projects + Host; run `Tooba.Pricing.Tests` + the new structure guard (capability-first/namespace-exactness/root-allowlist/solution-grouping/stale-copy); parse `Tooba.slnx`.
- W3: build Host + run `Tooba.Pricing.Tests`, the Host structure gate, `ErrorCatalogUniqueCodeGuardTests`, `PricingArchitectureGuardTests`, the W1/W2/W3 guards, and the focused Host Pricing/Cart/Checkout foundation filters.
- Known pre-existing failure to record honestly: `Tooba.Pricing.Tests.Architecture.PricingArchitectureGuardTests.Seller_price_write_uses_result_not_expected_semantic_exception_control_flow` fails at the W0 baseline because it still asserts the literal `pricing.SetPriceAsync` inside `Offer.Endpoints/Seller/OfferSellerEndpoints.cs` while the current endpoint dispatches `SetOfferPriceCommand` through `ISender` (the port call moved into the Offer handler). This is stale-guard drift owned by the Offer/endpoint refactor, not a Pricing defect; the guard must be corrected to the current canonical shape without weakening its intent.

## Certification blockers

1. `ILLEGAL` inbound edge `Promotion.Infrastructure → Pricing.Application` (two files) — microservice blocker both directions; Promotion's own guard is red.
2. Duplicated stable-code declaration (`Contracts/PricingErrorCodes.cs` + `Domain/Errors/PricingErrorCodes.cs`) and no canonical `Contracts/Errors` home with `KnownCodes`/`IsKnown(string?)`.
3. `PATH_NAMESPACE_ALIGNMENT` violation: 18 production files declare the project-level namespace instead of the path-derived namespace.
4. No certified manifest entry / no `structureLock.certifiedModules` membership (expected; W3 owns the promotion).
5. Stale Pricing architecture guard assertion (blocker for a clean W3 focused-validation record; repaired inside the AMSC run without weakening intent).

Analysis-only wave: zero production change, zero schema change, zero manifest mutation.
