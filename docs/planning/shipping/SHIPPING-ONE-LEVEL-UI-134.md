# TB-SHIPPING-ONE-LEVEL-TABS-134 — بازآزمایی و بازیابی پیش‌نویس پایدار ارسال در رفرش سرد

Parent: `TB-SHIPPING-ONE-LEVEL-TABS-133` (پذیرفته‌شده: انتخاب گروهی + هورایزن تاریخ سرور + handoff پرداخت)
Program: TOOBA-SHIPPING-CHECKOUT-DELIVERY-CLOSURE
Mode: BOUNDED_PERSISTED_SHIPPING_DRAFT_RELOAD_RECOVERY
Base SHA: `2f9f39cf6aad8c2fb650fb48901881fe8f579c9a`

## 1. شکاف پذیرفته‌نشده در TB-133

پس از یک انتخاب موفق ارسال (`PUT /v1/storefront/shipping/selection`) و رسیدن به `/fa/payment`،
یک رفرش سرد `/fa/shipping` انتخاب ذخیره‌شده را نشان نمی‌داد: فیلدهای آدرس خالی، کاشی‌های روش هر گروه
انتخاب‌نشده، صفر چیپ تاریخ، «ادامه به پرداخت» غیرفعال و پیام
«انتخاب ارسال دیگر معتبر نیست؛ لطفاً دوباره انتخاب کنید.»
در حالی که خود پیش‌نویس در دیتابیس باقی بود (همان handoff پرداخت TB-133 گواهش بود).

## 2. ریشهٔ واقعی (کد، نه حدس)

`POST /v1/storefront/shipping/projection` بدون `methodCode` مسیر زیر را طی می‌کند:

`StorefrontOrderEndpoints.ProjectShippingAsync` → `ProjectStorefrontShippingQuery` →
`StorefrontShippingService.ProjectAsync` (`src/backend/Modules/Order/Tooba.Order.Application/Storefront/Services/StorefrontShippingService.cs`).

سه لایهٔ ممکن بررسی شد:

| لایه | یافته |
| --- | --- |
| (الف) خواندن پیش‌نویس Order | سالم: `LoadDraftAsync` پیش‌نویس را از `IStorefrontShippingDraftStore` می‌خواند و مالکیت مهمان را سنجیده برمی‌گرداند. |
| (ب) بازآزمایی/اعتبارسنجی | **معیوب**: تنها شرط اعتبار، `methods.Any(m => m.MethodCode == draft.ShippingMethodCode)` بود. |
| (ج) هیدریشن فرانت‌اند | سالم و بی‌تغییر: هیدریشن آدرس/تاریخ/بازه از `page.draft` و اثر بازیابی گروه‌ها از `draft.shippingGroups` از قبل موجود بود؛ فقط ورودی‌اش (draft) هرگز نمی‌رسید. |

علت دقیق در مالک دامنه است — `CartShippingDraft.AddShippingGroups`:

```csharp
// سازگاری گذشته: فیلد تک‌روشی فقط projection است، نه حقیقت.
var multiGroup = groups.Count > 1;
ShippingMethodCode = multiGroup ? string.Empty : first.MethodCode;
```

یعنی پیش‌نویس چندفروشنده‌ای (سبد TB-133: دو فروشنده، دو روش مستقل) عمداً `shipping_method_code` را
**خالی** نگه می‌دارد و انتخاب واقعی را فقط در `cart_shipping_groups` نگه می‌دارد. پس:

* `methods.Any(m => m.MethodCode == "")` همیشه `false` است؛
* در نتیجه `stillValid=false` → `draft=null` + پیام بازآزمایی، در **هر** رفرش سرد سبد چندگروهی؛
* `selectedCode` هم خالی می‌ماند → `minimumDeliveryDate=null` و `deliveryDates=[]`.

این نقص فقط برای سبد چندگروهی رخ می‌داد؛ پیش‌نویس تک‌گروهی (legacy) که shorthand غیرخالی دارد،
قبلاً هم بازیابی می‌شد. هیچ نوشتنی، هیچ تراکنش شکست‌خورده و هیچ نشت دسترسی‌ای در میان نبود.

## 3. تعمیر (حداقلی، مالک‌درست، فقط Order)

### 3.1 فایل جدید — سیاست بازیابی فقط-خواندنی

`src/backend/Modules/Order/Tooba.Order.Application/Storefront/Shipping/Recovery/StorefrontShippingDraftRecovery.cs`

* `SelectedMethodCodes(draft)`: اگر گروه پایدار وجود دارد، همان کدهای روش گروه‌ها (بدون خالی، بدون تکرار)،
  وگرنه shorthand تک‌روشی قدیمی.
* `IsStillValid(draft, methods, provinceName, cartSellerPartyIds, options)`: همهٔ روش‌های انتخاب‌شده باید
  امروز هم در فهرست واجدین شرایط authoritative باشند؛ اگر استان ارسالی با استان ثبت‌شده فرق کند همان درز
  canonical `IsDestinationAllowed` تصمیم می‌گیرد؛ و هر گروه ذخیره‌شده باید هنوز به یکی از فروشنده‌های
  **همین** سبد تعلق داشته باشد (اگر ردیفی حذف شده باشد، انتخاب ذخیره‌شده تازه نیست).
* `ResolveProjectedLeadDays(draft, methods)`: دیرترین `LeadDays` روش‌های گروه‌های ذخیره‌شده بر پایهٔ فهرست
  **امروزِ** واجدین شرایط؛ اگر گروهی نباشد یا روشی دیگر واجد شرایط نباشد `null` (هیچ وعدهٔ ساختگی).
* `RevalidationMessage`: همان متن موجود مشتری، بدون تغییر.

هیچ قیمت‌گذاری، هیچ نوشتن، هیچ ذخیره‌سازی و هیچ ریاضیات تقویمی در این کلاس نیست.

### 3.2 سیم‌کشی در `StorefrontShippingService.ProjectAsync`

1. اعتبارسنجی پیش‌نویس از `methods.Any(...)` به `StorefrontShippingDraftRecovery.IsStillValid(...)`
   تغییر کرد (با فروشنده‌های همان سبد). رفتار تک‌گروهی legacy عیناً حفظ شده؛ فقط سبد چندگروهی، که
   انتخاب معتبرش را دور می‌ریخت، درست اعتبارسنجی می‌شود. مقایسهٔ کد روش نیز case-insensitive شد
   (هم‌راستا با `SaveSelectionAsync` که واجد شرایط بودن را case-insensitive می‌سنجد).
2. اگر روش واحدی انتخاب/بازیابی نشده باشد و پیش‌نویس گروه داشته باشد، `minimumDeliveryDate`
   از دیرترین وعدهٔ گروه‌ها با همان `StorefrontShippingCalculator.ComputeMinimumDeliveryDate`
   ساخته می‌شود؛ پس تاریخ بازیابی‌شده در فرانت‌اند دوباره با وعدهٔ واقعی سرور سنجیده می‌شود.

هیچ تغییر طرح‌واره، migration، روت جدید، endpoint جدید، DI، قرارداد بین‌ماژولی یا متن کاربر جدید نداریم.
خروجی projection همان شکل قبلی است (`draft`, `revalidationMessage`, ...) و فقط در حالت چندگروهی
اکنون پر می‌شود.

### 3.3 فرانت‌اند: هیچ تغییری لازم نبود (و انجام نشد)

* هیدریشن آدرس/تاریخ/بازه/یادداشت و `savedAddressId` از `page.draft` از قبل موجود بود.
* اثر بازیابی گروه‌ها (`draft.shippingGroups` → `groupMethodBySeller`) از قبل موجود بود و
  hook هورایزن TB-133 هیچ‌وقت گروه‌های ذخیره‌شده را بازنویسی نمی‌کند (فقط `horizon` خودش را ست می‌کند).
* تنها نویسندهٔ ارسال `persistAndContinue` است که فقط با کلیک «ادامه به پرداخت» و با `isValid` اجرا
  می‌شود؛ پس رفرش/برگشت هیچ PUT/POST نوشتنی ندارد.

## 4. اثبات (Mission B)

* تست‌های متمرکز جدید + کل تست‌های Storefront/Architecture ماژول Order: جزئیات در
  `docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-134/`.
* سنجش واقعی مرورگر روی هدف دورانداختنی جداشده: جزئیات و مشاهدات در همان پوشهٔ evidence.

## 5. بدهی/بلاکرهای باقی‌مانده (خارج از این تسک)

1. سبد تک‌فروشنده با دو روش مستقل هنوز در UI قابل آزمون نیست (همان بلاکر TB-133).
2. سیاست تخصیص تجاری/تأمین مالی فروشنده‌ها همچنان مسدود و بدون ابداع است.
3. اثبات migration/parity یک‌سطحی همان‌طور که TB-133 گزارش کرد باز است.
4. پرداخت اثبات نشده: فقط رسیدن به صفحهٔ پرداخت؛ هیچ روش پرداخت، احراز/برداشت یا ساخت سفارش.
