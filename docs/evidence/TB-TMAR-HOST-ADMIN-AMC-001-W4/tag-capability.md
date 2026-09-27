# Tag capability — W4

## Structure

```
Tooba.Catalog.Application/Tags/
  Commands/   Create, AssignProduct, RemoveProduct, AssignCategory, RemoveCategory (+ handlers)
  Queries/    ListTags, GetTag, ListProductTags, ListCategoryTags (+ handlers)
  Models/     CreateTagWriteModel
  Ports/      ITagDirectory
  Validators/ CreateTagCommandValidator
```

## Persistence

`TagDirectory : ITagDirectory` in Catalog.Infrastructure owns tag create/list/get/assign/remove.

`CatalogDirectory` tag members (except `PublishTagAsync`) delegate to `TagDirectory` and unwrap `Result` → `InvalidOperationException(code)` for CatalogDemo / legacy `ICatalogDirectory` callers.

## HTTP

`Catalog.Endpoints/Admin/Tags/CatalogTagEndpoints.cs` — 9 routes, `ICatalogAdminAuthorizer` + `ISender` + `ApiResponseFactory`.

No Endpoints → Infrastructure. No Catalog → Host.
