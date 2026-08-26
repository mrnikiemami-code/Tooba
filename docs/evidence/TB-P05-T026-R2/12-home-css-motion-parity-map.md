# Home CSS / Motion Parity Map — TB-P05-T026-R2

Accent mapping: Shopeiva `#E53935` → Tooba `#2563EB` (**MINOR TECHNICAL DEVIATION**, authorized).

| Section | Metric | Grade | Notes |
|---------|--------|-------|-------|
| Best Sellers | box-shadow / hover shadow | MATCH | Column `hover:shadow-xl`, row `hover:shadow-md` ported |
| Best Sellers | hover transform / scale | MATCH | Column `-translate-y-1`, row `scale-[1.02]` |
| Best Sellers | hover action reveal | MATCH | Wishlist/cart/eye opacity + translate restored |
| Best Sellers | badges / discount | MATCH | Live promo-derived % only; no fake discount |
| Best Sellers | image treatment | MATCH | `storefrontMediaUrl(mediaAssetId)` replaces placeholder |
| Best Sellers | rating | MATCH | Shown only when `reviewCount > 0` |
| Brands | image / gradient / overlay | MATCH | Logo media + `from-black/60` gradient |
| Brands | hover eye overlay | MATCH | `bg-black/40 backdrop-blur-sm` + Eye icon |
| Brands | swiper / freeMode | MATCH | Swiper FreeMode RTL |
| Newest Products | autoplay carousel | MATCH | Swiper Autoplay delay 4000, pauseOnMouseEnter |
| Newest Products | freeMode | MATCH | `freeMode sticky + momentumRatio 0.5` |
| Newest Products | card hover shadow/lift | MATCH | ProductCard `hover:shadow-xl hover:-translate-y-1` |
| Newest Products | hover actions | MATCH | wishlist/share/view on card |
| Customer Reviews | section presence | MATCH | Restored with live `featuredReviews` |
| Customer Reviews | carousel / pagination | MATCH | Swiper Autoplay + dynamicBullets |
| Customer Reviews | verified badge | MATCH | Only when `verifiedPurchase === true` |
| Customer Reviews | likes/helpful counts | MINOR TECHNICAL DEVIATION | UI structure kept; counts omitted (no backend) |
| Latest Articles | carousel / hover lift | MATCH | Swiper + `hover:-translate-y-2` |
| Latest Articles | cover / tags / featured | MATCH | Live Content module fields |
| Latest Articles | views/comments counts | MINOR TECHNICAL DEVIATION | Footer simplified; no fabricated metrics |
| Global | accent color | MINOR TECHNICAL DEVIATION | `#2563EB` per task |
| Hero/Stories/Categories/Flash/MostViewed/MiddleBanners | unchanged | MATCH | Part J preserved |

**Overall:** No material UNRESOLVED differences in user-identified sections pending user visual review.
