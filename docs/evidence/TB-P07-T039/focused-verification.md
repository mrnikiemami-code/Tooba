# Focused verification (TB-P07-T039)

Contract/smoke expectations:

- `five_g` (Boolean) → ValueKind disabled reason
- `screen_size` (Number, capability false) → capability disabled reason (not type-invalid)
- `color` / `storage` (Enumeration, allowed) → variant checkbox selectable when bound
- `false→true` capability: bindings unchanged
- `true→false` with active variant binding: preview + block
- Zero-usage disable: allowed via `CanDisable=true` path

Automated: `variant-axis-capability.test.ts`, `category-attributes-panel.test.ts`, `VariantAxisCapabilityRulesTests`, docker integration tests in `CatalogAttributeSchemaTests`.
