# TB-268 — user review (visual only, NO acceptance claimed)

You said the form is still too crowded for the operator («دقت کن کارمند با دیدین این فرم خیلی شلوغه براش»).
So the parcel area of that same page is now **tabbed**: one category at a time, with the counts written on the
tabs exactly as you asked («ارسال مجدد(1)`، `تحویل در مبدا(1)`»).

Nothing else about the page changed, and nothing about the order was touched.

## The one URL to review

```
http://127.0.0.1:3000/fa/admin/orders/01a1db2f-e36e-7000-b7d3-afb2fe86e81a/operations?fulfillmentId=01a1db2f-eb79-7000-b084-3d6b4add0fbb&shippingGroupId=01a1db2f-e351-7000-a516-71ab5b815d1c&from=queue
```

Your own Order B, read-only. The source parcel is still `Returned`, the linked resend is still `Delivered`,
the damaged decision and its links are still where they were.

## What to look at (short checklist)

1. **The strip under «مرسوله‌ها»** — «همه (۲)»، «دریافت‌شده در مبدأ (۱)»، «ارسال مجدد (۱)»، «تحویل‌شده (۱)».
   Only non-zero categories appear, so the strip is one line, not a wall.
2. **What opens first** — «ارسال مجدد» is already selected and only the resend parcel is on screen. If a parcel
   genuinely needs an action it wins instead, so the operator lands on what must be done.
3. **One category at a time** — click «دریافت‌شده در مبدأ» and only the source parcel remains; the badge number and
   the number of cards always agree.
4. **The parcel number never moves** — «مرسولهٔ ۱» is the source and «مرسولهٔ ۲» is the resend on every tab.
5. **Jumping between the two parcels** — the small blue chip «ارسال مجدد از مرسولهٔ ۱» (and «نمایش مرسولهٔ ۲»)
   takes you to the real counterpart card, switching to the tab that contains it. Nothing is duplicated.
6. **Keyboard** — Tab into the strip, then ← / → move the selection, Home / End jump to the ends, and the focus
   outline is visible.
7. **Phone** — at 390px the strip itself scrolls sideways and the page does **not**; the selected tab is always
   in view.
8. **Quiet defaults** — an order with one parcel shows no strip at all.

## Deliberately not changed

- No create / dispatch / deliver / return / refund / cancel / save / publish ran anywhere; your order was read only.
- Actions, permissions, dialogs, the reviewed envelope, retained attempts, the timeline, the recipient block and the
  finance-free shipping route are exactly as TB-267 left them.
- No backend, database, migration or configuration change.

Tell the architect what still reads badly. Nothing here is claimed as visually accepted.
