# 08 — Frontend live binding (TB-P06-T017)

## Removed

- Fake `STORY_IMAGES` category-circle stories from `storefront-home.tsx` production UI path.
- Home stories no longer hardcode demo circles; section returns `null` until live cards load.

## Live client

| File | Role |
|---|---|
| `app/stories/story-api.ts` | `fetchPublicStories(locale)`, admin list/create/enable/schedule/items mappers |
| `app/storefront/stories/home-stories.tsx` | Rail; `useLocale()` → `fetchPublicStories` |
| `app/storefront/stories/story-modal.tsx` | Full-screen modal over live `PublicStoryCard` |
| `storefront-home.tsx` | Composition case `stories` → `<HomeStoriesSection />` |

## Binding behavior

1. Client mounts HomeStoriesSection when composition includes `stories`.
2. Fetches Host `/v1/storefront/stories?locale={current}`.
3. Filters to cards with `items.length > 0`.
4. Opens StoryModal on circle click (Shopeiva chrome).

Admin panel uses same `story-api.ts` against `/v1/admin/stories*` with admin actor header.
