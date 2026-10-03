# TB-TMAR-CATALOG-AMC-001-W0 — Physical tree before

## Solution Explorer

`Tooba.slnx` → `/Modules/Catalog/` contains Application, Contracts, Domain, Endpoints, Infrastructure.

## Domain (`Tooba.Catalog.Domain`)

- Root dump: 33 standalone `.cs` + god-file `CatalogDomain.cs` (~2361 LOC, enums+aggregates+rules+events)
- No `Aggregates/`, `Enums/`, `Rules/`, `Events/` folders
- Namespace today: flat `Tooba.Catalog.Domain`

## Application (`Tooba.Catalog.Application`)

- Capability folders present: Attributes, Brands, Categories, CategoryChanges, Facets, MegaMenu, Product*, Seller, Settings, Storefront, StoreLandingPages, StoreMenus, Tags, TemplateCatalog, Units, Variants, …
- Root dumps: `CatalogActorContext.cs`, `CatalogContracts.cs` (~1136 LOC ports+models), `CatalogStoreScope.cs`, `ProductPublishPrep.cs`, `StoreAppearanceSettingsWrite*`, `StoreLandingPageWrite*`, `StoreMenuWrite*`
- `Validators/CatalogValidationCodes.cs` only at technical Validators root (codes file OK)

## Infrastructure (`Tooba.Catalog.Infrastructure`)

- Capability folders: Adapters, Admin, Checkout, Development, Events, Grid, Persistence, Reservation, Seller, StoreAppearance, Storefront, StoreLanding, …
- Root dumps: ~35 `*Directory.cs` / gateway / reader files + `CatalogModule.cs` + `CatalogOutboxRegistration.cs`
- God-file: `CatalogDirectory.cs` (~1697 LOC)

## Contracts (`Tooba.Catalog.Contracts`)

- Folders: `Errors/`, `Cart/`, `Checkout/`, `Reservation/`
- Root dumps: 10 contract/port files

## Endpoints (`Tooba.Catalog.Endpoints`)

- Audience folders: Admin, Seller, Storefront, Errors, Resources
- Root: `CatalogEndpointModule.cs` (composition allowlist)
