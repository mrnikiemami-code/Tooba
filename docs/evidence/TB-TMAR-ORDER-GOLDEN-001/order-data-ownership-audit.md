# Order data ownership audit

`OrderDbContext` and Order migrations are under `Tooba.Order.Infrastructure/Persistence`. The inspected Host route/composer paths do not own an Order DbContext, and no foreign module direct `OrderDbContext` use was found.

Order writes flow through Order Application/Infrastructure seams, but Host still orchestrates Order business use cases. No new cross-schema SQL join/FK was introduced by this pass. Data storage ownership is module-owned; application authority closure remains incomplete.
