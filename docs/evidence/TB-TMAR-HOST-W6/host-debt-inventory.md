# Host debt inventory — TB-TMAR-HOST-W6

## Axes
Write/behavior + Migration-risk.

## Selected write (pre-migration)
| File | Write | Risk | Notes |
|---|---|---|---|
| Admin/ShippingServiceEndpoints.cs Create/Update/Deactivate/EnsureSeed | DIRECT_DB_WRITE + BUSINESS_DECISION | SAFE_LOCAL_SLICE | Fulfillment-owned catalog; single SaveChanges; no cross-context ACID |

## Remaining live Host write sites (post-W6 scan)
| File | Write axis | Risk |
|---|---|---|
| Seller/SellerPanelComposer.cs | DIRECT_DB_WRITE | OWNERSHIP_AMBIGUOUS / residual |
| Admin/ProductWorkspaceComposer.cs | DIRECT_DB_WRITE | OWNERSHIP_AMBIGUOUS + source-size |
| Admin/HoldPolicySettingsEndpoints.cs | DIRECT_DB_WRITE | ORDER_WORKFLOW_ADJACENT |
| Admin/CheckoutAbuseSettingsEndpoints.cs | DIRECT_DB_WRITE | ORDER_WORKFLOW_ADJACENT |
| Admin/CheckoutIdentitySettingsEndpoints.cs | DIRECT_DB_WRITE | ORDER_WORKFLOW_ADJACENT |
| Admin/ReservationPolicy* | DIRECT_DB_WRITE | ORDER_WORKFLOW_ADJACENT |
| Admin/OrderInventoryRecoveryComposer.cs | DIRECT_DB_WRITE | ORDER_WORKFLOW_ADJACENT |
| Admin/OrderSupplyComposer.cs | DIRECT_DB_WRITE | ORDER_WORKFLOW_ADJACENT |
| Storefront/StorefrontShippingComposer.cs | DIRECT_DB_WRITE (draft/order path) | ORDER_WORKFLOW_ADJACENT; seed via CQRS |
| Storefront/CheckoutAbuseGate.cs / PendingPayment* / StorefrontComposer | DIRECT_DB_WRITE | ORDER_WORKFLOW_ADJACENT |
| *DevelopmentSeed / CatalogDemo / Template seeds | DIRECT_DB_WRITE | lower-value residual |
| Program.cs | FALSE_POSITIVE / bootstrap | n/a |

## Split
A. Write debt blocking modular readiness: residual Seller/ProductWorkspace ownership-ambiguous; checkout/order-adjacent settings.
B. Read-composition debt (not migrated): ShippingService List/Get/tree still FulfillmentDbContext reads; UoM List/Get; Grid engines.

JSON: host-debt-inventory.json
