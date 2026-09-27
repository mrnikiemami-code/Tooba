# W19 — Catalog read boundary

## Contract

`Tooba.Catalog.Contracts.ICatalogAdminProductWorkspaceReadGateway`

Method:
`Task<CatalogAdminProductWorkspaceSnapshot?> GetAggregateSnapshotAsync(Guid productId, CancellationToken cancellationToken)`

## Snapshot semantics (Catalog-owned projection)

Provides all Catalog fields required by aggregate GET without EF/domain leaks:

- product identity/status/kind/updatedAt
- title fallback inputs (localized name / slug / untitled applied by gateway to match Host)
- variants (id/fingerprint/status/catalogCodeSeam) — OfferCount/LocationCount filled by ProductWorkspace commercial composition
- product + variant-axis attributes
- media refs (primary-first, DisplayOrder)
- category assignments + leaf names + full paths + primary assignability + assignability warning FA
- brand id/name
- SEO seams + translations + short description
- quantity policy + unit options/code/display
- publish readiness shell + catalog readiness messages
- activity/audit history shells (take 20; audit filter lifecycle/seo/category)

## Implementation

`CatalogAdminProductWorkspaceReadGateway` in Catalog.Infrastructure:

- uses `CatalogDbContext` internally
- uses `IProductPublishReadinessReader` / `IProductHistoryReader` (Result) — maps failure to Host-compatible empty readiness/history shells (no IOE to ProductWorkspace)
- registered in `CatalogModule`

## Forbidden exposures

- CatalogDbContext
- EF entities
- Domain entities
- IQueryable
- Offer/Price/Inventory/Tax/Party data
