# cta-picker-proof — TB-P10-T022-R13-R1-R1

Destination select `[data-testid=hero-slide-destination-type]` exposes typed options including:

- بدون پیوند (`none`)
- همه محصولات (`all-products`)
- محصول مشخص (`product`)
- دسته‌بندی مشخص (`category`)
- آدرس سفارشی (`custom-url`)

Evidence:

- `slider-destination-type.png`
- `slider-product-picker.png` — AdminResourceSelector labels, no raw GUID chrome
- `slider-category-picker.png` — same for categories

See `runtime-report.json` steps `cta-options`, `product-picker-no-raw-guid-chrome`.
