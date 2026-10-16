# TB-SHIPPING-ONE-LEVEL-TABS-129 — authority and design (M3)

## Authority traced before coding

| Concern | Existing owner / seam |
| --- | --- |
| accepted order shipping groups | `Order.Domain.Checkout.OrderShippingGroup` + `OrderShippingGroupLine` (TB-127), materialized once inside `ICheckoutSubmitHost.PersistCheckoutAsync` |
| Order → Fulfillment handoff | `Tooba.Order.Contracts.Fulfillment.IOrderFulfillmentReader.GetHandoffAsync`, implemented by `OrderFulfillmentBridge` (Contracts-only, no cross-DbContext) |
| fulfillment creation | `FulfillmentPaymentSucceededHandler` → `FulfillmentDirectory.CreateFromPaidSellerOrdersAsync` (inbox-deduped) and `EnsureCreatedForPaidCheckoutAsync` |
| one unit per seller order | `FulfillmentUnit.SellerOrderId` unique index `ix_fulfillments_seller_order_id` |
| shipment creation / transitions | `FulfillmentUnit.CreateShipment`, `CancelShipment`, `AssignTracking`, `CorrectTracking`, `DispatchShipmentAsync`, `DeliverShipmentAsync`; seller seam `SellerMutateFulfillmentCommand` |
| persistence | `FulfillmentDbContext` schema `fulfillment`; raw-SQL additive migrations |

The task states the existing `sellerOrderIds` payment event carries no group data, so the group snapshot had to
travel on the **existing Order handoff seam** rather than through a new event or a new cross-module read.

## Design

```
accepted order                                Fulfillment (this task)
────────────────────────────────────────      ──────────────────────────────────────────────
order_shipping_groups (M2)                     fulfillment.shipping_groups   (new, additive)
  ShippingGroupId (stable identity)              same ShippingGroupId
  SellerOrderId / SellerPartyId                  SellerOrderId / SellerPartyId validation
  MethodCode/ServiceCode/OptionCode/Label         canonical identity snapshot (no rate)
  Minimum/Requested date, window, LeadDays        scheduling snapshot
  OriginContext = NULL                            OriginContext = NULL (never invented)
order_shipping_group_lines (M2)                items.shipping_group_id        (new, additive)
  (ShippingGroupId, OrderLineId)                  one line → exactly one group (bijective)
                                               shipments.shipping_group_id     (new, additive)
                                                 a shipment names its actual group
```

Boundaries respected:

- Order remains the identity/behaviour owner of the accepted grouping; Fulfillment stores only the
  handoff snapshot plus its own execution grouping. No Fulfillment DbContext is opened by Order and no
  Order DbContext is opened by Fulfillment.
- Payment/checkout/settlement code was not touched; the per-group customer charge buckets (TB-128) are
  unchanged and are still not seller revenue.
- Storefront/appearance is byte-identical: no frontend/CSS/template change and no API response shape
  change beyond additive optional fields.

## Fail-closed matrix (all before persistence)

| Situation | Code |
| --- | --- |
| group without stable identity | `fulfillment.shipping_group.identity_required` |
| group without seller / seller ≠ unit seller | `fulfillment.shipping_group.seller_required` / `…seller_mismatch` |
| group without method code / without lines | `fulfillment.shipping_group.method_code_required` / `…lines_required` |
| repeated group id in one unit | `fulfillment.shipping_group.duplicate` |
| line in two groups | `fulfillment.shipping_group.line_multiple` |
| line of this seller order with no group | `fulfillment.shipping_group.line_uncovered` |
| group references an unknown line | `fulfillment.shipping_group.line_unknown` |
| grouped order, shipment without group | `fulfillment.shipment.shipping_group_required` |
| foreign / unknown group on a shipment | `fulfillment.shipment.shipping_group_not_found` |
| shipment line belongs to another group | `fulfillment.shipment.shipping_group_mixed` |
| legacy order claiming a group | `fulfillment.shipment.shipping_group_not_applicable` |

## Idempotency

`CreateFromPaidSellerOrdersAsync` keeps its `PaymentInbox` event dedup and its
`Fulfillments.AnyAsync(x => x.SellerOrderId == …)` skip, so a replayed paid-order event produces no
second unit, no second group row and no shipment. `(fulfillment_id, shipping_group_id)` is unique as a
second durable guard. Shipments are never created as a side effect of reading or of opening
shipping/payment.

## Deliberately not done

- no Settlement M4 allocation policy, commission or carrier-cost invention;
- no automatic per-group shipment creation at payment time (explicit seller action remains the trigger);
- no change to admin bulk shipment semantics;
- no runtime migration execution against any database;
- no frontend work.
