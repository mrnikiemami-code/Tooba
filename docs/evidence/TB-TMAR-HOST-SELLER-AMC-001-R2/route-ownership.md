# Host/Seller — Seller-R2 — Route Ownership

**Task:** TB-TMAR-HOST-SELLER-AMC-001-R2

## 1. Ownership matrix (after R2)

| Route | Verb | Owner |
| --- | --- | --- |
| `/v1/seller/catalog-variants` | GET | `Tooba.Catalog.Endpoints` (`CatalogSellerEndpoints`) |
| `/v1/seller/products/{productId:guid}/attributes/{definitionId:guid}` | PUT | `Tooba.Catalog.Endpoints` (`CatalogSellerEndpoints`) |
| `/v1/seller/products/{productId:guid}/variant-axes` | PUT | `Tooba.Catalog.Endpoints` (`CatalogSellerEndpoints`) |
| `/v1/seller/dashboard` | GET | `Tooba.Host` (`SellerPanelEndpoints`) |
| `/v1/seller/dev-contexts` | GET | `Tooba.Host` (`SellerPanelEndpoints`) |
| `/v1/seller/settings` | GET | `Tooba.Host` (`SellerSettingsEndpoints`) |
| `/v1/seller/settings` | PUT | `Tooba.Host` (`SellerSettingsEndpoints`) |

## 2. Counts

| Metric | Before R2 | After R2 |
| --- | --- | --- |
| Host-owned seller routes | 7 | 4 |
| Catalog-owned seller routes | 0 | 3 |
| Duplicate route ownership | 0 | 0 |

## 3. Duplicate-ownership proof

- The three migrated route templates appear in `SellerPanelEndpoints.cs` **zero** times
  (`HostSellerAmcR2GuardTests.Host_seller_owns_exactly_four_routes_and_no_catalog_route`).
- `SellerPanelEndpoints.cs` contains exactly two `group.Map*` calls; `SellerSettingsEndpoints.cs`
  contains exactly two `Map*` calls => Host-owned total 4.
- `CatalogSellerEndpoints.cs` contains all three templates exactly once.
- `MapCatalogSellerEndpoints()` is invoked exactly once, in `CatalogEndpointModule.cs`.
- `Program.cs` calls `MapCatalogModuleEndpoints()` and does **not** call
  `MapCatalogSellerEndpoints()` directly, so the Catalog seller group cannot be double-mapped.

## 4. Host namespace-source mapping (`SellerPanelEndpoints.cs`)

| Host source | Role after R2 |
| --- | --- |
| `group.MapGet("/dashboard", GetDashboardAsync)` | Host-owned dashboard shell (Order CQRS + Party display) |
| `group.MapGet("/dev-contexts", GetDevContexts)` | Host-owned Development actor snapshot |
| ~~`/catalog-variants`~~ | evacuated to Catalog |
| ~~`/products/{productId:guid}/attributes/{definitionId:guid}`~~ | evacuated to Catalog |
| ~~`/products/{productId:guid}/variant-axes`~~ | evacuated to Catalog |
