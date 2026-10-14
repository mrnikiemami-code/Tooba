# TB-SHIPPING-MVP-GEOGRAPHY-ADMIN-056 — خواندن مجاز Geography و فعال‌شدن ایجاد نرخ/پوشش

وضعیت: PASS (کد آماده است؛ Host در حال اجرا کد قدیمی را اجرا می‌کند و ری‌استارت نشده، پس اثبات
زمان‌اجرای HTTP ارائه نمی‌شود).

## A — مسیرهای فقط-خواندنی ادمین Geography

پروژهٔ جدید و ماژول‌مالک: `src/backend/Modules/Geography/Tooba.Geography.Endpoints` (فقط Contracts +
BuildingBlocks را می‌شناسد؛ هیچ DbContext و هیچ منطق کسب‌وکاری در Host).

مسیرها (همه زیر `/v1/admin`، همه با مجوزدهی واقعی):

| مسیر | خروجی |
| --- | --- |
| `GET /v1/admin/geography/countries` | کشورها |
| `GET /v1/admin/geography/countries/{countryId}/administrative-areas` | ناحیه‌های اداری کشور |
| `GET /v1/admin/geography/countries/{countryId}/cities?administrativeAreaId=` | شهرها (اختیاری محدود به ناحیه) |
| `GET /v1/admin/geography/cities/{cityId}/service-areas` | ناحیه‌های خدمت شهر |

- خروجی `GeographyPlaceOption` است: `id` (شناسهٔ پایدار GUID)، `code`، `label` (محلی‌شده با
  `languageId` اختیاری) و `parentId` و `isActive`.
- برچسب از `IGeographyLabelLookup` می‌آید و **در نبود ترجمه به کد پایدار برمی‌گردد**؛ هیچ نام
  ساختگی تولید نمی‌شود.
- مجوز: `fulfillment.view` — همان مجوز موجودِ سطح ادمین «خدمات ارسال» که این خواندن ورودی آن است.
  هیچ مجوز تازه‌ای ساخته نشد، هیچ فایلی در `Host/Admin` یا `Host/Security` اضافه نشد (گاردهای
  AMSC ۱۷/۱۹ فایلی دست‌نخورده‌اند) و هیچ مسیر بی‌مجوزی وجود ندارد. نبود سرویس مجوز = ۵۰۳
  (`admin.authorization.unavailable`)، رد = ۴۰۳.
- مسیر قدیمی storefront (`/v1/storefront/geography/provinces` در Order) دست‌نخورده ماند.

## C — اعتبارسنجی مقصد روی مسیر نوشتن Shipping

- درز `IShippingDestinationScopeValidator` (Application) و آداپتور
  `GeographyDestinationScopeValidator` (Infrastructure) که فقط
  `IGeographyLookup.GetByIdsAsync` را از **Contracts** صدا می‌زند؛ هیچ DbContext جغرافیایی و هیچ FK
  میان‌طرح‌واره‌ای وجود ندارد (ارجاع Contracts-to-Contracts، همان الگوی `Shipping.Contracts`).
- `CreateShippingRatePolicyHandler` و `CreateShippingCoverageGrantHandler` **پیش از هر نوشتن**
  دامنه را می‌سنجند؛ شناسهٔ ناشناخته/غیرفعال/ناهم‌بسته با کد پایدار
  `shipping.destination_scope.invalid` رد می‌شود و به هیچ persistence نمی‌رسد.
- گاردهای ۰۵۵ (مرجع‌بودن کد خدمت مسیر، رد تغییر میان‌خدمتی با
  `shipping.service_scope.mismatch`، دامنهٔ ثابت در ویرایش، مجموعهٔ خالی پوشش = «بدون پوشش»)
  بدون تغییر حفظ شدند.

## B — ایجاد واقعی در همان دیالوگ موجود

`ShippingRatesCoverageDialog` همان صفحهٔ `/admin/shipping-services` است؛ هیچ صفحهٔ دوم ساخته نشد.

- کامپوننت تازهٔ `ShippingDestinationPicker` یک آبشار واقعی است: کشور → استان → شهر → ناحیهٔ خدمت،
  همه از خواندن مجاز Geography و همه با `id` پایدار. برچسب فقط نمایش است.
- سطوح بالادست صریح‌اند: «بدون استان (دامنهٔ کشوری)»، «بدون شهر (دامنهٔ بالاتر)»، «بدون ناحیه
  (دامنهٔ شهر)»؛ بنابراین دامنهٔ کشوری/استانی/شهری بدون حدس انتخاب می‌شود.
- تغییر والد، انتخاب‌های وابسته را پاک می‌کند (`clearDependents`) و payload فقط تا سطح انتخاب‌شده
  نوشته می‌شود (`buildScopeWrite`)، پس شناسهٔ ناهم‌بسته ارسال نمی‌شود و شناسهٔ دستی/ساختگی جایی
  ندارد.
- «افزودن نرخ» روی `POST /v1/admin/shipping-services/{serviceCode}/rates` و «افزودن مجوز پوشش» روی
  `POST .../coverage-grants` با `serviceCode` همان ردیف انجام می‌شود؛ پس از موفقیت، نما از سرور
  بازخوانی می‌شود. ویرایش/فعال‌سازی/غیرفعال‌سازی ۰۵۵ حفظ شده است.
- معنای‌ها دست‌نخورده: روزهای آماده‌سازی و حمل جدا، آستانهٔ ارسال رایگان خالی = «بدون آستانه»
  (نه صفر)، مجموعهٔ خالی پوشش = «بدون پوشش» (نه نامحدود)، خطاها با کد پایدار و متن روشن.

## راستی‌آزمایی

- `Tooba.Shipping.Tests`: ۸۲/۸۲ پاس (`log-shipping-tests.txt`) شامل تست‌های تازهٔ
  رد مقصد نامعتبر پیش از نوشتن، عبور یک‌بارِ مقصد معتبر به directory، و گاردهای منبع‌محور
  Geography/Shipping.
- بیلد Host با خروجی هدایت‌شده: `Build succeeded` (`log-host-build.txt`). هیچ ری‌استارت/دیپلوی، هیچ
  apply مهاجرت، هیچ DDL/DML روی پایگاه‌داده انجام نشد.
- گاردهای ساختاری Host: ۱۱۸/۱۱۹ پاس (`log-host-structural-guards.txt`). تنها شکست همان
  `HostAdminCanon001GuardTests.Contract_files_expose_no_persistence_types` پیش‌موجود است (ارجاع به
  مسیر قدیمی Party که در HEAD وجود ندارد و این تسک هیچ فایل Party را تغییر نداده).
- فرانت: `npx tsc --noEmit` تمیز (`log-frontend-tsc.txt`)؛ تست‌های متمرکز ۲۷/۲۷ پاس
  (`log-frontend-tests.txt`) شامل سطح دامنه، حذف انتخاب‌های وابسته، رد انتخاب ناهم‌بسته و معنای
  آستانه/پوشش.

## امتیاز زنده/آماده

کد مسیرها و اعتبارسنجی **آماده** است و کامپایل می‌شود، اما Host در حال اجرا (که bin را قفل کرده)
ری‌استارت نشده؛ پس هیچ اثبات HTTP زمان‌اجرا ادعا نمی‌شود. پس از ری‌استارت مجاز Host، مسیرهای
`/v1/admin/geography/*` قابل آزمون دودی مجاز هستند. سوئیچ قیمت ارسال در Checkout همچنان انجام
نشده و پوشش/نرخ پیش‌فرضی ساخته نشد.
