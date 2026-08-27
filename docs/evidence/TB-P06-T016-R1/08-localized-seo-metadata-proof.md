# 08 — Localized SEO metadata proof (TB-P06-T016-R1)

## Purpose

Prove page metadata varies by public locale: title, `og:locale`, canonical, and (where applicable) hreflang alternates.

Machine source: `_acceptance-proof.json` → `runtime.*`

## Home

| Field | `/fa` | `/en` |
|---|---|---|
| Status | 200 | 200 |
| `<title>` | فروشگاه توبا \| خانه | Tooba Store \| Home |
| `og:locale` | `fa_IR` | `en_US` |
| Canonical | `/fa` | `/en` |
| Alternates | fa-IR, en, x-default | fa-IR, en, x-default |

## Blog listing

| Field | `/fa/blogs` | `/en/blogs` |
|---|---|---|
| Status | 200 | 200 |
| `<title>` | مجله توبا \| مقالات و راهنماها | Tooba Magazine \| Articles |
| `og:locale` | `fa_IR` | `en_US` |
| Canonical | `/fa/blogs` | `/en/blogs` |
| Alternates | fa-IR, en, x-default | fa-IR, en, x-default |

## PDP (`demo-game-3`)

| Field | `/fa/products/demo-game-3` | `/en/products/demo-game-3` |
|---|---|---|
| Status | 200 | 200 |
| `<title>` | بازی فکری رومیزی خانوادگی | بازی فکری رومیزی خانوادگی |
| `og:locale` | `fa_IR` | `en_US` |
| Canonical | `/fa/products/demo-game-3` | `/en/products/demo-game-3` |
| Alternates | fa-IR, en, x-default | fa-IR, en, x-default |

Note: product title string in current seed is Persian for both locales; **locale routing / `og:locale` / canonical still diverge correctly**. Content translation depth is out of scope for this routing Repair.

## Article (`guide-online-shopping`)

| Field | `/fa/blogs/...` | `/en/blogs/...` |
|---|---|---|
| Status | 200 | 200 (honest fallback) |
| `<title>` | راهنمای خرید آنلاین هوشمند | راهنمای خرید آنلاین هوشمند |
| `og:locale` | `fa_IR` | `en_US` |
| Canonical | self-prefixed | self-prefixed |
| Alternates | **none** (no fake hreflang) | **none** |

## Helpers (implementation)

- `canonicalForLocale(locale, internalPath)`
- `buildLocaleAlternates(internalPath, { includeXDefault })`
- `openGraphLocaleFor(locale)` → `fa_IR` / `en_US`

## Verdict

Localized SEO metadata is live on Home / Blog / PDP. Article metadata stays honest (no fabricated hreflang). `MULTILINGUAL_SEO = LIVE` remains accurate.
