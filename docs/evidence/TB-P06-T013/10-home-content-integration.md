# 10 — Home content integration (TB-P06-T013)

Task: `TB-P06-T013`

## Canonical path

| Piece | Location |
|---|---|
| Storefront compose | Host `StorefrontComposer` → `latestArticles` from `IContentDirectory.ListPublishedForHomeAsync` |
| Home props | `storefront-home.tsx` passes `latestArticles` |
| UI | `HomeArticlesSection` in `storefront-home-repair-sections.tsx` |

## Behavior after T013

- Home rail cards link to `/blogs/{slug}` (not dead anchors).
- «مشاهده همه» links to `/blogs`.
- Article cards no longer expose a fake local heart/like toggle (removed in gap closure).
- Data remains Host Published articles only; empty rail hides section (`articles.length === 0`).

## Status

**LIVE** — Home `latestArticles` is the canonical Content integration point.
