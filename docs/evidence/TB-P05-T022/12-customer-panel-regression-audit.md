# 12 — Customer Panel Regression Audit (before)

| Area | Shopeiva | Tooba before | Severity | Plan |
| --- | --- | --- | --- | --- |
| Shell | sticky 65px header + full-height sidebar | promo bar + storefront header + card sidebar | Material | Restore layout.jsx pattern |
| Nav order | 10 items incl. tickets/settings | missing tickets/settings; tickets gray | Material | Full order + honest unavailable |
| Active state | solid accent + ChevronLeft | blue-50 soft pill | Material | Solid active + chevron |
| Mobile | 280px drawer overlay | Menu button non-functional | Material | Working drawer |
| Dashboard | welcome + stats + quick actions + recent | welcome + metrics + recent | Minor | Add quick actions; no fake charts |
| Orders | chips + expandable cards | chips + link rows | Minor | Keep live; density OK |
| Wishlist/Addresses/Profile | card grids / form | already live | Minor | Shell only |
| Optional surfaces | mock data in source | capability shells | Guard | Keep honestly unavailable |

Note: Shopeiva runtime captures may show login gate without auth; source inventory remains authoritative for structure.
