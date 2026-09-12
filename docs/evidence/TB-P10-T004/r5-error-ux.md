# TB-P10-T004-R5 — Error UX

| Code | Customer/Admin message |
| --- | --- |
| `inventory.manual_review.unavailable` | موجودی این سفارش در زمان بررسی پرداخت دیگر در دسترس نیست. لطفاً وضعیت سفارش و بازگشت وجه را بررسی کنید. |
| `inventory.reservation.not_active` | Mapped to same actionable late-confirm UX in Admin confirm path (defensive) |

Normal within-window confirm must not surface raw Held/Released/GUID/`not_active` as business copy.
