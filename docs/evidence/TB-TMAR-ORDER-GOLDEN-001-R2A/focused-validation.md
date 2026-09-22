# Focused validation
- `dotnet build src/backend/Tooba.slnx --no-restore`: PASS.
- Focused AdminOrderCompleteness/InvoiceHeader run before test expectation repair: 10 passed, 1 failed because the ownership test still counted the old authorizer method name.
- After repairing the expectation to assert six permission-aware calls and the view/handle split, the focused run passed: 11 passed, 0 failed.
- Combined ownership/durable/Checkout guard run: 21 passed, 1 failed. The failure was the PostgreSQL `CheckoutOrderFoundationTests.Checkout_revalidates_price_splits_sellers_and_isolates_tenants_on_postgres` fixture with an EF concurrency exception; no Checkout code was changed.
