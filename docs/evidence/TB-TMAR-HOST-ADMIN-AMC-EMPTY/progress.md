# Host/Admin empty progress (AMC)

## Current Admin `*.cs` count: **15** (W36 StoreAppearance evacuated — KEEP platform floor)

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
| **HoldPolicy aggregate (W35)** | Catalog.Endpoints + Application Settings/HoldPolicy (+ Contracts ports) |
| **StoreAppearance (W36)** | Catalog.Endpoints + Application Settings/StoreAppearance + Infrastructure projector |

### Host PW routes remaining: **0**
(shell MapProductWorkspaceEndpoints removed)

### Admin evacuation MOVE complete
Remaining Host Admin is **KEEP platform only** (authorizers, panel access, grid query, panel composer/endpoints/models, AdminDevActorBootstrap).

### Cannot empty yet
KEEP platform (~15)
