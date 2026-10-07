# TB-TMAR-PRICING-AMSC-001-W1 — Migrate (tooba-architecture-migrate)

## Scope

`src/backend/Modules/Pricing/Tooba.Pricing.*` plus the single foreign consumer edge in
`src/backend/Modules/Promotion/Tooba.Promotion.Infrastructure/`. W1 consumes the W0 Analyze verdict
`READY_TO_MIGRATE` (commit `08d47b6a`) and repairs the certification blockers recorded in
`docs/architecture/evidence/TB-TMAR-PRICING-AMSC-001-W0/analyze.md`. Pricing has no prior ARCH-COMPLETE-002
certification and is not a member of `structureLock.certifiedModules`; there is therefore no prior accepted
baseline to preserve. Behavior is preserved: every stable code value, every descriptor, every route and the
`pricing` schema are byte-identical after the wave.

## Repair 1 — Single canonical stable-code home (Contracts boundary)

- Created `Tooba.Pricing.Contracts/Errors/PricingErrorCodes.cs` with `namespace Tooba.Pricing.Contracts.Errors`
  and the certified declared-code guard:
  - `private static readonly HashSet<string> KnownCodes` (ordinal) seeded from the declared constants.
  - `public static bool IsKnown(string? code)` — null/whitespace false, exact ordinal membership only.
  - 11 `public const string` members keep their identical wire values (`pricing.amount.invalid`,
    `pricing.overlap`, `pricing.offer.missing`, `pricing.campaign.required`, `pricing.validity.inverted`,
    `pricing.retired.reactivate`, `pricing.retired.immutable`, `pricing.currency.change_forbidden`,
    `pricing.market.invalid`, `pricing.currency.invalid`, `pricing.currency.display_unit`).
- **Deleted the duplicate declaration** `Tooba.Pricing.Domain/Errors/PricingErrorCodes.cs` and removed the
  now-empty `Domain/Errors/` folder. Two parallel stable-code identities for one module was the W0 blocker;
  exactly one declaration remains in the module.
- `Tooba.Pricing.Domain.csproj` now references its **own** `Tooba.Pricing.Contracts` so the Domain consumes
  the single canonical home. This is own-module layering (the accepted `Domain -> own Contracts/Errors`
  precedent), never a foreign `Contracts` reference; the architecture guard was tightened so the only
  permitted `Contracts` reference on the Domain is `Tooba.Pricing.Contracts`.
- All internal consumers repointed to `Tooba.Pricing.Contracts.Errors`: the Domain aggregate/value
  objects/enums/events (7 files), `Infrastructure/Adapters/PriceDirectory.cs`,
  `Infrastructure/Adapters/PricingDevelopmentSeedGateway.cs`, `Infrastructure/DependencyInjection/PricingModule.cs`,
  `Endpoints/Errors/PricingErrorCatalogContributor.cs`, `Contracts/Dtos/CurrencyCode.cs` and the Pricing test
  suites. `Tooba.Pricing.Infrastructure.csproj` gained the explicit `Tooba.Pricing.Contracts` reference.

## Repair 2 — Canonical typed-fault seam

- Created `Tooba.Pricing.Application/Composition/PricingOperation.cs` mirroring the certified
  Media/Inventory/Party/Payment `*Operation` shape exactly:
  - `ExecuteAsync<T>(Func<Task<T>>)` and value-less `ExecuteAsync(Func<Task>)`.
  - `ArgumentNullException.ThrowIfNull`.
  - `catch (ContractOperationException ex) when (PricingErrorCodes.IsKnown(ex.Code))` →
    `Result.Failure<T>(new SemanticError(ex.Code))`.
  - `catch (SemanticException ex)` → `Result.Failure<T>(ex.Error)`.
  - A contract fault whose code is not a declared Pricing code, and every unknown exception, propagates
    untouched to the canonical global exception boundary.
- Classification is by typed code only — never by message/prose heuristics. The seam returns
  `Result`/`Result<T>` (never `IResult`), so the boundary contract is unchanged.
- Pricing owns **zero** endpoint-reachable requests (verified by W0), so the seam is deliberately a dormant
  boundary, exactly as the Analyze wave recorded; no CQRS ceremony was invented for the module.

## Repair 3 — Contracts-only decoupling of the inbound Promotion edge

- **Moved the write port** from `Tooba.Pricing.Application/Ports/IPriceDirectory.cs` to
  `Tooba.Pricing.Contracts/Ports/IPriceDirectory.cs` (same five members, same signatures, same
  `PriceDirectory` implementation). The `IPricingUseCaseGuard` seam stays Application-owned at
  `Tooba.Pricing.Application/Ports/IPricingUseCaseGuard.cs` — it is a module-internal authorization seam and
  is not part of the module's public boundary.
- **Removed** `<ProjectReference Include="..\..\Pricing\Tooba.Pricing.Application\..." />` from
  `Tooba.Promotion.Infrastructure.csproj`. Promotion now consumes `Tooba.Pricing.Contracts` only.
- Repointed both Promotion call sites: `Merchandising/MerchandisingCampaignAdminComposer.cs` and
  `Development/MerchandisingCampaignDevelopmentSeed.cs` dropped `using Tooba.Pricing.Application;` and resolve
  `IPriceDirectory` / `IPriceQueryGateway` from `Tooba.Pricing.Contracts`. The reads were already Contracts
  (`IPriceQueryGateway`); only the write port moved.
- Registered the Contracts port in `PricingModule.AddServices`; the same `PriceDirectory` singleton satisfies
  `IPriceDirectory`, `IPriceLookupGateway`, `ISellerOfferPricingGateway` and `IPriceQueryGateway`, so no
  resolution or lifetime behavior changed.
- Result: the `Tooba.Promotion.Infrastructure -> Tooba.Pricing.Application` edge is gone repository-wide, and
  Promotion's own `PromotionArchitectureGuardTests.Infrastructure_uses_contracts_not_foreign_application`
  flips from red to green.

## Repair 4 — Stale Pricing guard corrected to the canonical shape

- `PricingArchitectureGuardTests.Seller_price_write_uses_result_not_expected_semantic_exception_control_flow`
  still asserted the literal `pricing.SetPriceAsync` inside `Offer.Endpoints/Seller/OfferSellerEndpoints.cs`,
  while the current endpoint dispatches `SetOfferPriceCommand` through `ISender` and the port call lives in
  the Offer handler. The assertion was corrected to the current canonical shape
  (`new SetOfferPriceCommand(` + `api.From(result)` in the endpoint; `pricing.SetPriceAsync` +
  `Result<SellerOfferDetailPage>` in `SetOfferPriceCommand.cs`). Intent is preserved and strengthened (the
  endpoint is now explicitly asserted **not** to call the directory directly); nothing was weakened.
- `Pricing_golden_boundaries_remain_clean` now permits the Domain's **own** `Tooba.Pricing.Contracts`
  reference while still forbidding any foreign module `Contracts` on the Domain.

## Repair 5 — No further repair required

Logging, sensitive logging, telemetry, correlation, persistence, outbox and schema were verified canonical by
W0 and are untouched by W1. Zero log call sites, zero `Console.WriteLine`/`Debug.WriteLine`, zero second
`ActivitySource`/`Meter`, zero raw `StartActivity(`, zero `traceparent` parsing, zero correlation invention.
`PriceDirectory.FindOfferAsync` keeps its canonical `IModuleCallTracer.Begin("Pricing","Offer","LookupOffer")`
decoration. No migration file was touched.

## Verification

- `dotnet build` of `Tooba.Pricing.Tests`, `Tooba.Promotion.Infrastructure` and `Tooba.Host.Tests`: succeeded
  (0 errors).
- `Tooba.Pricing.Tests`: **14 passed / 0 failed** (the W0 baseline was 13 passed / 1 stale-guard failure; the
  stale guard is now corrected and green).
- New durable guard `PricingModuleAmsc001W1MigrateGuardTests` (`src/backend/Host/Tooba.Host.Tests/Architecture/`):
  **8 passed / 0 failed**, pinning the single canonical Contracts home + `IsKnown` declared count, the retired
  Domain duplicate, the `PricingOperation` shape, the Contracts-resident write port, the Promotion
  Contracts-only edge, the Contracts-only boundary, the no-re-inlined-literal rule and the no-message-text
  classification rule.
- `Tooba.Host.Tests` focused filter (`Pricing|Promotion|Merchandising|Cart|Checkout`):
  **123 passed / 14 skipped / 5 failed**. All five failures are pre-existing, out-of-scope and unchanged by
  this wave:
  - `HostAdminAmcCheckoutAbuseGuardTests.Host_Admin_count_15_StoreAppearance_evacuated` — asserts
    `Host/Admin` count 15 while the repository has 17 (unrelated Host evacuation drift).
  - `TaxFoundationTests.Pricing_and_catalog_do_not_own_tax_amounts_and_outcomes_stay_distinct` — reads a
    non-existent `Modules/Tax/Tooba.Tax.Domain/TaxDomain.cs`.
  - `PromotionPanelTests.Host_registers_seller_and_admin_promotion_routes_with_panel_access` — reads a
    non-existent `Host/Tooba.Host/Promotion/PromotionEndpoints.cs`.
  - `PromotionFoundationTests.Promotion_does_not_own_authored_price_and_keeps_module_boundaries` — reads a
    non-existent `Tooba.Promotion.Application/PromotionContracts.cs`.
  - `PricingFoundationTests.Pricing_projects_do_not_reference_masstransit_authzed_or_foreign_infrastructure` —
    asserts `typeof(SalesChannel) == AuthoredPrice.Channel` while the Domain owns `PriceChannel` (assertion is
    stale at the W0 baseline; identical before and after this wave).
- `Tooba.Promotion.Tests`: **7 passed / 1 failed**. The failure is the pre-existing
  `Promotion_golden_boundaries_and_physical_layout_remain_clean` root-allowlist gap
  (`Tooba.Promotion.Infrastructure/Development/` exists at `HEAD` but is absent from the guard's
  `AllowedInfrastructureFolders`); the `Infrastructure_uses_contracts_not_foreign_application` assertion that
  W0 recorded as red is now **green**.
- No migration, snapshot, route, descriptor, resource key or code value changed.

## State

- Stable-Error-Code-State: `CATALOGUED_11_DECLARED_11_REGISTERED_DUPLICATED_DECLARATION_NO_ISKNOWN_GUARD`
  → `CONTRACTS_OWNED_11_DECLARED_11_REGISTERED_SINGLE_HOME_ISKNOWN_GUARDED`.
- Contracts-Boundary-State: `VIOLATION` → `COMPLIANT` (single code home; the path↔namespace repair remains a
  W2 Structure obligation and is not claimed here).
- Cross-Module-Coupling-State: `ILLEGAL_INBOUND_ONE_EDGE` → `LEGAL_CONTRACTS_ONLY` (inbound edge removed;
  Promotion's own guard green).
- CQRS-State: `NOT_APPLICABLE_TODAY` (unchanged, correct — Pricing owns zero HTTP routes and zero
  endpoint-reachable requests; no ceremony invented).
- Validator-Coverage-State: `EXHAUSTIVE_0_OF_0_NO_VALIDATOR_REQUIRED` (unchanged, correct).
- Localization-State: `CANONICAL` (unchanged — 11 descriptors, bilingual pair, single `IErrorResourceSet`).
- `microserviceExtractable`:
  `TARGET_TRUE_BLOCKED_BY_INBOUND_PROMOTION_COUPLING_AND_DUPLICATED_STABLE_CODE_IDENTITY`
  → `TRUE_INBOUND_COUPLING_AND_STABLE_CODE_IDENTITY_REPAIRED_PATH_NAMESPACE_PENDING_W2`.
- Verdict: `READY_TO_STRUCTURE`. `Structure-Handoff-State = REQUIRED`.

Production code changed; schema unchanged; guards strengthened; zero guards weakened; zero unrelated files
touched.
