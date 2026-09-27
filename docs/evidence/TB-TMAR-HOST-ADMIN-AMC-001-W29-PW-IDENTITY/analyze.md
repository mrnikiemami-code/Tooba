# Analyze — W29 ProductWorkspace identity writes (W17-plan core writes)

## Target
Host Admin ProductWorkspace identity mutations:
- `POST /v1/admin/products/` (create)
- `PATCH .../catalog-title`
- `PATCH .../core`
- `PATCH .../quantity-policy`

## True ownership
| Concern | Owner |
|---|---|
| Mutation authority | Catalog Commands + ProductIdentityDirectory |
| HTTP returning `ProductWorkspaceView` | ProductWorkspace.Endpoints |
| Post-write composition | `GetProductWorkspaceQuery` |
| Create returns | `201` + workspace JSON after `Result<Guid>` |

## Disposition
| Member | Action |
|---|---|
| Host MapPost `/` + MapPatch ×3 + handlers | MOVE → ProductWorkspace.Endpoints |
| Composer Create/Title/Core/Quantity (+ UpsertLocalizedText) | DELETE |
| Admin create/core/quantity request DTOs | DELETE (models move to Catalog.Application) |
| Host ProductWorkspace* files | RETAIN_PARTIAL (list/grid/category/brand) |
| StoreAppearance / Merchandising | OUT OF SCOPE |
| Admin `*.cs` count | **31 unchanged** |

## Cross-module
ProductWorkspace.Endpoints → Catalog.Application (command types only).
ProductWorkspace.Application stays Contracts-only.

## Wave id
`TB-TMAR-HOST-ADMIN-AMC-001-W29-PW-IDENTITY`
