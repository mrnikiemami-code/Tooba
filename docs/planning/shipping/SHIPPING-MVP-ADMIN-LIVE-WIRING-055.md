# TB-SHIPPING-MVP-ADMIN-LIVE-WIRING-055 — نرخ/پوشش Shipping روی سطح ادمین موجود

وضعیت: PARTIAL (A و C کامل؛ B یک مسیر عمودی واقعی خواندن/ویرایش/فعال‌سازی، با ایجاد دامنهٔ
جغرافیایی مسدود و اعلام‌شده).

## A — فعال‌سازی مسیرهای HTTP ادمین (انجام شد)

- `Tooba.Host.csproj`: ارجاع فقط‌افزودنی به `Tooba.Shipping.Endpoints`.
- `Program.cs`:
  - `using Tooba.Shipping.Endpoints.Admin;`
  - ثبت اسمبلی MediatR با همان قرارداد `AddToobaCqrsFoundation` (نه ثبت موازی).
  - `app.MapShippingEndpoints();` بعد از آخرین نگاشت‌های ماژول موجود (ترتیب موجود دست‌نخورده).
  - ثبت `IShippingAdminAuthorizer` روی پیاده‌سازی ماژول `ShippingAdminAuthorizer`.
- `IModuleSchemaMigrator<ShippingDbContext>` ثبت **نشد** (DDL خودکار خاموش می‌ماند)؛ Host
  ری‌استارت/دیپلوی نشد.

### چرا آداپتور مجوز در Host اضافه نشد

مجموعهٔ فایل‌های `Host/Admin` (۱۷ فایل، گاردهای `HostAdminCanon00x` و
`HostAdminCanonicalCertificationGuardTests`) و `Host/Security` (۱۹ فایل،
`HostSecurityAmcCertGuardTests`) تحت گاردهای AMSC قفل‌اند. افزودن
`HostShippingAdminAuthorizer.cs` این گاردها را می‌شکست و تضعیف گارد معماری مجاز نیست.
بنابراین همان درز انتزاعی `IShippingAdminAuthorizer` نگه داشته شد و پیاده‌سازی پیش‌فرض روی
همان seamهای عمومی پلتفرم (`IAdminPanelAccess` + `IAuthorizationService` + `ICurrentTenant` از
`Tooba.BuildingBlocks`) در ماژول Shipping انجام شد؛ رفتار fail-closed و کدهای
`admin.authorization.denied` / `admin.authorization.unavailable` دقیقاً مثل
`HostSupportAdminAuthorizer` حفظ شده‌اند.

### مجوزها (بدون اختراع مجوز تازه)

هیچ شناسهٔ مجوز جدیدی در `PermissionCatalog` (خارج از مسیرهای مجاز این تسک) ساخته نشد و هیچ
مسیر بی‌مجوز/ناشناخته‌ای باز نشد. مسیرهای نرخ/پوشش داخل همان سطح ادمین «خدمات ارسال» هستند و
با مجوزهای موجود همان سطح محافظت می‌شوند:

- خواندن: `fulfillment.view`
- نوشتن: `fulfillment.manage`

هر ششت مسیر یک فراخوانی `authorizer.RequireAuthorizedAsync(...)` دارند (تست منبع‌محور این را
قفل می‌کند) و هیچ `AllowAnonymous` وجود ندارد. گیت پنل + قابلیت، در نبودِ سرویس مجوز ۵۰۳
(fail-closed) می‌دهد.

## C — درستی مرز خدمت (انجام شد)

- کد خدمت مسیر مرجع قطعی است: بدنه فقط اگر همان باشد پذیرفته می‌شود، وگرنه
  `shipping.validation.body_service_code_mismatch`. پس هیچ کلاینتی با بدنه مسیر را دور نمی‌زند.
- گارد مالکیت ردیف: `UpdateRate`، `SetRateActive` و `SetGrantActive` کد خدمت مسیر را با
  `ServiceCode` ذخیره‌شده می‌سنجند و تغییر میان‌خدمتی را با
  `shipping.service_scope.mismatch` رد می‌کنند (بدون هیچ DbContext بیگانه).
- تغییر دامنهٔ جغرافیایی یک ردیف موجود از این درز ممنوع است (دامنه ثابت می‌ماند).
- دامنهٔ تکراری و نبود ردیف کدهای پایدار موجود را برمی‌گردانند؛ مجموعهٔ خالی پوشش به‌معنای
  «بدون پوشش» است و هرگز «بدون محدودیت» خوانده نمی‌شود.

## B — بخش نرخ و پوشش در همان صفحهٔ موجود (PARTIAL)

در `/admin/shipping-services` یک کنش ردیفی «نرخ و پوشش» اضافه شد که دیالوگ
`ShippingRatesCoverageDialog` را با `code` پایدار همان خدمت باز می‌کند. این بخش:

- نرخ‌ها و پوشش واقعی را از `GET /v1/admin/shipping-services/{serviceCode}/rates` می‌خواند؛
- مبلغ/ارز، روزهای آماده‌سازی و حمل **جدا**، آستانهٔ ارسال رایگان (خالی = بدون آستانه)، وضعیت
  فعال و خلاصهٔ پوشش (`بدون پوشش — ...`) را نشان می‌دهد؛
- ویرایش مبلغ/زمان‌ها (`PUT .../rates/{id}`) و فعال/غیرفعال‌سازی نرخ و مجوز پوشش
  (`PATCH .../active`) را روی HTTP واقعی انجام می‌دهد و سپس از سرور بازخوانی می‌کند؛
- هیچ صفحهٔ دوم، CRUD تکراری Fulfillment یا ذخیرهٔ درون‌حافظه‌ای/جعلی ندارد.

### مانع بازِ باقی‌مانده (صادقانه)

«ایجاد نرخ/پوشش جدید» به دامنهٔ مقصد نیاز دارد و **هیچ API مجاز و امنی شناسه‌های پایدار
Geography را برنمی‌گرداند** (Geography فقط Contracts/Domain دارد و Endpoints ندارد؛ مسیر
storefront `geography/provinces` هم فقط فهرست نامی/ثابت Order است). بنابراین:

- کنترل ایجاد در UI **آشکارا غیرفعال** است و مانع با متن صریح نشان داده می‌شود
  (`GEOGRAPHY_ID_LOOKUP_BLOCKED_FA`)؛
- هیچ شناسهٔ ساختگی/UUID دستی قابل ارسال نیست.

رفع کامل B به یک خواندن مجاز Geography (Contracts/Endpoints + مجوز واقعی) نیاز دارد که بیرون
از بودجهٔ ۳۰ دقیقهٔ این تسک است.

## راستی‌آزمایی

- `Tooba.Shipping.Tests`: ۷۳/۷۳ پاس (`log-shipping-tests.txt`).
- بیلد Host با خروجی هدایت‌شده (چون پروسهٔ Host در حال اجراست و bin را قفل کرده):
  `Build succeeded` (`log-host-build.txt`). هیچ ری‌استارت/دیپلوی انجام نشد.
- گاردهای ساختاری Host (Admin/Security): ۱۱۵/۱۱۶ پاس. تنها شکست،
  `HostAdminCanon001GuardTests.Contract_files_expose_no_persistence_types` است که به مسیر
  قدیمی `Tooba.Party.Contracts/IPartyAdminSellerReadGateway.cs` ارجاع می‌دهد؛ این فایل در HEAD
  وجود ندارد و تسک ۰۵۵ هیچ فایل Party را تغییر نداده → شکست **پیش‌موجود و بی‌ربط**
  (`pre-existing-guard-note.txt`, `log-host-structural-guards.txt`).
- فرانت: `npx tsc --noEmit` بدون خطا (`log-frontend-tsc.txt`)؛ تست‌های متمرکز فرانت ۲۳/۲۳ پاس
  (`log-frontend-tests.txt`).
- هیچ تغییری در Checkout/Order/Payment/Settlement، هیچ تغییر snapshot/قیمت، هیچ DDL/DML و هیچ
  migration apply انجام نشد (`changed-paths.txt`).

## امتیاز شواهد زمان اجرا

Host در حال اجرا کد قدیمی را اجرا می‌کند (ری‌استارت ممنوع)، پس کد این تسک **آماده است ولی
زنده نیست**؛ هیچ اثبات زمان‌اجرای جعلی ارائه نمی‌شود. مسیرهای فعال‌شده پس از ری‌استارت مجاز
Host قابل آزمون دودی خواندنی‌اند.
