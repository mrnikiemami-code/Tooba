# IMPECCABLE REVIEW (R2) — evidence-based (رندرشده، نه چک‌لیست)

شواهد: `evidence/audit-r2-result.json` + اسکرین‌شات‌های `mockups/r2-*.png` (Chromium 1161, 1440×960 + موبایل 360×740).

## نتیجهٔ سنجش واقعی
- وابستگی کنترل‌ها: depsLockedWhenEmpty/depsEnabledAfterSelect/serviceDisabledWhenUnset/serviceEnabledAfterResp/
  retWindowHiddenWhenNone/unitReadonly/warehouseLocked/historyHiddenOnCreate/errorShown = **همه true**.
- وضعیت خرید: draftNotBuyable=true، activeBuyable=true (جدا از وضعیت موجودی).
- هم‌پوشانی نوار عملیات با فیلدها: **overlap=0**.
- خوانایی: کمینه فونت **13px**؛ کنتراست متن **16.4:1**، muted **6.99:1** (هر دو ≥ AA).
- خطا/موبایل: حالت خطای بخش و نمای 360px موجود و قابل استفاده.

## انطباق با ایرادات گزارش‌شده
طول/اسکرول ↓ (گرید ۲ستونه + پیشرفته جمع‌شده)، فاصلهٔ اضافی ↓ (عرض 1200/pad24)، فشردگی ↓ (فاصله/پدینگ)،
نوار روی فیلد حذف (overlap 0)، تک‌دکمهٔ ذخیره، کنتراست/فونت اصلاح، جدول‌های داخلی خواناتر، اولویت primary/ghost،
اکشن پرتکرار در هدر/ردیف، تفکیک ضروری/پیشرفته، حذف اصطلاح فنی.

## ایراد باقی‌ماندهٔ مسدودکننده
هیچ. موارد «افزودن موقعیت انبار» و «افزودن تخفیف» صادقانه «نیازمند تکرار قرارداد بک‌اند» علامت زده شده و به‌عنوان
قابلیت آماده معرفی نشده‌اند (مطابق معیار صحت).
Verdict: **APPROVED** (بدون ایراد مسدودکننده) بر پایهٔ خروجی رندرشده.
