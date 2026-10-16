# SHIPPING-ONE-LEVEL-UI-127 — M2 Durable Accepted-Order Shipping Groups

Task: `TB-SHIPPING-ONE-LEVEL-TABS-127` (parent `TB-SHIPPING-ONE-LEVEL-TABS-126`)
Program: `TOOBA-SHIPPING-CHECKOUT-DELIVERY-CLOSURE`
Mode: `BOUNDED_ORDER_ACCEPTED_SHIPPING_GROUPS_M2`
Base SHA: `7dfac497c2ca337f90746d662a2d32fd89cc7c98`

## Authority

- `docs/architecture/checkout/SHIPPING-METHOD-GROUPING-UI-CONTRACT.md`
- `docs/planning/shipping/SHIPPING-CHECKOUT-DELIVERY-CLOSURE-PLAN.md`
- `docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-125.md`
- Architect ruling in the task file (TB-127)

## What M2 adds (Order-owned only)

| Concern | Decision |
| --- | --- |
| Durable tables | `order.order_shipping_groups`, `order.order_shipping_group_lines` (additive) |
| Identity | `ShippingGroupId` carried from the M1 draft verbatim (`OrderShippingGroupPlan.ShippingGroupId`) |
| Ownership | `CheckoutId` + `SellerOrderId` + `SellerPartyId`; group must belong to the seller order of its own seller |
| Membership | one whole `OrderLineId` per row, `UNIQUE(order_line_id)` => bijective, no quantity split in v1 |
| Origin | always `null`; any non-empty origin plan is refused with `order.shipping.group.origin.unverified` (`BLOCKED_ORIGIN_AUTHORITY`) |
| Eligibility | unchanged canonical cart-wide eligibility; per-seller eligibility is **not** claimed |
| Charge | group snapshot from the M1 draft (server-computed); client money never accepted |
| Payable | `checkout.ShippingAmount = Σ group charges`, computed once in `CheckoutGroup.Submit` |
| Legacy scalars | retained for historical reads; a genuine multi-group order clears `shipping_method_code/label` instead of advertising one arbitrary method |
| Extra guard | removed from `StorefrontShippingService.CommitAsync` (order conversion now persists groups); **kept** in the payment-draft path |
| Settlement policy | not touched (no fabricated shares) |
| Fulfillment / UI / tax | not touched |

## Conversion boundary

`CheckoutProcessManager.SubmitAsync` already wraps reservation + order persistence in a
`TransactionScope(ReadCommitted)`. `PersistCheckoutAsync` materializes and freezes the accepted
groups **inside** that transaction, before `SaveChangesAsync`; any invariant violation throws
`ContractOperationException` with a stable `OrderShippingGroupFault` code before any row is written.

Cart-line identity is captured at the exact moment the order lines are created
(`CheckoutDirectory.QuoteSellerOrdersAsync` fills `_orderLineIdByCartLineId`); membership is never
inferred positionally. `QuoteSellerOrdersAsync` clears the map on every call.

## Idempotency

Retries are absorbed before persistence: `FindExistingCheckoutAsync` matches by `IdempotencyKey` or
`CartId`, and the persisted checkout process milestone short-circuits a second submit. No second set
of groups/charges can be produced for the same cart.

## Known limitations (unproven here)

1. **No runtime DB proof.** No isolated Postgres was started inside the 45-minute box, so the
   migration/EF model was not exercised against a real database. Domain, mapping, build and
   unit-level evidence only.
2. Draft group ids are regenerated on each draft re-save (`CartShippingGroupPlanner` factory); the
   identity carried into the order is the identity of the last persisted draft, not of the very first
   selection.
3. Per-seller method eligibility remains cart-wide (`BLOCKED_ORIGIN_AUTHORITY` /
   `per-seller eligibility` still open).
4. Payment still expects one `StoreShipping` allocation; the payment-draft boundary therefore still
   fails closed for >1 group until its owner is updated.
