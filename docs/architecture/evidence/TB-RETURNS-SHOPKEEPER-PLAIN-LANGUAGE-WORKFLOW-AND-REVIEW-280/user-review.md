# TB-280 — for the owner: what to look at, and what is honestly not proven

Open (read-only data; do not press a submit button unless you want a real effect):

- queue: <http://127.0.0.1:3000/fa/admin/returns>
- case: <http://127.0.0.1:3000/fa/admin/returns/01a1eefb-ea96-7000-838d-9a0b37661c8b>

## What changed for you

1. **One next step.** The «قدم بعدی» card sits right under «وضعیت پرونده» with one big button and a sentence that says
   what pressing it does. Everything else is quiet, under «اقدام‌های دیگر».
2. **No empty review form.** This case has received nothing, so the only offered action is «ثبت دریافت کالا». The
   «بررسی مرجوعی» form appears only after goods have actually arrived.
3. **Plain words.** «برنامهٔ تفکیک»، «نسخهٔ تصمیم»، «نسخهٔ بررسی‌شده»، «فقط افزودنی» and the lock/storage explanations are
   gone from the working area. The real history is still there, collapsed, under «سابقه».
4. **No red wall on opening.** A dialog opens clean; a mistake is explained next to the field after you type, with a
   short fix («بیشتر از مقدار دریافت‌شده وارد نکنید.» / «دلیل باید دست‌کم ۳ نویسه باشد.»).
5. **The reason is required.** In «بررسی مرجوعی» the per-line reason is labelled الزامی, not اختیاری, and you cannot send
   without it — because the server refuses such a review anyway.
6. **Money stays honest.** «تأیید مرجوعی» records a decision and says so; «بازگشت وجه» is a separate button that shows the
   amount and the destination and asks again.
7. **Still understandable at 390px.** No horizontal page scroll; the product table keeps its own scroller; the primary
   button is a full-width 68px target.

## What I could NOT prove for you here

- The stored case has no received goods, so the «بررسی مرجوعی» dialog was proven on the real component with controlled
  HTTP and a fixture, not against stored data. No live POST, receipt or decision was sent (none is authorized).
- The mounted suites are jsdom: they prove behaviour, labels, validation and wire bodies, not visual geometry. The live
  check was DOM/text measurement at 1440 and 390 (overflow, sizes, order, visible copy) — no screenshots.
- Visual taste is yours. If a label, sentence or ordering reads wrong to a shopkeeper, say which screen and I will change
  exactly that.

## Honest state labels

- Technical-Validation: **PASS** (focused backend/API/quantity/mounted/total suites, `tsc`, production build).
- Operator-UX-Review-Readiness: **READY FOR YOUR LOOK** (T1–T8 evidence in `ux-before-after.md`; the served build carries
  the new copy).
- User-Acceptance: **NOT CLAIMED**.

The next step on the screen is not a new permission: it comes from the server's own work description for the stored
facts, so if the work stage changes, the offered action changes with it.
