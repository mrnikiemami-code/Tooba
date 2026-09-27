# Result / errors — W11

## Canonical path

Handlers return `Result` / `Result<T>` with `SemanticError(CatalogErrorCodes.*)`; endpoints use `ApiResponseFactory.From`.

## Typed codes (minimum)

| Condition | Code |
|---|---|
| Product missing | catalog.product.missing |
| Duplicate axes | catalog.variant.axes.duplicate |
| Definition missing | catalog.attribute.missing |
| Definition inactive | catalog.attribute.definition.inactive |
| Axis capability disabled | catalog.attribute.variant_axis.capability_disabled |
| Axis not in effective schema | catalog.variant.axis.schema_not_enabled |
| No effective variant axes | catalog.variant.effective_axes.missing |
| Free-text axis | catalog.attribute.variant_axis.value_kind.invalid |
| Option mismatch/inactive | catalog.attribute.enum_option.* |
| Combination limit | catalog.variant.combination.limit_exceeded |
| Patch target missing | catalog.variant.patch.target_missing |
| Invalid patch status | catalog.variant.patch.status_invalid |
| Default missing | catalog.variant.default.missing |
| Archived default | catalog.variant.default.archived_forbidden |

## Absent on moved surface

- PlatformHttpException expected flow
- InvalidOperationException expected flow / message-as-code
- ex.Message classification
- Hard-coded endpoint error text

Success payload `messageFa` / `warningFa` preserved as DTO fields (not transport errors).
