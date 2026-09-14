# TB-P10-T008-R1 — Defect reproduction

Dark paper: `12 12 14`. CTA (`primary` vs `on-primary` white) already AA for all 7 palettes.

## A. wine-burgundy brand text on dark

Meaningful `text-primary` / link / price / title-hover / focus use `--color-primary` (`159 18 57`).

Measured contrast vs dark paper:

| Palette | primaryRgb | Contrast vs `12 12 14` | Meaningful text |
| --- | --- | --- | --- |
| wine-burgundy | `159 18 57` | **2.44** | FAIL (< 3:1) |
| slate-navy | `30 58 95` | **1.70** | FAIL |
| violet-royal | `124 58 237` | 3.43 | PASS 3:1 / FAIL 4.5:1 |
| teal-lagoon | `15 118 110` | 3.57 | PASS 3:1 / FAIL 4.5:1 |
| tooba-blue | `37 99 235` | 3.78 | PASS 3:1 / FAIL 4.5:1 |
| amber-gold | `180 83 9` | 3.89 | PASS 3:1 / FAIL 4.5:1 |
| forest-green | `21 128 61` | 3.90 | PASS 3:1 / FAIL 4.5:1 |

Failing selectors (canonical, not wine-only): `.text-primary`, `.group-hover\:text-primary`, `html.dark :focus-visible` when `--color-focus` equals primary, product-card price `text-primary`.

Decorative: `bg-primary` CTA fill + `text-white` on-primary (not a text-on-paper failure).

## B. Product Card light chrome

`StorefrontProductCardView` (one component):

| Surface | Pre-repair class | Classification |
| --- | --- | --- |
| outer | `bg-white border-gray-100 hover:shadow-xl` | UI chrome; `bg-white` remapped, shadow stayed light |
| media well | `aspect-[4/5] bg-gray-50` | media/content well |
| action buttons | `bg-white/90` | **defect** — `/90` missed `html.dark .bg-white` |
| title | `text-gray-800` | remapped via gray text rules |
| price | `text-primary` | brand-emphasis contrast defect on dark palettes |
| OOS CTA | `bg-gray-100 text-gray-400` | remapped |
