# Runtime Smoke

Host `:5088` `Host: alpha.localhost`. Script `docs/evidence/TB-P09-T018/_runtime.mjs`. Raw `runtime-raw.json` `ok: true`.

| Step | Result |
| --- | --- |
| A ReadyToFulfill `01a085b3-73c7-7000-a4fd-b89ea2da4231` | qty 1.25; codes `[mark_processing]` |
| B Processing pack 0.50 / remain 0.75 | 200 |
| C Shipment 0.50 | queue id = Order Detail id |
| D Tracking | correct/void/dispatch present; missing-tracking empty |
| E Dispatch | cancel blocked; stale dispatch/void 400 FA |
| F Multi-seller 3 rows | bulk 400 human |
| G Cancelled T016 | no forward; stale process 400 FA |
| Filters | all tabs 200 |
