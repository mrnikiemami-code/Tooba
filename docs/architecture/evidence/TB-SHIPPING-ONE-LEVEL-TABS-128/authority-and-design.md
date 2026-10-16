# TB-SHIPPING-ONE-LEVEL-TABS-128 — multi-group payment allocation closure

Status: PASS — multi-group payment draft and initiation are unblocked with no invented financial policy.

## Files

New:

- `src/backend/Modules/Payment/Tooba.Payment.Tests/Behavior/PaymentShippingGroupAllocationTests.cs`

Changed:

- `src/backend/Modules/Payment/Tooba.Payment.Domain/ValueObjects/PaymentAllocationTargetKind.cs`
  (`StoreShippingGroup = 2`)
- `src/backend/Modules/Payment/Tooba.Payment.Domain/Aggregates/PaymentAllocation.cs`
  (`CreateStoreShippingGroup`, `IsStoreShipping`, per-group target validation)
- `src/backend/Modules/Payment/Tooba.Payment.Contracts/Checkout/OrderPaymentProjectionContracts.cs`
  (`PayableShippingGroupSnapshot`, optional `PayableCheckoutSnapshot.ShippingGroups`)
- `src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Directories/PaymentDirectory.cs`
  (`ResolveShippingAllocationRows`, exact-sum fail-closed)
- `src/backend/Modules/Order/Tooba.Order.Infrastructure/Integrations/Payment/OrderPaymentBridge.cs`
- `src/backend/Modules/Order/Tooba.Order.Application/Storefront/Models/StorefrontOrderModels.cs`
- `src/backend/Modules/Order/Tooba.Order.Application/Storefront/Services/StorefrontCheckoutService.cs`
- `src/backend/Modules/Order/Tooba.Order.Application/Storefront/Services/StorefrontShippingService.cs`
  (guard removed, draft groups passed to the payment draft)
- `src/backend/Modules/Order/Tooba.Order.Domain/Checkout/CartShippingDraft.cs`
  (multi-group draft no longer projects one arbitrary method)

## Focused proof (8 new tests)

1. one seller, two methods → two `StoreShippingGroup` allocations, targets = the two group ids, no sentinel row
2. two sellers, same method → two seller-scoped group allocations, Σ shipping = 22 000
3. legacy one-method order (no groups) → exactly one sentinel `StoreShipping` bucket, 2 allocation rows
4. repeat of the same idempotency key → same payment, 3 allocations (no extra shipping row), 1 payment row
5. duplicate group identity → `payment.shipping_group.duplicate`, zero payments/allocation rows
6. Σ groups ≠ aggregate shipping → `payment.shipping_group.sum_mismatch`, zero payment rows
7. empty group identity → `payment.shipping_group.identity_required`, zero payment rows
8. `CreateStoreShippingGroup` is never `IsSellerOrder` and refuses the legacy sentinel

## Results

- build `src/backend/Tooba.slnx`: 0 errors
- `Tooba.Payment.Tests`: 160/160
- `Tooba.Order.Tests`: 338/338
- Host focused payment filter: 139 passed, 7 failed — all 7 reproduced at the base SHA in a detached
  worktree (repo-relative source-scanning helpers), therefore pre-existing and unrelated.

## Not executed / unproven

- No EF/runtime DB validation of any kind was run (no isolated disposable Postgres); the change adds
  no migration and no column change, so no DDL delta is expected — unverified.
- No browser acceptance of the payment screen.
- Fulfillment M3 group execution and Settlement M4 allocation policy remain unimplemented and
  uncertified.
