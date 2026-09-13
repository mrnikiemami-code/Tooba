# TB-P10-T004-R24-R1-R3 — Conversion timeline

CartId: `01a09b32-787c-7000-91b9-0be7d96bf8c4`

Checkout after COMMIT: `01a09b33-751e-7000-9977-1ad5999c8e35`

| T | Event | Cart | Badge | Route |
| --- | --- | --- | --- | --- |
| T0 | Cart page | Active, 2 lines | ۲ | `/fa/cart` |
| T1 | Shipping loaded | Active | ۲ | `/fa/shipping` |
| T2 | Address/method/date | Active | ۲ | `/fa/shipping` |
| T3 | click ادامه | Active | ۲ | `/fa/shipping` — 1789310235839 |
| T4 | commit request | Active | ۲ | POST `/v1/storefront/shipping/commit` — 1789310235892 |
| T5–T7 | Host atomic COMMIT | Converted + Order + Cycle | — | backend |
| T8 | commit 200 | Converted | — | 1789310235973 |
| T9 | navigate Payment | source Converted | — | `/fa/payment?checkoutId=…` — 1789310237948 |
| T10 | header refresh | new empty Active | ۰ | Payment |

Order: T3 < T4 < T8 < T9. FE does not pre-clear. `persistCommittedCheckoutAndDetachActiveCart` runs only after HTTP OK. `router.push(localizePath('/payment?checkoutId=…'))`.

Fault Q: injected commit 409 → stay `/fa/shipping`, badge ۲, no Payment.

Screenshot: `r24-r1-r3-shipping-before-commit.png`, `r24-r1-r3-payment-after-commit.png`.
