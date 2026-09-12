# TB-P10-T004-R12 — Runtime
Host :5088. Raw: r12-runtime-raw.json ok=true.

| Scenario | Result |
| --- | --- |
| A Online success | PASS — cart reserves=0, commit 200, Succeeded, hold=1 |
| B Fail then retry | PASS — same Payment, new attempt, Succeeded |
| C Timeout then retry+pay | PASS — hold 0 → new res → Succeeded |
| D Timeout + no stock | PASS — 409 «قابل تأمین نیست» |
| E Late captured | PASS — Succeeded hold=1 |
| F Manual normal | PASS — evidence+confirm 200, durable |
| G Reject + retry | PASS — reject releases; evidence2 NEW hold; old Released |
| H Review expiry + stock | PASS — confirm 200, new res |
| I Review expiry + no stock | PASS — confirm 400 |
| J Multi-seller + ship | PASS — 2217588=2017588+200000 |
| K Decimal 1.25 | PASS — cart=order=reservation 1.25 |
| L Cart no hard reserve | PASS — cart_lines.reservation_id null until commit |
| M Last-unit race | PASS — 200/409, reserved≤onHand |
| N Admin SupplyStatus | PASS — grids 200, sample Reserved |
| O Historical recover | PASS — Recovered, new id, old Released |
| P Result + ownership | PASS — owned 200; naked 400; wrong/empty 401; client Ensure 401 |
| Settings roundtrip | PASS — GET/PUT 200 |
