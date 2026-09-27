# Result / errors — W4 Tags

## Prior wire semantics (Host)

| Outcome | Prior status | Prior errorCode |
|---|---|---|
| Missing localized name | 400 Problem | `catalog.tag.invalid` |
| Duplicate explicit code | 400 Problem | `catalog.tag.invalid` (same code) |
| Duplicate product/category assign | 400 Problem | `catalog.tag.assign.duplicate` |
| Get missing | 404 bare NotFound | (no errorCode) |
| Missing product/category/tag on assign | incorrectly caught as assign.duplicate | `catalog.tag.assign.duplicate` |

## Canonical codes (CatalogErrorCodes + contributor + resx)

| Code | Status | Notes |
|---|---|---|
| `catalog.tag.invalid` | 400 | Missing name / empty localized names |
| `catalog.tag.code.duplicate` | 400 | Normalized: was same wire code as invalid |
| `catalog.tag.missing` | 404 | Get + assign when tag missing |
| `catalog.tag.assign.duplicate` | 400 | Product or category duplicate assign |
| `catalog.tag.product.missing` | 404 | Assign product missing |
| `catalog.tag.category.missing` | 404 | Assign category missing |

No `InvalidOperationException` / `PlatformHttpException` / message parsing on Tag HTTP or `TagDirectory`.
Unknown exceptions propagate to global boundary.
