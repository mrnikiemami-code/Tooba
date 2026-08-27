# 06 — Locale link generation (TB-P06-T016)

## Helpers

| Helper | Role |
|---|---|
| `localePath(locale, internal)` | Canonical public URL |
| `useLocalizedPath()` | Client helper for active locale |
| `LocalizedLink` | Storefront `Link` wrapper; auto-prefix unless `unprefixed` / panel path |
| `useSwitchLocalePath()` | Same bare path under other locale |

## Surfaces updated

Header, footer, home sections, product cards, listing, PDP, cart, checkout, merchandising, blogs list/detail — import `LocalizedLink as Link`.

## LocaleSwitcher

Writes cookie + `window.location.href = switchLocalePath(next)` (preserves search). Navigates `/fa/...` ↔ `/en/...`.

## Guard

No hard-coded `/fa` or `/en` string literals required in presentational components; panels stay unprefixed automatically.
