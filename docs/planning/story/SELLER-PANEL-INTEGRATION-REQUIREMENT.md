# CURRENT — existing seller integration retained; TB-282-R3 worker PASS awaiting review — 2026-11-19

TB-282-R3 worker PASS closed S1/S2 on the existing VendorShell/story.view/shared editor path. No parallel panel/menu/login. Await Architect/user review; automaticNextImplementationTask NONE.

# HISTORICAL — superseded by CURRENT above

Existing VendorShell/story.view integration remains mandatory. Current task TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R2 finishes actual recovery, lawful lookup/date UI, scoped grid/shell and served proof. No duplicate panel/menu/login. Historical status blocks below do not issue tasks. Await R2 result; automaticNextImplementationTask NONE.

# HISTORICAL — superseded by CURRENT above

Only issued task TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R1. Original integration requirements below remain binding. Complete real VendorShell behavior and mounted proof; regex checking the page import is not acceptance. Shared feature within existing seller shell/auth, no duplicate menu/panel/login. Prior task/status paragraphs below are historical. Await R1 result; automaticNextImplementationTask NONE.

# HISTORICAL — superseded by CURRENT above

The existing seller-panel requirement below remains binding. Current task is TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282. See its exact TASK.md and top recovery. Historical R2 active / UI NOT ISSUED statements below record the original note only; 282 now explicitly authorizes the shared UI after its Gate0. No duplicate panel/menu/login; one worker, await result. automaticNextImplementationTask NONE.

# الزام اتصال استوری به پنل فروشندهٔ موجود قالب

تاریخ: ۱۹ نوامبر ۲۰۲۶
وضعیت: دستور صریح صاحب محصول؛ فقط یادداشت و بازیابی به‌روز شد، پیاده‌سازی جدید صادر نشد.

## خواسته

صاحب محصول تأکید کرد پنل Seller در قالب خریداری‌شده موجود است و استوری باید به همان پنل اضافه/تکمیل شود. پنل فروشندهٔ مستقل، پوستهٔ تازه، ورود جداگانه یا کپی پنل ادمین ساخته نشود.

## تأیید از سورس

مبنای بررسی: `73202686f10ee4bceeb56fc4a2e43d0e74d8f949`.

- `src/frontend/app/vendor-panel/layout.tsx`: پوستهٔ موجود را با `VendorShell` دور children می‌گذارد؛ `data-panel-theme="seller"` و `seller-panel-theme-root`.
- `src/frontend/app/vendor-panel/vendor-shell.tsx`: پوستهٔ فروشنده با هندسهٔ Shopeiva، header/sidebar/drawer و زمینهٔ Actor+Seller موجود است.
- همان فایل، ردیف منوی `stories` با عنوان «استوری‌ها»، مسیر `/vendor-panel/stories` و `viewPermission: "story.view"` از قبل دارد؛ هنگام تکمیل، منوی تکراری اضافه نشود.
- `src/frontend/app/vendor-panel/stories/page.tsx` از قبل `StoryManagementScreen` مشترک را با `SELLER_STORY_CAPABILITIES` نمایش می‌دهد.
- `src/frontend/app/vendor-panel/page.tsx` داشبورد فعلی فروشنده است؛ برای پیاده‌سازی استوری به بازنویسی داشبورد نیاز نیست.
- مسیر ادمین همچنان `/admin/stories` و پوستهٔ ادمین است؛ اشتراک در feature/editor/grid است، نه تعویض پوستهٔ هر نقش.

## الزام تحویل رابط بعدی

۱. تکمیل در همان `/vendor-panel/stories`، داخل VendorShell موجود؛ sidebar/header/drawer/نشان فعال/RTL/موبایل قالب حفظ شوند.
۲. همان فرم و منطق feature مشترک با ادمین، با داده و امکانات مجاز همان فروشگاه. ادمین بررسی/تأیید و انتخاب فروشگاه دارد؛ فروشندهٔ عادی تأیید/دادهٔ دیگر فروشگاه‌ها ندارد.
۳. actor/shop و مجوز از زمینهٔ موجود و مرجع سرور استفاده کنند؛ auth محلی موازی، هدر ادمین یا جعل نقش برای کارکرد فرم ساخته نشود.
۴. نمای ذخیره‌شده، state/intent و دادهٔ grid میان admin و فروشگاه‌های مختلف مخلوط نشوند؛ تغییر زمینهٔ فروشگاه نباید پاسخ قبلی را روی فروشگاه جدید بنشاند.
۵. آزمون یکپارچهٔ صفحه در پوستهٔ واقعی VendorShell، مسیر منو، مجوز story.view، نقش/فروشگاه و موبایل جزء پذیرش تحویل رابط باشد؛ تست کامپوننت بی‌پوسته به‌تنهایی کافی نیست.
۶. جدول از الگوی Orders استفاده کند ولی ظاهر و ناوبری Seller قالب را به Admin تبدیل نکند.

## وضعیت اجرا و بازیابی معمار

تسک فعال همان TB-STORY-MARKETPLACE-OWNERSHIP-MEDIA-REVIEW-AND-PUBLICATION-281-R2 است؛ دامنهٔ آن با این یادداشت گسترش نمی‌یابد. رابط مشترک هنوز تسک صادرشده ندارد. هنگام نوشتن تسک رابط بعدی، این فایل باید مرجع اجباری باشد. منتظر نتیجهٔ R2 و بررسی معمار بمانید؛ خودکار تسک بعدی اجرا نشود.

automaticNextImplementationTask: NONE
New-Implementation-Issued-By-This-Note: NO
