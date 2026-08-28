# Category Facets (PLP Filter Configuration)

> **Task:** TB-P07-T008 · **Phase:** P07 Advanced Catalog
> **Depends on:** [CATEGORY-ATTRIBUTE-SCHEMA.md](./CATEGORY-ATTRIBUTE-SCHEMA.md) (T007 effective `IsFilterable`)

## Purpose

Configure **how** filterable category attributes appear on the Storefront category product listing page (PLP). T008 owns **configuration + contracts** only — not the full storefront PLP UI or live filtering.

Target runtime flow (future):

```text
Mega Menu → category route → CategoryId
  → effective attribute schema (T007)
  → effective facet configuration (T008)
  → PLP filter UI (T009+)
  → filtered product listing
```

## Eligibility

Only attributes in the **effective category schema** with **`IsFilterable == true`** (from nearest `CatalogCategoryAttributeBinding`) may receive facet configuration.

- Non-filterable attributes are rejected at API level.
- Inherited attributes that are effectively filterable are eligible.
- Eligibility is **not** read from definition-global `IsFilterable` after T007-R1.

## Configuration model

**Entity:** `CatalogCategoryFacetConfiguration`
**Table:** `catalog_category_facet_configurations`

| Field | Role |
|-------|------|
| `CategoryId` | Owning category for this configuration row |
| `AttributeDefinitionId` | Canonical attribute |
| `DisplayType` | Presentation type (see below) |
| `SortOrder` | Order among **local** configurations on this category |
| `IsVisible` | Show or hide filter on PLP |
| `IsSearchable` | Search within options (checklist/select only) |
| `IsCollapsedByDefault` | UI starts collapsed |
| `ShowCounts` | Show product counts next to options (future PLP) |

**Uniqueness:** `(CategoryId, AttributeDefinitionId)` — one configuration row per attribute per category.

## Display types

| Type | Persian label (Admin) | Allowed value kinds |
|------|----------------------|---------------------|
| `CheckboxList` | چندانتخابی | Enumeration, Text |
| `SearchableSelect` | انتخاب با جستجو | Enumeration, Text |
| `Range` | بازه | Number |
| `BooleanToggle` | روشن/خاموش | Boolean |
| `ColorSwatch` | رنگ | Reserved — **rejected** until color option metadata exists |

Validation is enforced in `CatalogCategoryFacetRules.ValidateDisplayType`. Invalid combinations return structured API errors.

**Default suggestion** (`suggestFacetDisplayType` / backend equivalent):

- Boolean → `BooleanToggle`
- Number → `Range`
- Text → `SearchableSelect`
- Enumeration → `CheckboxList`

## Inheritance and precedence

`CatalogCategoryFacetResolver` walks category ancestry (root → leaf). **Nearest configuration wins** for presentation fields.

- Parent configures facet → child **inherits** effective presentation without copying rows.
- Child may **override** presentation for the same `AttributeDefinitionId` (local row on child).
- **Remove local override** (`DELETE` local config) → falls back to parent configuration.
- **Hide** (`IsVisible = false`) is an explicit override on the current category.
- Sibling categories remain isolated.

Effective merge produces **at most one facet per attribute** with deterministic ordering:

1. Sort by `SortOrder` from winning configuration source.
2. Tie-break by localized attribute name.

## Remove vs hide

| Action | Meaning |
|--------|---------|
| **حذف تنظیم این دسته** | Delete local override row; inherited parent config applies again |
| **مخفی** (`IsVisible = false`) | Keep override but hide filter on this category’s PLP |

Neither action deletes `AttributeDefinition` nor changes T007 `IsFilterable` assignment.

## Admin UX (فیلترها tab)

Tab label: **فیلترها** · Heading: **فیلترهای صفحه محصولات**

- **VIEW:** inherited + local sections, badges (فعال / مخفی / ارث‌برده‌شده / قابل جستجو), display type in Persian.
- **EDIT:** add filter (eligible only), configure display type, visibility, searchable (when relevant), collapsed default, show counts, reorder local facets, override inherited, remove local override.
- No raw IDs in UI. VIEW/EDIT follows Category Workspace form mode.

Component: `CategoryFacetsPanel` in Category Workspace (`category-admin-screen.tsx`).

## API contracts

### Admin

| Method | Path | Purpose |
|--------|------|---------|
| `GET` | `/v1/admin/catalog/categories/{id}/facets/effective?locale=` | Effective facets for Admin UI |
| `GET` | `/v1/admin/catalog/categories/{id}/facets/local` | Local override rows only |
| `PUT` | `/v1/admin/catalog/categories/{id}/facets/{definitionId}` | Upsert local configuration |
| `DELETE` | `/v1/admin/catalog/categories/{id}/facets/{definitionId}` | Remove local override |
| `PUT` | `/v1/admin/catalog/categories/{id}/facets/order` | Reorder local facets |

DTO: `EffectiveCategoryFacet` — localized name, value kind, display type, flags, `SourceCategoryId`, `IsInherited`.

### Storefront (read-only schema)

| Method | Path | Purpose |
|--------|------|---------|
| `GET` | `/v1/storefront/categories/{categoryId}/facets?locale=` | Localized facet **schema** for future PLP |

Returns filter UI schema only — **not** selected filter values or product counts.

## Future typed filter values (PLP query)

Documented contract for listing queries (not fully implemented in T008):

| Value kind | Request shape (conceptual) |
|------------|-------------------------|
| Enumeration / Text options | `attributeId` + `selectedOptionIds[]` |
| Number | `attributeId` + `min` / `max` |
| Boolean | `attributeId` + `true` / `false` |
| Color (future) | `attributeId` + `optionIds[]` |

No raw SQL predicates, AG Grid filter models, or arbitrary JSON expression language.

**Range bounds:** PLP may derive min/max from product data at runtime; T008 does not compute aggregates.

## SEO rule

Facet configuration does **not** create indexable URLs. Category canonical page remains canonical; filtered PLP query states are **non-canonical by default** until an explicit SEO strategy (later task) defines otherwise.

## Authorization

SpiceDB-centered: view vs edit facet configuration. Backend enforces; Admin UI hides edit actions for view-only users.

## Color swatch gap

`ColorSwatch` is in the enum for extensibility but rejected until option-level color metadata exists. Admin falls back to checklist/select; do not infer hex from localized names.

## Migration

`20260828210000_AddCategoryFacetConfigurations` — table + unique index on `(CategoryId, AttributeDefinitionId)`.

## Handoff

| Task | Scope |
|------|-------|
| **T008** (this) | Configuration model, resolver, Admin tab, storefront facet schema contract |
| **T009+** | Storefront PLP filter UI, apply typed filter values to listing query |
| **T010+** | Runtime bounds, option counts, color metadata integration |

## Related files

- Domain: `CatalogDomain.cs` — entity, rules, resolver
- Application: `CatalogContracts.cs` — DTOs, `ICatalogDirectory` methods
- Infrastructure: `CatalogDirectory.cs`, `CatalogFacetEndpoints.cs`
- Frontend: `catalog-facet-api.ts`, `category-facets-panel.tsx`
