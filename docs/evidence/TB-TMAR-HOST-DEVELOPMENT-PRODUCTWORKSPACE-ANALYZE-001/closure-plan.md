# TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-ANALYZE-001 — Closure Plan

`implementationWaveCount = 2`

`finalTargetState = ABSENT` (Option A) —
`Development/ProductWorkspaceDevelopmentBootstrap.cs` is deleted by Wave 2; what remains in
`Host/Development` is a genuine Host-owned development-composition file, not this mixed 428-LOC file.

## Why two waves

The file contains two independent debts that need different owners and different Contracts surfaces:

- the **Catalog demo business seed** (product/category/brand/attribute/variant/media/seo/publish,
  operator copy refresh, Admin R3 preview) plus the foreign-module demo business setup
  (Party/Offer/Pricing/Tax/Inventory) — all of which is Catalog seed responsibility; and
- the **28-context schema-migration orchestration**, which is a Host platform composition concern
  whose only defect is that it obtains the contexts from Host instead of from a neutral seam.

Each is independently reviewable, has no hidden prerequisite on the other, and has a different
Contracts footprint (Wave 1 reuses four already-existing module Contracts gateways; Wave 2 needs one
neutral port). The two can be landed in either order; this plan orders them seed-first because that
removes the file's business authority, which is the larger architectural risk.

Ordering note: no cross-wave hidden prerequisite exists. If Wave 1 lands alone, the residual file is
already thin migration orchestration and fully legal once Wave 2 lands; if Wave 2 lands alone, the
residual file is a legal-but-business-bearing Catalog seed that Wave 1 then rehomes.

---

## Wave 1 — Catalog demo product seed rehome

**Proposed Task-ID:** `TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-CATALOG-SEED-REHOME-001`

### Exact files/modules allowed

Create:
- `src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Development/WorkspaceDemoProductSeed.cs`
  — the complete Catalog-owned demo product seed, moved verbatim in behavior from
  `ApplyCoreAsync`'s `seedCatalog: true` branch and `EnsureAdminR3PreviewSeedAsync` +
  `RefreshOperatorFacingCopyAsync`. Idempotent by `SeedSlug`/`draftSlug`/`archivedSlug`
  (exactly as today). Owns its own `CatalogDbContext` usage (intra-module → legal).
- `src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Development/WorkspaceDemoMarketplaceSeed.cs`
  — the two-seller marketplace demo (Seller A/B orgs, two active offers `ARM-LN-01`/`DGS-LN-01`,
  two base prices, tax category + assignment, three inventory locations, three positions,
  stock increase, reservation hold `workspace-live-hold` and `3` reserved units) expressed **only**
  through the existing Contracts gateways.
- `src/backend/Modules/Catalog/Tooba.Catalog.Application/Development/IWorkspaceDemoProductSeed.cs`
  — Catalog-owned application entry point (mirrors the accepted
  `Catalog.Application/Development/CatalogDemo/ICatalogDemoResetAndSeedGateway.cs` pattern) exposing
  exactly the two current modes: `ApplyAsync(services, seedCatalog: true)` and
  `MigrateSchemaOnly`-equivalent seed-free mode (the seed-free mode is only the migration path →
  owned by Wave 2).

Modify (narrow, Contracts-only):
- `src/backend/Modules/Party/Tooba.Party.Contracts/IPartyDevelopmentSeedGateway.cs` — add one narrow
  method for the operator copy refresh (`EnsureDevelopmentOrganizationDisplayNameAsync(displayName, …)`)
  to replace the direct `PartyDbContext.Parties` rewrite.
- `src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/CatalogModule.cs` — register the new seed
  services and map `IWorkspaceDemoProductSeed`.
- `src/backend/Host/Tooba.Host/Program.cs` — the `RunLegacyBootstraps=true` branch resolves
  `IWorkspaceDemoProductSeed` (Catalog Contracts/Application) instead of
  `ProductWorkspaceDevelopmentBootstrap.ApplyAsync`; keep the same order: workspace seed, then
  `StorefrontDemoCatalogBootstrap`, then attribute-schema seed, then the remaining Host dev seeds.

Modify (delete business parts only):
- `src/backend/Host/Tooba.Host/Development/ProductWorkspaceDevelopmentBootstrap.cs` — the Catalog
  business seed, the marketplace demo, `EnsureAdminR3PreviewSeedAsync`,
  `RefreshOperatorFacingCopyAsync`, `SeedSlug`, all foreign Application/Domain/`ISender` usage and
  every now-unused `using` are removed. What remains is migration-only (Wave 2 target).

### Exact responsibilities closed

17, 18, 19, 20, 21, 22, 23, 24, 25 from `responsibility-map.md`.

### Contracts changes required

Reuse only (already shipped by `TB-TMAR-HOST-DEVELOPMENT-ENRICHER-CLOSURE-001`):
- `IOfferDevelopmentSeedGateway.EnsureActiveSellerOfferAsync`
- `IPartyDevelopmentSeedGateway` (extended with one narrow display-name method)
- `IPricingDevelopmentSeedGateway.EnsureDevelopmentBasePriceAsync`
- `IInventoryDevelopmentSeedGateway.EnsureDevelopmentLocationAsync` / `IncreaseDevelopmentStockAsync`
- `ITaxDevelopmentSeedGateway.EnsureDevelopmentOfferCategoryAsync`

No new module, no new project, no broad Contracts redesign. The two remaining gaps are documented as
`NEEDS_FOLLOWUP_NARROW_METHODS` (not blockers):
- a Tax **rule** seed method (`CreateRuleAsync`/`ActivateRuleAsync` equivalent) — today's
  `ITaxDevelopmentSeedGateway` only covers category + assignment;
- an Inventory **reservation-hold** seed method (`ReserveAsync` equivalent with reason/reference) —
  today's gateway only covers location + increase.
Both must be added as narrow methods on the **owning** module's Contracts port: never as a
Host-side Tax/Inventory implementation, and never by exposing Tax/Inventory persistence.

### Files expected deleted/moved/created

| Action | Path |
| --- | --- |
| created | `Modules/Catalog/Tooba.Catalog.Infrastructure/Development/WorkspaceDemoProductSeed.cs` |
| created | `Modules/Catalog/Tooba.Catalog.Infrastructure/Development/WorkspaceDemoMarketplaceSeed.cs` |
| created | `Modules/Catalog/Tooba.Catalog.Application/Development/IWorkspaceDemoProductSeed.cs` |
| modified | `Modules/Party/Tooba.Party.Contracts/IPartyDevelopmentSeedGateway.cs` (+1 narrow method) |
| modified | `Modules/Tax/Tooba.Tax.Contracts/Ports/ITaxDevelopmentSeedGateway.cs` (+1 narrow method) |
| modified | `Modules/Inventory/Tooba.Inventory.Contracts/Availability/IInventoryDevelopmentSeedGateway.cs` (+1 narrow method) |
| modified | `Modules/Catalog/Tooba.Catalog.Infrastructure/CatalogModule.cs` |
| modified | `Host/Tooba.Host/Program.cs` |
| shrunk | `Host/Tooba.Host/Development/ProductWorkspaceDevelopmentBootstrap.cs` (428 → ~90 LOC migration-only) |

### Host file-count effect

`Host/Development` production file count: **5 → 5** (the file is not deleted in this wave; it is
reduced to migration-only). No new Host folder. No growth of any protected retained-file allowlist.

### Focused validation required

- `dotnet build` on `Tooba.Host`, `Tooba.Catalog.Infrastructure`, `Tooba.Party.Infrastructure`,
  `Tooba.Tax.Infrastructure`, `Tooba.Inventory.Infrastructure`.
- Focused `Tooba.Host.Tests` filter: `HostDevelopmentAmcGuardTests`,
  `HostDevelopmentEnricherClosureGuardTests`, `HostCustomerProfileEvacuationGuardTests`,
  `HostCartResidualGuardTests`, `ArchitectureBoundaryTests`, `CatalogFoundationTests`,
  `CatalogDemoResetSeedTests`, `TmarDurableGuardTests`.
- Static proof in the new seed: no `Tooba.<Foreign>.Application|Infrastructure|Domain` reference
  except Catalog; no `ISender`; no foreign `DbContext` type name.
- Seed-value parity proof (exact slugs, SKUs, amounts, quantities, reservation `3`,
  `workspace-live-hold`, media GUIDs `aaaaaaaa…eeeeeeee`, `11111111…`, `22222222…`).

### STOP point

After Wave 1's focused tests PASS and the result is posted. **Do not start Wave 2.** Do not touch the
`Host/Settings` or `Host/Wishlist` seeds.

---

## Wave 2 — Development schema-migration seam

**Proposed Task-ID:** `TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-MIGRATION-SEAM-001`

### Exact files/modules allowed

Create (neutral persistence foundation — the natural owner already exists as a shared project):
- `src/backend/BuildingBlocks/Tooba.Persistence/IModuleSchemaMigrator.cs` (or, if the Architect
  prefers, the identical port inside `Tooba.BuildingBlocks`) — one neutral, module-agnostic port
  `Task MigrateAsync(CancellationToken)` plus a keyed descriptor identity, so a Host composition seam
  can order migrations without naming any module's `DbContext`.

Create (thin module adapters, one per module that has no migrator yet):
- `Modules/<M>/Tooba.<M>.Infrastructure/Adapters/<M>ModuleMigrationAdapter.cs` for
  Catalog, Party, Identity, Cart, Order, Payment, Fulfillment, PlatformProbe, Reviews, ProductQnA,
  BulkInquiry, Wishlist, AddressBook, CustomerProfile, UserPreference, OperatorProfile, Content,
  Media, PageComposition, Story, Notification, AccessControl, Support.
  Each adapter wraps its own context and implements `IModuleSchemaMigrator` (the same shape as the
  existing `OfferModuleMigration`/`PricingModuleMigration`/`InventoryModuleMigration`/
  `TaxModuleMigration`/`PromotionModuleMigration` adapters, which already exist for five modules).
  Existing five `I*SchemaMigrator` ports are retained unchanged and are simply also exposed as
  `IModuleSchemaMigrator` for a uniform ordering list.

Create/modify (Host composition):
- `src/backend/Host/Tooba.Host/Development/DevelopmentSchemaMigrator.cs` — Host-owned ordered
  invocation of the registered `IModuleSchemaMigrator` descriptors. This is
  `ALLOWED_DEVELOPMENT_COMPOSITION` (the exact classification `HostDevelopmentAmcGuardTests` already
  grants to `MarketplaceDevelopmentBootstrap.cs`). No business policy, no context construction.
- `src/backend/Host/Tooba.MigrationRunner/ModuleMigrationRegistry.cs` — reuse as the single ordered
  source of truth for module order; if kept, it must resolve adapters through
  `IModuleSchemaMigrator` rather than constructing contexts by reflection, or the Development order
  must be proven equal to `ModuleMigrationRegistry.All` order by a guard.
- delete `src/backend/Host/Tooba.Host/Development/ProductWorkspaceDevelopmentBootstrap.cs`.
- `src/backend/Host/Tooba.Host/Program.cs` — both call sites (`ApplyAsync`,
  `MigrateSchemaOnlyAsync`) become `DevelopmentSchemaMigrator.MigrateAsync(app.Services)`.
- guard updates (allowed/required): `HostDevelopmentAmcGuardTests.ClassifiedAllowlist`
  (swap `ProductWorkspaceDevelopmentBootstrap.cs` → `DevelopmentSchemaMigrator.cs`),
  `HostDevelopmentEnricherClosureGuardTests.ProductWorkspace_bootstrap_remains_open_debt_and_untouched`
  (must be replaced by an ABSENT assertion — this is part of the closure),
  `HostAdminAmcW32PwShellFinalGuardTests.Development_bootstrap_exists_and_Program_maps_module_only`
  (assert the new file + unchanged Program mapping),
  `HostCustomerProfileEvacuationGuardTests` (the `CustomerProfileDevelopmentSeed.ApplyAsync` count
  moves with the seed to Catalog, so the guard must target the new owner),
  and the `HostDbContextAllowlist` entries in `Wallet/Cart/Support/Notification/Payment/Fulfillment`
  architecture guards must drop `ProductWorkspaceDevelopmentBootstrap.cs` and allow only
  `ModuleMigrationRegistry.cs` / `Program.cs`.

### Exact responsibilities closed

3, 9–16, 37 from `responsibility-map.md` — every `DbContext`-typed migration call and the
`MigrateAsync(DbContext)` helper.

### Contracts changes required

One new **neutral platform** port. Justification per the analyze skill's ownership rule: this concern
is genuinely cross-cutting/platform-level, has **no natural module owner** (every module would have to
own a copy of the Host's ordering), and the repository already has the appropriate neutral project
(`Tooba.Persistence`). It is **not** a "shared god-contract": it exposes one method and no module
semantics. It is **not** a new shared-errors layer. No module error codes are involved.

### Files expected deleted/moved/created

| Action | Path |
| --- | --- |
| created | `BuildingBlocks/Tooba.Persistence/IModuleSchemaMigrator.cs` |
| created | 23 × `Modules/<M>/Tooba.<M>.Infrastructure/Adapters/<M>ModuleMigrationAdapter.cs` |
| created | `Host/Tooba.Host/Development/DevelopmentSchemaMigrator.cs` |
| modified | `Host/Tooba.MigrationRunner/ModuleMigrationRegistry.cs` (consume the port, keep order) |
| modified | `Host/Tooba.Host/Program.cs` |
| modified | 3 Host guard files + 6 module-guard allowlists (listed above) |
| **deleted** | `Host/Tooba.Host/Development/ProductWorkspaceDevelopmentBootstrap.cs` |

### Host file-count effect

`Host/Development` production file count: **5 → 5** (delete one, add one composition file).
Net Host production business authority: **ZERO**. `ProductWorkspaceDevelopmentBootstrap.cs` = **ABSENT**.

### Focused validation required

- `dotnet build` on `Tooba.Host`, `Tooba.MigrationRunner`, `Tooba.Persistence` and every touched
  module Infrastructure project.
- `Tooba.MigrationRunner.Tests` (`MigrationRunnerIntegrationTests`, `OfferMigrationIntegrityTests`).
- Development start-up proof that the migration order and applied schema set are unchanged
  (order equality guard against `ModuleMigrationRegistry.All`).
- Focused guards: `HostDevelopmentAmcGuardTests`, `HostAdminAmcW32PwShellFinalGuardTests`,
  `HostCartResidualGuardTests`, `HostCustomerProfileEvacuationGuardTests`, `ArchitectureBoundaryTests`,
  `TmarSourceSizeAndInfraAppTests`, `TmarDurableGuardTests`,
  `TmarCompleteReferenceStructureGateTests`, plus the module architecture guards whose allowlists
  were edited.
- `npm run test:critical-storefront` is **NOT** required (no storefront/shared component touched) —
  recorded explicitly so the change is not over-tested.

### STOP point

After Wave 2's focused tests PASS and the result is posted. No Wave 3. No traversal into
`Host/Settings`, `Host/Wishlist`, `Host/Seller`, `Host/Admin`, or any other Host folder.

---

## Wave 3

**None.** `Wave-3-Task-ID: NONE`

---

## Deferred (explicitly OUT of these two waves — must not be opened here)

| Debt | Location | Why deferred |
| --- | --- | --- |
| `WishlistDevelopmentSeed` reads `CatalogDbContext` from Host | `Host/Wishlist/WishlistDevelopmentSeed.cs` | lives in a **different** Host folder; moving it is a new bounded recovery unit (closed-folder rule) |
| `SettingsFoundationDevelopmentSeed` reads `PartyDbContext` from Host | `Host/Settings/SettingsFoundationDevelopmentSeed.cs` | same reason |
| 12 additional `*DevelopmentSeedHost.cs` files across Host subfolders | Host-wide | later folder traversal; out of this bounded target |

Both deferred items are registered only as `OPEN_FOR_FUTURE_BOUNDED_TASK` placeholders, never as
destinations for this file, and this task must not modify them.

## Success condition (both waves landed)

1. `src/backend/Host/Tooba.Host/Development/ProductWorkspaceDevelopmentBootstrap.cs` does not exist.
2. No Host production file references a foreign module `DbContext`, `DbSet`, `.Application` port,
   or `.Domain` type for the product-workspace/development-migration path (except the already
   classified `MigrationRunner` registry and `Program.cs` composition seams, and the deferred
   `Host/Settings` + `Host/Wishlist` seeds).
3. `Host/Development` contains only `ALLOWED_DEVELOPMENT_COMPOSITION` /
   `ALLOWED_DEVELOPMENT_RUNTIME_SEAM` files.
4. Seed values, ordering, idempotency, both `RunLegacyBootstraps` modes, schema order and the
   `CommerceContext.TraceId` labels `workspace-dev-seed` / `workspace-dev-schema` are unchanged.
5. `latestAcceptedTask` remains `TB-TMAR-HOST-DEVELOPMENT-ENRICHER-CLOSURE-001` until the Architect
   accepts an implementation wave; this analysis task does not replace it.
