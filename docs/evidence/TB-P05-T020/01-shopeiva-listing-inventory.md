# 01 — Shopeiva Listing Inventory

| Source component | Source path | Visual structure | Tooba equivalent | Deviation (before) | Required action |
| --- | --- | --- | --- | --- | --- |
| Category client | `reference/shopeiva/.../categoryDetailClient.jsx` | max-w 1800, breadcrumb, 1/3 sidebar + 3/3 main | `storefront-listing.tsx` + `/products` | Flat filter list, no category header, no mobile drawer | Restore Shopeiva PLP shell |
| Category sidebar | `categoryDetailSidebar.jsx` | sticky top-24, subcategories + «همه» | category/seller/stock filters | Looked like admin filter card | Sticky subcategory-style filters |
| Category header | `categoryDetailHeader.jsx` | banner tile + title + count badge | missing | Title only in toolbar | Category/search header band |
| Best products toolbar | `categoryDetailBestProducts.jsx` | count + sort select + 4-col grid + numbered pagination | select + prev/next | Density/grid/pagination drift | Match toolbar/grid/pagination |
| Search client | `search/SearchClient.jsx` | rich filters + mobile drawer + sort | `/products?q=` same listing | No mobile drawer; weaker search chrome | Shared PLP with drawer |
| Product Card | `ui/ProductCard/ProductCard.jsx` | aspect 4/5, wishlist, rating if present | `storefront-product-card.tsx` | Mostly MATCH (Tooba blue accent) | Keep; no taste redesign |
| Brand listing | brand detail + filters | brand landing + product grid | `/brand/[slug]` merchandising | 5-col grid | Align to 4-col Shopeiva density |
| Seller listing | vendor public surfaces | seller store patterns | `/seller-profile/[publicId]` existing | Out of scope unless already public | Leave public seller route intact |

Accent color: Shopeiva uses `#E53935`; Tooba keeps established `#2563EB` brand (MINOR TECHNICAL DEVIATION).
