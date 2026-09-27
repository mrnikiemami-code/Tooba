# Hierarchy / slug / routing — W7

## Hierarchy (Domain authority preserved)

- `CatalogCategoryTreeRules.MaxCategoryDepth = 3`
- Self-parent forbidden → `catalog.category.parent.self`
- Descendant-as-parent forbidden → `catalog.category.parent.descendant`
- Max depth on create/move → `catalog.category.depth.max`
- Product-assignable level = 3 unchanged (not part of these 10 routes)
- Sibling reorder exact-set semantics → `catalog.category.reorder.invalid`
- CategoryDirectory pre-checks via public Domain APIs (no message parsing)

## Slug

- Locale/slug normalization via `CatalogCategorySlugNormalizer`
- Uniqueness via typed `EnsureSlugAvailableAsync` → `catalog.category.slug.duplicate` (409)
- Empty-after-normalize → `catalog.category.slug.invalid` (classified by call site)

## Route history / resolve

- Current slug wins
- Historical slug → redirect (`IsRedirect: true`) + canonical path `/{locale}/category/{slug}`
- `forStorefront` requires Published + IsVisible
- Missing / ineligible → `catalog.category.route.missing`
