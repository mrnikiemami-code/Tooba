# 02 — Locale route runtime proof (TB-P06-T016-R1)

## Purpose

Prove `/fa` and `/en` public routes are live with correct SSR `html lang` / `dir`, and that primary storefront surfaces resolve under both prefixes.

Machine source: `_acceptance-proof.json` → `runtime.*`  
Recorded: `2026-08-27T03:18:16.224Z`  
Predecessor: `5db5ddc220842a98e0447bafa4d885edf62397a6`

## Host / FE / Shopeiva

| Probe | Status |
|---|---|
| Host `/health/live` | 200 |
| Host `/health/ready` | 200 |
| Shopeiva `http://127.0.0.1:3001/` | 200 |

## Prefixed public routes (no redirect)

| URL | Status | `html lang` | `html dir` | Title (SSR) |
|---|---|---|---|---|
| `http://127.0.0.1:3000/fa` | **200** | `fa` | `rtl` | فروشگاه توبا \| خانه |
| `http://127.0.0.1:3000/en` | **200** | `en` | `ltr` | Tooba Store \| Home |
| `http://127.0.0.1:3000/fa/blogs` | **200** | `fa` | `rtl` | مجله توبا \| مقالات و راهنماها |
| `http://127.0.0.1:3000/en/blogs` | **200** | `en` | `ltr` | Tooba Magazine \| Articles |
| `http://127.0.0.1:3000/fa/products/demo-game-3` | **200** | `fa` | `rtl` | بازی فکری رومیزی خانوادگی |
| `http://127.0.0.1:3000/en/products/demo-game-3` | **200** | `en` | `ltr` | بازی فکری رومیزی خانوادگی |
| `http://127.0.0.1:3000/fa/blogs/guide-online-shopping` | **200** | `fa` | `rtl` | راهنمای خرید آنلاین هوشمند |
| `http://127.0.0.1:3000/en/blogs/guide-online-shopping` | **200** | `en` | `ltr` | (honest fallback; same title string in seed) |

`location` was `null` on all of the above (direct 200, not a bounce).

## SSR document open tags (snippets)

**fa Home**

```html
<html lang="fa" dir="rtl">
```

**en Home**

```html
<html lang="en" dir="ltr">
```

## Mechanism (unchanged from T016 + R1 client sync)

1. Middleware matches `/{locale}/...`, rewrites to internal App Router path, sets `x-tooba-locale`.
2. Root layout reads locale header → `langForLocale` / `dirForLocale` for SSR `<html>`.
3. R1 repair: `LocaleProvider` syncs `documentElement.lang` / `dir` from the public URL prefix so client navigations cannot leave a stale root direction.

## Verdict

Locale-prefixed public routing is **live** for Home, Blog listing, PDP, and Article under both `fa` and `en`. SSR lang/dir matches the URL prefix.
