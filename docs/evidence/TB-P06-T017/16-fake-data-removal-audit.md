# 16 — Fake data removal audit (TB-P06-T017)

## Target

Remove production fake category-circle stories driven by `STORY_IMAGES` from storefront home.

## Audit

| Check | Result |
|---|---|
| `STORY_IMAGES` in `storefront-home.tsx` | **Gone** (guard asserts `doesNotMatch`) |
| Grep production storefront UI for `STORY_IMAGES` | **No matches** in `src/frontend` app TS/TSX at evidence authoring |
| Live binding | `HomeStoriesSection` → `fetchPublicStories` |
| Empty state | Section returns `null` (no fake circles) |
| SSR/home probe | Proof script checks `!hasFakeStoryImagesConst` on `/fa` HTML |

## Remaining references (allowed)

- Test name / guard regex mentioning `STORY_IMAGES` as negative assertion
- Evidence docs / this file

Production UI path uses only Host-backed story cards.
