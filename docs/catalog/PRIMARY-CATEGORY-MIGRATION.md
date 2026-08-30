# Primary Category Migration

> **Task:** TB-P07-T036 · **Phase:** P07 Advanced Catalog  
> **Depends on:** [PRODUCT-CATEGORY-ASSIGNMENT.md](./PRODUCT-CATEGORY-ASSIGNMENT.md), [PRODUCT-ATTRIBUTES.md](./PRODUCT-ATTRIBUTES.md), [PRODUCT-VARIANTS.md](./PRODUCT-VARIANTS.md), [PRODUCT-PUBLISHING.md](./PRODUCT-PUBLISHING.md)

## Rule

Primary Category is schema-driving. Changing it is a **structural migration**, not a combobox save.

Only Product Workspace may change Primary. Category → Products creates **display (Additional)** membership only.

## Preview (non-mutating)

`PreviewCategoryChangeReportAsync` returns a human impact report (T036-P), including:

- current / target category path
- preserved / added (newly required) / removed attribute labels
- required-missing blockers
- variant compatibility + preserved/affected counts
- Additional→Primary promotion flag
- other display memberships that remain
- Persian readiness blockers

Preview never writes.

## Confirmed migration (transactional)

`ReplaceProductPrimaryCategoryAsync` runs in one DB transaction:

| Concern | Behavior |
|--------|----------|
| Attribute match | By **AttributeDefinition ID** only |
| Compatible values | Preserved |
| Orphans (not in target schema) | **Removed** from `ProductAttributeValues` |
| Newly required defs | No fake values; readiness incomplete until filled |
| Target was Additional | Atomically promote (delete Additional row, insert Primary) |
| Other display memberships | Remain |
| Invalid variant axes | Removed from `ProductVariantAxes` |
| Incompatible variants | Non-archived → **Draft**, defaults cleared; combinations **not** hard-deleted |
| History | Human paths + preserve/new/removed/variant counts; no raw GUID/JSON |

## Published safety (F)

If the product is **Published** and migration creates structural incompatibility — orphans removed, newly required missing, or variant axis incompatibility — Catalog calls the existing **Unpublish** domain path (`Status → Draft`) inside the same transaction.

Rationale: do not leave a publicly Published product with broken attribute/variant state. Re-publish requires normal readiness checks.

Compatible migrations (no orphans, no new required gaps, axes unchanged) leave Published status intact.

## Related

- [PRODUCT-HISTORY.md](./PRODUCT-HISTORY.md)
- [PRODUCT-PUBLISHING.md](./PRODUCT-PUBLISHING.md)
