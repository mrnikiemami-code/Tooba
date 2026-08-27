# 25 — Runtime kept alive (TB-P06-T029)

| Service | URL | Status |
| --- | --- | --- |
| Backend | http://127.0.0.1:5088 | health/live + health/ready 200 |
| Tooba FE | http://localhost:3000 | next dev -H localhost Ready |
| Original Shopeiva | http://127.0.0.1:3001 | next dev -H 127.0.0.1 Ready (restarted after accidental node cleanup) |

Do **not** stop after Result.
