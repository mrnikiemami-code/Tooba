# Host/Admin empty progress (AMC)

## Current Admin `*.cs` count: **31** (unchanged — RETAIN_PARTIAL)

### Evacuated
| Slice | Owner |
|---|---|
| W20–W25 store/settings/seeds | Catalog / Order |
| PW lifecycle (W26) | ProductWorkspace.Endpoints |
| PW variants (W27) | ProductWorkspace.Endpoints |
| Product DELETE (W28) | Catalog.Endpoints + Offer.Contracts gate |
| PW identity create/title/core/quantity (W29) | ProductWorkspace.Endpoints + Catalog.ProductIdentity |
| PW taxonomy category/brand (W30) | ProductWorkspace.Endpoints + Catalog.ProductTaxonomy |
| **PW list + grid (W31)** | ProductWorkspace.Endpoints + Catalog list Contracts |

### Host PW routes remaining: **0**
(shell MapProductWorkspaceEndpoints retained until W32)

### Next
1. Merchandising → Promotion
2. Template/attribute seeds
3. W32 / W24-final: delete Host ProductWorkspace* shells (Admin count drops)

### Cannot empty yet
KEEP platform (~16) · StoreAppearance BLOCK · HoldPolicy BLOCK · ProductWorkspace* shells · Merchandising MUST_SPLIT
