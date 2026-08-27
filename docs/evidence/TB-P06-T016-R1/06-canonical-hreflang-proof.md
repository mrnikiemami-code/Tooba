# 06 — Canonical / hreflang proof (TB-P06-T016-R1)

## Purpose

Prove self-referencing locale-prefixed canonicals and real-variant hreflang (`fa-IR`, `en`, `x-default`) where both locale routes exist. Prove honesty when a translation does **not** exist (article): no fabricated alternates.

Machine source: `_acceptance-proof.json` → `runtime.*.canonicalHref` / `hreflang`

## Home

| Locale URL | Canonical | hreflang set |
|---|---|---|
| `/fa` | `/fa` | `fa-IR` → `/fa`; `en` → `/en`; `x-default` → `/fa` |
| `/en` | `/en` | `fa-IR` → `/fa`; `en` → `/en`; `x-default` → `/fa` |

## Blog listing

| Locale URL | Canonical | hreflang set |
|---|---|---|
| `/fa/blogs` | `/fa/blogs` | `fa-IR` → `/fa/blogs`; `en` → `/en/blogs`; `x-default` → `/fa/blogs` |
| `/en/blogs` | `/en/blogs` | same pair + x-default → `/fa/blogs` |

## PDP (`demo-game-3`) — both variants exist

| Locale URL | Canonical | hreflang set |
|---|---|---|
| `/fa/products/demo-game-3` | `/fa/products/demo-game-3` | `fa-IR` → fa PDP; `en` → en PDP; `x-default` → fa PDP |
| `/en/products/demo-game-3` | `/en/products/demo-game-3` | same pair + x-default → fa PDP |

## Article (`guide-online-shopping`) — fa-IR only

| Locale URL | Status | Canonical | hreflang |
|---|---|---|---|
| `/fa/blogs/guide-online-shopping` | 200 | `/fa/blogs/guide-online-shopping` | **[]** (empty) |
| `/en/blogs/guide-online-shopping` | 200 (honest fallback) | `/en/blogs/guide-online-shopping` | **[]** (empty) |

No fake `en` alternate is emitted for an article that lacks a real English variant. Rule: **HREFLANG = REAL_VARIANTS_ONLY**.

## Policy checks

| Rule | Result |
|---|---|
| Canonical is self-prefixed | PASS on all probed pages |
| Unprefixed URLs as alternates | Not used (unprefixed redirect away) |
| Market ≠ hreflang | Locale codes only (`fa-IR` / `en`) |
| `x-default` | Points at default locale (`fa`) equivalent |

## Verdict

Canonical + hreflang proven on Home, Blog listing, and PDP. Article correctly omits hreflang when only one real language variant exists.
