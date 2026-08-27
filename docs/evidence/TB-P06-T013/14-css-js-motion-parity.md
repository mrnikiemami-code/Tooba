# 14 — CSS / JS / motion parity (TB-P06-T013)

Task: `TB-P06-T013`

## Listing (`blogs-ui.tsx`)

| Concern | Tooba approach |
|---|---|
| Slider | Swiper + Autoplay + Pagination + Navigation (Shopeiva-aligned modules) |
| Cards | Rounded cover, hover scale/shadow translate (CSS transitions) |
| Accent | `#2563EB` Tooba blue (not Shopeiva demo palette drift) |
| RTL | `dir="rtl"` on Swiper |

## Detail (`blog-detail-ui.tsx`)

| Concern | Tooba approach |
|---|---|
| Layout | Centered article column, cover aspect 16/9 |
| Typography | Title/excerpt/body hierarchy; body `whitespace-pre-wrap` |
| Motion | Minimal (load → render); no fake like animation |

## Home rail

| Concern | Tooba approach |
|---|---|
| Swiper | Autoplay + Pagination on article carousel |
| Hover | Cover scale + overlay CTA «مطالعه مقاله» |
| Fake like motion | Removed |

## Audit flag

`CSS_JS_MOTION_PARITY = AUDITED` (Content surfaces; Home/PDP critical visual ACCEPT remains separate).
