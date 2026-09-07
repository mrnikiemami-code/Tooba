# Whole-Order Action Dedup

Backend projects one whole-order `cancel` with `sellerOrderId=null` when any seller is cancellable. Execute without `sellerOrderId` cancels every cancellable seller.

`AdminOrderWholeOrderActions.Collapse` keeps each of `{cancel, confirm_deposit, reject_deposit, restore_deposit, restore_cancelled_order}` once (prefer null seller).

FE `filterOperationsForScope("whole-order")` applies the same dedupe. Seller-scoped codes (`cancel_shipment`, `correct_tracking`, unpack, …) stay in اقلام و ارسال / detail and the fulfillment queue kebab.
