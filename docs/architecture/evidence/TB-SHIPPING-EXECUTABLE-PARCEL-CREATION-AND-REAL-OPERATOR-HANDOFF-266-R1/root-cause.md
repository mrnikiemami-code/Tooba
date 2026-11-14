# Root cause of the reported packed-order dead end

Task: `TB-SHIPPING-EXECUTABLE-PARCEL-CREATION-AND-REAL-OPERATOR-HANDOFF-266-R1`
Diagnosed on the exact rejected page, read-only, before any change.

## Exact target the user reported

| Fact | Value |
| --- | --- |
| Page | `/fa/admin/orders/01a1daa2-519d-7000-bb9c-c4d0150a1971/operations?fulfillmentId=01a1daa2-5929-7000-98a4-32f0a306553e&from=queue` |
| Checkout | `01a1daa2-519d-7000-bb9c-c4d0150a1971` (`TB-20261114151713-01-cfbc30`) |
| Seller order | `01a1daa2-51a4-7000-97bf-7d06650b2b71` |
| Fulfillment | `01a1daa2-5929-7000-98a4-32f0a306553e` |
| Order line | `01a1daa2-51a8-7000-adb5-cb67d03d3047` — packed 1, processing 1, allocated 0 |
| Shipping method label | «ارسال پیش‌فرض فروشگاه» |
| Captured shipping groups | **0** |

Measured owning facts on the live runtime (authenticated admin route, no writes):

- `GET /v1/admin/orders/{checkout}/shipping-groups` → `groupCount: 0`, `groups: []`, `shipments: []`,
  `lineStatuses[0].operationalStatus = "Packed"`.
- `GET /v1/admin/orders/{checkout}/operations` → **`create_shipment` is advertised** in
  `lineCapabilities[0].bulkActionCodes` and `sellerCapabilities[0].shipmentCreationPossible = true`,
  `shipmentEligibleQuantity = 1`, `supplyStatus = "Reserved"`.
- Admin detail → `operationalDeliveryGroups` empty, `shippingGroups` empty, `quantityPacked = 1`.

## Why the screen was a dead end

Two independent facts collide:

1. **The backend projection is quantity/paid-based, not capture-based.** `create_shipment` is offered because
   the unit is paid, packed and has remaining quantity. It never asks whether an *accepted captured* group
   exists, so the operations API tells the operator "creation is possible".
2. **The delivery section can only open creation from an accepted captured group.** `AdminOrderOperationalDeliverySection`
   needs a `AdminAcceptedShippingGroup` to open `AdminCreateShipmentModal`. With zero captured groups it rendered
   only the generic amber strip «اطلاعات گروه ارسال این سفارش در دسترس نیست. وضعیت سفارش را دوباره بارگذاری کنید.»
   plus the unpack row action — exactly the screenshot the user sent.

The "reload" instruction was **false**: the group was never captured, so it can never appear on reload.

## Why no lawful legacy route exists for this order

`SellerMutateFulfillmentCommandValidator` requires a non-empty accepted group for creation:

```csharp
When(x => x.Kind == SellerFulfillmentMutationKind.CreateShipment, () =>
{
    RuleFor(x => x.ShippingGroupId)
        .NotNull()
        .Must(id => id is Guid g && g != Guid.Empty)
        .WithErrorCode(FulfillmentValidationCodes.ShippingGroupIdRequired);   // fulfillment.validation.shipping_group_id_required
});
```

There is therefore **no** executable legacy method/group route that could create a parcel for an order whose
checkout predates captured shipping groups. Exposing one would have to invent an accepted group, a method, a
customer window and a timezone — which the task forbids.

## Why the four TB266 orders have no captured group

They were checked out through the default storefront method («ارسال پیش‌فرض فروشگاه») without the captured-preview
selection path, so checkout never materialised `order.order_shipping_groups` rows. Evidence after this task:
`order_shipping_groups` = 2 while `checkouts` = 6 — the only two captured groups belong to the two coherent
trial orders created here through the public storefront pipeline (projection → captured selection → commit).

## Repair applied (responsible seam)

`src/frontend/app/admin/admin-order-operational-delivery-section.tsx` now distinguishes two different facts
inside the existing empty-projection branch:

| Situation | Copy | Test id |
| --- | --- | --- |
| No captured group **and** a packed line exists (permanent historical fact) | «این سفارش گروه ارسال ثبت‌شده ندارد؛ زمان ایجاد آن، انتخاب روش و بازهٔ تحویل مشتری ثبت نشده است. برای همین ایجاد مرسوله برای این سفارش ممکن نیست و بارگذاری دوباره هم آن را تغییر نمی‌دهد. آزمایش کامل ایجاد مرسوله را روی سفارشی انجام دهید که گروه ارسال ثبت‌شده دارد.» | `admin-order-operational-delivery-no-captured-group` |
| Genuinely transient empty projection (nothing packed yet) | existing «… وضعیت سفارش را دوباره بارگذاری کنید.» | `admin-order-operational-delivery-empty` |

No accepted group, snapshot, timezone or commit date is invented for an uncaptured order, and no unknown group is
routed to a seller fallback. The coarse target card from the operations route screen is unchanged and still says
«موعد ارسال مشخص نشده / بدون مرسولهٔ ساخته‌شده» — it now sits above an honest explanation instead of a false
reload instruction.

## Verified in a real browser after the change

On the same URL, the served production build renders the new strip and **not** the generic one, and the page has no
create button and no forward parcel action (`buttons: []` at the delivery section):

```
"admin-order-operational-delivery-no-captured-group":
"این سفارش گروه ارسال ثبت‌شده ندارد؛ زمان ایجاد آن، انتخاب روش و بازهٔ تحویل مشتری ثبت نشده است. برای همین ایجاد مرسوله
 برای این سفارش ممکن نیست و بارگذاری دوباره هم آن را تغییر نمی‌دهد. آزمایش کامل ایجاد مرسوله را روی سفارشی انجام دهید
 که گروه ارسال ثبت‌شده دارد."
"admin-order-operational-delivery-empty": null
```

See `browser-control-handoff.md` for the full browser sequence and `RESULT.md` for the deliverable facts.
