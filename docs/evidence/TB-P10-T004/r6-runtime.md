# TB-P10-T004-R6 — Runtime
Host :5088. Raw: r6-runtime-raw.json ok=true.
| Scenario | Result |
| --- | --- |
| A Class A recover+confirm | PASS |
| B Class B paid recover + idempotent AlreadyHealthy | PASS |
| C insufficient stock | PASS inventory.recovery.insufficient |
| D atomicity | PASS (source + rollback) |
| E decimal | PASS via focused tests |
| F known order | purged locally; fixtures cover equivalent |
| Audit endpoint | PASS |
