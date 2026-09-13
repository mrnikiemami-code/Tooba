# TB-P10-T004-R24 — Financial

No R24 product change to money path. Focused Host `PaymentShippingAllocationTests` + `PaidProjectionFinancial` + `QuantityDecimalRegressionTests` (1.25) passed in this wake.

Payable = seller merchandise + StoreShipping. StoreShipping excluded from seller payout. Retries do not duplicate projection (same Order; payment outbox). Multi-seller allocation remains in `StorefrontCheckoutComposer` / allocation tests. Sandbox success amount `9685771.0000` IRR on checkout `01a09993-9bdc-7000-b251-2b608e43d8d9` projected to SellerOrder Paid.
