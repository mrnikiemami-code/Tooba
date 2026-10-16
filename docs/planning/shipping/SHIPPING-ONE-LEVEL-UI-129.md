# SHIPPING-ONE-LEVEL-UI-129 — Fulfillment Group-Scoped Shipment Execution (M3)

Task: `TB-SHIPPING-ONE-LEVEL-TABS-129` (parent `TB-SHIPPING-ONE-LEVEL-TABS-128`)
Mode: `BOUNDED_FULFILLMENT_GROUP_SCOPED_SHIPMENT_EXECUTION_M3`
Base SHA: `8261804c6e5976440099e5c13e3f2bc59daf9c5e`

## Problem

TB-127 persisted accepted `OrderShippingGroup` / `OrderShippingGroupLine` rows and TB-128 paid them
per group, but Fulfillment still had no concept of a shipping group: the Order→Fulfillment handoff
(`OrderFulfillmentHandoffSnapshot`) exposed only `ShippingMethodCode/Label` plus lines, and
`Shipment` was always ungrouped. A seller order with two accepted groups could therefore not be
executed as two real shipments, and nothing prevented a shipment from being built out of lines that
belong to different accepted groups.

## Owner-correct change (smallest additive seam)

| Layer | Change |
| --- | --- |
| `Order.Contracts.Fulfillment` | new `OrderFulfillmentShippingGroupSnapshot(ShippingGroupId, SellerOrderId, SellerPartyId, MethodCode, ServiceCode, OptionCode, MethodLabel, OriginContext, MinimumDeliveryDate, RequestedDeliveryDate, RequestedDeliveryTimeWindow, LeadDays, OrderLineIds)`; optional trailing `ShippingGroups` on `OrderFulfillmentHandoffSnapshot` |
| `OrderFulfillmentBridge` | loads the accepted groups of **that SellerOrder only** (`order_shipping_groups` + `order_shipping_group_lines`) and projects them; legacy orders keep `ShippingGroups = null` |
| `Fulfillment.Domain` | new `FulfillmentShippingGroup` aggregate (**Fulfillment-owned**), `FulfillmentShippingGroupPlan` transfer record, `FulfillmentShippingGroupFault` stable codes |
| `FulfillmentItem` | additive `ShippingGroupId` — one order line belongs to exactly one accepted group |
| `Shipment` | additive `ShippingGroupId` — the shipment names its actual group |
| `FulfillmentUnit` | `ShippingGroups` collection + `AttachLoadedShippingGroups`; `CreateFromPaidOrder` resolves membership (`ResolveShippingGroupMembership`) with fail-closed coverage/seller checks; `CreateShipment` validates group scope before creating |
| `Fulfillment.Infrastructure` | additive migration `20261016120000_AddFulfillmentShippingGroups` (`fulfillment.shipping_groups`, `items.shipping_group_id`, `shipments.shipping_group_id` + partial indexes); creation paths persist groups in the same transaction; read model exposes group + membership |
| Seller API | wire-only `FulfillmentCreateShipmentRequest.ShippingGroupId` → `SellerMutateFulfillmentCommand.ShippingGroupId` → `IFulfillmentDirectory.CreateShipmentAsync(..., shippingGroupId)` |
| Guard bookkeeping | `FulfillmentModuleAmsc001W3CertGuardTests` migration allowlist gains the one new additive migration file (no entry removed) |

## Invariants kept

- **one `FulfillmentUnit` per `SellerOrder`** — unchanged; groups live *inside* a seller unit and can
  never merge two sellers;
- membership is **bijective**: every order line is covered exactly once, a line can never appear in
  two groups, and a group may not reference a line this seller order does not own;
- group identity is the **stable `ShippingGroupId`** from the accepted order — never a label, never a
  browser value;
- the seller of every group must equal the unit's `SellerPartyId` (`fulfillment.shipping_group.seller_mismatch`);
- shipment group scope is enforced before any row is written:
  `…shipment.shipping_group_required` (grouped order without group),
  `…shipping_group_not_found` (foreign/unknown group),
  `…shipping_group_mixed` (line from another group),
  `…shipping_group_not_applicable` (group claimed by a legacy ungrouped order);
- replay of `payment.succeeded.v1` stays idempotent via the existing inbox and the
  `SellerOrderId` uniqueness check, so no duplicate fulfillment, group row, or shipment is produced;
- no shipment is created by merely opening shipping/payment; shipments are still created explicitly;
- `OriginContext` stays `NULL` (no trusted fulfilment-origin authority — never invented);
- **no frontend/UI/CSS/theme/template change at all.**

## Decision recorded

A group-scoped shipment keeps using the existing seller-provided carrier/method fields
(`CarrierDisplayName`, `ShippingMethodCode/Label`, provider metadata) plus its new
`ShippingGroupId`; the accepted canonical method identity is carried by the Fulfillment-owned group
record, so the two cannot silently diverge. Auto-copying the group `MethodCode` into the shipment
would re-trigger provider-metadata validation (`fulfillment.shipping.post.recipient_required` when
metadata is absent) and would silently fabricate a booking context — explicitly out of scope.

## Proof

- `dotnet test Tooba.Fulfillment.Tests` → **173 passed / 0 failed** (10 new
  `FulfillmentShippingGroupExecutionTests`).
- `dotnet test Tooba.Order.Tests` → **338 passed / 0 failed**.
- `dotnet test Tooba.Host.Tests --filter FulfillmentModuleAmsc001W3CertGuardTests` → **14 passed**.
- `ARCH-SIZE-001`: `FulfillmentDirectory.cs` = 746 LOC (ceiling 800); the read model and the group
  write seam moved into cohesive partials `FulfillmentDirectory.ReadModels.cs` /
  `FulfillmentDirectory.ShippingGroups.cs`.

## Not done (out of authorized scope)

- Settlement M4 allocation policy — untouched.
- Automatic per-group shipment creation at payment time — not implemented; creation stays an explicit
  seller action (the bounded atomic association layer is what M3 authorizes).
- Runtime DB migration execution against a disposable database — not exercised; DDL is additive
  (`IF NOT EXISTS`) and unproven at runtime is disclosed as such.
- Frontend / storefront UI — untouched.
