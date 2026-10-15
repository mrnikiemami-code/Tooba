# TB-SHIPPING-FULL-OPERATIONS-077 — browser round-trip evidence

Surface: `http://127.0.0.1:3000/admin/shipping-services` (Next.js Development, rewrites `/v1/*` to
`http://127.0.0.1:5088`). Real Chromium tab, real admin session, real `tooba_alpha` data. Screenshots:
`screenshot-packaging-dialog-top.png`, `screenshot-packaging-dialog-scrolled.png`.

## 1. Row action is present on every service row

The `tipax` row exposes «هزینه بسته‌بندی و پرداخت کرایه» next to the existing `ویرایش`, `نرخ و پوشش`
and `برنامه کاری ارسال` actions (`data-testid="shipping-service-packaging-{shippingServiceId}"`).
Clicking it opens the dialog titled «هزینه بسته‌بندی و نحوه پرداخت کرایه — تیپاکس» with
`کد خدمت: tipax`.

## 2. The three boundary statements render

Observed dialog text (verbatim, from the DOM):

- Scope: «دامنهٔ مقصد فقط از فهرست مرجع Geography انتخاب می‌شود؛ شناسهٔ دستی یا ساختگی پذیرفته
  نمی‌شود.»
- Payer mode: «نحوهٔ پرداخت کرایه یک «واقعیت نرخ» است: Shipping می‌گوید کرایه پیش‌کرایه است یا
  هنگام تحویل وصول می‌شود. وصول و تسویهٔ واقعی «پس‌کرایه» هنوز به Checkout/پرداخت متصل نشده است.»
- Free shipping: «ارسال رایگان، هزینهٔ بسته‌بندی را رایگان نمی‌کند؛ معافیت ارسال رایگان فقط به
  کرایهٔ حامل مربوط است.»

## 3. Validation fires before any API call

With destination `ایران` + `فارس`, option `پیشتاز (express)`, payer `پس‌کرایه` and fee `12.345`, the
form refused to submit and rendered, in `data-testid="shipping-packaging-create-issues"`:

```
هزینهٔ بسته‌بندی باید عددی نامنفی با حداکثر دو رقم اعشار باشد (مثال: 12000 یا 12000.50).
```

No network request was issued for that attempt: three-decimal money is refused rather than silently
rounded.

## 4. Create — 12,000 IRR, CollectOnDelivery

Changing the fee to `12000` and submitting produced the success notice

```
سیاست بسته‌بندی «پیشتاز · استان · e3451206… / استان 24e00388… · ۱۲٬۰۰۰ IRR · پس‌کرایه» ایجاد شد.
```

and the new row rendered as

```
استان · e3451206… / استان 24e00388… | ۱۲٬۰۰۰ IRR | گزینه: پیشتاز | کرایه: پس‌کرایه | فعال | ویرایش | غیرفعال‌سازی
```

Summary line: `سیاست‌های بسته‌بندی فعال: ۱ از ۳`.

Independent wire confirmation (same session):

```
packagingPolicyId 01a13ec9-4e2a-7000-a5cb-428d28a26e8a
feeAmount 12000.00  currencyCode IRR  freightPayerMode CollectOnDelivery
isActive true  optionCode express  administrativeAreaId 24e00388-c216-531c-a3ca-11f382508760
```

## 5. Read → reopen → edit

`ویرایش` opened the inline editor and it was pre-filled from the **persisted** row, not from local
state: `{ fee: "12000", currency: "IRR", payer: "CollectOnDelivery" }`. Changing the fee to `15000`
and the payer to `پیش‌کرایه` and pressing `ذخیره` persisted:

```
feeAmount 15000.00  freightPayerMode Prepaid  isActive true  optionCode express (unchanged)
administrativeAreaId 24e00388-… (unchanged)  updatedAt 2026-10-15T08:59:34Z
```

The option identity and destination scope were preserved exactly — editing money did not move the
policy.

## 6. Duplicate scope is reported honestly

Re-submitting the same `tipax` / `express` / Fars / `12000` / `CollectOnDelivery` combination
produced, as the dialog notice:

```
برای همین خدمت/گزینه/دامنه از قبل سیاست بسته‌بندی ثبت شده است؛ همان را ویرایش کنید.
```

i.e. the backend's `shipping.packaging.policy.scope_duplicate` mapped to a field-adjacent Persian
message. No second row was created and no fake success was shown.

## 7. Toggle off / on

`غیرفعال‌سازی` produced `isActive=false` on the server (verified by re-read) and the notice
«سیاست بسته‌بندی «استان · e3451206… / استان 24e00388…» غیرفعال شد.»; `فعال‌سازی` returned it to
`isActive=true`. The row's fee and payer mode were untouched by both toggles.

## 8. Cleanup

The task-owned row was returned to **INACTIVE** (`isActive=false`) through the same UI. Final list for
`tipax` shows all three rows inactive; `post` shows an empty policy set, rendered as
«بدون سیاست بسته‌بندی = هزینهٔ بسته‌بندی صفر و طرف پرداخت کرایه پیش‌کرایهٔ پیش‌فرض است؛ هیچ هزینهٔ
پنهانی اضافه نمی‌شود.» See `fixture-ledger.md`.
