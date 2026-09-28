# TB-TMAR-HOST-ADMIN-CANON-001 — Contract Boundary Map

## AdminPanelComposer (Host) — after

| Consumer need | Boundary | Owner | Adapter |
|---|---|---|---|
| Published product count | `Tooba.Catalog.Contracts.ICatalogAdminProductCountGateway` | Catalog | `Catalog.Infrastructure.CatalogAdminProductCountGateway` |
| Active offer count | `Tooba.Offer.Contracts.Ports.IOfferQueryGateway` | Offer | existing Offer infrastructure gateway |
| Distinct seller party ids | `Tooba.Offer.Contracts.Ports.IOfferQueryGateway` | Offer | existing |
| Seller offer status rows | `Tooba.Offer.Contracts.Ports.IOfferQueryGateway` | Offer | existing |
| Seller display name + status | `Tooba.Party.Contracts.IPartyAdminSellerReadGateway` | Party | `Party.Infrastructure.Admin.PartyAdminSellerReadGateway` |
| Order dashboard counters | `Tooba.Order.Contracts.Admin.IAdminOrderDashboardMetricsPort` | Order | `Order.Infrastructure.Admin.AdminOrderDashboardMetricsPort` |
| Per-seller order counts | `Tooba.Order.Contracts.Admin.IAdminSellerOrderCountPort` | Order | `Order.Infrastructure.Admin.AdminSellerOrderCountPort` |

## AdminSellersGridQueryEngine (Host) — after

| Consumer need | Boundary |
|---|---|
| Party row projection (id/display name/status) | `IPartyAdminSellerReadGateway` |
| Offer metrics (all/active per seller) | `IOfferQueryGateway` |
| Seller ids universe | `IOfferQueryGateway.ListDistinctSellerPartyIdsAsync` |
| Order metrics per seller | `IAdminSellerOrderCountPort` |
| Filter/sort/page | Host-owned in-memory generic grid over the Contracts projections |

## Registry ownership (no Host registrations added)

| Contract | Registered in |
|---|---|
| `ICatalogAdminProductCountGateway` | `Catalog.Infrastructure.CatalogModule` |
| `IPartyAdminSellerReadGateway` | `Party.Infrastructure.PartyModule` |
| `IAdminOrderDashboardMetricsPort` | `Order.Infrastructure.OrderModule` |
| `IAdminSellerOrderCountPort` | `Order.Infrastructure.OrderModule` |

`Program.cs` host wiring only resolves `AdminPanelComposer` and `AdminSellersGridQueryEngine`; the new
adapters are resolved through `AddToobaModules`.

## Forbidden-edge verification

| Edge | Result |
|---|---|
| Host Admin composer `*.Infrastructure` | ZERO |
| Host Admin composer foreign `*.Application` | ZERO |
| Host Admin composer foreign `*.Domain` | ZERO |
| Host Admin composer `DbContext` | ZERO |
| Host sellers grid `DbContext` / `IQueryable` | ZERO |
| `A.Application → B.Application` newly added | ZERO |
| `A → B.Infrastructure` newly added | ZERO |
| Contract files exposing `DbContext`/`IQueryable`/ORM | ZERO |

## Durable guards

`src/backend/Host/Tooba.Host.Tests/Architecture/HostAdminCanon001GuardTests.cs`
(`HostAdminCanon001GuardTests`) asserts: composer boundary imports, seller-grid Contracts-only
surface, contract purity, adapter module ownership, route preservation, Host/Admin count = 15,
module-owned registrations, and W36 StoreAppearance closure.
