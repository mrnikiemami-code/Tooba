# 02 — Shopeiva content source map (TB-P06-T013)

Reference root: `../SarvNewVerRequirment/reference/shopeiva/`

| Shopeiva source | Role | Tooba target |
|---|---|---|
| `src/app/blogs/page.jsx` | Public blog listing route + metadata | `src/frontend/app/blogs/page.tsx` |
| `src/components/blogs/blogsUi.jsx` | Listing client: slider + grid | `src/frontend/app/blogs/blogs-ui.tsx` |
| `src/components/ui/BlogCard/BlogCard.jsx` | Card chrome (cover, tags, author, date) | Inlined card markup in `blogs-ui.tsx` + home `HomeArticlesSection` |
| `src/components/blogs/blogDetailClient.jsx` | Article detail client | `src/frontend/app/blogs/[slug]/blog-detail-ui.tsx` |
| `src/app/blogs/[id]/[slug]/page.jsx` | Detail route (id+slug) | `src/frontend/app/blogs/[slug]/page.tsx` (slug-only; Host slug is stable) |
| `src/components/blogs/blogsClient.jsx` | Alternate listing wrapper | Not required; Tooba uses `BlogsListingClient` directly |
| `src/app/blogs/loading.jsx` | Route loading | Deferred; client shows inline loading text |

## Fidelity notes

- Visual contract: Shopeiva structure (slider + cards + accent blue `#2563EB` Tooba brand) adapted, not pixel-copied wholesale.
- Data source: Shopeiva uses static/demo posts; Tooba binds Host `GET /v1/content/articles` and `/{slug}`.
- Likes/views counters from Shopeiva are **not** ported (honestly deferred — see `13-fake-data-action-audit.md`).
