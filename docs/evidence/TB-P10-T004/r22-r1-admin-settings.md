# TB-P10-T004-R22-R1 — Admin Settings

Tab هویت خرید / هویت مشتری در فرایند خرید.

`GET/PUT /v1/admin/settings/checkout-identity/`

Options: ورود الزامی (`AuthenticatedOnly`), خرید مهمان مجاز (`GuestAllowed`).

GuestAllowed warning: خرید مهمان می‌تواند کنترل سفارش‌های پرداخت‌نشده و محدودیت‌های رزرو موجودی را کاهش دهد.

Runtime N/O: GET 200, PUT GuestAllowed, policy switched, restore AuthenticatedOnly.
