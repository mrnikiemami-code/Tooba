# 12 — UI gap closure (TB-P06-T013)

Task: `TB-P06-T013`

## Gaps closed in this task

| Gap (before) | Closure |
|---|---|
| No public blog list route | Added `/blogs` + `blogs-ui.tsx` bound to Host |
| No public article detail route | Added `/blogs/[slug]` + `blog-detail-ui.tsx` |
| No admin content navigation / screen | `/admin/content` + nav `live: true` + DataGrid CMS |
| Home articles without destinations | Cards + «مشاهده همه» point at `/blogs` and `/blogs/{slug}` |
| Fake home article heart/like | Removed from home article rail (wishlist/product hearts elsewhere unchanged) |

## Not claimed closed

- Seller/customer stub panels listed in readiness matrix
- Shopeiva likes/views engagement APIs
- Full rich-text CMS / media picker beyond cover MediaAssetId reference
