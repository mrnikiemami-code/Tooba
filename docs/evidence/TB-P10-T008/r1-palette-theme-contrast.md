# TB-P10-T008-R1 — Palette × theme contrast

All 7 palettes × Light and Dark:

| Check | Light | Dark |
| --- | --- | --- |
| body / background | `24 24 27` on `250 250 249` AA | `250 250 250` on `12 12 14` AA |
| muted | `113 113 122` on paper | `180 180 188` on dark paper AA |
| brand-emphasis / links / `text-primary` | primary vs paper ≥ 4.5 | `primaryOnDarkRgb` vs `12 12 14` ≥ 4.5 |
| CTA | primary / on-primary ≥ 4.5 | same (fill not remapped) |
| focus | `--color-focus` = primary | outline `--color-primary-on-dark` |
| inputs | gray remaps to surface/border/foreground | same |
| danger/success/warning | independent of PaletteKey | dark status tokens unchanged |
| product-card title/price/actions | semantic | semantic + emphasis |
| account/menu | gray remaps | remapped |

No supported combination retains a known contrast failure.
