# TB-P10-T004-R24-R1-R1 — Focused validation

| Suite | Result |
| --- | --- |
| `StorefrontRecipientCanonicalizationTests` | 8/8 |
| `AddressBookFoundationTests` legacy + explicit win + snapshot Facts | PASS |
| `CartFoundationTests.Login_merge_adopts_guest_when_leftover_authenticated_cart_is_empty` | PASS |
| `AtomicCheckoutCommitTests` | PASS |
| `CheckoutIdentityContractTests` / `CheckoutAbusePolicyTests` | PASS (prior focused batch 30/31 then snapshot Null fix → 14/14 re-run) |
| `npm run test:storefront` | 56/56 |
| `npm run test:critical-storefront` | 16/16 |
| `npm run test:customer` | 34/34 (includes recipientDisplayName first+last) |
| `app/login/storefront-login.guard.test.ts` | PASS |
| `docs/ai/recovery-staleness.guard.test.mjs` | 4/4 (`CURRENT_TASK_ID=TB-P10-T004-R24-R1-R1`) |
| `git diff --check` | PASS |

No unrelated full suites.
