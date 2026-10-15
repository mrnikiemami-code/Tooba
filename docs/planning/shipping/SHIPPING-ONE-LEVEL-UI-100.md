# SHIPPING-ONE-LEVEL-UI-100 — برچسب فروشگاه یک‌سطحی (فقط نمایش، بدون قیمت/واجدیت)

Task: `TB-SHIPPING-ONE-LEVEL-TABS-100` (Program `TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE`, Mode
`BOUNDED_STOREFRONT_ONE_LEVEL_LABEL_PARITY`), base `ec5e693376b56d94441e017df4783e136c63525a`.

## مسئله

نام نمایشی روش یک‌سطحی در `fulfillment.shipping_method_translations` ذخیره می‌شود (ویرایش‌شده در ۰۹۴)،
اما برچسب مشتری در فروشگاه هنوز از ترجمهٔ سرویس/گزینهٔ قدیمی ساخته می‌شد:

```text
label = $"{serviceName} — {optionName}"   // "پست — معمولی"  برای  post:standard
```

یعنی نام ویرایش‌شدهٔ ادمین هرگز به مشتری نمی‌رسید.

## راه‌حل (فقط برچسب، مالکیت درست)

1. **قرارداد Fulfillment (Contracts-only)**: `IShippingOneLevelStorefrontLabelReader`
   در `Tooba.Fulfillment.Contracts/Shipping/ShippingOneLevelStorefrontLabelContracts.cs`.
   ورودی: کدهای پایدار روش + `languageId` حل‌شدهٔ درخواست؛ خروجی: نگاشت `methodCode → نام نمایشی`.
   هیچ نوع EF/DbContext در Contracts نیست.
2. **پیاده‌سازی Fulfillment**: `ShippingOneLevelStorefrontLabelRead` (Infrastructure/Shipping).
   - دروازهٔ `ShippingOneLevelCatalogPolicy` خاموش ⇒ نگاشت خالی و **هیچ پرس‌وجویی**.
   - خواندن از همان `IShippingOneLevelCatalogReader` موجود (خوانندهٔ دومی ساخته نشد).
   - فقط تطبیق **دقیق** `LanguageId` درخواستی؛ نبود ترجمه = برچسب قدیمی (هیچ زبانی حدس زده نمی‌شود).
   - `PostgresErrorCodes.UndefinedTable` ⇒ نگاشت خالی (کاتالوگ مهاجرت‌نشده = برچسب قدیمی).
   - کد خالی/سفیدسفید پیش از هر خواندن رد می‌شود؛ کد در Order یا مرورگر تجزیه نمی‌شود.
3. **DI**: ثبت `AddScoped<IShippingOneLevelStorefrontLabelReader, ShippingOneLevelStorefrontLabelRead>()`
   در `FulfillmentModule`.
4. **Order (فقط برچسب)**: `StorefrontShippingService.ApplyOneLevelDisplayNamesAsync` **پس از** ساخت
   فهرست واجدین شرایط قدیمی اجرا می‌شود و تنها `Label` را با نام یک‌سطحی جایگزین می‌کند:
   `MethodCode`, `ServiceCode`, `OptionCode`, `IconKey`, `PriceAmount`, `LeadDays`, `IsFree`,
   `EstimationLabel` دست‌نخورده می‌مانند. هیچ روشی اضافه/حذف نمی‌شود، پس واجدیت از همان منبع
   قدیمی (سرویس/گزینهٔ `IsActive` + `EnabledCodes` + پیکربندی نرخ) می‌آید.

هیچ‌کدام از این مسیرها قیمت، فعال‌سازی، نرخ، اسنپ‌شات سفارش، پرداخت/تسویه، مهاجرت، schema،
پیکربندی، Host یا فرانت‌اند را تغییر نمی‌دهد. `Tooba:ShippingRateAuthority` و
`Tooba:ShippingOperationalShadow` خوانده/فعال نشدند.

## نمونهٔ واقعی برچسب (خواندن فقط-خواندنی از `tooba_alpha`، بدون DML)

| method_code | قبل (برچسب قدیمی مشتری) | بعد (کاتالوگ یک‌سطحی، fa) | کد/قیمت |
| --- | --- | --- | --- |
| `post:standard` | `پست — معمولی` | `معمولی` | `post:standard` / `120000` ثابت |
| `post:express` | `پست — پیشتاز` | `پیشتاز` | `post:express` / `200000` ثابت |
| `snapp_courier` (بدون گزینه) | `اسنپ / پیک آنلاین` | `اسنپ / پیک آنلاین` (بدون ردیف یک‌سطحی) | `snapp_courier` / `90000` ثابت |

منبع اعداد: `docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-100/label-before-after.txt`
(SELECT خالص روی `fulfillment.shipping_methods`, `shipping_method_translations`,
`shipping_services`, `shipping_service_translations`, `shipping_service_options`,
`shipping_service_option_translations`).

## اثبات متمرکز

| Evidence | نتیجه |
| --- | --- |
| `StorefrontShippingOneLevelLabelTests` (4 تست، سطح `ProjectAsync`) | دروازهٔ خاموش = برچسب/کد/قیمت/روز قدیمی؛ دروازهٔ روشن = فقط همان برچسب؛ زبان درخواستی به درز می‌رسد و نبود ترجمه ⇒ برچسب قدیمی؛ کد ناشناخته/سرویس بدون گزینه ⇒ برچسب قدیمی دقیق |
| `ShippingOneLevelStorefrontLabelReadTests` (4 تست، سطح درز) | خاموش = بدون خواندن کاتالوگ؛ روشن = فقط ترجمهٔ زبان دقیق؛ کد ناشناخته/کد سفید/زبان `Guid.Empty` = بدون برچسب؛ `UndefinedTable` = نگاشت خالی |
| `Tooba.Fulfillment.Tests` (کامل) | 158/158 pass |
| `Tooba.Order.Tests` (کامل) | 316/316 pass |
| `Tooba.Host.Tests` (Architecture/Storefront/Shipping) | 1508 pass / 45 fail — همان مجموعهٔ از قبل موجود (شمارش Host/Admin `17` نه `15`، فایل‌های حذف‌شدهٔ `Tooba.Catalog.Domain/*`، `PaidProjectionFinancialTests.Settlement_and_initiate_exclude_store_shipping_from_seller_ids`)؛ هیچ تست مرتبط با OneLevel/Label شکست نخورد |
| `dotnet build Tooba.Host` | 0 error |

## ریسک و بازگشت

- ریسک باقی‌مانده: تا وقتی دروازهٔ `Tooba:ShippingOneLevelCatalog` روشن نشود، مسیر جدید هرگز
  برچسبی جایگزین نمی‌کند (فقط یک فراخوانی درز با نگاشت خالی). با روشن‌بودن دروازه و نبود/ناسازی
  ترجمه هم رفتار دقیقاً قبلی است.
- Rollback: `git revert` کامیت این تسک؛ چهار فایل production (Contracts جدید، Infrastructure جدید،
  یک خط DI، یک متد Order) و فایل‌های تست. هیچ مهاجرت/schema/داده‌ای بازگشتی ندارد.
- اثبات‌نشده: برچسب `en` روی دادهٔ واقعی توسعه دیده نشد (فقط در تست با زبان `en`)، و spread
  واقعی فروشگاه/Checkout در این تسک اجرا نشد (طبق منع صریح تسک: بدون restart روی `:5088`/`:3000`).
