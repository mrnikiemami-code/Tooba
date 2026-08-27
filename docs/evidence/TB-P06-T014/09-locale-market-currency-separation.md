# 09 — Locale / Market / Currency separation (TB-P06-T014)

| Dimension | Owner | Proof |
|---|---|---|
| Locale | `tooba_locale` cookie / UI language | `lib/i18n/locale.ts` |
| Market | Commercial context (not locale) | `assertLocaleMarketSeparation` rejects market===locale |
| Currency | Pricing/market context | Helper rejects currency===locale |
| Tax jurisdiction | Separate (unchanged) | Not inferred from UI locale |

Tests: `lib/i18n/locale.test.ts` — fa rtl, en ltr, separation assertions.

English UI may still show non-USD prices from Host; Persian UI does not hard-code Toman solely because locale=fa in pricing helpers introduced here.
