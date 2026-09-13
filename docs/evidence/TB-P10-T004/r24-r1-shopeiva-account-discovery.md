# TB-P10-T004-R24-R1 — Shopeiva account discovery

Inspected purchased Shopeiva header:

`D:\Users\User\source\repos\SarvNewVerRequirment\FrontStarter\shopeiva\src\components\common\Header\Header.jsx` (~378–408)

Logged-out: ورود.

Authenticated: user button + dropdown:

- پروفایل → `/user-panel/profile`
- سفارشات → `/user-panel/orders`
- خروج

Tooba already has customer panel routes. Reused the same dropdown pattern with Tooba blue `#2563EB` (not Shopeiva red `#E53935`):

- پنل مشتری → `/customer-panel`
- سفارش‌های من → `/customer-panel/orders`
- خروج از حساب کاربری → `POST /api/auth/logout`

One shared `StorefrontAccountMenu` inside `StorefrontShopeivaHeader` (desktop + mobile drawer). No second panel invented.
