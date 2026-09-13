# TB-P10-T004-R24-R1 — Runtime A–T

Host `http://127.0.0.1:5088` + FE `http://127.0.0.1:3000`. Demo OTP `09111111111` / `123456`.

| Step | Result |
| --- | --- |
| A logout / fresh anonymous | PASS — `/api/auth/me` 401; header shows ورود |
| B add 2 products | PASS — Adidas shirt + Apple ultrabook via ATC |
| C Cart count=2 | PASS — `/fa/cart` «۲ کالا در سبد خرید» |
| D continue → Login | PASS — `/fa/login?returnTo=/fa/shipping` |
| E OTP login | PASS |
| F authenticated header | PASS — حساب کاربری; no ورود. `r24-r1-header-account.png` |
| G Cart still=2 | PASS — `GET /v1/storefront/cart/current` Active `itemCount=2` cart `01a09a96-edb4-7000-8dd7-1eafe33893b8`. `r24-r1-cart-after-login.png` |
| H Shipping same 2 | PASS — summary ۲ / ۹٬۴۷۳٬۶۷۷. `r24-r1-shipping-before-commit.png` |
| I Address First/Last | PASS — نام / نام خانوادگی separate. `r24-r1-address-name-fields.png` |
| J save address; Cart still=2 | PASS — draft + saved-address select; badge ۲ |
| K Home → Cart → Shipping | PASS — header حساب کاربری + badge ۲ on all three |
| L account menu | PASS — پنل مشتری / سفارش‌های من / خروج |
| M Customer Orders | PASS — `/fa/customer-panel/orders` lists pending + history |
| N Logout | PASS — mobile drawer logout; session 401; no cart leak to guest |
| O login again; cart restored | PASS — same Active cart `itemCount=2` |
| P continue-to-payment | PASS — checkout `01a09a9b-3880-7000-af7a-0a21d7de6ac7` |
| Q badge 0 only after COMMIT | PASS — badge ۲ during «در حال ثبت…»; after COMMIT `GET /v1/storefront/cart/current` 404 + header ۰. `r24-r1-after-commit-badge-zero.png` |
| R Payment summary | PASS — `/fa/payment?checkoutId=01a09a9b-3880-7000-af7a-0a21d7de6ac7` پست پیشتاز / ۲۰۰٬۰۰۰ / ۱۰٬۵۲۶٬۳۰۸ / در انتظار پرداخت |
| S fault commit | PASS — covered by `AtomicCheckoutCommitTests` 2/2 (live fault not re-injected) |
| T mobile header/account/cart | PASS — drawer حساب کاربری + پنل / سفارش‌ها / خروج |

Notes:

- First shipping paint after login briefly shows ۰ while `current` hydrates; authoritative cart stayed Active with 2 lines.
- Selecting a legacy saved address (`محمد لمامی`) did not crash and did not guess a First/Last split.
- Payment recipient line displayed the saved-address `RecipientName` fallback (`محمد لمامی`) after that select; new First/Last fields remained the writable model (`علی` / `رضایی` entered on the form). Visual concern only; `USER_VISUAL_ACCEPTED=NO`.
- Host cookie origin must stay `127.0.0.1` (a `localhost` redirect drops the session).
