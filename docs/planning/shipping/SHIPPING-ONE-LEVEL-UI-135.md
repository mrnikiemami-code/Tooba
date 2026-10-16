# TB-SHIPPING-ONE-LEVEL-TABS-135 — انتخاب روش ارسال «هر قلم سبد» (هم‌فروشنده، چند روش)

Parent: `TB-SHIPPING-ONE-LEVEL-TABS-134` · Mode: `BOUNDED_SAME_SELLER_PER_ITEM_SHIPPING_METHOD_UI`
Base: `fb512dc732b7c6e5e94b5f01d1d29918f4ce2335` · Status: `STOP_FOR_ARCHITECT`

## شکاف محصولی که بسته شد

TB-131 رابط کاربری را **فقط بر اساس `SellerPartyId`** گروه‌بندی می‌کرد و برای سبد تک‌فروشنده همان
مسیر scalar قدیمی را می‌رفت؛ بنابراین «یک فروشنده با دو خط سبد و دو روش متفاوت» از نظر مشتری قابل
انتخاب نبود، در حالی که Order M1/M2، Payment، Fulfillment و Settlement از قبل گروه‌های
(فروشنده، روش) را می‌شناسند. الان انتخاب، «هر قلم سبد» است و قرارداد
`docs/architecture/checkout/SHIPPING-METHOD-GROUPING-UI-CONTRACT.md` (بند «one seller + two methods
⇒ two persisted and executable groups») در لایهٔ UI قابل استفاده شده است.

## تغییرات (حداقلی و افزایشی)

### ۱) مدل خالص گروه‌بندی هر خط — `src/frontend/app/storefront/storefront-shipping-item-groups.ts`

- `buildShippingItemGroups({ lines, methodCodeByLine, methods })`
  - کلید گروه = `(sellerPartyId خط، کد روش canonical)`؛ فروشنده همیشه از **خود خط سبد** خوانده می‌شود.
  - خطوط هم‌فروشنده/هم‌روش در **یک** گروه ادغام می‌شوند (ناوردایی Order M1: یک گروه برای هر
    (فروشنده، روش)؛ گروه تکراری همان جفت ⇒ `DuplicateSellerMethod`).
  - فروشندگان هرگز ادغام نمی‌شوند، حتی با کد روش یکسان.
  - روش ناشناخته (خارج فهرست authoritative سرور) هیچ گروهی نمی‌سازد و خط در `unassignedLineIds` می‌ماند.
  - هیچ مبلغ/برچسب/فروشنده/شناسهٔ خطی ساخته نمی‌شود.
- `isItemGroupingComplete(grouping, lines)`: هر خط دقیقاً یک بار عضو یک گروه معتبر باشد (سبد خالی ⇒ `false`).
- `restoreLineMethodSelections({ lines, persistedGroups, methods })`: بازیابی قطعی و fail-closed:
  - روش ناواجد در پیش‌نویس ⇒ آن گروه بازیابی **نمی‌شود** و روش جانشین حدس زده نمی‌شود.
  - خط ناپیدا یا متعلق به فروشندهٔ دیگر نادیده گرفته می‌شود؛ هیچ خطی دو بار تخصیص نمی‌یابد.
  - گروه بدون شناسهٔ خط تنها وقتی بازیابی می‌شود که فروشندهٔ آن دقیقاً **یک** خط زنده داشته باشد
    (قطعی، بدون حدس)؛ وگرنه آن فروشنده بدون انتخاب می‌ماند.

### ۲) سطح ارسال — `src/frontend/app/storefront/storefront-shipping.tsx`

- `isGroupedCart = cartLines.length > 1`: حالت «انتخاب هر خط» برای چند فروشنده **یا** یک فروشندهٔ
  چندخطی. سبد تک‌خطی همان مسیر و همان کارت قدیمی را دست‌نخورده نگه می‌دارد.
- وضعیت انتخاب از `groupMethodBySeller` به `lineMethodByLine` (کلید = `line.lineId`) تغییر کرد؛
  سرور همچنان مبناست و اعتبار روش از `projection.methods` می‌آید.
- داخل کارت هر فروشنده، هر قلم ردیف خودش را دارد و زیر همان ردیف، تایل‌های روش همان قلم با
  **کلاس‌های دقیق قبلی** (`rounded-2xl border-2 …`) رندر می‌شود؛ کارت فروشنده، ردیف قلم، تایل روش،
  فاصله‌ها و RTL تغییر نکرده‌اند. برای فروشنده با یک خط، ساختار بصری و `data-testid` قبلی
  (`shipping-group-method-${sellerPartyId}-${methodCode}`) حفظ شده؛ برای فروشندهٔ چندخطی
  `data-testid` با شناسهٔ خط یکتا می‌شود (چون دو تایل هم‌نام در یک صفحه بی‌معنی است).
- ارسال به `/v1/storefront/shipping/selection` از همان `Groups[]` موجود استفاده می‌کند و هر گروه
  فقط `shippingMethodCode`، `serviceCode` authoritative و `cartLineIds` واقعی می‌فرستد
  (`optionCode: null`). هیچ مبلغ/سرویس/شناسهٔ خطی ساخته نمی‌شود.
- افق تاریخ/بازه بدون تغییر: `distinctSelectedMethodCodes(groupSelections, …)` روی گروه‌های
  (فروشنده، روش) کار می‌کند، اشتراک پاسخ سرور معتبر می‌ماند و «ادامه» تا تکمیل همهٔ خطوط، آدرس و
  بازه بسته است. `continue` فقط پیش‌نویس را ذخیره می‌کند و هیچ commit/سفارشی نمی‌سازد.
- ردیف‌های کارت هزینهٔ ارسال به‌ازای هر گروه (فروشنده + روش) نمایش داده می‌شوند تا هزینهٔ دو روش
  یک فروشنده قابل‌مشاهده باشد.

### ۳) قرارداد خواندن افزایشی Order (بدون مهاجرت، بدون تغییر نوشتن)

برای بازیابی قطعی انتخاب «هر قلم» به شناسهٔ خطوط واقعی هر گروه ذخیره‌شده نیاز بود؛ سرور آن را داشت
اما در view فروشگاه منتشر نمی‌کرد.

- `StorefrontShippingGroupView` یک فیلد اختیاری `CartLineIds` گرفت (`IReadOnlyList<Guid>? = null`).
- `StorefrontShippingService.MapGroup` شناسهٔ خطوط همان گروه (`group.Lines`) را منتشر می‌کند.
- `StorefrontCheckoutService.MapGroupPlan` همان فیلد را از `OrderShippingGroupPlan.CartLineIds`
  منتشر می‌کند (readback پرداخت هم‌شکل با ارسال).
- کلاینت `cartLineIds` را با `asStringArray` می‌خواند؛ نبود/نامعتبر ⇒ `null` و fallback قطعی
  تک‌خطی (بدون حدس). هیچ جدول/ستون/مهاجرت/آلگوریتم قیمتی اضافه نشد.

## اثبات

| مورد | نتیجه |
| --- | --- |
| `npm run typecheck` (frontend) | PASS |
| `node --test storefront-shipping-item-groups.test.ts` | PASS — 9 آزمون (یک فروشنده/دو خط/دو روش ⇒ دو گروه؛ دو خط/یک روش ⇒ یک گروه؛ دو فروشنده/یک روش ⇒ جدایی فروشنده؛ خط ناواجد/بی‌انتخاب ⇒ بسته؛ بازیابی قطعی؛ fail-closed روش ناواجد/خط ناپیدا/فروشندهٔ بیگانه؛ fallback تک‌خطی؛ عدم تخصیص دوباره) |
| `storefront-shipping-group-horizon.test.ts` (TB-133) | PASS — ۷ آزمون؛ فقط یک assert سیم‌کشی به مدل جدید به‌روزرسانی شد |
| `storefront-shipping-api.test.ts` | PASS |
| `dotnet build Tooba.Order.Tests` | PASS |
| `dotnet test --filter FullyQualifiedName~Shipping` (Order) | PASS — 95/95 |
| `npm run test:architecture` | FAIL — **از قبل روی `main` قرمز است** (پایین) |

### گارد اندازهٔ فایل (پیش‌موجود، خارج از دامنهٔ این تسک)

`frontend-source-size.guard.test.ts` رشد فایل‌های بزرگ را «shrink-only» می‌سنجد و روی `HEAD` پایه
(`fb512dc…`) هم قرمز است؛ `storefront-shipping.tsx` در `HEAD` **۱۲۰۱** خط در برابر baseline ۹۴۵ است و
سه فایل نامرتبط دیگر (`category-admin-screen.tsx`, `admin-landing-page-composer.tsx`,
`storefront-landing-blocks.tsx`, `shared-composition-renderer.tsx`) هم از baseline عبور کرده‌اند.
این تسک baseline را دست‌کاری نکرد (ممنوع) و `shipping.tsx` را هم بازنویسی نکرد؛ منطق جدید در ماژول
خالص جداگانه قرار گرفت.

## اثبات Runtime — انجام نشد (fixture موجود نیست)

مورد کلیدی این تسک («یک فروشنده، دو خط، دو روش») نیازمند **دو قلم واقعی از یک فروشندهٔ واحد** در سبد
است. در fixture مجاز Development فقط یک محصول زندهٔ قابل‌خرید وجود دارد
(`workspace-live-shirt`) و `WorkspaceDemoMarketplaceSeed` برای آن **دقیقاً یک offer به‌ازای هر فروشنده**
می‌سازد (SellerA = `ARM-LN-01`, SellerB = `DGS-LN-01`). هیچ محصول زندهٔ دیگری با offer فعال از همان
فروشنده در seed نیست و افزودن یک offer دوم، ساخت داده/SQL insert یا جعل هویت فروشنده است که تسک
صراحتاً ممنوع کرده. بنابراین:

- **محصول/offer دوم از یک فروشنده: fixture وجود ندارد.**
- اثبات مرورگر برای این مورد **انجام‌نشده (unproven)** گزارش می‌شود؛ هیچ داده‌ای ساخته/درج نشد.
- Chrome/stack راه‌اندازی نشد تا بودجهٔ زمانی صرف retry راه‌اندازی نشود (طبق خود تسک).

## سازگاری عقب‌رو

- سبد تک‌خط/تک‌فروشنده: مسیر scalar قبلی، همان کارت «روش ارسال» و همان `shippingMethodCode`.
- فروشندهٔ تک‌خطی در سبد چندفروشنده: همان کارت/تایل/`data-testid` قبلی، فقط انتخاب در وضعیت هر خط
  نگه داشته می‌شود.
- سرور قدیمی بدون `cartLineIds`: راه‌اندازی از prefill موجود UI استفاده می‌کند و بازیابی فقط برای
  فروشندهٔ تک‌خطی قطعی است.
- هیچ تغییر در CSS/توکن/tokens/breakpoints، هیچ تغییر در صفحهٔ Admin یا صفحات نامرتبط، و پاسخ‌های
  خطای موجود (پیام بازاعتبارسنجی سرور) دست‌نخورده ماند.

## باقی‌مانده / Blocker برای معمار

1. اثبات runtime مورد «هم‌فروشنده، دو روش» نیازمند یک fixture مجاز با **دو offer فعال از یک فروشنده**
   (یا مجوز ساخت آن) است؛ تا آن زمان فقط با آزمون‌های متمرکز پوشش داده شده است.
2. گارد `frontend-source-size` روی `main` قرمز است (پیش از این تسک)؛ تصمیم دربارهٔ baseline/شکستن
   `shipping.tsx` به معمار واگذار می‌شود.
