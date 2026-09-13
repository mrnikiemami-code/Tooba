# TB-P10-T004-R23 — Admin Settings

API: `GET/PUT /v1/admin/settings/checkout-abuse/`

UI: Settings tab «سفارش باز» / section «کنترل سفارش‌های پرداخت‌نشده و سوءاستفاده از رزرو».

Labels (no raw setting names in `settings/page.tsx`):

- حداکثر سفارش‌های بازِ پرداخت‌نشده
- بازه کنترل رزروهای پیاپی
- حداکثر شروع رزرو در این بازه

Required copy present: لغو سفارش ظرفیت سفارش باز را آزاد می‌کند، اما سهمیه رزرو استفاده‌شده در بازه زمانی را بازنمی‌گرداند.

Runtime M: GET default 2/30/3; PUT 200; PUT maxOpen=0 → 400 `settings.max_open_unpaid.invalid`. Changes apply to future attempts only; existing Orders/cycles/audit rows are not mutated.
