# Variant axis semantics (TB-P07-T039)

- `AttributeDefinition.IsVariantAxisAllowed` (DB column `IsVariantAxis`): attribute **may** be used as a variant axis.
- Category binding `IsVariant`: category **actually** uses the attribute as a variant axis.
- Product variants derive from effective Primary Category variant schema only.
- Enabling capability does not auto-bind categories or create product variants.
