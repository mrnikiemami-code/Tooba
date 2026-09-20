# Design-code map — TB-TMAR-CHECKOUT-IMPL-W1

| Design concept | Current code |
|---|---|
| Submit entry | `CheckoutDirectory.SubmitAsync` → `CheckoutSubmitExecutor.ExecuteAsync` |
| Order write | `OrderDbContext.Checkouts` + SellerOrders/Lines inside TransactionScope |
| Cart conversion | `ICartDirectory.ConvertAsync` inside same scope |
| Inventory reserve | `ReserveCartLinesForOrderAsync` → Inventory directory |
| TX boundary | `new TransactionScope(... ReadCommitted ...)` in executor (preserved) |
| Persistence | `OrderDbContext` schema `order`; migration `20260920120000_AddCheckoutProcesses` |
| Outbox | Existing Order outbox interceptor unchanged; no new workflow messages |
| IDs | `IIdGenerator` in `CheckoutProcessTracker`; checkout id still `UuidV7` as before |
| States | Design PaymentPending terminal for submit; W1 enum `CheckoutProcessStatus` |
