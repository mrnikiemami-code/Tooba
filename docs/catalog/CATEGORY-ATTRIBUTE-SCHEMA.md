# Category Attribute Schema

> **Task:** TB-P07-T007 · **Phase:** P07 Advanced Catalog
> **Baseline:** Category Workspace (General + Translations) + T004 attribute foundation

## Purpose

Each catalog category has an **effective attribute schema**: the set of product attributes that apply to products in that category, including attributes inherited from ancestor categories and attributes assigned locally.

This schema later drives:

- Product Master data entry and validation
- Variant axis selection (standardized product variants)
- PLP dynamic facets (T008)
- Product comparison eligibility

## Concepts

### AttributeDefinition (global, Admin-owned)

Canonical attribute metadata shared across categories:

| Field | Meaning |
|-------|---------|
| `Code` | Stable internal identifier (not shown as primary UI label) |
| `ValueKind` | Text, Number, Boolean, Enumeration, Instant |
| `LocalizedNames` | Translation-backed display names (fa/en/ar) |
| `IsFilterable` | Eligible for product filters (PLP) |
| `IsComparable` | Eligible for product comparison |
| `IsVariantAxisAllowed` | Can be used as a variant axis when creating variants |
| `IsRequired` | Default required flag (overridable per category binding) |

**Seller cannot create or mutate canonical definitions.**

Backend path: `CatalogAttributeDefinition` · API `/v1/admin/catalog/attribute-definitions`

### CategoryAttributeAssignment (local binding)

Links a definition to a specific category:

| Field | Meaning |
|-------|---------|
| `CategoryId` | Category that owns this binding |
| `DefinitionId` | Attribute definition |
| `DisplayOrder` | Sort order among **local** bindings |
| `IsRequiredOverride` | Optional override of definition-level required |

Backend path: `CatalogCategoryAttributeBinding` · API bind/unbind/reorder on category schema endpoints.

**Removing a binding from a child does not delete the global definition.**

### Effective schema (resolved, not copied)

`CatalogCategorySchemaResolver` walks ancestry root → leaf, merging bindings. Child binding on the same `DefinitionId` overrides parent. No physical copy into every descendant.

Each effective row includes:

- Merged flags (`IsRequired`, filter/compare/variant from definition)
- `InheritedFromCategoryId` — source category of the winning binding

**Local vs inherited in Admin UI:**

- `InheritedFromCategoryId === currentCategoryId` → **local**
- otherwise → **inherited** (show source category name from tree)

## Admin UX (Category Workspace → ویژگی‌ها)

### VIEW mode

- Section **ویژگی‌های ارث‌برده‌شده** with source category name
- Section **ویژگی‌های این دسته**
- Badges: الزامی، فیلتر، تنوع، مقایسه
- No raw GUIDs in primary UI

### EDIT mode

- **افزودن ویژگی** — searchable list of existing definitions; duplicate effective attributes prevented
- **ایجاد ویژگی جدید** — name, value type, multivalue, enum options, human flag labels; code auto-generated (advanced override optional)
- Reorder **local** assignments only (↑/↓)
- Remove **local** assignment only (inherited rows are read-only in child)

User-facing flag labels:

| Internal | Persian label |
|----------|----------------|
| Required | برای ثبت محصول الزامی است |
| Filterable | نمایش در فیلتر محصولات |
| Variant axis | برای ساخت تنوع محصول |
| Comparable | نمایش در مقایسه محصولات |

### Override policy (current)

- **Required:** assignment-level override via `IsRequiredOverride` on bind
- **Filter / compare / variant eligibility:** definition-level in current architecture (T004). Per-category override is documented as a future extension if needed.

## Product Master handoff (future)

```
Category selected
  → GET effective category attribute schema
  → Product Master editor renders typed fields
  → Required validation from effective schema
  → Variant-axis attributes → standardized Product Variants
```

Product != Offer. Attribute values live on Product; pricing/stock remain on Offer.

## Facet handoff (T008)

T007 establishes **filter eligibility** (`IsFilterable`) and typed attributes.

T008 owns facet presentation: checkbox, range, color swatch, order, collapsed state, count behavior.

## API summary

| Operation | Endpoint |
|-----------|----------|
| List definitions | `GET /v1/admin/catalog/attribute-definitions` |
| Create definition | `POST /v1/admin/catalog/attribute-definitions` |
| Update definition metadata | `PATCH /v1/admin/catalog/attribute-definitions/{id}` |
| Effective schema | `GET /v1/admin/catalog/categories/{id}/attribute-schema/effective` |
| Bind | `POST .../categories/{id}/attribute-schema/bindings` |
| Unbind | `DELETE .../bindings/{definitionId}` |
| Reorder local | `PUT .../bindings/order` |

## Frontend integration

- Workspace tab: `src/frontend/app/admin/category-attributes-panel.tsx`
- Wired in: `category-admin-screen.tsx` (`attributes` tab, VIEW/EDIT)
- API client: `catalog-attribute-api.ts`
- Legacy standalone screens remain for dev (`CategorySchemaScreen`, `AttributeDefinitionsScreen`)

## Deletion policy

- **Unbind** from category: removes local assignment only
- **Hard delete** of shared `AttributeDefinition`: not exposed in workspace; must be governed separately if used elsewhere

## Locks

- `AppCategoryTree` — USER-APPROVED, do not redesign
- No raw `AgGridReact`; use `AppDataGrid` only if a grid is needed
- Public category URL: `/{locale}/category/{localizedSlug}` (no CategoryId suffix)
