# TB-277 — user review

## Review URL (primary, real exercised case)

http://127.0.0.1:3000/fa/admin/returns/01a1e6b0-f33a-7000-80ee-beffb6344490

Queue (operator surface):

http://127.0.0.1:3000/fa/admin/returns

## What you will see there

This is the case **you** already exercised after 276 (receipt 1 usable 1 → approval → successful refund). Its stored
state is now:

- وضعیت: **تکمیل شده**, تصمیم ثبت‌شده: **تأیید**, مبلغ: ۲٬۴۰۷٬۸۳۹ ریال
- یک دریافت ثبت‌شده و یک تلاش بازگشت وجه **موفق**
- تعیین تکلیف قلم: **برگشت به موجودی قابل فروش (۱)**
- وضعیت کار: **تسویه‌شده** — و به همین دلیل **هیچ دکمهٔ اقدام مرجوعی روی صفحه نیست**.

That is truthful, and it is also why this page **cannot** show the new dialogs: a settled case offers no review action,
and this task must not create a case, replay a refund or reset your history to manufacture one. The queue therefore reads
`۰ پروندهٔ باز` with `سابقه ۱`.

## What to check on this page

1. واقعی بودن داده‌ها: نام کالا («لنوو اولترابوک مدل 2»)، واحد («عدد»)، مقادیر درخواست/دریافت/سالم/معیوب با رقم فارسی.
2. «تعیین تکلیف» دیگر برچسب مبهم «باقی‌مانده» نیست؛ همان تصمیم ذخیره‌شده را می‌گوید.
3. تاریخ‌ها شمسی و بدون رشتهٔ خام ISO یا نام منطقهٔ زمانی؛ صفحه در ۱۴۴۰ و ۳۹۰ اسکرول افقی سند ندارد.
4. هیچ دکمهٔ «تأیید»، «شروع بازگشت وجه» یا «تعیین تکلیف» روی این پروندهٔ تسویه‌شده نمایش داده نمی‌شود (کار باقی نمانده).

## Where the new behaviour is proven

Because the only live case is settled history, the modal workflow is proven on the **real** `AdminReturnWorkspace`
component with a controlled HTTP harness, not by touching your case:

- every action opens exactly ONE design-system dialog and nothing is sent until you confirm;
- the receipt dialog shows the real product, the localized unit, «حداکثر قابل دریافت» and the unit's own step/precision;
  entering more than the remaining amount (e.g. ۵ against ۱) disables the confirm button and explains it **inside** the
  dialog, with no request leaving; «۱٫۲۵۰» grams is accepted as exactly 1.25;
- usable + damaged share one ceiling;
- «تأیید مرجوعی» sends only the decision; «شروع بازگشت وجه» is a separate dialog that says it executes money and travels
  to its own route;
- success uses the shared toast, a refusal uses the shared message box, and an invalid field stays in the dialog with its
  value.

## What you must NOT do

Do **not** press anything on this case to re-test the layout — there is nothing to press, and repeating the refund is not
authorized. No new case/order/payment/refund will be created for the review.

## Limits

No PSP/Wallet/Inventory effect was executed; no user visual/functional acceptance and no financial acceptance is claimed.
The dialogs' live appearance could not be opened on this settled case, so their proof is the mounted real component
(8/8) plus the code/route receipts.
