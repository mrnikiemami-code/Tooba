# 08 — Public article detail UI (TB-P06-T013)

Task: `TB-P06-T013`

## Routes / files

| Path | File |
|---|---|
| `/blogs/[slug]` | `src/frontend/app/blogs/[slug]/page.tsx` |
| Detail client | `src/frontend/app/blogs/[slug]/blog-detail-ui.tsx` (`BlogDetailClient`) |

## Behavior

- Client loads `GET /v1/content/articles/{slug}` via `loadPublishedArticleBySlug`.
- Renders cover, author, publish date, category chip, title, excerpt, body (whitespace-pre-wrap / prose).
- Not-found UI with link back to `/blogs`.
- Server `generateMetadata` uses SEO fields (see `06-content-seo-proof.md`).

## Status

**LIVE** — article detail binds published Host content by stable slug.
