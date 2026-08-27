# 10 — Runtime kept alive

After validation + FE restart (production build invalidated `.next`):

| Service | URL | Status |
| --- | --- | --- |
| Backend Host | `http://127.0.0.1:5088` | `/health/live` 200 · `/health/ready` 200 |
| Tooba Frontend | `http://localhost:3000` | `next dev -p 3000 -H localhost` Ready |
| Original Shopeiva | `http://127.0.0.1:3001` | 200 (`/payment`, `/cart`, `/user-panel/*`) |

**Do not stop** after Result. Bind FE as **localhost** (not `-H 127.0.0.1` alone) to avoid `/fa/*` rewrite 308 loops.
