# 10 — Runtime route proof

Probed with backend `:5088` and frontend `:3000` running:

| URL | Status |
| --- | --- |
| `/health/live` | 200 |
| `/health/ready` | 200 |
| `/vendor-panel/fulfillments` | 200 |
| `/admin/fulfillments` | 200 |
| `/` (Home) | 200 |

Fulfillment API routes unchanged from T009; UI surfaces now reachable.
