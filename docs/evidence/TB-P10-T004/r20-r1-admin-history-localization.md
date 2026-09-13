# R20-R1 Admin history localization

Normal Admin UX no longer shows `GATEWAY_REJECTED`.

FA `/fa/admin/orders/01a098eb-7669-7000-9fc2-1654986b455b`:

- label: پرداخت ناموفق
- summary: ردشده توسط درگاه پرداخت
- `GATEWAY_REJECTED` absent from rendered text

EN `/en/admin/orders/01a098eb-7669-7000-9fc2-1654986b455b`:

- label: Payment failed
- summary: Rejected by payment gateway

Host operational-history JSON still contains `GATEWAY_REJECTED` in `summaryFa`/`summaryEn` (audit unchanged).

Locale for presentation: `document.documentElement.lang` or `/en` pathname (Admin html lang can stay `fa` on `/en/admin`).
