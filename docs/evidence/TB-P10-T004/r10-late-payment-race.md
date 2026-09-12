# TB-P10-T004-R10 — Late payment race

- Expire uses row lock (`SKIP LOCKED` + status check). Succeeded rows are not expired.
- `ApplyVerifiedSuccess` is allowed from `Expired`. Captured money becomes Succeeded.
- `OrderPaymentBridge.ApplyVerifiedSuccessAsync` marks Paid then calls `EnsurePaidDurable` / `EnsureOrderSupply` (`EnsurePaidDurableOrKeepPaidAsync`). Money is never discarded. If supply cannot be secured, SupplyStatus stays Unavailable (paid-but-supply-unavailable).
- `OrderPaymentSucceededHandler` compares the event amount to seller `GrandTotalSnapshot` sum plus checkout `ShippingAmount` (storefront payable), then loads seller-order lines before Ensure.
- Manual late evidence after expiry must go through retry + EnsureOrderSupply (submit on Expired is invalid_state).
- Runtime G: timeout then sandbox success → Succeeded + hold=1. G2: Succeeded + SupplyStatus Unavailable.
