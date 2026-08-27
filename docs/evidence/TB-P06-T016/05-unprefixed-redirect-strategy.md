# 05 — Unprefixed redirect strategy (TB-P06-T016)

| Rule | Behavior |
|---|---|
| Status | **308** Permanent Redirect |
| Target locale | `resolvePreferredLocale(tooba_locale cookie)` else `DEFAULT_LOCALE` (`fa`) |
| Path | `/` → `/{locale}`; `/products` → `/{locale}/products`; etc. |
| Query | Preserved via `request.nextUrl.clone()` |
| Loops | Prefixed paths rewrite (not redirect); excluded paths `next()` |
| Invalid locale prefix | 404 (not redirect to default) |
| SEO | Unprefixed public URLs do not remain indexable duplicates |

## Proof

See `_locale-routing-api-proof.json`:

- `/` → `/fa`
- `/products` → `/fa/products`
- `/blogs` → `/fa/blogs`
- `/fr/...` → 404
