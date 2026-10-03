# TB-TMAR-ORDER-AMC-001-W2 — Structure: Infrastructure/Endpoints + stale guard repair

## Scope of this wave

W1 already delivered the Domain god-file split, the Application capability-first flattening, and the
`GlobalUsings.cs` namespace bridges. W2 closes the remaining *physical structure* debt and repairs the stale
Host-path guards that were red before this task started.

## Infrastructure (`Tooba.Order.Infrastructure`)

| Check | State |
|---|---|
| Root allowlist | `GlobalUsings.cs`, `OrderModule.cs` — `ENFORCED` |
| Capability folders | `Admin/`, `Checkout/{Abuse,Persistence}`, `Customer/`, `Seller/`, `Storefront/`, `ReservationCycle/`, `PurchaseVerification/`, `Events/Payment/`, `Guards/`, `Messaging/`, `Adapters/`, `Integrations/{Payment,Fulfillment,Returns,Notifications,GridEnrichment}` |
| Persistence | `Persistence/OrderDbContext.cs` + `Persistence/Migrations/` (migrations not regenerated) |
| Forbidden top-level folders | `CheckoutAbuse`, `Payments`, `Fulfillment` — absent |

`Integrations/<ForeignModule>/` is the coherent responsibility axis for Contracts-consuming bridges and is kept
as-is. Introducing a parallel `Directories/` canon (Catalog-style) is **not** required by the Order manifest and
would be pure churn against a certified `structureCertified: true` module; per the Structure skill, capability /
responsibility grouping wins over clone-the-reference foldering.

## Endpoints (`Tooba.Order.Endpoints`)

| Check | State |
|---|---|
| Root allowlist | `OrderEndpointModule.cs` — `ENFORCED` |
| Audience folders | `Admin/{Completeness,Customers,Detail,InventoryRecovery,Operations,OrdersGrid,Settings}`, `Seller/{,Settings}`, `Customer/`, `Storefront/` |
| Shared folders | `Errors/` (catalog contributor + codes), `Resources/` (resource set + resx) |
| Duplicate route ownership | none — Host `AdminPanelEndpoints` only maps `/v1/admin/dashboard` |

## Stale Host-path guard repair (was RED at task start)

The Host Admin panel was relocated to `Host/Tooba.Host/Admin/Panel/` and `Host/Customer`, `Host/Grid`,
`Host/Seller`, `Host/UnpaidOrderExpiryHostedService.cs` no longer exist. Order architecture guards still probed
the old flat paths, so they failed on `FileNotFoundException`/`DirectoryNotFoundException` rather than on a real
architecture regression. Repaired to assert the *post-evacuation* truth (absent/relocated paths, `Program.cs`
registration absent, Host composer consuming only `Order.Contracts` ports):

| Guard | Old assertion | New assertion |
|---|---|---|
| `OrderAdminOrderDetailArchitectureGuardTests` (×2) | `Host/Admin/AdminPanelEndpoints.cs`, `AdminPanelComposer.cs` | `Host/Admin/Panel/…` |
| `OrderOrdersGridArchitectureGuardTests` (×2) | `Host/Admin/AdminPanelComposer.cs`, `AdminPanelEndpoints.cs` | `Host/Admin/Panel/…` |
| `OrderAdminPanelResidualArchitectureGuardTests` (×2) | `Host/Customer/CustomerPanelComposer.cs`; `IAdminSellerOrderCountPort` in Host composer | Host `Customer`/`Grid`/`Seller` folders absent; `IAdminSellerOrderCountPort` asserted **absent** from Host composer |
| `OrderCustomerPanelArchitectureGuardTests` | read `Host/Customer/CustomerPanel*.cs` | Host/Customer absent + `Program.cs` does not register the panel |
| `OrderSellerPanelArchitectureGuardTests` | read `Host/Customer/CustomerPanel*.cs` | Host/Customer absent |
| `OrderEndpointOrganizationGuardTests` | read `Host/Admin/AdminPanelEndpoints.cs` | read `Host/Admin/Panel/AdminPanelEndpoints.cs` |
| `OrderReservationCycleArchitectureGuardTests` | `IUnpaidOrderExpiryReconciler` must appear in a Host worker | Host worker file absent; `UnpaidOrderExpiryReconciler` + `IUnpaidOrderExpiryReconciler` asserted in `Tooba.Order.Infrastructure/ReservationCycle/` |

`IAdminSellerOrderCountPort` is an **Order.Contracts** port consumed by `Party.Infrastructure`
(`AdminSellersGridQueryEngine`) — it was never a Host composer dependency, so the guard now enforces that.

No guard was weakened: every change replaces an assertion against a deleted file with an equal-or-stricter
assertion about the evacuated state.

## Remaining red (deferred to W3)

- `OrderEndpointPresentationTests.Every_order_owned_error_code_has_an_explicit_descriptor`
- `OrderEndpointPresentationTests.Composed_catalog_resolves_order_and_shared_codes_without_duplicates`
- `OrderEndpointValidatorCoverageGuardTests.Manifest_covers_every_endpoint_reachable_request_exactly_once`

All three are error-catalog / validator-manifest concerns owned by W3.
