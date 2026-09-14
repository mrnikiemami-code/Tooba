# TB-P10-T008 — Commerce/auth regression

Theme work did not change cart/auth/checkout contracts.

| Scenario | Proof |
| --- | --- |
| account identity | storefront-identity-api 8/8 |
| logout Cart persistence | cart-api + identity tests; no ThemeMode writes |
| re-login restore | one merge per login (existing test) |
| anonymous merge | storefront-cart-api merge tests |
| Shipping Cart Active pre-COMMIT | storefront-shipping-api |
| COMMIT converts | storefront-checkout-api |
| injected 409 keeps Cart + Shipping | existing shipping/checkout tests |
| no duplicate auth/merge/checkout | identity + cart merge tests |

test:storefront 67/67. test:critical-storefront 16/16.
