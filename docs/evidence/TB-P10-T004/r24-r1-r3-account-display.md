# TB-P10-T004-R24-R1-R3 — Account display

Canonical projection: `StorefrontAccountIdentity` + FE `storefrontAccountLabel` / `loadStorefrontSession`.

Precedence: DisplayName → First+Last → Mobile → generic حساب کاربری.

Development OTP user `09111111111` has no profile name. `/v1/auth/me` returns mobile only. Header label is `09111111111`, not generic حساب کاربری.

`StorefrontAccountMenu` is the only storefront account control (desktop header + compact mobile drawer). Dropdown: پنل مشتری / سفارش‌های من / خروج از حساب کاربری.

Same identity on `/fa`, `/fa/cart`, `/fa/shipping`. Screenshot: `r24-r1-r3-header-identity.png`, `r24-r1-r3-account-dropdown.png`.

Shipping recipient is not read for header identity.
