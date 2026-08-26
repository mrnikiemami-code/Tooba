# 15 — Final runtime preview (TB-P06-T001)

Runtime left running after validation.

| Service | URL | Status |
|---|---|---|
| Backend liveness | http://127.0.0.1:5088/health/live | 200 `{ "status": "ok" }` |
| Backend readiness | http://127.0.0.1:5088/health/ready | 200 with checks: edition, postgresql, authorization, messaging |
| Legacy health | http://127.0.0.1:5088/health | 200 |
| Frontend Home | http://127.0.0.1:3000/ | 200 |
| Frontend PDP | http://127.0.0.1:3000/products/demo-mobile-1 | 200 |

## USER-PREVIEW

- **Frontend:** http://127.0.0.1:3000/
- **Backend Health:** http://127.0.0.1:5088/health/live
- **Backend Readiness:** http://127.0.0.1:5088/health/ready
- **Home:** http://127.0.0.1:3000/
- **PDP:** http://127.0.0.1:3000/products/demo-mobile-1

Shopeiva reference not required for this ops task.
