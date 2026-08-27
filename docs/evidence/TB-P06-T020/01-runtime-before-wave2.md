# 01 — Runtime before Wave 2 (TB-P06-T020)

Predecessor: `4c839098d3896c76feefecb878cbace5a2d336dd`  
HEAD == origin/main: YES  
Recorded: 2026-08-27

| Probe | Status |
|---|---|
| http://127.0.0.1:5088/health | 200 |
| http://127.0.0.1:5088/health/live | 200 |
| http://127.0.0.1:5088/health/ready | 200 |
| http://127.0.0.1:3000/fa | 200 |
| http://127.0.0.1:3000/vendor-panel | 200 |
| http://127.0.0.1:3000/admin | 200 |
| http://127.0.0.1:3000/account | 404 (customer panel uses `/customer-panel`; expected) |
| http://127.0.0.1:3001/ | 200 |

Triad healthy; proceed with Wave 2.
