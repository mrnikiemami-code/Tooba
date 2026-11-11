# Runtime data repair — invalid enum labels created by the TB-260 / TB-260-R1 relational fixtures — 2026-11-11

## Symptom (user-visible)

`/fa/admin` showed «فروشگاه در دسترس نیست — 500: Internal Server Error» and the admin order grid was empty.
Host log (`artifacts/run-logs/host.out.log`):

```
GET  /v1/admin/dashboard        -> 500  Cannot convert string value 'Placed' from the database to any value in the mapped 'SellerOrderStatus' enum.
POST /v1/admin/orders/query     -> 500  Cannot convert string value 'Web' from the database to any value in the mapped 'SalesChannel' enum.
POST /v1/admin/orders/query     -> 500  Cannot convert string value 'SingleStore' from the database to any value in the mapped 'OrderMode' enum.
```

## Cause

The relational fixture harnesses insert raw SQL rows and used three labels that are **not** members of the
current enums:

| column | written value | legal values |
| --- | --- | --- |
| `order.checkouts.mode` | `SingleStore` (an edition name) | `OnlinePurchase`, `RequestToReserve` |
| `order.checkouts.channel` | `Web` | `Direct`, `Marketplace`, `Agency`, `Corporate`, `Affiliate`, `Api` |
| `order.seller_orders.status` | `Placed` | `Submitted`, `PendingPayment`, `ReservationRequested`, `Cancelled`, `Paid`, … |

EF Core materialises those columns through `HasConversion<string>()`, so any read of the affected checkouts (admin
dashboard, admin order grid, admin order detail) threw and answered 500. Every affected row was a task-labelled
fixture (`TB260-*` / `TB260R1-*`); no user row carried an invalid label.

## Repair

1. Fixture harnesses now write legal labels — `mode = OnlinePurchase`, `channel = Marketplace`,
   `status = Paid` — in
   `Tooba.Order.Tests/Admin/AdminFulfillmentWorkUnitProjection260R1RelationalTests.cs`,
   `Tooba.Order.Tests/Admin/AdminFulfillmentWorkQueueDispatch260RelationalTests.cs` and
   `Tooba.Order.Tests/Checkout/ShippingGroupAcceptedEvidenceStorageTests.cs`. No production code changed.
2. One-off data repair of the already-written rows (single transaction, before/after counts recorded):
   `27` checkouts `Web → Marketplace`, `27` checkouts `SingleStore → OnlinePurchase`, `51` seller orders `Placed → Paid`;
   `cancelled_from_status = 'Placed'` was `0` and stayed `0`.

## Verification (canonical `tooba_alpha` @ 127.0.0.1:5432, Host 5088, FE 3000)

```
GET  /v1/admin/dashboard            -> 200  {"publishedProducts":40,"activeOffers":3223,"openOrders":6,"paidOrders":360,"pendingOrders":6,"sellers":8,"customers":183}
POST /v1/admin/orders/query         -> 200
POST /v1/admin/fulfillments/work-queue/query -> 200 totalCount=919 (unchanged)
GET  /v1/admin/orders/13feece0-…    -> 200 (the fixture that previously answered 500)
GET  /v1/admin/orders/01a1b92d-…    -> 200
```

Browser (`/fa/admin`, `/fa/admin/orders`, `/fa/admin/fulfillments`, `/fa/admin/shipping-services`,
`/fa/admin/settlement`, `/fa/admin/returns`): dashboard cards render numbers, orders grid renders rows, no
«فروشگاه در دسترس نیست» card, and the host log records no 500 after the repair. `dotnet build Tooba.Order.Tests -c Release` = 0 errors.

No migration, no schema change, no production source change; only the three test harnesses and the three label sets above.
