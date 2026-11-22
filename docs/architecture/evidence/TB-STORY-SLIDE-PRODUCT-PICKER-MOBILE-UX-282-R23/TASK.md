BEGIN_TOOBA_TASK

Task-ID: TB-STORY-SLIDE-PRODUCT-PICKER-MOBILE-UX-282-R23
Parent-Task: TB-STORY-PUBLIC-PLAYER-IDENTITY-AND-EVIDENCE-282-R22
Channel: tooba-main
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Agent-Type: opencode
Model: metisdeepseek/deepseek-v4-flash
Title: Clear and mobile-accessible slide product selection
Mode: SMALL_CONNECTED_UX_IMPLEMENT_AND_BROWSER_VERIFY
Expected-Base-SHA: ec9fb9d6574167622948f2f9dfd0245849f8fdf0
Issued-Date: 2026-11-22 (Asia/Tehran)
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## Goal / authority

فقط تجربهٔ انتخاب محصول در پنجرهٔ ویرایش اسلاید استوری را
بهبود بده: محصول فعلی واضح باشد، تغییر انتخاب قابل فهم باشد،
جست‌وجو و خطا انتخاب را گم نکنند و کنترل‌ها در موبایل قابل
استفاده باشند.

در سورس بررسی‌شده، گزینهٔ انتخاب‌شده نشان مشخص ندارد و نام
محصول انتخابی پایین فهرست قرار گرفته است.

این تسک مجوز بازطراحی کل استوری، تغییر backend، رفع سایر
بدهی‌ها یا شروع Offer نیست. فقط یک عامل OpenCode اجرا شود.
از Cursor، subagent و ادامهٔ خودکار استفاده نکن.

## A — بررسی مخزن و ابزارها

قبل از تغییر فایل:

1. مسیر واقعی مخزن Tooba را پیدا و remote/branch را تأیید کن.
   AGENTS.md، دستورهای قابل اعمال، OPERATOR-UX-ACCEPTANCE و
   docs/architecture/TOOBA-DELIVERY-ASSURANCE.md را بخوان.

2. Expected-Base را از نظر ancestry و incoming delta بررسی کن.
   اگر HEAD جدیدتر است و تغییرات سازگارند، روی HEAD فعلی ادامه
   بده و SHA را ثبت کن؛ به مبنای قدیمی برنگرد.
   تغییرات dirty کاربر را حفظ کن.
   reset/stash/rebase/amend/force ممنوع.

3. مدل فعال باید metisdeepseek/deepseek-v4-flash باشد.
   اگر مدل قابل تأیید یا قابل استفاده نیست، BLOCKED ثبت کن؛
   مدل دیگری را خاموش جایگزین نکن.

4. دسترسی واقعی OpenSpec، Impeccable و Playwright/Chromium را
   در همین مخزن بررسی کن. نصب آزمایشی قبلی:
   C:\Users\User\OpenCode-UI-Lab-20261122-064208

   وجود ابزار در آن پوشه، دسترسی در این مخزن را ثابت نمی‌کند.
   نسخه و راهنمای ابزار نصب‌شده را بخوان؛ فرمان حدسی اجرا نکن.
   اسکریپت‌ها و package manager همین پروژه را رعایت کن.

   اگر لازم بود، فقط نصب محلیِ حداقلی و سازگار انجام بده.
   نصب global، تغییر config عمومی، کپی node_modules و تغییر
   عمدهٔ dependencyها ممنوع است.
   اگر یکی از سه ابزار یا Chromium آماده نشد، BLOCKED و توقف؛
   ابزار را حذف یا استفاده‌اش را صوری اعلام نکن.

## B — OpenSpec و تصمیم طراحی، قبل از کد

OpenSpec change:
story-slide-product-picker-mobile-282-r23

با workflow نسخهٔ نصب‌شده، شرح تغییر، تصمیم طراحی، کارها و
سناریوهای Given/When/Then را پیش از production edit ثبت کن.

راهنمای Impeccable نصب‌شده را بخوان و از آن برای طراحی استفاده
کن. خودت بر اساس نیاز کاربر چیدمان را انتخاب کن؛ از Dialog،
کنترل‌ها، رنگ‌ها، spacing و tokenهای موجود پروژه استفاده کن.

الزامات طراحی:
- محصول فعلی با نام روشن و نشان انتخاب قابل تشخیص باشد.
- انتخاب فقط با رنگ بیان نشود؛ وضعیت دسترس‌پذیر مناسب داشته باشد.
- نام طولانی فارسی، فهرست و دکمه‌های فرم را خراب نکند.
- loading، empty و error از هم قابل تشخیص باشند.
- انتخاب فعلی هنگام جست‌وجو، تغییر صفحه یا خطای lookup حفظ شود.
- محصول خارج از صفحهٔ فعلی همچنان با هویت خودش نمایش داده شود.
- keyboard، focus و لمس قابل استفاده باشند.
- toast یا پیام دائمی جدید برای نمایش انتخاب اضافه نکن.
- fa و en از کاتالوگ ترجمهٔ موجود و RTL/LTR صحیح استفاده کنند.

قبل از تغییر کد:
ARCHITECTURE-PLAN.md و DELIVERY.json را طبق governance ثبت کن.
REQهای این تسک را بدون حذف یا تغییر معنا وارد manifest کن.
گیت plan را با SHA256 واقعی TASK.md صادرشده اجرا کن.
OpenSpec را به برنامه لینک بده؛ متن TASK.md را بازنویسی نکن.

## C — فایل‌ها و مرز پیاده‌سازی

فایل اصلی:
src/frontend/app/stories/management/StoryManagementScreen.tsx

بخش‌ها و selectorهای موجود:
story-product-picker
story-product-search
story-product-option
story-product-selected
story-product-error
story-product-load-more
story-slide-edit
story-slide-save

کاتالوگ ترجمه:
src/frontend/app/stories/management/story-management-copy.ts

مرجع تست و DTO کنترل‌شده:
src/frontend/app/stories/management/story-product-picker-282r21.mount.test.ts

مرجع lookup:
src/frontend/app/stories/management/story-lookups.ts

مسیر کاربر:
 /fa/admin/stories/{storyId}
 → ویرایش اسلاید موجود
 → مقصد محصول
 → انتخاب محصول
 → ذخیره
 → بازکردن مجدد

همان picker مشترک در VendorShell فروشنده حفظ شود.
API، قرارداد lookup، اختیار فروشگاه، version/occurrence،
executor و recovery را برای این بهبود نمایشی بازنویسی نکن.

در صورت نیاز فقط یک کامپوننت نمایشی کوچک کنار همین feature
استخراج کن؛ state و owning command در مسیر موجود بمانند.
picker، shell، سیستم ترجمه یا abstraction عمومی موازی نساز.

## D — Playwright شرط تحویل

Chromium واقعی روی صفحهٔ واقعی برنامه لازم است.
صفحهٔ نمایشی جدا، HTML آزمایشی یا jsdom جای آن را نمی‌گیرد.

داده و transport را با HTTP کنترل‌شده و DTO/route واقعی فراهم کن.
از fixture زنده یا اولین رکورد واقعی جدول استفاده نکن.
auth موجود را تضعیف نکن؛ در صورت نیاز از harness کنترل‌شدهٔ
موجود استفاده کن. نبود راه اجرای امن یعنی BLOCKED.

پاسخ Save باید state تست را از بدنهٔ PUT به‌روز کند و GET بعدی
همان state را بازگرداند. پاسخ موفق ثابت و مستقل از درخواست
برای اثبات Save پذیرفته نیست.
این شاهد browser+controlled HTTP است؛ backend/PostgreSQL واقعی
یا persistence زنده ادعا نشود.

معیارهای اجباری:

REQ-01 — product-select-save-reread
- اسلاید موجود محصول A دارد؛ کاربر B را انتخاب می‌کند.
- نام و نشان انتخاب B روشن باشد.
- Save دقیقاً یک PUT به مسیر مالک همان اسلاید بفرستد.
- ctaTarget=B، expectedVersion صحیح و occurrence معتبر ثبت شود.
- mediaAssetId و mediaUrl قبلی حفظ شوند.
- upload/add/delete صفر.
- reload و GET مستقل، انتخاب B را دوباره نشان دهند.

REQ-02 — product-cancel-preserves-original
- A را باز کن، B را انتخاب کن، سپس انصراف بده.
- اگر Dialog موجودِ دورریختن تغییر ظاهر شد، همان مسیر را طی کن.
- هیچ فرمان mutation ارسال نشود.
- با بازکردن مجدد، A انتخاب‌شده باشد.
- Escape نیز وارد مسیر مجاز خروج از فرم dirty شود.

REQ-03 — product-error-retry-keeps-selection
- lookup محصولات یک بار 503 بدهد.
- خطا و «تلاش دوباره» دیده شوند؛ empty نمایش داده نشود.
- نام و هویت محصول فعلی حفظ شود.
- Retry نتیجه را بیاورد.
- Retry خودش هیچ محصولی انتخاب یا ذخیره نکند.

REQ-04 — product-mobile-long-name-outside-page
- محصول فعلی نام فارسی طولانی و چندکلمه‌ای دارد و خارج صفحهٔ
  اول نتایج است.
- جست‌وجو و تغییر نتایج، محصول فعلی را حذف یا با گزینهٔ اول
  جایگزین نکنند.
- در 360×640 و 360×800، overflow افقی سند صفر باشد.
- کنترل انتخاب و Save با اسکرول طبیعی در دسترس و با کلیک عادی
  قابل اجرا باشند.
- boundingBox و elementFromPoint نبود پوشاندگی کنترل‌های کلیدی
  را ثابت کنند؛ force:true یا CSS تزریقی تستی ممنوع.

REQ-05 — product-role-locale-smoke
- مسیر اصلی در 1440×900 هم اجرا شود.
- در پنل فروشنده، همان picker با فروشگاه ثابت خودش باز شود؛
  انتخاب و انصراف کار کند و shopId دیگری وارد درخواست نشود.
- در fa جهت RTL و در en جهت LTR و ترجمهٔ کنترل‌های همین بخش
  بررسی شود.

برای هر REQ، نام تست، viewport، نتیجه و مسیر شاهد ثبت کن.
عکس و screenshot لازم نیست؛ JSON/گزارش assertions، geometry و
transport capture کافی است. اطلاعات حساس در خروجی ثبت نشود.

## E — اجرای محدود و بررسی

ترتیب اجرا:
بررسی محیط → OpenSpec/برنامه → ثبت وضعیت قبل → طراحی و کد
→ اولین اجرای پذیرش Playwright → خودبازبینی diff و assertions
→ حداکثر یک نوبت اصلاح → اجرای مجدد معیارهای اثرپذیرفته.

برای نشان انتخاب/چیدمان، ضدنمونهٔ رفتار قبل را ثبت کن.
شکست import یا fixture خراب، اثبات باگ نیست.

معیارها و assertions قبل از کد ثابت شوند.
assertion را برای سبزشدن ضعیف نکن.
از retry خودکار برای پنهان‌کردن flaky failure استفاده نکن.
پس از یک دور اصلاح، اگر FAIL یا NOT_RUN باقی بود، PARTIAL
«ناقص» ثبت کن و توقف؛ چرخهٔ اصلاح بی‌پایان ممنوع است.

این مرحله خودبازبینی OpenCode است، نه بازبینی مستقل معمار.
PASS عامل، Architect ACCEPT یا پذیرش مالک نیست.

تست‌های مرتبط picker، tsc و production build را اجرا کن.
اگر ابزار تست جدید اضافه شد، script اجرای دقیق آن را ثبت کن.
چون backend تغییر نمی‌کند، build/publish backend لازم نیست.
سرویس‌های موجود را خودسرانه متوقف یا پورت‌هایشان را عوض نکن.

## F — حفاظت، شواهد و تحویل

ممنوع:
migration/DDL/seed/grant/reset/restore/cleanup دادهٔ زنده،
هر live Story/Media business write، تغییر auth/permission،
کلید/provider، پول/موجودی/notification، screenshot و subagent.

Task folder:
docs/ai/tasks/TB-STORY-SLIDE-PRODUCT-PICKER-MOBILE-UX-282-R23/

Evidence folder:
docs/architecture/evidence/TB-STORY-SLIDE-PRODUCT-PICKER-MOBILE-UX-282-R23/

TASK.md دقیق، برنامه، DELIVERY.json، تصمیم طراحی، OpenSpec
change، تست‌ها و رسیدها را با مسیر/hash واقعی ثبت کن.
گیت review را اجرا کن؛ شکست یا مورد باز پنهان نشود.
CURRENT/recovery مرتبط فقط وضعیت همین نتیجه را منعکس کنند؛
هیچ ادعای بسته‌شدن کل Story ثبت نشود.

فقط تغییرات مرتبط را commit/push کن، بدون force/amend.
باینری، گزارش HTML حجیم، secrets، node_modules و scratch
را stage نکن.

تحویل کوتاه و canonical:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT

Task-ID: TB-STORY-SLIDE-PRODUCT-PICKER-MOBILE-UX-282-R23
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: opencode
Status: PASS / PARTIAL / BLOCKED
Readiness: READY_FOR_ARCHITECT_REVIEW / NOT_READY

Changes: <تغییرات کوتاه>
Tools: <نسخه و شاهد استفادهٔ OpenSpec/Impeccable/Playwright>
Acceptance: <REQ → test name → viewport → result → evidence path>
Open-Items: <تمام FAIL/NOT_RUN/BLOCKEDها یا NONE>
Repair-Rounds: 0 / 1
Code-SHA: <واقعی یا NONE>
Evidence-SHA: <واقعی یا NONE>
Origin-Parity: <نتیجهٔ fetch واقعی>
Schema-Migration: NONE
Live-Business-Write: NONE
Evidence-Limit: browser with controlled HTTP; not live backend proof
Architect-Acceptance: NOT_CLAIMED
Owner-Acceptance: NOT_CLAIMED
automaticNextImplementationTask: NONE
Stop-State: STOP_FOR_ARCHITECT

END_TOOBA_WORKER_RESULT

پس از تحویل کاملاً متوقف شو؛ تسک بعدی صادر یا اجرا نکن.

END_TOOBA_TASK

## REQ manifest rows

| REQ | Name |
| --- | --- |
| REQ-01 | product-select-save-reread |
| REQ-02 | product-cancel-preserves-original |
| REQ-03 | product-error-retry-keeps-selection |
| REQ-04 | product-mobile-long-name-outside-page |
| REQ-05 | product-role-locale-smoke |
