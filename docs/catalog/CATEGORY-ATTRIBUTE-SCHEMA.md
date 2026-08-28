# Category Attribute Schema

> **Task:** TB-P07-T007 / TB-P07-T007-R1 · **Phase:** P07 Advanced Catalog
> **Baseline:** Category Workspace + T004 attribute foundation

## Purpose

Each catalog category has an **effective attribute schema**: product attributes that apply to products in that category, including inherited and local assignments.

## Definition vs Assignment (canonical separation)

### AttributeDefinition = intrinsic identity / capability

Global, Admin-owned. Describes **what** the attribute is:

| Field | Role |
|-------|------|
| `Code` | Stable internal identifier |
| `ValueKind` | Text, Number, Boolean, Enumeration, Instant |
| `LocalizedNames` | Translation-backed labels |
| `IsVariantAxisAllowed` | **Capability:** this type *may* be used as a variant axis |
| `Unit`, validation bounds, `IsMultivalue`, `IsActive` | Intrinsic metadata |

Definition-level `IsRequired`, `IsFilterable`, `IsComparable` remain as **legacy defaults** for migrations/bootstrap only. **Effective category behavior is NOT read from these fields after T007-R1.**

### CategoryAttributeAssignment = category-specific behavior

`CatalogCategoryAttributeBinding` per category:

| Field | Role |
|-------|------|
| `IsRequired` | Required for products in this category |
| `IsFilterable` | Eligible for PLP filters in this category (T008 consumes) |
| `IsVariantAxis` | **Enabled here** as variant axis (requires `IsVariantAxisAllowed`) |
| `IsComparable` | Eligible for comparison in this category |
| `DisplayOrder` | Order among bindings owned by this category |

**Allowed vs enabled:**

- `AttributeDefinition.IsVariantAxisAllowed` → *may* be variant axis
- `CategoryAttributeAssignment.IsVariantAxis` → *is* variant axis **in this category**

Same pattern for filter/compare/required: resolved at **assignment** level.

## Effective schema

`CatalogCategorySchemaResolver` walks ancestry root → leaf. Nearest binding for each `DefinitionId` wins. No physical copy to descendants.

Effective row exposes:

- `IsRequired`, `IsFilterable`, `IsVariantAxis`, `IsComparable` from winning binding
- `IsVariantAxisAllowed` from definition (capability)
- `InheritedFromCategoryId` — source category of winning binding

**Precedence:** child/local binding overrides parent for the same `DefinitionId`.

**Local vs inherited UI:**

- `InheritedFromCategoryId === currentCategoryId` → local assignment
- otherwise → inherited (show source category name)

**Child override:** create a local binding on the child with the same `DefinitionId` and different flags (`تنظیم برای این دسته` in Admin UI).

## Migration (T007-R1)

Migration `AddCategoryAttributeBindingBehavior` copies prior effective behavior into bindings:

```sql
is_required = COALESCE(is_required_override, definition.is_required)
is_filterable = definition.is_filterable
is_comparable = definition.is_comparable
is_variant_axis = definition.is_variant_axis
```

Then drops `is_required_override`. Existing effective behavior is preserved.

## Admin UX (ویژگی‌ها tab)

- VIEW: inherited + local sections, badges from **effective** flags
- EDIT: add/create with per-category flags on bind; **تنظیم رفتار** for local; **تنظیم برای این دسته** for inherited override
- Create definition: intrinsic type/capability only; assignment flags applied on bind to current category
- No raw GUIDs; Persian user-facing labels

## API

| Operation | Endpoint |
|-----------|----------|
| Effective schema | `GET .../categories/{id}/attribute-schema/effective` |
| Bind (with flags) | `POST .../bindings` |
| Update local assignment | `PATCH .../bindings/{definitionId}` |
| Unbind local | `DELETE .../bindings/{definitionId}` |
| Reorder local | `PUT .../bindings/order` |

## Handoffs

**T008 Facets:** consume category-specific `IsFilterable` from effective schema.

**Product Master:** consume category-specific `IsRequired`, `IsVariantAxis`.

**Comparison:** consume category-specific `IsComparable`.

Product != Offer. Catalog owns schema; Seller cannot define canonical definitions.

## Locks

- `AppCategoryTree` USER-APPROVED — do not redesign
- Public URL: `/{locale}/category/{localizedSlug}`
