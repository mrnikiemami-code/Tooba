# TB-267 — operator copy changes (before → after)

Presentation and wording only. Every value keeps its server-projected meaning; nothing is invented, and no stored business name is rewritten.

## Structure of the sentence-level rewrites

| Where | Before (base `500c6583c`) | After |
| --- | --- | --- |
| Page title | «عملیات ارسال سفارش» as `text-2xl` | «عملیات ارسال» `text-xl`, order reference as secondary LTR line |
| Overview heading | «گروه یا بستهٔ هدف» | «هدف کاری» (the target may be a group, parcel or package) |
| Customer promise | inline run-on `تحویل مشتری: …` bold label + value in one flow | labelled `dt` «تحویل مشتری» with its own value line |
| Carrier promise | `موعد ارسال:` + `target.plannedDispatchSummary` (one long mixed sentence of date + zone + transport policy) | labelled `dt` «موعد ارسال حامل» + Jalali date + isolated LTR time window + short note `پذیرش حامل · ۲ روز کاری حمل` |
| Deadlines | `مهلتهای عملیاتی:` then three flushed sentences `آمادهسازی از ۲۰۲۶-…`, `حرکت به سمت حامل …`, `آخرین مهلت …` | dedicated block: header «مهلتهای عملیاتی · به وقت تهران», then three label/value rows «آمادهسازی از», «خروج به سوی حامل», «آخرین مهلت تحویل به حامل» with Jalali human instants |
| Time zone | raw `Asia/Tehran` beside timestamps | human label «به وقت تهران» derived from the existing zone id, shown once per surface |
| Tracking | `کد رهگیری: ` inline inside a dense line, no way to copy | «کد رهگیری» label, selectable LTR `bdi`, 44px «کپی» button acknowledging «کپی شد» |
| Missing tracking | «کد رهگیری ثبت نشده است.» | «ثبت نشده است» as a quiet value |
| Source → resend relationship | «ظرفیت سالم دریافتی این مرسوله در مرسولهٔ ارسال مجدد `01a1dc16…` مصرف شده است.» (raw id) | chip «ارسال مجدد در مرسولهٔ ۲ · تحویلشده», sentence «ظرفیت سالم این مرسوله در ارسال مجدد (مرسولهٔ ۲) مصرف شده است.» and an explicit `<details>` «پیگیری ارسال مجدد: مرسولهٔ ۲» for the technical code |
| Child parcel chip | raw id in prose | «ارسال مجدد از مرسولهٔ ۱» |
| Dangling separator | `مرسولهٔ ارسال مجدد · ` printed even with no fact after it | the exception line joins only facts that exist (`عدم تحویل ثبت شده · دریافت در مبدأ تأیید شده`), otherwise it is not rendered |
| Group method heading | `text-sm font-black` | `text-base font-semibold` |
| Fee hint | `text-[11px]` | `text-xs` (12px) |
| Recipient block | separate paragraphs: name, then «روش ارسال: …» | labelled rows «نام گیرنده», «موبایل» (LTR beside its label), «نشانی» (wrapping) |
| Target not found | two sentences run together | same honest two sentences, separated and readable |
| Empty states | bare `—` | fact-appropriate: «تاریخ تحویل نامشخص», «موعد ارسال مشخص نشده», «تاریخ تحویل ثبت نشده», «ثبت نشده» |

## Wording rules applied

- No architecture jargon reaches the operator: no `sourceRevision`, `policyVersion`, `occurrenceKey`, capture/snapshot vocabulary, and no raw IANA zone, ISO date or GUID in the visible copy (measured: 0 / 0 / 0).
- A readiness verdict is only asserted from facts the projection actually carries: «کار عملیاتی بازِ این گروه باقی نمانده است» (done) and «وضعیت مرسولههای این گروه نیازمند بررسی است» (blocked) are separate states; nothing claims delivery or obligation closure from a zero packed remainder.
- Returned source stays «دریافتشده در مبدأ», a resend stays «ارسال مجدد», damaged goods stay «نیازمند تعیین تکلیف». A courier return is only called a case where the server projected the linked case.
- The user's stored fixture names were left untouched: «TB264R2 تیپاکس اکسپرس آزمایشی», «TB266R1 گیرنده», «فروشگاه آرمان». Test-looking names in stored data are not a licence to rewrite business data.
- Incumbent feedback ownership is unchanged: one `react-toastify` toast on a confirmed action, the existing `MessageBox` for blocking/irreversible decisions, field errors adjacent, retained uncertain attempts still recoverable.

## Persian numbers, dates, ranges

`operational-format.ts` converts only presentation: ISO day → Jalali «۲۶ آبان ۱۴۰۵», ISO instant → «۲۴ آبان ۱۴۰۵، ۱۳:۰۰», `14:00–15:00` → `۱۴:۰۰–۱۵:۰۰` in an isolated LTR `bdi` with `tabular-nums`, `Asia/Tehran` → «به وقت تهران». Instants are never recomputed, so the accepted customer window and carrier deadlines printed on Order B are byte-for-byte the projected ones.
