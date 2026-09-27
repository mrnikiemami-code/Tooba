# Host/Admin empty progress (AMC)

## Current Admin `*.cs` count: **39** (was 52)

Live disposition (reconciled with Analyze): `disposition-map-live-39.md`.

### Evacuated this session
| Slice | Owner | Notes |
|---|---|---|
| W20 brand-options | Catalog | Host PW routes 18→17 |
| CatalogDemo/* (10) | Catalog | Development under Catalog.Infrastructure |
| CheckoutAbuse settings | Catalog | Settings CQRS + ApiResponseFactory |
| StoreLandingPage Endpoints+Composer | Catalog | Host adapters for shell/merch |

### Next (from Analyze, risk order)
1. **StoreMenu*** → Catalog
2. CheckoutIdentity → Catalog; ReservationPolicy* → Order
3. ProductWorkspace W21 lifecycle (RETAIN_PARTIAL files)
4. Merchandising → Promotion (Contracts first)

### Cannot empty yet
KEEP platform (16) · StoreAppearance BLOCK · HoldPolicy BLOCK · ProductWorkspace* until W24 · remaining MOVE queue.
