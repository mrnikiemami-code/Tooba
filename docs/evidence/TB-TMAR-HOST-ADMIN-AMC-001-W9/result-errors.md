# Result / errors — W9 Category Attribute-Schema

## Canonical path

`Result` / `Result<T>` → `ApiResponseFactory` → ProblemDetails via `CatalogErrorCatalogContributor` + resx.

## Stable codes (schema surface)

| Outcome | Code | HTTP |
|---|---|---|
| Category missing | catalog.schema.category.missing | 404 |
| Definition missing | catalog.attribute.missing | 404 |
| Duplicate binding | catalog.schema.binding.duplicate | 409 |
| Binding missing | catalog.schema.binding.missing | 404 |
| Reorder mismatched set | catalog.schema.reorder.invalid | 400 |
| Variant-axis capability disabled | catalog.attribute.variant_axis.capability_disabled | 400 |
| Variant-axis value kind invalid | catalog.attribute.variant_axis.value_kind.invalid | 400 |
| Generic schema invalid | catalog.schema.invalid | 400 |

Former Host collapse of all failures to `catalog.schema.invalid` + Persian `ex.Message` is replaced by typed codes (documented split).

## Absent on moved surface

- PlatformHttpException expected flow
- InvalidOperationException message-as-code expected flow
- Persian/heuristic message parsing
