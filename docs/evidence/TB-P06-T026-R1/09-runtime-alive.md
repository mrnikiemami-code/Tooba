# 09 — Runtime alive

Task: TB-P06-T026-R1

Verified after validation + browser proof (kept running after Result):

| Service | Check | Status |
|---------|-------|--------|
| Host | `http://127.0.0.1:5088/health/live` | 200 `{"status":"ok"}` |
| Host | `http://127.0.0.1:5088/health/ready` | 200 ready |
| FE | `http://localhost:3000/customer-panel/wallet` | 200 |
| FE | `http://localhost:3000/customer-panel/gift-cards` | 200 |
| FE | `http://localhost:3000/admin/gift-cards` | 200 |
| FE | `http://localhost:3000/admin/wallets` | 200 |
| Shopeiva | `http://127.0.0.1:3001` | 200 |

Host temporarily stopped only during full `dotnet test` (DLL lock), then restarted before browser proof / Result.
