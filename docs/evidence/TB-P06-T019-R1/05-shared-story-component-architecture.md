# 05 — Shared Story component architecture (TB-P06-T019-R1)

## Shared root

```text
src/frontend/app/stories/management/
  StoryManagementScreen.tsx   ← single management UI
  StoryStatusBadge.tsx
  story-capabilities.ts       ← ADMIN_* / SELLER_* props
  story-management-copy.ts    ← shared Persian copy
  story-capabilities.test.ts
  index.ts
```

API client helpers live in `app/stories/story-api.ts` (admin + seller list/create/submit/review).

## Design

- **One** `StoryManagementScreen` for list, create modal, detail/editor, items, schedule, review actions, rejection reason.
- Mode difference is **only** `StoryCapabilities` + which API functions are called (`listAdminStories` vs `listSellerStories`).
- No duplicate `AdminStoryEditor` / `SellerStoryEditor` visual forks.
- Security does **not** depend on props; seller publish/approve controls are absent when `canPublish`/`canReview` are false, and backend still enforces.

## Thin wrappers

| Surface | Wrapper |
|---|---|
| Admin | `AdminStoriesScreen` → `<StoryManagementScreen capabilities={ADMIN_STORY_CAPABILITIES} />` |
| Seller | `vendor-panel/stories/page.tsx` → `<StoryManagementScreen capabilities={SELLER_STORY_CAPABILITIES} />` |
