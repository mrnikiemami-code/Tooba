# TB-SHIPPING-ONE-LEVEL-TABS-127 — M2 durable accepted-order shipping groups

Status: PASS (implementation) — runtime DB execution explicitly unproven.

## Scope actually delivered

M2 (accepted-order shipping groups) only. M3 Fulfillment, M4 Settlement, payment allocation
refactoring, shipping UI and tax fixtures were not touched.

## Files

New:

- `src/backend/Modules/Order/Tooba.Order.Domain/Checkout/OrderShippingGroup.cs`
  (`OrderShippingGroup`, `OrderShippingGroupLine`, `OrderShippingGroupFault`, `OrderShippingGroupPlan`)
- `src/backend/Modules/Order/Tooba.Order.Domain/Checkout/OrderShippingGroupMaterializer.cs`
- `src/backend/Modules/Order/Tooba.Order.Infrastructure/Persistence/Migrations/20261016100000_AddOrderShippingGroups.cs`
- `src/backend/Modules/Order/Tooba.Order.Tests/Storefront/OrderShippingGroupMaterializerTests.cs`

Changed:

- `CheckoutGroup` (owns `ShippingGroups`, freezes payable from group charges, clears legacy scalar
  method for genuine multi-group orders)
- `OrderDbContext` (DbSets + mappings + unique/index/FK rules)
- `CheckoutSnapshots.cs` (`SubmitCheckoutCommand.ShippingGroups`, `CheckoutShippingGroupSnapshot`,
  `CheckoutSnapshot.ShippingGroups`)
- `CheckoutDirectory.Reservations.cs` (authoritative cart-line → order-line map)
- `CheckoutSubmitHost.cs` (materialize + freeze groups inside the existing transaction, readback incl.)
- `CheckoutDirectory.cs` / `CheckoutDirectory.Access.cs` (read includes, snapshot projection)
- `StorefrontCheckoutService.cs` (pass group plans into submit)
- `StorefrontShippingService.cs` (build plans from the persisted draft, replay membership on
  revalidation, drop the conversion guard — the guard stays on the payment path)

## Invariants enforced

- Seller order/checkout ownership: each group carries its own `SellerOrderId`; the line/seller must
  match (`order.shipping.group.seller_mismatch`).
- Full coverage: every order line in exactly one group (`order.shipping.group.line.missing` /
  `.line.duplicate`).
- No invented mapping: missing cart-line → order-line entry aborts
  (`order.shipping.group.cart_line.mapping_missing`).
- No forged origin (`order.shipping.group.origin.unverified`).
- No duplicated seller+method group (`order.shipping.group.seller_method.duplicate`).
- Merchandise totals unchanged; shipping payable = Σ committed group charges, once.
- Legacy single-method path: empty plan list → no group rows, scalar columns unchanged.

## Proof run

- `dotnet build src/backend/Tooba.slnx` → 0 errors.
- `dotnet test .../Tooba.Order.Tests` → **338 passed / 0 failed** (9 new M2 tests + 329 pre-existing).
- Focused M2 filter → 9 passed.
- No frontend/CSS/theme/template/config edit; no shared :5088/:3000 or `tooba_alpha` mutation; no
  process start/stop; no DDL/DML against any database.

## Unproven

Runtime migration/EF validation on a real database and browser acceptance were not executed
(no isolated DB available inside the timebox). Claimed as unproven, not as passed.
