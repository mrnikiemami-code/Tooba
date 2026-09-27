# Analyze — W31 ProductWorkspace list + grid

## Target
Host Admin ProductWorkspace list/grid:
- `GET /v1/admin/products/`
- `POST /v1/admin/products/query`

## True ownership
| Concern | Owner |
|---|---|
| HTTP | ProductWorkspace.Endpoints |
| Composition queries | ListProductWorkspaceQuery / QueryProductWorkspaceGridQuery |
| Catalog page IDs + list slices | ICatalogAdminProductWorkspaceListGateway (Catalog.Infrastructure engine) |
| Commercial enrich | Offer / Pricing / Inventory Contracts in ProductWorkspace.Application |
| Grid policy | AdminProductGridQueryPolicy (Application; GridQueryValidationException → SemanticError) |

## Disposition
| Member | Action |
|---|---|
| Host MapGet `/` + MapPost `/query` + handlers | DELETE maps (shell file retained) |
| Composer ListAsync / QueryGridAsync / BuildListItems* | DELETE |
| Host AdminProductGridQueryEngine / Policy | MOVE → Catalog.Infrastructure.Grid + ProductWorkspace.Application.Grid |
| AdminProductListItem | MOVE → ProductWorkspace.Application.Composition.Models |
| Host ProductWorkspace* files | RETAIN_PARTIAL (shells until W32) |
| Merchandising / StoreAppearance | OUT OF SCOPE |
| Admin `*.cs` count | **31 unchanged** |

## Wave id
`TB-TMAR-HOST-ADMIN-AMC-001-W31-PW-LIST-GRID`
