# 11 — Final runtime (TB-P06-T011-R2)

Post-validation runtimes kept up for preview.

| Runtime | PID | URL | Status |
| --- | --- | --- | --- |
| Tooba Backend | restart after tests | 26008 | 5088 | http://127.0.0.1:5088 | `/health/live` 200, `/health/ready` 200 |
| Tooba Frontend | 28928 | 3000 | http://127.0.0.1:3000 | `/` 200 |
| Shopeiva | 8420 | 3001 | http://127.0.0.1:3001 | `/user-panel/orders` 200 |

## Route checks

| Route | Status |
| --- | --- |
| Home | http://127.0.0.1:3000/ → 200 |
| PDP | http://127.0.0.1:3000/products/demo-game-2 → 200 |
| Customer Returns entry | http://127.0.0.1:3000/customer-panel/orders → 200 |
| Seller Returns | http://127.0.0.1:3000/vendor-panel/returns → 200 |
| Admin Returns | http://127.0.0.1:3000/admin/returns → 200 |

## USER-PREVIEW URLs

**Tooba**

- Home: http://127.0.0.1:3000/
- PDP: http://127.0.0.1:3000/products/demo-game-2
- Customer Returns: http://127.0.0.1:3000/customer-panel/orders
- Customer order (live): http://127.0.0.1:3000/customer-panel/orders/01a03ef2-4d7c-7000-a47a-deee181523cd
- Seller Returns: http://127.0.0.1:3000/vendor-panel/returns?sellerPartyId=01a030d1-40cb-7000-8abe-6d31739956c5
- Admin Returns: http://127.0.0.1:3000/admin/returns

**Original Shopeiva**

- Customer orders: http://127.0.0.1:3001/user-panel/orders
- Seller orders: http://127.0.0.1:3001/vendor-panel/orders
- Seller order + return: http://127.0.0.1:3001/vendor-panel/orders/1

Login: dev seller/admin contexts via vendor/admin panel selectors; customer via existing dev session headers on Host.
