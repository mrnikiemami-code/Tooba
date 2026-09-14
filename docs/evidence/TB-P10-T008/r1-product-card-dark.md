# TB-P10-T008-R1 — Product Card dark

One `StorefrontProductCardView`. Geometry unchanged (`rounded-2xl`, `aspect-[4/5]`, hover translate).

| Surface | After | Note |
| --- | --- | --- |
| outer | `bg-surface border-border hover:shadow-xl hover:shadow-black/40` | semantic UI chrome |
| media well | `bg-background` + `data-storefront-media-well="true"` | intentional product-photo well; not UI chrome |
| actions | `bg-surface-elevated/90 text-muted` | no `bg-white/90` |
| title | `text-foreground group-hover:text-primary` | primary remaps to brand-emphasis in dark |
| price | `text-primary` | canonical emphasis token |
| OOS | `bg-secondary text-muted` | semantic |

No second card, no skins, no hard-coded dark hex.
