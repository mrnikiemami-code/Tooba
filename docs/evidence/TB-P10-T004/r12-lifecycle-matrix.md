# TB-P10-T004-R12 — Lifecycle matrix

| State Before | Action | Money? | Supply required? | Reservation expected | Order | Payment | Retry? | Outcome |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Empty cart | Add/Update/Remove/GET | No | Availability only | None (no ReserveAsync) | — | — | n/a | Cart persists via Cart:PersistenceHours |
| Browsing cart | Stock changes | No | Recheck on mutate | None | — | — | n/a | Line stays; commit later may 409 |
| Cart + address/method | Shipping commit | No | Yes at commit | NEW Order hold | PendingPayment | — | n/a | Unique checkout; failure deletes unsold + releases |
| Committed unpaid online | Initiate + success | Yes | Durable | Commit/EnsurePaidDurable | Paid | Succeeded | n/a | Inbox applies Paid |
| Online pending | Sandbox failure | No | Keep hold | Held | PendingPayment | Failed | Yes same Payment new attempt | No duplicate Order |
| Online pending | Timeout worker | No | Release | Released | PendingPayment | Expired | Yes if supply | PaymentExpired capability |
| Expired unpaid | unpaid-retry + stock | No yet | EnsureUnpaidRetryHold | NEW hold | PendingPayment | Pending | After pay | Same PaymentId |
| Expired unpaid | unpaid-retry + no stock | No | Block | None | PendingPayment | Expired | No pay | «قابل تأمین نیست» |
| Expired | Late sandbox success | Yes | EnsurePaidDurable | NEW or keep Paid | Paid | Succeeded | n/a | No lost money |
| Manual initial | Timeout, no evidence | No | Release | Released | PendingPayment | Expired | unpaid-retry | Same as online unpaid |
| Manual pending | Evidence submit | Claimed | R5 review hold | Promoted/reacquired review TTL | PendingPayment | Pending | n/a | Unpaid worker skips |
| Review | Admin confirm | Yes | Durable | ExpiresAt null | Paid | Succeeded | n/a | |
| Review | Admin reject | No | Release | Released | PendingPayment | Failed | manual-retry + new evidence | NEW review hold on evidence2 |
| Review expired + stock | Late confirm | Yes | Reacquire | NEW durable | Paid | Succeeded | n/a | Old Released unchanged |
| Review expired + no stock | Late confirm | Claimed | Block | None | PendingPayment | Pending | n/a | Business supply error; payment not discarded as Succeeded |
| Paid + old hold released | recover_inventory_reservation | Yes | EnsurePaidDurable | NEW | Paid | Succeeded | n/a | Never resurrect Released |
| Multi-seller + ship | Pay success | Yes | Per line | Per seller | Paid | Succeeded | n/a | StoreShipping ≠ seller payout |
| Qty 1.25 | Commit | No | Exact | 1.25 | PendingPayment | — | n/a | Cart=Order=reservation |
| Last unit | Two commits | No | One wins | Winner Held | one Order | — | n/a | Loser 409, reserved≤onHand |

No contradictory rows found.
