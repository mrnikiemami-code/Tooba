# TB-P10-T004-R22-R1 — Anti-Pattern Scan

| Pattern | Result |
| --- | --- |
| Duplicate auth stack | CLEAN — Identity OTP + existing session |
| Storefront password login | CLEAN — no type=password |
| Hardcoded OTP in Production | CLEAN — Dev/Testing only |
| Frontend-only gate | CLEAN — backend 401 |
| Open redirect | CLEAN — sanitizeReturnTo |
| localStorage cart merge SoT | CLEAN — Host merge API |
| Client-selected ownership | CLEAN — guest secret + session |
| Merge Reservation/Order/Payment | CLEAN |
| Silent line drop | CLEAN — catch keeps line |
| Cart overwrite | CLEAN — merge into auth Active |
| Polling / retry hacks | CLEAN |
| TB-P10-T005 / MaxOpenUnpaidOrders | CLEAN |

Scan CLEAN.
