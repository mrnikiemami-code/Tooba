# TB-P10-T004-R24-R1 — Focused validation

| Suite | Result |
| --- | --- |
| `storefront-cart-api.test.ts` (merge/continuity + no guest POST when auth) | PASS |
| `storefront-cart-ui.guard.test.ts` (header account/logout) | PASS |
| `storefront-checkout-api.test.ts` (First/Last body) | PASS |
| `storefront-shipping-api.test.ts` (split name UI) | PASS |
| `storefront-pending-payment-api.test.ts` | PASS |
| `customer-address-api.test.ts` | PASS |
| `storefront-login.guard.test.ts` | PASS |
| `npm run test:storefront` | 56/56 |
| `npm run test:critical-storefront` | 16/16 |
| `npm run test:customer` | 33/33 |
| `AtomicCheckoutCommitTests` | 2/2 |
| `CartFoundationTests` merge/source Facts | PASS |
| `AddressBookFoundationTests` entity/HTTP Facts | PASS |
| `CheckoutAbusePolicyTests` | PASS |
| `CheckoutIdentityContractTests` | PASS |
| `StorefrontPendingPaymentTests` | 11/11 |
| `docs/ai/recovery-staleness.guard.test.mjs` | 4/4 (`CURRENT_TASK_ID=TB-P10-T004-R24-R1`) |
| `git diff --check` | PASS |

No unrelated full suites.
