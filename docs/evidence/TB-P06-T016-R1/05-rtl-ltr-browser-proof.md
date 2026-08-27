# 05 — RTL / LTR browser proof (TB-P06-T016-R1)

## Purpose

Prove Persian routes render RTL and English routes render LTR at the document root, with browser captures for Architect review. SSR is the authoritative HTML contract; client sync repair closes hydration drift.

## SSR document direction (authoritative)

| URL | Status | `lang` | `dir` |
|---|---|---|---|
| `/fa` | 200 | `fa` | `rtl` |
| `/en` | 200 | `en` | `ltr` |
| `/fa/blogs` | 200 | `fa` | `rtl` |
| `/en/blogs` | 200 | `en` | `ltr` |
| `/fa/products/demo-game-3` | 200 | `fa` | `rtl` |
| `/en/products/demo-game-3` | 200 | `en` | `ltr` |
| `/fa/blogs/guide-online-shopping` | 200 | `fa` | `rtl` |
| `/en/blogs/guide-online-shopping` | 200 | `en` | `ltr` |

Source: `_acceptance-proof.json` → `runtime.*.htmlLang` / `htmlDir` and `bodySnippet` open tags.

## Client sync repair (R1)

Defect: after client navigations, `documentElement.lang` / `dir` could lag behind the public URL prefix.

Fix in `src/frontend/lib/i18n/locale-context.tsx`:

- Derive active locale from `parseLocalePrefix(window.location.pathname)`
- On locale change, set:

```ts
document.documentElement.lang = langForLocale(locale);
document.documentElement.dir = dirForLocale(locale);
```

No CSS rewrite; Shopeiva chrome continues to inherit document `dir`.

## Browser captures

| File | Intended surface | Viewport |
|---|---|---|
| `captures/01-fa-home-desktop.png` | Persian Home RTL | Desktop |
| `captures/02-en-home-desktop.png` | English Home LTR | Desktop |
| `captures/03-fa-pdp-desktop.png` | Persian PDP RTL | Desktop |
| `captures/04-en-pdp-desktop.png` | English PDP LTR | Desktop |
| `captures/05-fa-blogs-desktop.png` | Persian Blog listing RTL | Desktop |
| `captures/06-en-blogs-desktop.png` | English Blog listing LTR | Desktop |
| `captures/07-fa-article-desktop.png` | Persian Article RTL | Desktop |
| `captures/08-shopeiva-home-desktop.png` | Original Shopeiva Home (reference) | Desktop |
| `captures/09-fa-home-mobile.png` | Persian Home RTL | Mobile |
| `captures/10-en-home-mobile.png` | English Home LTR | Mobile |

## Surfaces covered by inheritance

Header, mega menu, carousels, PDP gallery, Blog listing/article, forms, breadcrumbs, pagination — same component trees; direction from root `dir` only. No per-surface direction forks introduced.

## Verdict

SSR proves `fa`→RTL and `en`→LTR. Captures 01–10 exist for visual review. LocaleProvider client sync repair prevents stale document direction after client navigation.
