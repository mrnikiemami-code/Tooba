# R16 anti-pattern scan

| Pattern | Result |
| --- | --- |
| Frontend-authoritative expiry | CLEAN — countdown uses server holdEndsAt + serverTime |
| Timer reset after failed payment | CLEAN — same HoldEndsAt asserted |
| Per-second API polling | CLEAN — local setInterval only; no fetch in interval |
| Duplicate Order from retry | CLEAN — unpaid-retry / EnsureRetryAfterExpiryAsync unchanged |
| Active Cart as pending ownership | CLEAN — listCommittedCheckoutProofs / TryGetForOwnershipAsync |
| Global single proof overwrite | CLEAN — proofs keyed by checkoutId |
| Raw cycle enums/codes in UI | CLEAN |
| Duplicated retry policy in FE | CLEAN — capability-driven |
| First-order-only shortcut | CLEAN — multiple cards + proof map |
| N+1 item/payment/cycle | CLEAN — batched Contains + GetProjectionsAsync |

No TB-P10-T005.
