# Category capability — W7

## Structure

```
Tooba.Catalog.Application/Categories/
  Commands/   Create, UpdateCore, UpsertTranslation, Move, Reorder, Publish, Archive (+ handlers)
  Queries/    GetTree, GetWorkspace, ResolveRoute (+ handlers)
  Models/     CreateCategoryWriteModel, CategoryOkResult
  Ports/      ICategoryDirectory
  Validators/ GetTree, Create, UpsertTranslation, Reorder, ResolveRoute
```

Shared DTOs remain in Application `CatalogContracts.cs` (`CategoryTreeNodeDto`, `CategoryWorkspaceSummaryDto`, `CategoryCreateRequest`, …) — no Categories `*Contracts.cs` bundle.

## Persistence

- `ICategoryDirectory` (Application.Categories.Ports)
- `CategoryDirectory` (Infrastructure) — Result + CatalogErrorCodes
- `CatalogDirectory` retains thin one-way Unwrap wrappers for CatalogDemo / foundation tests

## Endpoints

- `Endpoints/Admin/Categories/CatalogCategoryAdminEndpoints.cs` — 9 Admin routes
- `Endpoints/Storefront/Categories/CatalogCategoryStorefrontEndpoints.cs` — resolve route
- Registered via `CatalogEndpointModule.MapCatalogModuleEndpoints`
