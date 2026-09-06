# Existing operations audit (TB-P09-T001)

| Capability | Status | Reused seam |
|---|---|---|
| Cancel seller order | existing (no HTTP before) | `ICheckoutDirectory.CancelSellerOrderAsync` |
| Mark processing / packed | existing | `IFulfillmentDirectory` |
| Create shipment / tracking / dispatch / deliver | existing | `IFulfillmentDirectory` |
| Create / approve / reject return | existing | `IReturnDirectory` |
| Retry refund | existing | `IReturnDirectory.RetryRefundAsync` |
| Return eligibility | was inline in CreateAsync | now `IReturnEligibilityEvaluator` |
| Settlement adjust from refund | existing Debit | `AdjustFromRefundAsync` — not rewritten |
| Admin order ops HTTP | missing → added | Host Admin composer/endpoints |
| Permissions | existing catalog | order.cancel/handle/fulfill/refund, return.manage, fulfillment.manage |
