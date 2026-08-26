# 12 — Loading empty error states

| Surface | Loading | Empty | Error |
| --- | --- | --- | --- |
| Customer fulfillments | spinner text | «هنوز fulfillment…» | red Host unavailable |
| Seller list | DataGrid loading via source | empty grid | ErrorState + retry |
| Seller detail | «در حال بارگذاری» | N/A (404/denied) | ErrorState + retry |
| Admin grid | GridPage loading | empty grid | ErrorState + retry |
| Admin detail | «در حال بارگذاری» | N/A | ErrorState + retry |

No fabricated placeholder shipments when API returns empty array.
