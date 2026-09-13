# TB-P10-T004-R24 — Admin UX

`/fa/admin/settings` loads Admin workspace (session-gated). Settings surface in `settings/page.tsx`:

- هویت مشتری در فرایند خرید (AuthenticatedOnly / GuestAllowed)
- حداکثر سفارش‌های بازِ پرداخت‌نشده
- بازه کنترل رزروهای پیاپی
- حداکثر شروع رزرو در این بازه
- ReservationPolicyEditor initial/retry/maxCycles

FA labels only (no raw config keys in customer-facing copy). GET/PUT `/v1/admin/settings/checkout-abuse/` 200; invalid max 0 → 400 `settings.max_open_unpaid.invalid`. Identity GET 200; reservation-policy store GET 200. Unauthenticated PUT 401. Settings affect future attempts only; existing Orders/cycles not mutated by PUT.
