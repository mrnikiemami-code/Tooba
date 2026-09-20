# Order.Infrastructure → Inventory.Application usage (pre-W4)
| Source | Inventory.Application API | Semantics | TX | Classification |
|---|---|---|---|---|
| CheckoutDirectory.CancelSellerOrderAsync | IInventoryDirectory.ReleaseAsync | release Held on cancel | Order SaveChanges after | SAFE_CONTRACT_EXTRACTION |
| CheckoutDirectory.RestoreCancelledCheckoutAsync | FindReservationAsync + ReserveAsync(expiresAt:null) + ReleaseAsync rollback | durable reacquire; StockItemId peeked | Order SaveChanges | SAFE_CONTRACT_EXTRACTION |
| OrderPaymentBridge.PromoteReservationsForManualPaymentReviewAsync | Find + Promote or Reserve | manual review TTL | Order SaveChanges | SAFE_CONTRACT_EXTRACTION |
| OrderPaymentBridge.ReleaseReservationsAfterManualRejectAsync | Find + Release if Held | reject path | no Order write of inventory | SAFE_CONTRACT_EXTRACTION |
| OrderPaymentBridge.EnsurePaidDurableOrKeepPaidAsync | EnsureOrderSupplyAsync(EnsurePaidDurable) | late-captured paid supply | inside ApplyVerifiedSuccess | SAFE_CONTRACT_EXTRACTION |
All selected paths are checkout/order lifecycle related; none require async compensation redesign for W4.
