# user-review.md — TB-270-R3

## Review URL (history-only, read this first)

`http://127.0.0.1:3000/fa/admin/orders/01a1db2f-e36e-7000-b7d3-afb2fe86e81a/operations`

Served by build `iXZVso1qBFzB4wDa0huHP` on port 3000 against Host `5088` (both restarted after this change).

**This page is completed read-only history, not an eligible unplanned example.** On the canonical database:

- no live `order.shipment_dispatch_plans` row was created by this increment (relational writes rolled back);
- the only linked resend parcel in the data is `Delivered`, which is not plannable by design.

So the page shows the stored history and offers **no** «زمان‌بندی ارسال» button. No order, parcel, hold, reservation or
plan was manufactured to make one appear.

## What the same build does on a genuinely eligible parcel

Proven by the mounted suite and owning-application cases, not by a live row:

1. a `Created`, unplanned resend/replacement parcel states «موعد ارسال مشخص نشده» and offers exactly ONE
   «زمان‌بندی ارسال» button (44px, existing card, one action owner);
2. the preview shows the reviewed context and the feasible customer dates/slots with their own preparation-start and
   carrier-handoff deadlines — or a typed blocker with **no** calendar when the supply cannot be proven;
3. nothing is preselected; the operator chooses a date, then a slot;
4. confirming sends the server-issued review fingerprint; if the parcel/decision/hold/capture changed since the
   preview, the answer is a refusal (`order.shipment_plan.review_context_changed`) instead of a silent replan;
5. an expired slot is refused, never clamped — the retained intent stays visible so the operator can make a deliberate
   new review;
6. an unknown/timeout outcome keeps the exact request frozen and retries those same bytes; a late response for a
   superseded attempt cannot overwrite the newer surface;
7. after create/recover, the plan surface opens for the **exact** created parcel; a stock-pending decision, a referral,
   or a completed replay opens **no** calendar;
8. until a plan exists, «ارسال» is absent and the server refuses dispatch with `fulfillment.dispatch.plan_required`
   (single, package and bulk paths).

## Not claimed

No user visual/functional acceptance, no carrier booking, no provider/waybill, no customer notification or consent, no
new money fact, no universal "shipping complete" claim.
