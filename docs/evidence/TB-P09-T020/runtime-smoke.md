# Runtime Smoke — TB-P09-T020

Host `:5088` `Host: alpha.localhost`. Script `docs/evidence/TB-P09-T020/_runtime.mjs`. Raw `runtime-raw.json` `ok: true`. Checkout `01a08620-9529-7000-9e28-7d57ab0f9b72`.

| Step | Result |
| --- | --- |
| Partial dispatch remainder | Pack/ship/dispatch 0.50 → 200; `pack_selected` still present; cancel absent; POST cancel 400 `order.cancel.forbidden` FA |
| Multi-shipment continuation | Pack/ship/dispatch remaining 0.75 → 200; second shipment created after first dispatch |
| Split-delivery return | Deliver A+B then return 0.25 → 200 |
| Seller B separate shipment | Skipped (`addB` 404 storefront); remainder proven on kg 1.25 |
| Invoice snapshot | `invoice.html` 200 with `تعداد اقلام` |
| T016 cancelled paid | no `request_return` |
| Queues | fulfillments + returns query 200 |
| Settlement | foundation compensation path reused |
