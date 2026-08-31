# Category binding regression (TB-P07-T039)

- `ValidateVariantAxis` enforces ValueKind + `IsVariantAxisAllowed` on bind/update
- Stable codes: `catalog.attribute.variant_axis.capability_disabled`, `catalog.attribute.variant_axis.value_kind.invalid`
- Inheritance/override preserved; `false→true` capability does not mutate bindings (integration test)
- Active variant binding blocks capability disable without cascade
