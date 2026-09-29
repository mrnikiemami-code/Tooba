# TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-ANALYZE-001 — Destination Integrity

Closed-folder / destination-integrity guard (analyze skill §3d) applied to every destination proposed
by `closure-plan.md`. Classification vocabulary:
`OPEN_FOR_FUTURE_BOUNDED_TASK` | `LOCKED_BY_ACCEPTED_DISPOSITION` | `NEW_LOCATION_REQUIRES_ARCHITECT_APPROVAL`.

## Proposed destinations

| # | Destination | Current files / accepted state | Classification | Verdict |
| --- | --- | --- | --- | --- |
| 1 | `Modules/Catalog/Tooba.Catalog.Infrastructure/Development/` (new `WorkspaceDemoProductSeed.cs`, `WorkspaceDemoMarketplaceSeed.cs`) | Certified module; folder already holds `CatalogDemo/*`, `LandingPageDevelopmentSeed.cs`, `StoreMenuDevelopmentSeed.cs`, `CatalogAttributeSchemaDevelopmentSeed.cs`, `CatalogAttributeSchemaSellableEnricher.cs`, `FashionTemplateCatalogSeed.cs`, `IndustryBatchA/B/C…`. `CatalogAttributeSchemaSellableEnricher.cs` was **accepted** into this folder by `TB-TMAR-HOST-DEVELOPMENT-ENRICHER-CLOSURE-001`. | `OPEN_FOR_FUTURE_BOUNDED_TASK` | **ALLOWED** — same module, same purpose (Development seed), the folder is the module's own Development surface, and no certified file set is being reduced or redefined. Adding new seed files here does not resurrect a Host folder and does not touch a locked Host allowlist. |
| 2 | `Modules/Catalog/Tooba.Catalog.Application/Development/IWorkspaceDemoProductSeed.cs` | Folder exists (`CatalogDemo/ICatalogDemoResetAndSeedGateway.cs`, `CatalogDemo/CatalogDemoSeedOptions.cs`, `ICatalogAttributeSchemaSellableEnricher.cs`). | `OPEN_FOR_FUTURE_BOUNDED_TASK` | **ALLOWED** — follows the accepted module-owned gateway pattern already certified in this module. |
| 3 | `Modules/Party/Tooba.Party.Contracts/IPartyDevelopmentSeedGateway.cs` (+1 narrow method) | Port created and accepted by `TB-TMAR-HOST-DEVELOPMENT-ENRICHER-CLOSURE-001`; owner-of-file unchanged. | `LOCKED_BY_ACCEPTED_DISPOSITION` — the **file exists** and its ownership/contract-surface is accepted. | **ALLOWED, additive only.** The accepted disposition certifies the *port's existence and Contracts membership*, not a frozen method signature. Adding one narrow Development-support method does not reopen the port's ownership, does not change the Host evacuation outcome, and does not redefine the certified file set. No old method is removed or renamed. |
| 4 | `Modules/Tax/Tooba.Tax.Contracts/Ports/ITaxDevelopmentSeedGateway.cs` (+1 narrow rule-seed method) | Same: accepted Contracts port. | `LOCKED_BY_ACCEPTED_DISPOSITION` | **ALLOWED, additive only** (same justification). |
| 5 | `Modules/Inventory/Tooba.Inventory.Contracts/Availability/IInventoryDevelopmentSeedGateway.cs` (+1 narrow reservation-hold method) | Same: accepted Contracts port. | `LOCKED_BY_ACCEPTED_DISPOSITION` | **ALLOWED, additive only** (same justification). |
| 6 | `Modules/Offer/Tooba.Offer.Contracts/Ports/IOfferDevelopmentSeedGateway.cs` | Untouched by both waves — `EnsureActiveSellerOfferAsync` already covers the need. | `LOCKED_BY_ACCEPTED_DISPOSITION` | **NO CHANGE** — read-only use. |
| 7 | `Modules/Pricing/Tooba.Pricing.Contracts/Ports/IPricingDevelopmentSeedGateway.cs` | Untouched — `EnsureDevelopmentBasePriceAsync` already covers the need. | `LOCKED_BY_ACCEPTED_DISPOSITION` | **NO CHANGE** — read-only use. |
| 8 | `BuildingBlocks/Tooba.Persistence/IModuleSchemaMigrator.cs` (new) | Shared neutral persistence foundation project. | `NEW_LOCATION_REQUIRES_ARCHITECT_APPROVAL` (new file inside an existing, non-Host, non-certified-module foundation project) | **REQUIRES EXPLICIT ARCHITECT APPROVAL** before Wave 2. It is not a Host folder and not a staging sink; it is the platform-neutral project that already exists for exactly this kind of cross-cutting persistence seam. If the Architect prefers `Tooba.BuildingBlocks`, the same port moves there with no design change. Flagged, not assumed. |
| 9 | 23 × `Modules/<M>/Tooba.<M>.Infrastructure/Adapters/<M>ModuleMigrationAdapter.cs` | Of the 28 contexts, five modules already own `*ModuleMigration` adapters (`Offer`, `Pricing`, `Inventory`, `Tax`, `Promotion` — the five `I*SchemaMigrator` precedents). The other 23 have no adapter today; their `Infrastructure/Adapters/` folders exist (`Infrastructure` folder sets are established module-wide). | `OPEN_FOR_FUTURE_BOUNDED_TASK` | **ALLOWED** — each adapter is created inside its own module's own Infrastructure, one method, no semantics. This is the exact precedent the task itself instructs me to follow (`IOfferSchemaMigrator` … `IPromotionSchemaMigrator`). |
| 10 | `Host/Tooba.Host/Development/DevelopmentSchemaMigrator.cs` (new Host file) | `Host/Development` accepted retained set (5 files, `HostDevelopmentAmcGuardTests.ClassifiedAllowlist`). | `LOCKED_BY_ACCEPTED_DISPOSITION` — the folder's **retained set** is accepted, and any change requires the exact old/new set + justification + SoT + guard update. | **ALLOWED ONLY WITH EXPLICIT SET-CHANGE RECORDING.** The plan records the exact change: `-ProductWorkspaceDevelopmentBootstrap.cs`, `+DevelopmentSchemaMigrator.cs` (count stays 5). The new file's classification is `ALLOWED_DEVELOPMENT_COMPOSITION`, the **same classification already accepted for `MarketplaceDevelopmentBootstrap.cs`**, and the durable guard `HostDevelopmentAmcGuardTests.ClassifiedAllowlist` is updated in the same commit. This is the sanctioned "call out exact old/new set, justification, SoT update and durable guard update" path — not a silent allowlist growth. |
| 11 | `Host/Tooba.MigrationRunner/ModuleMigrationRegistry.cs` | Existing Host tool project; deliberately listed in module guards' `HostDbContextAllowlist` (`Wallet`, `Cart`). | `LOCKED_BY_ACCEPTED_DISPOSITION` | **ALLOWED, minimal edit** — it either keeps constructing contexts (existing accepted exception, unchanged) or is switched to resolve `IModuleSchemaMigrator`; either way it stays the single ordered source of truth. Do **not** delete it; deleting it would remove an accepted capability. |
| 12 | `Host/Tooba.Host/Program.cs` | Existing composition root. | `LOCKED_BY_ACCEPTED_DISPOSITION` | **ALLOWED, minimal edit** — call-site swap only, same ordering, same Development-only guard. `HostAdminAmcW32PwShellFinalGuardTests` asserts the mapping stays module-only. |
| 13 | Host guard files (3) + module-guard allowlists (6) | Accepted guards. | `LOCKED_BY_ACCEPTED_DISPOSITION` | **ALLOWED, required** — a guard that asserts the *debt* still exists must be updated when the debt is closed; otherwise the closure is unprovable. Only the entries naming `ProductWorkspaceDevelopmentBootstrap.cs` change. |

## Forbidden destinations (explicitly NOT proposed)

| Destination | Why forbidden |
| --- | --- |
| Any other `Host/*` folder (`Host/Settings`, `Host/Wishlist`, `Host/Seller`, `Host/Admin`, `Host/Storefront`, `Host/Composition`, …) | SINK_FOLDER_REGRESSION — moving the debt sideways into another Host folder would make a closed/non-active folder worse to make the active folder look clean. Every Host-living seed discovered here (`WishlistDevelopmentSeed`, `SettingsFoundationDevelopmentSeed`) is registered as `OPEN_FOR_FUTURE_BOUNDED_TASK` in its **own** folder's future recovery unit. |
| A new Host folder (e.g. `Host/Development/Migration`) | NEW Location in Host without Architect authorization — not invented as a staging sink. |
| `Tooba.ModuleContracts` | Not the right owner: the schema-migrator seam is EF/persistence-level, and `ModuleContracts` is the module-contract aggregation surface; dumping a persistence port there would be a shared god-contract. |
| Any new shared "errors" or "development" project | Would be a parallel mechanism; forbidden by the analyze skill. |
| `Host/Settings` / `Host/Wishlist` seed relocation into a module | Out of bounded scope; would be a second Host-folder recovery unit inside this task. |

## Closed-folder regression checklist

| Check | Result |
| --- | --- |
| Does any wave grow a protected retained-file allowlist? | No — Wave 1 leaves `Host/Development` at 5 files; Wave 2 swaps one file 1:1 (5 → 5) with the exact set recorded and the guard updated in the same commit. |
| Does any wave resurrect a previously removed file? | No. `CatalogAttributeSchemaSellableEnricher.cs` stays deleted from Host; no `*SeedHost.cs` wrapper returns. |
| Does any wave create a new Host folder? | No. |
| Does any wave move code into another closed Host folder? | No. Deferred Host seeds are left untouched and named as future bounded units. |
| Does any wave widen the `HostDbContextAllowlist` in module guards? | No — Wave 2 **removes** `ProductWorkspaceDevelopmentBootstrap.cs` from six of them (Wallet, Cart, Support, Notification, Payment, Fulfillment). |
| Is any certified/reference module broadly modified? | No. Catalog receives new files in its own Development surface; Party/Tax/Inventory Contracts receive one additive narrow method each; Offer/Pricing are read-only. |
| Is `Tooba.Offer` (reference module) re-audited or re-certified? | No. |
| Is `latestAcceptedTask` replaced by this analysis task? | No — it stays `TB-TMAR-HOST-DEVELOPMENT-ENRICHER-CLOSURE-001`. |

## Required Architect approvals before implementation

1. `BuildingBlocks/Tooba.Persistence/IModuleSchemaMigrator.cs` — confirm `Tooba.Persistence` (vs
   `Tooba.BuildingBlocks`) as the neutral owner for the Wave 2 seam.
2. `Host/Development` retained-set change (Wave 2): accept the exact 5 → 5 swap
   (`-ProductWorkspaceDevelopmentBootstrap.cs`, `+DevelopmentSchemaMigrator.cs`) and the matching
   `HostDevelopmentAmcGuardTests.ClassifiedAllowlist` update.
3. Wave 1 Tax **rule** and Inventory **reservation-hold** narrow gateway methods — confirm they are
   in scope as minimal additive Contracts changes on the owning modules.
4. Order of landing (Wave 1 first recommended; either order is safe, no hidden prerequisite).
