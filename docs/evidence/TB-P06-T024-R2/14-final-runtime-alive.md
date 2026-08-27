# 14 — Final runtime alive

Task: TB-P06-T024-R2

## Kept running after validation

| Service | URL | Check |
|---------|-----|-------|
| Backend Host | `http://127.0.0.1:5088` | `/health/live` 200, `/health/ready` 200 |
| Tooba Frontend | `http://127.0.0.1:3000` | `/admin/access-control` 200 |
| Original Shopeiva | `http://127.0.0.1:3001` | `/` 200 |

## Preview-ready APIs

| Endpoint | Status |
|----------|--------|
| `GET /v1/admin/access-control/demo-preview` | 200 (demo IDs populated) |
| Seller ACC / Orders via FE | reachable (see browser captures) |

## Policy

Do **not** shut down Backend / Tooba Frontend / Original Shopeiva after Bridge Result.
