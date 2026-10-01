# Analyze — Host/CatalogAdapters AMC-001

## Target

`src/backend/Host/Tooba.Host/CatalogAdapters/` (3 files):

- `MerchandisingStoreLandingReferenceGate.cs`
- `StoreLandingMerchandisingAdapter.cs`
- `StoreLandingShellAdapter.cs`

## True ownership

| Responsibility | Owner |
|---|---|
| Store landing external campaign reference gate | Catalog.Infrastructure.StoreLanding |
| Store landing merchandising resolve adapter | Catalog.Infrastructure.StoreLanding |
| Store landing shell product/category/brand/article/review cards | Catalog.Infrastructure.StoreLanding |
| Merchandising campaign runtime query seam | Promotion.Contracts.Merchandising |
| Merchandising directory / admin composer / EF | Promotion.Application + Promotion.Infrastructure |
| Host/CatalogAdapters folder | ZERO after evacuation |

## Coupling / blockers

1. Host CatalogAdapters was cross-module Composition (Promotion ↔ Catalog Landing) living illegally on Host.
2. Adapters consumed `Promotion.Application` merchandising runtime ports — foreign Application on Host path.
3. Store landing ports are Catalog Application ports; adapters belong in Catalog.Infrastructure.
4. Merchandising runtime constants/query interface must be Contracts-only for Catalog to consume without Promotion.Application.

## Final disposition

`READY_TO_MIGRATE` → Catalog.Infrastructure StoreLanding adapters + Promotion.Contracts merchandising seam; `HOST_ZERO`.
