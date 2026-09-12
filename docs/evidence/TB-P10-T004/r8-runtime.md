# TB-P10-T004-R8 — Runtime
Host :5088. Raw: r8-runtime-raw.json ok=true.

| Scenario | Result |
| --- | --- |
| A Orders grid badges | PASS Reserved / AvailableForReacquire / Unavailable |
| B Order detail shortage | PASS human lines |
| C Payments grid | PASS supply column batched |
| D Confirm AvailableForReacquire | PASS auto-reacquire, recover hidden, durable |
| E Confirm Unavailable | PASS 400 inventory.supply.unavailable; payment pending |
| F Recovery capability | PASS hidden when Reserved |
| G FA labels | PASS |
