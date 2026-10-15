# SHIPPING-ONE-LEVEL-UI-097 — فعال/غیرفعال‌سازی روش گزینه‌دار در زبانهٔ ۱

Parent: `TB-SHIPPING-ONE-LEVEL-TABS-096` · Status: `STOP_FOR_ARCHITECT`

## دامنه (bounded)

تنها قابلیت افزوده‌شده در زبانهٔ ۱ («سرویس‌ها»): نمایش وضعیت فعال‌بودن و اعمال فعال/غیرفعال‌سازی
برای روش‌های **گزینه‌دار**، از طریق همان درز مجاز ۰۹۶:

```text
PUT /v1/admin/shipping-services/shipping-methods/one-level/{methodCode}/activation
body: { isActive }
```

- هیچ endpoint دیگری ساخته نشد.
- هیچ `serviceCode`/`optionCode`/UUID گزینه‌ای از مرورگر فرستاده نمی‌شود.
- هیچ نوشتن مستقلی روی `shipping_methods` وجود ندارد؛ تنها مرجع ثبت رکورد، سرور است.
- روش‌های بدون گزینه هیچ سوئیچ کاربردی ندارند.
- ویرایشگر نام ۰۹۴، پنج زبانه، RTL، انتخاب و معناشناسی کلید دست‌نخورده است.

## منبع «گزینه‌دار بودن»

فهرست ادمین یک‌سطحی عمداً `legacyServiceCode`/`legacyOptionCode` را برنمی‌گرداند. پس تنها منبع مجاز،
حل‌کنندهٔ سرور ۰۸۶ است:

```text
GET /v1/admin/shipping-services/shipping-methods/one-level/{methodCode}/binding
```

| پیوند سرور | نتیجه |
|---|---|
| `resolved` و `optionCode` غیرتهی | `supported` — سوئیچ با وضعیت فعلی سرور |
| `resolved` و `optionCode` تهی | `optionless` — فقط پیام «فعال‌سازی این نوع سرویس هنوز پشتیبانی نمی‌شود» |
| `gate-off` / `not-ready` / `unresolved` / `untrusted` | `unavailable` — بدون سوئیچ (fail-closed) |

روش‌های `snapp_courier`, `store_courier`, `in_person` هیچ‌گاه گزینه‌دار تشخیص داده نمی‌شوند و
هیچ فعال‌سازی خودکار والد رخ نمی‌دهد.

## جریان کار

```text
methodCode (تغییرناپذیر)
  → loadOneLevelMethodBinding            (GET فقط-خواندنی، نگهبان نسل)
  → resolveActivationEligibility        (تابع ناب)
  → ShippingOneLevelActivationControl   (تأیید صریح + حالت مشغول)
  → setOneLevelMethodActivation         (PUT با بدنهٔ صرفاً { isActive })
  → onCompleted → load() + loadBinding() + router.refresh()   (بدون وضعیت خوش‌بینانه)
```

پس از پاسخ تأییدشدهٔ سرور، هیچ وضعیت محلی `isActive` ساخته نمی‌شود؛ کاتالوگ و پیوند دوباره خوانده
می‌شوند و اگر سرور مقدار دیگری برگردانده باشد، همان دیده می‌شود.

## قرارداد سرور (بررسی‌شده، نه اختراعی)

| کد خطا | HTTP |
|---|---|
| `fulfillment.rejected` (دروازهٔ خاموش / کاتالوگ مهاجرت‌نشده) | 400 |
| `shipping_method.code.invalid` | 400 |
| `fulfillment.shipping_method.unsupported` (گزینهٔ غایب / والد غیرفعال / بدون گزینه) | 400 |
| `shipping_service.not_found` (پیوند قدیمی غایب) | 404 |
| `shipping_method.not_found` | 404 |
| 401 / 403 | `denied` |

پاسخ نامعتبر (کد روش متفاوت یا `isActive` غیر boolean) ⇒ خطا، نه موفقیت جعلی.

## تأیید (verification)

- تست‌های متمرکز: `node --experimental-strip-types --test app/admin/shipping-services/shipping-one-level-activation.test.ts`
  (۱۷ تست) و کل پوشهٔ سرویس ارسال (۲۰۴ تست) — همه سبز.
- `npx tsc --noEmit` — بدون خطا.
- شواهد: `docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-097/`

## محدودیت‌ها (صادقانه)

- **نوشتن زنده: تأیید نشده (NOT VERIFIED).** Host مشترک Development پیش از ۰۹۶ اجرا شده است و مسیر
  `.../activation` در آن ۴۰۴ برمی‌گرداند؛ همچنین نوشتن زنده در Development در این تسک ممنوع است.
- دروازهٔ `Tooba:ShippingOneLevelCatalog` در Development خاموش است، پس شل یک‌سطحی mount نمی‌شود و
  زبانهٔ ۱ در محیط زنده قابل مشاهده نیست. این رفتار fail-closed و مطابق انتظار است.
