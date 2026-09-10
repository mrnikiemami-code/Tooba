# R1 access model — TB-P09-T021-R1

Allow when **any** of:

1. Actor owns checkout (`PlacedByUserId == actor`) — session, Dev actor, or Dev GuestActor fallback
2. Valid `X-Tooba-Guest-Secret` proves access to checkout's `CartId`

Deny:

- No actor and no guest secret → **401** `customer.actor.missing`
- Wrong actor / wrong secret / unknown checkout → **404** `customer.order.missing` (no leakage)

Production without session still allows guest secret cart proof; no new identity system.
