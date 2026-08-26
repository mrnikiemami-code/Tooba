# 13 — Final runtime preview (TB-P06-T002)

Recorded after validation; runtimes restarted for preview.

| Runtime | URL | Status |
|---|---|---|
| Backend health/live | http://127.0.0.1:5088/health/live | 200 |
| Backend health/ready | http://127.0.0.1:5088/health/ready | 200 |
| Frontend Home | http://localhost:3001/ | 200 (dev server; port 3000 occupied) |
| PostgreSQL | localhost:5432 | available (dev) |

## USER-PREVIEW

| Surface | URL |
|---|---|
| Frontend Home | http://localhost:3001/ |
| Backend Health (live) | http://127.0.0.1:5088/health/live |
| Backend Health (ready) | http://127.0.0.1:5088/health/ready |
| PDP (sample) | http://localhost:3001/products/workspace-live-shirt |

No visual changes in this task; HOME/PDP visual review remains OPEN_FOR_USER_FEEDBACK from P05.
