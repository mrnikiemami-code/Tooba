# 18 — Panel integration tests (TB-P06-T018)

## Scope

Frontend **nav integrity / honesty** tests for Wave 1. **No new Host APIs** → no new backend integration suites for notifications/tickets/settings modules.

## Expected frontend coverage

| Area | Assertion |
|---|---|
| Customer primary nav | Deferred hrefs (`wallet`, `tickets`, `gift-cards`, `notifications`) absent from primary nav |
| Customer settings | Profile bridge present; security/notification sections marked unavailable (no fake save controls) |
| Customer dashboard actions | Settings live; wallet tile absent |
| Seller primary nav | Deferred hrefs (`customers`, `coupons`, `reviews`, `tickets`, `gift-cards`) absent from primary nav |
| Seller settings | Renders operational live page (not stub N/A tile path) |
| Admin primary nav | Settings item absent from primary nav |

## Not covered (intentionally)

| Suite type | Why absent |
|---|---|
| Host notification list/mark-read | Foundation not selected |
| Host tickets thread/reply | Foundation not selected |
| Admin settings mutations | Module deferred |
| New unauthorized/foreign Host cases for new APIs | No new APIs |

## Existing Host tests

Prior customer ownership / seller isolation / admin SpiceDB tests remain the authority proofs (see `16`). Wave 1 does not regress them by changing auth.

## Command placeholders

See `19-final-validation.md` for `npm run test` / panel-related test invocation placeholders.
