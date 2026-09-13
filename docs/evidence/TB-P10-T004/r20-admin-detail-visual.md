# R20 Admin Order Detail visual

URL: `/fa/admin/orders/01a098eb-7669-7000-9fc2-1654986b455b`

- «وضعیت تأمین سفارش» separate from «رزرو موجودی».
- Reservation: max used 2/2, retry remaining 0, Persian retry-limit sentence.
- Timeline: چرخه #1 / #2, store · 1 دقیقه · max 2, شروع/پایان/درخواست رزرو مجدد — no raw cycle enums.
- Supply: «رزرو قبلی فعال نیست، اما موجودی لازم در حال حاضر قابل تأمین است.»
- History list readable.
- Pre-existing payment history still shows `GATEWAY_REJECTED` on the operations timeline (not parsed by reservation UI; known R19 note family). Not used as business logic.
