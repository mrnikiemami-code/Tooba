# TB-275 — user review

## What to look at

1. Queue: `http://127.0.0.1:3000/fa/admin/returns`
2. Honest empty case state (no live case exists):
   `http://127.0.0.1:3000/fa/admin/returns/00000000-0000-0000-0000-000000000000`

## What you will see right now

- One strip of counted work selections: «همهٔ کارهای باز», «دریافت کالا», «بررسی و تصمیم», «پیگیری بازگشت وجه»,
  «بازگشت وجه ناموفق», «تعیین تکلیف کالا», and a quieter «سابقه». Every count is the server's own pre-paging count.
- The queue is **empty** (`۰` on every strip, «ردیفی برای نمایش نیست», «نمایش ۰ تا ۰ از ۰ ردیف») because the canonical
  database has no return case at all. This is the truthful state, not a bug.
- The detail route of an unknown case shows «این پروندهٔ مرجوعی یافت نشد.» with «بازگشت به صف کار» — not an endless
  loading state.
- Old bookmarks still work: `?queueFilter=refund_needed` opens «پیگیری بازگشت وجه» and the URL becomes
  `?workCategory=refund` as soon as you pick a strip.

## What this build does NOT prove to you

- There is **no live return case**, so you cannot click a real receive/approve/reject/refund here yet. The actionable
  flow is proven by the mounted/controlled tests only. Do not read the empty queue as "the actions work in production".
- No money, no provider, no stock, no waybill was touched. No demo order/case/receipt was created on purpose.
- The shared 36px grid toolbar buttons are historical and were intentionally not restyled; only the controls introduced
  here are 44px.

## Suggested check when a real case exists

Open the case from its row («بررسی مرجوعی»), then in this order: «ثبت دریافت کالا» (type the real delivered quantity per
line, choose سالم/معیوب explicitly — nothing is pre-filled), then «تأیید مرجوعی» or «رد مرجوعی» (with a real reason), and
finally «پیگیری بازگشت وجه» or «تلاش دوبارهٔ بازگشت وجه» only when the stage offers it. Every button appears only when
the server says it is executable, and each action asks for confirmation before it is sent.

Worker PASS is not Architect ACCEPT and is not your functional acceptance.
