# TB-TMAR-FULFILLMENT-AMSC-001 — W3 CQRS / endpoint request matrix

15 endpoint-reachable requests, 15 handlers, 10 transport validators, 21 module-owned routes.
Every request and its handler are co-located in one capability-first file; all are dispatched through
`ISender` (no direct validator invocation).

## Requests

| # | Request | Capability / file | Handler | Validator | Route |
| --- | --- | --- | --- | --- | --- |
| 1 | `CreateShippingServiceCommand` | `Shipping/Commands/CreateShippingServiceCommand.cs` | `CreateShippingServiceHandler` | `CreateShippingServiceCommandValidator` | `POST /v1/admin/shipping-services/` |
| 2 | `UpdateShippingServiceCommand` | `Shipping/Commands/UpdateShippingServiceCommand.cs` | `UpdateShippingServiceHandler` | `UpdateShippingServiceCommandValidator` | `PUT /v1/admin/shipping-services/{serviceId}` |
| 3 | `DeactivateShippingServiceCommand` | `Shipping/Commands/DeactivateShippingServiceCommand.cs` | `DeactivateShippingServiceHandler` | `DeactivateShippingServiceCommandValidator` | `POST /v1/admin/shipping-services/{serviceId}/deactivate` |
| 4 | `EnsureShippingCatalogSeedCommand` | `Shipping/Commands/EnsureShippingCatalogSeedCommand.cs` | `EnsureShippingCatalogSeedHandler` | none (no input) | `POST /v1/admin/shipping-services/ensure-seed` |
| 5 | `GetShippingServiceQuery` | `Shipping/Queries/GetShippingServiceQuery.cs` | `GetShippingServiceHandler` | `GetShippingServiceQueryValidator` | `GET /v1/admin/shipping-services/{serviceId}` |
| 6 | `ListShippingServicesQuery` | `Shipping/Queries/ListShippingServicesQuery.cs` | `ListShippingServicesHandler` | none (optional presentation locale) | `GET /v1/admin/shipping-services/` |
| 7 | `ListEnabledShippingMethodsTreeQuery` | `Shipping/Queries/ListEnabledShippingMethodsTreeQuery.cs` | `ListEnabledShippingMethodsTreeHandler` | none (optional presentation locale) | `GET /v1/admin/shipping-methods` |
| 8 | `SellerMutateFulfillmentCommand` | `Fulfillments/Commands/SellerMutateFulfillmentCommand.cs` | `SellerMutateFulfillmentHandler` | `SellerMutateFulfillmentCommandValidator` | seller fulfillment routes |
| 9 | `GetSellerFulfillmentQuery` | `Fulfillments/Queries/GetSellerFulfillmentQuery.cs` | `GetSellerFulfillmentHandler` | `GetSellerFulfillmentQueryValidator` | `GET /v1/seller/...` |
| 10 | `ListSellerFulfillmentsQuery` | `Fulfillments/Queries/ListSellerFulfillmentsQuery.cs` | `ListSellerFulfillmentsHandler` | none (auth-scoped, no transport input) | `GET /v1/seller/...` |
| 11 | `GetAdminFulfillmentQuery` | `Fulfillments/Queries/GetAdminFulfillmentQuery.cs` | `GetAdminFulfillmentHandler` | `GetAdminFulfillmentQueryValidator` | `GET /v1/admin/...` |
| 12 | `ListAdminFulfillmentsQuery` | `Fulfillments/Queries/ListAdminFulfillmentsQuery.cs` | `ListAdminFulfillmentsHandler` | none (no input) | `GET /v1/admin/...` |
| 13 | `ExecuteAdminFulfillmentBulkCommand` | `WorkQueue/Commands/ExecuteAdminFulfillmentBulkCommand.cs` | `ExecuteAdminFulfillmentBulkHandler` | `ExecuteAdminFulfillmentBulkCommandValidator` | `POST /v1/admin/...` |
| 14 | `QueryAdminFulfillmentWorkQueueQuery` | `WorkQueue/Queries/QueryAdminFulfillmentWorkQueueQuery.cs` | `QueryAdminFulfillmentWorkQueueHandler` | `QueryAdminFulfillmentWorkQueueQueryValidator` | `GET /v1/admin/...` |
| 15 | `ListCustomerCheckoutFulfillmentsQuery` | `Checkout/Queries/ListCustomerCheckoutFulfillmentsQuery.cs` | `ListCustomerCheckoutFulfillmentsHandler` | `ListCustomerCheckoutFulfillmentsQueryValidator` | `GET /v1/customer/orders/{checkoutId:guid}/fulfillments` |

Validator coverage: **10 required + 5 no-validator-required**.

## Module-owned routes (21)

| Endpoint file | Routes |
| --- | --- |
| `Seller/FulfillmentSellerEndpoints.cs` | 8 |
| `Admin/FulfillmentAdminEndpoints.cs` | 5 |
| `Shipping/ShippingServiceEndpoints.cs` | 6 |
| `Shipping/ShippingMethodsEndpoints.cs` | 1 |
| `Customer/FulfillmentCustomerEndpoints.cs` | 1 |
| **Total** | **21** |

Groups mapped in `FulfillmentEndpointModule.MapFulfillmentEndpoints`: `/v1/seller`, `/v1/admin`,
`/v1/customer`, plus the public `/v1/admin/shipping-methods`.

## Canonical result envelope

- All endpoints return through `ApiResponseFactory` (`api.From(...)` / `api.FromFailure(...)`).
- The single raw/anonymous `Results.Json(new { ... })` in `FulfillmentCustomerEndpoints` is the **shipped
  customer contract** (consumed by `src/frontend/app/fulfillment/fulfillment-api.ts`) and is preserved.
- No `Results.BadRequest` / `Results.Problem` / `new ProblemDetails` / `Accept-Language` anywhere.
