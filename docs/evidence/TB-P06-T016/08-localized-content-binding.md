# 08 — Localized content binding (TB-P06-T016)

| Surface | Binding | Fallback |
|---|---|---|
| Blog list/detail | `resolveRequestLocale` → Host Content APIs with locale | Do not fabricate EN body; serve published locale or empty/honest miss |
| Content metadata | `generateMetadata` uses active locale | Titles/descriptions locale-aware where available |
| Product/category | Catalog projections (existing); URL locale-prefixed | No invented product translations |
| Home composition | `loadHomeComposition(localeToContentApi(locale))` | Null/default composition if locale edition absent |
| Chrome strings | `messages.ts` / LocaleSwitcher | Partial dictionary; Persian-first honesty retained |

## Policy

Unavailable translation → **no fake hreflang / no fabricated copy**. Cookie never invents content language.
