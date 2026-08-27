# 03 — Canonical locale policy (TB-P06-T016)

| Policy | Value |
|---|---|
| Supported public locales | `fa`, `en` (`LOCALES` in `lib/i18n/locale.ts`) |
| Default | `fa` (`DEFAULT_LOCALE`) |
| Canonical URL identity | Locale prefix in path (`/fa/...`, `/en/...`) |
| Cookie role | Preference / fallback for unprefixed → prefixed 308 only |
| Request header | `x-tooba-locale` set by middleware on rewrite |
| Invalid 2-letter prefix (e.g. `/fr`) | Rewrite to `/not-found` → **404** |
| Resolver order | Header → query → cookie → default (`resolveRequestLocale`) |
| Content API locale | `localeToContentApi`: fa→`fa-IR`, en→`en` |
| hreflang tags | `fa-IR`, `en`; optional `x-default` → default locale path |
| Single source | Helpers in `routing.ts` / `locale.ts` — no scattered hard-coded lists in UI |

## Forbidden

- Using Market or Currency as locale
- Emitting fake hreflang for unpublished locales
- Cookie alone as canonical public identity
