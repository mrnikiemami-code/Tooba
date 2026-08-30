# Attributes & Variants — TB-P07-T034

## Attributes
- Effective schema from Primary L3.
- All **required non-variant** attributes filled with typed values.
- Definitions with `IsVariantAxisAllowed` are never stored on Product (Catalog rule); values live on Variants when binding marks axis.
- Matrix fix: towers/aio/foundation bindings that required variant-allowed defs as non-axis were corrected to axis bindings.

## Variants
- Axes from `GetProductVariantEditorStateAsync`.
- Modest option subsets (2–3 per axis, cap-safe).
- Default variant set; deterministic `CatalogCodeSeam` (`DEMO-…`).
- No Variant.Price/Stock.

## Tags
- 2–5 existing `demo-tag-*` per product via `AssignProductTagAsync`.
