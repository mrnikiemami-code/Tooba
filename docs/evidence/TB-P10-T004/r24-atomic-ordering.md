# TB-P10-T004-R24 — Atomic ordering

`CheckoutDirectory.SubmitAsync` (R21 path, unchanged):

1. Cart load / version / Active / non-empty
2. Quote seller orders without reservation
3. Load abuse settings outside TX
4. `TransactionScope` ReadCommitted
5. `EnsureCanStartInitialReservationAsync` — customer lock, open-unpaid, then churn
6. `ReserveCartLinesForOrderAsync`
7. `CheckoutGroup.Submit` + bind reservations
8. `PrepareInitialCycleAsync` Cycle #1
9. `PrepareInitialCommit` churn row
10. `SaveChanges`
11. `ConvertAsync` source cart
12. `scope.Complete()`
13. Payment initiation is later (`StorefrontPaymentComposer.InitiateAsync`), not inside submit

Limit failures throw before reserve/order. Inventory fail (`F-inventory-failure`) 400, no checkoutId, commits 22→22, reservations 34→34. Open-unpaid/churn 409 keep cart Active and reservation count unchanged.

`AtomicCheckoutCommitTests` asserts identity/limit call sits before `ReserveCartLinesForOrderAsync`, Convert before Complete, and payment does not call `SubmitAsync`.
