# TB-P10-T004-R23 — Concurrency

Strategy: per-customer row lock `CheckoutAbuseCustomerLocks` acquired with UPDATE/INSERT `SaveChanges` **inside** the R21 `TransactionScope`, then open-unpaid count and churn window query, then reserve/order.

Not used: frontend disable, `Thread.Sleep` / `Task.Delay`, post-hoc cleanup of duplicates.

Runtime L: maxOpen=1, remaining slot=1, two prepared carts committed in `Promise.all` → wins=1 blocks=1 (`200` + `409 checkout.open_unpaid_limit_reached`). Checkout winner `01a09987-e312-7000-895f-402f89b7d43b`.
