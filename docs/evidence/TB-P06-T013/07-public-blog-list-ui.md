# 07 — Public blog list UI (TB-P06-T013)

Task: `TB-P06-T013`

## Routes / files

| Path | File |
|---|---|
| `/blogs` | `src/frontend/app/blogs/page.tsx` |
| Listing client | `src/frontend/app/blogs/blogs-ui.tsx` (`BlogsListingClient`) |
| API client | `src/frontend/app/content/content-api.ts` → `loadPublishedArticles` |

## Behavior

- Loads live Host `GET /v1/content/articles`.
- Shopeiva-shaped hero slider (top posts) + card grid.
- Cards link to `/blogs/{slug}`.
- Empty/error states are honest (no fake demo posts injected when API empty).
- Accent `#2563EB` (Tooba), Persian RTL layout.

## Status

**LIVE** — public blog list binds Content backend.
