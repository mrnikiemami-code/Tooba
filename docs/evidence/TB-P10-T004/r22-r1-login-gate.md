# TB-P10-T004-R22-R1 — Login Gate

FE: cart continue → `loginPath(locale, /{locale}/shipping)`. Shipping/payment `useCheckoutAuthGate`.

Backend `CheckoutIdentityGate.EnsureCheckoutActorAsync` on shipping projection/selection/commit, checkout preview/submit, payment initiate/wallet-quote.

Machine: `checkout.authentication_required`
FA: برای ادامه فرایند خرید وارد حساب خود شوید.

Runtime anonymous: shipping/checkout/payment 401 with that code. After OTP, shipping projection 200.
