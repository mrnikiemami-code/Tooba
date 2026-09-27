# Result / errors — W12

## Canonical path

`Result` / `Result<T>` → `ApiResponseFactory.From` → success raw JSON DTO; failure ProblemDetails via catalog.

## Stable codes

| Outcome | Code |
|---|---|
| Product missing | `catalog.product.missing` |
| Target category missing | `catalog.category.missing` |
| Level ≠ 3 | `catalog.category.assignment.level.invalid` (canonical Domain constant preserved) |
| Other invalid category-change | `catalog.category_change.invalid` |

Registered in `CatalogErrorCodes`, `CatalogErrorCatalogContributor`, `CatalogErrors.resx` + `.fa.resx`.

## Eliminated on migrated surface

- `PlatformHttpException` expected flow
- `InvalidOperationException` expected flow / catch
- `ex.Message == ProductAssignableLevelRequiredMessageFa`
- Persian message as machine classification

## Legacy thin wrappers

`CatalogDirectory` UnwrapCategoryChange maps assignment-level code → Persian IOE for ProductWorkspace / characterization tests only — not used by Catalog category-change HTTP.
