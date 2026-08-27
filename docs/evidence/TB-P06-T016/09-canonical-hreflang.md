# 09 — Canonical / hreflang (TB-P06-T016)

## Helpers

- `canonicalForLocale(locale, internalPath)` — self-referencing canonical for active locale
- `buildLocaleAlternates(internalPath, { includeXDefault })` — `fa-IR` + `en` (+ optional `x-default` → `/fa/...`)

## Applied pages

| Page | Canonical | Alternates |
|---|---|---|
| Home | `/{locale}` | fa-IR, en, x-default |
| PDP | `/{locale}/products/{slug}` | fa-IR, en, x-default |
| Blogs list | `/{locale}/blogs` | fa-IR, en, x-default |
| Article | `/{locale}/blogs/{slug}` | self canonical (slug-level) |

## Rules

- Real variants only for supported `LOCALES`
- Market ≠ hreflang
- Unprefixed URLs redirected — not alternate targets
