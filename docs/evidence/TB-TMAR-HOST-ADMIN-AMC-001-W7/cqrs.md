# CQRS — W7 Categories

| Route | Request | Handler | Port |
|---|---|---|---|
| GET .../tree | GetCategoryTreeQuery | GetCategoryTreeHandler | GetTreeAsync |
| GET .../{id} | GetCategoryWorkspaceQuery | GetCategoryWorkspaceHandler | GetWorkspaceAsync |
| POST .../ | CreateCategoryCommand | CreateCategoryHandler | CreateAsync |
| PATCH .../{id} | UpdateCategoryCoreCommand | UpdateCategoryCoreHandler | UpdateCore + GetWorkspace |
| PUT .../translations/{locale} | UpsertCategoryTranslationCommand | UpsertCategoryTranslationHandler | UpsertTranslationAsync |
| POST .../{id}/move | MoveCategoryCommand | MoveCategoryHandler | Move + GetWorkspace |
| POST .../reorder | ReorderCategoriesCommand | ReorderCategoriesHandler | ReorderAsync → CategoryOkResult |
| POST .../{id}/publish | PublishCategoryCommand | PublishCategoryHandler | Publish + GetWorkspace |
| POST .../{id}/archive | ArchiveCategoryCommand | ArchiveCategoryHandler | Archive + GetWorkspace |
| GET storefront/.../resolve | ResolveCategoryRouteQuery | ResolveCategoryRouteHandler | ResolveRouteAsync |

All requests: `IRequest<Result<T>>` / `IRequest<Result>` via MediatR `ISender`.
Endpoints inject no directory/DbContext.
