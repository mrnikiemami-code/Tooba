# 22 — Final runtime preview (TB-P05-T026)

**Do not stop Host/FE after Result — user preview mandatory.**

| Service | URL | PID (LISTEN) | Status |
|---|---|---|---|
| Frontend (`next dev`) | http://127.0.0.1:3000 | **29596** | UP (HTTP 200 `/`) |
| Host (`dotnet run`) | http://127.0.0.1:5088 | **21852** | UP (`/health` → `{"status":"ok"}`) |

## USER-PREVIEW URLs

| Surface | URL |
|---|---|
| Frontend | http://127.0.0.1:3000 |
| Health | http://127.0.0.1:5088/health |
| Home | http://127.0.0.1:3000/ |
| Listing | http://127.0.0.1:3000/products |
| PDP (demo-game-2) | http://127.0.0.1:3000/products/demo-game-2 |
| Cart | http://127.0.0.1:3000/cart |
| Checkout | http://127.0.0.1:3000/checkout |
| Customer | http://127.0.0.1:3000/customer-panel |
| Seller | http://127.0.0.1:3000/vendor-panel |
| Admin | http://127.0.0.1:3000/admin |

Verified after final restart for gate: FE `/` 200, Host health ok, favicon.ico 200.
