# SHIPPING-ONE-LEVEL-UI-128 — Multi-Group Payment Allocation Closure

Task: `TB-SHIPPING-ONE-LEVEL-TABS-128` (parent `TB-SHIPPING-ONE-LEVEL-TABS-127`)
Mode: `BOUNDED_MULTIGROUP_PAYMENT_DRAFT_ALLOCATION_CLOSURE`
Base SHA: `ef412bd0b8d9de132275a25cef9624bf61ce2f35`

## What was unblocked

TB-127 persisted per-group accepted-order shipping snapshots and summed the payable, but the
Payment draft still failed closed for `>1` group because Payment emitted exactly one `StoreShipping`
allocation pinned to the sentinel `PaymentAllocation.StoreShippingTargetId`. The user-visible
payment draft for a multi-method checkout could not exist.

## Owner-correct change (smallest additive representation)

| Layer | Change |
| --- | --- |
| `PaymentAllocationTargetKind` | new additive member `StoreShippingGroup = 2` |
| `PaymentAllocation` | `CreateStoreShippingGroup(...)` + `IsStoreShipping`; the per-group target must be a real `ShippingGroupId` and must NOT be the legacy sentinel |
| `Payment.Contracts` | new `PayableShippingGroupSnapshot(ShippingGroupId, SellerOrderId, Amount, Currency)`; optional trailing `ShippingGroups` on `PayableCheckoutSnapshot` |
| `PaymentDirectory.InitiateAsync` | shipping allocation rows resolved by `ResolveShippingAllocationRows`: one row per persisted group when groups exist, otherwise the legacy sentinel bucket |
| `OrderPaymentBridge.GetPayableAsync` | projects the accepted groups (seller order, group charge, currency) |
| `StorefrontCheckoutPage` | optional trailing `ShippingGroups` (reuses `StorefrontShippingGroupView`) |
| `StorefrontCheckoutService` | preview + submit now project the group breakdown; a multi-group checkout blanks the single method code/label instead of presenting one arbitrary method |
| `CartShippingDraft.AddShippingGroups` | multi-group draft clears the legacy scalar method code/label (projection only) |
| `StorefrontShippingService` | `ProjectPaymentDraftAsync` passes the draft group plans; the TB-126/127 `EnsureLegacySingleGroup` guard is removed (no longer needed by any path) |

## Accounting invariant

```
amount = Σ SellerOrder merchandise allocations + Σ StoreShipping/StoreShippingGroup allocations
```

- merchandise allocations are unchanged; seller totals are unchanged;
- shipping is counted **exactly once**: `ResolveShippingAllocationRows` refuses
  `payment.shipping_group.sum_mismatch` when Σ group amounts ≠ `payable.ShippingAmount`;
- duplicate group identity → `payment.shipping_group.duplicate`; empty identity →
  `payment.shipping_group.identity_required`; non-positive group amount →
  `payment.shipping_group.amount_positive`; all before any payment/allocation row is written;
- legacy single-method orders keep the sentinel `StoreShipping` bucket with the current
  `StoreShippingTargetId`, so historical readback and settlement filters are unchanged;
- group allocations are `StoreShippingGroup`, never `SellerOrder`, so they can never leak into
  `PaymentSucceededDomainEvent.SellerOrderIds`, the settlement bridge
  (`TargetKind == SellerOrder`), or seller payout.

No Settlement split, commission, or carrier-cost policy is invented here — the group allocation is
the *customer charge bucket*, not seller revenue.

## Explicitly not done

- Fulfillment M3 (group-scoped shipment execution) — untouched.
- Settlement M4 (allocation policy content) — untouched.
- Storefront UI/CSS/theme/template — untouched; the group readback is additive API data only.
- No new migration: `PaymentAllocationTargetKind` is stored as its integer via the existing
  allocation mapping, and no column shape changed.

## Proof

- `dotnet build src/backend/Tooba.slnx` → 0 errors.
- `Tooba.Payment.Tests` → **160 passed / 0 failed** (8 new `PaymentShippingGroupAllocationTests`).
- `Tooba.Order.Tests` → **338 passed / 0 failed**.
- Focused Host payment filter (`~Payment|~Paid`): 139 passed / 7 failed — all 7 verified
  **pre-existing at the base SHA** (source-scanning helpers that cannot resolve repo-relative paths
  from the test host); reproduced in a detached worktree at `ef412bd0b`.
