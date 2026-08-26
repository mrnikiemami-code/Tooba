# 11 — Home Visual Regression Audit (before)

Task: `TB-P05-T018`

Evidence: `09-tooba-home-before-full.png`, `10-tooba-home-before-categories.png` vs Shopeiva `02`–`08`.

| Divergence | Severity |
| --- | --- |
| Categories dumped as multi-column grid of **all** Catalog categories | material |
| Hero is single static image with title overlay (Shopeiva: multi-slide carousel, no title overlay) | material |
| Product sections use dense CSS grids instead of horizontal rails (~170–220px slides) | material |
| Best Sellers / Most Viewed / Testimonials / Blog missing | material |
| Brands rendered as text chips without `/brand/[slug]` tiles | material |
| Mid banners: 2 plain images vs 4 hover banners | material |
| Section order and `py-8 md:py-10` rhythm lost (`space-y-6` generic stack) | material |
| Page hierarchy no longer recognizable as Shopeiva Home at a glance | material |
