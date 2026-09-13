# TB-P10-T004-R24-R1-R3 — Focused validation

| Check | Result |
| --- | --- |
| Header identity (name/mobile/generic) | PASS — `storefront-identity-api.test.ts` + Host `StorefrontAccountIdentityTests` |
| Dropdown panel/orders/logout | PASS — guard + runtime |
| Logout hides, DB Active | PASS — runtime G/F |
| Re-login restore | PASS — runtime I |
| Commit after click only | PASS — shipping source + runtime M/N |
| Fault stays on Shipping | PASS — runtime Q |
| `test:storefront` | 62/62 |
| `test:critical-storefront` | 16/16 |
| Login guard | PASS |
| Host identity + atomic/cart/checkout focused | 22/22 (prior) + identity Facts |
| `docs/ai/recovery-staleness.guard.test.mjs` | 4/4 |
| `git diff --check` | PASS |

No unrelated full suites. No TB-P10-T005.
