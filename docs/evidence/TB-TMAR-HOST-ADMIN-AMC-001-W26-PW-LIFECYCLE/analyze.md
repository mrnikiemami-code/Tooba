# Analyze — W26 ProductWorkspace lifecycle (W17-plan W21)

## Target
Host Admin ProductWorkspace lifecycle POSTs:
- `POST .../publish`
- `POST .../unpublish`
- `POST .../archive`
- `POST .../restore`

## True ownership
| Concern | Owner |
|---|---|
| Mutation authority | Catalog Commands + Domain lifecycle |
| HTTP returning `ProductWorkspaceView` | ProductWorkspace.Endpoints |
| Post-write composition | `GetProductWorkspaceQuery` |
| Actor history binding | `ICatalogActorContext` (per-request) |

## Disposition
| Member | Action |
|---|---|
| Host MapPost ×4 + handlers | MOVE → ProductWorkspace.Endpoints |
| Composer Publish/Unpublish/Archive/Restore | DELETE |
| Host ProductWorkspace* files | RETAIN_PARTIAL (W24 final) |
| StoreAppearance / variants / delete | OUT OF SCOPE |
| Admin `*.cs` count | **31 unchanged** |

## Cross-module
ProductWorkspace.Endpoints → Catalog.Application (command types only).
ProductWorkspace.Application stays Contracts-only (no Catalog.Application).

## Wave id
`TB-TMAR-HOST-ADMIN-AMC-001-W26-PW-LIFECYCLE` (series W21–W25 already used for Store*/seeds).
