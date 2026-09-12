# TB-P10-T004-R11 — Financial invariant

`CheckoutPayableInvariant.CanonicalAmount`:

```text
Payable = Σ seller merchandise + max(0, StoreShipping)
```

- Seller allocations = `SellerOrder.GrandTotalSnapshot` / `PayableAmount`
- StoreShipping allocation = `Checkout.ShippingAmount` when > 0
- Shipping is never added to the first SellerOrder
- Settlement readers consume `SellerOrderIds` only
- Precision: same `decimal` as payment Amount / allocation `19,4`; exact equality, no ad-hoc tolerance
