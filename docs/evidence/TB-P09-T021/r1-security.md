# R1 security — TB-P09-T021-R1

- No arbitrary anonymous Order lookup: checkoutId alone is insufficient.
- Guest secret must match cart credential hash for that checkout's CartId.
- Unowned actor / wrong secret → identical 404 (`customer.order.missing`).
- Cancelled packages never become preferred primary tracking.
- Cross-customer: owned actor A cannot read guest-placed or actor-B checkout.
- Locks: LOCK-OPS-022, LOCK-OPS-023.
