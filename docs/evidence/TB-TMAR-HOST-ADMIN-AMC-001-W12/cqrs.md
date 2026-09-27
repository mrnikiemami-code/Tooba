# CQRS — W12

| Route | Request | Handler | Port |
|---|---|---|---|
| POST …/category-change-preview | `PreviewCategoryChangeQuery` | `PreviewCategoryChangeHandler` | `ICategoryChangeDirectory.PreviewReportAsync` |
| PUT …/primary-category | `ReplacePrimaryCategoryCommand` | `ReplacePrimaryCategoryHandler` | `ICategoryChangeDirectory.ReplacePrimaryAsync` |

Pipeline: HTTP → `ICatalogAdminAuthorizer` → `CatalogActorRequestBinding` → `ISender` → handler → directory → `Result` → `ApiResponseFactory`.

Endpoints inject neither directory nor `CatalogDbContext`.
