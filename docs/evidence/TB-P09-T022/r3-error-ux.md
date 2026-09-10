# R3 error UX — TB-P09-T022-R3

Mapped in `AdminOrderOperationsComposer.FulfillmentOpToFa` / `MapFulfillmentException` and `admin-error-map.ts`:

| Code / message | Mapping |
| --- | --- |
| `inventory.reservation.not_active` | existing FA/EN |
| `inventory.reservation.not_found` | new FA/EN |
| `رزرو پیدا نشد.` | → `inventory.reservation.not_found` |
| `مصرف رزرو با موجودی هم‌خوان نبود.` | → `inventory.reservation.stock_mismatch` |

No raw Held/Released/Consumed enums in Admin UI.
