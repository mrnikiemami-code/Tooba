# TB-P10-T004-R11 — Paid projection discovery

End-to-end money path (actual fields):

| Stage | Source | Fields |
| --- | --- | --- |
| Storefront payable | `StorefrontCheckoutComposer` | `sellers.Sum(PayableAmount) + snapshot.ShippingAmount` |
| Order snapshot | `CheckoutGroup` / `SellerOrder` | `GrandTotalSnapshot` (merchandise), `ShippingAmount` (Store shipping) |
| Payment amount | `PaymentDirectory.InitiateAsync` | `merchandise = pending.Sum(PayableAmount)`; `shipping = max(0, payable.ShippingAmount)`; `amount = merchandise + shipping` |
| Payment allocations | `CustomerPayment.Open` | SellerOrder rows (merchandise only) + optional `StoreShipping` / `StoreShippingTargetId` |
| Succeeded event | `PaymentSucceededIntegrationEvent` | `Amount` = payment amount; `SellerOrderIds` = seller allocations only |
| Inbox / projection | `OrderPaymentSucceededHandler` | `CheckoutPayableInvariant.CanonicalAmount(targets.GrandTotalSnapshot, payable.ShippingAmount) == event.Amount` |
| Order Paid | `OrderPaymentBridge.ApplyVerifiedSuccessAsync` | `RecordVerifiedPayment` + `EnsurePaidDurable` |
| Seller settlement | `SettlementPaymentSucceededHandler` | `AccrueFromPaymentAsync(..., SellerOrderIds)` — no StoreShipping |

Equation:

```text
PayableAmount
  == Sum(SellerOrder payment allocations)
   + StoreShipping allocation
  == Sum(SellerOrder.GrandTotalSnapshot)
   + Checkout.ShippingAmount
```

R10 architectural concern: comparing only seller totals left inbox empty when shipping > 0. Handler now uses the canonical sum. Seller payout stays merchandise-only.
