# Host Authority Scan — R3

Scan root: `src/backend/Host/Tooba.Host/Admin`

| Check | Result |
|---|---|
| `HostAdminOrderFulfillmentOperations.cs` | **ABSENT** |
| Host types implementing `IAdminOrderFulfillmentOperations` | **0** |
| Host `Program.cs` registers Host adapter | **0** (removed) |
| `ListEnabledMethodsTreeAsync` in ShippingServiceEndpoints | **0** |
| `DefaultColor` / `DefaultOptions` in ShippingServiceEndpoints | **0** |
| `IShippingCatalogReader` in ShippingServiceEndpoints | **0** |
| Shipping tree projection/fallback in Host | **0** |
| Host shipping-methods route | thin `ListEnabledShippingMethodsTreeQuery` only |

Order implementation path:

`src/backend/Modules/Order/Tooba.Order.Infrastructure/Fulfillment/AdminOrderFulfillmentOperations.cs`

Shipping tree path:

`src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/Shipping/ListEnabledShippingMethodsTreeQuery.cs`
