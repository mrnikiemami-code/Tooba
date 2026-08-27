# 22 — Final runtime (TB-P06-T020)

| Probe | Result |
|---|---|
| Host `/health` | 200 |
| Host `/health/live` | 200 |
| Host `/health/ready` | 200 |
| FE `/fa` | 200 |
| FE `/vendor-panel/coupons` | 200 + CDP live list/empty + New Discount |
| FE `/vendor-panel/reviews` | 200 + CDP live reviews list |
| FE `/admin/promotions` | 200 + CDP Admin shell |
| FE `/admin/reviews` | 200 + CDP Admin reviews |
| Shopeiva `:3001` | 200 |

Browser: `browser-proof.json` + `captures/01`…`05`

## USER-PREVIEW

- Seller Promotions: http://127.0.0.1:3000/vendor-panel/coupons
- Seller Reviews: http://127.0.0.1:3000/vendor-panel/reviews
- Admin Promotions: http://127.0.0.1:3000/admin/promotions
- Admin Reviews: http://127.0.0.1:3000/admin/reviews
- Customer/Seller Notifications: DEFERRED (nav hidden)
- Persian Home: http://127.0.0.1:3000/fa
- Shopeiva: http://127.0.0.1:3001/
