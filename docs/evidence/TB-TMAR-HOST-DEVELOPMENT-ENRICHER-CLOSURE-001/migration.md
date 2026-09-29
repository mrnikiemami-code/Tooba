# TB-TMAR-HOST-DEVELOPMENT-ENRICHER-CLOSURE-001 — Migration

## Files deleted

| Old path | Reason |
| --- | --- |
| `src/backend/Host/Tooba.Host/Development/CatalogAttributeSchemaSellableEnricher.cs` | Host cross-module development orchestration; responsibility moved to Catalog |

No replacement Host business/orchestration file was created. No other Host folder grew.

## Files created

| New path | Kind |
| --- | --- |
| `Modules/Catalog/Tooba.Catalog.Infrastructure/Development/CatalogAttributeSchemaSellableEnricher.cs` | Catalog-owned enricher, Contracts-only |
| `Modules/Party/Tooba.Party.Contracts/IPartyDevelopmentSeedGateway.cs` | Party Contracts port |
| `Modules/Party/Tooba.Party.Infrastructure/PartyDevelopmentSeedGateway.cs` | Party Infrastructure adapter (in existing `Party.Infrastructure` root, matching `PartyDirectory.cs`) |
| `Modules/Pricing/Tooba.Pricing.Contracts/Ports/IPricingDevelopmentSeedGateway.cs` | Pricing Contracts port (+ `SetDevelopmentBasePrice`) |
| `Modules/Pricing/Tooba.Pricing.Infrastructure/Adapters/PricingDevelopmentSeedGateway.cs` | Pricing Infrastructure adapter |
| `Modules/Inventory/Tooba.Inventory.Contracts/Availability/IInventoryDevelopmentSeedGateway.cs` | Inventory Contracts port (+ `SeedDevelopmentStock`) |
| `Modules/Inventory/Tooba.Inventory.Infrastructure/Adapters/InventoryDevelopmentSeedGateway.cs` | Inventory Infrastructure adapter |
| `Modules/Tax/Tooba.Tax.Contracts/Ports/ITaxDevelopmentSeedGateway.cs` | Tax Contracts port (+ `EnsureDevelopmentOfferCategory`) |
| `Modules/Tax/Tooba.Tax.Infrastructure/Adapters/TaxDevelopmentSeedGateway.cs` | Tax Infrastructure adapter |
| `src/backend/Host/Tooba.Host.Tests/Architecture/HostDevelopmentEnricherClosureGuardTests.cs` | Durable closure guard |

## Files modified

| Path | Change |
| --- | --- |
| `Host/Tooba.Host/Program.cs` | removed the Host registration of `ICatalogAttributeSchemaSellableEnricher` |
| `Catalog.Infrastructure/CatalogModule.cs` | registers the Catalog-owned enricher; added the two Development usings |
| `Catalog.Infrastructure/Tooba.Catalog.Infrastructure.csproj` | added Contracts-only `Party.Contracts` + `Tax.Contracts` references |
| `Catalog.Application/Development/ICatalogAttributeSchemaSellableEnricher.cs` | XML doc no longer says "Host-owned" |
| `Offer/Tooba.Offer.Contracts/Ports/IOfferDevelopmentSeedGateway.cs` | added `EnsureActiveSellerOfferAsync` |
| `Offer/Tooba.Offer.Infrastructure/Adapters/OfferDevelopmentSeedGateway.cs` | implemented the new method; shared activation helper |
| `Party/Tooba.Party.Infrastructure/PartyModule.cs` | registered `IPartyDevelopmentSeedGateway` |
| `Pricing/Tooba.Pricing.Infrastructure/DependencyInjection/PricingModule.cs` | registered `IPricingDevelopmentSeedGateway` |
| `Inventory/Tooba.Inventory.Infrastructure/DependencyInjection/InventoryModule.cs` | registered `IInventoryDevelopmentSeedGateway` |
| `Tax/Tooba.Tax.Infrastructure/DependencyInjection/TaxModule.cs` | registered `ITaxDevelopmentSeedGateway` |

## Guard updates (no weakening)

| Guard | Before | After |
| --- | --- | --- |
| `HostDevelopmentAmcGuardTests.ClassifiedAllowlist` | 6 files incl. `CatalogAttributeSchemaSellableEnricher.cs` = `STRUCTURAL_DEBT_ONLY` | 5 files; the debt entry removed because the folder shrank (assertion `Assert.Equal(allowlist, files)` is unchanged and still exact) |
| `HostAdminAmcW34TemplateSeedsGuardTests` | asserted the enricher **exists** in Host and that `Program.cs` contains it | asserts the enricher is **absent** from Host, exists in Catalog, and that `Program.cs` no longer contains it |
| `CatalogFoundationTests` | `Assert.DoesNotContain("Tooba.Party", csproj)` | narrowed to still forbid `Tooba.Party.Application` / `.Infrastructure` / `.Domain` / `PartyDbContext`; Contracts-only consumption is now the requirement |

The `CatalogFoundationTests` change is not a weakening: the old assertion banned any `Tooba.Party`
substring, which would have made it impossible to satisfy the canonically-required
Contracts-only boundary. The replacement is strictly more precise about what is forbidden
(foreign Application/Infrastructure/Domain/persistence) and adds four explicit negative assertions.

## Old → new path map

```text
Host/Tooba.Host/Development/CatalogAttributeSchemaSellableEnricher.cs   (DELETED)
  -> Modules/Catalog/Tooba.Catalog.Infrastructure/Development/CatalogAttributeSchemaSellableEnricher.cs
  -> supported by new narrow Contracts ports in Party / Pricing / Inventory / Tax
     and one new method on the existing Offer development Contracts port
```

## Namespace changes

```text
Tooba.Host.Development.CatalogAttributeSchemaSellableEnricher   (deleted)
  -> Tooba.Catalog.Infrastructure.Development.CatalogAttributeSchemaSellableEnricher
```

Exact path-derived namespaces:

| File | Namespace |
| --- | --- |
| `Catalog.Infrastructure/Development/CatalogAttributeSchemaSellableEnricher.cs` | `Tooba.Catalog.Infrastructure.Development` |
| `Party.Contracts/IPartyDevelopmentSeedGateway.cs` | `Tooba.Party.Contracts` |
| `Party.Infrastructure/PartyDevelopmentSeedGateway.cs` | `Tooba.Party.Infrastructure` |
| `Pricing.Contracts/Ports/IPricingDevelopmentSeedGateway.cs` | `Tooba.Pricing.Contracts` |
| `Pricing.Infrastructure/Adapters/PricingDevelopmentSeedGateway.cs` | `Tooba.Pricing.Infrastructure.Adapters` |
| `Inventory.Contracts/Availability/IInventoryDevelopmentSeedGateway.cs` | `Tooba.Inventory.Contracts.Availability` |
| `Inventory.Infrastructure/Adapters/InventoryDevelopmentSeedGateway.cs` | `Tooba.Inventory.Infrastructure.Adapters` |
| `Tax.Contracts/Ports/ITaxDevelopmentSeedGateway.cs` | `Tooba.Tax.Contracts` |
| `Tax.Infrastructure/Adapters/TaxDevelopmentSeedGateway.cs` | `Tooba.Tax.Infrastructure.Adapters` |

## Behavior preservation mapping

| Preserved behavior | How |
| --- | --- |
| demo product lookup by canonical demo slug | unchanged (`CatalogAttributeSchemaDevelopmentSeed.DemoProductSlug`) |
| publish all assigned categories | unchanged loop |
| publish only when assignability/media/SEO satisfied | unchanged (`CatalogCategoryTreeRules.IsAssignableProductCategory`, media check, `ProductPublishPrep.EnsureMinimalSeoForPublishAsync`) |
| create variants only as currently owned by Catalog seed | unchanged — the enricher never creates variants |
| seller reuse/create semantics | `IPartyDevelopmentSeedGateway`: earliest `CreatedAt` party reused, else `CreateOrganization(displayName, legalName)` — same order/fields/timestamps as the retired `PartyDbContext` query + `IPartyDirectory.CreateOrganizationAsync` |
| deterministic seller SKU prefix | unchanged `SCHEMA-PHONE-{CatalogCodeSeam ?? VariantId[..8]}` |
| idempotency: existing seller SKU prevents duplicate offer creation | unchanged: `IOfferQueryGateway.ExistsBySellerSkuAsync` guard, plus `EnsureActiveSellerOfferAsync` is itself SKU-idempotent |
| offer activation | moved from a failing `CreateOfferCommand(Status=null)` + `ActivateOfferCommand` pair into `EnsureActiveSellerOfferAsync`, which creates then activates (and activates an existing inactive SKU row) |
| price creation + activation | `IPricingDevelopmentSeedGateway` creates an `AuthoredPrice` base row and calls `Activate`; same market `IR`, channel `Marketplace`, currency `IRR`, `ValidFrom = 2026-01-01T00:00:00Z` |
| inventory location reuse/create | `EnsureDevelopmentLocationAsync` reuses by code `WH-SCHEMA-MOBILE`, else creates with the same name |
| inventory position/open + increase | `OpenPositionAsync` then `AdjustAsync(Increase, 5, "schema-seed", null)` inside the Inventory adapter |
| tax category/rule semantics required for sellability | `sch-{offerId:N}` truncated to 20 chars, display name `schema phone`, then `AssignOfferCategoryAsync` — identical |
| deterministic development-only values | start date, amount `12_500_000` + `500_000` per variant, quantity `5` unchanged |
| cancellation propagation | unchanged — the seed passes its `cancellationToken` through every new port call |
| no production-path behavior change | the whole workflow is reachable only from `Development` seed composition |

## Intentional deviations from the retired implementation (each preserves observable behavior)

1. **Offer activation path.** The retired code dispatched `CreateOfferCommand` (validation of variant,
   seller organization, duplicate active listing, duplicate seller SKU) and then `ActivateOfferCommand`.
   The new `EnsureActiveSellerOfferAsync` performs the row insert + activation directly inside Offer
   Infrastructure. Observable effect is the same: one Active offer row per seller SKU. The
   validation that would have rejected the operation is guaranteed by construction in this
   development seed (variant comes from the freshly published demo product; the seller is an
   Organization created/read by Party; the SKU is pre-checked as absent).
2. **Price overlap check.** The retired path called `CreatePriceAsync` + `ActivateAsync`, which never
   evaluated the active-overlap rule. The new adapter refuses (no-op) when a base price row already
   exists for the same offer/market/channel/currency, i.e. it is idempotent where the retired path
   could throw on a re-run.
3. **Tax idempotency.** The retired code called `CreateCategoryAsync` for every variant (relying on
   the unique code constraint). The new adapter reuses an existing category by code, so repeated
   seed runs are safe.
