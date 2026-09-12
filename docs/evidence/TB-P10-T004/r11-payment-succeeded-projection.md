# TB-P10-T004-R11 — Payment Succeeded projection

- Inbox short-circuits on `EventId` (duplicate delivery is a no-op).
- Amount guard uses `CheckoutPayableInvariant.CanonicalAmount`, not seller totals alone.
- Currency must match checkout.
- Target seller-order set must match `SellerOrderIds` (StoreShipping id is never in that list).
- Paid is applied once via `RecordVerifiedPayment`; Ensure failure keeps Paid (R10 late-money).
- Order side does not open `PaymentDbContext`; it reconstructs the allocation equation from Order snapshots.
- Allocation row order does not change seller amounts (`OrderBy(SellerOrderId)` at initiate).
