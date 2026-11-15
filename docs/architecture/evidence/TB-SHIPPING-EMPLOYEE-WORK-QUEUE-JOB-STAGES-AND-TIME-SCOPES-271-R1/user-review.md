# TB-271-R1 — user review

## Review URL (one)

`http://127.0.0.1:3000/fa/admin/fulfillments`

The same screen is also reachable at `http://127.0.0.1:3000/admin/fulfillments`.

## What to look at

1. **Stage strip** — one row of five work stages plus the quiet «سابقهٔ ارسال» tab. On the current data:
   همه ۴ · آماده‌سازی و بسته‌بندی ۲ · آمادهٔ ارسال ۱ · تحویل به حامل ۰ · پیگیری تحویل ۱ · برگشتی‌ها ۰.
2. **Time scope** — امروز (default) / آینده / همه on its own line: ۴ / ۰ / ۴. The archive tab additionally disables
   this control because history has no time dimension.
3. **Archive isolation** — the «سابقهٔ ارسال» badge stays ۱ while you switch between the active tabs, and opening the
   archive shows exactly one row with «۱ ردیف در این نمایش» and ۰/۰/۰ time badges. The archive is never added to an
   active total.
4. **Default order** — a clean entry sends no client sort, so the server's own order runs (overdue → due today →
   undated → future). Sorting any column still works and wins over the default.
5. **Honest undated work** — the summary reads «۴ ردیف در این نمایش · ۴ ردیف بدون موعد مشخص», and each row names the
   meaning of its own time («موعد ارسال مشخص نشده» / «پیگیری جاری؛ مهلت مشخصی ثبت نشده») instead of a guessed date.
6. **Mobile** — at 390px there is no horizontal page overflow and the stage/scope tabs are 44px.

## Read-only in this round

The review was performed read-only: GET page loads, read-only queue queries and DOM/text measurements. No business
action (create/dispatch/deliver/return/refund/cancel/save/publish) was performed, no order/parcel/plan/hold/setting was
written, and no screenshot was taken.

## Known limits of this review

- The live set is **entirely undated**: three legacy groups with no accepted plan and one dispatched parcel whose
  delivery follow-up stores no SLA. Live dated ordering, cutoff equality, independent time zones and the future scope
  are therefore proven by the focused tests, not by live data. No example row was manufactured.
- The archive currently holds a single row, so tab-invariance of the archive badge is proven by the live responses plus
  the focused tests.
- 25 pre-existing 36px grid toolbar buttons (advanced filter / columns / exports / saved view) are still under the 44px
  target; that is a pre-existing shared-grid surface and was not restyled here.
- This is a worker pass awaiting Architect review; no user visual/functional acceptance is claimed.

`STOP_FOR_ARCHITECT` · `automaticNextImplementationTask: NONE`
