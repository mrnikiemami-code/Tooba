# W22-STORE-MENU analyze

## Source
- `Host/Admin/StoreMenuEndpoints.cs` (HTTP_ENDPOINT — ILLEGAL_ENDPOINT_OWNERSHIP)
- `Host/Admin/StoreMenuComposer.cs` (APPLICATION_USE_CASE + cache + CatalogDbContext reads)
- `Host/Admin/StoreMenuDevelopmentSeed.cs` (DEVELOPMENT_SEED)

## Destination
- Catalog — FOUNDATION_READY (Endpoints/Application/Infrastructure/Domain/Contracts exist; write CQRS + Directory already Catalog-owned)

## Disposition
| Responsibility | Class | Destination |
|---|---|---|
| Admin `/v1/admin/menus*` routes | HTTP_ENDPOINT | Catalog.Endpoints/Admin/StoreMenus |
| Storefront `/v1/storefront/header-menu` + `/menus/{id}` | HTTP_ENDPOINT | Catalog.Endpoints/Storefront/StoreMenus |
| Menu read/write orchestration + cache | APPLICATION_USE_CASE | Catalog.Application ports + Infrastructure `StoreMenuWorkspace` |
| Request/response models | APPLICATION models | Catalog.Application/StoreMenus/Models |
| Existing write Commands/Directory | already Catalog | reuse |
| Cache scope key | CROSS_CUTTING | `CatalogStoreScope.ScopeKey` (no Host reference) |
| Demo seed | DEVELOPMENT | Catalog.Infrastructure/Development; thin Host commerce wrapper |
| StoreAppearance* / ProductWorkspace* / Merchandising* | OUT OF SCOPE | deferred |

## BLOCK leftovers in scope
- None for Host StoreMenu* evacuation.
- Directory/Domain `PlatformHttpException` throws remain transitional (mapped to Result at Application facade).
