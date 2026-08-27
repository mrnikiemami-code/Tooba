# 15 — Composition cache / refresh (TB-P06-T015)

| Layer | Behavior |
|---|---|
| DB | Source of truth per tenant page definition |
| Public GET | Reads current visible ordered sections (no long-lived stale cache required for MVP) |
| Admin mutation | Immediate persistence; next Home load sees new order/visibility |
| Frontend | Server load on Home request via `loadHomeComposition` |
| Restore-default | Rebuilds seed order atomically |

E2E proof exercised reorder → hide → restore against live Host without manual cache flush.
