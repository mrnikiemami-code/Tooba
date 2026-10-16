# TB-SHIPPING-ONE-LEVEL-TABS-130 — evidence summary

Program: TOOBA-SHIPPING-CHECKOUT-DELIVERY-CLOSURE
Mode: BOUNDED_SETTLEMENT_SHIPPING_GROUP_LEDGER_M4
Worker: tooba-worker-01 / cursor / channel tooba-main
Expected base SHA: 3446b751545d76ef66797203975272d327454bac

## Result

PASS (bounded, additive, fail-closed). Settlement now persists the authoritative **known** per-group
customer shipping charge keyed by stable `ShippingGroupId` + payment/order/seller identity, with every
commercial allocation field explicitly UNSPECIFIED (`NULL`) and blocked from settlement/payout.

## Deliverables

| item | path |
| --- | --- |
| Settlement table + model | `src/backend/Modules/Settlement/Tooba.Settlement.Infrastructure/Persistence/SettlementDbContext.cs` |
| Migration | `.../Persistence/Migrations/20261016140000_AddSettlementShippingGroupFees.cs` |
| Domain aggregate | `src/backend/Modules/Settlement/Tooba.Settlement.Domain/Aggregates/ShippingGroupFeeAllocation.cs` |
| State/kind values | `.../Tooba.Settlement.Domain/ValueObjects/ShippingGroupFeeAllocationState.cs` |
| Recording path | `.../Tooba.Settlement.Infrastructure/Directories/SettlementDirectory.ShippingGroupFees.cs` |
| Contracts seam bridge | `.../Tooba.Settlement.Infrastructure/Bridges/SettlementShippingGroupBridge.cs` |
| Order contract | `src/backend/Modules/Order/Tooba.Order.Contracts/Settlement/OrderSettlementShippingGroupContracts.cs` |
| Order read bridge | `src/backend/Modules/Order/Tooba.Order.Infrastructure/Integrations/Settlement/OrderShippingGroupSettlementBridge.cs` |
| Payment contract | `src/backend/Modules/Payment/Tooba.Payment.Contracts/Settlement/PaymentSettlementContracts.cs` |
| Payment read bridge | `src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Adapters/PaymentSettlementBridge.cs` |
| Handler wiring | `.../Tooba.Settlement.Infrastructure/Handlers/SettlementEventHandlers.cs` |
| Focused tests | `src/backend/Modules/Settlement/Tooba.Settlement.Tests/Behavior/ShippingGroupFeeAllocationTests.cs` |
| Design note | `docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-130.md` |

## Tests (measured on this machine)

| command | result |
| --- | --- |
| `dotnet test src/backend/Modules/Settlement/Tooba.Settlement.Tests` | Passed 39/39 (9 new) |
| `dotnet test src/backend/Host/Tooba.Host.Tests --filter FullyQualifiedName~SettlementModuleAmsc001` | Passed 37/37 |
| `dotnet test src/backend/Host/Tooba.Host.Tests --filter FullyQualifiedName~MarketplaceSettlementEventPathTests` | Passed 2/2 (real PostgreSQL container; applies new migration) |
| `dotnet build Tooba.Settlement.Infrastructure` / `Tooba.Order.Infrastructure` | succeeded |

Migration/runtime: PROVEN for schema application (container-migrated) and for the recording path being
invoked from `payment.succeeded`; no production activation performed.

Pre-existing failure (NOT caused by TB-130):
`SettlementFoundationTests.Settlement_lifecycle_applies_commission_refund_and_payout_safety` asserts
`InvalidOperationException` from `RequestPayoutAsync` / `AdjustFromRefundAsync`, which throw the typed
`ContractOperationException`. Both the assertions (at `HEAD`) and those untouched code paths predate
this task; the only TB-130 edit to that file is the 3-line directory construction update.

## Allocation unknowns (explicitly blocked)

carrier payable cost, signed seller share, signed marketplace share, shipping discount/subsidy funding,
shipping tax, funder/adjustment. All remain `NULL` / `PendingAllocation`; no percentage, tariff, subsidy,
tax rate or payout entitlement was invented. Blocked codes:
`settlement.shipping_group.allocation_policy_missing`, `settlement.shipping_group.not_seller_revenue`.

## Rollback

Revert the code commit and drop `settlement.shipping_group_fees`
(`Down()` of `20261016140000_AddSettlementShippingGroupFees`). The table is additive, isolated and read
by no existing settlement path, so removal restores the previous behaviour exactly.

## Shared environment parity

`:5088` / `:3000` and `tooba_alpha` were read-only. No process stop/restart, no shared DB writes, no
frontend/UI change (storefront identical).

## Next blocker

Authorized, versioned commercial allocation policy (carrier cost, signed seller/platform shares,
discount funding, tax) is required before any group fee may leave `PendingAllocation`. Until then group
shipping fees cannot be settled or paid out by design.
