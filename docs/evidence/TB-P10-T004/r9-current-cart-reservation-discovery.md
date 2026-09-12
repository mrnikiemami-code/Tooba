# Discovery — reservation start (before → after R9)

```
AddToCart -> Cart -> Shipping -> Order Commit -> Payment -> Confirmation
   avail        avail    avail      HARD HOLD      hold/promote     durable
```

Before R9: `CartDirectory.AddOrIncreaseLineAsync` called `ReserveAsync` with owner `cart:{cartId}` and 30m `HoldTtl`. Shipping commit copied `CartLine.ReservationId` onto OrderLine.

After R9:
- Add/update/read: availability only; no `ReserveAsync`
- `/shipping` projection/selection: no hold
- Shipping commit (`CheckoutDirectory.SubmitAsync`) is the Order commit boundary: unique checkout first, then order-level `ReserveAsync` (`order-commit:{cartId}`), bind to OrderLine
- Preview does not reserve
- Payment uses existing R5–R8 hold promotion / EnsurePaidDurable
