# Result / errors — W5 MegaMenu

## Codes

| Code | HTTP | When |
|---|---|---|
| `catalog.megamenu.category.missing` | 404 | Category absent on get/upsert |
| `catalog.megamenu.placement.invalid` | 400 | Tree placement violation (self-parent, missing parent, cycle, max depth) |
| `catalog.megamenu.remove.has_children` | 400 | Remove while presentation children exist |

## Mapping

- Handlers/Directory return `Result` / `Result<T>` + `SemanticError(CatalogErrorCodes.*)`
- Endpoints: `ApiResponseFactory.From(...)`
- Contributor + `CatalogErrors.resx` / `.fa.resx` registered
- Domain `CatalogMegaMenuTreeRules.ValidatePlacement` returns `CatalogMegaMenuPlacementViolation` (no IOE)

## Absent on MegaMenu surface

- PlatformHttpException
- InvalidOperationException expected-business / message-as-code
- Results.Problem(ex.Message)
