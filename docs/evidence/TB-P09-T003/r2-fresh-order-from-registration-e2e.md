# R2 fresh order from registration E2E

Checkout `01a079bc-9ca7-7000-8e29-8f0e93b629dc` (see `r2-e2e-raw.json`).

| Step | Result |
|------|--------|
| cart + line + checkout | 200 PendingPayment |
| initiate provider=manual | Pending |
| ops confirm_deposit | 200 → Paid/ReadyToFulfill |
| duplicate confirm | 400 invalid (no double success) |
| processing→packed→shipment→tracking→dispatch→deliver | 200 |
| request_return→approve_return | 200 |
| list status | Return/Refund composed visible |

No SQL state injection.
