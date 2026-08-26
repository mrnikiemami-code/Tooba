# 03 — Admin Data Grid capability map (TB-P05-T024)

Shared foundation: `src/frontend/design-system/data-grid/*`

| Grid screen | Columns | Filters | Sort | Resize | Reorder | Show/hide | Saved views | Bulk | Export | Responsive |
|---|---|---|---|---|---|---|---|---|---|---|
| Products `/admin/products` | Product identity/status fields | typed per-column where configured | yes | yes (foundation) | yes | yes | foundation + optional store | selection foundation | CSV visible/selected + server request notice | narrow mode / internal scroll |
| Orders `/admin/orders` | reference, buyer, payment, status, amount | yes | yes | yes | yes | yes | foundation | selection | same | same |
| Sellers `/admin/sellers` | seller, status, offers, orders | yes | yes | yes | yes | yes | foundation | selection | same | same |
| Customers `/admin/customers` | actor, contact, orders | yes | yes | yes | yes | yes | foundation | selection | same | same |
| Reviews `/admin/reviews` | review fields + moderation | yes | yes | yes | yes | yes | foundation | moderation actions (real API) | same | same |

No fake controls: export server button shows honest notice when Host export not available; moderation actions call real review endpoints only.
