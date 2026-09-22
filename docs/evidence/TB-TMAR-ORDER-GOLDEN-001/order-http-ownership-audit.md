# Order HTTP ownership audit

Repo-wide route and composer inspection classified the current surfaces as follows:

- `ORDER_OWNED_HTTP`: Host `AdminOrderOperationsEndpoints`, `AdminOrderCompletenessEndpoints`, their composers, `OrderInventoryRecoveryComposer`, `OrderSupplyComposer`, and `AdminOrdersGridQueryEngine`.
- `CHECKOUT_ORDER_HTTP`: Storefront checkout submission/result/pending-payment composition. These are coupled to the frozen Checkout W1-W5 process and require a dedicated safe migration.
- `FOREIGN_MODULE_HTTP_USING_ORDER_CONTRACT`: Fulfillment, Returns, Payment, and Notification module endpoints consuming Order contract readers.
- `PLATFORM_ONLY`: global authentication/session/tenant/correlation adapters.
- `DEVELOPMENT_ONLY`: explicit Host bootstrap/seed paths.

The Order-owned admin routes still map in `Program.cs` and invoke Host composers directly. No `Tooba.Order.Endpoints` project exists. Therefore HTTP ownership is not closed and this task is INCOMPLETE; R1 must migrate these routes through `ISender` without advancing Checkout W6.
