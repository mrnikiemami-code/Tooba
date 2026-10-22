# Tooba — ریکاوری جاری معماری ارسال / 2026-10-22

> **وضعیت جاری Architect پس از 195:** تحلیل محدود **ACCEPT WITH CORRECTIONS**؛ [بررسی/تصمیم](../../architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-ANALYSIS-195/ARCHITECT-REVIEW.md). تعریف نوع در Fulfillment و انتساب آینده در Shipping؛ Admin همان دیتابیس commerce/نصب مرجع تعریف، Seller صرفاً انتخاب‌کنندهٔ مجاز، بدون رجیستری جهانی یا سیاست ارثی. rules بخش26 و clarification قفل flat ثبت شد. شباهت نام جدول/ترجمه دلیل مالکیت نیست؛ مالکیت تصمیم صریح Architect است. تنها تسک بعدی **TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196** (Parent=195): کاتالوگ قابل مدیریت create/read/update/status و Contracts reader، فقط Fulfillment؛ نه assignment Shipping یا فرانت. permission واقعی view/manage لازم؛ seam قدیمی Fulfillment فقط Admin gate دارد. unavailable را empty-success نکنید. Structure handoff پیاده‌سازی REQUIRED، بدون recertification. تحلیل آزمون/DB نداشت؛ gateهای UI/نوع‌گذاری کالا/فعال‌سازی/مبلغ‌وتعداد/carrier/funder/استقرار باز و TB-185 متوقف؛ پایین تاریخچه است.


> **نتیجهٔ 195 (در انتظار تصمیم Architect):** تحلیل محدود D2 انجام و از Bridge ارسال شد؛ [شواهد](../../architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-ANALYSIS-195/RESULT.md). owner فعلی نوع ارسال وجود ندارد؛ پیشنهاد (PROPOSED، نه APPROVED): تعریف مشترک taxonomy با متن `LanguageId` در Fulfillment و انتساب مالک‌محور nullable روی ریشهٔ draft در Shipping از طریق Contracts reader، بدون FK/join. قفل قدیمی no-Types-of-Service برای دامنهٔ خودش معتبر است؛ تنها تنش واقعی، اشتراک نوع میان چند روش یک مالک با isolation §B/§C است. تصمیم‌های باز: authority تعریف، الزام‌بودن، اثر سازگاری روش/کالا، scope مارکت‌پلیس و اصلاح یک‌خطی قفل. هیچ کدنویسی/DB/فرانت انجام نشد و تسک بعدی صادر نشده است.
> **وضعیت جاری Architect پس از 194:** دو انتخاب مستقل پیش/پس‌کرایه و دستی/API/رایگان در ریشهٔ draft **ACCEPT**؛ [بررسی مستقل](../../architecture/evidence/TB-SHIPPING-ADMIN-METHOD-MODE-DRAFTS-194/ARCHITECT-REVIEW.md). checkpoint `6fde6ee768a9b6639d1f4df10600e19c327bd9c1`؛ 616/616 گزارش Worker، اینجا اجرا نشده. آزمون رقابت و قیدهای PostgreSQL از source بررسی شد. اصلاح گزارش: routeها39→43؛ timestamp در no-op از source پشتیبانی دارد ولی assert اجرایی ندارد؛ سه ویرایش پسینی نرخ/ترجمه/برنامه در تست تازه اجرا نشده‌اند (ویرایش مناطق اجرا شده). انتخاب حالت، اجرای مالی/حامل/فعال‌سازی نیست. تنها تسک بعدی **TB-SHIPPING-ADMIN-SHIPPING-TYPE-ANALYSIS-195**، Parent=194، تحلیل محدود مالکیت و reuse نوع ارسال و سازگاری با قفل قدیمی no-Types-of-Service؛ بدون پیاده‌سازی/DB/فرانت. نتیجه را برای تصمیم Architect برگردانید. سرفصل TB-194 در rules به25 تصحیح شد؛16 قبلی حفظ است. سایر gateهای واقعی UI/زبان DB، taxonomy/cutoff، مبنای مبلغ/تعداد گروه، carrier/funder، rollout باز؛ TB-185 متوقف. یادداشت‌های زیر تاریخچه‌اند.


> **وضعیت جاری پس از نتیجهٔ 194:** تسک **TB-SHIPPING-ADMIN-METHOD-MODE-DRAFTS-194** (Parent=193-R1) اجرا و نتیجهٔ آن
> از Bridge ارسال شد؛ وضعیت اعلام‌شده **PARTIAL** است فقط به‌دلیل عبور از سقف 45 دقیقه‌ای، نه به‌دلیل نقص اثبات.
> تمام واقعیت‌های فنی الزامی اجرا شد: 7 تست جدید (4 API + 3 ذخیره‌سازی/رقابت) و کل مجموعهٔ Shipping **616/616** روی
> PostgreSQL 17 یک‌بارمصرف؛ رقابت settings-vs-rate با barrier واقعی، دقیقاً یک برنده/یک تعارض، revision=2، نبود
> حالت ترکیبی و tracker خالی بازنده. شواهد در
> [RESULT.md](../../architecture/evidence/TB-SHIPPING-ADMIN-METHOD-MODE-DRAFTS-194/RESULT.md). این یادداشت پذیرش
> Architect نیست؛ منتظر بررسی مستقل بمانید و هیچ تسک بعدی خودکار صادر نمی‌شود. اتصال حامل، هزینه/تأمین مالی
> رایگان، اجرای قیمت، UI و فعال‌سازی همچنان بازند؛ TB-185 متوقف است.

> **وضعیت جاری پس از 193-R1:** برنامهٔ هفتگی draft با اصلاح اثبات رقابت **ACCEPT** شد؛ [بررسی مستقل](../../architecture/evidence/TB-SHIPPING-ADMIN-WEEKLY-SCHEDULE-DRAFTS-193-R1/ARCHITECT-REVIEW.md). checkpoint `fce7fab3082435eb9fb92092b1b885a73bf98335`؛ شرط دو read از revision=1، دقیقاً یک برنده/یک تعارض، revision نهایی2 و نبود حالت ترکیبی/تغییر معلق بازنده از source بررسی شد. 609/609 و پنج اجرای متوالی گزارش Worker است، نه اجرای مستقل Architect. عبارت45دقیقه در نتیجه اشتباه نگارشی است؛ سقف واقعی R1 بیست دقیقه و زمان گزارش‌شده8.5دقیقه است. تنها تسک بعدی **TB-SHIPPING-ADMIN-METHOD-MODE-DRAFTS-194**، Parent=193-R1، صرفاً ذخیرهٔ مستقل پیش/پس‌کرایه و دستی/API حامل/رایگان در ریشهٔ draft؛ نه اتصال حامل، محاسبهٔ واقعی یا اثر پرداخت. انتخاب رایگان جای ثبت هزینهٔ واقعی و تأمین‌کنندهٔ صریح آن را نمی‌گیرد. تعرفه‌ها/جغرافیا/زبان‌ها/برنامهٔ هفتگی با تغییر حالت حذف نمی‌شوند. اتصال Admin به دادهٔ واقعی و پذیرش UI، taxonomy/cutoff، مبنای مبلغ/تعداد گروه، carrier و تأمین مالی/فعال‌سازی/استقرار بازند؛ TB-185 متوقف است. تسک194 را صادر کنید و تا نتیجهٔ همان صبر کنید. یادداشت‌های زیر تاریخی‌اند.

> **وضعیت جاری پس از بررسی 193:** برنامهٔ هفتگی پیاده شده ولی Architect = **PARTIAL_PENDING_DETERMINISTIC_CONCURRENCY_PROOF**؛ [بررسی مستقل](../../architecture/evidence/TB-SHIPPING-ADMIN-WEEKLY-SCHEDULE-DRAFTS-193/ARCHITECT-REVIEW.md). تست رقابت فعلی قبل از read شروع را هماهنگ می‌کند، موفقیت هر دو را می‌پذیرد و tracker بازنده را نمی‌سنجد؛ ادعای گزارش دربارهٔ این تست پذیرفته نشد، نقص production هنوز اثبات نشده است. تنها تسک بعدی **TB-SHIPPING-ADMIN-WEEKLY-SCHEDULE-DRAFTS-193-R1** (Parent=193) برای اصلاح همان تست/گزارش، نه فیچر جدید؛ 194 صادر نشده. 609/609 گزارش Worker است و اینجا اجرا نشده. پذیرش190/191-R1/192-R1 حفظ؛ TB-185 متوقف و فعال‌سازی محصول همچنان باز است. وضعیت‌های پایین تاریخی‌اند.

> **وضعیت جاری Architect پس از 192-R1:** ارائهٔ چندزبانهٔ مالک‌محور **با R1 پذیرفته شد**؛ [پذیرش مستقل](../../architecture/evidence/TB-SHIPPING-ADMIN-LOCALIZED-DRAFT-METADATA-192-R1/ARCHITECT-REVIEW.md). checkpoint `0cd2ced0df02730a75300fa8fc2bacf5a76db8ac`، کد `df743ddf160e45c64a62998389c0a04477ce60cb`؛ 589/589 گزارش Worker و بررسی Architect از source/diff است، نه اجرای مستقل آزمون. تنها تسک بعدی **TB-SHIPPING-ADMIN-WEEKLY-SCHEDULE-DRAFTS-193** (Parent=192-R1): برنامهٔ هفتگی مشترک روش با ساعات مستقل هر روز، تعطیل/تنظیم‌نشدهٔ متمایز و timezone صریح؛ بدون جغرافیای تازه/تغییر legacy/وعدهٔ مشتری. منتظر نتیجهٔ همان تسک بمانید. اتصال واقعی UI/رجیستری فروشگاه، taxonomy/حالت‌ها/cutoff، مبنای مبلغ/اندازهٔ گروه و فعال‌سازی بازند؛ TB-185 متوقف است. یادداشت‌های پیشین تاریخچه‌اند.

> **وضعیت جاری پس از بررسی 192:** پیاده‌سازی ارائهٔ چندزبانه موجود است ولی Architect = **PARTIAL_PENDING_REQUIRED_PROOF**، هنوز ACCEPT نشده. [بررسی مستقل](../../architecture/evidence/TB-SHIPPING-ADMIN-LOCALIZED-DRAFT-METADATA-192/ARCHITECT-REVIEW.md): دو تست جدید مستقیماً directory را اجرا کرده‌اند؛ شواهد presentation GET/PATCH واقعی، یک/دو/سه‌زبان، تغییر فعال/پیش‌فرض و مجوزها کامل نیست. تنها تسک بعدی **TB-SHIPPING-ADMIN-LOCALIZED-DRAFT-METADATA-192-R1** (Parent=192)، برای تکمیل شواهد و اصلاح توضیح/گزارش؛ نه قابلیت تازه. 579/579 گزارش Worker است؛ Architect اینجا آزمون اجرا نکرده. پذیرش‌های190و191-R1 پابرجا؛ 193 صادر نشده و TB-185 متوقف است. زبان‌ها همچنان از مرجع دیتابیس، بدون فهرست ثابت‌اند. وضعیت‌های پایین تاریخی‌اند.

> **وضعیت جاری Architect پس از 191-R1:** ذخیرهٔ ردیف‌های تعرفهٔ دستی **با اصلاحات R1 پذیرفته شد**؛ [پذیرش مستقل](../../architecture/evidence/TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191-R1/ARCHITECT-REVIEW.md). checkpoint `7588dd80d2f916dc35b46fd7f0e0bea7efeb4073`، کد `7fc3b0bc6352cadd6be3a2290ef55ef7dcd1c44d`؛ 577/577 و PostgreSQL گزارش Worker است، Architect اینجا اجرا نکرده. تنها تسک بعدی **TB-SHIPPING-ADMIN-LOCALIZED-DRAFT-METADATA-192** (Parent=191-R1): نام/توضیح مالک‌محور روش و عنوان اختیاری گروه با languageId واقعی دیتابیس. منتظر نتیجهٔ آن بمانید. کاتالوگ Fulfillment تغییر نمی‌کند؛ زبان‌ها ثابت نیستند. سابقهٔ PARTIAL/تخلف Host در191 حفظ و انتساب شکست‌های Host همچنان تأییدنشده است. مبنای مبلغ/اندازهٔ گروه در اجرای قیمت، UI، taxonomy/حالت‌ها/تقویم و فعال‌سازی هنوز بازند؛ TB-185 متوقف است. یادداشت‌های pending/repair قبلی زیر تاریخی‌اند.

> **وضعیت جاری پس از بررسی 191:** نتیجهٔ Worker = PARTIAL؛ Architect = **REPAIR_REQUIRED**، نه صرفاً به علت زمان. [بررسی مستقل 191](../../architecture/evidence/TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191/ARCHITECT-REVIEW.md): CHECKهای nullable/سقف حداقل مبلغ/قالب ارز با دامنه برابر نیستند و رقابت هم‌زمان نرخ با ویرایش مناطق هنوز تست نشده است. تنها تسک بعدی **TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191-R1**، Parent=191؛ منتظر نتیجهٔ آن بمانید. 574/574 Shipping گزارش Worker است؛ Architect آزمون اجرا نکرده. اجرای ممنوع Host.Tests و عبور 60 از45 دقیقه ثبت شد؛ ادعای pre-existing بودن 132 شکست Host **تأیید نشده** و مجوز اجرای دوباره/اصلاح Host نیست. پذیرش 190 پابرجاست؛ 191/باقی Admin/Storefront/استقرار هنوز بازند. وضعیت‌های پایین checkpoint تاریخی‌اند.

> **وضعیت جاری Architect — پس از بررسی R2:** پایهٔ **190 + R1 + R2 پذیرفته شد، فقط برای پیش‌نویس مالک و گروه مناطق**؛ [پذیرش مستقل](../../architecture/evidence/TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R2/ARCHITECT-REVIEW.md). checkpoint بررسی‌شده `4895ff8888a0bc920d9b030525273bd9bf7efca8` و کد R2 `b29b228e2c49d042984a32ce507b35c30c17098f` است. 558/558 و PostgreSQL گزارش اجرای Worker است، اینجا دوباره اجرا نشده. تنها تسک بعدی **TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191** (Parent=190-R2)، برای ردیف‌های شرط/هزینه/مدت تحویل زیر group_id پایدار است؛ منتظر نتیجهٔ آن بمانید و تسک موازی/تکراری صادر نکنید. مقداردهی واقعی کالاهای گروه و مبنای تخفیف/مالیات در اجرای قیمت، اتصال UI، حالت‌ها/تقویم/taxonomy و فعال‌سازی هنوز بازند. یادداشت‌های pending/blocked قدیمی زیر تاریخچه‌اند؛ TB-185 متوقف می‌ماند.


> **آخرین وضعیت پس از 190-R2 (نتیجهٔ Worker، بدون پذیرش Architect):** گارد تراکنش به مرز دقیق مجاز اصلاح شد، جداسازی اتصال با پارس واقعی اثبات شد و مسابقه/لغو/برگشت روی PostgreSQL واقعی اجرا شد. Worker اعداد اندازه‌گیری‌شدهٔ خود را جدا از تاریخ 538/1/539 منتشر کرده است. این خط پذیرش Architect، تکمیل محصول یا فعال‌سازی نیست؛ بازبینی مستقل R2 لازم است و تسک بعدی صادر نشده است. [نتیجهٔ R2](../../architecture/evidence/TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R2/RESULT.md).

> **آخرین وضعیت پس از 190-R1:** Worker نتیجهٔ BLOCKED با 538 pass / 1 fail گزارش کرد؛ اصلاح null/preflight و تراکنش دو‌مرحله‌ای بررسی شد، اما پذیرش نهایی صادر نشده است. تصمیم Architect: حفظ همان تراکنش مجاز و اصلاح محدود گارد قدیمی، بدون migration تازه. هم‌زمان شواهد race و اتصال آزمایشی باید دقیق شوند. [بررسی R1](../../architecture/evidence/TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R1/ARCHITECT-REVIEW.md). تنها تسک بعدی **TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R2** است؛ نتیجهٔ آن را بررسی کنید. اشاره‌های زیر به انتظار R1/190 تاریخی‌اند. نرخ/فرانت/فعال‌سازی و پذیرش کامل محصول همچنان باز است.

> **وضعیت جدید پس از نتیجهٔ 190 — 2026-10-22:** نتیجه دریافت و کد تا `b84877f4f633714989d6458f2be9a7aeda59f8f5` بررسی شد؛ **ARCHITECT = REPAIR_REQUIRED**. عبارت‌های «190 منتظر نتیجه» در checkpoint اولیهٔ زیر تاریخی‌اند. سه مانع: null گروه‌ها از validator عبور می‌کند، preflight شناسهٔ گروه‌ها پیش از mutation کامل نیست، و swap ترتیب با UNIQUE فوری سازگار نیست. [بررسی مستقل](../../architecture/evidence/TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190/ARCHITECT-REVIEW.md). فقط **TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R1** برای اصلاح محدود آماده/صادر می‌شود؛ منتظر نتیجهٔ آن بمانید، تسک 191 صادر نشده است. نرخ/فرانت/فعال‌سازی/پذیرش PostgreSQL هنوز بازند. Worker گزارش 526 تست سبز دارد؛ اینجا آزمون اجرا نشده است.


> **نقطهٔ شروع جاری Shipping بعد از پاک‌شدن چت.** این سند بر اشاره‌های قدیمی «کار بعدی» در ریکاوری 2026-10-16، START-HERE و تاریخچهٔ فازهای عمومی اولویت دارد. تاریخچه حذف نشده است.
> این commit فقط مستندسازی/ریکاوری است؛ هیچ قابلیت، مهاجرت، پرچم یا پذیرش تولید را اجرا نمی‌کند.

## ۱. وضعیت دقیق همین لحظه

| مورد | مقدار |
| --- | --- |
| Repository / branch | `mrnikiemami-code/Tooba` / `main` |
| Protocol / channel / worker | `BRIDGE-WAKE-V1` / `tooba-main` / `tooba-worker-01` |
| Program جاری | `TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY` |
| آخرین نتیجهٔ بررسی‌شده و پذیرفته‌شده | `TB-SHIPPING-ADMIN-AUTHORIZED-OWNER-SCOPE-189`، پذیرش از بررسی کد/شواهد؛ نه E2E فروشگاه |
| HEAD مشاهده‌شده پیش از این مستندسازی | `676b7efcb9d4f402e662d5f955e82a33b252c835`؛ همیشه HEAD را دوباره fetch کنید |
| تنها تسک صادرشده و منتظر نتیجه | `TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190` |
| Parent تسک 190 | `TB-SHIPPING-ADMIN-AUTHORIZED-OWNER-SCOPE-189` |
| Mode تسک 190 | `BOUNDED_OWNER_ISOLATED_METHOD_DRAFT_AND_REGION_GROUP_PERSISTENCE` |
| وضعیت 190 | **ISSUED / AWAITING_WORKER_RESULT — نتیجه دریافت نشده؛ پیاده‌سازی/پذیرش آن ادعا نمی‌شود** |
| وضعیت claim/اجرای فعلی Bridge | **UNKNOWN**؛ از نبود commit نتیجه نگیرید Worker شروع نکرده است |
| کار بعدی Architect | دریافت نتیجهٔ همان 190، بررسی diff/شواهد، سپس ACCEPT / REPAIR / BLOCK و فقط یک تسک بعدی |
| فعالیت خودکار بعدی | `NONE`؛ تسک 191 یا تکرار 190 خودکار مجاز نیست |
| وضعیت محصول | Admin جدید هنوز کامل و یکپارچه نیست؛ Storefront/استقرار بعد از پذیرش Admin |
| وضعیت اجرای دیتابیس واقعی | برای 188/189 اثبات نشده؛ schema جدید 190 هنوز نتیجه ندارد |
| عملکرد عمومی Storefront | TB-185 **PAUSED**؛ PERF-CRIT-001 بسته نشده است |

**درخواست آخر مالک:** «ریکاوری را همین الان با کارهای انجام‌شده و کارهای بعدی در ریپو ثبت کن تا چت از بین رفت آماده باشی». این سند اجرای همین درخواست است، نه تسک پیاده‌سازی تازه.

## ۲. پروتکل بازیابی و ادامه؛ بدون تکرار کار

1. `AGENTS.md`، همین سند، تصمیم‌های محصول، مهارت مرتبط و HEAD تازه را بخوانید.
2. روی main، منشأ و diff تغییرات تازه را بررسی کنید؛ SHAهای نوشته‌شده checkpoint تاریخی‌اند، نه مجوز reset.
3. ابتدا مشخص کنید آیا نتیجه/رسید 190 رسیده یا همان تسک در Bridge Pending/Claimed است. یک بار بررسی وضعیت مرتبط با تسک مجاز است؛ polling دائمی Worker idle ممنوع.
4. اگر 190 هنوز فعال/بی‌نتیجه است، هیچ تسک جدید، re-claim، clone task یا تسک هم‌زمان صادر نکنید. اگر نتیجه رسیده، کد/شواهد همان نتیجه را بررسی کنید؛ Worker PASS برابر Architect ACCEPT نیست.
5. Parent، Task-ID، Program، Mode، Expected-Base-SHA، receipt/evidence و Result envelope را عین قرارداد حفظ کنید. نام هر فایل باید `<Task-ID>.task.md` باشد؛ فایل واقعی دانلودی و قابل preview، نه فقط متن inline.
6. قبل از اجرای کد، HEAD/origin/main/expected base و dirty/stashes را مطابق تسک بررسی کنید؛ mismatch/divergence را با reset/stash/rebase/force حل نکنید.
7. فقط یک تسک محدود، حداکثر **45 دقیقه HARD**؛ فرمان build/test حداکثر 180 ثانیه. توقف و گزارش PARTIAL بهتر از overrun/حلقهٔ تست است.
8. فایل‌های تاریخی `docs/ai/tasks` و نسخهٔ issued زیر، مجوز اجرای خودکار نیستند؛ Worker فقط تسک واقعاً claim‌شده از Bridge را اجرا می‌کند.

### نکتهٔ مهم دربارهٔ این commit مستندات و بیس 190

نسخهٔ اولیهٔ صادرشدهٔ 190 بیس `676b7efcb9d4f402e662d5f955e82a33b252c835` دارد. به‌روزرسانی ریکاوری main را فقط در فایل‌های مستندات جلو می‌برد. آن نسخهٔ اولیه برای حفظ متن دقیق در [آرشیو issued](issued/TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190.task.md) ذخیره شده؛ **رسید claim یا نتیجهٔ اجرا نیست**.

پس از این commit، Architect نسخهٔ دانلودی همان Task-ID را با Expected-Base-SHA تازه refresh می‌کند؛ این **تسک جدید/دوم نیست**. فقط پیش از claim از نسخهٔ تازه استفاده شود. اگر نسخهٔ قبلی از قبل Claim/شروع شده، آن را دوباره enqueue/replay نکنید؛ وضعیت واقعی و قاعدهٔ بازیابی همان تسک را بررسی و در صورت تعارض STOP_FOR_ARCHITECT کنید. هیچ معافیت عمومی «هر تغییر docs قابل قبول است» ساخته نشده است. اگر چت پیش از refresh قطع شد، نخست فقط نسخهٔ بیسِ همان تسک را پس از بررسی HEAD و diff به‌روز کنید؛ scope و ID را خودسرانه عوض نکنید.

## ۳. مراجع الزام‌آور

- [تصمیم محصول Shipping Admin](../../architecture/shipping/SHIPPING-ADMIN-RULES-DECISION.md) — `SHIPPING-ADMIN-RULES-001`، اکنون بخش‌های 1–18؛ تصمیم اجرایی draft در خود 190 است تا رسید/نتیجه ثبت شود.
- [تجربهٔ نهایی مشتری](../../architecture/shipping/SHIPPING-STOREFRONT-UX-REFERENCE.md) — `SHIPPING-STOREFRONT-UX-001` و تصویر اصلی دیجی‌کالا؛ هدف UX، نه کپی برند یا الگوریتم فرضی.
- [سیاست مالی ترکیبی ارسال](../../architecture/checkout/SHIPPING-GROUP-FEE-ALLOCATION-POLICY-DECISION.md) — ضمیمهٔ مصوب بر متن قدیمی «بسته‌بندی تصمیم‌گیری نشده» اولویت دارد.
- [ریکاوری تاریخی با جزئیات Shipping / performance](SHIPPING-ARCHITECT-RECOVERY-2026-10-16.md) — بخش‌های قدیمی next-task/چهار تب تاریخی‌اند.
- [مهارت‌های مخزنی](../../ai/AGENT-SKILLS.md) و `.cursor/skills/tooba-backend-feature/SKILL.md`، `.cursor/skills/tooba-frontend-feature/SKILL.md`.
- AMSC: `Analyze → Migrate → Structure → Certify`. `tooba-architecture-complete` فقط repair ویژه است، موج پنجم اجباری نیست.
- منابع معماری: `TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md`، `TMAR-architecture-locks.md`، `TOOBA-REFERENCE-MODULE-PATTERN.md`، `tmar-current-state.json` و `tmar-module-structure-manifests.json`.
- تصاویر baseline نامطلوب Admin: `docs/architecture/shipping/evidence/SHIPPING-ADMIN-UI-BASELINE-2026-10-21/`. فایل محلی upload ممکن است پاک شود؛ به نسخهٔ اصلی ذخیره‌شده در ریپو رجوع کنید.

## ۴. تصمیم‌های مالک که نباید دوباره گم یا اشتباه شوند

### محصول و ظاهر
- Admin اول؛ سپس Storefront و استقرار. ظاهر TB-186-R2 توسط مالک «اوکی» شد؛ این پذیرش فقط preview است، نه اتصال production.
- یک پنل مشترک با قواعد جدا برای فروشگاه/پلتفرم/فروشنده. در SingleStore انتخاب marketplace/seller مخفی و قابلیت مربوط در سرور غیرفعال باشد؛ دو موتور/دو پنل موازی نسازید.
- فهرست AppDataGrid موجود؛ نام خوانا و بدون GUID/کد فنی به‌عنوان عنوان مشتری. ویرایش صفحهٔ کامل با دو تب «اطلاعات روش» و «مناطق و هزینه‌ها»، نه مودال بلند چهار تب.
- نوع ارسال بالای نام روش. نمونه‌ها عادی/سنگین/فوق‌سنگین/سوپرمارکتی‌اند، نه فهرست جهانی بسته؛ مالکیت taxonomy و پیکربندی اجرایی هنوز تعیین/پیاده‌سازی کامل نشده است. Plus اگر مزیت عضویت است متعلق به Promotion است.
- روش‌ها، نام/توضیح و عنوان اختیاری گروه چندزبانه؛ زبان رابط مستقل از زبان محتوای درحال ویرایش. هر دو تغییر باید draft را حفظ کنند.
- زبان‌ها تعداد ثابت ندارند: یک، دو، سه یا بیشتر؛ فعال/پیش‌فرض/جهت/نام/ترتیب از دیتابیس مرجع می‌آیند. fa/en در preview فقط fixture است؛ زبان جدید را از کشور/مرورگر حدس نزنید.

### یک انتخاب جغرافیا، چند شرط/تعرفه
- گروه مناطق چند مقصد می‌گیرد؛ در هر سطح می‌توان توقف کرد. انتخاب ایران همهٔ فرزندان را می‌پوشاند؛ افزودن استان/شهر زیرش دوباره مجاز نیست.
- اگر گلپایگان انتخاب شده، افزودن اصفهان/ایران ممنوع؛ مقصد مستقل مانند قم مجاز. منع تکرار/والد/فرزند میان گروه‌های همان مالک/روش نیز برقرار است.
- **Browse با Add فرق دارد**: والد متعارض باید همچنان قابل پیمایش باشد تا به خواهر/برادر مستقل برسیم؛ فقط Add متعارض غیرفعال و علت خوانا باشد.
- geography برای هر ردیف نرخ/بسته‌بندی/برنامه دوباره وارد نمی‌شود.
- چند ردیف زیر یک گروه: وزن/تعداد/مبلغ کالاهای همان گروه؛ AND داخل ردیف، alternative میان ردیف‌ها، نه جمع نرخ‌ها یا انتخاب خودکار اولین/ارزان‌ترین ردیف.
- وزن ذخیره گرم، UI کیلو؛ بازه‌های صریح نیم‌باز `[min,max)`. نبود شرط با نبود نرخ یکی نیست؛ unmatched یا بیرون پوشش هرگز صفر/رایگان نیست.
- زمان آماده‌سازی فروشنده، مدت حمل و بازهٔ تحویل مشتری مفاهیم جدا هستند. min/max مدت تحویل preview را به PreparationDays+LeadDays تعبیر نکنید.
- هر روز هفته وضعیت فعال/تعطیل و ساعت مستقل دارد؛ مشترک‌بودن برنامه یعنی اشتراک مناطق همان روش، نه ساعت یکسان تمام روزها. تعطیلی نباید draft ساعت تایپ‌شده را پاک کند. دادهٔ legacy چندپنجره‌ای را به تک‌پنجره collapse نکنید.

### محاسبه و مالی
- پیش‌کرایه/پس‌کرایه محور مستقل از دستی/API/رایگان است؛ پس‌کرایه، COD بهای کالا نیست.
- ارز از StoreContext معتبر، مستقل از locale؛ پول و واحد تومان/ریال را حدس نزنید.
- API حامل نیازمند مرجع واقعی مبدأ/مقصد/مختصات و مشخصات بسته است؛ بدون اتصال و مستندات معتبر quote/زمان ساختگی تولید نکنید.
- رایگان پایهٔ روش با آفر ارسال رایگان Promotion جداست؛ آفر پوشش ممنوع را مجاز نمی‌کند.
- تصمیم مصوب: هزینهٔ ترکیبی حمل+بسته‌بندی+دیگر هزینه‌های ارسال در یک ردیف فاکتور، اجزا مستقل/گزارش‌پذیر. رایگان همهٔ اجزای هزینهٔ مشتری را صفر می‌کند؛ هزینهٔ واقعی و تأمین‌کنندهٔ صریح حفظ می‌شوند.
- این تصمیم اجرای مالی را اثبات نمی‌کند. PendingAllocation، مالیات/تسویه/بازپرداخت و consumer مالی معتبر را دور نزنید؛ پول/PSP/COD واقعی مجاز نشده است.
- گروه مشتری فقط seller-only نیست؛ یک فروشنده می‌تواند چند گروه کالا با مبدأ/شرایط متفاوت داشته باشد. هر گروه هزینه و روز/بازهٔ مستقل دارد؛ تغییر یکی دیگری را پاک نکند.

## ۵. آنچه واقعاً انجام و بررسی شده است

تمام counts زیر **گزارش اجرای Worker** هستند؛ Architect در این چت کد/diff/شواهد را مستقل خوانده، اما آزمون C# را اینجا دوباره اجرا نکرده است. PASS فنی با پذیرش صاحب محصول یا اجرای DB/browser واقعی برابر نیست.

| تسک | نتیجه/تصمیم Architect | خروجی واقعی و checkpoint |
| --- | --- | --- |
| TB-186 UI preview | بررسی/اصلاح شد | route مستقل `/admin/shipping-services/design-preview`؛ state حافظه، بدون business write؛ کد `1bd2ad26a2215f50d7507acd667bc703bc812e53`، checkpoint `4b7744eb0f7ba79d93e1ab063802bfa25aa1f14a` |
| 186-R1 | پذیرفته | browse در برابر Add، تعارض میان گروه‌ها، امنیت draft/owner، validation خام و first-submit؛ کد `b0ebe77985df14e771c869e093f3d203481d01a4`، شواهد `4a37697eef577963c08519616f44ff035ceb4cb1` |
| 186-R2 | ظاهر توسط مالک پذیرفته | ساعت مستقل روزها؛ 21 تست مدل؛ کد `65b2cfbd4e8e9e1ff64e4d319f2af2954977a2db`، شواهد `fefc38ce9ae9c8e741776782006b3041774a1279` |
| 187-W0 Analyze | پذیرفته با تصحیحات | نقشهٔ مسئولیت/قرارداد/legacy و D1–D7؛ کد production نداشت؛ checkpoint `e5c155ff910f817ce027023346c4483587cd9dc8` |
| 187-W1 | پذیرفته | 44 خطای معنایی در کاتالوگ/منابع canonical، اصلاح edition/tenant مجوزدهی؛ منبع `7e6b14798de7695716fee7567baa98d7654ca478`، checkpoint `a41e4274789848fc89ce7895dcce597d8bc997a7`؛ 29 تست جدید و 3 catalog guard طبق گزارش |
| 187-W2 Structure | پذیرفته برای ساختار | ShippingEndpointModule به root صحیح منتقل شد؛ guard و manifest غیرcertified؛ کد `59ce36250f109fec62572284049c607cd0f74c1f`، شواهد `0d9936f07045c9f7568a6e1f27336ac2d15ea7c2`؛ 74 focused |
| 187-W2-R1 | پذیرفته | اصلاح گارد تاریخی TB-076 با مجوز TB-079/080 برای Order shadow فقط‌خواندنی؛ exact مصرف‌کنندگان+تست منفی، بدون prod change؛ کد `6c54c8215540597f9e0a1ed630c3714ca54ae6a4`، شواهد `8fa3b7842985575ccb611eb7411be765caee00b1`؛ Shipping 382 و shadow Order 26 |
| 187-W3 | **رد شد؛ گواهی اولیه پذیرفته نشد** | Worker نبود IModuleCallTracer را optional خواند؛ skill صریحاً برای فراخوانی هم‌فرآیند هم لازم می‌داند. رکورد تاریخی حفظ/ابطال و توسط R1 جایگزین شد؛ checkpoint `2605344c10e6cd497349493357e9dcf3e6d666a4` |
| 187-W3-R1 | اصلاح پذیرفته | سه read خارجی در دو adapter با span واقعی، listener/correlation/exception/DI proof؛ کد `3b39c90ffe567cef06a5b9d048cfa92014085187`، شواهد `21d3900503475ce26052c33b97bb420cd8ec765c`؛ Shipping 407. baseline معماری موجود certified؛ نه محصول جدید |
| TB-188 | از بررسی کد پذیرفته | GET authoring-context با edition/ارز/زبان‌های فعال واقعی از Contracts؛ snapshot زبان metadata افزایشی؛ کد `f12e3e1ae2942e4dfa4ef1d8687d2dfacf9383cb`، checkpoint `5b74601fe6db10bde1575eb680b8104f2ac553a5`؛ Shipping 451 و LanguageLookup 6 |
| TB-189 | از بررسی کد پذیرفته | مالک سرورساخته Store/Marketplace/Seller و مجوز عملیات همان seller؛ دو GET و یک use case؛ کد `504df9888238ad85d0b183fcba4bc41ff5a1b5ec`، شواهد `0582151550634dfb591b60e36c77635fc82d1754`، checkpoint `676b7efcb9d4f402e662d5f955e82a33b252c835`؛ focused149، Shipping511 |
| TB-190 | **فقط صادر شده؛ نتیجه منتظر** | owner-isolated draft + گروه/مقاصد، CRUD، revision، ancestry و مهاجرت افزایشی آماده‌سازی‌شده؛ هیچ نتیجه‌ای را پیشاپیش PASS ندانید |

منابع هر ردیف: `docs/architecture/evidence/<Task-ID>/` و receipt در `docs/ai/tasks/<Task-ID>.task.md`، در صورت ثبت Worker. اصل task190 فعلاً در آرشیو issued است.

تخلفات/محدودیت‌های ثبت‌شده:
- 188 برخلاف محدوده build کامل solution اجرا کرد؛ در review ثبت و 189 تکرار نکرد. build گسترده ارزش پذیرش اضافی ندارد.
- W3 به‌اشتباه یک الزام tracing را debt اختیاری اعلام کرد؛ R1 به‌صورت صریح اصلاح و certificationHistory نگه‌داری شد. این تصمیم نمونهٔ مجوز تضعیف skill نیست.
- شواهد Source/DOM/fixture را به screenshot واقعی، مرورگر کاربر، PostgreSQL یا identity DB واقعی تبدیل نکنید. preview اولیه screenshot قابل‌اتکا نداشت؛ ادعای گالری تصاویر نشده است.
- متغیرهای عمومی «D1–D7 هنوز pending» در نتایج قدیمی پس از 188/189 باید با وضعیت تفکیکی زیر تفسیر شوند، نه دوباره از صفر تحلیل شوند.

## ۶. نقشهٔ مالکیت و مسیرهای فعلی کد

| مسئولیت | مرجع/مسیر فعلی |
| --- | --- |
| هویت سرویس/روش و binding، shared catalog و ترجمه‌های اصلی | Fulfillment.Contracts.Shipping؛ Shipping حق catalog/seed موازی ندارد |
| نرخ/پوشش/بسته‌بندی/برنامهٔ موجود | Shipping؛ `shipping` schema و ShippingDbContext؛ legacy موجود فعلاً global/بدون مالک جدید |
| هویت و ancestry مکان | Geography.Contracts.Places؛ رابطهٔ ParentAdministrativeAreaId می‌تواند عمق دلخواه داشته باشد |
| فعال/پیش‌فرض/جهت/نام/ترتیب زبان | Localization.Contracts.Ports.ILanguageLookup؛ bridge از LanguageDirectory روی LocalizationDbContext |
| بازار/ارز پیش‌فرض/کانال | StoreContext.Contracts.Current.ICurrentStoreCommerceContext |
| edition/tenant | BuildingBlocks ICurrentEdition/ICurrentTenant/ICurrentCommerceContext و resolver پلتفرم |
| actor و seller مجاز | canonical ISellerPanelAccess؛ candidate header به‌تنهایی مجوز نیست |
| مجوز مؤثر همان seller | IPlatformEffectiveAccessReader(actor, Seller, sellerPartyId)، permission موجود، فقط GlobalWithinOwner و بدون DeniedByCeiling |
| قیمت پذیرفته‌شدهٔ مشتری/سفارش/پرداخت | Order/Payment و snapshot معتبر؛ draft/operational quote حق تصاحب آن را ندارند |

مسیرهای مهم:
- `src/backend/Modules/Shipping/Tooba.Shipping.Application/Authoring/`: context/owner query، model و port داخلی.
- `...Shipping.Infrastructure/Authoring/ShippingAuthoringContextSource.cs`: read از Contracts Localization/StoreContext، با tracing.
- `...Shipping.Endpoints/Admin/ShippingInstallationContext.cs`: helper مشترک edition/active-tenant، نه mode authority جدید.
- `.../Admin/ShippingAuthoringOwnerAuthorizer.cs`: admin owner gate و seller gate+مجوز همان owner با span AccessControl.
- `.../ShippingEndpointModule.cs`: ترکیب HTTP/ارائه؛ Host فقط registration/map.
- `...Shipping.Infrastructure/Rates/{GeographyDestinationScopeValidator,FulfillmentShippingMethodOptionValidator}.cs`: سه call خارجی traced؛ tracer اجباری است.
- `...Shipping.Tests/Certification/ShippingRequestClassificationTests.cs` و `docs/architecture/shipping/shipping-request-classification.json`: manifest اکنون requestمحور است؛ هر request می‌تواند چند route داشته باشد.
- `...Shipping.Tests/Compatibility/OperationalQuoteCompatibilityTests.cs`: گارد سه نقش مجاز Order + محتوای مصرف، تست منفی اجرای غیرمجاز.
- `...Shipping.Tests/Observability/ShippingModuleCallTracingTests.cs`: listener واقعی، parent/correlation، exception/cancellation/PII.

APIهای جدید فعلی:
- `GET /v1/admin/shipping-services/authoring-context`: 188، edition/defaultCurrency/active contentLanguages/defaultContentLanguageId؛ پارامتر override ندارد.
- `GET /v1/admin/shipping-services/authoring-owner`: Store/Marketplace scope مجاز، بدون tenant داخلی در پاسخ.
- `GET /v1/seller/shipping-services/authoring-owner`: فقط Marketplace و seller مجاز با fulfillment.view همان مالک؛ SingleStore پیش از seller resolution با 404 بسته می‌شود.
- 189: 19 route، 18 request، 16 request validator، 2 NO_VALIDATOR_REQUIRED، 6 nested validator؛ اعداد snapshot‌اند، guard باید مجموعهٔ واقعی را اثبات کند نه فقط count.
- خطاها: 44 legacy همان 400/Business/Warning؛ configuration_unavailable =503؛ owner_scope_denied=403؛ owner_scope_unavailable=404؛ descriptor/resource یک مالک و fallback canonical.

## ۷. وضعیت تصمیم‌های D1–D7 و برنامهٔ وابستهٔ ادامه

| موضوع | وضعیت و آنچه هنوز لازم است |
| --- | --- |
| D1 مالک | authority model و read gate در 189 موجود؛ **stored owner isolation و rule writes هنوز موجود نیست**؛ 190 برای شروع آن صادر شده |
| D2 taxonomy نوع ارسال | طرح اجرایی/مالکیت/config/ترجمه/اثر عملیاتی باز؛ مثال‌های UI را enum جهانی یا حدس از وزن نکنید |
| D3 گروه مناطق و legacy | قرارداد هدف مصوب؛ 190 draft/group/area و prepare-only migration را خواسته؛ نتیجه هنوز نداریم. legacy attribution/backfill نیازمند تسک و تصمیم صریح |
| D4 محاسبه/پرداخت | دستی/API/رایگان و پیش/پس مستقل مصوب‌اند؛ مدل نویسندگی و اجرای جدید کامل نیست. free-above legacy را خودکار Promotion/free mode نکنید |
| D5 برنامه/cutoff | ساعات مستقل روزها در preview؛ legacy operating windows موجود؛ closed/absent/cutoff و اتصال واقعی نیازمند طراحی/پیاده‌سازی محدود، بدون collapse داده |
| D6 زبان | 188 API فعال/پیش‌فرض/metadata از منبع DB را فراهم کرده؛ **اتصال UI واقعی و پذیرش runtime DB هنوز pending**؛ در Marketplace رجیستری نصب مشترک است، تنظیم per-seller ساخته نشود |
| D7 اجرای مهاجرت | 190 فقط آماده‌سازی SQL/مدل/مهاجرت افزایشی، نه apply مشترک؛ proof PostgreSQL و rollout واقعی هنوز task/gate جدا دارد |

ادامهٔ منطقی پس از **بررسی نتیجهٔ 190**، فقط برنامه و نه تسک اجرایی/مجوز خودکار:
1. رفع هر نقص محدود 190؛ تأیید مالکیت ذخیره‌شده، ancestry واقعی، revision/atomic replacement و SQL/محدودیت proof.
2. افزودن ردیف‌های شروط/تعرفه زیر group IDs پایدار: وزن/تعداد/مبلغ گروه، حمل/بسته‌بندی و مدت تحویل؛ AND/full-tuple overlap و بدون جغرافیای تکراری. مبنای مبلغ قبل/بعد تخفیف/مالیات باید صریح شود، نه حدس در کد.
3. تکمیل metadata/نام‌های چندزبانهٔ مالک، taxonomy صحیح، محور محاسبه و پرداخت، برنامه هفتگی/cutoff با منابع و legacy compatibility. ترتیب دقیق slice بر اساس نتیجهٔ 190 تعیین شود؛ scope پُرحجم را در یک task 45 دقیقه‌ای جمع نکنید.
4. اتصال UI مصوب به API واقعی، زبان‌های DB و حالت نصب؛ SingleStore بدون انتخاب marketplace، View بدون write، draft safety/RTL/LTR و read-after-write واقعی. از preview state/fixture به‌عنوان production store استفاده نکنید.
5. اثبات relational واقعی با محیط test-owned ایزوله و مهاجرت امن، مرورگر واقعی Admin، create/edit/reload/conflict/access-denied، بررسی کاربر. EF InMemory اثبات UNIQUE/transaction/concurrency PostgreSQL نیست.
6. سپس Storefront با گروه‌ها/هزینه/زمان‌های مستقل و authority معتبر؛ سپس rollout/استقرار با gateهای مالی و عملیاتی واقعی. نه تغییر پرچمِ پنهانی و نه ادامهٔ Home performance.

برآورد قبلی به مالک: حدود **6–9 تسک محدود پس از 189** برای تکمیل Admin، نه تعهد زمانی/تعداد قطعی. با هر نتیجه بازبینی شود؛ Storefront و deployment داخل این برآورد نبودند. چیزی برای حفظ این برآورد از acceptance حذف نشود.

## ۸. محدودهٔ دقیق تسک فعال 190

اصل صادرشده: [TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190.task.md](issued/TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190.task.md).

- draft مستقل Shipping با binding موجود و immutable؛ نه catalog دوم و نه روش operationally active.
- گروه‌های مناطق با IDs پایدار، چند مقصد در هر گروه، منع duplicate/ancestor/descendant میان تمام گروه‌های همان draft.
- مسیر ancestry واقعی از Geography؛ GUID equality/نام/prefix و ancestors ارسالی client کافی نیست. admin areas تودرتو، city→area و serviceArea→city بررسی شوند.
- CRUD مشترک admin/seller با authority 189؛ view/manage جدا؛ full owner predicate در تمام read/list/update حتی در DB جداگانه.
- replace کامل region tree با revision واقعی و یک مرز atomic SaveChanges؛ خطای گروه آخر نباید گروه‌های قبلی را پاک/نیمه‌ذخیره کند.
- migration افزایشی فقط روی جدول‌های جدید draft/group/area و own-FK؛ snapshot تازه مجاز، migrationهای قدیمی byte-identical. بدون backfill/legacy attribution.
- SQL/model/concurrency/EF roundtrip proof و مرز InMemory شفاف؛ **عدم اجرای Postgres نباید به‌عنوان اثبات runtime معرفی شود**.
- نرخ/metadata کامل/ترجمهٔ مالک/حالت/تقویم/quote/activation/frontend در scope 190 نیستند.
- no shared DB/app mutation؛ Task 190 apply به `:5432/tooba_alpha` را مجاز نکرده است.

## ۹. خطوط قرمز معماری و ایمنی در هر ادامه

- هر قابلیت در ماژول صاحبش؛ foreign Application/Infrastructure/Domain/Endpoints، DbContext/DbSet/schema join/foreign FK/transaction مشترک ممنوع.
- Host composition/platform فقط؛ business/API ماژول/persistence را به Host برنگردانید.
- API module-owned، ISender/CQRS واقعی، FluentValidation با input provenance، Result/SemanticError/ApiResponseFactory، error descriptor تک‌مالک و resources/resolver canonical.
- هر call بین ماژول‌ها tracing canonical می‌خواهد؛ MediatR parent span یا precedent sibling جای IModuleCallTracer نیست.
- فرانت قبل هر تغییر مهارت frontend و استانداردش را بخواند؛ API/state/UI جدا، بدون موتور قواعد backend در مرورگر، design system و AppDataGrid موجود، frozen surfaces محفوظ.
- skill توسعه feature برای پیشگیری از debt است، مجوز AMSC مجدد یا recertification همه‌چیز نیست. Certify بدون proof همهٔ prerequisiteهای applicable PASS نشود.
- آزمون هدفمند، یک تعمیر deterministic و rerun محدود؛ نه حلقهٔ full build/test یا اصلاح نامرتبط برای سبزشدن.
- `:3000/:3419/:5088/:5432`، `tooba_alpha`، Redis/RabbitMQ، shared bin/obj/.next، stashes و dirty/untracked دیگران را دست نزنید.
- PID/URL preview ثبت‌شده تاریخی `127.0.0.1:3419/admin/shipping-services/design-preview` است؛ زنده‌بودن سرور/PID قدیمی را فرض نکنید.
- no reset/stash/rebase/force-push، no secret/credential logs و no dummy/probe به Bridge /api/results.
- user approval بصری، worker technical PASS، architecture certification، relational runtime proof و production release هر کدام gate مستقل‌اند.

## ۱۰. متن کوتاه برای شروع چت بعدی

«ریکاوری Tooba را از `docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md` بخوان. Admin Shipping اولویت دارد؛ 189 پذیرفته و 190 صادر شده و منتظر نتیجه است. قبل از تسک تازه نتیجه/وضعیت همان 190 و HEAD واقعی را بررسی کن. مسیر Home performance بعد از185 متوقف است. معماری تمیز frontend/backend، Contracts-only، زبان‌های دیتابیس و SingleStore/Marketplace را حفظ کن. فقط یک task دانلودی/preview در هر نوبت، بعد review نتیجه و task بعدی.»

## ۱۱. وضعیت واقعی TB-191 (2026-10-22)

- TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191 اجرا و نتیجه‌اش به Bridge ارسال شد: ردیف‌های نرخ دستی زیر
  `group_id` پایدار، بازه‌های نیم‌باز سه‌محوره، ارز trusted و اتمی بودن یک `SaveChanges` با نشانگر هم‌زمانی
  ریشهٔ مشترک. اثبات رابطه‌ای روی PostgreSQL دورریختنی همان تسک اجرا و کانتینر پاک شد.
- معنای ذخیره‌شده و دروازهٔ باز در `SHIPPING-ADMIN-RULES-DECISION.md` بخش ۲۱ ثبت شده‌اند؛ جزئیات اجرا در
  `docs/architecture/evidence/TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191/`.
- **دروازهٔ باز:** مبنای authoritative اندازه/مبلغ کالا حل نشده است. هیچ quote/ارزیاب/فعال‌سازی/انتشار/
  ویرایشگر کامل مجاز نیست تا Architect آن را صریح باز کند. Worker PASS تنها اثبات فنی است و Architect ACCEPT
  نیست؛ تسک بعدی خودکار نیست.

## ۱۲. وضعیت TB-191-R1 (2026-10-22)

- اصلاح هم‌ارزی قیدهای ذخیره‌سازی (R191-01) و اثبات اجرایی مسابقهٔ «نوشتن ردیف نرخ» با «جایگزینی درخت
  منطقه/حذف گروه» (R191-02) انجام و نتیجه به Bridge ارسال شد؛ جزئیات در
  `docs/architecture/evidence/TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191-R1/`.
- قاعده‌ها در `SHIPPING-ADMIN-RULES-DECISION.md` بخش ۲۲ ثبت شده‌اند. تعریف قیدها/مهاجرت اصلاحی فقط روی
  دیتابیس دورریختنی همان تسک اجرا شد.
- **دروازهٔ باز:** مبنای authoritative اندازه/مبلغ کالا همچنان حل نشده است؛ quote/ارزیاب/فعال‌سازی/انتشار
  و ویرایشگر کامل مجاز نیست. Worker PASS اثبات فنی است، نه Architect ACCEPT؛ تسک بعدی خودکار نیست.

## 13. TB-192 / TB-192-R1 outcome pointer

TB-SHIPPING-ADMIN-LOCALIZED-DRAFT-METADATA-192 delivered bounded persisted language-keyed owner
presentation (method name/description + optional group titles) with one additive migration and
executed relational/race proof (579/579 whole Shipping suite). This is not Admin/production
acceptance, not editor activation, and not module certification; configured-registry runtime/UI
acceptance remains a later integration gate. Runtime-Merchandise-Basis-And-Measurement-Gate stays
OPEN_NOT_IMPLEMENTED. Await Architect review; no automatic next task.

TB-SHIPPING-ADMIN-LOCALIZED-DRAFT-METADATA-192-R1 (worker, 2026-10-22) closed the R192-01/R192-02
proof gaps: real admin/seller presentation GET/PATCH through the real authorizer, validation pipeline,
handlers/projection and directory; one/two/three-plus dynamic-language matrix with deactivate/reactivate/
default-change/raw-text retention; selected clear and true no-ops; interface-culture invariance;
pre-dispatch denials and malformed-input preflight; and an independent raw PostgreSQL 17 storage matrix
on the two new tables plus retained-text reorder/rate-edit proof. Executed 589/589 whole Shipping suite;
production executable behavior unchanged (one XML remark corrected). Worker PASS only — not Architect
ACCEPT; no TB-193 and no automatic next task.

## 14. TB-193 outcome pointer (2026-10-22)

TB-SHIPPING-ADMIN-WEEKLY-SCHEDULE-DRAFTS-193 delivered the bounded D5 per-weekday draft operating
schedule: one owner-scoped schedule attached ONCE to the draft root, explicit validated timezone, exactly
seven unique ISO weekdays with explicit working/closed flags and independent same-day intervals,
remembered closed-day hours, truthful UNCONFIGURED versus configured-all-closed, whole-week preflight plus
one atomic SaveChanges on the shared root revision. Two new Shipping-owned tables plus one additive
migration; authorized admin/seller GET/PUT through the real owner authorizer; executed PostgreSQL 17
roundtrip, raw constraint (SQLSTATE + owned constraint) and schedule-vs-rate race proof; whole Shipping
suite 609/609. Legacy multi-window schedule rows and errors unchanged (74/74 legacy schedule tests).
Rules recorded in `SHIPPING-ADMIN-RULES-DECISION.md` section 24; details in
`docs/architecture/evidence/TB-SHIPPING-ADMIN-WEEKLY-SCHEDULE-DRAFTS-193/`.

**Open gates unchanged:** cutoff/holiday/capacity/customer-promise are NOT implemented; drafts remain
inactive configuration; Runtime-Merchandise-Basis-And-Measurement gate stays OPEN_NOT_IMPLEMENTED and
TB-185 stays paused. Worker PASS is technical proof only, not Architect ACCEPT; no automatic next task.

TB-SHIPPING-ADMIN-WEEKLY-SCHEDULE-DRAFTS-193-R1 (worker, 2026-10-22) closed R193-01 by replacing the
weak race fact with the established real `SavingChanges` two-reader barrier and strict assertions:
both participants proven to arrive with the original root token 1, exactly one winner and exactly one
loser with the canonical `AuthoringDraftRevisionConflict`, revision advanced exactly once, the actual
winner's capability asserted present and the loser's absent in both orders, raw day/rate row counts
proving no hybrid, and the ACTUAL losing context proven empty and re-reading the winner. Test-only
repair plus evidence correction: no executable production, contract, schema or migration change.
Re-executed 609/609 whole Shipping suite; the repaired fact passed 5/5 consecutive focused runs.
Worker PASS only — not Architect ACCEPT; no automatic next task.
