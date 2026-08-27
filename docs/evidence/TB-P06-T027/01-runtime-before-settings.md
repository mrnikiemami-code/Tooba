# 01 — Runtime before settings

- health/live: 200
- health/ready: 200
- Tooba customer settings: http://localhost:3000/customer-panel/settings => 200
- Tooba customer profile: http://localhost:3000/customer-panel/profile => 200
- Tooba vendor settings: http://localhost:3000/vendor-panel/settings => 200 (route is vendor-panel, not seller-panel)
- Tooba admin settings: http://localhost:3000/admin/settings => 200 (unavailable shell)
- Shopeiva customer settings: http://127.0.0.1:3001/user-panel/settings => 200
- Shopeiva customer profile: http://127.0.0.1:3001/user-panel/profile => 200
- Shopeiva vendor settings: http://127.0.0.1:3001/vendor-panel/settings => 200
- Shopeiva admin settings: none (no admin tree)
- PostgreSQL / Host :5088 / FE :3000 / Shopeiva :3001 kept alive
