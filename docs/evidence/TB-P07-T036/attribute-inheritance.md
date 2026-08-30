# Attribute Inheritance & Overrides (TB-P07-T036)

## Canonical behavior
- AttributeDefinition is unique; effective schema contains each DefinitionId once.
- Child may override **binding** behavior for the same DefinitionId (Required/Filterable/Variant/Comparable/order/facet metadata).
- Nearest explicit Category binding in ancestry wins.
- Reset removes child override only — never deletes AttributeDefinition or parent binding.

## UX
- Inherited: «به ارث رسیده از <parent>», action «تنظیم اختصاصی برای این دسته»
- Override badge: «تنظیم اختصاصی»; reset: «بازگشت به تنظیمات والد»
- Duplicate message when re-adding inherited definition.

## Tests
`CatalogAttributeSchemaTests` + `category-attributes-panel` source asserts.
