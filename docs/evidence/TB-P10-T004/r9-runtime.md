# TB-P10-T004-R9 — Runtime
Host :5088. Raw: r9-runtime-raw.json ok=true.

| Scenario | Result |
| --- | --- |
| A Browse beyond old 30m TTL | PASS — add reservedDelta=0, cart holds=0, commit 200, order hold=1 |
| B Stock lost while browsing | PASS — line remains, commit 409, orders=0 |
| C Last-unit race | PASS — win 200 / lose 409, reserved<=onHand |
| D Online payment hold | PASS — commit+pay 200 |
| E Manual payment hold | PASS — commit+pay 200 |
| F Admin SupplyStatus | PASS |
| G Add/GET no reservation writes | PASS — deltas 0 |
