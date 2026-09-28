# TB-TMAR-HOST-ADMIN-CANON-001 — Analyze

## Scope

Host/Admin module evacuation is complete at the 15-file platform KEEP floor (W36). This task begins
canonicalization of the remaining Host platform surface:

`src/backend/Host/Tooba.Host/Admin/AdminPanelComposer.cs`

and its immediate `/admin/sellers/query` grid path:

`src/backend/Host/Tooba.Host/Grid/AdminSellersGridQueryEngine.cs`

## Baseline defect (before)

`AdminPanelComposer.cs` direct foreign dependencies at parent commit
`e76bcb0d4e56aedad45c10285cb6bf318094383a`:

| Dependency | Kind |
|---|---|
| `Tooba.Catalog.Domain` (`CatalogPublicationStatus`) | foreign Domain |
| `Tooba.Catalog.Infrastructure.Persistence` (`CatalogDbContext`) | foreign Infrastructure/ORM |
| `Tooba.Party.Infrastructure.Persistence` (`PartyDbContext`) | foreign Infrastructure/ORM |
| `Tooba.Order.Application.Admin.Dashboard.Queries.GetAdminOrderDashboardMetrics` | foreign Application + MediatR request |
| `Tooba.Order.Application.Admin.Sellers.Ports` (`ISellerOrderCountReader`) | foreign Application |
| `Microsoft.EntityFrameworkCore` (`CountAsync`, `ToListAsync`) | ORM in Host |

`AdminSellersGridQueryEngine.cs` additionally held `PartyDbContext`, `Tooba.Party.Domain`
(`BusinessParty`, `PartyStatus` semantics) and `Microsoft.EntityFrameworkCore`/`IQueryable`
grid translation, plus `Tooba.Order.Application...ISellerOrderCountReader`.

## Existing Contracts surveyed (reuse over recreation)

| Module | Existing Contracts | Gap |
|---|---|---|
| Catalog | `ICatalogOfferReadGateway`, `ICatalogAdminProductWorkspaceReadGateway`, `ICatalogAdminProductWorkspaceListGateway`, `ICatalogVariantLookup` | no published-product count port |
| Party | `IPartyLookup` (id → kind/display name; display-name search/filter) | no `Status` projection, no seller-row batch slice |
| Order | `ICustomerOrderDashboardSummaryPort` (customer account), `ReservationCyclePolicyPreviewContracts` | no Admin dashboard counters port, no public seller-count port |
| Offer | `IOfferQueryGateway` (`CountActiveOffersAsync`, `ListSellerStatusRowsAsync`, `CountActiveOffersBySellerAsync`, `ListDistinctSellerPartyIdsAsync`) | none — already lawful |

Offer was already Contracts-only and was left unchanged.

## Decision

Add the smallest focused Contracts read ports required, each implemented inside its owning module,
instead of exposing ORM types or leaking foreign Application/MediatR request types into Host:

1. `Tooba.Catalog.Contracts.ICatalogAdminProductCountGateway` → `CountPublishedProductsAsync`.
2. `Tooba.Party.Contracts.IPartyAdminSellerReadGateway` → `GetStatusProjectionsAsync` (`PartyId`,
   `DisplayName`, `Status` as string) preserving exact seller status/display-name semantics.
3. `Tooba.Order.Contracts.Admin.IAdminOrderDashboardMetricsPort` → dashboard counters
   (Open/Paid/Pending/Customers).
4. `Tooba.Order.Contracts.Admin.IAdminSellerOrderCountPort` → per-seller counts.

The Admin seller grid moved from EF/`IQueryable` translation to an in-memory
filter/sort/page over the Contracts projections, preserving field names, operators,
sort fields (`name`, `status`, `offers`, `orders`), default sort (`name asc`),
tie-breakers, and paging semantics.

Host ownership of the cross-module platform composition (`AdminPanelComposer`) was retained; the
dashboard/seller aggregate was **not** moved into any module.
