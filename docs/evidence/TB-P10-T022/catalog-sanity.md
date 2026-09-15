# Catalog sanity — TB-P10-T022

## Hidden accidental alias

| Hidden key | Canonical | Reason |
|---|---|---|
| `banner.mosaic-2x2` | `banner.four-grid` | Identical 2×2 grid layout + identical renderer branch; alias-only clutter. Remapped via `canonicalizeVariantKey`; Admin selectable catalog excludes it (`implemented=false`). TileCeramic template now uses `banner.four-grid`. |

## Near-neighbors kept (meaningful difference)

| Pair | Why both useful |
|---|---|
| `story.circle` vs `story.image-circles` | Border/padding treatment — classic story ring vs image-filled circle |
| `product.grid` vs `product.large-cards` | Density vs large visual product cards |
| `product.compact-rows` vs `product.minimal-list` | Compact product row chrome vs title/price-only list |
| `product.card-carousel` / `featured-plus-rail` / `tabbed` | Plain rail vs featured+rail vs tabbed discovery |
| `reviews.card-carousel` vs `reviews.compact-quotes` | Full review cards vs quiet quote grid |
| `banner.four-grid` vs `banner.eight-compact` | 4-up vs dense 8-up (not aliases) |
| `banner.one-large-two-small` vs `banner.one-large-four-small` | Different mosaic slot counts |

## Rule

No new SectionTypes. Prefer Variant differentiation. Alias-only Variants remain forbidden (LOCK-SF-243 / LOCK-SF-250).
