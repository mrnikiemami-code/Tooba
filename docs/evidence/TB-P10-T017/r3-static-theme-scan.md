# Static theme scan — TB-P10-T017-R3

Scanned Storefront + customer-facing TSX (app/storefront, app/login, app/cart, app/shipping, app/checkout, app/payment, app/order, app/blogs, app/customer-panel, shared support/wallet used by customer).

| Metric | Count |
| --- | --- |
| Files scanned | Storefront/customer surfaces listed above |
| Candidate `bg-white` / `bg-gray-*` / hex backgrounds | many (cards, media, status, decorative) |
| Real wrapper violations | repaired (shipping full-width white, customer header/sidebar white, PDP giant white shells, login card, blogs without shell, wallet-checkout `#F5F5F5`, ticket form white island) |
| Intentional fixed surfaces | product cards, inputs, status (`bg-red-50`), media wells, decorative hero circles, primary CTAs |
| Repaired | all classified wrapper violations |
| Unresolved real wrapper violations | 0 |

Classification used: Card/Input/Header/Footer/Media/Status vs Page/Section/Alternate/Accent. No blanket `bg-white` deletion.
