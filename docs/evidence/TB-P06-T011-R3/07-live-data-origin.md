# 07 — Live data origin audit (TB-P06-T011-R3)

| Check | Result |
| --- | --- |
| Static return object in UI | **NO** — return row from `POST /v1/customer/returns` |
| Fake refund amount | **NO** — computed by Returns module from order line |
| Hardcoded status badge | **NO** — Host `ReturnRequestStatus` |
| Manually forced modal state | **NO** — customer click + seller page `useEffect` on Requested |
| Direct DB cross-module mutation | **NO** — HTTP APIs + module commands only |

Scenario reproducible via `node scripts/t011-r3-return-scenario.mjs` then `node scripts/capture-t011-r3-live-modal-evidence.mjs`.
