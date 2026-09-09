# R2 Runtime Smoke — TB-P09-T020-R2

Host `:5088`. Script `docs/evidence/TB-P09-T020/_r2_runtime.mjs` → `r2-runtime-raw.json` `ok: true`.

| Case | Result |
| --- | --- |
| qty 1 Cancel→Restore→Pack→Ship→Dispatch | ok; afterRestore OrderLine/Fulfillment Held aligned; afterDispatch Consumed |
| qty 1.25 same path | ok; exact decimal; no Held leak |
| error-ux-map | inventory.reservation.not_active mapped; domain no raw Held |

Reservation discovery confirmed before fix; rebind confirmed at runtime after restore.
