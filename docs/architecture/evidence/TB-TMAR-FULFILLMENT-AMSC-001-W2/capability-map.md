# TB-TMAR-FULFILLMENT-AMSC-001 — W2 capability map

Capabilities are business responsibility axes (skill §5), not invented folder names. They
are derived from the module's own Endpoints audience/capability map, its Contracts/Domain
aggregates, and the W0 Analyze findings.

| Capability | Owned requests | Owning endpoints | Application home |
| --- | --- | --- | --- |
| **Shipping catalog** | `CreateShippingServiceCommand`, `UpdateShippingServiceCommand`, `DeactivateShippingServiceCommand`, `EnsureShippingCatalogSeedCommand`, `GetShippingServiceQuery`, `ListShippingServicesQuery`, `ListEnabledShippingMethodsTreeQuery` | `Endpoints/Shipping/ShippingServiceEndpoints.cs`, `ShippingMethodsEndpoints.cs` (admin + public storefront tree) | `Application/Shipping/` |
| **Fulfillment lifecycle** | `SellerMutateFulfillmentCommand`, `GetSellerFulfillmentQuery`, `ListSellerFulfillmentsQuery`, `GetAdminFulfillmentQuery`, `ListAdminFulfillmentsQuery` | `Endpoints/Seller/FulfillmentSellerEndpoints.cs`, `Endpoints/Admin/FulfillmentAdminEndpoints.cs` | `Application/Fulfillments/` |
| **Admin work queue** | `ExecuteAdminFulfillmentBulkCommand`, `QueryAdminFulfillmentWorkQueueQuery` | `Endpoints/Admin/FulfillmentAdminEndpoints.cs` | `Application/WorkQueue/` |
| **Customer checkout** | `ListCustomerCheckoutFulfillmentsQuery` | `Endpoints/Customer/FulfillmentCustomerEndpoints.cs` | `Application/Checkout/` |

Cross-capability shared rules (consumed by all four, owned by none):

| Shared concern | Home | Rationale |
| --- | --- | --- |
| `FulfillmentFluentRules`, `FulfillmentValidationCodes` | `Application/Validators/` | Transport/input shape helpers used by every capability's validators; assigning them to one capability would create a false owner. |
| `FulfillmentErrors`, `FulfillmentExceptionMapper` | `Application/Errors/` | Module-wide failure semantics. |

## Placement decisions

- **`Validators/` at the Application root is not a technical axis.** It contains exactly the
  two cross-capability helper files; there is no per-request validator living at the root
  (all 10 concrete validators live under their capability).
- **`Shipping/` is a capability, not a technical axis.** It owns a real business
  responsibility (the two-level shipping-service catalog plus provider metadata rules), the
  same way `Carts/` does in the certified Cart module.
- **`WorkQueue/` vs `Fulfillments/`.** The admin work-queue projection/bulk-execution surface
  has its own models (`AdminFulfillmentWorkQueueRow`, filter codes, bulk request/result) and
  its own two requests, so it is a distinct capability rather than a child of `Fulfillments/`.
- **`Checkout/` holds a single request** and is intentionally not merged into
  `Fulfillments/`: it is the customer-audience checkout projection, and its two files make it
  a legitimate shallow capability folder (not a single-file use-case leaf).
