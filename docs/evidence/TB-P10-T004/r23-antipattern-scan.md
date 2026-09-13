# TB-P10-T004-R23 — Anti-Pattern Scan

| Pattern | Result |
| --- | --- |
| Counting pending cards instead of Orders | CLEAN — `SellerOrders` + `OpenUnpaidStatuses`; hide table not queried |
| Hide freeing open-order capacity | CLEAN — D runtime still 409 |
| Cancel refunding churn quota | CLEAN — events immutable; G kept history |
| Churn inferred from current Orders | CLEAN — `CheckoutReservationCommits` by OccurredAt window |
| Payment Attempts counted as Orders | CLEAN — no PaymentAttempt in gate |
| Cycle #2 counted as new checkout commit | CLEAN — J events=1 |
| Frontend-only limits | CLEAN — Host 409 before reserve |
| IP-based primary identity | CLEAN — Store + CustomerId |
| Race-prone count-then-insert | CLEAN — customer row lock inside TX |
| Sleep/retry hacks | CLEAN — no Sleep/Delay in gate |
| Cleanup-after-duplicate | CLEAN |
| Raw machine code UX | CLEAN — FA detail + notice component |
| Duplicated eligibility predicates | CLEAN — one `OpenUnpaidOrderPredicate` |
| N+1 | CLEAN — single count + window list |
| Offer-level T005 / `MaxReservationCommitsPerCustomerPerOfferInWindow` | CLEAN — not implemented |

Scan CLEAN.
