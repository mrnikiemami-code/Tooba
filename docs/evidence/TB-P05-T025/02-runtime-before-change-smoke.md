# 02 — Runtime before-change smoke (TB-P05-T025)

After confirming Host health and restarting Frontend (cleared corrupted `.next` from long-lived HMR):

| Route | Status |
|---|---|
| `GET http://127.0.0.1:5088/health` | 200 ok |
| `/` Home | 200 |
| `/products` Listing | 200 |
| `/cart` | 200 |
| `/checkout` | 200 |
| `/customer-panel` | 200 |
| `/vendor-panel` | 200 |
| `/admin` | 200 |
| `/v1/storefront/home` via Next rewrite | 200 |

Initial observation before FE restart: intermittent Home `500` from Next RSC/dev-cache (`SegmentViewNode` / `__webpack_modules__`). Treated as runtime dependency repair (no product redesign).
