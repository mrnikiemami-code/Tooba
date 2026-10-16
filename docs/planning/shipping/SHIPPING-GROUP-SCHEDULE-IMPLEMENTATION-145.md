# IMPLEMENTATION PLAN + CODE GAP MAP — زمان‌بندی مستقل هر گروه ارسال (TB-145)

Task: `TB-SHIPPING-INDEPENDENT-GROUP-SCHEDULE-145`
Mode: `BOUNDED_CONTRACT_GAP_AND_APPROVAL_WIREFRAME` — **DESIGN ONLY**
Authority: `docs/architecture/shipping/SHIPPING-GROUP-INDEPENDENT-DELIVERY-COMMITMENT.md` (§۳، §۵، §۶)،
`docs/architecture/checkout/SHIPPING-METHOD-GROUPING-UI-CONTRACT.md`
Wireframe (تأییدنی): `docs/planning/shipping/SHIPPING-GROUP-SCHEDULE-WIREFRAME-145.md`
این سند **هیچ مجوز پیاده‌سازی نمی‌دهد**؛ فقط پس از تأیید کاربر WIREFRAME اجرا می‌شود.

---

## بخش ۱ — CODE GAP MAP دقیق (Mission A)

همهٔ ارجاع‌ها به خطوط فعلیِ مخزن روی base `73160e3f0`. **قاعدهٔ غالب:** مدل پایدارِ Order
*از قبل* فیلد زمان‌بندی «هر گروه» را دارد، اما **مسیرِ خواندن/نوشتن و UI، یک تاریخ/بازهٔ سراسری
تحمیل می‌کند**. پس این تغییر «اتصال‌دهنده» است، نه بازنویسی schema.

### 1.1 صفحهٔ ارسال فروشگاه (frontend)
فایل: `src/frontend/app/storefront/storefront-shipping.tsx`

| مورد | خطوط | وضعیت کنونی | تغییر لازم |
|---|---|---|---|
| حالت سراسری تاریخ/بازه | L135-136 | `deliveryDate` / `deliveryTime` **یک** برای کل صفحه | تبدیل به نگاشت `Record<ShippingGroupId, {date, window}>` |
| افق مشترک | L396-406 | `useGroupShippingDateHorizon` → `resolveShippingDateHorizon` یک افق | افق **per-group** (`eligibleDatesByGroup`) |
| بازنشانی انتخاب | L409-412 | `pickEligibleDeliveryDate/Window` روی افق مشترک | همان تابع، به‌ازای هر گروه |
| اعتبار Continue | L420-434 | `isShippingDateHorizonComplete(dateHorizon, …)` یک مقدار | `everyGroup(complete)` |
| ارسال به سرور | L465-499 | `selectedDeliveryDate`/`selectedDeliveryTimeWindow` سراسری + `shippingGroups[]` بدون تاریخ | الگوی write per-group (بخش 1.4) |
| کارت اقلام | L320-380 | `sellerCards` = **یک کارت به‌ازای فروشنده** + انتخاب روش هر خط | کارت = **گروه** (`ShippingGroupId`) |
| کارت «زمان تحویل» | L912-1002 | **یک** تاریخ‌چیپ (`shipping-date-*`) + **یک** بازه (`shipping-time`) سراسری | محتوای آن داخل هر کارت گروه منتقل شود |
| استفاده از گروه‌های ذخیره‌شده | L217-231 | `restoreLineMethodSelections` فقط روش را برمی‌گرداند | تاریخ/بازهٔ هر گروه هم بازیابی شود |

### 1.2 منطق افق و hook (عمقِ غیرانطباق)
- `src/frontend/app/storefront/storefront-shipping-group-horizon.ts`
  - `deriveGroupDateHorizon` **L38-78**: `minimumDeliveryDate = latest(minimums)` و
    `dates = intersection(...)` و `timeWindows = intersection(...)` → **دقیقاً رفتار قدیمیِ ردشده (§۶ قرارداد)**.
  - `resolveShippingDateHorizon` **L84-95**: سبد چندگروهی → همان افق واحد.
  - `isShippingDateHorizonComplete` **L120-130**: یک (تاریخ، بازه) را می‌سنجد.
  - `distinctSelectedMethodCodes` **L137-144**: کلید واکشی = **کد روش** (نه گروه/فروشنده) → دو فروشندهٔ
    هم‌روش، پاسخ مشترک می‌گیرند.
- `src/frontend/app/storefront/use-storefront-group-shipping-horizon.ts` **L53-79**:
  «به‌ازای هر کد روشِ متمایز یک projection» و «یک افق مشترک». باید به «به‌ازای هر گروه یک projection،
  با کلید `ShippingGroupId + method/option + destination + promise`» تغییر کند.

### 1.3 پروژکشن خواندنی (backend read)
فایل: `src/backend/Modules/Order/Tooba.Order.Application/Storefront/Services/StorefrontShippingService.cs`
- `ProjectAsync` **L66-147**: `maxPrep = MaxSellerPreparationDays(همهٔ فروشندگان)` (L76)؛
  `selected` = یک روش بر اساس `methodCode` (L98-102)؛ `minDate` از
  `today + maxPrep + selected.LeadDays` (L106)؛ `dates = BuildDeliveryDates(...)` (L117-125)؛
  `timeWindows = DayTimeWindows` (L126). خروجی: **یک** `minimumDeliveryDate`، **یک** `dates`،
  **یک** `timeWindows`. → باید `Groups[]` با تاریخ/بازهٔ مجاز per-group برگرداند.
- `LoadEligibleMethodsAsync` **L479-527**: واجد‌بودن/نرخ/`LeadDays` **فقط روش‌محور** است؛
  فروشنده در آن هیچ نقشی ندارد. برای وعدهٔ متفاوت دو فروشنده با روش یکسان، **منبع وعدهٔ per-seller
  (آماده‌سازی فروشنده × بافر سرویس) لازم است** — امروز فقط `MaxSellerPreparationDays` سراسری وجود دارد.
- `ProjectPaymentDraftAsync` **L154-191**: فقط اسکالرهای پیش‌نویس سراسری را می‌خواند.

مدل‌ها: `src/backend/Modules/Order/Tooba.Order.Application/Storefront/Models/StorefrontOrderModels.cs`
`StorefrontShippingProjection` (L154 یک `DeliveryTimeWindows`) و
`StorefrontShippingGroupSelection` (write DTO، **بدون** فیلد تاریخ/بازه) → نیاز به گسترش افزایشی.

### 1.4 مسیر نوشتن — بند اصلیِ غیرانطباق (global write)
فایل: `StorefrontShippingService.SaveSelectionAsync` **L193-379**
- **L269**: `latestMinimum = eligible.Max(x => x.MinimumDeliveryDate)` («دیرترین حداقل»).
- **L274**: `EnsureDeliveryNotEarlier(selectedDate, latestMinimum)` — **یک** تاریخ در برابر **دیرترین**
  گروه سنجیده می‌شود.
- **L276-280**: `windows = ValidTimeWindows(selectedDate, latestMinimum)`؛ **یک** بازه باید در فهرست
  سراسری باشد.
- **L295-307**: `CartShippingGroupPlanner.Plan(..., selectedDate, request.SelectedDeliveryTimeWindow, …)`
  → همان مقدار به **همهٔ** گروه‌ها.
- **L362-364 / L408 / L451-452**: بازتاب اسکالر قدیمی = همان تاریخ سراسری.

فایل: `src/backend/Modules/Order/Tooba.Order.Domain/Checkout/CartShippingGroupPlanner.cs`
- `Plan(...)` **L40-146** پارامتر `selectedDeliveryDate`/`selectedDeliveryTimeWindow` را **اسکالر** می‌گیرد و
  در **L103-104** به `CartShippingDraftGroup.Create(...)` می‌دهد → **همهٔ گروه‌ها یک تاریخ/بازه**.
- **L85-91**: `seenSellerMethod = "{seller:N}|{MethodCode}"` و در تکراری ⇒
  `DuplicateSellerMethod` → **ناوردایی سختِ «یک گروه به‌ازای (فروشنده، روش)»** که قرارداد (§۶) می‌گوید
  باید بازنگری شود.
- `ResolveMethod` **L153-171**: فقط method/service/option — **بدون** کلید سازگاری فروشنده/مبدأ/وعده.
- `AddWholeLine` **L120-129**: فروشندهٔ مختلط رد می‌شود (درست).

فایل: `src/backend/Modules/Order/Tooba.Order.Domain/Checkout/CartShippingDraft.cs`
- `AddShippingGroups` **L212-233**: پروجکشن قدیمی؛ `SelectedDeliveryDate = first.SelectedDeliveryDate`
  و `SelectedDeliveryTimeWindow = first.SelectedDeliveryTimeWindow` (**L230-231**) → اسکالر قدیمی =
  تاریخ **اولین** گروه (به‌ترتیب `SellerPartyId`) — پروجکشن ناسازگار/گمراه‌کننده.

### 1.5 پایدارسازی — از قبل per-group ✅
- `src/backend/Modules/Order/Tooba.Order.Domain/Checkout/CartShippingDraftGroup.cs`
  **L160-166**: `MinimumDeliveryDate`, **`SelectedDeliveryDate`**, **`SelectedDeliveryTimeWindow`**, `LeadDays`.
  **L287-293**: `ApplySchedule(date, window)` **وجود دارد و هیچ‌جا فراخوانی نمی‌شود** → ظرفیت آماده، بی‌استفاده.
  **L229-233**: `originContext` غیرتهی ⇒ `OriginUnverified`.
- `src/backend/Modules/Order/Tooba.Order.Domain/Checkout/OrderShippingGroup.cs`
  **L55-61**: `MinimumDeliveryDate`, **`RequestedDeliveryDate`**, **`RequestedDeliveryTimeWindow`** per group.
  **L129-135**: `OriginContext` همیشه `null` (اجباراً).
- Migration `.../20261016100000_AddOrderShippingGroups.cs`:
  `order.order_shipping_groups` از قبل `requested_delivery_date date NULL` (L35) و
  `requested_delivery_time_window varchar(32) NULL` (L36) را دارد ⇒ **برای گروه‌محور شدن نیازی به migration نیست.**
  ⚠️ **محدودیت:** `ux_order_shipping_groups_checkout_seller_method (checkout_id, seller_party_id, method_code)`
  (L56-57) و `ux_order_shipping_group_lines_order_line (order_line_id)` (L73-74) → برای مورد «یک فروشنده+یک
  روش+وعده/مبدأ ناسازگار» (case D) **بازنگری ایندکس لازم است** (فقط پس از تعریف کلید سازگاری).
- `BuildOrderGroupPlans` (`StorefrontShippingService` **L460-477**) تاریخ/بازه را **per-group** منتقل می‌کند؛
  فقط **مقدارشان یکسان است** چون planner اسکالر را کپی کرده.

### 1.6 Handoff به Fulfillment — از قبل per-group ✅
`src/backend/Modules/Order/Tooba.Order.Infrastructure/Integrations/Fulfillment/OrderFulfillmentBridge.cs`
`LoadGroupsAsync` **L68-103**: به‌ترتیب `ShippingGroupId` می‌خواند و
`OrderFulfillmentShippingGroupSnapshot(..., MinimumDeliveryDate, RequestedDeliveryDate, RequestedDeliveryTimeWindow)`
می‌سازد → Fulfillment از قبل date/window هر گروه را می‌گیرد. **بدون تغییر لازم برای این تسک.**

### 1.7 Admin + زمان‌بندی/Timezone (منبعِ غایب)
- `StorefrontShippingCalculator.cs`
  **L11-18**: `DayTimeWindows` یک لیست **استاتیک سراسری** (۴ بازه)؛
  **L148-157**: `ValidTimeWindows` همان لیست را برای هر روز واجد برمی‌گرداند ⇒ **بدون** سهمیه/قطعِ زمانی/slot.
  **L94-97**: `BuildDeliveryDates` روزهای تقویمی پیوسته (۱..۳۰) بدون روز کاری/تعطیلات/Timezone.
  **L20-35**: `MaxSellerPreparationDays` آماده‌سازی فروشنده را **یکجا max** می‌کند (به گروه تبدیل نمی‌شود).
- `ShippingMethodRateOptions` (Rates): `LeadDays`, `AllowedProvinces`, `BasePrice`, `FreeAboveSubtotal` —
  **بدون** origin/Timezone/تقویمِ کاری.
- قرارداد Admin (`SHIPPING-ADMIN-FLAT-METHOD-CONTRACT.md`) تب «Transport Schedule» را per-method می‌خواهد اما
  **Phase 2 و هنوز پیاده نشده** ⇒ **صاحب authoritative زمان‌بندی/Timezone/slot وجود ندارد** ⇒ BLOCKED برای
  وعدهٔ دقیق per-group از سمت سرویس.
- **نتیجه:** وعدهٔ «۲ روز / ۳ روز» امروز فقط می‌تواند از `sellerPreparationDays + methodLeadDays` **ساخته**
  شود؛ برای اثبات واقعیِ دو وعدهٔ متفاوت با روش یکسان، به **fixture صراحتاً تأییدشده** نیاز است (بخش ۳).

### 1.8 خلاصهٔ ریسک و سازگاری
| تغییر | افزوده/جایگزین | سازگاری | ریسک |
|---|---|---|---|
| DTO پروژکشن `Groups[]` | افزوده | فیلدهای اسکالر فعلی برای تک‌گروهی می‌مانند | کم |
| write per-group | افزوده (کنار shorthand) | تک‌گروهی همان shorthand | متوسط |
| `CartShippingGroupPlanner` پذیرش تاریخ per-group | جایگزین امضا | نیاز به به‌روزرسانی فراخوان‌ها | متوسط |
| حذف ناوردایی (فروشنده،‌روش) | جایگزین | نیاز به کلید سازگاری + بازنگری ایندکس یگانه | **بالا** (فقط با تأیید) |
| UI per-group | جایگزین کارت سراسری | تک‌گروهی دست‌نخورده | متوسط (بصری — Visual ACCEPT جدا) |

**موردی که تغییر نمی‌کند:** FIulfillment handoff، schema گروه‌های پایدار، Settlement/Payout، PERF-CRIT-001.

---

## بخش ۲ — سه افزایش پیاده‌سازی (Mission C)

هر افزایش ≤۴۵ دقیقه، حداکثر یک، و **PASS/STOP** مستقل. هیچ‌کدام بدون تأیید WIREFRAME شروع نمی‌شود.

### افزایش ۱ — بک‌اند: زمان‌بندی per-group (read + write + اعتبارسنجی)
- دامنه: `Order` (Domain + Application + Infrastructure)، Contracts.
- کارها (minimal additive):
  1. `StorefrontShippingProjection` یک فیلد افزودهٔ `Groups[]` بگیرد که هر عضو
     `ShippingGroupId`, `methodCode/serviceCode/optionCode`, `eligibleDates[]`, `eligibleTimeWindows[]`,
     `earliestDate`, `quoteVersion`, `minimumDeliveryDate` داشته باشد (اسکالرها برای تک‌گروهی حفظ شوند).
  2. `StorefrontShippingGroupSelection` (write DTO) یک فیلد افزودهٔ `selectedDeliveryDate`/`selectedDeliveryTimeWindow`
     بگیرد (اختیاری؛ absent ⇒ shorthand قدیمی).
  3. `CartShippingGroupPlanner.Plan` نسخهٔ جدیدی بپذیرد که تاریخ/بازه را **per-group** می‌گیرد و
     `CartShippingDraftGroup.ApplySchedule(...)` (که امضایش از قبل وجود دارد) را فرا می‌خواند.
  4. `SaveSelectionAsync` اعتبارسنجی را per-group کند: به‌ازای هر گروه، انتخابش را در برابر
     `minimumDeliveryDate` **همان گروه** بسنجد (نه `Max` سراسری)، و `latestMinimum` سراسری را فقط برای
     پروجکشن قدیمی نگه دارد.
  5. `CartShippingDraft.AddShippingGroups` پروجکشن قدیمی را اصلاح کند (تاریخ اسکالر = فقط تک‌گروهی؛ چندگروهی
     ⇒ null).
- **PASS:** تست تمرکزی روی «دو گروه، دو تاریخ متفاوت، هر کدام معتبر با وعدهٔ خودش؛ انتخاب دیرتر برای A
  هیچ اثری روی B ندارد؛ انتخاب نامعتبر برای یک گروه با خطای همان گروه رد می‌شود».
- **STOP:** اگر منبعِ وعدهٔ per-seller/schedule موجود نبود، فقط با fixture تأییدشدهٔ توسعه ادامه بده؛ وگرنه STOP.

### افزایش ۲ — frontend: کنترل تاریخ/بازهٔ درون کارتِ هر گروه
- دامنه: `src/frontend/app/storefront/**`.
- کارها:
  1. `storefront-shipping-group-horizon.ts`: `deriveGroupDateHorizon` **حذف** شود و به تابع per-group
     `resolveHorizonForGroup(groupProjection)` تبدیل شود؛ هیچ `intersection` نماند.
  2. `use-storefront-group-shipping-horizon.ts`: کلید واکشی = `ShippingGroupId` (نه methodCode).
  3. `storefront-shipping.tsx`: `deliveryDate`/`deliveryTime` سراسری → نگاشت per-group؛ کارت `زمان تحویل`
     سراسری (L912-1002) حذف و محتوایش داخل کارت هر گروه؛ `isValid` = همهٔ گروه‌ها.
  4. کارت گروه = `ShippingGroupId`. `sellerCards` فقط برای سازگاری تک‌فروشنده باقی می‌ماند.
  5. حفظ کامل توکن‌ها/RTL/responsive فعلی (بدون طراحی جدید).
- **PASS:** تست کامپوننت/رفتار: دو کارت، تغییر A روی B بی‌اثر، دکمهٔ ادامه فقط با هر دو گروه کامل،
  بازگشت/رفرش تاریخِ هر گروه را برمی‌گرداند. **PASS بصری ≠ Visual ACCEPT.**
- **STOP:** عدم تأیید WIREFRAME ⇒ توقف.

### افزایش ۳ — اثبات scratch یکپارچه (یک بار)
- یک محیط scratch مجاز (فقط با تأیید)، یک سبد با دو فروشنده و روش یکسان، دو وعدهٔ متفاوت از fixture.
- سنجه‌ها: دو `ShippingGroupId` و دو تاریخ/بازهٔ مستقل در API و در DOM؛ persistence در پیش‌نویس؛ انتقال
  به `order.order_shipping_groups` و snapshot Fulfillment؛ عدم دوباره‌شماری هزینه.
- **PASS:** همهٔ موارد ۱..۸ بخش ۶ wireframe. **STOP:** هر کمبود ⇒ گزارش BLOCKED بدون جعل داده.

---

## بخش ۳ — نیازهای Fixture (بدون دست‌کاری دادهٔ واقعی/تولید)

برای اثبات «tipax:standard با دو وعدهٔ ۲ و ۳ روز برای دو فروشندهٔ متفاوت» **هیچ نرخ/تاریخ واقعی جعل نمی‌شود**؛
فقط مسیرهای موجود قابل‌اعتماد استفاده می‌شوند:
1. `ShippingMethodsOptions.SellerPreparationDaysByPartyId` — تنظیم آماده‌سازی **per-seller** (۲ و ۳) در
   یک پیکربندی **Development** صراحتاً تأییدشده (نه تولید/`tooba_alpha`/share).
2. همان `LeadDays` روش (`tipax:standard`) بدون تغییر ⇒ تفاضل وعده فقط از آماده‌سازی فروشنده می‌آید.
3. تاریخِ مرجع از `IClock` (نه ساعت مرورگر) تا محاسبهٔ `today + prep + lead` قابل تکرار باشد.

هیچ fixture در کدِ تولیدی افزوده نمی‌شود؛ فقط داده/پیکربندی محیط توسعهٔ مجاز.

---

## بخش ۴ — خارج از دامنه (صراحتاً)

تغییر Admin UX، فعال‌سازی تولیدی، shared `:5088`/`:3000`، `tooba_alpha`، migration واقعی، نوشتن داده،
تغییر UI/CSS در این تسک، سیاست‌های تجاری Settlement/Payout، و `PERF-CRIT-001` — **همه خارج از دامنه** هستند
(طبق متن Task). این تسک فقط WIREFRAME + change map + plan است.
