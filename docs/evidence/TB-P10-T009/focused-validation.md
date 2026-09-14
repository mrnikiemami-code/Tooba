# TB-P10-T009 — Focused validation

| Suite | Result |
| --- | --- |
| recovery-staleness.guard.test.mjs | 4/4 (`CURRENT_TASK_ID=TB-P10-T009`) |
| Host StoreAppearance | 16/16 |
| FE appearance/skin/card focused | 18/18 |
| `npm run test:storefront` | 67/67 |
| `npm run test:critical-storefront` | 18/18 |
| `git diff --check` | clean |
| Full tsc | not PASS (pre-existing Admin/grid; not fixed) |

Host filter: `FullyQualifiedName~StoreAppearance`.
