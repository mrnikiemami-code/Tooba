# TB-P10-T004-R24 — Customer UX

Real browser (Cursor browser tab):

- `/fa/login` title «ورود \| توبا», heading «ورود به حساب», mobile field, «ارسال کد», RTL
- `/en/login` heading «Sign in», «Mobile number», «Send code», LTR smoke
- `/fa/cart` «سبد خرید شما», empty-cart copy, Host shipping note, no raw codes

Host/API + FE:

- Anonymous cart 200; shipping 401 authentication_required
- OTP `09111111111` / `123456`
- Merge lines=1, no reservation/order
- Limit notice `StorefrontCheckoutLimitNotice`: FA Host detail; `#pending-payments` + `/customer-panel/orders`; churn says stock not reserved
- Cart intact on both 409s
- Cancel/hide APIs unchanged
- Countdown is local `setInterval` on `holdEndsAt` (no per-second fetch)
