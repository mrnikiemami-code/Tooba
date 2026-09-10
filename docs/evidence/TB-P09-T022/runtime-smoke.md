# Runtime smoke — TB-P09-T022

Script: `docs/evidence/TB-P09-T022/_runtime.mjs`  
Raw: `docs/evidence/TB-P09-T022/runtime-raw.json`

Host: `http://127.0.0.1:5088` · `Host: alpha.localhost`  
**OVERALL PASS** (2026-09-10)

## Results

| Scenario | Result |
| --- | --- |
| A Single-seller | PASS — checkout `01a08973-d831-7000-ae48-d6f8a6bc3fcf`; packages=0; `create_consolidated_package` absent |
| B Multi-seller create + lock | PASS — `MP-01A08973E914`; direct dispatch 400 (`order.operation.invalid`) |
| C Cancel + rebuild | PASS — new `MP-01A08973EB58`; old Cancelled remains (packages=2) |
| D Central dispatch | PASS — members Dispatched; whole-order cancel 400 (`order.cancel.forbidden`) |
| E Central deliver | PASS — package + members Delivered |
| F Owned customer | PASS — preferred `CENTRAL-T022-F` (SQL `placed_by_user_id`) |
| G Guest security | PASS — guest secret 200; GuestActor alone 404; wrong secret 404 |
| H Legacy independent | PASS — direct dispatch 200; packages=0 |
| I Concurrency overlap | PASS — second create 400 while first active |
| J Decimal 1.25 | PASS — cart accepted PARS qty 1.25; exact through package create |
| Order cancel voids package | PASS — pre-dispatch cancel → package Cancelled |

```text
node docs/evidence/TB-P09-T022/_runtime.mjs
```
