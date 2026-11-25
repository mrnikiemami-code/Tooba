# TB-296-R9 receipts

## Identity and timing

| Item | Value |
|---|---|
| Base | 6426349f7c4d0a6b4423e64a370156a067a88a28 |
| Implementation commit | bd05747712f2ebd022bd0bd8f89559f9eac58d6a |
| Start | 2026-11-25 17:18:53 +03:30 |
| Implementation end | 2026-11-25 18:52:46 +03:30 |
| Duration | 01:33:52 |
| FE | http://127.0.0.1:3000 (PID 36624 at capture) |
| Host | http://127.0.0.1:5088 (PID 46736 at capture) |
| Build | QtUkz110oTvxQPkjamMqI |
| Health | ready; SingleStore; inmemory authorization; messaging Healthy |

## Gates

| Gate | Result | Evidence |
|---|---|---|
| typecheck | EXIT 0 | `evidence/r9-typecheck.log` |
| lint | EXIT 0; pre-existing unrelated warnings | `evidence/r9-lint.log` |
| build | EXIT 0 | `evidence/r9-build.log` |
| mounted workspace | 26/26 | `evidence/r9-mount.log` |
| admin/seller API | 16/16 | `evidence/r9-api-tests.log` |
| model/i18n | 15/15 | `evidence/r9-model-i18n.log` |
| Offer backend | 97/97 | `evidence/r9-offer-tests.log` |
| Catalog/Localization/Stage2 guards | 11/11 | `evidence/r9-host-guards-stage2.log` |
| orders/products/campaigns | 10/10 rows (orders total 11, campaigns total 11) | `evidence/r9-regression.json` |
| OpenSpec strict | valid | `evidence/r9-openspec.log` |
| Runtime | 32 checks / 0 errors | `evidence/r9-runtime/verify-r9.json` |
| Cleanup | ok true | `evidence/r9-runtime/cleanup.json` |
| Impeccable source | `[]` | `evidence/r9-impeccable-source.json` |
| Impeccable URL | 12 findings; 1 workspace nested-cards | `evidence/r9-impeccable-url.json` |

## Runtime highlights

- Pre-create tabs: pricing/conditions enabled after valid identity, inventory/history disabled; one save created one Draft with amount 876543, Custom/9, qty 2..6.
- Toast: success visible, no `offer-save-notice`, gone after 4.8s; provider config autoClose=3000.
- Inventory reason: blank rejected; correction 199→50 saved; restored 199.
- Two positions: first 10 saved, second 20 draft/reason preserved after aborted write; retry only second; restored [0,0].
- Locale: `/fa/admin/offers/457e...?locale=ru-RU` gives dir=ltr, lang=ru-RU, Russian identity tab; admin list Russian title.
- Security: cross-owner read 404, write 404, seller admin fulfillment 403.
- Cleanup: R9 offers 0; roles [], assignments [], ceiling enabled 0; both baselines restored.

## Fixture disposition

- Seed `457e375f-ceec-4b66-b81e-3829c4e15893`: inventory 199 restored and reread.
- Two-position `01a0ba10-5a31-7000-b733-4cd74ef3763a`: both positions 0 restored and reread.
- R9-created offers `TB296-R9-*`: archived (not deleted), search returns 0.
- Temporary seller-B role/assignment removed; ceiling restored to 0 enabled.
