# 14 — i18n UI tests (TB-P06-T014)

| Suite | Command | Result |
|---|---|---|
| Locale helpers | `npm run test:i18n` | PASS |
| Content API | `npm run test:content` | PASS |
| Home structure | `npm run test:home` | PASS (via full `npm run test`) |

Covered: `dirForLocale` fa=rtl / en=ltr; `assertLocaleMarketSeparation` locale≠currency≠market.
