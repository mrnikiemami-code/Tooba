# Result / errors — W10

Handler/Directory → `Result` / `Result<T>` + `SemanticError(CatalogErrorCodes.*)` → `ApiResponseFactory`.

## Typed codes (product-attribute surface)

| Outcome | Code |
|---|---|
| Product missing | catalog.product.missing |
| Definition missing | catalog.attribute.missing |
| Definition inactive | catalog.attribute.definition.inactive |
| Not in effective schema | catalog.attribute.schema.not_allowed |
| Variant-axis on product | catalog.attribute.variant_axis.on_product_forbidden |
| Enum option required | catalog.attribute.enum_option.required |
| Enum option mismatch | catalog.attribute.enum_option.mismatch |
| Enum option inactive | catalog.attribute.enum_option.inactive |
| Enum option invalid | catalog.attribute.enum_option.invalid |
| Value canonicalize fail | catalog.attribute.value.invalid |
| Validation bounds | catalog.attribute.validation.bounds |
| Clear required forbidden | catalog.attribute.clear.required_forbidden |
| Value empty | catalog.attribute.value.empty |

Canonicalizer throws are mapped by **operation** (canonicalize vs bounds), not by parsing `ex.Message`.

No PlatformHttpException / expected IOE message-as-code on moved surface.
Registered in `CatalogErrorCatalogContributor` + CatalogErrors.resx/.fa.resx.
