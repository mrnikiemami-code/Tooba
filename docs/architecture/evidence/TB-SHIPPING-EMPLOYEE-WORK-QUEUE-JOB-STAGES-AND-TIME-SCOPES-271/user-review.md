# TB-271 — user review

Route (one link): **http://127.0.0.1:3000/fa/admin/fulfillments**
(the English-prefixed route `http://127.0.0.1:3000/admin/fulfillments` renders the same screen.)

## What you should see now

1. **One** row of job tabs with Persian counts: همه · آماده‌سازی و بسته‌بندی · آمادهٔ ارسال · تحویل به حامل ·
   پیگیری تحویل · برگشتی‌ها. The old second row (نیازمند اقدام / موعد آماده‌سازی / …) is gone.
2. A clearly secondary time control: **امروز** (default) · آینده · همه.
3. A quiet, separate **سابقهٔ ارسال** tab for finished work — it is not one of the active job tabs.
4. Each row: the parcel in human form (`مرسولهٔ ۱ از ۱`), the exact next verb (ایجاد مرسوله / بسته‌بندی / ثبت تحویل …),
   and the time that belongs to that job with its own label (آخرین مهلت شروع آماده‌سازی / آخرین مهلت خروج به سمت حامل /
   آخرین مهلت تحویل به حامل). Ongoing delivery/return follow-up is labelled
   «پیگیری جاری؛ مهلت مشخصی ثبت نشده» — no invented deadline.
5. Work with no plan/date is **not hidden**: it stays in امروز and is counted separately as «۴ ردیف بدون موعد مشخص».
6. The single action is still the existing «عملیات ارسال» link, which opens the same operational detail and returns you
   to the same stage/time selection. The queue adds no new buttons and changes nothing in the order.

## Current data on this environment (honest)

- 4 active rows: آماده‌سازی ۲ · آمادهٔ ارسال ۱ · تحویل به حامل ۰ · پیگیری ۱ · برگشتی ۰.
- امروز ۴ · آینده ۰ · همه ۴ · سابقهٔ ارسال ۰.
- All 4 rows are undated (they have no accepted dispatch plan of their own), so no live row exercises a dated
  preparation/departure/handoff deadline. The dated ordering and the future scope are proven by the focused tests,
  not by this data.
- No live returned/resendable row exists right now, so برگشتی‌ها is 0.

Clicking tabs/time only changes what you read; nothing is sent except the read query, and no planning or confirmation
state is cleared.
