# 10 — Locale and composition compatibility (TB-P06-T017)

## Locale filter (backend)

`StoryRules.MatchesLocale` + public query `?locale=`:

| Request | Seeded result |
|---|---|
| `fa` | موبایل (`fa`) + بازی (`null`); **not** English rail |
| `en` | English rail (`en`) + بازی (`null`) |
| (omitted) | All Active stories for tenant (locale-null + all locales) |

Market filter independent; not inferred from locale.

## Frontend locale

`HomeStoriesSection` uses `useLocale()` so `/fa` and `/en` homes refetch the matching public list after T016 prefixed routing.

## Page composition

| Piece | Behavior |
|---|---|
| Section type | `stories` (T015 catalog) |
| Renderer | `renderHomeSection` → `<HomeStoriesSection />` |
| Admin composition | Order/visibility only; no CSS/HTML injection |
| Empty / unloaded | Section renders nothing (no fake placeholders) |

Composition remains locale-route compatible: stories section appears on composed Home under `/fa` and `/en` without changing composition ownership rules.
