# Disable impact preview (TB-P07-T039)

- `GET .../variant-axis-capability/disable-preview` returns non-mutating impact:
  - `categoryBindingCount`, `affectedCategories[]` (name + count), `productCount`, `variantCombinationCount`, `canDisable`
- In-use disable blocked with `catalog.attribute.variant_axis.in_use`
- Impact dialog in edit UI when `canDisable=false`; zero-usage confirm when allowed
