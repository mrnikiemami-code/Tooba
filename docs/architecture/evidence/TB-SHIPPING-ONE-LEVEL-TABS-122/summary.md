# TB-SHIPPING-ONE-LEVEL-TABS-122 — Evidence index

Parent: TB-SHIPPING-ONE-LEVEL-TABS-121 · Channel: `tooba-main` · Worker: `tooba-worker-01`
Status: **PASS** · Stop-State: `STOP_FOR_ARCHITECT` · Code-SHA: NONE (observation-only)

| File | Content |
| --- | --- |
| `task.md` | Received downloadable task contract |
| `task-receipt.json` | Machine-readable result summary |
| `prepare-isolated-target-122.js` / `prepare-output.txt` / `isolation-guard.json` | Offline effective-config builder and fail-closed isolation verdict (`PREPARE=PASS`) |
| `launch-isolated-host-122.ps1` / `watchdog-result.txt` | The single bounded Host launch and its PID/absolute-path/health/stop proof |
| `launch-isolated-frontend-122.ps1` / `frontend-watchdog-result.txt` | The single bounded isolated frontend launch and its PID/stop proof |
| `run-api-prep-122.js` / `api-prereq-122.txt` | Authorized scratch prerequisites: admin actor, `GuestAllowed` policy, one-level catalog, guest cart + real offer line, authorized projection |
| `dom-proof-122.txt` | The browser DOM parity record: provenance, cart session, DOM flags, seven cards, 7/7 three-way comparison |
| `dom-shipping-fa.png` | Screenshot of the rendered `/fa/shipping` on the isolated instance |
| `shared-preflight.txt` / `shared-postflight.txt` | Shared host/PID/provenance/container parity, before and after |
| `shared-alpha-parity.txt` / `shared-alpha-postflight-counts.txt` | Read-only `tooba_alpha` count parity |
| `cleanup.txt` | Container/port/temp-dir/process removal proof |
| `summary.md` | Concise result summary |
| `RESULT.bridge.txt` / `post-result.js` | The exact Bridge result contract and its poster |

## Key figures

- Two task-owned process launches, both single, absolute-path and bounded: isolated Host `/health`
  200 after **36 s** (watchdog 180 s, never hit); isolated frontend ready in **28 s** (external bound).
- Binding: `TOOBA_HOST_ORIGIN` process env only → `/v1` proxy resolved to the **scratch** host
  (`GuestAllowed`) and not the shared host (`AuthenticatedOnly`); **no tracked file changed**.
- Genuine cart session created by the real PDP add-to-cart UI click — no seeding, no forgery.
- DOM: `shipping-page`, `shipping-steps`, `shipping-methods`, `shipping-continue`, `shipping-summary`
  present; `shipping-methods-empty` and `shipping-error` **absent** (projection branch, no fallback).
- **7 method cards; 7/7 Persian labels match BOTH the enabled one-level catalog and the
  browser-origin projection; formatted prices match; mismatches = 0.**
- Cleanup: both PIDs stopped after path verification; scratch container and temp dirs removed;
  0 listeners on 5401/3101/55453; raw cart secret never committed.
- Shared parity: identical PID/path/health/provenance, `:3000` 308, containers unchanged, and
  `tooba_alpha` counts byte-identical.
- Not performed: continue/payment click, `PUT shipping/selection`, `shipping/commit`, order/checkout/
  payment/shipment creation, manual SQL.
