# 07 — RTL/LTR runtime proof (TB-P06-T016)

| URL | `html lang` | `html dir` | Status |
|---|---|---|---|
| `/fa` | `fa` | `rtl` | 200 |
| `/en` | `en` | `ltr` | 200 |
| `/fa/blogs` | `fa` | `rtl` | 200 |
| `/en/blogs` | `en` | `ltr` | 200 |

## Mechanism

- Middleware sets `x-tooba-locale` on rewrite.
- `app/layout.tsx` reads header first → `langForLocale` / `dirForLocale`.
- No CSS rewrite; existing Shopeiva chrome inherits document direction.

## Surfaces covered by reuse

Header, mega menu, carousels, PDP, Blog, Article, forms, breadcrumbs, pagination — same components; direction from root `dir`.
