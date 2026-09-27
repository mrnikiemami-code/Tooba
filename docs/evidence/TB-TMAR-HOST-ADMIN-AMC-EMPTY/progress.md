# Host/Admin empty progress (AMC)

## Current Admin `*.cs` count: **36** (was 39)

Live disposition (reconciled with Analyze): `disposition-map-remaining-52.md` / session maps.

### Evacuated this session
| Slice | Owner | Notes |
|---|---|---|
| W20 brand-options | Catalog | Host PW routes 18→17 |
| CatalogDemo/* (10) | Catalog | Development under Catalog.Infrastructure |
| CheckoutAbuse settings | Catalog | Settings CQRS + ApiResponseFactory |
| StoreLandingPage Endpoints+Composer | Catalog | Host adapters for shell/merch |
| **StoreMenu*** (3) | Catalog | Workspace + Admin/Storefront endpoints; seed in Catalog.Infrastructure/Development |

### Next (from Analyze, risk order)
1. CheckoutIdentity → Catalog; ReservationPolicy* → Order
2. ProductWorkspace W21 lifecycle (RETAIN_PARTIAL files)
3. Merchandising → Promotion (Contracts first)

### Cannot empty yet
KEEP platform (16) · StoreAppearance BLOCK · HoldPolicy BLOCK · ProductWorkspace* until W24 · remaining MOVE queue.
