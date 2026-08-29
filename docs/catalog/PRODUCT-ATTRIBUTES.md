# Product Attributes

> **Task:** TB-P07-T012 · **Phase:** P07 Advanced Catalog  
> **Depends on:** [CATEGORY-ATTRIBUTE-SCHEMA.md](./CATEGORY-ATTRIBUTE-SCHEMA.md), [PRODUCT-CATALOG-ADMIN.md](./PRODUCT-CATALOG-ADMIN.md)

## Ownership

Product Master attribute **values** live in Catalog (`CatalogProductAttributeValue`).

- Typed canonical values only — not free-form JSON bags
- Driven by the product’s primary category → **effective category attribute schema** (T007)
- Product ≠ Offer ≠ Price ≠ Inventory

## Typed value model

| ValueKind | Storage (canonical) | Editor control |
|-----------|---------------------|----------------|
| Text | trimmed string | text |
| Number | invariant decimal string | number (+ unit label) |
| Boolean | `True` / `False` | بله / خیر |
| Enumeration | option Guid `N` format | localized select |
| Instant | ISO-8601 | datetime-local |
| Multivalue Enumeration | comma-separated option Guids `N` | multi-checkbox |

Uniqueness: single-valued definitions keep one row per `(ProductId, DefinitionId)`.

**MVP note:** multivalue non-enumeration kinds are not first-class; multivalue Enumeration uses comma-separated option ids in `RawValue`.

## Effective schema

Source of truth: `GetEffectiveCategorySchema` / category bindings with inheritance.

Editor fields include:

- Localized name (`CatalogLocalizedText`, `OwnerKind=AttributeDefinition` / `AttributeOption`)
- ValueKind, unit, required / filterable / comparable / multivalue
- Display order
- Options (active preferred)
- Current canonical + display value
- Missing-required flag

### Variant axes

Effective `IsVariantAxis` fields appear as **informational** rows only (محور تنوع — در تب تنوع‌ها).

They are **not** editable product-level values. `SetProductAttribute` / batch set reject axis definitions.

Full matrix authoring: [PRODUCT-VARIANTS.md](./PRODUCT-VARIANTS.md).

## APIs

| Method | Path |
|--------|------|
| GET | `/v1/admin/catalog/products/{id}/attributes?locale=` |
| PUT | `/v1/admin/catalog/products/{id}/attributes` body `{ locale?, values:[{ definitionId, rawValue, enumOptionId, clear }] }` |
| GET | `/v1/admin/catalog/products/{id}/attributes/readiness` |
| POST | `/v1/admin/catalog/products/{id}/category-change-preview` → enriched report |

Module methods: `GetProductAttributeEditorStateAsync`, `SetProductAttributesAsync`, `GetProductAttributeReadinessAsync`, `PreviewCategoryChangeReportAsync`.

## Validation

Backend rejects:

- unknown / inactive definition
- definition outside effective schema (when schema-bound)
- wrong type / bounds
- invalid or inactive enum option
- clear of a required field
- writing variant-axis definitions onto the product

Frontend marks required fields, blocks invalid save with localized field errors, and never shows raw IDs as the primary control (enum = localized select).

## Readiness

`ProductAttributeReadiness`:

- `IsComplete`
- `MissingRequiredCodes`
- `InvalidValues`

Publishing tab may consume this later; publish still runs `ValidateProductAttributesAsync`.

## Category change

`PreviewCategoryChangeReportAsync` compares current values to the **new** effective schema:

| Count | Meaning |
|-------|---------|
| CompatiblePreservedCount | values whose definition remains in the new schema |
| OrphanCount | values no longer in schema (not silently deleted) |
| NewlyRequiredMissingCount | required fields in new schema without a value |

Persian `MessageFa` example:

```text
۳ مقدار حفظ می‌شود
۲ ویژگی دیگر در دسته جدید وجود ندارد
۱ ویژگی الزامی جدید باید تکمیل شود
```

After confirmed category assign/replace: compatible values remain; orphans stay until an explicit policy removes them (no silent delete on preview or assign). Documented policy: preview never mutates; `ReplaceProductPrimaryCategoryAsync` swaps category links only and leaves orphan value rows for follow-up cleanup/audit.

## PLP / PDP

Filterable saved product values continue to feed T010 PLP facets via existing `ProductAttributeValues` — no duplicate facet store.

## Localization

- Attribute / option **labels**: localized definitions
- Free-text attribute **values**: not treated as translatable product fields (no `NameFa`/`NameEn`); gap if multilingual value text is required later

## Admin UX

Product Workspace → ویژگی‌ها:

- Summary: completion, missing required count, category path
- VIEW: readable rows + chips + missing/required badges
- EDIT: typed controls, Save all / Cancel, dirty state
- View-only (`canEdit=false` or VIEW mode): no editable controls
