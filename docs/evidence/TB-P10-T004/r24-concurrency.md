# TB-P10-T004-R24 — Concurrency

Enforcement: `CheckoutAbuseCustomerLocks` UPDATE/INSERT inside the same `TransactionScope` before counts. No frontend disable, no Sleep in the gate, no compensating cleanup.

| Case | Result |
| --- | --- |
| A same Cart twice | conc-same-cart wins=0 (409/409) — at most one Order |
| B last open slot, two carts | L-concurrent-one-winner wins=1 blocks=1; winner `01a09993-b6b4-7000-94fa-015b2702ae07`; loser `checkout.open_unpaid_limit_reached` |
| C last churn slot, two carts | S-concurrent-last-churn wins=1 churnBlocks=1 |
| D cancel vs new | cancel then replacement 200; final open count correct |
| E hide vs new | hide does not create capacity; still 409 |
| F paid projection vs new | count source is SellerOrder status (`PendingPayment`/`Submitted`). K waited for authoritative `Paid` via payment.succeeded outbox (poll status only; no limit bypass). |

No sleeps/retry hacks inside the product gate.
