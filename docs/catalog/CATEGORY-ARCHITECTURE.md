# Category Architecture (TB-P07-T004)

Foundation for Admin Category Tree / Workspace and storefront category routes.
UI (Ant Tree / AppCategoryTree) is **T005** — this document is the locked backend contract.

## Aggregate

`CatalogCategory` (schema `catalog`) is language-independent:

| Field | Notes |
| --- | --- |
| CategoryId | Immutable identity (UuidV7) |
| ParentCategoryId? | Adjacency taxonomy only |
| Status | Draft / Published / Archived |
| SortOrder | Deterministic among siblings |
| IsVisible | Storefront visibility (separate from Status) |
| ImageMediaAssetId? / IconMediaAssetId? | Opaque Media refs |
| CreatedAt / UpdatedAt | Server clocks |

**No** `NameFa` / `NameEn` or other locale columns on the aggregate.

Methods: `Create`, `Move`/`Reparent`, `SetCoreFields`, `SetSortOrder`, `Publish`, `Archive`.

## Translations

`CatalogCategoryTranslation`: one row per `(CategoryId, Locale)`.

- Required: Name, Slug (for routable locales)
- Optional: ShortDescription, Description, SeoTitle, SeoDescription, MetaKeywords
- Locale normalized (trim); Slug normalized (lowercase, kebab, Unicode letters kept)

Backward compat: `LocalizedText` name rows remain synced on create/upsert for `GetCategoryNamesAsync` fallback. Prefer Translation.Name.

## Slug uniqueness

Unique index on `(Locale, Slug)` — route identity is **locale-specific**, not global.

Same slug in different locales is allowed.

## Slug history

`CatalogCategorySlugHistory`: `(HistoryId, CategoryId, Locale, OldSlug, ChangedAt)`.

On slug change, previous slug is appended (skipped if that old slug already equals another category’s **current** slug — current wins).

Index on `(Locale, OldSlug)` for resolve.

## Route resolution

`ResolveCategoryRouteAsync(locale, slug, forStorefront)`:

1. Match current translation → `IsRedirect=false`
2. Else match history → load current slug → `IsRedirect=true`
3. Else null

Canonical path: `/{locale}/category/{currentSlug}`

Storefront eligibility when `forStorefront=true`: `Status == Published` **and** `IsVisible`.
Admin resolve (`forStorefront=false`) allows any status.

**Hierarchy does not define URL identity.** Moving a category does not rewrite descendant slugs.

## Tree / move rules

`CatalogCategoryTreeRules`:

- Self-parent forbidden
- Descendant-as-parent forbidden (`IsDescendant` / `ValidateMove` / `ValidateNoCycle`)
- Sibling reorder replaces SortOrder 0..n-1 for exact sibling set

`GetCategoryTreeAsync(locale, search?)` loads categories + translations in bulk, builds nodes in memory (no N+1). Search keeps ancestor chain for Ant Tree coherence.

## Multilingual rules

| Global | Localized |
| --- | --- |
| Parent, Status, SortOrder, Image/Icon, IsVisible | Name, Slug, ShortDescription, Description, SEO fields |

T006 UI will switch locale tabs over the same CategoryId.

## Mega Menu separation

Mega Menu is presentation only. Future link: `MegaMenuItem → CategoryId`.
Category must not own menu layout/column positioning. Marker type: `MegaMenuItemCategoryBindingMarker`.

## PLP / facet future flow

```text
/{locale}/category/{slug}
  → ResolveCategoryRouteAsync
  → CategoryId
  → Catalog listing (subtree products, subcategories, facets, sort, pagination, SEO)
```

Extension markers (types only): `CategoryFacetConfigurationMarker`, `CategoryAttributeAssignmentMarker`, `AttributeInheritanceMarker`, `VariantAxisAssignmentMarker`.
Attribute schema/inheritance already exists via `CatalogCategoryAttributeBinding`.

## Admin UI target (T005+)

Locked visual contract:

- `docs/evidence/TB-P07-T004/visual-contract-admin-categories.png`
- `docs/reference/ui-mockups/admin-categories-management-mockup-2026-08-28.png`

Reuse canonical **AppDataGrid** for grids. Tree = `AppCategoryTree` over Ant Design Tree. Do not invent a second grid.

## APIs

Admin (`AdminPanelAccess.RequireAuthorizedAsync`):

- `GET /v1/admin/catalog/categories/tree?locale=`
- `GET /v1/admin/catalog/categories/{id}`
- `POST /v1/admin/catalog/categories`
- `PATCH /v1/admin/catalog/categories/{id}`
- `PUT /v1/admin/catalog/categories/{id}/translations/{locale}`
- `POST /v1/admin/catalog/categories/{id}/move`
- `POST /v1/admin/catalog/categories/reorder`
- `POST /v1/admin/catalog/categories/{id}/publish`
- `POST /v1/admin/catalog/categories/{id}/archive`

Storefront/internal:

- `GET /v1/storefront/category-routes/resolve?locale=&slug=&forStorefront=`

## Module ownership

All Category tables live in Catalog schema. No cross-module SQL joins. Media via opaque ids / module contracts only.

## Migration

`20260828172727_AddCategoryFoundation` — category columns + translations + slug history + optional LocalizedText→translation backfill.
