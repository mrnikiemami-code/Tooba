# Host/Admin empty progress (AMC)

## Current Admin `*.cs` count: **18** (W34 template/attribute seeds evacuated)

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
| **Merchandising admin (W33)** | Promotion.Endpoints + Infrastructure |
| **PW Host shells (W32)** | DELETED; bootstrap → Host.Development |
| **Template + attribute seeds (W34)** | Catalog.Infrastructure/Development (+ Host SeedHost / sellable enricher) |

### Host PW routes remaining: **0**
(shell MapProductWorkspaceEndpoints removed)

### Next
1. HoldPolicy MUST_SPLIT
2. StoreAppearance BLOCK

### Cannot empty yet
KEEP platform (~16) · StoreAppearance BLOCK · HoldPolicy BLOCK
