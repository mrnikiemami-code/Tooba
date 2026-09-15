# Template acceptance — TB-P10-T022

All 10 industry templates verified in registry seeds (`INDUSTRY_TEMPLATE_SEEDS`).

| Template | nameFa | Distinct composition signal |
|---|---|---|
| fashion | پوشاک | editorial hero + story + editorial tiles + large cards |
| auto-parts | لوازم یدکی خودرو | contained hero + compact tiles + minimal list + ticker |
| building-supplies | لوازم ساختمانی | category-first + grid + three banners + featured brand |
| tools-hardware | ابزار و یراق | compact tiles + compact rows + multi-column ranked + eight banners |
| tile-ceramic | کاشی و سرامیک | side-promos + editorial tiles + mosaic banners + large cards |
| interior-decor | دکوراسیون داخلی | split hero + featured-plus-rail + featured article |
| home-appliance | لوازم خانگی | featured brand + tabbed products + ranked grid |
| shoes | کفش | full-width hero + rounded story cards + product carousel |
| plants | گل و گیاه | editorial + icon shortcuts + featured-plus-rail (calmer) |
| beauty | آرایشی بهداشتی | image-circles story + brand + tabbed + promo |

## Picker UX

- Miniature composition strip differs by section-kind sequence.
- FA label + short description + section count + plain summary.
- Creates normal editable Draft (no hidden post-create behavior).
- TileCeramic second banner remapped from hidden alias to `banner.four-grid`.

Guard: `industry-templates.guard.test.ts` + composition-engine distinctness.
