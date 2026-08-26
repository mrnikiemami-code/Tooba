# 01 — Shopeiva Home Inventory

Task: `TB-P05-T018`

Authority: `SarvNewVerRequirment/reference/shopeiva/src/app/page.jsx` + `homeSections.jsx` (Home v1 `/`, not index2/index3).

| Order | Section | Source | Desktop | Mobile | Data | Tooba before | Deviation | Required action |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Header | `components/common/Header` | sticky z-50 | hamburger | menu JSON | Storefront shell | OK (T016) | preserve |
| 1 | Hero Slider | `home/slider` | h 290–350, rounded-3xl Swiper | h 190–230 | inline slides | static + overlay text | FAIL | restore slider geometry, no marketing overlay |
| 2 | Stories | `home/stories` | circles 100px Swiper | 80px | inline stories | small circles | FAIL density | horizontal circle rail from live categories |
| 3 | Categories | `home/categories` | horizontal cards 180px, ≤20 | 160px | categories.json slice 20 | **giant all-category grid** | FAIL | horizontal rail HomeCategories only |
| 4 | FlashSales | `home/flashSales` | rail 210px + countdown chrome | 170px | discount products | CSS grid special offers | FAIL | horizontal rail; no fake countdown |
| 5 | BestSellers | `home/bestSellers` | 4-col × 3 rows | 1–2 col | cats×products | missing | FAIL | add columns from live Catalog |
| 6 | MostViewed | `home/mostViewed` | grouped rail | stack | views sort | missing | FAIL | rail ordered by live reviewCount (no ViewCount) |
| 7 | MiddleBanners | `home/middleBanners` | 2×2 aspect 21/7 | stack 21/9 | inline 4 | 2 plain banners | FAIL | 4 linked hover banners |
| 8 | Brands | `home/brands` | square tiles → /brands | 160px | brands.json | text chips | FAIL | square tiles → `/brand/[slug]` |
| 9 | NewProducts | `home/newProducts` | rail 220px | 180px | isNew | grid | FAIL | horizontal rail |
| 10 | Testimonials | `home/testimonials` | 3 cards | 1 | inline quotes | missing | Content module absent | omit until live Content (documented) |
| 11 | Blog | `home/blog` | cards 320px | 260px | blogPosts.json | missing | Content module absent | omit until live Content |
| 12 | Footer | DynamicFooter | trust 4-col | 2-col | inline | Storefront footer | OK | preserve |

Runtime used for references: `http://127.0.0.1:3017/`.
