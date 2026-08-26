# 18 — Cross-surface regression proof (TB-P05-T024)

Admin shell changes are isolated under `src/frontend/app/admin/**`.

| Surface | Check | Result |
|---|---|---|
| Home / PDP / Listing | `npm run test:critical-storefront` | required in validation |
| Customer | `npm run test:customer` | required |
| Seller | `npm run test:seller` | required |
| Cart/Checkout | untouched paths | no admin edits |
| Admin suites | `npm run test:admin` (+ product-workspace) | required |

No public/customer/seller chrome shared with Admin shell.
