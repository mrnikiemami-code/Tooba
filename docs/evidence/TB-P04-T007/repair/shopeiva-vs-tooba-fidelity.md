# TB-P04-T007 Repair — Shopeiva vs Tooba fidelity

Rule: Tooba should look like Shopeiva with Tooba live data, not a simplified reinterpretation.

| Surface | Shopeiva runtime (T006) | Tooba live | Preserved | Necessary deviation | Reason |
| --- | --- | --- | --- | --- | --- |
| Home | T006 atlas `docs/evidence/TB-P04-T006/visual-atlas/storefront/A01-home-v1-1440x900-rtl.png` | `docs/evidence/TB-P04-T007/repair/01-home-1440x900-rtl.png` | Promo bar, logo+search, nav, slider, stories, category tiles, product rails, mid banners, footer | Product rails bind Host cards; stories/banners remain purchased presentational assets | Business truth is Catalog/Offer; merchandising chrome is template |
| Listing | Search/PLP grid + filter chrome | `02-listing-1440x900-rtl.png` | Header, sidebar filter chrome, 4-col card grid | Sort control is visual-only; facets are live categories | No search index yet; Host listing filter only |
| PDP | 3-column gallery / info / buy box + tabs | `03-pdp-1440x900-rtl.png` | Gallery, thumbs, identity, rating seam, qty, CTA, tabs, other-seller seam | Rating/reviews/compare/chart are chrome; amounts from primary Offer | Review module not in this slice |
| Header | Promo bar, logo, search, mega menu, cart | `08-header-megamenu-1440x900.png` | Density, mega 3-pane, cart drawer chrome | Mega tree from purchased `menuCategories.json`; live Tooba categories pinned as chips | Catalog tree is flat in current seed |
| Product card | Image, badges, title, price, stars, CTA | `10-blue-theme-product-cards.png` | Aspect 4/5, hover lift, badge, stars, CTA | Price from Offer exclusive amount; blue accent | Product.Price forbidden |
| Footer | Trust row, newsletter, 4 columns, badges | `09-footer-1440x900.png` | Trust/newsletter/column layout | Newsletter is client-only; no backend list | Cart/newsletter APIs out of scope |

Accepted visual claim: Tooba storefront is Shopeiva chrome with Tooba data, not a Shopeiva-inspired mini-app.
