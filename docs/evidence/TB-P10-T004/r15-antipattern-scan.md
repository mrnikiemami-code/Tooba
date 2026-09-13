# R15 anti-pattern scan

| Pattern | Result |
| --- | --- |
| PaymentAttempt == ReservationCycle | CLEAN — optional PaymentAttemptId only |
| Retry resets ExpiresAt on Active | CLEAN — Correlate + Ensure without ReviewExpiresAt |
| Resurrect Released | CLEAN — EnsureOrderSupply / new Reserve |
| Client cycle number | CLEAN — server NextNumber |
| Magic TTL | CLEAN — Settings keys |
| Scattered resolution | CLEAN — `ReservationCyclePolicyResolver` |
| Reacquire outside EnsureOrderSupply | KEEP existing restore/recovery new-row paths; retry uses Ensure |
| History rewrite | CLEAN — Close/events only |
| Partial per-line cycle | CLEAN — Order-level cycle |
| First-seller policy shortcut | CLEAN — MIN across lines |
| Polling workaround | CLEAN — no FE setInterval lifecycle |
| Test-only bypass | CLEAN |
| TB-P10-T005 | CLEAN |
