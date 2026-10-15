# TB-SHIPPING-FULL-OPERATIONS-078 — browser + wire proof of the operational quote inspector

Surface: `http://127.0.0.1:3000/admin/shipping-services` (Next.js Development, `/v1/*` rewritten to
`http://127.0.0.1:5088`). Real Chromium tab, real admin session, real `tooba_alpha` data.
Screenshot: `screenshot-quote-result-quoted.png`. Wire log: `log-live-operational-quote.txt`.

## 1. The action is discoverable on every service row

`data-testid="shipping-service-quote-{shippingServiceId}"` exists on all five rows (post, tipax,
snapp_courier, store_courier, in_person) with the label «بررسی هزینه و شرایط ارسال», next to the
existing `ویرایش`, `نرخ و پوشش`, `برنامه کاری ارسال` and `هزینه بسته‌بندی و پرداخت کرایه` actions.
The dialog title is «بررسی هزینه و شرایط ارسال — تیپاکس» and shows `کد خدمت: tipax`, i.e. the code
comes from the clicked row and cannot be typed by hand.

## 2. The two boundary statements render verbatim

```
این ارزیابی یک «تشخیص عملیاتی» درون ادمین است، نه قیمت نهایی مشتری: Checkout امروز همچنان با
مرجع قیمت قدیمی کار می‌کند و این عدد روی هیچ سفارشی نوشته نمی‌شود.

بازهٔ حامل یعنی «چه زمانی حامل مرسوله را می‌پذیرد»، نه زمان تحویل وعده‌داده‌شده به مشتری؛
وعدهٔ مشتری دارایی Order است.
```

## 3. Exact unit conversion and explicit timezone are shown before submitting

With weight `1.999` the dialog rendered `معادل دقیق: ۱٬۹۹۹ گرم (بدون گرد کردن)`, and with
`2026-10-18T13:30` in `Asia/Tehran` it rendered
`2026-10-18T10:00:00.000Z (UTC+03:30 → UTC)`. Neither value is inferred from the browser timezone.

## 4. Quoted run, inside the active carrier window (Alborz scope, `standard`)

UI output, matching `log-live-operational-quote.txt` phase 4 byte-for-byte on every amount:

```
قیمت اعلام شد — کرایه و بسته‌بندی قطعی اعلام شدند.
کرایهٔ حامل              ۱۸۰٬۰۰۰ IRR
هزینهٔ بسته‌بندی          ۰ IRR
جمع هزینهٔ خدمت          ۱۸۰٬۰۰۰ IRR
قابل پرداخت هنگام تسویه   ۱۸۰٬۰۰۰ IRR
وصول هنگام تحویل         ۰ IRR
طرف پرداخت کرایه          پیش‌کرایه
وضعیت بازهٔ حامل: داخل بازهٔ فعال حامل · منطقه: Asia/Tehran · روز محلی: یکشنبه · ساعت محلی: 13:30:00
                  · پنجرهٔ منطبق: 01a13e1c-2913-7000-b30a-0d3fb02dfcde · مجموع ظرفیت تنظیم‌شده: ۲۵
ظرفیت: فقط تنظیم شده؛ رزرو/اجرای ظرفیت وجود ندارد.
واجد شرایط عملیاتی: بله
```

`۰ IRR` here is a **real** quoted zero (the existing identity has no active packaging policy), not a
missing amount — which is exactly the distinction the model enforces.

## 5. Outside-window and NotEvaluated are honest, not optimistic

Same form with `2026-10-18T14:00` (the half-open end of the 13:00–14:00 window):

```
وضعیت بازهٔ حامل: خارج از همهٔ بازه‌های فعال حامل · روز محلی: یکشنبه · ساعت محلی: 14:00:00
واجد شرایط عملیاتی: خیر
```

With the pickup input cleared (no instant supplied):

```
وضعیت بازهٔ حامل: ارزیابی‌نشده (لحظهٔ تحویل اعلام نشده) · منطقه: Asia/Tehran · مجموع ظرفیت تنظیم‌شده: ۲۵
ارزیابی‌نشده: لحظهٔ تحویل به حامل اعلام نشده است، پس هیچ ادعایی دربارهٔ بازهٔ حامل نمی‌شود و این
حالت «قابل رزرو» نیست. — در این حالت «قابل رزرو» ادعا نمی‌شود.
```

The freight stays `۱۸۰٬۰۰۰ IRR` in that state, yet `واجد شرایط عملیاتی` is `خیر`: priced and
operationally eligible are deliberately not conflated.

## 6. Nullable amounts are never rendered as zero

A real live case where the backend declares no amounts at all is the Alborz-vs-Fars scope with a
service-level option (`log-live-operational-quote.txt` phase 9). The dialog rendered:

```
کرایه اعلام نشد — کرایه اعلام نشد (پوشش/پلهٔ اندازه/ارز/خدمت)؛ هیچ مبلغی نمایش داده نمی‌شود.
کرایهٔ حامل            اعلام نشده
هزینهٔ بسته‌بندی        اعلام نشده
جمع هزینهٔ خدمت        اعلام نشده
قابل پرداخت هنگام تسویه  اعلام نشده
وصول هنگام تحویل       اعلام نشده
```

i.e. `اعلام نشده`, never `۰`. The same UI path also rendered a genuine `Unavailable` carrier window
together with the unquoted money, and `واجد شرایط عملیاتی: خیر` — no «قابل رزرو» claim anywhere.

## 7. Failure states are distinguished

| Case | Live wire result | Dialog behaviour |
| --- | --- | --- |
| invalid destination (`11111111-…`) | `400 shipping.destination_scope.invalid` | red failure line with the mapped Persian message |
| invalid option (`no-such-option`) | `400 shipping.service_option.unknown` | same, message names the option |
| unknown service code | `400 shipping.service_code.unknown` | same |
| `weightGrams = -1` | `400 validation.failed` → `shipping.validation.operational_weight_negative` | child code is surfaced, not the envelope |
| `itemCount = 0` | `400 … operational_item_count_out_of_range` | same |
| `currencyCode = "IR"` | `400 … currency_invalid` | same |
| absent scope / unreadable body | `400 … scope_required` / `… body_invalid` | same |
| no credential at all | `401 admin.actor.missing` | «دسترسی لازم برای ارزیابی عملیاتی را ندارید (۴۰۱/۴۰۳).» |
| `currencyCode = "USD"` against IRR rates | `200 FreightNotQuoted` + `shipping.currency.mismatch` | `اعلام نشده` amounts plus the explicit currency-mismatch warning line |

Submit is disabled while a request is outstanding, so no double submission or fake success is
possible.

## 8. No mutation

The dialog exposes no save/edit/delete control at all, and the log's phase 16 shows the rates,
packaging-policies and operating-schedules bodies byte-identical before and after the whole matrix.
