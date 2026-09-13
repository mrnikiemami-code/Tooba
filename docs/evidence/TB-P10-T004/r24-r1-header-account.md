# TB-P10-T004-R24-R1 — Header / account

Shared `StorefrontAccountMenu` on `StorefrontShopeivaHeader` (all storefront routes via `StorefrontShopeivaShell`).

Logged-out: `header-login-link` ورود.

Logged-in: `header-account-button` + dropdown — پنل `/customer-panel`, سفارش‌ها `/customer-panel/orders`, خروج.

Auth state from `GET /api/auth/me` + `AUTH_CHANGED_EVENT` after login/logout. No page-specific header.

Desktop + mobile drawer (`compact`).
