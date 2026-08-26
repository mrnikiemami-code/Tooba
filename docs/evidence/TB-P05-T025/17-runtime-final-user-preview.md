# 17 — Runtime final user preview (TB-P05-T025)

## URLs (user-openable)

| Surface | URL |
|---|---|
| Backend health | `http://127.0.0.1:5088/health` |
| Frontend | `http://127.0.0.1:3000` |
| Home | `http://127.0.0.1:3000/` |
| PDP (live demo) | `http://127.0.0.1:3000/products/demo-game-3` |
| Listing | `http://127.0.0.1:3000/products` |
| Cart | `http://127.0.0.1:3000/cart` |
| Checkout | `http://127.0.0.1:3000/checkout` |
| Customer | `http://127.0.0.1:3000/customer-panel` |
| Seller | `http://127.0.0.1:3000/vendor-panel` |
| Admin | `http://127.0.0.1:3000/admin` |

## Runtime notes

- PostgreSQL dependency: Docker container `postgres-db` on `127.0.0.1:5432` (Docker Desktop was started when found stopped).
- Frontend: `npm run dev -- --hostname 127.0.0.1 --port 3000` kept running after Result.
- Backend: `dotnet run` Host on `5088` kept running after Result.
- `next build` while `next dev` is alive can corrupt RSC cache → clear `.next` and restart `next dev` before user preview.

## Process IDs (final verification)

| Process | PID | Port |
|---|---|---|
| Tooba.Host | 21760 | 5088 |
| Next.js (`node`) | 11612 | 3000 |
| postgres-db (Docker) | container `postgres-db` | 5432 |

Status at final check: Host `/health` = ok; Frontend `/` = 200; `/v1/storefront/home` = 200.
