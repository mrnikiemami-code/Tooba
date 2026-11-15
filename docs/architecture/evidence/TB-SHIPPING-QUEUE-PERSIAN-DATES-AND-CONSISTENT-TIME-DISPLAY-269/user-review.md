# User review — TB-SHIPPING-QUEUE-PERSIAN-DATES-AND-CONSISTENT-TIME-DISPLAY-269

## Review URL

```text
http://127.0.0.1:3000/fa/admin/fulfillments
```

Your own route renders the same result: `http://127.0.0.1:3000/admin/fulfillments`

## What to look at

1. **«موعد ارسال»** — row TB-20261114173514-01-73cd8a (the parcel in transit) now reads
   `۲۴ آبان ۱۴۰۵ · ۰۸:۰۰–۲۰:۰۰ · به وقت تهران` instead of the raw `2026-11-15 … (Asia/Tehran)`.
   The tooltip carries exactly the same string.
2. **«مهلت‌های عملیاتی»** — `آماده‌سازی تا ۲۴ آبان ۱۴۰۵، ۱۳:۰۰ · خروج تا ۲۴ آبان ۱۴۰۵، ۱۸:۴۰ ·
   تحویل به حامل تا ۲۴ آبان ۱۴۰۵، ۲۰:۰۰ · به وقت تهران` — one zone label for all three instants,
   not the raw zone repeated three times.
3. **«تحویل مشتری»** — `۲۶ آبان ۱۴۰۵ · ۰۹:۰۰-۱۰:۰۰` (the customer promise keeps its own window).
4. Rows with no accepted plan still say `مشخص نشده` — nothing is guessed.

## What did not change

Sorting, filtering, the timing tabs and their counts, the 64px row height, the TB-268 parcel tabs, and every order fact.
The date *values* on the wire are unchanged; only how they are shown. Nothing was written to any order.

## Honestly not fixed

Nothing in this task is left open on the display side. If you still see any Gregorian date or raw `Asia/Tehran`
anywhere in the fulfilment queue, that is a defect I did not find — say which column and I will look again.
