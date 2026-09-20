# Process state persistence

- Table: `order.checkout_processes` via Order-owned migration AddCheckoutProcesses
- Mapped on `OrderDbContext.CheckoutProcesses`
- No Host persistence; no cross-schema FK; no shared mega-DbContext
- Tracker: `CheckoutProcessTracker` : `ICheckoutProcessTracker`
