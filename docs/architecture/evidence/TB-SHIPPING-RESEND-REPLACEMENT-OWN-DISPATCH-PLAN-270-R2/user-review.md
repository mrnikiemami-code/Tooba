# user-review.md — TB-270-R2

## The one review URL

`http://127.0.0.1:3000/fa/admin/orders/01a1db2f-e36e-7000-b7d3-afb2fe86e81a/operations`

That is the existing operational order surface of the exercised order (TB-266-R1 Order B), served by the new build
`jdEuO1DKPbRL6rVo1vqUG` on port 3000 against Host `5088`.

## Truthful state of that page — read this before opening it

**It is read-only completed history, not an eligible unplanned example.** Checked on the canonical database at
2026-11-15 (read-only SQL, no writes):

- `order.shipment_dispatch_plans` → **0 rows** (the worker deliberately created no live plan);
- the only linked resend/replacement parcel in the data is
  `01a1dc16-482d-7000-8d64-53b199873504` (source `01a1dc15-b0b7-7000-9118-953a3f6e92d2`), status **`Delivered`**.

A `Delivered` parcel is not plannable by design (`order.shipment_plan.not_created`), so this page will show the stored
history and offer **no** «زمان‌بندی ارسال» button. Nothing was manufactured to make the button appear, and no demo order,
parcel, plan, hold or reservation was created to satisfy a handoff URL.

## What a real eligible page will show (same build, proven by mounted tests)

On an order whose resend/replacement parcel is `Created` and unplanned:

1. the parcel card states it has no confirmed promise («موعد ارسال مشخص نشده») and shows exactly one
   «زمان‌بندی ارسال» button (44px, existing card, no second dialog owner);
2. pressing it loads the preview: the exact parcel/binding context, the readiness source, and the feasible customer
   dates/slots **with their own carrier-handoff and preparation-start deadlines**;
3. nothing is preselected; the operator must choose a date, then a slot;
4. confirming writes the durable reviewed envelope **before** the POST; success refreshes the owning read models and the
   card then shows the stored promise (customer date + window, «تحویل به حامل تا …» in the carrier zone,
   «شروع آماده‌سازی تا …»);
5. a refusal keeps the exact envelope (same occurrence, same selection) so a retry/reload resumes the SAME reviewed
   intent, and a stock-pending replacement shows an honest blocker with **no** calendar;
6. until that plan exists, «ارسال» is absent from both the order card and the queue row, and the server refuses dispatch
   with `fulfillment.dispatch.plan_required`.

## Persian refusal copy the operator will see

«ابتدا زمان‌بندی این مرسوله را تأیید کنید؛ تا آن زمان ارسال مجاز نیست و موجودی مصرف نمی‌شود.» plus the specific cause
(origin receipt missing, replacement hold not durable, captured inputs incomplete, no feasible date, selection expired,
another operator's plan already confirmed …), all mapped in `admin-error-map.ts`.

## Not claimed

No user visual/functional acceptance, no carrier booking, no provider/waybill, no customer notification or consent, no
new money fact, and no universal "shipping complete" claim.
