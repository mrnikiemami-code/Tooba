# TB-TMAR-HOST-ADMIN-CANON-001 — Behavior Parity

Read-only composition repair; no message parsing, no `InvalidOperationException` flow, no new
`PlatformHttpException` control flow.

## Route parity

| Route | Owner before | Owner after | Shape |
|---|---|---|---|
| `GET /v1/admin/dashboard` | Host `AdminPanelEndpoints` | Host `AdminPanelEndpoints` | unchanged |
| `GET /v1/admin/sellers` | Host `AdminPanelEndpoints` | Host `AdminPanelEndpoints` | unchanged |
| `POST /v1/admin/sellers/query` | Host `AdminPanelEndpoints` | Host `AdminPanelEndpoints` | unchanged |
| `GET /v1/admin/dev-context` | Host `AdminPanelEndpoints` | Host `AdminPanelEndpoints` | unchanged |
| `GET/POST /v1/admin/orders*` | Order.Endpoints | Order.Endpoints | unchanged |
| `GET/POST /v1/admin/customers*` | Order.Endpoints | Order.Endpoints | unchanged |

Authorization, tenant resolution, and error mapping in `AdminPanelEndpoints.ExecuteAsync` /
`AdminGridQueryEndpoint.ExecuteAsync` were not modified.

## Dashboard semantics

`AdminDashboardSummary(PublishedProducts, ActiveOffers, OpenOrders, PaidOrders, PendingOrders,
Sellers, Customers)` — record shape unchanged.

| Counter | Before | After |
|---|---|---|
| PublishedProducts | `CatalogDbContext.Products.Count(Status == Published)` | `ICatalogAdminProductCountGateway.CountPublishedProductsAsync()` → same predicate |
| ActiveOffers | `IOfferQueryGateway.CountActiveOffersAsync()` | unchanged |
| Sellers | `IOfferQueryGateway.ListDistinctSellerPartyIdsAsync().Count` | unchanged |
| OpenOrders | Order metrics `open` | `IAdminOrderDashboardMetricsPort` → same store |
| PaidOrders | Order metrics `paid` | same store |
| PendingOrders | Order metrics `pending` | same store |
| Customers | Order metrics `customers` | same store |

Order counters are delegated through a Contracts adapter to the same
`IAdminOrderDashboardMetricsStore` logic (`AdminOrderDashboardMetricsStore`), so counting rules
(Paid, PendingPayment/Submitted, not-Paid-not-Cancelled open, distinct `PlacedByUserId`) are
preserved.

## Sellers list semantics

`GET /v1/admin/sellers` → `AdminPanelComposer.ListSellersAsync`:

- universe of sellers = distinct `SellerPartyId` from offers (unchanged);
- `DisplayName` and `Status` now come from `IPartyAdminSellerReadGateway`, which selects the same
  `BusinessParty.PartyId / DisplayName / Status` columns and projects `Status.ToString()` — same
  string value as the previous `party.Status.ToString()`;
- `ActiveOffers` computed from the same offer status rows;
- `OrderCount` from the same per-seller count logic.

`AdminSellerListItem(SellerPartyId, DisplayName, Status, ActiveOffers, OrderCount)` unchanged.

## Sellers grid semantics (`POST /v1/admin/sellers/query`)

Policy and normalization unchanged: `AdminListGridPolicies.Sellers.Normalize(request)` still runs in
Host before the engine.

| Aspect | Before | After |
|---|---|---|
| Search | substring on `DisplayName`, case-insensitive | same |
| Filter `name` | blank / notBlank / equals / notEqual / startsWith / endsWith / notContains / contains | same operators, same case-insensitive semantics |
| Filter `status` | enum filter, in / notIn / notEqual | same string matching on projected status |
| Filter `offers` / `orders` | numeric operators incl. `between`, `blank`, `notBlank` | identical `NumberMatch` logic retained |
| Advanced filter | left-to-right via `GridAdvancedFilterEvaluator` | identical evaluator retained |
| Sort `name` | asc/desc, tie-break `PartyId` | same (ordinal display-name ordering) |
| Sort `status` | asc/desc, tie-break `DisplayName` | same |
| Sort `offers` / `orders` | metric sort then `PartyId` tie-break | same |
| Paging | `Skip((page-1)*pageSize).Take(pageSize)`, `TotalCount` = filtered total | same |
| Empty result | `GridPageResponse([], page, pageSize, 0)` | same |

`GridPageResponse<AdminSellerListItem>` shape unchanged.

## Explicit non-changes

- No frontend edits.
- No schema/migrations.
- No Storefront, authorizer, DevActor, ProductWorkspace, Support/Wallet changes.
- No new Host/Admin business endpoint; Host/Admin remains 15 files.
- W36 StoreAppearance closure untouched.
