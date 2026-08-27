# 01 — Runtime before (TB-P06-T011-R3)

Task: `TB-P06-T011-R3`  
Predecessor: `c519cf36042ec2d13fee38ec79cafa2353be3836`

| Runtime | Port | URL | HTTP |
| --- | --- | --- | --- |
| PostgreSQL | 5432 | local dev | connected via Host |
| Tooba Backend | 5088 | http://127.0.0.1:5088 | `/health/live` 200, `/health/ready` 200 |
| Tooba Frontend | 3000 | http://127.0.0.1:3000 | `/` 200 |
| Original Shopeiva | 3001 | http://127.0.0.1:3001 | `/user-panel/orders` 200 |

Bridge: `http://127.0.0.1:17321/health` ok — task `TB-P06-T011-R3` / UUID `bc0fbef4-de04-4efd-9cd0-90b619b6a91c`
