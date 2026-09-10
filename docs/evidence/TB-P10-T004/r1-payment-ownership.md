# Payment Ownership — TB-P10-T004-R1

- Owner A initiate payment amount == payable; duplicate idempotencyKey → same paymentId
- Foreign B token + wrong guest secret denied (401 `payment.guest.invalid`)
- Allocations sum == payable; StoreShipping retained
- Payment actor aligned with authenticated UserId (composer fix)

`payment-ownership` + `financial-consistency` ok=true.
