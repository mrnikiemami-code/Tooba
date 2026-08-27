# 09 — Tests

Suite: `WalletCheckoutRefundTests` (+ existing `WalletFoundationTests`, `ReturnFoundationTests`, `PaymentFoundationTests` constructor updates)

Covered (where feasible):

1. full wallet debit
2. duplicate spend / verify safe
3. insufficient balance
4. currency mismatch
5. concurrent spend no overdraw
6. Paid Order/Payment state after wallet verify
7. no sandbox/PSP redirect for wallet
8. refund credit exactly once
9. duplicate refund safe
10. partial refund amount
11. foreign/stranger spend deny
12. seed idempotency keys present
13. notification create-if-absent

Mixed tender: deferred (contract assert only).
