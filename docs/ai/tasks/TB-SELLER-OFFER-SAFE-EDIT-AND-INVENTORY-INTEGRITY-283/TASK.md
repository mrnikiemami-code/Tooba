PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-SELLER-OFFER-SAFE-EDIT-AND-INVENTORY-INTEGRITY-283
Parent-Task: NONE (first implementation task of the Seller Offer roadmap)
Channel: tooba-main
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Agent-Type: opencode
Model: metisdeepseek/deepseek-v4-flash
Title: ویرایش امن آفر موجود فروشنده و حفظ مستقل قیمت و موجودی هر محل
Mode: ROADMAP_STAGE_1_IMPLEMENT_AND_VERIFY
Expected-Base-SHA: 33c69baffdd8d0280de0c8a6edd1c625e0c4396e
Issued-Date: 2026-11-22 (Asia/Tehran)
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## Goal / authority

مسیر کاربر از منوی محصولات فروشنده به آفر موجود، ویرایش، ذخیره، خروج و بازخوانی است. فروشنده باید بتواند SKU یا حدود خرید را تغییر دهد بدون اینکه قیمت، موجودی، رزرو یا وضعیت فروش ناخواسته تغییر کند. ویرایش عمدی قیمت یا موجودی همچنان باید کار کند و اثرش قابل مشاهده باشد. دامنه فقط مرحلهٔ ۱ از docs/planning/offer/TOOBA-SELLER-OFFER-ARCHITECTURE-AUDIT-AND-ROADMAP-2026-11-22.md است؛ مرحلهٔ ۲ (ایجاد و منوی مدیر)، تخفیف، ارسال و تسویه تسک‌های بعدی این برنامه‌اند. یافته‌های O-01/02/03/05 را فقط در مسیر Edit پوشش بده.

## A — مبنا و ابزارها

از ریشهٔ واقعی Tooba اجرا کن. remote، شاخه، ancestry، incoming diff و وضعیت فایل‌های کاربر را بررسی کن. ثبت همین سند روی main تغییر مستنداتی مورد انتظار است. AGENTS.md، دستورهای جاری ماژول‌ها، docs/product/OPERATOR-UX-ACCEPTANCE.md و docs/architecture/TOOBA-DELIVERY-ASSURANCE.md را بخوان. یک عامل OpenCode با مدل اعلام‌شده اجرا شود.
در همین checkout، دسترسی OpenSpec، راهنما و workflow نصب‌شدهٔ Impeccable، Playwright و executable مرورگر Chromium را عملاً بررسی و نسخه/مسیر مؤثر را ثبت کن. نصب آزمایشی قبلی در C:\Users\User\OpenCode-UI-Lab-20261122-064208 است. استفاده از ابزار موجود آنجا از cwd ریپوی توبا مجاز است. از تغییر global config، کپی node_modules، نصب مدل دیگر یا تغییر عمدهٔ dependencyها پرهیز کن. نبود ابزار اجباری را صریح گزارش کن.

## B — OpenSpec و طراحی با Impeccable، پیش از production edit

OpenSpec change: seller-offer-safe-edit-283
proposal، design، tasks و سناریوهای Given/When/Then برای REQ-01 تا REQ-07 را بنویس؛ مسیر فایل و دستور واقعی اعتبارسنجی strict را ثبت کن. در docs/ai/tasks/TB-SELLER-OFFER-SAFE-EDIT-AND-INVENTORY-INTEGRITY-283/، ARCHITECTURE-PLAN.md و DELIVERY.json را مطابق الگوی ریپو قرار بده. تصمیم هر فرمان، مالک داده، تشخیص تغییر، مرز پاسخ نامطمئن، و راه ایزوله‌کردن آزمون مرورگر متصل به دیتابیس را قبل از کد مشخص کن. plan gate را با SHA256 سند صادرشده پاس کن و این طرح را پیش از کد commit کن.
با Impeccable، فرم فعلی را از دید کسی که فقط SKU را عوض می‌کند نقد کن. براساس همان نقد، چیدمان و متن دکمه‌ها را با اجزای فعلی طراحی کن. باید معلوم باشد کاربر مشخصات آفر، قیمت یا موجودی کدام محل را ذخیره می‌کند. مجموع موجودی با مقدار قابل ویرایش یک محل اشتباه نشود. وضعیت ذخیره‌نشده، انصراف، خطا و حفظ ورودی در خطا قابل فهم باشند. فارسی/RTL، کیبورد، فوکوس و اندازهٔ موبایل رعایت شوند. تصمیم طراحی و شاهد بررسی نتیجه با همان workflow را ثبت کن؛ ذکر نام ابزار یا نصب آن به‌تنهایی شاهد استفاده نیست.

## C — مسیر کد و حدود پیاده‌سازی

- نقطهٔ ورود: src/frontend/app/vendor-panel/products/[offerId]/page.tsx، توابع applyDetail/onSave، و src/frontend/app/vendor-panel/seller-api.ts. هنگام ورود از فهرست، فروشگاه و offerId واقعی حفظ شوند. فرم جدید موازی یا پوستهٔ جدا ایجاد نکن.
- سمت سرور: OfferSellerEndpoints.cs، SellerOfferPanelDtos.cs، UpdateOfferCommand و validator، OfferReadModelComposer. فیلد غایب در PATCH، پاک‌کردن صریح و فیلد تغییرکرده سه معنای روشن داشته باشند. فرستادن Draft در ویرایش عنوانی مانند SKU نباید به تغییر چرخهٔ وضعیت نیاز داشته باشد.
- InventoryDirectory.Availability.cs مجموع موقعیت‌ها را می‌خواند. InventoryDirectory.SellerWrite.cs اکنون اولین موقعیت را برای Set برمی‌گزیند. برای ویرایش عمدی موجودی، محلِ مجاز و مقدار همان محل را به فرمان مالک ببند؛ مالکیت آفر/فروشگاه/موقعیت و رزرو در سرور بررسی شود. کلاینت قدیمی که مقصد مشخص نکرده نباید بتواند مجموع چند محل را روی یکی از آن‌ها بنویسد. رفتار تک‌محلی و caller ایجاد فعلی را با تست سازگاری حفظ کن. API باریک افزایشی یا اصلاح قرارداد با تست callerهای موجود مجاز است.
- قیمت از Pricing و موجودی از Inventory می‌آید. از روی دادهٔ قدیمی کل فرم را بازنویسی نکن. بازخوانی موفق یک بخش نباید ویرایش ذخیره‌نشدهٔ بخش دیگر را پاک کند. نتیجه را با هویت، فروشگاه و اثر فرمان بسنج؛ با پاسخ ناقص/متناقض یا قطع ارتباط، پیام موفقیت نده. در نتیجهٔ نامطمئن ابتدا بازخوانی و تطبیق کن؛ نوشتن مجدد خودکار کورکورانه انجام نشود.

## D — پذیرش قابل مشاهده و آزمون مستقل

| ID | نام پیشنهادی تست | داده و انتظار الزام‌آور |
|---|---|---|
| REQ-01 | edit_sku_two_positions_preserved | از فهرست به آفر A با دو موقعیت مشخص ۳ و ۷ برو. فقط SKU را عوض کن. دقیقاً یک PATCH متعلق به A و صفر نوشتن قیمت/موجودی. پس از بستن، بازکردن و GET مستقل، SKU جدید، قیمت و رزرو اولیه، و همان دو ردیف ۳ و ۷ را بررسی کن. تعداد adjustmentها ثابت باشد. |
| REQ-02 | draft_stays_draft_clear_limits | در پیش‌نویس، ویرایش SKU با حفظ Draft موفق شود. روی حدود موجود، پاک‌کردن صریح هر دو حد پس از reload هر دو را null کند؛ PATCH بی‌ارتباط حدود قبلی را حفظ کند. |
| REQ-03 | concurrent_price_and_sku | دو نشست مستقل یک آفر را باز کنند. کاربر اول قیمت جدید را ثبت کند؛ کاربر دوم با فرم قدیمی فقط SKU را ثبت کند. پس از GET مستقل قیمت جدید و SKU جدید باقی باشند. ثبت قیمت فقط مسیر قیمت را بنویسد. |
| REQ-04 | stock_update_targets_named_position | با موقعیت‌های ۳ و ۷، فقط دومی را به ۸ تغییر بده؛ در بازخوانی ۳ و ۸ و جمع ۱۱ باشد. SKU، قیمت، رزرو و موقعیت اول ثابت بمانند. درخواست مبهم چندمحلی، موقعیت فروشگاه دیگر و مقدار کمتر از رزرو هرکدام بدون تغییر نامعتبر متوقف شوند. |
| REQ-05 | wrong_response_does_not_claim_save | پاسخ ۲۰۰ با آفر/فروشگاه دیگر، پاسخ همان آفر با مقدار کهنه و قطع ارتباط پس از ثبت را پوشش بده. UI موفقیت کاذب نشان ندهد؛ تطبیق با GET مستقل نتیجه را روشن کند و قطع ارتباط فرمان دوم خودکار ایجاد نکند. |
| REQ-06 | cancel_and_mobile_reachable | روی فرم واقعی، تغییر و انصراف، صفر درخواست نوشتن و بازکردن مجدد مقدار قبلی. در Chromium با ۳۶۰×۶۴۰ **در حالی که فرم باز و دارای تغییر است**، عنوان محل، کنترل ذخیره و انصراف در RTL خوانا و با اسکرول عادی قابل دسترسی/کلیک باشند؛ overflow افقی صفحه صفر. |
| REQ-07 | independent_section_edits_survive | در یک نشست دو بخش را تغییر بده؛ ذخیره یا شکست ذخیرهٔ بخش اول ورودی بخش دوم را پاک یا ثبت نکند. کلیک «ذخیره» روی فرم بدون تغییر، صفر درخواست نوشتن. بعد از ذخیرهٔ عمدی بخش دوم و بازخوانی، هر دو مقدار صحیح باشند. |

ابتدا همان مثال SKU و ۳+۷ را روی نسخهٔ قبل اجرا و شکست قابل انتساب به رفتار قدیمی را ثبت کن. انتظار تست در اجرای نهایی یکسان بماند. برای سرعت، سناریوهای با دادهٔ مشترک می‌توانند یک fixture داشته باشند، ولی خروجی مستقل هر REQ ثبت شود.

## E — روش اثبات و حفاظت از داده

- شرط تحویل، Playwright در Chromium واقعی روی صفحهٔ خود توبا است. مسیر اصلی ذخیره/بازخوانی (REQ-01)، انصراف و حالت مرزی فرم کوتاه (REQ-06) و بازخورد ذخیرهٔ نامطمئن (REQ-05) را در مرورگر اجرا کن. برای موفقیت اصلی از فرم تا backend مالک و GET مستقل و مشاهدهٔ مقادیر PostgreSQL در harness ایزوله استفاده کن. ۲xx، toast، state در React، regex و fixture که خودش پاسخ مطلوب می‌دهد به‌تنهایی کافی نیستند. تزریق خطا فقط برای شاخه‌های خطا پذیرفته است و روش هر تست باید مشخص باشد.
- تست‌های داده برای REQ-01/03/04 روی PostgreSQL واقعی با fixture کنترل‌شده و سازوکار ایزوله/rollback پروژه باشند. همهٔ درخواست‌های یک آزمون چنددرخواستی و خواندن تازه باید واقعاً به همان محیط ایزوله برسند؛ rollback یک اتصالِ جدا، درخواست Host عادی را پوشش نمی‌دهد. در صورت نبود راه امن، آزمون مربوط را NOT_RUN/BLOCKED ثبت کن. روی آفر، قیمت، انبار یا رزرو زنده هیچ write/restore آزمایشی انجام نده. بدون migration، DDL، seed یا grant روی محیط واقعی، بدون تماس provider، عملیات مالی، تغییر edition یا bootstrap.
- تست‌های متمرکز تغییرات frontend و backend، typecheck، build بخش‌های تغییرکرده و کنترل caller قدیمی را اجرا کن. SHA و نسخهٔ سروشده برای مرورگر و provider شواهد را ثبت کن. بدون screenshot، sleep مصنوعی، force-click، دست‌کاری CSS تست یا تضعیف guard.

## F — قاعدهٔ پایان و رسید

در docs/architecture/evidence/TB-SELLER-OFFER-SAFE-EDIT-AND-INVENTORY-INTEGRITY-283/ رسیدها، خروجی واقعی و خلاصهٔ کوتاه فارسی قرار بگیرد؛ artifact و manifest مورد نیاز check-readiness را طبق قرارداد خودش در task-folder نگه دار. در رسید برای هر REQ نام تست، وضعیت، قبل/بعد، تعداد و مسیر نوشتن، هویت و مقدار بازخوانی‌شده، provider و مرجع قابل بازاجرا بیاور. secrets و دادهٔ مشتری وارد git نشوند. SHA طرح، کد، شواهد و parity پس از fetch ثبت شود. commit/push معمول روی main؛ بدون force/reset/rebase/amend/stash یا staging فایل کاربر.
حداکثر یک نوبت اصلاح پس از نخستین اجرای پذیرش؛ ابتدا علت شکست ثبت و همان سناریو دوباره اجرا شود. PASS فقط با اجرای تمام معیارها و گیت review مجاز است. FAIL، NOT_RUN یا BLOCKED به معنی تحویل ناقص با ذکر شناسه و علت است. خروجی BRIDGE-WAKE-V1 با Task-ID کامل، مدل، شواهد واقعی OpenSpec/Impeccable، جدول Playwright/PostgreSQL، Repair-Rounds، Code/Evidence SHA، موارد باز و STOP_FOR_ARCHITECT. پذیرش نهایی با معمار و مالک است.

END_TOOBA_TASK
