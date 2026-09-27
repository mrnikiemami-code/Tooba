# Analyze — W30 ProductWorkspace taxonomy writes (category/brand)

## Target
Host Admin ProductWorkspace taxonomy mutations:
- `PUT /v1/admin/products/{productId}/category`
- `POST .../categories/additional`
- `DELETE .../categories/additional/{categoryId}` (query `expectedUpdatedAt`)
- `PUT .../brand`

## True ownership
| Concern | Owner |
|---|---|
| Mutation authority | Catalog Commands + ProductTaxonomyDirectory |
| HTTP returning `ProductWorkspaceView` | ProductWorkspace.Endpoints |
| Post-write composition | `GetProductWorkspaceQuery` |
| DELETE missing expectedUpdatedAt | `catalog.category.assignment.stale` via ApiResponseFactory |

## Disposition
| Member | Action |
|---|---|
| Host MapPut category/brand + MapPost/Delete additional + handlers | MOVE → ProductWorkspace.Endpoints |
| Composer Assign/Add/Remove category + AssignBrand | DELETE |
| Admin category/additional/brand request DTOs | DELETE (models move to Catalog.Application) |
| Host ProductWorkspace* files | RETAIN_PARTIAL (list/grid only) |
| StoreAppearance / Merchandising | OUT OF SCOPE |
| Admin `*.cs` count | **31 unchanged** |

## Cross-module
ProductWorkspace.Endpoints → Catalog.Application (command types only).
ProductWorkspace.Application stays Contracts-only.
Reuse `ICatalogDirectory` for replace/assign/add/remove/history.

## Wave id
`TB-TMAR-HOST-ADMIN-AMC-001-W30-PW-TAXONOMY`
