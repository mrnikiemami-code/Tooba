# R1 Multi-Seller Runtime Projection

Real `POST /v1/storefront/shipping/projection` on multi-seller cart `01a08c19-1233-7000-bdba-ae0d7b02b96c`.

| Check | Result |
|---|---|
| HTTP | **200** |
| `sellerCount` | **2** |
| `maxSellerPreparationDays` | **3** (= max(1,3)) |
| Methods eligible | 7 Store-enabled codes incl. `post:express` |
| Selected amount | **200000** IRR (`post:express`) |
| Earliest delivery | **2026-09-15** |
| Delivery cards start at minimum | first date == minimum |
| FE `/fa/shipping` | **200** |
| Per-seller FE waterfall | **none** — single projection payload |
| Seller GUIDs in customer UI | not required; projection exposes counts/prep only |

Both sellers remain one customer checkout/cart. No Admin Consolidated Package concepts exposed on Storefront.

Raw step: `projection` in `r1-runtime-raw.json`.
