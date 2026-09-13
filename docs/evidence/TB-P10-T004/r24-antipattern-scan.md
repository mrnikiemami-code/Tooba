# TB-P10-T004-R24 — Anti-pattern scan

| Pattern | Result |
| --- | --- |
| Frontend-only auth/limits | CLEAN — Host 401/409 |
| Duplicated open-unpaid predicate | CLEAN — one `OpenUnpaidOrderPredicate` |
| Counting visible cards | CLEAN — SellerOrders only |
| Cancel refunding churn | CLEAN — immutable events |
| Hide freeing slot | CLEAN — still 409 |
| Cycle #2 new churn | CLEAN — events=1 |
| Payment attempts as Orders | CLEAN — not in gate |
| Race-prone count-then-insert | CLEAN — customer lock inside TX |
| IP-primary identity | CLEAN — Store + CustomerId |
| Sleep/retry hacks in gate | CLEAN |
| Compensating cleanup | CLEAN |
| Optimistic cart clearing | CLEAN — cart Active on blocks |
| Raw code UX | CLEAN — FA detail + notice |
| Settings mutating history | CLEAN — PUT future-only |
| N+1 | CLEAN — single count + window list |
| Polling | CLEAN — local countdown only |
| Offer-level T005 | CLEAN — not implemented |

Scan CLEAN. No new product code in R24.
