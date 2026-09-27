# Error / Result migration — W3

## Removed

- Host catch of `PlatformHttpException` / `InvalidOperationException` with `Results.Json({title,errorCode})`.
- Directory throws of `PlatformHttpException` (`unit.missing`) and `InvalidOperationException` (`unit.dimension.invalid`, `unit.code.duplicate`, `unit.language.unknown` via gate).

## Canonical

| Code | CatalogErrorCodes | HTTP |
|---|---|---|
| `unit.missing` | `UnitMissing` | 404 |
| `unit.dimension.invalid` | `UnitDimensionInvalid` | 400 |
| `unit.code.duplicate` | `UnitCodeDuplicate` | 400 |
| `unit.language.unknown` | `UnitLanguageUnknown` | 400 |

Contributor + `CatalogErrors.resx` / `.fa.resx` extended. `CatalogErrorResourceSet.Owns` includes `unit.*`.

Endpoints map failures exclusively through `ApiResponseFactory` / error catalog — no `ex.Message` classification.
