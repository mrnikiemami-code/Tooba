# 15 — Locale routing tests (TB-P06-T016)

## Unit (`lib/i18n/routing.test.ts`)

| Case | Assert |
|---|---|
| `localePath` | `/fa`, `/en/products`, `/fa/blogs/guide` |
| `parseLocalePrefix` | extracts locale + internal |
| `stripLocalePrefix` | removes prefix |
| Invalid locale | `parseInvalidLocalePrefix('/fr/...')` → `fr` |
| Public vs excluded | `/` `/products` public; `/admin` excluded |
| Canonical/hreflang | `canonicalForLocale` + `buildLocaleAlternates` incl. x-default |

## Existing i18n

`lib/i18n/locale.test.ts` — RTL/LTR + market/currency separation.

## Suite status (Task validation)

| Command | Result |
|---|---|
| Frontend tests (incl. routing/i18n/critical-storefront) | **PASS** |
| Frontend build | **PASS** |
| Middleware first-load | ~34.7 kB (build report) |
| Backend | **unchanged** — no backend test delta required |
