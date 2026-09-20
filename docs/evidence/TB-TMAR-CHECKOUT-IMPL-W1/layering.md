# Layering

- Domain: CheckoutProcess
- Application: ICheckoutProcessTracker
- Infrastructure: CheckoutProcessTracker, CheckoutSubmitExecutor, migration, OrderDbContext mapping
- Host transport only; no new App→App edges
