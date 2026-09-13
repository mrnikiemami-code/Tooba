# TB-P10-T004-R21-R1 — Transaction Boundary

`CheckoutDirectory.SubmitAsync`:

1. Revalidate cart/price/shipping (quote, no persist)
2. `TransactionScope` begin
3. Acquire all inventory (`ReserveCartLinesForOrderAsync`)
4. Barrier after reserve
5. Create Order + SellerOrders/lines, bind reservations, Cycle #1 `PrepareStart`
6. `SaveChanges` order
7. Barrier after order write
8. `CartDirectory.ConvertAsync`
9. Barrier after cart converted write
10. `scope.Complete()`

Idempotent replay of an already-committed checkout still reconciles conversion only.

Payment initiate (`StorefrontPaymentComposer.InitiateAsync`) is after commit; it does not call `SubmitAsync`.
