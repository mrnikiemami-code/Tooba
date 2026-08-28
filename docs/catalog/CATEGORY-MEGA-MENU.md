# Category Mega Menu

> **Task:** TB-P07-T009 · **Phase:** P07 Advanced Catalog
> **Depends on:** [CATEGORY-ARCHITECTURE.md](./CATEGORY-ARCHITECTURE.md)

## Separation of concerns

| Concern | Source of truth |
|---------|-----------------|
| **Category taxonomy** | `CatalogCategory.ParentCategoryId` — catalog truth |
| **Mega Menu presentation** | `CatalogMegaMenuItem.ParentMegaMenuItemId` — navigation layout |

Changing menu placement **must not** mutate `Category.ParentCategoryId`. Changing the category tree **must not** auto-relayout the mega menu.

## Model

**Entity:** `CatalogMegaMenuItem` · table `catalog.mega_menu_items`

| Field | Role |
|-------|------|
| `CategoryId` | Internal destination (unique — one menu row per category) |
| `ParentMegaMenuItemId` | Presentation parent in menu tree |
| `SortOrder` | Order among siblings |
| `IsVisible` / `IsFeatured` | Menu visibility flags |
| `ImageMediaAssetId` / `IconMediaAssetId` | Optional presentation media |

**Translations:** `CatalogMegaMenuItemTranslation` — optional `TitleOverride`, `BadgeText`, `ShortLabel` per locale.

## Category-backed items

- Destination resolved at read time: `CategoryId` → current localized slug → `/{uiLocale}/category/{slug}`
- Title: override if present, else `CategoryTranslation.Name`
- Slug changes **do not** require editing menu rows
- No visible CategoryId suffix in URLs

## Visibility

Storefront menu excludes items when:

- `MegaMenuItem.IsVisible == false`
- Category not `Published` or not `IsVisible`
- Missing translation/slug for requested locale

## Admin UX (مگامنو tab)

Per-category workspace panel:

- VIEW: status, placement path, title, destination preview
- EDIT: enable/disable bind, presentation parent selector (human menu path), order, featured, progressive title override
- No manual URL entry; no raw CategoryId in UI

## API

### Admin

| Method | Path |
|--------|------|
| `GET` | `/v1/admin/catalog/categories/{id}/mega-menu?locale=` |
| `GET` | `/v1/admin/catalog/categories/{id}/mega-menu/placement-options?locale=` |
| `PUT` | `/v1/admin/catalog/categories/{id}/mega-menu?locale=` |
| `DELETE` | `/v1/admin/catalog/categories/{id}/mega-menu` |

### Storefront

| Method | Path |
|--------|------|
| `GET` | `/v1/storefront/mega-menu?locale=` |

Returns flat `StorefrontMegaMenuItem[]` with localized title and canonical destination. Header builds L1/L2/L3 from `ParentMegaMenuItemId`. Falls back to taxonomy projection when no menu items configured.

## Depth

Presentation hierarchy supports up to **3 levels** (L1/L2/L3) aligned with Shopeiva mega menu fidelity.

## Handoff

| Task | Scope |
|------|-------|
| **T009** (this) | Menu bind model, Admin tab, storefront read model |
| **T010** | Category PLP at canonical route — see [CATEGORY-PLP.md](./CATEGORY-PLP.md) |

## Related files

- Domain: `CatalogDomain.cs` — `CatalogMegaMenuItem`, `CatalogMegaMenuComposer`
- Infrastructure: `CatalogDirectory.cs`, `CatalogMegaMenuEndpoints.cs`
- Frontend: `catalog-mega-menu-api.ts`, `category-mega-menu-panel.tsx`, `storefront-header.tsx`
