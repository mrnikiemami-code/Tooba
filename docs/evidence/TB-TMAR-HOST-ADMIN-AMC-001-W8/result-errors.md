# Result / errors — W8

## Canonical path

Handler/Directory → `Result` / `Result<T>` + `SemanticError(CatalogErrorCodes.*)` → `ApiResponseFactory`.

## Codes

| Code | HTTP | Notes |
|---|---|---|
| catalog.attribute.missing | 404 | get / preview / update / set / option target |
| catalog.attribute.invalid | 400 | input / non-enum option / metadata bounds / empty names |
| catalog.attribute.code.duplicate | 409 | typed; no Persian parse |
| catalog.attribute.name.duplicate | 409 | typed; no Persian parse |
| catalog.attribute.variant_axis.value_kind.invalid | 400 | enable capability / create with axis |
| catalog.attribute.variant_axis.in_use | 409 | disable blocked |
| catalog.attribute.variant_axis.capability_disabled | 400 | catalogued for retained Host assignment paths |

Registered in `CatalogErrorCatalogContributor` + `CatalogErrors.resx` / `.fa.resx`.

## Removed from Definition surface

- PlatformHttpException catch-and-map
- InvalidOperationException expected-business flow
- Persian message classification (`Contains("کد")` / `تکراری`)
- hard-coded Results.Json Problem bags

Retained Host Attribute routes still use `MapAttributeInvalid` until later waves.
