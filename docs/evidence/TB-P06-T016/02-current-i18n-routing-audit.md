# 02 — Current i18n routing audit (TB-P06-T016)

| Surface | Pre-T016 | Post-T016 |
|---|---|---|
| Locale cookie | `tooba_locale` fa\|en preference | Same cookie; preference for unprefixed redirect only |
| Root `lang`/`dir` | Cookie-driven in `app/layout.tsx` | URL prefix via `x-tooba-locale` header first, then cookie |
| Route helpers | `lib/i18n/locale.ts` only | + `lib/i18n/routing.ts` canonical helpers |
| Middleware | None for locale | `middleware.ts` rewrite/redirect |
| Link helpers | Raw `next/link` | `LocalizedLink` + `useLocalizedPath` |
| LocaleSwitcher | Cookie write + soft reload | Navigates `/fa` ↔ `/en` same internal path |
| Metadata | OG locale from cookie | + `canonicalForLocale` / `buildLocaleAlternates` |
| Sitemap | Unprefixed static/dynamic | Locale-prefixed + hreflang |
| Blog locale | Host `locale` + cookie | Prefix → `resolveRequestLocale` → content API |
| Product locale path | Unprefixed PDP | `/fa\|en/products/{slug}` rewrite to same page |
| Page Composition | `locale` query optional | Home loads via `localeToContentApi(locale)` |

## Separation preserved

`Locale ≠ Market ≠ Currency` (`assertLocaleMarketSeparation` unchanged).
