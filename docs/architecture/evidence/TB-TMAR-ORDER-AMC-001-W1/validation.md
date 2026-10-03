# TB-TMAR-ORDER-AMC-001-W1 — Validation

## Build

```text
dotnet build src/backend/Tooba.slnx -clp:ErrorsOnly
Build succeeded. 0 Error(s)
```

## Focused tests

`dotnet test src/backend/Modules/Order/Tooba.Order.Tests`

| State | Failed | Passed | Total |
|---|---|---|---|
| baseline (HEAD `04c22c37`, W1 stashed) | 13 | 124 | 137 |
| after W1 | 13 | 124 | 137 |

**W1 introduces zero new failures.** The same 13 pre-existing red guards remain (they reference Host files that
were already evacuated before this task, plus two error-catalog completeness guards).

Pre-existing red guards (unchanged by W1, evidence `artifacts/order-baseline-test.log`):

1. `OrderAdminOrderDetailArchitectureGuardTests.Host_composer_no_longer_owns_GetOrderAsync_or_AdminViewAck`
2. `OrderAdminOrderDetailArchitectureGuardTests.Order_endpoints_own_detail_route_via_ISender`
3. `OrderAdminPanelResidualArchitectureGuardTests.Host_no_longer_owns_admin_orders_customers_or_OrderDbContext_in_admin_composers`
4. `OrderAdminPanelResidualArchitectureGuardTests.R4_through_R10_host_removals_remain_intact`
5. `OrderCustomerPanelArchitectureGuardTests.Host_no_longer_registers_customer_order_routes_or_retry_business`
6. `OrderEndpointOrganizationGuardTests.Host_does_not_duplicate_order_route_ownership`
7. `OrderOrdersGridArchitectureGuardTests.Host_no_longer_owns_the_orders_grid_query_engine`
8. `OrderOrdersGridArchitectureGuardTests.Orders_grid_route_is_mapped_once_and_only_by_order_endpoints`
9. `OrderReservationCycleArchitectureGuardTests.Unpaid_expiry_Host_is_shell_only_and_Order_owns_reconciler`
10. `OrderSellerPanelArchitectureGuardTests.R4_through_R9_host_removals_remain_intact`
11. `OrderEndpointPresentationTests.Composed_catalog_resolves_order_and_shared_codes_without_duplicates`
12. `OrderEndpointPresentationTests.Every_order_owned_error_code_has_an_explicit_descriptor`
13. `OrderEndpointValidatorCoverageGuardTests.Manifest_covers_every_endpoint_reachable_request_exactly_once`

Root causes of the pre-existing set: Host panel files moved to `Host/.../Panel/` while guards still probe the old
flat paths; Party seller-count port ownership changed; Order error descriptors still live in
`Tooba.Order.Endpoints` instead of `Tooba.Order.Contracts`. All three are scheduled in W2/W3.

## Guard updates in W1 (path-only, no weakening)

- `OrderApplicationOrganizationGuardTests`: root allowlist now `GlobalUsings.cs`; flattened leaf paths.
- `OrderAdminPanelResidualArchitectureGuardTests`: flattened leaf paths.
- `OrderCompletenessArchitectureGuardTests`: `InvoiceHeaderSemantics.cs` → `Rules/InvoiceHeaderSemantics.cs`.
- `OrderCustomerPanelArchitectureGuardTests`, `OrderSellerPanelArchitectureGuardTests`,
  `OrderStorefrontArchitectureGuardTests`: flattened leaf paths.
- `OrderInfrastructureOrganizationGuardTests`: `GlobalUsings.cs` added to root allowlist.
- `OrderTestData.cs`: domain type references use the new capability namespaces.
